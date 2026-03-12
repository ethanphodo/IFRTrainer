using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IFRTrainer.Progression;

namespace IFRTrainer.UI
{
    /// <summary>
    /// UI for displaying player progression, stats, and certifications.
    /// </summary>
    public class ProgressionUI : MonoBehaviour
    {
        [Header("Level Display")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text xpText;
        [SerializeField] private Slider xpProgressBar;

        [Header("Stats Panel")]
        [SerializeField] private Text missionsCompletedText;
        [SerializeField] private Text flightTimeText;
        [SerializeField] private Text bestGradeText;
        [SerializeField] private Text streakText;

        [Header("Grade Distribution")]
        [SerializeField] private Text gradesSText;
        [SerializeField] private Text gradesAText;
        [SerializeField] private Text gradesBText;
        [SerializeField] private Text gradesCText;
        [SerializeField] private Text gradesDText;
        [SerializeField] private Text gradesFText;

        [Header("Certifications")]
        [SerializeField] private Transform certificationsContainer;
        [SerializeField] private GameObject certificationBadgePrefab;

        [Header("Daily Challenge")]
        [SerializeField] private Text dailyChallengeTitle;
        [SerializeField] private Text dailyChallengeDesc;
        [SerializeField] private Text dailyStreakText;
        [SerializeField] private Text dailyResetTimer;
        [SerializeField] private Button playDailyButton;
        [SerializeField] private GameObject dailyCompletedBadge;

        [Header("Notification")]
        [SerializeField] private GameObject notificationPopup;
        [SerializeField] private Text notificationText;
        [SerializeField] private Image notificationIcon;
        [SerializeField] private float notificationDuration = 3f;

        private Queue<string> notificationQueue = new Queue<string>();
        private bool isShowingNotification;

        private void OnEnable()
        {
            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.OnCertificationEarned += OnCertificationEarned;
                PlayerProgress.Instance.OnMissionUnlocked += OnMissionUnlocked;
                PlayerProgress.Instance.OnXPGained += OnXPGained;
            }

            RefreshUI();
        }

        private void OnDisable()
        {
            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.OnCertificationEarned -= OnCertificationEarned;
                PlayerProgress.Instance.OnMissionUnlocked -= OnMissionUnlocked;
                PlayerProgress.Instance.OnXPGained -= OnXPGained;
            }
        }

        private void Update()
        {
            UpdateDailyResetTimer();
            ProcessNotificationQueue();
        }

        public void RefreshUI()
        {
            if (PlayerProgress.Instance == null)
                return;

            var progress = PlayerProgress.Instance;
            var stats = progress.Stats;

            // Level display
            if (levelText != null)
                levelText.text = $"Level {progress.Level}";

            if (xpText != null)
                xpText.text = $"{progress.TotalXP} XP ({progress.XPToNextLevel} to next level)";

            if (xpProgressBar != null)
                xpProgressBar.value = progress.LevelProgress;

            // Stats
            if (missionsCompletedText != null)
                missionsCompletedText.text = $"{stats.missionsCompleted} / {stats.missionsAttempted}";

            if (flightTimeText != null)
            {
                float hours = stats.totalFlightTimeSeconds / 3600f;
                flightTimeText.text = $"{hours:F1} hours";
            }

            if (bestGradeText != null)
            {
                string best = stats.gradesS > 0 ? "S+" :
                             stats.gradesA > 0 ? "A" :
                             stats.gradesB > 0 ? "B" :
                             stats.gradesC > 0 ? "C" :
                             stats.gradesD > 0 ? "D" : "-";
                bestGradeText.text = best;
            }

            if (streakText != null)
                streakText.text = $"{stats.currentDailyStreak} days (best: {stats.longestDailyStreak})";

            // Grade distribution
            if (gradesSText != null) gradesSText.text = stats.gradesS.ToString();
            if (gradesAText != null) gradesAText.text = stats.gradesA.ToString();
            if (gradesBText != null) gradesBText.text = stats.gradesB.ToString();
            if (gradesCText != null) gradesCText.text = stats.gradesC.ToString();
            if (gradesDText != null) gradesDText.text = stats.gradesD.ToString();
            if (gradesFText != null) gradesFText.text = stats.gradesF.ToString();

            // Certifications
            RefreshCertifications();

            // Daily challenge
            RefreshDailyChallenge();
        }

        private void RefreshCertifications()
        {
            if (certificationsContainer == null || certificationBadgePrefab == null)
                return;

            // Clear existing
            foreach (Transform child in certificationsContainer)
            {
                Destroy(child.gameObject);
            }

            // Show all certifications (earned ones highlighted)
            foreach (var cert in Certification.AllCertifications)
            {
                var badge = Instantiate(certificationBadgePrefab, certificationsContainer);
                var badgeUI = badge.GetComponent<CertificationBadgeUI>();

                bool earned = PlayerProgress.Instance.EarnedCertifications.Contains(cert);

                if (badgeUI != null)
                {
                    badgeUI.Setup(cert, earned);
                }
                else
                {
                    // Fallback: just set text
                    var text = badge.GetComponentInChildren<Text>();
                    if (text != null)
                    {
                        text.text = cert.Name;
                        text.color = earned ? Color.white : Color.gray;
                    }
                }
            }
        }

        private void RefreshDailyChallenge()
        {
            if (DailyChallengeManager.Instance == null)
                return;

            var daily = DailyChallengeManager.Instance;
            var challenge = daily.TodaysChallenge;

            if (dailyChallengeTitle != null && challenge != null)
                dailyChallengeTitle.text = challenge.Title;

            if (dailyChallengeDesc != null && challenge != null)
                dailyChallengeDesc.text = challenge.Description;

            if (dailyStreakText != null)
                dailyStreakText.text = daily.GetStreakBonusText();

            if (playDailyButton != null)
                playDailyButton.interactable = !daily.HasCompletedToday;

            if (dailyCompletedBadge != null)
                dailyCompletedBadge.SetActive(daily.HasCompletedToday);
        }

        private void UpdateDailyResetTimer()
        {
            if (dailyResetTimer == null || DailyChallengeManager.Instance == null)
                return;

            var timeLeft = DailyChallengeManager.Instance.TimeUntilReset();
            dailyResetTimer.text = $"Resets in {timeLeft.Hours:D2}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
        }

        // Event handlers
        private void OnCertificationEarned(Certification cert)
        {
            QueueNotification($"Certification Earned: {cert.Name}!");
            RefreshCertifications();
        }

        private void OnMissionUnlocked(string missionId)
        {
            QueueNotification($"New Mission Unlocked: {FormatMissionName(missionId)}");
        }

        private void OnXPGained(int xp)
        {
            int oldLevel = PlayerProgress.Instance.Level;
            RefreshUI();

            if (PlayerProgress.Instance.Level > oldLevel)
            {
                QueueNotification($"Level Up! You are now level {PlayerProgress.Instance.Level}!");
            }
        }

        private void QueueNotification(string message)
        {
            notificationQueue.Enqueue(message);
        }

        private void ProcessNotificationQueue()
        {
            if (isShowingNotification || notificationQueue.Count == 0)
                return;

            ShowNotification(notificationQueue.Dequeue());
        }

        private void ShowNotification(string message)
        {
            if (notificationPopup == null)
                return;

            isShowingNotification = true;
            notificationPopup.SetActive(true);

            if (notificationText != null)
                notificationText.text = message;

            // Auto-hide after duration
            Invoke(nameof(HideNotification), notificationDuration);
        }

        private void HideNotification()
        {
            if (notificationPopup != null)
                notificationPopup.SetActive(false);

            isShowingNotification = false;
        }

        private string FormatMissionName(string missionId)
        {
            return missionId.Replace("_", " ").ToUpper();
        }

        // Button handlers
        public void PlayDailyChallenge()
        {
            if (DailyChallengeManager.Instance != null &&
                !DailyChallengeManager.Instance.HasCompletedToday)
            {
                var mission = DailyChallengeManager.Instance.CreateMissionFromChallenge();
                var missionManager = FindFirstObjectByType<Mission.MissionManager>();
                if (missionManager != null)
                {
                    missionManager.LoadMissionFromJson(JsonUtility.ToJson(mission));
                    missionManager.StartMission();
                }
            }
        }
    }

    /// <summary>
    /// Individual certification badge UI component.
    /// </summary>
    public class CertificationBadgeUI : MonoBehaviour
    {
        [SerializeField] private Image badgeIcon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descText;
        [SerializeField] private Image tierBorder;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Tier Colors")]
        [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color platinumColor = new Color(0.9f, 0.9f, 1f);
        [SerializeField] private Color diamondColor = new Color(0.7f, 0.9f, 1f);

        public void Setup(Certification cert, bool earned)
        {
            if (nameText != null)
                nameText.text = cert.Name;

            if (descText != null)
                descText.text = earned ? cert.Description : "???";

            if (tierBorder != null)
                tierBorder.color = GetTierColor(cert.Tier);

            if (canvasGroup != null)
                canvasGroup.alpha = earned ? 1f : 0.4f;
        }

        private Color GetTierColor(CertificationTier tier)
        {
            return tier switch
            {
                CertificationTier.Bronze => bronzeColor,
                CertificationTier.Silver => silverColor,
                CertificationTier.Gold => goldColor,
                CertificationTier.Platinum => platinumColor,
                CertificationTier.Diamond => diamondColor,
                _ => Color.white
            };
        }
    }
}
