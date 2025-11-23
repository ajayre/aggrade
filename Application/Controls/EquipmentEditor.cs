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
            Settings.FrontPanSettings.Equipped = FrontPanEquipped.SelectedIndex == 1;
            Settings.FrontPanSettings.AntennaHeightCm = ValidateUInt(FrontPanAntennaHeight.Value, "Front Pan Antenna Height");
            Settings.FrontPanSettings.WidthCm = ValidateUInt(FrontPanWidth.Value, "Front Pan Width");
            Settings.FrontPanSettings.EndofCutting = FrontPanEndofCutting.SelectedIndex == 0 
                ? PanSettings.EndOfCuttingOptions.Float 
                : PanSettings.EndOfCuttingOptions.Raise;
            Settings.FrontPanSettings.RaiseHeightMm = ValidateUInt(FrontPanRaiseHeight.Value, "Front Pan Raise Height");

            // Parse Rear Pan Settings
            Settings.RearPanSettings.Equipped = RearPanEquipped.SelectedIndex == 1;
            Settings.RearPanSettings.AntennaHeightCm = ValidateUInt(RearPanAntennaHeight.Value, "Rear Pan Antenna Height");
            Settings.RearPanSettings.WidthCm = ValidateUInt(RearPanWidth.Value, "Rear Pan Width");
            Settings.RearPanSettings.EndofCutting = RearPanEndofCutting.SelectedIndex == 0 
                ? PanSettings.EndOfCuttingOptions.Float 
                : PanSettings.EndOfCuttingOptions.Raise;
            Settings.RearPanSettings.RaiseHeightMm = ValidateUInt(RearPanRaiseHeight.Value, "Rear Pan Raise Height");

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
            FrontPanEquipped.SelectedIndex = Settings.FrontPanSettings.Equipped ? 1 : 0;
            FrontPanAntennaHeight.Value = (int)Settings.FrontPanSettings.AntennaHeightCm;
            FrontPanWidth.Value = (int)Settings.FrontPanSettings.WidthCm;
            FrontPanEndofCutting.SelectedIndex = Settings.FrontPanSettings.EndofCutting == PanSettings.EndOfCuttingOptions.Float ? 0 : 1;
            FrontPanRaiseHeight.Value = (int)Settings.FrontPanSettings.RaiseHeightMm;

            // Display Rear Pan Settings
            // Enforce constraint: if front pan is not equipped, rear pan must not be equipped
            bool rearPanEquipped = Settings.RearPanSettings.Equipped && Settings.FrontPanSettings.Equipped;
            RearPanEquipped.SelectedIndex = rearPanEquipped ? 1 : 0;
            RearPanAntennaHeight.Value = (int)Settings.RearPanSettings.AntennaHeightCm;
            RearPanWidth.Value = (int)Settings.RearPanSettings.WidthCm;
            RearPanEndofCutting.SelectedIndex = Settings.RearPanSettings.EndofCutting == PanSettings.EndOfCuttingOptions.Float ? 0 : 1;
            RearPanRaiseHeight.Value = (int)Settings.RearPanSettings.RaiseHeightMm;
            
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
            
            // Enable/disable associated labels
            FrontPanAntennaHeightUnitsLabel.Enabled = isEquipped;
            FrontPanAntennaHeightLabel.Enabled = isEquipped;
            FrontPanWidthUnitsLabel.Enabled = isEquipped;
            FrontPanWidthLabel.Enabled = isEquipped;
            FrontPanEndofCuttingLabel.Enabled = isEquipped;
            FrontPanRaiseUnitsLabel.Enabled = isEquipped && isRaise;
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
            
            // Enable/disable associated labels
            RearPanAntennaHeightUnitsLabel.Enabled = isEquipped;
            RearPanAntennaHeightLabel.Enabled = isEquipped;
            RearPanWidthUnitsLabel.Enabled = isEquipped;
            RearPanWidthLabel.Enabled = isEquipped;
            RearPanEndofCuttingLabel.Enabled = isEquipped;
            RearPanRaiseUnitsLabel.Enabled = isEquipped && isRaise;
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
    }
}
