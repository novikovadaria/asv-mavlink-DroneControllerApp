namespace DroneControllerApp.DroneConfig
{
    public class DroneDiscoveryConfig
    {
        public int DeviceTimeoutMs { get; set; } = 1000;
        public int DeviceCheckIntervalMs { get; set; } = 30_000;
        public TimeSpan DiscoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public byte SystemId { get; set; } = 255;
        public byte ComponentId { get; set; } = 255;
    }


}
