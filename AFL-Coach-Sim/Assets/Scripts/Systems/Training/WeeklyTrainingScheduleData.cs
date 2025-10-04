using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Represents a complete weekly training schedule
    /// </summary>
    [System.Serializable]
    public class WeeklyTrainingSchedule
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public WeeklyScheduleTemplate Template { get; set; }
        public List<DailyTrainingSession> DailySessions { get; set; } = new List<DailyTrainingSession>();
        public List<ScheduledMatch> WeekMatches { get; set; } = new List<ScheduledMatch>();
        
        /// <summary>
        /// Get total planned training load for the week
        /// </summary>
        public float GetTotalWeeklyLoad()
        {
            return DailySessions.Sum(s => s.TrainingComponents.Sum(c => c.LoadMultiplier));
        }
        
        /// <summary>
        /// Check if the week contains a match
        /// </summary>
        public bool IsMatchWeek()
        {
            return WeekMatches.Any();
        }
        
        /// <summary>
        /// Get next match in the week (if any)
        /// </summary>
        public ScheduledMatch GetNextMatch(DateTime fromDate)
        {
            return WeekMatches
                .Where(m => m.ScheduledDateTime >= fromDate)
                .OrderBy(m => m.ScheduledDateTime)
                .FirstOrDefault();
        }
    }
    
    /// <summary>
    /// Template defining how to structure training for a week
    /// </summary>
    [System.Serializable]
    public class WeeklyScheduleTemplate
    {
        public string TemplateName { get; set; }
        public ScheduleTemplateType TemplateType { get; set; }
        public List<DailySessionTemplate> DayTemplates { get; set; } = new List<DailySessionTemplate>();
        public string Description { get; set; }
        
        /// <summary>
        /// Get the template for a specific day
        /// </summary>
        public DailySessionTemplate GetDayTemplate(DayOfWeek dayOfWeek)
        {
            return DayTemplates.FirstOrDefault(dt => dt.DayOfWeek == dayOfWeek);
        }
        
        /// <summary>
        /// Get total planned load for this template
        /// </summary>
        public float GetTemplateLoad()
        {
            return DayTemplates.Where(dt => !dt.IsRestDay)
                              .Sum(dt => dt.Components.Sum(c => c.LoadMultiplier));
        }
    }
    
    /// <summary>
    /// Template for a single training day
    /// </summary>
    [System.Serializable]
    public class DailySessionTemplate
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string SessionName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DailySessionType SessionType { get; set; }
        public bool IsRestDay { get; set; } = false;
        public bool SkipOnMatchDay { get; set; } = false;
        public List<ComponentTemplate> Components { get; set; } = new List<ComponentTemplate>();
        
        /// <summary>
        /// Get total load for this day template
        /// </summary>
        public float GetDayLoad()
        {
            return IsRestDay ? 0 : Components.Sum(c => c.LoadMultiplier);
        }
    }
    
    /// <summary>
    /// Template for individual training components within a session
    /// </summary>
    [System.Serializable]
    public class ComponentTemplate
    {
        public TrainingComponentType Type { get; set; }
        public TrainingFocus Focus { get; set; }
        public TimeSpan Duration { get; set; }
        public TrainingIntensity Intensity { get; set; }
        public float LoadMultiplier { get; set; }
        public string Notes { get; set; }
    }
    
    /// <summary>
    /// Represents an actual daily training session
    /// </summary>
    [System.Serializable]
    public class DailyTrainingSession
    {
        public int SessionId { get; set; }
        public DateTime SessionDate { get; set; }
        public string SessionName { get; set; }
        public TimeSpan ScheduledStartTime { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public DailySessionType SessionType { get; set; }
        public TrainingSessionStatus Status { get; set; } = TrainingSessionStatus.Scheduled;
        public List<TrainingComponent> TrainingComponents { get; set; } = new List<TrainingComponent>();
        public List<int> TargetParticipants { get; set; } = new List<int>(); // Player IDs
        public List<int> ActualParticipants { get; set; } = new List<int>(); // Player IDs who attended
        public DateTime? CompletionTime { get; set; }
        
        /// <summary>
        /// Get scheduled start date and time
        /// </summary>
        public DateTime GetScheduledDateTime()
        {
            return SessionDate.Add(ScheduledStartTime);
        }
        
        /// <summary>
        /// Get total load for this session
        /// </summary>
        public float GetSessionLoad()
        {
            return TrainingComponents.Sum(c => c.LoadMultiplier);
        }
        
        /// <summary>
        /// Check if session is in the past
        /// </summary>
        public bool IsPastDue()
        {
            return GetScheduledDateTime() < DateTime.Now;
        }
    }
    
    /// <summary>
    /// Individual training component within a session
    /// </summary>
    [System.Serializable]
    public class TrainingComponent
    {
        public TrainingComponentType ComponentType { get; set; }
        public TrainingProgram Program { get; set; }
        public TimeSpan Duration { get; set; }
        public TrainingIntensity Intensity { get; set; }
        public float LoadMultiplier { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Get effective load considering intensity
        /// </summary>
        public float GetEffectiveLoad()
        {
            var intensityMultiplier = Intensity switch
            {
                TrainingIntensity.Light => 0.7f,
                TrainingIntensity.Moderate => 1.0f,
                TrainingIntensity.High => 1.3f,
                TrainingIntensity.VeryHigh => 1.6f,
                _ => 1.0f
            };
            
            return LoadMultiplier * intensityMultiplier;
        }
    }
    
    /// <summary>
    /// Tracks a player's weekly training load
    /// </summary>
    [System.Serializable]
    public class PlayerWeeklyLoad
    {
        public int PlayerId { get; set; }
        public float CurrentLoad { get; private set; } = 0f;
        public float MaxLoad { get; set; } = 100f;
        public List<TrainingLoadEntry> LoadEntries { get; set; } = new List<TrainingLoadEntry>();
        public DateTime WeekStartDate { get; set; }
        
        /// <summary>
        /// Add training load to the player's weekly total
        /// </summary>
        public void AddTrainingLoad(float load, float effectiveness = 1.0f)
        {
            var entry = new TrainingLoadEntry
            {
                Load = load,
                Effectiveness = effectiveness,
                Timestamp = DateTime.Now
            };
            
            LoadEntries.Add(entry);
            CurrentLoad += load;
        }
        
        /// <summary>
        /// Reset weekly load tracking
        /// </summary>
        public void ResetWeekly()
        {
            CurrentLoad = 0f;
            LoadEntries.Clear();
            WeekStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Start of current week
        }
        
        /// <summary>
        /// Get load utilization percentage
        /// </summary>
        public float GetLoadUtilization()
        {
            return MaxLoad > 0 ? (CurrentLoad / MaxLoad) * 100f : 0f;
        }
        
        /// <summary>
        /// Check if player is approaching load limit
        /// </summary>
        public bool IsApproachingLimit(float threshold = 0.8f)
        {
            return GetLoadUtilization() >= (threshold * 100f);
        }
        
        /// <summary>
        /// Get average effectiveness of training this week
        /// </summary>
        public float GetAverageEffectiveness()
        {
            return LoadEntries.Any() ? LoadEntries.Average(le => le.Effectiveness) : 0f;
        }
    }
    
    /// <summary>
    /// Individual training load entry
    /// </summary>
    [System.Serializable]
    public class TrainingLoadEntry
    {
        public float Load { get; set; }
        public float Effectiveness { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } // Which training component/program
    }
    
    /// <summary>
    /// Types of training schedule templates
    /// </summary>
    public enum ScheduleTemplateType
    {
        Standard,           // Regular season training
        MatchWeek,          // Week with matches
        ByeWeek,           // Bye round week
        PreSeason,         // Pre-season conditioning
        Finals,            // Finals preparation
        PostSeason,        // Off-season maintenance
        Recovery,          // Recovery week
        Intensive          // High-load week
    }
    
    /// <summary>
    /// Types of daily training sessions
    /// </summary>
    public enum DailySessionType
    {
        Main,              // Primary training session
        Supplementary,     // Additional/optional training
        Recovery,          // Recovery and regeneration
        MatchPreparation,  // Pre-match preparation
        SkillsOnly,       // Skills-focused session
        FitnessOnly,      // Fitness-focused session
        Tactical          // Tactical/strategy session
    }
    
    /// <summary>
    /// Types of training components
    /// </summary>
    public enum TrainingComponentType
    {
        Skills,           // Ball skills, kicking, marking
        Fitness,          // Endurance, strength, speed
        Tactical,         // Game plan, positioning
        Recovery,         // Rest, regeneration
        Specialized,      // Position-specific training
        Mental,           // Psychology, mental preparation
        Medical          // Injury prevention, rehabilitation
    }
    
    /// <summary>
    /// Status of training sessions
    /// </summary>
    public enum TrainingSessionStatus
    {
        Scheduled,        // Planned but not started
        InProgress,       // Currently running
        Completed,        // Finished successfully
        Cancelled,        // Cancelled due to weather/other
        Modified,         // Changed from original plan
        Postponed         // Delayed to another time
    }
    
    /// <summary>
    /// Weekly training analytics
    /// </summary>
    [System.Serializable]
    public class WeeklyTrainingAnalytics
    {
        public DateTime WeekStartDate { get; set; }
        public int TotalSessionsScheduled { get; set; }
        public int TotalSessionsCompleted { get; set; }
        public float AveragePlayerLoad { get; set; }
        public float AverageEffectiveness { get; set; }
        public int PlayersOverLoaded { get; set; }
        public Dictionary<TrainingFocus, int> FocusDistribution { get; set; } = new Dictionary<TrainingFocus, int>();
        public Dictionary<DailySessionType, int> SessionTypeBreakdown { get; set; } = new Dictionary<DailySessionType, int>();
        
        /// <summary>
        /// Get completion rate as percentage
        /// </summary>
        public float GetCompletionRate()
        {
            return TotalSessionsScheduled > 0 ? 
                (float)TotalSessionsCompleted / TotalSessionsScheduled * 100f : 0f;
        }
        
        /// <summary>
        /// Get overload rate as percentage
        /// </summary>
        public float GetOverloadRate(int totalPlayers)
        {
            return totalPlayers > 0 ? 
                (float)PlayersOverLoaded / totalPlayers * 100f : 0f;
        }
    }
    
    /// <summary>
    /// Training schedule validation result
    /// </summary>
    public class ScheduleValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }
        
        public string GetSummary()
        {
            var parts = new List<string>();
            
            if (Errors.Any())
                parts.Add($"{Errors.Count} errors");
            if (Warnings.Any())
                parts.Add($"{Warnings.Count} warnings");
                
            return parts.Any() ? 
                $"Validation: {string.Join(", ", parts)}" : 
                "Validation: All clear";
        }
    }
}