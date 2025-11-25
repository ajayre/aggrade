using AgGrade.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class EquipmentEditor : UserControl
    {
        public event Action OnApplySettings = null;

        public EquipmentEditor()
        {
            InitializeComponent();

            // Wire up event handlers
            FrontPanEquipped.SelectedIndexChanged += FrontPanEquipped_SelectedIndexChanged;
            FrontPanEndofCutting.SelectedIndexChanged += FrontPanEndofCutting_SelectedIndexChanged;
            RearPanEquipped.SelectedIndexChanged += RearPanEquipped_SelectedIndexChanged;
            RearPanEndofCutting.SelectedIndexChanged += RearPanEndofCutting_SelectedIndexChanged;
        }

        private void EquipmentEditor_Load(object sender, EventArgs e)
        {
            // Update UI state when form loads
            UpdateFrontPanUI();
            UpdateRearPanUI();
        }

        /// <summary>
        /// Validates a uint value (must be non-negative)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="fieldName">Name of the field for error messages</param>
        /// <returns>The validated uint value</returns>
        /// <exception cref="ArgumentException">Thrown if value is negative</exception>
        private uint ValidateUInt(int value, string fieldName)
        {
            if (value < 0)
            {
                throw new ArgumentException($"{fieldName} cannot be negative.");
            }

            return (uint)value;
        }

        /// <summary>
        /// Gets the current settings from the UI with validation
        /// </summary>
        /// <returns>Current settings</returns>
        /// <exception cref="ArgumentException">Thrown if any input validation fails</exception>
        public EquipmentSettings GetSettings
            (
            )
        {
            EquipmentSettings Settings = new EquipmentSettings();

            // Validate and parse tractor fields
            Settings.TractorAntennaHeightCm = ValidateUInt(TractorAntennaHeight.Value, "Tractor Antenna Height");
            Settings.TractorAntennaLeftOffsetCm = TractorAntennaLeftOffset.Value; // Can be negative
            Settings.TractorAntennaForwardOffsetCm = TractorAntennaForwardOffset.Value; // Can be negative
            Settings.TractorTurningCircleFt = ValidateUInt(TractorTurningCircle.Value, "Tractor Turning Circle");
            Settings.TractorWidthCm = ValidateUInt(TractorWidth.Value, "Tractor Width");

            // Parse Front Pan Settings
            Settings.FrontPan.Equipped = FrontPanEquipped.SelectedIndex == 1;
            Settings.FrontPan.AntennaHeightCm = ValidateUInt(FrontPanAntennaHeight.Value, "Front Pan Antenna Height");
            Settings.FrontPan.WidthCm = ValidateUInt(FrontPanWidth.Value, "Front Pan Width");
            Settings.FrontPan.EndofCutting = FrontPanEndofCutting.SelectedIndex == 0
                ? PanSettings.EndOfCuttingOptions.Float
                : PanSettings.EndOfCuttingOptions.Raise;
            Settings.FrontPan.RaiseHeightMm = ValidateUInt(FrontPanRaiseHeight.Value, "Front Pan Raise Height");
            Settings.FrontPan.MaxCutDepthCm = ValidateUInt(FrontPanMaxCutDepth.Value, "Front Pan Max Cutting Depth");

            // Parse Rear Pan Settings
            Settings.RearPan.Equipped = RearPanEquipped.SelectedIndex == 1;
            Settings.RearPan.AntennaHeightCm = ValidateUInt(RearPanAntennaHeight.Value, "Rear Pan Antenna Height");
            Settings.RearPan.WidthCm = ValidateUInt(RearPanWidth.Value, "Rear Pan Width");
            Settings.RearPan.EndofCutting = RearPanEndofCutting.SelectedIndex == 0
                ? PanSettings.EndOfCuttingOptions.Float
                : PanSettings.EndOfCuttingOptions.Raise;
            Settings.RearPan.RaiseHeightMm = ValidateUInt(RearPanRaiseHeight.Value, "Rear Pan Raise Height");
            Settings.RearPan.MaxCutDepthCm = ValidateUInt(RearPanMaxCutDepth.Value, "Rear Pan Max Cutting Depth");

            return Settings;
        }

        /// <summary>
        /// Displays the settings in the UI controls
        /// </summary>
        /// <param name="Settings">The settings to display</param>
        public void ShowSettings
            (
            EquipmentSettings Settings
            )
        {
            // Display tractor fields
            TractorAntennaHeight.Value = (int)Settings.TractorAntennaHeightCm;
            TractorAntennaLeftOffset.Value = Settings.TractorAntennaLeftOffsetCm;
            TractorAntennaForwardOffset.Value = Settings.TractorAntennaForwardOffsetCm;
            TractorTurningCircle.Value = (int)Settings.TractorTurningCircleFt;
            TractorWidth.Value = (int)Settings.TractorWidthCm;

            // Display Front Pan Settings
            FrontPanEquipped.SelectedIndex = Settings.FrontPan.Equipped ? 1 : 0;
            FrontPanAntennaHeight.Value = (int)Settings.FrontPan.AntennaHeightCm;
            FrontPanWidth.Value = (int)Settings.FrontPan.WidthCm;
            FrontPanEndofCutting.SelectedIndex = Settings.FrontPan.EndofCutting == PanSettings.EndOfCuttingOptions.Float ? 0 : 1;
            FrontPanRaiseHeight.Value = (int)Settings.FrontPan.RaiseHeightMm;
            FrontPanMaxCutDepth.Value = (int)Settings.FrontPan.MaxCutDepthCm;

            // Display Rear Pan Settings
            // Enforce constraint: if front pan is not equipped, rear pan must not be equipped
            bool rearPanEquipped = Settings.RearPan.Equipped && Settings.FrontPan.Equipped;
            RearPanEquipped.SelectedIndex = rearPanEquipped ? 1 : 0;
            RearPanAntennaHeight.Value = (int)Settings.RearPan.AntennaHeightCm;
            RearPanWidth.Value = (int)Settings.RearPan.WidthCm;
            RearPanEndofCutting.SelectedIndex = Settings.RearPan.EndofCutting == PanSettings.EndOfCuttingOptions.Float ? 0 : 1;
            RearPanRaiseHeight.Value = (int)Settings.RearPan.RaiseHeightMm;
            RearPanMaxCutDepth.Value = (int)Settings.RearPan.MaxCutDepthCm;

            // Update UI state after displaying settings
            UpdateFrontPanUI();
            UpdateRearPanUI();
        }

        /// <summary>
        /// Updates the UI state for the Front Pan based on current selections
        /// </summary>
        private void UpdateFrontPanUI()
        {
            bool isEquipped = FrontPanEquipped.SelectedIndex == 1;
            bool isRaise = FrontPanEndofCutting.SelectedIndex == 1;

            // Enable/disable all pan controls based on equipped status
            FrontPanAntennaHeight.Enabled = isEquipped;
            FrontPanWidth.Enabled = isEquipped;
            FrontPanEndofCutting.Enabled = isEquipped;
            FrontPanRaiseHeight.Enabled = isEquipped && isRaise;
            FrontPanMaxCutDepth.Enabled = isEquipped;

            // Enable/disable associated labels
            FrontPanAntennaHeightUnitsLabel.Enabled = isEquipped;
            FrontPanAntennaHeightLabel.Enabled = isEquipped;
            FrontPanWidthUnitsLabel.Enabled = isEquipped;
            FrontPanWidthLabel.Enabled = isEquipped;
            FrontPanEndofCuttingLabel.Enabled = isEquipped;
            FrontPanRaiseUnitsLabel.Enabled = isEquipped && isRaise;
            FrontPanMaxCutDepthLabel.Enabled = isEquipped;
        }

        /// <summary>
        /// Updates the UI state for the Rear Pan based on current selections
        /// </summary>
        private void UpdateRearPanUI()
        {
            bool frontPanEquipped = FrontPanEquipped.SelectedIndex == 1;
            bool isEquipped = RearPanEquipped.SelectedIndex == 1;
            bool isRaise = RearPanEndofCutting.SelectedIndex == 1;

            // Rear pan can only be equipped if front pan is equipped
            RearPanEquipped.Enabled = frontPanEquipped;
            RearPanEquippedLabel.Enabled = frontPanEquipped;

            // Enable/disable all pan controls based on equipped status
            RearPanAntennaHeight.Enabled = isEquipped;
            RearPanWidth.Enabled = isEquipped;
            RearPanEndofCutting.Enabled = isEquipped;
            RearPanRaiseHeight.Enabled = isEquipped && isRaise;
            RearPanMaxCutDepth.Enabled = isEquipped;

            // Enable/disable associated labels
            RearPanAntennaHeightUnitsLabel.Enabled = isEquipped;
            RearPanAntennaHeightLabel.Enabled = isEquipped;
            RearPanWidthUnitsLabel.Enabled = isEquipped;
            RearPanWidthLabel.Enabled = isEquipped;
            RearPanEndofCuttingLabel.Enabled = isEquipped;
            RearPanRaiseUnitsLabel.Enabled = isEquipped && isRaise;
            RearPanMaxCutDepthLabel.Enabled = isEquipped;
        }

        /// <summary>
        /// Called when Front Pan Equipped selection changes
        /// </summary>
        private void FrontPanEquipped_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If front pan is not equipped, set rear pan to not equipped
            if (FrontPanEquipped.SelectedIndex == 0)
            {
                // Temporarily remove event handler to prevent recursion
                RearPanEquipped.SelectedIndexChanged -= RearPanEquipped_SelectedIndexChanged;
                RearPanEquipped.SelectedIndex = 0;
                RearPanEquipped.SelectedIndexChanged += RearPanEquipped_SelectedIndexChanged;
            }

            UpdateFrontPanUI();
            UpdateRearPanUI();
        }

        /// <summary>
        /// Called when Front Pan End of Cutting selection changes
        /// </summary>
        private void FrontPanEndofCutting_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFrontPanUI();
        }

        /// <summary>
        /// Called when Rear Pan Equipped selection changes
        /// </summary>
        private void RearPanEquipped_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateRearPanUI();
        }

        /// <summary>
        /// Called when Rear Pan End of Cutting selection changes
        /// </summary>
        private void RearPanEndofCutting_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateRearPanUI();
        }

        /// <summary>
        /// Called when user taps on the apply button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyBtn_Click(object sender, EventArgs e)
        {
            OnApplySettings?.Invoke();
        }
    }
}
