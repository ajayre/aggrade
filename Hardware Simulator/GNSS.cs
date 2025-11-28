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
        Fix = 1,
        DGPS = 2,
        RTKFix = 4,
        RTKFloat = 5
    }

    internal class GNSS
    {
        public double TractorLatitude { get; private set; }
        public double TractorLongitude { get; private set; }
        public double TractorAltitude { get; private set; }
        public RTKQuality TractorRTKQuality { get; private set; }

        public double TrueHeading {  get; private set; }
        public double SpeedMPH {  get; private set; }

        public event Action<string> OnNewTractorFix = null;
        public event Action<IMUValue> OnNewTractorIMU = null;

        private Timer GNSSUpdateTimer;
        private Timer IMUUpdateTimer;
        private IMUValue TractorIMU;

        public GNSS
            (
            )
        {
            GNSSUpdateTimer = new Timer();
            GNSSUpdateTimer.Interval = 1000;
            GNSSUpdateTimer.Tick += GNSSUpdateTimer_Tick;

            IMUUpdateTimer = new Timer();
            IMUUpdateTimer.Interval = 50;
            IMUUpdateTimer.Tick += IMUUpdateTimer_Tick;

            TractorIMU = new IMUValue();

            // fixme - remove
            TractorIMU.Pitch = 1;
            TractorIMU.Roll = 2;
            TractorIMU.Heading = 3;
            TractorIMU.YawRate = 4;
            TractorIMU.CalibrationStatus = IMUValue.Calibration.Adequate;

            SpeedMPH = 0;
            TrueHeading = 0;
        }

        private void IMUUpdateTimer_Tick(object? sender, EventArgs e)
        {
            OnNewTractorIMU?.Invoke(TractorIMU);
        }

        private void GNSSUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (SpeedMPH != 0)
            {
                GetNewLocation();
            }

            OnNewTractorFix?.Invoke(GetNMEAGNGGAString());
            OnNewTractorFix?.Invoke(GetNMEAGNVTGString());
        }

        private void GetNewLocation
            (
            )
        {
            // how far have we travelled since the last call?
            double MetersPerSec = SpeedMPH * 0.44704;

            double Lat = TractorLatitude;
            double Lon = TractorLongitude;
            Haversine.MoveDistanceBearing(ref Lat, ref Lon, TrueHeading, MetersPerSec);
            TractorLatitude = Lat;
            TractorLongitude = Lon;
        }

        public void SetTractorLocation
            (
            double Latitude,
            double Longitude,
            double Altitude,
            RTKQuality RTKQuality
            )
        {
            this.TractorLatitude = Latitude;
            this.TractorLongitude = Longitude;
            this.TractorAltitude = Altitude;
            this.TractorRTKQuality = RTKQuality;
        }

        /// <summary>
        /// Gets an NMEA-0183 string represending the current heading and speed
        /// </summary>
        /// <returns>GNVTG string</returns>
        private string GetNMEAGNVTGString
            (
            )
        {
            // Magnetic declination: -1.43 degrees
            const double magneticDeclination = -1.483333;

            // Calculate magnetic heading from true heading
            double magHeading = TrueHeading - magneticDeclination;
            
            // Normalize heading to 0-360 range
            while (magHeading < 0) magHeading += 360.0;
            while (magHeading >= 360) magHeading -= 360.0;
            
            // Normalize true heading to 0-360 range
            double trueHeadingNormalized = TrueHeading;
            while (trueHeadingNormalized < 0) trueHeadingNormalized += 360.0;
            while (trueHeadingNormalized >= 360) trueHeadingNormalized -= 360.0;

            // Convert speed from kph to knots (1 knot = 1.852 kph)
            double Speedkph = SpeedMPH * 1.60934;
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
        /// <returns>GNGGA string</returns>
        private string GetNMEAGNGGAString
            (
            )
        {
            // Get current UTC time in HHMMSS.SS format
            DateTime utcNow = DateTime.UtcNow;
            string timeString = utcNow.ToString("HHmmss.ff");

            // Convert latitude from decimal degrees to DDMM.MMMMMM format
            double absLat = Math.Abs(TractorLatitude);
            int latDegrees = (int)absLat;
            double latMinutes = (absLat - latDegrees) * 60.0;
            string latString = string.Format("{0:00}{1:00.000000}", latDegrees, latMinutes);
            string latDirection = TractorLatitude >= 0 ? "N" : "S";

            // Convert longitude from decimal degrees to DDDMM.MMMMMM format
            double absLon = Math.Abs(TractorLongitude);
            int lonDegrees = (int)absLon;
            double lonMinutes = (absLon - lonDegrees) * 60.0;
            string lonString = string.Format("{0:000}{1:00.000000}", lonDegrees, lonMinutes);
            string lonDirection = TractorLongitude >= 0 ? "E" : "W";

            // Quality indicator (0-6, using TractorRTKQuality)
            int quality = (int)TractorRTKQuality;

            // Number of satellites (default to 12)
            int numSV = 12;

            // HDOP (Horizontal Dilution of Precision, default to 1.0)
            double hdop = 1.0;

            // Altitude in meters
            double altitude = TractorAltitude;

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
        }

        public void IncreaseSpeed
            (
            )
        {
            SpeedMPH += 1;
        }

        public void DecreaseSpeed
            (
            )
        {
            SpeedMPH -= 1;
            if (SpeedMPH < 0) SpeedMPH = 0;
        }

        public void TurnLeft
            (
            )
        {
            TrueHeading -= 2;
            if (TrueHeading < 0) TrueHeading += 360;
            if (TrueHeading > 360) TrueHeading -= 360;
        }

        public void TurnRight
            (
            )
        {
            TrueHeading += 2;
            if (TrueHeading < 0) TrueHeading += 360;
            if (TrueHeading > 360) TrueHeading -= 360;
        }
    }
}
