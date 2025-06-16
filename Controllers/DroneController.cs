using Asv.Common;
using Asv.IO;
using Asv.Mavlink;
using DroneControllerApp.View;
using Microsoft.Extensions.Logging;
using R3;
using System.Xml.Linq;


namespace DroneConsoleApp.Services
{
    public class DroneController : IDisposable
    {
        private readonly IClientDevice? _drone;
        private readonly ConsoleView _consoleView;
        private IModeClient? _mode;
        private IPositionClient? _position;
        private IHeartbeatClient? _heartbeat;
        private ControlClient? _control;
        private readonly CancellationTokenSource? _cts;


        public DroneController(IClientDevice drone)
        {
            _drone = drone;
            _consoleView = new ConsoleView();

            InitServices();
        }

        private void InitServices()
        {
            if (_drone == null) throw new InvalidOperationException("Drone not assigned");

            _control = _drone.GetMicroservice<ControlClient>()
                ?? throw new InvalidOperationException("ControlClient microservice not found");
            _position = _drone.GetMicroservice<IPositionClient>()
                ?? throw new InvalidOperationException("IPositionClient microservice not found");
            _heartbeat = _drone.GetMicroservice<IHeartbeatClient>()
                ?? throw new InvalidOperationException("IHeartbeatClient microservice not found");
            _mode = _drone.GetMicroservice<IModeClient>()
                ?? throw new InvalidOperationException("IModeClient microservice not found");

            SubscribeToPosition();
        }

        public async Task TakeOff(double altitude)
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone is not connected");

            if (_control == null)
                throw new InvalidOperationException("ControlClient is not initialized");

            var cancel = _cts?.Token ?? CancellationToken.None;

            await _control.SetGuidedMode(cancel);

            if (_mode == null)
                throw new InvalidOperationException("ModeClient is not initialized");

            await _mode.CurrentMode
                .SelectMany(_ => _control.IsGuidedMode(cancel).AsTask().ToObservable())
                .Where(isGuided => isGuided)
                .FirstAsync(cancel);

            _consoleView.ShowTakingOff(altitude);

            await _control.TakeOff(altitude, cancel);

            await Task.Delay(TimeSpan.FromSeconds(5), cancel);
        }

        public async Task FlyToAndLand(GeoPoint target, CancellationToken cancel)
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone is not connected");

            if (_control == null)
                throw new InvalidOperationException("ControlClient is not initialized");

            var currentPosition = _position?.GlobalPosition.CurrentValue;

            double newLatitude = currentPosition?.Lat + target.Latitude ?? 0;
            double newLongitude = currentPosition?.Lon + target.Longitude ?? 0;
            double newAltitude = currentPosition?.Alt + target.Altitude ?? 0;

            var adjustedTarget = new GeoPoint(newLatitude, newLongitude, newAltitude);
            
            await _control.SetGuidedMode(cancel);
            await _control.GoTo(adjustedTarget, cancel);
            await _control.DoLand(cancel);

            _consoleView.ShowLanded();
        }


        private void SubscribeToPosition()
        {
            if (_position == null) return;

            _positionSubscription = _position.GlobalPosition.Subscribe(pos =>
            {
                double latitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos?.Lat ?? 0);
                double longitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos?.Lon ?? 0);
                double altitude = MavlinkTypesHelper.AltFromMmToDoubleMeter(pos?.Alt ?? 0);

                _consoleView.ShowPosition(latitude, longitude, altitude);
            });
        }

        #region IDisposable Implementation  

        private IDisposable? _positionSubscription;
        private bool _disposed;

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _positionSubscription?.Dispose();
                _heartbeat?.Dispose();
                _mode?.Dispose();
                _drone?.Dispose();
                _control?.Dispose();
                _position?.Dispose();
                _cts?.Cancel();
                _cts?.Dispose();
            }

            _disposed = true;
        }
        #endregion
    }
}
