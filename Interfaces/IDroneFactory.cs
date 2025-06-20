using Asv.IO;
using DroneControllerApp.DroneConfig;

namespace DroneControllerApp.Interfaces
{
    public interface IDroneFactory
    {
        Task<IClientDevice> FindAndPrepareDrone(DroneFactoryConfig config);
    }
}
