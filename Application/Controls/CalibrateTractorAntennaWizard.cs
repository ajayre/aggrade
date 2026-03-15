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
                case 2:
                    if (Pose1Valid && Pose2Valid)
                    {
                        if (CurrentEquipmentSettings != null)
                        {
                            PointD Offset = TractorAntennaFinder.Calculate(T, Pose1Heading, Pose2Heading, Pose1Latitude, Pose1Longitude, Pose2Latitude, Pose2Longitude, D);
                            ResultMsg.Text = string.Format("Antenna offset is X = {0}mm, Y = {1}mm", (int)Offset.X, (int)Offset.Y);
                            CurrentEquipmentSettings.TractorAntennaLeftOffsetMm = (int)Offset.X;
                            CurrentEquipmentSettings.TractorAntennaForwardOffsetMm = (int)Offset.Y;
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
            if ((CurrentEquipmentStatus != null) && (CurrentEquipmentStatus.TractorFix.IsValid == true))
            {
                Pose1Latitude = CurrentEquipmentStatus.TractorFix.Latitude;
                Pose1Longitude = CurrentEquipmentStatus.TractorFix.Longitude;
                Pose1Heading = CurrentEquipmentStatus.TractorFix.Vector.TrackMagneticDeg;
                Pose1Valid = true;
            }
        }

        /// <summary>
        /// Called when user taps to capture pose 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapturePose2Btn_Click(object sender, EventArgs e)
        {
            if ((CurrentEquipmentStatus != null) && (CurrentEquipmentStatus.TractorFix.IsValid == true))
            {
                Pose2Latitude = CurrentEquipmentStatus.TractorFix.Latitude;
                Pose2Longitude = CurrentEquipmentStatus.TractorFix.Longitude;
                Pose2Heading = CurrentEquipmentStatus.TractorFix.Vector.TrackMagneticDeg;
                Pose2Valid = true;
            }
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
        }
    }
}
