using System.Runtime;
using System.Windows.Forms;
using System.Media;
using AgGrade.Controls;
using AgGrade.Data;
using AgGrade.Properties;
using AgGrade.Controller;
using System.IO.MemoryMappedFiles;

using Timer = System.Windows.Forms.Timer;

namespace AgGrade
{
    public partial class MainForm : Form
    {
        // how often to attempt to connect to the controller
        private const int CONTROLLER_TRY_CONNECT_PERIOD_MS = 200;

        private AppSettings CurrentAppSettings;
        private EquipmentSettings CurrentEquipmentSettings;
        private EquipmentStatus CurrentEquipmentStatus;
        private OGController Controller;
        private bool TractorIMUFound;
        private bool FrontIMUFound;
        private bool RearIMUFound;
        private bool FrontHeightFound;
        private bool RearHeightFound;
        private Timer ControllerConnectTimer;
        private BladeController BladeCtrl;
        private Field? CurrentField;
        private FieldUpdater FieldUpdater;

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

            BladeCtrl = new BladeController(Controller);
            BladeCtrl.SetEquipmentStatus(CurrentEquipmentStatus);
            BladeCtrl.OnFrontStoppedCutting += BladeCtrl_OnFrontStoppedCutting;
            BladeCtrl.OnRearStoppedCutting += BladeCtrl_OnRearStoppedCutting;
            BladeCtrl.OnRequestRearBladeStartCutting += BladeCtrl_OnRequestRearBladeStartCutting;

            TractorIMUFound = false;
            FrontIMUFound = false;
            RearIMUFound = false;

            FrontHeightFound = false;
            RearHeightFound = false;

            CurrentField = null;

            ShowMap();
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
            CurrentEquipmentStatus.RearPan.IMU.CalibrationStatus = IMUValue.Calibration.None;
            TractorIMUFound = false;
            FrontIMUFound = false;
            RearIMUFound = false;

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
            settingsEditor.OnPowerOff += () => { Close(); };
            settingsEditor.OnApplySettings += () => { ApplyAppSettings(settingsEditor); };

            // Load and display settings
            settingsEditor.ShowSettings(CurrentAppSettings);

            settingsEditor.Show();
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
            calibrationPage.Parent = ContentPanel;
            calibrationPage.Dock = DockStyle.Fill;
            calibrationPage.Show();
        }

        /// <summary>
        /// Shows the UI for surveying
        /// </summary>
        private void ShowSurveyPage
            (
            )
        {
            ClosePage();

            SurveyPage surveyPage = new SurveyPage();
            surveyPage.Parent = ContentPanel;
            surveyPage.Dock = DockStyle.Fill;
            surveyPage.Show();
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
            map.SetEquipmentSettings(CurrentEquipmentSettings);
            map.SetApplicationSettings(CurrentAppSettings);
            map.SetEquipmentStatus(CurrentEquipmentStatus);

            ZoomInBtn.Enabled = true;
            ZoomOutBtn.Enabled = true;

            if (CurrentField != null)
            {
                map.ShowField(CurrentField);
            }

            // give map initial conditions
            map.SetTractor(CurrentEquipmentStatus.TractorFix);
            map.SetFrontScraper(CurrentEquipmentStatus.FrontPan.Fix);
            map.SetRearScraper(CurrentEquipmentStatus.RearPan.Fix);

            // connect events
            map.OnResetPanLoad += Map_OnResetPanLoad;

            map.Show();
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
        /// Loads a field
        /// </summary>
        /// <param name="Folder">Path of the field data</param>
        private void LoadField
            (
            string Folder
            )
        {
            CurrentField = new Field();
            CurrentField.Load(Folder);

            BladeCtrl.SetField(CurrentField);
            FieldUpdater.SetField(CurrentField);

            // if showing map then update to show field
            GetMap()?.ShowField(CurrentField);
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

            Controller?.SetFrontBladeConfiguration(CurrentEquipmentSettings.FrontBlade);
            Controller?.SetRearBladeConfiguration(CurrentEquipmentSettings.RearBlade);

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
                }
            }

            ContentPanel.Controls.Clear();

            // disable controls specific to the map            
            ZoomInBtn.Enabled = false;
            ZoomOutBtn.Enabled = false;

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
            ShowSurveyPage();
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
            Controller.OnRearIMUChanged += Controller_OnRearIMUChanged;

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

            UpdateEnabledLeds();
            UpdateIMULeds();
            UpdateHeightLeds();
            UpdateRTKLeds();

            // fixme - allow user to choose file to load
            //LoadField(@"C:\Users\andy\OneDrive\Documents\AgGrade\Application\FieldData\ShopB4");
            LoadField(@"C:\Users\andy\OneDrive\Documents\AgGrade\Application\FieldData\TheShop2_2ft");

            // turn off indicators
            SetFrontPanIndicator(PanIndicatorStates.None);
            SetRearPanIndicator(PanIndicatorStates.None);
        }

        /// <summary>
        /// Called when rear scraper starts and stops dumping
        /// </summary>
        /// <param name="IsDumping">true if dumping</param>
        private void Controller_OnRearDumpingChanged(bool IsDumping)
        {
            if (IsDumping)
            {
                CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.AutoFilling;
                SetRearPanIndicator(PanIndicatorStates.Filling);
            }
            else
            {
                BladeCtrl.SetRearToTransportState();
                SetRearPanIndicator(PanIndicatorStates.None);
            }

            UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
        }

        /// <summary>
        /// Called when front scraper starts and stops dumping
        /// </summary>
        /// <param name="IsDumping">true if dumping</param>
        private void Controller_OnFrontDumpingChanged(bool IsDumping)
        {
            if (IsDumping)
            {
                CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.AutoFilling;
                SetFrontPanIndicator(PanIndicatorStates.Filling);
            }
            else
            {
                BladeCtrl.SetFrontToTransportState();
                SetFrontPanIndicator(PanIndicatorStates.None);
            }

            UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
        }

        /// <summary>
        /// Called when a new front blade height command has been sent to the controller
        /// Doesn't mean the blade is moving, only that the height has been requested
        /// </summary>
        /// <param name="Value">Blade height command 0 -> 200</param>
        private void Controller_OnFrontBladeCommandSent(uint Value)
        {
            // fixme - to do - show requested height
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
            if (IsCutting)
            {
                CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.AutoCutting;
                SetRearPanIndicator(PanIndicatorStates.Cutting);
            }
            else
            {
                BladeCtrl.SetRearToTransportState();
                SetRearPanIndicator(PanIndicatorStates.None);
            }

            UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
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
            if (Mode == PanStatus.BladeMode.AutoCutting)
            {
                CurrentEquipmentStatus.RearPan.CapacityWarningOccurred = false;
                FieldUpdater.StartRearCutting();
                RearBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Green;
            }
            else if (Mode == PanStatus.BladeMode.AutoFilling)
            {
                FieldUpdater.StartRearFilling();
                // fixme - what to show on button indicator?
            }
            else
            {
                RearBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Red;
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
        /// Possible states for the pan indicators
        /// </summary>
        private enum PanIndicatorStates
        {
            None,
            Cutting,
            Filling
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
            if (IsCutting)
            {
                CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.AutoCutting;
                SetFrontPanIndicator(PanIndicatorStates.Cutting);
            }
            else
            {
                BladeCtrl.SetFrontToTransportState();
                SetFrontPanIndicator(PanIndicatorStates.None);
            }

            UpdateFrontBladeMode(CurrentEquipmentStatus.FrontPan.Mode);
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
            if (Mode == PanStatus.BladeMode.AutoCutting)
            {
                CurrentEquipmentStatus.FrontPan.CapacityWarningOccurred = false;
                FieldUpdater.StartFrontCutting();
                FrontBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Green;
            }
            else if (Mode == PanStatus.BladeMode.AutoFilling)
            {
                FieldUpdater.StartFrontFilling();
                // fixme - what to show on button indicator?
            }
            else
            {
                FrontBladeControlBtn.Indicator = IndicatorButton.IndicatorColor.Red;
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
                case EquipType.Tractor: TractorIMUFound = false; break;
                case EquipType.Front: FrontIMUFound = false; break;
                case EquipType.Rear: RearIMUFound = false; break;
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
                case EquipType.Tractor: TractorIMUFound = true; break;
                case EquipType.Front: FrontIMUFound = true; break;
                case EquipType.Rear: RearIMUFound = true; break;
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
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.Disabled);
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
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.Disabled);
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

            Controller.SetRearBladeMode(CurrentEquipmentStatus.FrontPan.Mode);

            UpdateRearBladeMode(CurrentEquipmentStatus.FrontPan.Mode);

            SetRearPanIndicator(PanIndicatorStates.None);
        }

        /// <summary>
        /// Called when the blade controller has stopped the front blade from cutting
        /// </summary>
        private void BladeCtrl_OnFrontStoppedCutting()
        {
            // make sure controller and UI match current state

            Controller.SetFrontBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            UpdateFrontBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            SetFrontPanIndicator(PanIndicatorStates.None);
        }

        /// <summary>
        /// Called when user taps on the front blade control button
        /// Toggles auto on/off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrontBladeControlBtn_OnButtonClicked(object sender, EventArgs e)
        {
            if (CurrentEquipmentStatus.FrontPan.Mode != PanStatus.BladeMode.AutoCutting)
            {
                CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.AutoCutting;
                SetFrontPanIndicator(PanIndicatorStates.Cutting);
            }
            else
            {
                BladeCtrl.SetFrontToTransportState();
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
            if (CurrentEquipmentStatus.RearPan.Mode != PanStatus.BladeMode.AutoCutting)
            {
                CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.AutoCutting;
                SetRearPanIndicator(PanIndicatorStates.Cutting);
            }
            else
            {
                BladeCtrl.SetRearToTransportState();
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
            CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.AutoCutting;
            SetRearPanIndicator(PanIndicatorStates.Cutting);

            Controller.SetRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);

            UpdateRearBladeMode(CurrentEquipmentStatus.RearPan.Mode);
        }
    }
}
