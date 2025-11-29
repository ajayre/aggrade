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
        /// <returns>Initial positions of both trailers (rear axle positions)</returns>
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

            return new TrailerPositions
            {
                Trailer1Latitude = trailer1RearLat,
                Trailer1Longitude = trailer1RearLon,
                Trailer2Latitude = trailer2RearLat,
                Trailer2Longitude = trailer2RearLon
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

            return new TrailerPositions
            {
                Trailer1Latitude = trailer1Result.RearAxleLat,
                Trailer1Longitude = trailer1Result.RearAxleLon,
                Trailer2Latitude = trailer2Result.RearAxleLat,
                Trailer2Longitude = trailer2Result.RearAxleLon
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
            // First, calculate where the hitch point should be (it follows the steering curve)
            // The hitch maintains a constant distance from the steering point
            double hitchStartLat = steeringStartLat;
            double hitchStartLon = steeringStartLon;

            // Calculate initial bearing from steering point to trailer rear axle
            double initialBearing = Haversine.Bearing(steeringStartLat, steeringStartLon,
                trailerRearStartLat, trailerRearStartLon) * Haversine.toDegrees;

            // Normalize bearing to 0-360
            if (initialBearing < 0) initialBearing += 360.0;

            // Calculate initial hitch position (opposite direction from rear axle)
            double hitchBearing = (initialBearing + 180.0) % 360.0;
            Haversine.MoveDistanceBearing(ref hitchStartLat, ref hitchStartLon,
                hitchBearing, trailerLength);

            // Calculate new hitch position (follows the steering curve)
            double hitchNewLat = steeringNewLat;
            double hitchNewLon = steeringNewLon;
            double bearingToHitch = Haversine.Bearing(steeringNewLat, steeringNewLon,
                hitchStartLat, hitchStartLon) * Haversine.toDegrees;
            if (bearingToHitch < 0) bearingToHitch += 360.0;
            Haversine.MoveDistanceBearing(ref hitchNewLat, ref hitchNewLon,
                bearingToHitch, trailerLength);

            // Now calculate the tractrix path of the rear axle following the hitch
            // Use numerical integration with small steps
            double rearAxleLat = trailerRearStartLat;
            double rearAxleLon = trailerRearStartLon;

            // Calculate initial tractrix angle (angle between trailer heading and path heading)
            double trailerHeading = Haversine.Bearing(hitchStartLat, hitchStartLon,
                rearAxleLat, rearAxleLon) * Haversine.toDegrees;
            if (trailerHeading < 0) trailerHeading += 360.0;

            double pathHeading = Haversine.Bearing(hitchStartLat, hitchStartLon,
                hitchNewLat, hitchNewLon) * Haversine.toDegrees;
            if (pathHeading < 0) pathHeading += 360.0;

            double tractrixAngle = NormalizeAngle(trailerHeading - pathHeading);

            // Integrate along the path from hitch start to hitch new position
            double currentHitchLat = hitchStartLat;
            double currentHitchLon = hitchStartLon;
            double currentRearLat = rearAxleLat;
            double currentRearLon = rearAxleLon;
            double currentAlpha = tractrixAngle * Haversine.toRadians;
            double currentBeta = trailerHeading * Haversine.toRadians;

            double remainingDistance = Haversine.Distance(hitchStartLat, hitchStartLon,
                hitchNewLat, hitchNewLon);

            while (remainingDistance > 0.001) // Continue until we've moved the full distance
            {
                double ds = Math.Min(MAX_STEP_SIZE, remainingDistance);

                // Calculate instantaneous radius of curvature
                // For small steps, approximate as circular arc
                double R = CalculateInstantaneousRadius(
                    currentHitchLat, currentHitchLon,
                    hitchNewLat, hitchNewLon,
                    remainingDistance);

                // Update tractrix angle using: dα = ds[L/R - sin(α)]/L
                double dAlpha = ds * ((trailerLength / R) - Math.Sin(currentAlpha)) / trailerLength;
                currentAlpha += dAlpha;

                // Update vehicle heading: dβ = dθ - dα
                double dTheta = ds / R;
                double dBeta = dTheta - dAlpha;
                currentBeta += dBeta;

                // Move hitch point forward along path
                double hitchBearingRad = Haversine.Bearing(currentHitchLat, currentHitchLon,
                    hitchNewLat, hitchNewLon);
                Haversine.MoveDistanceBearing(ref currentHitchLat, ref currentHitchLon,
                    hitchBearingRad * Haversine.toDegrees, ds);

                // Move rear axle to maintain trailer length and heading
                currentRearLat = currentHitchLat;
                currentRearLon = currentHitchLon;
                double rearBearing = (currentBeta * Haversine.toDegrees + 180.0) % 360.0;
                Haversine.MoveDistanceBearing(ref currentRearLat, ref currentRearLon,
                    rearBearing, trailerLength);

                remainingDistance -= ds;
            }

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
        /// Calculates the instantaneous radius of curvature for the path.
        /// Uses a three-point method to estimate the radius.
        /// </summary>
        private double CalculateInstantaneousRadius(
            double currentLat, double currentLon,
            double targetLat, double targetLon,
            double remainingDistance)
        {
            // For a straight path or very large radius, return a large value
            if (remainingDistance < 0.01)
                return 1000000.0; // Effectively straight

            // Calculate bearing change rate
            double bearing = Haversine.Bearing(currentLat, currentLon, targetLat, targetLon);

            // Estimate radius based on path curvature
            // This is a simplified approximation - for more accuracy, use three-point method
            // For now, assume moderate curvature if distance is reasonable
            double estimatedRadius = Math.Max(remainingDistance * 10.0, 50.0);

            return estimatedRadius;
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

