using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Momentum
{
    #region Enums

    /// <summary>
    /// Types of momentum events that can occur during a match
    /// </summary>
    public enum MomentumEventType
    {
        // Positive momentum events
        Goal,                   // Standard goal scored
        QuickGoals,             // Multiple goals in quick succession
        BrilliantGoal,          // Spectacular individual goal
        RunningGoal,            // Goal from running play
        SpectacularMark,        // Amazing mark that lifts the crowd
        DefensiveStop,          // Key defensive play
        Intercept,              // Ball intercepted
        FromBehind,             // Goal scored from behind
        CrowdBoost,             // Crowd energy lifting the team
        UmpireDecision,         // Favorable umpire decision
        CoachingChange,         // Tactical change that works

        // Negative momentum events
        Turnover,               // Costly turnover
        MissedEasy,             // Easy shot missed
        FreeKickSpray,          // Multiple free kicks against in succession
        Injury,                 // Player injury
        SinBin,                 // Player sent to sin bin
        FightBreakout,          // Players fighting
        
        // Neutral/variable events
        WeatherChange,          // Weather conditions changing
        SubstitutionImpact      // Impact of player substitutions
    }

    /// <summary>
    /// Types of pressure events that affect player decision-making
    /// </summary>
    public enum PressureEventType
    {
        // Time-based pressure
        CloseGameStart,         // Game becomes close (within 2 goals)
        FinalQuarterStart,      // Fourth quarter begins
        FinalFiveMinutes,       // Last 5 minutes of the match
        FinalTwoMinutes,        // Last 2 minutes of the match
        
        // Situation-based pressure
        ShotOnGoal,             // Taking a shot on goal
        SetShotPressure,        // Set shot for goal under pressure
        CrucialContest,         // Important contest/ruck
        TurnoverInDefense,      // Dangerous turnover in defensive area
        
        // External pressure
        InjuryToKeyPlayer,      // Important player injured
        UmpireControversy,      // Controversial umpiring decision
        CrowdPressure,          // Hostile crowd creating pressure
        CoachingDecision,       // High-stakes coaching decision
        WeatherDeteriorating,   // Weather getting worse
        MediaScrutiny,          // High-profile match with media attention
        
        // Legacy/milestone pressure
        LegacyMoment,           // Historic moment or record on the line
        RecordPressure,         // Record being threatened
        
        // Pressure relief
        PressureRelief,         // Situation that reduces pressure
        BlowoutReduction        // Score differential reduces pressure
    }

    /// <summary>
    /// Crowd mood states
    /// </summary>
    public enum CrowdMood
    {
        Hostile,        // Angry, booing
        Frustrated,     // Disappointed, restless
        Restless,       // Unsettled, murmuring
        Neutral,        // Quiet, watching
        Positive,       // Pleased, supportive
        Excited,        // Energetic, loud
        Ecstatic        // Euphoric, deafening
    }

    /// <summary>
    /// Momentum trends over time
    /// </summary>
    public enum MomentumTrend
    {
        StronglyNegative,   // Momentum dropping rapidly
        Negative,           // Momentum declining
        Stable,             // Momentum steady
        Positive,           // Momentum building
        StronglyPositive    // Momentum surging
    }

    /// <summary>
    /// Pressure trends over time
    /// </summary>
    public enum PressureTrend
    {
        Decreasing,     // Pressure reducing
        Stable,         // Pressure steady
        Increasing      // Pressure building
    }

    #endregion

    #region Event Data Classes

    /// <summary>
    /// Data for creating momentum events
    /// </summary>
    public class MomentumEventData
    {
        public MomentumEventType EventType { get; set; }
        public bool IsHomeTeam { get; set; }
        public Guid? PlayerId { get; set; }
        public float Intensity { get; set; } = 1.0f; // 0.5 to 2.0
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; }
        public int ScoreImpact { get; set; } = 0; // Points scored/conceded
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Data for creating pressure events
    /// </summary>
    public class PressureEventData
    {
        public PressureEventType EventType { get; set; }
        public List<Guid> AffectedPlayers { get; set; } = new List<Guid>();
        public float Intensity { get; set; } = 1.0f;
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; }
        public float ScoreDifferential { get; set; }
        public string Description { get; set; } = "";
    }

    #endregion

    #region State Classes

    /// <summary>
    /// Complete momentum event record
    /// </summary>
    public class MomentumEvent
    {
        public MomentumEventType EventType { get; set; }
        public bool IsHomeTeam { get; set; }
        public Guid? PlayerId { get; set; }
        public float Intensity { get; set; }
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; }
        public int ScoreImpact { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = "";
        
        // Calculated values
        public float MomentumChange { get; set; }
        public float NewMomentumLevel { get; set; }

        public override string ToString()
        {
            string team = IsHomeTeam ? "Home" : "Away";
            return $"{EventType} ({team}) - Q{Quarter} {TimeRemaining / 60f:F1}min: {MomentumChange:+F3} momentum";
        }
    }

    /// <summary>
    /// Complete pressure event record
    /// </summary>
    public class PressureEvent
    {
        public PressureEventType EventType { get; set; }
        public List<Guid> AffectedPlayers { get; set; } = new List<Guid>();
        public float Intensity { get; set; }
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; }
        public float ScoreDifferential { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = "";
        
        // Calculated values
        public float PressureChange { get; set; }
        public float NewPressureLevel { get; set; }

        public override string ToString()
        {
            return $"{EventType} - Q{Quarter} {TimeRemaining / 60f:F1}min: {PressureChange:+F3} pressure (affects {AffectedPlayers.Count} players)";
        }
    }

    /// <summary>
    /// Current crowd state and dynamics
    /// </summary>
    public class CrowdState
    {
        public int TotalSize { get; set; }
        public float HomeSupport { get; set; } = 0.75f; // 0-1, percentage supporting home team
        public float BaseEnergy { get; set; } = 1.0f;
        public float CurrentEnergy { get; set; } = 1.0f; // 0.2 to 2.0
        public CrowdMood CurrentMood { get; set; } = CrowdMood.Neutral;
        public CrowdMood PreviousMood { get; set; } = CrowdMood.Neutral;
        public DateTime MoodChangedAt { get; set; }
        public string Venue { get; set; } = "";
        public bool IsNightGame { get; set; }
        public bool IsFinalSeries { get; set; }

        /// <summary>
        /// Calculate effective crowd influence
        /// </summary>
        public float GetCrowdInfluence()
        {
            float sizeInfluence = Math.Min(TotalSize / 50000f, 1.5f); // Cap at 75k for max influence
            float energyInfluence = CurrentEnergy;
            
            return sizeInfluence * energyInfluence;
        }

        /// <summary>
        /// Get crowd description
        /// </summary>
        public string GetCrowdDescription()
        {
            string sizeDesc = TotalSize switch
            {
                < 20000 => "Small",
                < 40000 => "Moderate",
                < 60000 => "Good",
                < 80000 => "Large",
                _ => "Massive"
            };

            string energyDesc = CurrentEnergy switch
            {
                < 0.6f => "Quiet",
                < 0.9f => "Subdued",
                < 1.2f => "Active",
                < 1.6f => "Loud",
                _ => "Deafening"
            };

            return $"{sizeDesc} {energyDesc} crowd ({TotalSize:N0}) - {CurrentMood}";
        }

        /// <summary>
        /// Clone crowd state for analytics
        /// </summary>
        public CrowdState Clone()
        {
            return new CrowdState
            {
                TotalSize = TotalSize,
                HomeSupport = HomeSupport,
                BaseEnergy = BaseEnergy,
                CurrentEnergy = CurrentEnergy,
                CurrentMood = CurrentMood,
                PreviousMood = PreviousMood,
                MoodChangedAt = MoodChangedAt,
                Venue = Venue,
                IsNightGame = IsNightGame,
                IsFinalSeries = IsFinalSeries
            };
        }
    }

    /// <summary>
    /// Team-specific momentum state
    /// </summary>
    public class TeamMomentumState
    {
        public Guid TeamId { get; set; }
        public bool IsHomeTeam { get; set; }
        public int PlayerCount { get; set; }
        public float CurrentMomentum { get; set; } = 0f; // -1 to +1
        public int MomentumStreak { get; set; } = 0; // Consecutive positive/negative events
        public DateTime LastMomentumChange { get; set; }
        public float PeakMomentum { get; set; } = 0f;
        public float LowestMomentum { get; set; } = 0f;
        public List<float> MomentumHistory { get; set; } = new List<float>();

        public TeamMomentumState(Guid teamId, bool isHomeTeam, int playerCount)
        {
            TeamId = teamId;
            IsHomeTeam = isHomeTeam;
            PlayerCount = playerCount;
            LastMomentumChange = DateTime.Now;
        }

        /// <summary>
        /// Get momentum description
        /// </summary>
        public string GetMomentumDescription()
        {
            return CurrentMomentum switch
            {
                > 0.7f => "Surging",
                > 0.4f => "Strong",
                > 0.2f => "Building",
                > 0.05f => "Slight",
                > -0.05f => "Neutral",
                > -0.2f => "Declining",
                > -0.4f => "Poor",
                > -0.7f => "Struggling",
                _ => "Terrible"
            };
        }

        /// <summary>
        /// Update momentum history
        /// </summary>
        public void RecordMomentum()
        {
            MomentumHistory.Add(CurrentMomentum);
            if (MomentumHistory.Count > 50) // Keep last 50 readings
            {
                MomentumHistory.RemoveAt(0);
            }

            // Update peaks
            if (CurrentMomentum > PeakMomentum) PeakMomentum = CurrentMomentum;
            if (CurrentMomentum < LowestMomentum) LowestMomentum = CurrentMomentum;
        }
    }

    /// <summary>
    /// Individual player pressure profile and current state
    /// </summary>
    public class PlayerPressureProfile
    {
        public Guid PlayerId { get; set; }
        public bool IsHomePlayer { get; set; }
        public Role Position { get; set; }
        
        // Base characteristics
        public float PressureResistance { get; set; } = 0.5f; // 0-1, higher = better under pressure
        public float CrowdSensitivity { get; set; } = 0.5f; // 0-1, how much crowd affects performance
        public float ExperienceLevel { get; set; } = 0.5f; // 0-1, based on age/games played
        public float MomentumSensitivity { get; set; } = 0.5f; // 0-1, how much momentum affects this player
        public float BaseRiskTolerance { get; set; } = 0.5f; // 0-1, willingness to take risks

        // Current state
        public float CurrentPressure { get; set; } = 0f; // 0-1 current pressure level
        public float CurrentConfidenceModifier { get; set; } = 0f; // -0.3 to +0.3 from momentum
        public List<PlayerPressureEvent> PressureEvents { get; set; } = new List<PlayerPressureEvent>();

        public PlayerPressureProfile(Guid playerId)
        {
            PlayerId = playerId;
        }

        /// <summary>
        /// Get current pressure level description
        /// </summary>
        public string GetPressureDescription()
        {
            return CurrentPressure switch
            {
                < 0.2f => "Relaxed",
                < 0.4f => "Composed",
                < 0.6f => "Feeling it",
                < 0.8f => "Under pressure",
                _ => "Intense pressure"
            };
        }

        /// <summary>
        /// Calculate effective pressure resistance
        /// </summary>
        public float GetEffectivePressureResistance()
        {
            // Experience helps with pressure resistance
            float experienceBonus = ExperienceLevel * 0.2f;
            return Math.Min(0.95f, PressureResistance + experienceBonus);
        }

        /// <summary>
        /// Get recent pressure events
        /// </summary>
        public List<PlayerPressureEvent> GetRecentPressureEvents(TimeSpan timespan)
        {
            var cutoff = DateTime.Now.Subtract(timespan);
            return PressureEvents.Where(e => e.Timestamp >= cutoff).ToList();
        }
    }

    /// <summary>
    /// Individual pressure event affecting a player
    /// </summary>
    public class PlayerPressureEvent
    {
        public PressureEventType EventType { get; set; }
        public float Intensity { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"{EventType}: {Intensity:F2} intensity at {Timestamp:HH:mm:ss}";
        }
    }

    #endregion

    #region Context Classes

    /// <summary>
    /// Match context for pressure calculations
    /// </summary>
    public class MatchPressureContext
    {
        public bool IsNightGame { get; set; }
        public bool IsFinalSeries { get; set; }
        public string Venue { get; set; } = "";
        public int CrowdSize { get; set; }
        public float ScoreDifferential { get; set; }
        public float CurrentPressure { get; set; }
        public PressureEventType? LastPressureEvent { get; set; }
    }

    #endregion

    #region Decision Making Impact

    /// <summary>
    /// Impact of momentum and pressure on player decision making
    /// </summary>
    public class DecisionMakingImpact
    {
        public Guid PlayerId { get; set; }
        public float BaseDecisionSpeed { get; set; } = 1.0f; // Multiplier for decision time
        public float BaseDecisionAccuracy { get; set; } = 1.0f; // Multiplier for decision quality
        public float BaseRiskTolerance { get; set; } = 0.5f; // 0-1, willingness to take risks

        /// <summary>
        /// Calculate overall decision quality modifier
        /// </summary>
        public float GetOverallDecisionQuality()
        {
            // Combine speed and accuracy with risk assessment
            float speedWeight = BaseDecisionSpeed > 1.0f ? 0.3f : 0.7f; // Fast decisions less weighted if rushed
            float accuracyWeight = 1.0f - speedWeight;
            
            return (BaseDecisionSpeed * speedWeight) + (BaseDecisionAccuracy * accuracyWeight);
        }

        /// <summary>
        /// Get decision making style description
        /// </summary>
        public string GetDecisionStyle()
        {
            bool isFast = BaseDecisionSpeed > 1.1f;
            bool isAccurate = BaseDecisionAccuracy > 1.05f;
            bool isRisky = BaseRiskTolerance > 0.6f;

            if (isFast && isAccurate) return "Sharp and decisive";
            if (isFast && !isAccurate) return "Quick but erratic";
            if (!isFast && isAccurate) return "Careful and precise";
            if (!isFast && !isAccurate) return "Hesitant and uncertain";
            
            return isRisky ? "Bold decision maker" : "Conservative approach";
        }
    }

    #endregion

    #region Analytics Classes

    /// <summary>
    /// Comprehensive analytics for the momentum and pressure system
    /// </summary>
    public class MomentumPressureAnalytics
    {
        public float CurrentMomentum { get; set; }
        public float CurrentPressure { get; set; }
        public MomentumTrend MomentumTrend { get; set; }
        public PressureTrend PressureTrend { get; set; }
        public CrowdState CrowdState { get; set; }
        public List<Guid> MostPressuredPlayers { get; set; } = new List<Guid>();
        public List<MomentumEvent> HighestMomentumEvents { get; set; } = new List<MomentumEvent>();
        public float TeamMomentumComparison { get; set; } // Home - Away momentum

        /// <summary>
        /// Get momentum description
        /// </summary>
        public string GetMomentumDescription()
        {
            string direction = CurrentMomentum switch
            {
                > 0.5f => "Strong Home momentum",
                > 0.2f => "Home momentum building",
                > 0.05f => "Slight Home edge",
                > -0.05f => "Momentum balanced",
                > -0.2f => "Slight Away edge", 
                > -0.5f => "Away momentum building",
                _ => "Strong Away momentum"
            };

            string trend = MomentumTrend switch
            {
                MomentumTrend.StronglyPositive => " and surging",
                MomentumTrend.Positive => " and building",
                MomentumTrend.Stable => " but stable",
                MomentumTrend.Negative => " but declining",
                MomentumTrend.StronglyNegative => " and dropping",
                _ => ""
            };

            return direction + trend;
        }

        /// <summary>
        /// Get pressure description
        /// </summary>
        public string GetPressureDescription()
        {
            string level = CurrentPressure switch
            {
                < 0.2f => "Low pressure environment",
                < 0.4f => "Moderate pressure",
                < 0.6f => "High pressure situation",
                < 0.8f => "Intense pressure",
                _ => "Extreme pressure cooker"
            };

            string trend = PressureTrend switch
            {
                PressureTrend.Increasing => " and rising",
                PressureTrend.Decreasing => " but easing",
                _ => ""
            };

            return level + trend;
        }

        /// <summary>
        /// Get key insights
        /// </summary>
        public List<string> GetKeyInsights()
        {
            var insights = new List<string>();

            // Momentum insights
            if (Math.Abs(CurrentMomentum) > 0.6f)
            {
                string team = CurrentMomentum > 0 ? "Home" : "Away";
                insights.Add($"{team} team has strong momentum advantage");
            }

            if (MomentumTrend == MomentumTrend.StronglyPositive || MomentumTrend == MomentumTrend.StronglyNegative)
            {
                insights.Add("Momentum is shifting rapidly");
            }

            // Pressure insights
            if (CurrentPressure > 0.7f)
            {
                insights.Add("Players under significant pressure");
            }

            if (MostPressuredPlayers.Count > 5)
            {
                insights.Add("Multiple players feeling the pressure");
            }

            // Crowd insights
            if (CrowdState != null)
            {
                if (CrowdState.CurrentEnergy > 1.5f)
                {
                    insights.Add("Crowd energy is electric");
                }
                
                if (CrowdState.CurrentMood == CrowdMood.Hostile)
                {
                    insights.Add("Hostile crowd environment");
                }
            }

            return insights;
        }
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Configuration for the momentum and pressure system
    /// </summary>
    public class MomentumConfiguration
    {
        // Base settings
        public float MomentumSensitivity { get; set; } = 1.0f; // Global sensitivity multiplier
        public float PressureSensitivity { get; set; } = 1.0f; // Global pressure multiplier
        public float MomentumDecayRate { get; set; } = 0.98f; // Per-update decay (0.98 = 2% decay)
        public float PressureDecayRate { get; set; } = 0.95f; // Per-update decay (0.95 = 5% decay)
        
        // Initial conditions
        public float InitialHomeAdvantage { get; set; } = 0.05f; // Slight initial home advantage
        public float BasePressureLevel { get; set; } = 0.1f; // Base pressure level
        
        // Crowd effects
        public float CrowdInfluenceStrength { get; set; } = 0.3f; // How much crowd affects momentum
        public float CrowdPressureStrength { get; set; } = 0.2f; // How much crowd affects pressure
        
        // Player effects
        public float PlayerMomentumSensitivityRange { get; set; } = 0.4f; // 0.3-0.7 range
        public float ExperienceBonusMax { get; set; } = 0.2f; // Max experience pressure resistance bonus
        
        // Update frequencies
        public float MomentumUpdateInterval { get; set; } = 1.0f; // Seconds
        public float PressureUpdateInterval { get; set; } = 2.0f; // Seconds
        public float CrowdUpdateInterval { get; set; } = 3.0f; // Seconds

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static MomentumConfiguration CreateDefault()
        {
            return new MomentumConfiguration();
        }

        /// <summary>
        /// Create high-intensity configuration with more dramatic swings
        /// </summary>
        public static MomentumConfiguration CreateHighIntensity()
        {
            return new MomentumConfiguration
            {
                MomentumSensitivity = 1.3f,
                PressureSensitivity = 1.4f,
                MomentumDecayRate = 0.96f, // Faster decay
                CrowdInfluenceStrength = 0.5f,
                CrowdPressureStrength = 0.4f
            };
        }

        /// <summary>
        /// Create stable configuration with more gradual changes
        /// </summary>
        public static MomentumConfiguration CreateStable()
        {
            return new MomentumConfiguration
            {
                MomentumSensitivity = 0.7f,
                PressureSensitivity = 0.8f,
                MomentumDecayRate = 0.99f, // Slower decay
                CrowdInfluenceStrength = 0.2f,
                CrowdPressureStrength = 0.15f
            };
        }

        /// <summary>
        /// Create crowd-focused configuration where crowd has major impact
        /// </summary>
        public static MomentumConfiguration CreateCrowdFocused()
        {
            return new MomentumConfiguration
            {
                MomentumSensitivity = 1.1f,
                PressureSensitivity = 1.2f,
                CrowdInfluenceStrength = 0.7f,
                CrowdPressureStrength = 0.6f,
                InitialHomeAdvantage = 0.1f // Stronger home advantage
            };
        }
    }

    #endregion
}