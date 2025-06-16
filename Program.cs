using Asv.Common;
using Asv.Mavlink;
using DroneConsoleApp.Services;
using DroneControllerApp.DroneConfig;
using DroneControllerApp.DroneControllerServices;
using DroneControllerApp.DroneServices;

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

using var droneController = new DroneController(drone);
var mission = new MissionController(droneController);

try
{
    int lat = MavlinkTypesHelper.LatLonDegDoubleToFromInt32E7To(55.7558);
    int lon = MavlinkTypesHelper.LatLonDegDoubleToFromInt32E7To(37.6173);
    int alt = MavlinkTypesHelper.AltFromDoubleMeterToInt32Mm(20);

    GeoPoint target = MavlinkTypesHelper.FromInt32ToGeoPoint(lat, lon, alt);

    await mission.Run(20.0, target);

    explorer.Dispose();
    router.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"Mission failed: {ex.Message}");
}