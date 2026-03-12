using System;
using System.Collections.Generic;
using UnityEngine;
using IFRTrainer.Mission;
using IFRTrainer.UI;

namespace IFRTrainer.Core
{
    /// <summary>
    /// Monitors player performance and dynamically adjusts difficulty.
    /// Provides hints when struggling, injects challenges when coasting.
    /// </summary>
    public class AdaptiveDifficulty : MonoBehaviour
    {
        [Header("Performance Tracking")]
        [SerializeField] private float trackingWindowSeconds = 30f;
        [SerializeField] private float sampleInterval = 1f;

        [Header("Thresholds")]
        [SerializeField] private float strugglingThreshold = 0.6f; // CDI > 0.6 for too long
        [SerializeField] private float coastingThreshold = 0.2f;   // CDI < 0.2 for too long
        [SerializeField] private float hintCooldown = 20f;
        [SerializeField] private float challengeCooldown = 45f;

        [Header("References")]
        [SerializeField] private HintSystem hintSystem;

        public event Action<PerformanceState> OnPerformanceStateChanged;
        public event Action<EmergencyEvent> OnEmergencyTriggered;

        public PerformanceState CurrentState { get; private set; } = PerformanceState.Normal;
        public float AverageDeviation { get; private set; }
        public float PerformanceScore { get; private set; } = 0.5f; // 0-1, 0.5 = normal

        private Queue<float> deviationHistory = new Queue<float>();
        private float lastSampleTime;
        private float lastHintTime;
        private float lastChallengeTime;
        private MissionManager missionManager;
        private bool isActive;

        private void Awake()
        {
            missionManager = FindFirstObjectByType<MissionManager>();
            if (hintSystem == null)
                hintSystem = FindFirstObjectByType<HintSystem>();
        }

        private void OnEnable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted += OnMissionStarted;
                missionManager.OnMissionCompleted += OnMissionCompleted;
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= OnMissionStarted;
                missionManager.OnMissionCompleted -= OnMissionCompleted;
            }
        }

        private void OnMissionStarted(MissionData mission)
        {
            isActive = true;
            deviationHistory.Clear();
            PerformanceScore = 0.5f;
            CurrentState = PerformanceState.Normal;
        }

        private void OnMissionCompleted(MissionData mission, MissionResult result)
        {
            isActive = false;
        }

        private void Update()
        {
            if (!isActive || GameManager.Instance == null || !GameManager.Instance.IsInitialized)
                return;

            // Sample deviation periodically
            if (Time.time - lastSampleTime >= sampleInterval)
            {
                SamplePerformance();
                lastSampleTime = Time.time;
            }

            // Evaluate and respond
            EvaluatePerformance();
        }

        private void SamplePerformance()
        {
            var indication = GameManager.Instance.CurrentVORIndication;
            if (!indication.IsValid)
                return;

            float deviation = Mathf.Abs(indication.CDIDeflection);
            deviationHistory.Enqueue(deviation);

            // Keep only samples within the tracking window
            int maxSamples = Mathf.CeilToInt(trackingWindowSeconds / sampleInterval);
            while (deviationHistory.Count > maxSamples)
            {
                deviationHistory.Dequeue();
            }

            // Calculate average
            float sum = 0;
            foreach (float d in deviationHistory)
            {
                sum += d;
            }
            AverageDeviation = deviationHistory.Count > 0 ? sum / deviationHistory.Count : 0;

            // Update performance score (inverse of deviation, smoothed)
            float targetScore = 1f - Mathf.Clamp01(AverageDeviation);
            PerformanceScore = Mathf.Lerp(PerformanceScore, targetScore, 0.1f);
        }

        private void EvaluatePerformance()
        {
            var previousState = CurrentState;

            // Determine state
            if (AverageDeviation > strugglingThreshold)
            {
                CurrentState = PerformanceState.Struggling;
            }
            else if (AverageDeviation < coastingThreshold && deviationHistory.Count >= 10)
            {
                CurrentState = PerformanceState.Coasting;
            }
            else
            {
                CurrentState = PerformanceState.Normal;
            }

            // State changed
            if (CurrentState != previousState)
            {
                OnPerformanceStateChanged?.Invoke(CurrentState);
            }

            // Respond to state
            switch (CurrentState)
            {
                case PerformanceState.Struggling:
                    ConsiderProvidingHelp();
                    break;

                case PerformanceState.Coasting:
                    ConsiderAddingChallenge();
                    break;
            }
        }

        private void ConsiderProvidingHelp()
        {
            if (Time.time - lastHintTime < hintCooldown)
                return;

            // Check game mode allows hints
            if (GameModeManager.Instance != null &&
                !GameModeManager.Instance.CurrentSettings.HintsEnabled)
                return;

            lastHintTime = Time.time;

            // Provide contextual hint
            var indication = GameManager.Instance.CurrentVORIndication;
            if (indication.IsValid && hintSystem != null)
            {
                string direction = indication.CDIDeflection > 0 ? "right" : "left";
                float correction = Mathf.Abs(indication.CDIDeflection) * 20f;

                hintSystem.ShowHint(
                    $"You're drifting off course. Turn {direction} about {correction:F0}° to recenter.",
                    Color.yellow
                );
            }
        }

        private void ConsiderAddingChallenge()
        {
            if (Time.time - lastChallengeTime < challengeCooldown)
                return;

            // Check if game mode allows dynamic challenges
            var settings = GameModeManager.Instance?.CurrentSettings;
            if (settings != null && !settings.WindEnabled && !settings.EndlessMode)
                return; // Training mode - don't add challenges

            lastChallengeTime = Time.time;

            // Trigger a mild challenge to keep things interesting
            TriggerRandomChallenge();
        }

        private void TriggerRandomChallenge()
        {
            var challenges = new Action[]
            {
                () => TriggerWindShift(),
                () => TriggerMildTurbulence(),
                () => TriggerRadialChange()
            };

            int index = UnityEngine.Random.Range(0, challenges.Length);
            challenges[index]();
        }

        private void TriggerWindShift()
        {
            // Wind shift would be handled by native plugin in full implementation
            Debug.Log("Adaptive: Wind shift triggered");

            hintSystem?.ShowHint(
                "Wind shift detected. You may need to adjust your heading.",
                Color.cyan
            );
        }

        private void TriggerMildTurbulence()
        {
            Debug.Log("Adaptive: Mild turbulence triggered");

            hintSystem?.ShowHint(
                "Light turbulence encountered. Maintain steady control inputs.",
                Color.cyan
            );
        }

        private void TriggerRadialChange()
        {
            // In a full implementation, this would add a new objective
            Debug.Log("Adaptive: Suggesting radial change for variety");

            hintSystem?.ShowHint(
                "Nice flying! For an extra challenge, try intercepting a different radial.",
                Color.green
            );
        }

        // === Emergency Event System ===

        public void TriggerEmergency(EmergencyType type)
        {
            var emergencyEvent = CreateEmergency(type);
            OnEmergencyTriggered?.Invoke(emergencyEvent);

            switch (type)
            {
                case EmergencyType.VOROutage:
                    HandleVOROutage(emergencyEvent);
                    break;

                case EmergencyType.LostComms:
                    HandleLostComms(emergencyEvent);
                    break;

                case EmergencyType.InstrumentFailure:
                    HandleInstrumentFailure(emergencyEvent);
                    break;

                case EmergencyType.TrafficConflict:
                    HandleTrafficConflict(emergencyEvent);
                    break;
            }
        }

        private EmergencyEvent CreateEmergency(EmergencyType type)
        {
            return new EmergencyEvent
            {
                Type = type,
                StartTime = Time.time,
                Duration = GetEmergencyDuration(type),
                Description = GetEmergencyDescription(type),
                Instructions = GetEmergencyInstructions(type)
            };
        }

        private float GetEmergencyDuration(EmergencyType type)
        {
            return type switch
            {
                EmergencyType.VOROutage => 60f,
                EmergencyType.LostComms => 90f,
                EmergencyType.InstrumentFailure => 45f,
                EmergencyType.TrafficConflict => 30f,
                _ => 60f
            };
        }

        private string GetEmergencyDescription(EmergencyType type)
        {
            return type switch
            {
                EmergencyType.VOROutage => "VOR SIGNAL LOST",
                EmergencyType.LostComms => "COMMUNICATION FAILURE",
                EmergencyType.InstrumentFailure => "INSTRUMENT MALFUNCTION",
                EmergencyType.TrafficConflict => "TRAFFIC ALERT",
                _ => "EMERGENCY"
            };
        }

        private string GetEmergencyInstructions(EmergencyType type)
        {
            return type switch
            {
                EmergencyType.VOROutage =>
                    "Primary VOR signal lost. Maintain current heading and wait for signal restoration.",

                EmergencyType.LostComms =>
                    "Communications failure. Squawk 7600. Continue on last assigned heading.",

                EmergencyType.InstrumentFailure =>
                    "CDI malfunction. Use backup instruments and maintain situational awareness.",

                EmergencyType.TrafficConflict =>
                    "Traffic 12 o'clock, same altitude. Consider altitude change or turn.",

                _ => "Handle the emergency situation."
            };
        }

        private void HandleVOROutage(EmergencyEvent emergency)
        {
            Debug.Log("Emergency: VOR Outage - Signal lost");
            // In full implementation: disable VOR indication temporarily
        }

        private void HandleLostComms(EmergencyEvent emergency)
        {
            Debug.Log("Emergency: Lost Comms - Squawk 7600");
            // In full implementation: show squawk code, disable certain UI
        }

        private void HandleInstrumentFailure(EmergencyEvent emergency)
        {
            Debug.Log("Emergency: Instrument Failure");
            // In full implementation: flag certain instruments as failed
        }

        private void HandleTrafficConflict(EmergencyEvent emergency)
        {
            Debug.Log("Emergency: Traffic Conflict - TCAS alert");
            // In full implementation: show traffic on radar, require avoidance
        }
    }

    public enum PerformanceState
    {
        Struggling,  // Player having difficulty
        Normal,      // Performing adequately
        Coasting     // Doing very well, could use more challenge
    }

    public enum EmergencyType
    {
        VOROutage,
        LostComms,
        InstrumentFailure,
        TrafficConflict
    }

    [Serializable]
    public class EmergencyEvent
    {
        public EmergencyType Type;
        public float StartTime;
        public float Duration;
        public string Description;
        public string Instructions;
        public bool Resolved;
    }
}
