using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Fatigue
{
    #region Enums

    /// <summary>
    /// Different types of activities that cause fatigue
    /// </summary>
    public enum FatigueActivity
    {
        Walking,        // Low intensity movement
        Running,        // Moderate pace running
        Sprinting,      // High intensity sprinting
        Contest,        // Physical contests (ruck, marking)
        Tackling,       // Tackling actions
        Marking,        // Marking contests
        Kicking,        // Kicking the ball
        Handballing,    // Handballing
        Stationary      // Standing/minimal movement
    }

    /// <summary>
    /// Types of recovery activities
    /// </summary>
    public enum RecoveryType
    {
        PassiveRest,     // Standing/walking recovery
        ActiveRecovery,  // Light jogging for recovery
        QuarterBreak,    // Quarter time break
        HalfTimeBreak,   // Half time break
        Substitution,    // Off the field (no recovery while on bench)
        Medical          // Medical treatment/recovery
    }

    /// <summary>
    /// Fatigue zones for easy classification
    /// </summary>
    public enum FatigueZone
    {
        Fresh,      // 0-20% fatigue
        Light,      // 20-40% fatigue
        Moderate,   // 40-65% fatigue
        Heavy,      // 65-85% fatigue
        Exhausted   // 85-100% fatigue
    }

    /// <summary>
    /// Substitution urgency levels based on fatigue
    /// </summary>
    public enum SubstitutionUrgency
    {
        None,           // Player is fine
        Consider,       // Player could benefit from a rest
        Recommended,    // Player should be substituted soon
        Urgent          // Player needs immediate substitution
    }

    /// <summary>
    /// Types of fatigue-related recommendations
    /// </summary>
    public enum RecommendationType
    {
        TacticChange,
        SingleSubstitution,
        MultipleSubstitutions,
        PositionRotation,
        IntensityReduction,
        RecoveryFocus
    }

    /// <summary>
    /// Priority levels for recommendations
    /// </summary>
    public enum RecommendationPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    #endregion

    #region Player Fatigue State

    /// <summary>
    /// Complete fatigue state for an individual player
    /// </summary>
    public class PlayerFatigueState
    {
        public Guid PlayerId { get; set; }
        public Role Position { get; set; }
        public int Age { get; set; }
        
        // Current state
        public float CurrentFatigue { get; set; } = 0f; // 0-100%
        public float CurrentFitness { get; set; } = 100f; // Current fitness level
        public float MaxFitness { get; set; } = 100f; // Maximum possible fitness
        public float FitnessLevel { get; set; } = 100f; // Base fitness level
        public FatigueZone CurrentZone { get; set; } = FatigueZone.Fresh;
        
        // Base attributes
        public float BaseEndurance { get; set; } = 70f; // 0-100 endurance rating
        public float FatigueResistance { get; set; } = 1.0f; // Multiplier for fatigue accumulation
        public float RecoveryRate { get; set; } = 1.0f; // Multiplier for recovery
        
        // Tracking data
        public float TotalFatigueAccumulated { get; set; } = 0f;
        public float TotalRecovery { get; set; } = 0f;
        public int MinutesPlayed { get; set; } = 0;
        
        // Activity history
        public List<FatigueActivityRecord> ActivityHistory { get; set; } = new List<FatigueActivityRecord>();
        public List<FatigueZoneTransition> ZoneTransitions { get; set; } = new List<FatigueZoneTransition>();

        public PlayerFatigueState(Guid playerId)
        {
            PlayerId = playerId;
        }

        /// <summary>
        /// Calculate fatigue percentage (0-100)
        /// </summary>
        public float GetFatiguePercentage()
        {
            return Math.Min(100f, CurrentFatigue);
        }

        /// <summary>
        /// Calculate remaining energy percentage (100-0)
        /// </summary>
        public float GetEnergyPercentage()
        {
            return Math.Max(0f, 100f - CurrentFatigue);
        }

        /// <summary>
        /// Get time spent in current fatigue zone
        /// </summary>
        public TimeSpan GetTimeInCurrentZone()
        {
            var lastTransition = ZoneTransitions.LastOrDefault();
            if (lastTransition != null)
            {
                return DateTime.Now - lastTransition.Timestamp;
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Check if player is in a dangerous fatigue state
        /// </summary>
        public bool IsInDangerousState()
        {
            return CurrentZone >= FatigueZone.Heavy;
        }
    }

    /// <summary>
    /// Record of a specific fatigue-causing activity
    /// </summary>
    public class FatigueActivityRecord
    {
        public FatigueActivity Activity { get; set; }
        public float Duration { get; set; } // Duration in seconds
        public float FatigueCost { get; set; } // Fatigue points added
        public DateTime Timestamp { get; set; }
        public string Context { get; set; } = ""; // Additional context (e.g., "Sprint to contest")

        public override string ToString()
        {
            return $"{Activity} for {Duration:F1}s (+{FatigueCost:F1} fatigue) at {Timestamp:HH:mm:ss}";
        }
    }

    /// <summary>
    /// Record of transitions between fatigue zones
    /// </summary>
    public class FatigueZoneTransition
    {
        public FatigueZone FromZone { get; set; }
        public FatigueZone ToZone { get; set; }
        public DateTime Timestamp { get; set; }
        public float FatigueLevel { get; set; }

        public override string ToString()
        {
            return $"{FromZone} → {ToZone} ({FatigueLevel:F1}%) at {Timestamp:HH:mm:ss}";
        }
    }

    #endregion

    #region Position Profiles

    /// <summary>
    /// Position-specific fatigue characteristics
    /// </summary>
    public class PositionFatigueProfile
    {
        public float BaseFatigueRate { get; set; } = 1.0f; // Base fatigue accumulation rate
        public float RunningFatigueMultiplier { get; set; } = 1.0f; // Multiplier for running activities
        public float ContestFatigueMultiplier { get; set; } = 1.0f; // Multiplier for contest activities
        public float BaseRecoveryRate { get; set; } = 1.0f; // Base recovery rate
        public float FatigueResistance { get; set; } = 1.0f; // Resistance to fatigue (lower = more resistant)
        
        // Position-specific attributes most affected by fatigue
        public string[] PrimaryAttributes { get; set; } = new string[0];
        
        /// <summary>
        /// Calculate relative fatigue load for this position
        /// </summary>
        public float CalculateRelativeFatigueLoad()
        {
            return (BaseFatigueRate + RunningFatigueMultiplier + ContestFatigueMultiplier) / 3f;
        }

        /// <summary>
        /// Get position difficulty rating (1-5)
        /// </summary>
        public int GetDifficultyRating()
        {
            float load = CalculateRelativeFatigueLoad();
            return load switch
            {
                < 1.2f => 1, // Easy
                < 1.5f => 2, // Moderate
                < 1.8f => 3, // Hard
                < 2.1f => 4, // Very Hard
                _ => 5       // Extreme
            };
        }
    }

    #endregion

    #region Performance Impact

    /// <summary>
    /// Performance impact modifiers from fatigue
    /// </summary>
    public class FatiguePerformanceImpact
    {
        // Overall modifiers
        public float OverallPerformanceModifier { get; set; } = 0f; // -1.0 to +0.1
        public float InjuryRiskMultiplier { get; set; } = 1.0f; // 1.0 = normal risk

        // Specific attribute impacts
        public float SpeedReduction { get; set; } = 0f; // 0.0 to 1.0 (percentage reduction)
        public float AccuracyReduction { get; set; } = 0f; // 0.0 to 1.0
        public float EnduranceReduction { get; set; } = 0f; // 0.0 to 1.0
        public float DecisionMakingImpact { get; set; } = 0f; // 0.0 to 1.0
        public float RecoveryRateReduction { get; set; } = 0f; // 0.0 to 1.0

        /// <summary>
        /// Calculate combined performance impact score
        /// </summary>
        public float GetCombinedImpactScore()
        {
            float impactScore = Math.Abs(OverallPerformanceModifier) * 2f;
            impactScore += (SpeedReduction + AccuracyReduction + EnduranceReduction + DecisionMakingImpact) / 4f;
            impactScore += (InjuryRiskMultiplier - 1f) * 0.5f;
            
            return Math.Min(2.0f, impactScore); // Cap at 2.0
        }

        /// <summary>
        /// Get impact severity description
        /// </summary>
        public string GetImpactSeverity()
        {
            float score = GetCombinedImpactScore();
            return score switch
            {
                < 0.2f => "Minimal",
                < 0.5f => "Light",
                < 0.8f => "Moderate", 
                < 1.2f => "Heavy",
                _ => "Severe"
            };
        }
    }

    #endregion

    #region Team Analysis

    /// <summary>
    /// Team-wide fatigue analysis
    /// </summary>
    public class TeamFatigueAnalysis
    {
        public float AverageFatigue { get; set; } = 0f;
        public float MaxFatigue { get; set; } = 0f;
        public float MinFatigue { get; set; } = 0f;
        public Dictionary<FatigueZone, int> ZoneDistribution { get; set; } = new Dictionary<FatigueZone, int>();
        public List<Guid> PlayersNeedingSubstitution { get; set; } = new List<Guid>();
        
        // Team performance impacts
        public float TeamPerformanceImpact { get; set; } = 0f;
        public float TeamSpeedImpact { get; set; } = 0f;
        public float TeamAccuracyImpact { get; set; } = 0f;

        /// <summary>
        /// Get team fatigue status description
        /// </summary>
        public string GetTeamFatigueStatus()
        {
            return AverageFatigue switch
            {
                < 30f => "Fresh",
                < 50f => "Moderate",
                < 70f => "Tired",
                < 85f => "Fatigued",
                _ => "Exhausted"
            };
        }

        /// <summary>
        /// Calculate substitution priority
        /// </summary>
        public int GetSubstitutionPriority()
        {
            int urgentCount = ZoneDistribution.GetValueOrDefault(FatigueZone.Exhausted, 0);
            int heavyCount = ZoneDistribution.GetValueOrDefault(FatigueZone.Heavy, 0);
            
            return urgentCount switch
            {
                >= 3 => 5, // Critical
                >= 2 => 4, // Urgent  
                >= 1 => 3, // High
                _ => heavyCount >= 4 ? 2 : 1 // Medium or Low
            };
        }
    }

    /// <summary>
    /// Position-specific fatigue data for analysis
    /// </summary>
    public class PositionFatigueData
    {
        public float AverageFatigue { get; set; }
        public float MaxFatigue { get; set; }
        public int PlayerCount { get; set; }
        
        /// <summary>
        /// Check if this position group needs attention
        /// </summary>
        public bool NeedsAttention()
        {
            return AverageFatigue > 65f || MaxFatigue > 80f;
        }
    }

    /// <summary>
    /// Fatigue-based tactical recommendation
    /// </summary>
    public class FatigueTacticalRecommendation
    {
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Description { get; set; } = "";
        public string Suggestion { get; set; } = "";
        public List<Guid> AffectedPlayers { get; set; } = new List<Guid>();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Get formatted recommendation for display
        /// </summary>
        public string GetFormattedRecommendation()
        {
            string priorityIcon = Priority switch
            {
                RecommendationPriority.Urgent => "🔴",
                RecommendationPriority.High => "🟡",
                RecommendationPriority.Medium => "🟠",
                _ => "🟢"
            };

            return $"{priorityIcon} {Description}: {Suggestion}";
        }
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Global fatigue system configuration
    /// </summary>
    public class FatigueConfiguration
    {
        // Base rates
        public float BaseRecoveryRate { get; set; } = 2.5f; // Base recovery points per second

        // Risk thresholds
        public float InjuryRiskThreshold { get; set; } = 0.05f; // 5% chance per check when heavily fatigued
        public float PerformanceImpactThreshold { get; set; } = 40f; // Fatigue % where impacts start

        // Zone thresholds (can be customized)
        public float FreshZoneMax { get; set; } = 20f;
        public float LightZoneMax { get; set; } = 40f;
        public float ModerateZoneMax { get; set; } = 65f;
        public float HeavyZoneMax { get; set; } = 85f;

        // Update frequencies
        public float FatigueUpdateInterval { get; set; } = 1.0f; // Update every second
        public float RecoveryUpdateInterval { get; set; } = 2.0f; // Update every 2 seconds

        // Match-specific settings
        public bool EnableWeatherEffects { get; set; } = true;
        public bool EnablePositionSpecificFatigue { get; set; } = true;
        public bool EnableAgeEffects { get; set; } = true;
        public bool EnableFitnessEffects { get; set; } = true;

        /// <summary>
        /// Create a more lenient configuration for lower difficulties
        /// </summary>
        public static FatigueConfiguration CreateLenientConfiguration()
        {
            return new FatigueConfiguration
            {
                BaseRecoveryRate = 3.5f, // Faster recovery
                InjuryRiskThreshold = 0.03f, // Lower injury risk
                FreshZoneMax = 25f, // Larger fresh zone
                LightZoneMax = 45f,
                ModerateZoneMax = 70f
            };
        }

        /// <summary>
        /// Create a more challenging configuration for higher difficulties
        /// </summary>
        public static FatigueConfiguration CreateChallengingConfiguration()
        {
            return new FatigueConfiguration
            {
                BaseRecoveryRate = 1.8f, // Slower recovery
                InjuryRiskThreshold = 0.08f, // Higher injury risk
                FreshZoneMax = 15f, // Smaller fresh zone
                LightZoneMax = 35f,
                ModerateZoneMax = 60f
            };
        }
    }

    #endregion

    #region Statistics and Analytics

    /// <summary>
    /// Match-wide fatigue statistics
    /// </summary>
    public class MatchFatigueStatistics
    {
        public Dictionary<Guid, PlayerMatchFatigueStats> PlayerStats { get; set; } = new Dictionary<Guid, PlayerMatchFatigueStats>();
        public float TotalFatigueGenerated { get; set; }
        public float TotalRecoveryApplied { get; set; }
        public int TotalSubstitutionsDueToFatigue { get; set; }
        public Dictionary<FatigueActivity, float> ActivityFatigueBreakdown { get; set; } = new Dictionary<FatigueActivity, float>();

        /// <summary>
        /// Calculate match fatigue efficiency
        /// </summary>
        public float CalculateMatchFatigueEfficiency()
        {
            if (TotalFatigueGenerated == 0) return 1.0f;
            return TotalRecoveryApplied / TotalFatigueGenerated;
        }

        /// <summary>
        /// Get most fatiguing activities
        /// </summary>
        public List<KeyValuePair<FatigueActivity, float>> GetMostFatiguingActivities()
        {
            return ActivityFatigueBreakdown.OrderByDescending(kvp => kvp.Value).ToList();
        }
    }

    /// <summary>
    /// Individual player match fatigue statistics
    /// </summary>
    public class PlayerMatchFatigueStats
    {
        public Guid PlayerId { get; set; }
        public float PeakFatigue { get; set; }
        public float AverageFatigue { get; set; }
        public float TotalFatigueAccumulated { get; set; }
        public float TotalRecovery { get; set; }
        public int MinutesInHeavyFatigue { get; set; }
        public int ZoneTransitions { get; set; }
        public Dictionary<FatigueActivity, int> ActivityCounts { get; set; } = new Dictionary<FatigueActivity, int>();

        /// <summary>
        /// Calculate player's fatigue management rating (0-100)
        /// </summary>
        public float CalculateFatigueManagementRating()
        {
            float efficiency = TotalFatigueAccumulated > 0 ? TotalRecovery / TotalFatigueAccumulated : 1.0f;
            float peakPenalty = PeakFatigue > 85f ? 0.8f : 1.0f;
            float heavyTimePenalty = 1.0f - (MinutesInHeavyFatigue / 60f); // Penalty for time in heavy fatigue

            return Math.Max(0f, Math.Min(100f, efficiency * peakPenalty * heavyTimePenalty * 100f));
        }
    }

    #endregion
}