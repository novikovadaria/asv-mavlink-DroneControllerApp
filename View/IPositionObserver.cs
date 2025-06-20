namespace DroneControllerApp.View
{
    public interface IPositionObserver
    {
        void OnPositionUpdate(double latitude, double longitude, double altitude);
        void OnLanded();
    }

}
