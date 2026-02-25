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

        /// <summary>
        /// Number of 100ms segments to accumulate so swept polygon is long enough for MinBinCoveragePcent.
        /// </summary>
        private const int ACCUMULATED_SEGMENTS_MAX = 10;

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
        private Coordinate? LastRearBladeLeft;
        private Coordinate? LastRearBladeRight;
        private Coordinate? FrontAccumulatedStartLeft;
        private Coordinate? FrontAccumulatedStartRight;
        private List<(Coordinate Right, Coordinate Left)> FrontAccumulatedEnds = new List<(Coordinate, Coordinate)>();
        private Coordinate? RearAccumulatedStartLeft;
        private Coordinate? RearAccumulatedStartRight;
        private List<(Coordinate Right, Coordinate Left)> RearAccumulatedEnds = new List<(Coordinate, Coordinate)>();

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

            CalcTimer.Start();
            ElapsedTimer.Start();
        }

        public void StartFrontCutting
            (
            )
        {
            LastFrontBladeLeft = null;
            LastFrontBladeRight = null;
            FrontAccumulatedStartLeft = null;
            FrontAccumulatedStartRight = null;
            FrontAccumulatedEnds.Clear();

            OnFrontVolumeCutUpdated?.Invoke(FrontCutVolumeBCY);
        }

        public void StartFrontFilling
            (
            )
        {
            LastFrontBladeLeft = null;
            LastFrontBladeRight = null;
            FrontAccumulatedStartLeft = null;
            FrontAccumulatedStartRight = null;
            FrontAccumulatedEnds.Clear();

            OnFrontVolumeCutUpdated?.Invoke(FrontCutVolumeBCY);
        }

        public void StartRearCutting
            (
            )
        {
            LastRearBladeLeft = null;
            LastRearBladeRight = null;
            RearAccumulatedStartLeft = null;
            RearAccumulatedStartRight = null;
            RearAccumulatedEnds.Clear();

            OnRearVolumeCutUpdated?.Invoke(RearCutVolumeBCY);
        }

        public void StartRearFilling
            (
            )
        {
            LastRearBladeLeft = null;
            LastRearBladeRight = null;
            RearAccumulatedStartLeft = null;
            RearAccumulatedStartRight = null;
            RearAccumulatedEnds.Clear();

            OnRearVolumeCutUpdated?.Invoke(RearCutVolumeBCY);
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
                if (CurrentEquipmentStatus.FrontPan.Mode == PanStatus.BladeMode.AutoCutting ||
                    CurrentEquipmentStatus.RearPan.Mode  == PanStatus.BladeMode.AutoCutting ||
                    CurrentEquipmentStatus.FrontPan.Mode == PanStatus.BladeMode.AutoFilling ||
                    CurrentEquipmentStatus.RearPan.Mode  == PanStatus.BladeMode.AutoFilling)
                {
                    CullProcessedBins();
                }

                // front blade is set to auto cutting
                if (CurrentEquipmentSettings.FrontPan.Equipped && (CurrentEquipmentStatus.FrontPan.Mode == PanStatus.BladeMode.AutoCutting))
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
                        if (FrontAccumulatedStartLeft == null)
                        {
                            FrontAccumulatedStartLeft = LastFrontBladeLeft;
                            FrontAccumulatedStartRight = LastFrontBladeRight;
                        }
                        FrontAccumulatedEnds.Add((FrontBladeRight, FrontBladeLeft));
                        if (FrontAccumulatedEnds.Count > ACCUMULATED_SEGMENTS_MAX)
                        {
                            var first = FrontAccumulatedEnds[0];
                            FrontAccumulatedEnds.RemoveAt(0);
                            FrontAccumulatedStartLeft = first.Left;
                            FrontAccumulatedStartRight = first.Right;
                        }
                        List<Coordinate> SweptPolygon = BuildAccumulatedSweptPolygon(FrontAccumulatedStartLeft, FrontAccumulatedStartRight, FrontAccumulatedEnds);
                        List<Bin> BinsToCut = Field.GetBinsInside(SweptPolygon, CurrentEquipmentSettings.MinBinCoveragePcent);
                        foreach (Bin B in BinsToCut)
                        {
                            FrontCutBin(B);
                        }
                    }

                    LastFrontBladeLeft = FrontBladeLeft;
                    LastFrontBladeRight = FrontBladeRight;

                    OnFrontVolumeCutUpdated?.Invoke(FrontCutVolumeBCY);
                }

                // rear blade is set to auto cutting
                if (CurrentEquipmentSettings.RearPan.Equipped && (CurrentEquipmentStatus.RearPan.Mode == PanStatus.BladeMode.AutoCutting))
                {
                    // get angle perpendicular to heading
                    double BladeHeading;
                    BladeHeading = CurrentEquipmentStatus.RearPan.Fix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);
                    double BladePerp = BladeHeading + 90;
                    if (BladePerp > 360) BladePerp -= 360;
                    if (BladePerp < 0) BladePerp += 360;

                    // get length of blade
                    double BladeLengthM = (double)CurrentEquipmentSettings.RearPan.WidthMm / 1000.0;

                    // get blade location
                    double Lat = CurrentEquipmentStatus.RearPan.Fix.Latitude;
                    double Lon = CurrentEquipmentStatus.RearPan.Fix.Longitude;

                    // get left end of blade
                    Haversine.MoveDistanceBearing(ref Lat, ref Lon, BladePerp, BladeLengthM / 2);
                    Coordinate RearBladeLeft = new Coordinate(Lat, Lon);
                    Bin? StartBin = Field.LatLonToBin(RearBladeLeft);

                    // get right end of blade
                    Lat = CurrentEquipmentStatus.RearPan.Fix.Latitude;
                    Lon = CurrentEquipmentStatus.RearPan.Fix.Longitude;
                    Haversine.MoveDistanceBearing(ref Lat, ref Lon, BladePerp, -(BladeLengthM / 2));
                    Coordinate RearBladeRight = new Coordinate(Lat, Lon);
                    Bin? EndBin = Field.LatLonToBin(RearBladeRight);

                    // we have a previous location
                    if ((LastRearBladeLeft != null) && (LastRearBladeRight != null))
                    {
                        if (RearAccumulatedStartLeft == null)
                        {
                            RearAccumulatedStartLeft = LastRearBladeLeft;
                            RearAccumulatedStartRight = LastRearBladeRight;
                        }
                        RearAccumulatedEnds.Add((RearBladeRight, RearBladeLeft));
                        if (RearAccumulatedEnds.Count > ACCUMULATED_SEGMENTS_MAX)
                        {
                            var first = RearAccumulatedEnds[0];
                            RearAccumulatedEnds.RemoveAt(0);
                            RearAccumulatedStartLeft = first.Left;
                            RearAccumulatedStartRight = first.Right;
                        }
                        List<Coordinate> SweptPolygon = BuildAccumulatedSweptPolygon(RearAccumulatedStartLeft, RearAccumulatedStartRight, RearAccumulatedEnds);
                        List<Bin> BinsToCut = Field.GetBinsInside(SweptPolygon, CurrentEquipmentSettings.MinBinCoveragePcent);
                        foreach (Bin B in BinsToCut)
                        {
                            RearCutBin(B);
                        }
                    }

                    LastRearBladeLeft = RearBladeLeft;
                    LastRearBladeRight = RearBladeRight;

                    OnRearVolumeCutUpdated?.Invoke(RearCutVolumeBCY);
                }

                // front blade is set to auto filling
                if (CurrentEquipmentSettings.FrontPan.Equipped && (CurrentEquipmentStatus.FrontPan.Mode == PanStatus.BladeMode.AutoFilling))
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
                        if (FrontAccumulatedStartLeft == null)
                        {
                            FrontAccumulatedStartLeft = LastFrontBladeLeft;
                            FrontAccumulatedStartRight = LastFrontBladeRight;
                        }
                        FrontAccumulatedEnds.Add((FrontBladeRight, FrontBladeLeft));
                        if (FrontAccumulatedEnds.Count > ACCUMULATED_SEGMENTS_MAX)
                        {
                            var first = FrontAccumulatedEnds[0];
                            FrontAccumulatedEnds.RemoveAt(0);
                            FrontAccumulatedStartLeft = first.Left;
                            FrontAccumulatedStartRight = first.Right;
                        }
                        List<Coordinate> SweptPolygon = BuildAccumulatedSweptPolygon(FrontAccumulatedStartLeft, FrontAccumulatedStartRight, FrontAccumulatedEnds);
                        List<Bin> BinsToFill = Field.GetBinsInside(SweptPolygon, CurrentEquipmentSettings.MinBinCoveragePcent);
                        foreach (Bin B in BinsToFill)
                        {
                            FrontFillBin(B);
                        }
                    }

                    LastFrontBladeLeft = FrontBladeLeft;
                    LastFrontBladeRight = FrontBladeRight;

                    OnFrontVolumeCutUpdated?.Invoke(FrontCutVolumeBCY);
                }

                // rear blade is set to auto filling
                if (CurrentEquipmentSettings.RearPan.Equipped && (CurrentEquipmentStatus.RearPan.Mode == PanStatus.BladeMode.AutoFilling))
                {
                    // get angle perpendicular to heading
                    double BladeHeading;
                    BladeHeading = CurrentEquipmentStatus.RearPan.Fix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes);
                    double BladePerp = BladeHeading + 90;
                    if (BladePerp > 360) BladePerp -= 360;
                    if (BladePerp < 0) BladePerp += 360;

                    // get length of blade
                    double BladeLengthM = (double)CurrentEquipmentSettings.RearPan.WidthMm / 1000.0;

                    // get blade location
                    double Lat = CurrentEquipmentStatus.RearPan.Fix.Latitude;
                    double Lon = CurrentEquipmentStatus.RearPan.Fix.Longitude;

                    // get left end of blade
                    Haversine.MoveDistanceBearing(ref Lat, ref Lon, BladePerp, BladeLengthM / 2);
                    Coordinate RearBladeLeft = new Coordinate(Lat, Lon);
                    Bin? StartBin = Field.LatLonToBin(RearBladeLeft);

                    // get right end of blade
                    Lat = CurrentEquipmentStatus.RearPan.Fix.Latitude;
                    Lon = CurrentEquipmentStatus.RearPan.Fix.Longitude;
                    Haversine.MoveDistanceBearing(ref Lat, ref Lon, BladePerp, -(BladeLengthM / 2));
                    Coordinate RearBladeRight = new Coordinate(Lat, Lon);
                    Bin? EndBin = Field.LatLonToBin(RearBladeRight);

                    // we have a previous location
                    if ((LastRearBladeLeft != null) && (LastRearBladeRight != null))
                    {
                        if (RearAccumulatedStartLeft == null)
                        {
                            RearAccumulatedStartLeft = LastRearBladeLeft;
                            RearAccumulatedStartRight = LastRearBladeRight;
                        }
                        RearAccumulatedEnds.Add((RearBladeRight, RearBladeLeft));
                        if (RearAccumulatedEnds.Count > ACCUMULATED_SEGMENTS_MAX)
                        {
                            var first = RearAccumulatedEnds[0];
                            RearAccumulatedEnds.RemoveAt(0);
                            RearAccumulatedStartLeft = first.Left;
                            RearAccumulatedStartRight = first.Right;
                        }
                        List<Coordinate> SweptPolygon = BuildAccumulatedSweptPolygon(RearAccumulatedStartLeft, RearAccumulatedStartRight, RearAccumulatedEnds);
                        List<Bin> BinsToFill = Field.GetBinsInside(SweptPolygon, CurrentEquipmentSettings.MinBinCoveragePcent);
                        foreach (Bin B in BinsToFill)
                        {
                            RearFillBin(B);
                        }
                    }

                    LastRearBladeLeft = RearBladeLeft;
                    LastRearBladeRight = RearBladeRight;

                    OnRearVolumeCutUpdated?.Invoke(RearCutVolumeBCY);
                }
            }
        }

        /// <summary>
        /// Adds material to a bin
        /// </summary>
        /// <param name="BinToFill">Bill to add to or null for no bin</param>
        private void FrontFillBin
            (
            Bin? BinToFill
            )
        {
            // if bin is specified and has valid data then process it
            if ((BinToFill != null) && (BinToFill.ExistingElevationM > 0))
            {
                // if we haven't already seen this bin
                if (!FrontProcessedBins.Contains(BinToFill))
                {
                    // blade is above the surface and we have soil to deposit
                    if ((CurrentEquipmentStatus!.FrontPan.BladeHeight > 0) && (FrontCutVolumeBCY > 0))
                    {
                        // get height change for bin based on how much we have left in the scraper
                        double FillHeightM = FrontCutVolumeBCY / 1.30795 / BIN_SIZE_M / BIN_SIZE_M;
                        if (FillHeightM > CurrentEquipmentStatus.FrontPan.BladeHeight / 1000.0)
                        {
                            FillHeightM = CurrentEquipmentStatus.FrontPan.BladeHeight / 1000.0;
                        }

                        if (FillHeightM > 0)
                        {
                            // increase bin height
                            BinToFill.ExistingElevationM += FillHeightM;
                            BinToFill.Dirty = true;
                            BinToFill.NumberofFills++;

                            // update volume, can't go negative
                            FrontCutVolumeBCY -= BIN_SIZE_M * BIN_SIZE_M * FillHeightM * 1.30795;
                            if (FrontCutVolumeBCY < 0)
                            {
                                FrontCutVolumeBCY = 0;
                            }

                            // remember this bin so we don't process it more than one this pass of the blade
                            FrontProcessedBins.Add(BinToFill);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds material to a bin
        /// </summary>
        /// <param name="BinToFill">Bill to add to or null for no bin</param>
        private void RearFillBin
            (
            Bin? BinToFill
            )
        {
            // if bin is specified and has valid data then process it
            if ((BinToFill != null) && (BinToFill.ExistingElevationM > 0))
            {
                // if we haven't already seen this bin
                if (!RearProcessedBins.Contains(BinToFill))
                {
                    // blade is above the surface
                    if (CurrentEquipmentStatus!.RearPan.BladeHeight > 0)
                    {
                        // get height change for bin based on how much we have left in the scraper
                        double FillHeightM = RearCutVolumeBCY / 1.30795 / BIN_SIZE_M / BIN_SIZE_M;
                        if (FillHeightM > CurrentEquipmentStatus.RearPan.BladeHeight / 1000.0)
                        {
                            FillHeightM = CurrentEquipmentStatus.RearPan.BladeHeight / 1000.0;
                        }

                        if (FillHeightM > 0)
                        {
                            // increase bin height
                            BinToFill.ExistingElevationM += FillHeightM;
                            BinToFill.Dirty = true;
                            BinToFill.NumberofFills++;

                            // update volume, can't go negative
                            RearCutVolumeBCY -= BIN_SIZE_M * BIN_SIZE_M * FillHeightM * 1.30795;
                            if (RearCutVolumeBCY < 0)
                            {
                                RearCutVolumeBCY = 0;
                            }

                            // remember this bin so we don't process it more than one this pass of the blade
                            RearProcessedBins.Add(BinToFill);
                        }
                    }
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

                        if (CutHeightM < 0)
                        {
                            // reduce bin height (adding because the cut height is negative)
                            BinToCut.ExistingElevationM += CutHeightM;
                            BinToCut.Dirty = true;
                            BinToCut.NumberOfCuts++;

                            // update volume
                            FrontCutVolumeBCY += BIN_SIZE_M * BIN_SIZE_M * -CutHeightM * 1.30795;

                            // remember this bin so we don't process it more than one this pass of the blade
                            FrontProcessedBins.Add(BinToCut);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove material from the bin
        /// </summary>
        /// <param name="BinToCut">Bin to cut from or null for no bin</param>
        private void RearCutBin
            (
            Bin? BinToCut
            )
        {
            // if bin is specified and has valid data then process it
            if ((BinToCut != null) && (BinToCut.ExistingElevationM > 0))
            {
                // if we haven't already seen this bin
                if (!RearProcessedBins.Contains(BinToCut))
                {
                    // blade is below the surface
                    if (CurrentEquipmentStatus!.RearPan.BladeHeight < 0)
                    {
                        double CutHeightM = CurrentEquipmentStatus.RearPan.BladeHeight / 1000.0;

                        if (CutHeightM < 0)
                        {
                            // reduce bin height (adding because the cut height is negative)
                            BinToCut.ExistingElevationM += CutHeightM;
                            BinToCut.Dirty = true;
                            BinToCut.NumberOfCuts++;

                            // update volume
                            RearCutVolumeBCY += BIN_SIZE_M * BIN_SIZE_M * -CutHeightM * 1.30795;

                            // remember this bin so we don't process it more than one this pass of the blade
                            RearProcessedBins.Add(BinToCut);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Builds a single polygon from accumulated segment start and segment ends (outline of swept strip).
        /// </summary>
        private static List<Coordinate> BuildAccumulatedSweptPolygon(Coordinate startLeft, Coordinate startRight, List<(Coordinate Right, Coordinate Left)> ends)
        {
            List<Coordinate> polygon = new List<Coordinate>();
            polygon.Add(startLeft);
            polygon.Add(startRight);
            for (int i = 0; i < ends.Count; i++)
                polygon.Add(ends[i].Right);
            polygon.Add(ends[ends.Count - 1].Left);
            for (int i = ends.Count - 2; i >= 0; i--)
                polygon.Add(ends[i].Left);
            polygon.Add(startLeft);
            return polygon;
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
                        CurrentEquipmentStatus.RearPan.Fix.Latitude, CurrentEquipmentStatus.RearPan.Fix.Longitude);
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
