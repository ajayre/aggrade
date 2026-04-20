using AgGrade.Controller;
using AgGrade.Data;
using AgGrade.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace AgGrade.Controls
{
    public partial class StatusPage : UserControl
    {
        private const double KPH_TO_MPH = 0.621371;
        private EquipmentStatus? PreviousStatus = null;
        private Field? ChartField;

        public StatusPage()
        {
            InitializeComponent();
            Pages.SelectedIndexChanged += Pages_SelectedIndexChanged;
        }

        /// <summary>
        /// Shows the current field status
        /// </summary>
        /// <param name="CurrentField">Current field</param>
        public void ShowFieldStatus
            (
            Field? CurrentField
            )
        {
            if (InvokeRequired)
            {
                BeginInvoke(ShowFieldStatus, CurrentField);
                return;
            }

            if (CurrentField == null)
            {
                Pages.Controls.Remove(Field);
                ChartField = null;
                ClearProgressChart();
                Stats.Text = string.Empty;
            }
            else
            {
                ChartField = CurrentField;

                if (!Pages.Controls.Contains(Field))
                {
                    Pages.Controls.Add(Field);
                }

                FieldProgress.Value = (int)CurrentField.PercentageComplete();
                FieldProgressLabel.Text = string.Format("{0:0.00}%", CurrentField.PercentageComplete());

                if (Pages.SelectedTab == Field)
                {
                    LoadProgressChart(CurrentField);
                }
            }
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
            UpdateTextBoxIfChanged(FrontPanBladeAuto, Status.FrontPan.Mode == PanStatus.BladeMode.AutoCutting ? "Yes" : "No", PreviousStatus == null || PreviousStatus.FrontPan.Mode != Status.FrontPan.Mode);
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
            UpdateTextBoxIfChanged(RearPanBladeAuto, Status.RearPan.Mode == PanStatus.BladeMode.AutoCutting ? "Yes" : "No", PreviousStatus == null || PreviousStatus.RearPan.Mode != Status.RearPan.Mode);
            UpdateTextBoxIfChanged(RearPanBladePWM, Status.RearPan.BladePWM.ToString(), PreviousStatus == null || PreviousStatus.RearPan.BladePWM != Status.RearPan.BladePWM);
            UpdateTextBoxIfChanged(RearPanBladeDirection, Status.RearPan.Direction.ToString(), PreviousStatus == null || PreviousStatus.RearPan.Direction != Status.RearPan.Direction);
            UpdateTextBoxIfChanged(RearPanSpeed, FormatDouble(Status.RearPan.Fix.Vector.SpeedMph), PreviousStatus == null || PreviousStatus.RearPan.Fix.Vector.Speedkph != Status.RearPan.Fix.Vector.Speedkph);
            UpdateTextBoxIfChanged(RearPanGNSSHeading, FormatDouble(Status.RearPan.Fix.Vector.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.RearPan.Fix.Vector.TrackMagneticDeg != Status.RearPan.Fix.Vector.TrackMagneticDeg);
            UpdateTextBoxIfChanged(RearPanAltitude, FormatDouble(Status.RearPan.Fix.Altitude), PreviousStatus == null || PreviousStatus.RearPan.Fix.Altitude != Status.RearPan.Fix.Altitude);
            UpdateIMUCalibrationTextBox(RearPanIMUCalibrationStatus, Status.RearPan.IMU.CalibrationStatus, PreviousStatus == null || PreviousStatus.RearPan.IMU.CalibrationStatus != Status.RearPan.IMU.CalibrationStatus);

            // update Front Apron IMU
            UpdateTextBoxIfChanged(FrontApronPitch, FormatDouble(Status.FrontPan.ApronIMU.Pitch), PreviousStatus == null || PreviousStatus.FrontPan.ApronIMU.Pitch != Status.FrontPan.ApronIMU.Pitch);
            UpdateTextBoxIfChanged(FrontApronRoll, FormatDouble(Status.FrontPan.ApronIMU.Roll), PreviousStatus == null || PreviousStatus.FrontPan.ApronIMU.Roll != Status.FrontPan.ApronIMU.Roll);
            UpdateTextBoxIfChanged(FrontApronYawRate, FormatDouble(Status.FrontPan.ApronIMU.YawRate), PreviousStatus == null || PreviousStatus.FrontPan.ApronIMU.YawRate != Status.FrontPan.ApronIMU.YawRate);
            UpdateTextBoxIfChanged(FrontApronHeading, FormatDouble(Status.FrontPan.ApronIMU.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.FrontPan.ApronIMU.Heading != Status.FrontPan.ApronIMU.Heading);
            UpdateIMUCalibrationTextBox(FrontApronCalibration, Status.FrontPan.ApronIMU.CalibrationStatus, PreviousStatus == null || PreviousStatus.FrontPan.ApronIMU.CalibrationStatus != Status.FrontPan.ApronIMU.CalibrationStatus);

            // update Front Bucket IMU
            UpdateTextBoxIfChanged(FrontBucketPitch, FormatDouble(Status.FrontPan.BucketIMU.Pitch), PreviousStatus == null || PreviousStatus.FrontPan.BucketIMU.Pitch != Status.FrontPan.BucketIMU.Pitch);
            UpdateTextBoxIfChanged(FrontBucketRoll, FormatDouble(Status.FrontPan.BucketIMU.Roll), PreviousStatus == null || PreviousStatus.FrontPan.BucketIMU.Roll != Status.FrontPan.BucketIMU.Roll);
            UpdateTextBoxIfChanged(FrontBucketYawRate, FormatDouble(Status.FrontPan.BucketIMU.YawRate), PreviousStatus == null || PreviousStatus.FrontPan.BucketIMU.YawRate != Status.FrontPan.BucketIMU.YawRate);
            UpdateTextBoxIfChanged(FrontBucketHeading, FormatDouble(Status.FrontPan.BucketIMU.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.FrontPan.BucketIMU.Heading != Status.FrontPan.BucketIMU.Heading);
            UpdateIMUCalibrationTextBox(FrontBucketCalibration, Status.FrontPan.BucketIMU.CalibrationStatus, PreviousStatus == null || PreviousStatus.FrontPan.BucketIMU.CalibrationStatus != Status.FrontPan.BucketIMU.CalibrationStatus);

            // update Rear Bucket IMU
            UpdateTextBoxIfChanged(RearBucketPitch, FormatDouble(Status.RearPan.BucketIMU.Pitch), PreviousStatus == null || PreviousStatus.RearPan.BucketIMU.Pitch != Status.RearPan.BucketIMU.Pitch);
            UpdateTextBoxIfChanged(RearBucketRoll, FormatDouble(Status.RearPan.BucketIMU.Roll), PreviousStatus == null || PreviousStatus.RearPan.BucketIMU.Roll != Status.RearPan.BucketIMU.Roll);
            UpdateTextBoxIfChanged(RearBucketYawRate, FormatDouble(Status.RearPan.BucketIMU.YawRate), PreviousStatus == null || PreviousStatus.RearPan.BucketIMU.YawRate != Status.RearPan.BucketIMU.YawRate);
            UpdateTextBoxIfChanged(RearBucketHeading, FormatDouble(Status.RearPan.BucketIMU.GetTrueHeading(Settings.MagneticDeclinationDegrees, Settings.MagneticDeclinationMinutes)), PreviousStatus == null || PreviousStatus.RearPan.BucketIMU.Heading != Status.RearPan.BucketIMU.Heading);
            UpdateIMUCalibrationTextBox(RearBucketCalibration, Status.RearPan.BucketIMU.CalibrationStatus, PreviousStatus == null || PreviousStatus.RearPan.BucketIMU.CalibrationStatus != Status.RearPan.BucketIMU.CalibrationStatus);

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
                    BucketIMU = new IMUValue
                    {
                        Pitch = Status.FrontPan.BucketIMU.Pitch,
                        Heading = Status.FrontPan.BucketIMU.Heading,
                        Roll = Status.FrontPan.BucketIMU.Roll,
                        YawRate = Status.FrontPan.BucketIMU.YawRate,
                        CalibrationStatus = Status.FrontPan.BucketIMU.CalibrationStatus
                    },
                    ApronIMU = new IMUValue
                    {
                        Pitch = Status.FrontPan.ApronIMU.Pitch,
                        Heading = Status.FrontPan.ApronIMU.Heading,
                        Roll = Status.FrontPan.ApronIMU.Roll,
                        YawRate = Status.FrontPan.ApronIMU.YawRate,
                        CalibrationStatus = Status.FrontPan.ApronIMU.CalibrationStatus
                    },
                    BladeHeight = Status.FrontPan.BladeHeight,
                    BladeOffset = Status.FrontPan.BladeOffset,
                    Mode = Status.FrontPan.Mode,
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
                    BucketIMU = new IMUValue
                    {
                        Pitch = Status.RearPan.BucketIMU.Pitch,
                        Heading = Status.RearPan.BucketIMU.Heading,
                        Roll = Status.RearPan.BucketIMU.Roll,
                        YawRate = Status.RearPan.BucketIMU.YawRate,
                        CalibrationStatus = Status.RearPan.BucketIMU.CalibrationStatus
                    },
                    BladeHeight = Status.RearPan.BladeHeight,
                    BladeOffset = Status.RearPan.BladeOffset,
                    Mode = Status.RearPan.Mode,
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

        private void Pages_SelectedIndexChanged
            (
            object? sender,
            EventArgs e
            )
        {
            if (Pages.SelectedTab != Field || ChartField == null)
                return;

            LoadProgressChart(ChartField);
        }

        private void ClearProgressChart
            (
            )
        {
            ProgressChart.Plot.Clear();
            ProgressChart.Refresh();
        }

        private void LoadProgressChart
            (
            Field field
            )
        {
            ChartField = field;
            List<Database.ProgressHistoryPoint> history = field.GetProgressHistory();
            UpdateFieldStats(field, history);

            ProgressChart.Plot.Clear();

            if (history.Count == 0)
            {
                ProgressChart.Plot.Title("Progress (no cut/fill history yet)");
                ProgressChart.Plot.XLabel("Time");
                ProgressChart.Plot.YLabel("Cubic Yards");
                ProgressChart.Refresh();
                return;
            }

            double[] xs = history
                .Select(point => DateTimeOffset.FromUnixTimeMilliseconds(point.TimestampMs).ToLocalTime().DateTime.ToOADate())
                .ToArray();
            double[] cutYs = history
                .Select(point => point.CompletedCutCY)
                .ToArray();
            double[] fillYs = history
                .Select(point => point.CompletedFillCY)
                .ToArray();

            var cutSeries = ProgressChart.Plot.Add.Scatter(xs, cutYs);
            cutSeries.LegendText = "Completed Cut CY";
            cutSeries.Color = ScottPlot.Colors.Orange;

            var fillSeries = ProgressChart.Plot.Add.Scatter(xs, fillYs);
            fillSeries.LegendText = "Completed Fill CY";
            fillSeries.Color = ScottPlot.Colors.DodgerBlue;

            ProgressChart.Plot.Axes.DateTimeTicksBottom();
            ProgressChart.Plot.XLabel("Time");
            ProgressChart.Plot.YLabel("Cubic Yards");
            ProgressChart.Plot.Legend.IsVisible = true;
            ProgressChart.Plot.Title("Field Progress");

            ProgressChart.Refresh();
        }

        private void UpdateFieldStats
            (
            Field field,
            List<Database.ProgressHistoryPoint> history
            )
        {
            double cutPerHour = 0;
            bool hasCutRate = false;

            if (history.Count >= 2)
            {
                Database.ProgressHistoryPoint first = history[0];
                Database.ProgressHistoryPoint last = history[^1];

                double elapsedHours = (last.TimestampMs - first.TimestampMs) / (1000.0 * 60.0 * 60.0);
                double cutDelta = last.CompletedCutCY - first.CompletedCutCY;

                if (elapsedHours > 0 && cutDelta > 0)
                {
                    cutPerHour = cutDelta / elapsedHours;
                    hasCutRate = true;
                }
            }

            if (!hasCutRate)
            {
                Stats.Text =
                    "Cubic yards cut per hour: N/A" + Environment.NewLine +
                    "Estimated hours to complete all cuts: N/A";
                return;
            }

            double remainingCutCY = Math.Max(0, field.TotalCutCY - field.CompletedCutCY);
            double estimatedHours = remainingCutCY <= 0 ? 0 : remainingCutCY / cutPerHour;

            Stats.Text =
                $"Cubic yards cut per hour: {cutPerHour:0.00}" + Environment.NewLine +
                $"Estimated hours to complete all cuts: {estimatedHours:0.00}";
        }
    }
}
