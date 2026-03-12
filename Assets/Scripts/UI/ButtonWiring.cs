using UnityEngine;
using UnityEngine.UI;
using IFRTrainer.Core;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Runtime script that wires up UI button click handlers.
    /// Auto-creates itself if not present in scene.
    /// </summary>
    public class ButtonWiring : MonoBehaviour
    {
        private static ButtonWiring instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (instance == null && FindFirstObjectByType<ButtonWiring>() == null)
            {
                var go = new GameObject("ButtonWiring");
                instance = go.AddComponent<ButtonWiring>();
                Debug.Log("ButtonWiring: Auto-created");
            }
        }

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            // Delay slightly to ensure all UI is created
            Invoke(nameof(WireAllButtons), 0.1f);
        }

        private void WireAllButtons()
        {
            WireHeadingButtons();
            WireMissionButtons();
            Debug.Log("ButtonWiring: UI buttons connected");
        }

        private void WireHeadingButtons()
        {
            // Find heading buttons by name
            WireButton("L30", () => AdjustHeading(-30));
            WireButton("L10", () => AdjustHeading(-10));
            WireButton("R10", () => AdjustHeading(10));
            WireButton("R30", () => AdjustHeading(30));
        }

        private void WireMissionButtons()
        {
            // Find mission buttons by name
            WireButton("vor_intercept_001", () => LoadMission("vor_intercept_001"));
            WireButton("vor_intercept_002", () => LoadMission("vor_intercept_002"));
            WireButton("vor_intercept_003", () => LoadMission("vor_intercept_003"));
            WireButton("vor_tracking_001", () => LoadMission("vor_tracking_001"));
            WireButton("time_attack_001", () => LoadMission("time_attack_001"));
        }

        private void WireButton(string name, UnityEngine.Events.UnityAction action)
        {
            var buttonObj = GameObject.Find(name);
            if (buttonObj == null)
            {
                Debug.LogWarning($"ButtonWiring: Button '{name}' not found");
                return;
            }

            var button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"ButtonWiring: '{name}' has no Button component");
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            Debug.Log($"ButtonWiring: Wired button '{name}'");
        }

        private void AdjustHeading(float delta)
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("ButtonWiring: GameManager.Instance is null");
                return;
            }
            if (!gm.IsInitialized)
            {
                Debug.LogWarning("ButtonWiring: GameManager not initialized yet");
                return;
            }

            float current = gm.CurrentDynamics.Heading;
            float newHdg = (current + delta + 360) % 360;
            gm.SetTargetHeading(newHdg);
            Debug.Log($"Heading: {current:F0}° → {newHdg:F0}°");
        }

        private void LoadMission(string missionId)
        {
            var mm = FindFirstObjectByType<MissionManager>();
            if (mm == null)
            {
                Debug.LogError("ButtonWiring: MissionManager not found!");
                return;
            }

            Debug.Log($"ButtonWiring: Loading mission {missionId}...");
            bool loaded = mm.LoadMission(missionId);

            if (loaded)
            {
                mm.StartMission();

                // Hide mission select overlay
                var overlay = GameObject.Find("MissionSelectOverlay");
                if (overlay != null)
                    overlay.SetActive(false);

                Debug.Log($"ButtonWiring: Mission {missionId} started!");
            }
            else
            {
                Debug.LogError($"ButtonWiring: Failed to load mission {missionId}");
            }
        }
    }
}
