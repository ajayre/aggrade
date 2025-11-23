using System.Runtime;
using System.Windows.Forms;
using AgGrade.Controls;
using AgGrade.Data;
using Controller;

namespace AgGrade
{
    public partial class MainForm : Form
    {
        private AppSettings CurrentAppSettings;
        private EquipmentSettings CurrentEquipmentSettings;
        private OGController controller;

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

            // connect to controller
            controller = new OGController();
            controller.Connect(CurrentAppSettings.ControllerAddress, CurrentAppSettings.Port, CurrentAppSettings.SubnetMask);

            ShowMap();

            // fixme - remove
            //StatusBar.SetLedState(StatusBar.Leds.TractorRTK, StatusBar.LedState.OK);
            //StatusBar.SetLedState(StatusBar.Leds.TractorIMU, StatusBar.LedState.Error);
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
                        AppSettings AppSettings = (Ctrl as AppSettingsEditor)!.GetSettings();
                        AppSettings.Save();
                        CurrentAppSettings = AppSettings;
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
                        EquipmentSettings EquipmentSettings = (Ctrl as EquipmentEditor)!.GetSettings();
                        EquipmentSettings.Save();
                        CurrentEquipmentSettings = EquipmentSettings;
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
        }
    }
}
