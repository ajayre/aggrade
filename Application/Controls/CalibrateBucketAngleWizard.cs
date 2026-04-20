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
    internal partial class CalibrateBucketAngleWizard : WizardControl
    {
        private bool ZeroPositionValid = false;
        private bool MaxPositionValid = false;

        public enum Blades
        {
            Front,
            Rear
        }

        public Blades Blade = Blades.Front;

        public Color PanColor = Color.Black;

        public CalibrateBucketAngleWizard
            (
            Blades Blade,
            Color PanColor
            )
        {
            this.Blade = Blade;
            this.PanColor = PanColor;

            InitializeComponent();

            ResultMsg.Text = "Not run";
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

                if (Blade == Blades.Front)
                {
                    Controller.FrontBucketAtZero();
                }
                else
                {
                    Controller.RearBucketAtZero();
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

            Angle1.ForeColor = PanColor;
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
                if (Blade == Blades.Front)
                {
                    Angle1.Text = CurrentEquipmentStatus.FrontPan.BucketAngle.ToString("F2") + " deg";
                }
                else
                {
                    Angle1.Text = CurrentEquipmentStatus.RearPan.BucketAngle.ToString("F2") + " deg";
                }
            }
        }
    }
}
