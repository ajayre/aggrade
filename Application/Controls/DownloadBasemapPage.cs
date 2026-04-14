using AgGrade.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class DownloadBasemapPage : UserControl
    {
        public event Action<double, double> OnDownloadBasemap = null;

        /// <summary>
        /// Sets the download progress bar (0 hides the bar; 1–100 shows progress).
        /// </summary>
        public int DownloadProgress
        {
            set
            {
                if (value == 0)
                    ProgressBar.Visible = false;
                else
                {
                    ProgressBar.Visible = true;
                    ProgressBar.Value = Math.Clamp(value, 0, 100);
                }
            }
        }

        public DownloadBasemapPage()
        {
            InitializeComponent();

            ErrorMessage.Visible = false;
        }

        private const NumberStyles AngleNumberStyles =
            NumberStyles.Float | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

        /// <summary>
        /// Parses a decimal angle string (e.g. latitude/longitude). Tries invariant culture first, then current culture.
        /// </summary>
        private static bool TryParseAngle(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            if (double.TryParse(text, AngleNumberStyles, CultureInfo.InvariantCulture, out value) &&
                double.IsFinite(value))
                return true;
            if (double.TryParse(text, AngleNumberStyles, CultureInfo.CurrentCulture, out value) &&
                double.IsFinite(value))
                return true;
            return false;
        }

        /// <summary>
        /// Called when user taps on the button to create the survey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadBtn_Click(object sender, EventArgs e)
        {
            string latText = LatitudeInput.Text.Trim();
            if (latText.Length == 0)
            {
                ErrorMessage.Text = "No latitude given";
                ErrorMessage.Visible = true;
                return;
            }

            string lonText = LongitudeInput.Text.Trim();
            if (lonText.Length == 0)
            {
                ErrorMessage.Text = "No longitude given";
                ErrorMessage.Visible = true;
                return;
            }

            if (!TryParseAngle(latText, out double latD))
            {
                ErrorMessage.Text = "Invalid latitude: enter a number (e.g. 40.7128).";
                ErrorMessage.Visible = true;
                return;
            }

            if (!TryParseAngle(lonText, out double lonD))
            {
                ErrorMessage.Text = "Invalid longitude: enter a number (e.g. -74.0060).";
                ErrorMessage.Visible = true;
                return;
            }

            const double minLat = -90.0;
            const double maxLat = 90.0;
            if (latD < minLat || latD > maxLat)
            {
                ErrorMessage.Text = $"Latitude must be between {minLat} and {maxLat} degrees.";
                ErrorMessage.Visible = true;
                return;
            }

            const double minLon = -180.0;
            const double maxLon = 180.0;
            if (lonD < minLon || lonD > maxLon)
            {
                ErrorMessage.Text = $"Longitude must be between {minLon} and {maxLon} degrees.";
                ErrorMessage.Visible = true;
                return;
            }

            ErrorMessage.Visible = false;

            try
            {
                OnDownloadBasemap?.Invoke(latD, lonD);
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Download failed: {ex.Message}";
                ErrorMessage.Visible = true;
            }
        }
    }
}
