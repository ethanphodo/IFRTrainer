using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IFRTrainer.Core;
using IFRTrainer.Navigation;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Provides hints and feedback to help players learn VOR navigation.
    /// </summary>
    public class HintSystem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private Image hintIcon;
        [SerializeField] private Button dismissButton;

        [Header("Settings")]
        [SerializeField] private bool hintsEnabled = true;
        [SerializeField] private float hintDisplayDuration = 5f;
        [SerializeField] private float hintCooldown = 10f;
        [SerializeField] private float deviationWarningThreshold = 0.7f; // CDI deflection

        [Header("Colors")]
        [SerializeField] private Color infoColor = Color.cyan;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color correctColor = Color.green;

        private MissionManager missionManager;
        private VORReceiver vorReceiver;
        private float lastHintTime;
        private Coroutine hideHintCoroutine;

        private void Awake()
        {
            missionManager = FindFirstObjectByType<MissionManager>();
            vorReceiver = FindFirstObjectByType<VORReceiver>();
        }

        private void Start()
        {
            dismissButton?.onClick.AddListener(HideHint);
            HideHint();
        }

        private void OnEnable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted += OnMissionStarted;
                missionManager.OnObjectiveUpdated += OnObjectiveUpdated;
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= OnMissionStarted;
                missionManager.OnObjectiveUpdated -= OnObjectiveUpdated;
            }
        }

        private void Update()
        {
            if (!hintsEnabled || missionManager?.State != MissionState.InProgress)
                return;

            CheckForHints();
        }

        private void CheckForHints()
        {
            if (Time.time - lastHintTime < hintCooldown)
                return;

            var indication = GameManager.Instance?.CurrentVORIndication;
            if (indication == null || !indication.Value.IsValid)
                return;

            var ind = indication.Value;

            // Check for large CDI deviation
            if (Mathf.Abs(ind.CDIDeflection) > deviationWarningThreshold)
            {
                ShowDeviationHint(ind);
            }
        }

        private void ShowDeviationHint(VORIndication indication)
        {
            string direction = indication.CDIDeflection > 0 ? "right" : "left";
            float correction = RadialCalculator.SuggestHeadingCorrection(
                indication.CDIDeflection,
                indication.ToFromFlag == ToFromFlag.To
            );

            string hint;
            if (indication.ToFromFlag == ToFromFlag.To)
            {
                hint = $"You're off course! Turn {direction} by about {Mathf.Abs(correction):F0}° to intercept the radial.";
            }
            else
            {
                hint = $"Flying outbound: The needle shows you need to fly {direction} to get back on the radial.";
            }

            ShowHint(hint, warningColor);
        }

        private void OnMissionStarted(MissionData mission)
        {
            // Show initial hint
            if (mission.objectives.Length > 0)
            {
                var firstObj = mission.objectives[0];
                if (firstObj.type == "intercept_radial")
                {
                    ShowHint(
                        $"Set your OBS to {firstObj.radial:000}°, then watch the CDI needle. " +
                        "Fly toward the needle to intercept the radial.",
                        infoColor
                    );
                }
            }
        }

        private void OnObjectiveUpdated(string objective)
        {
            // Provide context hint for new objectives
            if (objective.Contains("Track radial"))
            {
                ShowHint(
                    "Now tracking the radial. Keep the CDI centered. " +
                    "Small heading corrections work best.",
                    infoColor
                );
            }
        }

        public void ShowHint(string message, Color color)
        {
            if (!hintsEnabled)
                return;

            lastHintTime = Time.time;

            if (hintPanel != null)
            {
                hintPanel.SetActive(true);
            }

            if (hintText != null)
            {
                hintText.text = message;
                hintText.color = color;
            }

            if (hintIcon != null)
            {
                hintIcon.color = color;
            }

            // Auto-hide after duration
            if (hideHintCoroutine != null)
            {
                StopCoroutine(hideHintCoroutine);
            }
            hideHintCoroutine = StartCoroutine(HideHintAfterDelay());
        }

        private IEnumerator HideHintAfterDelay()
        {
            yield return new WaitForSeconds(hintDisplayDuration);
            HideHint();
        }

        public void HideHint()
        {
            hintPanel?.SetActive(false);

            if (hideHintCoroutine != null)
            {
                StopCoroutine(hideHintCoroutine);
                hideHintCoroutine = null;
            }
        }

        public void SetHintsEnabled(bool enabled)
        {
            hintsEnabled = enabled;

            if (!enabled)
            {
                HideHint();
            }
        }

        public void ToggleHints()
        {
            SetHintsEnabled(!hintsEnabled);
        }

        // Manual hint triggers for specific scenarios
        public void ShowInterceptHint(float targetRadial, bool inbound)
        {
            var pos = GameManager.Instance?.CurrentPosition;
            var dynamics = GameManager.Instance?.CurrentDynamics;

            if (!pos.HasValue || !dynamics.HasValue)
                return;

            float currentRadial = vorReceiver?.GetCurrentRadial() ?? 0f;
            float heading = RadialCalculator.CalculateInterceptHeading(
                currentRadial, targetRadial, inbound, 45f
            );

            ShowHint(
                $"To intercept the {targetRadial:000}° radial {(inbound ? "inbound" : "outbound")}, " +
                $"turn to heading {heading:000}°.",
                infoColor
            );
        }

        public void ShowTrackingHint()
        {
            ShowHint(
                "Tip: When tracking a radial, make small corrections. " +
                "A 5-10° heading change is usually enough to recenter the CDI.",
                infoColor
            );
        }

        public void ShowToFromHint()
        {
            var indication = GameManager.Instance?.CurrentVORIndication;
            if (indication == null || !indication.Value.IsValid)
                return;

            string explanation;
            if (indication.Value.ToFromFlag == ToFromFlag.To)
            {
                explanation = "TO flag: Flying this heading will take you toward the VOR station.";
            }
            else
            {
                explanation = "FROM flag: You're flying away from the VOR station on this radial.";
            }

            ShowHint(explanation, infoColor);
        }
    }
}
