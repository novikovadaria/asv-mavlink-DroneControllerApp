using Asv.Cfg;
using Asv.IO;
using Asv.Mavlink;
using DroneControllerApp.DroneConfig;
using DroneControllerApp.Interfaces;
using ObservableCollections;
using R3;
using Microsoft.Extensions.Logging;
namespace DroneControllerApp.DroneControllerServices
{
    public class DroneFactory : IDroneFactory
    {
        private readonly DroneFactoryConfig _config;
        private readonly ILogger<DroneFactory> _logger;

        public DroneFactory(DroneFactoryConfig config, ILogger<DroneFactory>? logger = null)
        {
            _config = config;
            _logger = logger ?? LoggerFactory
                .Create(builder =>
                {
                    builder
                        .AddConsole() 
                        .SetMinimumLevel(LogLevel.Information); 
                })
                .CreateLogger<DroneFactory>();
        }

        public async Task<(IClientDevice drone, IDeviceExplorer explorer)> FindAndPrepareDrone(IProtocolRouter router)
        {
            var tcs = new TaskCompletionSource();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20), TimeProvider.System);
            await using var cancelReg = cts.Token.Register(() => tcs.TrySetCanceled());

            // Device explorer creation
            var seq = new PacketSequenceCalculator();
            var identity = new MavlinkIdentity(_config.SystemId, _config.ComponentId);
            var deviceExplorer = DeviceExplorer.Create(router, builder =>
            {
                builder.SetConfig(new ClientDeviceBrowserConfig()
                {
                    DeviceTimeoutMs = _config.DeviceTimeoutMs,
                    DeviceCheckIntervalMs = _config.DeviceCheckIntervalMs,
                });
                builder.Factories.RegisterDefaultDevices(
                    new MavlinkIdentity(identity.SystemId, identity.ComponentId),
                    seq,
                    new InMemoryConfiguration());
            });

            // Device search
            IClientDevice? drone = null;
            using var sub = deviceExplorer.Devices
                .ObserveAdd()
                .Take(1)
                .Subscribe(kvp =>
                {
                    drone = kvp.Value.Value;
                    tcs.TrySetResult();
                });

            await tcs.Task;

            if (drone is null)
            {
                await deviceExplorer.DisposeAsync();
                throw new Exception("Drone not found");
            }

            _logger.LogInformation("Drone found: {DroneName}", drone.Name);

            // Drone init
            tcs = new TaskCompletionSource();

            using var sub2 = drone.State
                .Subscribe(x =>
                {
                    if (x == ClientDeviceState.Complete)
                    {
                        tcs.TrySetResult();
                    }
                });

            await tcs.Task;

            _logger.LogInformation("Drone initialized: {DroneName}", drone.Name);

            // Heartbeat client search
            var heartbeat = drone.GetMicroservice<IHeartbeatClient>();

            if (heartbeat is null)
            {
                await deviceExplorer.DisposeAsync();
                throw new Exception("No control client found");
            }

            _logger.LogInformation("Heartbeat client found: {HeartbeatName}", heartbeat.Id);

            // Test
            tcs = new TaskCompletionSource();

            var count = 0;
            using var sub3 = heartbeat.RawHeartbeat
                .ThrottleLast(TimeSpan.FromMilliseconds(100))
                .Subscribe(p =>
                {
                    if (p is null)
                        return;

                    if (++count >= 20)
                    {
                        tcs.TrySetResult();
                    }
                });

            await tcs.Task;

            return (drone, deviceExplorer);
        }
    }
}
