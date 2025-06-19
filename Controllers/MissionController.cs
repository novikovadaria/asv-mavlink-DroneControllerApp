using Asv.Common;
namespace DroneConsoleApp.Services
{
    public class MissionController
    {
        private readonly DroneController _controller;

        public MissionController(DroneController controller)
        {
            _controller = controller;
        }

        public async Task Run(double altitude, GeoPoint target)
        {
            await _controller.TakeOff(altitude, CancellationToken.None);
            await _controller.FlyToAndLand(target, CancellationToken.None);
        }
    }


}
