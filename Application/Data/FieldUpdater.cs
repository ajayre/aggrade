using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace AgGrade.Data
{
    public class FieldUpdater
    {
        // how often we perform the calculations in milliseconds
        private const int CALC_PERIOD_MS = 100;

        // size of a side of a bin in meters (2ft)
        private const double BIN_SIZE_M = 0.6096;

        /// <summary>
        /// Distance bin has to be from a blade center before it can be cut again
        /// </summary>
        private const double RECUT_MIN_BLADE_BIN_DISTANCE_M = 3.0;

        // max time before we can process a bin for cutting again
        // this avoids processing a bin more than once if the blade is moving slowly
        // specified in seconds
        // for a value of 1.0s the cutting speed must be a minimum of 1.4 MPH to avoid
        // double cutting a bin
        // but at the same time this value cannot be so low than a bin could be processed by the
        // front blade but skipped by the rear blade which is 5m behind, if both blades are cutting
        // at the same time (unlikely)
        // minimum cutting speed: Speed (mph) = 7200 / (5280 × MAX_BIN_AGE_S)
        // maximum cutting speed: Speed (mph) = (5 × 3.28084 × 3600) / (5280 × MAX_BIN_AGE_S)
        // for a value of 1.4: min speed = 1 mph, max speed = 8 mph
        private const double MAX_BIN_AGE_S = 6.0;

        private Field Field;
        private Timer CalcTimer;
        private AppSettings? CurrentAppSettings;
        private EquipmentSettings? CurrentEquipmentSettings;
        private EquipmentStatus? CurrentEquipmentStatus;
        private List<Bin> FrontProcessedBins = new List<Bin>();
        private List<Bin> RearProcessedBins = new List<Bin>();
        private Stopwatch ElapsedTimer;
        private double FrontCutVolumeBCY;
        private double RearCutVolumeBCY;
        private Coordinate? LastFrontBladeLeft;
        private Coordinate? LastFrontBladeRight;

        public delegate void VolumeCutUpdated(double VolumeBCY);
        public event VolumeCutUpdated OnFrontVolumeCutUpdated = null;
        public event VolumeCutUpdated OnRearVolumeCutUpdated = null;

        public FieldUpdater
            (
            )
        {
            CalcTimer = new Timer();
            CalcTimer.Interval = CALC_PERIOD_MS;
            CalcTimer.Elapsed += CalcTimer_Elapsed;

            ElapsedTimer = new Stopwatch();
        }

        /// <summary>
        /// Start processing of the blades
        /// </summary>
        public void Start
            (
            )
        {
            FrontCutVolumeBCY = 0;
            RearCutVolumeBCY = 0;

            OnFrontVolumeCutUpdated?.Invoke(FrontCutVolumeBCY);
            OnRearVolumeCutUpdated?.Invoke(RearCutVolumeBCY);

            CalcTimer.Start();
            ElapsedTimer.Start();
        }

        /// <summary>
        /// Stop processing of the blades
        /// </summary>
        public void Stop
            (
            )
        {
            CalcTimer.Stop();
            ElapsedTimer.Stop();
        }

        /// <summary>
        /// Sets the field to work on
        /// </summary>
        /// <param name="NewField">Field to work on</param>
        public void SetField
            (
            Field NewField
            )
        {
            Field = NewField;
        }

        /// <summary>
        /// Perform the field updating calculations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalcTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if ((Field == null) || (CurrentEquipmentSettings == null) || (CurrentEquipmentStatus == null) || (CurrentAppSettings == null)) return;

            // only cut if we are moving
            if (CurrentEquipmentStatus.TractorFix.Vector.Speedkph > 0)
            {
                // get time since last call
                long ElapsedMilliseconds = ElapsedTimer.ElapsedMilliseconds;
                ElapsedTimer.Restart();

                // remove bins that were processed in the past
                if (CurrentEquipmentStatus.FrontPan.BladeAuto || CurrentEquipmentStatus.RearPan.BladeAuto)
                {
                    CullProcessedBins();
                }

                // front blade is set to auto cutting
                if (CurrentEquipmentStatus.FrontPan.BladeAuto)
                {
                    // get angle perpendicular to heading
                    double BladeHeading;
                    BladeHeading = CurrentEquipmentStatus.FrontPan.Fix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);
                    double BladePerp = BladeHeading + 90;
                    if (BladePerp > 360) BladePerp -= 360;
                    if (BladePerp < 0) BladePerp += 360;

                    // get length of blade
                    double BladeLengthM = (double)CurrentEquipmentSettings.FrontPan.WidthMm / 1000.0;

                    // get blade location
                    double Lat = CurrentEquipmentStatus.FrontPan.Fix.Latitude;
                    double Lon = CurrentEquipmentStatus.FrontPan.Fix.Longitude;

                    // get left end of blade
                    Haversine.MoveDistanceBearing(ref Lat, ref Lon, BladePerp, BladeLengthM / 2);
                    Coordinate FrontBladeLeft = new Coordinate(Lat, Lon);
                    Bin? StartBin = Field.LatLonToBin(FrontBladeLeft);

                    // get right end of blade
                    Lat = CurrentEquipmentStatus.FrontPan.Fix.Latitude;
                    Lon = CurrentEquipmentStatus.FrontPan.Fix.Longitude;
                    Haversine.MoveDistanceBearing(ref Lat, ref Lon, BladePerp, -(BladeLengthM / 2));
                    Coordinate FrontBladeRight = new Coordinate(Lat, Lon);
                    Bin? EndBin = Field.LatLonToBin(FrontBladeRight);

                    // we have a previous location
                    if ((LastFrontBladeLeft != null) && (LastFrontBladeRight != null))
                    {
                        List<Coordinate> SweptPolygon = new List<Coordinate>();
                        SweptPolygon.Add(LastFrontBladeLeft);
                        SweptPolygon.Add(LastFrontBladeRight);
                        SweptPolygon.Add(FrontBladeRight);
                        SweptPolygon.Add(FrontBladeLeft);
                        List<Bin> BinsToCut = Field.GetBinsInside(SweptPolygon);
                        foreach (Bin B in BinsToCut)
                        {
                            FrontCutBin(B);
                        }
                    }

                    LastFrontBladeLeft = FrontBladeLeft;
                    LastFrontBladeRight = FrontBladeRight;

                    OnFrontVolumeCutUpdated?.Invoke(FrontCutVolumeBCY);
                }
            }
        }

        /// <summary>
        /// Remove material from the bin
        /// </summary>
        /// <param name="BinToCut">Bin to cut from or null for no bin</param>
        private void FrontCutBin
            (
            Bin? BinToCut
            )
        {
            // if bin is specified and has valid data then process it
            if ((BinToCut != null) && (BinToCut.ExistingElevationM > 0))
            {
                // if we haven't already seen this bin
                if (!FrontProcessedBins.Contains(BinToCut))
                {
                    // blade is below the surface
                    if (CurrentEquipmentStatus!.FrontPan.BladeHeight < 0)
                    {
                        double CutHeightM = CurrentEquipmentStatus.FrontPan.BladeHeight / 1000.0;

                        // reduce bin height (adding because the cut height is negative)
                        BinToCut.ExistingElevationM += CutHeightM;
                        BinToCut.Dirty = true;

                        // update volume
                        FrontCutVolumeBCY += BIN_SIZE_M * BIN_SIZE_M * -CutHeightM * 1.30795;

                        // remember this bin so we don't process it more than one this pass of the blade
                        FrontProcessedBins.Add(BinToCut);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all processed bins that are too old (i.e. have passed under the blade and can now be cut again)
        /// </summary>
        private void CullProcessedBins
            (
            )
        {
            DateTime now = DateTime.Now;

            if ((CurrentEquipmentStatus != null) && (FrontProcessedBins.Count > 0))
            {
                for (int b = FrontProcessedBins.Count - 1; b >= 0; b--)
                {
                    double d = Haversine.Distance(FrontProcessedBins[b].Centroid.Latitude, FrontProcessedBins[b].Centroid.Longitude,
                        CurrentEquipmentStatus.FrontPan.Fix.Latitude, CurrentEquipmentStatus.FrontPan.Fix.Longitude);
                    if (d > RECUT_MIN_BLADE_BIN_DISTANCE_M)
                    {
                        FrontProcessedBins.RemoveAt(b);
                    }
                }
            }

            if ((CurrentEquipmentStatus != null) && (RearProcessedBins.Count > 0))
            {
                for (int b = RearProcessedBins.Count - 1; b >= 0; b--)
                {
                    double d = Haversine.Distance(RearProcessedBins[b].Centroid.Latitude, RearProcessedBins[b].Centroid.Longitude,
                        CurrentEquipmentStatus.FrontPan.Fix.Latitude, CurrentEquipmentStatus.FrontPan.Fix.Longitude);
                    if (d > RECUT_MIN_BLADE_BIN_DISTANCE_M)
                    {
                        RearProcessedBins.RemoveAt(b);
                    }
                }
            }
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

        /// <summary>
        /// Sets the application settings
        /// </summary>
        /// <param name="Settings">New settings</param>
        public void SetApplicationSettings
            (
            AppSettings Settings
            )
        {
            CurrentAppSettings = Settings;
        }
    }
}
