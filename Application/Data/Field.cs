using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.Data.Sqlite;

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
        public List<HaulDirection> HaulDirections;

        public Field()
        {
            TopologyPoints = new List<TopologyPoint>();
            Bins = new List<Bin>();
            Benchmarks = new List<Benchmark>();
            HaulDirections = new List<HaulDirection>();
        }

        /// <summary>
        /// Clears the data from the field
        /// </summary>
        public void Clear
            (
            )
        {
            TopologyPoints.Clear();
            Bins.Clear();
            Benchmarks.Clear();
            HaulDirections.Clear();
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
        /// Gets a set of bins inside a polygon
        /// </summary>
        /// <param name="Vertices">Vertices of polygon</param>
        /// <param name="MinCoverage">Minimum bin coverage percentage to be included 0 -> 100</param>
        /// <returns>Set of bins</returns>
        public List<Bin> GetBinsInside
            (
            List<Coordinate> Vertices,
            uint MinCoverage
            )
        {
            List<Bin> result = new List<Bin>();

            if (Vertices == null || Vertices.Count < 3)
            {
                return result; // Need at least 3 vertices for a polygon
            }

            // Convert polygon vertices from lat/lon to UTM coordinates
            List<PointD> polygonUTM = new List<PointD>();
            foreach (Coordinate vertex in Vertices)
            {
                UTM.UTMCoordinate utm = UTM.FromLatLon(vertex.Latitude, vertex.Longitude);
                polygonUTM.Add(new PointD(utm.Easting, utm.Northing));
            }

            // Find bounding box of the polygon in UTM coordinates
            double minX = polygonUTM[0].X;
            double maxX = polygonUTM[0].X;
            double minY = polygonUTM[0].Y;
            double maxY = polygonUTM[0].Y;

            foreach (PointD pt in polygonUTM)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            // Convert bounding box to bin grid indices
            int minBinX = (int)Math.Floor((minX - FieldMinX) / BIN_SIZE_M);
            int maxBinX = (int)Math.Floor((maxX - FieldMinX) / BIN_SIZE_M);
            int minBinY = (int)Math.Floor((minY - FieldMinY) / BIN_SIZE_M);
            int maxBinY = (int)Math.Floor((maxY - FieldMinY) / BIN_SIZE_M);

            // Bin area in square meters
            double binArea = BIN_SIZE_M * BIN_SIZE_M;
            double minRequiredArea = binArea * (double)MinCoverage / 100.0;

            // Iterate through all bins in the bounding box
            for (int x = minBinX; x <= maxBinX; x++)
            {
                for (int y = minBinY; y <= maxBinY; y++)
                {
                    // Get the bin's corners in UTM coordinates
                    PointD binSW = BinToFieldM(x, y);
                    PointD binNE = BinToFieldM(x + 1, y + 1);

                    // Clip the polygon against the bin's rectangle to get intersection
                    List<PointD> intersectionPolygon = ClipPolygonToRectangle(polygonUTM, binSW.X, binSW.Y, binNE.X, binNE.Y);

                    // Calculate the area of the intersection
                    if (intersectionPolygon.Count >= 3)
                    {
                        double intersectionArea = CalculatePolygonArea(intersectionPolygon);

                        // Only include bin if intersection area is at least 30% of bin area
                        if (intersectionArea >= minRequiredArea)
                        {
                            // Find the bin at this grid position
                            Bin? bin = Bins.FirstOrDefault(b => b.X == x && b.Y == y);
                            if (bin != null)
                            {
                                result.Add(bin);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Clips a polygon against a rectangle using the Sutherland-Hodgman algorithm
        /// </summary>
        private List<PointD> ClipPolygonToRectangle(List<PointD> polygon, double rectMinX, double rectMinY, double rectMaxX, double rectMaxY)
        {
            if (polygon == null || polygon.Count < 3)
                return new List<PointD>();

            List<PointD> output = new List<PointD>(polygon);

            // Clip against each edge of the rectangle: bottom, right, top, left
            output = ClipAgainstBottomEdge(output, rectMinY);
            output = ClipAgainstRightEdge(output, rectMaxX);
            output = ClipAgainstTopEdge(output, rectMaxY);
            output = ClipAgainstLeftEdge(output, rectMinX);

            return output;
        }

        /// <summary>
        /// Clips polygon against the bottom edge (y >= rectMinY is inside)
        /// </summary>
        private List<PointD> ClipAgainstBottomEdge(List<PointD> polygon, double rectMinY)
        {
            if (polygon == null || polygon.Count == 0)
                return new List<PointD>();

            List<PointD> output = new List<PointD>();
            PointD s = polygon[polygon.Count - 1];

            for (int i = 0; i < polygon.Count; i++)
            {
                PointD p = polygon[i];
                bool sInside = s.Y >= rectMinY;
                bool pInside = p.Y >= rectMinY;

                if (pInside)
                {
                    if (!sInside)
                    {
                        // Add intersection point
                        if (Math.Abs(p.Y - s.Y) > 1e-10)
                        {
                            double t = (rectMinY - s.Y) / (p.Y - s.Y);
                            double x = s.X + t * (p.X - s.X);
                            output.Add(new PointD(x, rectMinY));
                        }
                        else
                        {
                            // Horizontal line segment, use the point that's closer to the edge
                            output.Add(new PointD(p.X, rectMinY));
                        }
                    }
                    output.Add(p);
                }
                else if (sInside)
                {
                    // Add intersection point
                    if (Math.Abs(p.Y - s.Y) > 1e-10)
                    {
                        double t = (rectMinY - s.Y) / (p.Y - s.Y);
                        double x = s.X + t * (p.X - s.X);
                        output.Add(new PointD(x, rectMinY));
                    }
                    else
                    {
                        // Horizontal line segment, use the point that's closer to the edge
                        output.Add(new PointD(s.X, rectMinY));
                    }
                }

                s = p;
            }

            return output;
        }

        /// <summary>
        /// Clips polygon against the right edge (x <= rectMaxX is inside)
        /// </summary>
        private List<PointD> ClipAgainstRightEdge(List<PointD> polygon, double rectMaxX)
        {
            if (polygon == null || polygon.Count == 0)
                return new List<PointD>();

            List<PointD> output = new List<PointD>();
            PointD s = polygon[polygon.Count - 1];

            for (int i = 0; i < polygon.Count; i++)
            {
                PointD p = polygon[i];
                bool sInside = s.X <= rectMaxX;
                bool pInside = p.X <= rectMaxX;

                if (pInside)
                {
                    if (!sInside)
                    {
                        // Add intersection point
                        if (Math.Abs(p.X - s.X) > 1e-10)
                        {
                            double t = (rectMaxX - s.X) / (p.X - s.X);
                            double y = s.Y + t * (p.Y - s.Y);
                            output.Add(new PointD(rectMaxX, y));
                        }
                        else
                        {
                            // Vertical line segment, use the point that's closer to the edge
                            output.Add(new PointD(rectMaxX, p.Y));
                        }
                    }
                    output.Add(p);
                }
                else if (sInside)
                {
                    // Add intersection point
                    if (Math.Abs(p.X - s.X) > 1e-10)
                    {
                        double t = (rectMaxX - s.X) / (p.X - s.X);
                        double y = s.Y + t * (p.Y - s.Y);
                        output.Add(new PointD(rectMaxX, y));
                    }
                    else
                    {
                        // Vertical line segment, use the point that's closer to the edge
                        output.Add(new PointD(rectMaxX, s.Y));
                    }
                }

                s = p;
            }

            return output;
        }

        /// <summary>
        /// Clips polygon against the top edge (y <= rectMaxY is inside)
        /// </summary>
        private List<PointD> ClipAgainstTopEdge(List<PointD> polygon, double rectMaxY)
        {
            if (polygon == null || polygon.Count == 0)
                return new List<PointD>();

            List<PointD> output = new List<PointD>();
            PointD s = polygon[polygon.Count - 1];

            for (int i = 0; i < polygon.Count; i++)
            {
                PointD p = polygon[i];
                bool sInside = s.Y <= rectMaxY;
                bool pInside = p.Y <= rectMaxY;

                if (pInside)
                {
                    if (!sInside)
                    {
                        // Add intersection point
                        if (Math.Abs(p.Y - s.Y) > 1e-10)
                        {
                            double t = (rectMaxY - s.Y) / (p.Y - s.Y);
                            double x = s.X + t * (p.X - s.X);
                            output.Add(new PointD(x, rectMaxY));
                        }
                        else
                        {
                            // Horizontal line segment, use the point that's closer to the edge
                            output.Add(new PointD(p.X, rectMaxY));
                        }
                    }
                    output.Add(p);
                }
                else if (sInside)
                {
                    // Add intersection point
                    if (Math.Abs(p.Y - s.Y) > 1e-10)
                    {
                        double t = (rectMaxY - s.Y) / (p.Y - s.Y);
                        double x = s.X + t * (p.X - s.X);
                        output.Add(new PointD(x, rectMaxY));
                    }
                    else
                    {
                        // Horizontal line segment, use the point that's closer to the edge
                        output.Add(new PointD(s.X, rectMaxY));
                    }
                }

                s = p;
            }

            return output;
        }

        /// <summary>
        /// Clips polygon against the left edge (x >= rectMinX is inside)
        /// </summary>
        private List<PointD> ClipAgainstLeftEdge(List<PointD> polygon, double rectMinX)
        {
            if (polygon == null || polygon.Count == 0)
                return new List<PointD>();

            List<PointD> output = new List<PointD>();
            PointD s = polygon[polygon.Count - 1];

            for (int i = 0; i < polygon.Count; i++)
            {
                PointD p = polygon[i];
                bool sInside = s.X >= rectMinX;
                bool pInside = p.X >= rectMinX;

                if (pInside)
                {
                    if (!sInside)
                    {
                        // Add intersection point
                        if (Math.Abs(p.X - s.X) > 1e-10)
                        {
                            double t = (rectMinX - s.X) / (p.X - s.X);
                            double y = s.Y + t * (p.Y - s.Y);
                            output.Add(new PointD(rectMinX, y));
                        }
                        else
                        {
                            // Vertical line segment, use the point that's closer to the edge
                            output.Add(new PointD(rectMinX, p.Y));
                        }
                    }
                    output.Add(p);
                }
                else if (sInside)
                {
                    // Add intersection point
                    if (Math.Abs(p.X - s.X) > 1e-10)
                    {
                        double t = (rectMinX - s.X) / (p.X - s.X);
                        double y = s.Y + t * (p.Y - s.Y);
                        output.Add(new PointD(rectMinX, y));
                    }
                    else
                    {
                        // Vertical line segment, use the point that's closer to the edge
                        output.Add(new PointD(rectMinX, s.Y));
                    }
                }

                s = p;
            }

            return output;
        }

        /// <summary>
        /// Finds the intersection point of two line segments
        /// </summary>
        private PointD GetLineIntersection(PointD p1, PointD p2, PointD p3, PointD p4)
        {
            return GetLineIntersection(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y, p4.X, p4.Y);
        }

        /// <summary>
        /// Finds the intersection point of two line segments
        /// </summary>
        private PointD GetLineIntersection(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            
            if (Math.Abs(denom) < 1e-10)
            {
                // Lines are parallel, return midpoint of first segment
                return new PointD((x1 + x2) / 2.0, (y1 + y2) / 2.0);
            }

            double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
            double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

            // Clamp to segment bounds
            t = Math.Max(0.0, Math.Min(1.0, t));
            u = Math.Max(0.0, Math.Min(1.0, u));

            double x = x1 + t * (x2 - x1);
            double y = y1 + t * (y2 - y1);

            return new PointD(x, y);
        }

        /// <summary>
        /// Calculates the area of a polygon using the shoelace formula
        /// </summary>
        private double CalculatePolygonArea(List<PointD> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return 0.0;

            double area = 0.0;
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += polygon[i].X * polygon[j].Y;
                area -= polygon[j].X * polygon[i].Y;
            }

            return Math.Abs(area) / 2.0;
        }

        /// <summary>
        /// Checks if a point is inside a polygon using the ray casting algorithm
        /// </summary>
        private bool IsPointInPolygon(PointD point, List<PointD> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                PointD pi = polygon[i];
                PointD pj = polygon[j];

                if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                    (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        /// <summary>
        /// Checks if two line segments intersect
        /// </summary>
        private bool DoLineSegmentsIntersect(PointD p1, PointD p2, PointD p3, PointD p4)
        {
            // Calculate orientation of three points
            double orientation1 = Orientation(p1, p2, p3);
            double orientation2 = Orientation(p1, p2, p4);
            double orientation3 = Orientation(p3, p4, p1);
            double orientation4 = Orientation(p3, p4, p2);

            // General case: segments intersect if orientations are different
            if (orientation1 != orientation2 && orientation3 != orientation4)
                return true;

            // Special cases: check if points are collinear and on the segment
            if (orientation1 == 0 && IsPointOnSegment(p1, p2, p3))
                return true;
            if (orientation2 == 0 && IsPointOnSegment(p1, p2, p4))
                return true;
            if (orientation3 == 0 && IsPointOnSegment(p3, p4, p1))
                return true;
            if (orientation4 == 0 && IsPointOnSegment(p3, p4, p2))
                return true;

            return false;
        }

        /// <summary>
        /// Calculates the orientation of three points (0 = collinear, >0 = clockwise, <0 = counterclockwise)
        /// </summary>
        private double Orientation(PointD p1, PointD p2, PointD p3)
        {
            return (p2.Y - p1.Y) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Y - p2.Y);
        }

        /// <summary>
        /// Checks if point p lies on the line segment from p1 to p2
        /// </summary>
        private bool IsPointOnSegment(PointD p1, PointD p2, PointD p)
        {
            return p.X <= Math.Max(p1.X, p2.X) && p.X >= Math.Min(p1.X, p2.X) &&
                   p.Y <= Math.Max(p1.Y, p2.Y) && p.Y >= Math.Min(p1.Y, p2.Y) &&
                   Orientation(p1, p2, p) == 0;
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

        /// <summary>
        /// Loads a field from a folder containing field data
        /// </summary>
        /// <param name="Folder">Path of folder to load from</param>
        public void Load
            (
            string Folder
            )
        {
            // get first AGD file in folder
            string[] AGDFiles = Directory.GetFiles(Folder, "*.agd");
            if (AGDFiles.Length == 0) throw new Exception("No AGD file found for field");
            string AGDFile = AGDFiles[0];

            Clear();

            // load AGD file
            AGDLoader Loader = new AGDLoader();
            Loader.Load(this, AGDFile);

            // set field name using AGD file name
            Name = Path.GetFileNameWithoutExtension(AGDFile);

            // if haul directions do not exist, then create them now
            string HaulDirectionsCSV = Folder + Path.DirectorySeparatorChar + "HaulDirections.csv";
            if (!File.Exists(HaulDirectionsCSV))
            {
                CreateHaulDirections(Folder, HaulDirectionsCSV);
            }

            LoadHaulDirections(HaulDirectionsCSV);
        }

        /// <summary>
        /// Creates a CSV of haul directions from a set of georeferenced images
        /// </summary>
        /// <param name="Folder">Folder containing images to process</param>
        /// <param name="OutputFileName">CSV file to generate</param>
        private void CreateHaulDirections
            (
            string Folder,
            string OutputFileName
            )
        {
            HaulImageProcessor Processor = new HaulImageProcessor();
            Processor.Process(Folder, OutputFileName);
        }

        /// <summary>
        /// Loads in the haul directions from a CSV file
        /// </summary>
        /// <param name="FileName">Path and name of CSV file</param>
        private void LoadHaulDirections
            (
            string FileName
            )
        {
            if (!File.Exists(FileName))
                return;

            HaulDirections.Clear();
            using (StreamReader reader = new StreamReader(FileName))
            {
                // Skip header: lat,lon,direction_degrees,source
                string? header = reader.ReadLine();
                if (header == null)
                    return;

                while ((header = reader.ReadLine()) != null)
                {
                    string[] parts = header.Split(',');
                    if (parts.Length < 3)
                        continue;

                    if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double directionDeg))
                    {
                        HaulDirections.Add(new HaulDirection(lat, lon, directionDeg));
                    }
                }
            }
        }
    }
}
