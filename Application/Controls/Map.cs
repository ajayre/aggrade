using AgGrade.Controller;
using AgGrade.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        private const int DEFAULT_SCALE_FACTOR = 8;
        private const double ZOOM_FACTOR = 1.3;
        private const int MAX_NAME_LENGTH = 20;

        private const string DegreeSymbol = "°";

        private Field CurrentField;
        private MapGenerator MapGen;
        private GNSSFix TractorFix;
        private GNSSFix FrontScraperFix;
        private GNSSFix RearScraperFix;
        private Timer DeadReckoningTimer;
        private List<Coordinate> TractorLocationHistory = new List<Coordinate>();
        private Stopwatch PrecisionTractorFixTimer;
        private Timer RefreshTimer;

        // maximum number of tractor history points to keep
        private int MaxTractorHistoryLength = 500;

        /// <summary>
        /// pixels per meter
        /// </summary>
        public double ScaleFactor { get; private set; }

        private EquipmentSettings _CurrentEquipmentSettings;
        private AppSettings _CurrentAppSettings;

        public Map()
        {
            InitializeComponent();

            MapGen = new MapGenerator();
            MapGen.TractorColor = MapGenerator.TractorColors.Red;
            MapGen.TractorYOffset = 7;

            TractorFix = new GNSSFix();
            FrontScraperFix = new GNSSFix();
            RearScraperFix = new GNSSFix();

            ScaleFactor = DEFAULT_SCALE_FACTOR;

            FrontBladeDepthLabel.Text = "X mm";
            RearBladeDepthLabel.Text = "X mm";
            FrontLoadLabel.Text = "0 LCY";
            RearLoadLabel.Text = "0 LCY";
            HeadingLabel.Text = "0" + DegreeSymbol;
            SpeedLabel.Text = "0 MPH";
            FieldNameLabel.Text = "No Field";

            DeadReckoningTimer = new Timer();
            DeadReckoningTimer.Interval = DEAD_RECKONING_PERIOD_MS;
            DeadReckoningTimer.Tick += DeadReckoningTimer_Tick;

            PrecisionTractorFixTimer = new Stopwatch();
            PrecisionTractorFixTimer.Start();

            RefreshTimer = new Timer();
            RefreshTimer.Interval = 250;
            RefreshTimer.Tick += RefreshTimer_Tick;
            RefreshTimer.Start();
        }

        /// <summary>
        /// Called periodically to render the map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            RefreshMap();
        }

        public void SetEquipmentSettings
            (
            EquipmentSettings Settings
            )
        {
            _CurrentEquipmentSettings = Settings;
        }

        public void SetApplicationSettings
            (
            AppSettings Settings
            )
        {
            _CurrentAppSettings = Settings;
        }

        /// <summary>
        /// Gets the heading of the tractor
        /// </summary>
        /// <returns>Heading in degrees</returns>
        private double TractorHeading
            (
            )
        {
            return TractorFix.Vector.GetTrueHeading(_CurrentAppSettings.MagneticDeclinationDegrees, _CurrentAppSettings.MagneticDeclinationMinutes);
        }

        /// <summary>
        /// Gets the heading of the front scraper
        /// </summary>
        /// <returns>Heading in degrees</returns>
        private double FrontScraperHeading
            (
            )
        {
            return FrontScraperFix.Vector.GetTrueHeading(_CurrentAppSettings.MagneticDeclinationDegrees, _CurrentAppSettings.MagneticDeclinationMinutes);
        }

        /// <summary>
        /// Gets the heading of the rear scraper
        /// </summary>
        /// <returns>Heading in degrees</returns>
        private double RearScraperHeading
            (
            )
        {
            return RearScraperFix.Vector.GetTrueHeading(_CurrentAppSettings.MagneticDeclinationDegrees, _CurrentAppSettings.MagneticDeclinationMinutes);
        }

        /// <summary>
        /// Performs dead reckoning movement between GNSS data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeadReckoningTimer_Tick(object? sender, EventArgs e)
        {
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
                SetTractor(TractorFix);

                // move front scraper along current heading
                Lat = FrontScraperFix.Latitude;
                Lon = FrontScraperFix.Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, FrontScraperHeading(), DistanceMovedM);
                FrontScraperFix.Latitude = Lat;
                FrontScraperFix.Longitude = Lon;
                SetFrontScraper(FrontScraperFix);

                // move rear scraper along current heading
                Lat = RearScraperFix.Latitude;
                Lon = RearScraperFix.Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, RearScraperHeading(), DistanceMovedM);
                RearScraperFix.Latitude = Lat;
                RearScraperFix.Longitude = Lon;
                SetRearScraper(RearScraperFix);
            }
        }

        public void ShowField
            (
            Field Field
            )
        {
            CurrentField = Field;

            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading());

            FieldNameLabel.Text = Field.Name.Substring(0, Field.Name.Length > MAX_NAME_LENGTH ? MAX_NAME_LENGTH : Field.Name.Length);
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
            GNSSFix Fix
            )
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<GNSSFix>(SetTractor), Fix);
                return;
            }

            if (Fix.IsValid)
            {
                TractorFix = Fix.Clone();

                // if moving then update location history
                if (Fix.Vector.Speedkph > 0)
                {
                    TractorLocationHistory.Add(new Coordinate(Fix.Latitude, Fix.Longitude));
                    if (TractorLocationHistory.Count > MaxTractorHistoryLength)
                    {
                        TractorLocationHistory.RemoveAt(0);
                    }

                    // start dead reckoning
                    if (!DeadReckoningTimer.Enabled)
                    {
                        DeadReckoningTimer.Start();
                    }
                }
                else
                {
                    // not moving, stop dead reckoning
                    DeadReckoningTimer.Stop();
                }

                PrecisionTractorFixTimer.Restart();

                double TrueHeading = TractorFix.Vector.GetTrueHeading(_CurrentAppSettings.MagneticDeclinationDegrees, _CurrentAppSettings.MagneticDeclinationMinutes);
                if (TrueHeading >= 359.5) TrueHeading = 0;

                HeadingLabel.Text = TrueHeading.ToString("F1") + DegreeSymbol;
                SpeedLabel.Text = TractorFix.Vector.SpeedMph.ToString("F1") + " MPH";
            }
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
            GNSSFix Fix
            )
        {
            if (Fix.IsValid)
            {
                FrontScraperFix = Fix.Clone();
            }
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
            GNSSFix Fix
            )
        {
            if (Fix.IsValid)
            {
                RearScraperFix = Fix.Clone();
            }
        }

        private long LastPerf = 0;

        private void RefreshMap
            (
            )
        {
            // fixme - remove
            List<Benchmark> Benchmarks = new List<Benchmark>();
            Benchmarks.Add(new Benchmark(36.446857119955279, -90.72280187456794, "B1"));

            // fixme - remove debug code
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            MapCanvas.Image = MapGen.Generate(CurrentField, MapCanvas.Width, MapCanvas.Height, false, ScaleFactor,
                TractorFix, FrontScraperFix, RearScraperFix,
                Benchmarks, TractorLocationHistory, _CurrentEquipmentSettings, _CurrentAppSettings);
            //sw.Stop();
            //LastPerf = sw.ElapsedMilliseconds;
            //ShowPerf();
        }

        private void ShowPerf()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowPerf));
                return;
            }
            FieldNameLabel.Text = LastPerf.ToString();
        }

        /// <summary>
        /// Zooms the map to fit
        /// </summary>
        public void ZoomToFit
            (
            )
        {
            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading());
        }

        /// <summary>
        /// Zoom into the map
        /// </summary>
        public void ZoomIn
            (
            )
        {
            if (ScaleFactor * ZOOM_FACTOR <= MAX_SCALE_FACTOR)
            {
                ScaleFactor *= ZOOM_FACTOR;
            }
        }

        /// <summary>
        /// Zoom out of the map
        /// </summary>
        public void ZoomOut
            (
            )
        {
            if (ScaleFactor / ZOOM_FACTOR >= MIN_SCALE_FACTOR)
            {
                ScaleFactor /= ZOOM_FACTOR;
            }
        }

        private void MapCanvas_SizeChanged(object sender, EventArgs e)
        {
            if (_CurrentAppSettings != null)
            {
                if (CurrentField != null)
                {
                    ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading());
                }
            }
        }
    }
}
