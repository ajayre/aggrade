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

        public Benchmark
            (
            Coordinate Location,
            string Name
            )
        {
            this.Location = Location;
            this.Name = Name;
        }

        public Benchmark
            (
            double Latitude,
            double Longitude,
            string Name
            ) : this(new Coordinate(Latitude, Longitude), Name)
        {
        }
    }
}
