using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Mission selection and briefing UI.
    /// </summary>
    public class MissionSelect : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject missionSelectPanel;
        [SerializeField] private GameObject missionBriefingPanel;
        [SerializeField] private GameObject debriefingPanel;

        [Header("Mission List")]
        [SerializeField] private Transform missionListContainer;
        [SerializeField] private GameObject missionButtonPrefab;

        [Header("Briefing")]
        [SerializeField] private TextMeshProUGUI briefingTitle;
        [SerializeField] private TextMeshProUGUI briefingDescription;
        [SerializeField] private TextMeshProUGUI briefingObjectives;
        [SerializeField] private Button startMissionButton;
        [SerializeField] private Button backButton;

        [Header("Debriefing")]
        [SerializeField] private TextMeshProUGUI debriefTitle;
        [SerializeField] private TextMeshProUGUI debriefGrade;
        [SerializeField] private TextMeshProUGUI debriefScore;
        [SerializeField] private TextMeshProUGUI debriefBreakdown;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;

        [Header("Available Missions")]
        [SerializeField] private string[] missionIds = {
            "vor_intercept_001",
            "vor_intercept_002",
            "vor_intercept_003"
        };

        private MissionManager missionManager;
        private ScoreManager scoreManager;
        private string selectedMissionId;

        private void Awake()
        {
            missionManager = FindFirstObjectByType<MissionManager>();
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        private void Start()
        {
            SetupButtons();
            PopulateMissionList();
            ShowMissionSelect();
        }

        private void OnEnable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionLoaded += OnMissionLoaded;
                missionManager.OnMissionCompleted += OnMissionCompleted;
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionLoaded -= OnMissionLoaded;
                missionManager.OnMissionCompleted -= OnMissionCompleted;
            }
        }

        private void SetupButtons()
        {
            startMissionButton?.onClick.AddListener(StartSelectedMission);
            backButton?.onClick.AddListener(ShowMissionSelect);
            continueButton?.onClick.AddListener(ShowMissionSelect);
            retryButton?.onClick.AddListener(RetryMission);
        }

        private void PopulateMissionList()
        {
            if (missionListContainer == null || missionButtonPrefab == null)
                return;

            // Clear existing buttons
            foreach (Transform child in missionListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create buttons for each mission
            foreach (string missionId in missionIds)
            {
                CreateMissionButton(missionId);
            }
        }

        private void CreateMissionButton(string missionId)
        {
            GameObject buttonObj = Instantiate(missionButtonPrefab, missionListContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                // Format mission ID for display
                string displayName = FormatMissionName(missionId);
                label.text = displayName;
            }

            button?.onClick.AddListener(() => SelectMission(missionId));
        }

        private string FormatMissionName(string missionId)
        {
            // Convert "vor_intercept_001" to "VOR Intercept 001"
            return missionId
                .Replace("_", " ")
                .Replace("vor", "VOR")
                .ToUpper();
        }

        public void SelectMission(string missionId)
        {
            selectedMissionId = missionId;
            missionManager?.LoadMission(missionId);
        }

        private void OnMissionLoaded(MissionData mission)
        {
            ShowMissionBriefing(mission);
        }

        private void ShowMissionBriefing(MissionData mission)
        {
            missionSelectPanel?.SetActive(false);
            missionBriefingPanel?.SetActive(true);
            debriefingPanel?.SetActive(false);

            if (briefingTitle != null)
            {
                briefingTitle.text = mission.title;
            }

            if (briefingDescription != null)
            {
                briefingDescription.text = GetMissionDescription(mission);
            }

            if (briefingObjectives != null)
            {
                briefingObjectives.text = GetObjectivesText(mission);
            }
        }

        private string GetMissionDescription(MissionData mission)
        {
            var ic = mission.initial_conditions.aircraft;
            return $"Starting Position:\n" +
                   $"  Altitude: {ic.altitude_feet} ft\n" +
                   $"  Heading: {ic.heading}°\n" +
                   $"  Speed: {ic.speed_knots} kts\n\n" +
                   $"Difficulty: {"★".PadRight(mission.difficulty, '★').PadRight(5, '☆')}";
        }

        private string GetObjectivesText(MissionData mission)
        {
            string text = "Objectives:\n";
            int num = 1;

            foreach (var obj in mission.objectives)
            {
                text += $"{num}. {GetObjectiveDescription(obj)}\n";
                num++;
            }

            return text;
        }

        private string GetObjectiveDescription(ObjectiveData obj)
        {
            switch (obj.type)
            {
                case "intercept_radial":
                    return $"Intercept {obj.station} {obj.radial:000}° radial (±{obj.max_deviation_deg}°)";
                case "track_radial":
                    return $"Track radial for {obj.duration_seconds}s";
                case "reach_distance":
                    return $"Fly to {obj.distance_nm} NM from station";
                default:
                    return obj.type;
            }
        }

        public void StartSelectedMission()
        {
            missionBriefingPanel?.SetActive(false);
            missionManager?.StartMission();
        }

        private void OnMissionCompleted(MissionData mission, MissionResult result)
        {
            ShowDebriefing(mission, result);
        }

        private void ShowDebriefing(MissionData mission, MissionResult result)
        {
            missionSelectPanel?.SetActive(false);
            missionBriefingPanel?.SetActive(false);
            debriefingPanel?.SetActive(true);

            if (debriefTitle != null)
            {
                debriefTitle.text = result.Success ? "Mission Complete!" : "Mission Failed";
            }

            if (debriefGrade != null)
            {
                debriefGrade.text = result.Grade;
                debriefGrade.color = GetGradeColor(result.Grade);
            }

            if (debriefScore != null)
            {
                debriefScore.text = $"{result.TotalScore} points";
            }

            if (debriefBreakdown != null && scoreManager != null)
            {
                debriefBreakdown.text = scoreManager.GetScoreBreakdown(result);
            }
        }

        private Color GetGradeColor(string grade)
        {
            switch (grade)
            {
                case "S+": return new Color(1f, 0.84f, 0f);  // Gold
                case "A": return Color.green;
                case "B": return Color.cyan;
                case "C": return Color.yellow;
                case "D": return new Color(1f, 0.5f, 0f);    // Orange
                default: return Color.red;
            }
        }

        public void ShowMissionSelect()
        {
            missionSelectPanel?.SetActive(true);
            missionBriefingPanel?.SetActive(false);
            debriefingPanel?.SetActive(false);
        }

        public void RetryMission()
        {
            if (!string.IsNullOrEmpty(selectedMissionId))
            {
                SelectMission(selectedMissionId);
                StartSelectedMission();
            }
        }
    }
}
