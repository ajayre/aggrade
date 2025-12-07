using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace AgGrade.Data
{
    public class DeadReckoner
    {
        private const int DEAD_RECKONING_PERIOD_MS = 50;

        private Timer DeadReckoningTimer;
        private Stopwatch PrecisionTractorFixTimer;
        private GNSSFix? TractorFix;
        private GNSSFix? FrontScraperFix;
        private GNSSFix? RearScraperFix;
        private AppSettings CurrentAppSettings;

        public delegate void NewLocations(GNSSFix TractorFix, GNSSFix FrontScraperFix, GNSSFix RearScraperFix);
        public event NewLocations OnNewLocations = null;

        public DeadReckoner
            (
            )
        {
            TractorFix = null;
            FrontScraperFix = null;
            RearScraperFix = null;

            DeadReckoningTimer = new Timer();
            DeadReckoningTimer.Interval = DEAD_RECKONING_PERIOD_MS;
            DeadReckoningTimer.Tick += DeadReckoningTimer_Tick;

            PrecisionTractorFixTimer = new Stopwatch();
        }

        /// <summary>
        /// Sets the application settings
        /// </summary>
        /// <param name="Settings">New settings</param>
        public void SetApplicationSettings
            (
            AppSettings Settings
            )
        {
            CurrentAppSettings = Settings;
        }

        /// <summary>
        /// Starts dead reckoning
        /// </summary>
        public void Start
            (
            )
        {
            PrecisionTractorFixTimer.Start();
            DeadReckoningTimer.Start();
        }

        /// <summary>
        /// Stops dead reckoning
        /// </summary>
        public void Stop
            (
            )
        {
            DeadReckoningTimer.Stop();
            PrecisionTractorFixTimer.Stop();

            TractorFix = null;
            FrontScraperFix = null;
            RearScraperFix = null;
        }

        /// <summary>
        /// Sets the tractor location
        /// </summary>
        /// <param name="Fix">Current tractor location</param>
        public void SetTractor
            (
            GNSSFix Fix
            )
        {
            TractorFix = Fix.Clone();

            PrecisionTractorFixTimer.Restart();
        }

        /// <summary>
        /// Sets the front scraper location
        /// </summary>
        /// <param name="Fix">Current front scraper location</param>
        public void SetFrontScraper
            (
            GNSSFix Fix
            )
        {
            FrontScraperFix = Fix.Clone();
        }

        /// <summary>
        /// Sets the rear scraper location
        /// </summary>
        /// <param name="Fix">Current rear scraper location</param>
        public void SetRearScraper
            (
            GNSSFix Fix
            )
        {
            RearScraperFix = Fix.Clone();
        }

        /// <summary>
        /// Performs dead reckoning movement between GNSS data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeadReckoningTimer_Tick
            (
            object? sender,
            EventArgs e
            )
        {
            // need at least one fix from all three pieces of equipment
            if ((TractorFix == null) || (FrontScraperFix == null) || (RearScraperFix == null)) return;

            if (TractorFix.Vector.Speedkph > 0)
            {
                // get time since last tractor fix
                long ElapsedMilliseconds = PrecisionTractorFixTimer.ElapsedMilliseconds;

                // calculate distance moved since last location update
                // Calculate distance: speed (kph) * time (ms) / 3600 (to convert kph*ms to meters)
                double DistanceMovedM = (TractorFix.Vector.Speedkph * ElapsedMilliseconds) / 3600.0;

                // move tractor along current heading
                double Lat = TractorFix.Latitude;
                double Lon = TractorFix.Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, TractorHeading(), DistanceMovedM);
                TractorFix.Latitude = Lat;
                TractorFix.Longitude = Lon;

                // move front scraper along current heading
                Lat = FrontScraperFix.Latitude;
                Lon = FrontScraperFix.Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, FrontScraperHeading(), DistanceMovedM);
                FrontScraperFix.Latitude = Lat;
                FrontScraperFix.Longitude = Lon;

                // move rear scraper along current heading
                Lat = RearScraperFix.Latitude;
                Lon = RearScraperFix.Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, RearScraperHeading(), DistanceMovedM);
                RearScraperFix.Latitude = Lat;
                RearScraperFix.Longitude = Lon;

                OnNewLocations?.Invoke(TractorFix, FrontScraperFix, RearScraperFix);

                PrecisionTractorFixTimer.Restart();
            }
        }

        /// <summary>
        /// Gets the heading of the tractor
        /// </summary>
        /// <returns>Heading in degrees</returns>
        private double TractorHeading
            (
            )
        {
            if (TractorFix != null)
            {
                return TractorFix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);
            }

            return 0;
        }

        /// <summary>
        /// Gets the heading of the front scraper
        /// </summary>
        /// <returns>Heading in degrees</returns>
        private double FrontScraperHeading
            (
            )
        {
            if (FrontScraperFix != null)
            {
                return FrontScraperFix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);
            }

            return 0;
        }

        /// <summary>
        /// Gets the heading of the rear scraper
        /// </summary>
        /// <returns>Heading in degrees</returns>
        private double RearScraperHeading
            (
            )
        {
            if (RearScraperFix != null)
            {
                return RearScraperFix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);
            }

            return 0;
        }
    }
}
