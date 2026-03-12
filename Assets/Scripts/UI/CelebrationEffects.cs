using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Visual celebration effects for achievements and high scores.
    /// Includes confetti particles, screen shake, and victory animations.
    /// </summary>
    public class CelebrationEffects : MonoBehaviour
    {
        [Header("Confetti Settings")]
        [SerializeField] private int smallConfettiCount = 20;
        [SerializeField] private int mediumConfettiCount = 50;
        [SerializeField] private int bigConfettiCount = 100;
        [SerializeField] private float confettiDuration = 3f;
        [SerializeField] private float confettiSpeed = 300f;
        [SerializeField] private float confettiSpread = 400f;

        [Header("Screen Shake")]
        [SerializeField] private float smallShakeAmount = 5f;
        [SerializeField] private float mediumShakeAmount = 10f;
        [SerializeField] private float bigShakeAmount = 15f;
        [SerializeField] private float shakeDuration = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color[] confettiColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),    // Red
            new Color(1f, 0.8f, 0.2f),    // Yellow
            new Color(0.2f, 1f, 0.2f),    // Green
            new Color(0.2f, 0.6f, 1f),    // Blue
            new Color(1f, 0.4f, 0.8f),    // Pink
            new Color(0.8f, 0.4f, 1f),    // Purple
            new Color(1f, 0.6f, 0.2f),    // Orange
            new Color(0.2f, 1f, 0.8f),    // Cyan
        };

        private Transform confettiContainer;
        private RectTransform canvasRect;
        private List<ConfettiPiece> activeConfetti = new List<ConfettiPiece>();
        private Vector2 originalCanvasPosition;
        private bool isShaking = false;

        private void Start()
        {
            CreateConfettiContainer();
        }

        private void CreateConfettiContainer()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            canvasRect = canvas.GetComponent<RectTransform>();
            originalCanvasPosition = canvasRect.anchoredPosition;

            var containerObj = new GameObject("ConfettiContainer");
            containerObj.transform.SetParent(canvas.transform, false);

            var rect = containerObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            confettiContainer = containerObj.transform;
        }

        private void Update()
        {
            UpdateConfetti();
        }

        public void TriggerSmallCelebration()
        {
            SpawnConfetti(smallConfettiCount);
            StartCoroutine(DoScreenShake(smallShakeAmount, shakeDuration * 0.5f));
        }

        public void TriggerMediumCelebration()
        {
            SpawnConfetti(mediumConfettiCount);
            StartCoroutine(DoScreenShake(mediumShakeAmount, shakeDuration));
        }

        public void TriggerBigCelebration()
        {
            SpawnConfetti(bigConfettiCount);
            StartCoroutine(DoScreenShake(bigShakeAmount, shakeDuration * 1.5f));
            StartCoroutine(SpawnConfettiWaves());
        }

        private IEnumerator SpawnConfettiWaves()
        {
            yield return new WaitForSeconds(0.5f);
            SpawnConfetti(bigConfettiCount / 2);
            yield return new WaitForSeconds(0.5f);
            SpawnConfetti(bigConfettiCount / 2);
        }

        private void SpawnConfetti(int count)
        {
            if (confettiContainer == null)
            {
                CreateConfettiContainer();
                if (confettiContainer == null) return;
            }

            for (int i = 0; i < count; i++)
            {
                SpawnConfettiPiece();
            }
        }

        private void SpawnConfettiPiece()
        {
            var confettiObj = new GameObject("Confetti");
            confettiObj.transform.SetParent(confettiContainer, false);

            var rect = confettiObj.AddComponent<RectTransform>();

            // Random starting position at top of screen
            float screenWidth = canvasRect != null ? canvasRect.rect.width : 1920;
            float screenHeight = canvasRect != null ? canvasRect.rect.height : 1080;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(
                Random.Range(-screenWidth / 2, screenWidth / 2),
                Random.Range(0, 100)
            );

            // Random size (rectangles for confetti look)
            float width = Random.Range(8f, 16f);
            float height = Random.Range(4f, 12f);
            rect.sizeDelta = new Vector2(width, height);

            // Random color
            var image = confettiObj.AddComponent<Image>();
            image.color = confettiColors[Random.Range(0, confettiColors.Length)];

            // Random rotation
            rect.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

            // Create piece data
            var piece = new ConfettiPiece
            {
                GameObject = confettiObj,
                Rect = rect,
                Velocity = new Vector2(
                    Random.Range(-confettiSpread, confettiSpread),
                    Random.Range(-confettiSpeed * 0.5f, -confettiSpeed)
                ),
                RotationSpeed = Random.Range(-360f, 360f),
                Lifetime = confettiDuration + Random.Range(-0.5f, 0.5f),
                Age = 0f,
                SwayPhase = Random.Range(0f, Mathf.PI * 2f),
                SwaySpeed = Random.Range(2f, 5f),
                SwayAmount = Random.Range(30f, 80f)
            };

            activeConfetti.Add(piece);
        }

        private void UpdateConfetti()
        {
            for (int i = activeConfetti.Count - 1; i >= 0; i--)
            {
                var piece = activeConfetti[i];
                piece.Age += Time.deltaTime;

                if (piece.Age >= piece.Lifetime || piece.GameObject == null)
                {
                    if (piece.GameObject != null)
                        Destroy(piece.GameObject);
                    activeConfetti.RemoveAt(i);
                    continue;
                }

                // Update position with gravity and sway
                piece.Velocity.y += 200f * Time.deltaTime; // Gravity (positive = down in UI coords)

                float sway = Mathf.Sin(piece.Age * piece.SwaySpeed + piece.SwayPhase) * piece.SwayAmount;
                Vector2 movement = piece.Velocity * Time.deltaTime;
                movement.x += sway * Time.deltaTime;

                piece.Rect.anchoredPosition += movement;

                // Update rotation
                float currentZ = piece.Rect.localEulerAngles.z;
                piece.Rect.localEulerAngles = new Vector3(0, 0, currentZ + piece.RotationSpeed * Time.deltaTime);

                // Fade out near end
                if (piece.Age > piece.Lifetime * 0.7f)
                {
                    float fadeProgress = (piece.Age - piece.Lifetime * 0.7f) / (piece.Lifetime * 0.3f);
                    var image = piece.GameObject.GetComponent<Image>();
                    if (image != null)
                    {
                        var color = image.color;
                        color.a = 1f - fadeProgress;
                        image.color = color;
                    }
                }
            }
        }

        private IEnumerator DoScreenShake(float amount, float duration)
        {
            if (isShaking || canvasRect == null)
                yield break;

            isShaking = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Decrease shake intensity over time
                float currentAmount = amount * (1f - progress);

                Vector2 offset = new Vector2(
                    Random.Range(-currentAmount, currentAmount),
                    Random.Range(-currentAmount, currentAmount)
                );

                canvasRect.anchoredPosition = originalCanvasPosition + offset;
                yield return null;
            }

            canvasRect.anchoredPosition = originalCanvasPosition;
            isShaking = false;
        }

        private class ConfettiPiece
        {
            public GameObject GameObject;
            public RectTransform Rect;
            public Vector2 Velocity;
            public float RotationSpeed;
            public float Lifetime;
            public float Age;
            public float SwayPhase;
            public float SwaySpeed;
            public float SwayAmount;
        }
    }
}
