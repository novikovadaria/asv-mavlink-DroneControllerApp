using Asv.IO;
using DroneControllerApp.DroneConfig;
using Microsoft.Extensions.Logging;

namespace DroneControllerApp.Interfaces
{
    public interface IDroneFactory
    {
        Task<(IClientDevice drone, IDeviceExplorer explorer)> FindAndPrepareDrone(DroneFactoryConfig config);
    }
}
