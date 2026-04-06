using AgGrade.Controller;
using Microsoft.VisualBasic.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Point = System.Drawing.Point;

namespace AgGrade.Data
{
    public class Field
    {
        // Bin size in meters (2ft = 0.6096m)
        public const double BIN_SIZE_M = 0.6096;

        private const double CUBIC_YARDS_PER_CUBIC_METER = 1.30795061931439;

        public const double CUT_FILL_RATIO = 1.2;

        /// <summary>
        /// If a bin has this elevation then it is outside of the field
        /// </summary>
        public const int BIN_NO_DATA_SENTINEL = -1000;

        /// <summary>No-data value for elevation DEM cells with no height (e.g. zero or missing).</summary>
        public const float ELEVATION_DEM_NO_DATA_VALUE = -9999f;

        /// <summary>Default Gaussian sigma for DEM smoothing (matches Python demgenerator).</summary>
        private const double ELEVATION_DEM_DEFAULT_SMOOTH_SIGMA = 3.0;

        /// <summary>Python helper script to write GeoTIFF via rasterio (same layout as demgenerator).</summary>
        private const string WRITE_GEOTIFF_SCRIPT = "write_geotiff_from_raw.py";

        private Database Db;

        public struct Calibration
        {
            public double EastingM;
            public double NorthingM;
            public double HeightM;

            public Calibration
                (
                double EastingM,
                double NorthingM,
                double HeightM
                )
            {
                this.EastingM = EastingM;
                this.NorthingM = NorthingM;
                this.HeightM = HeightM;
            }
        }

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
        public double TotalCutCY;
        public double TotalFillCY;
        public List<Benchmark> Benchmarks;
        public List<HaulDirection> HaulDirections;
        public double CompletedCutCY;
        public double CompletedFillCY;
        private bool IsLevelingOperationActive;
        private Database.LevelingOperationType? ActiveOperationType;
        private bool HasTractorFixBeenStored;
        private bool WasTractorMoving;
        private bool HasFrontScraperFixBeenStored;
        private bool WasFrontScraperMoving;
        private bool HasRearScraperFixBeenStored;
        private bool WasRearScraperMoving;

        public Bin?[,] BinGrid;
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        public Field()
        {
            TopologyPoints = new List<TopologyPoint>();
            Bins = new List<Bin>();
            Benchmarks = new List<Benchmark>();
            HaulDirections = new List<HaulDirection>();

            Db = new Database();
            Db.OnStatsNeeded += HandleStatsNeeded;
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
            HasTractorFixBeenStored = false;
            WasTractorMoving = false;
            HasFrontScraperFixBeenStored = false;
            WasFrontScraperMoving = false;
            HasRearScraperFixBeenStored = false;
            WasRearScraperMoving = false;
        }

        /// <summary>
        /// Checks if the field has a calibration
        /// </summary>
        /// <returns>true if calibrated, false if not calibrated</returns>
        public bool IsCalibrated
            (
            )
        {
            return Db.GetBoolData(Database.DataNames.Calibrated);
        }

        /// <summary>
        /// Returns field progress history while keeping database access encapsulated in Field.
        /// </summary>
        public List<Database.ProgressHistoryPoint> GetProgressHistory
            (
            )
        {
            return Db.GetProgressHistory();
        }

        /// <summary>
        /// Gets the current field calibration
        /// </summary>
        /// <returns>Current calibration</returns>
        public Calibration GetCalibration
            (
            )
        {
            Calibration NewCalib = new Calibration();

            NewCalib.EastingM  = Db.GetData(Database.DataNames.EastingOffsetM);
            NewCalib.NorthingM = Db.GetData(Database.DataNames.NorthingOffsetM);
            NewCalib.HeightM   = Db.GetData(Database.DataNames.HeightOffsetM);

            return NewCalib;
        }

        /// <summary>
        /// Sets a new calibration
        /// </summary>
        /// <param name="NewCalib">New calibration to use</param>
        public void SetCalibration
            (
            Calibration NewCalib
            )
        {
            Calibration oldCalib = GetCalibration();

            Db.SetData(Database.DataNames.EastingOffsetM,  NewCalib.EastingM);
            Db.SetData(Database.DataNames.NorthingOffsetM, NewCalib.NorthingM);
            Db.SetData(Database.DataNames.HeightOffsetM,   NewCalib.HeightM);
            Db.SetBoolData(Database.DataNames.Calibrated, true);
            Db.RefreshCalibration();

            double deltaEastingM = NewCalib.EastingM - oldCalib.EastingM;
            double deltaNorthingM = NewCalib.NorthingM - oldCalib.NorthingM;
            double deltaHeightM = NewCalib.HeightM - oldCalib.HeightM;

            if (deltaEastingM == 0 && deltaNorthingM == 0 && deltaHeightM == 0) return;

            Coordinate shiftedFieldCentroid = UTM.OffsetLocation(FieldCentroidLat, FieldCentroidLon, deltaEastingM, deltaNorthingM);
            FieldCentroidLat = shiftedFieldCentroid.Latitude;
            FieldCentroidLon = shiftedFieldCentroid.Longitude;
            FieldMinX += deltaEastingM;
            FieldMinY += deltaNorthingM;
            FieldMaxX += deltaEastingM;
            FieldMaxY += deltaNorthingM;

            foreach (TopologyPoint point in TopologyPoints)
            {
                Coordinate shifted = UTM.OffsetLocation(point.Latitude, point.Longitude, deltaEastingM, deltaNorthingM);
                point.Latitude = shifted.Latitude;
                point.Longitude = shifted.Longitude;
                point.ExistingElevation += deltaHeightM;
                point.ProposedElevation += deltaHeightM;
            }

            foreach (Bin bin in Bins)
            {
                if (bin.CurrentElevationM != BIN_NO_DATA_SENTINEL) bin.CurrentElevationM += deltaHeightM;
                if (bin.InitialElevationM != BIN_NO_DATA_SENTINEL) bin.InitialElevationM += deltaHeightM;
                if (bin.TargetElevationM != BIN_NO_DATA_SENTINEL) bin.TargetElevationM += deltaHeightM;

                if (bin.Centroid != null)
                {
                    bin.Centroid = UTM.OffsetLocation(bin.Centroid, deltaEastingM, deltaNorthingM);
                }

                if (bin.SouthwestCorner != null)
                {
                    bin.SouthwestCorner = UTM.OffsetLocation(bin.SouthwestCorner, deltaEastingM, deltaNorthingM);
                }

                if (bin.NortheastCorner != null)
                {
                    bin.NortheastCorner = UTM.OffsetLocation(bin.NortheastCorner, deltaEastingM, deltaNorthingM);
                }
            }

            foreach (Benchmark benchmark in Benchmarks)
            {
                benchmark.Location = UTM.OffsetLocation(benchmark.Location, deltaEastingM, deltaNorthingM);
                benchmark.Elevation += deltaHeightM;
            }

            foreach (HaulDirection direction in HaulDirections)
            {
                direction.Location = UTM.OffsetLocation(direction.Location, deltaEastingM, deltaNorthingM);
            }
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
        /// Gets the nearest benchmark to a location
        /// </summary>
        /// <param name="Latitude">Latitude to check</param>
        /// <param name="Longitude">Longitude to check</param>
        /// <param name="CurrentElevationM">The current elevation in m</param>
        /// <param name="EastingM">UTM easting distance to benchmark in m</param>
        /// <param name="NorthingM">UTM northing distance to benchmark in m</param>
        /// <param name="HeightM">Height difference between current height and nearest benchmark height in m (negative is benchmark is higher)</param>
        /// <returns>The nearest benchmark or null for none</returns>
        public Benchmark? GetNearestBenchmark
            (
            double Latitude,
            double Longitude,
            double CurrentElevationM,
            out double EastingM,
            out double NorthingM,
            out double HeightM
            )
        {
            EastingM = 0;
            NorthingM = 0;
            HeightM = 0;

            if (Benchmarks == null || Benchmarks.Count == 0)
                return null;

            Benchmark? nearest = null;
            double bestM = double.PositiveInfinity;

            foreach (Benchmark b in Benchmarks)
            {
                double dM = Haversine.Distance(
                    Latitude, Longitude,
                    b.Location.Latitude, b.Location.Longitude);
                if (dM < bestM)
                {
                    bestM = dM;
                    nearest = b;
                }
            }

            if (nearest == null) return nearest;

            UTM.UTMCoordinate posUtm = UTM.FromLatLon(Latitude, Longitude);
            UTM.UTMCoordinate benchUtm = UTM.FromLatLon(nearest.Location.Latitude, nearest.Location.Longitude);

            EastingM = posUtm.Easting - benchUtm.Easting;
            NorthingM = posUtm.Northing- benchUtm.Northing;
            HeightM = CurrentElevationM - nearest.Elevation;

            return nearest;
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
        /// <param name="DbFile">Database file to load or null to create new database</param>
        public void Load
            (
            string Folder,
            string? DbFile
            )
        {
            // if no database file then get the name of the database file to create
            // we increment based on the previous database files so no data is overwritten
            if (DbFile == null)
            {
                int nextVersion = 1;
                foreach (string path in Directory.GetFiles(Folder, "V*.db"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (name.Length > 1 && name[0] == 'V' && int.TryParse(name.Substring(1), out int v) && v >= nextVersion)
                        nextVersion = v + 1;
                }
                DbFile = Path.Combine(Folder, $"V{nextVersion}.db");

                // get base database
                string[] BaseDbs = Directory.GetFiles(Folder, "*-base.db");
                if (BaseDbs.Length != 1) throw new Exception("No base database found for field");

                // copy base to new database file
                File.Copy(BaseDbs[0], DbFile);
            }

            Db.Open(DbFile);

            Clear();

            // load in field
            Database.BinState[] BinStates = Db.GetBinStates();
            foreach (Database.BinState BinState in BinStates)
            {
                Bin NewBin = new Bin();
                NewBin.X = BinState.X;
                NewBin.Y = BinState.Y;
                NewBin.CurrentElevationM = BinState.CurrentHeightM;
                NewBin.InitialElevationM = BinState.InitialHeightM;
                NewBin.TargetElevationM = BinState.TargetHeightM;
                NewBin.Centroid = new Coordinate(BinState.CentroidLat, BinState.CentroidLon);
                NewBin.HaulPath = BinState.HaulPath;
                NewBin.Field = this;
                Bins.Add(NewBin);
            }

            // load in field data
            FieldCentroidLat = Db.GetData(Database.DataNames.MeanLat);
            FieldCentroidLon = Db.GetData(Database.DataNames.MeanLon);
            GridWidth = (int)Db.GetData(Database.DataNames.GridWidth);
            GridHeight = (int)Db.GetData(Database.DataNames.GridHeight);
            CompletedCutCY = Db.GetData(Database.DataNames.CompletedCutCY);
            CompletedFillCY = Db.GetData(Database.DataNames.CompletedFillCY);

            // set field name using folder name and version
            Name = string.Format("{0}", Path.GetFileNameWithoutExtension(DbFile));

            // load in haul arrows
            Database.HaulArrow[] HaulArrows = Db.GetHaulArrows();
            foreach (Database.HaulArrow Arrow in HaulArrows)
            {
                HaulDirections.Add(new HaulDirection(Arrow.Latitude, Arrow.Longitude, Arrow.Heading));
            }

            // load in benchmarks
            Database.BenchMark[] benchMarks = Db.GetBenchMarks();
            foreach (Database.BenchMark BMark in benchMarks)
            {
                this.Benchmarks.Add(new Benchmark(new Coordinate(BMark.Latitude, BMark.Longitude), BMark.Name, BMark.ElevationM));
            }

            // find the Southwest corner (minimum X and Y) to use as origin
            double MinLat = Db.GetData(Database.DataNames.MinLat);
            double MinLon = Db.GetData(Database.DataNames.MinLon);
            UTM.UTMCoordinate MinXY = UTM.FromLatLon(MinLat, MinLon);
            FieldMinX = MinXY.Easting;
            FieldMinY = MinXY.Northing;

            UTMZone = MinXY.Zone;
            IsNorthernHemisphere = MinXY.IsNorthernHemisphere;

            // find the northeast corner (maximum X and Y)
            double MaxLat = Db.GetData(Database.DataNames.MaxLat);
            double MaxLon = Db.GetData(Database.DataNames.MaxLon);
            UTM.UTMCoordinate MaxXY = UTM.FromLatLon(MaxLat, MaxLon);
            FieldMaxX = MaxXY.Easting;
            FieldMaxY = MaxXY.Northing;

            TotalCutCY = Db.GetData(Database.DataNames.TotalCutCY);
            TotalFillCY = Db.GetData(Database.DataNames.TotalFillCY);

            CalculateBinGridSize();

            // construct bin grid to access bins vix y, x
            BinGrid = CreateBinsGrid();

            string OutFolder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)! + Path.DirectorySeparatorChar;
            FlowMapGenerator FlowGen = new FlowMapGenerator();
        }

        /// <summary>
        /// Gets the percentage complete of the field
        /// </summary>
        /// <returns>Percentage complete</returns>
        public double PercentageComplete
            (
            )
        {
            return CompletedCutCY / TotalCutCY * 100.0;
        }

        /// <summary>
        /// Cuts soil from a bin
        /// </summary>
        /// <param name="BinToCut">Bin to cut from</param>
        /// <param name="CutHeightM">Height of soil to remove</param>
        public void CutBin
            (
            Bin BinToCut,
            double CutHeightM
            )
        {
            if (CutHeightM == 0) return;
            bool shouldAutoCommit = false;
            if (!IsLevelingOperationActive)
            {
                BeginLevelingOperation(Database.LevelingOperationType.CUT);
                shouldAutoCommit = true;
            }

            BinToCut.CurrentElevationM -= CutHeightM;

            AddBinDelta(BinToCut.X, BinToCut.Y, -CutHeightM);

            // updated completed cuts
            CompletedCutCY += BIN_SIZE_M * BIN_SIZE_M * CutHeightM * CUBIC_YARDS_PER_CUBIC_METER;
            if (shouldAutoCommit)
            {
                CommitLevelingOperation();
            }
        }

        /// <summary>
        /// Adds soil to a bin
        /// </summary>
        /// <param name="BinToCut">Bin to add to</param>
        /// <param name="CutHeightM">Height of soil to add</param>
        public void FillBin
            (
            Bin BinToFill,
            double FillHeightM
            )
        {
            if (FillHeightM == 0) return;
            bool shouldAutoCommit = false;
            if (!IsLevelingOperationActive)
            {
                BeginLevelingOperation(Database.LevelingOperationType.FILL);
                shouldAutoCommit = true;
            }

            BinToFill.CurrentElevationM += FillHeightM;

            AddBinDelta(BinToFill.X, BinToFill.Y, FillHeightM);

            // update completed fills
            CompletedFillCY += (BIN_SIZE_M * BIN_SIZE_M * FillHeightM * CUBIC_YARDS_PER_CUBIC_METER) / Field.CUT_FILL_RATIO;
            if (shouldAutoCommit)
            {
                CommitLevelingOperation();
            }
        }

        /// <summary>
        /// Begins one leveled swath operation for batched journaled persistence.
        /// </summary>
        public void BeginLevelingOperation
            (
            Database.LevelingOperationType operationType
            )
        {
            if (IsLevelingOperationActive)
            {
                if (ActiveOperationType == operationType) return;
                throw new InvalidOperationException("Cannot mix operation types in one active leveling operation.");
            }

            Db.BeginLevelingOperation(operationType);
            IsLevelingOperationActive = true;
            ActiveOperationType = operationType;
        }

        /// <summary>
        /// Adds one bin delta to the current active leveling operation.
        /// </summary>
        public void AddBinDelta
            (
            int x,
            int y,
            double deltaHeightM
            )
        {
            if (!IsLevelingOperationActive) throw new InvalidOperationException("No active leveling operation.");
            Db.AddLevelingOperationBinDelta(x, y, deltaHeightM);
        }

        /// <summary>
        /// Commits the current active leveling operation into one SQLite transaction.
        /// </summary>
        public void CommitLevelingOperation
            (
            )
        {
            if (!IsLevelingOperationActive) return;
            Db.CommitLevelingOperation(CompletedCutCY, CompletedFillCY);
            IsLevelingOperationActive = false;
            ActiveOperationType = null;
        }

        /// <summary>
        /// Calculates the size of the bin grid in bins
        /// </summary>
        private void CalculateBinGridSize
            (
            )
        {
            // Calculate grid dimensions
            var bins = Bins;
            var minX = bins.Min(b => b.X);
            var maxX = bins.Max(b => b.X);
            var minY = bins.Min(b => b.Y);
            var maxY = bins.Max(b => b.Y);

            GridWidth = maxX - minX + 1;
            GridHeight = maxY - minY + 1;
        }

        /// <summary>
        /// Organizes the bins into a grid
        /// </summary>
        /// <returns></returns>
        public Bin?[,] CreateBinsGrid
            (
            )
        {
            var BinGrid = new Bin?[GridHeight, GridWidth];

            int minX = 0;
            int minY = 0;

            foreach (var bin in Bins)
            {
                var x = bin.X - minX;
                var y = bin.Y - minY;

                if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
                {
                    BinGrid[y, x] = bin;
                }
            }

            return BinGrid;
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
        /// Gets the haul path for a specific starting bin
        /// </summary>
        /// <param name="StartBin">Starting bin</param>
        /// <returns>List of coordinates or empty list for no path</returns>
        public List<Coordinate> GetHaulPath
            (
            Bin StartBin
            )
        {
            return Db.GetHaulPath(StartBin);
        }

        /// <summary>
        /// Sets a tractor fix
        /// </summary>
        /// <param name="Fix">Fix</param>
        public void SetTractorFix
            (
            GNSSFix Fix
            )
        {
            bool isMoving = Fix.Vector.SpeedMph > 0;
            bool isFirstFixAfterStartup = !HasTractorFixBeenStored;
            bool justStopped = WasTractorMoving && !isMoving;

            if (isFirstFixAfterStartup || isMoving || justStopped)
            {
                // store in database
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.TractorLat, Fix.Latitude));
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.TractorLon, Fix.Longitude));
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.Speedkph, Fix.Vector.Speedkph));
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.Heading, Fix.Vector.TrackMagneticDeg));
            }

            HasTractorFixBeenStored = true;
            WasTractorMoving = isMoving;
        }

        /// <summary>
        /// Sets a front scraper fix
        /// </summary>
        /// <param name="Fix">Fix</param>
        public void SetFrontScraperFix
            (
            GNSSFix Fix
            )
        {
            bool isMoving = Fix.Vector.Speedkph > 0;
            bool isFirstFixAfterStartup = !HasFrontScraperFixBeenStored;
            bool justStopped = WasFrontScraperMoving && !isMoving;

            if (isFirstFixAfterStartup || isMoving || justStopped)
            {
                // store in database
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.FrontScraperLat, Fix.Latitude));
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.FrontScraperLon, Fix.Longitude));
            }

            HasFrontScraperFixBeenStored = true;
            WasFrontScraperMoving = isMoving;
        }

        /// <summary>
        /// Sets a rear scraper fix
        /// </summary>
        /// <param name="Fix">Fix</param>
        public void SetRearScraperFix
            (
            GNSSFix Fix
            )
        {
            bool isMoving = Fix.Vector.SpeedMph > 0;
            bool isFirstFixAfterStartup = !HasRearScraperFixBeenStored;
            bool justStopped = WasRearScraperMoving && !isMoving;

            if (isFirstFixAfterStartup || isMoving || justStopped)
            {
                // store in database
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.RearScraperLat, Fix.Latitude));
                Db.AddEvent(new Database.Event(Database.Event.EventTypes.RearScraperLon, Fix.Longitude));
            }

            HasRearScraperFixBeenStored = true;
            WasRearScraperMoving = isMoving;
        }

        /// <summary>
        /// Stores periodic progress stats when requested by the database timer logic.
        /// </summary>
        private void HandleStatsNeeded
            (
            object? sender,
            EventArgs e
            )
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Db.AddEvent
                (
                new Database.Event(Database.Event.EventTypes.CompletedCutCY, CompletedCutCY)
                {
                    Timestamp = timestamp
                }
                );
            Db.AddEvent
                (
                new Database.Event(Database.Event.EventTypes.CompletedFillCY, CompletedFillCY)
                {
                    Timestamp = timestamp
                }
                );
        }

        /// <summary>
        /// Builds the elevation grid from this field's bins (row 0 = north). Used by GenerateElevationDEM and by ponding/flow generators that need the grid in memory.
        /// </summary>
        /// <param name="elevationType">Initial, Current, or Target elevation</param>
        /// <param name="enableSmoothing">If true, apply Gaussian smoothing to soften bin edges</param>
        /// <param name="data">Output DEM raster (nrows x ncols)</param>
        /// <param name="nrows">Number of rows</param>
        /// <param name="ncols">Number of columns</param>
        /// <param name="SWCorner">South-west corner in lat/lon</param>
        /// <param name="NECorner">North-east corner in lat/lon</param>
        public void BuildElevationGrid(
            FlowMapGenerator.ElevationTypes elevationType,
            bool enableSmoothing,
            out float[,] data,
            out int nrows,
            out int ncols,
            out Coordinate SWCorner,
            out Coordinate NECorner)
        {
            if (Bins == null || Bins.Count == 0)
                throw new InvalidOperationException("Field has no bins.");

            int minX = Bins.Min(b => b.X);
            int maxX = Bins.Max(b => b.X);
            int minY = Bins.Min(b => b.Y);
            int maxY = Bins.Max(b => b.Y);

            ncols = maxX - minX + 1;
            nrows = maxY - minY + 1;

            data = new float[nrows, ncols];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                    data[r, c] = ELEVATION_DEM_NO_DATA_VALUE;

            foreach (Bin bin in Bins)
            {
                double h = elevationType switch
                {
                    FlowMapGenerator.ElevationTypes.Initial => bin.InitialElevationM,
                    FlowMapGenerator.ElevationTypes.Current => bin.CurrentElevationM,
                    FlowMapGenerator.ElevationTypes.Target => bin.TargetElevationM,
                    _ => bin.CurrentElevationM
                };
                bool treatAsNoData = h == BIN_NO_DATA_SENTINEL;
                float value = treatAsNoData ? ELEVATION_DEM_NO_DATA_VALUE : (float)h;
                int row = maxY - bin.Y;
                int col = bin.X - minX;
                if (row >= 0 && row < nrows && col >= 0 && col < ncols)
                    data[row, col] = value;
            }

            if (enableSmoothing && ELEVATION_DEM_DEFAULT_SMOOTH_SIGMA > 0)
                data = SmoothDemArray(data, nrows, ncols, ELEVATION_DEM_NO_DATA_VALUE, ELEVATION_DEM_DEFAULT_SMOOTH_SIGMA);

            double topLeftX = FieldMinX + minX * BIN_SIZE_M;
            double topLeftY = FieldMinY + (maxY + 1) * BIN_SIZE_M;
            double swEasting = topLeftX;
            double swNorthing = topLeftY - nrows * BIN_SIZE_M;
            double neEasting = topLeftX + ncols * BIN_SIZE_M;
            double neNorthing = topLeftY;
            UTM.ToLatLon(UTMZone, IsNorthernHemisphere, swEasting, swNorthing, out double swLat, out double swLon);
            UTM.ToLatLon(UTMZone, IsNorthernHemisphere, neEasting, neNorthing, out double neLat, out double neLon);
            SWCorner = new Coordinate(swLat, swLon);
            NECorner = new Coordinate(neLat, neLon);
        }

        /// <summary>
        /// Creates a DEM georeferenced TIFF from this field's bin elevation. The elevation source is
        /// determined by <paramref name="elevationType"/> (Initial, Current, or Target). BIN_NO_DATA_SENTINEL elevation is treated as no-data.
        /// Writes a .tfw world file for UTM georeferencing (same convention as Python demgenerator).
        /// </summary>
        /// <param name="elevationType">Type of elevation to use</param>
        /// <param name="outputFile">Path and name of TIFF to generate</param>
        /// <param name="SWCorner">On return set to SW corner in latitude and longitude</param>
        /// <param name="NECorner">On return set to NE corner in latitude and longitude</param>
        /// <param name="enableSmoothing">If true (default), apply Gaussian smoothing to soften bin edges.</param>
        public void GenerateElevationDEM(
            FlowMapGenerator.ElevationTypes elevationType,
            string outputFile,
            out Coordinate SWCorner,
            out Coordinate NECorner,
            bool enableSmoothing = true)
        {
            if (string.IsNullOrWhiteSpace(outputFile))
                throw new ArgumentException("Output file path is required.", nameof(outputFile));

            BuildElevationGrid(elevationType, enableSmoothing, out float[,] data, out int nrows, out int ncols, out SWCorner, out NECorner);

            double topLeftX = FieldMinX + Bins.Min(b => b.X) * BIN_SIZE_M;
            double topLeftY = FieldMinY + (Bins.Max(b => b.Y) + 1) * BIN_SIZE_M;

            WriteGeoTiff(data, nrows, ncols, outputFile, topLeftX, topLeftY, UTMZone, IsNorthernHemisphere);
            WriteWorldFile(outputFile, topLeftX, topLeftY);
        }

        private static void WriteGeoTiff(float[,] data, int nrows, int ncols, string outputPath,
            double topLeftEasting, double topLeftNorthing, int utmZone, bool isNorthernHemisphere)
        {
            string scriptPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) + Path.DirectorySeparatorChar + WRITE_GEOTIFF_SCRIPT;
            string rawPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".bin");
            string metaPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
            try
            {
                using (var fs = File.Create(rawPath))
                using (var bw = new BinaryWriter(fs))
                {
                    for (int r = 0; r < nrows; r++)
                        for (int c = 0; c < ncols; c++)
                            bw.Write(data[r, c]);
                }

                var inv = CultureInfo.InvariantCulture;
                string json = "{" +
                    "\"nrows\":" + nrows + "," +
                    "\"ncols\":" + ncols + "," +
                    "\"topLeftEasting\":" + topLeftEasting.ToString("R", inv) + "," +
                    "\"topLeftNorthing\":" + topLeftNorthing.ToString("R", inv) + "," +
                    "\"utmZone\":" + utmZone + "," +
                    "\"isNorthernHemisphere\":" + (isNorthernHemisphere ? "true" : "false") + "," +
                    "\"nodata\":" + ELEVATION_DEM_NO_DATA_VALUE.ToString("R", inv) + "}";
                File.WriteAllText(metaPath, json);

                string outputFull = Path.GetFullPath(outputPath);
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" \"{rawPath}\" \"{metaPath}\" \"{outputFull}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? ".",
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Failed to start Python for GeoTIFF write.");
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string msg = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
                        throw new InvalidOperationException(
                            $"GeoTIFF write failed (Python exit {process.ExitCode}). {msg}");
                    }
                }
            }
            finally
            {
                try { if (File.Exists(rawPath)) File.Delete(rawPath); } catch { }
                try { if (File.Exists(metaPath)) File.Delete(metaPath); } catch { }
            }
        }

        private static void WriteWorldFile(string tiffPath, double topLeftX, double topLeftY)
        {
            string dir = Path.GetDirectoryName(tiffPath) ?? ".";
            string baseName = Path.GetFileNameWithoutExtension(tiffPath);
            string tfwPath = Path.Combine(dir, baseName + ".tfw");

            double halfCell = BIN_SIZE_M * 0.5;
            double centerX = topLeftX + halfCell;
            double centerY = topLeftY - halfCell;

            var inv = CultureInfo.InvariantCulture;
            using (var writer = new StreamWriter(tfwPath, false))
            {
                writer.WriteLine(BIN_SIZE_M.ToString("R", inv));
                writer.WriteLine("0");
                writer.WriteLine("0");
                writer.WriteLine((-BIN_SIZE_M).ToString("R", inv));
                writer.WriteLine(centerX.ToString("R", inv));
                writer.WriteLine(centerY.ToString("R", inv));
            }
        }

        private static float[,] SmoothDemArray(float[,] data, int nrows, int ncols, float nodata, double sigma)
        {
            if (sigma <= 0) return data;

            int radius = (int)Math.Ceiling(4.0 * sigma);
            int kernelSize = 2 * radius + 1;
            double[] kernel1D = BuildGaussianKernel1D(sigma, kernelSize);

            double[,] v = new double[nrows, ncols];
            double[,] w = new double[nrows, ncols];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                {
                    bool valid = data[r, c] != nodata;
                    v[r, c] = valid ? data[r, c] : 0.0;
                    w[r, c] = valid ? 1.0 : 0.0;
                }

            Convolve1DRows(v, w, nrows, ncols, kernel1D, kernelSize, radius);
            Convolve1DCols(v, w, nrows, ncols, kernel1D, kernelSize, radius);

            float[,] result = new float[nrows, ncols];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                {
                    if (data[r, c] == nodata) { result[r, c] = nodata; continue; }
                    result[r, c] = (float)(v[r, c] / Math.Max(w[r, c], 1e-12));
                }
            return result;
        }

        private static double[] BuildGaussianKernel1D(double sigma, int size)
        {
            int half = size / 2;
            double[] k = new double[size];
            double sum = 0;
            for (int i = 0; i < size; i++)
            {
                double x = i - half;
                k[i] = Math.Exp(-(x * x) / (2 * sigma * sigma));
                sum += k[i];
            }
            for (int i = 0; i < size; i++) k[i] /= sum;
            return k;
        }

        private static void Convolve1DRows(double[,] v, double[,] w, int nrows, int ncols, double[] kernel, int kSize, int kHalf)
        {
            double[] rowV = new double[ncols];
            double[] rowW = new double[ncols];
            for (int r = 0; r < nrows; r++)
            {
                for (int c = 0; c < ncols; c++)
                {
                    double sumV = 0, sumW = 0;
                    for (int k = 0; k < kSize; k++)
                    {
                        int sc = Math.Clamp(c + k - kHalf, 0, ncols - 1);
                        sumV += v[r, sc] * kernel[k];
                        sumW += w[r, sc] * kernel[k];
                    }
                    rowV[c] = sumV;
                    rowW[c] = sumW;
                }
                for (int c = 0; c < ncols; c++)
                {
                    v[r, c] = rowV[c];
                    w[r, c] = rowW[c];
                }
            }
        }

        private static void Convolve1DCols(double[,] v, double[,] w, int nrows, int ncols, double[] kernel, int kSize, int kHalf)
        {
            double[] colV = new double[nrows];
            double[] colW = new double[nrows];
            for (int c = 0; c < ncols; c++)
            {
                for (int r = 0; r < nrows; r++)
                {
                    double sumV = 0, sumW = 0;
                    for (int k = 0; k < kSize; k++)
                    {
                        int sr = Math.Clamp(r + k - kHalf, 0, nrows - 1);
                        sumV += v[sr, c] * kernel[k];
                        sumW += w[sr, c] * kernel[k];
                    }
                    colV[r] = sumV;
                    colW[r] = sumW;
                }
                for (int r = 0; r < nrows; r++)
                {
                    v[r, c] = colV[r];
                    w[r, c] = colW[r];
                }
            }
        }
    }
}
