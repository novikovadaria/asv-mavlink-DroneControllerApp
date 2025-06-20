using Asv.Common;
using Asv.IO;
using Asv.Mavlink;
using DroneControllerApp.View;
using R3;

namespace DroneConsoleApp.Services
{
    public class DroneController : IDisposable
    {
        private readonly IClientDevice _drone;
        private readonly ConsolePositionObserver _positionObserver;
        private readonly IModeClient _mode;
        private readonly IPositionClient _position;
        private readonly IHeartbeatClient _heartbeat;
        private readonly ControlClient _control;

        private IDisposable? _positionSubscription;
        private bool _disposed;

        public DroneController(IClientDevice drone, ConsolePositionObserver positionObserver)
        {
            _drone = drone ?? throw new ArgumentNullException(nameof(drone));
         
            _control = _drone.GetMicroservice<ControlClient>()
                ?? throw new InvalidOperationException("ControlClient microservice not found");
            _position = _drone.GetMicroservice<IPositionClient>()
                ?? throw new InvalidOperationException("IPositionClient microservice not found");
            _heartbeat = _drone.GetMicroservice<IHeartbeatClient>()
                ?? throw new InvalidOperationException("IHeartbeatClient microservice not found");
            _mode = _drone.GetMicroservice<IModeClient>()
                ?? throw new InvalidOperationException("IModeClient microservice not found");

            _positionObserver = positionObserver;

            SubscribeToPosition();
        }

        public async Task Run(double altitude, GeoPoint target)
        {
            await TakeOff(altitude, CancellationToken.None);
            await FlyToAndLand(target, CancellationToken.None);
        }

        public async Task TakeOff(double altitude, CancellationToken cancel)
        {
            await _control.SetGuidedMode(cancel);

            using var sub = _mode.CurrentMode 
                .Where(mode => mode == ArduCopterMode.Guided)
                .Take(1) 
                .SubscribeAwait(async (_, ct) => await _control.TakeOff(altitude, ct));

            await Task.Delay(TimeSpan.FromSeconds(5), cancel);
        }

        public async Task FlyToAndLand(GeoPoint target, CancellationToken cancel)
        {
            await _control.SetGuidedMode(cancel);

            using var sub = _mode.CurrentMode
                .Where(mode => mode == ArduCopterMode.Guided)
                .Take(1)
                .SubscribeAwait(async (_, ct) => await _control.GoTo(target, cancel));

            await _control.DoLand(cancel);

            _positionObserver.OnLanded();
        }

        private void SubscribeToPosition()
        {
            _positionSubscription = _position.GlobalPosition.Subscribe(pos =>
            {
                double latitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos?.Lat ?? 0);
                double longitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos?.Lon ?? 0);
                double altitude = MavlinkTypesHelper.AltFromMmToDoubleMeter(pos?.Alt ?? 0);

                _positionObserver.OnPositionUpdate(latitude, longitude, altitude);
            });
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _positionSubscription?.Dispose();
                _heartbeat.Dispose();
                _mode.Dispose();
                _drone.Dispose();
                _control.Dispose();
                _position.Dispose();
            }

            _disposed = true;
        }
    }
}
