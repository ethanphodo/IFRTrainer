using UnityEngine;
using UnityEngine.UI;
using IFRTrainer.Core;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Runtime script that wires up UI button click handlers.
    /// Attached to MainCanvas to connect buttons to game logic.
    /// </summary>
    public class ButtonWiring : MonoBehaviour
    {
        private void Start()
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
            if (buttonObj != null)
            {
                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(action);
                }
            }
        }

        private void AdjustHeading(float delta)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsInitialized)
            {
                float current = gm.CurrentDynamics.Heading;
                float newHdg = (current + delta + 360) % 360;
                gm.SetTargetHeading(newHdg);
                Debug.Log($"Heading: {current:F0}° → {newHdg:F0}°");
            }
        }

        private void LoadMission(string missionId)
        {
            var mm = FindFirstObjectByType<MissionManager>();
            if (mm != null)
            {
                mm.LoadMission(missionId);
                mm.StartMission();

                // Hide mission select overlay
                var overlay = GameObject.Find("MissionSelectOverlay");
                if (overlay != null)
                    overlay.SetActive(false);

                Debug.Log($"Loaded mission: {missionId}");
            }
        }
    }
}
