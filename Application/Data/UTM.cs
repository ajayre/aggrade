using System;

namespace AgGrade.Data
{
    /// <summary>
    /// Utility methods for converting between WGS‑84 latitude/longitude and
    /// UTM (Universal Transverse Mercator) easting/northing coordinates.
    ///
    /// Assumptions / simplifications:
    /// - WGS‑84 ellipsoid is assumed.
    /// - Standard UTM scale factor k0 = 0.9996 is used.
    /// - Special-case longitude zone tweaks for Norway/Svalbard are ignored.
    /// - Intended use is over relatively small working areas (e.g. a few km),
    ///   which is much larger than the 2,640 ft × 2,640 ft requirement.
    /// </summary>
    public static class UTM
    {
        /// <summary>
        /// Represents a UTM coordinate (zone, hemisphere, easting, northing).
        /// </summary>
        public struct UTMCoordinate
        {
            public int Zone;
            public bool IsNorthernHemisphere;
            public double Easting;   // meters
            public double Northing;  // meters

            public UTMCoordinate(int zone, bool isNorthernHemisphere, double easting, double northing)
            {
                Zone = zone;
                IsNorthernHemisphere = isNorthernHemisphere;
                Easting = easting;
                Northing = northing;
            }
        }

        // WGS‑84 ellipsoid constants
        private const double a = 6378137.0;                   // semi-major axis (meters)
        private const double f = 1.0 / 298.257223563;         // flattening
        private const double k0 = 0.9996;                     // UTM scale factor

        private static readonly double b = a * (1.0 - f);     // semi-minor axis
        private static readonly double e2 = 1.0 - (b * b) / (a * a);         // first eccentricity squared
        private static readonly double ePrime2 = e2 / (1.0 - e2);           // second eccentricity squared

        /// <summary>
        /// Converts WGS‑84 latitude/longitude to a UTM coordinate.
        /// </summary>
        /// <param name="latitudeDeg">Latitude in decimal degrees.</param>
        /// <param name="longitudeDeg">Longitude in decimal degrees.</param>
        /// <returns>UTM coordinate (zone, hemisphere, easting, northing).</returns>
        public static UTMCoordinate FromLatLon(double latitudeDeg, double longitudeDeg)
        {
            if (latitudeDeg < -80.0 || latitudeDeg > 84.0)
            {
                // Standard UTM is defined only between 80°S and 84°N.
                throw new ArgumentOutOfRangeException(nameof(latitudeDeg), "Latitude out of UTM bounds (-80 to 84 degrees).");
            }

            // Normalize longitude to [-180, 180)
            double lonNorm = ((longitudeDeg + 180.0) % 360.0 + 360.0) % 360.0 - 180.0;

            int zone = (int)Math.Floor((lonNorm + 180.0) / 6.0) + 1;
            bool isNorthern = latitudeDeg >= 0.0;

            double latRad = DegreesToRadians(latitudeDeg);
            double lonRad = DegreesToRadians(lonNorm);

            double lonOrigin = (zone - 1) * 6 - 180 + 3; // +3 puts origin in middle of zone
            double lonOriginRad = DegreesToRadians(lonOrigin);

            double N = a / Math.Sqrt(1.0 - e2 * Math.Sin(latRad) * Math.Sin(latRad));
            double T = Math.Tan(latRad) * Math.Tan(latRad);
            double C = ePrime2 * Math.Cos(latRad) * Math.Cos(latRad);
            double A = Math.Cos(latRad) * (lonRad - lonOriginRad);

            double M = a * (
                (1.0 - e2 / 4.0 - 3.0 * e2 * e2 / 64.0 - 5.0 * e2 * e2 * e2 / 256.0) * latRad
                - (3.0 * e2 / 8.0 + 3.0 * e2 * e2 / 32.0 + 45.0 * e2 * e2 * e2 / 1024.0) * Math.Sin(2.0 * latRad)
                + (15.0 * e2 * e2 / 256.0 + 45.0 * e2 * e2 * e2 / 1024.0) * Math.Sin(4.0 * latRad)
                - (35.0 * e2 * e2 * e2 / 3072.0) * Math.Sin(6.0 * latRad)
            );

            double x = k0 * N *
                       (A +
                        (1.0 - T + C) * Math.Pow(A, 3) / 6.0 +
                        (5.0 - 18.0 * T + T * T + 72.0 * C - 58.0 * ePrime2) * Math.Pow(A, 5) / 120.0);

            double y = k0 *
                       (M +
                        N * Math.Tan(latRad) *
                        (A * A / 2.0 +
                         (5.0 - T + 9.0 * C + 4.0 * C * C) * Math.Pow(A, 4) / 24.0 +
                         (61.0 - 58.0 * T + T * T + 600.0 * C - 330.0 * ePrime2) * Math.Pow(A, 6) / 720.0));

            // Apply false easting and northing
            double easting = x + 500000.0;
            double northing = y;
            if (!isNorthern)
            {
                // 10,000,000 meter offset for southern hemisphere
                northing += 10000000.0;
            }

            return new UTMCoordinate(zone, isNorthern, easting, northing);
        }

        /// <summary>
        /// Converts a UTM coordinate back to WGS‑84 latitude/longitude.
        /// </summary>
        /// <param name="zone">UTM zone.</param>
        /// <param name="isNorthernHemisphere">True if in northern hemisphere, false if southern.</param>
        /// <param name="easting">Easting in meters.</param>
        /// <param name="northing">Northing in meters (includes false northing if southern hemisphere).</param>
        /// <param name="latitudeDeg">Output latitude in decimal degrees.</param>
        /// <param name="longitudeDeg">Output longitude in decimal degrees.</param>
        public static void ToLatLon(
            int zone,
            bool isNorthernHemisphere,
            double easting,
            double northing,
            out double latitudeDeg,
            out double longitudeDeg)
        {
            if (zone < 1 || zone > 60)
            {
                throw new ArgumentOutOfRangeException(nameof(zone), "UTM zone must be between 1 and 60.");
            }

            // Remove false easting and northing
            double x = easting - 500000.0;
            double y = northing;

            if (!isNorthernHemisphere)
            {
                y -= 10000000.0;
            }

            double lonOrigin = (zone - 1) * 6 - 180 + 3; // central meridian of zone
            double lonOriginRad = DegreesToRadians(lonOrigin);

            // Footpoint latitude
            double M = y / k0;
            double mu = M / (a * (1.0 - e2 / 4.0 - 3.0 * e2 * e2 / 64.0 - 5.0 * e2 * e2 * e2 / 256.0));

            double e1 = (1.0 - Math.Sqrt(1.0 - e2)) / (1.0 + Math.Sqrt(1.0 - e2));

            double J1 = (3.0 * e1 / 2.0 - 27.0 * e1 * e1 * e1 / 32.0);
            double J2 = (21.0 * e1 * e1 / 16.0 - 55.0 * e1 * e1 * e1 * e1 / 32.0);
            double J3 = (151.0 * e1 * e1 * e1 / 96.0);
            double J4 = (1097.0 * e1 * e1 * e1 * e1 / 512.0);

            double fp = mu
                        + J1 * Math.Sin(2.0 * mu)
                        + J2 * Math.Sin(4.0 * mu)
                        + J3 * Math.Sin(6.0 * mu)
                        + J4 * Math.Sin(8.0 * mu);

            double sinFp = Math.Sin(fp);
            double cosFp = Math.Cos(fp);

            double C1 = ePrime2 * cosFp * cosFp;
            double T1 = Math.Tan(fp) * Math.Tan(fp);
            double N1 = a / Math.Sqrt(1.0 - e2 * sinFp * sinFp);
            double R1 = N1 * (1.0 - e2) / (1.0 - e2 * sinFp * sinFp);
            double D = x / (N1 * k0);

            double latRad = fp
                            - (N1 * Math.Tan(fp) / R1) *
                              (D * D / 2.0
                               - (5.0 + 3.0 * T1 + 10.0 * C1 - 4.0 * C1 * C1 - 9.0 * ePrime2) * Math.Pow(D, 4) / 24.0
                               + (61.0 + 90.0 * T1 + 298.0 * C1 + 45.0 * T1 * T1 - 252.0 * ePrime2 - 3.0 * C1 * C1) * Math.Pow(D, 6) / 720.0);

            double lonRad = lonOriginRad
                            + (D
                               - (1.0 + 2.0 * T1 + C1) * Math.Pow(D, 3) / 6.0
                               + (5.0 - 2.0 * C1 + 28.0 * T1 - 3.0 * C1 * C1 + 8.0 * ePrime2 + 24.0 * T1 * T1) * Math.Pow(D, 5) / 120.0)
                              / cosFp;

            latitudeDeg = RadiansToDegrees(latRad);
            longitudeDeg = RadiansToDegrees(lonRad);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}


