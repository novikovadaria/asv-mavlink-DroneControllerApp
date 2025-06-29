namespace DroneControllerApp.View
{
    public class ConsoleView
    {
        public void ShowPosition(double lat, double lon, double alt)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"POSITION:");
            Console.WriteLine($"Lat: {lat:F6}  ");
            Console.WriteLine($"Lon: {lon:F6}  ");
            Console.WriteLine($"Alt: {alt:F2} m ");
            Console.WriteLine(); 
        }


        public void ShowLanded()
        {
            Console.SetCursorPosition(0, 6);
            Console.WriteLine($"LANDED");
        }
    }
}
