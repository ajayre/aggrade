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

        /// <summary>
        /// Convert field coordinate in meters to bin grid indices
        /// </summary>
        /// <param name="FieldX">Field X coordinate in meters</param>
        /// <param name="FieldY">Field Y coordinate in meters</param>
        /// <returns>Bin grid X and Y</returns>
        public Point FieldMToBin
            (
            double FieldX,
            double FieldY
            )
        {
            int binGridX = (int)Math.Floor((FieldX - FieldMinX) / BIN_SIZE_M);
            int binGridY = (int)Math.Floor((FieldY - FieldMinY) / BIN_SIZE_M);

            return new Point(binGridX, binGridY);
        }

        /// <summary>
        /// Convert bin grid indices to field coordinate in meters
        /// </summary>
        /// <param name="BinGridX">Bin grid X index</param>
        /// <param name="BinGridY">Bin grid Y index</param>
        /// <returns>Field X and Y coordinates in meters</returns>
        public PointD BinToFieldM
            (
            int BinGridX,
            int BinGridY
            )
        {
            double FieldX = FieldMinX + (BinGridX * Field.BIN_SIZE_M);
            double FieldY = FieldMinY + (BinGridY * Field.BIN_SIZE_M);

            return new PointD(FieldX, FieldY);
        }

        /// <summary>
        /// Converts a loation to a bin
        /// </summary>
        /// <param name="Latitude">Latitude to convert</param>
        /// <param name="Longitude">Longitude to convert</param>
        /// <returns>Bin or null if not found</returns>
        public Bin? LatLonToBin
            (
            double Latitude,
            double Longitude
            )
        {
            // Convert input location to UTM coordinates
            UTM.UTMCoordinate Pos = UTM.FromLatLon(Latitude, Longitude);

            // Convert UTM coordinates to bin grid indices using the same logic as AGDLoader
            // The bin calculation uses: (Point.X - MinX) / BinSizeM
            int binGridX = (int)Math.Floor((Pos.Easting - FieldMinX) / BIN_SIZE_M);
            int binGridY = (int)Math.Floor((Pos.Northing - FieldMinY) / BIN_SIZE_M);

            // Find and return the bin with matching X and Y coordinates
            return Bins.FirstOrDefault(bin => bin.X == binGridX && bin.Y == binGridY);
        }
    }
}
