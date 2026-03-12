using System;
using UnityEngine;
using IFRTrainer.Core;

namespace IFRTrainer.Navigation
{
    /// <summary>
    /// Represents a VOR/NAV receiver in the aircraft.
    /// Handles tuning frequency and setting OBS.
    /// </summary>
    public class VORReceiver : MonoBehaviour
    {
        [Header("Receiver Settings")]
        [SerializeField] private float tunedFrequency = 113.6f;
        [SerializeField] private float selectedRadial = 0f;

        [Header("Frequency Limits")]
        [SerializeField] private float minFrequency = 108.0f;
        [SerializeField] private float maxFrequency = 117.95f;
        [SerializeField] private float frequencyStep = 0.05f;

        public event Action<float> OnFrequencyChanged;
        public event Action<float> OnOBSChanged;
        public event Action<VORIndication> OnIndicationUpdated;

        public float TunedFrequency => tunedFrequency;
        public float SelectedRadial => selectedRadial;
        public VORIndication CurrentIndication { get; private set; }

        private void Start()
        {
            // Initialize the native receiver
            TuneFrequency(tunedFrequency);
            SetOBS(selectedRadial);
        }

        private void Update()
        {
            // Update indication from native plugin
            CurrentIndication = SimulationBridge.GetVORIndication();
            OnIndicationUpdated?.Invoke(CurrentIndication);
        }

        public void TuneFrequency(float frequency)
        {
            tunedFrequency = Mathf.Clamp(frequency, minFrequency, maxFrequency);

            // Round to nearest valid frequency (0.05 MHz steps)
            tunedFrequency = Mathf.Round(tunedFrequency / frequencyStep) * frequencyStep;

            SimulationBridge.TuneNAV(tunedFrequency);
            OnFrequencyChanged?.Invoke(tunedFrequency);
        }

        public void IncrementFrequency()
        {
            TuneFrequency(tunedFrequency + frequencyStep);
        }

        public void DecrementFrequency()
        {
            TuneFrequency(tunedFrequency - frequencyStep);
        }

        public void SetOBS(float radial)
        {
            selectedRadial = NormalizeAngle(radial);
            SimulationBridge.SetOBS(selectedRadial);
            OnOBSChanged?.Invoke(selectedRadial);
        }

        public void IncrementOBS(float degrees = 1f)
        {
            SetOBS(selectedRadial + degrees);
        }

        public void DecrementOBS(float degrees = 1f)
        {
            SetOBS(selectedRadial - degrees);
        }

        public void SetOBSToCurrentRadial()
        {
            if (CurrentIndication.IsValid)
            {
                // Calculate current radial from bearing
                float currentRadial = NormalizeAngle(CurrentIndication.BearingToStation + 180f);
                SetOBS(currentRadial);
            }
        }

        private float NormalizeAngle(float angle)
        {
            while (angle < 0) angle += 360f;
            while (angle >= 360f) angle -= 360f;
            return angle;
        }

        /// <summary>
        /// Gets the current radial the aircraft is on (FROM the station).
        /// </summary>
        public float GetCurrentRadial()
        {
            if (!CurrentIndication.IsValid) return 0f;
            return NormalizeAngle(CurrentIndication.BearingToStation + 180f);
        }

        /// <summary>
        /// Gets the deviation from the selected radial in degrees.
        /// </summary>
        public float GetDeviationDegrees()
        {
            return CurrentIndication.CDIDeflection * 10f; // Full scale = 10 degrees
        }
    }
}
