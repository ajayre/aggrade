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
    internal partial class CalibrateBladeHeightWizard : WizardControl
    {
        private bool ZeroPositionValid = false;
        private bool MinPositionValid = false;
        private bool MaxPositionValid = false;

        public CalibrateBladeHeightWizard()
        {
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
            if (CurrentEquipmentStatus != null)
            {
                ZeroPositionValid = true;
            }
        }

        private void CalibrateBladeHeightWizard_Load(object sender, EventArgs e)
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
        /// Called when user taps on the button to return to the calibration menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnBtn_Click(object sender, EventArgs e)
        {
            Exit();
        }
    }
}
