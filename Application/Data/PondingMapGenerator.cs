using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace AgGrade.Data
{
    /// <summary>
    /// Generates ponding map from field bin elevation using SCS Curve Number runoff,
    /// D8 flow routing, and depression fill/spill (matches AI Flow Analysis/ponding.py).
    /// </summary>
    public class PondingMapGenerator
    {
        private static readonly (int dr, int dc)[] Neigh8 = new[]
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1),           (0, 1),
            (1, -1),  (1, 0),  (1, 1)
        };

        /// <summary>
        /// Builds the elevation grid from field bins (via Field.BuildElevationGrid)
        /// and runs the ponding model. Writes PNG to <paramref name="outputPngPath"/>.
        /// </summary>
        /// <param name="field">Field with bins</param>
        /// <param name="elevationType">Initial, Current, or Target elevation</param>
        /// <param name="outputPngPath">Path for output PNG</param>
        /// <param name="SWCorner">South-west corner (lat/lon)</param>
        /// <param name="NECorner">North-east corner (lat/lon)</param>
        /// <param name="rainfallMm">Event rainfall depth in mm</param>
        /// <param name="curveNumber">SCS Curve Number (e.g. 78, 85)</param>
        /// <param name="opacity">PNG transparency: 0=fully opaque, 100=white fully transparent</param>
        /// <param name="enableSmoothing">Apply Gaussian smoothing to DEM</param>
        public void GeneratePondingPNG(
            Field field,
            FlowMapGenerator.ElevationTypes elevationType,
            string outputPngPath,
            out Coordinate SWCorner,
            out Coordinate NECorner,
            double rainfallMm = 50,
            double curveNumber = 85,
            int opacity = 50,
            bool enableSmoothing = true)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (string.IsNullOrWhiteSpace(outputPngPath))
                throw new ArgumentException("Output PNG path is required.", nameof(outputPngPath));
            if (field.Bins == null || field.Bins.Count == 0)
                throw new InvalidOperationException("Field has no bins.");

            field.BuildElevationGrid(elevationType, enableSmoothing,
                out float[,] dem, out int nrows, out int ncols, out SWCorner, out NECorner);

            double cellSizeM = Field.BIN_SIZE_M;
            RunPondingModel(dem, nrows, ncols, cellSizeM, Field.ElevationDemNoDataValue, rainfallMm, curveNumber,
                out float[,] pondDepth, out byte[,] gray);

            ConvertPondingToPNG(gray, nrows, ncols, outputPngPath, true, opacity);
        }

        /// <summary>
        /// Runs the ponding model (SCS runoff, D8 routing, fill-spill). Outputs pond depth and grayscale (black = deeper).
        /// </summary>
        private static void RunPondingModel(
            float[,] dem,
            int nrows,
            int ncols,
            double cellSizeM,
            float nodataValue,
            double rainfallMm,
            double curveNumber,
            out float[,] pondDepth,
            out byte[,] gray)
        {
            int size = nrows * ncols;
            bool[] valid = new bool[size];
            double[] demFlat = new double[size];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                {
                    int i = r * ncols + c;
                    float v = dem[r, c];
                    valid[i] = v != nodataValue && !float.IsNaN(v) && !float.IsInfinity(v);
                    demFlat[i] = valid[i] ? v : 0;
                }

            double cellArea = cellSizeM * cellSizeM;
            double dx = cellSizeM, dy = cellSizeM;

            // Depth in sink from priority flood
            double[] depthInSink = ComputeDepthInSinkFromDem(demFlat, valid, nrows, ncols);

            // D8
            D8Result d8 = ComputeD8Receiver(demFlat, valid, nrows, ncols, dx, dy);

            // SCS runoff -> volume per cell
            double cn = Math.Clamp(curveNumber, 1.0, 100.0);
            double S = 25400.0 / cn - 254.0;
            double Ia = 0.2 * S;
            double P = rainfallMm;
            double runoffMm = P <= Ia ? 0 : ((P - Ia) * (P - Ia)) / (P - Ia + S);
            double runoffDepthM = runoffMm / 1000.0;
            double[] runoffVolumePerCell = new double[size];
            for (int i = 0; i < size; i++)
                runoffVolumePerCell[i] = valid[i] ? runoffDepthM * cellArea : 0;

            RouteRunoffToTerminals(runoffVolumePerCell, d8, out double[] accumulated, out long[] terminalOfCell);

            // Label sink regions (8-connected where depth_in_sink > 0)
            int[] labels = LabelSinkRegions(depthInSink, valid, nrows, ncols);

            // Terminal -> sink id
            int[] terminalSinkId = new int[size];
            for (int i = 0; i < size; i++)
            {
                long t = terminalOfCell[i];
                terminalSinkId[i] = (t >= 0 && t < size) ? labels[t] : 0;
            }

            var incomingVolume = new Dictionary<int, double>();
            for (int i = 0; i < size; i++)
            {
                if (!valid[i]) continue;
                int sid = terminalSinkId[i];
                if (sid > 0)
                {
                    if (!incomingVolume.ContainsKey(sid)) incomingVolume[sid] = 0;
                    incomingVolume[sid] += runoffVolumePerCell[i];
                }
            }

            var sinkInfos = BuildSinkInfos(demFlat, depthInSink, labels, valid, terminalOfCell, nrows, ncols, cellArea);
            var orderedSinkIds = sinkInfos.Keys.OrderBy(sid => sinkInfos[sid].SpillElev).ToList();
            var storedVolume = new Dictionary<int, double>();
            var overflowVolume = new Dictionary<int, double>();
            foreach (int sid in sinkInfos.Keys)
            {
                storedVolume[sid] = 0;
                overflowVolume[sid] = 0;
            }

            foreach (int sid in orderedSinkIds)
            {
                var info = sinkInfos[sid];
                double volIn = (incomingVolume.TryGetValue(sid, out double inc) ? inc : 0) + (overflowVolume[sid]);
                double cap = info.CapacityVolume;
                if (volIn <= cap)
                    storedVolume[sid] = volIn;
                else
                {
                    storedVolume[sid] = cap;
                    double excess = volIn - cap;
                    int? dsid = info.DownstreamSinkId;
                    if (dsid.HasValue && sinkInfos.ContainsKey(dsid.Value))
                        overflowVolume[dsid.Value] += excess;
                }
            }

            pondDepth = new float[nrows, ncols];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                    pondDepth[r, c] = float.NaN;

            foreach (var kv in sinkInfos)
            {
                int sid = kv.Key;
                var info = kv.Value;
                double vol = storedVolume[sid];
                double[] capDepth = new double[info.CellsFlat.Length];
                for (int i = 0; i < capDepth.Length; i++)
                    capDepth[i] = depthInSink[info.CellsFlat[i]];
                double[] realized = VolumeToDepths(capDepth, vol, cellArea);
                for (int i = 0; i < info.CellsFlat.Length; i++)
                {
                    int flat = info.CellsFlat[i];
                    int r = flat / ncols, c = flat % ncols;
                    pondDepth[r, c] = (float)realized[i];
                }
            }

            double maxd = 0;
            for (int i = 0; i < size; i++)
                if (valid[i] && !float.IsNaN(pondDepth[i / ncols, i % ncols]))
                    maxd = Math.Max(maxd, pondDepth[i / ncols, i % ncols]);

            gray = new byte[nrows, ncols];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                {
                    int i = r * ncols + c;
                    if (!valid[i]) { gray[r, c] = 255; continue; }
                    float d = pondDepth[r, c];
                    if (float.IsNaN(d) || d <= 0) gray[r, c] = 255;
                    else if (maxd > 0) gray[r, c] = (byte)Math.Clamp(255.0 * (1.0 - d / maxd), 0, 255);
                    else gray[r, c] = 255;
                }
        }

        private static bool InBounds(int r, int c, int nrows, int ncols) =>
            r >= 0 && r < nrows && c >= 0 && c < ncols;

        private static double[] ComputeDepthInSinkFromDem(double[] dem, bool[] valid, int nrows, int ncols)
        {
            double[] filled = PriorityFloodFill(dem, valid, nrows, ncols);
            double[] depth = new double[dem.Length];
            for (int i = 0; i < depth.Length; i++)
                depth[i] = valid[i] ? Math.Max(filled[i] - dem[i], 0) : 0;
            return depth;
        }

        private static double[] PriorityFloodFill(double[] dem, bool[] valid, int nrows, int ncols)
        {
            double[] filled = (double[])dem.Clone();
            bool[] visited = new bool[dem.Length];
            var pq = new PriorityQueue<(int r, int c), double>();

            for (int r = 0; r < nrows; r++)
                foreach (int c in new[] { 0, ncols - 1 })
                {
                    int i = r * ncols + c;
                    if (valid[i] && !visited[i]) { pq.Enqueue((r, c), filled[i]); visited[i] = true; }
                }
            for (int c = 0; c < ncols; c++)
                foreach (int r in new[] { 0, nrows - 1 })
                {
                    int i = r * ncols + c;
                    if (valid[i] && !visited[i]) { pq.Enqueue((r, c), filled[i]); visited[i] = true; }
                }

            while (pq.Count > 0)
            {
                var (r, c) = pq.Dequeue();
                double elev = filled[r * ncols + c];
                foreach (var (dr, dc) in Neigh8)
                {
                    int rr = r + dr, cc = c + dc;
                    if (!InBounds(rr, cc, nrows, ncols)) continue;
                    int idx = rr * ncols + cc;
                    if (!valid[idx] || visited[idx]) continue;
                    visited[idx] = true;
                    double ne = filled[idx];
                    if (ne < elev) { filled[idx] = elev; ne = elev; }
                    pq.Enqueue((rr, cc), ne);
                }
            }
            return filled;
        }

        private class D8Result
        {
            public long[] Receiver;
            public int[] DonorCount;
            public List<long> Order;
        }

        private static D8Result ComputeD8Receiver(double[] dem, bool[] valid, int nrows, int ncols, double dx, double dy)
        {
            int size = nrows * ncols;
            long[] receiver = new long[size];
            int[] donorCount = new int[size];
            for (int i = 0; i < size; i++) receiver[i] = -1;

            double diag = Math.Sqrt(dx * dx + dy * dy);
            var dist = new Dictionary<(int, int), double>
            {
                [(-1, -1)] = diag, [(-1, 0)] = dy, [(-1, 1)] = diag,
                [(0, -1)] = dx, [(0, 1)] = dx,
                [(1, -1)] = diag, [(1, 0)] = dy, [(1, 1)] = diag
            };

            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                {
                    int cur = r * ncols + c;
                    if (!valid[cur]) continue;
                    double z = dem[cur];
                    double bestSlope = 0;
                    long bestIdx = -1;
                    foreach (var (dr, dc) in Neigh8)
                    {
                        int rr = r + dr, cc = c + dc;
                        if (!InBounds(rr, cc, nrows, ncols) || !valid[rr * ncols + cc]) continue;
                        long idx = rr * ncols + cc;
                        double dz = z - dem[idx];
                        double s = dz / dist[(dr, dc)];
                        if (s > bestSlope) { bestSlope = s; bestIdx = idx; }
                    }
                    receiver[cur] = bestIdx;
                    if (bestIdx >= 0) donorCount[bestIdx]++;
                }

            var donorWork = (int[])donorCount.Clone();
            var order = new List<long>();
            var q = new Queue<long>();
            for (long u = 0; u < size; u++)
                if (valid[u] && donorWork[u] == 0) q.Enqueue(u);
            while (q.Count > 0)
            {
                long u = q.Dequeue();
                order.Add(u);
                long v = receiver[u];
                if (v >= 0 && --donorWork[v] == 0) q.Enqueue(v);
            }
            return new D8Result { Receiver = receiver, DonorCount = donorCount, Order = order };
        }

        private static void RouteRunoffToTerminals(double[] runoffVolumePerCell, D8Result d8,
            out double[] accumulated, out long[] terminalOfCell)
        {
            int size = runoffVolumePerCell.Length;
            accumulated = (double[])runoffVolumePerCell.Clone();
            terminalOfCell = new long[size];
            for (int i = 0; i < size; i++) terminalOfCell[i] = -1;

            foreach (long u in d8.Order)
            {
                long v = d8.Receiver[u];
                if (v >= 0) accumulated[v] += accumulated[u];
            }

            for (long u = 0; u < size; u++)
            {
                if (terminalOfCell[u] >= 0) continue;
                long cur = u;
                var path = new List<long>();
                long t = -1;
                while (cur >= 0 && terminalOfCell[cur] < 0)
                {
                    path.Add(cur);
                    long nxt = d8.Receiver[cur];
                    if (nxt < 0) { t = cur; break; }
                    cur = nxt;
                }
                if (t < 0 && cur >= 0) t = terminalOfCell[cur];
                foreach (long p in path) terminalOfCell[p] = t;
            }
        }

        private static int[] LabelSinkRegions(double[] depthInSink, bool[] valid, int nrows, int ncols)
        {
            int size = nrows * ncols;
            int[] labels = new int[size];
            int nextLabel = 1;
            var stack = new Stack<int>();
            for (int start = 0; start < size; start++)
            {
                if (labels[start] != 0 || !valid[start] || depthInSink[start] <= 0) continue;
                int label = nextLabel++;
                labels[start] = label;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    int r = i / ncols, c = i % ncols;
                    foreach (var (dr, dc) in Neigh8)
                    {
                        int rr = r + dr, cc = c + dc;
                        if (!InBounds(rr, cc, nrows, ncols)) continue;
                        int j = rr * ncols + cc;
                        if (labels[j] != 0 || !valid[j] || depthInSink[j] <= 0) continue;
                        labels[j] = label;
                        stack.Push(j);
                    }
                }
            }
            return labels;
        }

        private class SinkInfo
        {
            public int SinkId;
            public int[] CellsFlat;
            public double SpillElev;
            public double CapacityVolume;
            public int? DownstreamSinkId;
        }

        private static double ComputeSinkSpillElevation(double[] dem, int[] labels, int sinkId, bool[] valid, int nrows, int ncols)
        {
            double spill = double.PositiveInfinity;
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                {
                    int i = r * ncols + c;
                    if (labels[i] != sinkId) continue;
                    foreach (var (dr, dc) in Neigh8)
                    {
                        int rr = r + dr, cc = c + dc;
                        if (!InBounds(rr, cc, nrows, ncols)) { spill = Math.Min(spill, dem[i]); continue; }
                        int j = rr * ncols + cc;
                        if (!valid[j]) { spill = Math.Min(spill, dem[i]); continue; }
                        if (labels[j] != sinkId) spill = Math.Min(spill, Math.Max(dem[i], dem[j]));
                    }
                }
            return double.IsInfinity(spill) ? 0 : spill;
        }

        private static Dictionary<int, SinkInfo> BuildSinkInfos(double[] dem, double[] depthInSink, int[] labels, bool[] valid,
            long[] terminalOfCell, int nrows, int ncols, double cellArea)
        {
            var infos = new Dictionary<int, SinkInfo>();
            var uniqueIds = labels.Where(l => l > 0).Distinct().OrderBy(x => x).ToList();
            foreach (int sid in uniqueIds)
            {
                var cellsFlat = new List<int>();
                double capSum = 0;
                for (int i = 0; i < labels.Length; i++)
                    if (labels[i] == sid) { cellsFlat.Add(i); capSum += depthInSink[i] * cellArea; }
                double spillElev = ComputeSinkSpillElevation(dem, labels, sid, valid, nrows, ncols);
                infos[sid] = new SinkInfo
                {
                    SinkId = sid,
                    CellsFlat = cellsFlat.ToArray(),
                    SpillElev = spillElev,
                    CapacityVolume = capSum,
                    DownstreamSinkId = null
                };
            }

            foreach (int sid in uniqueIds)
            {
                double bestEscape = double.PositiveInfinity;
                int? bestPair = null;
                for (int r = 0; r < nrows; r++)
                    for (int c = 0; c < ncols; c++)
                    {
                        int i = r * ncols + c;
                        if (labels[i] != sid) continue;
                        foreach (var (dr, dc) in Neigh8)
                        {
                            int rr = r + dr, cc = c + dc;
                            if (!InBounds(rr, cc, nrows, ncols))
                            {
                                if (dem[i] < bestEscape) { bestEscape = dem[i]; bestPair = null; }
                                continue;
                            }
                            int j = rr * ncols + cc;
                            if (!valid[j]) { if (dem[i] < bestEscape) { bestEscape = dem[i]; bestPair = null; } continue; }
                            int other = labels[j];
                            if (other != sid)
                            {
                                double escape = Math.Max(dem[i], dem[j]);
                                if (escape < bestEscape) { bestEscape = escape; bestPair = other > 0 ? other : (int?)null; }
                            }
                        }
                    }
                infos[sid].DownstreamSinkId = bestPair;
            }
            return infos;
        }

        private static double[] VolumeToDepths(double[] depthCapacity, double targetVolume, double cellArea)
        {
            if (targetVolume <= 0) return new double[depthCapacity.Length];
            double totalCap = depthCapacity.Sum() * cellArea;
            if (targetVolume >= totalCap) return (double[])depthCapacity.Clone();
            double lo = 0, hi = depthCapacity.Max();
            for (int iter = 0; iter < 60; iter++)
            {
                double mid = 0.5 * (lo + hi);
                double vol = depthCapacity.Sum(d => Math.Max(d - mid, 0)) * cellArea;
                if (vol > targetVolume) lo = mid; else hi = mid;
            }
            double y = hi;
            return depthCapacity.Select(d => Math.Max(d - y, 0)).ToArray();
        }

        /// <summary>
        /// Writes ponding grayscale to PNG (black = deeper). Optional transparency.
        /// </summary>
        private static void ConvertPondingToPNG(byte[,] gray, int nrows, int ncols, string pngPath, bool transparent, int opacity)
        {
            int size = nrows * ncols;
            byte[] greyFlat = new byte[size];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                    greyFlat[r * ncols + c] = gray[r, c];

            int opacityPct = Math.Clamp(opacity, 0, 100);
            if (transparent && opacityPct > 0)
            {
                byte[] bgra = new byte[size * 4];
                for (int i = 0; i < size; i++)
                {
                    byte g = greyFlat[i];
                    bgra[i * 4 + 0] = bgra[i * 4 + 1] = bgra[i * 4 + 2] = g;
                    bgra[i * 4 + 3] = (byte)(255 - (g * opacityPct) / 100);
                }
                using (var mat = new Mat(nrows, ncols, MatType.CV_8UC4))
                {
                    Marshal.Copy(bgra, 0, mat.Data, bgra.Length);
                    if (!Cv2.ImWrite(pngPath, mat))
                        throw new InvalidOperationException($"Failed to write PNG: {pngPath}");
                }
            }
            else
            {
                using (var mat = new Mat(nrows, ncols, MatType.CV_8UC1))
                {
                    Marshal.Copy(greyFlat, 0, mat.Data, greyFlat.Length);
                    if (!Cv2.ImWrite(pngPath, mat))
                        throw new InvalidOperationException($"Failed to write PNG: {pngPath}");
                }
            }
        }
    }
}
