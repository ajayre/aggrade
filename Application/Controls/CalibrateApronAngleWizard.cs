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
    internal partial class CalibrateApronAngleWizard : WizardControl
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

        public CalibrateApronAngleWizard
            (
            Blades Blade,
            Color PanColor
            )
        {
            this.Blade = Blade;
            this.PanColor = PanColor;

            InitializeComponent();
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

                // completed second page, perform calculation
                case 2:
                    if (ZeroPositionValid)
                    {
                        if (CurrentEquipmentSettings != null)
                        {
                            //CurrentEquipmentSettings.TractorAntennaForwardOffsetMm = (int)Offset.Y;
                        }
                        else
                        {
                            ErrorMessage.Text = ResultMsg.Text = "Failed to store result";
                            ErrorMessage.Visible = true;
                        }
                    }
                    else
                    {
                        ResultMsg.Text = "Failed to perform measurements";
                    }
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

                // fixme - to do
                if (Blade == Blades.Front)
                {
                    //Controller.FrontBladeAtZero();
                }
                else
                {
                    //Controller.RearBladeAtZero();
                }

                CaptureZeroBtn.BackColor = Color.FromArgb(0x36, 0x7C, 0x2B);
                CaptureZeroBtn.ForeColor = Color.White;
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
            else
            {
                ResultMsg.Text = "Not run";
            }

            CaptureZeroBtn.Enabled = CanExecute;
            CaptureMaxBtn.Enabled = CanExecute;
        }

        /// <summary>
        /// Called when wizard is being shown
        /// </summary>
        public override void Activated
            (
            )
        {
            ValidateStatusAndSettings();

            Height1.ForeColor = PanColor;
            Height2.ForeColor = PanColor;
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
        /// Called when user taps on the button to set the minimum height 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaptureMinBtn_Click(object sender, EventArgs e)
        {
            if ((CurrentEquipmentStatus != null) && (CurrentEquipmentSettings != null))
            {
                // fixme - to do
                if (Blade == Blades.Front)
                {
                    //CurrentEquipmentSettings.FrontPan.MinHeightMm = CurrentEquipmentStatus.FrontPan.BladeHeight;
                }
                else
                {
                    //CurrentEquipmentSettings.RearPan.MinHeightMm = CurrentEquipmentStatus.RearPan.BladeHeight;
                }

                CaptureMaxBtn.BackColor = Color.FromArgb(0x36, 0x7C, 0x2B);
                CaptureMaxBtn.ForeColor = Color.White;
            }
        }

        /// <summary>
        /// Called when user taps on the button to set the maximum height
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaptureMaxBtn_Click(object sender, EventArgs e)
        {
            if ((CurrentEquipmentStatus != null) && (CurrentEquipmentSettings != null))
            {
                // fixme - to do
                if (Blade == Blades.Front)
                {
                    //CurrentEquipmentSettings.FrontPan.MaxHeightMm = (uint)CurrentEquipmentStatus.FrontPan.BladeHeight;
                }
                else
                {
                    //CurrentEquipmentSettings.RearPan.MaxHeightMm = (uint)CurrentEquipmentStatus.RearPan.BladeHeight;
                }

                CaptureMaxBtn.BackColor = Color.FromArgb(0x36, 0x7C, 0x2B);
                CaptureMaxBtn.ForeColor = Color.White;
            }
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
                // fixme - to do
                if (Blade == Blades.Front)
                {
                    //Height1.Text = Height2.Text = CurrentEquipmentStatus.FrontPan.BladeHeight.ToString() + " deg";
                }
                else
                {
                    //Height1.Text = Height2.Text = CurrentEquipmentStatus.RearPan.BladeHeight.ToString() + " deg";
                }
            }
        }
    }
}
