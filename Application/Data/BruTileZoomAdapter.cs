using System;

namespace AgGrade.Data
{
    /// <summary>
    /// Converts AgGrade's continuous zoom scale (pixels per meter) into
    /// discrete WebMercator zoom levels used by BruTile.
    /// </summary>
    public sealed class BruTileZoomAdapter
    {
        private const double EarthRadiusM = 6378137.0;
        private const double TileSizePx = 256.0;
        private const double ZoomHysteresis = 0.2;

        private int? _lastZoom;

        public int SelectZoomLevel(
            double pixelsPerMeter,
            double latitudeDeg,
            int minZoom,
            int maxZoom)
        {
            if (pixelsPerMeter <= 0.0)
            {
                return minZoom;
            }

            // AgGrade uses pixels-per-meter. Convert to meters-per-pixel.
            double metersPerPixel = 1.0 / pixelsPerMeter;
            double latRad = latitudeDeg * Math.PI / 180.0;
            double cosLat = Math.Cos(latRad);
            if (Math.Abs(cosLat) < 1e-6)
            {
                cosLat = 1e-6;
            }

            // WebMercator zoom formula.
            double zoomFloat = Math.Log((cosLat * 2.0 * Math.PI * EarthRadiusM) / (TileSizePx * metersPerPixel), 2.0);
            int candidateZoom = (int)Math.Round(zoomFloat);

            if (_lastZoom.HasValue)
            {
                // Prevent tile thrashing around integer zoom boundaries.
                if (Math.Abs(zoomFloat - _lastZoom.Value) < ZoomHysteresis)
                {
                    candidateZoom = _lastZoom.Value;
                }
            }

            if (candidateZoom < minZoom) candidateZoom = minZoom;
            if (candidateZoom > maxZoom) candidateZoom = maxZoom;

            _lastZoom = candidateZoom;
            return candidateZoom;
        }
    }
}
