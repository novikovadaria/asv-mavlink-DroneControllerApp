using Asv.Common;
using Asv.Mavlink;
using DroneConsoleApp.Services;
using DroneControllerApp.DroneConfig;
using DroneControllerApp.DroneControllerServices;
using DroneControllerApp.DroneServices;
using DroneControllerApp.View;

var config = new DroneFactoryConfig
{
    DeviceTimeoutMs = 1500,
    DeviceCheckIntervalMs = 20000,
    DiscoveryTimeout = TimeSpan.FromSeconds(45),
    SystemId = 255,
    ComponentId = 255
};

RouterFactory routerProvider = new RouterFactory();
var router = routerProvider.CreateRouter();

var droneFactory = new DroneFactory(router);
var explorer = droneFactory.CreateExplorer(config);

try
{
    var drone = await droneFactory.FindAndPrepareDrone(explorer, config.DiscoveryTimeout);
    using var droneController = new DroneController(drone, new ConsoleView());

    GeoPoint target = new GeoPoint(55.7558, 37.6173, 20.0);
    await droneController.Run(20.0, target);
}
finally
{
    explorer.Dispose();
    router.Dispose();
}
