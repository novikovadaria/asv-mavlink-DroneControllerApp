using Asv.IO;


namespace DroneControllerApp.Interfaces
{
    public interface IRouterFactory
    {
       IProtocolRouter CreateRouter();
    }
}
