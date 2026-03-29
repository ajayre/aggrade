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
    internal partial class CalibrateFieldWizard : WizardControl
    {
        private bool PositionValid = false;

        public Field? CurrentField = null;

        public CalibrateFieldWizard
            (
            Field? CurrentField
            )
        {
            this.CurrentField = CurrentField;

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
                    if (PositionValid)
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
        /// Called when user taps to capture current location
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CaptureLocationBtn_Click(object sender, EventArgs e)
        {
            if (Controller != null)
            {
                PositionValid = true;
            }
        }

        private void CalibrateFieldWizard_Load(object sender, EventArgs e)
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
            else if (CurrentField == null)
            {
                CanExecute = false;
                ErrorMessage.Text = ResultMsg.Text = "No field loaded";
                ErrorMessage.Visible = true;
            }
            else if (CurrentField.Benchmarks.Count == 0)
            {
                CanExecute = false;
                ErrorMessage.Text = ResultMsg.Text = "No benchmarks found";
                ErrorMessage.Visible = true;
            }
            else
            {
                ResultMsg.Text = "Not run";
            }

            CaptureLocationBtn.Enabled = CanExecute;
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

        /// <summary>
        /// Called periodically to update the offset display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if ((CurrentEquipmentStatus != null) && (CurrentField != null))
            {
                double EastingM;
                double NorthingM;
                double HeightM;

                Benchmark? NearestBM = CurrentField.GetNearestBenchmark(CurrentEquipmentStatus.TractorFix.Latitude,
                    CurrentEquipmentStatus.TractorFix.Longitude,
                    CurrentEquipmentStatus.TractorFix.Altitude,
                    out EastingM,
                    out NorthingM,
                    out HeightM);

                if (NearestBM != null)
                {
                    BMOffset.Text = string.Format("{0} E = {1}mm, N = {2}mm, H = {3}mm", NearestBM.Name,
                        (int)(EastingM * 1000), (int)(NorthingM * 1000), (int)(HeightM * 1000));
                }
            }
        }
    }
}
