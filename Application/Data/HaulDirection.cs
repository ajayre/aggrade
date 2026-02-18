using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    public class HaulDirection
    {
        public Coordinate Location;
        public double DirectionDeg;

        public HaulDirection
            (
            )
        {
            Location = new Coordinate();
        }

        public HaulDirection
            (
            double Latitude,
            double Longitude,
            double DirectionDeg
            )
        {
            Location = new Coordinate(Latitude, Longitude);
            this.DirectionDeg = DirectionDeg;
        }
    }
}
