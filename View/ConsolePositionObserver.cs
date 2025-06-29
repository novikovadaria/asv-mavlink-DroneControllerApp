namespace DroneControllerApp.View
{
    public class ConsolePositionObserver : IPositionObserver
    {
        private readonly ConsoleView _view;

        public ConsolePositionObserver()
        {
            _view = new ConsoleView();
        }

        public void OnPositionUpdate(double latitude, double longitude, double altitude)
        {
            _view.ShowPosition(latitude, longitude, altitude);
        }

        public void OnLanded()
        {
            _view.ShowLanded();
        }
    }

}
