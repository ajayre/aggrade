using System;

namespace AgGrade.Data
{
    /// <summary>
    /// Computes GPS antenna offset (relative to rear axle center) from two opposing
    /// poses using the pole-in-line calibration method. All linear units are in mm.
    /// </summary>
    public static class TractorAntennaFinder
    {
        // WGS84 ellipsoid
        private const double a = 6378137.0;
        private const double f = 1.0 / 298.257223563;
        private static readonly double e2 = 2 * f - f * f;

        /// <summary>
        /// Calculate antenna offset in tractor frame from two poses.
        /// </summary>
        /// <param name="Tmm">Rear track width (mm), distance between left and right rear hub centers.</param>
        /// <param name="heading1Deg">Tractor heading at pose 1 (degrees, compass: 0=North, 90=East).</param>
        /// <param name="heading2Deg">Tractor heading at pose 2 (degrees).</param>
        /// <param name="lat1">Antenna latitude at pose 1 (degrees).</param>
        /// <param name="lon1">Antenna longitude at pose 1 (degrees).</param>
        /// <param name="lat2">Antenna latitude at pose 2 (degrees).</param>
        /// <param name="lon2">Antenna longitude at pose 2 (degrees).</param>
        /// <param name="Dmm">Optional: distance (mm) from origin to hub along pole (pose 1 axle). Enables 180° heading correction.</param>
        /// <returns>Antenna offset (X = left positive, Y = forward positive) in mm. Origin at rear axle center.</returns>
        public static PointD Calculate
            (
            double Tmm,
            double heading1Deg,
            double heading2Deg,
            double lat1,
            double lon1,
            double lat2,
            double lon2,
            double? Dmm = null
            )
        {
            double lat0 = (lat1 + lat2) / 2.0;
            double lon0 = (lon1 + lon2) / 2.0;

            LatLonToLocalMm(lat1, lon1, lat0, lon0, out double e1Mm, out double n1Mm);
            LatLonToLocalMm(lat2, lon2, lat0, lon0, out double e2Mm, out double n2Mm);

            double P1x = e1Mm;
            double P1y = n1Mm;
            double P2x = e2Mm;
            double P2y = n2Mm;

            Rotation2D(-heading1Deg, out double r1_00, out double r1_01, out double r1_10, out double r1_11);
            Rotation2D(-heading2Deg, out double r2_00, out double r2_01, out double r2_10, out double r2_11);

            double hAx = -Tmm / 2.0;
            double hAy = 0.0;
            double hBx = Tmm / 2.0;
            double hBy = 0.0;

            double t1x = -(r1_00 * hAx + r1_01 * hAy);
            double t1y = -(r1_10 * hAx + r1_11 * hAy);
            double t2x = -(r2_00 * hBx + r2_01 * hBy);
            double t2y = -(r2_10 * hBx + r2_11 * hBy);

            if (Dmm.HasValue)
            {
                double D = Dmm.Value;
                double thRad = heading1Deg * Math.PI / 180.0;
                double Gx = D * Math.Cos(thRad);
                double Gy = -D * Math.Sin(thRad);

                heading2Deg = heading1Deg + 180.0;
                Rotation2D(-heading2Deg, out r2_00, out r2_01, out r2_10, out r2_11);

                t1x = Gx - (r1_00 * hAx + r1_01 * hAy);
                t1y = Gy - (r1_10 * hAx + r1_11 * hAy);
                t2x = Gx - (r2_00 * hBx + r2_01 * hBy);
                t2y = Gy - (r2_10 * hBx + r2_11 * hBy);
            }

            double diffPx = P1x - P2x;
            double diffPy = P1y - P2y;
            double diffTx = t1x - t2x;
            double diffTy = t1y - t2y;
            double rhsX = diffPx - diffTx;
            double rhsY = diffPy - diffTy;

            double rm00 = r1_00 - r2_00;
            double rm01 = r1_01 - r2_01;
            double rm10 = r1_10 - r2_10;
            double rm11 = r1_11 - r2_11;
            double det = rm00 * rm11 - rm01 * rm10;
            double inv00 = rm11 / det;
            double inv01 = -rm01 / det;
            double inv10 = -rm10 / det;
            double inv11 = rm00 / det;

            double xAmm = inv00 * rhsX + inv01 * rhsY;
            double yAmm = inv10 * rhsX + inv11 * rhsY;

            // Report X with positive = left of centerline
            return new PointD(-xAmm, yAmm);
        }

        private static void LatLonToLocalMm(double latDeg, double lonDeg, double lat0Deg, double lon0Deg,
            out double eastMm, out double northMm)
        {
            double lat0Rad = lat0Deg * Math.PI / 180.0;
            double lon0Rad = lon0Deg * Math.PI / 180.0;
            double latRad = latDeg * Math.PI / 180.0;
            double lonRad = lonDeg * Math.PI / 180.0;

            double sinLat0 = Math.Sin(lat0Rad);
            double N = a / Math.Sqrt(1.0 - e2 * sinLat0 * sinLat0);
            double M = N * (1.0 - e2) / (1.0 - e2 * sinLat0 * sinLat0);

            double northM = M * (latRad - lat0Rad);
            double eastM = N * Math.Cos(lat0Rad) * (lonRad - lon0Rad);

            eastMm = eastM * 1000.0;
            northMm = northM * 1000.0;
        }

        private static void Rotation2D(double angleDeg, out double r00, out double r01, out double r10, out double r11)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double c = Math.Cos(rad);
            double s = Math.Sin(rad);
            r00 = c;
            r01 = -s;
            r10 = s;
            r11 = c;
        }
    }
}
