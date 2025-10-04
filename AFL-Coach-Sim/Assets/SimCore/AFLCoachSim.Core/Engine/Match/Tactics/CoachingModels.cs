using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Tactics
{
    #region Coaching Profile

    /// <summary>
    /// Profile defining a coach's tactical preferences and capabilities
    /// </summary>
    public class CoachingProfile
    {
        public string Name { get; set; } = "Unknown Coach";
        public float Aggressiveness { get; set; } = 50f; // 0-100, defensive to aggressive approach
        public float Defensiveness { get; set; } = 50f; // 0-100, emphasis on defensive tactics
        public float TacticalKnowledge { get; set; } = 50f; // 0-100, understanding of tactical systems
        public float Adaptability { get; set; } = 50f; // 0-100, ability to change tactics mid-game
        public float RiskTolerance { get; set; } = 50f; // 0-100, willingness to make risky tactical moves
        public float PlayerTrust { get; set; } = 50f; // 0-100, trust in players to execute complex tactics
        public float PressureHandling { get; set; } = 50f; // 0-100, performance under pressure situations

        // Timing preferences
        public float MinTimeBetweenAdjustments { get; set; } = 300f; // Seconds between tactical changes
        public float UrgencySensitivity { get; set; } = 50f; // How quickly coach reacts to game situations

        // Formation preferences
        public List<string> PreferredFormations { get; set; } = new List<string> { "Standard" };
        public Dictionary<string, float> FormationComfortLevel { get; set; } = new Dictionary<string, float>();

        // Style preferences
        public OffensiveStyle PreferredOffensiveStyle { get; set; } = OffensiveStyle.Balanced;
        public DefensiveStyle PreferredDefensiveStyle { get; set; } = DefensiveStyle.Zoning;

        // Experience factors
        public int YearsExperience { get; set; } = 5;
        public float BigGameExperience { get; set; } = 50f; // Experience in high-stakes games
        public Dictionary<TacticalAdjustmentType, float> AdjustmentSuccessRate { get; set; } = 
            new Dictionary<TacticalAdjustmentType, float>();

        /// <summary>
        /// Get the coach's comfort level with a specific formation
        /// </summary>
        public float GetFormationComfort(string formationName)
        {
            return FormationComfortLevel.GetValueOrDefault(formationName, 50f);
        }

        /// <summary>
        /// Calculate overall coaching effectiveness in current situation
        /// </summary>
        public float CalculateCoachingEffectiveness(MatchSituation situation)
        {
            float effectiveness = (TacticalKnowledge + Adaptability + PressureHandling) / 3f;

            // Pressure situations affect coaching effectiveness
            float pressureFactor = Math.Abs(situation.ScoreDifferential) / 30f; // Normalize to ~0-1
            pressureFactor += (1f - situation.TimeRemainingPercent) * 0.5f; // Final quarter pressure

            effectiveness *= (1f + (PressureHandling - 50f) / 100f * pressureFactor);

            return Math.Max(20f, Math.Min(100f, effectiveness));
        }
    }

    /// <summary>
    /// Factory for creating coaching profiles with different archetypes
    /// </summary>
    public static class CoachingProfileFactory
    {
        public static CoachingProfile CreateDefensiveMinded()
        {
            return new CoachingProfile
            {
                Name = "Defensive Specialist",
                Aggressiveness = 30f,
                Defensiveness = 85f,
                TacticalKnowledge = 75f,
                Adaptability = 60f,
                RiskTolerance = 25f,
                PreferredFormations = new List<string> { "Defensive", "Flooding", "Standard" },
                PreferredDefensiveStyle = DefensiveStyle.Zoning,
                PreferredOffensiveStyle = OffensiveStyle.Possession,
                MinTimeBetweenAdjustments = 420f // 7 minutes - patient approach
            };
        }

        public static CoachingProfile CreateAttackingMinded()
        {
            return new CoachingProfile
            {
                Name = "Attacking Specialist",
                Aggressiveness = 85f,
                Defensiveness = 40f,
                TacticalKnowledge = 70f,
                Adaptability = 80f,
                RiskTolerance = 75f,
                PreferredFormations = new List<string> { "Attacking", "Pressing", "Standard" },
                PreferredDefensiveStyle = DefensiveStyle.Pressing,
                PreferredOffensiveStyle = OffensiveStyle.FastBreak,
                MinTimeBetweenAdjustments = 240f // 4 minutes - more reactive
            };
        }

        public static CoachingProfile CreateTacticalGenius()
        {
            return new CoachingProfile
            {
                Name = "Tactical Master",
                Aggressiveness = 60f,
                Defensiveness = 60f,
                TacticalKnowledge = 95f,
                Adaptability = 90f,
                RiskTolerance = 65f,
                PlayerTrust = 80f,
                PressureHandling = 85f,
                PreferredFormations = new List<string> { "Standard", "Attacking", "Defensive", "Pressing" },
                MinTimeBetweenAdjustments = 300f,
                FormationComfortLevel = new Dictionary<string, float>
                {
                    ["Standard"] = 90f,
                    ["Attacking"] = 85f,
                    ["Defensive"] = 85f,
                    ["Pressing"] = 80f,
                    ["Flooding"] = 75f
                }
            };
        }

        public static CoachingProfile CreateInexperienced()
        {
            return new CoachingProfile
            {
                Name = "Rookie Coach",
                Aggressiveness = 45f,
                Defensiveness = 45f,
                TacticalKnowledge = 35f,
                Adaptability = 40f,
                RiskTolerance = 30f,
                PlayerTrust = 40f,
                PressureHandling = 25f,
                YearsExperience = 1,
                BigGameExperience = 20f,
                PreferredFormations = new List<string> { "Standard" },
                MinTimeBetweenAdjustments = 480f // 8 minutes - hesitant to make changes
            };
        }

        public static CoachingProfile CreateVeteran()
        {
            return new CoachingProfile
            {
                Name = "Veteran Coach",
                Aggressiveness = 55f,
                Defensiveness = 65f,
                TacticalKnowledge = 85f,
                Adaptability = 70f,
                RiskTolerance = 50f,
                PlayerTrust = 90f,
                PressureHandling = 95f,
                YearsExperience = 20,
                BigGameExperience = 90f,
                MinTimeBetweenAdjustments = 360f // 6 minutes - experienced timing
            };
        }
    }

    #endregion

    #region Tactical Triggers

    /// <summary>
    /// Types of triggers that can cause tactical adjustments
    /// </summary>
    public enum TriggerType
    {
        ScoreDifferential,      // Based on current score difference
        TimeRemaining,          // Based on time left in game
        Momentum,               // Based on team momentum
        OpponentFormation,      // Response to opponent tactical changes
        PossessionTurnover,     // Based on turnover rates
        WeatherChange,          // Response to changing weather conditions
        PlayerInjury,           // Response to key player injuries
        QuarterTransition,      // Between quarters tactical adjustments
        OpponentScoring         // Response to opponent scoring runs
    }

    /// <summary>
    /// A trigger condition that can cause tactical adjustments
    /// </summary>
    public class TacticalTrigger
    {
        public TriggerType Type { get; set; }
        public string Description { get; set; } = "";
        public TacticalAdjustmentType PreferredAdjustmentType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public float Priority { get; set; } = 1.0f; // Higher priority triggers are evaluated first
        public bool IsActive { get; set; } = true;
        public DateTime LastTriggered { get; set; } = DateTime.MinValue;
        public float CooldownSeconds { get; set; } = 180f; // 3 minutes default cooldown

        /// <summary>
        /// Check if this trigger is currently on cooldown
        /// </summary>
        public bool IsOnCooldown()
        {
            return DateTime.Now - LastTriggered < TimeSpan.FromSeconds(CooldownSeconds);
        }

        /// <summary>
        /// Mark this trigger as having been activated
        /// </summary>
        public void MarkTriggered()
        {
            LastTriggered = DateTime.Now;
        }
    }

    /// <summary>
    /// Factory for creating common tactical triggers
    /// </summary>
    public static class TacticalTriggerFactory
    {
        public static TacticalTrigger CreateScoreDifferentialTrigger(float threshold, string condition, 
            TacticalAdjustmentType adjustmentType)
        {
            return new TacticalTrigger
            {
                Type = TriggerType.ScoreDifferential,
                Description = $"{condition} by {threshold} points",
                PreferredAdjustmentType = adjustmentType,
                Parameters = new Dictionary<string, object>
                {
                    ["threshold"] = threshold,
                    ["condition"] = condition
                },
                Priority = 0.8f,
                CooldownSeconds = 240f
            };
        }

        public static TacticalTrigger CreateFinalQuarterTrigger()
        {
            return new TacticalTrigger
            {
                Type = TriggerType.TimeRemaining,
                Description = "Final quarter tactical adjustment",
                PreferredAdjustmentType = TacticalAdjustmentType.OffensiveStyle,
                Parameters = new Dictionary<string, object>
                {
                    ["threshold"] = 0.25f
                },
                Priority = 0.9f,
                CooldownSeconds = 600f // Only triggers once per quarter
            };
        }

        public static TacticalTrigger CreateMomentumSwingTrigger(bool negative = true)
        {
            return new TacticalTrigger
            {
                Type = TriggerType.Momentum,
                Description = negative ? "Counter negative momentum" : "Build on positive momentum",
                PreferredAdjustmentType = negative ? TacticalAdjustmentType.PressureIntensity : 
                                                   TacticalAdjustmentType.OffensiveStyle,
                Parameters = new Dictionary<string, object>
                {
                    ["threshold"] = negative ? -0.3f : 0.3f,
                    ["condition"] = negative ? "negative" : "positive"
                },
                Priority = 0.6f,
                CooldownSeconds = 300f
            };
        }

        public static TacticalTrigger CreateTurnoverTrigger()
        {
            return new TacticalTrigger
            {
                Type = TriggerType.PossessionTurnover,
                Description = "High turnovers, adjust defensive structure",
                PreferredAdjustmentType = TacticalAdjustmentType.DefensiveStructure,
                Parameters = new Dictionary<string, object>
                {
                    ["threshold"] = 0.65f
                },
                Priority = 0.5f,
                CooldownSeconds = 420f
            };
        }

        public static TacticalTrigger CreateWeatherTrigger(Weather targetWeather, 
            TacticalAdjustmentType adjustmentType)
        {
            return new TacticalTrigger
            {
                Type = TriggerType.WeatherChange,
                Description = $"Adjust for {targetWeather} weather",
                PreferredAdjustmentType = adjustmentType,
                Parameters = new Dictionary<string, object>
                {
                    ["weather"] = (int)targetWeather
                },
                Priority = 0.4f,
                CooldownSeconds = 900f // 15 minutes - weather doesn't change often
            };
        }
    }

    #endregion

    #region Tactical Decisions

    /// <summary>
    /// A tactical decision made by the coaching AI
    /// </summary>
    public class TacticalDecision
    {
        public TeamId TeamId { get; set; }
        public bool ShouldAdjust { get; set; }
        public TacticalAdjustment? Adjustment { get; set; }
        public float Confidence { get; set; } = 0f; // 0-1, confidence in this decision
        public string Reason { get; set; } = "";
        public DateTime DecisionTime { get; set; } = DateTime.Now;
        public List<string> ConsideredOptions { get; set; } = new List<string>();

        /// <summary>
        /// Get a human-readable description of this decision
        /// </summary>
        public string GetDescription()
        {
            if (!ShouldAdjust)
                return $"No tactical change: {Reason}";

            return $"Tactical adjustment ({Confidence:P0} confidence): {Adjustment?.Type} - {Reason}";
        }
    }

    /// <summary>
    /// Context information about the current match state for tactical decisions
    /// </summary>
    public class MatchState
    {
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public float ElapsedTime { get; set; } // In seconds
        public float TotalGameTime { get; set; } = 6000f; // 100 minutes default
        public Phase CurrentPhase { get; set; }
        public TeamId? PossessionTeam { get; set; }
        public Weather Weather { get; set; }
        public Dictionary<TeamId, float> TeamMomentum { get; set; } = new Dictionary<TeamId, float>();
        public Dictionary<TeamId, Dictionary<string, float>> TeamStats { get; set; } = 
            new Dictionary<TeamId, Dictionary<string, float>>();

        /// <summary>
        /// Calculate score differential from perspective of specified team
        /// </summary>
        public float GetScoreDifferential(TeamId teamId, TeamId homeTeam)
        {
            bool isHome = teamId == homeTeam;
            return isHome ? HomeScore - AwayScore : AwayScore - HomeScore;
        }

        /// <summary>
        /// Calculate time remaining as percentage (1.0 = full time, 0.0 = finished)
        /// </summary>
        public float GetTimeRemainingPercent()
        {
            return Math.Max(0f, (TotalGameTime - ElapsedTime) / TotalGameTime);
        }

        /// <summary>
        /// Get team momentum for specified team
        /// </summary>
        public float GetTeamMomentum(TeamId teamId)
        {
            return TeamMomentum.GetValueOrDefault(teamId, 0f);
        }

        /// <summary>
        /// Get team stat value
        /// </summary>
        public float GetTeamStat(TeamId teamId, string statName, float defaultValue = 0f)
        {
            return TeamStats.GetValueOrDefault(teamId, new Dictionary<string, float>())
                           .GetValueOrDefault(statName, defaultValue);
        }
    }

    #endregion

    #region Coaching Helpers

    /// <summary>
    /// Helper functions for coaching AI calculations
    /// </summary>
    public static class CoachingHelpers
    {
        /// <summary>
        /// Calculate pressure multiplier based on game situation
        /// </summary>
        public static float CalculateSituationPressure(MatchSituation situation)
        {
            float pressure = 0f;

            // Score pressure
            pressure += Math.Abs(situation.ScoreDifferential) / 50f; // Normalize to roughly 0-1

            // Time pressure (increases in final quarter)
            if (situation.TimeRemainingPercent < 0.25f)
                pressure += (1f - situation.TimeRemainingPercent) * 2f;

            // Momentum pressure
            pressure += Math.Abs(situation.TeamMomentum) * 0.5f;

            return Math.Min(2.0f, pressure);
        }

        /// <summary>
        /// Determine if situation calls for conservative or aggressive tactics
        /// </summary>
        public static bool ShouldBeConservative(MatchSituation situation, CoachingProfile coach)
        {
            // Leading by significant margin in late stages
            if (situation.ScoreDifferential > 18 && situation.TimeRemainingPercent < 0.33f)
                return true;

            // Close game with defensive-minded coach
            if (Math.Abs(situation.ScoreDifferential) < 12 && coach.Defensiveness > 70)
                return true;

            // Bad momentum and conservative coach
            if (situation.TeamMomentum < -0.3f && coach.RiskTolerance < 40)
                return true;

            return false;
        }

        /// <summary>
        /// Calculate appropriate adjustment timing based on coach profile
        /// </summary>
        public static float CalculateAdjustmentTiming(CoachingProfile coach, float urgency)
        {
            float baseDelay = coach.MinTimeBetweenAdjustments;
            float urgencyFactor = 1f - (urgency * coach.UrgencySensitivity / 100f);
            
            return baseDelay * Math.Max(0.3f, urgencyFactor);
        }
    }

    #endregion
}