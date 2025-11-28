using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Timer = System.Windows.Forms.Timer;

namespace HardwareSim
{
    internal class GNSS
    {
        public double TractorLatitude { get; private set; }
        public double TractorLongitude { get; private set; }

        public event Action<string> OnNewTractorFix = null;

        private Timer UpdateTimer;

        public GNSS
            (
            )
        {
            UpdateTimer = new Timer();
            UpdateTimer.Interval = 1000;
            UpdateTimer.Tick += UpdateTimer_Tick;
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            string NMEAString = "";

            OnNewTractorFix?.Invoke(NMEAString);
        }

        public void SetTractorLocation
            (
            double Latitude,
            double Longitude
            )
        {
            this.TractorLatitude = Latitude;
            this.TractorLongitude = Longitude;
        }

        public void Start
            (
            )
        {
            UpdateTimer.Start();
        }
    }
}
