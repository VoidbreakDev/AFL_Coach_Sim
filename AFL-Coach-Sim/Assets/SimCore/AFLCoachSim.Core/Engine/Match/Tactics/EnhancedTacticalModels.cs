using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Momentum;
using AFLCoachSim.Core.Engine.Match.Weather;
using AFLCoachSim.Core.Engine.Match;

namespace AFLCoachSim.Core.Engine.Match.Tactics
{
    #region Enhanced Analysis Models

    /// <summary>
    /// Comprehensive tactical analysis integrating all match systems
    /// </summary>
    public class ComprehensiveTacticalAnalysis
    {
        public Guid TeamId { get; set; }
        public DateTime Timestamp { get; set; }
        public MatchContext MatchContext { get; set; }

        // Core tactical analysis
        public TacticalGamePlan CurrentGamePlan { get; set; }
        public FormationEffectiveness FormationEffectiveness { get; set; }
        public TacticalBalance TacticalBalance { get; set; }

        // System integrations
        public PlayerPerformanceAnalysis PlayerPerformanceAnalysis { get; set; }
        public FatigueTacticalImpact FatigueImpact { get; set; }
        public MomentumTacticalImpact MomentumImpact { get; set; }
        public WeatherTacticalImpact WeatherImpact { get; set; }

        // Intelligent recommendations
        public List<SmartTacticalRecommendation> SmartRecommendations { get; set; } = new List<SmartTacticalRecommendation>();

        /// <summary>
        /// Get overall tactical situation assessment
        /// </summary>
        public TacticalSituation GetOverallSituation()
        {
            float positiveFactors = 0f;
            float negativeFactors = 0f;

            // Analyze each system's impact
            if (PlayerPerformanceAnalysis != null)
            {
                if (PlayerPerformanceAnalysis.AverageTeamForm > 80f) positiveFactors += 0.3f;
                else if (PlayerPerformanceAnalysis.AverageTeamForm < 60f) negativeFactors += 0.3f;
            }

            if (FatigueImpact != null)
            {
                if (FatigueImpact.AverageTeamFatigue > 75f) negativeFactors += 0.4f;
                else if (FatigueImpact.AverageTeamFatigue < 40f) positiveFactors += 0.2f;
            }

            if (MomentumImpact != null)
            {
                if (Math.Abs(MomentumImpact.CurrentMomentum) > 0.5f)
                {
                    if (MomentumImpact.CurrentMomentum > 0) positiveFactors += 0.3f;
                    else negativeFactors += 0.3f;
                }
            }

            float netAssessment = positiveFactors - negativeFactors;

            return netAssessment switch
            {
                > 0.4f => TacticalSituation.VeryFavorable,
                > 0.2f => TacticalSituation.Favorable,
                > -0.2f => TacticalSituation.Neutral,
                > -0.4f => TacticalSituation.Challenging,
                _ => TacticalSituation.Critical
            };
        }
    }

    /// <summary>
    /// Player performance analysis for tactical decision-making
    /// </summary>
    public class PlayerPerformanceAnalysis
    {
        public List<Guid> TopPerformers { get; set; } = new List<Guid>();
        public List<Guid> BottomPerformers { get; set; } = new List<Guid>();
        public float AverageTeamForm { get; set; }
        public float AverageTeamConfidence { get; set; }
        public Dictionary<Role, float> PositionalPerformance { get; set; } = new Dictionary<Role, float>();
        public List<string> TacticalOpportunities { get; set; } = new List<string>();
    }

    /// <summary>
    /// Fatigue impact on tactical decisions
    /// </summary>
    public class FatigueTacticalImpact
    {
        public float AverageTeamFatigue { get; set; }
        public int CriticalFatigueCount { get; set; }
        public float FormationSustainability { get; set; }
        public int SubstitutionUrgency { get; set; }
        public List<Guid> RecommendedSubstitutions { get; set; } = new List<Guid>();
        public float RecommendedIntensityAdjustment { get; set; }
    }

    /// <summary>
    /// Momentum and pressure impact on tactical decisions
    /// </summary>
    public class MomentumTacticalImpact
    {
        public float CurrentMomentum { get; set; }
        public MomentumTrend MomentumTrend { get; set; }
        public float TeamSpecificMomentum { get; set; }
        public float CurrentPressure { get; set; }
        public PressureTrend PressureTrend { get; set; }
        public float CrowdInfluence { get; set; }
        public CrowdMood CrowdMood { get; set; }
        public List<string> TacticalRecommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Weather impact on tactical decisions
    /// </summary>
    public class WeatherTacticalImpact
    {
        public WeatherConditions WeatherConditions { get; set; }
        public PlayingConditions PlayingConditions { get; set; }
        public float FormationImpact { get; set; }
        public float KickingAccuracyImpact { get; set; }
        public float FatigueImpactMultiplier { get; set; } = 1.0f;
        public List<string> RecommendedAdjustments { get; set; } = new List<string>();
    }

    #endregion

    #region Smart Recommendation System

    /// <summary>
    /// Intelligent tactical recommendation with system integration
    /// </summary>
    public class SmartTacticalRecommendation
    {
        public SmartTacticalRecommendationType Type { get; set; }
        public TacticalPriority Priority { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string SystemReasoning { get; set; } = "";
        public float ExpectedImpact { get; set; }
        public float Confidence { get; set; }
        public float RiskLevel { get; set; } = 0.5f;
        public Dictionary<string, object> IntegratedData { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Calculate recommendation value score
        /// </summary>
        public float GetValueScore()
        {
            float priorityWeight = Priority switch
            {
                TacticalPriority.Urgent => 1.0f,
                TacticalPriority.High => 0.8f,
                TacticalPriority.Medium => 0.6f,
                TacticalPriority.Low => 0.4f,
                _ => 0.3f
            };

            return (ExpectedImpact * Confidence * priorityWeight) / (1f + RiskLevel);
        }
    }

    /// <summary>
    /// Smart tactical decision request
    /// </summary>
    public class SmartTacticalDecisionRequest
    {
        public Guid TeamId { get; set; }
        public SmartTacticalDecisionType DecisionType { get; set; }
        public string Reasoning { get; set; } = "";
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Smart tactical decision with integrated analysis
    /// </summary>
    public class SmartTacticalDecision
    {
        public Guid TeamId { get; set; }
        public SmartTacticalDecisionType DecisionType { get; set; }
        public string Reasoning { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; }
        public SmartTacticalDecisionResult Result { get; set; }
    }

    /// <summary>
    /// Result of smart tactical decision with system impacts
    /// </summary>
    public class SmartTacticalDecisionResult
    {
        public bool Success { get; set; }
        public Dictionary<string, SystemImpactResult> ActualImpacts { get; set; } = new Dictionary<string, SystemImpactResult>();
        public Dictionary<string, SystemImpactPrediction> ExpectedImpacts { get; set; } = new Dictionary<string, SystemImpactPrediction>();
        public int AdaptationTime { get; set; } // seconds
        public LongTermTacticalEffects LongTermEffects { get; set; }
        public Dictionary<Guid, PlayerReactionAnalysis> PlayerReactions { get; set; } = new Dictionary<Guid, PlayerReactionAnalysis>();
    }

    #endregion

    #region Coach AI Models

    /// <summary>
    /// Intelligent coach profile with learning capabilities
    /// </summary>
    public class IntelligentCoachProfile
    {
        public Guid CoachId { get; set; }
        public float TacticalKnowledge { get; set; } = 70f; // 0-100
        public float AdaptabilityRating { get; set; } = 60f; // 0-100
        public float RiskTolerance { get; set; } = 50f; // 0-100
        public float SystemIntegrationSkill { get; set; } = 40f; // 0-100
        public float ExperienceLevel { get; set; } = 50f; // 0-100
        public CoachingStyle PreferredStyle { get; set; } = CoachingStyle.Balanced;

        // Learning data
        public List<TacticalDecisionOutcome> DecisionHistory { get; set; } = new List<TacticalDecisionOutcome>();
        public Dictionary<string, float> SystemPreferences { get; set; } = new Dictionary<string, float>();
        public DateTime LastLearningUpdate { get; set; } = DateTime.Now;

        public IntelligentCoachProfile(Guid coachId)
        {
            CoachId = coachId;
        }

        /// <summary>
        /// Calculate coach's confidence in a tactical decision type
        /// </summary>
        public float GetDecisionConfidence(SmartTacticalDecisionType decisionType)
        {
            float baseConfidence = TacticalKnowledge / 100f;
            
            // Adjust based on experience with this decision type
            var relevantOutcomes = DecisionHistory.Where(d => d.DecisionType == decisionType).ToList();
            if (relevantOutcomes.Any())
            {
                float successRate = relevantOutcomes.Count(o => o.Success) / (float)relevantOutcomes.Count;
                baseConfidence = (baseConfidence + successRate) / 2f;
            }

            return Math.Max(0.2f, Math.Min(0.95f, baseConfidence));
        }
    }

    /// <summary>
    /// AI coaching recommendation with coach-specific insights
    /// </summary>
    public class AICoachingRecommendation
    {
        public SmartTacticalRecommendationType Type { get; set; }
        public string CoachReasoning { get; set; } = "";
        public float CoachConfidence { get; set; }
        public CoachingStyle StyleBias { get; set; }
        public List<string> LearningInsights { get; set; } = new List<string>();
        public Dictionary<string, float> SystemWeights { get; set; } = new Dictionary<string, float>();
    }

    /// <summary>
    /// Coaching attributes for AI coach initialization
    /// </summary>
    public class CoachingAttributes
    {
        public float TacticalKnowledge { get; set; } = 70f;
        public float Adaptability { get; set; } = 60f;
        public float RiskTolerance { get; set; } = 50f;
        public float SystemsThinking { get; set; } = 40f;
        public float Experience { get; set; } = 50f;
        public CoachingStyle PreferredStyle { get; set; } = CoachingStyle.Balanced;
    }

    /// <summary>
    /// Tactical decision outcome for learning
    /// </summary>
    public class TacticalDecisionOutcome
    {
        public SmartTacticalDecisionType DecisionType { get; set; }
        public bool Success { get; set; }
        public float ImpactMagnitude { get; set; }
        public Dictionary<string, float> SystemContributions { get; set; } = new Dictionary<string, float>();
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region System Impact Models

    /// <summary>
    /// Tactical balance analysis across different areas of the game
    /// </summary>
    public class TacticalBalance
    {
        public float OffensiveBalance { get; set; } = 0.5f; // 0 = fully defensive, 1 = fully offensive
        public float DefensiveBalance { get; set; } = 0.5f; // 0 = weak defense, 1 = strong defense
        public float MidfieldBalance { get; set; } = 0.5f; // 0 = weak midfield, 1 = strong midfield
        public float FieldPositionBalance { get; set; } = 0.5f; // 0 = poor position, 1 = excellent position
        public float PossessionBalance { get; set; } = 0.5f; // 0 = poor possession, 1 = excellent possession
        public float OverallBalance { get; set; } = 0.5f; // Overall tactical balance
        
        /// <summary>
        /// Calculate overall balance score
        /// </summary>
        public float CalculateOverallBalance()
        {
            return (OffensiveBalance + DefensiveBalance + MidfieldBalance + FieldPositionBalance + PossessionBalance) / 5f;
        }
        
        /// <summary>
        /// Get balance description
        /// </summary>
        public string GetBalanceDescription()
        {
            var overall = CalculateOverallBalance();
            return overall switch
            {
                >= 0.8f => "Excellent Balance",
                >= 0.6f => "Good Balance",
                >= 0.4f => "Moderate Balance",
                >= 0.2f => "Poor Balance",
                _ => "Very Poor Balance"
            };
        }
    }
    
    /// <summary>
    /// Predicted system impact from tactical decision
    /// </summary>
    public class SystemImpactPrediction
    {
        public string SystemName { get; set; } = "";
        public float ExpectedImpact { get; set; } // -1 to +1
        public float Confidence { get; set; } // 0 to 1
        public int TimeToEffect { get; set; } // seconds
        public List<string> PredictedEffects { get; set; } = new List<string>();
    }

    /// <summary>
    /// Actual system impact result after decision
    /// </summary>
    public class SystemImpactResult
    {
        public string SystemName { get; set; } = "";
        public float ActualImpact { get; set; } // -1 to +1
        public float AccuracyVsPrediction { get; set; } // How accurate was the prediction
        public List<string> ObservedEffects { get; set; } = new List<string>();
        public int ActualTimeToEffect { get; set; } // seconds
    }

    /// <summary>
    /// Long-term effects of tactical decisions
    /// </summary>
    public class LongTermTacticalEffects
    {
        public float PlayerConfidenceImpact { get; set; }
        public float TeamCohesionImpact { get; set; }
        public float TacticalLearningValue { get; set; }
        public List<string> PersistentEffects { get; set; } = new List<string>();
        public int EffectDuration { get; set; } // seconds
    }

    /// <summary>
    /// Player reaction to tactical changes
    /// </summary>
    public class PlayerReactionAnalysis
    {
        public Guid PlayerId { get; set; }
        public float AdaptationRate { get; set; } // How quickly they adapt
        public float PerformanceChange { get; set; } // Performance impact
        public float ConfidenceChange { get; set; } // Confidence impact
        public PlayerTacticalReaction Reaction { get; set; } = PlayerTacticalReaction.Neutral;
    }

    #endregion

    #region Analytics Models

    /// <summary>
    /// Enhanced tactical system analytics
    /// </summary>
    public class TacticalSystemAnalytics
    {
        public int TotalDecisions { get; set; }
        public int SuccessfulDecisions { get; set; }
        public float SystemIntegrationLevel { get; set; }
        public int ActiveCoaches { get; set; }
        public List<SmartTacticalDecision> RecentDecisions { get; set; } = new List<SmartTacticalDecision>();

        // Legacy compatibility
        public int TotalTeams { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalAdjustments { get; set; }
        public Dictionary<Formation, int> FormationDistribution { get; set; } = new Dictionary<Formation, int>();
        public Dictionary<GamePlan, int> GamePlanDistribution { get; set; } = new Dictionary<GamePlan, int>();
        public List<TacticalAdjustment> MostEffectiveAdjustments { get; set; } = new List<TacticalAdjustment>();

        /// <summary>
        /// Calculate success rate
        /// </summary>
        public float GetSuccessRate()
        {
            return TotalDecisions > 0 ? (float)SuccessfulDecisions / TotalDecisions : 0f;
        }
    }

    /// <summary>
    /// Tactical analytics engine for processing historical data
    /// </summary>
    public class TacticalAnalyticsEngine
    {
        private readonly List<TacticalDecisionPattern> _patterns = new List<TacticalDecisionPattern>();

        /// <summary>
        /// Identify patterns in tactical decision outcomes
        /// </summary>
        public List<TacticalDecisionPattern> IdentifyPatterns(List<SmartTacticalDecision> decisions)
        {
            var patterns = new List<TacticalDecisionPattern>();

            // Pattern analysis would be implemented here
            // This is a placeholder implementation

            return patterns;
        }
    }

    /// <summary>
    /// Tactical decision pattern for learning
    /// </summary>
    public class TacticalDecisionPattern
    {
        public string PatternName { get; set; } = "";
        public List<string> Conditions { get; set; } = new List<string>();
        public SmartTacticalDecisionType RecommendedDecision { get; set; }
        public float SuccessRate { get; set; }
        public int Occurrences { get; set; }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Types of smart tactical recommendations
    /// </summary>
    public enum SmartTacticalRecommendationType
    {
        FormationAdjustment,
        IntensityReduction,
        SubstitutionStrategy,
        AggressiveStrategy,
        ConservativeStrategy,
        PressureManagement,
        WeatherAdaptation,
        DesperateStrategy,
        DefensiveStrategy
    }

    /// <summary>
    /// Types of smart tactical decisions
    /// </summary>
    public enum SmartTacticalDecisionType
    {
        FormationChange,
        GamePlanAdjustment,
        IntensityChange,
        SubstitutionPlan,
        RoleReassignment,
        StrategyShift
    }
    
    /// <summary>
    /// Game plan tactical strategies
    /// </summary>
    public enum GamePlan
    {
        Attacking,
        Defensive,
        Balanced,
        CounterAttack,
        Possession,
        HighPressure,
        Conservative,
        Aggressive
    }

    /// <summary>
    /// Overall tactical situation assessment
    /// </summary>
    public enum TacticalSituation
    {
        VeryFavorable,
        Favorable,
        Neutral,
        Challenging,
        Critical
    }

    /// <summary>
    /// Playing conditions based on weather
    /// </summary>
    public enum PlayingConditions
    {
        Perfect,
        Good,
        Challenging,
        Difficult,
        Extreme
    }

    /// <summary>
    /// Coaching style preferences
    /// </summary>
    public enum CoachingStyle
    {
        Aggressive,
        Conservative,
        Adaptive,
        Defensive,
        Attacking,
        Balanced
    }

    /// <summary>
    /// Player reactions to tactical changes
    /// </summary>
    public enum PlayerTacticalReaction
    {
        VeryPositive,
        Positive,
        Neutral,
        Negative,
        VeryNegative
    }

    /// <summary>
    /// Tactical priority levels
    /// </summary>
    public enum TacticalPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    #endregion

    #region Helper Models

    /// <summary>
    /// Match context for tactical analysis
    /// </summary>
    public class MatchContext
    {
        public int Quarter { get; set; } = 1;
        public float TimeRemaining { get; set; } = 1800f; // 30 minutes
        public string Venue { get; set; } = "";
        public int CrowdSize { get; set; }
        public bool IsNightGame { get; set; }
        public bool IsFinalSeries { get; set; }
    }


    /// <summary>
    /// Wind directions
    /// </summary>
    public enum WindDirection
    {
        None,
        North,
        Northeast,
        East,
        Southeast,
        South,
        Southwest,
        West,
        Northwest
    }

    #endregion
}