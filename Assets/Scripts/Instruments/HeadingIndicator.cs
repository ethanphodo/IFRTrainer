using UnityEngine;
using UnityEngine.UI;
using IFRTrainer.Core;

namespace IFRTrainer.Instruments
{
    /// <summary>
    /// Heading Indicator (Directional Gyro) instrument display.
    /// Shows current heading with rotating compass card.
    /// </summary>
    public class HeadingIndicator : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform compassCard;
        [SerializeField] private RectTransform headingBug;
        [SerializeField] private Text headingText;
        [SerializeField] private Text targetHeadingText;

        [Header("Settings")]
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private bool showTargetHeading = true;

        private float currentRotation;
        private float rotationVelocity;
        private float targetHeading;
        private float currentHeading;

        private void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsInitialized)
                return;

            // Get current heading from simulation
            currentHeading = GameManager.Instance.CurrentDynamics.Heading;

            // Update heading text
            if (headingText != null)
            {
                headingText.text = $"{currentHeading:000}°";
            }

            // Smooth compass card rotation
            if (compassCard != null)
            {
                float targetRotation = currentHeading;
                float currentZ = compassCard.localEulerAngles.z;

                // Handle wrap-around smoothly
                float diff = Mathf.DeltaAngle(currentZ, targetRotation);
                float newZ = Mathf.SmoothDamp(currentZ, currentZ + diff, ref rotationVelocity, smoothTime);

                compassCard.localEulerAngles = new Vector3(0, 0, newZ);
            }

            // Update heading bug position (relative to compass card)
            UpdateHeadingBug();
        }

        private void UpdateHeadingBug()
        {
            if (headingBug == null)
                return;

            // Heading bug shows target heading relative to current compass position
            float bugAngle = targetHeading - currentHeading;

            // Normalize to -180 to 180
            while (bugAngle > 180f) bugAngle -= 360f;
            while (bugAngle < -180f) bugAngle += 360f;

            // Rotate bug relative to aircraft symbol (which is fixed at top)
            headingBug.localEulerAngles = new Vector3(0, 0, -bugAngle);

            // Update target heading text
            if (showTargetHeading && targetHeadingText != null)
            {
                targetHeadingText.text = $"HDG {targetHeading:000}°";
            }
        }

        public void SetTargetHeading(float heading)
        {
            targetHeading = NormalizeHeading(heading);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetTargetHeading(targetHeading);
            }
        }

        public void AdjustTargetHeading(float delta)
        {
            SetTargetHeading(targetHeading + delta);
        }

        public void SyncBugToCurrentHeading()
        {
            SetTargetHeading(currentHeading);
        }

        public float GetCurrentHeading()
        {
            return currentHeading;
        }

        public float GetTargetHeading()
        {
            return targetHeading;
        }

        private float NormalizeHeading(float heading)
        {
            while (heading < 0) heading += 360f;
            while (heading >= 360f) heading -= 360f;
            return heading;
        }
    }
}
