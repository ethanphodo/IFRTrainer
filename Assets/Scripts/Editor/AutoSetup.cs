using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using IFRTrainer.Core;
using IFRTrainer.Navigation;
using IFRTrainer.Instruments;
using IFRTrainer.Mission;
using IFRTrainer.Progression;
using IFRTrainer.UI;

namespace IFRTrainer.Editor
{
    /// <summary>
    /// Automatically sets up the scene if it's empty when entering play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSetup
    {
        static AutoSetup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Check if scene needs setup
                if (GameObject.Find("--- MANAGERS ---") == null)
                {
                    Debug.Log("IFR Trainer: Auto-setting up complete scene...");
                    CreateCompleteScene();
                }
            }
        }

        [MenuItem("Window/IFR Trainer/Quick Setup (Complete)")]
        public static void CreateCompleteScene()
        {
            CreateCamera();
            CreateManagers();
            CreateUI();
            CreateTestMissionLoader();

            // Mark scene dirty so it can be saved
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("IFR Trainer: Complete scene setup finished!");
            Debug.Log("Use number keys 1-5 to load missions, arrow keys to adjust heading/OBS.");
        }

        private static void CreateCamera()
        {
            // Create Main Camera if it doesn't exist
            if (Camera.main == null && GameObject.Find("Main Camera") == null)
            {
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                var cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
                cam.orthographic = true;
                camObj.AddComponent<AudioListener>();
                Debug.Log("Main Camera created");
            }
        }

        private static void CreateManagers()
        {
            var managers = new GameObject("--- MANAGERS ---");

            // GameManager
            var gm = new GameObject("GameManager");
            gm.transform.SetParent(managers.transform);
            gm.AddComponent<GameManager>();

            // MissionManager
            var mm = new GameObject("MissionManager");
            mm.transform.SetParent(managers.transform);
            mm.AddComponent<MissionManager>();
            mm.AddComponent<ScoreManager>();

            // VORReceiver
            var vr = new GameObject("VORReceiver");
            vr.transform.SetParent(managers.transform);
            vr.AddComponent<VORReceiver>();

            // GameModeManager
            var gmm = new GameObject("GameModeManager");
            gmm.transform.SetParent(managers.transform);
            gmm.AddComponent<GameModeManager>();

            // PlayerProgress
            var pp = new GameObject("PlayerProgress");
            pp.transform.SetParent(managers.transform);
            pp.AddComponent<PlayerProgress>();

            // DailyChallengeManager
            var dcm = new GameObject("DailyChallengeManager");
            dcm.transform.SetParent(managers.transform);
            dcm.AddComponent<DailyChallengeManager>();

            // AdaptiveDifficulty
            var ad = new GameObject("AdaptiveDifficulty");
            ad.transform.SetParent(managers.transform);
            ad.AddComponent<AdaptiveDifficulty>();

            // SpriteManager
            var sm = new GameObject("SpriteManager");
            sm.transform.SetParent(managers.transform);
            sm.AddComponent<SpriteManager>();

            // ButtonWiring - wires up UI buttons at runtime
            var bw = new GameObject("ButtonWiring");
            bw.transform.SetParent(managers.transform);
            bw.AddComponent<ButtonWiring>();

            // FlightFeedback - in-flight dopamine hits
            var ff = new GameObject("FlightFeedback");
            ff.transform.SetParent(managers.transform);
            ff.AddComponent<FlightFeedback>();

            // FeedbackPopup - animated score popups
            var fp = new GameObject("FeedbackPopup");
            fp.transform.SetParent(managers.transform);
            fp.AddComponent<FeedbackPopup>();

            // CelebrationEffects - confetti and celebrations
            var ce = new GameObject("CelebrationEffects");
            ce.transform.SetParent(managers.transform);
            ce.AddComponent<CelebrationEffects>();

            // FlightRecorder - records flight data for debrief
            var fr = new GameObject("FlightRecorder");
            fr.transform.SetParent(managers.transform);
            fr.AddComponent<FlightRecorder>();

            // DebriefPanel - post-mission analysis
            var dp = new GameObject("DebriefPanel");
            dp.transform.SetParent(managers.transform);
            dp.AddComponent<DebriefPanel>();

            Debug.Log("Managers created");
        }

        private static void CreateUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("MainCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<ButtonWiring>(); // Wire up buttons at runtime

            // EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Background
            var background = CreatePanel("Background", canvasObj.transform, new Color(0.1f, 0.1f, 0.15f));
            SetFullStretch(background.GetComponent<RectTransform>());

            // Header Panel
            var header = CreatePanel("HeaderPanel", canvasObj.transform, new Color(0.15f, 0.15f, 0.2f));
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.92f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            CreateText("MissionTitle", header.transform, "IFR TRAINER - Press 1-5 to load mission", 24, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(0.7f, 1), new Vector2(20, 0), Vector2.zero);

            CreateText("ScoreText", header.transform, "Score: 0", 20, TextAnchor.MiddleRight,
                new Vector2(0.85f, 0), new Vector2(1, 1), Vector2.zero, new Vector2(-20, 0));

            // Left Panel (Radar Scope)
            var leftPanel = CreatePanel("RadarPanel", canvasObj.transform, new Color(0.05f, 0.1f, 0.05f));
            var leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0.15f);
            leftRect.anchorMax = new Vector2(0.5f, 0.92f);
            leftRect.offsetMin = new Vector2(10, 10);
            leftRect.offsetMax = new Vector2(-5, -10);

            CreateRadarContent(leftPanel);

            // Right Panel (Instruments)
            var rightPanel = CreatePanel("InstrumentPanel", canvasObj.transform, new Color(0.12f, 0.12f, 0.15f));
            var rightRect = rightPanel.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.5f, 0.15f);
            rightRect.anchorMax = new Vector2(1, 0.92f);
            rightRect.offsetMin = new Vector2(5, 10);
            rightRect.offsetMax = new Vector2(-10, -10);

            CreateInstrumentContent(rightPanel);

            // Bottom Panel (Controls)
            var bottomPanel = CreatePanel("ControlPanel", canvasObj.transform, new Color(0.15f, 0.15f, 0.2f));
            var bottomRect = bottomPanel.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0.15f);
            bottomRect.offsetMin = new Vector2(10, 10);
            bottomRect.offsetMax = new Vector2(-10, 0);

            CreateControlContent(bottomPanel);

            // Mission Select Overlay
            CreateMissionSelectOverlay(canvasObj.transform);

            Debug.Log("UI created");
        }

        private static void CreateRadarContent(GameObject panel)
        {
            var radarScope = panel.AddComponent<RadarScope>();

            // Scope area
            var scopeArea = CreatePanel("ScopeArea", panel.transform, new Color(0, 0.05f, 0));
            var scopeRect = scopeArea.GetComponent<RectTransform>();
            scopeRect.anchorMin = new Vector2(0.05f, 0.05f);
            scopeRect.anchorMax = new Vector2(0.95f, 0.95f);
            scopeRect.offsetMin = Vector2.zero;
            scopeRect.offsetMax = Vector2.zero;

            // Aircraft icon
            var aircraft = CreatePanel("AircraftIcon", scopeArea.transform, Color.green);
            var acRect = aircraft.GetComponent<RectTransform>();
            acRect.anchorMin = new Vector2(0.5f, 0.5f);
            acRect.anchorMax = new Vector2(0.5f, 0.5f);
            acRect.sizeDelta = new Vector2(20, 20);
            acRect.anchoredPosition = Vector2.zero;

            // VOR icon
            var vor = CreatePanel("VORIcon", scopeArea.transform, Color.cyan);
            var vorRect = vor.GetComponent<RectTransform>();
            vorRect.anchorMin = new Vector2(0.5f, 0.5f);
            vorRect.anchorMax = new Vector2(0.5f, 0.5f);
            vorRect.sizeDelta = new Vector2(15, 15);
            vorRect.anchoredPosition = new Vector2(0, 100);

            // Info text
            CreateText("InfoText", panel.transform, "Radar Scope\nGreen = Aircraft\nCyan = VOR Station", 14, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(0.5f, 0.15f), new Vector2(10, 5), Vector2.zero);

            // Wire up RadarScope fields via reflection
            SetFieldValue(radarScope, "scopeArea", scopeRect);
            SetFieldValue(radarScope, "aircraftIcon", acRect);
        }

        private static void CreateInstrumentContent(GameObject panel)
        {
            var instrumentPanel = panel.AddComponent<InstrumentPanel>();

            // Top row - basic instruments
            var topRow = CreatePanel("TopRow", panel.transform, Color.clear);
            var topRect = topRow.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.75f);
            topRect.anchorMax = new Vector2(1, 0.95f);
            topRect.offsetMin = new Vector2(10, 0);
            topRect.offsetMax = new Vector2(-10, -10);

            // Instrument displays
            var asiText = CreateInstrumentBox("ASI", "150 KTS", topRow.transform, 0);
            var altText = CreateInstrumentBox("ALT", "5000 FT", topRow.transform, 1);
            var hdgText = CreateInstrumentBox("HDG", "045°", topRow.transform, 2);
            var vsText = CreateInstrumentBox("VS", "+0 FPM", topRow.transform, 3);

            SetFieldValue(instrumentPanel, "airspeedText", asiText);
            SetFieldValue(instrumentPanel, "altitudeText", altText);
            SetFieldValue(instrumentPanel, "headingText", hdgText);
            SetFieldValue(instrumentPanel, "verticalSpeedText", vsText);

            // VOR Indicator
            var vorPanel = CreatePanel("VORIndicator", panel.transform, new Color(0.08f, 0.08f, 0.1f));
            var vorRect = vorPanel.GetComponent<RectTransform>();
            vorRect.anchorMin = new Vector2(0.1f, 0.25f);
            vorRect.anchorMax = new Vector2(0.9f, 0.72f);
            vorRect.offsetMin = Vector2.zero;
            vorRect.offsetMax = Vector2.zero;

            var vorIndicator = vorPanel.AddComponent<VORIndicator>();
            CreateVORIndicatorContent(vorPanel, vorIndicator);
        }

        private static Text CreateInstrumentBox(string label, string value, Transform parent, int index)
        {
            var box = CreatePanel(label + "Panel", parent, new Color(0.08f, 0.08f, 0.1f));
            var rect = box.GetComponent<RectTransform>();

            float width = 0.23f;
            float gap = 0.02f;
            float startX = gap + index * (width + gap);

            rect.anchorMin = new Vector2(startX, 0.1f);
            rect.anchorMax = new Vector2(startX + width, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Label
            CreateText("Label", box.transform, label, 12, TextAnchor.UpperCenter,
                new Vector2(0, 0.6f), new Vector2(1, 1), Vector2.zero, Vector2.zero);

            // Value
            var valueText = CreateText("Value", box.transform, value, 18, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero);

            return valueText;
        }

        private static void CreateVORIndicatorContent(GameObject panel, VORIndicator indicator)
        {
            // CDI Background
            var cdiBack = CreatePanel("CDIBackground", panel.transform, new Color(0.2f, 0.2f, 0.2f));
            var cdiRect = cdiBack.GetComponent<RectTransform>();
            cdiRect.anchorMin = new Vector2(0.15f, 0.4f);
            cdiRect.anchorMax = new Vector2(0.85f, 0.5f);
            cdiRect.offsetMin = Vector2.zero;
            cdiRect.offsetMax = Vector2.zero;

            // Center dots
            for (int i = -2; i <= 2; i++)
            {
                var dot = CreatePanel($"Dot{i}", cdiBack.transform, Color.white);
                var dotRect = dot.GetComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(0.5f + i * 0.1f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f + i * 0.1f, 0.5f);
                dotRect.sizeDelta = new Vector2(8, 8);
            }

            // CDI Needle
            var needle = CreatePanel("CDINeedle", cdiBack.transform, Color.yellow);
            var needleRect = needle.GetComponent<RectTransform>();
            needleRect.anchorMin = new Vector2(0.5f, 0);
            needleRect.anchorMax = new Vector2(0.5f, 1);
            needleRect.sizeDelta = new Vector2(4, 0);
            needleRect.anchoredPosition = Vector2.zero;

            SetFieldValue(indicator, "cdiNeedle", needleRect);

            // OBS Text
            var obsText = CreateText("OBSText", panel.transform, "OBS 090°", 20, TextAnchor.MiddleCenter,
                new Vector2(0.3f, 0), new Vector2(0.7f, 0.15f), Vector2.zero, Vector2.zero);
            SetFieldValue(indicator, "obsText", obsText);

            // Frequency
            var freqText = CreateText("FreqText", panel.transform, "113.60", 16, TextAnchor.UpperLeft,
                new Vector2(0, 0.85f), new Vector2(0.3f, 1), new Vector2(10, 0), Vector2.zero);
            SetFieldValue(indicator, "frequencyText", freqText);

            // TO/FROM
            var toFromText = CreateText("ToFromText", panel.transform, "---", 24, TextAnchor.UpperRight,
                new Vector2(0.7f, 0.85f), new Vector2(1, 1), Vector2.zero, new Vector2(-10, 0));
            SetFieldValue(indicator, "toFromText", toFromText);

            // Distance
            var distText = CreateText("DistText", panel.transform, "--- NM", 16, TextAnchor.LowerRight,
                new Vector2(0.7f, 0), new Vector2(1, 0.15f), Vector2.zero, new Vector2(-10, 0));
            SetFieldValue(indicator, "distanceText", distText);

            // Title
            CreateText("VORTitle", panel.transform, "VOR / CDI", 14, TextAnchor.UpperCenter,
                new Vector2(0.3f, 0.85f), new Vector2(0.7f, 1), Vector2.zero, Vector2.zero);
        }

        private static void CreateControlContent(GameObject panel)
        {
            // Objective text
            CreateText("ObjectiveText", panel.transform, "Press 1-5 to load a mission, Arrow keys to adjust HDG/OBS", 16, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(0.55f, 1), new Vector2(20, 0), Vector2.zero);

            // Heading buttons
            var buttonsPanel = CreatePanel("HeadingButtons", panel.transform, Color.clear);
            var btnRect = buttonsPanel.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.55f, 0.1f);
            btnRect.anchorMax = new Vector2(1, 0.9f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = new Vector2(-20, 0);

            string[] labels = { "L30", "L10", "R10", "R30" };
            float[] deltas = { -30, -10, 10, 30 };

            for (int i = 0; i < labels.Length; i++)
            {
                var btn = CreateButton(labels[i], buttonsPanel.transform, labels[i]);
                var bRect = btn.GetComponent<RectTransform>();
                float x = 0.02f + i * 0.245f;
                bRect.anchorMin = new Vector2(x, 0.15f);
                bRect.anchorMax = new Vector2(x + 0.23f, 0.85f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;

                float delta = deltas[i];
                btn.onClick.AddListener(() => {
                    var gm = Object.FindFirstObjectByType<GameManager>();
                    if (gm != null && gm.IsInitialized)
                    {
                        float current = gm.CurrentDynamics.Heading;
                        float newHdg = (current + delta + 360) % 360;
                        gm.SetTargetHeading(newHdg);
                    }
                });
            }
        }

        private static void CreateMissionSelectOverlay(Transform canvasTransform)
        {
            var overlay = CreatePanel("MissionSelectOverlay", canvasTransform, new Color(0, 0, 0, 0.9f));
            SetFullStretch(overlay.GetComponent<RectTransform>());

            var missionSelect = overlay.AddComponent<MissionSelect>();

            // Title
            CreateText("Title", overlay.transform, "IFR TRAINER - SELECT MISSION", 32, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.85f), new Vector2(0.8f, 0.95f), Vector2.zero, Vector2.zero);

            // Instructions
            CreateText("Instructions", overlay.transform, "Click a mission or press keys 1-5", 18, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.85f), Vector2.zero, Vector2.zero);

            // Mission buttons
            string[] missions = {
                "vor_intercept_001|1. VOR Intercept - Basic",
                "vor_intercept_002|2. VOR Inbound Intercept",
                "vor_intercept_003|3. Cross-Radial Navigation",
                "vor_tracking_001|4. Radial Tracking",
                "time_attack_001|5. Time Attack"
            };

            for (int i = 0; i < missions.Length; i++)
            {
                var parts = missions[i].Split('|');
                var btn = CreateButton(parts[0], overlay.transform, parts[1]);
                var bRect = btn.GetComponent<RectTransform>();

                float y = 0.65f - i * 0.12f;
                bRect.anchorMin = new Vector2(0.25f, y);
                bRect.anchorMax = new Vector2(0.75f, y + 0.1f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;

                string missionId = parts[0];
                btn.onClick.AddListener(() => {
                    var mm = Object.FindFirstObjectByType<MissionManager>();
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

        private static void CreateTestMissionLoader()
        {
            var loader = new GameObject("TestMissionLoader");
            var managers = GameObject.Find("--- MANAGERS ---");
            if (managers != null)
                loader.transform.SetParent(managers.transform);
            loader.AddComponent<TestMissionLoader>();
        }

        // Helper methods
        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = alignment;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return txt;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var btnObj = CreatePanel(name, parent, new Color(0.2f, 0.4f, 0.2f));

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.3f, 0.1f);
            btn.colors = colors;

            // Label
            var labelText = CreateText("Label", btnObj.transform, label, 16, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

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
