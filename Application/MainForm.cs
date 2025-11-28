using System.Runtime;
using System.Windows.Forms;
using System.Media;
using AgGrade.Controls;
using AgGrade.Data;
using AgGrade.Properties;
using Controller;

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

            TractorIMUFound = false;
            FrontIMUFound = false;
            RearIMUFound = false;

            FrontHeightFound = false;
            RearHeightFound = false;

            ShowMap();
        }

        /// <summary>
        /// Sounds an alarm to alert the operator
        /// </summary>
        private void SoundAlarm
            (
            )
        {
            SoundPlayer player = new SoundPlayer(Resources.mixkit_street_public_alarm_997);
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
        }

        private void Controller_OnControllerLost()
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnControllerLost);
                return;
            }

            StatusBar.SetLedState(StatusBar.Leds.Controller, StatusBar.LedState.Error);

            SoundAlarm();

            // start trying to connect
            ControllerConnectTimer.Start();
        }

        private void Controller_OnEmergencyStop()
        {
            if (InvokeRequired)
            {
                BeginInvoke(Controller_OnEmergencyStop);
                return;
            }

            StatusBar.ShowEStop = true;

            SoundAlarm();
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
            map.Show();

            ZoomInBtn.Enabled = true;
            ZoomOutBtn.Enabled = true;
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

            Controller.SetFrontBladeConfiguration(CurrentEquipmentSettings.FrontBlade);
            Controller.SetRearBladeConfiguration(CurrentEquipmentSettings.RearBlade);

            UpdateEnabledLeds();
            UpdateIMULeds();
            UpdateHeightLeds();
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
            Controller = new OGController();
            Controller.OnEmergencyStop += Controller_OnEmergencyStop;
            Controller.OnEmergencyStopCleared += Controller_OnEmergencyStopCleared;
            Controller.OnControllerLost += Controller_OnControllerLost;
            Controller.OnControllerFound += Controller_OnControllerFound;
            Controller.OnIMUFound += Controller_OnIMUFound;
            Controller.OnIMULost += Controller_OnIMULost;
            Controller.OnHeightFound += Controller_OnHeightFound;
            Controller.OnHeightLost += Controller_OnHeightLost;

            // initally we don't know if there is a controller or not
            // and we don't know status of tractor RTK and IMU
            StatusBar.SetLedState(StatusBar.Leds.Controller, StatusBar.LedState.Error);
            StatusBar.SetLedState(StatusBar.Leds.TractorRTK, StatusBar.LedState.Error);

            ControllerConnectTimer = new Timer();
            ControllerConnectTimer.Interval = CONTROLLER_TRY_CONNECT_PERIOD_MS;
            ControllerConnectTimer.Tick += ControllerConnectTimer_Tick;
            ControllerConnectTimer.Start();

            UpdateEnabledLeds();
            UpdateIMULeds();
            UpdateHeightLeds();
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
                case EquipType.Front:   FrontIMUFound   = false; break;
                case EquipType.Rear:    RearIMUFound    = false; break;
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
                case EquipType.Front:   FrontIMUFound   = true; break;
                case EquipType.Rear:    RearIMUFound    = true; break;
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
        /// Updates the IMU LEDs based on if they have been found or not
        /// </summary>
        private void UpdateIMULeds
            (
            )
        {
            if (TractorIMUFound)
            {
                StatusBar.SetLedState(StatusBar.Leds.TractorIMU, StatusBar.LedState.OK);
            }
            else
            {
                StatusBar.SetLedState(StatusBar.Leds.TractorIMU, StatusBar.LedState.Error);
            }

            if (CurrentEquipmentSettings.FrontPan.Equipped)
            {
                if (FrontIMUFound)
                {
                    StatusBar.SetLedState(StatusBar.Leds.FrontIMU, StatusBar.LedState.OK);
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
                    StatusBar.SetLedState(StatusBar.Leds.RearIMU, StatusBar.LedState.OK);
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
    }
}
