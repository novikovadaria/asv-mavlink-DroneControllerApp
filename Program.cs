using Asv.Common;
using DroneConsoleApp.Services;
using DroneControllerApp.DroneConfig;
using DroneControllerApp.DroneControllerServices;
using DroneControllerApp.View;

var config = new DroneFactoryConfig
{
    DeviceTimeoutMs = 1500,
    DeviceCheckIntervalMs = 20000,
    DiscoveryTimeout = TimeSpan.FromSeconds(45),
    SystemId = 255,
    ComponentId = 255
};

var droneFactory = new DroneFactory();
try
{
    var drone = await droneFactory.FindAndPrepareDrone(config);
    using var droneController = new DroneController(drone, new ConsoleView());

    GeoPoint target = new GeoPoint(55.7558, 37.6173, 20.0);
    await droneController.Run(20.0, target);
}
finally
{
    droneFactory.Dispose();
}
