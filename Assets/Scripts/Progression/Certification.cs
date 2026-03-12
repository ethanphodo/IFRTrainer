using System;
using System.Collections.Generic;
using UnityEngine;

namespace IFRTrainer.Progression
{
    /// <summary>
    /// Certification/badge that players can earn through achievements.
    /// </summary>
    [Serializable]
    public class Certification
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string IconName { get; private set; }
        public int XPReward { get; private set; }
        public CertificationTier Tier { get; private set; }

        private Func<PlayerProgress, bool> requirement;

        public Certification(string id, string name, string description, string icon,
                            int xpReward, CertificationTier tier, Func<PlayerProgress, bool> req)
        {
            Id = id;
            Name = name;
            Description = description;
            IconName = icon;
            XPReward = xpReward;
            Tier = tier;
            requirement = req;
        }

        public bool CheckRequirement(PlayerProgress progress)
        {
            return requirement?.Invoke(progress) ?? false;
        }

        // ====================================================================
        // Static certification definitions
        // ====================================================================

        private static List<Certification> _allCertifications;

        public static List<Certification> AllCertifications
        {
            get
            {
                if (_allCertifications == null)
                    InitializeCertifications();
                return _allCertifications;
            }
        }

        public static Certification GetById(string id)
        {
            return AllCertifications.Find(c => c.Id == id);
        }

        private static void InitializeCertifications()
        {
            _allCertifications = new List<Certification>
            {
                // === Beginner Certifications ===
                new Certification(
                    "first_flight",
                    "First Flight",
                    "Complete your first mission",
                    "badge_first_flight",
                    50,
                    CertificationTier.Bronze,
                    p => p.Stats.missionsCompleted >= 1
                ),

                new Certification(
                    "student_pilot",
                    "Student Pilot",
                    "Complete 5 missions",
                    "badge_student",
                    100,
                    CertificationTier.Bronze,
                    p => p.Stats.missionsCompleted >= 5
                ),

                new Certification(
                    "vor_basics",
                    "VOR Fundamentals",
                    "Earn a B grade or better on any mission",
                    "badge_vor_basics",
                    75,
                    CertificationTier.Bronze,
                    p => p.Stats.gradesB + p.Stats.gradesA + p.Stats.gradesS >= 1
                ),

                // === Intermediate Certifications ===
                new Certification(
                    "private_pilot",
                    "Private Pilot",
                    "Complete 15 missions",
                    "badge_private",
                    200,
                    CertificationTier.Silver,
                    p => p.Stats.missionsCompleted >= 15
                ),

                new Certification(
                    "precision_navigator",
                    "Precision Navigator",
                    "Earn 5 A grades",
                    "badge_precision",
                    250,
                    CertificationTier.Silver,
                    p => p.Stats.gradesA + p.Stats.gradesS >= 5
                ),

                new Certification(
                    "consistent_performer",
                    "Consistent Performer",
                    "Complete 10 missions without failing any",
                    "badge_consistent",
                    200,
                    CertificationTier.Silver,
                    p => p.Stats.missionsCompleted >= 10 &&
                         p.Stats.missionsCompleted == p.Stats.missionsAttempted
                ),

                new Certification(
                    "daily_devotee",
                    "Daily Devotee",
                    "Complete daily challenges 7 days in a row",
                    "badge_daily",
                    300,
                    CertificationTier.Silver,
                    p => p.Stats.longestDailyStreak >= 7
                ),

                new Certification(
                    "radial_tracker",
                    "Radial Tracker",
                    "Accumulate 30 minutes of radial tracking time",
                    "badge_tracker",
                    200,
                    CertificationTier.Silver,
                    p => p.Stats.totalRadialTrackingTime >= 1800
                ),

                // === Advanced Certifications ===
                new Certification(
                    "instrument_rated",
                    "Instrument Rated",
                    "Complete 30 missions with B or better average",
                    "badge_instrument",
                    500,
                    CertificationTier.Gold,
                    p => p.Stats.missionsCompleted >= 30 &&
                         (p.Stats.gradesB + p.Stats.gradesA + p.Stats.gradesS) >=
                         p.Stats.missionsCompleted * 0.8f
                ),

                new Certification(
                    "vor_master",
                    "VOR Master",
                    "Earn 10 S+ grades",
                    "badge_vor_master",
                    750,
                    CertificationTier.Gold,
                    p => p.Stats.gradesS >= 10
                ),

                new Certification(
                    "emergency_handler",
                    "Emergency Handler",
                    "Successfully handle 5 emergency scenarios",
                    "badge_emergency",
                    400,
                    CertificationTier.Gold,
                    p => p.Stats.emergenciesHandled >= 5
                ),

                new Certification(
                    "marathon_flyer",
                    "Marathon Flyer",
                    "Accumulate 5 hours of flight time",
                    "badge_marathon",
                    500,
                    CertificationTier.Gold,
                    p => p.Stats.totalFlightTimeSeconds >= 18000
                ),

                new Certification(
                    "streak_master",
                    "Streak Master",
                    "Maintain a 30-day daily challenge streak",
                    "badge_streak",
                    1000,
                    CertificationTier.Gold,
                    p => p.Stats.longestDailyStreak >= 30
                ),

                // === Expert Certifications ===
                new Certification(
                    "airline_transport",
                    "Airline Transport Pilot",
                    "Complete 100 missions",
                    "badge_atp",
                    1500,
                    CertificationTier.Platinum,
                    p => p.Stats.missionsCompleted >= 100
                ),

                new Certification(
                    "perfectionist",
                    "Perfectionist",
                    "Achieve 25 perfect intercepts (<0.5° deviation)",
                    "badge_perfect",
                    1000,
                    CertificationTier.Platinum,
                    p => p.Stats.perfectIntercepts >= 25
                ),

                new Certification(
                    "legend",
                    "Living Legend",
                    "Earn all other certifications",
                    "badge_legend",
                    2500,
                    CertificationTier.Diamond,
                    p => {
                        // Check if all other certs are earned
                        int requiredCount = AllCertifications.Count - 1; // Exclude this one
                        return p.EarnedCertifications.Count >= requiredCount;
                    }
                )
            };
        }
    }

    public enum CertificationTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond
    }
}
