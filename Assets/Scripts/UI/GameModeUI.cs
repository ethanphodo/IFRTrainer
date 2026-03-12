using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IFRTrainer.Core;
using IFRTrainer.Mission;
using IFRTrainer.Progression;

namespace IFRTrainer.UI
{
    /// <summary>
    /// UI for selecting and displaying game modes.
    /// </summary>
    public class GameModeUI : MonoBehaviour
    {
        [Header("Mode Selection")]
        [SerializeField] private Button trainingButton;
        [SerializeField] private Button standardButton;
        [SerializeField] private Button realisticButton;
        [SerializeField] private Button timeAttackButton;
        [SerializeField] private Button endlessButton;

        [Header("Mode Info")]
        [SerializeField] private TextMeshProUGUI modeNameText;
        [SerializeField] private TextMeshProUGUI modeDescriptionText;
        [SerializeField] private TextMeshProUGUI modeSettingsText;

        [Header("Endless Mode UI")]
        [SerializeField] private GameObject endlessModePanel;
        [SerializeField] private TextMeshProUGUI endlessWaveText;
        [SerializeField] private TextMeshProUGUI endlessScoreText;
        [SerializeField] private TextMeshProUGUI endlessHighScoreText;
        [SerializeField] private Button endlessStartButton;
        [SerializeField] private Button endlessNextWaveButton;

        [Header("Button Highlights")]
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lockedColor = Color.gray;

        private Button[] modeButtons;

        private void Start()
        {
            modeButtons = new[] { trainingButton, standardButton, realisticButton, timeAttackButton, endlessButton };

            SetupButtons();
            CheckUnlocks();
            UpdateModeInfo(GameMode.Training);
        }

        private void OnEnable()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.OnModeChanged += OnModeChanged;
            }
        }

        private void OnDisable()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.OnModeChanged -= OnModeChanged;
            }
        }

        private void SetupButtons()
        {
            trainingButton?.onClick.AddListener(() => SelectMode(GameMode.Training));
            standardButton?.onClick.AddListener(() => SelectMode(GameMode.Standard));
            realisticButton?.onClick.AddListener(() => SelectMode(GameMode.Realistic));
            timeAttackButton?.onClick.AddListener(() => SelectMode(GameMode.TimeAttack));
            endlessButton?.onClick.AddListener(() => SelectMode(GameMode.Endless));

            endlessStartButton?.onClick.AddListener(StartEndlessMode);
            endlessNextWaveButton?.onClick.AddListener(AdvanceEndlessWave);
        }

        private void CheckUnlocks()
        {
            // Some modes require progression to unlock
            var progress = PlayerProgress.Instance;

            // Training always available
            SetButtonState(trainingButton, true);

            // Standard unlocks at level 2
            bool standardUnlocked = progress == null || progress.Level >= 2;
            SetButtonState(standardButton, standardUnlocked);

            // Realistic unlocks at level 5
            bool realisticUnlocked = progress == null || progress.Level >= 5;
            SetButtonState(realisticButton, realisticUnlocked);

            // Time Attack unlocks after 5 completed missions
            bool timeAttackUnlocked = progress == null || progress.Stats.missionsCompleted >= 5;
            SetButtonState(timeAttackButton, timeAttackUnlocked);

            // Endless unlocks at level 10
            bool endlessUnlocked = progress == null || progress.Level >= 10;
            SetButtonState(endlessButton, endlessUnlocked);
        }

        private void SetButtonState(Button button, bool unlocked)
        {
            if (button == null) return;

            button.interactable = unlocked;

            var colors = button.colors;
            colors.normalColor = unlocked ? normalColor : lockedColor;
            button.colors = colors;

            // Add lock icon or text if locked
            var lockText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (lockText != null && !unlocked)
            {
                lockText.text += " (Locked)";
            }
        }

        public void SelectMode(GameMode mode)
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SetGameMode(mode);
            }

            UpdateModeInfo(mode);
            UpdateButtonHighlights(mode);

            // Show endless panel if endless mode selected
            if (endlessModePanel != null)
            {
                endlessModePanel.SetActive(mode == GameMode.Endless);
            }
        }

        private void OnModeChanged(GameMode mode)
        {
            UpdateModeInfo(mode);
            UpdateButtonHighlights(mode);
        }

        private void UpdateModeInfo(GameMode mode)
        {
            var settings = GameModeManager.GetSettingsForMode(mode);

            if (modeNameText != null)
                modeNameText.text = settings.ModeName;

            if (modeDescriptionText != null)
                modeDescriptionText.text = settings.Description;

            if (modeSettingsText != null)
            {
                string settingsInfo = "";
                settingsInfo += settings.HintsEnabled ? "Hints: ON\n" : "Hints: OFF\n";
                settingsInfo += $"Intercept Tolerance: ±{settings.InterceptToleranceDeg:F1}°\n";
                settingsInfo += $"Tracking Tolerance: ±{settings.TrackingToleranceDeg:F1}°\n";
                settingsInfo += settings.WindEnabled ? "Wind: ON\n" : "Wind: OFF\n";
                settingsInfo += $"Score Multiplier: {settings.ScoreMultiplier:F2}x\n";

                if (settings.TimeLimit > 0)
                    settingsInfo += $"Time Limit: {settings.TimeLimit}s\n";

                if (!settings.AllowPause)
                    settingsInfo += "Pause: DISABLED\n";

                modeSettingsText.text = settingsInfo;
            }
        }

        private void UpdateButtonHighlights(GameMode selectedMode)
        {
            var modeToButton = new System.Collections.Generic.Dictionary<GameMode, Button>
            {
                { GameMode.Training, trainingButton },
                { GameMode.Standard, standardButton },
                { GameMode.Realistic, realisticButton },
                { GameMode.TimeAttack, timeAttackButton },
                { GameMode.Endless, endlessButton }
            };

            foreach (var kvp in modeToButton)
            {
                if (kvp.Value == null) continue;

                var colors = kvp.Value.colors;
                colors.normalColor = kvp.Key == selectedMode ? selectedColor : normalColor;
                kvp.Value.colors = colors;
            }
        }

        // Endless mode methods
        private void StartEndlessMode()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.StartEndlessMode();
                UpdateEndlessUI();

                // Start first wave
                var mission = GameModeManager.Instance.GenerateEndlessWaveMission();
                var missionManager = FindFirstObjectByType<MissionManager>();
                if (missionManager != null)
                {
                    missionManager.LoadMissionFromJson(JsonUtility.ToJson(mission));
                    missionManager.StartMission();
                }
            }
        }

        private void AdvanceEndlessWave()
        {
            if (GameModeManager.Instance != null && GameModeManager.Instance.IsEndlessMode)
            {
                GameModeManager.Instance.AdvanceEndlessWave();
                UpdateEndlessUI();

                // Start next wave
                var mission = GameModeManager.Instance.GenerateEndlessWaveMission();
                var missionManager = FindFirstObjectByType<MissionManager>();
                if (missionManager != null)
                {
                    missionManager.LoadMissionFromJson(JsonUtility.ToJson(mission));
                    missionManager.StartMission();
                }
            }
        }

        private void UpdateEndlessUI()
        {
            if (GameModeManager.Instance == null)
                return;

            if (endlessWaveText != null)
                endlessWaveText.text = $"Wave {GameModeManager.Instance.EndlessWave}";

            if (endlessScoreText != null)
                endlessScoreText.text = $"Score: {GameModeManager.Instance.EndlessScore}";

            // Load high score from PlayerPrefs
            int highScore = PlayerPrefs.GetInt("EndlessHighScore", 0);
            if (endlessHighScoreText != null)
                endlessHighScoreText.text = $"High Score: {highScore}";
        }

        public void SaveEndlessHighScore()
        {
            if (GameModeManager.Instance == null)
                return;

            int currentScore = GameModeManager.Instance.EndlessScore;
            int highScore = PlayerPrefs.GetInt("EndlessHighScore", 0);

            if (currentScore > highScore)
            {
                PlayerPrefs.SetInt("EndlessHighScore", currentScore);
                PlayerPrefs.Save();
            }
        }
    }
}
