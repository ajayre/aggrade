using AgGrade.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
            Settings.TractorAntennaHeightMm = ValidateUInt(TractorAntennaHeight.Value, "Tractor Antenna Height");
            Settings.TractorAntennaLeftOffsetMm = TractorAntennaLeftOffset.Value; // Can be negative
            Settings.TractorAntennaForwardOffsetMm = TractorAntennaForwardOffset.Value; // Can be negative
            Settings.TractorTurningCircleM = ValidateUInt(TractorTurningCircle.Value, "Tractor Turning Circle");
            Settings.TractorWidthMm = ValidateUInt(TractorWidth.Value, "Tractor Width");

            // Parse Front Pan Settings
            Settings.FrontPan.Equipped = FrontPanEquipped.SelectedIndex == 1;
            Settings.FrontPan.AntennaHeightMm = ValidateUInt(FrontPanAntennaHeight.Value, "Front Pan Antenna Height");
            Settings.FrontPan.WidthMm = ValidateUInt(FrontPanWidth.Value, "Front Pan Width");
            Settings.FrontPan.EndofCutting = FrontPanEndofCutting.SelectedIndex == 0
                ? PanSettings.EndOfCuttingOptions.Float
                : PanSettings.EndOfCuttingOptions.Raise;
            Settings.FrontPan.MaxHeightMm = ValidateUInt(FrontPanMaxHeight.Value, "Front Pan Max Height");
            Settings.FrontPan.MaxCutDepthMm = ValidateUInt(FrontPanMaxCutDepth.Value, "Front Pan Max Cutting Depth");
            Settings.FrontPan.MaxFillDepthMm = ValidateUInt(FrontPanMaxFillDepth.Value, "Front Pan Max Fill Depth");
            Settings.FrontPan.CapacityCY = ValidateUInt(FrontPanCapacity.Value, "Front Pan Capacity");
            Settings.FrontPan.StopCuttingWhenFull = FrontPanStopCuttingWhenFull.SelectedIndex == 1;

            // Parse Rear Pan Settings
            Settings.RearPan.Equipped = RearPanEquipped.SelectedIndex == 1;
            Settings.RearPan.AntennaHeightMm = ValidateUInt(RearPanAntennaHeight.Value, "Rear Pan Antenna Height");
            Settings.RearPan.WidthMm = ValidateUInt(RearPanWidth.Value, "Rear Pan Width");
            Settings.RearPan.EndofCutting = RearPanEndofCutting.SelectedIndex == 0
                ? PanSettings.EndOfCuttingOptions.Float
                : PanSettings.EndOfCuttingOptions.Raise;
            Settings.RearPan.MaxHeightMm = ValidateUInt(RearPanMaxHeight.Value, "Rear Pan Max Height");
            Settings.RearPan.MaxCutDepthMm = ValidateUInt(RearPanMaxCutDepth.Value, "Rear Pan Max Cutting Depth");
            Settings.RearPan.MaxFillDepthMm = ValidateUInt(RearPanMaxFillDepth.Value, "Rear Pan Max Fill Depth");
            Settings.RearPan.CapacityCY = ValidateUInt(RearPanCapacity.Value, "Rear Pan Capacity");
            Settings.RearPan.StopCuttingWhenFull = RearPanStopCuttingWhenFull.SelectedIndex == 1;
            Settings.RearPan.AutoCutWhenFrontStops = RearPanAutoCutWhenFrontStops.SelectedIndex == 1;
            Settings.RearPan.BladeDistanceToFrontBladeMm = ValidateUInt(RearPanBladeDistanceToFrontBlade.Value, "Rear Pan Blade Dist to Front Blade");

            // Parse Front Blade PWM Settings
            Settings.FrontBlade.PWMGainUp = ValidateUInt(FrontBladePWMGainUp.Value, "Front Blade PWM Gain Up");
            Settings.FrontBlade.PWMGainDown = ValidateUInt(FrontBladePWMGainDown.Value, "Front Blade PWM Gain Down");
            Settings.FrontBlade.PWMMinUp = ValidateUInt(FrontBladePWMMinUp.Value, "Front Blade PWM Min Up");
            Settings.FrontBlade.PWMMinDown = ValidateUInt(FrontBladePWMMinDown.Value, "Front Blade PWM Min Down");
            Settings.FrontBlade.PWMMaxUp = ValidateUInt(FrontBladePWMMaxUp.Value, "Front Blade PWM Max Up");
            Settings.FrontBlade.PWMMaxDown = ValidateUInt(FrontBladePWMMaxDown.Value, "Front Blade PWM Max Down");
            Settings.FrontBlade.IntegralMultiplier = ValidateUInt(FrontBladeIntegralMultiplier.Value, "Front Blade Integral Multiplier");
            Settings.FrontBlade.Deadband = ValidateUInt(FrontBladeDeadband.Value, "Front Blade Deadband");

            // Parse Rear Blade PWM Settings
            Settings.RearBlade.PWMGainUp = ValidateUInt(RearBladePWMGainUp.Value, "Rear Blade PWM Gain Up");
            Settings.RearBlade.PWMGainDown = ValidateUInt(RearBladePWMGainDown.Value, "Rear Blade PWM Gain Down");
            Settings.RearBlade.PWMMinUp = ValidateUInt(RearBladePWMMinUp.Value, "Rear Blade PWM Min Up");
            Settings.RearBlade.PWMMinDown = ValidateUInt(RearBladePWMMinDown.Value, "Rear Blade PWM Min Down");
            Settings.RearBlade.PWMMaxUp = ValidateUInt(RearBladePWMMaxUp.Value, "Rear Blade PWM Max Up");
            Settings.RearBlade.PWMMaxDown = ValidateUInt(RearBladePWMMaxDown.Value, "Rear Blade PWM Max Down");
            Settings.RearBlade.IntegralMultiplier = ValidateUInt(RearBladeIntegralMultiplier.Value, "Rear Blade Integral Multiplier");
            Settings.RearBlade.Deadband = ValidateUInt(RearBladeDeadband.Value, "Rear Blade Deadband");

            // parse misc settings
            Settings.MinBinCoveragePcent = ValidateUInt(MinBinCoveragePcent.Value, "Minimum Bin Coverage");
            if (Settings.MinBinCoveragePcent > 100) Settings.MinBinCoveragePcent = 100;

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
            TractorAntennaHeight.Value = (int)Settings.TractorAntennaHeightMm;
            TractorAntennaLeftOffset.Value = Settings.TractorAntennaLeftOffsetMm;
            TractorAntennaForwardOffset.Value = Settings.TractorAntennaForwardOffsetMm;
            TractorTurningCircle.Value = (int)Settings.TractorTurningCircleM;
            TractorWidth.Value = (int)Settings.TractorWidthMm;

            // Display Front Pan Settings
            FrontPanEquipped.SelectedIndex = Settings.FrontPan.Equipped ? 1 : 0;
            FrontPanAntennaHeight.Value = (int)Settings.FrontPan.AntennaHeightMm;
            FrontPanWidth.Value = (int)Settings.FrontPan.WidthMm;
            FrontPanEndofCutting.SelectedIndex = Settings.FrontPan.EndofCutting == PanSettings.EndOfCuttingOptions.Float ? 0 : 1;
            FrontPanMaxHeight.Value = (int)Settings.FrontPan.MaxHeightMm;
            FrontPanMaxCutDepth.Value = (int)Settings.FrontPan.MaxCutDepthMm;
            FrontPanMaxFillDepth.Value = (int)Settings.FrontPan.MaxFillDepthMm;
            FrontPanCapacity.Value = (int)Settings.FrontPan.CapacityCY;
            FrontPanStopCuttingWhenFull.SelectedIndex = Settings.FrontPan.StopCuttingWhenFull ? 1 : 0;

            // Display Rear Pan Settings
            // Enforce constraint: if front pan is not equipped, rear pan must not be equipped
            bool rearPanEquipped = Settings.RearPan.Equipped && Settings.FrontPan.Equipped;
            RearPanEquipped.SelectedIndex = rearPanEquipped ? 1 : 0;
            RearPanAntennaHeight.Value = (int)Settings.RearPan.AntennaHeightMm;
            RearPanWidth.Value = (int)Settings.RearPan.WidthMm;
            RearPanEndofCutting.SelectedIndex = Settings.RearPan.EndofCutting == PanSettings.EndOfCuttingOptions.Float ? 0 : 1;
            RearPanMaxHeight.Value = (int)Settings.RearPan.MaxHeightMm;
            RearPanMaxCutDepth.Value = (int)Settings.RearPan.MaxCutDepthMm;
            RearPanMaxFillDepth.Value = (int)Settings.RearPan.MaxFillDepthMm;
            RearPanCapacity.Value = (int)Settings.RearPan.CapacityCY;
            RearPanStopCuttingWhenFull.SelectedIndex = Settings.RearPan.StopCuttingWhenFull ? 1 : 0;
            RearPanAutoCutWhenFrontStops.SelectedIndex = Settings.RearPan.AutoCutWhenFrontStops ? 1 : 0;
            RearPanBladeDistanceToFrontBlade.Value = (int)Settings.RearPan.BladeDistanceToFrontBladeMm;

            // Display Front Blade PWM Settings
            FrontBladePWMGainUp.Value = (int)Settings.FrontBlade.PWMGainUp;
            FrontBladePWMGainDown.Value = (int)Settings.FrontBlade.PWMGainDown;
            FrontBladePWMMinUp.Value = (int)Settings.FrontBlade.PWMMinUp;
            FrontBladePWMMinDown.Value = (int)Settings.FrontBlade.PWMMinDown;
            FrontBladePWMMaxUp.Value = (int)Settings.FrontBlade.PWMMaxUp;
            FrontBladePWMMaxDown.Value = (int)Settings.FrontBlade.PWMMaxDown;
            FrontBladeIntegralMultiplier.Value = (int)Settings.FrontBlade.IntegralMultiplier;
            FrontBladeDeadband.Value = (int)Settings.FrontBlade.Deadband;

            // Display Rear Blade PWM Settings
            RearBladePWMGainUp.Value = (int)Settings.RearBlade.PWMGainUp;
            RearBladePWMGainDown.Value = (int)Settings.RearBlade.PWMGainDown;
            RearBladePWMMinUp.Value = (int)Settings.RearBlade.PWMMinUp;
            RearBladePWMMinDown.Value = (int)Settings.RearBlade.PWMMinDown;
            RearBladePWMMaxUp.Value = (int)Settings.RearBlade.PWMMaxUp;
            RearBladePWMMaxDown.Value = (int)Settings.RearBlade.PWMMaxDown;
            RearBladeIntegralMultiplier.Value = (int)Settings.RearBlade.IntegralMultiplier;
            RearBladeDeadband.Value = (int)Settings.RearBlade.Deadband;

            // Display misc settings
            MinBinCoveragePcent.Value = (int)Settings.MinBinCoveragePcent;

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
            FrontPanMaxHeight.Enabled = isEquipped;
            FrontPanMaxCutDepth.Enabled = isEquipped;
            FrontPanMaxFillDepth.Enabled = isEquipped;
            FrontPanCapacity.Enabled = isEquipped;
            FrontPanStopCuttingWhenFull.Enabled = isEquipped;

            // Enable/disable associated labels
            FrontPanAntennaHeightUnitsLabel.Enabled = isEquipped;
            FrontPanAntennaHeightLabel.Enabled = isEquipped;
            FrontPanWidthUnitsLabel.Enabled = isEquipped;
            FrontPanWidthLabel.Enabled = isEquipped;
            FrontPanEndofCuttingLabel.Enabled = isEquipped;
            FrontPanMaxHeightUnitsLabel.Enabled = isEquipped;
            FrontPanMaxCutDepthLabel.Enabled = isEquipped;
            FrontPanMaxFillDepthLabel.Enabled = isEquipped;
            FrontPanMaxCutDepthUnitsLabel.Enabled = isEquipped;
            FrontPanMaxFillDepthUnitsLabel.Enabled = isEquipped;
            FrontPanCapacityLabel.Enabled = isEquipped;
            FrontPanCapacityUnitsLabel.Enabled = isEquipped;
            FrontPanStopCuttingWhenFullLabel.Enabled = isEquipped;
            FrontPanMaxHeightLabel.Enabled = isEquipped;

            // Enable/disable Front Blade PWM controls based on equipped status
            FrontBladePWMGainUp.Enabled = isEquipped;
            FrontBladePWMGainUpLabel.Enabled = isEquipped;
            FrontBladePWMMinUp.Enabled = isEquipped;
            FrontBladePWMMinUpLabel.Enabled = isEquipped;
            FrontBladePWMMaxUp.Enabled = isEquipped;
            FrontBladePWMMaxUpLabel.Enabled = isEquipped;
            FrontBladePWMGainDown.Enabled = isEquipped;
            FrontBladePWMGainDownLabel.Enabled = isEquipped;
            FrontBladePWMMinDown.Enabled = isEquipped;
            FrontBladePWMMinDownLabel.Enabled = isEquipped;
            FrontBladePWMMaxDown.Enabled = isEquipped;
            FrontBladePWMMaxDownLabel.Enabled = isEquipped;
            FrontBladeIntegralMultiplier.Enabled = isEquipped;
            FrontBladeIntegralMulLabel.Enabled = isEquipped;
            FrontBladeDeadband.Enabled = isEquipped;
            FrontBladeDeadbandLabel.Enabled = isEquipped;
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
            RearPanMaxHeight.Enabled = isEquipped;
            RearPanMaxCutDepth.Enabled = isEquipped;
            RearPanMaxFillDepth.Enabled = isEquipped;
            RearPanCapacity.Enabled = isEquipped;
            RearPanStopCuttingWhenFull.Enabled = isEquipped;
            RearPanAutoCutWhenFrontStops.Enabled = isEquipped;
            RearPanBladeDistanceToFrontBlade.Enabled = isEquipped;

            // Enable/disable associated labels
            RearPanAntennaHeightUnitsLabel.Enabled = isEquipped;
            RearPanAntennaHeightLabel.Enabled = isEquipped;
            RearPanWidthUnitsLabel.Enabled = isEquipped;
            RearPanWidthLabel.Enabled = isEquipped;
            RearPanEndofCuttingLabel.Enabled = isEquipped;
            RearPanMaxHeightUnitsLabel.Enabled = isEquipped;
            RearPanMaxCutDepthLabel.Enabled = isEquipped;
            RearPanMaxFillDepthLabel.Enabled = isEquipped;
            RearPanMaxCutDepthUnitsLabel.Enabled = isEquipped;
            RearPanMaxFillDepthUnitsLabel.Enabled = isEquipped;
            RearPanCapacityLabel.Enabled = isEquipped;
            RearPanCapacityUnitsLabel.Enabled = isEquipped;
            RearPanStopCuttingWhenFullLabel.Enabled = isEquipped;
            RearPanMaxHeightLabel.Enabled = isEquipped;
            RearPanAutoCutWhenFrontStopsLabel.Enabled = isEquipped;
            RearPanBladeDistanceToFrontBladeLabel.Enabled = isEquipped;

            // Enable/disable Rear Blade PWM controls based on equipped status
            RearBladePWMGainUp.Enabled = isEquipped;
            RearBladePWMGainUpLabel.Enabled = isEquipped;
            RearBladePWMMinUp.Enabled = isEquipped;
            RearBladePWMMinUpLabel.Enabled = isEquipped;
            RearBladePWMMaxUp.Enabled = isEquipped;
            RearBladePWMMaxUpLabel.Enabled = isEquipped;
            RearBladePWMGainDown.Enabled = isEquipped;
            RearBladePWMGainDownLabel.Enabled = isEquipped;
            RearBladePWMMinDown.Enabled = isEquipped;
            RearBladePWMMinDownLabel.Enabled = isEquipped;
            RearBladePWMMaxDown.Enabled = isEquipped;
            RearBladePWMMaxDownLabel.Enabled = isEquipped;
            RearBladeIntegralMultiplier.Enabled = isEquipped;
            RearBladeIntegralMulLabel.Enabled = isEquipped;
            RearBladeDeadband.Enabled = isEquipped;
            RearBladeDeadbandLabel.Enabled = isEquipped;
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
