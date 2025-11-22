using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using AgGrade.Data;

namespace AgGrade.Controls
{
    public partial class AppSettingsEditor : UserControl
    {
        public delegate void PowerOff();
        public event PowerOff OnPowerOff;

        public AppSettingsEditor()
        {
            InitializeComponent();

            double ScalingFactor = this.DeviceDpi / 96.0;

            Bitmap scaledImage = new Bitmap(PowerBtn.Image!, new Size((int)(PowerBtn.Image!.Width * ScalingFactor), (int)(PowerBtn.Image!.Height * ScalingFactor)));
            PowerBtn.Image = scaledImage;
        }

        /// <summary>
        /// Called when user taps on power off button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerBtn_Click(object sender, EventArgs e)
        {
            OnPowerOff?.Invoke();
        }

        /// <summary>
        /// Gets the current settings
        /// </summary>
        /// <returns>Current settings</returns>
        public AppSettings GetSettings
            (
            )
        {
            AppSettings Settings = new AppSettings();

            // fixme - to do - read UI

            return Settings;
        }
    }
}
