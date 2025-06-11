using Asv.IO;
using Microsoft.Extensions.Logging;

namespace DroneControllerApp.Interfaces
{
    public interface IDroneFactory
    {
        Task<(IClientDevice drone, IDeviceExplorer explorer)> FindAndPrepareDrone(IProtocolRouter router);
    }
}
