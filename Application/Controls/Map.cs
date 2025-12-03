using AgGrade.Controller;
using AgGrade.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AgGrade.Controls
{
    public partial class Map : UserControl
    {
        private const double MIN_SCALE_FACTOR = 0.5;
        private const double MAX_SCALE_FACTOR = 750.0;
        private const int DEAD_RECKONING_PERIOD_MS = 50;

        private Field CurrentField;
        private MapGenerator MapGen;
        private double TractorHeading;
        private double TractorSpeedkph;
        private Coordinate TractorLocation;
        private Coordinate FrontScraperLocation;
        private double FrontScraperHeading;
        private Coordinate RearScraperLocation;
        private double RearScraperHeading;
        private Timer DeadReckoningTimer;
        private DateTime LastLocationUpdate;
        private List<Coordinate> TractorLocationHistory = new List<Coordinate>();

        // maximum number of tractor history points to keep
        private int MaxTractorHistoryLength = 60;

        /// <summary>
        /// pixels per meter
        /// </summary>
        public double ScaleFactor { get; private set; }

        public Map()
        {
            InitializeComponent();

            MapGen = new MapGenerator();
            MapGen.TractorColor = MapGenerator.TractorColors.Red;
            MapGen.TractorYOffset = 7;

            TractorLocation = new Coordinate();
            FrontScraperLocation = new Coordinate();
            RearScraperLocation = new Coordinate();

            DeadReckoningTimer = new Timer();
            DeadReckoningTimer.Interval = DEAD_RECKONING_PERIOD_MS;
            DeadReckoningTimer.Tick += DeadReckoningTimer_Tick;
            //DeadReckoningTimer.Start();
        }

        /// <summary>
        /// Performs dead reckoning movement between GNSS data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeadReckoningTimer_Tick(object? sender, EventArgs e)
        {
            if (TractorSpeedkph > 0)
            {
                // calculate distance moved since last location update
                TimeSpan ElapsedTime = DateTime.Now - LastLocationUpdate;
                double ElapsedTimeMs = ElapsedTime.TotalMilliseconds;
                // Calculate distance: speed (kph) * time (ms) / 3600 (to convert kph*ms to meters)
                double DistanceMovedM = (TractorSpeedkph * ElapsedTimeMs) / 3600.0;

                // move tractor along current heading
                double Lat = TractorLocation.Latitude;
                double Lon = TractorLocation.Longitude;

                Haversine.MoveDistanceBearing(ref Lat, ref Lon, TractorHeading, DistanceMovedM);

                TractorLocation.Latitude = Lat;
                TractorLocation.Longitude = Lon;

                RefreshMap();

                LastLocationUpdate = DateTime.Now;
            }
        }

        public void ShowField
            (
            Field Field
            )
        {
            CurrentField = Field;

            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading);

            RefreshMap();
        }

        /// <summary>
        /// Sets the tractor location and heading
        /// </summary>
        /// <param name="Latitude">New latitude</param>
        /// <param name="Longitude">New longitude</param>
        /// <param name="Heading">New heading in degrees</param>
        /// <param name="Speedkph">Speed in kph</param>
        public void SetTractor
            (
            double Latitude,
            double Longitude,
            double Heading,
            double Speedkph
            )
        {
            TractorLocation.Latitude = Latitude;
            TractorLocation.Longitude = Longitude;
            TractorHeading = Heading;
            TractorSpeedkph = Speedkph;

            TractorLocationHistory.Add(new Coordinate(Latitude, Longitude));
            if (TractorLocationHistory.Count > MaxTractorHistoryLength)
            {
                TractorLocationHistory.RemoveAt(0);
            }

            RefreshMap();

            LastLocationUpdate = DateTime.Now;
        }

        /// <summary>
        /// Sets the front scraper location and heading
        /// </summary>
        /// <param name="Latitude">New latitude</param>
        /// <param name="Longitude">New longitude</param>
        /// <param name="Heading">New heading in degrees</param>
        /// <param name="Speedkph">Speed in kph</param>
        public void SetFrontScraper
            (
            double Latitude,
            double Longitude,
            double Heading
            )
        {
            FrontScraperLocation.Latitude = Latitude;
            FrontScraperLocation.Longitude = Longitude;
            FrontScraperHeading = Heading;

            RefreshMap();
        }

        /// <summary>
        /// Sets the rear scraper location and heading
        /// </summary>
        /// <param name="Latitude">New latitude</param>
        /// <param name="Longitude">New longitude</param>
        /// <param name="Heading">New heading in degrees</param>
        /// <param name="Speedkph">Speed in kph</param>
        public void SetRearScraper
            (
            double Latitude,
            double Longitude,
            double Heading
            )
        {
            RearScraperLocation.Latitude = Latitude;
            RearScraperLocation.Longitude = Longitude;
            RearScraperHeading = Heading;

            RefreshMap();
        }

        private void RefreshMap
            (
            )
        {
            List<Coordinate> Benchmarks = new List<Coordinate>();

            Coordinate B1 = new Coordinate();
            B1.Latitude = 36.446847109944279;
            B1.Longitude = -90.72286177445794;
            Benchmarks.Add(B1);

            MapCanvas.Image = MapGen.Generate(CurrentField, MapCanvas.Width, MapCanvas.Height, false, ScaleFactor,
                TractorLocation.Latitude, TractorLocation.Longitude, TractorHeading,
                FrontScraperLocation, FrontScraperHeading,
                RearScraperLocation, RearScraperHeading, Benchmarks,
                TractorLocationHistory);
        }

        /// <summary>
        /// Zooms the map to fit
        /// </summary>
        public void ZoomToFit
            (
            )
        {
            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading);

            RefreshMap();
        }

        /// <summary>
        /// Zoom into the map
        /// </summary>
        public void ZoomIn
            (
            )
        {
            if (ScaleFactor * 2 <= MAX_SCALE_FACTOR)
            {
                ScaleFactor *= 2;
            }
            RefreshMap();
        }

        /// <summary>
        /// Zoom out of the map
        /// </summary>
        public void ZoomOut
            (
            )
        {
            if (ScaleFactor / 2 >= MIN_SCALE_FACTOR)
            {
                ScaleFactor /= 2;
            }
            RefreshMap();
        }

        private void MapCanvas_SizeChanged(object sender, EventArgs e)
        {
            if (CurrentField != null)
            {
                ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading);
                RefreshMap();
            }
        }
    }
}
