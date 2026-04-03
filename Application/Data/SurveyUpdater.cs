using OpenCvSharp;
using AgGrade.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace AgGrade.Data
{
    public class SurveyUpdater
    {
        // how often we perform the updates in milliseconds
        private const int UPDATE_PERIOD_MS = 250;

        // spacing of points
        private const double MIN_POINT_SPACING_FT = 16.0;

        public enum BoundaryModes
        {
            None,
            Left,
            Right
        }

        public Survey? Survey;

        private Timer UpdateTimer;
        private EquipmentStatus? CurrentEquipmentStatus;
        private EquipmentSettings? CurrentEquipmentSettings;
        private BoundaryModes BoundaryMode;

        public SurveyUpdater
            (
            )
        {
            UpdateTimer = new Timer();
            UpdateTimer.Interval = UPDATE_PERIOD_MS;
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;

            Survey = null;
        }

        /// <summary>
        /// Sets the survey to work on or null for no survey
        /// </summary>
        /// <param name="NewSurvey">Survey to work on</param>
        public void SetSurvey
            (
            Survey? NewSurvey
            )
        {
            Survey = NewSurvey;
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
        /// Call to change the boundary mode when already started
        /// </summary>
        /// <param name="Mode">New boundary mode</param>
        public void BoundaryChanged
            (
            BoundaryModes Mode
            )
        {
            BoundaryMode = Mode;
        }

        /// <summary>
        /// Call to start recording survey points
        /// </summary>
        /// <param name="Mode">Boundary mode</param>
        public void Start
            (
            BoundaryModes Mode
            )
        {
            BoundaryMode = Mode;

            UpdateTimer.Start();
        }

        /// <summary>
        /// Call to stop recording survey points
        /// </summary>
        public void Stop
            (
            )
        {
            UpdateTimer.Stop();
        }

        /// <summary>
        /// Adds a new benchmark to the survey
        /// </summary>
        public void AddBenchmark
            (
            )
        {
            if ((Survey != null) && (CurrentEquipmentStatus != null))
            {
                Survey.AddBenchmark(new Coordinate(CurrentEquipmentStatus.TractorFix.Latitude, CurrentEquipmentStatus.TractorFix.Longitude), CurrentEquipmentStatus.TractorFix.Altitude);
                Survey.SaveToMultiplane();
            }
        }

        /// <summary>
        /// Perform the survey updates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
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

            double pointLat = CurrentEquipmentStatus.TractorFix.Latitude;
            double pointLon = CurrentEquipmentStatus.TractorFix.Longitude;

            if (isBoundary)
            {
                double halfTractorWidthM = (CurrentEquipmentSettings.TractorWidthMm / 1000.0) / 2.0;
                double tractorHeadingDeg = CurrentEquipmentStatus.TractorFix.Vector.TrackMagneticDeg;
                double boundaryHeadingDeg = tractorHeadingDeg + (BoundaryMode == BoundaryModes.Left ? -90.0 : 90.0);
                Haversine.MoveDistanceBearing(ref pointLat, ref pointLon, boundaryHeadingDeg, halfTractorWidthM);
            }

            targetPoints.Add(new TopologyPoint
            {
                Latitude = pointLat,
                Longitude = pointLon,
                ExistingElevation = CurrentEquipmentStatus.TractorFix.Altitude
            });

            Survey.SaveToMultiplane();
        }
    }
}
