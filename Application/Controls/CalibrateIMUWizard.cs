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
using AgGrade.Controller;

namespace AgGrade.Controls
{
    internal partial class CalibrateIMUWizard : WizardControl
    {
        private Controller.IMUOrientations Orientation;

        public IMUs IMU = IMUs.Tractor;
        public Color PanColor = Color.Black;

        public CalibrateIMUWizard
            (
            IMUs IMU,
            Color PanColor
            )
        {
            this.IMU = IMU;
            this.PanColor = PanColor;

            InitializeComponent();

            ResultMsg.Text = "Not run";

            OrientationSelector.Items.Clear();
            OrientationSelector.Items.Add("Horizontal A");
            OrientationSelector.Items.Add("Vertical A");

            OrientationSelector.SelectedIndex = 0;
            OrientationImage.BackgroundImage = Properties.Resources.IMU_Horizontal;
            Orientation = AgGrade.Controller.IMUOrientations.HorizontalA;
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
                    ValidateStatusAndSettings();
                    if (Controller != null)
                    {
                        Controller.SetIMUOrientation(IMU, Orientation);
                    }
                    break;
            }
        }

        private void CalibrateBladeHeightWizard_Load(object sender, EventArgs e)
        {
            RefreshTimer.Start();
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
            else if (CurrentEquipmentSettings == null)
            {
                CanExecute = false;
                ErrorMessage.Text = ResultMsg.Text = "No equipment settings found";
                ErrorMessage.Visible = true;
            }

            CapturePositionBtn.Enabled = CanExecute;
        }

        /// <summary>
        /// Called when wizard is being shown
        /// </summary>
        public override void Activated
            (
            )
        {
            ValidateStatusAndSettings();

            IMUData.ForeColor = PanColor;
        }

        /// <summary>
        /// Called when wizard stops being shown
        /// </summary>
        public override void Deactivated()
        {
            RefreshTimer.Stop();
        }

        /// <summary>
        /// Called when user taps on the button to return to the calibration menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnBtn_Click(object sender, EventArgs e)
        {
            Exit();
        }

        /// <summary>
        /// Called periodically to update the height displays
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (CurrentEquipmentStatus != null)
            {
                IMUValue Value;

                switch (IMU)
                {
                    default:
                    case IMUs.Tractor:
                        Value = CurrentEquipmentStatus.TractorIMU;
                        break;

                    case IMUs.Front:
                        Value = CurrentEquipmentStatus.FrontPan.IMU;
                        break;

                    case IMUs.Rear:
                        Value = CurrentEquipmentStatus.RearPan.IMU;
                        break;

                    case IMUs.FrontApron:
                        Value = CurrentEquipmentStatus.FrontPan.ApronIMU;
                        break;

                    case IMUs.FrontBucket:
                        Value = CurrentEquipmentStatus.FrontPan.BucketIMU;
                        break;

                    case IMUs.RearBucket:
                        Value = CurrentEquipmentStatus.RearPan.BucketIMU;
                        break;
                }

                IMUData.Text = string.Format("Pitch {0:0.00} deg, Roll {1:0.00} deg", Value.Pitch, Value.Roll);
            }
        }

        /// <summary>
        /// Called when the user chooses an IMU orientation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrientationSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OrientationSelector.SelectedIndex == 0)
            {
                OrientationImage.BackgroundImage = Properties.Resources.IMU_Horizontal;
                Orientation = AgGrade.Controller.IMUOrientations.HorizontalA;
            }
            else
            {
                OrientationImage.BackgroundImage = Properties.Resources.IMU_Vertical;
                Orientation = AgGrade.Controller.IMUOrientations.VerticalA;
            }
        }

        /// <summary>
        /// Called when user taps on the button to capture the IMU postion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapturePositionBtn_Click(object sender, EventArgs e)
        {
            if (Controller != null)
            {
                Controller.SetIMULevel(IMU);

                CapturePositionBtn.BackColor = Color.FromArgb(0x36, 0x7C, 0x2B);
                CapturePositionBtn.ForeColor = Color.White;

                ResultMsg.Text = "Calibration completed";
            }
        }
    }
}
