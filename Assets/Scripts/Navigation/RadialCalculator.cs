using UnityEngine;
using IFRTrainer.Core;

namespace IFRTrainer.Navigation
{
    /// <summary>
    /// Utility class for VOR radial calculations.
    /// Provides static methods for navigation math.
    /// </summary>
    public static class RadialCalculator
    {
        private const float DEG_TO_RAD = Mathf.PI / 180f;
        private const float RAD_TO_DEG = 180f / Mathf.PI;

        /// <summary>
        /// Calculate the intercept heading to join a VOR radial.
        /// </summary>
        /// <param name="currentRadial">Current radial the aircraft is on (FROM station)</param>
        /// <param name="targetRadial">Desired radial to intercept</param>
        /// <param name="inbound">True for inbound intercept (TO station), false for outbound</param>
        /// <param name="interceptAngle">Desired intercept angle (typically 30-90 degrees)</param>
        /// <returns>Heading to fly for intercept</returns>
        public static float CalculateInterceptHeading(float currentRadial, float targetRadial,
                                                       bool inbound, float interceptAngle = 45f)
        {
            float diff = NormalizeAngleDiff(targetRadial - currentRadial);

            float heading;
            if (inbound)
            {
                // Inbound: fly toward the station on the target radial
                float inboundCourse = NormalizeAngle(targetRadial + 180f);

                if (diff > 0)
                {
                    // Radial is to the right, intercept from right
                    heading = NormalizeAngle(inboundCourse - interceptAngle);
                }
                else
                {
                    // Radial is to the left, intercept from left
                    heading = NormalizeAngle(inboundCourse + interceptAngle);
                }
            }
            else
            {
                // Outbound: fly away from station on the target radial
                if (diff > 0)
                {
                    // Radial is to the right, intercept from right
                    heading = NormalizeAngle(targetRadial + interceptAngle);
                }
                else
                {
                    // Radial is to the left, intercept from left
                    heading = NormalizeAngle(targetRadial - interceptAngle);
                }
            }

            return heading;
        }

        /// <summary>
        /// Calculate the heading to track a radial (inbound or outbound).
        /// </summary>
        public static float CalculateTrackingHeading(float radial, bool inbound)
        {
            if (inbound)
            {
                return NormalizeAngle(radial + 180f);
            }
            return radial;
        }

        /// <summary>
        /// Calculate wind correction angle for tracking.
        /// </summary>
        /// <param name="trackCourse">Desired ground track</param>
        /// <param name="windDirection">Wind FROM direction</param>
        /// <param name="windSpeed">Wind speed in knots</param>
        /// <param name="trueAirspeed">Aircraft TAS in knots</param>
        /// <returns>Heading to fly (with wind correction)</returns>
        public static float CalculateWindCorrectionAngle(float trackCourse, float windDirection,
                                                          float windSpeed, float trueAirspeed)
        {
            if (windSpeed < 1f || trueAirspeed < 1f)
                return trackCourse;

            // Wind correction angle formula
            float relativeWind = NormalizeAngleDiff(windDirection - trackCourse);
            float wca = Mathf.Asin((windSpeed / trueAirspeed) * Mathf.Sin(relativeWind * DEG_TO_RAD)) * RAD_TO_DEG;

            return NormalizeAngle(trackCourse + wca);
        }

        /// <summary>
        /// Check if aircraft is established on a radial within tolerance.
        /// </summary>
        public static bool IsOnRadial(float currentRadial, float targetRadial, float toleranceDegrees = 2f)
        {
            float diff = Mathf.Abs(NormalizeAngleDiff(currentRadial - targetRadial));
            return diff <= toleranceDegrees;
        }

        /// <summary>
        /// Calculate time to intercept a radial at current closure rate.
        /// </summary>
        public static float EstimateTimeToIntercept(float currentRadial, float targetRadial,
                                                     float closureRateDegPerSec)
        {
            if (Mathf.Abs(closureRateDegPerSec) < 0.01f)
                return float.MaxValue;

            float diff = Mathf.Abs(NormalizeAngleDiff(targetRadial - currentRadial));
            return diff / Mathf.Abs(closureRateDegPerSec);
        }

        /// <summary>
        /// Suggest heading corrections based on CDI deflection.
        /// Returns recommended heading change in degrees.
        /// </summary>
        public static float SuggestHeadingCorrection(float cdiDeflection, bool inbound)
        {
            // Rule of thumb: 1 dot = 2 degrees off, need about 20 degree correction
            // CDI deflection is -1 to +1 (full scale = 5 dots)

            float dotsOff = cdiDeflection * 5f; // Convert to dots
            float correction = dotsOff * 10f;   // 10 degrees per dot

            // Clamp to reasonable correction
            correction = Mathf.Clamp(correction, -30f, 30f);

            return correction;
        }

        /// <summary>
        /// Calculate bearing from one point to another.
        /// </summary>
        public static float CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            return SimulationBridge.CalculateBearing(lat1, lon1, lat2, lon2);
        }

        /// <summary>
        /// Calculate distance between two points in nautical miles.
        /// </summary>
        public static float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            return SimulationBridge.CalculateDistance(lat1, lon1, lat2, lon2);
        }

        /// <summary>
        /// Normalize angle to 0-360 range.
        /// </summary>
        public static float NormalizeAngle(float angle)
        {
            while (angle < 0) angle += 360f;
            while (angle >= 360f) angle -= 360f;
            return angle;
        }

        /// <summary>
        /// Normalize angle difference to -180 to +180 range.
        /// </summary>
        public static float NormalizeAngleDiff(float diff)
        {
            while (diff > 180f) diff -= 360f;
            while (diff < -180f) diff += 360f;
            return diff;
        }
    }
}
