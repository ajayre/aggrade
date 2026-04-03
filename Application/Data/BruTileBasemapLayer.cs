using BruTile.Predefined;
using AgGrade.Controller;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    /// <summary>
    /// Add-on basemap renderer that draws cached BruTile tiles under the existing map overlays.
    /// Designed to fail silently when offline and automatically recover once connectivity returns.
    /// </summary>
    public sealed class BruTileBasemapLayer : IDisposable
    {
        public enum BasemapProviders
        {
            OpenStreetMap,
            SatelliteEsri
        }

        private readonly record struct TileKey(BasemapProviders Provider, int Zoom, int X, int Y);

        private sealed class CacheEntry
        {
            public required Bitmap Bitmap { get; init; }
            public DateTime LastAccessUtc { get; set; }
        }

        private const int MaxCacheTiles = 512;
        private const int MaxInflightRequests = 12;
        private const int MinZoom = 0;
        private const int MaxZoomSatellite = 22;
        private const int MaxZoomOpenStreetMap = 19;
        private const double MaxWebMercatorLat = 85.05112878;

        private readonly BruTileZoomAdapter _zoomAdapter = new BruTileZoomAdapter();
        private readonly ConcurrentDictionary<TileKey, CacheEntry> _tileCache = new();
        private readonly ConcurrentDictionary<TileKey, byte> _inflight = new();
        private readonly HttpClient _httpClient;
        private readonly object _stateLock = new();
        private readonly ImageAttributes _tileImageAttributes = new ImageAttributes();

        private bool _offlineMode;
        private int _failureCount;
        private DateTime _nextRetryUtc = DateTime.MinValue;
        private bool _disposed;

        public BruTileBasemapLayer()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(4)
            };
            _tileImageAttributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);

            /*float alpha = Math.Clamp(0.3f, 0f, 1f);
            var matrix = new ColorMatrix
            {
                Matrix33 = alpha // overall image opacity
            };
            _tileImageAttributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);*/

            try
            {
                // Keep an explicit BruTile dependency in the add-on path.
                _ = KnownTileSources.Create();
            }
            catch
            {
            }
        }

        public void Render(
            Graphics graphics,
            int imageWidthPx,
            int imageHeightPx,
            GNSSFix tractorFix,
            int tractorXpx,
            int tractorYpx,
            double tractorHeadingDeg,
            double pixelsPerMeter,
            BasemapProviders provider,
            Func<Coordinate, PointF> latLonToPixel,
            string? basemapFolderPath)
        {
            if (_disposed || !tractorFix.IsValid || imageWidthPx <= 0 || imageHeightPx <= 0 || pixelsPerMeter <= 0.0)
            {
                return;
            }

            int providerMaxZoom = provider == BasemapProviders.OpenStreetMap ? MaxZoomOpenStreetMap : MaxZoomSatellite;
            int zoom = _zoomAdapter.SelectZoomLevel(pixelsPerMeter, tractorFix.Latitude, MinZoom, providerMaxZoom);
            List<TileKey> visibleTiles = GetVisibleTiles(
                imageWidthPx,
                imageHeightPx,
                tractorFix,
                tractorXpx,
                tractorYpx,
                tractorHeadingDeg,
                pixelsPerMeter,
                provider,
                zoom);

            bool canRequest = ShouldAttemptRequests();
            int inflightCount = _inflight.Count;

            foreach (TileKey key in visibleTiles)
            {
                if (_tileCache.TryGetValue(key, out CacheEntry? entry))
                {
                    entry.LastAccessUtc = DateTime.UtcNow;
                    DrawTile(graphics, entry.Bitmap, key.Zoom, key.X, key.Y, latLonToPixel);
                    continue;
                }

                if (TryLoadTileFromDisk(key, basemapFolderPath, out CacheEntry? diskEntry))
                {
                    _tileCache[key] = diskEntry;
                    DrawTile(graphics, diskEntry.Bitmap, key.Zoom, key.X, key.Y, latLonToPixel);
                    continue;
                }

                if (canRequest && inflightCount < MaxInflightRequests && !_inflight.ContainsKey(key))
                {
                    if (_inflight.TryAdd(key, 0))
                    {
                        inflightCount++;
                        _ = FetchTileAsync(key, basemapFolderPath);
                    }
                }
            }

            TrimCacheIfNeeded();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (CacheEntry entry in _tileCache.Values)
            {
                entry.Bitmap.Dispose();
            }
            _tileCache.Clear();
            _inflight.Clear();
            _tileImageAttributes.Dispose();
            _httpClient.Dispose();
        }

        private bool ShouldAttemptRequests()
        {
            lock (_stateLock)
            {
                if (!_offlineMode)
                {
                    return true;
                }

                return DateTime.UtcNow >= _nextRetryUtc;
            }
        }

        private async Task FetchTileAsync(TileKey key, string? basemapFolderPath)
        {
            try
            {
                if (TryLoadTileFromDisk(key, basemapFolderPath, out CacheEntry? diskEntry))
                {
                    _tileCache[key] = diskEntry;
                    RegisterSuccess();
                    return;
                }

                byte[]? tileBytes = await DownloadTileAsync(key).ConfigureAwait(false);
                if (tileBytes == null || tileBytes.Length == 0)
                {
                    RegisterFailure();
                    return;
                }

                using MemoryStream ms = new MemoryStream(tileBytes);
                using Bitmap decoded = new Bitmap(ms);
                CacheEntry newEntry = new CacheEntry
                {
                    Bitmap = new Bitmap(decoded),
                    LastAccessUtc = DateTime.UtcNow
                };
                _tileCache[key] = newEntry;
                SaveTileToDisk(tileBytes, key, basemapFolderPath);
                RegisterSuccess();
            }
            catch
            {
                // Silent fail-soft behavior by design.
                RegisterFailure();
            }
            finally
            {
                _inflight.TryRemove(key, out _);
            }
        }

        private async Task<byte[]?> DownloadTileAsync(TileKey key)
        {
            string? directUrl = BuildTileUrl(key.Provider, key.Zoom, key.X, key.Y);
            if (string.IsNullOrWhiteSpace(directUrl))
            {
                return null;
            }

            using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, directUrl);
            req.Headers.UserAgent.ParseAdd("AgGrade/1.0");
            using HttpResponseMessage res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }

            return await res.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        private static string BuildTileUrl(BasemapProviders provider, int zoom, int x, int y)
        {
            if (provider == BasemapProviders.OpenStreetMap)
            {
                return $"https://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{zoom}/{y}/{x}";
            }

            return $"https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{zoom}/{y}/{x}";
        }

        private static string ProviderFolderName(BasemapProviders provider)
        {
            return provider == BasemapProviders.OpenStreetMap ? "OpenStreetMap" : "SatelliteEsri";
        }

        private static string? BuildDiskTilePath(TileKey key, string? basemapFolderPath)
        {
            if (string.IsNullOrWhiteSpace(basemapFolderPath))
            {
                return null;
            }

            string providerFolder = Path.Combine(basemapFolderPath, ProviderFolderName(key.Provider));
            return Path.Combine(providerFolder, key.Zoom.ToString(), key.X.ToString(), $"{key.Y}.tile");
        }

        private static bool TryLoadTileFromDisk(TileKey key, string? basemapFolderPath, out CacheEntry? entry)
        {
            entry = null;
            string? tilePath = BuildDiskTilePath(key, basemapFolderPath);
            if (string.IsNullOrWhiteSpace(tilePath) || !File.Exists(tilePath))
            {
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(tilePath);
                if (bytes.Length == 0)
                {
                    return false;
                }

                using MemoryStream ms = new MemoryStream(bytes);
                using Bitmap decoded = new Bitmap(ms);
                entry = new CacheEntry
                {
                    Bitmap = new Bitmap(decoded),
                    LastAccessUtc = DateTime.UtcNow
                };
                return true;
            }
            catch
            {
                // Ignore corrupt/unreadable files and continue fail-soft.
                return false;
            }
        }

        private static void SaveTileToDisk(byte[] tileBytes, TileKey key, string? basemapFolderPath)
        {
            string? tilePath = BuildDiskTilePath(key, basemapFolderPath);
            if (string.IsNullOrWhiteSpace(tilePath))
            {
                return;
            }

            try
            {
                string? parent = Path.GetDirectoryName(tilePath);
                if (string.IsNullOrWhiteSpace(parent))
                {
                    return;
                }

                Directory.CreateDirectory(parent);
                string tempFile = tilePath + ".tmp";
                File.WriteAllBytes(tempFile, tileBytes);
                File.Move(tempFile, tilePath, true);
            }
            catch
            {
                // Disk persistence must never break rendering.
            }
        }

        private void RegisterSuccess()
        {
            lock (_stateLock)
            {
                _offlineMode = false;
                _failureCount = 0;
                _nextRetryUtc = DateTime.MinValue;
            }
        }

        private void RegisterFailure()
        {
            lock (_stateLock)
            {
                _offlineMode = true;
                _failureCount = Math.Min(_failureCount + 1, 8);
                int delaySeconds = Math.Min(30, 1 << _failureCount);
                _nextRetryUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
            }
        }

        private void DrawTile(Graphics g, Bitmap tileBitmap, int zoom, int tileX, int tileY, Func<Coordinate, PointF> latLonToPixel)
        {
            // Convert WebMercator tile bounds to geographic coordinates.
            double n = Math.Pow(2.0, zoom);
            double lonLeft = tileX / n * 360.0 - 180.0;
            double lonRight = (tileX + 1) / n * 360.0 - 180.0;
            double latTop = RadiansToDegrees(Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * tileY / n))));
            double latBottom = RadiansToDegrees(Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * (tileY + 1) / n))));

            PointF topLeft = latLonToPixel(new Coordinate(latTop, lonLeft));
            PointF topRight = latLonToPixel(new Coordinate(latTop, lonRight));
            PointF bottomLeft = latLonToPixel(new Coordinate(latBottom, lonLeft));

            PointF[] destParallelogram = new PointF[] { topLeft, topRight, bottomLeft };
            Rectangle srcRect = new Rectangle(0, 0, tileBitmap.Width, tileBitmap.Height);
            g.DrawImage(tileBitmap, destParallelogram, srcRect, GraphicsUnit.Pixel, _tileImageAttributes);
        }

        private List<TileKey> GetVisibleTiles(
            int imageWidthPx,
            int imageHeightPx,
            GNSSFix tractorFix,
            int tractorXpx,
            int tractorYpx,
            double tractorHeadingDeg,
            double pixelsPerMeter,
            BasemapProviders provider,
            int zoom)
        {
            Coordinate c1 = ScreenPixelToLatLon(0, 0, tractorFix, tractorXpx, tractorYpx, tractorHeadingDeg, pixelsPerMeter);
            Coordinate c2 = ScreenPixelToLatLon(imageWidthPx, 0, tractorFix, tractorXpx, tractorYpx, tractorHeadingDeg, pixelsPerMeter);
            Coordinate c3 = ScreenPixelToLatLon(imageWidthPx, imageHeightPx, tractorFix, tractorXpx, tractorYpx, tractorHeadingDeg, pixelsPerMeter);
            Coordinate c4 = ScreenPixelToLatLon(0, imageHeightPx, tractorFix, tractorXpx, tractorYpx, tractorHeadingDeg, pixelsPerMeter);

            double minLat = Math.Max(-MaxWebMercatorLat, Math.Min(Math.Min(c1.Latitude, c2.Latitude), Math.Min(c3.Latitude, c4.Latitude)));
            double maxLat = Math.Min(MaxWebMercatorLat, Math.Max(Math.Max(c1.Latitude, c2.Latitude), Math.Max(c3.Latitude, c4.Latitude)));
            double minLon = Math.Min(Math.Min(c1.Longitude, c2.Longitude), Math.Min(c3.Longitude, c4.Longitude));
            double maxLon = Math.Max(Math.Max(c1.Longitude, c2.Longitude), Math.Max(c3.Longitude, c4.Longitude));

            (int minX, int minY) = LonLatToTile(minLon, maxLat, zoom);
            (int maxX, int maxY) = LonLatToTile(maxLon, minLat, zoom);

            int tilesPerAxis = 1 << zoom;
            minX = Math.Max(0, Math.Min(tilesPerAxis - 1, minX - 1));
            maxX = Math.Max(0, Math.Min(tilesPerAxis - 1, maxX + 1));
            minY = Math.Max(0, Math.Min(tilesPerAxis - 1, minY - 1));
            maxY = Math.Max(0, Math.Min(tilesPerAxis - 1, maxY + 1));

            List<TileKey> keys = new List<TileKey>((maxX - minX + 1) * (maxY - minY + 1));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    keys.Add(new TileKey(provider, zoom, x, y));
                }
            }

            return keys;
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

        private static Coordinate ScreenPixelToLatLon(
            int screenX,
            int screenY,
            GNSSFix tractorFix,
            int tractorXpx,
            int tractorYpx,
            double tractorHeadingDeg,
            double pixelsPerMeter)
        {
            UTM.UTMCoordinate tractorUtm = UTM.FromLatLon(tractorFix.Latitude, tractorFix.Longitude);

            double dxScreen = screenX - tractorXpx;
            double dyScreen = screenY - tractorYpx;

            // Inverse of LatLonToWorldF rotate(-heading): rotate(+heading).
            double radians = tractorHeadingDeg * Math.PI / 180.0;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            double unrotX = dxScreen * cos - dyScreen * sin;
            double unrotY = dxScreen * sin + dyScreen * cos;

            double eastM = unrotX / pixelsPerMeter;
            double northM = -unrotY / pixelsPerMeter;

            double sampleEasting = tractorUtm.Easting + eastM;
            double sampleNorthing = tractorUtm.Northing + northM;
            UTM.ToLatLon(tractorUtm.Zone, tractorUtm.IsNorthernHemisphere, sampleEasting, sampleNorthing, out double lat, out double lon);
            return new Coordinate(lat, lon);
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        private void TrimCacheIfNeeded()
        {
            if (_tileCache.Count <= MaxCacheTiles)
            {
                return;
            }

            List<KeyValuePair<TileKey, CacheEntry>> snapshot = new List<KeyValuePair<TileKey, CacheEntry>>(_tileCache);
            snapshot.Sort((a, b) => a.Value.LastAccessUtc.CompareTo(b.Value.LastAccessUtc));

            int removeCount = _tileCache.Count - MaxCacheTiles;
            for (int i = 0; i < removeCount && i < snapshot.Count; i++)
            {
                if (_tileCache.TryRemove(snapshot[i].Key, out CacheEntry? removed))
                {
                    removed.Bitmap.Dispose();
                }
            }
        }
    }
}
