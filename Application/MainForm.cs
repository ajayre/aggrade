using AgGrade.Controller;
using AgGrade.Controls;
using AgGrade.Data;
using AgGrade.Properties;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Media;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AgGrade
{
    public partial class MainForm : Form
    {
        // how often to attempt to connect to the controller
        private const int CONTROLLER_TRY_CONNECT_PERIOD_MS = 200;

        // how often to send fixes to the field
        private const int FIX_PERIOD_MS = 250;

        static Color FRONT_PAN_COLOR = Color.RoyalBlue;
        static Color REAR_PAN_COLOR = Color.DarkGoldenrod;

        private AppSettings CurrentAppSettings;
        private EquipmentSettings CurrentEquipmentSettings;
        private EquipmentStatus CurrentEquipmentStatus;
        private OGController Controller;
        private bool TractorIMUFound;
        private bool FrontIMUFound;
        private bool FrontApronIMUFound;
        private bool FrontBucketIMUFound;
        private bool RearIMUFound;
        private bool RearBucketIMUFound;
        private bool FrontHeightFound;
        private bool RearHeightFound;
        private Timer ControllerConnectTimer;
        private BladeController BladeCtrl;
        private Field? CurrentField;
        private Survey? CurrentSurvey;
        private FieldUpdater FieldUpdater;
        private SurveyUpdater SurveyUpdater;
        private string DataFolder;
        private string FieldDataFolder;
        private string SurveyDataFolder;
        private string BasemapDataFolder;
        private Timer FixTimer;
        private bool EnableBladeLimits;

        /// <summary>
        /// Possible states for the pan indicators
        /// </summary>
        private enum PanIndicatorStates
        {
            None,
            Cutting,
            Filling,
            Transport
        }

        public MainForm
            (
            bool Windowed
            )
        {
            InitializeComponent();

            if (!Windowed)
            {
                // Set the form to not have a border, allowing it to cover the entire screen
                this.FormBorderStyle = FormBorderStyle.None;
                // Specify that the form's position and size will be set manually
                this.StartPosition = FormStartPosition.Manual;
                // Set the form's location to the top-left corner of the primary screen
                this.Location = Screen.PrimaryScreen!.Bounds.Location;
                // Set the form's size to the full resolution of the primary screen
                this.Size = Screen.PrimaryScreen.Bounds.Size;
            }

            double ScalingFactor = this.DeviceDpi / 96.0;

            Bitmap scaledImage = new Bitmap(EditEquipmentBtn.Image!, new Size((int)(EditEquipmentBtn.Image!.Width * ScalingFactor), (int)(EditEquipmentBtn.Image!.Height * ScalingFactor)));
            EditEquipmentBtn.Image = scaledImage;

            scaledImage = new Bitmap(EditSettingsBtn.Image!, new Size((int)(EditSettingsBtn.Image!.Width * ScalingFactor), (int)(EditSettingsBtn.Image!.Height * ScalingFactor)));
            EditSettingsBtn.Image = scaledImage;

            scaledImage = new Bitmap(CalibrationBtn.Image!, new Size((int)(CalibrationBtn.Image!.Width * ScalingFactor), (int)(CalibrationBtn.Image!.Height * ScalingFactor)));
            CalibrationBtn.Image = scaledImage;

            scaledImage = new Bitmap(SurveyBtn.Image!, new Size((int)(SurveyBtn.Image!.Width * ScalingFactor), (int)(SurveyBtn.Image!.Height * ScalingFactor)));
            SurveyBtn.Image = scaledImage;

            scaledImage = new Bitmap(MapBtn.Image!, new Size((int)(MapBtn.Image!.Width * ScalingFactor), (int)(MapBtn.Image!.Height * ScalingFactor)));
            MapBtn.Image = scaledImage;

            scaledImage = new Bitmap(ZoomInBtn.Image!, new Size((int)(ZoomInBtn.Image!.Width * ScalingFactor), (int)(ZoomInBtn.Image!.Height * ScalingFactor)));
            ZoomInBtn.Image = scaledImage;

            scaledImage = new Bitmap(ZoomOutBtn.Image!, new Size((int)(ZoomOutBtn.Image!.Width * ScalingFactor), (int)(ZoomOutBtn.Image!.Height * ScalingFactor)));
            ZoomOutBtn.Image = scaledImage;

            CurrentAppSettings = new AppSettings();
            CurrentAppSettings.Load();

            CurrentEquipmentSettings = new EquipmentSettings();
            CurrentEquipmentSettings.Load();

            CurrentEquipmentStatus = new EquipmentStatus();

            Controller = new OGController();

            FieldUpdater = new FieldUpdater();
            FieldUpdater.SetEquipmentStatus(CurrentEquipmentStatus);
            FieldUpdater.SetApplicationSettings(CurrentAppSettings);
            FieldUpdater.OnFrontVolumeCutUpdated += FieldUpdater_OnFrontVolumeCutUpdated;
            FieldUpdater.OnRearVolumeCutUpdated += FieldUpdater_OnRearVolumeCutUpdated;

            SurveyUpdater = new SurveyUpdater();
            SurveyUpdater.SetEquipmentStatus(CurrentEquipmentStatus);

            BladeCtrl = new BladeController(Controller);
            BladeCtrl.SetEquipmentStatus(CurrentEquipmentStatus);
            BladeCtrl.OnFrontStoppedCutting += BladeCtrl_OnFrontStoppedCutting;
            BladeCtrl.OnRearStoppedCutting += BladeCtrl_OnRearStoppedCutting;
            BladeCtrl.OnRequestRearBladeStartCutting += BladeCtrl_OnRequestRearBladeStartCutting;

            TractorIMUFound = false;
            FrontIMUFound = false;
            FrontApronIMUFound = false;
            FrontBucketIMUFound = false;
            RearIMUFound = false;
            RearBucketIMUFound = false;

            FrontHeightFound = false;
            RearHeightFound = false;

            CurrentField = null;
            CurrentSurvey = null;

            FixTimer = new Timer();
            FixTimer.Interval = FIX_PERIOD_MS;
            FixTimer.Tick += FixTimer_Tick;

            FrontPanIndicator.BackColor = FRONT_PAN_COLOR;
            RearPanIndicator.BackColor = REAR_PAN_COLOR;

            EnableBladeLimits = true;

            // fixme - remove
            /*FieldDesign Design = new FieldDesign();
            Design.SurveyFileName = @"C:\Users\andy\OneDrive\Documents\AgGrade\Application\SurveyData\Shop.txt";
            Design.MainSlope = 0.1;
            Design.MainSlopeDirection = 270;
            Design.CrossSlope = 0;
            Design.CutFillRatio = 1.2;
            Design.ImportToField = 0;
            Design.ExportFromField = 0;
            var fieldCreator = new FieldCreator(msg => System.Diagnostics.Debug.WriteLine(msg));
            FieldCreator.Statistics Stats = fieldCreator.CreateFromSurveyAndDesign(Design, @"C:\Users\andy\OneDrive\Documents\AgGrade\Application\FieldData\Custom\Custom.db");*/

            ShowMap();
        }

        /// <summary>
        /// Called periodically to give fixes to the current field or survey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FixTimer_Tick(object? sender, EventArgs e)
        {
            if (CurrentField != null)
            {
                CurrentField.SetTractorFix(CurrentEquipmentStatus.TractorFix);
                if (CurrentEquipmentSettings.FrontPan.Equipped)
                {
                    CurrentField.SetFrontScraperFix(CurrentEquipmentStatus.FrontPan.Fix);
                }
                if (CurrentEquipmentSettings.RearPan.Equipped)
                {
                    CurrentField.SetRearScraperFix(CurrentEquipmentStatus.RearPan.Fix);
                }
            }

            if (CurrentSurvey != null)
            {
                // fixme - to do
            }
        }

        /// <summary>
        /// Called when the amount of soil cut by rear scraper has been updated
        /// </summary>
        /// <param name="VolumeBCY">Total soil cut in BCY</param>
        private void FieldUpdater_OnRearVolumeCutUpdated(double VolumeBCY)
        {
            // calculate LCY
            double VolumeLCY = VolumeBCY * CurrentEquipmentSettings.SoilSwellFactor;

            // update in equipment status
            CurrentEquipmentStatus.RearPan.LoadLCY = VolumeLCY;
            Properties.Settings.Default.RearLoadLCY = VolumeLCY;
            Properties.Settings.Default.Save();

            // check if rear scraper has reached capacity
            if (VolumeLCY >= CurrentEquipmentSettings.RearPan.CapacityCY && !CurrentEquipmentStatus.RearPan.CapacityWarningOccurred)
            {
                CurrentEquipmentStatus.RearPan.CapacityWarningOccurred = true;

                SoundCapacityAlarm();
            }
        }

        /// <summary>
        /// Called when the amount of soil cut by front scraper has been updated
        /// </summary>
        /// <param name="VolumeBCY">Total soil cut in BCY</param>
        private void FieldUpdater_OnFrontVolumeCutUpdated(double VolumeBCY)
        {
            // calculate LCY
            double VolumeLCY = VolumeBCY * CurrentEquipmentSettings.SoilSwellFactor;

            // update in equipment status
            CurrentEquipmentStatus.FrontPan.LoadLCY = VolumeLCY;
            Properties.Settings.Default.FrontLoadLCY = VolumeLCY;
            Properties.Settings.Default.Save();

            // check if front scraper has reached capacity
            if (VolumeLCY >= CurrentEquipmentSettings.FrontPan.CapacityCY && !CurrentEquipmentStatus.FrontPan.CapacityWarningOccurred)
            {
                CurrentEquipmentStatus.FrontPan.CapacityWarningOccurred = true;

                SoundCapacityAlarm();
            }
        }

        /// <summary>
        /// Sounds an alarm to alert the operator that something has happened with the controller
        /// </summary>
        private void SoundControllerAlarm
            (
            )
        {
            SoundPlayer player = new SoundPlayer(Resources.mixkit_street_public_alarm_997);
            player.Play();
        }

        /// <summary>
        /// Sounds an alarm to alert the operator that scraper has reached capacity
        /// </summary>
        private void SoundCapacityAlarm
            (
            )
        {
            SoundPlayer player = new SoundPlayer(Resources.public_domain_beep_sound_100267);
            player.Play();
        }

        private void Controller_OnControllerFound()
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnControllerFound);
                return;
            }

            StatusBar.SetLedState(StatusBar.Leds.Controller, StatusBar.LedState.OK);

            // stop trying to connect
            ControllerConnectTimer.Stop();

            // send current configuration
            ConfigureController();

            // get the current heights of the blades
            // this will result in Controller.OnFrontBladeHeightChanged and Controller OnRearBladeHeightChanged being raised
            Controller.RequestFrontBladeHeight();
            Controller.RequestRearBladeHeight();
        }

        private void Controller_OnControllerLost()
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnControllerLost);
                return;
            }

            StatusBar.SetLedState(StatusBar.Leds.Controller, StatusBar.LedState.Error);

            SoundControllerAlarm();

            // no longer have RTK
            CurrentEquipmentStatus.TractorFix.RTK = RTKStatus.None;
            CurrentEquipmentStatus.FrontPan.Fix.RTK = RTKStatus.None;
            CurrentEquipmentStatus.RearPan.Fix.RTK = RTKStatus.None;
            // no longer have fixes
            CurrentEquipmentStatus.TractorFix.LastFixTime = DateTime.MinValue;
            CurrentEquipmentStatus.FrontPan.Fix.LastFixTime = DateTime.MinValue;
            CurrentEquipmentStatus.RearPan.Fix.LastFixTime = DateTime.MinValue;

            // no longer have IMU
            CurrentEquipmentStatus.TractorIMU.CalibrationStatus = IMUValue.Calibration.None;
            CurrentEquipmentStatus.FrontPan.IMU.CalibrationStatus = IMUValue.Calibration.None;
            CurrentEquipmentStatus.FrontPan.ApronIMU.CalibrationStatus = IMUValue.Calibration.None;
            CurrentEquipmentStatus.FrontPan.BucketIMU.CalibrationStatus = IMUValue.Calibration.None;
            CurrentEquipmentStatus.RearPan.IMU.CalibrationStatus = IMUValue.Calibration.None;
            CurrentEquipmentStatus.RearPan.BucketIMU.CalibrationStatus = IMUValue.Calibration.None;
            TractorIMUFound = false;
            FrontIMUFound = false;
            FrontApronIMUFound = false;
            FrontBucketIMUFound = false;
            RearIMUFound = false;
            RearBucketIMUFound = false;

            // no longer have height
            FrontHeightFound = false;
            RearHeightFound = false;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            UpdateIMULeds();
            UpdateRTKLeds();
            UpdateHeightLeds();

            // start trying to connect
            ControllerConnectTimer.Start();

            // force map update
            GetMap()?.SetTractor(CurrentEquipmentStatus.TractorFix);
        }

        private void Controller_OnEmergencyStop()
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnEmergencyStop);
                return;
            }

            StatusBar.ShowEStop = true;

            CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.Manual;
            SetFrontPanIndicator(PanIndicatorStates.None);
            Controller.SetFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
            UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);

            CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.Manual;
            SetRearPanIndicator(PanIndicatorStates.None);
            Controller.SetRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
            UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            SoundControllerAlarm();
        }

        private void Controller_OnEmergencyStopCleared()
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnEmergencyStopCleared);
                return;
            }

            StatusBar.ShowEStop = false;
        }

        /// <summary>
        /// Connects to the controller using the current settings
        /// </summary>
        private void ConnectToController
            (
            )
        {
            Controller.Disconnect();

            // connect to controller
            Controller.Connect(CurrentAppSettings.ControllerAddress, CurrentAppSettings.ControllerPort, CurrentAppSettings.SubnetMask, CurrentAppSettings.LocalPort);
        }

        /// <summary>
        /// Called when user taps on the edit equipment button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditEquipmentBtn_Click(object sender, EventArgs e)
        {
            ShowEditEquipment();
        }

        /// <summary>
        /// Shows the UI for editing the equipment
        /// </summary>
        private void ShowEditEquipment
            (
            )
        {
            ClosePage();

            EquipmentEditor equipmentEditor = new EquipmentEditor();
            equipmentEditor.FrontPanColor = FRONT_PAN_COLOR;
            equipmentEditor.RearPanColor = REAR_PAN_COLOR;
            equipmentEditor.Parent = ContentPanel;
            equipmentEditor.Dock = DockStyle.Fill;
            equipmentEditor.OnApplySettings += () => { ApplyEquipmentSettings(equipmentEditor); };

            // Load and display settings
            equipmentEditor.ShowSettings(CurrentEquipmentSettings);

            equipmentEditor.Show();
        }

        /// <summary>
        /// Shows the UI for editing the application settings
        /// </summary>
        private void ShowAppSettings
            (
            )
        {
            ClosePage();

            AppSettingsEditor settingsEditor = new AppSettingsEditor();
            settingsEditor.Parent = ContentPanel;
            settingsEditor.Dock = DockStyle.Fill;
            settingsEditor.OnPowerOff += () => { PowerOff(); };
            settingsEditor.OnCloseApplication += () => { Close(); };
            settingsEditor.OnApplySettings += () => { ApplyAppSettings(settingsEditor); };
            settingsEditor.OnOpenDataFolder += () => { OpenDataFolder(); };

            // Load and display settings
            settingsEditor.ShowSettings(CurrentAppSettings);

            settingsEditor.Show();
        }

        /// <summary>
        /// Powers off the PC
        /// </summary>
        private void PowerOff
            (
            )
        {
#if DEBUG
            Close();
#else
            Process.Start("shutdown", "/s /f /t 0");
#endif
        }

        /// <summary>
        /// Opens the data folder in windows explorer
        /// </summary>
        private void OpenDataFolder
            (
            )
        {
            Process.Start("explorer.exe", DataFolder);
        }

        /// <summary>
        /// Shows the UI for calibration
        /// </summary>
        private void ShowCalibrationPage
            (
            )
        {
            ClosePage();

            CalibrationPage calibrationPage = new CalibrationPage();
            calibrationPage.CurrentEquipmentSettings = CurrentEquipmentSettings;
            calibrationPage.CurrentField = CurrentField;
            calibrationPage.CurrentEquipmentStatus = CurrentEquipmentStatus;
            calibrationPage.Controller = Controller;
            calibrationPage.FrontPanColor = FRONT_PAN_COLOR;
            calibrationPage.RearPanColor = REAR_PAN_COLOR;
            calibrationPage.Parent = ContentPanel;
            calibrationPage.Dock = DockStyle.Fill;
            calibrationPage.OnEnableBladeLimits += CalibrationPage_OnEnableBladeLimits;
            calibrationPage.OnDisableBladeLimits += CalibrationPage_OnDisableBladeLimits;
            calibrationPage.Show();
        }

        /// <summary>
        /// Called when the calibration page wants to disable the blade limits
        /// </summary>
        private void CalibrationPage_OnDisableBladeLimits()
        {
            EnableBladeLimits = false;
        }

        /// <summary>
        /// Called when the calibration page wants to enable the blade limits
        /// </summary>
        private void CalibrationPage_OnEnableBladeLimits()
        {
            EnableBladeLimits = true;
        }

        /// <summary>
        /// Shows the UI for loading fields
        /// </summary>
        private void ShowFieldChooserPage
            (
            )
        {
            ClosePage();

            FieldChooserPage fieldChooserPage = new FieldChooserPage();
            fieldChooserPage.FieldDataFolder = FieldDataFolder;
            fieldChooserPage.Parent = ContentPanel;
            fieldChooserPage.Dock = DockStyle.Fill;
            fieldChooserPage.OnFieldChosen += (folder, dbfile) => { LoadField(folder, dbfile); };
            fieldChooserPage.OnDownloadFieldBasemap += (folder, dbfile) => { DownloadFieldBasemap(folder, dbfile); };
            fieldChooserPage.OnCreateNewField += () => { CreateField(); };
            fieldChooserPage.OnImportField += () => { ImportField(); };
            fieldChooserPage.Show();
        }

        /// <summary>
        /// Shows the UI for creating and loading surveys
        /// </summary>
        private void ShowSurveyChooserPage
            (
            )
        {
            ClosePage();

            SurveyChooserPage surveyChooserPage = new SurveyChooserPage();
            surveyChooserPage.SurveyDataFolder = SurveyDataFolder;
            surveyChooserPage.Parent = ContentPanel;
            surveyChooserPage.Dock = DockStyle.Fill;
            surveyChooserPage.OnSurveyChosen += (filename) => { LoadSurvey(filename); };
            surveyChooserPage.OnCreateSurvey += () => { CreateSurvey(); };
            surveyChooserPage.OnDownloadBasemap += () => { DownloadBasemap(); };
            surveyChooserPage.Show();
        }

        /// <summary>
        /// Shows the UI for creating a survey
        /// </summary>
        private void ShowCreateSurveyPage
            (
            )
        {
            ClosePage();

            CreateSurveyPage createSurveyPage = new CreateSurveyPage();
            createSurveyPage.SurveyDataFolder = SurveyDataFolder;
            createSurveyPage.Parent = ContentPanel;
            createSurveyPage.Dock = DockStyle.Fill;
            createSurveyPage.OnSurveyCreated += (filename) => { LoadSurvey(filename); };
            createSurveyPage.Show();
        }

        /// <summary>
        /// Shows the UI for downloading a basemap
        /// </summary>
        private void ShowDownloadBasemapPage
            (
            )
        {
            ClosePage();

            DownloadBasemapPage downloadBasemapPage = new DownloadBasemapPage();
            downloadBasemapPage.OnDownloadBasemap += (lat, lon) => { DownloadBasemap(lat, lon); };
            downloadBasemapPage.Parent = ContentPanel;
            downloadBasemapPage.Dock = DockStyle.Fill;
            downloadBasemapPage.Show();
        }

        /// <summary>
        /// Shows the UI for creating a field
        /// </summary>
        private void ShowCreateFieldPage
            (
            )
        {
            ClosePage();

            CreateFieldPage createFieldPage = new CreateFieldPage();
            createFieldPage.SurveyDataFolder = SurveyDataFolder;
            createFieldPage.FieldDataFolder = FieldDataFolder;
            createFieldPage.Parent = ContentPanel;
            createFieldPage.Dock = DockStyle.Fill;
            createFieldPage.OnCreateField += (fielddesign) => { CreateField(fielddesign); };
            createFieldPage.Show();
        }

        /// <summary>
        /// Shows the UI import a field
        /// </summary>
        private void ShowImportFieldPage
            (
            )
        {
            ClosePage();

            ImportFieldPage importFieldPage = new ImportFieldPage();
            importFieldPage.Parent = ContentPanel;
            importFieldPage.Dock = DockStyle.Fill;
            importFieldPage.FieldDataFolder = FieldDataFolder;
            importFieldPage.Show();
        }

        /// <summary>
        /// Creates a new field from a design
        /// </summary>
        /// <param name="Design">Design to use</param>
        private void CreateField
            (
            FieldDesign Design
            )
        {

        }

        /// <summary>
        /// Creates a new survey
        /// </summary>
        private void CreateSurvey
            (
            )
        {
            ShowCreateSurveyPage();
        }

        /// <summary>
        /// Prompts user to enter details to download a basemap
        /// </summary>
        private void DownloadBasemap
            (
            )
        {
            ShowDownloadBasemapPage();
        }

        /// <summary>
        /// Creates a new field
        /// </summary>
        private void CreateField
            (
            )
        {
            ShowCreateFieldPage();
        }

        /// <summary>
        /// Imports a field
        /// </summary>
        private void ImportField
            (
            )
        {
            ShowImportFieldPage();
        }

        /// <summary>
        /// Shows the map
        /// </summary>
        private void ShowMap
            (
            )
        {
            ClosePage();

            Map map = new Map();
            map.Parent = ContentPanel;
            map.Dock = DockStyle.Fill;
            map.FrontPanColor = FRONT_PAN_COLOR;
            map.RearPanColor = REAR_PAN_COLOR;
            map.SetEquipmentSettings(CurrentEquipmentSettings);
            map.SetApplicationSettings(CurrentAppSettings);
            map.SetEquipmentStatus(CurrentEquipmentStatus);
            map.SetBasemapDataFolder(BasemapDataFolder);

            ZoomInBtn.Enabled = true;
            ZoomOutBtn.Enabled = true;
            ZoomFitBtn.Enabled = true;

            if (CurrentField != null)
            {
                map.ShowField(CurrentField);
            }

            if (CurrentSurvey != null)
            {
                map.ShowSurvey(CurrentSurvey);
            }

            // give map initial conditions
            map.SetTractor(CurrentEquipmentStatus.TractorFix);
            map.SetFrontScraper(CurrentEquipmentStatus.FrontPan.Fix);
            map.SetRearScraper(CurrentEquipmentStatus.RearPan.Fix);

            // connect events
            map.OnResetPanLoad += Map_OnResetPanLoad;
            map.OnStartSurveying += Map_OnStartSurveying;
            map.OnStopSurveying += Map_OnStopSurveying;
            map.OnSurveyBoundaryChanged += Map_OnSurveyBoundaryChanged;
            map.OnAddBenchmark += Map_OnAddBenchmark;

            map.Show();
        }

        /// <summary>
        /// Called when the map requests to add a benchmark to the current survey
        /// </summary>
        private void Map_OnAddBenchmark()
        {
            SurveyUpdater.AddBenchmark();
        }

        /// <summary>
        /// Called when the map changes the boundary mode for surveying
        /// </summary>
        /// <param name="Mode"></param>
        private void Map_OnSurveyBoundaryChanged(Map.BoundaryModes Mode)
        {
            switch (Mode)
            {
                default:
                case Map.BoundaryModes.None: SurveyUpdater.BoundaryChanged(SurveyUpdater.BoundaryModes.None); break;
                case Map.BoundaryModes.Left: SurveyUpdater.BoundaryChanged(SurveyUpdater.BoundaryModes.Left); break;
                case Map.BoundaryModes.Right: SurveyUpdater.BoundaryChanged(SurveyUpdater.BoundaryModes.Right); break;
            }
        }

        /// <summary>
        /// Called when map requests a stop to surveying
        /// </summary>
        private void Map_OnStopSurveying
            (
            )
        {
            SurveyUpdater.Stop();
        }

        /// <summary>
        /// Called when map requests a start to surveying
        /// </summary>
        /// <param name="Mode">Boundary mode</param>
        private void Map_OnStartSurveying
            (
            Map.BoundaryModes Mode
            )
        {
            switch (Mode)
            {
                default:
                case Map.BoundaryModes.None: SurveyUpdater.Start(SurveyUpdater.BoundaryModes.None); break;
                case Map.BoundaryModes.Left: SurveyUpdater.Start(SurveyUpdater.BoundaryModes.Left); break;
                case Map.BoundaryModes.Right: SurveyUpdater.Start(SurveyUpdater.BoundaryModes.Right); break;
            }
        }

        /// <summary>
        /// Called when user wishes to reset the load of a pan
        /// </summary>
        /// <param name="Front">true for front pan, false for rear pan</param>
        private void Map_OnResetPanLoad(bool Front)
        {
            if (Front)
            {
                CurrentEquipmentStatus.FrontPan.LoadLCY = 0;
            }
            else
            {
                CurrentEquipmentStatus.RearPan.LoadLCY = 0;
            }
        }

        /// <summary>
        /// Unloads the current field
        /// </summary>
        private void UnloadField
            (
            )
        {
            if (CurrentField != null)
            {
                CurrentField = null;

                BladeCtrl.SetField(CurrentField);
                FieldUpdater.SetField(CurrentField);

                // if showing map then update to remove field
                GetMap()?.ShowField(CurrentField);

                FixTimer.Enabled = false;
            }
        }

        private void UnloadSurvey
            (
            )
        {
            if (CurrentSurvey != null)
            {
                CurrentSurvey = null;

                SurveyUpdater.SetSurvey(CurrentSurvey);

                // if showing map then update to remove survey
                GetMap()?.ShowSurvey(CurrentSurvey);

                FixTimer.Enabled = false;
            }
        }

        /// <summary>
        /// Loads a survey for further editing
        /// </summary>
        /// <param name="FileName">Path and name of survey to load</param>
        private void LoadSurvey
            (
            string FileName
            )
        {
            UnloadField();
            ShowMap();

            CurrentSurvey = new Survey();
            CurrentSurvey.Load(FileName);

            SurveyUpdater.SetSurvey(CurrentSurvey);

            // if showing map then update to show survey
            GetMap()?.ShowSurvey(CurrentSurvey);

            FixTimer.Enabled = true;
        }

        /// <summary>
        /// Downloads the basemap for a field
        /// </summary>
        /// <param name="Folder">Path to folder containing field data</param>
        /// <param name="DbFile">Database to load or null to create new field</param>
        private void DownloadFieldBasemap
            (
            string Folder,
            string? DbFile
            )
        {
            if (string.IsNullOrWhiteSpace(DbFile))
            {
                MessageBox.Show(
                    "Please choose a saved field version before downloading a basemap.",
                    "Basemap Download",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _ = DownloadFieldBasemapAsync(Folder, DbFile);
        }

        /// <summary>
        /// Downloads the basemap for an area
        /// </summary>
        /// <param name="Latitude">Area centroid latitude</param>
        /// <param name="Longitude">Area centroid longitude</param>
        private void DownloadBasemap
            (
            double Latitude,
            double Longitude
            )
        {
            _ = DownloadBasemapAsync(Latitude, Longitude);
        }

        private async Task DownloadBasemapAsync
            (
            double latitude,
            double longitude
            )
        {
            Cursor? previousCursor = Cursor.Current;
            try
            {
                UseWaitCursor = true;
                Cursor.Current = Cursors.WaitCursor;

                BasemapDownloader downloader = new BasemapDownloader();
                downloader.OnProgressChanged += (pcent) => { UpdateDownloadBasemapProgress(pcent); };
                await downloader.DownloadAsync(BasemapDataFolder, latitude, longitude).ConfigureAwait(true);
            }
            catch (Exception)
            {
            }
            finally
            {
                UseWaitCursor = false;
                Cursor.Current = previousCursor;
            }
        }

        /// <summary>
        /// Called during download of a basemap to indicate progress
        /// </summary>
        /// <param name="Percentage">Percentage complted</param>
        private void UpdateDownloadBasemapProgress
            (
            double PercentageComplete
            )
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<double>(UpdateDownloadBasemapProgress), PercentageComplete);
                return;
            }

            if (ContentPanel.Controls.Count == 1)
            {
                Control ctrl = ContentPanel.Controls[0];
                if (ctrl is FieldChooserPage fieldChooserPage)
                {
                    fieldChooserPage.DownloadProgress = (int)PercentageComplete;
                }
                else if (ctrl is DownloadBasemapPage downloadBasemapPage)
                {
                    downloadBasemapPage.DownloadProgress = (int)PercentageComplete;
                }
            }
        }

        private async Task DownloadFieldBasemapAsync
            (
            string folder,
            string dbFile
            )
        {
            Cursor previousCursor = Cursor.Current;
            try
            {
                UseWaitCursor = true;
                Cursor.Current = Cursors.WaitCursor;

                BasemapDownloader downloader = new BasemapDownloader();
                downloader.OnProgressChanged += (pcent) => { UpdateDownloadBasemapProgress(pcent); };
                BasemapDownloader.DownloadSummary summary = await downloader.DownloadAsync(BasemapDataFolder, dbFile);

                /*MessageBox.Show(
                    $"Basemap download complete.\n\n" +
                    $"Requested: {summary.RequestedTiles}\n" +
                    $"Downloaded: {summary.DownloadedTiles}\n" +
                    $"Skipped (already present): {summary.SkippedTiles}\n" +
                    $"Failed: {summary.FailedTiles}\n\n" +
                    $"Saved in:\n{summary.BasemapFolder}",
                    "Basemap Download",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);*/
            }
            catch (Exception ex)
            {
                /*MessageBox.Show(
                    $"Basemap download failed:\n{ex.Message}",
                    "Basemap Download",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);*/
            }
            finally
            {
                UseWaitCursor = false;
                Cursor.Current = previousCursor;
            }
        }

        /// <summary>
        /// Loads a field
        /// </summary>
        /// <param name="Folder">Path to folder containing field data</param>
        /// <param name="DbFile">Database to load or null to create new field</param>
        private void LoadField
            (
            string Folder,
            string? DbFile
            )
        {
            UnloadSurvey();
            ShowMap();

            CurrentField = new Field();
            CurrentField.Load(Folder, DbFile);

            BladeCtrl.SetField(CurrentField);
            FieldUpdater.SetField(CurrentField);

            // if showing map then update to show field
            GetMap()?.ShowField(CurrentField);

            FixTimer.Enabled = true;
        }

        /// <summary>
        /// Shows the status page
        /// </summary>
        private void ShowStatusPage
            (
            )
        {
            ClosePage();

            StatusPage statusPage = new StatusPage();
            statusPage.Parent = ContentPanel;
            statusPage.Dock = DockStyle.Fill;
            statusPage.Show();

            statusPage.ShowFieldStatus(CurrentField);
            statusPage.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);
        }

        /// <summary>
        /// Gets and applies the current application settings
        /// </summary>
        /// <param name="Editor">The app settings editor</param>
        private void ApplyAppSettings
            (
            AppSettingsEditor Editor
            )
        {
            AppSettings AppSettings = Editor!.GetSettings();
            AppSettings.Save();
            CurrentAppSettings = AppSettings;

            GetMap()?.SetApplicationSettings(AppSettings);

            FieldUpdater.SetApplicationSettings(CurrentAppSettings);

            ConnectToController();
            ConfigureController();
        }

        /// <summary>
        /// Gets and applies the current equipment settings
        /// </summary>
        /// <param name="Editor">The equipment settings editor</param>
        private void ApplyEquipmentSettings
            (
            EquipmentEditor Editor
            )
        {
            EquipmentSettings EquipmentSettings = Editor!.GetSettings();
            EquipmentSettings.Save();
            CurrentEquipmentSettings = EquipmentSettings;

            GetMap()?.SetEquipmentSettings(EquipmentSettings);
            Controller?.SetEquipmentSettings(EquipmentSettings);
            FieldUpdater.SetEquipmentSettings(EquipmentSettings);
            BladeCtrl.SetEquipmentSettings(EquipmentSettings);
            SurveyUpdater.SetEquipmentSettings(EquipmentSettings);

            Controller?.SetFrontBladeConfiguration(CurrentEquipmentSettings.FrontBlade);
            Controller?.SetRearBladeConfiguration(CurrentEquipmentSettings.RearBlade);
            
            Controller?.SetTractorAntennaLocation(CurrentEquipmentSettings.TractorAntennaHeightMm, CurrentEquipmentSettings.TractorAntennaLeftOffsetMm, CurrentEquipmentSettings.TractorAntennaForwardOffsetMm);
            Controller?.SetFrontAntennaHeight(CurrentEquipmentSettings.FrontPan.AntennaHeightMm);
            Controller?.SetRearAntennaHeight(CurrentEquipmentSettings.RearPan.AntennaHeightMm);
            Controller?.SetMagneticDeclination(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);

            UpdateEnabledLeds();
            UpdateIMULeds();
            UpdateHeightLeds();

            SetFrontPanIndicator(null);
            SetRearPanIndicator(null);
        }

        /// <summary>
        /// Closes the current page
        /// </summary>
        /// <returns>true if page was closed</returns>
        private bool ClosePage
            (
            )
        {
            if (ContentPanel.Controls.Count == 1)
            {
                Control Ctrl = (Control)ContentPanel.Controls[0];
                if (Ctrl is AppSettingsEditor)
                {
                    try
                    {
                        ApplyAppSettings((AppSettingsEditor)Ctrl);
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show($"Validation error: {ex.Message}", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                else if (Ctrl is EquipmentEditor)
                {
                    try
                    {
                        ApplyEquipmentSettings((EquipmentEditor)Ctrl);
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show($"Validation error: {ex.Message}", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
                else if (Ctrl is Map)
                {
                    // remove event handlers
                    (Ctrl as Map)!.OnResetPanLoad -= Map_OnResetPanLoad;
                    (Ctrl as Map)!.OnStartSurveying -= Map_OnStartSurveying;
                    (Ctrl as Map)!.OnStopSurveying -= Map_OnStopSurveying;
                    (Ctrl as Map)!.OnSurveyBoundaryChanged -= Map_OnSurveyBoundaryChanged;
                    (Ctrl as Map)!.OnAddBenchmark -= Map_OnAddBenchmark;
                }
                else if (Ctrl is CalibrationPage)
                {
                    // tell calibration page it is being closed
                    (Ctrl as CalibrationPage)!.Closing();

                    // remove event handlers
                    (Ctrl as CalibrationPage)!.OnEnableBladeLimits -= CalibrationPage_OnEnableBladeLimits;
                    (Ctrl as CalibrationPage)!.OnDisableBladeLimits -= CalibrationPage_OnDisableBladeLimits;

                    CalibrationPage_OnEnableBladeLimits();
                }
            }

            ContentPanel.Controls.Clear();

            // disable controls specific to the map            
            ZoomInBtn.Enabled = false;
            ZoomOutBtn.Enabled = false;
            ZoomFitBtn.Enabled = false;

            return true;
        }

        /// <summary>
        /// Called when user taps the close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerBtn_Click(object sender, EventArgs e)
        {
            ClosePage();

            // fixme - change into shutting down windows
            Close();
        }

        /// <summary>
        /// Called when user taps on the edit settings button
        /// Shows the application settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditSettingsBtn_Click(object sender, EventArgs e)
        {
            ShowAppSettings();
        }

        /// <summary>
        /// Called when user taps on the calibration button
        /// Shows the calibration page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalibrationBtn_Click(object sender, EventArgs e)
        {
            ShowCalibrationPage();
        }

        /// <summary>
        /// Called when user taps on the survey button
        /// Shows the survey page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SurveyBtn_Click(object sender, EventArgs e)
        {
            ShowSurveyChooserPage();
        }

        /// <summary>
        /// Called when the user taps on the map button
        /// Shows the map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapBtn_Click(object sender, EventArgs e)
        {
            ShowMap();
        }

        private void StatusBtn_Click(object sender, EventArgs e)
        {
            ShowStatusPage();
        }

        /// <summary>
        /// Called when application is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // make sure we save the settings
            CurrentAppSettings.Save();
            CurrentEquipmentSettings.Save();

            Controller.Disconnect();
        }

        /// <summary>
        /// This code has to execute in the load event instead of the constructor
        /// otherwise the controller events cannot update the UI
        /// see: https://stackoverflow.com/questions/17603339/invoke-or-begininvoke-cannot-be-called-on-a-control-until-the-window-handle-has
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            Controller.OnEmergencyStop += Controller_OnEmergencyStop;
            Controller.OnEmergencyStopCleared += Controller_OnEmergencyStopCleared;

            Controller.OnControllerLost += Controller_OnControllerLost;
            Controller.OnControllerFound += Controller_OnControllerFound;
            Controller.OnIMUFound += Controller_OnIMUFound;
            Controller.OnIMULost += Controller_OnIMULost;
            Controller.OnHeightFound += Controller_OnHeightFound;
            Controller.OnHeightLost += Controller_OnHeightLost;

            Controller.OnTractorLocationChanged += Controller_OnTractorLocationChanged;
            Controller.OnFrontLocationChanged += Controller_OnFrontLocationChanged;
            Controller.OnRearLocationChanged += Controller_OnRearLocationChanged;

            Controller.OnTractorIMUChanged += Controller_OnTractorIMUChanged;
            Controller.OnFrontIMUChanged += Controller_OnFrontIMUChanged;
            Controller.OnFrontApronIMUChanged += Controller_OnFrontApronIMUChanged;
            Controller.OnFrontBucketIMUChanged += Controller_OnFrontBucketIMUChanged;
            Controller.OnRearIMUChanged += Controller_OnRearIMUChanged;
            Controller.OnRearBucketIMUChanged += Controller_OnRearBucketIMUChanged;

            Controller.OnFrontBladeCuttingChanged += Controller_OnFrontBladeCuttingChanged;
            Controller.OnFrontBladeDirectionChanged += Controller_OnFrontBladeDirectionChanged;
            Controller.OnFrontBladePWMChanged += Controller_OnFrontBladePWMChanged;
            Controller.OnFrontBladeHeightChanged += Controller_OnFrontBladeHeightChanged;
            Controller.OnFrontSlaveOffsetChanged += Controller_OnFrontSlaveOffsetChanged;

            Controller.OnRearBladeCuttingChanged += Controller_OnRearBladeCuttingChanged;
            Controller.OnRearBladeDirectionChanged += Controller_OnRearBladeDirectionChanged;
            Controller.OnRearBladePWMChanged += Controller_OnRearBladePWMChanged;
            Controller.OnRearBladeHeightChanged += Controller_OnRearBladeHeightChanged;
            Controller.OnRearSlaveOffsetChanged += Controller_OnRearSlaveOffsetChanged;

            Controller.OnFrontDumpingChanged += Controller_OnFrontDumpingChanged;
            Controller.OnRearDumpingChanged += Controller_OnRearDumpingChanged;

            Controller.OnFrontBladeCommandSent += Controller_OnFrontBladeCommandSent;

            Controller.OnFrontBladeJogged += Controller_OnFrontBladeJogged;
            Controller.OnRearBladeJogged += Controller_OnRearBladeJogged;

            Controller.OnEnableSecondaryTabletMode += Controller_EnableSecondaryTabletMode;

            // initally we don't know if there is a controller or not
            // and we don't know status of tractor RTK and IMU
            StatusBar.SetLedState(StatusBar.Leds.Controller, StatusBar.LedState.Error);
            StatusBar.SetLedState(StatusBar.Leds.TractorRTK, StatusBar.LedState.Error);

            ControllerConnectTimer = new Timer();
            ControllerConnectTimer.Interval = CONTROLLER_TRY_CONNECT_PERIOD_MS;
            ControllerConnectTimer.Tick += ControllerConnectTimer_Tick;
            ControllerConnectTimer.Start();

            Controller.SetEquipmentSettings(CurrentEquipmentSettings);
            FieldUpdater.SetEquipmentSettings(CurrentEquipmentSettings);
            BladeCtrl.SetEquipmentSettings(CurrentEquipmentSettings);
            SurveyUpdater.SetEquipmentSettings(CurrentEquipmentSettings);

            UpdateEnabledLeds();
            UpdateIMULeds();
            UpdateHeightLeds();
            UpdateRTKLeds();

            DataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "AgGrade" + Path.DirectorySeparatorChar;

            FieldDataFolder = DataFolder + "FieldData" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(FieldDataFolder)) Directory.CreateDirectory(FieldDataFolder);

            SurveyDataFolder = DataFolder + "SurveyData" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(SurveyDataFolder)) Directory.CreateDirectory(SurveyDataFolder);

            BasemapDataFolder = DataFolder + "BasemapData" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(BasemapDataFolder)) Directory.CreateDirectory(BasemapDataFolder);

            // turn off indicators
            SetFrontPanIndicator(PanIndicatorStates.None);
            SetRearPanIndicator(PanIndicatorStates.None);

            Properties.Settings.Default.Reload();

            // get previous loads in case the application crashed during cutting or filling
            CurrentEquipmentStatus.FrontPan.LoadLCY = Properties.Settings.Default.FrontLoadLCY;
            CurrentEquipmentStatus.RearPan.LoadLCY = Properties.Settings.Default.RearLoadLCY;
        }

        /// <summary>
        /// The rear blade has been jogged
        /// </summary>
        /// <param name="Up">true if jogged up, false if jogged down</param>
        private void Controller_OnRearBladeJogged(bool Up)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(Controller_OnRearBladeJogged), Up);
                return;
            }

            CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.Manual;
            Controller.SetRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
            UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            SetRearPanIndicator(PanIndicatorStates.None);

            if (Up)
            {
                BladeCtrl.SetRearBladeHeight(CurrentEquipmentStatus.RearPan.BladeHeight + 1, EnableBladeLimits);
            }
            else
            {
                BladeCtrl.SetRearBladeHeight(CurrentEquipmentStatus.RearPan.BladeHeight - 1, EnableBladeLimits);
            }
        }

        /// <summary>
        /// The front blade has been jogged
        /// </summary>
        /// <param name="Up">true if jogged up, false if jogged down</param>
        private void Controller_OnFrontBladeJogged(bool Up)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(Controller_OnFrontBladeJogged), Up);
                return;
            }

            CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.Manual;
            Controller.SetFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
            UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);

            SetFrontPanIndicator(PanIndicatorStates.None);

            if (Up)
            {
                BladeCtrl.SetFrontBladeHeight(CurrentEquipmentStatus.FrontPan.BladeHeight + 1, EnableBladeLimits);
            }
            else
            {
                BladeCtrl.SetFrontBladeHeight(CurrentEquipmentStatus.FrontPan.BladeHeight - 1, EnableBladeLimits);
            }
        }

        /// <summary>
        /// Called when rear scraper starts and stops dumping
        /// </summary>
        /// <param name="IsDumping">true if dumping</param>
        private void Controller_OnRearDumpingChanged(bool IsDumping)
        {
            if (CurrentEquipmentStatus.RearPan.Mode != PanStatus.BladeMode.Manual)
            {
                if (IsDumping)
                {
                    CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.AutoFilling;
                    SetRearPanIndicator(PanIndicatorStates.Filling);
                    Controller.SetRearBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
                }
                else
                {
                    BladeCtrl.SetRearToTransportState();
                    SetRearPanIndicator(PanIndicatorStates.Transport);
                }

                UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
            }
            else
            {
                SetRearPanIndicator(PanIndicatorStates.None);
            }
        }

        /// <summary>
        /// Called when front scraper starts and stops dumping
        /// </summary>
        /// <param name="IsDumping">true if dumping</param>
        private void Controller_OnFrontDumpingChanged(bool IsDumping)
        {
            if (CurrentEquipmentStatus.FrontPan.Mode != PanStatus.BladeMode.Manual)
            {
                if (IsDumping)
                {
                    CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.AutoFilling;
                    SetFrontPanIndicator(PanIndicatorStates.Filling);
                    Controller.SetFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
                }
                else
                {
                    BladeCtrl.SetFrontToTransportState();
                    SetFrontPanIndicator(PanIndicatorStates.Transport);
                }

                UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
            }
            else
            {
                SetFrontPanIndicator(PanIndicatorStates.None);
            }
        }

        /// <summary>
        /// Called when a new front blade height command has been sent to the controller
        /// Doesn't mean the blade is moving, only that the height has been requested
        /// </summary>
        /// <param name="Value">Blade height command 0 -> 200</param>
        private void Controller_OnFrontBladeCommandSent(uint Value)
        {
        }

        private void Controller_OnRearSlaveOffsetChanged(int Offset)
        {
            CurrentEquipmentStatus.RearPan.BladeOffset = Offset;
        }

        private void Controller_OnRearBladeHeightChanged(uint Height)
        {
            // convert to signed millimeters
            CurrentEquipmentStatus.RearPan.BladeHeight = (int)Height - BladeController.BLADE_HEIGHT_GROUND_LEVEL;
        }

        private void Controller_OnRearBladePWMChanged(byte PWMValue)
        {
            CurrentEquipmentStatus.RearPan.BladePWM = PWMValue;
        }

        private void Controller_OnRearBladeDirectionChanged(bool IsMovingUp)
        {
            if (IsMovingUp)
            {
                CurrentEquipmentStatus.RearPan.Direction = PanStatus.BladeDirection.Up;
            }
            else
            {
                CurrentEquipmentStatus.RearPan.Direction = PanStatus.BladeDirection.Down;
            }
        }

        private void Controller_OnRearBladeCuttingChanged(bool IsCutting)
        {
            if (CurrentEquipmentStatus.RearPan.Mode != PanStatus.BladeMode.Manual)
            {
                if (IsCutting)
                {
                    CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.AutoCutting;
                    SetRearPanIndicator(PanIndicatorStates.Cutting);
                    Controller.SetRearBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
                }
                else
                {
                    BladeCtrl.SetRearToTransportState();
                    SetRearPanIndicator(PanIndicatorStates.Transport);
                }

                UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
            }
            else
            {
                SetRearPanIndicator(PanIndicatorStates.None);
            }
        }

        /// <summary>
        /// Called when controller tells tablet it is the secondary tablet
        /// </summary>
        private void Controller_EnableSecondaryTabletMode
            (
            )
        {
            SwitchToSecondaryMode();
        }

        /// <summary>
        /// Switches to secondary tablet mode
        /// </summary>
        private void SwitchToSecondaryMode
            (
            )
        {
            // fixme - to do
        }

        /// <summary>
        /// Updates the UI and manages the field updater based on the new rear blade mode
        /// </summary>
        /// <param name="Mode">New rear blade mode</param>
        private void UpdateRearBladeMode
            (
            PanStatus.BladeMode Mode
            )
        {
            if (Mode == PanStatus.BladeMode.Manual)
            {
                RearBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Red;
            }
            else
            {
                RearBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Green;
            }

            if (Mode == PanStatus.BladeMode.AutoCutting)
            {
                FieldUpdater.StartRearCutting();
                GetMap()?.StartRearCutting();
            }
            else if (Mode == PanStatus.BladeMode.AutoFilling)
            {
                FieldUpdater.StartRearFilling();
                GetMap()?.StartRearFilling();
            }
        }

        private void Controller_OnFrontBladeHeightChanged(uint Height)
        {
            // convert to signed millimeters
            CurrentEquipmentStatus.FrontPan.BladeHeight = (int)Height - BladeController.BLADE_HEIGHT_GROUND_LEVEL;
        }

        private void Controller_OnFrontSlaveOffsetChanged(int Offset)
        {
            CurrentEquipmentStatus.FrontPan.BladeOffset = Offset;
        }

        private void Controller_OnFrontBladePWMChanged(byte PWMValue)
        {
            CurrentEquipmentStatus.FrontPan.BladePWM = PWMValue;
        }

        /// <summary>
        /// Sets the front pan indicator to show a state
        /// </summary>
        /// <param name="State">State to show or null for no change</param>
        private void SetFrontPanIndicator
            (
            PanIndicatorStates? State
            )
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<PanIndicatorStates?>(SetFrontPanIndicator), State);
                return;
            }

            if (CurrentEquipmentSettings.FrontPan.Equipped)
            {
                FrontPanIndicator.Visible = true;
            }
            else
            {
                FrontPanIndicator.Visible = false;
            }

            if (State.HasValue)
            {
                switch (State)
                {
                    default:
                    case PanIndicatorStates.None:
                        FrontPanIndicator.BackgroundImage = null;
                        break;

                    case PanIndicatorStates.Cutting:
                        FrontPanIndicator.BackgroundImage = Properties.Resources.cut_48px;
                        break;

                    case PanIndicatorStates.Filling:
                        FrontPanIndicator.BackgroundImage = Properties.Resources.fill_48px;
                        break;

                    case PanIndicatorStates.Transport:
                        FrontPanIndicator.BackgroundImage = Properties.Resources.transport_48px;
                        break;
                }
            }
        }

        /// <summary>
        /// Sets the rear pan indicator to show a state
        /// </summary>
        /// <param name="State">State to show or null for no change</param>
        private void SetRearPanIndicator
            (
            PanIndicatorStates? State
            )
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<PanIndicatorStates?>(SetRearPanIndicator), State);
                return;
            }

            if (CurrentEquipmentSettings.RearPan.Equipped)
            {
                RearPanIndicator.Visible = true;
            }
            else
            {
                RearPanIndicator.Visible = false;
            }

            if (State.HasValue)
            {
                switch (State)
                {
                    default:
                    case PanIndicatorStates.None:
                        RearPanIndicator.BackgroundImage = null;
                        break;

                    case PanIndicatorStates.Cutting:
                        RearPanIndicator.BackgroundImage = Properties.Resources.cut_48px;
                        break;

                    case PanIndicatorStates.Filling:
                        RearPanIndicator.BackgroundImage = Properties.Resources.fill_48px;
                        break;

                    case PanIndicatorStates.Transport:
                        RearPanIndicator.BackgroundImage = Properties.Resources.transport_48px;
                        break;
                }
            }
        }

        private void Controller_OnFrontBladeDirectionChanged(bool IsMovingUp)
        {
            if (IsMovingUp)
            {
                CurrentEquipmentStatus.FrontPan.Direction = PanStatus.BladeDirection.Up;
            }
            else
            {
                CurrentEquipmentStatus.FrontPan.Direction = PanStatus.BladeDirection.Down;
            }
        }

        private void Controller_OnFrontBladeCuttingChanged(bool IsCutting)
        {
            if (CurrentEquipmentStatus.FrontPan.Mode != PanStatus.BladeMode.Manual)
            {
                if (IsCutting)
                {
                    CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.AutoCutting;
                    SetFrontPanIndicator(PanIndicatorStates.Cutting);
                    Controller.SetFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
                }
                else
                {
                    BladeCtrl.SetFrontToTransportState();
                    SetFrontPanIndicator(PanIndicatorStates.Transport);
                }

                UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
            }
            else
            {
                SetFrontPanIndicator(PanIndicatorStates.None);
            }
        }

        /// <summary>
        /// Updates the UI and manages the field updater based on the new front blade mode
        /// </summary>
        /// <param name="Mode">New front blade mode</param>
        private void UpdateFrontBladeMode
            (
            PanStatus.BladeMode Mode
            )
        {
            if (Mode == PanStatus.BladeMode.Manual)
            {
                FrontBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Red;
            }
            else
            {
                FrontBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Green;
            }

            if (Mode == PanStatus.BladeMode.AutoCutting)
            {
                CurrentEquipmentStatus.FrontPan.CapacityWarningOccurred = false;
                FieldUpdater.StartFrontCutting();
                GetMap()?.StartFrontCutting();
            }
            else if (Mode == PanStatus.BladeMode.AutoFilling)
            {
                FieldUpdater.StartFrontFilling();
                GetMap()?.StartFrontFilling();
            }
        }

        /// <summary>
        /// Received IMU data for the rear scraper
        /// </summary>
        /// <param name="Value"></param>
        private void Controller_OnRearIMUChanged(IMUValue Value)
        {
            CurrentEquipmentStatus.FrontPan.IMU = Value;

            FrontIMUFound = true;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            UpdateIMULeds();
        }

        private void Controller_OnFrontApronIMUChanged(IMUValue Value)
        {
            CurrentEquipmentStatus.FrontPan.ApronIMU = Value;

            FrontApronIMUFound = true;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            UpdateIMULeds();
        }

        private void Controller_OnFrontBucketIMUChanged(IMUValue Value)
        {
            CurrentEquipmentStatus.FrontPan.BucketIMU = Value;

            FrontBucketIMUFound = true;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            UpdateIMULeds();
        }

        private void Controller_OnRearBucketIMUChanged(IMUValue Value)
        {
            CurrentEquipmentStatus.RearPan.BucketIMU = Value;

            RearBucketIMUFound = true;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            UpdateIMULeds();
        }

        /// <summary>
        /// Received IMU data for the front scraper
        /// </summary>
        /// <param name="Value"></param>
        private void Controller_OnFrontIMUChanged(IMUValue Value)
        {
            CurrentEquipmentStatus.RearPan.IMU = Value;

            RearIMUFound = true;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            UpdateIMULeds();
        }

        /// <summary>
        /// Received new IMU data for the tractor
        /// </summary>
        /// <param name="Value">New IMU data</param>
        private void Controller_OnTractorIMUChanged(IMUValue Value)
        {
            CurrentEquipmentStatus.TractorIMU = Value;

            TractorIMUFound = true;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            UpdateIMULeds();
        }

        /// <summary>
        /// Called when the tractor's location changes
        /// </summary>
        /// <param name="Fix">New tractor location</param>
        private void Controller_OnTractorLocationChanged(GNSSFix Fix)
        {
            CurrentEquipmentStatus.TractorFix = Fix;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            GetMap()?.SetTractor(Fix);

            UpdateRTKLeds();
        }

        /// <summary>
        /// Called when the front scraper's location changed
        /// </summary>
        /// <param name="Fix">New scraper location</param>
        private void Controller_OnRearLocationChanged(GNSSFix Fix)
        {
            CurrentEquipmentStatus.RearPan.Fix = Fix;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            GetMap()?.SetRearScraper(Fix);

            UpdateRTKLeds();
        }

        /// <summary>
        /// Called when the rear scraper's location changes
        /// </summary>
        /// <param name="Fix">New scraper location</param>
        private void Controller_OnFrontLocationChanged(GNSSFix Fix)
        {
            CurrentEquipmentStatus.FrontPan.Fix = Fix;

            GetStatusPage()?.ShowStatus(CurrentEquipmentStatus, CurrentAppSettings);

            GetMap()?.SetFrontScraper(Fix);

            UpdateRTKLeds();
        }

        /// <summary>
        /// Checks if we are showing the status page and gets it if we are
        /// </summary>
        /// <returns>The status page or null if we are not showing it </returns>
        private StatusPage? GetStatusPage
            (
            )
        {
            if (ContentPanel.Controls.Count == 0) return null;
            if (ContentPanel.Controls[0] is StatusPage) return ContentPanel.Controls[0] as StatusPage;

            return null;
        }

        /// <summary>
        /// Checks if we are showing the map and gets it if we are
        /// </summary>
        /// <returns>The map or null if we are not showing it</returns>
        private Map? GetMap
            (
            )
        {
            if (ContentPanel.Controls.Count == 0) return null;
            if (ContentPanel.Controls[0] is Map) return ContentPanel.Controls[0] as Map;

            return null;
        }

        /// <summary>
        /// Called perodically to attempt to connect to the controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ControllerConnectTimer_Tick(object? sender, EventArgs e)
        {
            // if no controller then try to connect
            if (!Controller.IsControllerFound)
            {
                ConnectToController();
            }
        }

        /// <summary>
        /// Sends a configuration to the controller
        /// </summary>
        private void ConfigureController
            (
            )
        {
            Controller.SetFrontBladeConfiguration(CurrentEquipmentSettings.FrontBlade);
            Controller.SetRearBladeConfiguration(CurrentEquipmentSettings.RearBlade);

            Controller.SetTractorAntennaLocation(CurrentEquipmentSettings.TractorAntennaHeightMm, CurrentEquipmentSettings.TractorAntennaLeftOffsetMm, CurrentEquipmentSettings.TractorAntennaForwardOffsetMm);
            Controller.SetFrontAntennaHeight(CurrentEquipmentSettings.FrontPan.AntennaHeightMm);
            Controller.SetRearAntennaHeight(CurrentEquipmentSettings.RearPan.AntennaHeightMm);

            Controller?.SetMagneticDeclination(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);
        }

        /// <summary>
        /// Called when height sensor has been lost
        /// </summary>
        /// <param name="Equip">The height sensor that was lost</param>
        private void Controller_OnHeightLost(EquipType Equip)
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnHeightLost, Equip);
                return;
            }

            switch (Equip)
            {
                case EquipType.Front: FrontHeightFound = false; break;
                case EquipType.Rear: RearHeightFound = false; break;
            }

            UpdateHeightLeds();
        }

        /// <summary>
        /// Called when height sensor has been found
        /// </summary>
        /// <param name="Equip">The height sensor that was found</param>
        private void Controller_OnHeightFound(EquipType Equip)
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnHeightFound, Equip);
                return;
            }

            switch (Equip)
            {
                case EquipType.Front: FrontHeightFound = true; break;
                case EquipType.Rear: RearHeightFound = true; break;
            }

            UpdateHeightLeds();
        }

        /// <summary>
        /// Called when an IMU has dissappeared from the network
        /// </summary>
        /// <param name="Equip">The IMU that was lost</param>
        private void Controller_OnIMULost(EquipType Equip)
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnIMULost, Equip);
                return;
            }

            switch (Equip)
            {
                case EquipType.Tractor:     TractorIMUFound     = false; break;
                case EquipType.Front:       FrontIMUFound       = false; break;
                case EquipType.FrontApron:  FrontApronIMUFound  = false; break;
                case EquipType.FrontBucket: FrontBucketIMUFound = false; break;
                case EquipType.Rear:        RearIMUFound        = false; break;
                case EquipType.RearBucket:  RearBucketIMUFound  = false; break;
            }

            UpdateIMULeds();
        }

        /// <summary>
        /// Called when an IMU has appeared on the network
        /// </summary>
        /// <param name="Equip">The IMU that was found</param>
        private void Controller_OnIMUFound(EquipType Equip)
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnIMUFound, Equip);
                return;
            }

            switch (Equip)
            {
                case EquipType.Tractor:     TractorIMUFound     = true; break;
                case EquipType.Front:       FrontIMUFound       = true; break;
                case EquipType.FrontApron:  FrontApronIMUFound  = true; break;
                case EquipType.FrontBucket: FrontBucketIMUFound = true; break;
                case EquipType.Rear:        RearIMUFound        = true; break;
                case EquipType.RearBucket:  RearBucketIMUFound  = true; break;
            }

            UpdateIMULeds();
        }

        #region LED Update
        /// <summary>
        /// Updates the height LEDs based on if they have been found or not
        /// </summary>
        private void UpdateHeightLeds
            (
            )
        {
            if (CurrentEquipmentSettings.FrontPan.Equipped)
            {
                if (FrontHeightFound)
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontHeight, StatusBar.LedState.OK);
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontHeight, StatusBar.LedState.Error);
                }
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.FrontHeight, StatusBar.LedState.Disabled);
            }

            if (CurrentEquipmentSettings.RearPan.Equipped)
            {
                if (RearHeightFound)
                {
                    StatusBar.SetLedState(StatusBar.Leds.RearHeight, StatusBar.LedState.OK);
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.RearHeight, StatusBar.LedState.Error);
                }
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.RearHeight, StatusBar.LedState.Disabled);
            }
        }

        /// <summary>
        /// Updates the RTK LEDs based on if they have been found or not
        /// </summary>
        private void UpdateRTKLeds
            (
            )
        {
            if (CurrentEquipmentStatus.TractorFix.RTK == RTKStatus.Fix)
            {
                StatusBar.SetLedState(StatusBar.Leds.TractorRTK, StatusBar.LedState.OK);
            }
            else if (CurrentEquipmentStatus.TractorFix.RTK == RTKStatus.Float)
            {
                StatusBar.SetLedState(StatusBar.Leds.TractorRTK, StatusBar.LedState.Warning);
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.TractorRTK, StatusBar.LedState.Error);
            }

            if (CurrentEquipmentSettings.FrontPan.Equipped)
            {
                if (CurrentEquipmentStatus.FrontPan.Fix.RTK == RTKStatus.Fix)
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontRTK, StatusBar.LedState.OK);
                }
                else if (CurrentEquipmentStatus.FrontPan.Fix.RTK == RTKStatus.Float)
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontRTK, StatusBar.LedState.Warning);
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontRTK, StatusBar.LedState.Error);
                }
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.FrontRTK, StatusBar.LedState.Disabled);
            }

            if (CurrentEquipmentSettings.RearPan.Equipped)
            {
                if (CurrentEquipmentStatus.RearPan.Fix.RTK == RTKStatus.Fix)
                {
                    StatusBar.SetLedState(StatusBar.Leds.RearRTK, StatusBar.LedState.OK);
                }
                else if (CurrentEquipmentStatus.RearPan.Fix.RTK == RTKStatus.Float)
                {
                    StatusBar.SetLedState(StatusBar.Leds.RearRTK, StatusBar.LedState.Warning);
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.RearRTK, StatusBar.LedState.Error);
                }
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.RearRTK, StatusBar.LedState.Disabled);
            }
        }

        /// <summary>
        /// Updates the IMU LEDs based on if they have been found or not
        /// </summary>
        private void UpdateIMULeds
            (
            )
        {
            if (TractorIMUFound)
            {
                if ((CurrentEquipmentStatus.TractorIMU.CalibrationStatus == IMUValue.Calibration.Good) ||
                    (CurrentEquipmentStatus.TractorIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                {
                    StatusBar.SetLedState(StatusBar.Leds.TractorIMU, StatusBar.LedState.OK);
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.TractorIMU, StatusBar.LedState.Warning);
                }
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.TractorIMU, StatusBar.LedState.Error);
            }

            if (CurrentEquipmentSettings.FrontPan.Equipped)
            {
                if (FrontIMUFound)
                {
                    if ((CurrentEquipmentStatus.FrontPan.IMU.CalibrationStatus == IMUValue.Calibration.Good) ||
                        (CurrentEquipmentStatus.FrontPan.IMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                    {
                        StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.OK);
                    }
                    else
                    {
                        StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.Warning);
                    }
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.Error);
                }

                if (FrontApronIMUFound)
                {
                    if ((CurrentEquipmentStatus.FrontPan.ApronIMU.CalibrationStatus == IMUValue.Calibration.Good) ||
                        (CurrentEquipmentStatus.FrontPan.ApronIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                    {
                        StatusBar.SetLedState(StatusBar.Leds.FrontApronIMU, StatusBar.LedState.OK);
                    }
                    else
                    {
                        StatusBar.SetLedState(StatusBar.Leds.FrontApronIMU, StatusBar.LedState.Warning);
                    }
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontApronIMU, StatusBar.LedState.Error);
                }

                if (FrontBucketIMUFound)
                {
                    if ((CurrentEquipmentStatus.FrontPan.BucketIMU.CalibrationStatus == IMUValue.Calibration.Good) ||
                        (CurrentEquipmentStatus.FrontPan.BucketIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                    {
                        StatusBar.SetLedState(StatusBar.Leds.FrontBucketIMU, StatusBar.LedState.OK);
                    }
                    else
                    {
                        StatusBar.SetLedState(StatusBar.Leds.FrontBucketIMU, StatusBar.LedState.Warning);
                    }
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontBucketIMU, StatusBar.LedState.Error);
                }
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.Disabled);
                StatusBar.SetLedState(StatusBar.Leds.FrontApronIMU, StatusBar.LedState.Disabled);
                StatusBar.SetLedState(StatusBar.Leds.FrontBucketIMU, StatusBar.LedState.Disabled);
            }

            if (CurrentEquipmentSettings.RearPan.Equipped)
            {
                if (RearIMUFound)
                {
                    if ((CurrentEquipmentStatus.RearPan.IMU.CalibrationStatus == IMUValue.Calibration.Good) ||
                        (CurrentEquipmentStatus.RearPan.IMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                    {
                        StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.OK);
                    }
                    else
                    {
                        StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.Warning);
                    }
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.Error);
                }

                if (RearBucketIMUFound)
                {
                    if ((CurrentEquipmentStatus.RearPan.BucketIMU.CalibrationStatus == IMUValue.Calibration.Good) ||
                        (CurrentEquipmentStatus.RearPan.BucketIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                    {
                        StatusBar.SetLedState(StatusBar.Leds.RearBucketIMU, StatusBar.LedState.OK);
                    }
                    else
                    {
                        StatusBar.SetLedState(StatusBar.Leds.RearBucketIMU, StatusBar.LedState.Warning);
                    }
                }
                else
                {
                    StatusBar.SetLedState(StatusBar.Leds.RearBucketIMU, StatusBar.LedState.Error);
                }
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.Disabled);
                StatusBar.SetLedState(StatusBar.Leds.RearBucketIMU, StatusBar.LedState.Disabled);
            }
        }

        /// <summary>
        /// Updates which LEDs are enabled based on the current equipment settings
        /// </summary>
        private void UpdateEnabledLeds
            (
            )
        {
            if (CurrentEquipmentSettings.FrontPan.Equipped)
            {
                StatusBar.SetLedState(StatusBar.Leds.FrontRTK, StatusBar.LedState.Error);
                StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.Error);
                StatusBar.SetLedState(StatusBar.Leds.FrontHeight, StatusBar.LedState.Error);
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.FrontRTK, StatusBar.LedState.Disabled);
                StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.Disabled);
                StatusBar.SetLedState(StatusBar.Leds.FrontHeight, StatusBar.LedState.Disabled);
            }

            if (CurrentEquipmentSettings.RearPan.Equipped)
            {
                StatusBar.SetLedState(StatusBar.Leds.RearRTK, StatusBar.LedState.Error);
                StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.Error);
                StatusBar.SetLedState(StatusBar.Leds.RearHeight, StatusBar.LedState.Error);
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.RearRTK, StatusBar.LedState.Disabled);
                StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.Disabled);
                StatusBar.SetLedState(StatusBar.Leds.RearHeight, StatusBar.LedState.Disabled);
            }
        }
        #endregion

        /// <summary>
        /// Called when user taps on the zoom in button
        /// Zooms into the map if we are displaying it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomInBtn_Click(object sender, EventArgs e)
        {
            GetMap()?.ZoomIn();
        }

        /// <summary>
        /// Called when user taps on the zoom out button
        /// Zooms out of the map if we are displaying it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomOutBtn_Click(object sender, EventArgs e)
        {
            GetMap()?.ZoomOut();
        }

        /// <summary>
        /// Called when user taps on the zoom to fit button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomFitBtn_Click(object sender, EventArgs e)
        {
            GetMap()?.ZoomToFit();
        }

        /// <summary>
        /// Called when the blade controller has stopped the rear blade from cutting
        /// </summary>
        private void BladeCtrl_OnRearStoppedCutting()
        {
            // make sure controller and UI match current state

            Controller.SetRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            SetRearPanIndicator(PanIndicatorStates.Transport);
        }

        /// <summary>
        /// Called when the blade controller has stopped the front blade from cutting
        /// </summary>
        private void BladeCtrl_OnFrontStoppedCutting()
        {
            // make sure controller and UI match current state

            Controller.SetFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);

            UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);

            SetFrontPanIndicator(PanIndicatorStates.Transport);
        }

        /// <summary>
        /// Called when user taps on the front blade control button
        /// Toggles auto on/off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrontBladeControlBtn_OnButtonClicked(object sender, EventArgs e)
        {
            // if manual then enter transport state
            if (CurrentEquipmentStatus.FrontPan.Mode == PanStatus.BladeMode.Manual)
            {
                BladeCtrl.SetFrontToTransportState();
                SetFrontPanIndicator(PanIndicatorStates.Transport);
            }
            // enter manual
            else
            {
                CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.Manual;
                SetFrontPanIndicator(PanIndicatorStates.None);
            }

            Controller.SetFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);

            UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
        }

        /// <summary>
        /// Called when user taps on the rear blade control button
        /// Toggles auto on/off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RearBladeControlBtn_OnButtonClicked(object sender, EventArgs e)
        {
            // if manual then enter transport state
            if (CurrentEquipmentStatus.RearPan.Mode == PanStatus.BladeMode.Manual)
            {
                BladeCtrl.SetRearToTransportState();
                SetRearPanIndicator(PanIndicatorStates.Transport);
            }
            // enter manual
            else
            {
                CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.Manual;
                SetRearPanIndicator(PanIndicatorStates.None);
            }

            Controller.SetRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
        }

        /// <summary>
        /// Called when the blade controller wants to request that the rear blade start cutting
        /// </summary>
        private void BladeCtrl_OnRequestRearBladeStartCutting()
        {
            if (CurrentEquipmentStatus.RearPan.Mode != PanStatus.BladeMode.Manual)
            {
                CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.AutoCutting;
                SetRearPanIndicator(PanIndicatorStates.Cutting);

                Controller.SetRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);

                UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
            }
            else
            {
                SetRearPanIndicator(PanIndicatorStates.None);
            }
        }

        /// <summary>
        /// Called when user taps on the button to open a field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFieldBtn_Click(object sender, EventArgs e)
        {
            ShowFieldChooserPage();
        }
    }
}
