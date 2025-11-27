using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AgGrade.Data;

namespace AgGrade.Controls
{
    public partial class StatusPage : UserControl
    {
        private EquipmentStatus? PreviousStatus = null;

        public StatusPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows the status values
        /// </summary>
        /// <param name="Status">Status to show</param>
        public void ShowStatus
            (
            EquipmentStatus Status
            )
        {
            // Update Tractor fields
            UpdateLocationTextBox(TractorLocation, Status.TractorLatitude, Status.TractorLongitude, PreviousStatus == null || PreviousStatus.TractorLatitude != Status.TractorLatitude || PreviousStatus.TractorLongitude != Status.TractorLongitude);
            UpdateRTKTextBox(TractorRTK, Status.TractorRTK, PreviousStatus == null || PreviousStatus.TractorRTK != Status.TractorRTK);
            UpdateTextBoxIfChanged(TractorPitch, FormatDouble(Status.TractorPitch), PreviousStatus == null || PreviousStatus.TractorPitch != Status.TractorPitch);
            UpdateTextBoxIfChanged(TractorRoll, FormatDouble(Status.TractorRoll), PreviousStatus == null || PreviousStatus.TractorRoll != Status.TractorRoll);
            UpdateTextBoxIfChanged(TractorYawRate, FormatDouble(Status.TractorYawRate), PreviousStatus == null || PreviousStatus.TractorYawRate != Status.TractorYawRate);
            UpdateTextBoxIfChanged(TractorHeading, FormatDouble(Status.TractorHeading), PreviousStatus == null || PreviousStatus.TractorHeading != Status.TractorHeading);
            UpdateIMUCalibrationTextBox(TractorIMUCalibrationStatus, Status.TractorIMUCalibrationStatus, PreviousStatus == null || PreviousStatus.TractorIMUCalibrationStatus != Status.TractorIMUCalibrationStatus);

            // Update Front Pan fields
            UpdateLocationTextBox(FrontPanLocation, Status.FrontPan.Latitude, Status.FrontPan.Longitude, PreviousStatus == null || PreviousStatus.FrontPan.Latitude != Status.FrontPan.Latitude || PreviousStatus.FrontPan.Longitude != Status.FrontPan.Longitude);
            UpdateRTKTextBox(FrontPanRTK, Status.FrontPan.RTK, PreviousStatus == null || PreviousStatus.FrontPan.RTK != Status.FrontPan.RTK);
            UpdateTextBoxIfChanged(FrontPanPitch, FormatDouble(Status.FrontPan.Pitch), PreviousStatus == null || PreviousStatus.FrontPan.Pitch != Status.FrontPan.Pitch);
            UpdateTextBoxIfChanged(FrontPanRoll, FormatDouble(Status.FrontPan.Roll), PreviousStatus == null || PreviousStatus.FrontPan.Roll != Status.FrontPan.Roll);
            UpdateTextBoxIfChanged(FrontPanYawRate, FormatDouble(Status.FrontPan.YawRate), PreviousStatus == null || PreviousStatus.FrontPan.YawRate != Status.FrontPan.YawRate);
            UpdateTextBoxIfChanged(FrontPanHeading, FormatDouble(Status.FrontPan.Heading), PreviousStatus == null || PreviousStatus.FrontPan.Heading != Status.FrontPan.Heading);
            UpdateTextBoxIfChanged(FrontPanBladeHeight, FormatDouble(Status.FrontPan.BladeHeight), PreviousStatus == null || PreviousStatus.FrontPan.BladeHeight != Status.FrontPan.BladeHeight);
            UpdateIMUCalibrationTextBox(FrontPanIMUCalibrationStatus, Status.FrontPan.IMUCalibrationStatus, PreviousStatus == null || PreviousStatus.FrontPan.IMUCalibrationStatus != Status.FrontPan.IMUCalibrationStatus);

            // Update Rear Pan fields
            UpdateLocationTextBox(RearPanLocation, Status.RearPan.Latitude, Status.RearPan.Longitude, PreviousStatus == null || PreviousStatus.RearPan.Latitude != Status.RearPan.Latitude || PreviousStatus.RearPan.Longitude != Status.RearPan.Longitude);
            UpdateRTKTextBox(RearPanRTK, Status.RearPan.RTK, PreviousStatus == null || PreviousStatus.RearPan.RTK != Status.RearPan.RTK);
            UpdateTextBoxIfChanged(RearPanPitch, FormatDouble(Status.RearPan.Pitch), PreviousStatus == null || PreviousStatus.RearPan.Pitch != Status.RearPan.Pitch);
            UpdateTextBoxIfChanged(RearPanRoll, FormatDouble(Status.RearPan.Roll), PreviousStatus == null || PreviousStatus.RearPan.Roll != Status.RearPan.Roll);
            UpdateTextBoxIfChanged(RearPanYawRate, FormatDouble(Status.RearPan.YawRate), PreviousStatus == null || PreviousStatus.RearPan.YawRate != Status.RearPan.YawRate);
            UpdateTextBoxIfChanged(RearPanHeading, FormatDouble(Status.RearPan.Heading), PreviousStatus == null || PreviousStatus.RearPan.Heading != Status.RearPan.Heading);
            UpdateTextBoxIfChanged(RearPanBladeHeight, FormatDouble(Status.RearPan.BladeHeight), PreviousStatus == null || PreviousStatus.RearPan.BladeHeight != Status.RearPan.BladeHeight);
            UpdateIMUCalibrationTextBox(RearPanIMUCalibrationStatus, Status.RearPan.IMUCalibrationStatus, PreviousStatus == null || PreviousStatus.RearPan.IMUCalibrationStatus != Status.RearPan.IMUCalibrationStatus);

            // Store current status for next comparison
            PreviousStatus = new EquipmentStatus
            {
                TractorLatitude = Status.TractorLatitude,
                TractorLongitude = Status.TractorLongitude,
                TractorPitch = Status.TractorPitch,
                TractorRoll = Status.TractorRoll,
                TractorHeading = Status.TractorHeading,
                TractorYawRate = Status.TractorYawRate,
                TractorRTK = Status.TractorRTK,
                TractorIMUCalibrationStatus = Status.TractorIMUCalibrationStatus,
                FrontPan = new PanStatus
                {
                    Latitude = Status.FrontPan.Latitude,
                    Longitude = Status.FrontPan.Longitude,
                    Pitch = Status.FrontPan.Pitch,
                    Roll = Status.FrontPan.Roll,
                    Heading = Status.FrontPan.Heading,
                    YawRate = Status.FrontPan.YawRate,
                    RTK = Status.FrontPan.RTK,
                    BladeHeight = Status.FrontPan.BladeHeight,
                    IMUCalibrationStatus = Status.FrontPan.IMUCalibrationStatus
                },
                RearPan = new PanStatus
                {
                    Latitude = Status.RearPan.Latitude,
                    Longitude = Status.RearPan.Longitude,
                    Pitch = Status.RearPan.Pitch,
                    Roll = Status.RearPan.Roll,
                    Heading = Status.RearPan.Heading,
                    YawRate = Status.RearPan.YawRate,
                    RTK = Status.RearPan.RTK,
                    BladeHeight = Status.RearPan.BladeHeight,
                    IMUCalibrationStatus = Status.RearPan.IMUCalibrationStatus
                }
            };
        }

        /// <summary>
        /// Formats a double value with three decimal places
        /// </summary>
        private string FormatDouble(double value)
        {
            return value.ToString("F3");
        }

        /// <summary>
        /// Formats location as latitude and longitude with three decimal places
        /// </summary>
        private string FormatLocation(double latitude, double longitude)
        {
            return $"{latitude:F3} {longitude:F3}";
        }

        /// <summary>
        /// Updates a textbox only if the value has changed
        /// </summary>
        private void UpdateTextBoxIfChanged(TextBox textBox, string newValue, bool hasChanged)
        {
            if (hasChanged)
            {
                textBox.Text = newValue;
            }
        }

        /// <summary>
        /// Updates location textbox with text and color based on coordinates
        /// If both latitude and longitude are zero, sets background to red and foreground to white
        /// Otherwise uses default textbox colors
        /// </summary>
        private void UpdateLocationTextBox(TextBox textBox, double latitude, double longitude, bool hasChanged)
        {
            if (hasChanged)
            {
                textBox.Text = FormatLocation(latitude, longitude);

                // If both coordinates are zero, set red background and white foreground
                // Otherwise use default textbox colors
                if (latitude == 0.0 && longitude == 0.0)
                {
                    textBox.BackColor = Color.Red;
                    textBox.ForeColor = Color.White;
                }
                else
                {
                    textBox.BackColor = SystemColors.Window;
                    textBox.ForeColor = SystemColors.WindowText;
                }
            }
        }

        /// <summary>
        /// Updates RTK textbox with text and background color based on RTK status
        /// </summary>
        private void UpdateRTKTextBox(TextBox textBox, RTKStatus rtkStatus, bool hasChanged)
        {
            if (hasChanged)
            {
                string rtkText = rtkStatus.ToString();
                textBox.Text = rtkText;

                // Set background color based on RTK status
                switch (rtkStatus)
                {
                    case RTKStatus.None:
                        textBox.BackColor = Color.Red;
                        textBox.ForeColor = Color.White;
                        break;
                    case RTKStatus.Float:
                        textBox.BackColor = Color.Orange;
                        textBox.ForeColor = Color.Black;
                        break;
                    case RTKStatus.Fix:
                        textBox.BackColor = Color.Green;
                        textBox.ForeColor = Color.Black;
                        break;
                }
            }
        }

        /// <summary>
        /// Updates IMU calibration status textbox with text and background color
        /// </summary>
        private void UpdateIMUCalibrationTextBox(TextBox textBox, uint calibrationStatus, bool hasChanged)
        {
            if (hasChanged)
            {
                textBox.Text = calibrationStatus.ToString();

                // Set background color based on IMU calibration status
                // 3 = green, 2 = orange, anything else = red
                if (calibrationStatus == 3)
                {
                    textBox.BackColor = Color.Green;
                    textBox.ForeColor = Color.Black;
                }
                else if (calibrationStatus == 2)
                {
                    textBox.BackColor = Color.Orange;
                    textBox.ForeColor = Color.Black;
                }
                else
                {
                    textBox.BackColor = Color.Red;
                    textBox.ForeColor = Color.White;
                }
            }
        }
    }
}
