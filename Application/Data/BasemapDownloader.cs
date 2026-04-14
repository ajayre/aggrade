using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    /// <summary>
    /// Downloads and stores satellite basemap tiles in app-wide cache for offline use.
    /// </summary>
    public sealed class BasemapDownloader
    {
        /// <summary>International acre in square meters (exact definition).</summary>
        public const double SquareMetersPerAcre = 4046.8564224;

        /// <summary>Default field size when downloading from a centroid (survey flow).</summary>
        public const double CentroidDownloadAcres = 40.0;

        /// <summary>
        /// Extra padding around the centroid field square: half of the field lon span added west and east,
        /// and half of the field lat span added north and south (total footprint 2× field width × 2× field height in each axis).
        /// </summary>
        public const double CentroidPreviewEdgeMarginFraction = 0.5;

        private const int MinZoom = 14; // top 9 usable zoom levels in app (14..22)
        private const int MaxZoom = 22;
        // For the worst case where the field edge is centered on screen, prefetch at least
        // half a viewport worth of tiles on each side. 256 px per WebMercator tile.
        // 881x550 viewport -> ceil(881/(2*256))=2, ceil(550/(2*256))=2, then +1 safety.
        private const int TileMarginX = 3;
        private const int TileMarginY = 3;
        private const double MaxWebMercatorLat = 85.05112878;
        private const int MaxConcurrentRequests = 8;

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public sealed class DownloadSummary
        {
            public int RequestedTiles { get; init; }
            public int DownloadedTiles { get; init; }
            public int SkippedTiles { get; init; }
            public int FailedTiles { get; init; }
            public string BasemapFolder { get; init; } = string.Empty;
            public int MinZoom { get; init; }
            public int MaxZoom { get; init; }
        }

        private readonly record struct TileAddress(int Zoom, int X, int Y);
        /// <summary>
        /// Optional callback invoked with completion percentage in range 0..100.
        /// </summary>
        public Action<double>? OnProgressChanged { get; set; }

        /// <summary>
        /// Square extent (axis-aligned N-S / E-W) centered on <paramref name="centroidLatitudeDeg"/>,
        /// <paramref name="centroidLongitudeDeg"/> with total area <paramref name="acres"/>.
        /// </summary>
        public static (double minLat, double minLon, double maxLat, double maxLon) GetSquareExtentsFromAcres(
            double centroidLatitudeDeg,
            double centroidLongitudeDeg,
            double acres)
        {
            if (acres <= 0 || !double.IsFinite(acres))
            {
                throw new ArgumentOutOfRangeException(nameof(acres));
            }
            if (!double.IsFinite(centroidLatitudeDeg) || !double.IsFinite(centroidLongitudeDeg))
            {
                throw new ArgumentException("Centroid must be finite.", nameof(centroidLatitudeDeg));
            }

            double areaM2 = acres * SquareMetersPerAcre;
            double sideM = Math.Sqrt(areaM2);
            double halfM = sideM / 2.0;

            double latRad = centroidLatitudeDeg * (Math.PI / 180.0);
            const double metersPerDegreeLatitude = 111132.0;
            double metersPerDegreeLongitude = metersPerDegreeLatitude * Math.Cos(latRad);
            if (metersPerDegreeLongitude < 1e-3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(centroidLatitudeDeg),
                    "Latitude is too close to a pole to form a square extent.");
            }

            double dLat = halfM / metersPerDegreeLatitude;
            double dLon = halfM / metersPerDegreeLongitude;

            double minLat = centroidLatitudeDeg - dLat;
            double maxLat = centroidLatitudeDeg + dLat;
            double minLon = centroidLongitudeDeg - dLon;
            double maxLon = centroidLongitudeDeg + dLon;

            minLat = Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, minLat));
            maxLat = Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, maxLat));
            return (minLat, minLon, maxLat, maxLon);
        }

        /// <summary>
        /// Expands axis-aligned extents by <see cref="CentroidPreviewEdgeMarginFraction"/> times the lon span on the left
        /// and right, and the same fraction times the lat span on the top and bottom.
        /// </summary>
        public static (double minLat, double minLon, double maxLat, double maxLon) ExpandExtentsByCentroidPreviewMargin(
            double minLat,
            double minLon,
            double maxLat,
            double maxLon)
        {
            double lonSpan = maxLon - minLon;
            double latSpan = maxLat - minLat;
            double m = CentroidPreviewEdgeMarginFraction;
            minLon -= m * lonSpan;
            maxLon += m * lonSpan;
            minLat -= m * latSpan;
            maxLat += m * latSpan;
            minLat = Math.Max(-MaxWebMercatorLat, minLat);
            maxLat = Math.Min(MaxWebMercatorLat, maxLat);
            return (minLat, minLon, maxLat, maxLon);
        }

        /// <summary>
        /// Geographic bounds for map preview: the 40-acre centroid square plus <see cref="ExpandExtentsByCentroidPreviewMargin"/>,
        /// then snapped to the <see cref="MaxZoom"/> tile grid with the same margins as <see cref="BuildTileList"/> at that zoom.
        /// </summary>
        public static (double minLat, double minLon, double maxLat, double maxLon) GetCentroidDownloadTileCoverageBounds(
            double centroidLatitudeDeg,
            double centroidLongitudeDeg)
        {
            (double minLat, double minLon, double maxLat, double maxLon) =
                GetSquareExtentsFromAcres(centroidLatitudeDeg, centroidLongitudeDeg, CentroidDownloadAcres);
            (minLat, minLon, maxLat, maxLon) = ExpandExtentsByCentroidPreviewMargin(minLat, minLon, maxLat, maxLon);
            return GetTileDownloadGeographicEnvelope(minLat, minLon, maxLat, maxLon, MaxZoom);
        }

        /// <summary>
        /// Geographic envelope of tiles at <paramref name="zoom"/> selected like <see cref="BuildTileList"/> for the given field extents.
        /// Use <see cref="MaxZoom"/> for a tight preview around the field; lower zoom spans much larger areas per tile.
        /// </summary>
        public static (double minLat, double minLon, double maxLat, double maxLon) GetTileDownloadGeographicEnvelope(
            double minLat,
            double minLon,
            double maxLat,
            double maxLon,
            int zoom)
        {
            double clampedMinLat = Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, minLat));
            double clampedMaxLat = Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, maxLat));
            double westLon = Math.Min(minLon, maxLon);
            double eastLon = Math.Max(minLon, maxLon);
            double southLat = Math.Min(clampedMinLat, clampedMaxLat);
            double northLat = Math.Max(clampedMinLat, clampedMaxLat);

            double envMinLat = double.PositiveInfinity;
            double envMaxLat = double.NegativeInfinity;
            double envMinLon = double.PositiveInfinity;
            double envMaxLon = double.NegativeInfinity;

            (int minX, int minY) = LonLatToTile(westLon, northLat, zoom);
            (int maxX, int maxY) = LonLatToTile(eastLon, southLat, zoom);

            int tilesPerAxis = 1 << zoom;
            minX = Math.Max(0, Math.Min(tilesPerAxis - 1, minX - TileMarginX));
            maxX = Math.Max(0, Math.Min(tilesPerAxis - 1, maxX + TileMarginX));
            minY = Math.Max(0, Math.Min(tilesPerAxis - 1, minY - TileMarginY));
            maxY = Math.Max(0, Math.Min(tilesPerAxis - 1, maxY + TileMarginY));

            for (int ty = minY; ty <= maxY; ty++)
            {
                for (int tx = minX; tx <= maxX; tx++)
                {
                    TileGeographicBounds(zoom, tx, ty, out double w, out double e, out double n, out double s);
                    envMinLon = Math.Min(envMinLon, w);
                    envMaxLon = Math.Max(envMaxLon, e);
                    envMinLat = Math.Min(envMinLat, s);
                    envMaxLat = Math.Max(envMaxLat, n);
                }
            }

            return (envMinLat, envMinLon, envMaxLat, envMaxLon);
        }

        /// <summary>Web Mercator tile edges in degrees (north edge &gt; south edge latitude).</summary>
        private static void TileGeographicBounds(int zoom, int tileX, int tileY, out double west, out double east, out double north, out double south)
        {
            double n = Math.Pow(2.0, zoom);
            west = tileX / n * 360.0 - 180.0;
            east = (tileX + 1) / n * 360.0 - 180.0;
            north = TileYToLatitudeDeg(tileY, zoom);
            south = TileYToLatitudeDeg(tileY + 1, zoom);
        }

        private static double TileYToLatitudeDeg(int tileY, int zoom)
        {
            double n = Math.Pow(2.0, zoom);
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * tileY / n)));
            return latRad * 180.0 / Math.PI;
        }

        public async Task<DownloadSummary> DownloadAsync
            (
            string basemapDataFolder,
            string databaseFile,
            CancellationToken cancellationToken = default
            )
        {
            if (string.IsNullOrWhiteSpace(basemapDataFolder))
            {
                throw new ArgumentException("Basemap data folder is required.", nameof(basemapDataFolder));
            }
            if (string.IsNullOrWhiteSpace(databaseFile))
            {
                throw new ArgumentException("Database file is required.", nameof(databaseFile));
            }
            if (!File.Exists(databaseFile))
            {
                throw new FileNotFoundException("Database file not found.", databaseFile);
            }

            (double minLat, double minLon, double maxLat, double maxLon) = ReadFieldExtents(databaseFile);
            return await DownloadFromExtentsAsync(
                basemapDataFolder,
                minLat,
                minLon,
                maxLat,
                maxLon,
                Path.GetFileName(databaseFile),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Downloads tiles covering a square field of <see cref="CentroidDownloadAcres"/> centered on the centroid.
        /// </summary>
        public Task<DownloadSummary> DownloadAsync(
            string basemapDataFolder,
            double centroidLatitude,
            double centroidLongitude,
            CancellationToken cancellationToken = default)
        {
            return DownloadAsync(
                basemapDataFolder,
                centroidLatitude,
                centroidLongitude,
                CentroidDownloadAcres,
                cancellationToken);
        }

        /// <summary>
        /// Downloads tiles covering a square field of <paramref name="fieldAcres"/> centered on the centroid.
        /// </summary>
        public async Task<DownloadSummary> DownloadAsync(
            string basemapDataFolder,
            double centroidLatitude,
            double centroidLongitude,
            double fieldAcres,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(basemapDataFolder))
            {
                throw new ArgumentException("Basemap data folder is required.", nameof(basemapDataFolder));
            }

            (double minLat, double minLon, double maxLat, double maxLon) =
                GetSquareExtentsFromAcres(centroidLatitude, centroidLongitude, fieldAcres);

            if (fieldAcres == CentroidDownloadAcres)
            {
                (minLat, minLon, maxLat, maxLon) = ExpandExtentsByCentroidPreviewMargin(minLat, minLon, maxLat, maxLon);
            }

            return await DownloadFromExtentsAsync(
                basemapDataFolder,
                minLat,
                minLon,
                maxLat,
                maxLon,
                fieldDatabaseForManifest: null,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<DownloadSummary> DownloadFromExtentsAsync(
            string basemapDataFolder,
            double minLat,
            double minLon,
            double maxLat,
            double maxLon,
            string? fieldDatabaseForManifest,
            CancellationToken cancellationToken)
        {
            List<TileAddress> tiles = BuildTileList(minLat, minLon, maxLat, maxLon);

            string basemapFolder = basemapDataFolder;
            string providerFolder = Path.Combine(basemapDataFolder, "SatelliteEsri");
            Directory.CreateDirectory(providerFolder);

            int downloaded = 0;
            int skipped = 0;
            int failed = 0;
            int processed = 0;

            ReportProgress(0);

            using SemaphoreSlim gate = new SemaphoreSlim(MaxConcurrentRequests);
            List<Task> work = new List<Task>(tiles.Count);

            foreach (TileAddress tile in tiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

                work.Add(DownloadOneTileAsync(tile));
            }

            async Task DownloadOneTileAsync(TileAddress tile)
            {
                try
                {
                    string tilePath = GetTileFilePath(providerFolder, tile.Zoom, tile.X, tile.Y);
                    string tempFile = tilePath + ".tmp";
                    if (File.Exists(tempFile))
                    {
                        // Clean up a previous interrupted write so resume can proceed.
                        try { File.Delete(tempFile); } catch { }
                    }

                    if (File.Exists(tilePath))
                    {
                        try
                        {
                            FileInfo info = new FileInfo(tilePath);
                            if (info.Length > 0)
                            {
                                Interlocked.Increment(ref skipped);
                                return;
                            }
                        }
                        catch
                        {
                            // If we cannot inspect the existing file, treat it as bad and re-download.
                        }
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(tilePath)!);
                    byte[]? payload = await DownloadTileAsync(tile.Zoom, tile.X, tile.Y, cancellationToken).ConfigureAwait(false);
                    if (payload == null || payload.Length == 0)
                    {
                        Interlocked.Increment(ref failed);
                        return;
                    }

                    await File.WriteAllBytesAsync(tempFile, payload, cancellationToken).ConfigureAwait(false);
                    File.Move(tempFile, tilePath, true);
                    Interlocked.Increment(ref downloaded);
                }
                catch
                {
                    Interlocked.Increment(ref failed);
                }
                finally
                {
                    int done = Interlocked.Increment(ref processed);
                    ReportProgress(done * 100.0 / tiles.Count);
                    gate.Release();
                }
            }

            await Task.WhenAll(work).ConfigureAwait(false);
            ReportProgress(100);

            await WriteManifestAsync(
                basemapFolder,
                fieldDatabaseForManifest,
                minLat,
                minLon,
                maxLat,
                maxLon,
                tiles.Count,
                downloaded,
                skipped,
                failed,
                cancellationToken).ConfigureAwait(false);

            return new DownloadSummary
            {
                RequestedTiles = tiles.Count,
                DownloadedTiles = downloaded,
                SkippedTiles = skipped,
                FailedTiles = failed,
                BasemapFolder = basemapFolder,
                MinZoom = MinZoom,
                MaxZoom = MaxZoom
            };
        }

        private void ReportProgress(double percent)
        {
            int rounded = (int)Math.Round(percent);
            rounded = Math.Max(0, Math.Min(100, rounded));
            int previous = Volatile.Read(ref _lastPercentSnapshot);
            if (rounded == 100 || rounded > previous)
            {
                Interlocked.Exchange(ref _lastPercentSnapshot, rounded);
                try
                {
                    OnProgressChanged?.Invoke(rounded);
                }
                catch
                {
                    // Progress callback issues must never stop downloads.
                }
            }
        }

        private int _lastPercentSnapshot = -1;

        private static (double minLat, double minLon, double maxLat, double maxLon) ReadFieldExtents(string databaseFile)
        {
            Database db = new Database();
            db.Open(databaseFile);
            try
            {
                double minLat = db.GetData(Database.DataNames.MinLat);
                double minLon = db.GetData(Database.DataNames.MinLon);
                double maxLat = db.GetData(Database.DataNames.MaxLat);
                double maxLon = db.GetData(Database.DataNames.MaxLon);
                return (minLat, minLon, maxLat, maxLon);
            }
            finally
            {
                db.Close();
            }
        }

        private static List<TileAddress> BuildTileList(double minLat, double minLon, double maxLat, double maxLon)
        {
            double clampedMinLat = Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, minLat));
            double clampedMaxLat = Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, maxLat));
            double westLon = Math.Min(minLon, maxLon);
            double eastLon = Math.Max(minLon, maxLon);
            double southLat = Math.Min(clampedMinLat, clampedMaxLat);
            double northLat = Math.Max(clampedMinLat, clampedMaxLat);

            List<TileAddress> tiles = new List<TileAddress>();
            for (int zoom = MinZoom; zoom <= MaxZoom; zoom++)
            {
                (int minX, int minY) = LonLatToTile(westLon, northLat, zoom);
                (int maxX, int maxY) = LonLatToTile(eastLon, southLat, zoom);

                int tilesPerAxis = 1 << zoom;
                minX = Math.Max(0, Math.Min(tilesPerAxis - 1, minX - TileMarginX));
                maxX = Math.Max(0, Math.Min(tilesPerAxis - 1, maxX + TileMarginX));
                minY = Math.Max(0, Math.Min(tilesPerAxis - 1, minY - TileMarginY));
                maxY = Math.Max(0, Math.Min(tilesPerAxis - 1, maxY + TileMarginY));

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        tiles.Add(new TileAddress(zoom, x, y));
                    }
                }
            }

            return tiles;
        }

        private static async Task<byte[]?> DownloadTileAsync(int zoom, int x, int y, CancellationToken cancellationToken)
        {
            string url = $"https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{zoom}/{y}/{x}";
            using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("AgGrade/1.0");

            using HttpResponseMessage res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }
            return await res.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }

        private static string GetTileFilePath(string providerFolder, int zoom, int x, int y)
        {
            return Path.Combine(providerFolder, zoom.ToString(), x.ToString(), $"{y}.tile");
        }

        private static async Task WriteManifestAsync(
            string basemapFolder,
            string? fieldDatabaseFileName,
            double minLat,
            double minLon,
            double maxLat,
            double maxLon,
            int requested,
            int downloaded,
            int skipped,
            int failed,
            CancellationToken cancellationToken)
        {
            string fieldDbLabel = string.IsNullOrEmpty(fieldDatabaseFileName) ? "(centroid)" : fieldDatabaseFileName;
            var manifest = new
            {
                Provider = "SatelliteEsri",
                ZoomMin = MinZoom,
                ZoomMax = MaxZoom,
                TileMarginX = TileMarginX,
                TileMarginY = TileMarginY,
                FieldDatabase = fieldDbLabel,
                Extents = new { MinLat = minLat, MinLon = minLon, MaxLat = maxLat, MaxLon = maxLon },
                RequestedTiles = requested,
                DownloadedTiles = downloaded,
                SkippedTiles = skipped,
                FailedTiles = failed,
                GeneratedUtc = DateTime.UtcNow
            };

            string manifestPath = Path.Combine(basemapFolder, "manifest.json");
            string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(manifestPath, json, cancellationToken).ConfigureAwait(false);
        }

        private static (int x, int y) LonLatToTile(double lon, double lat, int zoom)
        {
            lat = Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, lat));
            double n = Math.Pow(2.0, zoom);
            int x = (int)Math.Floor((lon + 180.0) / 360.0 * n);
            double latRad = lat * Math.PI / 180.0;
            int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n);
            return (x, y);
        }
    }
}
