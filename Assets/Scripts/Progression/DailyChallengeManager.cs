using System;
using UnityEngine;
using IFRTrainer.Mission;

namespace IFRTrainer.Progression
{
    /// <summary>
    /// Manages daily challenges with procedurally generated variations.
    /// Each day has a unique challenge based on the date seed.
    /// </summary>
    public class DailyChallengeManager : MonoBehaviour
    {
        public static DailyChallengeManager Instance { get; private set; }

        [Header("Challenge Settings")]
        [SerializeField] private float baseScoreMultiplier = 1.5f;
        [SerializeField] private int streakBonusPerDay = 10;
        [SerializeField] private int maxStreakBonus = 100;

        public event Action<DailyChallenge> OnDailyChallengeGenerated;
        public event Action<DailyChallengeResult> OnDailyChallengeCompleted;

        public DailyChallenge TodaysChallenge { get; private set; }
        public bool HasCompletedToday { get; private set; }
        public int CurrentStreak => PlayerProgress.Instance?.Stats.currentDailyStreak ?? 0;

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
            GenerateTodaysChallenge();
            CheckIfCompletedToday();
        }

        public void GenerateTodaysChallenge()
        {
            var today = DateTime.UtcNow.Date;
            int seed = today.Year * 10000 + today.Month * 100 + today.Day;

            TodaysChallenge = GenerateChallenge(seed, today);
            OnDailyChallengeGenerated?.Invoke(TodaysChallenge);
        }

        private DailyChallenge GenerateChallenge(int seed, DateTime date)
        {
            var rng = new System.Random(seed);

            // Select challenge type based on day of week for variety
            ChallengeType type = (ChallengeType)(rng.Next(0, 5));

            // Generate varied parameters
            var challenge = new DailyChallenge
            {
                Date = date,
                Seed = seed,
                Type = type,
                Title = GetChallengeTitle(type, rng),
                Description = GetChallengeDescription(type),
                ScoreMultiplier = baseScoreMultiplier + (CurrentStreak * 0.05f),
                StreakBonus = Mathf.Min(CurrentStreak * streakBonusPerDay, maxStreakBonus)
            };

            // Generate mission parameters
            challenge.MissionParams = GenerateMissionParams(type, rng);

            return challenge;
        }

        private string GetChallengeTitle(ChallengeType type, System.Random rng)
        {
            string[] prefixes = { "Dawn", "Midday", "Twilight", "Night", "Storm" };
            string prefix = prefixes[rng.Next(prefixes.Length)];

            return type switch
            {
                ChallengeType.PrecisionIntercept => $"{prefix} Precision Challenge",
                ChallengeType.SpeedRun => $"{prefix} Speed Run",
                ChallengeType.EnduranceTrack => $"{prefix} Endurance Test",
                ChallengeType.MultiRadial => $"{prefix} Multi-Radial Navigation",
                ChallengeType.LowVisibility => $"{prefix} Low Visibility Approach",
                _ => $"{prefix} Navigation Challenge"
            };
        }

        private string GetChallengeDescription(ChallengeType type)
        {
            return type switch
            {
                ChallengeType.PrecisionIntercept =>
                    "Intercept and track the radial with maximum precision. Tighter tolerances, higher rewards.",
                ChallengeType.SpeedRun =>
                    "Complete the intercept as fast as possible. Time is everything!",
                ChallengeType.EnduranceTrack =>
                    "Track the radial for an extended period. Consistency wins.",
                ChallengeType.MultiRadial =>
                    "Navigate multiple radials in sequence. Test your planning skills.",
                ChallengeType.LowVisibility =>
                    "Reduced instrument visibility. Trust your training!",
                _ => "Complete today's unique navigation challenge."
            };
        }

        private DailyMissionParams GenerateMissionParams(ChallengeType type, System.Random rng)
        {
            var p = new DailyMissionParams();

            // Base VOR - varies daily
            string[] vorIds = { "LAX", "VNY", "SLI", "PDZ", "PMD" };
            double[][] vorPositions = {
                new[] { 33.934, -118.40 },  // LAX
                new[] { 34.209, -118.489 }, // VNY
                new[] { 33.818, -117.795 }, // SLI
                new[] { 33.629, -116.467 }, // PDZ
                new[] { 34.629, -118.084 }  // PMD
            };

            int vorIndex = rng.Next(vorIds.Length);
            p.VORIdentifier = vorIds[vorIndex];
            p.VORLatitude = vorPositions[vorIndex][0];
            p.VORLongitude = vorPositions[vorIndex][1];
            p.VORFrequency = 108.0f + (float)(rng.NextDouble() * 9.9);
            p.VORFrequency = Mathf.Round(p.VORFrequency * 20) / 20; // Round to 0.05

            // Target radial - varies daily
            p.TargetRadial = rng.Next(36) * 10; // 0, 10, 20, ... 350

            // Starting position - offset from VOR
            double offsetNM = 5 + rng.NextDouble() * 10;
            double offsetBearing = rng.Next(360);
            double offsetBearingRad = offsetBearing * Math.PI / 180;

            // Simple offset (not accounting for Earth curvature - close enough for training)
            double nmToDegrees = 1.0 / 60.0;
            p.StartLatitude = p.VORLatitude + Math.Cos(offsetBearingRad) * offsetNM * nmToDegrees;
            p.StartLongitude = p.VORLongitude + Math.Sin(offsetBearingRad) * offsetNM * nmToDegrees /
                              Math.Cos(p.VORLatitude * Math.PI / 180);

            // Starting heading - somewhat toward VOR
            p.StartHeading = rng.Next(360);
            p.StartAltitude = 3000 + rng.Next(6) * 1000; // 3000-8000
            p.StartSpeed = 120 + rng.Next(5) * 10; // 120-160 knots

            // Challenge-specific parameters
            switch (type)
            {
                case ChallengeType.PrecisionIntercept:
                    p.MaxDeviationDeg = 1.0f; // Tighter than normal
                    p.TrackDurationSec = 90;
                    p.MaxAvgDeviationDeg = 0.5f;
                    break;

                case ChallengeType.SpeedRun:
                    p.MaxDeviationDeg = 3.0f; // More forgiving
                    p.TrackDurationSec = 30;
                    p.TimeBonusThreshold = 60; // Fast completion bonus
                    break;

                case ChallengeType.EnduranceTrack:
                    p.MaxDeviationDeg = 2.0f;
                    p.TrackDurationSec = 300; // 5 minutes
                    p.MaxAvgDeviationDeg = 1.0f;
                    break;

                case ChallengeType.MultiRadial:
                    p.MaxDeviationDeg = 2.0f;
                    p.TrackDurationSec = 60;
                    p.SecondaryRadial = (p.TargetRadial + 90 + rng.Next(180)) % 360;
                    break;

                case ChallengeType.LowVisibility:
                    p.MaxDeviationDeg = 2.5f;
                    p.TrackDurationSec = 120;
                    p.ReducedVisibility = true;
                    break;

                default:
                    p.MaxDeviationDeg = 2.0f;
                    p.TrackDurationSec = 120;
                    break;
            }

            return p;
        }

        public MissionData CreateMissionFromChallenge()
        {
            var p = TodaysChallenge.MissionParams;

            var mission = new MissionData
            {
                mission_id = $"daily_{TodaysChallenge.Seed}",
                title = TodaysChallenge.Title,
                difficulty = GetChallengeDifficulty(TodaysChallenge.Type),
                initial_conditions = new InitialConditions
                {
                    aircraft = new AircraftInitialState
                    {
                        latitude = p.StartLatitude,
                        longitude = p.StartLongitude,
                        altitude_feet = p.StartAltitude,
                        heading = p.StartHeading,
                        speed_knots = p.StartSpeed
                    }
                },
                vor_stations = new VORStationData[]
                {
                    new VORStationData
                    {
                        identifier = p.VORIdentifier,
                        latitude = p.VORLatitude,
                        longitude = p.VORLongitude,
                        frequency_mhz = p.VORFrequency
                    }
                },
                objectives = CreateObjectives(TodaysChallenge.Type, p),
                scoring = new ScoringData
                {
                    time_bonus_threshold_seconds = p.TimeBonusThreshold > 0 ? p.TimeBonusThreshold : 180,
                    deviation_penalty_per_degree = 5
                }
            };

            return mission;
        }

        private int GetChallengeDifficulty(ChallengeType type)
        {
            return type switch
            {
                ChallengeType.SpeedRun => 2,
                ChallengeType.PrecisionIntercept => 3,
                ChallengeType.EnduranceTrack => 2,
                ChallengeType.MultiRadial => 4,
                ChallengeType.LowVisibility => 4,
                _ => 2
            };
        }

        private ObjectiveData[] CreateObjectives(ChallengeType type, DailyMissionParams p)
        {
            var objectives = new System.Collections.Generic.List<ObjectiveData>();

            // Primary intercept
            objectives.Add(new ObjectiveData
            {
                type = "intercept_radial",
                station = p.VORIdentifier,
                radial = p.TargetRadial,
                max_deviation_deg = p.MaxDeviationDeg
            });

            // Tracking
            objectives.Add(new ObjectiveData
            {
                type = "track_radial",
                station = p.VORIdentifier,
                radial = p.TargetRadial,
                duration_seconds = p.TrackDurationSec,
                max_avg_deviation_deg = p.MaxAvgDeviationDeg > 0 ? p.MaxAvgDeviationDeg : 1.0f
            });

            // Multi-radial adds second intercept
            if (type == ChallengeType.MultiRadial && p.SecondaryRadial > 0)
            {
                objectives.Add(new ObjectiveData
                {
                    type = "intercept_radial",
                    station = p.VORIdentifier,
                    radial = p.SecondaryRadial,
                    max_deviation_deg = p.MaxDeviationDeg
                });

                objectives.Add(new ObjectiveData
                {
                    type = "track_radial",
                    station = p.VORIdentifier,
                    radial = p.SecondaryRadial,
                    duration_seconds = p.TrackDurationSec,
                    max_avg_deviation_deg = p.MaxAvgDeviationDeg > 0 ? p.MaxAvgDeviationDeg : 1.0f
                });
            }

            return objectives.ToArray();
        }

        private void CheckIfCompletedToday()
        {
            if (PlayerProgress.Instance == null)
                return;

            var lastDate = PlayerProgress.Instance.Stats.lastDailyChallengeDate;
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            HasCompletedToday = (lastDate == today);
        }

        public void RecordCompletion(MissionResult result)
        {
            if (HasCompletedToday)
                return;

            HasCompletedToday = true;

            // Apply daily challenge multipliers
            int bonusScore = Mathf.RoundToInt(result.TotalScore * (TodaysChallenge.ScoreMultiplier - 1));
            int finalScore = result.TotalScore + bonusScore + TodaysChallenge.StreakBonus;

            var challengeResult = new DailyChallengeResult
            {
                Challenge = TodaysChallenge,
                BaseScore = result.TotalScore,
                MultiplierBonus = bonusScore,
                StreakBonus = TodaysChallenge.StreakBonus,
                FinalScore = finalScore,
                Grade = result.Grade,
                Success = result.Success
            };

            // Update player progress
            PlayerProgress.Instance?.RecordDailyChallenge(result.Success, finalScore);

            OnDailyChallengeCompleted?.Invoke(challengeResult);
        }

        public string GetStreakBonusText()
        {
            if (CurrentStreak == 0)
                return "Start your streak today!";
            if (CurrentStreak == 1)
                return "1 day streak! Keep it going!";
            return $"{CurrentStreak} day streak! (+{TodaysChallenge.StreakBonus} bonus)";
        }

        public TimeSpan TimeUntilReset()
        {
            var now = DateTime.UtcNow;
            var tomorrow = now.Date.AddDays(1);
            return tomorrow - now;
        }
    }

    public enum ChallengeType
    {
        PrecisionIntercept,
        SpeedRun,
        EnduranceTrack,
        MultiRadial,
        LowVisibility
    }

    [Serializable]
    public class DailyChallenge
    {
        public DateTime Date;
        public int Seed;
        public ChallengeType Type;
        public string Title;
        public string Description;
        public float ScoreMultiplier;
        public int StreakBonus;
        public DailyMissionParams MissionParams;
    }

    [Serializable]
    public class DailyMissionParams
    {
        public string VORIdentifier;
        public double VORLatitude;
        public double VORLongitude;
        public float VORFrequency;
        public int TargetRadial;
        public int SecondaryRadial;
        public double StartLatitude;
        public double StartLongitude;
        public int StartAltitude;
        public int StartHeading;
        public int StartSpeed;
        public float MaxDeviationDeg;
        public int TrackDurationSec;
        public float MaxAvgDeviationDeg;
        public int TimeBonusThreshold;
        public bool ReducedVisibility;
    }

    [Serializable]
    public class DailyChallengeResult
    {
        public DailyChallenge Challenge;
        public int BaseScore;
        public int MultiplierBonus;
        public int StreakBonus;
        public int FinalScore;
        public string Grade;
        public bool Success;
    }
}
