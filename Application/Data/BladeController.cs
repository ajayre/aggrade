using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace AgGrade.Data
{
    public class BladeController
    {
        private const int CALC_PERIOD_MS = 50;

        private bool FrontBladeAuto;
        private bool RearBladeAuto;
        private GNSSFix FrontFix;
        private Timer CalcTimer;

        public BladeController
            (
            )
        {
            FrontBladeAuto = false;
            RearBladeAuto = false;

            CalcTimer = new Timer();
            CalcTimer.Interval = CALC_PERIOD_MS;
            CalcTimer.Tick += CalcTimer_Tick;
        }

        private void CalcTimer_Tick(object? sender, EventArgs e)
        {

        }

        /// <summary>
        /// Start automatic front blade control
        /// </summary>
        public void StartFront
            (
            )
        {
            FrontBladeAuto = true;
            if (!CalcTimer.Enabled)
            {
                CalcTimer.Start();
            }
        }

        /// <summary>
        /// Stop automatic front blade control
        /// </summary>
        public void StopFront
            (
            )
        {
            FrontBladeAuto = false;
            if (!RearBladeAuto)
            {
                CalcTimer.Stop();
            }
        }

        /// <summary>
        /// Sets the current fix of the front blade
        /// </summary>
        /// <param name="Fix">Current fix</param>
        public void SetFrontFix
            (
            GNSSFix Fix
            )
        {
            FrontFix = Fix;
        }
    }
}
