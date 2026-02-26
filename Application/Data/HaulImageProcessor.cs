using System.Globalization;
using System.Text.RegularExpressions;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace AgGrade.Data;

/// <summary>
/// Processes haul images (PNG + KML pairs) and writes detected arrow positions to a CSV file.
/// </summary>
public class HaulImageProcessor
{
    /// <summary>Earth radius in meters (WGS84 approximate) used for haversine distance.</summary>
    const double EARTH_RADIUS_M = 6_371_009;
    /// <summary>Maximum BGR value (0–255) per channel to treat a pixel as black in the outline mask.</summary>
    const int BLACK_THRESHOLD = 120;
    /// <summary>Minimum contour area in pixels; contours smaller than this are ignored.</summary>
    const int MIN_AREA = 15;
    /// <summary>Maximum contour area in pixels; contours larger than this are ignored.</summary>
    const int MAX_AREA = 2500;
    /// <summary>Minimum separation in feet for deduplication; points closer than this to an already-kept point are dropped.</summary>
    const double MIN_SEPARATION_FT = 1.0;
    /// <summary>Meters per foot; used to convert minimum separation from feet to meters for haversine.</summary>
    const double METERS_PER_FOOT = 0.3048;
    /// <summary>Epsilon fractions of contour perimeter used by approxPolyDP to try to reduce hull to 3 points.</summary>
    static readonly double[] APPROX_POLY_DP_FRACTIONS = new[] { 0.03, 0.06, 0.10, 0.15, 0.22, 0.30 };
    /// <summary>Minimum vector length (in angle calc) below which we treat as degenerate and return Pi.</summary>
    const double MIN_VECTOR_LENGTH_EPS = 1e-9;
    /// <summary>Minimum direction vector length below which contour is skipped (avoids normalizing zero).</summary>
    const double MIN_DIRECTION_LENGTH = 1e-6;
    /// <summary>Degrees per radian; used to convert atan2 result to degrees.</summary>
    static readonly double DEGREES_PER_RADIAN = 180.0 / Math.PI;
    /// <summary>Full circle in degrees; used to normalize angle to [0, 360).</summary>
    const double DEGREES_PER_CIRCLE = 360.0;
    /// <summary>Maximum |degrees| for which we format as whole number with one decimal (e.g. 270.0).</summary>
    const double MAX_ABS_DEGREES_FOR_WHOLE_NUMBER_FORMAT = 1e15;

    /// <summary>
    /// Processes all PNG+KML pairs in the given folder and writes results to the specified CSV file.
    /// </summary>
    /// <param name="folderPath">Path to the folder containing PNG and KML files.</param>
    /// <param name="csvOutputPath">Path (or file name) for the output CSV file.</param>
    /// <param name="onMessage">Optional callback invoked for each progress message (e.g. per-image arrow count, duplicate count, final path).</param>
    /// <exception cref="InvalidOperationException">Thrown when the folder is invalid, no PNG+KML pairs are found (after skipping PNGs without KML or without bbox), an image cannot be loaded, or no arrows are detected.</exception>
    public void Process(string folderPath, string csvOutputPath, Action<string>? onMessage = null)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            throw new InvalidOperationException("Error: folder path required and must exist.");

        string[] pngs = Directory.GetFiles(folderPath, "*.png").OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray();
        List<(string pngPath, string kmlPath, Bbox bbox)> pairs = new List<(string pngPath, string kmlPath, Bbox bbox)>();

        foreach (string png in pngs)
        {
            string kml = Path.Combine(Path.GetDirectoryName(png)!, Path.GetFileNameWithoutExtension(png) + ".kml");
            if (!File.Exists(kml))
                continue;
            Bbox? bbox = ExtractBboxFromKml(kml);
            if (bbox == null)
                continue;
            pairs.Add((png, kml, bbox!));
        }

        if (pairs.Count == 0)
            return;

        List<CsvRow> allRows = new List<CsvRow>();
        foreach ((string pngPath, _, Bbox bbox) in pairs)
        {
            using Mat? image = Cv2.ImRead(pngPath);
            if (image == null || image.Empty())
                throw new InvalidOperationException($"Could not load: {pngPath}");
            using Mat mask = BuildOutlineMask(image, BLACK_THRESHOLD);
            List<Arrow> arrows = FindArrowsFromOutline(mask, MIN_AREA, MAX_AREA);
            int w = image.Width, h = image.Height;
            List<CsvRow> rows = ArrowsToRows(arrows, w, h, bbox, Path.GetFileName(pngPath));
            allRows.AddRange(rows);
            onMessage?.Invoke($"  {Path.GetFileName(pngPath)}: {arrows.Count} arrow(s)");
        }

        if (allRows.Count == 0)
            throw new InvalidOperationException("Error: no arrows detected in any image.");

        int nBefore = allRows.Count;
        allRows = DropDuplicateDots(allRows, MIN_SEPARATION_FT);
        int nRemoved = nBefore - allRows.Count;
        WriteCsv(csvOutputPath, allRows);
        onMessage?.Invoke($"  Duplicates within 1 ft removed: {nRemoved}");
        onMessage?.Invoke($"Combined CSV saved to: {csvOutputPath} ({allRows.Count} rows from {pairs.Count} image(s))");
    }

    /// <summary>Geographic bounding box from KML coordinates (North, South, East, West in degrees).</summary>
    record Bbox(double North, double South, double East, double West);
    /// <summary>One output CSV row: lat, lon, direction_degrees, source filename.</summary>
    record CsvRow(double Lat, double Lon, double DirectionDegrees, string Source);
    /// <summary>Detected arrow: centroid (Cx, Cy) and unit direction vector (Dx, Dy) in image coordinates.</summary>
    record Arrow(double Cx, double Cy, double Dx, double Dy);

    /// <summary>Parses a KML file and extracts the bounding box from the first &lt;coordinates&gt; element (lon,lat,alt triplets).</summary>
    /// <param name="kmlPath">Full path to the .kml file.</param>
    /// <returns>Bbox with North/South/East/West, or null if parsing fails or fewer than two points are found.</returns>
    static Bbox? ExtractBboxFromKml(string kmlPath)
    {
        string text;
        try { text = File.ReadAllText(kmlPath); }
        catch { return null; }
        Match match = Regex.Match(text, @"<coordinates>([^<]+)</coordinates>", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        string[] parts = Regex.Replace(match.Groups[1].Value, @"[\s,]+", " ").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        List<double> lons = new List<double>();
        List<double> lats = new List<double>();
        for (int i = 0; i + 2 <= parts.Length; i += 3)
        {
            if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                double.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
            {
                lons.Add(lon);
                lats.Add(lat);
            }
        }
        if (lats.Count < 2 || lons.Count < 2) return null;
        return new Bbox(lats.Max(), lats.Min(), lons.Max(), lons.Min());
    }

    /// <summary>Builds a binary mask of "black" pixels (B,G,R all &lt;= threshold), then applies a 2x2 morphological close to connect outline gaps.</summary>
    /// <param name="img">Input BGR image.</param>
    /// <param name="blackThreshold">Maximum value per channel (0–255) to count as black; typically 120.</param>
    /// <returns>Single-channel binary mask (0 or 255) of the dark outline regions.</returns>
    static Mat BuildOutlineMask(Mat img, int blackThreshold)
    {
        Mat[] channels = Cv2.Split(img);
        try
        {
            using Mat black = new Mat();
            Cv2.InRange(img, new Scalar(0, 0, 0), new Scalar(blackThreshold, blackThreshold, blackThreshold), black);
            Mat mask = new Mat();
            Cv2.Threshold(black, mask, 1, 255, ThresholdTypes.Binary);
            using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
            return mask;
        }
        finally
        {
            foreach (Mat c in channels) c?.Dispose();
        }
    }

    /// <summary>Computes the interior angle at vertex <paramref name="pt"/> formed by the segments to <paramref name="prev"/> and <paramref name="next"/>.</summary>
    /// <param name="prev">Previous vertex (neighbor of <paramref name="pt"/>).</param>
    /// <param name="pt">Vertex at which the angle is computed.</param>
    /// <param name="next">Next vertex (neighbor of <paramref name="pt"/>).</param>
    /// <returns>Angle in radians in [0, Pi].</returns>
    static double AngleAtVertex(Point2f prev, Point2f pt, Point2f next)
    {
        float ax = prev.X - pt.X, ay = prev.Y - pt.Y;
        float bx = next.X - pt.X, by = next.Y - pt.Y;
        double na = Math.Sqrt(ax * ax + ay * ay);
        double nb = Math.Sqrt(bx * bx + by * by);
        if (na < MIN_VECTOR_LENGTH_EPS || nb < MIN_VECTOR_LENGTH_EPS) return Math.PI;
        double cosAngle = (ax * bx + ay * by) / (na * nb);
        cosAngle = Math.Clamp(cosAngle, -1.0, 1.0);
        return Math.Acos(cosAngle);
    }

    /// <summary>Finds which vertex of the triangle has the smallest interior angle (the "tip" of the arrow).</summary>
    /// <param name="tri">Exactly three points representing the triangle.</param>
    /// <returns>Index 0, 1, or 2 of the acute (pointy) vertex.</returns>
    static int AcuteVertexIndex(Point2f[] tri)
    {
        if (tri.Length != 3) return 0;
        double a0 = AngleAtVertex(tri[2], tri[0], tri[1]);
        double a1 = AngleAtVertex(tri[0], tri[1], tri[2]);
        double a2 = AngleAtVertex(tri[1], tri[2], tri[0]);
        if (a0 <= a1 && a0 <= a2) return 0;
        if (a1 <= a2) return 1;
        return 2;
    }

    /// <summary>Computes the signed area of the triangle (a,b,c) using the cross-product formula; returns absolute value.</summary>
    /// <param name="a">First vertex of the triangle.</param>
    /// <param name="b">Second vertex of the triangle.</param>
    /// <param name="c">Third vertex of the triangle.</param>
    static double TriangleArea(Point2f a, Point2f b, Point2f c)
    {
        return Math.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));
    }

    /// <summary>Reduces the convex hull to three points: tries approxPolyDP with epsilon = fraction of perimeter (0.03–0.30); if no 3-point result, returns the three hull points that form the largest-area triangle.</summary>
    /// <param name="hullPts">Points of the convex hull (at least 3).</param>
    /// <returns>Three Point2f vertices, or null if fewer than 3 hull points.</returns>
    static Point2f[]? ApproxTriangle(Point[] hullPts)
    {
        if (hullPts.Length < 3) return null;
        Point2f[] pts = hullPts.Select(p => new Point2f(p.X, p.Y)).ToArray();
        if (hullPts.Length == 3) return pts;

        using Mat contour = new Mat(hullPts.Length, 1, MatType.CV_32SC2);
        for (int i = 0; i < hullPts.Length; i++)
            contour.Set(i, 0, hullPts[i]);
        double peri = Cv2.ArcLength(contour, true);
        foreach (double frac in APPROX_POLY_DP_FRACTIONS)
        {
            using Mat approx = new Mat();
            Cv2.ApproxPolyDP(contour, approx, frac * peri, true);
            if (approx.Total() == 3)
            {
                Point2f[] outPts = new Point2f[3];
                for (int i = 0; i < 3; i++)
                {
                    Point v = approx.At<Point>(i, 0);
                    outPts[i] = new Point2f(v.X, v.Y);
                }
                return outPts;
            }
        }

        double bestArea = 0;
        int bi = 0, bj = 1, bk = 2;
        int n = pts.Length;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
                for (int k = j + 1; k < n; k++)
                {
                    double area = TriangleArea(pts[i], pts[j], pts[k]);
                    if (area > bestArea) { bestArea = area; bi = i; bj = j; bk = k; }
                }
        return new[] { pts[bi], pts[bj], pts[bk] };
    }

    /// <summary>Finds contours on the outline mask, filters by area, fits a triangle per contour, computes centroid and tip direction, and returns one Arrow per valid contour.</summary>
    /// <param name="outlineMask">Binary mask from BuildOutlineMask.</param>
    /// <param name="minArea">Minimum contour area (pixels) to accept.</param>
    /// <param name="maxArea">Maximum contour area (pixels) to accept; 0 to disable.</param>
    /// <returns>List of Arrow (centroid + unit direction toward tip).</returns>
    static List<Arrow> FindArrowsFromOutline(Mat outlineMask, int minArea, int maxArea)
    {
        Cv2.FindContours(outlineMask, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        List<Arrow> result = new List<Arrow>();
        foreach (Point[] c in contours)
        {
            double area = Cv2.ContourArea(c);
            if (area < minArea) continue;
            if (maxArea > 0 && area > maxArea) continue;

            Point[] hullPts = Cv2.ConvexHull((IEnumerable<Point>)c, false);
            Point2f[]? tri = ApproxTriangle(hullPts);
            if (tri == null) continue;

            Moments m = Cv2.Moments(InputArray.Create(c));
            if (m.M00 == 0) continue;
            double cx = m.M10 / m.M00;
            double cy = m.M01 / m.M00;

            int tipIdx = AcuteVertexIndex(tri);
            double tx = tri[tipIdx].X, ty = tri[tipIdx].Y;
            double dx = tx - cx, dy = ty - cy;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < MIN_DIRECTION_LENGTH) continue;
            dx /= len;
            dy /= len;
            result.Add(new Arrow(cx, cy, dx, dy));
        }
        return result;
    }

    /// <summary>Maps a pixel (pixelX, pixelY) to geographic lat/lon using the image dimensions and KML bounding box. Origin is top-left; Y increases downward.</summary>
    /// <param name="pixelX">Pixel X coordinate (0 = left).</param>
    /// <param name="pixelY">Pixel Y coordinate (0 = top).</param>
    /// <param name="imageWidth">Image width in pixels.</param>
    /// <param name="imageHeight">Image height in pixels.</param>
    /// <param name="bbox">Geographic bounding box from KML (North, South, East, West in degrees).</param>
    /// <returns>(latitude, longitude) in degrees.</returns>
    static (double lat, double lon) PixelToLatLon(double pixelX, double pixelY, int imageWidth, int imageHeight, Bbox bbox)
    {
        double lon = bbox.West + (bbox.East - bbox.West) * (pixelX / imageWidth);
        double lat = bbox.North - (bbox.North - bbox.South) * (pixelY / imageHeight);
        return (lat, lon);
    }

    /// <summary>Converts a unit direction vector (dx, dy) in image coordinates to compass degrees with North = 0, East = 90. Uses atan2(dx, -dy) so that up in the image is North.</summary>
    /// <param name="dx">X component of the unit direction vector.</param>
    /// <param name="dy">Y component of the unit direction vector.</param>
    /// <returns>Angle in [0, 360) degrees.</returns>
    static double DirectionDegreesNorthUp(double dx, double dy)
    {
        double deg = Math.Atan2(dx, -dy) * DEGREES_PER_RADIAN;
        return deg < 0 ? deg + DEGREES_PER_CIRCLE : deg;
    }

    /// <summary>Converts a list of Arrows (image coordinates) to CSV rows: lat, lon, direction_degrees, and source filename.</summary>
    /// <param name="arrows">Detected arrows (centroid + direction).</param>
    /// <param name="w">Image width in pixels.</param>
    /// <param name="h">Image height in pixels.</param>
    /// <param name="bbox">Geographic bounding box from KML.</param>
    /// <param name="source">Source image filename (e.g. "The Shop 2 A.png").</param>
    static List<CsvRow> ArrowsToRows(List<Arrow> arrows, int w, int h, Bbox bbox, string source)
    {
        return arrows.Select(a =>
        {
            (double lat, double lon) = PixelToLatLon(a.Cx, a.Cy, w, h, bbox);
            double dir = DirectionDegreesNorthUp(a.Dx, a.Dy);
            return new CsvRow(lat, lon, dir, source);
        }).ToList();
    }

    /// <summary>Computes the great-circle distance between two (lat, lon) points in meters using the WGS84 Earth radius.</summary>
    /// <param name="lat1">Latitude of the first point in degrees.</param>
    /// <param name="lon1">Longitude of the first point in degrees.</param>
    /// <param name="lat2">Latitude of the second point in degrees.</param>
    /// <param name="lon2">Longitude of the second point in degrees.</param>
    static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double phi1 = lat1 * Math.PI / 180, phi2 = lat2 * Math.PI / 180;
        double dphi = (lat2 - lat1) * Math.PI / 180;
        double dlam = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2) +
                   Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dlam / 2) * Math.Sin(dlam / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EARTH_RADIUS_M * c;
    }

    /// <summary>Removes duplicate points: keeps the first of each pair that is closer than minSeparationFt (in feet). Each row is compared against all previously kept rows.</summary>
    /// <param name="rows">CSV rows in processing order.</param>
    /// <param name="minSeparationFt">Minimum separation in feet; points closer than this to an already-kept point are dropped.</param>
    /// <returns>Filtered list (first occurrence of each "cluster" within min separation).</returns>
    static List<CsvRow> DropDuplicateDots(List<CsvRow> rows, double minSeparationFt)
    {
        double minM = minSeparationFt * METERS_PER_FOOT;
        List<CsvRow> kept = new List<CsvRow>();
        foreach (CsvRow row in rows)
        {
            bool isDup = false;
            foreach (CsvRow k in kept)
            {
                if (HaversineMeters(row.Lat, row.Lon, k.Lat, k.Lon) < minM) { isDup = true; break; }
            }
            if (!isDup) kept.Add(row);
        }
        return kept;
    }

    /// <summary>Writes CSV rows to the given path with header "lat,lon,direction_degrees,source". Uses UTF-8 without BOM and invariant culture for numbers. Whole-number directions are formatted with one decimal (e.g. 270.0) to match reference output.</summary>
    /// <param name="path">Full path (or file name) for the output CSV file.</param>
    /// <param name="rows">CSV rows to write (lat, lon, direction_degrees, source).</param>
    static void WriteCsv(string path, List<CsvRow> rows)
    {
        System.Text.UTF8Encoding utf8NoBom = new System.Text.UTF8Encoding(false);
        using StreamWriter w = new StreamWriter(path, false, utf8NoBom);
        w.WriteLine("lat,lon,direction_degrees,source");
        CultureInfo ci = CultureInfo.InvariantCulture;
        foreach (CsvRow r in rows)
        {
            string dir = FormatDirection(r.DirectionDegrees, ci);
            w.WriteLine($"{r.Lat.ToString(ci)},{r.Lon.ToString(ci)},{dir},{r.Source}");
        }
    }

    /// <summary>Formats direction in degrees for CSV: whole numbers get one decimal place (e.g. 270.0); others use default ToString for full precision.</summary>
    /// <param name="deg">Direction in degrees.</param>
    /// <param name="ci">Format provider for numeric output (e.g. invariant culture).</param>
    static string FormatDirection(double deg, IFormatProvider ci)
    {
        if (deg == Math.Floor(deg) && Math.Abs(deg) < MAX_ABS_DEGREES_FOR_WHOLE_NUMBER_FORMAT)
            return deg.ToString("0.0", ci);
        return deg.ToString(ci);
    }
}
