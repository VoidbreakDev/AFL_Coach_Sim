using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Injuries.Domain;
using AFLManager.Models;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Represents the execution context and state of a training session
    /// </summary>
    [System.Serializable]
    public class TrainingSessionExecution
    {
        public int SessionId { get; set; }
        public DailyTrainingSession Session { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? ActualDuration { get; set; }
        public SessionExecutionStatus Status { get; set; }
        public List<Player> EligibleParticipants { get; set; } = new List<Player>();
        public Dictionary<int, PlayerSessionResult> ParticipantResults { get; set; } = new Dictionary<int, PlayerSessionResult>();
        public List<ComponentExecutionResult> ComponentResults { get; set; } = new List<ComponentExecutionResult>();
        public SessionExecutionMetrics SessionMetrics { get; set; } = new SessionExecutionMetrics();
        public string CompletionMessage { get; set; }
        
        // Real-time execution state
        public int CurrentComponentIndex { get; set; } = 0;
        public DateTime ComponentStartTime { get; set; }
        
        /// <summary>
        /// Get overall execution progress as percentage
        /// </summary>
        public float GetExecutionProgress()
        {
            if (Session?.TrainingComponents == null || Session.TrainingComponents.Count == 0)
                return Status == SessionExecutionStatus.Completed ? 100f : 0f;
                
            return ((float)ComponentResults.Count / Session.TrainingComponents.Count) * 100f;
        }
        
        /// <summary>
        /// Check if execution is currently active
        /// </summary>
        public bool IsActive => Status == SessionExecutionStatus.Running || Status == SessionExecutionStatus.Preparing;
        
        /// <summary>
        /// Get total injuries occurred during this session
        /// </summary>
        public int GetTotalInjuries()
        {
            return ComponentResults.Sum(cr => cr.PlayerResults.Values.Count(pr => pr.InjuryOccurred));
        }
        
        /// <summary>
        /// Get average effectiveness across all participants and components
        /// </summary>
        public float GetAverageEffectiveness()
        {
            var allResults = ComponentResults.SelectMany(cr => cr.PlayerResults.Values).ToList();
            return allResults.Any() ? allResults.Average(pr => pr.EffectivenessRating) : 0f;
        }
    }
    
    /// <summary>
    /// Result of session preparation phase
    /// </summary>
    public class SessionPreparationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Player> EligibleParticipants { get; set; } = new List<Player>();
        public List<string> Details { get; set; } = new List<string>();
        
        /// <summary>
        /// Get participant availability rate
        /// </summary>
        public float GetAvailabilityRate(int totalRequested)
        {
            return totalRequested > 0 ? (float)EligibleParticipants.Count / totalRequested : 0f;
        }
    }
    
    /// <summary>
    /// Player eligibility check result
    /// </summary>
    public class PlayerEligibilityResult
    {
        public int PlayerId { get; set; }
        public bool IsEligible { get; set; }
        public string Reason { get; set; }
        public List<string> Details { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Player state during session execution
    /// </summary>
    [System.Serializable]
    public class PlayerSessionState
    {
        public int PlayerId { get; set; }
        public Player Player { get; set; }
        public int SessionId { get; set; }
        public float StartingFatigue { get; set; }
        public float StartingCondition { get; set; }
        public List<int> ComponentsCompleted { get; set; } = new List<int>();
        public float TotalLoadAccumulated { get; set; }
        public Dictionary<string, float> EffectivenessModifiers { get; set; } = new Dictionary<string, float>();
        public bool IsInjured { get; set; } = false;
        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Get current fatigue including session accumulation
        /// </summary>
        public float GetCurrentFatigue()
        {
            return StartingFatigue + (TotalLoadAccumulated * 2f); // Load contributes to fatigue
        }
        
        /// <summary>
        /// Check if player can continue with more training
        /// </summary>
        public bool CanContinue(float fatigueThreshold = 85f)
        {
            return !IsInjured && GetCurrentFatigue() < fatigueThreshold;
        }
    }
    
    /// <summary>
    /// Results of executing a single training component
    /// </summary>
    [System.Serializable]
    public class ComponentExecutionResult
    {
        public int ComponentIndex { get; set; }
        public TrainingComponent Component { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CompletionTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<int, PlayerComponentResult> PlayerResults { get; set; } = new Dictionary<int, PlayerComponentResult>();
        
        /// <summary>
        /// Get average effectiveness for this component
        /// </summary>
        public float GetAverageEffectiveness()
        {
            return PlayerResults.Values.Any() ? PlayerResults.Values.Average(pr => pr.EffectivenessRating) : 0f;
        }
        
        /// <summary>
        /// Get injury count for this component
        /// </summary>
        public int GetInjuryCount()
        {
            return PlayerResults.Values.Count(pr => pr.InjuryOccurred);
        }
        
        /// <summary>
        /// Get total load contribution from this component
        /// </summary>
        public float GetTotalLoad()
        {
            return PlayerResults.Values.Sum(pr => pr.LoadContribution);
        }
    }
    
    /// <summary>
    /// Individual player result for a training component
    /// </summary>
    [System.Serializable]
    public class PlayerComponentResult
    {
        public int PlayerId { get; set; }
        public TrainingComponentType ComponentType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime CompletionTime { get; set; }
        public float EffectivenessRating { get; set; }
        public PlayerStatsDelta StatChanges { get; set; }
        public float FatigueIncrease { get; set; }
        public float LoadContribution { get; set; }
        public float InjuryRisk { get; set; }
        public bool InjuryOccurred { get; set; } = false;
        public Injury Injury { get; set; }
        public bool ErrorOccurred { get; set; } = false;
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Check if this component execution was successful
        /// </summary>
        public bool IsSuccessful => !InjuryOccurred && !ErrorOccurred;
        
        /// <summary>
        /// Get development value from stat changes
        /// </summary>
        public float GetDevelopmentValue()
        {
            return StatChanges?.GetTotalChange() ?? 0f;
        }
        
        /// <summary>
        /// Get component duration
        /// </summary>
        public TimeSpan GetDuration()
        {
            return CompletionTime - StartTime;
        }
    }
    
    /// <summary>
    /// Complete session result for an individual player
    /// </summary>
    [System.Serializable]
    public class PlayerSessionResult
    {
        public int PlayerId { get; set; }
        public int SessionId { get; set; }
        public string PlayerName { get; set; }
        public List<PlayerComponentResult> ComponentResults { get; set; } = new List<PlayerComponentResult>();
        public PlayerStatsDelta TotalStatChanges { get; set; } = new PlayerStatsDelta();
        public float TotalFatigueIncrease { get; set; }
        public float TotalLoadContribution { get; set; }
        public float AverageEffectiveness { get; set; }
        public int TotalInjuries { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public SessionParticipationGrade Grade { get; set; }
        
        /// <summary>
        /// Calculate participation grade based on performance
        /// </summary>
        public SessionParticipationGrade CalculateGrade()
        {
            if (TotalInjuries > 0)
                return SessionParticipationGrade.Injured;
                
            var successRate = ComponentResults.Any() ? 
                (float)ComponentResults.Count(cr => cr.IsSuccessful) / ComponentResults.Count : 0f;
                
            if (successRate >= 0.9f && AverageEffectiveness >= 1.1f)
                return SessionParticipationGrade.Excellent;
            else if (successRate >= 0.8f && AverageEffectiveness >= 0.9f)
                return SessionParticipationGrade.Good;
            else if (successRate >= 0.6f && AverageEffectiveness >= 0.7f)
                return SessionParticipationGrade.Fair;
            else
                return SessionParticipationGrade.Poor;
        }
        
        /// <summary>
        /// Get summary text for this player's session
        /// </summary>
        public string GetSummary()
        {
            var grade = CalculateGrade();
            var totalDev = TotalStatChanges?.GetTotalChange() ?? 0f;
            
            return $"{PlayerName}: {grade} ({ComponentResults.Count} components, " +
                   $"{totalDev:F1} development, {AverageEffectiveness:F2} effectiveness)";
        }
    }
    
    /// <summary>
    /// Session-wide execution metrics
    /// </summary>
    [System.Serializable]
    public class SessionExecutionMetrics
    {
        public int TotalParticipants { get; set; }
        public int ComponentsExecuted { get; set; }
        public int TotalInjuries { get; set; }
        public float AverageEffectiveness { get; set; }
        public float TotalTrainingLoad { get; set; }
        public float SuccessRate { get; set; }
        public TimeSpan ExecutionDuration { get; set; }
        
        // Performance metrics
        public Dictionary<TrainingComponentType, float> EffectivenessByType { get; set; } = new Dictionary<TrainingComponentType, float>();
        public Dictionary<TrainingIntensity, int> InjuriesByIntensity { get; set; } = new Dictionary<TrainingIntensity, int>();
        public Dictionary<string, float> PlayerPerformanceRanking { get; set; } = new Dictionary<string, float>();
        
        /// <summary>
        /// Calculate overall session quality score
        /// </summary>
        public float GetQualityScore()
        {
            float qualityScore = 0f;
            
            // Effectiveness contributes 40%
            qualityScore += (AverageEffectiveness * 0.4f);
            
            // Success rate contributes 40%
            qualityScore += (SuccessRate * 0.4f);
            
            // Safety (inverse of injury rate) contributes 20%
            var injuryRate = TotalParticipants > 0 ? (float)TotalInjuries / (TotalParticipants * ComponentsExecuted) : 0f;
            var safetyScore = Mathf.Clamp(1f - (injuryRate * 10f), 0f, 1f); // Scale injury rate
            qualityScore += (safetyScore * 0.2f);
            
            return Mathf.Clamp(qualityScore, 0f, 1f);
        }
        
        /// <summary>
        /// Get metrics summary text
        /// </summary>
        public string GetSummary()
        {
            var qualityScore = GetQualityScore();
            var injuryRate = TotalParticipants > 0 ? (TotalInjuries / (float)TotalParticipants) * 100f : 0f;
            
            return $"Quality: {qualityScore:P1}, Effectiveness: {AverageEffectiveness:F2}, " +
                   $"Success Rate: {SuccessRate:P1}, Injury Rate: {injuryRate:F1}%";
        }
    }
    
    /// <summary>
    /// Real-time session monitoring data
    /// </summary>
    [System.Serializable]
    public class SessionMonitoringData
    {
        public int SessionId { get; set; }
        public DateTime LastUpdate { get; set; }
        public Dictionary<int, PlayerLiveState> PlayerStates { get; set; } = new Dictionary<int, PlayerLiveState>();
        public List<SessionEvent> RecentEvents { get; set; } = new List<SessionEvent>();
        public SessionHealthStatus HealthStatus { get; set; }
        
        /// <summary>
        /// Add a new event to the monitoring data
        /// </summary>
        public void AddEvent(SessionEvent eventData)
        {
            RecentEvents.Add(eventData);
            LastUpdate = DateTime.Now;
            
            // Keep only recent events (last 50)
            if (RecentEvents.Count > 50)
            {
                RecentEvents = RecentEvents.TakeLast(50).ToList();
            }
        }
        
        /// <summary>
        /// Update health status based on current conditions
        /// </summary>
        public void UpdateHealthStatus()
        {
            var recentInjuries = RecentEvents.Count(e => e.Type == SessionEventType.PlayerInjured && 
                                                   (DateTime.Now - e.Timestamp).TotalMinutes < 10);
            
            var overloadedPlayers = PlayerStates.Values.Count(ps => ps.FatigueLevel > 80f);
            var totalPlayers = PlayerStates.Count;
            
            if (recentInjuries >= 2 || (totalPlayers > 0 && overloadedPlayers / (float)totalPlayers > 0.5f))
                HealthStatus = SessionHealthStatus.Critical;
            else if (recentInjuries >= 1 || (totalPlayers > 0 && overloadedPlayers / (float)totalPlayers > 0.3f))
                HealthStatus = SessionHealthStatus.Warning;
            else
                HealthStatus = SessionHealthStatus.Good;
        }
    }
    
    /// <summary>
    /// Live player state during session
    /// </summary>
    [System.Serializable]
    public class PlayerLiveState
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public float FatigueLevel { get; set; }
        public float ConditionLevel { get; set; }
        public float CurrentEffectiveness { get; set; }
        public int ComponentsCompleted { get; set; }
        public bool IsActive { get; set; } = true;
        public string CurrentActivity { get; set; }
        public DateTime LastUpdate { get; set; }
    }
    
    /// <summary>
    /// Session event for monitoring and logging
    /// </summary>
    [System.Serializable]
    public class SessionEvent
    {
        public SessionEventType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Create an injury event
        /// </summary>
        public static SessionEvent CreateInjuryEvent(int playerId, string playerName, Injury injury)
        {
            return new SessionEvent
            {
                Type = SessionEventType.PlayerInjured,
                Timestamp = DateTime.Now,
                PlayerId = playerId,
                PlayerName = playerName,
                Description = $"{playerName} injured: {injury.Description}",
                Data = new Dictionary<string, object>
                {
                    ["injuryType"] = injury.Type.ToString(),
                    ["severity"] = injury.Severity.ToString()
                }
            };
        }
        
        /// <summary>
        /// Create a component completion event
        /// </summary>
        public static SessionEvent CreateComponentCompletedEvent(int playerId, string playerName, TrainingComponentType componentType, float effectiveness)
        {
            return new SessionEvent
            {
                Type = SessionEventType.ComponentCompleted,
                Timestamp = DateTime.Now,
                PlayerId = playerId,
                PlayerName = playerName,
                Description = $"{playerName} completed {componentType}",
                Data = new Dictionary<string, object>
                {
                    ["componentType"] = componentType.ToString(),
                    ["effectiveness"] = effectiveness
                }
            };
        }
    }
    
    /// <summary>
    /// Session execution status
    /// </summary>
    public enum SessionExecutionStatus
    {
        NotStarted,
        Preparing,
        Running,
        Completed,
        Failed,
        Cancelled
    }
    
    /// <summary>
    /// Player participation grade
    /// </summary>
    public enum SessionParticipationGrade
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Injured,
        DidNotParticipate
    }
    
    /// <summary>
    /// Session health status
    /// </summary>
    public enum SessionHealthStatus
    {
        Good,
        Warning,
        Critical
    }
    
    /// <summary>
    /// Types of session events
    /// </summary>
    public enum SessionEventType
    {
        SessionStarted,
        SessionEnded,
        ComponentStarted,
        ComponentCompleted,
        PlayerInjured,
        PlayerFatigued,
        PlayerWithdrawn,
        SessionPaused,
        SessionResumed,
        EmergencyStop
    }
    
    /// <summary>
    /// Training session analytics summary
    /// </summary>
    [System.Serializable]
    public class SessionAnalyticsSummary
    {
        public int SessionId { get; set; }
        public DateTime SessionDate { get; set; }
        public string SessionName { get; set; }
        public DailySessionType SessionType { get; set; }
        public int TotalParticipants { get; set; }
        public int ComponentsPlanned { get; set; }
        public int ComponentsCompleted { get; set; }
        public float AverageEffectiveness { get; set; }
        public float TotalDevelopment { get; set; }
        public int TotalInjuries { get; set; }
        public float AvgFatigueIncrease { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public SessionParticipationGrade OverallGrade { get; set; }
        
        // Comparative metrics
        public float EffectivenessVsTarget { get; set; }
        public float InjuryRateVsExpected { get; set; }
        public float ParticipationRate { get; set; }
        
        /// <summary>
        /// Calculate comparison to target metrics
        /// </summary>
        public void CalculateComparativeMetrics(SessionTargetMetrics targets)
        {
            EffectivenessVsTarget = targets.TargetEffectiveness > 0 ? 
                AverageEffectiveness / targets.TargetEffectiveness : 1f;
                
            InjuryRateVsExpected = targets.ExpectedInjuries > 0 ? 
                TotalInjuries / (float)targets.ExpectedInjuries : 
                (TotalInjuries > 0 ? 2f : 0f); // If no expected injuries but some occurred
                
            ParticipationRate = targets.PlannedParticipants > 0 ? 
                TotalParticipants / (float)targets.PlannedParticipants : 1f;
        }
    }
    
    /// <summary>
    /// Target metrics for session planning
    /// </summary>
    [System.Serializable]
    public class SessionTargetMetrics
    {
        public float TargetEffectiveness { get; set; } = 1.0f;
        public int ExpectedInjuries { get; set; } = 0;
        public int PlannedParticipants { get; set; }
        public float TargetDevelopment { get; set; }
        public float MaxAcceptableInjuryRate { get; set; } = 0.05f; // 5%
        public float MinAcceptableEffectiveness { get; set; } = 0.8f;
    }
    
    /// <summary>
    /// Session execution configuration
    /// </summary>
    [System.Serializable]
    public class SessionExecutionConfig
    {
        public bool EnableRealTimeSimulation { get; set; } = false;
        public float RealTimeSpeedMultiplier { get; set; } = 60f;
        public bool EnableAutomaticIntensityReduction { get; set; } = true;
        public float FatigueInjuryThreshold { get; set; } = 75f;
        public float MaxInjuryRiskPerComponent { get; set; } = 0.1f;
        public bool AllowEarlyTermination { get; set; } = true;
        public int MinimumParticipantsThreshold { get; set; } = 3;
        public bool LogDetailedEvents { get; set; } = false;
        
        /// <summary>
        /// Create default configuration
        /// </summary>
        public static SessionExecutionConfig Default => new SessionExecutionConfig();
        
        /// <summary>
        /// Create safe configuration with conservative settings
        /// </summary>
        public static SessionExecutionConfig Safe => new SessionExecutionConfig
        {
            FatigueInjuryThreshold = 60f,
            MaxInjuryRiskPerComponent = 0.05f,
            EnableAutomaticIntensityReduction = true,
            AllowEarlyTermination = true
        };
        
        /// <summary>
        /// Create performance-focused configuration
        /// </summary>
        public static SessionExecutionConfig Performance => new SessionExecutionConfig
        {
            FatigueInjuryThreshold = 85f,
            MaxInjuryRiskPerComponent = 0.15f,
            EnableAutomaticIntensityReduction = false,
            AllowEarlyTermination = false
        };
    }
}