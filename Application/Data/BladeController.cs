using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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

        public delegate void StoppedCutting();
        public event StoppedCutting OnFrontStoppedCutting = null;
        public event StoppedCutting OnRearStoppedCutting = null;

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
        /// Sets the front blade to transportation state
        /// </summary>
        public void SetFrontToTransportState
            (
            )
        {
            if ((Field == null) || (CurrentEquipmentSettings == null) || (CurrentEquipmentStatus == null)) return;

            // carrying a load
            if (CurrentEquipmentStatus.FrontPan.LoadLCY > 0)
            {
                if (CurrentEquipmentSettings.FrontPan.EndofCutting == PanSettings.EndOfCuttingOptions.Float)
                {
                    // float blade
                    Controller.SetFrontCutValve(100);
                    CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.Floating;
                }
                else
                {
                    // raise blade
                    Controller.SetFrontCutValve(CurrentEquipmentSettings.FrontPan.MaxHeightMm + 100);
                    CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.None;
                }
            }
            // empty
            else
            {
                // raise blade
                Controller.SetFrontCutValve(CurrentEquipmentSettings.FrontPan.MaxHeightMm + 100);
                CurrentEquipmentStatus.FrontPan.Mode = PanStatus.BladeMode.None;
            }
        }

        /// <summary>
        /// Sets the rear blade to it's transportation state
        /// </summary>
        public void SetRearToTransportState
            (
            )
        {
            if ((Field == null) || (CurrentEquipmentSettings == null) || (CurrentEquipmentStatus == null)) return;

            // carrying a load
            if (CurrentEquipmentStatus.FrontPan.LoadLCY > 0)
            {
                if (CurrentEquipmentSettings.RearPan.EndofCutting == PanSettings.EndOfCuttingOptions.Float)
                {
                    // float blade
                    Controller.SetRearCutValve(100);
                    CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.Floating;
                }
                else
                {
                    // raise blade
                    Controller.SetRearCutValve(CurrentEquipmentSettings.RearPan.MaxHeightMm + 100);
                    CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.None;
                }
            }
            // empty
            else
            {
                // raise blade
                Controller.SetRearCutValve(CurrentEquipmentSettings.RearPan.MaxHeightMm + 100);
                CurrentEquipmentStatus.RearPan.Mode = PanStatus.BladeMode.None;
            }
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
            if (CurrentEquipmentStatus.FrontPan.Mode == PanStatus.BladeMode.AutoCutting)
            {
                // if we need to stop cutting when scraper is full and scraper is full, then stop cutting
                if (CurrentEquipmentSettings.FrontPan.StopCuttingWhenFull &&
                    (CurrentEquipmentStatus.FrontPan.LoadLCY >= CurrentEquipmentSettings.FrontPan.CapacityCY))
                {
                    SetFrontToTransportState();

                    OnFrontStoppedCutting?.Invoke();
                }
                else
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

            // rear blade is set to auto cutting
            if (CurrentEquipmentStatus.RearPan.Mode == PanStatus.BladeMode.AutoCutting)
            {
                // if we need to stop cutting when scraper is full and scraper is full, then stop cutting
                if (CurrentEquipmentSettings.RearPan.StopCuttingWhenFull &&
                    (CurrentEquipmentStatus.RearPan.LoadLCY >= CurrentEquipmentSettings.RearPan.CapacityCY))
                {
                    SetRearToTransportState();

                    OnRearStoppedCutting?.Invoke();
                }
                else
                {
                    Bin? CurrentBin = Field.LatLonToBin(CurrentEquipmentStatus.RearPan.Fix.Latitude, CurrentEquipmentStatus.RearPan.Fix.Longitude);
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
                            double CutDepthM = CurrentEquipmentSettings.RearPan.MaxCutDepthMm / 1000.0;
                            if ((CurrentBin.ExistingElevationM - CutDepthM) < CurrentBin.TargetElevationM)
                            {
                                CutDepthM = CurrentBin.ExistingElevationM - CurrentBin.TargetElevationM;
                            }

                            // convert to command for controller
                            uint Value = 100 - (uint)(CutDepthM * 1000.0);
                            Controller.SetRearCutValve(Value);
                        }
                        // need to fill, but we are cutting
                        else
                        {
                            // float on surface
                            Controller.SetRearCutValve(100);
                        }
                    }
                    // no bin - outside of field
                    else
                    {
                        Controller.SetRearCutValve(MAX_BLADE_HEIGHT_MM + 100);
                    }
                }
            }

            // front blade is set to auto filling
            if (CurrentEquipmentStatus.FrontPan.Mode == PanStatus.BladeMode.AutoFilling)
            {
                Bin? CurrentBin = Field.LatLonToBin(CurrentEquipmentStatus.FrontPan.Fix.Latitude, CurrentEquipmentStatus.FrontPan.Fix.Longitude);
                if (CurrentBin != null)
                {
                    // no data for this bin
                    if (CurrentBin.ExistingElevationM == 0)
                    {
                        Controller.SetFrontCutValve(MAX_BLADE_HEIGHT_MM + 100);
                    }
                    // need to fill
                    else if (CurrentBin.ExistingElevationM < CurrentBin.TargetElevationM)
                    {
                        // get depth of fill, use max fill depth unless the target elevation is shallower
                        double FillDepthM = CurrentEquipmentSettings.FrontPan.MaxFillDepthMm / 1000.0;
                        if ((CurrentBin.ExistingElevationM + FillDepthM) > CurrentBin.TargetElevationM)
                        {
                            FillDepthM = CurrentBin.TargetElevationM - CurrentBin.ExistingElevationM;
                        }

                        // convert to command for controller
                        uint Value = 100 + (uint)(FillDepthM * 1000.0);
                        Controller.SetFrontCutValve(Value);
                        // rear scraper mirrors height
                        Controller.SetRearCutValve(Value);
                    }
                    // need to cut, but we are filling
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

            // rear blade is set to auto filling
            if (CurrentEquipmentStatus.RearPan.Mode == PanStatus.BladeMode.AutoFilling)
            {
                Bin? CurrentBin = Field.LatLonToBin(CurrentEquipmentStatus.RearPan.Fix.Latitude, CurrentEquipmentStatus.RearPan.Fix.Longitude);
                if (CurrentBin != null)
                {
                    // no data for this bin
                    if (CurrentBin.ExistingElevationM == 0)
                    {
                        Controller.SetRearCutValve(MAX_BLADE_HEIGHT_MM + 100);
                    }
                    // need to fill
                    else if (CurrentBin.ExistingElevationM < CurrentBin.TargetElevationM)
                    {
                        // get depth of fill, use max fill depth unless the target elevation is shallower
                        double FillDepthM = CurrentEquipmentSettings.RearPan.MaxFillDepthMm / 1000.0;
                        if ((CurrentBin.ExistingElevationM + FillDepthM) > CurrentBin.TargetElevationM)
                        {
                            FillDepthM = CurrentBin.TargetElevationM - CurrentBin.ExistingElevationM;
                        }

                        // convert to command for controller
                        uint Value = 100 + (uint)(FillDepthM * 1000.0);
                        Controller.SetRearCutValve(Value);
                    }
                    // need to cut, but we are filling
                    else
                    {
                        // float on surface
                        Controller.SetRearCutValve(100);
                    }
                }
                // no bin - outside of field
                else
                {
                    Controller.SetRearCutValve(MAX_BLADE_HEIGHT_MM + 100);
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
