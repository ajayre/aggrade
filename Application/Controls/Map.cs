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
        private List<Coordinate> TractorLocationHistory = new List<Coordinate>();
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
                }

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
