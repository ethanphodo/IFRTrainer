using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using IFRTrainer.Core;
using IFRTrainer.Navigation;
using IFRTrainer.Instruments;
using IFRTrainer.Mission;
using IFRTrainer.Progression;
using IFRTrainer.UI;

namespace IFRTrainer.Editor
{
    /// <summary>
    /// Editor script to auto-generate the complete IFR Trainer scene.
    /// Use: Window > IFR Trainer > Setup Scene
    /// </summary>
    public class SceneSetup : EditorWindow
    {
        [MenuItem("Window/IFR Trainer/Setup Scene")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetup>("IFR Trainer Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("IFR Trainer Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will create all necessary GameObjects and UI for the IFR Trainer.\n\n" +
                "Make sure you have TextMeshPro installed (Window > TextMeshPro > Import TMP Essential Resources)",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Create Complete Scene", GUILayout.Height(40)))
            {
                CreateCompleteScene();
            }

            GUILayout.Space(10);

            GUILayout.Label("Individual Components:", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Managers Only"))
            {
                CreateManagers();
            }

            if (GUILayout.Button("Create UI Only"))
            {
                CreateUI();
            }

            if (GUILayout.Button("Create Test Mission Loader"))
            {
                CreateTestMissionLoader();
            }
        }

        private static void CreateCompleteScene()
        {
            // Clear existing if desired
            if (EditorUtility.DisplayDialog("Create Scene",
                "This will set up the complete IFR Trainer scene. Continue?",
                "Yes", "Cancel"))
            {
                CreateManagers();
                CreateUI();
                CreateTestMissionLoader();

                Debug.Log("IFR Trainer scene setup complete!");
                EditorUtility.DisplayDialog("Setup Complete",
                    "Scene has been created!\n\n" +
                    "Press Play to test. Use the UI buttons to:\n" +
                    "1. Select a mission\n" +
                    "2. Start the mission\n" +
                    "3. Use heading buttons to navigate\n\n" +
                    "Watch the instruments respond to your inputs.",
                    "OK");
            }
        }

        private static void CreateManagers()
        {
            // Find or create Managers parent
            GameObject managers = GameObject.Find("--- MANAGERS ---");
            if (managers == null)
            {
                managers = new GameObject("--- MANAGERS ---");
            }

            // GameManager
            CreateManagerObject<GameManager>("GameManager", managers.transform);

            // MissionManager + ScoreManager
            var missionObj = CreateManagerObject<MissionManager>("MissionManager", managers.transform);
            if (missionObj.GetComponent<ScoreManager>() == null)
                missionObj.AddComponent<ScoreManager>();

            // GameModeManager
            CreateManagerObject<GameModeManager>("GameModeManager", managers.transform);

            // PlayerProgress
            CreateManagerObject<PlayerProgress>("PlayerProgress", managers.transform);

            // DailyChallengeManager
            CreateManagerObject<DailyChallengeManager>("DailyChallengeManager", managers.transform);

            // VORReceiver
            CreateManagerObject<VORReceiver>("VORReceiver", managers.transform);

            // AdaptiveDifficulty
            CreateManagerObject<AdaptiveDifficulty>("AdaptiveDifficulty", managers.transform);

            Debug.Log("Managers created successfully");
        }

        private static GameObject CreateManagerObject<T>(string name, Transform parent) where T : Component
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent);
            }

            if (obj.GetComponent<T>() == null)
            {
                obj.AddComponent<T>();
            }

            return obj;
        }

        private static void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = GameObject.Find("MainCanvas");
            Canvas canvas;

            if (canvasObj == null)
            {
                canvasObj = new GameObject("MainCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }

            // Create main layout
            CreateMainLayout(canvasObj.transform);

            // Create EventSystem if needed
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("UI created successfully");
        }

        private static void CreateMainLayout(Transform canvasTransform)
        {
            // Background
            var background = CreatePanel("Background", canvasTransform);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f);

            // Header Panel
            var header = CreatePanel("HeaderPanel", canvasTransform);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.92f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            // Header text
            CreateText("MissionTitle", header.transform, "IFR TRAINER", 24,
                new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20, 0), new Vector2(400, 40));

            CreateText("ScoreText", header.transform, "Score: 0", 20,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(120, 40));

            CreateText("TimeText", header.transform, "Time: 00:00", 20,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-280, 0), new Vector2(120, 40));

            // Left Panel (Radar Scope)
            var leftPanel = CreatePanel("RadarPanel", canvasTransform);
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0.15f);
            leftRect.anchorMax = new Vector2(0.5f, 0.92f);
            leftRect.offsetMin = new Vector2(10, 10);
            leftRect.offsetMax = new Vector2(-5, -10);
            leftPanel.GetComponent<Image>().color = new Color(0.05f, 0.1f, 0.05f);

            // Radar scope content
            var radarScope = leftPanel.AddComponent<RadarScope>();
            CreateRadarContent(leftPanel.transform, radarScope);

            // Right Panel (Instruments)
            var rightPanel = CreatePanel("InstrumentPanel", canvasTransform);
            var rightRect = rightPanel.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.5f, 0.15f);
            rightRect.anchorMax = new Vector2(1, 0.92f);
            rightRect.offsetMin = new Vector2(5, 10);
            rightRect.offsetMax = new Vector2(-10, -10);
            rightPanel.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f);

            var instrumentPanel = rightPanel.AddComponent<InstrumentPanel>();
            CreateInstrumentContent(rightPanel.transform, instrumentPanel);

            // Bottom Panel (Controls & Objective)
            var bottomPanel = CreatePanel("ControlPanel", canvasTransform);
            var bottomRect = bottomPanel.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0.15f);
            bottomRect.offsetMin = new Vector2(10, 10);
            bottomRect.offsetMax = new Vector2(-10, 0);
            bottomPanel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            CreateControlContent(bottomPanel.transform, instrumentPanel);

            // Mission Select Overlay
            CreateMissionSelectOverlay(canvasTransform);

            // Hint Panel
            CreateHintPanel(canvasTransform);
        }

        private static void CreateRadarContent(Transform parent, RadarScope radarScope)
        {
            // Scope area
            var scopeArea = CreatePanel("ScopeArea", parent);
            var scopeRect = scopeArea.GetComponent<RectTransform>();
            scopeRect.anchorMin = new Vector2(0.05f, 0.1f);
            scopeRect.anchorMax = new Vector2(0.95f, 0.95f);
            scopeRect.offsetMin = Vector2.zero;
            scopeRect.offsetMax = Vector2.zero;
            scopeArea.GetComponent<Image>().color = new Color(0, 0.05f, 0);

            // Assign to RadarScope
            var scopeField = typeof(RadarScope).GetField("scopeArea",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            scopeField?.SetValue(radarScope, scopeRect);

            // Aircraft icon
            var aircraft = CreatePanel("AircraftIcon", scopeArea.transform);
            var acRect = aircraft.GetComponent<RectTransform>();
            acRect.sizeDelta = new Vector2(20, 20);
            acRect.anchoredPosition = Vector2.zero;
            aircraft.GetComponent<Image>().color = Color.green;

            var acField = typeof(RadarScope).GetField("aircraftIcon",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            acField?.SetValue(radarScope, acRect);

            // Scale text
            var scaleText = CreateText("ScaleText", parent, "20 NM", 14,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 10), new Vector2(80, 30));

            var scaleField = typeof(RadarScope).GetField("scaleText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            scaleField?.SetValue(radarScope, scaleText);
        }

        private static void CreateInstrumentContent(Transform parent, InstrumentPanel panel)
        {
            // Top row - basic instruments
            var topRow = CreatePanel("TopRow", parent);
            topRow.GetComponent<Image>().color = Color.clear;
            var topRect = topRow.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.7f);
            topRect.anchorMax = new Vector2(1, 0.95f);
            topRect.offsetMin = new Vector2(10, 0);
            topRect.offsetMax = new Vector2(-10, -10);

            // Airspeed
            var asiPanel = CreateInstrumentDisplay("ASI", topRow.transform, 0);
            var asiText = CreateText("Value", asiPanel.transform, "150 KTS", 18,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetFieldValue(panel, "airspeedText", asiText);

            // Altitude
            var altPanel = CreateInstrumentDisplay("ALT", topRow.transform, 1);
            var altText = CreateText("Value", altPanel.transform, "5000 FT", 18,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetFieldValue(panel, "altitudeText", altText);

            // Heading
            var hdgPanel = CreateInstrumentDisplay("HDG", topRow.transform, 2);
            var hdgText = CreateText("Value", hdgPanel.transform, "045°", 18,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetFieldValue(panel, "headingText", hdgText);

            // VS
            var vsPanel = CreateInstrumentDisplay("VS", topRow.transform, 3);
            var vsText = CreateText("Value", vsPanel.transform, "+0 FPM", 18,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetFieldValue(panel, "verticalSpeedText", vsText);

            // VOR Indicator (center)
            var vorPanel = CreatePanel("VORIndicator", parent);
            vorPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);
            var vorRect = vorPanel.GetComponent<RectTransform>();
            vorRect.anchorMin = new Vector2(0.1f, 0.25f);
            vorRect.anchorMax = new Vector2(0.9f, 0.68f);
            vorRect.offsetMin = Vector2.zero;
            vorRect.offsetMax = Vector2.zero;

            var vorIndicator = vorPanel.AddComponent<VORIndicator>();
            CreateVORIndicatorContent(vorPanel.transform, vorIndicator);

            // OBS display
            var obsText = CreateText("OBSText", vorPanel.transform, "OBS 090°", 20,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(150, 40));
            SetFieldValue(vorIndicator, "obsText", obsText);

            // Frequency display
            var freqText = CreateText("FreqText", vorPanel.transform, "113.60", 16,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(80, 30));
            SetFieldValue(vorIndicator, "frequencyText", freqText);

            // TO/FROM text
            var toFromText = CreateText("ToFromText", vorPanel.transform, "---", 24,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), new Vector2(80, 40));
            SetFieldValue(vorIndicator, "toFromText", toFromText);

            // Distance text
            var distText = CreateText("DistText", vorPanel.transform, "--- NM", 16,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 10), new Vector2(80, 30));
            SetFieldValue(vorIndicator, "distanceText", distText);
        }

        private static void CreateVORIndicatorContent(Transform parent, VORIndicator indicator)
        {
            // CDI background
            var cdiBack = CreatePanel("CDIBackground", parent);
            cdiBack.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            var cdiBackRect = cdiBack.GetComponent<RectTransform>();
            cdiBackRect.anchorMin = new Vector2(0.2f, 0.35f);
            cdiBackRect.anchorMax = new Vector2(0.8f, 0.45f);
            cdiBackRect.offsetMin = Vector2.zero;
            cdiBackRect.offsetMax = Vector2.zero;

            // Center dots
            for (int i = -2; i <= 2; i++)
            {
                var dot = CreatePanel($"Dot_{i}", cdiBack.transform);
                dot.GetComponent<Image>().color = Color.white;
                var dotRect = dot.GetComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(0.5f + i * 0.1f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f + i * 0.1f, 0.5f);
                dotRect.sizeDelta = new Vector2(8, 8);
            }

            // CDI Needle
            var needle = CreatePanel("CDINeedle", cdiBack.transform);
            needle.GetComponent<Image>().color = Color.yellow;
            var needleRect = needle.GetComponent<RectTransform>();
            needleRect.anchorMin = new Vector2(0.5f, 0);
            needleRect.anchorMax = new Vector2(0.5f, 1);
            needleRect.sizeDelta = new Vector2(4, 0);
            needleRect.anchoredPosition = Vector2.zero;

            SetFieldValue(indicator, "cdiNeedle", needleRect);
        }

        private static GameObject CreateInstrumentDisplay(string label, Transform parent, int index)
        {
            var panel = CreatePanel(label + "Panel", parent);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);
            var rect = panel.GetComponent<RectTransform>();

            float width = 0.23f;
            float gap = 0.02f;
            float startX = gap + index * (width + gap);

            rect.anchorMin = new Vector2(startX, 0.1f);
            rect.anchorMax = new Vector2(startX + width, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Label
            CreateText("Label", panel.transform, label, 12,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(60, 20));

            return panel;
        }

        private static void CreateControlContent(Transform parent, InstrumentPanel panel)
        {
            // Objective text
            var objText = CreateText("ObjectiveText", parent, "Select a mission to begin", 16,
                new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20, 0), new Vector2(400, 60));
            SetFieldValue(panel, "objectiveText", objText);

            // Heading buttons
            var buttonsPanel = CreatePanel("HeadingButtons", parent);
            buttonsPanel.GetComponent<Image>().color = Color.clear;
            var btnRect = buttonsPanel.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.1f);
            btnRect.anchorMax = new Vector2(1, 0.9f);
            btnRect.offsetMin = new Vector2(0, 0);
            btnRect.offsetMax = new Vector2(-20, 0);

            // Create heading buttons
            string[] labels = { "L30", "L10", "R10", "R30" };
            float[] deltas = { -30, -10, 10, 30 };

            for (int i = 0; i < labels.Length; i++)
            {
                var btn = CreateButton(labels[i], buttonsPanel.transform, labels[i]);
                var bRect = btn.GetComponent<RectTransform>();
                float x = 0.05f + i * 0.24f;
                bRect.anchorMin = new Vector2(x, 0.2f);
                bRect.anchorMax = new Vector2(x + 0.2f, 0.8f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;

                // Wire up button
                float delta = deltas[i];
                btn.onClick.AddListener(() => {
                    if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
                    {
                        float current = GameManager.Instance.CurrentDynamics.Heading;
                        float newHdg = (current + delta + 360) % 360;
                        GameManager.Instance.SetTargetHeading(newHdg);
                    }
                });
            }
        }

        private static void CreateMissionSelectOverlay(Transform canvasTransform)
        {
            var overlay = CreatePanel("MissionSelectOverlay", canvasTransform);
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);
            var rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var missionSelect = overlay.AddComponent<MissionSelect>();

            // Title
            CreateText("Title", overlay.transform, "SELECT MISSION", 32,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(400, 50));

            // Mission buttons container
            var container = CreatePanel("MissionList", overlay.transform);
            container.GetComponent<Image>().color = Color.clear;
            var contRect = container.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.2f, 0.2f);
            contRect.anchorMax = new Vector2(0.8f, 0.85f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;

            // Add vertical layout
            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // Create mission buttons
            string[] missions = {
                "vor_intercept_001|VOR Intercept - Basic",
                "vor_intercept_002|VOR Inbound Intercept",
                "vor_intercept_003|Cross-Radial Navigation",
                "vor_tracking_001|Radial Tracking",
                "time_attack_001|Time Attack: Speed Intercept"
            };

            foreach (var mission in missions)
            {
                var parts = mission.Split('|');
                var btn = CreateButton(parts[0], container.transform, parts[1]);
                var bRect = btn.GetComponent<RectTransform>();
                bRect.sizeDelta = new Vector2(0, 50);

                string missionId = parts[0];
                btn.onClick.AddListener(() => {
                    var mm = FindFirstObjectByType<MissionManager>();
                    if (mm != null)
                    {
                        mm.LoadMission(missionId);
                        mm.StartMission();
                        overlay.SetActive(false);
                    }
                });
            }

            SetFieldValue(missionSelect, "missionSelectPanel", overlay);
        }

        private static void CreateHintPanel(Transform canvasTransform)
        {
            var hintPanel = CreatePanel("HintPanel", canvasTransform);
            hintPanel.GetComponent<Image>().color = new Color(0.1f, 0.3f, 0.1f, 0.95f);
            var rect = hintPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0.75f);
            rect.anchorMax = new Vector2(0.75f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var hintText = CreateText("HintText", hintPanel.transform, "", 16,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(20, 0), new Vector2(-20, 0));

            var hintSystem = hintPanel.AddComponent<HintSystem>();
            SetFieldValue(hintSystem, "hintPanel", hintPanel);
            SetFieldValue(hintSystem, "hintText", hintText);

            hintPanel.SetActive(false);
        }

        private static void CreateTestMissionLoader()
        {
            var loader = GameObject.Find("TestMissionLoader");
            if (loader == null)
            {
                loader = new GameObject("TestMissionLoader");
            }

            if (loader.GetComponent<TestMissionLoader>() == null)
            {
                loader.AddComponent<TestMissionLoader>();
            }

            Debug.Log("Test mission loader created");
        }

        // Helper methods
        private static GameObject CreatePanel(string name, Transform parent)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            panel.AddComponent<Image>();
            return panel;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.2f);

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.3f, 0.1f);
            btn.colors = colors;

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);

            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static void SetFieldValue(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            field?.SetValue(obj, value);
        }
    }
}
