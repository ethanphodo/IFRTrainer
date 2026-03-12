using UnityEngine;
using IFRTrainer.Core;
using IFRTrainer.Mission;
using IFRTrainer.Navigation;

namespace IFRTrainer.Editor
{
    /// <summary>
    /// Runtime helper for testing the IFR Trainer.
    /// Provides keyboard shortcuts and debug info.
    /// </summary>
    public class TestMissionLoader : MonoBehaviour
    {
        [Header("Quick Load")]
        [SerializeField] private string defaultMission = "vor_intercept_001";
        [SerializeField] private bool autoLoadOnStart = false;

        [Header("Debug Display")]
        [SerializeField] private bool showDebugInfo = true;

        private MissionManager missionManager;
        private VORReceiver vorReceiver;
        private GUIStyle debugStyle;

        private void Start()
        {
            missionManager = FindFirstObjectByType<MissionManager>();
            vorReceiver = FindFirstObjectByType<VORReceiver>();

            if (autoLoadOnStart && missionManager != null)
            {
                Invoke(nameof(LoadDefaultMission), 0.5f);
            }

            Debug.Log("=== IFR TRAINER TEST MODE ===");
            Debug.Log("Keyboard shortcuts:");
            Debug.Log("  1-5: Load missions 1-5");
            Debug.Log("  R: Restart current mission");
            Debug.Log("  P: Pause/unpause");
            Debug.Log("  M: Show mission select");
            Debug.Log("  Arrow Left/Right: Adjust OBS");
            Debug.Log("  Arrow Up/Down: Adjust heading");
            Debug.Log("  +/-: Zoom radar");
            Debug.Log("==============================");
        }

        private void LoadDefaultMission()
        {
            if (missionManager != null)
            {
                missionManager.LoadMission(defaultMission);
                missionManager.StartMission();

                // Hide mission select overlay
                var overlay = GameObject.Find("MissionSelectOverlay");
                if (overlay != null) overlay.SetActive(false);
            }
        }

        private void Update()
        {
            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            // Mission loading (1-5)
            if (Input.GetKeyDown(KeyCode.Alpha1)) LoadMission("vor_intercept_001");
            if (Input.GetKeyDown(KeyCode.Alpha2)) LoadMission("vor_intercept_002");
            if (Input.GetKeyDown(KeyCode.Alpha3)) LoadMission("vor_intercept_003");
            if (Input.GetKeyDown(KeyCode.Alpha4)) LoadMission("vor_tracking_001");
            if (Input.GetKeyDown(KeyCode.Alpha5)) LoadMission("time_attack_001");

            // Restart
            if (Input.GetKeyDown(KeyCode.R) && missionManager != null)
            {
                missionManager.StartMission();
            }

            // Pause
            if (Input.GetKeyDown(KeyCode.P) && GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }

            // Mission select
            if (Input.GetKeyDown(KeyCode.M))
            {
                var overlay = GameObject.Find("MissionSelectOverlay");
                if (overlay != null) overlay.SetActive(!overlay.activeSelf);
            }

            // OBS adjustment
            if (Input.GetKeyDown(KeyCode.LeftArrow) && vorReceiver != null)
            {
                vorReceiver.DecrementOBS(5f);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) && vorReceiver != null)
            {
                vorReceiver.IncrementOBS(5f);
            }

            // Heading adjustment
            if (Input.GetKeyDown(KeyCode.UpArrow) && GameManager.Instance != null)
            {
                float hdg = (GameManager.Instance.CurrentDynamics.Heading + 10) % 360;
                GameManager.Instance.SetTargetHeading(hdg);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) && GameManager.Instance != null)
            {
                float hdg = (GameManager.Instance.CurrentDynamics.Heading - 10 + 360) % 360;
                GameManager.Instance.SetTargetHeading(hdg);
            }

            // Radar zoom
            var radar = FindFirstObjectByType<UI.RadarScope>();
            if (radar != null)
            {
                if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
                {
                    radar.ZoomIn();
                }
                if (Input.GetKeyDown(KeyCode.Minus))
                {
                    radar.ZoomOut();
                }
            }
        }

        private void LoadMission(string missionId)
        {
            if (missionManager != null)
            {
                missionManager.LoadMission(missionId);
                missionManager.StartMission();

                var overlay = GameObject.Find("MissionSelectOverlay");
                if (overlay != null) overlay.SetActive(false);

                Debug.Log($"Loaded mission: {missionId}");
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo || GameManager.Instance == null || !GameManager.Instance.IsInitialized)
                return;

            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.label);
                debugStyle.fontSize = 14;
                debugStyle.normal.textColor = Color.green;
            }

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            var pos = GameManager.Instance.CurrentPosition;
            var dyn = GameManager.Instance.CurrentDynamics;
            var vor = GameManager.Instance.CurrentVORIndication;

            GUILayout.Label($"Position: {pos.Latitude:F4}, {pos.Longitude:F4}", debugStyle);
            GUILayout.Label($"Altitude: {pos.Altitude:F0} ft", debugStyle);
            GUILayout.Label($"Heading: {dyn.Heading:F1}° | Speed: {dyn.Speed:F0} kts", debugStyle);
            GUILayout.Label($"Bank: {dyn.BankAngle:F1}° | VS: {dyn.VerticalSpeed:F0} fpm", debugStyle);

            GUILayout.Space(5);

            if (vor.IsValid)
            {
                GUILayout.Label($"VOR: {(vor.ToFromFlag == ToFromFlag.To ? "TO" : "FROM")}", debugStyle);
                GUILayout.Label($"CDI: {vor.CDIDeflection:F2} | Bearing: {vor.BearingToStation:F1}°", debugStyle);
                GUILayout.Label($"Distance: {vor.DistanceNM:F1} NM", debugStyle);
                GUILayout.Label($"OBS: {SimulationBridge.GetOBS():F0}°", debugStyle);
            }
            else
            {
                GUILayout.Label("VOR: NO SIGNAL", debugStyle);
            }

            GUILayout.Space(5);

            if (missionManager != null)
            {
                GUILayout.Label($"Mission: {missionManager.State}", debugStyle);
                GUILayout.Label($"Time: {missionManager.MissionTime:F1}s", debugStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
