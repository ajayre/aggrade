using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Timer = System.Windows.Forms.Timer;

namespace HardwareSim
{
    internal enum RTKQuality
    {
        NotValid = 0,
        BasicFix = 1,
        DGPS = 2,
        RTKFix = 4,
        RTKFloat = 5
    }

    internal class GNSSData
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public RTKQuality RTKQuality;

        public double TrueHeading;
        public double SpeedMPH;
    }

    internal class PathPoint
    {
        public double Latitude;
        public double Longitude;
        public double Heading;

        public PathPoint
            (
            double Latitude,
            double Longitude,
            double Heading
            )
        {
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            this.Heading = Heading;
        }
    }

    internal class GNSS
    {
        /// <summary>
        /// time between calculations in millseconds
        /// </summary>
        private const int CALC_PERIOD_MS = 50;

        /// <summary>
        /// Distances between tractor and scrapers
        /// </summary>
        private const double TRACTOR_TO_FRONT_SCRAPER_M = 5.0;
        private const double FRONT_SCRAPER_TO_REAR_SCRAPER_M = 5.0;

        private const int STEERING_DAMPENING = 1;

        // the distance moved in meters at one MPH in the calc period
        /*
            1 mile = 1,609.34 meters
            1 hour = 3,600 seconds
            1 MPH = 1,609.34 m ÷ 3,600 s ≈ 0.447 m/s
            Distance in 50ms:
            50 ms = 0.05 seconds
            Distance = 0.447 m/s × 0.05 s ≈ 0.0224 meters (about 2.24 centimeters)
        */
        private const double DIST_M_PER_MPH_PER_CALC_PERIOD = 0.0224;

        public GNSSData TractorGNSS = new GNSSData();
        public GNSSData FrontGNSS = new GNSSData();
        public GNSSData RearGNSS = new GNSSData();

        public event Action<string> OnNewTractorFix = null;
        public event Action<string> OnNewFrontFix = null;
        public event Action<string> OnNewRearFix = null;
        public event Action<IMUValue> OnNewTractorIMU = null;
        public event Action<IMUValue> OnNewFrontIMU = null;
        public event Action<IMUValue> OnNewRearIMU = null;

        private Timer GNSSUpdateTimer;
        private Timer IMUUpdateTimer;
        private Timer CalcTimer;
        private Timer TurnTimer;
        private IMUValue TractorIMU;
        private IMUValue FrontIMU;
        private IMUValue RearIMU;
        private int TurnHeadingChange;
        private int StraightenSteeringCounter;

        private List<PathPoint> TractorPath = new List<PathPoint> ();
        private int FrontScraperPathPointIndex;
        private int RearScraperPathPointIndex;

        public GNSS
            (
            )
        {
            GNSSUpdateTimer = new Timer();
            GNSSUpdateTimer.Interval = 100;
            GNSSUpdateTimer.Tick += GNSSUpdateTimer_Tick;

            IMUUpdateTimer = new Timer();
            IMUUpdateTimer.Interval = 50;
            IMUUpdateTimer.Tick += IMUUpdateTimer_Tick;

            CalcTimer = new Timer();
            CalcTimer.Interval = CALC_PERIOD_MS;
            CalcTimer.Tick += CalcTimer_Tick;

            TurnTimer = new Timer();
            TurnTimer.Interval = 500;
            TurnTimer.Tick += TurnTimer_Tick;

            TractorIMU = new IMUValue();

            // fixme - remove
            TractorIMU.Pitch = 0;
            TractorIMU.Roll = 0;
            TractorIMU.Heading = 0;
            TractorIMU.YawRate = 0;
            TractorIMU.CalibrationStatus = IMUValue.Calibration.Good;

            FrontIMU = new IMUValue();

            FrontIMU.Pitch = 0;
            FrontIMU.Roll = 0;
            FrontIMU.Heading = 0;
            FrontIMU.YawRate = 0;
            FrontIMU.CalibrationStatus = IMUValue.Calibration.Good;

            RearIMU = new IMUValue();

            RearIMU.Pitch = 0;
            RearIMU.Roll = 0;
            RearIMU.Heading = 0;
            RearIMU.YawRate = 0;
            RearIMU.CalibrationStatus = IMUValue.Calibration.Good;

            TractorGNSS.SpeedMPH = 0;
            TractorGNSS.TrueHeading = 0;

            TurnHeadingChange = 0;

            TractorPath.Clear();
        }

        /// <summary>
        /// Called frequency to update the tractor and scraper locations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalcTimer_Tick(object? sender, EventArgs e)
        {
            if (TractorGNSS.SpeedMPH != 0)
            {
                GetNewLocation();
            }
        }

        private void IMUUpdateTimer_Tick(object? sender, EventArgs e)
        {
            OnNewTractorIMU?.Invoke(TractorIMU);
            OnNewFrontIMU?.Invoke(FrontIMU);
            OnNewRearIMU?.Invoke(RearIMU);
        }

        private void GNSSUpdateTimer_Tick(object? sender, EventArgs e)
        {
            OnNewTractorFix?.Invoke(GetNMEAGNGGAString(TractorGNSS));
            OnNewTractorFix?.Invoke(GetNMEAGNVTGString(TractorGNSS));

            OnNewFrontFix?.Invoke(GetNMEAGNGGAString(FrontGNSS));
            OnNewFrontFix?.Invoke(GetNMEAGNVTGString(FrontGNSS));

            OnNewRearFix?.Invoke(GetNMEAGNGGAString(RearGNSS));
            OnNewRearFix?.Invoke(GetNMEAGNVTGString(RearGNSS));
        }

        /// <summary>
        /// Calculate new equipment locations based on current speed and heading of tractor
        /// Called once per second
        /// </summary>
        private void GetNewLocation
            (
            )
        {
            // how far have we travelled since the last call?
            double MetersPerSec = (TractorGNSS.SpeedMPH * 0.44704) * CALC_PERIOD_MS / 1000.0;

            double StartLat = TractorGNSS.Latitude;
            double StartLon = TractorGNSS.Longitude;

            // move the tractor forwards along the current heading
            double Lat = TractorGNSS.Latitude;
            double Lon = TractorGNSS.Longitude;
            Haversine.MoveDistanceBearing(ref Lat, ref Lon, TractorGNSS.TrueHeading, MetersPerSec);
            TractorGNSS.Latitude = Lat;
            TractorGNSS.Longitude = Lon;

            // update path, adding in intermediate points (the faster tractor goes the more points to add)
            // ending with the current tractor location
            // at 1MPH we only add the current tractor location
            /*
                INITIAL:
                R         F         T
                0 1 2 3 4 5 6 7 8 9 10

                1 MPH:
                0 1 2 3 4 5 6 7 8 9 10 11
                  1 2 3 4 5 6 7 8 9 10 11
                  0 1 2 3 4 5 6 7 8 9  10
                  R         F          T
  
                2 MPH:
                0 1 2 3 4 5 6 7 8 9 10 11 12
                    2 3 4 5 6 7 8 9 10 11 12
	                0 1 2 3 4 5 6 7 8  9  10
	                R         F           T

                4 MPH:
                0 1 2 3 4 5 6 7 8 9 10 11 12 13 14
                        4 5 6 7 8 9 10 11 12 13 14
                        0 1 2 3 4 5 6  7  8  9  10
		                R         F             T
            */
            int PointsAdded = 0;
            for (double m = (DIST_M_PER_MPH_PER_CALC_PERIOD * TractorGNSS.SpeedMPH) - DIST_M_PER_MPH_PER_CALC_PERIOD; m >= 0; m -= DIST_M_PER_MPH_PER_CALC_PERIOD)
            {
                Lat = TractorGNSS.Latitude;
                Lon = TractorGNSS.Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, TractorGNSS.TrueHeading, -m);
                TractorPath.Add(new PathPoint(Lat, Lon, TractorGNSS.TrueHeading));
                PointsAdded++;
            }
            // remove oldest points on path so overall number of points stays the same (= same path length)
            for (int p = 0; p < PointsAdded; p++)
            {
                TractorPath.RemoveAt(0);
            }

            // update front location
            FrontGNSS.Latitude = TractorPath[FrontScraperPathPointIndex].Latitude;
            FrontGNSS.Longitude = TractorPath[FrontScraperPathPointIndex].Longitude;
            FrontGNSS.TrueHeading = TractorPath[FrontScraperPathPointIndex].Heading;
            FrontGNSS.Altitude = TractorGNSS.Altitude;
            FrontGNSS.RTKQuality = TractorGNSS.RTKQuality;

            // update rear location
            RearGNSS.Latitude = TractorPath[RearScraperPathPointIndex].Latitude;
            RearGNSS.Longitude = TractorPath[RearScraperPathPointIndex].Longitude;
            RearGNSS.TrueHeading = TractorPath[RearScraperPathPointIndex].Heading;
            RearGNSS.Altitude = TractorGNSS.Altitude;
            RearGNSS.RTKQuality = TractorGNSS.RTKQuality;
        }

        public void SetTractorLocation
            (
            double Latitude,
            double Longitude,
            double Altitude,
            RTKQuality RTKQuality,
            bool CalcScraperInitialLocations
            )
        {
            this.TractorGNSS.Latitude = Latitude;
            this.TractorGNSS.Longitude = Longitude;
            this.TractorGNSS.Altitude = Altitude;
            this.TractorGNSS.RTKQuality = RTKQuality;

            if (CalcScraperInitialLocations)
            {
                double Lat = Latitude;
                double Lon = Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, 0, -TRACTOR_TO_FRONT_SCRAPER_M);
                FrontGNSS.Latitude = Lat;
                FrontGNSS.Longitude = Lon;
                FrontGNSS.Altitude = Altitude;
                FrontGNSS.RTKQuality = RTKQuality;

                Haversine.MoveDistanceBearing(ref Lat, ref Lon, 0, -FRONT_SCRAPER_TO_REAR_SCRAPER_M);
                RearGNSS.Latitude = Lat;
                RearGNSS.Longitude = Lon;
                RearGNSS.Altitude = Altitude;
                RearGNSS.RTKQuality = RTKQuality;

                // create path from tractor to rear scraper
                // the number of points in the path represents the total distance from tractor to rear scraper
                TractorPath.Clear();
                Lat = Latitude;
                Lon = Longitude;
                for (double m = TRACTOR_TO_FRONT_SCRAPER_M + FRONT_SCRAPER_TO_REAR_SCRAPER_M; m >= 0; m -= DIST_M_PER_MPH_PER_CALC_PERIOD)
                {
                    Lat = Latitude;
                    Lon = Longitude;

                    // if in the approximate middle of the path then this is where the front scraper is located
                    if ((m >= TRACTOR_TO_FRONT_SCRAPER_M - 0.1) && (m <= TRACTOR_TO_FRONT_SCRAPER_M + 0.1))
                    {
                        FrontScraperPathPointIndex = TractorPath.Count;
                    }

                    Haversine.MoveDistanceBearing(ref Lat, ref Lon, 0, -m);
                    TractorPath.Add(new PathPoint(Lat, Lon, 0));
                }
                // always at the end of the path
                RearScraperPathPointIndex = 0;
            }
        }

        public void SetFrontLocation
            (
            double Latitude,
            double Longitude,
            double Altitude,
            RTKQuality RTKQuality
            )
        {
            this.FrontGNSS.Latitude = Latitude;
            this.FrontGNSS.Longitude = Longitude;
            this.FrontGNSS.Altitude = Altitude;
            this.FrontGNSS.RTKQuality = RTKQuality;
        }

        public void SetRearLocation
            (
            double Latitude,
            double Longitude,
            double Altitude,
            RTKQuality RTKQuality
            )
        {
            this.RearGNSS.Latitude = Latitude;
            this.RearGNSS.Longitude = Longitude;
            this.RearGNSS.Altitude = Altitude;
            this.RearGNSS.RTKQuality = RTKQuality;
        }

        /// <summary>
        /// Gets an NMEA-0183 string represending the current heading and speed
        /// </summary>
        /// <param name="Data">Data to convert into string</param>
        /// <returns>GNVTG string</returns>
        private string GetNMEAGNVTGString
            (
            GNSSData Data
            )
        {
            // Magnetic declination: -1.43 degrees
            const double magneticDeclination = -1.483333;

            // Calculate magnetic heading from true heading
            double magHeading = Data.TrueHeading - magneticDeclination;
            
            // Normalize heading to 0-360 range
            while (magHeading < 0) magHeading += 360.0;
            while (magHeading >= 360) magHeading -= 360.0;
            
            // Normalize true heading to 0-360 range
            double trueHeadingNormalized = Data.TrueHeading;
            while (trueHeadingNormalized < 0) trueHeadingNormalized += 360.0;
            while (trueHeadingNormalized >= 360) trueHeadingNormalized -= 360.0;

            // Convert speed from kph to knots (1 knot = 1.852 kph)
            double Speedkph = Data.SpeedMPH * 1.60934;
            double SpeedKnots = Speedkph / 1.852;

            // Build the NMEA sentence without checksum
            // Format: $GNVTG,trueHeading,T,magneticHeading,M,speedKnots,N,speedKph,K,mode*checksum
            string sentence = string.Format("$GNVTG,{0:F2},T,{1:F2},M,{2:F2},N,{3:F2},K,D",
                trueHeadingNormalized,
                magHeading,
                SpeedKnots,
                Speedkph);

            // Calculate checksum (XOR of all characters between $ and *)
            byte checksum = 0;
            for (int i = 1; i < sentence.Length; i++)
            {
                checksum ^= (byte)sentence[i];
            }

            // Append checksum
            return sentence + "*" + checksum.ToString("X2");
        }

        /// <summary>
        /// Gets an NMEA-0183 string representing the current location, altitude and fix type
        /// </summary>
        /// <param name="Data">Data to convert into string</param>
        /// <returns>GNGGA string</returns>
        private string GetNMEAGNGGAString
            (
            GNSSData Data
            )
        {
            // Get current UTC time in HHMMSS.SS format
            DateTime utcNow = DateTime.UtcNow;
            string timeString = utcNow.ToString("HHmmss.ff");

            // Convert latitude from decimal degrees to DDMM.MMMMMM format
            double absLat = Math.Abs(Data.Latitude);
            int latDegrees = (int)absLat;
            double latMinutes = (absLat - latDegrees) * 60.0;
            string latString = string.Format("{0:00}{1:00.000000}", latDegrees, latMinutes);
            string latDirection = Data.Latitude >= 0 ? "N" : "S";

            // Convert longitude from decimal degrees to DDDMM.MMMMMM format
            double absLon = Math.Abs(Data.Longitude);
            int lonDegrees = (int)absLon;
            double lonMinutes = (absLon - lonDegrees) * 60.0;
            string lonString = string.Format("{0:000}{1:00.000000}", lonDegrees, lonMinutes);
            string lonDirection = Data.Longitude >= 0 ? "E" : "W";

            // Quality indicator (0-6, using TractorRTKQuality)
            int quality = (int)Data.RTKQuality;

            // Number of satellites (default to 12)
            int numSV = 12;

            // HDOP (Horizontal Dilution of Precision, default to 1.0)
            double hdop = 1.0;

            // Altitude in meters
            double altitude = Data.Altitude;

            // Geoid separation (default to 0.0)
            double geoidSep = 0.0;

            // Age of differential GPS data (empty if not used)
            string diffAge = "";

            // Reference station ID (empty if not used)
            string diffStation = "";

            // Build the NMEA sentence without checksum
            string sentence = string.Format("$GNGGA,{0},{1},{2},{3},{4},{5},{6},{7:F2},{8:F3},M,{9:F3},M,{10},{11}",
                timeString,
                latString,
                latDirection,
                lonString,
                lonDirection,
                quality,
                numSV,
                hdop,
                altitude,
                geoidSep,
                diffAge,
                diffStation);

            // Calculate checksum (XOR of all characters between $ and *)
            byte checksum = 0;
            for (int i = 1; i < sentence.Length; i++)
            {
                checksum ^= (byte)sentence[i];
            }

            // Append checksum
            return sentence + "*" + checksum.ToString("X2");
        }

        public void Start
            (
            )
        {
            GNSSUpdateTimer.Start();
            IMUUpdateTimer.Start();
            CalcTimer.Start();
            TurnTimer.Start();
        }

        public void IncreaseSpeed
            (
            )
        {
            // all equipment moves at the same speed
            TractorGNSS.SpeedMPH += 1;
            FrontGNSS.SpeedMPH += 1;
            RearGNSS.SpeedMPH += 1;
        }

        public void DecreaseSpeed
            (
            )
        {
            if (TractorGNSS.SpeedMPH > 0)
            {
                // all equipment moves at the same speed
                TractorGNSS.SpeedMPH -= 1;
                if (TractorGNSS.SpeedMPH < 0) TractorGNSS.SpeedMPH = 0;

                FrontGNSS.SpeedMPH -= 1;
                if (FrontGNSS.SpeedMPH < 0) FrontGNSS.SpeedMPH = 0;

                RearGNSS.SpeedMPH -= 1;
                if (RearGNSS.SpeedMPH < 0) RearGNSS.SpeedMPH = 0;
            }
        }

        public void TurnLeft
            (
            )
        {
            if (TurnHeadingChange > 0)
            {
                TurnHeadingChange = 0;
            }
            else
            {
                TurnHeadingChange -= 3;
            }
            StraightenSteeringCounter = STEERING_DAMPENING;
        }

        public void TurnRight
            (
            )
        {
            if (TurnHeadingChange < 0)
            {
                TurnHeadingChange = 0;
            }
            else
            {
                TurnHeadingChange += 3;
            }
            StraightenSteeringCounter = STEERING_DAMPENING;
        }

        private void TurnTimer_Tick(object? sender, EventArgs e)
        {
            // turn tractor - we will work out how the front and rear scrapers turn based on this
            TractorGNSS.TrueHeading += TurnHeadingChange;
            if (TractorGNSS.TrueHeading < 0) TractorGNSS.TrueHeading += 360;
            if (TractorGNSS.TrueHeading > 360) TractorGNSS.TrueHeading -= 360;

            if (--StraightenSteeringCounter == 0)
            {
                if (TurnHeadingChange > 0) TurnHeadingChange -= 1;
                if (TurnHeadingChange < 0) TurnHeadingChange += 1;

                StraightenSteeringCounter = STEERING_DAMPENING;
            }
        }
    }
}
