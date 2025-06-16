using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void ShowTakingOff(double altitude)
        {
            Console.SetCursorPosition(0, 5); 
            Console.WriteLine($"TAKING OFF to {altitude:F1} m       ");
        }

        public void ShowLanded()
        {
            Console.SetCursorPosition(0, 6);
            Console.WriteLine($"LANDED");
        }
    }
}
