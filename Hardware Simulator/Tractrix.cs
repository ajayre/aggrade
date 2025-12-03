using System;

namespace HardwareSim
{
    /// <summary>
    /// Calculates new trailer positions for a tractor pulling multiple trailers using tractrix mathematics.
    /// 
    /// This implementation is based on the method described in:
    /// "A Method for Determining Offtracking of Multiple Unit Vehicle Combinations"
    /// by T. W. Erkert, J. Sessions, and R. D. Layton (Oregon State University)
    /// 
    /// Paper URL: https://journals.lib.unb.ca/index.php/IJFE/article/download/30089/1882525353/1882525968
    /// 
    /// The tractrix is the mathematical description of the path that the rear axle of a vehicle
    /// follows from a given steering curve for low speed maneuvers. For multiple unit vehicle
    /// combinations, each trailer's hitch point follows the path of the previous unit's rear axle,
    /// and the trailer's rear axle follows a tractrix of the hitch point path.
    /// </summary>
    public class Tractrix
    {
        // Maximum step size for tractrix integration (0.30 meters as per paper)
        private const double MAX_STEP_SIZE = 0.30;

        // Equipment configuration parameters (set via SetEquipment)
        private double _tractorWheelbase;
        private double _tractorToTrailer1Distance;
        private double _trailer1Length;
        private double _trailer1ToTrailer2Distance;
        private double _trailer2Length;
        
        // Distance from rear axle to the trailer's location point (where GNSS receiver is mounted)
        private double _trailer1LocationOffset;  // meters in front of rear axle
        private double _trailer2LocationOffset;  // meters in front of rear axle

        /// <summary>
        /// Structure to hold trailer position results
        /// </summary>
        public class TrailerPositions
        {
            public double Trailer1Latitude;
            public double Trailer1Longitude;
            public double Trailer2Latitude;
            public double Trailer2Longitude;
        }

        /// <summary>
        /// Sets the equipment configuration parameters that remain constant.
        /// Must be called before using CalculateTrailerPositions.
        /// </summary>
        /// <param name="tractorWheelbase">Distance from front axle to rear axle of tractor (meters)</param>
        /// <param name="tractorToTrailer1Distance">Distance from tractor rear axle to first trailer hitch (meters)</param>
        /// <param name="trailer1Length">Length of first trailer from hitch to rear axle (meters)</param>
        /// <param name="trailer1ToTrailer2Distance">Distance from first trailer rear axle to second trailer hitch (meters)</param>
        /// <param name="trailer2Length">Length of second trailer from hitch to rear axle (meters)</param>
        /// <param name="trailer1LocationOffset">Distance from first trailer rear axle to the trailer's location point (meters, in front of rear axle)</param>
        /// <param name="trailer2LocationOffset">Distance from second trailer rear axle to the trailer's location point (meters, in front of rear axle)</param>
        public void SetEquipment(
            double tractorWheelbase,
            double tractorToTrailer1Distance,
            double trailer1Length,
            double trailer1ToTrailer2Distance,
            double trailer2Length,
            double trailer1LocationOffset,
            double trailer2LocationOffset)
        {
            _tractorWheelbase = tractorWheelbase;
            _tractorToTrailer1Distance = tractorToTrailer1Distance;
            _trailer1Length = trailer1Length;
            _trailer1ToTrailer2Distance = trailer1ToTrailer2Distance;
            _trailer2Length = trailer2Length;
            _trailer1LocationOffset = trailer1LocationOffset;
            _trailer2LocationOffset = trailer2LocationOffset;
        }

        /// <summary>
        /// Calculates the initial starting locations for the trailers when all equipment is stationary and aligned.
        /// All equipment has the same heading, positioned in a straight line behind the tractor.
        /// </summary>
        /// <param name="tractorLat">Tractor rear axle latitude (degrees)</param>
        /// <param name="tractorLon">Tractor rear axle longitude (degrees)</param>
        /// <param name="tractorHeading">Tractor heading (degrees, 0-360)</param>
        /// <returns>Initial positions of both trailers (location points, accounting for offsets from rear axles)</returns>
        public TrailerPositions GetInitialLocations(
            double tractorLat,
            double tractorLon,
            double tractorHeading)
        {
            // Tractor location is already the rear axle position
            // Calculate first trailer hitch position (behind tractor rear axle)
            double trailer1HitchLat = tractorLat;
            double trailer1HitchLon = tractorLon;
            Haversine.MoveDistanceBearing(ref trailer1HitchLat, ref trailer1HitchLon,
                tractorHeading + 180.0, _tractorToTrailer1Distance);

            // Calculate first trailer rear axle position (behind hitch)
            double trailer1RearLat = trailer1HitchLat;
            double trailer1RearLon = trailer1HitchLon;
            Haversine.MoveDistanceBearing(ref trailer1RearLat, ref trailer1RearLon,
                tractorHeading + 180.0, _trailer1Length);

            // Calculate first trailer location point (offset forward from rear axle)
            double trailer1LocationLat = trailer1RearLat;
            double trailer1LocationLon = trailer1RearLon;
            Haversine.MoveDistanceBearing(ref trailer1LocationLat, ref trailer1LocationLon,
                tractorHeading, _trailer1LocationOffset);

            // Calculate second trailer hitch position (behind first trailer rear axle)
            double trailer2HitchLat = trailer1RearLat;
            double trailer2HitchLon = trailer1RearLon;
            Haversine.MoveDistanceBearing(ref trailer2HitchLat, ref trailer2HitchLon,
                tractorHeading + 180.0, _trailer1ToTrailer2Distance);

            // Calculate second trailer rear axle position (behind hitch)
            double trailer2RearLat = trailer2HitchLat;
            double trailer2RearLon = trailer2HitchLon;
            Haversine.MoveDistanceBearing(ref trailer2RearLat, ref trailer2RearLon,
                tractorHeading + 180.0, _trailer2Length);

            // Calculate second trailer location point (offset forward from rear axle)
            double trailer2LocationLat = trailer2RearLat;
            double trailer2LocationLon = trailer2RearLon;
            Haversine.MoveDistanceBearing(ref trailer2LocationLat, ref trailer2LocationLon,
                tractorHeading, _trailer2LocationOffset);

            return new TrailerPositions
            {
                Trailer1Latitude = trailer1LocationLat,
                Trailer1Longitude = trailer1LocationLon,
                Trailer2Latitude = trailer2LocationLat,
                Trailer2Longitude = trailer2LocationLon
            };
        }

        /// <summary>
        /// Calculates the new positions of two trailers after the tractor moves.
        /// </summary>
        /// <param name="tractorStartLat">Tractor rear axle starting latitude (degrees)</param>
        /// <param name="tractorStartLon">Tractor rear axle starting longitude (degrees)</param>
        /// <param name="tractorStartHeading">Tractor starting heading (degrees, 0-360)</param>
        /// <param name="tractorNewLat">Tractor rear axle new latitude after 1 second (degrees)</param>
        /// <param name="tractorNewLon">Tractor rear axle new longitude after 1 second (degrees)</param>
        /// <param name="tractorNewHeading">Tractor new heading after 1 second (degrees, 0-360)</param>
        /// <param name="tractorSpeed">Tractor speed (meters per second)</param>
        /// <param name="trailer1StartLat">First trailer starting latitude (degrees)</param>
        /// <param name="trailer1StartLon">First trailer starting longitude (degrees)</param>
        /// <param name="trailer2StartLat">Second trailer starting latitude (degrees)</param>
        /// <param name="trailer2StartLon">Second trailer starting longitude (degrees)</param>
        /// <returns>New positions of both trailers</returns>
        public TrailerPositions CalculateTrailerPositions(
            double tractorStartLat, double tractorStartLon, double tractorStartHeading,
            double tractorNewLat, double tractorNewLon, double tractorNewHeading,
            double tractorSpeed,
            double trailer1StartLat, double trailer1StartLon,
            double trailer2StartLat, double trailer2StartLon)
        {
            // Tractor location is already the rear axle position
            // Calculate distance traveled by tractor rear axle
            double distanceTraveled = Haversine.Distance(
                tractorStartLat, tractorStartLon,
                tractorNewLat, tractorNewLon);

            // Calculate first trailer position using tractrix
            var trailer1Result = CalculateTractrixPosition(
                tractorStartLat, tractorStartLon, tractorNewLat, tractorNewLon,
                trailer1StartLat, trailer1StartLon,
                _trailer1Length,
                distanceTraveled);

            // Calculate second trailer position using tractrix
            // The second trailer's hitch follows the first trailer's rear axle path
            var trailer2Result = CalculateTractrixPosition(
                trailer1Result.HitchStartLat, trailer1Result.HitchStartLon,
                trailer1Result.HitchNewLat, trailer1Result.HitchNewLon,
                trailer2StartLat, trailer2StartLon,
                _trailer2Length,
                distanceTraveled);

            // Calculate trailer headings (from rear axle to hitch - forward direction) to apply location offsets
            double trailer1Heading = Haversine.Bearing(trailer1Result.RearAxleLat, trailer1Result.RearAxleLon,
                trailer1Result.HitchNewLat, trailer1Result.HitchNewLon) * Haversine.toDegrees;
            if (trailer1Heading < 0) trailer1Heading += 360.0;

            double trailer2Heading = Haversine.Bearing(trailer2Result.RearAxleLat, trailer2Result.RearAxleLon,
                trailer2Result.HitchNewLat, trailer2Result.HitchNewLon) * Haversine.toDegrees;
            if (trailer2Heading < 0) trailer2Heading += 360.0;

            // Calculate trailer location points (offset forward from rear axle)
            double trailer1LocationLat = trailer1Result.RearAxleLat;
            double trailer1LocationLon = trailer1Result.RearAxleLon;
            Haversine.MoveDistanceBearing(ref trailer1LocationLat, ref trailer1LocationLon,
                trailer1Heading, _trailer1LocationOffset);

            double trailer2LocationLat = trailer2Result.RearAxleLat;
            double trailer2LocationLon = trailer2Result.RearAxleLon;
            Haversine.MoveDistanceBearing(ref trailer2LocationLat, ref trailer2LocationLon,
                trailer2Heading, _trailer2LocationOffset);

            return new TrailerPositions
            {
                Trailer1Latitude = trailer1LocationLat,
                Trailer1Longitude = trailer1LocationLon,
                Trailer2Latitude = trailer2LocationLat,
                Trailer2Longitude = trailer2LocationLon
            };
        }

        /// <summary>
        /// Internal structure for tractrix calculation results
        /// </summary>
        private class TractrixResult
        {
            public double HitchStartLat;
            public double HitchStartLon;
            public double HitchNewLat;
            public double HitchNewLon;
            public double RearAxleLat;
            public double RearAxleLon;
        }

        /// <summary>
        /// Calculates the new position of a trailer's rear axle using tractrix mathematics.
        /// The hitch point follows the steering curve (previous unit's rear axle path),
        /// and the rear axle follows a tractrix of the hitch point path.
        /// 
        /// Uses the differential equation: dα = ds[L/R - sin(α)]/L
        /// where:
        ///   α = tractrix angle (angle between vehicle heading and path heading)
        ///   ds = incremental distance along steering curve
        ///   L = vehicle unit length
        ///   R = instantaneous radius of steering curve
        /// </summary>
        private TractrixResult CalculateTractrixPosition(
            double steeringStartLat, double steeringStartLon,
            double steeringNewLat, double steeringNewLon,
            double trailerRearStartLat, double trailerRearStartLon,
            double trailerLength,
            double totalDistance)
        {
            // The hitch point follows the steering curve (path of previous unit's rear axle)
            // According to the paper, the hitch follows this path directly.
            // The rear axle follows a tractrix of the hitch path.
            
            // Calculate initial hitch position
            // The hitch is at distance L from the rear axle, in the direction from rear axle to steering point
            double hitchStartLat = trailerRearStartLat;
            double hitchStartLon = trailerRearStartLon;
            double bearingToSteering = Haversine.Bearing(trailerRearStartLat, trailerRearStartLon,
                steeringStartLat, steeringStartLon) * Haversine.toDegrees;
            if (bearingToSteering < 0) bearingToSteering += 360.0;
            Haversine.MoveDistanceBearing(ref hitchStartLat, ref hitchStartLon,
                bearingToSteering, trailerLength);
            
            // Verify hitch is at correct distance from steering point (should be approximately L)
            // If not, adjust to maintain geometric consistency
            double hitchToSteeringDist = Haversine.Distance(hitchStartLat, hitchStartLon,
                steeringStartLat, steeringStartLon);
            if (Math.Abs(hitchToSteeringDist - trailerLength) > 0.1)
            {
                // Recalculate: hitch should be at distance L from steering point
                // in direction from steering to rear axle
                double bearingFromSteering = Haversine.Bearing(steeringStartLat, steeringStartLon,
                    trailerRearStartLat, trailerRearStartLon) * Haversine.toDegrees;
                if (bearingFromSteering < 0) bearingFromSteering += 360.0;
                hitchStartLat = steeringStartLat;
                hitchStartLon = steeringStartLon;
                Haversine.MoveDistanceBearing(ref hitchStartLat, ref hitchStartLon,
                    bearingFromSteering, trailerLength);
            }

            // The hitch follows the steering curve, so the hitch path is the steering curve
            // Integrate along this path to calculate the tractrix
            double currentHitchLat = hitchStartLat;
            double currentHitchLon = hitchStartLon;
            double currentRearLat = trailerRearStartLat;
            double currentRearLon = trailerRearStartLon;
            
            // Calculate initial trailer heading (from rear axle to hitch)
            double initialTrailerHeading = Haversine.Bearing(currentRearLat, currentRearLon,
                currentHitchLat, currentHitchLon) * Haversine.toRadians;
            
            // Calculate initial path heading (tangent to steering curve/hitch path at start)
            double initialPathHeading = Haversine.Bearing(steeringStartLat, steeringStartLon,
                steeringNewLat, steeringNewLon) * Haversine.toRadians;
            
            // Initial tractrix angle α (angle between trailer heading and path heading)
            double currentAlpha = NormalizeAngle((initialTrailerHeading - initialPathHeading) * Haversine.toDegrees) * Haversine.toRadians;
            
            // Initial vehicle heading β (trailer heading)
            double currentBeta = initialTrailerHeading;
            
            // Total distance along steering curve (hitch path)
            double totalHitchDistance = Haversine.Distance(steeringStartLat, steeringStartLon,
                steeringNewLat, steeringNewLon);
            
            double remainingHitchDistance = totalHitchDistance;
            
            // Store previous positions for radius calculation
            double prevHitchLat = hitchStartLat;
            double prevHitchLon = hitchStartLon;
            double prevSteeringLat = steeringStartLat;
            double prevSteeringLon = steeringStartLon;
            
            // Current position along steering curve
            double currentSteeringLat = steeringStartLat;
            double currentSteeringLon = steeringStartLon;
            
            while (remainingHitchDistance > 0.001)
            {
                double ds = Math.Min(MAX_STEP_SIZE, remainingHitchDistance);
                
                // Move along steering curve (hitch path)
                // The steering point moves along its path
                double steeringBearing = Haversine.Bearing(currentSteeringLat, currentSteeringLon,
                    steeringNewLat, steeringNewLon) * Haversine.toRadians;
                
                prevSteeringLat = currentSteeringLat;
                prevSteeringLon = currentSteeringLon;
                double steeringBearingDeg = steeringBearing * Haversine.toDegrees;
                if (steeringBearingDeg < 0) steeringBearingDeg += 360.0;
                Haversine.MoveDistanceBearing(ref currentSteeringLat, ref currentSteeringLon,
                    steeringBearingDeg, ds);
                
                // The hitch follows the steering curve path (same path, moves by same distance ds)
                // The hitch path is the steering curve, so move hitch along the same path
                prevHitchLat = currentHitchLat;
                prevHitchLon = currentHitchLon;
                double hitchBearing = Haversine.Bearing(currentHitchLat, currentHitchLon,
                    steeringNewLat, steeringNewLon) * Haversine.toDegrees;
                if (hitchBearing < 0) hitchBearing += 360.0;
                Haversine.MoveDistanceBearing(ref currentHitchLat, ref currentHitchLon,
                    hitchBearing, ds);
                
                // Calculate instantaneous radius of curvature R of the steering curve
                double nextSteeringLat = currentSteeringLat;
                double nextSteeringLon = currentSteeringLon;
                double nextBearing = Haversine.Bearing(currentSteeringLat, currentSteeringLon,
                    steeringNewLat, steeringNewLon) * Haversine.toDegrees;
                if (nextBearing < 0) nextBearing += 360.0;
                double nextDs = Math.Min(MAX_STEP_SIZE, remainingHitchDistance - ds);
                if (nextDs > 0.001)
                {
                    Haversine.MoveDistanceBearing(ref nextSteeringLat, ref nextSteeringLon,
                        nextBearing, nextDs);
                }
                else
                {
                    nextSteeringLat = steeringNewLat;
                    nextSteeringLon = steeringNewLon;
                }
                
                double R = CalculateRadiusOfCurvature(
                    prevSteeringLat, prevSteeringLon,
                    currentSteeringLat, currentSteeringLon,
                    nextSteeringLat, nextSteeringLon);
                
                // Update tractrix angle using: dα = ds[L/R - sin(α)]/L
                double dAlpha = ds * ((trailerLength / R) - Math.Sin(currentAlpha)) / trailerLength;
                currentAlpha += dAlpha;
                
                // Update path heading (tangent to steering curve at current position)
                double currentPathHeading = Haversine.Bearing(currentSteeringLat, currentSteeringLon,
                    steeringNewLat, steeringNewLon) * Haversine.toRadians;
                
                // Update vehicle heading: dβ = dθ - dα
                // where dθ is the change in path heading
                double prevPathHeading = steeringBearing;
                double dTheta = NormalizeAngle((currentPathHeading - prevPathHeading) * Haversine.toDegrees) * Haversine.toRadians;
                double dBeta = dTheta - dAlpha;
                currentBeta += dBeta;
                
                // Normalize currentBeta to 0-2π range
                while (currentBeta < 0) currentBeta += 2 * Math.PI;
                while (currentBeta >= 2 * Math.PI) currentBeta -= 2 * Math.PI;
                
                // Update rear axle position: it's at distance L from hitch, in direction opposite to trailer heading
                double rearBearing = (currentBeta * Haversine.toDegrees + 180.0) % 360.0;
                currentRearLat = currentHitchLat;
                currentRearLon = currentHitchLon;
                Haversine.MoveDistanceBearing(ref currentRearLat, ref currentRearLon,
                    rearBearing, trailerLength);
                
                remainingHitchDistance -= ds;
            }
            
            // Final hitch position: the hitch follows the steering curve path
            // So the final hitch is at the end of the steering curve path
            // (which is the same as steeringNewLat/Lon since the hitch path = steering curve)
            double hitchNewLat = currentHitchLat;
            double hitchNewLon = currentHitchLon;

            return new TractrixResult
            {
                HitchStartLat = hitchStartLat,
                HitchStartLon = hitchStartLon,
                HitchNewLat = hitchNewLat,
                HitchNewLon = hitchNewLon,
                RearAxleLat = currentRearLat,
                RearAxleLon = currentRearLon
            };
        }

        /// <summary>
        /// Calculates the radius of curvature for a path using three points.
        /// Uses the previous point, current point, and next point to estimate curvature.
        /// </summary>
        private double CalculateRadiusOfCurvature(
            double prevLat, double prevLon,
            double currentLat, double currentLon,
            double nextLat, double nextLon)
        {
            // Calculate distances between points
            double d1 = Haversine.Distance(prevLat, prevLon, currentLat, currentLon);
            double d2 = Haversine.Distance(currentLat, currentLon, nextLat, nextLon);
            
            // If points are too close or path is nearly straight, return large radius
            if (d1 < 0.01 || d2 < 0.01)
                return 1000000.0;
            
            // Calculate bearings
            double bearing1 = Haversine.Bearing(prevLat, prevLon, currentLat, currentLon);
            double bearing2 = Haversine.Bearing(currentLat, currentLon, nextLat, nextLon);
            
            // Calculate change in bearing (curvature)
            double deltaBearing = NormalizeAngle((bearing2 - bearing1) * Haversine.toDegrees) * Haversine.toRadians;
            
            // If bearing change is very small, path is nearly straight
            if (Math.Abs(deltaBearing) < 0.001)
                return 1000000.0;
            
            // For a circular arc, the radius is: R = ds / dθ
            // Use average distance as ds
            double avgDistance = (d1 + d2) / 2.0;
            double radius = avgDistance / Math.Abs(deltaBearing);
            
            // Ensure minimum reasonable radius
            return Math.Max(radius, 1.0);
        }

        /// <summary>
        /// Normalizes an angle to the range -180 to +180 degrees
        /// </summary>
        private double NormalizeAngle(double angleDegrees)
        {
            while (angleDegrees > 180.0) angleDegrees -= 360.0;
            while (angleDegrees < -180.0) angleDegrees += 360.0;
            return angleDegrees;
        }
    }
}

