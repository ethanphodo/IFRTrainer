using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Animated popup system for showing feedback messages and score bonuses.
    /// Popups float up and fade out for satisfying visual feedback.
    /// </summary>
    public class FeedbackPopup : MonoBehaviour
    {
        [Header("Popup Settings")]
        [SerializeField] private float popupDuration = 2f;
        [SerializeField] private float floatSpeed = 50f;
        [SerializeField] private float fadeStartTime = 1f;
        [SerializeField] private float scalePopAmount = 1.2f;
        [SerializeField] private float scalePopDuration = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color infoColor = Color.white;
        [SerializeField] private Color successColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color perfectColor = new Color(1f, 0.85f, 0f);
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0.2f);
        [SerializeField] private Color trackingColor = new Color(0.5f, 0.8f, 1f);

        [Header("Positions")]
        [SerializeField] private Vector2 mainPopupPosition = new Vector2(0, 100);
        [SerializeField] private Vector2 scorePopupPosition = new Vector2(200, 50);

        private Transform popupContainer;

        private void Awake()
        {
            CreatePopupContainer();
        }

        private void CreatePopupContainer()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var containerObj = new GameObject("PopupContainer");
            containerObj.transform.SetParent(canvas.transform, false);

            var rect = containerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            popupContainer = containerObj.transform;
        }

        public void Show(string message, int points, FeedbackType type)
        {
            if (popupContainer == null)
            {
                CreatePopupContainer();
                if (popupContainer == null)
                {
                    Debug.Log($"[POPUP] {message} +{points}");
                    return;
                }
            }

            // Create main message popup
            CreatePopup(message, GetColorForType(type), mainPopupPosition, GetFontSizeForType(type));

            // Create score popup if points > 0
            if (points > 0)
            {
                string scoreText = $"+{points}";
                Vector2 scorePos = mainPopupPosition + scorePopupPosition;
                CreatePopup(scoreText, perfectColor, scorePos, 32, true);
            }
        }

        private void CreatePopup(string text, Color color, Vector2 position, int fontSize, bool isScore = false)
        {
            var popupObj = new GameObject("Popup");
            popupObj.transform.SetParent(popupContainer, false);

            var rect = popupObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, 100);

            // Add outline/shadow for visibility
            var outline = popupObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            var textComp = popupObj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = fontSize;
            textComp.color = color;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComp.verticalOverflow = VerticalWrapMode.Overflow;

            // Add CanvasGroup for fading
            var canvasGroup = popupObj.AddComponent<CanvasGroup>();

            StartCoroutine(AnimatePopup(popupObj, rect, canvasGroup, isScore));
        }

        private IEnumerator AnimatePopup(GameObject obj, RectTransform rect, CanvasGroup canvasGroup, bool isScore)
        {
            Vector2 startPos = rect.anchoredPosition;
            Vector3 startScale = Vector3.one;

            // Scale pop animation
            float scaleElapsed = 0f;
            while (scaleElapsed < scalePopDuration)
            {
                scaleElapsed += Time.deltaTime;
                float t = scaleElapsed / scalePopDuration;

                // Pop out then back
                float scale;
                if (t < 0.5f)
                {
                    scale = Mathf.Lerp(1f, scalePopAmount, t * 2f);
                }
                else
                {
                    scale = Mathf.Lerp(scalePopAmount, 1f, (t - 0.5f) * 2f);
                }

                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            rect.localScale = Vector3.one;

            // Float up and fade
            float elapsed = 0f;
            while (elapsed < popupDuration)
            {
                elapsed += Time.deltaTime;

                // Float up
                float yOffset = floatSpeed * elapsed;
                rect.anchoredPosition = startPos + new Vector2(0, yOffset);

                // Fade out after fadeStartTime
                if (elapsed > fadeStartTime)
                {
                    float fadeProgress = (elapsed - fadeStartTime) / (popupDuration - fadeStartTime);
                    canvasGroup.alpha = 1f - fadeProgress;
                }

                yield return null;
            }

            Destroy(obj);
        }

        private Color GetColorForType(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.Info => infoColor,
                FeedbackType.Success => successColor,
                FeedbackType.Perfect => perfectColor,
                FeedbackType.Warning => warningColor,
                FeedbackType.Tracking => trackingColor,
                _ => infoColor
            };
        }

        private int GetFontSizeForType(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.Perfect => 48,
                FeedbackType.Success => 36,
                FeedbackType.Warning => 32,
                FeedbackType.Tracking => 28,
                _ => 28
            };
        }
    }
}
