using UnityEngine;
using UnityEngine.UI;
using IFRTrainer.Core;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Runtime script that wires up UI button click handlers.
    /// Runs every frame until buttons are found and wired.
    /// </summary>
    public class ButtonWiring : MonoBehaviour
    {
        private bool hasWiredButtons = false;
        private float retryTimer = 0f;

        private void Update()
        {
            if (hasWiredButtons)
                return;

            retryTimer += Time.deltaTime;
            if (retryTimer > 0.5f)
            {
                retryTimer = 0f;
                TryWireButtons();
            }
        }

        private void TryWireButtons()
        {
            // Find all buttons in the scene
            var allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            Debug.Log($"ButtonWiring: Found {allButtons.Length} buttons in scene");

            if (allButtons.Length == 0)
                return;

            int wiredCount = 0;

            foreach (var btn in allButtons)
            {
                string name = btn.gameObject.name;

                // Wire mission buttons
                if (name.StartsWith("vor_") || name.StartsWith("time_") || name.StartsWith("emergency_"))
                {
                    string missionId = name;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LoadMission(missionId));
                    Debug.Log($"ButtonWiring: Wired mission button '{name}'");
                    wiredCount++;
                }
                // Wire heading buttons
                else if (name == "L30") { WireHeadingButton(btn, -30); wiredCount++; }
                else if (name == "L10") { WireHeadingButton(btn, -10); wiredCount++; }
                else if (name == "R10") { WireHeadingButton(btn, 10); wiredCount++; }
                else if (name == "R30") { WireHeadingButton(btn, 30); wiredCount++; }
            }

            if (wiredCount > 0)
            {
                hasWiredButtons = true;
                Debug.Log($"ButtonWiring: Successfully wired {wiredCount} buttons!");
            }
        }

        private void WireHeadingButton(Button btn, float delta)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => AdjustHeading(delta));
            Debug.Log($"ButtonWiring: Wired heading button '{btn.name}' ({delta:+0;-0}°)");
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
