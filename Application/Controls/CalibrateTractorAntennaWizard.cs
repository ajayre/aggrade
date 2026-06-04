using AgGrade.Data;
using AgGrade.Controller;
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
    internal partial class CalibrateTractorAntennaWizard : WizardControl
    {
        private bool Pose1Valid;
        private double Pose1Latitude;
        private double Pose1Longitude;
        private double Pose1Heading;
        private bool Pose2Valid;
        private double Pose2Latitude;
        private double Pose2Longitude;
        private double Pose2Heading;
        private int T;
        private int D;
        private int SavedLeftOffsetMm;
        private int SavedForwardOffsetMm;
        private bool CalibrationCompleted;

        public CalibrateTractorAntennaWizard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when user taps on the button to return to the calibration list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnBtn_Click(object sender, EventArgs e)
        {
            Exit();
        }

        /// <summary>
        /// Called when the user changes the page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pages_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (Pages.SelectedIndex)
            {
                // completed first page, validate inputs
                case 1:
                    T = TInput.Value;
                    D = DInput.Value;
                    if (T < 1000)
                    {
                        ErrorMessage.Text = ResultMsg.Text = "Invalid value for T";
                        ErrorMessage.Visible = true;
                    }
                    else if (D < 10)
                    {
                        ErrorMessage.Text = ResultMsg.Text = "Invalid value for D";
                        ErrorMessage.Visible = true;
                    }
                    else
                    {
                        ValidateStatusAndSettings();
                    }
                    break;

                // completed second page, perform calculation
                case 2:
                    if (Pose1Valid && Pose2Valid)
                    {
                        if (CurrentEquipmentSettings != null)
                        {
                            PointD Offset = TractorAntennaFinder.Calculate(T, Pose1Heading, Pose2Heading, Pose1Latitude, Pose1Longitude, Pose2Latitude, Pose2Longitude, D);

                            ResultMsg.Text = string.Format("Antenna offset is X = {0}mm, Y = {1}mm", (int)Offset.X, (int)Offset.Y);
                            CurrentEquipmentSettings.TractorAntennaLeftOffsetMm = (int)Offset.X;
                            CurrentEquipmentSettings.TractorAntennaForwardOffsetMm = (int)Offset.Y;
                            CurrentEquipmentSettings.Save();
                            CalibrationCompleted = true;
                            SendTractorAntennaOffsetsToController((int)Offset.X, (int)Offset.Y);
                        }
                        else
                        {
                            ErrorMessage.Text = ResultMsg.Text = "Failed to store calculation result";
                            ErrorMessage.Visible = true;
                        }
                    }
                    else
                    {
                        ResultMsg.Text = "Failed to perform calculation";
                    }
                    break;
            }
        }

        /// <summary>
        /// Called when user taps to capture pose 1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapturePose1Btn_Click(object sender, EventArgs e)
        {
            if (!TryCapturePose(out Pose1Latitude, out Pose1Longitude, out Pose1Heading))
                return;

            Pose1Valid = true;
            CapturePose1Btn.BackColor = Color.FromArgb(0x36, 0x7C, 0x2B);
            CapturePose1Btn.ForeColor = Color.White;
        }

        /// <summary>
        /// Called when user taps to capture pose 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapturePose2Btn_Click(object sender, EventArgs e)
        {
            if (!TryCapturePose(out Pose2Latitude, out Pose2Longitude, out Pose2Heading))
                return;

            Pose2Valid = true;
            CapturePose2Btn.BackColor = Color.FromArgb(0x36, 0x7C, 0x2B);
            CapturePose2Btn.ForeColor = Color.White;
        }

        /// <summary>
        /// Captures a calibration pose when GNSS is high quality and a settled position is available.
        /// </summary>
        /// <param name="latitude">Captured latitude.</param>
        /// <param name="longitude">Captured longitude.</param>
        /// <param name="heading">Captured heading in degrees.</param>
        /// <returns>True when the pose was captured.</returns>
        private bool TryCapturePose(out double latitude, out double longitude, out double heading)
        {
            latitude = 0.0;
            longitude = 0.0;
            heading = 0.0;

            if (CurrentEquipmentStatus == null)
                return false;

            if (!CurrentEquipmentStatus.TractorFix.IsValid)
            {
                MessageBox.Show("No valid tractor location", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            if (CurrentEquipmentStatus.TractorFixQuality != GnssQualityState.HighQuality)
            {
                MessageBox.Show(
                    "GNSS is not settled yet. Wait until the tractor RTK LED is solid green before capturing a pose. " +
                    "After opening this wizard the controller stops applying antenna offset, so GNSS must settle again.",
                    Application.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return false;
            }

            if (Controller == null || !Controller.TryGetTractorHighQualityPosition(out latitude, out longitude))
            {
                MessageBox.Show(
                    "Unable to obtain a settled GNSS position.",
                    Application.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return false;
            }

            heading = CurrentEquipmentStatus.TractorFix.Vector.TrackTrueDeg;
            return true;
        }

        private void CalibrateTractorAntennaWizard_Load(object sender, EventArgs e)
        {
        }

        private void ErrorMessage_VisibleChanged(object sender, EventArgs e)
        {
        }

        private void ValidateStatusAndSettings
            (
            )
        {
            ErrorMessage.Visible = false;
            bool CanExecute = true;

            if (CurrentEquipmentStatus == null)
            {
                CanExecute = false;
                ErrorMessage.Text = ResultMsg.Text = "No equipment status found";
                ErrorMessage.Visible = true;
            }
            else if (CurrentEquipmentStatus.TractorFix.IsValid == false)
            {
                CanExecute = false;
                ErrorMessage.Text = ResultMsg.Text = "No tractor location";
                ErrorMessage.Visible = true;
            }
            else if (CurrentEquipmentSettings == null)
            {
                CanExecute = false;
                ErrorMessage.Text = ResultMsg.Text = "No equipment settings found";
                ErrorMessage.Visible = true;
            }
            else
            {
                ResultMsg.Text = "Not run";
            }

            CapturePose1Btn.Enabled = CanExecute;
            CapturePose2Btn.Enabled = CanExecute;
        }

        /// <summary>
        /// Called when wizard is being shown
        /// </summary>
        public override void Activated
            (
            )
        {
            ValidateStatusAndSettings();

            TInput.Value = (int)CurrentEquipmentSettings!.TractorWidthMm;
            DInput.Value = 100;

            SavedLeftOffsetMm = CurrentEquipmentSettings.TractorAntennaLeftOffsetMm;
            SavedForwardOffsetMm = CurrentEquipmentSettings.TractorAntennaForwardOffsetMm;
            CalibrationCompleted = false;

            // Controller firmware applies antenna offset to outgoing GGA; calibration needs raw antenna positions.
            SendTractorAntennaOffsetsToController(0, 0);
            Controller?.ResetTractorGnssQualityMonitor();
        }

        /// <summary>
        /// Called when wizard stops being shown
        /// </summary>
        public override void Deactivated()
        {
            if (!CalibrationCompleted)
                SendTractorAntennaOffsetsToController(SavedLeftOffsetMm, SavedForwardOffsetMm);
        }

        /// <summary>
        /// Sends tractor antenna offsets to the controller firmware.
        /// </summary>
        private void SendTractorAntennaOffsetsToController(int leftOffsetMm, int forwardOffsetMm)
        {
            if (Controller == null || CurrentEquipmentSettings == null)
                return;

            Controller.SetTractorAntennaLocation(
                CurrentEquipmentSettings.TractorAntennaHeightMm,
                leftOffsetMm,
                forwardOffsetMm);
        }
    }
}
