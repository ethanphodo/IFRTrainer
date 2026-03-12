using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using IFRTrainer.Progression;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Main menu progression dashboard showing XP, level, certifications, and daily challenge.
    /// Makes progression visible and motivating.
    /// </summary>
    public class ProgressionDashboard : MonoBehaviour
    {
        public static ProgressionDashboard Instance { get; private set; }

        // Level titles based on progression
        private static readonly string[] LevelTitles = new string[]
        {
            "Student Pilot",           // Level 1-2
            "Student Pilot",
            "Private Pilot",           // Level 3-5
            "Private Pilot",
            "Private Pilot",
            "Instrument Student",      // Level 6-9
            "Instrument Student",
            "Instrument Student",
            "Instrument Student",
            "Instrument Rated",        // Level 10-14
            "Instrument Rated",
            "Instrument Rated",
            "Instrument Rated",
            "Instrument Rated",
            "Commercial Pilot",        // Level 15-19
            "Commercial Pilot",
            "Commercial Pilot",
            "Commercial Pilot",
            "Commercial Pilot",
            "ATP",                     // Level 20+
        };

        private GameObject dashboardPanel;
        private Text levelText;
        private Text titleText;
        private Text xpText;
        private Slider xpBar;
        private Text statsText;
        private Text dailyChallengeText;
        private Transform badgeContainer;
        private List<GameObject> badges = new List<GameObject>();

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
            CreateDashboardUI();

            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.OnXPGained += OnXPGained;
                PlayerProgress.Instance.OnCertificationEarned += OnCertificationEarned;
            }

            RefreshDashboard();
        }

        private void OnDestroy()
        {
            if (PlayerProgress.Instance != null)
            {
                PlayerProgress.Instance.OnXPGained -= OnXPGained;
                PlayerProgress.Instance.OnCertificationEarned -= OnCertificationEarned;
            }
        }

        private void CreateDashboardUI()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Find or create mission select overlay to add dashboard to
            var overlay = GameObject.Find("MissionSelectOverlay");
            if (overlay == null) return;

            // Dashboard panel (top of mission select)
            dashboardPanel = CreatePanel("ProgressionDashboard", overlay.transform, new Color(0.08f, 0.08f, 0.12f));
            var rect = dashboardPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.7f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // === Left Section: Level & XP ===
            var levelSection = CreatePanel("LevelSection", dashboardPanel.transform, Color.clear);
            var levelRect = levelSection.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0);
            levelRect.anchorMax = new Vector2(0.25f, 1);
            levelRect.offsetMin = new Vector2(10, 10);
            levelRect.offsetMax = new Vector2(-5, -10);

            // Level number (big)
            levelText = CreateText("LevelNum", levelSection.transform, "1", 72,
                new Vector2(0.5f, 0.7f), TextAnchor.MiddleCenter, Color.white);

            // Title
            titleText = CreateText("Title", levelSection.transform, "Student Pilot", 18,
                new Vector2(0.5f, 0.35f), TextAnchor.MiddleCenter, new Color(0.7f, 0.85f, 1f));

            // XP Bar
            CreateXPBar(levelSection.transform);

            // XP Text
            xpText = CreateText("XPText", levelSection.transform, "0 / 100 XP", 14,
                new Vector2(0.5f, 0.08f), TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f));

            // === Middle Section: Certifications ===
            var certSection = CreatePanel("CertSection", dashboardPanel.transform, new Color(0.06f, 0.06f, 0.08f));
            var certRect = certSection.GetComponent<RectTransform>();
            certRect.anchorMin = new Vector2(0.26f, 0);
            certRect.anchorMax = new Vector2(0.64f, 1);
            certRect.offsetMin = new Vector2(5, 10);
            certRect.offsetMax = new Vector2(-5, -10);

            CreateText("CertLabel", certSection.transform, "CERTIFICATIONS", 14,
                new Vector2(0.5f, 0.95f), TextAnchor.UpperCenter, new Color(0.5f, 0.5f, 0.5f));

            // Badge container (grid)
            var badgePanel = CreatePanel("BadgeContainer", certSection.transform, Color.clear);
            var badgeRect = badgePanel.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.02f, 0.05f);
            badgeRect.anchorMax = new Vector2(0.98f, 0.85f);
            badgeRect.offsetMin = Vector2.zero;
            badgeRect.offsetMax = Vector2.zero;
            badgeContainer = badgeRect;

            CreateCertificationBadges();

            // === Right Section: Stats & Daily ===
            var statsSection = CreatePanel("StatsSection", dashboardPanel.transform, Color.clear);
            var statsRect = statsSection.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.65f, 0);
            statsRect.anchorMax = new Vector2(1, 1);
            statsRect.offsetMin = new Vector2(5, 10);
            statsRect.offsetMax = new Vector2(-10, -10);

            // Stats
            CreateText("StatsLabel", statsSection.transform, "PILOT STATS", 14,
                new Vector2(0, 0.95f), TextAnchor.UpperLeft, new Color(0.5f, 0.5f, 0.5f));

            statsText = CreateText("Stats", statsSection.transform, "", 16,
                new Vector2(0, 0.5f), TextAnchor.MiddleLeft, Color.white);
            var statsTextRect = statsText.GetComponent<RectTransform>();
            statsTextRect.anchorMin = new Vector2(0, 0.4f);
            statsTextRect.anchorMax = new Vector2(1, 0.9f);
            statsTextRect.offsetMin = Vector2.zero;
            statsTextRect.offsetMax = Vector2.zero;

            // Daily Challenge
            CreateText("DailyLabel", statsSection.transform, "DAILY CHALLENGE", 14,
                new Vector2(0, 0.35f), TextAnchor.UpperLeft, new Color(1f, 0.8f, 0.3f));

            dailyChallengeText = CreateText("Daily", statsSection.transform, "", 14,
                new Vector2(0, 0.05f), TextAnchor.LowerLeft, new Color(0.8f, 0.8f, 0.8f));
            var dailyRect = dailyChallengeText.GetComponent<RectTransform>();
            dailyRect.anchorMin = new Vector2(0, 0.05f);
            dailyRect.anchorMax = new Vector2(1, 0.3f);
            dailyRect.offsetMin = Vector2.zero;
            dailyRect.offsetMax = Vector2.zero;
        }

        private void CreateXPBar(Transform parent)
        {
            var barObj = new GameObject("XPBar");
            barObj.transform.SetParent(parent, false);

            var rect = barObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.15f);
            rect.anchorMax = new Vector2(0.9f, 0.25f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background
            var bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f);

            xpBar = barObj.AddComponent<Slider>();
            xpBar.interactable = false;
            xpBar.minValue = 0;
            xpBar.maxValue = 1;

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(barObj.transform, false);
            var fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRectT = fill.AddComponent<RectTransform>();
            fillRectT.anchorMin = Vector2.zero;
            fillRectT.anchorMax = Vector2.one;
            fillRectT.offsetMin = Vector2.zero;
            fillRectT.offsetMax = Vector2.zero;

            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f);

            xpBar.fillRect = fillRectT;
        }

        private void CreateCertificationBadges()
        {
            if (badgeContainer == null) return;

            // Clear existing
            foreach (var badge in badges)
            {
                if (badge != null) Destroy(badge);
            }
            badges.Clear();

            var allCerts = Certification.AllCertifications;
            var earned = PlayerProgress.Instance?.EarnedCertifications ?? new List<Certification>();

            int cols = 5;
            int rows = 3;
            float badgeSize = 0.18f;
            float gapX = 0.02f;
            float gapY = 0.05f;

            for (int i = 0; i < Mathf.Min(allCerts.Count, cols * rows); i++)
            {
                int col = i % cols;
                int row = i / cols;

                var cert = allCerts[i];
                bool isEarned = earned.Contains(cert);

                var badge = CreateBadge(cert, isEarned,
                    col * (badgeSize + gapX) + gapX,
                    1f - (row + 1) * (0.3f + gapY),
                    badgeSize);

                badges.Add(badge);
            }
        }

        private GameObject CreateBadge(Certification cert, bool earned, float x, float y, float size)
        {
            var badge = CreatePanel("Badge_" + cert.Id, badgeContainer, GetTierColor(cert.Tier, earned));
            var rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x + size, y + 0.28f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Icon/Initial
            string initial = cert.Name.Length > 0 ? cert.Name.Substring(0, 1) : "?";
            var iconText = CreateText("Icon", badge.transform, initial, 20,
                new Vector2(0.5f, 0.6f), TextAnchor.MiddleCenter,
                earned ? Color.white : new Color(0.4f, 0.4f, 0.4f));

            // Name (truncated)
            string shortName = cert.Name.Length > 8 ? cert.Name.Substring(0, 7) + "." : cert.Name;
            var nameText = CreateText("Name", badge.transform, shortName, 10,
                new Vector2(0.5f, 0.15f), TextAnchor.MiddleCenter,
                earned ? Color.white : new Color(0.4f, 0.4f, 0.4f));

            // Lock overlay if not earned
            if (!earned)
            {
                var lockOverlay = CreatePanel("Lock", badge.transform, new Color(0, 0, 0, 0.5f));
                var lockRect = lockOverlay.GetComponent<RectTransform>();
                lockRect.anchorMin = Vector2.zero;
                lockRect.anchorMax = Vector2.one;
                lockRect.offsetMin = Vector2.zero;
                lockRect.offsetMax = Vector2.zero;

                CreateText("LockIcon", lockOverlay.transform, "?", 24,
                    new Vector2(0.5f, 0.5f), TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f));
            }

            return badge;
        }

        private Color GetTierColor(CertificationTier tier, bool earned)
        {
            if (!earned) return new Color(0.15f, 0.15f, 0.15f);

            return tier switch
            {
                CertificationTier.Bronze => new Color(0.6f, 0.4f, 0.2f),
                CertificationTier.Silver => new Color(0.6f, 0.6f, 0.65f),
                CertificationTier.Gold => new Color(0.8f, 0.65f, 0.1f),
                CertificationTier.Platinum => new Color(0.7f, 0.75f, 0.85f),
                CertificationTier.Diamond => new Color(0.5f, 0.8f, 0.95f),
                _ => new Color(0.3f, 0.3f, 0.3f)
            };
        }

        public void RefreshDashboard()
        {
            if (PlayerProgress.Instance == null) return;

            var progress = PlayerProgress.Instance;
            int level = progress.Level;

            // Level
            levelText.text = level.ToString();

            // Title
            int titleIndex = Mathf.Clamp(level - 1, 0, LevelTitles.Length - 1);
            titleText.text = LevelTitles[titleIndex];

            // XP Bar
            xpBar.value = progress.LevelProgress;
            int currentLevelXP = progress.TotalXP - XPRequiredForLevel(level);
            int xpNeeded = XPRequiredForLevel(level + 1) - XPRequiredForLevel(level);
            xpText.text = $"{currentLevelXP} / {xpNeeded} XP";

            // Stats
            var stats = progress.Stats;
            string statsStr = "";
            statsStr += $"Missions: {stats.missionsCompleted}/{stats.missionsAttempted}\n";
            statsStr += $"Flight Time: {stats.totalFlightTimeSeconds / 60:F0} min\n";
            statsStr += $"Best Grade: {GetBestGrade(stats)}\n";
            statsStr += $"Daily Streak: {stats.currentDailyStreak}";
            statsText.text = statsStr;

            // Daily Challenge
            if (DailyChallengeManager.Instance != null)
            {
                var challenge = DailyChallengeManager.Instance.TodaysChallenge;
                if (challenge != null)
                {
                    string status = DailyChallengeManager.Instance.HasCompletedToday ? " ✓" : "";
                    dailyChallengeText.text = $"{challenge.Title}{status}\n+{challenge.BonusXP} XP";
                }
            }
            else
            {
                dailyChallengeText.text = "Complete any mission today!";
            }

            // Refresh badges
            CreateCertificationBadges();
        }

        private string GetBestGrade(PlayerStats stats)
        {
            if (stats.gradesS > 0) return "S+";
            if (stats.gradesA > 0) return "A";
            if (stats.gradesB > 0) return "B";
            if (stats.gradesC > 0) return "C";
            if (stats.gradesD > 0) return "D";
            return "-";
        }

        private int XPRequiredForLevel(int level)
        {
            // Same formula as PlayerProgress
            return 100 * level * level;
        }

        private void OnXPGained(int xp)
        {
            RefreshDashboard();
        }

        private void OnCertificationEarned(Certification cert)
        {
            RefreshDashboard();
        }

        // UI Helpers
        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private Text CreateText(string name, Transform parent, string text, int fontSize,
            Vector2 anchor, TextAnchor alignment, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(300, 50);

            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;

            return txt;
        }
    }
}
