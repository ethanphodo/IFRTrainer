using UnityEngine;
using System.Collections.Generic;
using IFRTrainer.Core;

namespace IFRTrainer.Mission
{
    /// <summary>
    /// Records flight data during a mission for debrief replay and analysis.
    /// Tracks position, heading, CDI deviation, and key events.
    /// </summary>
    public class FlightRecorder : MonoBehaviour
    {
        public static FlightRecorder Instance { get; private set; }

        [Header("Recording Settings")]
        [SerializeField] private float recordInterval = 0.5f; // seconds between samples
        [SerializeField] private int maxSamples = 1000; // prevent memory issues

        public List<FlightSample> Samples { get; private set; } = new List<FlightSample>();
        public List<FlightEvent> Events { get; private set; } = new List<FlightEvent>();
        public bool IsRecording { get; private set; }

        private float timeSinceLastSample;
        private MissionManager missionManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            missionManager = FindFirstObjectByType<MissionManager>();

            if (missionManager != null)
            {
                missionManager.OnMissionStarted += OnMissionStarted;
                missionManager.OnMissionCompleted += OnMissionCompleted;
                missionManager.OnObjectiveCompleted += OnObjectiveCompleted;
            }
        }

        private void OnDestroy()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= OnMissionStarted;
                missionManager.OnMissionCompleted -= OnMissionCompleted;
                missionManager.OnObjectiveCompleted -= OnObjectiveCompleted;
            }
        }

        private void Update()
        {
            if (!IsRecording)
                return;

            if (GameManager.Instance == null || !GameManager.Instance.IsInitialized)
                return;

            timeSinceLastSample += Time.deltaTime;

            if (timeSinceLastSample >= recordInterval)
            {
                RecordSample();
                timeSinceLastSample = 0f;
            }
        }

        public void StartRecording()
        {
            Samples.Clear();
            Events.Clear();
            IsRecording = true;
            timeSinceLastSample = 0f;

            AddEvent(FlightEventType.MissionStart, "Mission Started");
            Debug.Log("FlightRecorder: Started recording");
        }

        public void StopRecording()
        {
            IsRecording = false;
            AddEvent(FlightEventType.MissionEnd, "Mission Ended");
            Debug.Log($"FlightRecorder: Stopped recording. {Samples.Count} samples, {Events.Count} events");
        }

        private void RecordSample()
        {
            if (Samples.Count >= maxSamples)
                return;

            var pos = GameManager.Instance.CurrentPosition;
            var dyn = GameManager.Instance.CurrentDynamics;
            var vor = GameManager.Instance.CurrentVORIndication;

            var sample = new FlightSample
            {
                Time = missionManager?.MissionTime ?? 0f,
                Latitude = pos.Latitude,
                Longitude = pos.Longitude,
                Altitude = pos.Altitude,
                Heading = dyn.Heading,
                Speed = dyn.Speed,
                BankAngle = dyn.BankAngle,
                CDIDeflection = vor.CDIDeflection,
                BearingToStation = vor.BearingToStation,
                DistanceNM = vor.DistanceNM,
                IsVORValid = vor.IsValid,
                ToFromFlag = vor.ToFromFlag
            };

            Samples.Add(sample);
        }

        private void AddEvent(FlightEventType type, string description, float? value = null)
        {
            Events.Add(new FlightEvent
            {
                Time = missionManager?.MissionTime ?? 0f,
                Type = type,
                Description = description,
                Value = value
            });
        }

        private void OnMissionStarted(MissionData mission)
        {
            StartRecording();
        }

        private void OnMissionCompleted(MissionData mission, MissionResult result)
        {
            AddEvent(FlightEventType.MissionEnd, $"Grade: {result.Grade}", result.TotalScore);
            StopRecording();
        }

        private void OnObjectiveCompleted(ObjectiveData objective)
        {
            AddEvent(FlightEventType.ObjectiveComplete, $"Completed: {objective.type}");
        }

        // Analysis methods
        public float GetAverageDeviation()
        {
            if (Samples.Count == 0) return 0f;

            float sum = 0f;
            int validCount = 0;

            foreach (var sample in Samples)
            {
                if (sample.IsVORValid)
                {
                    sum += Mathf.Abs(sample.CDIDeflection);
                    validCount++;
                }
            }

            return validCount > 0 ? sum / validCount : 0f;
        }

        public float GetMaxDeviation()
        {
            float max = 0f;
            foreach (var sample in Samples)
            {
                if (sample.IsVORValid)
                {
                    max = Mathf.Max(max, Mathf.Abs(sample.CDIDeflection));
                }
            }
            return max;
        }

        public float GetTimeOnRadial(float threshold = 0.2f)
        {
            float time = 0f;
            foreach (var sample in Samples)
            {
                if (sample.IsVORValid && Mathf.Abs(sample.CDIDeflection) <= threshold)
                {
                    time += recordInterval;
                }
            }
            return time;
        }

        public int GetRadialCrossings()
        {
            int crossings = 0;
            float lastCDI = 0f;
            bool first = true;

            foreach (var sample in Samples)
            {
                if (sample.IsVORValid)
                {
                    if (!first && Mathf.Sign(sample.CDIDeflection) != Mathf.Sign(lastCDI) && Mathf.Abs(lastCDI) > 0.1f)
                    {
                        crossings++;
                    }
                    lastCDI = sample.CDIDeflection;
                    first = false;
                }
            }
            return crossings;
        }

        // Get normalized positions for radar trace (0-1 range)
        public List<Vector2> GetNormalizedFlightPath()
        {
            if (Samples.Count == 0) return new List<Vector2>();

            // Find bounds
            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLon = double.MaxValue, maxLon = double.MinValue;

            foreach (var sample in Samples)
            {
                minLat = System.Math.Min(minLat, sample.Latitude);
                maxLat = System.Math.Max(maxLat, sample.Latitude);
                minLon = System.Math.Min(minLon, sample.Longitude);
                maxLon = System.Math.Max(maxLon, sample.Longitude);
            }

            // Add some padding
            double latRange = maxLat - minLat;
            double lonRange = maxLon - minLon;
            double padding = System.Math.Max(latRange, lonRange) * 0.1;
            if (padding < 0.001) padding = 0.01;

            minLat -= padding;
            maxLat += padding;
            minLon -= padding;
            maxLon += padding;

            // Normalize to 0-1
            List<Vector2> path = new List<Vector2>();
            foreach (var sample in Samples)
            {
                float x = (float)((sample.Longitude - minLon) / (maxLon - minLon));
                float y = (float)((sample.Latitude - minLat) / (maxLat - minLat));
                path.Add(new Vector2(x, y));
            }

            return path;
        }

        public List<float> GetDeviationOverTime()
        {
            List<float> deviations = new List<float>();
            foreach (var sample in Samples)
            {
                deviations.Add(sample.CDIDeflection);
            }
            return deviations;
        }
    }

    [System.Serializable]
    public class FlightSample
    {
        public float Time;
        public double Latitude;
        public double Longitude;
        public float Altitude;
        public float Heading;
        public float Speed;
        public float BankAngle;
        public float CDIDeflection;
        public float BearingToStation;
        public float DistanceNM;
        public bool IsVORValid;
        public ToFromFlag ToFromFlag;
    }

    public enum FlightEventType
    {
        MissionStart,
        MissionEnd,
        ObjectiveComplete,
        RadialCaptured,
        StationPassage,
        Warning
    }

    [System.Serializable]
    public class FlightEvent
    {
        public float Time;
        public FlightEventType Type;
        public string Description;
        public float? Value;
    }
}
