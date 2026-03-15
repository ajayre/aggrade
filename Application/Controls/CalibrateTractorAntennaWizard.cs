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

        }

        /// <summary>
        /// Called when user taps to capture pose 1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapturePose1Btn_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Called when user taps to capture pose 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapturePose2Btn_Click(object sender, EventArgs e)
        {

        }
    }
}
