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
        /// <param name="Location">Location to convert</param>
        /// <returns>Bin or null if not found</returns>
        public Bin? LatLonToBin
            (
            Coordinate Location
            )
        {
            return LatLonToBin(Location.Latitude, Location.Longitude);
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

        /// <summary>
        /// Gets a set of bins inside a rectangle
        /// </summary>
        /// <param name="BottomLeft">Bottom left coordinate</param>
        /// <param name="TopRight">Top right coordinate</param>
        /// <returns>Set of bins</returns>
        public List<Bin> GetBinsInside
            (
            Coordinate BottomLeft,
            Coordinate TopRight
            )
        {
            List<Bin> result = new List<Bin>();

            // Convert coordinates to UTM
            UTM.UTMCoordinate bottomLeftUTM = UTM.FromLatLon(BottomLeft.Latitude, BottomLeft.Longitude);
            UTM.UTMCoordinate topRightUTM = UTM.FromLatLon(TopRight.Latitude, TopRight.Longitude);

            // Convert UTM coordinates to bin grid indices
            int minBinX = (int)Math.Floor((bottomLeftUTM.Easting - FieldMinX) / BIN_SIZE_M);
            int minBinY = (int)Math.Floor((bottomLeftUTM.Northing - FieldMinY) / BIN_SIZE_M);
            int maxBinX = (int)Math.Floor((topRightUTM.Easting - FieldMinX) / BIN_SIZE_M);
            int maxBinY = (int)Math.Floor((topRightUTM.Northing - FieldMinY) / BIN_SIZE_M);

            // Ensure we iterate in the correct direction (handle cases where coordinates might be swapped)
            int startX = Math.Min(minBinX, maxBinX);
            int endX = Math.Max(minBinX, maxBinX);
            int startY = Math.Min(minBinY, maxBinY);
            int endY = Math.Max(minBinY, maxBinY);

            // Iterate through all bins in the rectangular range
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    // Find the bin at this grid position
                    Bin? bin = Bins.FirstOrDefault(b => b.X == x && b.Y == y);
                    if (bin != null)
                    {
                        result.Add(bin);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all of the bins in a straight line from a start bin to an end bin
        /// </summary>
        /// <param name="StartBin">Start bin</param>
        /// <param name="EndBin">End bin</param>
        /// <returns></returns>
        public List<Bin> GetBinsBetween
            (
            Bin? StartBin,
            Bin? EndBin
            )
        {
            List<Bin> result = new List<Bin>();

            if ((StartBin == null) || (EndBin == null)) return result;

            int x0 = StartBin.X;
            int y0 = StartBin.Y;
            int x1 = EndBin.X;
            int y1 = EndBin.Y;

            // Use Bresenham's line algorithm to find all bins on the line
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;

            while (true)
            {
                // Find the bin at this grid position
                Bin? bin = Bins.FirstOrDefault(b => b.X == x && b.Y == y);
                if (bin != null)
                {
                    result.Add(bin);
                }

                // Check if we've reached the end bin
                if (x == x1 && y == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return result;
        }
    }
}
