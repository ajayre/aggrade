using Microsoft.Data.Sqlite;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Point = OpenCvSharp.Point;

namespace AgGrade.Data
{
    /// <summary>
    /// Imports one AGD file and creates a field database.
    /// Uses AGDLoader for base field DB and an in-process C# port of fieldcreator.py haul algorithms.
    /// </summary>
    public class FieldImporter
    {
        private const double EarthRadiusM = 6_378_137.0;
        private const int BlackThreshold = 120;
        private const int MinArrowArea = 15;
        private const int MaxArrowArea = 2500;
        private const double MinSeparationFt = 1.0;
        private const double MetersPerFoot = 0.3048;

        public Action<string>? Progress { get; }

        public FieldImporter(Action<string>? progress = null)
        {
            Progress = progress;
        }

        private void ReportProgress(string message)
        {
            Progress?.Invoke(message);
        }

        public void CreateFromAgd(string folder, string agdFileName, string databaseFileName, bool generateHaulPaths)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException("Folder is required.", nameof(folder));
            if (string.IsNullOrWhiteSpace(agdFileName))
                throw new ArgumentException("AGD file name is required.", nameof(agdFileName));
            if (string.IsNullOrWhiteSpace(databaseFileName))
                throw new ArgumentException("Database file name is required.", nameof(databaseFileName));

            string agdPath = ResolvePath(folder, agdFileName);
            string dbPath = ResolvePath(folder, databaseFileName);
            if (!File.Exists(agdPath))
                throw new FileNotFoundException("AGD file not found.", agdPath);

            ReportProgress("Loading AGD file...");
            var field = new Field();
            var agdLoader = new AGDLoader();
            agdLoader.Load(field, agdPath);

            ReportProgress("Writing base database...");
            WriteDatabase(field, dbPath);

            if (!generateHaulPaths)
            {
                ReportProgress("Skipping haul arrows and haul paths generation.");
                ReportProgress("Done.");
                return;
            }

            ReportProgress("Preparing AGD bins for haul algorithms...");
            ParseAgdPoints(
                agdPath,
                out List<AgdPoint> points,
                out _);

            BinAgdPoints2Ft(
                points,
                out Dictionary<(int Bx, int By), double> existingValues,
                out Dictionary<(int Bx, int By), double> targetValues,
                out _,
                out _,
                out _,
                out double minX,
                out double minY,
                out _,
                out _,
                out int gridWidth,
                out int gridHeight,
                out int utmZone,
                out bool utmNorth);

            Dictionary<(int Bx, int By), double> existingFilled = FillMissingBins(existingValues, gridWidth, gridHeight);
            Dictionary<(int Bx, int By), double> targetFilled = FillMissingBins(targetValues, gridWidth, gridHeight);
            var binsToDraw = new HashSet<(int Bx, int By)>(existingFilled.Keys);

            ReportProgress("Detecting haul arrows from georeferenced images...");
            List<HaulSample> haulSamples = HaulSamplesFromPngKmlDir(folder);

            ReportProgress("Building haul heading field...");
            Dictionary<(int Bx, int By), double> headingByBin = BuildHaulHeadingByBin(
                haulSamples,
                minX,
                minY,
                gridWidth,
                gridHeight,
                binsToDraw,
                utmZone,
                utmNorth);

            ReportProgress("Writing HaulArrows and HaulPaths to database...");
            PopulateHaulTables(
                dbPath,
                haulSamples,
                headingByBin,
                existingFilled,
                targetFilled,
                minX,
                minY,
                gridWidth,
                gridHeight,
                utmZone,
                utmNorth);

            ReportProgress("Done.");
        }

        private static string ResolvePath(string folder, string fileNameOrPath)
        {
            if (Path.IsPathRooted(fileNameOrPath))
                return Path.GetFullPath(fileNameOrPath);
            return Path.GetFullPath(Path.Combine(folder, fileNameOrPath));
        }

        private static string SqliteConnectionStringForPath(string filePathOrName)
        {
            string full = Path.GetFullPath(filePathOrName);
            return $"Data Source={full};Pooling=False";
        }

        private List<HaulSample> HaulSamplesFromPngKmlDir(string inputDir)
        {
            if (!Directory.Exists(inputDir))
                throw new DirectoryNotFoundException($"Haul image directory not found: {inputDir}");

            string[] pngFiles = Directory.GetFiles(inputDir, "*.png")
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var allRows = new List<HaulCsvRow>();
            int pairCount = 0;

            foreach (string pngPath in pngFiles)
            {
                string kmlPath = Path.Combine(Path.GetDirectoryName(pngPath) ?? inputDir, Path.GetFileNameWithoutExtension(pngPath) + ".kml");
                if (!File.Exists(kmlPath))
                    continue;

                if (!TryExtractBboxFromKml(kmlPath, out Bbox bbox))
                    continue;

                pairCount++;
                using Mat image = Cv2.ImRead(pngPath);
                if (image.Empty())
                    throw new InvalidOperationException($"Could not load image: {pngPath}");

                using Mat outlineMask = BuildOutlineMask(image, BlackThreshold);
                List<DetectedArrow> arrows = FindArrowsFromOutline(outlineMask, MinArrowArea, MaxArrowArea);
                List<HaulCsvRow> rows = ArrowsToRows(arrows, image.Width, image.Height, bbox, Path.GetFileName(pngPath));
                allRows.AddRange(rows);
                ReportProgress($"INFO: {Path.GetFileName(pngPath)}: detected {arrows.Count} arrow(s)");
            }

            if (pairCount == 0)
                throw new InvalidOperationException($"No PNG+KML pairs found in haul image directory: {inputDir}");
            if (allRows.Count == 0)
                throw new InvalidOperationException($"No arrows detected in any haul image in {inputDir}");

            int before = allRows.Count;
            allRows = DropDuplicateDots(allRows, MinSeparationFt);
            int removed = before - allRows.Count;
            if (removed > 0)
                ReportProgress($"INFO: haul image dedup removed {removed} near-duplicate dots (within {MinSeparationFt} ft)");

            ReportProgress($"INFO: total haul samples after dedup: {allRows.Count}");
            var samples = new List<HaulSample>(allRows.Count);
            foreach (HaulCsvRow row in allRows)
                samples.Add(new HaulSample(row.Lat, row.Lon, row.DirectionDegrees));
            return samples;
        }

        private static bool TryExtractBboxFromKml(string kmlPath, out Bbox bbox)
        {
            bbox = default;
            XDocument doc;
            try
            {
                doc = XDocument.Load(kmlPath);
            }
            catch
            {
                return false;
            }

            foreach (XElement el in doc.Descendants())
            {
                if (!el.Name.LocalName.Contains("coordinates", StringComparison.OrdinalIgnoreCase))
                    continue;
                string? txt = el.Value;
                if (string.IsNullOrWhiteSpace(txt))
                    continue;

                string[] parts = txt.Replace("\n", " ").Replace(",", " ")
                    .Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var lons = new List<double>();
                var lats = new List<double>();
                for (int i = 0; i + 1 < parts.Length; i += 3)
                {
                    if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                        double.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                    {
                        lons.Add(lon);
                        lats.Add(lat);
                    }
                }

                if (lons.Count >= 2 && lats.Count >= 2)
                {
                    bbox = new Bbox(lats.Max(), lats.Min(), lons.Max(), lons.Min());
                    return true;
                }
            }

            return false;
        }

        private static Mat BuildOutlineMask(Mat img, int blackThreshold)
        {
            Mat black = new Mat();
            Cv2.InRange(img, new Scalar(0, 0, 0), new Scalar(blackThreshold, blackThreshold, blackThreshold), black);
            Mat mask = new Mat();
            Cv2.Threshold(black, mask, 1, 255, ThresholdTypes.Binary);
            black.Dispose();
            using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
            return mask;
        }

        private static List<DetectedArrow> FindArrowsFromOutline(Mat outlineMask, int minArea, int maxArea)
        {
            Cv2.FindContours(outlineMask, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            var result = new List<DetectedArrow>();
            foreach (Point[] contour in contours)
            {
                double area = Cv2.ContourArea(contour);
                if (area < minArea) continue;
                if (maxArea > 0 && area > maxArea) continue;

                Point[] hull = Cv2.ConvexHull(contour, false);
                Point2f[]? tri = ApproxTriangle(hull);
                if (tri == null) continue;

                Moments m = Cv2.Moments(contour);
                if (m.M00 == 0) continue;
                double cx = m.M10 / m.M00;
                double cy = m.M01 / m.M00;

                int tipIdx = AcuteVertexIndex(tri);
                double tx = tri[tipIdx].X;
                double ty = tri[tipIdx].Y;
                double dx = tx - cx;
                double dy = ty - cy;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-6) continue;
                dx /= len;
                dy /= len;

                result.Add(new DetectedArrow(cx, cy, dx, dy));
            }

            return result;
        }

        private static Point2f[]? ApproxTriangle(Point[] hullPts)
        {
            if (hullPts.Length < 3) return null;
            if (hullPts.Length == 3)
                return new[] { new Point2f(hullPts[0].X, hullPts[0].Y), new Point2f(hullPts[1].X, hullPts[1].Y), new Point2f(hullPts[2].X, hullPts[2].Y) };

            using Mat contour = new Mat(hullPts.Length, 1, MatType.CV_32SC2);
            for (int i = 0; i < hullPts.Length; i++)
                contour.Set(i, 0, hullPts[i]);

            double peri = Cv2.ArcLength(contour, true);
            double[] fracs = new[] { 0.03, 0.06, 0.10, 0.15, 0.22, 0.30 };
            foreach (double frac in fracs)
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

            Point2f[] pts = hullPts.Select(p => new Point2f(p.X, p.Y)).ToArray();
            double bestArea = 0;
            int bi = 0, bj = 1, bk = 2;
            for (int i = 0; i < pts.Length; i++)
            {
                for (int j = i + 1; j < pts.Length; j++)
                {
                    for (int k = j + 1; k < pts.Length; k++)
                    {
                        double area = Math.Abs(
                            (pts[j].X - pts[i].X) * (pts[k].Y - pts[i].Y) -
                            (pts[k].X - pts[i].X) * (pts[j].Y - pts[i].Y));
                        if (area > bestArea)
                        {
                            bestArea = area;
                            bi = i; bj = j; bk = k;
                        }
                    }
                }
            }

            return new[] { pts[bi], pts[bj], pts[bk] };
        }

        private static int AcuteVertexIndex(Point2f[] tri)
        {
            if (tri.Length != 3) return 0;
            double a0 = AngleAtVertex(tri[2], tri[0], tri[1]);
            double a1 = AngleAtVertex(tri[0], tri[1], tri[2]);
            double a2 = AngleAtVertex(tri[1], tri[2], tri[0]);
            if (a0 <= a1 && a0 <= a2) return 0;
            if (a1 <= a2) return 1;
            return 2;
        }

        private static double AngleAtVertex(Point2f prev, Point2f pt, Point2f next)
        {
            double ax = prev.X - pt.X;
            double ay = prev.Y - pt.Y;
            double bx = next.X - pt.X;
            double by = next.Y - pt.Y;
            double na = Math.Sqrt(ax * ax + ay * ay);
            double nb = Math.Sqrt(bx * bx + by * by);
            if (na < 1e-9 || nb < 1e-9) return Math.PI;
            double cos = (ax * bx + ay * by) / (na * nb);
            cos = Math.Clamp(cos, -1.0, 1.0);
            return Math.Acos(cos);
        }

        private static List<HaulCsvRow> ArrowsToRows(List<DetectedArrow> arrows, int imageWidth, int imageHeight, Bbox bbox, string source)
        {
            var rows = new List<HaulCsvRow>(arrows.Count);
            foreach (DetectedArrow a in arrows)
            {
                (double lat, double lon) = PixelToLatLon(a.Cx, a.Cy, imageWidth, imageHeight, bbox);
                double dir = DirectionDegreesNorthUp(a.Dx, a.Dy);
                rows.Add(new HaulCsvRow(lat, lon, dir, source));
            }
            return rows;
        }

        private static (double lat, double lon) PixelToLatLon(double pixelX, double pixelY, int imageWidth, int imageHeight, Bbox bbox)
        {
            double lon = bbox.West + (bbox.East - bbox.West) * (pixelX / imageWidth);
            double lat = bbox.North - (bbox.North - bbox.South) * (pixelY / imageHeight);
            return (lat, lon);
        }

        private static double DirectionDegreesNorthUp(double dx, double dy)
        {
            double deg = Math.Atan2(dx, -dy) * (180.0 / Math.PI);
            return deg < 0 ? deg + 360.0 : deg;
        }

        private static List<HaulCsvRow> DropDuplicateDots(List<HaulCsvRow> rows, double minSeparationFt)
        {
            double minSepM = minSeparationFt * MetersPerFoot;
            var kept = new List<HaulCsvRow>();
            foreach (HaulCsvRow row in rows)
            {
                bool isDup = false;
                foreach (HaulCsvRow k in kept)
                {
                    if (HaversineMeters(row.Lat, row.Lon, k.Lat, k.Lon) < minSepM)
                    {
                        isDup = true;
                        break;
                    }
                }
                if (!isDup)
                    kept.Add(row);
            }
            return kept;
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            double p1 = lat1 * Math.PI / 180.0;
            double p2 = lat2 * Math.PI / 180.0;
            double dp = (lat2 - lat1) * Math.PI / 180.0;
            double dl = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dp / 2.0) * Math.Sin(dp / 2.0) +
                       Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2.0) * Math.Sin(dl / 2.0);
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return EarthRadiusM * c;
        }

        private Dictionary<(int Bx, int By), double> BuildHaulHeadingByBin(
            List<HaulSample> samples,
            double minX,
            double minY,
            int gridWidth,
            int gridHeight,
            HashSet<(int Bx, int By)> binsToDraw,
            int utmZone,
            bool utmNorth)
        {
            if (samples.Count == 0)
                return new Dictionary<(int Bx, int By), double>();

            var accum = new Dictionary<(int Bx, int By), List<(double Dx, double Dy)>>();
            int nSamples = samples.Count;
            for (int i = 0; i < nSamples; i++)
            {
                if ((i + 1) % 2000 == 0 || i == 0)
                    ReportProgress($"INFO: haul_heading: assigning samples {i + 1}/{nSamples}...");

                UTM.UTMCoordinate utm;
                try
                {
                    utm = UTM.FromLatLon(samples[i].Lat, samples[i].Lon);
                }
                catch
                {
                    continue;
                }

                if (utm.Zone != utmZone || utm.IsNorthernHemisphere != utmNorth)
                    continue;

                int bx = (int)Math.Floor((utm.Easting - minX) / Field.BIN_SIZE_M);
                int by = (int)Math.Floor((utm.Northing - minY) / Field.BIN_SIZE_M);
                if (bx < 0 || bx >= gridWidth || by < 0 || by >= gridHeight)
                    continue;

                double rad = samples[i].DirectionDeg * Math.PI / 180.0;
                double dx = Math.Sin(rad);
                double dy = Math.Cos(rad);

                var key = (bx, by);
                if (!accum.TryGetValue(key, out List<(double Dx, double Dy)>? list))
                {
                    list = new List<(double Dx, double Dy)>();
                    accum[key] = list;
                }
                list.Add((dx, dy));
            }

            ReportProgress($"INFO: haul_heading: {accum.Count} bins from samples");
            if (accum.Count == 0)
                return new Dictionary<(int Bx, int By), double>();

            var known = new Dictionary<(int Bx, int By), (double Dx, double Dy)>();
            foreach (KeyValuePair<(int Bx, int By), List<(double Dx, double Dy)>> kv in accum)
            {
                double sx = 0;
                double sy = 0;
                foreach ((double Dx, double Dy) v in kv.Value)
                {
                    sx += v.Dx;
                    sy += v.Dy;
                }
                double norm = Math.Sqrt(sx * sx + sy * sy);
                if (norm <= 0) continue;
                known[kv.Key] = (sx / norm, sy / norm);
            }

            if (known.Count == 0)
                return new Dictionary<(int Bx, int By), double>();

            var fieldVectors = new Dictionary<(int Bx, int By), (double Dx, double Dy)>(known);
            var pending = new HashSet<(int Bx, int By)>(binsToDraw);
            foreach ((int Bx, int By) key in fieldVectors.Keys)
                pending.Remove(key);

            ReportProgress($"INFO: haul_heading: interpolating to {pending.Count} bins (from {known.Count} sample bins)...");
            (int Ox, int Oy)[] neighbors =
            {
                (-1,-1),(0,-1),(1,-1),
                (-1, 0),       (1, 0),
                (-1, 1),(0, 1),(1, 1),
            };

            int iterNum = 0;
            while (pending.Count > 0)
            {
                iterNum++;
                if (iterNum % 5 == 1 || iterNum <= 2)
                    ReportProgress($"INFO: haul_heading: interpolation pass {iterNum}, {pending.Count} pending...");

                bool progressed = false;
                var stillPending = new HashSet<(int Bx, int By)>();
                foreach ((int Bx, int By) bin in pending)
                {
                    double sx = 0;
                    double sy = 0;
                    int count = 0;
                    foreach ((int Ox, int Oy) n in neighbors)
                    {
                        if (fieldVectors.TryGetValue((bin.Bx + n.Ox, bin.By + n.Oy), out (double Dx, double Dy) vec))
                        {
                            sx += vec.Dx;
                            sy += vec.Dy;
                            count++;
                        }
                    }

                    if (count < 2)
                    {
                        stillPending.Add(bin);
                        continue;
                    }

                    double norm = Math.Sqrt(sx * sx + sy * sy);
                    if (norm <= 0)
                    {
                        stillPending.Add(bin);
                        continue;
                    }

                    fieldVectors[bin] = (sx / norm, sy / norm);
                    progressed = true;
                }

                if (!progressed)
                    break;
                pending = stillPending;
            }

            ReportProgress($"INFO: haul_heading: interpolation done; {pending.Count} isolated bins remaining");
            if (pending.Count > 0 && known.Count > 0)
            {
                var knownItems = known.ToList();
                int done = 0;
                int total = pending.Count;
                ReportProgress($"INFO: haul_heading: nearest-vector fallback for {total} isolated bins...");
                foreach ((int Bx, int By) b in pending)
                {
                    done++;
                    if (total >= 10000 && (done % 50000 == 0 || done == total))
                        ReportProgress($"INFO: haul_heading: fallback {done}/{total}...");

                    double? bestD2 = null;
                    (double Dx, double Dy) bestVec = (0, 0);
                    foreach (KeyValuePair<(int Bx, int By), (double Dx, double Dy)> kv in knownItems)
                    {
                        double d2 = (kv.Key.Bx - b.Bx) * (kv.Key.Bx - b.Bx) + (kv.Key.By - b.By) * (kv.Key.By - b.By);
                        if (bestD2 == null || d2 < bestD2.Value)
                        {
                            bestD2 = d2;
                            bestVec = kv.Value;
                        }
                    }
                    if (bestD2 != null)
                        fieldVectors[b] = bestVec;
                }
            }

            ReportProgress("INFO: haul_heading: blending (5 passes)...");
            fieldVectors = BlendVectorField(fieldVectors, binsToDraw, known, passes: 5);

            var headingByBin = new Dictionary<(int Bx, int By), double>(fieldVectors.Count);
            foreach (KeyValuePair<(int Bx, int By), (double Dx, double Dy)> kv in fieldVectors)
            {
                double heading = Math.Atan2(kv.Value.Dx, kv.Value.Dy) * (180.0 / Math.PI);
                heading = ((heading % 360.0) + 360.0) % 360.0;
                headingByBin[kv.Key] = heading;
            }

            return headingByBin;
        }

        private void PopulateHaulTables(
            string dbPath,
            List<HaulSample> haulSamples,
            Dictionary<(int Bx, int By), double> headingByBin,
            Dictionary<(int Bx, int By), double> existingFilled,
            Dictionary<(int Bx, int By), double> targetFilled,
            double minX,
            double minY,
            int gridWidth,
            int gridHeight,
            int utmZone,
            bool utmNorth)
        {
            using var con = new SqliteConnection(SqliteConnectionStringForPath(dbPath));
            con.Open();
            using var tx = con.BeginTransaction();

            var coverageBins = new HashSet<(int Bx, int By)>(existingFilled.Keys);

            using (var clear = con.CreateCommand())
            {
                clear.Transaction = tx;
                clear.CommandText = "DELETE FROM HaulArrows";
                clear.ExecuteNonQuery();
                clear.CommandText = "DELETE FROM HaulPaths";
                clear.ExecuteNonQuery();
                clear.CommandText = "UPDATE FieldState SET HaulPath = 0";
                clear.ExecuteNonQuery();
            }

            var trimmedHaulSamples = new List<HaulSample>(haulSamples.Count);
            foreach (HaulSample sample in haulSamples)
            {
                UTM.UTMCoordinate utm;
                try
                {
                    utm = UTM.FromLatLon(sample.Lat, sample.Lon);
                }
                catch
                {
                    continue;
                }

                if (utm.Zone != utmZone || utm.IsNorthernHemisphere != utmNorth)
                    continue;

                int bx = (int)Math.Floor((utm.Easting - minX) / Field.BIN_SIZE_M);
                int by = (int)Math.Floor((utm.Northing - minY) / Field.BIN_SIZE_M);
                if (bx < 0 || bx >= gridWidth || by < 0 || by >= gridHeight)
                    continue;
                if (!coverageBins.Contains((bx, by)))
                    continue;

                trimmedHaulSamples.Add(sample);
            }
            ReportProgress($"HaulArrows trimmed to bin coverage: {trimmedHaulSamples.Count}/{haulSamples.Count}");

            using (var insArrow = con.CreateCommand())
            {
                insArrow.Transaction = tx;
                insArrow.CommandText = "INSERT INTO HaulArrows (Latitude, Longitude, Heading) VALUES (@Latitude, @Longitude, @Heading)";
                SqliteParameter pLat = insArrow.Parameters.Add("@Latitude", SqliteType.Real);
                SqliteParameter pLon = insArrow.Parameters.Add("@Longitude", SqliteType.Real);
                SqliteParameter pHeading = insArrow.Parameters.Add("@Heading", SqliteType.Real);
                for (int i = 0; i < trimmedHaulSamples.Count; i++)
                {
                    pLat.Value = trimmedHaulSamples[i].Lat;
                    pLon.Value = trimmedHaulSamples[i].Lon;
                    pHeading.Value = trimmedHaulSamples[i].DirectionDeg;
                    insArrow.ExecuteNonQuery();
                    if ((i + 1) % 1000 == 0 || i + 1 == trimmedHaulSamples.Count)
                        ReportProgress($"HaulArrows insert progress: {i + 1}/{trimmedHaulSamples.Count}");
                }
            }

            var cutBins = new List<(int Bx, int By)>();
            for (int by = 0; by < gridHeight; by++)
            {
                for (int bx = 0; bx < gridWidth; bx++)
                {
                    if (!coverageBins.Contains((bx, by)))
                        continue;

                    if (existingFilled.TryGetValue((bx, by), out double ex) &&
                        targetFilled.TryGetValue((bx, by), out double tg) &&
                        ex > tg)
                    {
                        cutBins.Add((bx, by));
                    }
                }
            }

            if (cutBins.Count == 0)
            {
                ReportProgress("INFO: no cut bins found; HaulPaths table remains empty.");
                tx.Commit();
                return;
            }

            using var insPath = con.CreateCommand();
            insPath.Transaction = tx;
            insPath.CommandText = "INSERT INTO HaulPaths (HaulPath, PointNumber, Latitude, Longitude) VALUES (@HaulPath, @PointNumber, @Latitude, @Longitude)";
            SqliteParameter pPath = insPath.Parameters.Add("@HaulPath", SqliteType.Integer);
            SqliteParameter pPoint = insPath.Parameters.Add("@PointNumber", SqliteType.Integer);
            SqliteParameter pPathLat = insPath.Parameters.Add("@Latitude", SqliteType.Real);
            SqliteParameter pPathLon = insPath.Parameters.Add("@Longitude", SqliteType.Real);

            using var updField = con.CreateCommand();
            updField.Transaction = tx;
            updField.CommandText = "UPDATE FieldState SET HaulPath = @HaulPath WHERE X = @X AND Y = @Y";
            SqliteParameter pUpdPath = updField.Parameters.Add("@HaulPath", SqliteType.Integer);
            SqliteParameter pUpdX = updField.Parameters.Add("@X", SqliteType.Integer);
            SqliteParameter pUpdY = updField.Parameters.Add("@Y", SqliteType.Integer);

            int nextHaulId = 1;
            Stopwatch sw = Stopwatch.StartNew();
            for (int idx = 0; idx < cutBins.Count; idx++)
            {
                (int Bx, int By) start = cutBins[idx];
                int haulId = nextHaulId;
                nextHaulId++;

                List<(double XCenter, double YCenter)> soilPath;
                if (headingByBin.ContainsKey(start))
                {
                    soilPath = CalculateSoilPath(
                        start,
                        headingByBin,
                        hitThresholdInches: 3.0,
                        maxTurnDeg: 90.0,
                        lookAheadEnabled: true,
                        lookAheadBins: 6,
                        tractorTurningCircleM: 2.0,
                        maxSteps: 10000);
                }
                else
                {
                    soilPath = new List<(double XCenter, double YCenter)>();
                }

                if (soilPath.Count < 2)
                    continue;

                soilPath = SimplifyCollinear(soilPath, 1e-4);
                if (soilPath.Count < 2)
                    continue;

                for (int pointIdx = 0; pointIdx < soilPath.Count; pointIdx++)
                {
                    double cxLocal = minX + soilPath[pointIdx].XCenter * Field.BIN_SIZE_M;
                    double cyLocal = minY + soilPath[pointIdx].YCenter * Field.BIN_SIZE_M;
                    UTM.ToLatLon(utmZone, utmNorth, cxLocal, cyLocal, out double lat, out double lon);
                    pPath.Value = haulId;
                    pPoint.Value = pointIdx;
                    pPathLat.Value = lat;
                    pPathLon.Value = lon;
                    insPath.ExecuteNonQuery();
                }

                pUpdPath.Value = haulId;
                pUpdX.Value = start.Bx;
                pUpdY.Value = start.By;
                updField.ExecuteNonQuery();

                int done = idx + 1;
                if (done % 10 == 0 || done == cutBins.Count)
                {
                    double elapsed = sw.Elapsed.TotalSeconds;
                    double frac = done / (double)cutBins.Count;
                    double eta = frac > 0 ? elapsed * (1.0 - frac) / frac : 0.0;
                    ReportProgress($"INFO: haul_path_db_progress {done}/{cutBins.Count} ({frac * 100.0:F1}%) elapsed={elapsed:F1}s eta={eta:F1}s");
                }
            }

            tx.Commit();
            ReportProgress($"INFO: completed haul path generation for {cutBins.Count} cut bins.");
        }

        private static List<(double XCenter, double YCenter)> SimplifyCollinear(List<(double XCenter, double YCenter)> path, double eps)
        {
            if (path.Count <= 2)
                return path;

            var simplified = new List<(double XCenter, double YCenter)> { path[0] };
            for (int i = 1; i < path.Count - 1; i++)
            {
                (double x0, double y0) = simplified[simplified.Count - 1];
                (double x1, double y1) = path[i];
                (double x2, double y2) = path[i + 1];
                double vx1 = x1 - x0;
                double vy1 = y1 - y0;
                double vx2 = x2 - x1;
                double vy2 = y2 - y1;
                double area = Math.Abs(vx1 * vy2 - vy1 * vx2);
                if (area < eps)
                    continue;
                simplified.Add(path[i]);
            }
            simplified.Add(path[path.Count - 1]);
            return simplified;
        }

        private Dictionary<(int Bx, int By), (double Dx, double Dy)> BlendVectorField(
            Dictionary<(int Bx, int By), (double Dx, double Dy)> fieldVectors,
            HashSet<(int Bx, int By)> binsToDraw,
            Dictionary<(int Bx, int By), (double Dx, double Dy)> knownVectors,
            int passes)
        {
            if (fieldVectors.Count == 0)
                return fieldVectors;

            (int Ox, int Oy)[] neighbors =
            {
                (-1,-1),(0,-1),(1,-1),
                (-1, 0),       (1, 0),
                (-1, 1),(0, 1),(1, 1),
            };

            var blended = new Dictionary<(int Bx, int By), (double Dx, double Dy)>(fieldVectors);
            for (int pass = 0; pass < Math.Max(0, passes); pass++)
            {
                ReportProgress($"INFO: haul_heading: blend pass {pass + 1}/{passes}...");
                var next = new Dictionary<(int Bx, int By), (double Dx, double Dy)>();
                foreach ((int Bx, int By) b in binsToDraw)
                {
                    if (!blended.TryGetValue(b, out (double Dx, double Dy) baseVec))
                        continue;

                    bool isKnown = knownVectors.ContainsKey(b);
                    double selfWeight = isKnown ? 6.0 : 1.0;
                    double neighborWeight = isKnown ? 0.35 : 0.8;

                    double sx = baseVec.Dx * selfWeight;
                    double sy = baseVec.Dy * selfWeight;
                    double w = selfWeight;

                    foreach ((int Ox, int Oy) n in neighbors)
                    {
                        if (blended.TryGetValue((b.Bx + n.Ox, b.By + n.Oy), out (double Dx, double Dy) v))
                        {
                            sx += v.Dx * neighborWeight;
                            sy += v.Dy * neighborWeight;
                            w += neighborWeight;
                        }
                    }

                    if (w <= 0)
                    {
                        next[b] = baseVec;
                        continue;
                    }

                    sx /= w;
                    sy /= w;
                    double norm = Math.Sqrt(sx * sx + sy * sy);
                    if (norm <= 0)
                        next[b] = baseVec;
                    else
                        next[b] = (sx / norm, sy / norm);
                }

                blended = next;
            }

            return blended;
        }

        private List<(double XCenter, double YCenter)> CalculateSoilPath(
            (int Bx, int By) startBin,
            Dictionary<(int Bx, int By), double> headingByBin,
            double hitThresholdInches,
            double maxTurnDeg,
            bool lookAheadEnabled,
            int lookAheadBins,
            double tractorTurningCircleM,
            int maxSteps)
        {
            if (!headingByBin.ContainsKey(startBin))
                return new List<(double XCenter, double YCenter)>();

            List<(int Bx, int By)> route = BuildDiscreteBinRoute(startBin, headingByBin, hitThresholdInches, maxTurnDeg, maxSteps);
            if (route.Count == 0)
                return new List<(double XCenter, double YCenter)>();
            if (!lookAheadEnabled)
                return route.Select(r => ((double)r.Bx + 0.5, (double)r.By + 0.5)).ToList();
            return SmoothRouteWithCurvatureLimit(route, headingByBin, lookAheadBins, tractorTurningCircleM);
        }

        private List<(int Bx, int By)> BuildDiscreteBinRoute(
            (int Bx, int By) startBin,
            Dictionary<(int Bx, int By), double> headingByBin,
            double hitThresholdInches,
            double maxTurnDeg,
            int maxSteps)
        {
            if (!headingByBin.TryGetValue(startBin, out double currentHeading))
                return new List<(int Bx, int By)>();

            double thresholdBins = Math.Max(0.01, hitThresholdInches / 24.0);
            var bins = new HashSet<(int Bx, int By)>(headingByBin.Keys);
            var route = new List<(int Bx, int By)> { startBin };
            var visited = new HashSet<(int Bx, int By)> { startBin };
            (int Bx, int By) current = startBin;

            for (int step = 0; step < maxSteps; step++)
            {
                double posX = current.Bx + 0.5;
                double posY = current.By + 0.5;
                (int Bx, int By)? next = FindNextBinOnHeading(posX, posY, currentHeading, bins, thresholdBins, current);
                if (next == null || !headingByBin.TryGetValue(next.Value, out double nextHeading))
                    break;
                if (TurnAngleDeg(currentHeading, nextHeading) > maxTurnDeg)
                    break;
                if (visited.Contains(next.Value))
                    break;

                route.Add(next.Value);
                visited.Add(next.Value);
                current = next.Value;
                currentHeading = nextHeading;
            }

            return route;
        }

        private (int Bx, int By)? FindNextBinOnHeading(
            double posX,
            double posY,
            double headingDeg,
            HashSet<(int Bx, int By)> bins,
            double thresholdBins,
            (int Bx, int By) currentBin)
        {
            (double dx, double dy) = HeadingToVec(headingDeg);
            double perpX = -dy;
            double perpY = dx;
            double r2 = thresholdBins * thresholdBins;

            var candidates = new HashSet<(int Bx, int By)>();
            for (int u = 1; u <= 8; u++)
            {
                for (int v = -4; v <= 4; v++)
                {
                    double px = posX + u * dx + v * perpX;
                    double py = posY + u * dy + v * perpY;
                    int bx = (int)Math.Round(px - 0.5);
                    int by = (int)Math.Round(py - 0.5);
                    var cand = (bx, by);
                    if (cand != currentBin && bins.Contains(cand))
                        candidates.Add(cand);
                }
            }

            if (candidates.Count == 0)
            {
                for (int ox = -10; ox <= 10; ox++)
                {
                    for (int oy = -10; oy <= 10; oy++)
                    {
                        var cand = (currentBin.Bx + ox, currentBin.By + oy);
                        if (cand != currentBin && bins.Contains(cand))
                            candidates.Add(cand);
                    }
                }
            }

            double? bestT = null;
            (int Bx, int By)? bestBin = null;
            foreach ((int Bx, int By) b in candidates)
            {
                double cx = b.Bx + 0.5;
                double cy = b.By + 0.5;
                double rx = cx - posX;
                double ry = cy - posY;
                double tCenter = rx * dx + ry * dy;
                if (tCenter <= 1e-9)
                    continue;

                double perp2 = rx * rx + ry * ry - tCenter * tCenter;
                if (perp2 < 0) perp2 = 0;
                if (perp2 > r2) continue;

                double dt = Math.Sqrt(r2 - perp2);
                double tEnter = tCenter - dt;
                if (tEnter <= 1e-9) tEnter = tCenter + dt;
                if (tEnter <= 1e-9) continue;

                if (bestT == null || tEnter < bestT.Value)
                {
                    bestT = tEnter;
                    bestBin = b;
                }
            }

            return bestBin;
        }

        private List<(double XCenter, double YCenter)> SmoothRouteWithCurvatureLimit(
            List<(int Bx, int By)> route,
            Dictionary<(int Bx, int By), double> headingByBin,
            int lookAheadBins,
            double tractorTurningCircleM)
        {
            if (route.Count < 2)
            {
                if (route.Count == 1)
                    return new List<(double XCenter, double YCenter)> { (route[0].Bx + 0.5, route[0].By + 0.5) };
                return new List<(double XCenter, double YCenter)>();
            }

            double radiusM = Math.Max(0.01, tractorTurningCircleM * 0.5);
            double maxDeltaDeg = (Field.BIN_SIZE_M / radiusM) * (180.0 / Math.PI);
            int n = route.Count;

            var baseHeadings = new List<double>(n);
            for (int i = 0; i < n; i++)
                baseHeadings.Add(headingByBin.TryGetValue(route[i], out double h) ? (h % 360.0) : 0.0);

            int window = Math.Max(2, lookAheadBins);
            var desired = new List<double>(n);
            for (int i = 0; i < n; i++)
            {
                int j1 = Math.Min(n, i + window);
                desired.Add(CircularMeanDeg(baseHeadings.GetRange(i, j1 - i)));
            }

            var desiredLp = new List<double> { desired[0] };
            for (int i = 1; i < n; i++)
                desiredLp.Add(BlendAngleDeg(desiredLp[i - 1], desired[i], 0.30));

            var smoothed = new List<double> { baseHeadings[0] };
            for (int i = 1; i < n; i++)
            {
                double prev = smoothed[i - 1];
                double delta = SignedTurnDeltaDeg(prev, desiredLp[i]);
                double clamped = Math.Max(-maxDeltaDeg, Math.Min(maxDeltaDeg, delta));
                smoothed.Add((prev + clamped) % 360.0);
            }

            for (int pass = 0; pass < 2; pass++)
            {
                var relaxed = new List<double>(smoothed);
                for (int i = 1; i < n - 1; i++)
                {
                    double localAvg = CircularMeanDeg(new[] { smoothed[i - 1], smoothed[i], smoothed[i + 1] });
                    relaxed[i] = BlendAngleDeg(smoothed[i], localAvg, 0.35);
                }

                var constrained = new List<double> { relaxed[0] };
                for (int i = 1; i < n; i++)
                {
                    double prev = constrained[i - 1];
                    double delta = SignedTurnDeltaDeg(prev, relaxed[i]);
                    double clamped = Math.Max(-maxDeltaDeg, Math.Min(maxDeltaDeg, delta));
                    constrained.Add((prev + clamped) % 360.0);
                }

                smoothed = constrained;
            }

            var poly = new List<(double XCenter, double YCenter)>();
            for (int i = 0; i < route.Count - 1; i++)
            {
                (double p0x, double p0y) = (route[i].Bx + 0.5, route[i].By + 0.5);
                (double p1x, double p1y) = (route[i + 1].Bx + 0.5, route[i + 1].By + 0.5);
                double dist = Math.Sqrt((p1x - p0x) * (p1x - p0x) + (p1y - p0y) * (p1y - p0y));
                if (dist <= 1e-9)
                    continue;

                (double d0x, double d0y) = HeadingToVec(smoothed[i]);
                (double d1x, double d1y) = HeadingToVec(smoothed[i + 1]);
                double turnHere = TurnAngleDeg(smoothed[i], smoothed[i + 1]);
                double sharpness = Math.Max(0.0, Math.Min(1.0, turnHere / 180.0));
                double tangentScale = 0.45 * (1.0 - sharpness) + 0.12 * sharpness;
                (double t0x, double t0y) = (d0x * dist * tangentScale, d0y * dist * tangentScale);
                (double t1x, double t1y) = (d1x * dist * tangentScale, d1y * dist * tangentScale);

                double pad = 0.22;
                double minX = Math.Min(p0x, p1x) - pad;
                double maxX = Math.Max(p0x, p1x) + pad;
                double minY = Math.Min(p0y, p1y) - pad;
                double maxY = Math.Max(p0y, p1y) + pad;

                int samples = 6;
                for (int s = 0; s < samples; s++)
                {
                    double t = s / (double)samples;
                    double t2 = t * t;
                    double t3 = t2 * t;
                    double h00 = 2 * t3 - 3 * t2 + 1;
                    double h10 = t3 - 2 * t2 + t;
                    double h01 = -2 * t3 + 3 * t2;
                    double h11 = t3 - t2;
                    double x = h00 * p0x + h10 * t0x + h01 * p1x + h11 * t1x;
                    double y = h00 * p0y + h10 * t0y + h01 * p1y + h11 * t1y;
                    x = Math.Max(minX, Math.Min(maxX, x));
                    y = Math.Max(minY, Math.Min(maxY, y));
                    poly.Add((x, y));
                }
            }

            poly.Add((route[route.Count - 1].Bx + 0.5, route[route.Count - 1].By + 0.5));
            return poly;
        }

        private static (double Dx, double Dy) HeadingToVec(double headingDeg)
        {
            double r = headingDeg * Math.PI / 180.0;
            return (Math.Sin(r), Math.Cos(r));
        }

        private static double TurnAngleDeg(double aDeg, double bDeg)
        {
            return Math.Abs(((bDeg - aDeg + 180.0) % 360.0) - 180.0);
        }

        private static double SignedTurnDeltaDeg(double fromDeg, double toDeg)
        {
            return ((toDeg - fromDeg + 180.0) % 360.0) - 180.0;
        }

        private static double CircularMeanDeg(IEnumerable<double> anglesDeg)
        {
            double sx = 0;
            double sy = 0;
            int n = 0;
            double last = 0;
            foreach (double a in anglesDeg)
            {
                double r = a * Math.PI / 180.0;
                sx += Math.Cos(r);
                sy += Math.Sin(r);
                last = a;
                n++;
            }
            if (n == 0) return 0.0;
            if (Math.Abs(sx) < 1e-12 && Math.Abs(sy) < 1e-12) return last;
            double outDeg = Math.Atan2(sy, sx) * 180.0 / Math.PI;
            return (outDeg % 360.0 + 360.0) % 360.0;
        }

        private static double BlendAngleDeg(double baseDeg, double targetDeg, double alpha)
        {
            alpha = Math.Max(0.0, Math.Min(1.0, alpha));
            double delta = SignedTurnDeltaDeg(baseDeg, targetDeg);
            return (baseDeg + alpha * delta) % 360.0;
        }

        private void ParseAgdPoints(string agdPath, out List<AgdPoint> points, out List<AgdBenchmark> benchmarks)
        {
            string text;
            byte[] bytes = File.ReadAllBytes(agdPath);
            text = System.Text.Encoding.UTF8.GetString(bytes);

            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            if (lines.Length == 0)
                throw new InvalidDataException("AGD file is empty.");

            points = new List<AgdPoint>();
            benchmarks = new List<AgdBenchmark>();
            NumberFormatInfo inv = CultureInfo.InvariantCulture.NumberFormat;

            for (int i = 1; i < lines.Length; i++)
            {
                string raw = lines[i].Trim();
                if (raw.Length == 0)
                    continue;

                string[] split = raw.Split(',');
                if (split.Length < 4)
                    continue;

                string latS = split[0].Trim();
                string lonS = split[1].Trim();
                string existingS = split[2].Trim();
                string targetS = split[3].Trim();
                string pointName = split.Length > 4 ? split[4].Trim() : string.Empty;
                string codeS = split.Length > 5 ? split[5].Trim() : string.Empty;

                string benchmarkToken = (pointName + " " + codeS).ToUpperInvariant();
                if (benchmarkToken.Contains("MB") || benchmarkToken.Contains("BM"))
                {
                    if (!double.TryParse(latS, NumberStyles.Float, inv, out double bmLat) ||
                        !double.TryParse(lonS, NumberStyles.Float, inv, out double bmLon) ||
                        !double.TryParse(existingS, NumberStyles.Float, inv, out double bmElevation))
                    {
                        continue;
                    }

                    string name = pointName.Length > 0 ? pointName : codeS;
                    benchmarks.Add(new AgdBenchmark(bmLat, bmLon, name, bmElevation));
                    continue;
                }

                if (existingS.Length == 0 || targetS.Length == 0)
                    continue;

                if (!double.TryParse(latS, NumberStyles.Float, inv, out double lat) ||
                    !double.TryParse(lonS, NumberStyles.Float, inv, out double lon) ||
                    !double.TryParse(existingS, NumberStyles.Float, inv, out double existing) ||
                    !double.TryParse(targetS, NumberStyles.Float, inv, out double target))
                {
                    continue;
                }

                points.Add(new AgdPoint(lat, lon, existing, target));
            }

            if (points.Count == 0)
                throw new InvalidDataException("No valid elevation points parsed from AGD.");
        }

        private void BinAgdPoints2Ft(
            List<AgdPoint> points,
            out Dictionary<(int Bx, int By), double> existingValues,
            out Dictionary<(int Bx, int By), double> targetValues,
            out Dictionary<(int Bx, int By), (double Lat, double Lon)> centroids,
            out double meanLat,
            out double meanLon,
            out double minX,
            out double minY,
            out double maxX,
            out double maxY,
            out int gridWidth,
            out int gridHeight,
            out int utmZone,
            out bool utmNorth)
        {
            if (points.Count == 0)
                throw new InvalidDataException("No AGD points to bin.");

            meanLat = points.Average(p => p.Lat);
            meanLon = points.Average(p => p.Lon);
            double minLat = points.Min(p => p.Lat);
            double minLon = points.Min(p => p.Lon);

            UTM.UTMCoordinate sw = UTM.FromLatLon(minLat, minLon);
            utmZone = sw.Zone;
            utmNorth = sw.IsNorthernHemisphere;

            var utmPoints = new List<UtmAgdPoint>(points.Count);
            foreach (AgdPoint p in points)
            {
                UTM.UTMCoordinate c = UTM.FromLatLon(p.Lat, p.Lon);
                if (c.Zone != utmZone)
                    throw new InvalidDataException($"AGD field crosses UTM zones (got zone {c.Zone}, expected {utmZone}).");
                utmPoints.Add(new UtmAgdPoint(c.Easting, c.Northing, p.Existing, p.Target));
            }

            minX = utmPoints.Min(p => p.Easting);
            maxX = utmPoints.Max(p => p.Easting);
            minY = utmPoints.Min(p => p.Northing);
            maxY = utmPoints.Max(p => p.Northing);

            gridWidth = (int)Math.Ceiling((maxX - minX) / Field.BIN_SIZE_M);
            gridHeight = (int)Math.Ceiling((maxY - minY) / Field.BIN_SIZE_M);
            if (gridWidth <= 0 || gridHeight <= 0)
                throw new InvalidDataException("Computed non-positive grid dimensions from AGD data.");

            var grouped = new Dictionary<(int Bx, int By), AccumPair>();
            foreach (UtmAgdPoint p in utmPoints)
            {
                int bx = (int)Math.Floor((p.Easting - minX) / Field.BIN_SIZE_M);
                int by = (int)Math.Floor((p.Northing - minY) / Field.BIN_SIZE_M);
                if (bx < 0 || bx >= gridWidth || by < 0 || by >= gridHeight)
                    continue;
                var key = (bx, by);
                if (grouped.TryGetValue(key, out AccumPair ap))
                    grouped[key] = new AccumPair(ap.SumExisting + p.Existing, ap.SumTarget + p.Target, ap.Count + 1);
                else
                    grouped[key] = new AccumPair(p.Existing, p.Target, 1);
            }

            existingValues = new Dictionary<(int Bx, int By), double>();
            targetValues = new Dictionary<(int Bx, int By), double>();
            foreach (KeyValuePair<(int Bx, int By), AccumPair> kv in grouped)
            {
                existingValues[kv.Key] = kv.Value.SumExisting / kv.Value.Count;
                targetValues[kv.Key] = kv.Value.SumTarget / kv.Value.Count;
            }

            centroids = new Dictionary<(int Bx, int By), (double Lat, double Lon)>(gridWidth * gridHeight);
            for (int by = 0; by < gridHeight; by++)
            {
                for (int bx = 0; bx < gridWidth; bx++)
                {
                    double cx = minX + (bx + 0.5) * Field.BIN_SIZE_M;
                    double cy = minY + (by + 0.5) * Field.BIN_SIZE_M;
                    UTM.ToLatLon(utmZone, utmNorth, cx, cy, out double lat, out double lon);
                    centroids[(bx, by)] = (lat, lon);
                }
            }
        }

        private Dictionary<(int Bx, int By), double> FillMissingBins(
            Dictionary<(int Bx, int By), double> values,
            int gridWidth,
            int gridHeight)
        {
            int maxBinX = gridWidth - 1;
            int maxBinY = gridHeight - 1;

            var original = new HashSet<(int Bx, int By)>(values.Keys);
            var output = new Dictionary<(int Bx, int By), double>(values);
            if (original.Count == 0)
                return output;

            for (int by = 0; by <= maxBinY; by++)
            {
                for (int bx = 0; bx <= maxBinX; bx++)
                {
                    if (original.Contains((bx, by)))
                        continue;

                    int origNeighbors = 0;
                    if (by - 1 >= 0 && original.Contains((bx, by - 1))) origNeighbors++;
                    if (by + 1 <= maxBinY && original.Contains((bx, by + 1))) origNeighbors++;
                    if (bx - 1 >= 0 && original.Contains((bx - 1, by))) origNeighbors++;
                    if (bx + 1 <= maxBinX && original.Contains((bx + 1, by))) origNeighbors++;
                    if (origNeighbors < 2)
                        continue;

                    double sum = 0.0;
                    int count = 0;
                    if (by - 1 >= 0 && output.TryGetValue((bx, by - 1), out double v0)) { sum += v0; count++; }
                    if (by + 1 <= maxBinY && output.TryGetValue((bx, by + 1), out double v1)) { sum += v1; count++; }
                    if (bx - 1 >= 0 && output.TryGetValue((bx - 1, by), out double v2)) { sum += v2; count++; }
                    if (bx + 1 <= maxBinX && output.TryGetValue((bx + 1, by), out double v3)) { sum += v3; count++; }
                    if (count > 0)
                        output[(bx, by)] = sum / count;
                }
            }
            return output;
        }

        private static void WriteDatabase(Field field, string dbPath)
        {
            if (field.Bins == null || field.Bins.Count == 0)
                throw new InvalidDataException("AGDLoader produced no bins.");

            int gridWidth = 0;
            int gridHeight = 0;
            foreach (Bin bin in field.Bins)
            {
                if (bin.X + 1 > gridWidth) gridWidth = bin.X + 1;
                if (bin.Y + 1 > gridHeight) gridHeight = bin.Y + 1;
            }

            var binsByKey = new Dictionary<(int X, int Y), Bin>(field.Bins.Count);
            foreach (Bin bin in field.Bins)
                binsByKey[(bin.X, bin.Y)] = bin;

            UTM.ToLatLon(field.UTMZone, field.IsNorthernHemisphere, field.FieldMinX, field.FieldMinY, out double dataMinLat, out double dataMinLon);
            UTM.ToLatLon(field.UTMZone, field.IsNorthernHemisphere, field.FieldMaxX, field.FieldMaxY, out double dataMaxLat, out double dataMaxLon);

            var db = new Database();
            try
            {
                db.CreateEmptyFieldDatabase(dbPath);

                var rows = new List<Database.BinState>(gridWidth * gridHeight);
                for (int by = 0; by < gridHeight; by++)
                {
                    for (int bx = 0; bx < gridWidth; bx++)
                    {
                        if (binsByKey.TryGetValue((bx, by), out Bin? bin) && bin != null)
                        {
                            double initialHeight = bin.CurrentElevationM;
                            double currentHeight = bin.CurrentElevationM;
                            double targetHeight = bin.TargetElevationM;

                            // AGDLoader leaves bins with no valid data at zero values.
                            // Persist those bins using the field no-data sentinel.
                            if (bin.CurrentElevationM == 0 &&
                                bin.TargetElevationM == 0 &&
                                bin.CutAmountM == 0 &&
                                bin.FillAmountM == 0)
                            {
                                initialHeight = Field.BIN_NO_DATA_SENTINEL;
                                currentHeight = Field.BIN_NO_DATA_SENTINEL;
                                targetHeight = Field.BIN_NO_DATA_SENTINEL;
                            }

                            rows.Add(new Database.BinState(
                                bx,
                                by,
                                initialHeight,
                                currentHeight,
                                targetHeight,
                                bin.Centroid.Latitude,
                                bin.Centroid.Longitude,
                                0));
                        }
                        else
                        {
                            rows.Add(new Database.BinState(
                                bx,
                                by,
                                Field.BIN_NO_DATA_SENTINEL,
                                Field.BIN_NO_DATA_SENTINEL,
                                Field.BIN_NO_DATA_SENTINEL,
                                0,
                                0,
                                0));
                        }
                    }
                }

                db.BulkInsertFieldStateRows(rows);
                db.SetData(Database.DataNames.GridWidth, gridWidth);
                db.SetData(Database.DataNames.GridHeight, gridHeight);
                db.SetData(Database.DataNames.MeanLat, field.FieldCentroidLat);
                db.SetData(Database.DataNames.MeanLon, field.FieldCentroidLon);
                db.SetData(Database.DataNames.MinX, field.FieldMinX);
                db.SetData(Database.DataNames.MinY, field.FieldMinY);
                db.SetData(Database.DataNames.MaxX, field.FieldMaxX);
                db.SetData(Database.DataNames.MaxY, field.FieldMaxY);
                db.SetData(Database.DataNames.MinLat, dataMinLat);
                db.SetData(Database.DataNames.MinLon, dataMinLon);
                db.SetData(Database.DataNames.MaxLat, dataMaxLat);
                db.SetData(Database.DataNames.MaxLon, dataMaxLon);
                db.SetData(Database.DataNames.CompletedCutCY, 0.0);
                db.SetData(Database.DataNames.CompletedFillCY, 0.0);
                db.SetData(Database.DataNames.TotalCutCY, field.TotalCutCY);
                db.SetData(Database.DataNames.TotalFillCY, field.TotalFillCY);
                db.SetBoolData(Database.DataNames.Calibrated, false);
                db.SetData(Database.DataNames.HeightOffsetM, 0.0);
                db.SetData(Database.DataNames.EastingOffsetM, 0.0);
                db.SetData(Database.DataNames.NorthingOffsetM, 0.0);

                foreach (Benchmark bm in field.Benchmarks)
                    db.InsertBenchmarkRow(bm.Location.Latitude, bm.Location.Longitude, bm.Name, bm.Elevation);
            }
            finally
            {
                db.Close();
            }
        }

        private readonly struct Bbox
        {
            public readonly double North;
            public readonly double South;
            public readonly double East;
            public readonly double West;
            public Bbox(double north, double south, double east, double west)
            {
                North = north;
                South = south;
                East = east;
                West = west;
            }
        }

        private readonly struct DetectedArrow
        {
            public readonly double Cx;
            public readonly double Cy;
            public readonly double Dx;
            public readonly double Dy;
            public DetectedArrow(double cx, double cy, double dx, double dy)
            {
                Cx = cx;
                Cy = cy;
                Dx = dx;
                Dy = dy;
            }
        }

        private readonly struct HaulCsvRow
        {
            public readonly double Lat;
            public readonly double Lon;
            public readonly double DirectionDegrees;
            public readonly string Source;
            public HaulCsvRow(double lat, double lon, double directionDegrees, string source)
            {
                Lat = lat;
                Lon = lon;
                DirectionDegrees = directionDegrees;
                Source = source;
            }
        }

        private readonly struct HaulSample
        {
            public readonly double Lat;
            public readonly double Lon;
            public readonly double DirectionDeg;
            public HaulSample(double lat, double lon, double directionDeg)
            {
                Lat = lat;
                Lon = lon;
                DirectionDeg = directionDeg;
            }
        }

        private readonly struct AgdPoint
        {
            public readonly double Lat;
            public readonly double Lon;
            public readonly double Existing;
            public readonly double Target;

            public AgdPoint(double lat, double lon, double existing, double target)
            {
                Lat = lat;
                Lon = lon;
                Existing = existing;
                Target = target;
            }
        }

        private readonly struct AgdBenchmark
        {
            public readonly double Lat;
            public readonly double Lon;
            public readonly string Name;
            public readonly double ElevationM;
            public AgdBenchmark(double lat, double lon, string name, double elevationM)
            {
                Lat = lat;
                Lon = lon;
                Name = name;
                ElevationM = elevationM;
            }
        }

        private readonly struct UtmAgdPoint
        {
            public readonly double Easting;
            public readonly double Northing;
            public readonly double Existing;
            public readonly double Target;
            public UtmAgdPoint(double easting, double northing, double existing, double target)
            {
                Easting = easting;
                Northing = northing;
                Existing = existing;
                Target = target;
            }
        }

        private readonly struct AccumPair
        {
            public readonly double SumExisting;
            public readonly double SumTarget;
            public readonly int Count;
            public AccumPair(double sumExisting, double sumTarget, int count)
            {
                SumExisting = sumExisting;
                SumTarget = sumTarget;
                Count = count;
            }
        }
    }
}
