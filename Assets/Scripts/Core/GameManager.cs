using System;
using UnityEngine;
using IFRTrainer.Mission;

namespace IFRTrainer.Core
{
    /// <summary>
    /// Main game manager - handles simulation lifecycle and game state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Simulation Settings")]
        [SerializeField] private float simulationTimeScale = 1.0f;
        [SerializeField] private bool pauseOnStart = false;

        [Header("Default Aircraft Settings")]
        [SerializeField] private double defaultLatitude = 33.94;
        [SerializeField] private double defaultLongitude = -118.40;
        [SerializeField] private float defaultAltitude = 5000f;
        [SerializeField] private float defaultHeading = 45f;
        [SerializeField] private float defaultSpeed = 150f;

        public event Action OnSimulationInitialized;
        public event Action OnSimulationShutdown;
        public event Action<bool> OnPauseStateChanged;

        public bool IsInitialized => SimulationBridge.IsInitialized;
        public bool IsPaused { get; private set; }
        public float SimulationTimeScale
        {
            get => simulationTimeScale;
            set => simulationTimeScale = Mathf.Clamp(value, 0.1f, 4.0f);
        }

        // Current aircraft state (cached each frame)
        public AircraftPosition CurrentPosition { get; private set; }
        public AircraftDynamics CurrentDynamics { get; private set; }
        public VORIndication CurrentVORIndication { get; private set; }

        private MissionManager missionManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // DontDestroyOnLoad only works for root objects
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            missionManager = GetComponent<MissionManager>();
        }

        private void Start()
        {
            IsPaused = pauseOnStart;

            if (!InitializeSimulation())
            {
                Debug.LogError("Failed to initialize IFR simulation");
            }
        }

        private void Update()
        {
            if (!IsInitialized || IsPaused)
                return;

            float dt = Time.deltaTime * simulationTimeScale;
            SimulationBridge.Update(dt);

            // Cache current state
            CurrentPosition = SimulationBridge.GetPosition();
            CurrentDynamics = SimulationBridge.GetDynamics();
            CurrentVORIndication = SimulationBridge.GetVORIndication();
        }

        private void OnDestroy()
        {
            ShutdownSimulation();
        }

        private void OnApplicationQuit()
        {
            ShutdownSimulation();
        }

        public bool InitializeSimulation()
        {
            return InitializeSimulation(defaultLatitude, defaultLongitude, defaultAltitude,
                                        defaultHeading, defaultSpeed);
        }

        public bool InitializeSimulation(double lat, double lon, float alt, float heading, float speed)
        {
            if (IsInitialized)
            {
                ShutdownSimulation();
            }

            bool success = SimulationBridge.Initialize(lat, lon, alt, heading, speed);

            if (success)
            {
                Debug.Log($"IFR Simulation initialized at ({lat:F4}, {lon:F4}), Alt: {alt}ft, Hdg: {heading}°");
                OnSimulationInitialized?.Invoke();
            }

            return success;
        }

        public void ShutdownSimulation()
        {
            if (IsInitialized)
            {
                SimulationBridge.Shutdown();
                OnSimulationShutdown?.Invoke();
                Debug.Log("IFR Simulation shutdown");
            }
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused != paused)
            {
                IsPaused = paused;
                OnPauseStateChanged?.Invoke(paused);
            }
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        // Aircraft control methods
        public void SetTargetHeading(float heading)
        {
            SimulationBridge.SetTargetHeading(heading);
        }

        public void SetTargetAltitude(float altitude)
        {
            SimulationBridge.SetTargetAltitude(altitude);
        }

        public void TurnLeft(float degrees)
        {
            float newHeading = (CurrentDynamics.Heading - degrees + 360f) % 360f;
            SetTargetHeading(newHeading);
        }

        public void TurnRight(float degrees)
        {
            float newHeading = (CurrentDynamics.Heading + degrees) % 360f;
            SetTargetHeading(newHeading);
        }

        // VOR methods
        public int AddVOR(string identifier, double lat, double lon, float frequency)
        {
            return SimulationBridge.AddVOR(identifier, lat, lon, frequency);
        }

        public void ClearVORs()
        {
            SimulationBridge.ClearVORs();
        }

        public void TuneNAV(float frequency)
        {
            SimulationBridge.TuneNAV(frequency);
        }

        public void SetOBS(float radial)
        {
            SimulationBridge.SetOBS(radial);
        }

        public float GetOBS()
        {
            return SimulationBridge.GetOBS();
        }
    }
}
