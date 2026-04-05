using System;
using System.Collections.Generic;
using System.IO;

namespace AgGrade.Data
{
    /// <summary>
    /// Builds a field SQLite database from the survey in <see cref="FieldDesign.SurveyFileName"/> (.txt multiplane or .ags)
    /// and a <see cref="FieldDesign"/>, matching <c>Field Creator/fieldcreator.py</c> schema and data keys.
    /// Existing elevations are generated over the full grid using the same MLS + warp smoothing pipeline as
    /// <c>AI Field Design/fielddesign.py</c> <c>build_all_bins</c>, so sparse surveys and dense surveys use one consistent
    /// field-surface construction path before design-plane solve / DB write.
    /// Does not write an AGD file; HaulPaths and HaulArrows tables are left empty.
    /// </summary>
    public static class FieldCreator
    {
        private const double CubicYardsPerCubicMeter = 1.30795061931439;
        /// <summary>International acre (4046.8564224 m²).</summary>
        private const double SquareMetersPerAcre = 4046.8564224;
        private const double SpatialHashCellM = 25.0;
        private const int EmptyBinMlsK = 64;
        private const double EmptyBinInterpEpsSq = 1e-12;
        private const double EmptyBinMlsRegScale = 5e-6;
        private const double EmptyBinMlsSmoothFrac = 0.75;
        private const int WarpBaseRadiusBins = 3;
        private const int WarpBasePasses = 2;
        private const int WarpRadiusBins = 12;
        private const int WarpPasses = 4;
        private const double WarpAlpha = 0.85;
        private const double WarpTolM = 0.009;
        private const int WarpTolIters = 8;
        private const int WarpTolRadiusBins = 8;
        private const int WarpTolPasses = 3;
        private const double WarpTolAlpha = 1.0;
        private const int WarpPostRadiusBins = 4;
        private const int WarpPostPasses = 3;
        private const int WarpPostTolIters = 6;

        /// <summary>
        /// Summary values from <see cref="CreateFromSurveyAndDesign(FieldDesign, string)"/>.
        /// Composite slope/heading follow <c>AI Field Design/fielddesign.py</c> <c>composite_slope_and_heading</c>
        /// (steepest downhill grade from dz/dE=a, dz/dN=b).
        /// </summary>
        public readonly struct Statistics
        {
            /// <summary>Full bin grid footprint: <c>gridWidth × gridHeight × bin area</c>, in acres.</summary>
            public double TotalAreaAcres { get; init; }
            public double TotalCutCY { get; init; }
            public double TotalFillCY { get; init; }
            /// <summary>Downhill direction on the design plane, degrees clockwise from north.</summary>
            public double CompositeSlopeHeadingDeg { get; init; }
            /// <summary>Steepest downhill grade of the design plane, percent (m/m × 100).</summary>
            public double CompositeSlopePercent { get; init; }
        }

        /// <summary>
        /// Loads <see cref="FieldDesign.SurveyFileName"/> via <see cref="Survey.Load"/> (.txt multiplane, .ags),
        /// bins at <see cref="Field.BIN_SIZE_M"/>, applies the planar design (same ratio solve as <c>AI Field Design/fielddesign.py</c>),
        /// writes <paramref name="databaseOutputPath"/>, and returns <see cref="Statistics"/> for the UI.
        /// </summary>
        public static Statistics CreateFromSurveyAndDesign
            (
            FieldDesign design,
            string databaseOutputPath
            )
        {
            if (design == null)
                throw new ArgumentNullException(nameof(design));
            if (string.IsNullOrWhiteSpace(databaseOutputPath))
                throw new ArgumentException("Database output path is required.", nameof(databaseOutputPath));

            string path = design.SurveyFileName;
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("FieldDesign.SurveyFileName is required.", nameof(design));

            if (!File.Exists(path))
                throw new FileNotFoundException("Survey file was not found.", path);

            double ratio = design.CutFillRatio > 0 ? design.CutFillRatio : Field.CUT_FILL_RATIO;
            double importM3 = CubicYardsToM3(design.ImportToField);
            double exportM3 = CubicYardsToM3(design.ExportFromField);

            var survey = new Survey();
            survey.Load(path);

            if (survey.InteriorPoints.Count == 0 && survey.BoundaryPoints.Count == 0)
                throw new InvalidDataException("Survey has no interior or boundary topology points.");

            BuildGrid(
                survey,
                design.MainSlopeDirection,
                design.MainSlope,
                design.CrossSlope,
                ratio,
                importM3,
                exportM3,
                out int gridW,
                out int gridH,
                out double minX,
                out double minY,
                out double maxX,
                out double maxY,
                out int utmZone,
                out bool utmNorth,
                out double meanLat,
                out double meanLon,
                out double totalCutCy,
                out double totalFillCy,
                out double planeA,
                out double planeB,
                out List<Database.BinState> binRows);

            CompositeSlopeAndHeading(planeA, planeB, out double compositeSlopePct, out double compositeHeadingDeg);
            double totalAreaM2 = gridW * gridH * Field.BIN_SIZE_M * Field.BIN_SIZE_M;
            double totalAreaAcres = totalAreaM2 / SquareMetersPerAcre;
            var statistics = new Statistics
            {
                TotalAreaAcres = totalAreaAcres,
                TotalCutCY = totalCutCy,
                TotalFillCY = totalFillCy,
                CompositeSlopeHeadingDeg = compositeHeadingDeg,
                CompositeSlopePercent = compositeSlopePct
            };

            UTM.ToLatLon(utmZone, utmNorth, minX, minY, out double dataMinLat, out double dataMinLon);
            UTM.ToLatLon(utmZone, utmNorth, maxX, maxY, out double dataMaxLat, out double dataMaxLon);

            var db = new Database();
            db.CreateEmptyFieldDatabase(databaseOutputPath);

            try
            {
                db.BulkInsertFieldStateRows(binRows);

                db.SetData(Database.DataNames.GridWidth, gridW);
                db.SetData(Database.DataNames.GridHeight, gridH);
                db.SetData(Database.DataNames.MeanLat, meanLat);
                db.SetData(Database.DataNames.MeanLon, meanLon);
                db.SetData(Database.DataNames.MinX, minX);
                db.SetData(Database.DataNames.MinY, minY);
                db.SetData(Database.DataNames.MaxX, maxX);
                db.SetData(Database.DataNames.MaxY, maxY);
                db.SetData(Database.DataNames.MinLat, dataMinLat);
                db.SetData(Database.DataNames.MinLon, dataMinLon);
                db.SetData(Database.DataNames.MaxLat, dataMaxLat);
                db.SetData(Database.DataNames.MaxLon, dataMaxLon);
                db.SetData(Database.DataNames.CompletedCutCY, 0);
                db.SetData(Database.DataNames.CompletedFillCY, 0);
                db.SetData(Database.DataNames.TotalCutCY, totalCutCy);
                db.SetData(Database.DataNames.TotalFillCY, totalFillCy);
                db.SetBoolData(Database.DataNames.Calibrated, false);
                db.SetData(Database.DataNames.HeightOffsetM, 0);
                db.SetData(Database.DataNames.EastingOffsetM, 0);
                db.SetData(Database.DataNames.NorthingOffsetM, 0);

                foreach (Benchmark bm in survey.Benchmarks)
                {
                    if (bm?.Location == null) continue;
                    db.InsertBenchmarkRow(bm.Location.Latitude, bm.Location.Longitude, bm.Name, bm.Elevation);
                }
            }
            finally
            {
                db.Close();
            }

            return statistics;
        }

        /// <summary>Matches <c>fielddesign.composite_slope_and_heading</c>.</summary>
        private static void CompositeSlopeAndHeading(double a, double b, out double slopePercent, out double downhillHeadingDegCwFromNorth)
        {
            double mag = Math.Sqrt(a * a + b * b);
            slopePercent = mag * 100.0;
            double de = -a;
            double dn = -b;
            if (Math.Abs(de) < 1e-18 && Math.Abs(dn) < 1e-18)
            {
                downhillHeadingDegCwFromNorth = 0.0;
                return;
            }

            double h = Math.Atan2(de, dn) * (180.0 / Math.PI);
            downhillHeadingDegCwFromNorth = (h % 360.0 + 360.0) % 360.0;
        }

        private static double CubicYardsToM3(double cubicYards)
        {
            return cubicYards / CubicYardsPerCubicMeter;
        }

        private static void ComputePlaneSlopes(double mainSlopePct, double crossSlopePct, double headingDeg, out double a, out double b)
        {
            double H = headingDeg * Math.PI / 180.0;
            double sM = mainSlopePct / 100.0;
            double sC = crossSlopePct / 100.0;
            double sinH = Math.Sin(H);
            double cosH = Math.Cos(H);
            a = -sM * sinH + sC * cosH;
            b = -sM * cosH - sC * sinH;
        }

        private static void VolumesF(
            IReadOnlyList<BinSample> bins,
            double a,
            double b,
            double c,
            double rTarget,
            double e0,
            double n0,
            double importM3,
            double exportM3,
            out double vCut,
            out double vFill,
            out double f)
        {
            vCut = 0;
            vFill = 0;
            double binArea = Field.BIN_SIZE_M * Field.BIN_SIZE_M;
            foreach (BinSample ob in bins)
            {
                if (ob.ZInit == Field.BIN_NO_DATA_SENTINEL)
                    continue;
                double zf = a * (ob.EastingC - e0) + b * (ob.NorthingC - n0) + c;
                double d = ob.ZInit - zf;
                if (d > 0)
                    vCut += d * binArea;
                else
                    vFill += (-d) * binArea;
            }

            f = (vCut + importM3) - rTarget * vFill - exportM3;
        }

        private static double SolveC(
            IReadOnlyList<BinSample> bins,
            double a,
            double b,
            double rTarget,
            double e0,
            double n0,
            double importM3,
            double exportM3)
        {
            double F(double cc)
            {
                VolumesF(bins, a, b, cc, rTarget, e0, n0, importM3, exportM3, out _, out _, out double fn);
                return fn;
            }

            double lo = 0, hi = 0, flo = 0, fhi = 0;
            bool bracketed = false;
            foreach ((double aLo, double aHi) in new[] { (0.0, 50.0), (0.0, 100.0), (50.0, 100.0) })
            {
                lo = aLo;
                hi = aHi;
                flo = F(lo);
                fhi = F(hi);
                if (flo * fhi < 0)
                {
                    bracketed = true;
                    break;
                }
            }

            if (!bracketed)
                throw new InvalidOperationException("Could not bracket a root for the design plane offset c; try different slopes or import/export.");

            for (int i = 0; i < 100; i++)
            {
                double mid = 0.5 * (lo + hi);
                double fm = F(mid);
                if (Math.Abs(fm) < 1e-3)
                    return mid;
                if (Math.Abs(hi - lo) < 1e-10)
                    return mid;
                if (fm * flo < 0)
                {
                    hi = mid;
                    fhi = fm;
                }
                else
                {
                    lo = mid;
                    flo = fm;
                }
            }

            return 0.5 * (lo + hi);
        }

        /// <summary>
        /// Matches <c>fieldcreator.py</c> <c>fill_missing_bins</c>: a missing bin is filled only if it has at least two
        /// neighbors in the original sparse data; value is the mean of cardinal neighbors present in <paramref name="outDict"/>.
        /// </summary>
        private static Dictionary<(int Bx, int By), double> FillMissingBins(
            Dictionary<(int Bx, int By), double> values,
            int gridWidth,
            int gridHeight)
        {
            var original = new HashSet<(int, int)>();
            foreach ((int bx, int by) in values.Keys)
                original.Add((bx, by));

            var outDict = new Dictionary<(int, int), double>();
            foreach (KeyValuePair<(int Bx, int By), double> kv in values)
                outDict[kv.Key] = kv.Value;

            if (original.Count == 0)
                return outDict;

            int maxBinX = gridWidth - 1;
            int maxBinY = gridHeight - 1;

            for (int by = 0; by <= maxBinY; by++)
            {
                for (int bx = 0; bx <= maxBinX; bx++)
                {
                    if (original.Contains((bx, by)))
                        continue;

                    int origNeighbors = 0;
                    if (bx >= 0 && bx <= maxBinX && by - 1 >= 0 && by - 1 <= maxBinY && original.Contains((bx, by - 1))) origNeighbors++;
                    if (bx >= 0 && bx <= maxBinX && by + 1 >= 0 && by + 1 <= maxBinY && original.Contains((bx, by + 1))) origNeighbors++;
                    if (bx - 1 >= 0 && bx - 1 <= maxBinX && by >= 0 && by <= maxBinY && original.Contains((bx - 1, by))) origNeighbors++;
                    if (bx + 1 >= 0 && bx + 1 <= maxBinX && by >= 0 && by <= maxBinY && original.Contains((bx + 1, by))) origNeighbors++;

                    if (origNeighbors < 2)
                        continue;

                    double sumNv = 0;
                    int countNv = 0;
                    if (bx >= 0 && bx <= maxBinX && by - 1 >= 0 && by - 1 <= maxBinY && outDict.TryGetValue((bx, by - 1), out double v0))
                    {
                        sumNv += v0;
                        countNv++;
                    }

                    if (bx >= 0 && bx <= maxBinX && by + 1 >= 0 && by + 1 <= maxBinY && outDict.TryGetValue((bx, by + 1), out double v1))
                    {
                        sumNv += v1;
                        countNv++;
                    }

                    if (bx - 1 >= 0 && bx - 1 <= maxBinX && by >= 0 && by <= maxBinY && outDict.TryGetValue((bx - 1, by), out double v2))
                    {
                        sumNv += v2;
                        countNv++;
                    }

                    if (bx + 1 >= 0 && bx + 1 <= maxBinX && by >= 0 && by <= maxBinY && outDict.TryGetValue((bx + 1, by), out double v3))
                    {
                        sumNv += v3;
                        countNv++;
                    }

                    if (countNv > 0)
                        outDict[(bx, by)] = sumNv / countNv;
                }
            }

            return outDict;
        }

        private static Dictionary<(int Ix, int Iy), List<int>> BuildSpatialHash(double[] pe, double[] pn, int n)
        {
            var d = new Dictionary<(int Ix, int Iy), List<int>>();
            for (int i = 0; i < n; i++)
            {
                int ix = (int)Math.Floor(pe[i] / SpatialHashCellM);
                int iy = (int)Math.Floor(pn[i] / SpatialHashCellM);
                var key = (ix, iy);
                if (!d.TryGetValue(key, out List<int>? list))
                {
                    list = new List<int>();
                    d[key] = list;
                }

                list.Add(i);
            }

            return d;
        }

        private static void GatherKNearest(
            double cx,
            double cy,
            double[] pe,
            double[] pn,
            Dictionary<(int Ix, int Iy), List<int>> buck,
            int maxRing,
            int kTarget,
            double[] kd2,
            int[] kidx)
        {
            int nPts = pe.Length;
            for (int s = 0; s < kTarget; s++)
                kd2[s] = double.PositiveInfinity;

            int qx = (int)Math.Floor(cx / SpatialHashCellM);
            int qy = (int)Math.Floor(cy / SpatialHashCellM);

            for (int r = 0; r < maxRing; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (r > 0 && Math.Abs(dx) < r && Math.Abs(dy) < r)
                            continue;
                        if (!buck.TryGetValue((qx + dx, qy + dy), out List<int>? list))
                            continue;
                        foreach (int idx in list)
                            ConsiderCandidate(cx, cy, pe, pn, idx, kTarget, kd2, kidx);
                    }
                }

                if (CountFilled(kd2, kTarget) >= kTarget)
                {
                    double worstSq = WorstFilledD2(kd2, kTarget);
                    if ((r + 1) * SpatialHashCellM > Math.Sqrt(worstSq) + 1e-6)
                        break;
                }
            }

            if (CountFilled(kd2, kTarget) < kTarget)
            {
                for (int i = 0; i < nPts; i++)
                    ConsiderCandidate(cx, cy, pe, pn, i, kTarget, kd2, kidx);
            }
        }

        private static void ConsiderCandidate(
            double cx,
            double cy,
            double[] pe,
            double[] pn,
            int idx,
            int kTarget,
            double[] kd2,
            int[] kidx)
        {
            double tdx = cx - pe[idx];
            double tdy = cy - pn[idx];
            double d2 = tdx * tdx + tdy * tdy;
            int slot = -1;
            for (int s = 0; s < kTarget; s++)
            {
                if (kd2[s] >= double.PositiveInfinity)
                {
                    slot = s;
                    break;
                }
            }

            if (slot >= 0)
            {
                kd2[slot] = d2;
                kidx[slot] = idx;
                return;
            }

            int worst = 0;
            double worstD = kd2[0];
            for (int s = 1; s < kTarget; s++)
            {
                if (kd2[s] > worstD)
                {
                    worstD = kd2[s];
                    worst = s;
                }
            }

            if (d2 < worstD)
            {
                kd2[worst] = d2;
                kidx[worst] = idx;
            }
        }

        private static int CountFilled(double[] kd2, int kTarget)
        {
            int n = 0;
            for (int s = 0; s < kTarget; s++)
            {
                if (kd2[s] < double.PositiveInfinity)
                    n++;
            }

            return n;
        }

        private static double WorstFilledD2(double[] kd2, int kTarget)
        {
            double w = 0;
            for (int s = 0; s < kTarget; s++)
            {
                if (kd2[s] < double.PositiveInfinity && kd2[s] > w)
                    w = kd2[s];
            }

            return w;
        }

        private static bool TrySolve3x3(
            double a00,
            double a01,
            double a02,
            double a11,
            double a12,
            double a22,
            double b0,
            double b1,
            double b2,
            out double x0,
            out double x1,
            out double x2)
        {
            var m = new double[3, 3];
            var v = new double[3];
            m[0, 0] = a00; m[0, 1] = a01; m[0, 2] = a02;
            m[1, 0] = a01; m[1, 1] = a11; m[1, 2] = a12;
            m[2, 0] = a02; m[2, 1] = a12; m[2, 2] = a22;
            v[0] = b0; v[1] = b1; v[2] = b2;

            for (int c = 0; c < 3; c++)
            {
                int p = c;
                double best = Math.Abs(m[c, c]);
                for (int r = c + 1; r < 3; r++)
                {
                    double av = Math.Abs(m[r, c]);
                    if (av > best)
                    {
                        best = av;
                        p = r;
                    }
                }

                if (best <= 1e-20)
                {
                    x0 = x1 = x2 = double.NaN;
                    return false;
                }

                if (p != c)
                {
                    for (int k = c; k < 3; k++)
                    {
                        double tmp = m[c, k];
                        m[c, k] = m[p, k];
                        m[p, k] = tmp;
                    }

                    double tv = v[c];
                    v[c] = v[p];
                    v[p] = tv;
                }

                double piv = m[c, c];
                for (int k = c; k < 3; k++)
                    m[c, k] /= piv;
                v[c] /= piv;

                for (int r = 0; r < 3; r++)
                {
                    if (r == c)
                        continue;
                    double f = m[r, c];
                    if (Math.Abs(f) <= 0)
                        continue;
                    for (int k = c; k < 3; k++)
                        m[r, k] -= f * m[c, k];
                    v[r] -= f * v[c];
                }
            }

            x0 = v[0];
            x1 = v[1];
            x2 = v[2];
            return double.IsFinite(x0) && double.IsFinite(x1) && double.IsFinite(x2);
        }

        private static double[] BoxBlur1dAxis(double[] arr, int width, int height, int radius, bool axisX)
        {
            if (radius <= 0)
            {
                var clone = new double[arr.Length];
                Array.Copy(arr, clone, arr.Length);
                return clone;
            }

            int window = 2 * radius + 1;
            var outArr = new double[arr.Length];
            if (axisX)
            {
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        double sum = 0;
                        for (int k = -radius; k <= radius; k++)
                        {
                            int xx = x + k;
                            if (xx < 0) xx = 0;
                            else if (xx >= width) xx = width - 1;
                            sum += arr[row + xx];
                        }

                        outArr[row + x] = sum / window;
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double sum = 0;
                        for (int k = -radius; k <= radius; k++)
                        {
                            int yy = y + k;
                            if (yy < 0) yy = 0;
                            else if (yy >= height) yy = height - 1;
                            sum += arr[yy * width + x];
                        }

                        outArr[y * width + x] = sum / window;
                    }
                }
            }

            return outArr;
        }

        private static double[] BoxBlur2d(double[] arr, int width, int height, int radius, int passes)
        {
            var outArr = new double[arr.Length];
            Array.Copy(arr, outArr, arr.Length);
            if (radius <= 0 || passes <= 0)
                return outArr;

            for (int p = 0; p < passes; p++)
            {
                outArr = BoxBlur1dAxis(outArr, width, height, radius, axisX: true);
                outArr = BoxBlur1dAxis(outArr, width, height, radius, axisX: false);
            }

            return outArr;
        }

        private static double[] NormalizedResidualWarp(
            double[] residualAtObs,
            bool[] observedMask,
            int width,
            int height,
            int radiusBins,
            int passes)
        {
            var maskedResidual = new double[residualAtObs.Length];
            var maskAsDouble = new double[residualAtObs.Length];
            for (int i = 0; i < residualAtObs.Length; i++)
            {
                if (observedMask[i])
                {
                    maskedResidual[i] = residualAtObs[i];
                    maskAsDouble[i] = 1.0;
                }
            }

            double[] num = BoxBlur2d(maskedResidual, width, height, radiusBins, passes);
            double[] den = BoxBlur2d(maskAsDouble, width, height, radiusBins, passes);
            var corr = new double[residualAtObs.Length];
            for (int i = 0; i < corr.Length; i++)
                corr[i] = num[i] / Math.Max(den[i], 1e-12);
            return corr;
        }

        private static double[] BuildFielddesignSurface(
            List<(double E, double N, double Z, bool IsBoundary, double Lat, double Lon)> utmPoints,
            double minX,
            double minY,
            int gridW,
            int gridH)
        {
            int nPts = utmPoints.Count;
            var pe = new double[nPts];
            var pn = new double[nPts];
            var pz = new double[nPts];
            for (int i = 0; i < nPts; i++)
            {
                pe[i] = utmPoints[i].E;
                pn[i] = utmPoints[i].N;
                pz[i] = utmPoints[i].Z;
            }

            var grouped = new Dictionary<(int Bx, int By), (double SumZ, int Count)>();
            foreach (var t in utmPoints)
            {
                int bx = (int)Math.Floor((t.E - minX) / Field.BIN_SIZE_M);
                int by = (int)Math.Floor((t.N - minY) / Field.BIN_SIZE_M);
                if (bx < 0 || bx >= gridW || by < 0 || by >= gridH)
                    continue;
                var key = (bx, by);
                if (grouped.TryGetValue(key, out var agg))
                    grouped[key] = (agg.SumZ + t.Z, agg.Count + 1);
                else
                    grouped[key] = (t.Z, 1);
            }

            int nCells = gridW * gridH;
            var zMls = new double[nCells];
            var observedMask = new bool[nCells];
            var observedValues = new double[nCells];

            Dictionary<(int Ix, int Iy), List<int>> spatial = BuildSpatialHash(pe, pn, nPts);
            int maxRing = (int)Math.Ceiling(Math.Max(
                    Math.Max(1.0, gridW * Field.BIN_SIZE_M),
                    Math.Max(1.0, gridH * Field.BIN_SIZE_M)
                ) / SpatialHashCellM) + 3;
            int kUse = Math.Min(EmptyBinMlsK, nPts);
            var kd2 = new double[kUse];
            var kidx = new int[kUse];

            for (int by = 0; by < gridH; by++)
            {
                for (int bx = 0; bx < gridW; bx++)
                {
                    double cx = minX + (bx + 0.5) * Field.BIN_SIZE_M;
                    double cy = minY + (by + 0.5) * Field.BIN_SIZE_M;
                    GatherKNearest(cx, cy, pe, pn, spatial, maxRing, kUse, kd2, kidx);

                    double r2k = 0;
                    for (int s = 0; s < kUse; s++)
                    {
                        if (kd2[s] < double.PositiveInfinity && kd2[s] > r2k)
                            r2k = kd2[s];
                    }

                    double smooth2 = EmptyBinMlsSmoothFrac * EmptyBinMlsSmoothFrac * r2k;
                    double a00 = 0, a01 = 0, a02 = 0, a11 = 0, a12 = 0, a22 = 0;
                    double b0 = 0, b1 = 0, b2 = 0;
                    double wSum = 0, wzSum = 0;
                    for (int s = 0; s < kUse; s++)
                    {
                        if (kd2[s] >= double.PositiveInfinity)
                            continue;
                        int idx = kidx[s];
                        double dx = pe[idx] - cx;
                        double dy = pn[idx] - cy;
                        double z = pz[idx];
                        double w = 1.0 / (kd2[s] + smooth2 + EmptyBinInterpEpsSq);
                        a00 += w * dx * dx;
                        a01 += w * dx * dy;
                        a02 += w * dx;
                        a11 += w * dy * dy;
                        a12 += w * dy;
                        a22 += w;
                        b0 += w * dx * z;
                        b1 += w * dy * z;
                        b2 += w * z;
                        wSum += w;
                        wzSum += w * z;
                    }

                    double trace = (a00 + a11 + a22) / 3.0;
                    double lam = Math.Max(EmptyBinInterpEpsSq, EmptyBinMlsRegScale * trace);
                    bool solved = TrySolve3x3(
                        a00 + lam, a01, a02,
                        a11 + lam, a12,
                        a22 + lam,
                        b0, b1, b2,
                        out _, out _, out double c0);
                    if (!solved || !double.IsFinite(c0))
                        c0 = wSum > 0 ? wzSum / wSum : pz[0];

                    int gi = by * gridW + bx;
                    zMls[gi] = c0;

                    if (grouped.TryGetValue((bx, by), out var g))
                    {
                        observedMask[gi] = true;
                        observedValues[gi] = g.SumZ / g.Count;
                    }
                }
            }

            double[] zSmooth = BoxBlur2d(zMls, gridW, gridH, WarpBaseRadiusBins, WarpBasePasses);
            var residualObs = new double[nCells];
            for (int i = 0; i < nCells; i++)
            {
                if (observedMask[i])
                    residualObs[i] = observedValues[i] - zSmooth[i];
            }

            double[] corr = NormalizedResidualWarp(residualObs, observedMask, gridW, gridH, WarpRadiusBins, WarpPasses);
            var zFinal = new double[nCells];
            for (int i = 0; i < nCells; i++)
                zFinal[i] = zSmooth[i] + WarpAlpha * corr[i];

            var err = new double[nCells];
            var excess = new double[nCells];
            var need = new bool[nCells];
            for (int it = 0; it < WarpTolIters; it++)
            {
                bool anyNeed = false;
                for (int i = 0; i < nCells; i++)
                {
                    err[i] = 0;
                    excess[i] = 0;
                    need[i] = false;
                    if (!observedMask[i])
                        continue;
                    err[i] = observedValues[i] - zFinal[i];
                    double absErr = Math.Abs(err[i]);
                    if (absErr > WarpTolM)
                    {
                        need[i] = true;
                        anyNeed = true;
                        excess[i] = Math.Sign(err[i]) * (absErr - WarpTolM);
                    }
                }

                if (!anyNeed)
                    break;
                double[] corrEx = NormalizedResidualWarp(excess, need, gridW, gridH, WarpTolRadiusBins, WarpTolPasses);
                for (int i = 0; i < nCells; i++)
                    zFinal[i] += WarpTolAlpha * corrEx[i];
            }

            if (WarpPostRadiusBins > 0 && WarpPostPasses > 0)
            {
                zFinal = BoxBlur2d(zFinal, gridW, gridH, WarpPostRadiusBins, WarpPostPasses);
                for (int it = 0; it < WarpPostTolIters; it++)
                {
                    bool anyNeed = false;
                    for (int i = 0; i < nCells; i++)
                    {
                        err[i] = 0;
                        excess[i] = 0;
                        need[i] = false;
                        if (!observedMask[i])
                            continue;
                        err[i] = observedValues[i] - zFinal[i];
                        double absErr = Math.Abs(err[i]);
                        if (absErr > WarpTolM)
                        {
                            need[i] = true;
                            anyNeed = true;
                            excess[i] = Math.Sign(err[i]) * (absErr - WarpTolM);
                        }
                    }

                    if (!anyNeed)
                        break;
                    double[] corrEx = NormalizedResidualWarp(excess, need, gridW, gridH, WarpTolRadiusBins, WarpTolPasses);
                    for (int i = 0; i < nCells; i++)
                        zFinal[i] += WarpTolAlpha * corrEx[i];
                }
            }

            for (int i = 0; i < nCells; i++)
            {
                if (!observedMask[i])
                    continue;
                double eo = observedValues[i] - zFinal[i];
                if (eo < -WarpTolM) eo = -WarpTolM;
                else if (eo > WarpTolM) eo = WarpTolM;
                zFinal[i] = observedValues[i] - eo;
            }

            return zFinal;
        }

        private static void BuildGrid(
            Survey survey,
            int headingDeg,
            double mainSlopePct,
            double crossSlopePct,
            double cutFillRatio,
            double importM3,
            double exportM3,
            out int gridW,
            out int gridH,
            out double minX,
            out double minY,
            out double maxX,
            out double maxY,
            out int utmZone,
            out bool utmNorth,
            out double meanLat,
            out double meanLon,
            out double totalCutCy,
            out double totalFillCy,
            out double planeA,
            out double planeB,
            out List<Database.BinState> binRows)
        {
            planeA = 0;
            planeB = 0;

            var utmPoints = new List<(double E, double N, double Z, bool IsBoundary, double Lat, double Lon)>();
            double sumLat = 0, sumLon = 0;
            int nTopo = 0;

            foreach (TopologyPoint p in survey.InteriorPoints)
            {
                UTM.UTMCoordinate c = UTM.FromLatLon(p.Latitude, p.Longitude);
                utmPoints.Add((c.Easting, c.Northing, p.ExistingElevation, false, p.Latitude, p.Longitude));
                sumLat += p.Latitude;
                sumLon += p.Longitude;
                nTopo++;
            }

            foreach (TopologyPoint p in survey.BoundaryPoints)
            {
                UTM.UTMCoordinate c = UTM.FromLatLon(p.Latitude, p.Longitude);
                utmPoints.Add((c.Easting, c.Northing, p.ExistingElevation, true, p.Latitude, p.Longitude));
                sumLat += p.Latitude;
                sumLon += p.Longitude;
                nTopo++;
            }

            meanLat = sumLat / nTopo;
            meanLon = sumLon / nTopo;

            double minLatTopo = double.MaxValue;
            double minLonTopo = double.MaxValue;
            foreach (TopologyPoint p in survey.InteriorPoints)
            {
                if (p.Latitude < minLatTopo) minLatTopo = p.Latitude;
                if (p.Longitude < minLonTopo) minLonTopo = p.Longitude;
            }

            foreach (TopologyPoint p in survey.BoundaryPoints)
            {
                if (p.Latitude < minLatTopo) minLatTopo = p.Latitude;
                if (p.Longitude < minLonTopo) minLonTopo = p.Longitude;
            }

            UTM.UTMCoordinate sw = UTM.FromLatLon(minLatTopo, minLonTopo);
            utmZone = sw.Zone;
            utmNorth = sw.IsNorthernHemisphere;

            minX = double.MaxValue;
            minY = double.MaxValue;
            maxX = double.MinValue;
            maxY = double.MinValue;

            foreach (var t in utmPoints)
            {
                UTM.UTMCoordinate c = UTM.FromLatLon(t.Lat, t.Lon);
                if (c.Zone != utmZone || c.IsNorthernHemisphere != utmNorth)
                    throw new InvalidDataException("Field crosses UTM zone boundaries (not supported).");

                if (t.E < minX) minX = t.E;
                if (t.E > maxX) maxX = t.E;
                if (t.N < minY) minY = t.N;
                if (t.N > maxY) maxY = t.N;
            }

            gridW = (int)Math.Ceiling((maxX - minX) / Field.BIN_SIZE_M);
            gridH = (int)Math.Ceiling((maxY - minY) / Field.BIN_SIZE_M);
            if (gridW <= 0 || gridH <= 0)
                throw new InvalidDataException("Computed non-positive grid dimensions from survey extent.");

            double e0 = minX;
            double n0 = minY;

            double[] filledZ = BuildFielddesignSurface(utmPoints, minX, minY, gridW, gridH);

            var samples = new List<BinSample>(gridW * gridH);
            for (int by = 0; by < gridH; by++)
            {
                for (int bx = 0; bx < gridW; bx++)
                {
                    double cx = minX + (bx + 0.5) * Field.BIN_SIZE_M;
                    double cy = minY + (by + 0.5) * Field.BIN_SIZE_M;
                    UTM.ToLatLon(utmZone, utmNorth, cx, cy, out double cLat, out double cLon);

                    double zInit = filledZ[by * gridW + bx];

                    samples.Add(new BinSample(bx, by, zInit, cx, cy, cLat, cLon));
                }
            }

            ComputePlaneSlopes(mainSlopePct, crossSlopePct, headingDeg, out double a, out double b);
            planeA = a;
            planeB = b;
            double cStar = SolveC(samples, a, b, cutFillRatio, e0, n0, importM3, exportM3);

            double binArea = Field.BIN_SIZE_M * Field.BIN_SIZE_M;
            double vCutM3 = 0;
            double vFillM3 = 0;
            foreach (BinSample s in samples)
            {
                double zf = a * (s.EastingC - e0) + b * (s.NorthingC - n0) + cStar;
                s.TargetZ = zf;
                double d = s.ZInit - zf;
                if (d > 0)
                    vCutM3 += d * binArea;
                else if (d < 0)
                    vFillM3 += (-d) * binArea;
            }

            totalCutCy = vCutM3 * CubicYardsPerCubicMeter;
            totalFillCy = vFillM3 * CubicYardsPerCubicMeter;

            binRows = new List<Database.BinState>(samples.Count);
            foreach (BinSample s in samples)
            {
                binRows.Add(new Database.BinState(
                    s.Bx,
                    s.By,
                    s.ZInit,
                    s.ZInit,
                    s.TargetZ,
                    s.CentroidLat,
                    s.CentroidLon,
                    0));
            }
        }

        private sealed class BinSample
        {
            public int Bx;
            public int By;
            public double ZInit;
            public double EastingC;
            public double NorthingC;
            public double CentroidLat;
            public double CentroidLon;
            public double TargetZ;

            public BinSample(int bx, int by, double zInit, double eastingC, double northingC, double centroidLat, double centroidLon)
            {
                Bx = bx;
                By = by;
                ZInit = zInit;
                EastingC = eastingC;
                NorthingC = northingC;
                CentroidLat = centroidLat;
                CentroidLon = centroidLon;
            }
        }
    }
}
