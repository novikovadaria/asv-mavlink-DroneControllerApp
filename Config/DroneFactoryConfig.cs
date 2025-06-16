namespace DroneControllerApp.DroneConfig
{
    public class DroneFactoryConfig
    {
        public int DeviceTimeoutMs { get; init; } = 1000;
        public int DeviceCheckIntervalMs { get; init; } = 30_000;
        public TimeSpan DiscoveryTimeout { get; init; } = TimeSpan.FromSeconds(30);

        public byte SystemId { get; init; } = 255;
        public byte ComponentId { get; init; } = 255;
    }


}
