using System;
using System.Collections.Generic;
using UnityEngine;
using IFRTrainer.Core;

namespace IFRTrainer.Mission
{
    /// <summary>
    /// Manages mission loading, objective tracking, and completion.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        [Header("Mission Settings")]
        [SerializeField] private string missionsResourcePath = "Missions";
        [SerializeField] private bool autoStartMission = false;

        public event Action<MissionData> OnMissionLoaded;
        public event Action<MissionData> OnMissionStarted;
        public event Action<MissionData, MissionResult> OnMissionCompleted;
        public event Action<ObjectiveData> OnObjectiveCompleted;
        public event Action<string> OnObjectiveUpdated;

        public MissionData CurrentMission { get; private set; }
        public MissionState State { get; private set; } = MissionState.NotLoaded;
        public float MissionTime { get; private set; }
        public List<ObjectiveStatus> ObjectiveStatuses { get; private set; } = new List<ObjectiveStatus>();

        private ScoreManager scoreManager;
        private int currentObjectiveIndex;

        private void Awake()
        {
            scoreManager = GetComponent<ScoreManager>();
            if (scoreManager == null)
            {
                scoreManager = gameObject.AddComponent<ScoreManager>();
            }
        }

        private void Update()
        {
            if (State != MissionState.InProgress)
                return;

            MissionTime += Time.deltaTime;
            CheckObjectives();
        }

        public bool LoadMission(string missionId)
        {
            string path = $"{missionsResourcePath}/{missionId}";
            TextAsset jsonAsset = Resources.Load<TextAsset>(path);

            if (jsonAsset == null)
            {
                Debug.LogError($"Mission not found: {path}");
                return false;
            }

            return LoadMissionFromJson(jsonAsset.text);
        }

        public bool LoadMissionFromJson(string json)
        {
            try
            {
                CurrentMission = JsonUtility.FromJson<MissionData>(json);

                if (CurrentMission == null || string.IsNullOrEmpty(CurrentMission.mission_id))
                {
                    Debug.LogError("Invalid mission data");
                    return false;
                }

                State = MissionState.Loaded;
                InitializeObjectiveStatuses();

                OnMissionLoaded?.Invoke(CurrentMission);
                Debug.Log($"Mission loaded: {CurrentMission.title}");

                if (autoStartMission)
                {
                    StartMission();
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse mission JSON: {e.Message}");
                return false;
            }
        }

        public void StartMission()
        {
            if (CurrentMission == null || State == MissionState.InProgress)
                return;

            // Initialize simulation with mission starting conditions
            var ic = CurrentMission.initial_conditions.aircraft;
            GameManager.Instance.InitializeSimulation(
                ic.latitude, ic.longitude, ic.altitude_feet,
                ic.heading, ic.speed_knots
            );

            // Add VOR stations
            GameManager.Instance.ClearVORs();
            foreach (var vor in CurrentMission.vor_stations)
            {
                GameManager.Instance.AddVOR(vor.identifier, vor.latitude, vor.longitude, vor.frequency_mhz);
            }

            // Tune to first VOR if available
            if (CurrentMission.vor_stations.Length > 0)
            {
                GameManager.Instance.TuneNAV(CurrentMission.vor_stations[0].frequency_mhz);
            }

            // Reset mission state
            MissionTime = 0;
            currentObjectiveIndex = 0;
            InitializeObjectiveStatuses();

            // Start scoring
            scoreManager?.StartMission(CurrentMission);

            State = MissionState.InProgress;
            OnMissionStarted?.Invoke(CurrentMission);

            Debug.Log($"Mission started: {CurrentMission.title}");
        }

        public void CompleteMission(bool success)
        {
            if (State != MissionState.InProgress)
                return;

            State = success ? MissionState.Completed : MissionState.Failed;

            // Get final score
            MissionResult result = scoreManager?.CalculateFinalScore(MissionTime) ?? new MissionResult();
            result.Success = success;
            result.TotalTime = MissionTime;

            OnMissionCompleted?.Invoke(CurrentMission, result);
            Debug.Log($"Mission {(success ? "completed" : "failed")}: {result.TotalScore} points ({result.Grade})");
        }

        public void AbortMission()
        {
            if (State == MissionState.InProgress)
            {
                CompleteMission(false);
            }
        }

        private void InitializeObjectiveStatuses()
        {
            ObjectiveStatuses.Clear();

            if (CurrentMission?.objectives == null)
                return;

            foreach (var obj in CurrentMission.objectives)
            {
                ObjectiveStatuses.Add(new ObjectiveStatus
                {
                    Objective = obj,
                    State = ObjectiveState.Pending,
                    Progress = 0f
                });
            }
        }

        private void CheckObjectives()
        {
            if (currentObjectiveIndex >= ObjectiveStatuses.Count)
            {
                // All objectives complete
                CompleteMission(true);
                return;
            }

            var status = ObjectiveStatuses[currentObjectiveIndex];
            if (status.State == ObjectiveState.Completed)
            {
                currentObjectiveIndex++;
                return;
            }

            // Mark as active if pending
            if (status.State == ObjectiveState.Pending)
            {
                status.State = ObjectiveState.Active;
                OnObjectiveUpdated?.Invoke(GetObjectiveDescription(status.Objective));
            }

            // Check objective based on type
            bool completed = CheckObjective(status);

            if (completed)
            {
                status.State = ObjectiveState.Completed;
                OnObjectiveCompleted?.Invoke(status.Objective);
                currentObjectiveIndex++;

                if (currentObjectiveIndex < ObjectiveStatuses.Count)
                {
                    var nextStatus = ObjectiveStatuses[currentObjectiveIndex];
                    OnObjectiveUpdated?.Invoke(GetObjectiveDescription(nextStatus.Objective));
                }
            }
        }

        private bool CheckObjective(ObjectiveStatus status)
        {
            var obj = status.Objective;
            var indication = GameManager.Instance.CurrentVORIndication;

            switch (obj.type)
            {
                case "intercept_radial":
                    return CheckInterceptRadial(obj, indication, status);

                case "track_radial":
                    return CheckTrackRadial(obj, indication, status);

                case "reach_distance":
                    return CheckReachDistance(obj, indication, status);

                default:
                    Debug.LogWarning($"Unknown objective type: {obj.type}");
                    return false;
            }
        }

        private bool CheckInterceptRadial(ObjectiveData obj, VORIndication indication, ObjectiveStatus status)
        {
            if (!indication.IsValid)
                return false;

            // Calculate current radial
            float currentRadial = (indication.BearingToStation + 180f) % 360f;
            float deviation = Mathf.Abs(Mathf.DeltaAngle(currentRadial, obj.radial));

            status.Progress = 1f - Mathf.Clamp01(deviation / 10f);

            // Check if within tolerance
            if (deviation <= obj.max_deviation_deg)
            {
                // Start tracking for track_radial objectives
                SimulationBridge.StartTracking();
                return true;
            }

            return false;
        }

        private bool CheckTrackRadial(ObjectiveData obj, VORIndication indication, ObjectiveStatus status)
        {
            if (!indication.IsValid)
                return false;

            var trackingStats = SimulationBridge.GetTrackingStats();

            status.Progress = Mathf.Clamp01(trackingStats.TotalTime / obj.duration_seconds);

            // Check if tracked long enough with acceptable deviation
            if (trackingStats.TotalTime >= obj.duration_seconds)
            {
                if (trackingStats.AverageDeviation <= obj.max_avg_deviation_deg)
                {
                    SimulationBridge.StopTracking();
                    return true;
                }
            }

            return false;
        }

        private bool CheckReachDistance(ObjectiveData obj, VORIndication indication, ObjectiveStatus status)
        {
            if (!indication.IsValid)
                return false;

            float targetDist = obj.distance_nm;
            float currentDist = indication.DistanceNM;

            status.Progress = Mathf.Clamp01(currentDist / targetDist);

            return currentDist >= targetDist;
        }

        private string GetObjectiveDescription(ObjectiveData obj)
        {
            switch (obj.type)
            {
                case "intercept_radial":
                    return $"Intercept {obj.station} {obj.radial:000}° radial";

                case "track_radial":
                    return $"Track radial for {obj.duration_seconds}s (max avg dev: {obj.max_avg_deviation_deg}°)";

                case "reach_distance":
                    return $"Fly to {obj.distance_nm}NM from station";

                default:
                    return obj.type;
            }
        }

        public string GetCurrentObjectiveText()
        {
            if (currentObjectiveIndex >= ObjectiveStatuses.Count)
                return "All objectives complete!";

            return GetObjectiveDescription(ObjectiveStatuses[currentObjectiveIndex].Objective);
        }
    }

    // ========================================================================
    // Data structures
    // ========================================================================

    public enum MissionState
    {
        NotLoaded,
        Loaded,
        InProgress,
        Completed,
        Failed
    }

    public enum ObjectiveState
    {
        Pending,
        Active,
        Completed,
        Failed
    }

    [Serializable]
    public class ObjectiveStatus
    {
        public ObjectiveData Objective;
        public ObjectiveState State;
        public float Progress;
    }

    [Serializable]
    public class MissionResult
    {
        public bool Success;
        public int TotalScore;
        public int BaseScore;
        public int TimeBonus;
        public int InterceptBonus;
        public int TrackingBonus;
        public int DeviationPenalty;
        public float TotalTime;
        public float AverageDeviation;
        public string Grade;
    }

    // JSON data structures
    [Serializable]
    public class MissionData
    {
        public string mission_id;
        public string title;
        public int difficulty;
        public InitialConditions initial_conditions;
        public VORStationData[] vor_stations;
        public ObjectiveData[] objectives;
        public ScoringData scoring;
    }

    [Serializable]
    public class InitialConditions
    {
        public AircraftInitialState aircraft;
    }

    [Serializable]
    public class AircraftInitialState
    {
        public double latitude;
        public double longitude;
        public float altitude_feet;
        public float heading;
        public float speed_knots;
    }

    [Serializable]
    public class VORStationData
    {
        public string identifier;
        public double latitude;
        public double longitude;
        public float frequency_mhz;
    }

    [Serializable]
    public class ObjectiveData
    {
        public string type;
        public string station;
        public float radial;
        public float max_deviation_deg;
        public float duration_seconds;
        public float max_avg_deviation_deg;
        public float distance_nm;
    }

    [Serializable]
    public class ScoringData
    {
        public float time_bonus_threshold_seconds;
        public float deviation_penalty_per_degree;
    }
}
