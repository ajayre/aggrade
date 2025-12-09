using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace AgGrade.Data
{
    public class BladeController
    {
        private const int CALC_PERIOD_MS = 50;

        // height to place blade at when not cutting, e.g. going outside of field
        private const int MAX_BLADE_HEIGHT_MM = 80;

        private bool FrontBladeAuto;
        private bool RearBladeAuto;
        private GNSSFix FrontFix;
        private GNSSFix RearFix;
        private Timer CalcTimer;
        private Field? Field;
        private EquipmentSettings? CurrentEquipmentSettings;
        private OGController Controller;

        public BladeController
            (
            OGController Controller
            )
        {
            this.Controller = Controller;

            FrontBladeAuto = false;
            RearBladeAuto = false;

            CalcTimer = new Timer();
            CalcTimer.Interval = CALC_PERIOD_MS;
            CalcTimer.Elapsed += CalcTimer_Elapsed;
        }

        /// <summary>
        /// Perform the cutting calculations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalcTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if ((Field == null) || (CurrentEquipmentSettings == null)) return;

            // front blade cutting depth
            if (FrontBladeAuto)
            {
                Bin? CurrentBin = Field.LatLonToBin(FrontFix.Latitude, FrontFix.Longitude);
                if (CurrentBin != null)
                {
                    // no data for this bin
                    if (CurrentBin.ExistingElevationM == 0)
                    {
                        Controller.SetFrontCutValve(MAX_BLADE_HEIGHT_MM + 100);
                    }
                    // need to cut
                    else if (CurrentBin.ExistingElevationM > CurrentBin.TargetElevationM)
                    {

                        // get depth of cut, use max cut depth unless the target elevation is shallower
                        double CutDepthM = CurrentEquipmentSettings.FrontPan.MaxCutDepthMm / 1000.0;
                        if ((CurrentBin.ExistingElevationM - CutDepthM) < CurrentBin.TargetElevationM)
                        {
                            CutDepthM = CurrentBin.ExistingElevationM - CurrentBin.TargetElevationM;
                        }

                        // convert to command for controller
                        uint Value = 100 - (uint)(CutDepthM * 1000.0);
                        Controller.SetFrontCutValve(Value);
                    }
                    // need to fill, but we are cutting
                    else
                    {
                        // float on surface
                        Controller.SetFrontCutValve(100);
                    }
                }
                // no bin - outside of field
                else
                {
                    Controller.SetFrontCutValve(MAX_BLADE_HEIGHT_MM + 100);
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
