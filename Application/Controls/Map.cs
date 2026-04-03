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
        private const int DEFAULT_SCALE_FACTOR = 13;
        private const double ZOOM_FACTOR = 1.3;
        private const int MAX_NAME_LENGTH = 30;

        private const string DegreeSymbol = "°";

        private Field? CurrentField;
        private Survey? CurrentSurvey;
        private MapGenerator MapGen;
        private GNSSFix TractorFix;
        private GNSSFix FrontScraperFix;
        private GNSSFix RearScraperFix;
        private List<Coordinate> TractorLocationHistory = new List<Coordinate>();
        private Timer RefreshTimer;
        private FlowMapGenerator.ElevationTypes SurfaceFlowElevationType = FlowMapGenerator.ElevationTypes.Current;
        private FlowMapGenerator.ElevationTypes PondingElevationType = FlowMapGenerator.ElevationTypes.Current;
        private List<Coordinate> HaulPath = new List<Coordinate>();
        private bool FirstRender;

        private static bool ShowHaulArrows;
        private static bool ShowSurfaceFlow;
        private static bool ShowPonding;
        private static bool ShowBenchmarks;
        private static bool ShowSurveyCoverage;
        private static bool ShowSatelliteBasemap;
        private static MapGenerator.TractorStyles TractorStyle;
        private static MapGenerator.MapTypes MapType;
        private BoundaryModes BoundaryMode;
        private bool SurveyRecording;

        public enum BoundaryModes
        {
            None,
            Left,
            Right
        }

        /// <summary>
        /// pixels per meter
        /// </summary>
        public static double ScaleFactor { get; private set; }

        // maximum number of tractor history points to keep
        private int MaxTractorHistoryLength = 500;

        private EquipmentSettings _CurrentEquipmentSettings;
        private AppSettings _CurrentAppSettings;
        private EquipmentStatus _CurrentEquipmentStatus;

        public delegate void ResetPanLoad(bool Front);
        public event ResetPanLoad OnResetPanLoad = null;

        public delegate void StartSurveying(BoundaryModes Mode);
        public delegate void SurveyBoundaryChanged(BoundaryModes Mode);
        public event StartSurveying OnStartSurveying = null;
        public event Action OnStopSurveying = null;
        public event SurveyBoundaryChanged OnSurveyBoundaryChanged = null;
        public event Action OnAddBenchmark = null;

        public Color FrontPanColor = Color.Black;
        public Color RearPanColor = Color.Black;

        static Map()
        {
            ScaleFactor = DEFAULT_SCALE_FACTOR;

            ShowHaulArrows = true;
            MapType = MapGenerator.MapTypes.CutFill;
            TractorStyle = MapGenerator.TractorStyles.Arrow;
            ShowSurfaceFlow = false;
            ShowPonding = false;

            ShowBenchmarks = true;
            ShowSatelliteBasemap = false;
            ShowSurveyCoverage = true;
        }

        public Map()
        {
            InitializeComponent();

            MapGen = new MapGenerator();
            MapGen.TractorYOffset = 5;
            MapGen.TractorStyle = MapGenerator.TractorStyles.Arrow;

            TractorFix = new GNSSFix();
            FrontScraperFix = new GNSSFix();
            RearScraperFix = new GNSSFix();

            FrontBladeHeightLabel.Text = "X mm";
            RearBladeHeightLabel.Text = "X mm";
            FrontLoadLabel.Text = "0.0 LCY";
            RearLoadLabel.Text = "0.0 LCY";
            HeadingLabel.Text = "0" + DegreeSymbol;
            SpeedLabel.Text = "0 MPH";
            FieldNameLabel.Text = "";

            FirstRender = true;

            BoundaryMode = BoundaryModes.None;
            SurveyRecording = false;

            ShowBasicUI();

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

            switch (_CurrentAppSettings.TractorColor)
            {
                case AppSettings.TractorColors.Red: MapGen.TractorColor = MapGenerator.TractorColors.Red; break;
                default:
                case AppSettings.TractorColors.Green: MapGen.TractorColor = MapGenerator.TractorColors.Green; break;
                case AppSettings.TractorColors.Blue: MapGen.TractorColor = MapGenerator.TractorColors.Blue; break;
                case AppSettings.TractorColors.Yellow: MapGen.TractorColor = MapGenerator.TractorColors.Yellow; break;
            }
        }

        public void SetEquipmentStatus
            (
            EquipmentStatus Status
            )
        {
            _CurrentEquipmentStatus = Status;
        }

        public void SetBasemapDataFolder
            (
            string? basemapDataFolder
            )
        {
            MapGen.BasemapDataFolderPath = basemapDataFolder;
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
        /// Shows a field on the map
        /// </summary>
        /// <param name="Field">Field to show or null for no field</param>
        public void ShowField
            (
            Field? Field
            )
        {
            CurrentSurvey = null;
            CurrentField = Field;

            if (CurrentField != null)
            {
                FieldNameLabel.Text = Field!.Name.Substring(0, Field.Name.Length > MAX_NAME_LENGTH ? MAX_NAME_LENGTH : Field.Name.Length);
                ShowFieldUI();
                FirstRender = true;
            }
            else
            {
                FieldNameLabel.Text = "";
                ShowBasicUI();
            }
        }

        /// <summary>
        /// Shows a survey on the map
        /// </summary>
        /// <param name="Survey">Survey to show or null for no survey</param>
        public void ShowSurvey
            (
            Survey? Survey
            )
        {
            CurrentField = null;
            CurrentSurvey = Survey;

            if (CurrentSurvey != null)
            {
                FieldNameLabel.Text = Survey!.Name.Substring(0, Survey.Name.Length > MAX_NAME_LENGTH ? MAX_NAME_LENGTH : Survey.Name.Length);
                ShowSurveyUI();
                FirstRender = true;

                BoundaryMode = BoundaryModes.None;
                ShowBoundaryMode(BoundaryMode);

                SurveyRecording = false;
                ShowRecordingMode(SurveyRecording);
            }
            else
            {
                FieldNameLabel.Text = "";
                ShowBasicUI();
            }
        }

        /// <summary>
        /// Shows a survey recording state in the UI
        /// </summary>
        /// <param name="IsRecording">Recording state to show</param>
        private void ShowRecordingMode
            (
            bool IsRecording
            )
        {
            if (IsRecording)
            {
                StartStopSurveyBtn.Image = Properties.Resources.stop_48px;
            }
            else
            {
                StartStopSurveyBtn.Image = Properties.Resources.start_48px;
            }
        }

        /// <summary>
        /// Shows a boundary mode in the UI
        /// </summary>
        /// <param name="Mode">The boundary mode to show</param>
        private void ShowBoundaryMode
            (
            BoundaryModes Mode
            )
        {
            switch (Mode)
            {
                case BoundaryModes.None:
                    BoundaryLeftBtn.Image = Properties.Resources.boundary_left48px;
                    BoundaryRightBtn.Image = Properties.Resources.boundary_right48px;
                    break;

                case BoundaryModes.Left:
                    BoundaryLeftBtn.Image = Properties.Resources.boundary_left_selected48px;
                    BoundaryRightBtn.Image = Properties.Resources.boundary_right48px;
                    break;

                case BoundaryModes.Right:
                    BoundaryLeftBtn.Image = Properties.Resources.boundary_left48px;
                    BoundaryRightBtn.Image = Properties.Resources.boundary_right_selected48px;
                    break;
            }
        }

        /// <summary>
        /// Shows the UI for surveying
        /// </summary>
        private void ShowSurveyUI
            (
            )
        {
            FrontBladeHeightLabel.Visible = false;
            FrontLoadLabel.Visible = false;
            RearBladeHeightLabel.Visible = false;
            RearLoadLabel.Visible = false;

            ElevationMapBtn.Visible = false;
            CutFillMapBtn.Visible = false;
            FlowBtn.Visible = false;
            PondingBtn.Visible = false;

            ToggleHaulArrowsBtn.Visible = false;
            BenchmarkBtn.Visible = false;

            AddBenchmarkBtn.Visible = true;
            ToggleSurveyCoverageBtn.Visible = true;
            StartStopSurveyBtn.Visible = true;
            BoundaryLeftBtn.Visible = true;
            BoundaryRightBtn.Visible = true;
        }

        /// <summary>
        /// Shows the UI for fields
        /// </summary>
        private void ShowFieldUI
            (
            )
        {
            FrontBladeHeightLabel.Visible = true;
            FrontLoadLabel.Visible = true;
            RearBladeHeightLabel.Visible = true;
            RearLoadLabel.Visible = true;

            ElevationMapBtn.Visible = true;
            CutFillMapBtn.Visible = true;
            FlowBtn.Visible = true;
            PondingBtn.Visible = true;

            ToggleHaulArrowsBtn.Visible = true;
            BenchmarkBtn.Visible = true;

            AddBenchmarkBtn.Visible = false;
            ToggleSurveyCoverageBtn.Visible = false;
            StartStopSurveyBtn.Visible = false;
            BoundaryLeftBtn.Visible = false;
            BoundaryRightBtn.Visible = false;
        }

        /// <summary>
        /// Shows a basic UI for when no field and no survey are loaded
        /// </summary>
        private void ShowBasicUI
            (
            )
        {
            FrontBladeHeightLabel.Visible = false;
            FrontLoadLabel.Visible = false;
            RearBladeHeightLabel.Visible = false;
            RearLoadLabel.Visible = false;

            ElevationMapBtn.Visible = false;
            CutFillMapBtn.Visible = false;
            FlowBtn.Visible = false;
            PondingBtn.Visible = false;

            ToggleHaulArrowsBtn.Visible = false;
            BenchmarkBtn.Visible = false;

            AddBenchmarkBtn.Visible = false;
            ToggleSurveyCoverageBtn.Visible = false;
            StartStopSurveyBtn.Visible = false;
            BoundaryLeftBtn.Visible = false;
            BoundaryRightBtn.Visible = false;
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

#if SHOW_MAP_PERF
        private long LastPerf = 0;
#endif

        private void RefreshMap
            (
            )
        {
#if SHOW_MAP_PERF
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            MapCanvas.Image = MapGen.Generate(CurrentField, CurrentSurvey, MapCanvas.Width, MapCanvas.Height, false, ScaleFactor,
                TractorFix, FrontScraperFix, RearScraperFix, TractorLocationHistory, _CurrentEquipmentSettings, _CurrentAppSettings,
                ShowHaulArrows, MapType, TractorStyle, HaulPath, ShowSurfaceFlow, ShowPonding, ShowBenchmarks, ShowSatelliteBasemap,
                ShowSurveyCoverage);

#if SHOW_MAP_PERF
            sw.Stop();
            LastPerf = sw.ElapsedMilliseconds;
            ShowPerf();
#endif

            if (CurrentField != null)
            {
                FrontBladeHeightLabel.Text = _CurrentEquipmentStatus.FrontPan.BladeHeight.ToString() + " mm";
                RearBladeHeightLabel.Text = _CurrentEquipmentStatus.RearPan.BladeHeight.ToString() + " mm";

                FrontLoadLabel.Text = _CurrentEquipmentStatus.FrontPan.LoadLCY.ToString("F1") + " LCY";
                RearLoadLabel.Text = _CurrentEquipmentStatus.RearPan.LoadLCY.ToString("F1") + " LCY";
            }

            double TrueHeading = TractorFix.Vector.GetTrueHeading(_CurrentAppSettings.MagneticDeclinationDegrees, _CurrentAppSettings.MagneticDeclinationMinutes);
            if (TrueHeading >= 359.5) TrueHeading = 0;

            HeadingLabel.Text = TrueHeading.ToString("F1") + DegreeSymbol;
            SpeedLabel.Text = TractorFix.Vector.SpeedMph.ToString("F1") + " MPH";

            if (FirstRender)
            {
                FirstRender = false;

                if (CurrentField != null)
                {
                    if (ShowPonding)
                    {
                        ShowPondingMap();
                    }

                    if (ShowSurfaceFlow)
                    {
                        ShowSurfaceFlowMap();
                    }
                }
            }
        }

#if SHOW_MAP_PERF
        private void ShowPerf()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowPerf));
                return;
            }
            FieldNameLabel.Text = LastPerf.ToString();
        }
#endif

        /// <summary>
        /// Called when the front starts cutting
        /// </summary>
        public void StartFrontCutting
            (
            )
        {
            Bin? CurrentBin = CurrentField.LatLonToBin(_CurrentEquipmentStatus.FrontPan.Fix.Latitude, _CurrentEquipmentStatus.FrontPan.Fix.Longitude);
            if ((CurrentBin != null) && (CurrentBin.HaulPath != 0))
            {
                HaulPath = CurrentField.GetHaulPath(CurrentBin);
            }
            else
            {
                HaulPath = new List<Coordinate>();
            }
        }

        /// <summary>
        /// Called when the rear starts cutting
        /// </summary>
        public void StartRearCutting
            (
            )
        {
            Bin? CurrentBin = CurrentField.LatLonToBin(_CurrentEquipmentStatus.RearPan.Fix.Latitude, _CurrentEquipmentStatus.RearPan.Fix.Longitude);
            if ((CurrentBin != null) && (CurrentBin.HaulPath != 0))
            {
                HaulPath = CurrentField.GetHaulPath(CurrentBin);
            }
            else
            {
                HaulPath = new List<Coordinate>();
            }
        }

        /// <summary>
        /// Called when the front starts filling
        /// </summary>
        public void StartFrontFilling
            (
            )
        {
        }

        /// <summary>
        /// Called when the rear starts filling
        /// </summary>
        public void StartRearFilling
            (
            )
        {
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

        /// <summary>
        /// Called when the user taps on the front load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrontLoadLabel_Click(object sender, EventArgs e)
        {
            OnResetPanLoad?.Invoke(true);
        }

        /// <summary>
        /// Called when the user taps on the rear load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RearLoadLabel_Click(object sender, EventArgs e)
        {
            OnResetPanLoad?.Invoke(false);
        }

        /// <summary>
        /// Called when user taps on the button to toggle display of the haul arrows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleHaulArrowsBtn_Click(object sender, EventArgs e)
        {
            ShowHaulArrows = !ShowHaulArrows;
        }

        /// <summary>
        /// Called when user taps on the button to show the cut/fill map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CutFillMapBtn_Click(object sender, EventArgs e)
        {
            MapType = MapGenerator.MapTypes.CutFill;
        }

        /// <summary>
        /// Called when user taps on the button to show the elevation map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ElevationMapBtn_Click(object sender, EventArgs e)
        {
            MapType = MapGenerator.MapTypes.Elevation;
        }

        /// <summary>
        /// Called when user taps on the tractor style button
        /// Changes the style of the tractor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TractorStyleBtn_Click(object sender, EventArgs e)
        {
            if (TractorStyle == MapGenerator.TractorStyles.Arrow)
            {
                TractorStyle = MapGenerator.TractorStyles.Dot;
            }
            else
            {
                TractorStyle = MapGenerator.TractorStyles.Arrow;
            }
        }

        /// <summary>
        /// Called when user clicks on the button to show the surface water flow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlowBtn_Click(object sender, EventArgs e)
        {
            ShowSurfaceFlow = !ShowSurfaceFlow;
            if (ShowSurfaceFlow)
            {
                ShowSurfaceFlowMap();
            }
        }

        /// <summary>
        /// Shows the surface flow map
        /// </summary>
        private void ShowSurfaceFlowMap
            (
            )
        {
            SurfaceFlowElevationType = FlowMapGenerator.ElevationTypes.Current;
            MapGen.CalculateSurfaceFlow(SurfaceFlowElevationType);
        }

        /// <summary>
        /// Toggles display of benchmarks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BenchmarkBtn_Click(object sender, EventArgs e)
        {
            ShowBenchmarks = !ShowBenchmarks;
        }

        /// <summary>
        /// Toggles display of satellite images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SatelliteBtn_Click(object sender, EventArgs e)
        {
            ShowSatelliteBasemap = !ShowSatelliteBasemap;
        }

        /// <summary>
        /// Toggles display of ponding
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PondingBtn_Click(object sender, EventArgs e)
        {
            ShowPonding = !ShowPonding;
            if (ShowPonding)
            {
                ShowPondingMap();
            }
        }

        /// <summary>
        /// Shows the ponding map
        /// </summary>
        private void ShowPondingMap
            (
            )
        {
            PondingElevationType = FlowMapGenerator.ElevationTypes.Current;
            double curveNumber = _CurrentAppSettings?.PondingCurveNumber ?? 85;
            double rainfallMm = _CurrentAppSettings?.PondingRainfallMm ?? 50;
            MapGen.CalculatePonding(PondingElevationType, curveNumber, rainfallMm, 50);
        }

        private void Map_Load(object sender, EventArgs e)
        {
        }

        private void Map_VisibleChanged(object sender, EventArgs e)
        {
            FrontBladeHeightLabel.ForeColor = FrontPanColor;
            FrontLoadLabel.ForeColor = FrontPanColor;
            RearBladeHeightLabel.ForeColor = RearPanColor;
            RearLoadLabel.ForeColor = RearPanColor;
        }

        /// <summary>
        /// Called when user taps button to add a benchmark to the current survey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddBenchmarkBtn_Click(object sender, EventArgs e)
        {
            OnAddBenchmark?.Invoke();
        }

        /// <summary>
        /// Called when user taps on the left boundary button
        /// Toggles the setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoundaryLeftBtn_Click(object sender, EventArgs e)
        {
            if (BoundaryMode == BoundaryModes.Left)
            {
                BoundaryMode = BoundaryModes.None;
            }
            else
            {
                BoundaryMode = BoundaryModes.Left;
            }

            ShowBoundaryMode(BoundaryMode);

            OnSurveyBoundaryChanged?.Invoke(BoundaryMode);
        }

        /// <summary>
        /// Called when user taps on the right boundary button
        /// Toggles the setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoundaryRightBtn_Click(object sender, EventArgs e)
        {
            if (BoundaryMode == BoundaryModes.Right)
            {
                BoundaryMode = BoundaryModes.None;
            }
            else
            {
                BoundaryMode = BoundaryModes.Right;
            }

            ShowBoundaryMode(BoundaryMode);
            OnSurveyBoundaryChanged?.Invoke(BoundaryMode);
        }

        /// <summary>
        /// Called when user taps on the button to start and stop surveying
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartStopSurveyBtn_Click(object sender, EventArgs e)
        {
            SurveyRecording = !SurveyRecording;
            ShowRecordingMode(SurveyRecording);

            if (SurveyRecording)
            {
                OnStartSurveying?.Invoke(BoundaryMode);
            }
            else
            {
                OnStopSurveying?.Invoke();
            }
        }

        private void ToggleSurveyCoverageBtn_Click(object sender, EventArgs e)
        {
            ShowSurveyCoverage = !ShowSurveyCoverage;
        }
    }
}
