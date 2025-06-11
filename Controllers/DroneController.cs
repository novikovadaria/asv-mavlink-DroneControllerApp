using Asv.Common;
using Asv.IO;
using Asv.Mavlink;
using Microsoft.Extensions.Logging;
using R3;


namespace DroneConsoleApp.Services
{
    public class DroneController : IDisposable, IAsyncDisposable
    {
        private readonly IProtocolRouter _router;
        private readonly ILogger<DroneController> _logger;
        private IDeviceExplorer? _deviceExplorer;
        private IClientDevice? _drone;
        private IPositionClient? _position;
        private IHeartbeatClient? _heartbeat;
        private ControlClient? _control;
        private CancellationTokenSource? _cts;

        public DroneController(IDeviceExplorer deviceExplorer, IClientDevice drone, IProtocolRouter router)
        {
            _router = router;
            _drone = drone;
            _deviceExplorer = deviceExplorer;

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

            _control = _drone.GetMicroservice<ControlClient>();
            _position = _drone.GetMicroservice<IPositionClient>();
            _heartbeat = _drone.GetMicroservice<IHeartbeatClient>() ;

            SubscribeToPosition();
        }

        public async Task TakeOff(double altitude)
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone is not connected");

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

            _logger.LogInformation("Switching to GUIDED mode...");
            await _control.SetGuidedMode(cancel);

            _logger.LogInformation($"Flying to: Lat={target.Latitude}, Lon={target.Longitude}, Alt={target.Altitude}");
            await _control.GoTo(target, cancel);

            _logger.LogInformation("Reached target point.");
            _logger.LogInformation("Landing...");
            await _control.DoLand(cancel);
            _logger.LogInformation("Landed.");
        }

        private void SubscribeToPosition()
        {
            _positionSubscription?.Dispose();

            if (_position == null)
            {
                _logger.LogError("PositionClient not available. Skipping position subscription.");
                return;
            }

            _logger.LogInformation("Subscribing to drone position updates...");

            _positionSubscription = _position.GlobalPosition.Subscribe(pos =>
            {
                double latitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos.Lat);
                double longitude = MavlinkTypesHelper.LatLonFromInt32E7ToDegDouble(pos.Lon);
                double altitude = MavlinkTypesHelper.AltFromMmToDoubleMeter(pos.Alt);

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
                _deviceExplorer?.Dispose();
                _heartbeat?.Dispose();
                _drone?.Dispose();
                _control?.Dispose();
                _position?.Dispose();
                _cts?.Cancel();
                _cts?.Dispose();
            }

            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            if (_router is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }

            Dispose(disposing: true);
        }
        #endregion
    }
}
