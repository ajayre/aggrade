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
    internal partial class CalibrateIMUWizard : WizardControl
    {
        private bool ZeroPositionValid = false;
        private bool MaxPositionValid = false;

        public enum IMUs
        {
            Tractor,
            Front,
            Rear,
            FrontApron,
            FrontBucket,
            RearBucket
        }

        public IMUs IMU = IMUs.Tractor;

        public CalibrateIMUWizard
            (
            IMUs IMU
            )
        {
            this.IMU = IMU;

            InitializeComponent();

            ResultMsg.Text = "Not run";

            OrientationSelector.SelectedIndex = 0;
            OrientationImage.BackgroundImage = Properties.Resources.IMU_Horizontal;
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
                    break;
            }
        }

        /// <summary>
        /// Called when user taps to capture zero position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaptureZeroBtn_Click(object sender, EventArgs e)
        {
            if (Controller != null)
            {
                ZeroPositionValid = true;

                switch (IMU)
                {
                    case IMUs.Tractor:
                        //Controller.FrontApronAtZero();
                        break;

                    case IMUs.Front:
                        break;

                    case IMUs.FrontApron:
                        break;

                    case IMUs.FrontBucket:
                        break;

                    case IMUs.Rear:
                        break;
                }

                CaptureZeroBtn.BackColor = Color.FromArgb(0x36, 0x7C, 0x2B);
                CaptureZeroBtn.ForeColor = Color.White;

                ResultMsg.Text = "Calibration completed";
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

            CaptureZeroBtn.Enabled = CanExecute;
        }

        /// <summary>
        /// Called when wizard is being shown
        /// </summary>
        public override void Activated
            (
            )
        {
            ValidateStatusAndSettings();
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
            }
            else
            {
                OrientationImage.BackgroundImage = Properties.Resources.IMU_Vertical;
            }
        }
    }
}
