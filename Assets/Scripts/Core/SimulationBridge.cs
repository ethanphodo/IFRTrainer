using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IFRTrainer.Core
{
    /// <summary>
    /// P/Invoke bridge to the native C++ IFR simulation library.
    /// Handles all communication with libifr_sim.
    /// </summary>
    public static class SimulationBridge
    {
        private const string LIBRARY_NAME = "ifr_sim";

        #region Lifecycle

        [DllImport(LIBRARY_NAME)]
        private static extern int ifr_sim_init(double lat, double lon, double alt, double heading, double speed);

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_sim_update(float dt);

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_sim_shutdown();

        [DllImport(LIBRARY_NAME)]
        private static extern int ifr_sim_is_initialized();

        #endregion

        #region Aircraft Control

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_set_target_heading(float heading);

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_set_target_altitude(float altitude);

        #endregion

        #region Aircraft State

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_get_position(out double lat, out double lon, out float alt);

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_get_dynamics(out float hdg, out float spd, out float vspd, out float bank);

        [DllImport(LIBRARY_NAME)]
        private static extern double ifr_get_heading();

        [DllImport(LIBRARY_NAME)]
        private static extern double ifr_get_speed();

        [DllImport(LIBRARY_NAME)]
        private static extern double ifr_get_altitude();

        #endregion

        #region VOR Navigation

        [DllImport(LIBRARY_NAME)]
        private static extern int ifr_add_vor(string id, double lat, double lon, float freq);

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_clear_vors();

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_tune_nav(float frequency);

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_set_obs(float radial);

        [DllImport(LIBRARY_NAME)]
        private static extern float ifr_get_obs();

        [DllImport(LIBRARY_NAME)]
        private static extern float ifr_get_tuned_frequency();

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_get_vor_indication(out float cdi, out int toFrom, out float bearing, out float dist);

        [DllImport(LIBRARY_NAME)]
        private static extern int ifr_is_vor_valid();

        #endregion

        #region Scoring

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_start_tracking();

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_stop_tracking(out float avgDev, out float maxDev, out float timeOnRadial);

        [DllImport(LIBRARY_NAME)]
        private static extern void ifr_get_tracking_stats(out float avgDev, out float maxDev, out float timeOnRadial, out float totalTime);

        [DllImport(LIBRARY_NAME)]
        private static extern int ifr_is_tracking();

        #endregion

        #region Utility

        [DllImport(LIBRARY_NAME)]
        private static extern double ifr_calc_bearing(double lat1, double lon1, double lat2, double lon2);

        [DllImport(LIBRARY_NAME)]
        private static extern double ifr_calc_distance(double lat1, double lon1, double lat2, double lon2);

        #endregion

        // ====================================================================
        // Public API (managed wrappers)
        // ====================================================================

        public static bool IsInitialized => ifr_sim_is_initialized() != 0;

        public static bool Initialize(double latitude, double longitude, double altitudeFeet,
                                       double headingDegrees, double speedKnots)
        {
            try
            {
                int result = ifr_sim_init(latitude, longitude, altitudeFeet, headingDegrees, speedKnots);
                return result != 0;
            }
            catch (DllNotFoundException e)
            {
                Debug.LogError($"Native library not found: {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize simulation: {e.Message}");
                return false;
            }
        }

        public static void Update(float deltaTime)
        {
            if (IsInitialized)
            {
                ifr_sim_update(deltaTime);
            }
        }

        public static void Shutdown()
        {
            if (IsInitialized)
            {
                ifr_sim_shutdown();
            }
        }

        public static void SetTargetHeading(float heading)
        {
            if (IsInitialized)
            {
                ifr_set_target_heading(heading);
            }
        }

        public static void SetTargetAltitude(float altitude)
        {
            if (IsInitialized)
            {
                ifr_set_target_altitude(altitude);
            }
        }

        public static AircraftPosition GetPosition()
        {
            var pos = new AircraftPosition();
            if (IsInitialized)
            {
                ifr_get_position(out pos.Latitude, out pos.Longitude, out pos.Altitude);
            }
            return pos;
        }

        public static AircraftDynamics GetDynamics()
        {
            var dyn = new AircraftDynamics();
            if (IsInitialized)
            {
                ifr_get_dynamics(out dyn.Heading, out dyn.Speed, out dyn.VerticalSpeed, out dyn.BankAngle);
            }
            return dyn;
        }

        public static float GetHeading() => IsInitialized ? (float)ifr_get_heading() : 0f;
        public static float GetSpeed() => IsInitialized ? (float)ifr_get_speed() : 0f;
        public static float GetAltitude() => IsInitialized ? (float)ifr_get_altitude() : 0f;

        public static int AddVOR(string identifier, double latitude, double longitude, float frequencyMHz)
        {
            if (IsInitialized)
            {
                return ifr_add_vor(identifier, latitude, longitude, frequencyMHz);
            }
            return -1;
        }

        public static void ClearVORs()
        {
            if (IsInitialized)
            {
                ifr_clear_vors();
            }
        }

        public static void TuneNAV(float frequencyMHz)
        {
            if (IsInitialized)
            {
                ifr_tune_nav(frequencyMHz);
            }
        }

        public static void SetOBS(float radial)
        {
            if (IsInitialized)
            {
                ifr_set_obs(radial);
            }
        }

        public static float GetOBS() => IsInitialized ? ifr_get_obs() : 0f;
        public static float GetTunedFrequency() => IsInitialized ? ifr_get_tuned_frequency() : 0f;

        public static VORIndication GetVORIndication()
        {
            var ind = new VORIndication();
            if (IsInitialized)
            {
                ifr_get_vor_indication(out ind.CDIDeflection, out int toFrom, out ind.BearingToStation, out ind.DistanceNM);
                ind.ToFromFlag = (ToFromFlag)toFrom;
                ind.IsValid = ifr_is_vor_valid() != 0;
            }
            return ind;
        }

        public static void StartTracking()
        {
            if (IsInitialized)
            {
                ifr_start_tracking();
            }
        }

        public static TrackingResult StopTracking()
        {
            var result = new TrackingResult();
            if (IsInitialized)
            {
                ifr_stop_tracking(out result.AverageDeviation, out result.MaxDeviation, out result.TimeOnRadial);
            }
            return result;
        }

        public static TrackingStats GetTrackingStats()
        {
            var stats = new TrackingStats();
            if (IsInitialized)
            {
                ifr_get_tracking_stats(out stats.AverageDeviation, out stats.MaxDeviation,
                                       out stats.TimeOnRadial, out stats.TotalTime);
                stats.IsTracking = ifr_is_tracking() != 0;
            }
            return stats;
        }

        public static float CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            return (float)ifr_calc_bearing(lat1, lon1, lat2, lon2);
        }

        public static float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            return (float)ifr_calc_distance(lat1, lon1, lat2, lon2);
        }
    }

    // ========================================================================
    // Data structures
    // ========================================================================

    [Serializable]
    public struct AircraftPosition
    {
        public double Latitude;
        public double Longitude;
        public float Altitude;
    }

    [Serializable]
    public struct AircraftDynamics
    {
        public float Heading;
        public float Speed;
        public float VerticalSpeed;
        public float BankAngle;
    }

    public enum ToFromFlag
    {
        Off = 0,
        To = 1,
        From = 2
    }

    [Serializable]
    public struct VORIndication
    {
        public float CDIDeflection;      // -1.0 to +1.0
        public ToFromFlag ToFromFlag;
        public float BearingToStation;
        public float DistanceNM;
        public bool IsValid;
    }

    [Serializable]
    public struct TrackingResult
    {
        public float AverageDeviation;
        public float MaxDeviation;
        public float TimeOnRadial;
    }

    [Serializable]
    public struct TrackingStats
    {
        public float AverageDeviation;
        public float MaxDeviation;
        public float TimeOnRadial;
        public float TotalTime;
        public bool IsTracking;
    }
}
