using Asv.Common;
using Asv.IO;
using Asv.Mavlink;
using Microsoft.Extensions.Logging;
using R3;


namespace DroneConsoleApp.Services
{
    public class DroneController : IDisposable
    {
        private readonly ILogger<DroneController> _logger;
        private readonly IClientDevice? _drone;
        private IPositionClient? _position;
        private IHeartbeatClient? _heartbeat;
        private ControlClient? _control;
        private readonly CancellationTokenSource? _cts;

        public DroneController(IClientDevice drone)
        {
            _drone = drone;

            _logger = LoggerFactory
                .Create(builder =>
                {
                    builder
                        .AddConsole()
                        .SetMinimumLevel(LogLevel.Information); 
                })
                .CreateLogger<DroneController>();

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

            SubscribeToPosition();
        }

        public async Task TakeOff(double altitude)
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone is not connected");

            if (_control == null)
                throw new InvalidOperationException("ControlClient is not initialized");

            var cancel = _cts?.Token ?? CancellationToken.None;

            _logger.LogInformation("Switching to GUIDED mode...");
            await _control.SetGuidedMode(cancel);
            await Task.Delay(TimeSpan.FromSeconds(5), cancel);

            _logger.LogInformation($"Taking off to {altitude} meters...");
            await _control.TakeOff(altitude, cancel);
            await Task.Delay(TimeSpan.FromSeconds(5), cancel);

            _logger.LogInformation("Takeoff complete. Flying to target...");

            var currentPos = _position?.GlobalPosition.CurrentValue;

            double currentLat = currentPos?.Lat / 1_000_000.0 ?? 0;
            double currentLon = currentPos?.Lon / 1_000_000.0 ?? 0;
            double currentAlt = currentPos?.Alt ?? 0;

            _logger.LogInformation($"Current position: Lat={currentLat:F6}, Lon={currentLon:F6}, Alt={currentAlt:F2} m");

            int latMicro = (int)(55.7558 * 1_000_000);
            int lonMicro = (int)(37.6173 * 1_000_000);

            var target = new GeoPoint(latMicro, lonMicro, currentAlt);
            _logger.LogInformation($"Target position:  Lat={target.Latitude:F6}, Lon={target.Longitude:F6}, Alt={target.Altitude:F2} m");
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

            _logger.LogInformation("Switching to GUIDED mode...");
            await _control.SetGuidedMode(cancel);

            _logger.LogInformation($"Flying to: Lat={adjustedTarget.Latitude}, Lon={adjustedTarget.Longitude}, Alt={adjustedTarget.Altitude}");
            await _control.GoTo(adjustedTarget, cancel);

            _logger.LogInformation("Reached target point.");
            _logger.LogInformation("Landing...");
            await _control.DoLand(cancel);
            _logger.LogInformation("Landed.");
        }


        private void SubscribeToPosition()
        {
            if (_position == null)
            {
                _logger.LogError("PositionClient not available. Skipping position subscription.");
                return;
            }

            _logger.LogInformation("Subscribing to drone position updates...");

            _positionSubscription = _position.GlobalPosition.Subscribe(pos =>
            {
                double latitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos?.Lat ?? 0);
                double longitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos?.Lat ?? 0);
                double altitude = MavlinkTypesHelper.AltFromMmToDoubleMeter(pos?.Lat ?? 0);

                _logger.LogInformation($"Updated position: Lat={latitude:F6}, Lon={longitude:F6}, Alt={altitude:F2} m");
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
