using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace AgGrade.Data
{
    public class BladeController
    {
        private const int CALC_PERIOD_MS = 50;

        private bool FrontBladeAuto;
        private bool RearBladeAuto;
        private GNSSFix FrontFix;
        private GNSSFix RearFix;
        private Timer CalcTimer;
        private Field? Field;
        private EquipmentSettings? CurrentEquipmentSettings;
        private List<Bin> FrontCutBins;
        private List<Bin> RearCutBins;

        public BladeController
            (
            )
        {
            FrontBladeAuto = false;
            RearBladeAuto = false;

            CalcTimer = new Timer();
            CalcTimer.Interval = CALC_PERIOD_MS;
            CalcTimer.Elapsed += CalcTimer_Elapsed;

            FrontCutBins = new List<Bin>();
            RearCutBins = new List<Bin>();
        }

        /// <summary>
        /// Perform the cutting calculations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalcTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if ((Field == null) || (CurrentEquipmentSettings == null)) return;

            // front blade cutting
            if (FrontBladeAuto)
            {
                double LCYCut = 0;

                Bin? CurrentBin = Field.LatLonToBin(FrontFix.Latitude, FrontFix.Longitude);
                if (CurrentBin != null)
                {
                    // if we haven't already cut from this bin then cut now
                    if (!FrontCutBins.Contains(CurrentBin))
                    {
                        // get depth of cut, use max cut depth unless the target elevation is shallower
                        double CutDepthM = CurrentEquipmentSettings.FrontPan.MaxCutDepthMm / 1000.0;
                        if ((CurrentBin.ExistingElevationM - CutDepthM) < CurrentBin.TargetElevationM)
                        {
                            CutDepthM = CurrentBin.ExistingElevationM - CurrentBin.TargetElevationM;
                        }

                        // perform the cut
                        CurrentBin.ExistingElevationM -= CurrentEquipmentSettings.FrontPan.MaxCutDepthMm / 1000.0;

                        // calculate volume cut
                        LCYCut = ?;

                        // don't cut this bin again
                        FrontCutBins.Add(CurrentBin);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the field to cut
        /// </summary>
        /// <param name="NewField">Field to cut</param>
        public void SetField
            (
            Field NewField
            )
        {
            this.Field = NewField;
        }

        /// <summary>
        /// Sets the equipment settings
        /// </summary>
        /// <param name="EquipmentSettings">New equipment settings</param>
        public void SetEquipmentSettings
            (
            EquipmentSettings EquipmentSettings
            )
        {
            CurrentEquipmentSettings = EquipmentSettings;
        }

        /// <summary>
        /// Start automatic front blade control
        /// </summary>
        public void StartFront
            (
            )
        {
            FrontBladeAuto = true;

            FrontCutBins.Clear();

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

        /// <summary>
        /// Sets the current fix of the rear blade
        /// </summary>
        /// <param name="Fix">Current fix</param>
        public void SetRearFix
            (
            GNSSFix Fix
            )
        {
            RearFix = Fix;
        }
    }
}
