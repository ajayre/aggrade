using OpenCvSharp;
using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace AgGrade.Data
{
    /// <summary>
    /// Records live GPS survey points into a <see cref="Survey"/> while the operator drives the field.
    /// Converts absolute GPS altitudes into multiplane-relative elevations so incremental file saves
    /// remain correct and surveys can be resumed after a crash.
    /// </summary>
    public class SurveyUpdater
    {
        /// <summary>How often survey point updates are evaluated, in milliseconds.</summary>
        private const int UPDATE_PERIOD_MS = 250;

        /// <summary>Minimum spacing between recorded points, in feet.</summary>
        private const double MIN_POINT_SPACING_FT = 10.0;

        /// <summary>Which edge of the tractor path is being surveyed as the field boundary.</summary>
        public enum BoundaryModes
        {
            None,
            Left,
            Right
        }

        /// <summary>Survey being updated, or null when not recording.</summary>
        public Survey? Survey;

        private Timer UpdateTimer;
        private EquipmentStatus? CurrentEquipmentStatus;
        private EquipmentSettings? CurrentEquipmentSettings;
        private BoundaryModes BoundaryMode;
        private readonly SynchronizationContext? UiContext;

        /// <summary>
        /// Creates the updater and configures the periodic timer (not started until <see cref="Start"/>).
        /// </summary>
        public SurveyUpdater
            (
            )
        {
            UiContext = SynchronizationContext.Current;

            UpdateTimer = new Timer();
            UpdateTimer.Interval = UPDATE_PERIOD_MS;
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;

            Survey = null;
        }

        /// <summary>
        /// Sets the survey to work on, or null to detach from any survey.
        /// </summary>
        /// <param name="NewSurvey">Survey to record into, or null.</param>
        public void SetSurvey
            (
            Survey? NewSurvey
            )
        {
            Survey = NewSurvey;
        }

        /// <summary>
        /// Supplies the latest equipment/GPS status used when adding benchmarks and survey points.
        /// </summary>
        /// <param name="Status">Current equipment status from the controller layer.</param>
        public void SetEquipmentStatus
            (
            EquipmentStatus Status
            )
        {
            CurrentEquipmentStatus = Status;
        }

        /// <summary>
        /// Supplies equipment dimensions and settings used for boundary offset calculations.
        /// </summary>
        /// <param name="Settings">Current equipment settings.</param>
        public void SetEquipmentSettings
            (
            EquipmentSettings Settings
            )
        {
            CurrentEquipmentSettings = Settings;
        }

        /// <summary>
        /// Changes boundary recording mode while surveying is active.
        /// </summary>
        /// <param name="Mode">New boundary mode (left edge, right edge, or interior only).</param>
        public void BoundaryChanged
            (
            BoundaryModes Mode
            )
        {
            BoundaryMode = Mode;
        }

        /// <summary>
        /// Starts periodic survey point recording.
        /// </summary>
        /// <param name="Mode">Initial boundary mode.</param>
        public void Start
            (
            BoundaryModes Mode
            )
        {
            BoundaryMode = Mode;

            UpdateTimer.Start();
        }

        /// <summary>
        /// Stops periodic survey point recording.
        /// </summary>
        public void Stop
            (
            )
        {
            UpdateTimer.Stop();
        }

        /// <summary>
        /// Adds a benchmark at the current tractor GPS fix. The first benchmark becomes MB and
        /// establishes the absolute GPS anchor; subsequent benchmarks are stored as multiplane-relative
        /// elevations. The survey file is saved immediately after each benchmark is added.
        /// </summary>
        public void AddBenchmark
            (
            )
        {
            if ((Survey != null) && (CurrentEquipmentStatus != null))
            {
                Survey.AddBenchmark(
                    new Coordinate(
                        CurrentEquipmentStatus.TractorFix.Latitude,
                        CurrentEquipmentStatus.TractorFix.Longitude),
                    CurrentEquipmentStatus.TractorFix.Altitude);
                Survey.Save();
            }
        }

        /// <summary>
        /// Timer callback that appends a boundary or interior point when the tractor has moved far
        /// enough from the last recorded point. GPS altitude is converted to multiplane-relative
        /// storage before the survey file is saved.
        /// </summary>
        /// <param name="sender">Timer source.</param>
        /// <param name="e">Elapsed event arguments.</param>
        private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (UiContext != null)
            {
                UiContext.Post(_ => RecordSurveyPointIfNeeded(), null);
                return;
            }

            RecordSurveyPointIfNeeded();
        }

        /// <summary>
        /// Appends a survey point when spacing criteria are met. Runs on the UI thread when
        /// <see cref="SynchronizationContext.Current"/> was captured at construction.
        /// </summary>
        private void RecordSurveyPointIfNeeded()
        {
            if ((Survey == null) || (CurrentEquipmentStatus == null) || (CurrentEquipmentSettings == null)) return;

            const double FEET_TO_METERS = 0.3048;
            const double MIN_POINT_SPACING_M = MIN_POINT_SPACING_FT * FEET_TO_METERS;

            bool isBoundary = (BoundaryMode == BoundaryModes.Left) || (BoundaryMode == BoundaryModes.Right);
            List<TopologyPoint> targetPoints = isBoundary ? Survey.BoundaryPoints : Survey.InteriorPoints;

            if (targetPoints.Count > 0)
            {
                TopologyPoint lastPoint = targetPoints[targetPoints.Count - 1];
                double distanceFromLastM = Haversine.Distance(
                    CurrentEquipmentStatus.TractorFix.Latitude,
                    CurrentEquipmentStatus.TractorFix.Longitude,
                    lastPoint.Latitude,
                    lastPoint.Longitude);

                if (distanceFromLastM <= MIN_POINT_SPACING_M) return;
            }

            // MB must be set first so Survey knows the absolute GPS anchor for relative conversion.
            if (!Survey.MasterAbsoluteElevationM.HasValue)
                return;

            double pointLat = CurrentEquipmentStatus.TractorFix.Latitude;
            double pointLon = CurrentEquipmentStatus.TractorFix.Longitude;

            if (isBoundary)
            {
                double halfTractorWidthM = (CurrentEquipmentSettings.TractorWidthMm / 1000.0) / 2.0;
                double tractorHeadingDeg = CurrentEquipmentStatus.TractorFix.Vector.TrackTrueDeg;
                double boundaryHeadingDeg = tractorHeadingDeg + (BoundaryMode == BoundaryModes.Left ? -90.0 : 90.0);
                Haversine.MoveDistanceBearing(ref pointLat, ref pointLon, boundaryHeadingDeg, halfTractorWidthM);
            }

            targetPoints.Add(new TopologyPoint
            {
                Latitude = pointLat,
                Longitude = pointLon,
                ExistingElevation = Survey.ToMultiplaneRelativeElevationM(CurrentEquipmentStatus.TractorFix.Altitude)
            });

            SurveyPointAnomalyFilter.PruneAfterAppend(targetPoints);

            Survey.Save();
        }
    }
}
