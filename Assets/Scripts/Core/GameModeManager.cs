using System;
using UnityEngine;
using IFRTrainer.Mission;
using IFRTrainer.Navigation;

namespace IFRTrainer.Core
{
    /// <summary>
    /// Manages different game modes with varying difficulty and rules.
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        [Header("Current Mode")]
        [SerializeField] private GameMode currentMode = GameMode.Training;

        public event Action<GameMode> OnModeChanged;
        public event Action<GameModeSettings> OnSettingsApplied;

        public GameMode CurrentMode => currentMode;
        public GameModeSettings CurrentSettings { get; private set; }

        // Endless mode state
        public bool IsEndlessMode => currentMode == GameMode.Endless;
        public int EndlessWave { get; private set; }
        public int EndlessScore { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ApplyModeSettings(currentMode);
        }

        public void SetGameMode(GameMode mode)
        {
            if (currentMode != mode)
            {
                currentMode = mode;
                ApplyModeSettings(mode);
                OnModeChanged?.Invoke(mode);

                Debug.Log($"Game mode changed to: {mode}");
            }
        }

        private void ApplyModeSettings(GameMode mode)
        {
            CurrentSettings = GetSettingsForMode(mode);
            OnSettingsApplied?.Invoke(CurrentSettings);
        }

        public static GameModeSettings GetSettingsForMode(GameMode mode)
        {
            return mode switch
            {
                GameMode.Training => new GameModeSettings
                {
                    ModeName = "Training",
                    Description = "Learn the basics with helpful hints and forgiving tolerances.",
                    HintsEnabled = true,
                    ShowRadialGuides = true,
                    InterceptToleranceDeg = 3.0f,
                    TrackingToleranceDeg = 2.0f,
                    ScoreMultiplier = 0.75f,
                    WindEnabled = false,
                    TurbulenceEnabled = false,
                    InstrumentFailures = false,
                    TimeLimit = 0, // No limit
                    AllowPause = true
                },

                GameMode.Standard => new GameModeSettings
                {
                    ModeName = "Standard",
                    Description = "Realistic navigation with standard instrument tolerances.",
                    HintsEnabled = false,
                    ShowRadialGuides = false,
                    InterceptToleranceDeg = 2.0f,
                    TrackingToleranceDeg = 1.0f,
                    ScoreMultiplier = 1.0f,
                    WindEnabled = true,
                    WindVariability = 0.3f,
                    TurbulenceEnabled = false,
                    InstrumentFailures = false,
                    TimeLimit = 0,
                    AllowPause = true
                },

                GameMode.Realistic => new GameModeSettings
                {
                    ModeName = "Realistic",
                    Description = "Full realism with wind, tight tolerances, and no assists.",
                    HintsEnabled = false,
                    ShowRadialGuides = false,
                    InterceptToleranceDeg = 1.5f,
                    TrackingToleranceDeg = 0.5f,
                    ScoreMultiplier = 1.5f,
                    WindEnabled = true,
                    WindVariability = 0.5f,
                    TurbulenceEnabled = true,
                    TurbulenceIntensity = 0.3f,
                    InstrumentFailures = true,
                    FailureProbability = 0.05f,
                    TimeLimit = 0,
                    AllowPause = false // No pausing in realistic!
                },

                GameMode.TimeAttack => new GameModeSettings
                {
                    ModeName = "Time Attack",
                    Description = "Race against the clock! Speed is everything.",
                    HintsEnabled = false,
                    ShowRadialGuides = true, // Show guides to speed up
                    InterceptToleranceDeg = 2.5f, // Slightly forgiving
                    TrackingToleranceDeg = 1.5f,
                    ScoreMultiplier = 1.25f,
                    WindEnabled = false,
                    TurbulenceEnabled = false,
                    InstrumentFailures = false,
                    TimeLimit = 180, // 3 minute limit
                    TimeBonusEnabled = true,
                    TimeBonusPerSecond = 2,
                    AllowPause = false
                },

                GameMode.Endless => new GameModeSettings
                {
                    ModeName = "Endless",
                    Description = "Survive as long as possible with escalating difficulty.",
                    HintsEnabled = false,
                    ShowRadialGuides = false,
                    InterceptToleranceDeg = 2.0f, // Starts normal
                    TrackingToleranceDeg = 1.0f,
                    ScoreMultiplier = 1.0f,
                    WindEnabled = true,
                    WindVariability = 0.2f, // Increases per wave
                    TurbulenceEnabled = false, // Enabled at higher waves
                    InstrumentFailures = false, // Enabled at higher waves
                    TimeLimit = 0,
                    AllowPause = true,
                    EndlessMode = true,
                    WaveScalingFactor = 0.1f
                },

                _ => GetSettingsForMode(GameMode.Standard)
            };
        }

        // === Endless Mode Methods ===

        public void StartEndlessMode()
        {
            SetGameMode(GameMode.Endless);
            EndlessWave = 1;
            EndlessScore = 0;
        }

        public void AdvanceEndlessWave()
        {
            EndlessWave++;

            // Scale difficulty
            float waveFactor = 1 + (EndlessWave - 1) * CurrentSettings.WaveScalingFactor;

            CurrentSettings.InterceptToleranceDeg = Mathf.Max(0.5f, 2.0f / waveFactor);
            CurrentSettings.TrackingToleranceDeg = Mathf.Max(0.3f, 1.0f / waveFactor);
            CurrentSettings.WindVariability = Mathf.Min(1.0f, 0.2f * waveFactor);
            CurrentSettings.ScoreMultiplier = 1.0f + (EndlessWave - 1) * 0.25f;

            // Enable turbulence at wave 5
            if (EndlessWave >= 5)
            {
                CurrentSettings.TurbulenceEnabled = true;
                CurrentSettings.TurbulenceIntensity = Mathf.Min(0.8f, (EndlessWave - 4) * 0.1f);
            }

            // Enable failures at wave 10
            if (EndlessWave >= 10)
            {
                CurrentSettings.InstrumentFailures = true;
                CurrentSettings.FailureProbability = Mathf.Min(0.2f, (EndlessWave - 9) * 0.02f);
            }

            OnSettingsApplied?.Invoke(CurrentSettings);
            Debug.Log($"Endless wave {EndlessWave}: Tolerance={CurrentSettings.InterceptToleranceDeg:F1}°, " +
                     $"Wind={CurrentSettings.WindVariability:F1}, Score multiplier={CurrentSettings.ScoreMultiplier:F2}x");
        }

        public void AddEndlessScore(int points)
        {
            EndlessScore += Mathf.RoundToInt(points * CurrentSettings.ScoreMultiplier);
        }

        public MissionData GenerateEndlessWaveMission()
        {
            var rng = new System.Random(DateTime.Now.Millisecond + EndlessWave * 1000);

            // Randomize parameters
            int radial = rng.Next(36) * 10;
            int trackDuration = 30 + EndlessWave * 10; // Longer tracking each wave

            return new MissionData
            {
                mission_id = $"endless_wave_{EndlessWave}",
                title = $"Wave {EndlessWave}",
                difficulty = Mathf.Min(5, 1 + EndlessWave / 3),
                initial_conditions = new InitialConditions
                {
                    aircraft = new AircraftInitialState
                    {
                        latitude = 33.94 + (rng.NextDouble() - 0.5) * 0.2,
                        longitude = -118.40 + (rng.NextDouble() - 0.5) * 0.2,
                        altitude_feet = 4000 + rng.Next(4) * 1000,
                        heading = rng.Next(360),
                        speed_knots = 140 + rng.Next(4) * 10
                    }
                },
                vor_stations = new VORStationData[]
                {
                    new VORStationData
                    {
                        identifier = "LAX",
                        latitude = 33.934,
                        longitude = -118.40,
                        frequency_mhz = 113.6f
                    }
                },
                objectives = new ObjectiveData[]
                {
                    new ObjectiveData
                    {
                        type = "intercept_radial",
                        station = "LAX",
                        radial = radial,
                        max_deviation_deg = CurrentSettings.InterceptToleranceDeg
                    },
                    new ObjectiveData
                    {
                        type = "track_radial",
                        station = "LAX",
                        radial = radial,
                        duration_seconds = trackDuration,
                        max_avg_deviation_deg = CurrentSettings.TrackingToleranceDeg
                    }
                },
                scoring = new ScoringData
                {
                    time_bonus_threshold_seconds = 120,
                    deviation_penalty_per_degree = 5 + EndlessWave
                }
            };
        }
    }

    public enum GameMode
    {
        Training,
        Standard,
        Realistic,
        TimeAttack,
        Endless
    }

    [Serializable]
    public class GameModeSettings
    {
        public string ModeName;
        public string Description;

        // Assistance
        public bool HintsEnabled;
        public bool ShowRadialGuides;

        // Tolerances
        public float InterceptToleranceDeg;
        public float TrackingToleranceDeg;

        // Scoring
        public float ScoreMultiplier;

        // Environmental
        public bool WindEnabled;
        public float WindVariability; // 0-1
        public bool TurbulenceEnabled;
        public float TurbulenceIntensity; // 0-1

        // Failures
        public bool InstrumentFailures;
        public float FailureProbability; // 0-1

        // Time
        public int TimeLimit; // seconds, 0 = no limit
        public bool TimeBonusEnabled;
        public int TimeBonusPerSecond;
        public bool AllowPause;

        // Endless
        public bool EndlessMode;
        public float WaveScalingFactor;
    }
}
