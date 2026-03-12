using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
                    Debug.Log("IFR Trainer: Auto-setting up scene...");
                    CreateMinimalScene();
                }
            }
        }

        [MenuItem("Window/IFR Trainer/Quick Setup (Minimal)")]
        public static void CreateMinimalScene()
        {
            // Create managers
            var managers = new GameObject("--- MANAGERS ---");

            // GameManager
            var gm = new GameObject("GameManager");
            gm.transform.SetParent(managers.transform);
            gm.AddComponent<Core.GameManager>();

            // MissionManager
            var mm = new GameObject("MissionManager");
            mm.transform.SetParent(managers.transform);
            mm.AddComponent<Mission.MissionManager>();
            mm.AddComponent<Mission.ScoreManager>();

            // VORReceiver
            var vr = new GameObject("VORReceiver");
            vr.transform.SetParent(managers.transform);
            vr.AddComponent<Navigation.VORReceiver>();

            // GameModeManager
            var gmm = new GameObject("GameModeManager");
            gmm.transform.SetParent(managers.transform);
            gmm.AddComponent<Core.GameModeManager>();

            // PlayerProgress
            var pp = new GameObject("PlayerProgress");
            pp.transform.SetParent(managers.transform);
            pp.AddComponent<Progression.PlayerProgress>();

            // SpriteManager
            var sm = new GameObject("SpriteManager");
            sm.transform.SetParent(managers.transform);
            sm.AddComponent<UI.SpriteManager>();

            // TestMissionLoader
            var tml = new GameObject("TestMissionLoader");
            tml.transform.SetParent(managers.transform);
            tml.AddComponent<TestMissionLoader>();

            // Create simple UI Canvas
            var canvas = new GameObject("MainCanvas");
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Mark scene dirty so it can be saved
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("IFR Trainer: Minimal scene setup complete! Press Play to test.");
            Debug.Log("Use number keys 1-5 to load missions, arrow keys to adjust heading/OBS.");
        }
    }
}
