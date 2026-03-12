using UnityEngine;
using UnityEngine.UI;
using IFRTrainer.Core;
using IFRTrainer.Mission;
using IFRTrainer.Navigation;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Real-time flight feedback system.
    /// Provides dopamine hits during flight with popups, sounds, and visual effects.
    /// </summary>
    public class FlightFeedback : MonoBehaviour
    {
        public static FlightFeedback Instance { get; private set; }

        [Header("Feedback Settings")]
        [SerializeField] private float radialCaptureThreshold = 2f; // degrees
        [SerializeField] private float captureHoldTime = 3f; // seconds to confirm capture
        [SerializeField] private float goodTrackingInterval = 30f; // seconds between "good tracking" messages
        [SerializeField] private float driftWarningThreshold = 5f; // degrees before warning

        [Header("Score Values")]
        [SerializeField] private int capturePoints = 50;
        [SerializeField] private int trackingBonusPoints = 25;
        [SerializeField] private int perfectInterceptBonus = 100;

        [Header("Audio")]
        [SerializeField] private AudioClip captureSound;
        [SerializeField] private AudioClip trackingSound;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip perfectSound;

        // State tracking
        private bool hasCaputuredRadial = false;
        private bool isTracking = false;
        private float timeOnRadial = 0f;
        private float timeSinceLastTrackingBonus = 0f;
        private float lastCDIDeflection = 0f;
        private int currentSessionScore = 0;

        // References
        private AudioSource audioSource;
        private MissionManager missionManager;
        private FeedbackPopup popup;
        private CelebrationEffects celebration;
        private Image screenFlash;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        private void Start()
        {
            missionManager = FindFirstObjectByType<MissionManager>();
            popup = FindFirstObjectByType<FeedbackPopup>();
            celebration = FindFirstObjectByType<CelebrationEffects>();

            if (missionManager != null)
            {
                missionManager.OnMissionStarted += OnMissionStarted;
                missionManager.OnMissionCompleted += OnMissionCompleted;
            }

            // Create screen flash overlay if needed
            CreateScreenFlash();
        }

        private void OnDestroy()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= OnMissionStarted;
                missionManager.OnMissionCompleted -= OnMissionCompleted;
            }
        }

        private void Update()
        {
            if (missionManager == null || missionManager.State != MissionState.InProgress)
                return;

            if (GameManager.Instance == null || !GameManager.Instance.IsInitialized)
                return;

            var indication = GameManager.Instance.CurrentVORIndication;
            if (!indication.IsValid)
                return;

            float cdiDeflection = Mathf.Abs(indication.CDIDeflection);

            CheckRadialCapture(cdiDeflection);
            CheckTracking(cdiDeflection);
            CheckDriftWarning(cdiDeflection);

            lastCDIDeflection = cdiDeflection;
        }

        private void CheckRadialCapture(float cdiDeflection)
        {
            if (hasCaputuredRadial)
                return;

            // CDI deflection is -1 to +1, where 0.2 = 1 dot = ~2 degrees
            float deviationDegrees = cdiDeflection * 10f; // Approximate conversion

            if (deviationDegrees <= radialCaptureThreshold)
            {
                timeOnRadial += Time.deltaTime;

                if (timeOnRadial >= captureHoldTime)
                {
                    TriggerRadialCapture();
                }
            }
            else
            {
                timeOnRadial = Mathf.Max(0, timeOnRadial - Time.deltaTime * 2f); // Decay faster
            }
        }

        private void TriggerRadialCapture()
        {
            hasCaputuredRadial = true;
            isTracking = true;
            currentSessionScore += capturePoints;

            // Check if it was a perfect intercept (very small deviation)
            float finalDeviation = Mathf.Abs(lastCDIDeflection) * 10f;
            bool isPerfect = finalDeviation < 0.5f;

            if (isPerfect)
            {
                currentSessionScore += perfectInterceptBonus;
                ShowFeedback("PERFECT INTERCEPT!", perfectInterceptBonus + capturePoints, FeedbackType.Perfect);
                PlaySound(perfectSound);
                FlashScreen(Color.yellow, 0.3f);
                celebration?.TriggerSmallCelebration();
            }
            else
            {
                ShowFeedback("Radial Captured!", capturePoints, FeedbackType.Success);
                PlaySound(captureSound);
                FlashScreen(Color.green, 0.2f);
            }

            Debug.Log($"FlightFeedback: Radial captured! Score: +{capturePoints}");
        }

        private void CheckTracking(float cdiDeflection)
        {
            if (!isTracking)
                return;

            float deviationDegrees = cdiDeflection * 10f;

            if (deviationDegrees <= radialCaptureThreshold)
            {
                timeSinceLastTrackingBonus += Time.deltaTime;

                if (timeSinceLastTrackingBonus >= goodTrackingInterval)
                {
                    TriggerTrackingBonus();
                    timeSinceLastTrackingBonus = 0f;
                }
            }
            else if (deviationDegrees > driftWarningThreshold)
            {
                // Lost tracking
                isTracking = false;
                timeSinceLastTrackingBonus = 0f;
            }
        }

        private void TriggerTrackingBonus()
        {
            currentSessionScore += trackingBonusPoints;
            ShowFeedback("Good Tracking!", trackingBonusPoints, FeedbackType.Tracking);
            PlaySound(trackingSound);

            Debug.Log($"FlightFeedback: Tracking bonus! Score: +{trackingBonusPoints}");
        }

        private void CheckDriftWarning(float cdiDeflection)
        {
            float deviationDegrees = cdiDeflection * 10f;
            float lastDeviationDegrees = lastCDIDeflection * 10f;

            // Warn when crossing the threshold
            if (lastDeviationDegrees < driftWarningThreshold && deviationDegrees >= driftWarningThreshold)
            {
                ShowFeedback("Drifting off course!", 0, FeedbackType.Warning);
                PlaySound(warningSound);
                FlashScreen(new Color(1f, 0.5f, 0f, 0.3f), 0.15f); // Amber flash
            }
        }

        private void OnMissionStarted(MissionData mission)
        {
            // Reset state
            hasCaputuredRadial = false;
            isTracking = false;
            timeOnRadial = 0f;
            timeSinceLastTrackingBonus = 0f;
            currentSessionScore = 0;

            ShowFeedback($"Mission: {mission.title}", 0, FeedbackType.Info);
        }

        private void OnMissionCompleted(MissionData mission, MissionResult result)
        {
            // Big celebration based on grade
            switch (result.Grade)
            {
                case "S+":
                    ShowFeedback("EXCEPTIONAL PILOT!", result.TotalScore, FeedbackType.Perfect);
                    PlaySound(perfectSound);
                    celebration?.TriggerBigCelebration();
                    break;
                case "A":
                    ShowFeedback("Excellent Work!", result.TotalScore, FeedbackType.Success);
                    PlaySound(successSound);
                    celebration?.TriggerMediumCelebration();
                    break;
                case "B":
                    ShowFeedback("Good Job!", result.TotalScore, FeedbackType.Success);
                    PlaySound(successSound);
                    celebration?.TriggerSmallCelebration();
                    break;
                case "C":
                    ShowFeedback("Mission Complete", result.TotalScore, FeedbackType.Info);
                    PlaySound(successSound);
                    break;
                default:
                    ShowFeedback("Keep Practicing!", result.TotalScore, FeedbackType.Warning);
                    break;
            }
        }

        private void ShowFeedback(string message, int points, FeedbackType type)
        {
            if (popup != null)
            {
                popup.Show(message, points, type);
            }
            else
            {
                // Fallback to debug log
                Debug.Log($"[FEEDBACK] {message} {(points > 0 ? $"+{points}" : "")}");
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void CreateScreenFlash()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var flashObj = new GameObject("ScreenFlash");
            flashObj.transform.SetParent(canvas.transform, false);

            var rect = flashObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            screenFlash = flashObj.AddComponent<Image>();
            screenFlash.color = Color.clear;
            screenFlash.raycastTarget = false;

            // Make sure it's on top
            flashObj.transform.SetAsLastSibling();
        }

        private void FlashScreen(Color color, float duration)
        {
            if (screenFlash != null)
            {
                StartCoroutine(DoScreenFlash(color, duration));
            }
        }

        private System.Collections.IEnumerator DoScreenFlash(Color color, float duration)
        {
            screenFlash.color = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
                screenFlash.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            screenFlash.color = Color.clear;
        }

        // Public methods for external triggers
        public void TriggerCustomFeedback(string message, int points, FeedbackType type)
        {
            ShowFeedback(message, points, type);
        }

        public int GetSessionScore() => currentSessionScore;
    }

    public enum FeedbackType
    {
        Info,
        Success,
        Perfect,
        Warning,
        Tracking
    }
}
