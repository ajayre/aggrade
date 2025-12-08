using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    public class Benchmark
    {
        public Coordinate Location;
        public string Name;
        public double Elevation;

        public Benchmark
            (
            Coordinate Location,
            string Name,
            double Elevation
            )
        {
            this.Location = Location;
            this.Name = Name;
            this.Elevation = Elevation;
        }

        public Benchmark
            (
            double Latitude,
            double Longitude,
            string Name,
            double Elevation
            ) : this(new Coordinate(Latitude, Longitude), Name, Elevation)
        {
        }
    }
}
