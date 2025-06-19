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

DroneFactory droneFactory = new DroneFactory(router);
var (drone, explorer) = await droneFactory.FindAndPrepareDrone(config);

using var droneController = new DroneController(drone, new ConsoleView());
var mission = new MissionController(droneController);

try
{
    double lat = 55.7558;
    double lon = 37.6173;
    double alt = 20.0;

    GeoPoint target = new GeoPoint(lat, lon, alt); 

    await mission.Run(20.0, target);

    explorer.Dispose();
    router.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"Mission failed: {ex.Message}");
}