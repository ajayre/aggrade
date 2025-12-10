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

        // fixme - this needs to go into the settings
        // height to place blade at when not cutting, e.g. going outside of field
        private const int MAX_BLADE_HEIGHT_MM = 80;

        private Timer CalcTimer;
        private Field? Field;
        private EquipmentSettings? CurrentEquipmentSettings;
        private EquipmentStatus? CurrentEquipmentStatus;
        private OGController Controller;

        public BladeController
            (
            OGController Controller
            )
        {
            this.Controller = Controller;

            CalcTimer = new Timer();
            CalcTimer.Interval = CALC_PERIOD_MS;
            CalcTimer.Elapsed += CalcTimer_Elapsed;
            CalcTimer.Start();
        }

        /// <summary>
        /// Perform the cutting calculations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalcTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if ((Field == null) || (CurrentEquipmentSettings == null) || (CurrentEquipmentStatus == null)) return;

            // front blade is set to auto cutting
            if (CurrentEquipmentStatus.FrontPan.BladeAuto)
            {
                Bin? CurrentBin = Field.LatLonToBin(CurrentEquipmentStatus.FrontPan.Fix.Latitude, CurrentEquipmentStatus.FrontPan.Fix.Longitude);
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
        /// <param name="Settings">New settings</param>
        public void SetEquipmentSettings
            (
            EquipmentSettings Settings
            )
        {
            CurrentEquipmentSettings = Settings;
        }

        /// <summary>
        /// Sets the equipment status
        /// </summary>
        /// <param name="Status">New status</param>
        public void SetEquipmentStatus
            (
            EquipmentStatus Status
            )
        {
            CurrentEquipmentStatus = Status;
        }
    }
}
