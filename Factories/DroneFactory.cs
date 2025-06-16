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
        private readonly IProtocolRouter _router;
        private readonly ILogger<DroneFactory> _logger;

        public DroneFactory(IProtocolRouter router, ILogger<DroneFactory>? logger = null)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));

            _logger = logger ?? LoggerFactory
                .Create(builder =>
                {
                    builder
                        .AddConsole() 
                        .SetMinimumLevel(LogLevel.Information); 
                })
                .CreateLogger<DroneFactory>();
        }

        public async Task<(IClientDevice drone, IDeviceExplorer explorer)> FindAndPrepareDrone(DroneFactoryConfig config)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20), TimeProvider.System);

            var seq = new PacketSequenceCalculator();
            var identity = new MavlinkIdentity(config.SystemId, config.ComponentId);

            var deviceExplorer = DeviceExplorer.Create(_router, builder =>
            {
                builder.SetConfig(new ClientDeviceBrowserConfig()
                {
                    DeviceTimeoutMs = config.DeviceTimeoutMs,
                    DeviceCheckIntervalMs = config.DeviceCheckIntervalMs,
                });
                builder.Factories.RegisterDefaultDevices(
                    new MavlinkIdentity(identity.SystemId, identity.ComponentId),
                    seq,
                    new InMemoryConfiguration());
            });

            IClientDevice? drone = null;

            var foundTcs = new TaskCompletionSource();
            using var cancelReg1 = cts.Token.Register(() => foundTcs.TrySetCanceled());
            using var sub1 = deviceExplorer.Devices
                .ObserveAdd()
                .Take(1)
                .Subscribe(kvp =>
                {
                    drone = kvp.Value.Value;
                    foundTcs.TrySetResult();
                });

            await foundTcs.Task;

            if (drone is null)
            {
                await deviceExplorer.DisposeAsync();
                throw new Exception("Drone not found");
            }

            _logger.LogInformation("Drone found: {DroneName}", drone.Name);

            var readyTcs = new TaskCompletionSource();
            using var cancelReg2 = cts.Token.Register(() => readyTcs.TrySetCanceled());
            using var sub2 = drone.State
                .Subscribe(x =>
                {
                    if (x == ClientDeviceState.Complete)
                    {
                        readyTcs.TrySetResult();
                    }
                });

            await readyTcs.Task;
            _logger.LogInformation("Drone initialized: {DroneName}", drone.Name);

            return (drone, deviceExplorer);
        }
    }
}
