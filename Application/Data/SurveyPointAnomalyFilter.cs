using System;
using System.Collections.Generic;
using AgGrade.Controller;

namespace AgGrade.Data
{
    /// <summary>
    /// Detects and removes GPS elevation glitches from a survey path while points are being recorded.
    /// When a new point is appended, the last few points are checked for V-shaped spikes and for short
    /// runs of abnormally low readings that recover sharply on the next shot.
    /// </summary>
    public static class SurveyPointAnomalyFilter
    {
        /// <summary>Ignore very short segments when computing slope.</summary>
        private const double MinSegmentDistanceM = 0.5;

        /// <summary>Minimum dip below both neighbors to treat a point as a spike (~2 ft).</summary>
        private const double MinSpikeDeflectionM = 0.6096;

        /// <summary>Typical paddock grades stay below this; steeper segments into/out of a dip are glitches.</summary>
        private const double MaxFeasibleSlope = 0.08;

        /// <summary>Steep uphill onto a new point after a low run indicates the run was bad (~12%).</summary>
        private const double MinRecoverySlope = 0.12;

        /// <summary>Minimum elevation gain on a recovery segment (~2 ft).</summary>
        private const double MinRecoveryRiseM = 0.6096;

        /// <summary>How far below the chord a run point must sit to count as bad (~1 ft).</summary>
        private const double MinChordDeflectionM = 0.3048;

        /// <summary>Maximum number of prior points to inspect for a bad run.</summary>
        private const int MaxRunLookback = 12;

        /// <summary>
        /// After a point is appended, inspect the tail of <paramref name="points"/> and remove spikes
        /// or bad runs. Returns how many points were removed.
        /// </summary>
        public static int PruneAfterAppend
            (
            List<TopologyPoint> points
            )
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int removed = 0;
            bool changed;
            do
            {
                changed = false;
                if (TryRemoveBadRunAtEnd(points))
                {
                    removed++;
                    changed = true;
                    continue;
                }

                if (TryRemoveMiddleSpikeAtEnd(points))
                {
                    removed++;
                    changed = true;
                }
            }
            while (changed);

            return removed;
        }

        /// <summary>
        /// Replays incremental append pruning across an entire point list. Useful when cleaning a
        /// survey file that was recorded before filtering existed.
        /// </summary>
        public static int PruneAll
            (
            List<TopologyPoint> points
            )
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int removed = 0;
            int index = 0;
            while (index < points.Count)
            {
                index++;
                removed += PruneAfterAppend(points);
            }

            return removed;
        }

        private static bool TryRemoveMiddleSpikeAtEnd
            (
            List<TopologyPoint> points
            )
        {
            int endIndex = points.Count - 1;
            if (endIndex < 2)
                return false;

            int middleIndex = endIndex - 1;
            int startIndex = endIndex - 2;

            if (IsLowSpike(points, startIndex, middleIndex, endIndex))
            {
                points.RemoveAt(middleIndex);
                return true;
            }

            if (IsHighSpike(points, startIndex, middleIndex, endIndex))
            {
                points.RemoveAt(middleIndex);
                return true;
            }

            return false;
        }

        private static bool IsLowSpike
            (
            List<TopologyPoint> points,
            int startIndex,
            int middleIndex,
            int endIndex
            )
        {
            TopologyPoint start = points[startIndex];
            TopologyPoint middle = points[middleIndex];
            TopologyPoint end = points[endIndex];

            double dropFromStartM = start.ExistingElevation - middle.ExistingElevation;
            double riseToEndM = end.ExistingElevation - middle.ExistingElevation;
            if (dropFromStartM < MinSpikeDeflectionM || riseToEndM < MinSpikeDeflectionM)
                return false;

            double distanceInM = HorizontalDistanceM(start, middle);
            double distanceOutM = HorizontalDistanceM(middle, end);
            if (distanceInM < MinSegmentDistanceM || distanceOutM < MinSegmentDistanceM)
                return false;

            double slopeIn = (middle.ExistingElevation - start.ExistingElevation) / distanceInM;
            double slopeOut = (end.ExistingElevation - middle.ExistingElevation) / distanceOutM;
            return slopeIn < -MaxFeasibleSlope && slopeOut > MaxFeasibleSlope;
        }

        private static bool IsHighSpike
            (
            List<TopologyPoint> points,
            int startIndex,
            int middleIndex,
            int endIndex
            )
        {
            TopologyPoint start = points[startIndex];
            TopologyPoint middle = points[middleIndex];
            TopologyPoint end = points[endIndex];

            double riseFromStartM = middle.ExistingElevation - start.ExistingElevation;
            double dropToEndM = middle.ExistingElevation - end.ExistingElevation;
            if (riseFromStartM < MinSpikeDeflectionM || dropToEndM < MinSpikeDeflectionM)
                return false;

            double distanceInM = HorizontalDistanceM(start, middle);
            double distanceOutM = HorizontalDistanceM(middle, end);
            if (distanceInM < MinSegmentDistanceM || distanceOutM < MinSegmentDistanceM)
                return false;

            double slopeIn = (middle.ExistingElevation - start.ExistingElevation) / distanceInM;
            double slopeOut = (end.ExistingElevation - middle.ExistingElevation) / distanceOutM;
            return slopeIn > MaxFeasibleSlope && slopeOut < -MaxFeasibleSlope;
        }

        private static bool TryRemoveBadRunAtEnd
            (
            List<TopologyPoint> points
            )
        {
            int endIndex = points.Count - 1;
            if (endIndex < 2)
                return false;

            int recoveryStartIndex = endIndex - 1;
            TopologyPoint recoveryStart = points[recoveryStartIndex];
            TopologyPoint recoveryEnd = points[endIndex];

            double recoveryDistanceM = HorizontalDistanceM(recoveryStart, recoveryEnd);
            double recoveryRiseM = recoveryEnd.ExistingElevation - recoveryStart.ExistingElevation;
            if (recoveryDistanceM < MinSegmentDistanceM ||
                recoveryRiseM < MinRecoveryRiseM ||
                recoveryRiseM / recoveryDistanceM < MinRecoverySlope)
            {
                return false;
            }

            int earliestRunStart = Math.Max(1, endIndex - MaxRunLookback);
            int bestRunStart = -1;
            for (int runStart = earliestRunStart; runStart <= recoveryStartIndex; runStart++)
            {
                if (RunLiesBelowChord(points, runStart - 1, runStart, recoveryStartIndex, endIndex))
                    bestRunStart = runStart;
            }

            if (bestRunStart < 0)
                return false;

            while (bestRunStart > 1 &&
                   IsPointBelowChord(points, bestRunStart - 2, endIndex, bestRunStart - 1))
            {
                bestRunStart--;
            }

            points.RemoveRange(bestRunStart, recoveryStartIndex - bestRunStart + 1);
            return true;
        }

        private static bool IsPointBelowChord
            (
            List<TopologyPoint> points,
            int anchorIndex,
            int endIndex,
            int pointIndex
            )
        {
            TopologyPoint anchor = points[anchorIndex];
            TopologyPoint end = points[endIndex];
            TopologyPoint point = points[pointIndex];

            double chordDistanceM = HorizontalDistanceM(anchor, end);
            if (chordDistanceM < MinSegmentDistanceM)
                return false;

            double distanceFromAnchorM = HorizontalDistanceM(anchor, point);
            double expectedElevationM = anchor.ExistingElevation +
                (distanceFromAnchorM / chordDistanceM) *
                (end.ExistingElevation - anchor.ExistingElevation);

            return point.ExistingElevation < expectedElevationM - MinChordDeflectionM;
        }

        private static bool RunLiesBelowChord
            (
            List<TopologyPoint> points,
            int anchorIndex,
            int runStartIndex,
            int runEndIndex,
            int endIndex
            )
        {
            for (int index = runStartIndex; index <= runEndIndex; index++)
            {
                if (!IsPointBelowChord(points, anchorIndex, endIndex, index))
                    return false;
            }

            return true;
        }

        private static double HorizontalDistanceM
            (
            TopologyPoint from,
            TopologyPoint to
            )
        {
            return Haversine.Distance(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        }
    }
}
