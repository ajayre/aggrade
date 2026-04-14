using AgGrade.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    /// <summary>
    /// Pan/zoom viewer for ArcGIS Online Esri World Imagery (satellite) tiles. North is fixed upward; no persistent tile cache.
    /// </summary>
    public partial class MiniMap : UserControl
    {
        private const double MaxWebMercatorLat = 85.05112878;
        private const int TileSizePx = 256;
        private const int MinZoom = 0;
        private const int MaxZoom = 19;
        private const int WmMouseWheel = 0x020A;
        private const int PanDragThresholdPixels = 5;

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        static MiniMap()
        {
            try
            {
                Http.DefaultRequestHeaders.UserAgent.ParseAdd("AgGrade/1.0");
            }
            catch
            {
            }
        }

        private readonly object _tileLock = new object();
        private readonly Dictionary<TileKey, Bitmap> _tileBitmaps = new Dictionary<TileKey, Bitmap>();
        private readonly HashSet<TileKey> _inflight = new HashSet<TileKey>();

        /// <summary>Pre-zoom full-client capture, warped under new tiles until they load.</summary>
        private Bitmap? _zoomTransitionSnapshot;
        /// <summary>Snapshot resampled to the new view (avoids GDI+ DrawImage parallelogram limitations on .NET 8).</summary>
        private Bitmap? _transitionWarped;
        private int _transSnapW;
        private int _transSnapH;
        private int _transOldZoom;
        private double _transOldWcX;
        private double _transOldWcY;

        private double _centerLat = 39.8283;
        private double _centerLon = -98.5795;
        private int _zoom = 4;

        private Point _pressLocation;
        private Point _lastPanPoint;
        private bool _panning;

        private bool _hasMarker;
        private double _markerLat;
        private double _markerLon;

        /// <summary>Fired when the user clicks (without dragging) to place the location marker.</summary>
        public event Action<double, double>? MarkerLocationChosen;

        /// <summary>
        /// Bitmap drawn at the chosen location (tip at bottom center). Assign from application resources (e.g. <c>Properties.Resources.location_48px</c>).
        /// </summary>
        public Bitmap? MapMarker { get; set; }

        /// <summary>Hatch fill color for the download area preview (40 ac + tile margin).</summary>
        public Color DownloadAreaColor { get; set; } = Color.PaleGoldenrod;

        public MiniMap()
        {
            InitializeComponent();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            DoubleBuffered = true;
            BackColor = Color.FromArgb(32, 32, 36);
        }

        /// <summary>Map center latitude in degrees (WGS84).</summary>
        public double CenterLatitude
        {
            get => _centerLat;
            set
            {
                _centerLat = ClampLat(value);
                ClearZoomTransitionSnapshot();
                ClearTileImages();
                Invalidate();
            }
        }

        /// <summary>Map center longitude in degrees (WGS84).</summary>
        public double CenterLongitude
        {
            get => _centerLon;
            set
            {
                _centerLon = WrapLon(value);
                ClearZoomTransitionSnapshot();
                ClearTileImages();
                Invalidate();
            }
        }

        /// <summary>Zoom level (larger = closer).</summary>
        public int Zoom
        {
            get => _zoom;
            set
            {
                int z = Math.Clamp(value, MinZoom, MaxZoom);
                if (z == _zoom)
                {
                    return;
                }
                _zoom = z;
                ClearZoomTransitionSnapshot();
                ClearTileImages();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            LatLonToWorldPixel(_centerLon, _centerLat, _zoom, out double wcX, out double wcY);

            if (_zoomTransitionSnapshot != null)
            {
                if (w != _transSnapW || h != _transSnapH)
                {
                    ClearZoomTransitionSnapshot();
                }
                else
                {
                    TryDropZoomTransitionIfComplete(w, h, wcX, wcY);
                }
            }

            if (_zoomTransitionSnapshot != null)
            {
                try
                {
                    EnsureWarpedTransitionBitmap(w, h, wcX, wcY);
                    if (_transitionWarped != null)
                    {
                        g.DrawImageUnscaled(_transitionWarped, 0, 0);
                    }
                }
                catch
                {
                    ClearZoomTransitionSnapshot();
                }
            }

            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            double leftWorld = wcX - w * 0.5;
            double topWorld = wcY - h * 0.5;
            int tilesPerAxis = 1 << _zoom;

            int txStart = (int)Math.Floor(leftWorld / TileSizePx);
            int txEnd = (int)Math.Floor((leftWorld + w) / TileSizePx);
            int tyStart = (int)Math.Floor(topWorld / TileSizePx);
            int tyEnd = (int)Math.Floor((topWorld + h) / TileSizePx);

            bool showPlaceholder = _zoomTransitionSnapshot == null && _transitionWarped == null;

            for (int ty = tyStart; ty <= tyEnd; ty++)
            {
                if (ty < 0 || ty >= tilesPerAxis)
                {
                    continue;
                }

                for (int tx = txStart; tx <= txEnd; tx++)
                {
                    int tcx = ((tx % tilesPerAxis) + tilesPerAxis) % tilesPerAxis;
                    TileKey key = new TileKey(_zoom, tcx, ty);

                    double tileLeftWorld = tx * TileSizePx;
                    double tileTopWorld = ty * TileSizePx;
                    float screenX = (float)(tileLeftWorld - leftWorld);
                    float screenY = (float)(tileTopWorld - topWorld);
                    var dest = new RectangleF(screenX, screenY, TileSizePx, TileSizePx);

                    Bitmap? bmp;
                    lock (_tileLock)
                    {
                        _tileBitmaps.TryGetValue(key, out bmp);
                    }

                    if (bmp != null)
                    {
                        g.DrawImage(bmp, dest);
                    }
                    else
                    {
                        if (showPlaceholder)
                        {
                            using var brush = new SolidBrush(Color.FromArgb(48, 48, 52));
                            g.FillRectangle(brush, dest.X, dest.Y, dest.Width, dest.Height);
                        }
                        RequestTile(key);
                    }
                }
            }

            if (_hasMarker)
            {
                DrawDownloadAreaOverlay(g, w, h, leftWorld, topWorld);
            }

            if (_hasMarker && MapMarker != null)
            {
                LatLonToWorldPixel(_markerLon, _markerLat, _zoom, out double mkWx, out double mkWy);
                float mx = (float)(mkWx - leftWorld);
                float my = (float)(mkWy - topWorld);
                int mw = MapMarker.Width;
                int mh = MapMarker.Height;
                float drawX = mx - mw * 0.5f;
                float drawY = my - mh;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(MapMarker, drawX, drawY, mw, mh);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
            }
        }

        private void DrawDownloadAreaOverlay(Graphics g, int clientW, int clientH, double leftWorld, double topWorld)
        {
            (double minLat, double minLon, double maxLat, double maxLon) =
                BasemapDownloader.GetCentroidDownloadTileCoverageBounds(_markerLat, _markerLon);

            LonLatToScreenPx(minLon, maxLat, leftWorld, topWorld, out float nwX, out float nwY);
            LonLatToScreenPx(maxLon, maxLat, leftWorld, topWorld, out float neX, out float neY);
            LonLatToScreenPx(maxLon, minLat, leftWorld, topWorld, out float seX, out float seY);
            LonLatToScreenPx(minLon, minLat, leftWorld, topWorld, out float swX, out float swY);

            var quad = new[]
            {
                new PointF(nwX, nwY),
                new PointF(neX, neY),
                new PointF(seX, seY),
                new PointF(swX, swY),
            };

            Color c = DownloadAreaColor;
            GraphicsState saved = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.SetClip(new Rectangle(0, 0, clientW, clientH));
                using var hatch = new HatchBrush(
                    HatchStyle.LightUpwardDiagonal,
                    Color.FromArgb(140, c),
                    Color.FromArgb(55, c));
                g.FillPolygon(hatch, quad);
                using var edge = new Pen(Color.FromArgb(200, c), 1f);
                g.DrawPolygon(edge, quad);
            }
            finally
            {
                g.Restore(saved);
            }
        }

        private void LonLatToScreenPx(double lon, double lat, double leftWorld, double topWorld, out float sx, out float sy)
        {
            LatLonToWorldPixel(lon, lat, _zoom, out double wx, out double wy);
            sx = (float)(wx - leftWorld);
            sy = (float)(wy - topWorld);
        }

        private void ScreenClientToLatLon(int sx, int sy, out double lon, out double lat)
        {
            int cw = ClientSize.Width;
            int ch = ClientSize.Height;
            LatLonToWorldPixel(_centerLon, _centerLat, _zoom, out double wcX, out double wcY);
            double wx = wcX + (sx - cw * 0.5);
            double wy = wcY + (sy - ch * 0.5);
            WorldPixelToLatLon(wx, wy, _zoom, out lon, out lat);
        }

        private void PlaceMarkerFromClick(int sx, int sy)
        {
            ScreenClientToLatLon(sx, sy, out double lon, out double lat);
            _markerLat = ClampLat(lat);
            _markerLon = WrapLon(lon);
            _hasMarker = true;
            Invalidate();
            MarkerLocationChosen?.Invoke(_markerLat, _markerLon);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClearZoomTransitionSnapshot();
        }

        /// <summary>
        /// Maps a point on the current (new-zoom) screen to coordinates in the pre-zoom snapshot bitmap.
        /// </summary>
        private void MapScreenCornerToSnapshot(
            float sx,
            float sy,
            int w,
            int h,
            double newWcX,
            double newWcY,
            out float snapX,
            out float snapY)
        {
            double wx1 = newWcX + (sx - w * 0.5);
            double wy1 = newWcY + (sy - h * 0.5);
            WorldPixelToLatLon(wx1, wy1, _zoom, out double lon, out double lat);
            LatLonToWorldPixel(lon, lat, _transOldZoom, out double wx0, out double wy0);
            snapX = (float)(wx0 - (_transOldWcX - _transSnapW * 0.5));
            snapY = (float)(wy0 - (_transOldWcY - _transSnapH * 0.5));
        }

        private void TryDropZoomTransitionIfComplete(int w, int h, double wcX, double wcY)
        {
            double leftWorld = wcX - w * 0.5;
            double topWorld = wcY - h * 0.5;
            int tilesPerAxis = 1 << _zoom;
            int txStart = (int)Math.Floor(leftWorld / TileSizePx);
            int txEnd = (int)Math.Floor((leftWorld + w) / TileSizePx);
            int tyStart = (int)Math.Floor(topWorld / TileSizePx);
            int tyEnd = (int)Math.Floor((topWorld + h) / TileSizePx);

            lock (_tileLock)
            {
                for (int ty = tyStart; ty <= tyEnd; ty++)
                {
                    if (ty < 0 || ty >= tilesPerAxis)
                    {
                        continue;
                    }

                    for (int tx = txStart; tx <= txEnd; tx++)
                    {
                        int tcx = ((tx % tilesPerAxis) + tilesPerAxis) % tilesPerAxis;
                        var key = new TileKey(_zoom, tcx, ty);
                        if (!_tileBitmaps.ContainsKey(key))
                        {
                            return;
                        }
                    }
                }
            }

            ClearZoomTransitionSnapshot();
        }

        private void ClearZoomTransitionSnapshot()
        {
            if (_zoomTransitionSnapshot != null)
            {
                _zoomTransitionSnapshot.Dispose();
                _zoomTransitionSnapshot = null;
            }
            if (_transitionWarped != null)
            {
                _transitionWarped.Dispose();
                _transitionWarped = null;
            }
        }

        private void EnsureWarpedTransitionBitmap(int w, int h, double wcX, double wcY)
        {
            if (_zoomTransitionSnapshot == null)
            {
                return;
            }
            if (_transitionWarped != null && _transitionWarped.Width == w && _transitionWarped.Height == h)
            {
                return;
            }

            _transitionWarped?.Dispose();
            _transitionWarped = CreateWarpedTransitionBitmap(w, h, wcX, wcY);
        }

        private Bitmap CreateWarpedTransitionBitmap(int w, int h, double wcX, double wcY)
        {
            Bitmap srcRaw = _zoomTransitionSnapshot!;
            int sw = srcRaw.Width;
            int sh = srcRaw.Height;
            using Bitmap src = srcRaw.Clone(new Rectangle(0, 0, sw, sh), PixelFormat.Format32bppArgb);
            var dest = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            BitmapData srcData = src.LockBits(
                new Rectangle(0, 0, sw, sh),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData destData = dest.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int srcStride = srcData.Stride;
                int destStride = destData.Stride;
                int srcRowBytes = Math.Abs(srcStride);
                int destRowBytes = Math.Abs(destStride);
                byte[] srcBuf = new byte[srcRowBytes * sh];
                byte[] destBuf = new byte[destRowBytes * h];
                Marshal.Copy(srcData.Scan0, srcBuf, 0, srcBuf.Length);

                for (int py = 0; py < h; py++)
                {
                    int row = py * destRowBytes;
                    for (int px = 0; px < w; px++)
                    {
                        MapScreenCornerToSnapshot(px + 0.5f, py + 0.5f, w, h, wcX, wcY, out float sx, out float sy);
                        BilinearSampleBgra(srcBuf, srcRowBytes, sw, sh, sx, sy, out byte bb, out byte gg, out byte rr, out byte aa);
                        int di = row + px * 4;
                        destBuf[di] = bb;
                        destBuf[di + 1] = gg;
                        destBuf[di + 2] = rr;
                        destBuf[di + 3] = aa;
                    }
                }

                for (int row = 0; row < h; row++)
                {
                    IntPtr rowPtr = IntPtr.Add(destData.Scan0, row * destStride);
                    Marshal.Copy(destBuf, row * destRowBytes, rowPtr, destRowBytes);
                }
            }
            finally
            {
                src.UnlockBits(srcData);
                dest.UnlockBits(destData);
            }

            return dest;
        }

        private static void BilinearSampleBgra(
            byte[] buffer,
            int rowBytes,
            int width,
            int height,
            float x,
            float y,
            out byte b,
            out byte g,
            out byte r,
            out byte a)
        {
            if (width <= 0 || height <= 0)
            {
                b = g = r = a = 0;
                return;
            }

            float fx = Math.Clamp(x, 0f, width - 1f);
            float fy = Math.Clamp(y, 0f, height - 1f);
            int x0 = (int)Math.Floor(fx);
            int y0 = (int)Math.Floor(fy);
            int x1 = Math.Min(x0 + 1, width - 1);
            int y1 = Math.Min(y0 + 1, height - 1);
            float tx = fx - x0;
            float ty = fy - y0;

            int o00 = y0 * rowBytes + x0 * 4;
            int o10 = y0 * rowBytes + x1 * 4;
            int o01 = y1 * rowBytes + x0 * 4;
            int o11 = y1 * rowBytes + x1 * 4;

            float Mix(float v0, float v1, float t) => v0 + (v1 - v0) * t;

            float b0 = Mix(buffer[o00], buffer[o10], tx);
            float b1 = Mix(buffer[o01], buffer[o11], tx);
            b = (byte)Math.Clamp(Mix(b0, b1, ty), 0f, 255f);
            float g0 = Mix(buffer[o00 + 1], buffer[o10 + 1], tx);
            float g1 = Mix(buffer[o01 + 1], buffer[o11 + 1], tx);
            g = (byte)Math.Clamp(Mix(g0, g1, ty), 0f, 255f);
            float r0 = Mix(buffer[o00 + 2], buffer[o10 + 2], tx);
            float r1 = Mix(buffer[o01 + 2], buffer[o11 + 2], tx);
            r = (byte)Math.Clamp(Mix(r0, r1, ty), 0f, 255f);
            float a0 = Mix(buffer[o00 + 3], buffer[o10 + 3], tx);
            float a1 = Mix(buffer[o01 + 3], buffer[o11 + 3], tx);
            a = (byte)Math.Clamp(Mix(a0, a1, ty), 0f, 255f);
        }

        private void CaptureZoomTransitionSnapshot()
        {
            ClearZoomTransitionSnapshot();
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            LatLonToWorldPixel(_centerLon, _centerLat, _zoom, out _transOldWcX, out _transOldWcY);
            _transOldZoom = _zoom;
            _transSnapW = w;
            _transSnapH = h;

            try
            {
                var snap = new Bitmap(w, h);
                snap.SetResolution(96f, 96f);
                DrawToBitmap(snap, new Rectangle(0, 0, w, h));
                _zoomTransitionSnapshot = snap;
            }
            catch
            {
                _transSnapW = 0;
                _transSnapH = 0;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                ClearZoomTransitionSnapshot();
                _pressLocation = e.Location;
                _lastPanPoint = e.Location;
                _panning = false;
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if ((e.Button & MouseButtons.Left) == 0)
            {
                return;
            }

            if (!_panning)
            {
                int ddx = e.X - _pressLocation.X;
                int ddy = e.Y - _pressLocation.Y;
                if (ddx * ddx + ddy * ddy <= PanDragThresholdPixels * PanDragThresholdPixels)
                {
                    return;
                }
                _panning = true;
                _lastPanPoint = _pressLocation;
            }

            int dx = e.X - _lastPanPoint.X;
            int dy = e.Y - _lastPanPoint.Y;
            _lastPanPoint = e.Location;
            if (dx == 0 && dy == 0)
            {
                return;
            }

            LatLonToWorldPixel(_centerLon, _centerLat, _zoom, out double wcX, out double wcY);
            double worldSize = WorldSizePixels(_zoom);
            double nwx = wcX - dx;
            double nwy = wcY - dy;
            nwx = ((nwx % worldSize) + worldSize) % worldSize;
            nwy = Math.Clamp(nwy, 0.0, worldSize - 1e-6);
            WorldPixelToLatLon(nwx, nwy, _zoom, out double lon, out double lat);
            _centerLon = lon;
            _centerLat = ClampLat(lat);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                if (!_panning)
                {
                    int rdx = e.X - _pressLocation.X;
                    int rdy = e.Y - _pressLocation.Y;
                    if (rdx * rdx + rdy * rdy <= PanDragThresholdPixels * PanDragThresholdPixels)
                    {
                        PlaceMarkerFromClick(_pressLocation.X, _pressLocation.Y);
                    }
                }
                _panning = false;
                Capture = false;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!Capture)
            {
                _panning = false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmMouseWheel)
            {
                Point client = PointToClient(Cursor.Position);
                if (ClientRectangle.Contains(client))
                {
                    uint wparam = unchecked((uint)(nint)m.WParam);
                    int delta = (short)(wparam >> 16);
                    OnMouseWheelHover(client, delta);
                    m.Result = IntPtr.Zero;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void OnMouseWheelHover(Point client, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            int steps = delta / 120;
            if (steps == 0)
            {
                steps = delta > 0 ? 1 : -1;
            }

            int newZoom = Math.Clamp(_zoom + steps, MinZoom, MaxZoom);
            if (newZoom == _zoom)
            {
                return;
            }

            CaptureZoomTransitionSnapshot();

            LatLonToWorldPixel(_centerLon, _centerLat, _zoom, out double wcX, out double wcY);
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            double wxAt = wcX + (client.X - w * 0.5);
            double wyAt = wcY + (client.Y - h * 0.5);
            WorldPixelToLatLon(wxAt, wyAt, _zoom, out double anchorLon, out double anchorLat);

            _zoom = newZoom;

            LatLonToWorldPixel(anchorLon, anchorLat, _zoom, out double wxn, out double wyn);
            double wcXn = wxn - (client.X - w * 0.5);
            double wcYn = wyn - (client.Y - h * 0.5);
            double worldSize = WorldSizePixels(_zoom);
            wcXn = ((wcXn % worldSize) + worldSize) % worldSize;
            wcYn = Math.Clamp(wcYn, 0.0, worldSize - 1e-6);
            WorldPixelToLatLon(wcXn, wcYn, _zoom, out _centerLon, out double clat);
            _centerLat = ClampLat(clat);

            ClearTileImages();
            Invalidate();
        }

        private void RequestTile(TileKey key)
        {
            lock (_tileLock)
            {
                if (_inflight.Contains(key) || _tileBitmaps.ContainsKey(key))
                {
                    return;
                }
                _inflight.Add(key);
            }

            _ = FetchTileAsync(key);
        }

        private async Task FetchTileAsync(TileKey key)
        {
            try
            {
                string url =
                    $"https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{key.Z}/{key.Y}/{key.X}";
                using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
                using HttpResponseMessage res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode)
                {
                    return;
                }

                byte[] bytes = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                if (bytes.Length == 0)
                {
                    return;
                }

                using MemoryStream ms = new MemoryStream(bytes, writable: false);
                Bitmap decoded = new Bitmap(ms);

                void Apply()
                {
                    if (IsDisposed)
                    {
                        decoded.Dispose();
                        return;
                    }

                    lock (_tileLock)
                    {
                        _inflight.Remove(key);
                        if (_tileBitmaps.ContainsKey(key))
                        {
                            decoded.Dispose();
                            return;
                        }
                        if (key.Z != _zoom)
                        {
                            decoded.Dispose();
                            return;
                        }
                        _tileBitmaps[key] = decoded;
                    }
                    Invalidate();
                }

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(Apply));
                }
                else
                {
                    Apply();
                }
            }
            catch
            {
                lock (_tileLock)
                {
                    _inflight.Remove(key);
                }
            }
        }

        private void ClearTileImages()
        {
            lock (_tileLock)
            {
                foreach (Bitmap b in _tileBitmaps.Values)
                {
                    b.Dispose();
                }
                _tileBitmaps.Clear();
                _inflight.Clear();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ClearZoomTransitionSnapshot();
                ClearTileImages();
            }
            base.Dispose(disposing);
        }

        private static double WorldSizePixels(int zoom)
        {
            return TileSizePx * Math.Pow(2, zoom);
        }

        private static void LatLonToWorldPixel(double lon, double lat, int zoom, out double wx, out double wy)
        {
            lat = ClampLat(lat);
            lon = WrapLon(lon);
            double worldSize = WorldSizePixels(zoom);
            wx = (lon + 180.0) / 360.0 * worldSize;
            double latRad = lat * Math.PI / 180.0;
            wy = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * worldSize;
        }

        private static void WorldPixelToLatLon(double wx, double wy, int zoom, out double lon, out double lat)
        {
            double worldSize = WorldSizePixels(zoom);
            wx = ((wx % worldSize) + worldSize) % worldSize;
            wy = Math.Clamp(wy, 0.0, worldSize - 1e-9);
            lon = wx / worldSize * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * wy / worldSize)));
            lat = latRad * 180.0 / Math.PI;
        }

        private static double ClampLat(double lat)
        {
            return Math.Max(-MaxWebMercatorLat, Math.Min(MaxWebMercatorLat, lat));
        }

        private static double WrapLon(double lon)
        {
            double w = (lon + 180.0) % 360.0;
            if (w < 0)
            {
                w += 360.0;
            }
            return w - 180.0;
        }

        private readonly struct TileKey : IEquatable<TileKey>
        {
            public TileKey(int z, int x, int y)
            {
                Z = z;
                X = x;
                Y = y;
            }

            public int Z { get; }
            public int X { get; }
            public int Y { get; }

            public bool Equals(TileKey other) => Z == other.Z && X == other.X && Y == other.Y;
            public override bool Equals(object? obj) => obj is TileKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Z, X, Y);
        }

    }
}
