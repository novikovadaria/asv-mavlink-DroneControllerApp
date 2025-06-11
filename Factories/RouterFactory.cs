using Asv.IO;
using Asv.Mavlink;
using DroneControllerApp.Interfaces;

namespace DroneControllerApp.DroneServices
{
    public class RouterFactory : IRouterFactory
    {
        public IProtocolRouter CreateRouter()
        {
            var protocol = Protocol.Create(builder =>
            {
                builder.RegisterMavlinkV2Protocol();
                builder.Features.RegisterBroadcastFeature<MavlinkMessage>();
                builder.Formatters.RegisterSimpleFormatter();
            });

            var router = protocol.CreateRouter("ROUTER");

            router.AddTcpClientPort(p =>
            {
                p.Host = "127.0.0.1";
                p.Port = 5760;
            });

            return router;
        }
    }

}
