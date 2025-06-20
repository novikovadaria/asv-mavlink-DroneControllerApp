using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroneControllerApp.View
{
    public interface IPositionObserver
    {
        void OnPositionUpdate(double latitude, double longitude, double altitude);
        void OnLanded();
    }

}
