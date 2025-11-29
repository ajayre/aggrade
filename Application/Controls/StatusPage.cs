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
using AgGrade.Controller;

namespace AgGrade.Controls
{
    public partial class StatusPage : UserControl
    {
        private const double KPH_TO_MPH = 0.621371;
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
            EquipmentStatus Status,
            AppSettings Settings
            )
        {
            if (InvokeRequired)
            {
                BeginInvoke(ShowStatus, Status, Settings);
                return;
            }

            // Update Tractor fields
            UpdateLocationTextBox(TractorLocation, Status.TractorFix.Latitude, Status.TractorFix.Longitude, PreviousStatus == null || PreviousStatus.TractorFix.Latitude != Status.TractorFix.Latitude || PreviousStatus.TractorFix.Longitude != Status.TractorFix.Longitude);
            UpdateRTKTextBox(TractorRTK, Status.TractorFix.RTK, PreviousStatus == null || PreviousStatus.TractorFix.RTK != Status.TractorFix.RTK);
            UpdateTextBoxIfChanged(TractorPitch, FormatDouble(Status.TractorIMU.Pitch), PreviousStatus == null || PreviousStatus.TractorIMU.Pitch != Status.TractorIMU.Pitch);
            UpdateTextBoxIfChanged(TractorRoll, FormatDouble(Status.TractorIMU.Roll), PreviousStatus == null || PreviousStatus.TractorIMU.Roll != Status.TractorIMU.Roll);
            UpdateTextBoxIfChanged(TractorYawRate, FormatDouble(Status.TractorIMU.YawRate), PreviousStatus == null || PreviousStatus.TractorIMU.YawRate != Status.TractorIMU.YawRate);
            UpdateTextBoxIfChanged(TractorHeading, FormatDouble(Status.TractorIMU.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.TractorIMU.Heading != Status.TractorIMU.Heading);
            UpdateTextBoxIfChanged(TractorSpeed, FormatDouble(Status.TractorFix.Vector.SpeedMph), PreviousStatus == null || PreviousStatus.TractorFix.Vector.Speedkph != Status.TractorFix.Vector.Speedkph);
            UpdateTextBoxIfChanged(TractorGNSSHeading, FormatDouble(Status.TractorFix.Vector.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.TractorFix.Vector.TrackMagneticDeg != Status.TractorFix.Vector.TrackMagneticDeg);
            UpdateTextBoxIfChanged(TractorAltitude, FormatDouble(Status.TractorFix.Altitude), PreviousStatus == null || PreviousStatus.TractorFix.Altitude != Status.TractorFix.Altitude);
            UpdateIMUCalibrationTextBox(TractorIMUCalibrationStatus, Status.TractorIMU.CalibrationStatus, PreviousStatus == null || PreviousStatus.TractorIMU.CalibrationStatus != Status.TractorIMU.CalibrationStatus);

            // Update Front Pan fields
            UpdateLocationTextBox(FrontPanLocation, Status.FrontPan.Fix.Latitude, Status.FrontPan.Fix.Longitude, PreviousStatus == null || PreviousStatus.FrontPan.Fix.Latitude != Status.FrontPan.Fix.Latitude || PreviousStatus.FrontPan.Fix.Longitude != Status.FrontPan.Fix.Longitude);
            UpdateRTKTextBox(FrontPanRTK, Status.FrontPan.Fix.RTK, PreviousStatus == null || PreviousStatus.FrontPan.Fix.RTK != Status.FrontPan.Fix.RTK);
            UpdateTextBoxIfChanged(FrontPanPitch, FormatDouble(Status.FrontPan.IMU.Pitch), PreviousStatus == null || PreviousStatus.FrontPan.IMU.Pitch != Status.FrontPan.IMU.Pitch);
            UpdateTextBoxIfChanged(FrontPanRoll, FormatDouble(Status.FrontPan.IMU.Roll), PreviousStatus == null || PreviousStatus.FrontPan.IMU.Roll != Status.FrontPan.IMU.Roll);
            UpdateTextBoxIfChanged(FrontPanYawRate, FormatDouble(Status.FrontPan.IMU.YawRate), PreviousStatus == null || PreviousStatus.FrontPan.IMU.YawRate != Status.FrontPan.IMU.YawRate);
            UpdateTextBoxIfChanged(FrontPanHeading, FormatDouble(Status.FrontPan.IMU.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.FrontPan.IMU.Heading != Status.FrontPan.IMU.Heading);
            UpdateTextBoxIfChanged(FrontPanBladeHeight, Status.FrontPan.BladeHeight.ToString(), PreviousStatus == null || PreviousStatus.FrontPan.BladeHeight != Status.FrontPan.BladeHeight);
            UpdateTextBoxIfChanged(FrontPanBladeOffset, Status.FrontPan.BladeOffset.ToString(), PreviousStatus == null || PreviousStatus.FrontPan.BladeOffset != Status.FrontPan.BladeOffset);
            UpdateTextBoxIfChanged(FrontPanBladeAuto, Status.FrontPan.BladeAuto ? "Yes" : "No", PreviousStatus == null || PreviousStatus.FrontPan.BladeAuto != Status.FrontPan.BladeAuto);
            UpdateTextBoxIfChanged(FrontPanBladePWM, Status.FrontPan.BladePWM.ToString(), PreviousStatus == null || PreviousStatus.FrontPan.BladePWM != Status.FrontPan.BladePWM);
            UpdateTextBoxIfChanged(FrontPanBladeDirection, Status.FrontPan.Direction.ToString(), PreviousStatus == null || PreviousStatus.FrontPan.Direction != Status.FrontPan.Direction);
            UpdateTextBoxIfChanged(FrontPanSpeed, FormatDouble(Status.FrontPan.Fix.Vector.SpeedMph), PreviousStatus == null || PreviousStatus.FrontPan.Fix.Vector.Speedkph != Status.FrontPan.Fix.Vector.Speedkph);
            UpdateTextBoxIfChanged(FrontPanGNSSHeading, FormatDouble(Status.FrontPan.Fix.Vector.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.FrontPan.Fix.Vector.TrackMagneticDeg != Status.FrontPan.Fix.Vector.TrackMagneticDeg);
            UpdateTextBoxIfChanged(FrontPanAltitude, FormatDouble(Status.FrontPan.Fix.Altitude), PreviousStatus == null || PreviousStatus.FrontPan.Fix.Altitude != Status.FrontPan.Fix.Altitude);
            UpdateIMUCalibrationTextBox(FrontPanIMUCalibrationStatus, Status.FrontPan.IMU.CalibrationStatus, PreviousStatus == null || PreviousStatus.FrontPan.IMU.CalibrationStatus != Status.FrontPan.IMU.CalibrationStatus);

            // Update Rear Pan fields
            UpdateLocationTextBox(RearPanLocation, Status.RearPan.Fix.Latitude, Status.RearPan.Fix.Longitude, PreviousStatus == null || PreviousStatus.RearPan.Fix.Latitude != Status.RearPan.Fix.Latitude || PreviousStatus.RearPan.Fix.Longitude != Status.RearPan.Fix.Longitude);
            UpdateRTKTextBox(RearPanRTK, Status.RearPan.Fix.RTK, PreviousStatus == null || PreviousStatus.RearPan.Fix.RTK != Status.RearPan.Fix.RTK);
            UpdateTextBoxIfChanged(RearPanPitch, FormatDouble(Status.RearPan.IMU.Pitch), PreviousStatus == null || PreviousStatus.RearPan.IMU.Pitch != Status.RearPan.IMU.Pitch);
            UpdateTextBoxIfChanged(RearPanRoll, FormatDouble(Status.RearPan.IMU.Roll), PreviousStatus == null || PreviousStatus.RearPan.IMU.Roll != Status.RearPan.IMU.Roll);
            UpdateTextBoxIfChanged(RearPanYawRate, FormatDouble(Status.RearPan.IMU.YawRate), PreviousStatus == null || PreviousStatus.RearPan.IMU.YawRate != Status.RearPan.IMU.YawRate);
            UpdateTextBoxIfChanged(RearPanHeading, FormatDouble(Status.RearPan.IMU.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.RearPan.IMU.Heading != Status.RearPan.IMU.Heading);
            UpdateTextBoxIfChanged(RearPanBladeHeight, Status.RearPan.BladeHeight.ToString(), PreviousStatus == null || PreviousStatus.RearPan.BladeHeight != Status.RearPan.BladeHeight);
            UpdateTextBoxIfChanged(RearPanBladeOffset, Status.RearPan.BladeOffset.ToString(), PreviousStatus == null || PreviousStatus.RearPan.BladeOffset != Status.RearPan.BladeOffset);
            UpdateTextBoxIfChanged(RearPanBladeAuto, Status.RearPan.BladeAuto ? "Yes" : "No", PreviousStatus == null || PreviousStatus.RearPan.BladeAuto != Status.RearPan.BladeAuto);
            UpdateTextBoxIfChanged(RearPanBladePWM, Status.RearPan.BladePWM.ToString(), PreviousStatus == null || PreviousStatus.RearPan.BladePWM != Status.RearPan.BladePWM);
            UpdateTextBoxIfChanged(RearPanBladeDirection, Status.RearPan.Direction.ToString(), PreviousStatus == null || PreviousStatus.RearPan.Direction != Status.RearPan.Direction);
            UpdateTextBoxIfChanged(RearPanSpeed, FormatDouble(Status.RearPan.Fix.Vector.SpeedMph), PreviousStatus == null || PreviousStatus.RearPan.Fix.Vector.Speedkph != Status.RearPan.Fix.Vector.Speedkph);
            UpdateTextBoxIfChanged(RearPanGNSSHeading, FormatDouble(Status.RearPan.Fix.Vector.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.RearPan.Fix.Vector.TrackMagneticDeg != Status.RearPan.Fix.Vector.TrackMagneticDeg);
            UpdateTextBoxIfChanged(RearPanAltitude, FormatDouble(Status.RearPan.Fix.Altitude), PreviousStatus == null || PreviousStatus.RearPan.Fix.Altitude != Status.RearPan.Fix.Altitude);
            UpdateIMUCalibrationTextBox(RearPanIMUCalibrationStatus, Status.RearPan.IMU.CalibrationStatus, PreviousStatus == null || PreviousStatus.RearPan.IMU.CalibrationStatus != Status.RearPan.IMU.CalibrationStatus);

            // Store current status for next comparison
            PreviousStatus = new EquipmentStatus
            {
                TractorIMU = new IMUValue
                {
                    Pitch = Status.TractorIMU.Pitch,
                    Heading = Status.TractorIMU.Heading,
                    Roll = Status.TractorIMU.Roll,
                    YawRate = Status.TractorIMU.YawRate,
                    CalibrationStatus = Status.TractorIMU.CalibrationStatus
                },
                TractorFix = new GNSSFix
                {
                    Latitude = Status.TractorFix.Latitude,
                    Longitude = Status.TractorFix.Longitude,
                    Altitude = Status.TractorFix.Altitude,
                    RTK = Status.TractorFix.RTK,
                    Vector = Status.TractorFix.Vector
                },
                FrontPan = new PanStatus
                {
                    IMU = new IMUValue
                    {
                        Pitch = Status.FrontPan.IMU.Pitch,
                        Heading = Status.FrontPan.IMU.Heading,
                        Roll = Status.FrontPan.IMU.Roll,
                        YawRate = Status.FrontPan.IMU.YawRate,
                        CalibrationStatus = Status.FrontPan.IMU.CalibrationStatus
                    },
                    BladeHeight = Status.FrontPan.BladeHeight,
                    BladeOffset = Status.FrontPan.BladeOffset,
                    BladeAuto = Status.FrontPan.BladeAuto,
                    BladePWM = Status.FrontPan.BladePWM,
                    Direction = Status.FrontPan.Direction,
                    Fix = new GNSSFix
                    {
                        Latitude = Status.FrontPan.Fix.Latitude,
                        Longitude = Status.FrontPan.Fix.Longitude,
                        Altitude = Status.FrontPan.Fix.Altitude,
                        RTK = Status.FrontPan.Fix.RTK,
                        Vector = Status.FrontPan.Fix.Vector
                    }
                },
                RearPan = new PanStatus
                {
                    IMU = new IMUValue
                    {
                        Pitch = Status.RearPan.IMU.Pitch,
                        Heading = Status.RearPan.IMU.Heading,
                        Roll = Status.RearPan.IMU.Roll,
                        YawRate = Status.RearPan.IMU.YawRate,
                        CalibrationStatus = Status.RearPan.IMU.CalibrationStatus
                    },
                    BladeHeight = Status.RearPan.BladeHeight,
                    BladeOffset = Status.RearPan.BladeOffset,
                    BladeAuto = Status.RearPan.BladeAuto,
                    BladePWM = Status.RearPan.BladePWM,
                    Direction = Status.RearPan.Direction,
                    Fix = new GNSSFix
                    {
                        Latitude = Status.RearPan.Fix.Latitude,
                        Longitude = Status.RearPan.Fix.Longitude,
                        Altitude = Status.RearPan.Fix.Altitude,
                        RTK = Status.RearPan.Fix.RTK,
                        Vector = Status.RearPan.Fix.Vector
                    }
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
            return $"{latitude:F8} {longitude:F8}";
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
                    textBox.BackColor = SystemColors.Control;
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
                        textBox.ForeColor = Color.White;
                        break;
                }
            }
        }

        /// <summary>
        /// Updates IMU calibration status textbox with text and background color
        /// </summary>
        private void UpdateIMUCalibrationTextBox(TextBox textBox, IMUValue.Calibration calibrationStatus, bool hasChanged)
        {
            if (hasChanged)
            {
                switch (calibrationStatus)
                {
                    default:
                    case IMUValue.Calibration.None:
                        textBox.Text = "None";
                        textBox.BackColor = Color.Red;
                        textBox.ForeColor = Color.White;
                        break;

                    case IMUValue.Calibration.Poor:
                        textBox.Text = "Poor";
                        textBox.BackColor = Color.Red;
                        textBox.ForeColor = Color.White;
                        break;

                    case IMUValue.Calibration.Adequate:
                        textBox.Text = "Adequate";
                        textBox.BackColor = Color.Orange;
                        textBox.ForeColor = Color.Black;
                        break;

                    case IMUValue.Calibration.Good:
                        textBox.Text = "Good";
                        textBox.BackColor = Color.Green;
                        textBox.ForeColor = Color.White;
                        break;

                    case IMUValue.Calibration.Excellent:
                        textBox.Text = "Excellent";
                        textBox.BackColor = Color.Green;
                        textBox.ForeColor = Color.White;
                        break;
                }
            }
        }
    }
}
