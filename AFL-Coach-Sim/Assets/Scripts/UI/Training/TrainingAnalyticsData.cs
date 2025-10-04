using System;
using System.Collections.Generic;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems.Training;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLManager.UI.Training
{
    #region Core Analytics Data

    /// <summary>
    /// Main analytics data container for training dashboard
    /// </summary>
    [System.Serializable]
    public class TrainingAnalyticsData
    {
        public DateTime AnalysisPeriodStart { get; set; }
        public DateTime AnalysisPeriodEnd { get; set; }
        public List<Player> TeamPlayers { get; set; } = new List<Player>();
        
        // Training Session Analytics
        public int TotalTrainingSessions { get; set; }
        public float AverageWeeklyLoad { get; set; }
        public List<WeeklySessionCount> WeeklySessionCounts { get; set; } = new List<WeeklySessionCount>();
        
        // Load Management Analytics
        public List<PlayerLoadAnalytics> PlayerLoadStates { get; set; } = new List<PlayerLoadAnalytics>();
        public float TeamAverageLoad { get; set; }
        public int HighRiskPlayersCount { get; set; }
        public Dictionary<string, int> LoadDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ConditionDistribution { get; set; } = new Dictionary<string, int>();
        
        // Performance Analytics
        public TeamPerformanceMetrics PerformanceMetrics { get; set; }
        public List<PerformanceTrend> PerformanceTrends { get; set; } = new List<PerformanceTrend>();
        
        // Injury Analytics
        public InjuryAnalyticsMetrics InjuryMetrics { get; set; }
        
        // Recommendations
        public List<TrainingRecommendation> TrainingRecommendations { get; set; } = new List<TrainingRecommendation>();
        
        public TimeSpan AnalysisPeriod => AnalysisPeriodEnd - AnalysisPeriodStart;
        public int AnalysisDays => (int)AnalysisPeriod.TotalDays;
    }

    #endregion

    #region Training Session Analytics

    /// <summary>
    /// Weekly session count tracking
    /// </summary>
    [System.Serializable]
    public class WeeklySessionCount
    {
        public DateTime WeekStartDate { get; set; }
        public int SessionsCompleted { get; set; }
        public int SessionsScheduled { get; set; }
        public float AverageLoad { get; set; }
        public float CompletionRate => SessionsScheduled > 0 ? (float)SessionsCompleted / SessionsScheduled : 0f;
        
        public string WeekLabel => $"Week of {WeekStartDate:MMM dd}";
    }

    /// <summary>
    /// Daily session analytics
    /// </summary>
    [System.Serializable]
    public class DailySessionAnalytics
    {
        public DateTime SessionDate { get; set; }
        public string SessionName { get; set; }
        public TrainingSessionStatus Status { get; set; }
        public float PlannedLoad { get; set; }
        public float ActualLoad { get; set; }
        public int ParticipantsTargeted { get; set; }
        public int ParticipantsActual { get; set; }
        public List<string> SessionIssues { get; set; } = new List<string>();
        
        public float LoadVariance => ActualLoad - PlannedLoad;
        public float ParticipationRate => ParticipantsTargeted > 0 ? (float)ParticipantsActual / ParticipantsTargeted : 0f;
    }

    #endregion

    #region Player Load Analytics

    /// <summary>
    /// Individual player load analytics
    /// </summary>
    [System.Serializable]
    public class PlayerLoadAnalytics
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public float CurrentLoad { get; set; }
        public float FatigueLevel { get; set; }
        public float Condition { get; set; }
        public FatigueRiskLevel RiskLevel { get; set; }
        public string RecommendedAction { get; set; }
        
        // Historical data
        public List<DailyLoadEntry> DailyLoads { get; set; } = new List<DailyLoadEntry>();
        public List<RecoverySessionEntry> RecoverySessions { get; set; } = new List<RecoverySessionEntry>();
        public List<InjuryEntry> InjuryHistory { get; set; } = new List<InjuryEntry>();
        
        // Calculated metrics
        public float AverageWeeklyLoad { get; set; }
        public float LoadTrend { get; set; } // Positive = increasing, negative = decreasing
        public int DaysAboveThreshold { get; set; }
        public float RecoveryEfficiency { get; set; }
        
        public string RiskLevelText => RiskLevel.ToString();
        public Color RiskLevelColor => GetRiskLevelColor(RiskLevel);
        
        private Color GetRiskLevelColor(FatigueRiskLevel risk)
        {
            return risk switch
            {
                FatigueRiskLevel.Low => Color.green,
                FatigueRiskLevel.Moderate => Color.yellow,
                FatigueRiskLevel.High => Color.red,
                FatigueRiskLevel.Critical => Color.magenta,
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// Daily load entry for tracking
    /// </summary>
    [System.Serializable]
    public class DailyLoadEntry
    {
        public DateTime Date { get; set; }
        public float Load { get; set; }
        public TrainingIntensity Intensity { get; set; }
        public string Source { get; set; } // Training, Match, etc.
    }

    /// <summary>
    /// Recovery session tracking
    /// </summary>
    [System.Serializable]
    public class RecoverySessionEntry
    {
        public DateTime Date { get; set; }
        public RecoveryType RecoveryType { get; set; }
        public TimeSpan Duration { get; set; }
        public float EffectivenessRating { get; set; }
    }

    /// <summary>
    /// Injury tracking for analytics
    /// </summary>
    [System.Serializable]
    public class InjuryEntry
    {
        public DateTime InjuryDate { get; set; }
        public string InjuryType { get; set; }
        public string BodyPart { get; set; }
        public int SeverityLevel { get; set; } // 1-5
        public TimeSpan RecoveryTime { get; set; }
        public float PreInjuryLoad { get; set; }
        public bool PreventableFlag { get; set; }
    }

    #endregion

    #region Performance Analytics

    /// <summary>
    /// Team performance metrics
    /// </summary>
    [System.Serializable]
    public class TeamPerformanceMetrics
    {
        public float AverageCondition { get; set; }
        public float TrainingEffectivenessScore { get; set; }
        public float DevelopmentProgressScore { get; set; }
        public float InjuryPreventionScore { get; set; }
        
        // Derived metrics
        public float OverallTeamHealthScore { get; set; }
        public string PerformanceGrade { get; set; } // A, B, C, D, F
        public List<string> StrengthAreas { get; set; } = new List<string>();
        public List<string> ImprovementAreas { get; set; } = new List<string>();
        
        public void CalculateDerivedMetrics()
        {
            OverallTeamHealthScore = (AverageCondition + TrainingEffectivenessScore + DevelopmentProgressScore + InjuryPreventionScore) / 4f;
            PerformanceGrade = GetPerformanceGrade(OverallTeamHealthScore);
            
            // Analyze strength and improvement areas
            AnalyzePerformanceAreas();
        }
        
        private string GetPerformanceGrade(float score)
        {
            if (score >= 90) return "A";
            if (score >= 80) return "B";
            if (score >= 70) return "C";
            if (score >= 60) return "D";
            return "F";
        }
        
        private void AnalyzePerformanceAreas()
        {
            StrengthAreas.Clear();
            ImprovementAreas.Clear();
            
            if (AverageCondition >= 80) StrengthAreas.Add("Player Condition");
            else if (AverageCondition < 60) ImprovementAreas.Add("Player Condition");
            
            if (TrainingEffectivenessScore >= 80) StrengthAreas.Add("Training Effectiveness");
            else if (TrainingEffectivenessScore < 60) ImprovementAreas.Add("Training Effectiveness");
            
            if (DevelopmentProgressScore >= 80) StrengthAreas.Add("Player Development");
            else if (DevelopmentProgressScore < 60) ImprovementAreas.Add("Player Development");
            
            if (InjuryPreventionScore >= 80) StrengthAreas.Add("Injury Prevention");
            else if (InjuryPreventionScore < 60) ImprovementAreas.Add("Injury Prevention");
        }
    }

    /// <summary>
    /// Performance trend over time
    /// </summary>
    [System.Serializable]
    public class PerformanceTrend
    {
        public DateTime Date { get; set; }
        public float ConditionAverage { get; set; }
        public float LoadAverage { get; set; }
        public float EffectivenessScore { get; set; }
        public int ActivePlayers { get; set; }
        public int InjuredPlayers { get; set; }
        
        // Trend indicators
        public float ConditionChange { get; set; } // vs previous period
        public float LoadChange { get; set; }
        public float EffectivenessChange { get; set; }
        
        public string TrendLabel => Date.ToString("MMM dd");
        public bool IsImproving => ConditionChange > 0 && EffectivenessChange > 0;
    }

    #endregion

    #region Injury Analytics

    /// <summary>
    /// Injury analytics metrics
    /// </summary>
    [System.Serializable]
    public class InjuryAnalyticsMetrics
    {
        public int TotalInjuries { get; set; }
        public float InjuryRate { get; set; } // Injuries per 100 training hours
        public TimeSpan AverageRecoveryTime { get; set; }
        public List<InjuryPreventionTip> InjuryPrevention { get; set; } = new List<InjuryPreventionTip>();
        
        // Injury breakdown
        public Dictionary<string, int> InjuriesByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> InjuriesByBodyPart { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, int> InjuriesBySeverity { get; set; } = new Dictionary<int, int>();
        
        // Risk factors
        public List<InjuryRiskFactor> RiskFactors { get; set; } = new List<InjuryRiskFactor>();
        public float PreventableInjuryPercentage { get; set; }
        
        // Prevention metrics
        public float PreventionEffectivenessScore { get; set; }
        public int DaysWithoutInjury { get; set; }
        public List<string> SuccessfulPreventionMeasures { get; set; } = new List<string>();
    }

    /// <summary>
    /// Injury prevention tip
    /// </summary>
    [System.Serializable]
    public class InjuryPreventionTip
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public PreventionPriority Priority { get; set; }
        public List<string> ApplicablePlayers { get; set; } = new List<string>();
        public string Category { get; set; } // Load Management, Recovery, Technique, etc.
        
        public Color PriorityColor => Priority switch
        {
            PreventionPriority.Critical => Color.red,
            PreventionPriority.High => new Color(1f, 0.5f, 0f), // Orange
            PreventionPriority.Medium => Color.yellow,
            PreventionPriority.Low => Color.green,
            _ => Color.white
        };
    }

    /// <summary>
    /// Injury risk factor analysis
    /// </summary>
    [System.Serializable]
    public class InjuryRiskFactor
    {
        public string FactorName { get; set; }
        public float RiskMultiplier { get; set; } // 1.0 = normal, >1.0 = higher risk
        public int AffectedPlayers { get; set; }
        public string Mitigation { get; set; }
        public bool IsModifiable { get; set; }
    }

    #endregion

    #region Recommendations

    /// <summary>
    /// Training recommendation
    /// </summary>
    [System.Serializable]
    public class TrainingRecommendation
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public List<int> TargetPlayerIds { get; set; } = new List<int>();
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public bool IsActionable { get; set; } = true;
        public string ActionButton { get; set; } // e.g., "Reduce Load", "Add Recovery"
        
        // Context
        public string Reasoning { get; set; }
        public List<string> DataPoints { get; set; } = new List<string>();
        public float ConfidenceLevel { get; set; } // 0-1
        
        public Color PriorityColor => Priority switch
        {
            RecommendationPriority.Urgent => Color.red,
            RecommendationPriority.High => new Color(1f, 0.5f, 0f), // Orange
            RecommendationPriority.Medium => Color.yellow,
            RecommendationPriority.Low => Color.green,
            _ => Color.white
        };
        
        public string PriorityText => Priority.ToString().ToUpper();
    }

    #endregion

    #region Enums

    /// <summary>
    /// Fatigue risk levels for players
    /// </summary>
    public enum FatigueRiskLevel
    {
        Low = 0,
        Moderate = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Recovery types
    /// </summary>
    public enum RecoveryType
    {
        PassiveRest,
        ActiveRecovery,
        Massage,
        IceBath,
        Stretching,
        Physiotherapy,
        Sleep,
        Nutrition
    }

    /// <summary>
    /// Prevention priority levels
    /// </summary>
    public enum PreventionPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Recommendation types
    /// </summary>
    public enum RecommendationType
    {
        LoadReduction,
        LoadIncrease,
        RecoverySession,
        RestDay,
        IntensityAdjustment,
        PlayerRotation,
        InjuryPrevention,
        PerformanceOptimization
    }

    /// <summary>
    /// Recommendation priorities
    /// </summary>
    public enum RecommendationPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }

    #endregion
}