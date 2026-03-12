using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IFRTrainer.Core;
using IFRTrainer.Navigation;

namespace IFRTrainer.Instruments
{
    /// <summary>
    /// VOR Course Deviation Indicator (CDI) with TO/FROM flag.
    /// Displays OBS setting, CDI needle, and TO/FROM indication.
    /// </summary>
    public class VORIndicator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform cdiNeedle;
        [SerializeField] private RectTransform obsRose;
        [SerializeField] private TextMeshProUGUI obsText;
        [SerializeField] private TextMeshProUGUI toFromText;
        [SerializeField] private TextMeshProUGUI frequencyText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Image toFlag;
        [SerializeField] private Image fromFlag;
        [SerializeField] private Image navFlag;

        [Header("Settings")]
        [SerializeField] private float cdiMaxDeflection = 50f; // pixels
        [SerializeField] private Color toColor = Color.white;
        [SerializeField] private Color fromColor = Color.white;
        [SerializeField] private Color flagOffColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color navInvalidColor = Color.red;

        [Header("Animation")]
        [SerializeField] private float needleSmoothTime = 0.1f;
        [SerializeField] private float roseSmoothTime = 0.2f;

        private VORReceiver vorReceiver;
        private float currentNeedlePosition;
        private float needleVelocity;
        private float currentRoseAngle;
        private float roseVelocity;

        private void Awake()
        {
            vorReceiver = FindFirstObjectByType<VORReceiver>();
        }

        private void OnEnable()
        {
            if (vorReceiver != null)
            {
                vorReceiver.OnIndicationUpdated += UpdateIndication;
                vorReceiver.OnOBSChanged += UpdateOBS;
                vorReceiver.OnFrequencyChanged += UpdateFrequency;
            }
        }

        private void OnDisable()
        {
            if (vorReceiver != null)
            {
                vorReceiver.OnIndicationUpdated -= UpdateIndication;
                vorReceiver.OnOBSChanged -= UpdateOBS;
                vorReceiver.OnFrequencyChanged -= UpdateFrequency;
            }
        }

        private void Start()
        {
            // Initialize display
            if (vorReceiver != null)
            {
                UpdateOBS(vorReceiver.SelectedRadial);
                UpdateFrequency(vorReceiver.TunedFrequency);
            }
        }

        private void Update()
        {
            // Smooth needle animation
            if (cdiNeedle != null)
            {
                Vector3 pos = cdiNeedle.anchoredPosition;
                pos.x = Mathf.SmoothDamp(pos.x, currentNeedlePosition, ref needleVelocity, needleSmoothTime);
                cdiNeedle.anchoredPosition = pos;
            }

            // Smooth OBS rose rotation
            if (obsRose != null)
            {
                float currentZ = obsRose.localEulerAngles.z;
                float targetZ = currentRoseAngle;

                // Handle wrap-around
                float diff = Mathf.DeltaAngle(currentZ, targetZ);
                float newZ = Mathf.SmoothDamp(currentZ, currentZ + diff, ref roseVelocity, roseSmoothTime);
                obsRose.localEulerAngles = new Vector3(0, 0, newZ);
            }
        }

        private void UpdateIndication(VORIndication indication)
        {
            // Update CDI needle target position
            currentNeedlePosition = indication.CDIDeflection * cdiMaxDeflection;

            // Update TO/FROM flags
            UpdateFlags(indication);

            // Update distance if DME available
            if (distanceText != null && indication.IsValid)
            {
                distanceText.text = $"{indication.DistanceNM:F1} NM";
            }
            else if (distanceText != null)
            {
                distanceText.text = "---";
            }
        }

        private void UpdateFlags(VORIndication indication)
        {
            if (!indication.IsValid)
            {
                // NAV flag (invalid signal)
                SetFlagState(toFlag, false, flagOffColor);
                SetFlagState(fromFlag, false, flagOffColor);
                SetFlagState(navFlag, true, navInvalidColor);

                if (toFromText != null)
                {
                    toFromText.text = "NAV";
                    toFromText.color = navInvalidColor;
                }
                return;
            }

            // Valid signal - hide NAV flag
            SetFlagState(navFlag, false, flagOffColor);

            switch (indication.ToFromFlag)
            {
                case ToFromFlag.To:
                    SetFlagState(toFlag, true, toColor);
                    SetFlagState(fromFlag, false, flagOffColor);
                    if (toFromText != null)
                    {
                        toFromText.text = "TO";
                        toFromText.color = toColor;
                    }
                    break;

                case ToFromFlag.From:
                    SetFlagState(toFlag, false, flagOffColor);
                    SetFlagState(fromFlag, true, fromColor);
                    if (toFromText != null)
                    {
                        toFromText.text = "FROM";
                        toFromText.color = fromColor;
                    }
                    break;

                default:
                    SetFlagState(toFlag, false, flagOffColor);
                    SetFlagState(fromFlag, false, flagOffColor);
                    if (toFromText != null)
                    {
                        toFromText.text = "---";
                        toFromText.color = flagOffColor;
                    }
                    break;
            }
        }

        private void SetFlagState(Image flag, bool active, Color color)
        {
            if (flag != null)
            {
                flag.color = color;
                flag.gameObject.SetActive(active);
            }
        }

        private void UpdateOBS(float radial)
        {
            // Update OBS text
            if (obsText != null)
            {
                obsText.text = $"OBS {radial:000}°";
            }

            // Update rose rotation (negative because Unity rotates counter-clockwise)
            currentRoseAngle = -radial;
        }

        private void UpdateFrequency(float frequency)
        {
            if (frequencyText != null)
            {
                frequencyText.text = $"{frequency:F2}";
            }
        }

        // Public methods for OBS control
        public void IncrementOBS()
        {
            vorReceiver?.IncrementOBS();
        }

        public void DecrementOBS()
        {
            vorReceiver?.DecrementOBS();
        }

        public void SetOBS(float radial)
        {
            vorReceiver?.SetOBS(radial);
        }

        public void CenterOBS()
        {
            vorReceiver?.SetOBSToCurrentRadial();
        }
    }
}
