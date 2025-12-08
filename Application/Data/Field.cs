using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    public class Field
    {
        // Bin size in meters (2ft = 0.6096m)
        public const double BIN_SIZE_M = 0.6096;

        public string Name;
        public List<TopologyPoint> TopologyPoints;
        public double FieldCentroidLat;
        public double FieldCentroidLon;
        public double FieldMinX;
        public double FieldMinY;
        public double FieldMaxX;
        public double FieldMaxY;
        public List<Bin> Bins;
        public int UTMZone;
        public bool IsNorthernHemisphere;
        public double TotalCutBCY;
        public double TotalFillBCY;
        public List<Benchmark> Benchmarks;

        public Field()
        {
            TopologyPoints = new List<TopologyPoint>();
            Bins = new List<Bin>();
            Benchmarks = new List<Benchmark>();
        }
    }
}
