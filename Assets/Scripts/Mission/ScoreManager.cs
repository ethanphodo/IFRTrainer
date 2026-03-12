using System;
using UnityEngine;
using IFRTrainer.Core;

namespace IFRTrainer.Mission
{
    /// <summary>
    /// Tracks and calculates mission scoring based on performance.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Scoring Constants")]
        [SerializeField] private int baseCompletionScore = 100;
        [SerializeField] private int maxTimeBonus = 50;
        [SerializeField] private int perfectInterceptBonus = 25;
        [SerializeField] private int maxTrackingBonus = 25;
        [SerializeField] private float perfectInterceptThreshold = 0.5f;

        [Header("Grade Thresholds")]
        [SerializeField] private int gradeSPlus = 175;
        [SerializeField] private int gradeA = 150;
        [SerializeField] private int gradeB = 120;
        [SerializeField] private int gradeC = 90;
        [SerializeField] private int gradeD = 75;

        public event Action<int> OnScoreChanged;

        public int CurrentScore { get; private set; }
        public float InterceptDeviation { get; private set; }
        public float TrackingTimeOnRadial { get; private set; }
        public float AverageDeviation { get; private set; }

        private MissionData currentMission;
        private bool isTracking;

        public void StartMission(MissionData mission)
        {
            currentMission = mission;
            CurrentScore = 0;
            InterceptDeviation = float.MaxValue;
            TrackingTimeOnRadial = 0;
            AverageDeviation = 0;
            isTracking = false;
        }

        public void RecordInterceptDeviation(float deviation)
        {
            if (deviation < InterceptDeviation)
            {
                InterceptDeviation = deviation;
            }
        }

        public void UpdateTrackingStats()
        {
            var stats = SimulationBridge.GetTrackingStats();
            TrackingTimeOnRadial = stats.TimeOnRadial;
            AverageDeviation = stats.AverageDeviation;
        }

        public MissionResult CalculateFinalScore(float missionTime)
        {
            var result = new MissionResult();

            // Base completion score
            result.BaseScore = baseCompletionScore;

            // Time bonus
            float threshold = currentMission?.scoring?.time_bonus_threshold_seconds ?? 180f;
            if (missionTime < threshold)
            {
                float timeRatio = 1f - (missionTime / threshold);
                result.TimeBonus = Mathf.RoundToInt(maxTimeBonus * timeRatio);
            }

            // Perfect intercept bonus
            if (InterceptDeviation <= perfectInterceptThreshold)
            {
                result.InterceptBonus = perfectInterceptBonus;
            }
            else if (InterceptDeviation < 2f)
            {
                // Partial bonus for good intercept
                float ratio = 1f - (InterceptDeviation / 2f);
                result.InterceptBonus = Mathf.RoundToInt(perfectInterceptBonus * ratio * 0.5f);
            }

            // Tracking bonus
            UpdateTrackingStats();
            var trackingStats = SimulationBridge.GetTrackingStats();

            if (trackingStats.TotalTime > 0)
            {
                float onRadialRatio = trackingStats.TimeOnRadial / trackingStats.TotalTime;
                result.TrackingBonus = Mathf.RoundToInt(maxTrackingBonus * onRadialRatio);
            }

            // Deviation penalty
            float penaltyPerDegree = currentMission?.scoring?.deviation_penalty_per_degree ?? 5f;
            result.DeviationPenalty = Mathf.RoundToInt(AverageDeviation * penaltyPerDegree);
            result.AverageDeviation = AverageDeviation;

            // Calculate total
            result.TotalScore = result.BaseScore + result.TimeBonus + result.InterceptBonus +
                               result.TrackingBonus - result.DeviationPenalty;
            result.TotalScore = Mathf.Max(0, result.TotalScore);

            // Determine grade
            result.Grade = CalculateGrade(result.TotalScore);

            CurrentScore = result.TotalScore;
            OnScoreChanged?.Invoke(CurrentScore);

            return result;
        }

        public string CalculateGrade(int score)
        {
            if (score >= gradeSPlus) return "S+";
            if (score >= gradeA) return "A";
            if (score >= gradeB) return "B";
            if (score >= gradeC) return "C";
            if (score >= gradeD) return "D";
            return "F";
        }

        public int GetProjectedScore()
        {
            // Calculate current projected score without finalizing
            int projected = baseCompletionScore;

            if (InterceptDeviation <= perfectInterceptThreshold)
            {
                projected += perfectInterceptBonus;
            }

            var stats = SimulationBridge.GetTrackingStats();
            if (stats.TotalTime > 0)
            {
                float onRadialRatio = stats.TimeOnRadial / stats.TotalTime;
                projected += Mathf.RoundToInt(maxTrackingBonus * onRadialRatio);

                float penaltyPerDegree = currentMission?.scoring?.deviation_penalty_per_degree ?? 5f;
                projected -= Mathf.RoundToInt(stats.AverageDeviation * penaltyPerDegree);
            }

            return Mathf.Max(0, projected);
        }

        public string GetScoreBreakdown(MissionResult result)
        {
            return $"Base: {result.BaseScore}\n" +
                   $"Time Bonus: +{result.TimeBonus}\n" +
                   $"Intercept Bonus: +{result.InterceptBonus}\n" +
                   $"Tracking Bonus: +{result.TrackingBonus}\n" +
                   $"Deviation Penalty: -{result.DeviationPenalty}\n" +
                   $"────────────────\n" +
                   $"Total: {result.TotalScore} ({result.Grade})";
        }
    }
}
