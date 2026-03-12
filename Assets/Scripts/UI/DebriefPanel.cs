using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using IFRTrainer.Core;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Post-mission debrief panel showing flight analysis, tips, and replay.
    /// </summary>
    public class DebriefPanel : MonoBehaviour
    {
        public static DebriefPanel Instance { get; private set; }

        [Header("Panel")]
        private GameObject debriefPanel;
        private CanvasGroup canvasGroup;

        [Header("Grade Display")]
        private Text gradeText;
        private Text scoreText;
        private Text titleText;

        [Header("Stats")]
        private Text statsText;

        [Header("Flight Trace")]
        private RectTransform traceContainer;
        private List<GameObject> traceLines = new List<GameObject>();

        [Header("Deviation Graph")]
        private RectTransform graphContainer;

        [Header("Tips")]
        private Text tipsText;

        [Header("Buttons")]
        private Button retryButton;
        private Button nextButton;
        private Button menuButton;

        private MissionManager missionManager;
        private FlightRecorder flightRecorder;
        private MissionData lastMission;
        private MissionResult lastResult;

        // IFR Tips database
        private static readonly string[] InterceptTips = new string[]
        {
            "For outbound intercepts, turn to the radial heading when the CDI starts moving toward center.",
            "Use a 30° intercept angle for efficient radial capture without overshooting.",
            "Watch the CDI rate of movement - fast movement means you're close to the radial.",
            "The TO/FROM flag tells you if you're heading toward or away from the station.",
            "Anticipate your turn - start rolling out 5-10° before reaching the desired heading."
        };

        private static readonly string[] TrackingTips = new string[]
        {
            "Use small corrections (5-10°) to track a radial - large corrections cause oscillation.",
            "If the CDI drifts, turn toward the needle. Fly 'toward the needle' to correct.",
            "Wind correction: if you're consistently drifting one way, crab into the wind.",
            "Bracketing: start with 10° correction, then halve it each time you cross the radial.",
            "Don't chase the CDI - make a correction and wait for the result before adjusting again."
        };

        private static readonly string[] GeneralTips = new string[]
        {
            "The CDI shows position, not direction. Full deflection means 10°+ off course.",
            "Station passage is indicated by the TO/FROM flag flip and full CDI deflection.",
            "VOR accuracy is ±1° at the station, but increases with distance.",
            "Always positively identify VOR stations by their Morse code identifier.",
            "Standard rate turns are 3°/second - a 30° turn takes 10 seconds."
        };

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
            missionManager = FindFirstObjectByType<MissionManager>();
            flightRecorder = FindFirstObjectByType<FlightRecorder>();

            if (missionManager != null)
            {
                missionManager.OnMissionCompleted += ShowDebrief;
            }

            CreateDebriefUI();
            HidePanel();
        }

        private void OnDestroy()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionCompleted -= ShowDebrief;
            }
        }

        private void CreateDebriefUI()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Main panel
            debriefPanel = CreatePanel("DebriefPanel", canvas.transform, new Color(0, 0, 0, 0.95f));
            var panelRect = debriefPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            canvasGroup = debriefPanel.AddComponent<CanvasGroup>();

            // Title
            titleText = CreateText("Title", debriefPanel.transform, "MISSION COMPLETE", 48,
                new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), TextAnchor.MiddleCenter);
            titleText.color = Color.white;

            // Grade (big, centered)
            gradeText = CreateText("Grade", debriefPanel.transform, "A", 120,
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), TextAnchor.MiddleCenter);
            gradeText.color = Color.green;

            // Score
            scoreText = CreateText("Score", debriefPanel.transform, "1250 Points", 32,
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), TextAnchor.MiddleCenter);
            scoreText.color = Color.white;

            // Left panel - Flight Trace
            var tracePanel = CreatePanel("TracePanel", debriefPanel.transform, new Color(0.05f, 0.1f, 0.05f));
            var traceRect = tracePanel.GetComponent<RectTransform>();
            traceRect.anchorMin = new Vector2(0.05f, 0.25f);
            traceRect.anchorMax = new Vector2(0.35f, 0.62f);
            traceRect.offsetMin = Vector2.zero;
            traceRect.offsetMax = Vector2.zero;

            CreateText("TraceLabel", tracePanel.transform, "FLIGHT PATH", 16,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAnchor.UpperCenter);

            traceContainer = tracePanel.GetComponent<RectTransform>();

            // Middle panel - Stats
            var statsPanel = CreatePanel("StatsPanel", debriefPanel.transform, new Color(0.1f, 0.1f, 0.15f));
            var statsRect = statsPanel.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.37f, 0.25f);
            statsRect.anchorMax = new Vector2(0.63f, 0.62f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;

            CreateText("StatsLabel", statsPanel.transform, "STATISTICS", 16,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAnchor.UpperCenter);

            statsText = CreateText("StatsContent", statsPanel.transform, "", 18,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TextAnchor.MiddleCenter);

            // Right panel - Tips
            var tipsPanel = CreatePanel("TipsPanel", debriefPanel.transform, new Color(0.1f, 0.12f, 0.1f));
            var tipsRect = tipsPanel.GetComponent<RectTransform>();
            tipsRect.anchorMin = new Vector2(0.65f, 0.25f);
            tipsRect.anchorMax = new Vector2(0.95f, 0.62f);
            tipsRect.offsetMin = Vector2.zero;
            tipsRect.offsetMax = Vector2.zero;

            CreateText("TipsLabel", tipsPanel.transform, "PILOT TIPS", 16,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAnchor.UpperCenter);

            tipsText = CreateText("TipsContent", tipsPanel.transform, "", 16,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.85f), TextAnchor.UpperLeft);

            // Buttons
            retryButton = CreateButton("RetryButton", debriefPanel.transform, "RETRY MISSION",
                new Vector2(0.25f, 0.08f), new Vector2(0.4f, 0.15f));
            retryButton.onClick.AddListener(OnRetryClicked);

            nextButton = CreateButton("NextButton", debriefPanel.transform, "NEXT MISSION",
                new Vector2(0.42f, 0.08f), new Vector2(0.58f, 0.15f));
            nextButton.onClick.AddListener(OnNextClicked);

            menuButton = CreateButton("MenuButton", debriefPanel.transform, "MAIN MENU",
                new Vector2(0.6f, 0.08f), new Vector2(0.75f, 0.15f));
            menuButton.onClick.AddListener(OnMenuClicked);
        }

        public void ShowDebrief(MissionData mission, MissionResult result)
        {
            lastMission = mission;
            lastResult = result;

            // Update UI
            titleText.text = result.Success ? "MISSION COMPLETE!" : "MISSION FAILED";
            titleText.color = result.Success ? Color.white : new Color(1f, 0.5f, 0.5f);

            gradeText.text = result.Grade;
            gradeText.color = GetGradeColor(result.Grade);

            scoreText.text = $"{result.TotalScore} Points";

            // Stats
            UpdateStats(result);

            // Flight trace
            DrawFlightTrace();

            // Tips based on performance
            GenerateTips(result);

            // Show panel
            debriefPanel.SetActive(true);
        }

        private void UpdateStats(MissionResult result)
        {
            string stats = "";
            stats += $"Total Time: {result.TotalTime:F1}s\n\n";

            if (flightRecorder != null)
            {
                float avgDev = flightRecorder.GetAverageDeviation();
                float maxDev = flightRecorder.GetMaxDeviation();
                float timeOnRadial = flightRecorder.GetTimeOnRadial();
                int crossings = flightRecorder.GetRadialCrossings();

                stats += $"Avg Deviation: {avgDev * 10:F1}°\n";
                stats += $"Max Deviation: {maxDev * 10:F1}°\n";
                stats += $"Time on Radial: {timeOnRadial:F1}s\n";
                stats += $"Radial Crossings: {crossings}\n";
            }

            stats += $"\n--- Score Breakdown ---\n";
            stats += $"Base: {result.BaseScore}\n";
            stats += $"Time Bonus: +{result.TimeBonus}\n";
            stats += $"Intercept Bonus: +{result.InterceptBonus}\n";
            stats += $"Tracking Bonus: +{result.TrackingBonus}\n";
            stats += $"Deviation Penalty: -{result.DeviationPenalty}";

            statsText.text = stats;
        }

        private void DrawFlightTrace()
        {
            // Clear old trace
            foreach (var line in traceLines)
            {
                if (line != null) Destroy(line);
            }
            traceLines.Clear();

            if (flightRecorder == null) return;

            var path = flightRecorder.GetNormalizedFlightPath();
            if (path.Count < 2) return;

            // Draw lines connecting the path points
            for (int i = 1; i < path.Count; i++)
            {
                DrawTraceLine(path[i - 1], path[i], GetTraceColor(i, path.Count));
            }

            // Draw start point (green)
            DrawTracePoint(path[0], Color.green, 8f);

            // Draw end point (based on result)
            Color endColor = lastResult.Success ? Color.cyan : Color.red;
            DrawTracePoint(path[path.Count - 1], endColor, 10f);
        }

        private void DrawTraceLine(Vector2 from, Vector2 to, Color color)
        {
            var lineObj = new GameObject("TraceLine");
            lineObj.transform.SetParent(traceContainer, false);

            var rect = lineObj.AddComponent<RectTransform>();
            var image = lineObj.AddComponent<Image>();
            image.color = color;

            // Calculate line properties
            Vector2 dir = to - from;
            float length = dir.magnitude * traceContainer.rect.width;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Position at midpoint
            Vector2 midpoint = (from + to) / 2f;
            rect.anchorMin = midpoint;
            rect.anchorMax = midpoint;
            rect.sizeDelta = new Vector2(length, 2f);
            rect.localRotation = Quaternion.Euler(0, 0, angle);

            traceLines.Add(lineObj);
        }

        private void DrawTracePoint(Vector2 pos, Color color, float size)
        {
            var pointObj = new GameObject("TracePoint");
            pointObj.transform.SetParent(traceContainer, false);

            var rect = pointObj.AddComponent<RectTransform>();
            var image = pointObj.AddComponent<Image>();
            image.color = color;

            rect.anchorMin = pos;
            rect.anchorMax = pos;
            rect.sizeDelta = new Vector2(size, size);

            traceLines.Add(pointObj);
        }

        private Color GetTraceColor(int index, int total)
        {
            // Gradient from green (start) to cyan (end)
            float t = (float)index / total;
            return Color.Lerp(new Color(0.3f, 1f, 0.3f), new Color(0.3f, 0.8f, 1f), t);
        }

        private void GenerateTips(MissionResult result)
        {
            List<string> tips = new List<string>();

            // Select tips based on performance
            float avgDev = flightRecorder?.GetAverageDeviation() ?? 0f;
            int crossings = flightRecorder?.GetRadialCrossings() ?? 0;

            // Intercept tip if deviation was high at start
            if (avgDev > 0.3f)
            {
                tips.Add("• " + InterceptTips[Random.Range(0, InterceptTips.Length)]);
            }

            // Tracking tip if many crossings (oscillation)
            if (crossings > 5)
            {
                tips.Add("• " + TrackingTips[Random.Range(0, TrackingTips.Length)]);
            }

            // Always add a general tip
            tips.Add("• " + GeneralTips[Random.Range(0, GeneralTips.Length)]);

            // Grade-specific encouragement
            switch (result.Grade)
            {
                case "S+":
                    tips.Insert(0, "★ EXCEPTIONAL! You're flying like a pro.");
                    break;
                case "A":
                    tips.Insert(0, "★ Excellent work! Minor improvements possible.");
                    break;
                case "B":
                    tips.Insert(0, "★ Good job! Focus on smoother corrections.");
                    break;
                case "C":
                    tips.Insert(0, "★ Passing grade. Practice the fundamentals.");
                    break;
                default:
                    tips.Insert(0, "★ Keep practicing! VOR navigation takes time to master.");
                    break;
            }

            tipsText.text = string.Join("\n\n", tips);
        }

        private void HidePanel()
        {
            if (debriefPanel != null)
                debriefPanel.SetActive(false);
        }

        private void OnRetryClicked()
        {
            HidePanel();
            if (lastMission != null && missionManager != null)
            {
                missionManager.LoadMission(lastMission.mission_id);
                missionManager.StartMission();
            }
        }

        private void OnNextClicked()
        {
            HidePanel();
            // Show mission select
            var overlay = GameObject.Find("MissionSelectOverlay");
            if (overlay != null)
                overlay.SetActive(true);
        }

        private void OnMenuClicked()
        {
            HidePanel();
            var overlay = GameObject.Find("MissionSelectOverlay");
            if (overlay != null)
                overlay.SetActive(true);
        }

        private Color GetGradeColor(string grade)
        {
            return grade switch
            {
                "S+" => new Color(1f, 0.85f, 0f),  // Gold
                "A" => new Color(0.3f, 1f, 0.3f),  // Green
                "B" => new Color(0.3f, 0.8f, 1f),  // Cyan
                "C" => new Color(1f, 1f, 0.3f),    // Yellow
                "D" => new Color(1f, 0.6f, 0.2f),  // Orange
                _ => new Color(1f, 0.3f, 0.3f)     // Red
            };
        }

        // UI Helper methods
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
            Vector2 anchorPos, Vector2 pivot, TextAnchor alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(500, 100);

            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = alignment;

            return txt;
        }

        private Button CreateButton(string name, Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var btnObj = CreatePanel(name, parent, new Color(0.2f, 0.4f, 0.2f));
            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.3f, 0.1f);
            btn.colors = colors;

            var labelText = CreateText("Label", btnObj.transform, label, 20,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), TextAnchor.MiddleCenter);

            return btn;
        }
    }
}
