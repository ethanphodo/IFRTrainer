using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IFRTrainer.Core;
using IFRTrainer.Instruments;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Main instrument panel UI controller.
    /// Manages instrument displays and heading selection controls.
    /// </summary>
    public class InstrumentPanel : MonoBehaviour
    {
        [Header("Instrument References")]
        [SerializeField] private VORIndicator vorIndicator;
        [SerializeField] private HeadingIndicator headingIndicator;

        [Header("Readout Displays")]
        [SerializeField] private TextMeshProUGUI airspeedText;
        [SerializeField] private TextMeshProUGUI altitudeText;
        [SerializeField] private TextMeshProUGUI verticalSpeedText;
        [SerializeField] private TextMeshProUGUI headingText;

        [Header("Mission Info")]
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timeText;

        [Header("Heading Selection Buttons")]
        [SerializeField] private Button[] headingButtons;
        [SerializeField] private float[] headingIncrements = { 30f, 60f, 90f, 120f };

        [Header("Control Buttons")]
        [SerializeField] private Button turnLeft10Button;
        [SerializeField] private Button turnRight10Button;
        [SerializeField] private Button turnLeft30Button;
        [SerializeField] private Button turnRight30Button;
        [SerializeField] private Button obsUpButton;
        [SerializeField] private Button obsDownButton;

        public event Action<float> OnHeadingSelected;

        private MissionManager missionManager;
        private ScoreManager scoreManager;

        private void Awake()
        {
            missionManager = FindFirstObjectByType<MissionManager>();
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        private void Start()
        {
            SetupButtons();
        }

        private void OnEnable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted += OnMissionStarted;
                missionManager.OnObjectiveUpdated += OnObjectiveUpdated;
                missionManager.OnMissionCompleted += OnMissionCompleted;
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= OnMissionStarted;
                missionManager.OnObjectiveUpdated -= OnObjectiveUpdated;
                missionManager.OnMissionCompleted -= OnMissionCompleted;
            }
        }

        private void Update()
        {
            UpdateInstrumentReadouts();
            UpdateMissionInfo();
        }

        private void SetupButtons()
        {
            // Setup heading increment buttons
            for (int i = 0; i < headingButtons.Length && i < headingIncrements.Length; i++)
            {
                float increment = headingIncrements[i];
                int index = i;
                headingButtons[i]?.onClick.AddListener(() => SelectHeadingIncrement(increment));
            }

            // Turn buttons
            turnLeft10Button?.onClick.AddListener(() => TurnAircraft(-10f));
            turnRight10Button?.onClick.AddListener(() => TurnAircraft(10f));
            turnLeft30Button?.onClick.AddListener(() => TurnAircraft(-30f));
            turnRight30Button?.onClick.AddListener(() => TurnAircraft(30f));

            // OBS buttons
            obsUpButton?.onClick.AddListener(() => vorIndicator?.IncrementOBS());
            obsDownButton?.onClick.AddListener(() => vorIndicator?.DecrementOBS());
        }

        private void UpdateInstrumentReadouts()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsInitialized)
                return;

            var dynamics = GameManager.Instance.CurrentDynamics;
            var position = GameManager.Instance.CurrentPosition;

            // Airspeed
            if (airspeedText != null)
            {
                airspeedText.text = $"{dynamics.Speed:F0} KTS";
            }

            // Altitude
            if (altitudeText != null)
            {
                altitudeText.text = $"{position.Altitude:F0} FT";
            }

            // Vertical Speed
            if (verticalSpeedText != null)
            {
                string vsSign = dynamics.VerticalSpeed >= 0 ? "+" : "";
                verticalSpeedText.text = $"{vsSign}{dynamics.VerticalSpeed:F0} FPM";
            }

            // Heading
            if (headingText != null)
            {
                headingText.text = $"{dynamics.Heading:000}°";
            }
        }

        private void UpdateMissionInfo()
        {
            if (missionManager == null)
                return;

            // Score
            if (scoreText != null && scoreManager != null)
            {
                int projected = scoreManager.GetProjectedScore();
                scoreText.text = $"Score: {projected}";
            }

            // Time
            if (timeText != null && missionManager.State == MissionState.InProgress)
            {
                float time = missionManager.MissionTime;
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }

        private void OnMissionStarted(MissionData mission)
        {
            if (missionTitleText != null)
            {
                missionTitleText.text = mission.title;
            }

            if (objectiveText != null)
            {
                objectiveText.text = missionManager.GetCurrentObjectiveText();
            }
        }

        private void OnObjectiveUpdated(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }

        private void OnMissionCompleted(MissionData mission, MissionResult result)
        {
            if (objectiveText != null)
            {
                objectiveText.text = result.Success ?
                    $"Mission Complete! Grade: {result.Grade}" :
                    "Mission Failed";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Final Score: {result.TotalScore}";
            }
        }

        public void SelectHeadingIncrement(float increment)
        {
            if (!GameManager.Instance)
                return;

            float currentHeading = GameManager.Instance.CurrentDynamics.Heading;
            float newHeading = NormalizeHeading(currentHeading + increment);

            GameManager.Instance.SetTargetHeading(newHeading);
            OnHeadingSelected?.Invoke(newHeading);

            // Update heading indicator bug
            headingIndicator?.SetTargetHeading(newHeading);
        }

        public void SetAbsoluteHeading(float heading)
        {
            if (!GameManager.Instance)
                return;

            float normalizedHeading = NormalizeHeading(heading);
            GameManager.Instance.SetTargetHeading(normalizedHeading);
            OnHeadingSelected?.Invoke(normalizedHeading);

            headingIndicator?.SetTargetHeading(normalizedHeading);
        }

        public void TurnAircraft(float degrees)
        {
            if (!GameManager.Instance)
                return;

            float currentHeading = GameManager.Instance.CurrentDynamics.Heading;
            float newHeading = NormalizeHeading(currentHeading + degrees);

            GameManager.Instance.SetTargetHeading(newHeading);
            headingIndicator?.SetTargetHeading(newHeading);
        }

        private float NormalizeHeading(float heading)
        {
            while (heading < 0) heading += 360f;
            while (heading >= 360f) heading -= 360f;
            return heading;
        }

        // Quick heading buttons for common intercept headings
        public void SetHeading030() => SetAbsoluteHeading(30f);
        public void SetHeading060() => SetAbsoluteHeading(60f);
        public void SetHeading090() => SetAbsoluteHeading(90f);
        public void SetHeading120() => SetAbsoluteHeading(120f);
        public void SetHeading150() => SetAbsoluteHeading(150f);
        public void SetHeading180() => SetAbsoluteHeading(180f);
        public void SetHeading210() => SetAbsoluteHeading(210f);
        public void SetHeading240() => SetAbsoluteHeading(240f);
        public void SetHeading270() => SetAbsoluteHeading(270f);
        public void SetHeading300() => SetAbsoluteHeading(300f);
        public void SetHeading330() => SetAbsoluteHeading(330f);
        public void SetHeading360() => SetAbsoluteHeading(360f);
    }
}
