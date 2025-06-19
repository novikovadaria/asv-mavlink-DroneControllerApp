using Asv.Cfg;
using Asv.IO;
using Asv.Mavlink;
using DroneControllerApp.DroneConfig;
using DroneControllerApp.Interfaces;
using ObservableCollections;
using R3;
namespace DroneControllerApp.DroneControllerServices
{
    public class DroneFactory : IDroneFactory
    {
        private readonly IProtocolRouter _router;

        public DroneFactory(IProtocolRouter router)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public IDeviceExplorer CreateExplorer(DroneFactoryConfig config)
        {
            var seq = new PacketSequenceCalculator();
            var identity = new MavlinkIdentity(config.SystemId, config.ComponentId);

            return DeviceExplorer.Create(_router, builder =>
            {
                builder.SetConfig(new ClientDeviceBrowserConfig
                {
                    DeviceTimeoutMs = config.DeviceTimeoutMs,
                    DeviceCheckIntervalMs = config.DeviceCheckIntervalMs,
                });
                builder.Factories.RegisterDefaultDevices(
                    identity,
                    seq,
                    new InMemoryConfiguration());
            });
        }

        public async Task<IClientDevice> FindAndPrepareDrone(IDeviceExplorer explorer, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout, TimeProvider.System);

            IClientDevice? drone = null;

            var foundTcs = new TaskCompletionSource();
            using var cancelReg1 = cts.Token.Register(() => foundTcs.TrySetCanceled());
            using var sub1 = explorer.Devices
                .ObserveAdd()
                .Take(1)
                .Subscribe(kvp =>
                {
                    drone = kvp.Value.Value;
                    foundTcs.TrySetResult();
                });

            await foundTcs.Task;

            if (drone is null)
                throw new Exception("Drone not found");

            var readyTcs = new TaskCompletionSource();
            using var cancelReg2 = cts.Token.Register(() => readyTcs.TrySetCanceled());
            using var sub2 = drone.State
                .Subscribe(x =>
                {
                    if (x == ClientDeviceState.Complete)
                        readyTcs.TrySetResult();
                });

            await readyTcs.Task;

            return drone;
        }

    }
}
