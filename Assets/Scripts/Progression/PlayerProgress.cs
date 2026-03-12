using System;
using System.Collections.Generic;
using UnityEngine;

namespace IFRTrainer.Progression
{
    /// <summary>
    /// Persistent player progress tracking - stats, certifications, unlocks.
    /// </summary>
    public class PlayerProgress : MonoBehaviour
    {
        public static PlayerProgress Instance { get; private set; }

        private const string SAVE_KEY = "IFRTrainer_PlayerProgress";

        public event Action<Certification> OnCertificationEarned;
        public event Action<string> OnMissionUnlocked;
        public event Action<int> OnXPGained;
        public event Action OnProgressLoaded;

        public PlayerStats Stats { get; private set; } = new PlayerStats();
        public List<Certification> EarnedCertifications { get; private set; } = new List<Certification>();
        public HashSet<string> UnlockedMissions { get; private set; } = new HashSet<string>();
        public HashSet<string> CompletedMissions { get; private set; } = new HashSet<string>();

        // XP and Level
        public int TotalXP => Stats.totalXP;
        public int Level => CalculateLevel(Stats.totalXP);
        public int XPToNextLevel => XPRequiredForLevel(Level + 1) - Stats.totalXP;
        public float LevelProgress => (float)(Stats.totalXP - XPRequiredForLevel(Level)) /
                                      (XPRequiredForLevel(Level + 1) - XPRequiredForLevel(Level));

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // DontDestroyOnLoad only works for root objects
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }

        public void RecordMissionComplete(string missionId, Mission.MissionResult result)
        {
            Stats.missionsAttempted++;

            if (result.Success)
            {
                Stats.missionsCompleted++;
                CompletedMissions.Add(missionId);

                // Track best grade for this mission
                if (!Stats.missionBestGrades.ContainsKey(missionId) ||
                    GradeToValue(result.Grade) > GradeToValue(Stats.missionBestGrades[missionId]))
                {
                    Stats.missionBestGrades[missionId] = result.Grade;
                }

                // Track best score
                if (!Stats.missionBestScores.ContainsKey(missionId) ||
                    result.TotalScore > Stats.missionBestScores[missionId])
                {
                    Stats.missionBestScores[missionId] = result.TotalScore;
                }

                // Grade tracking
                IncrementGradeCount(result.Grade);

                // Award XP
                int xpEarned = CalculateXPEarned(result);
                Stats.totalXP += xpEarned;
                OnXPGained?.Invoke(xpEarned);

                // Check for new certifications
                CheckCertifications();

                // Check for mission unlocks
                CheckMissionUnlocks();
            }

            Stats.totalFlightTimeSeconds += result.TotalTime;
            SaveProgress();
        }

        public void RecordDailyChallenge(bool completed, int score)
        {
            Stats.dailyChallengesAttempted++;

            if (completed)
            {
                Stats.dailyChallengesCompleted++;

                // Check streak
                var today = DateTime.UtcNow.Date;
                var lastDaily = DateTime.Parse(Stats.lastDailyChallengeDate);

                if ((today - lastDaily).Days == 1)
                {
                    Stats.currentDailyStreak++;
                    Stats.longestDailyStreak = Mathf.Max(Stats.longestDailyStreak, Stats.currentDailyStreak);
                }
                else if ((today - lastDaily).Days > 1)
                {
                    Stats.currentDailyStreak = 1;
                }

                Stats.lastDailyChallengeDate = today.ToString("yyyy-MM-dd");

                // Streak bonus XP
                int streakBonus = Stats.currentDailyStreak * 10;
                Stats.totalXP += streakBonus;
                OnXPGained?.Invoke(streakBonus);
            }

            SaveProgress();
        }

        private int CalculateXPEarned(Mission.MissionResult result)
        {
            int baseXP = result.TotalScore;

            // Grade multiplier
            float gradeMultiplier = result.Grade switch
            {
                "S+" => 2.0f,
                "A" => 1.5f,
                "B" => 1.2f,
                "C" => 1.0f,
                "D" => 0.8f,
                _ => 0.5f
            };

            return Mathf.RoundToInt(baseXP * gradeMultiplier);
        }

        private int CalculateLevel(int xp)
        {
            // Each level requires progressively more XP
            // Level 1: 0, Level 2: 100, Level 3: 300, Level 4: 600, etc.
            int level = 1;
            int required = 0;
            while (xp >= required)
            {
                level++;
                required += level * 100;
            }
            return level - 1;
        }

        private int XPRequiredForLevel(int level)
        {
            int total = 0;
            for (int i = 1; i < level; i++)
            {
                total += (i + 1) * 100;
            }
            return total;
        }

        private void IncrementGradeCount(string grade)
        {
            switch (grade)
            {
                case "S+": Stats.gradesS++; break;
                case "A": Stats.gradesA++; break;
                case "B": Stats.gradesB++; break;
                case "C": Stats.gradesC++; break;
                case "D": Stats.gradesD++; break;
                default: Stats.gradesF++; break;
            }
        }

        private int GradeToValue(string grade)
        {
            return grade switch
            {
                "S+" => 6,
                "A" => 5,
                "B" => 4,
                "C" => 3,
                "D" => 2,
                _ => 1
            };
        }

        private void CheckCertifications()
        {
            // Check all certification requirements
            foreach (var cert in Certification.AllCertifications)
            {
                if (EarnedCertifications.Contains(cert))
                    continue;

                if (cert.CheckRequirement(this))
                {
                    EarnedCertifications.Add(cert);
                    Stats.totalXP += cert.XPReward;
                    OnCertificationEarned?.Invoke(cert);
                    Debug.Log($"Certification earned: {cert.Name}");
                }
            }
        }

        private void CheckMissionUnlocks()
        {
            // Unlock missions based on level and completions
            var unlockRules = new Dictionary<string, Func<bool>>
            {
                { "vor_intercept_001", () => true }, // Always unlocked
                { "vor_intercept_002", () => CompletedMissions.Contains("vor_intercept_001") },
                { "vor_intercept_003", () => CompletedMissions.Contains("vor_intercept_002") },
                { "vor_tracking_001", () => Level >= 3 },
                { "vor_tracking_002", () => Level >= 5 && CompletedMissions.Contains("vor_tracking_001") },
                { "emergency_001", () => Level >= 5 },
                { "emergency_002", () => Level >= 8 && CompletedMissions.Contains("emergency_001") },
                { "time_attack_001", () => Stats.missionsCompleted >= 5 },
                { "time_attack_002", () => Stats.gradesA >= 3 },
                { "endless_mode", () => Level >= 10 }
            };

            foreach (var rule in unlockRules)
            {
                if (!UnlockedMissions.Contains(rule.Key) && rule.Value())
                {
                    UnlockedMissions.Add(rule.Key);
                    OnMissionUnlocked?.Invoke(rule.Key);
                    Debug.Log($"Mission unlocked: {rule.Key}");
                }
            }
        }

        public bool IsMissionUnlocked(string missionId)
        {
            // First mission always unlocked
            if (missionId == "vor_intercept_001")
                return true;

            return UnlockedMissions.Contains(missionId);
        }

        public string GetMissionBestGrade(string missionId)
        {
            return Stats.missionBestGrades.TryGetValue(missionId, out string grade) ? grade : "-";
        }

        public int GetMissionBestScore(string missionId)
        {
            return Stats.missionBestScores.TryGetValue(missionId, out int score) ? score : 0;
        }

        public void SaveProgress()
        {
            var data = new ProgressSaveData
            {
                stats = Stats,
                earnedCertificationIds = new List<string>(),
                unlockedMissions = new List<string>(UnlockedMissions),
                completedMissions = new List<string>(CompletedMissions)
            };

            foreach (var cert in EarnedCertifications)
            {
                data.earnedCertificationIds.Add(cert.Id);
            }

            string json = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public void LoadProgress()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY))
            {
                // Initialize defaults
                UnlockedMissions.Add("vor_intercept_001");
                Stats.lastDailyChallengeDate = DateTime.MinValue.ToString("yyyy-MM-dd");
                OnProgressLoaded?.Invoke();
                return;
            }

            string json = PlayerPrefs.GetString(SAVE_KEY);
            var data = JsonUtility.FromJson<ProgressSaveData>(json);

            Stats = data.stats ?? new PlayerStats();
            UnlockedMissions = new HashSet<string>(data.unlockedMissions ?? new List<string>());
            CompletedMissions = new HashSet<string>(data.completedMissions ?? new List<string>());

            EarnedCertifications.Clear();
            foreach (var certId in data.earnedCertificationIds ?? new List<string>())
            {
                var cert = Certification.GetById(certId);
                if (cert != null)
                    EarnedCertifications.Add(cert);
            }

            // Ensure first mission is unlocked
            UnlockedMissions.Add("vor_intercept_001");

            OnProgressLoaded?.Invoke();
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            Stats = new PlayerStats();
            EarnedCertifications.Clear();
            UnlockedMissions.Clear();
            CompletedMissions.Clear();
            UnlockedMissions.Add("vor_intercept_001");
            Stats.lastDailyChallengeDate = DateTime.MinValue.ToString("yyyy-MM-dd");
            SaveProgress();
        }
    }

    [Serializable]
    public class PlayerStats
    {
        public int missionsAttempted;
        public int missionsCompleted;
        public int totalXP;
        public float totalFlightTimeSeconds;

        // Grade counts
        public int gradesS;
        public int gradesA;
        public int gradesB;
        public int gradesC;
        public int gradesD;
        public int gradesF;

        // Daily challenges
        public int dailyChallengesAttempted;
        public int dailyChallengesCompleted;
        public int currentDailyStreak;
        public int longestDailyStreak;
        public string lastDailyChallengeDate = "";

        // Best scores per mission
        public SerializableDictionary missionBestGrades = new SerializableDictionary();
        public SerializableDictionaryInt missionBestScores = new SerializableDictionaryInt();

        // Lifetime stats
        public int perfectIntercepts; // < 0.5 degree deviation
        public float totalRadialTrackingTime;
        public int emergenciesHandled;
    }

    [Serializable]
    public class SerializableDictionary : ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<string> values = new List<string>();
        private Dictionary<string, string> dict = new Dictionary<string, string>();

        public string this[string key]
        {
            get => dict[key];
            set => dict[key] = value;
        }

        public bool ContainsKey(string key) => dict.ContainsKey(key);
        public bool TryGetValue(string key, out string value) => dict.TryGetValue(key, out value);

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dict.Clear();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                dict[keys[i]] = values[i];
            }
        }
    }

    [Serializable]
    public class SerializableDictionaryInt : ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<int> values = new List<int>();
        private Dictionary<string, int> dict = new Dictionary<string, int>();

        public int this[string key]
        {
            get => dict[key];
            set => dict[key] = value;
        }

        public bool ContainsKey(string key) => dict.ContainsKey(key);
        public bool TryGetValue(string key, out int value) => dict.TryGetValue(key, out value);

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dict.Clear();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                dict[keys[i]] = values[i];
            }
        }
    }

    [Serializable]
    public class ProgressSaveData
    {
        public PlayerStats stats;
        public List<string> earnedCertificationIds;
        public List<string> unlockedMissions;
        public List<string> completedMissions;
    }
}
