using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    // Coordinate pair (latitude/longitude)
    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Coordinate() : this(0.0, 0.0) { }

        public Coordinate
            (
            double Latitude,
            double Longitude
            )
        {
            this.Latitude = Latitude;
            this.Longitude = Longitude;
        }
    }
}
