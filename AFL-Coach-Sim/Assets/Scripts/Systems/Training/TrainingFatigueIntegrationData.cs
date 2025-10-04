using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    #region Training Load Data Structures
    
    /// <summary>
    /// Tracks cumulative training load for a player over time
    /// </summary>
    [System.Serializable]
    public class PlayerLoadState
    {
        public int PlayerId { get; private set; }
        public float CurrentDailyLoad { get; private set; }
        public float CurrentWeeklyLoad { get; private set; }
        public DateTime LastLoadUpdate { get; private set; }
        
        private List<LoadEntry> loadHistory = new List<LoadEntry>();
        private Dictionary<DateTime, float> dailyLoads = new Dictionary<DateTime, float>();
        
        public PlayerLoadState(int playerId)
        {
            PlayerId = playerId;
            CurrentDailyLoad = 0f;
            CurrentWeeklyLoad = 0f;
            LastLoadUpdate = DateTime.Now;
        }
        
        public void AddTrainingLoad(float load, TrainingIntensity intensity, DateTime timestamp)
        {
            var entry = new LoadEntry
            {
                Load = load,
                Intensity = intensity,
                Timestamp = timestamp,
                Type = LoadType.Training
            };
            
            loadHistory.Add(entry);
            UpdateDailyLoad(load, timestamp.Date);
            UpdateWeeklyLoad();
            LastLoadUpdate = timestamp;
        }
        
        public void AddMatchLoad(float load, DateTime timestamp)
        {
            var entry = new LoadEntry
            {
                Load = load,
                Intensity = TrainingIntensity.VeryHigh, // Matches are always high intensity
                Timestamp = timestamp,
                Type = LoadType.Match
            };
            
            loadHistory.Add(entry);
            UpdateDailyLoad(load, timestamp.Date);
            UpdateWeeklyLoad();
            LastLoadUpdate = timestamp;
        }
        
        public void ApplyRecovery(float loadReduction)
        {
            CurrentDailyLoad = Math.Max(0, CurrentDailyLoad - loadReduction * 0.3f);
            CurrentWeeklyLoad = Math.Max(0, CurrentWeeklyLoad - loadReduction);
        }
        
        public void ApplyDailyDecay(DateTime currentDate, float decayRate)
        {
            // Decay loads older than today
            foreach (var date in dailyLoads.Keys.ToList())
            {
                if (date < currentDate)
                {
                    dailyLoads[date] *= decayRate;
                }
            }
            
            // Recalculate weekly load
            UpdateWeeklyLoad();
            
            // Reset daily load if new day
            if (currentDate > LastLoadUpdate.Date)
            {
                CurrentDailyLoad = 0f;
            }
        }
        
        public float GetDailyLoad()
        {
            return CurrentDailyLoad;
        }
        
        public float GetWeeklyLoad()
        {
            return CurrentWeeklyLoad;
        }
        
        public List<LoadEntry> GetLoadHistory(int days = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            return loadHistory.Where(entry => entry.Timestamp >= cutoffDate).ToList();
        }
        
        public float GetLoadByType(LoadType type, int days = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            return loadHistory
                .Where(entry => entry.Timestamp >= cutoffDate && entry.Type == type)
                .Sum(entry => entry.Load);
        }
        
        private void UpdateDailyLoad(float load, DateTime date)
        {
            if (!dailyLoads.ContainsKey(date))
            {
                dailyLoads[date] = 0f;
            }
            
            dailyLoads[date] += load;
            
            // Update current daily load if it's today
            if (date == DateTime.Now.Date)
            {
                CurrentDailyLoad = dailyLoads[date];
            }
        }
        
        private void UpdateWeeklyLoad()
        {
            var weekStart = DateTime.Now.AddDays(-7).Date;
            CurrentWeeklyLoad = dailyLoads
                .Where(kvp => kvp.Key >= weekStart)
                .Sum(kvp => kvp.Value);
        }
    }
    
    /// <summary>
    /// Individual load entry for tracking purposes
    /// </summary>
    [System.Serializable]
    public class LoadEntry
    {
        public float Load { get; set; }
        public TrainingIntensity Intensity { get; set; }
        public DateTime Timestamp { get; set; }
        public LoadType Type { get; set; }
        public string Notes { get; set; }
    }
    
    #endregion
    
    #region Fatigue Tracking Data
    
    /// <summary>
    /// Comprehensive fatigue tracking for a player
    /// </summary>
    [System.Serializable]
    public class FatigueTrackingData
    {
        public int PlayerId { get; private set; }
        public float CurrentFatigueLevel { get; private set; }
        public float MaxFatigueLevel { get; private set; }
        public DateTime LastFatigueUpdate { get; private set; }
        public TimeSpan MinimumRecoveryTimeRemaining { get; private set; }
        public bool HighFatigueAlertSent { get; set; }
        
        private List<FatigueEntry> fatigueHistory = new List<FatigueEntry>();
        private List<RecoveryEntry> recoveryHistory = new List<RecoveryEntry>();
        
        public FatigueTrackingData(int playerId)
        {
            PlayerId = playerId;
            CurrentFatigueLevel = 0f;
            MaxFatigueLevel = 0f;
            LastFatigueUpdate = DateTime.Now;
            MinimumRecoveryTimeRemaining = TimeSpan.Zero;
            HighFatigueAlertSent = false;
        }
        
        public void AccumulateFatigue(float fatigueAmount, float conditionImpact)
        {
            CurrentFatigueLevel += fatigueAmount;
            MaxFatigueLevel = Math.Max(MaxFatigueLevel, CurrentFatigueLevel);
            
            var entry = new FatigueEntry
            {
                FatigueAmount = fatigueAmount,
                ConditionImpact = conditionImpact,
                Timestamp = DateTime.Now,
                Source = FatigueSource.Training,
                ResultingFatigueLevel = CurrentFatigueLevel
            };
            
            fatigueHistory.Add(entry);
            LastFatigueUpdate = DateTime.Now;
            
            // Reset alert flag if fatigue increases
            if (fatigueAmount > 0)
            {
                HighFatigueAlertSent = false;
            }
        }
        
        public void AccumulateMatchFatigue(float fatigueAmount, float conditionImpact)
        {
            CurrentFatigueLevel += fatigueAmount;
            MaxFatigueLevel = Math.Max(MaxFatigueLevel, CurrentFatigueLevel);
            
            var entry = new FatigueEntry
            {
                FatigueAmount = fatigueAmount,
                ConditionImpact = conditionImpact,
                Timestamp = DateTime.Now,
                Source = FatigueSource.Match,
                ResultingFatigueLevel = CurrentFatigueLevel
            };
            
            fatigueHistory.Add(entry);
            LastFatigueUpdate = DateTime.Now;
            HighFatigueAlertSent = false;
        }
        
        public void ApplyRecovery(float recoveryAmount)
        {
            float actualRecovery = Math.Min(recoveryAmount, CurrentFatigueLevel);
            CurrentFatigueLevel -= actualRecovery;
            CurrentFatigueLevel = Math.Max(0f, CurrentFatigueLevel);
            
            LastFatigueUpdate = DateTime.Now;
            
            // Reset alert if fatigue is lowered
            if (CurrentFatigueLevel < 80f)
            {
                HighFatigueAlertSent = false;
            }
        }
        
        public void ApplyPassiveRecovery(float recoveryAmount)
        {
            ApplyRecovery(recoveryAmount);
            
            // Reduce minimum recovery time
            if (MinimumRecoveryTimeRemaining > TimeSpan.Zero)
            {
                var timeReduction = TimeSpan.FromHours(recoveryAmount * 0.1); // Small time reduction
                MinimumRecoveryTimeRemaining = MinimumRecoveryTimeRemaining.Subtract(timeReduction);
                if (MinimumRecoveryTimeRemaining < TimeSpan.Zero)
                {
                    MinimumRecoveryTimeRemaining = TimeSpan.Zero;
                }
            }
        }
        
        public void RecordRecoverySession(RecoveryType recoveryType, TimeSpan duration, float recoveryAmount)
        {
            var entry = new RecoveryEntry
            {
                RecoveryType = recoveryType,
                Duration = duration,
                RecoveryAmount = recoveryAmount,
                Timestamp = DateTime.Now,
                PreRecoveryFatigue = CurrentFatigueLevel + recoveryAmount, // Before recovery was applied
                PostRecoveryFatigue = CurrentFatigueLevel
            };
            
            recoveryHistory.Add(entry);
            
            // Reduce minimum recovery time for active recovery
            if (recoveryType != RecoveryType.PassiveRest)
            {
                var timeReduction = TimeSpan.FromHours(recoveryAmount * 0.2);
                MinimumRecoveryTimeRemaining = MinimumRecoveryTimeRemaining.Subtract(timeReduction);
                if (MinimumRecoveryTimeRemaining < TimeSpan.Zero)
                {
                    MinimumRecoveryTimeRemaining = TimeSpan.Zero;
                }
            }
        }
        
        public void SetMinimumRecoveryTime(TimeSpan recoveryTime)
        {
            MinimumRecoveryTimeRemaining = recoveryTime;
        }
        
        public List<FatigueEntry> GetFatigueHistory(int days = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            return fatigueHistory.Where(entry => entry.Timestamp >= cutoffDate).ToList();
        }
        
        public List<RecoveryEntry> GetRecoveryHistory(int days = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            return recoveryHistory.Where(entry => entry.Timestamp >= cutoffDate).ToList();
        }
        
        public float GetFatigueRate(int hours = 24)
        {
            var cutoffTime = DateTime.Now.AddHours(-hours);
            var recentFatigue = fatigueHistory
                .Where(entry => entry.Timestamp >= cutoffTime)
                .Sum(entry => entry.FatigueAmount);
            
            return recentFatigue / hours;
        }
        
        public float GetRecoveryRate(int hours = 24)
        {
            var cutoffTime = DateTime.Now.AddHours(-hours);
            var recentRecovery = recoveryHistory
                .Where(entry => entry.Timestamp >= cutoffTime)
                .Sum(entry => entry.RecoveryAmount);
            
            return recentRecovery / hours;
        }
    }
    
    /// <summary>
    /// Individual fatigue entry for tracking purposes
    /// </summary>
    [System.Serializable]
    public class FatigueEntry
    {
        public float FatigueAmount { get; set; }
        public float ConditionImpact { get; set; }
        public DateTime Timestamp { get; set; }
        public FatigueSource Source { get; set; }
        public float ResultingFatigueLevel { get; set; }
        public string Notes { get; set; }
    }
    
    /// <summary>
    /// Individual recovery entry for tracking purposes
    /// </summary>
    [System.Serializable]
    public class RecoveryEntry
    {
        public RecoveryType RecoveryType { get; set; }
        public TimeSpan Duration { get; set; }
        public float RecoveryAmount { get; set; }
        public DateTime Timestamp { get; set; }
        public float PreRecoveryFatigue { get; set; }
        public float PostRecoveryFatigue { get; set; }
        public string Notes { get; set; }
    }
    
    #endregion
    
    #region Result Types
    
    /// <summary>
    /// Result of applying training load to a player
    /// </summary>
    public class TrainingLoadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public float LoadApplied { get; set; }
        public float ConditionChange { get; set; }
        public float EffectivenessMultiplier { get; set; }
        public float FatigueLevel { get; set; }
        public TimeSpan RecoveryTimeRequired { get; set; }
        public string PostTrainingRecommendation { get; set; }
        public string RecommendedAction { get; set; }
    }
    
    /// <summary>
    /// Result of applying recovery to a player
    /// </summary>
    public class RecoveryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public RecoveryType RecoveryType { get; set; }
        public float FatigueReduction { get; set; }
        public float ConditionImprovement { get; set; }
        public float LoadReduction { get; set; }
        public float NewFatigueLevel { get; set; }
        public int NewCondition { get; set; }
    }
    
    /// <summary>
    /// Check if player can handle additional training load
    /// </summary>
    public class PlayerLoadCapacityCheck
    {
        public int PlayerId { get; set; }
        public bool CanTrain { get; set; }
        public string Reason { get; set; }
        public string RecommendedAction { get; set; }
        public float RemainingDailyCapacity { get; set; }
        public float RemainingWeeklyCapacity { get; set; }
    }
    
    /// <summary>
    /// Comprehensive fatigue status for a player
    /// </summary>
    public class PlayerFatigueStatus
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int CurrentCondition { get; set; }
        public float CurrentFatigueLevel { get; set; }
        public float DailyLoadAccumulated { get; set; }
        public float WeeklyLoadAccumulated { get; set; }
        public float TrainingCapacityRemaining { get; set; }
        public float RecommendedRestHours { get; set; }
        public FitnessForTraining FitnessForTraining { get; set; }
        public LoadManagementStatus LoadManagementStatus { get; set; }
        public List<TrainingRestriction> NextTrainingRestrictions { get; set; }
        public TimeSpan EstimatedFullRecoveryTime { get; set; }
    }
    
    /// <summary>
    /// Load management recommendation for coaching staff
    /// </summary>
    public class LoadManagementRecommendation
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public LoadManagementType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Message { get; set; }
        public string RecommendedAction { get; set; }
        public string EstimatedBenefit { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsUrgent { get; set; }
        public List<string> AlternativeActions { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Fatigue alert for monitoring systems
    /// </summary>
    public class FatigueAlert
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public FatigueAlertType AlertType { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; }
        public string RecommendedAction { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool RequiresImmediateAttention { get; set; }
        public float FatigueLevel { get; set; }
        public float ConditionLevel { get; set; }
    }
    
    /// <summary>
    /// Recovery session recommendation
    /// </summary>
    public class RecoverySession
    {
        public int PlayerId { get; set; }
        public RecoveryType RecommendedType { get; set; }
        public TimeSpan RecommendedDuration { get; set; }
        public string Rationale { get; set; }
        public float ExpectedBenefit { get; set; }
        public RecoveryPriority Priority { get; set; }
        public DateTime RecommendedStartTime { get; set; }
        public List<string> Equipment { get; set; } = new List<string>();
        public List<string> Instructions { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Training restriction for load management
    /// </summary>
    public class TrainingRestriction
    {
        public TrainingIntensity MaxAllowedIntensity { get; set; }
        public float MaxAllowedLoad { get; set; }
        public TimeSpan MaxAllowedDuration { get; set; }
        public string Reason { get; set; }
        public DateTime ExpiresAt { get; set; }
        public List<string> RestrictedActivities { get; set; } = new List<string>();
        public List<string> RecommendedActivities { get; set; } = new List<string>();
    }
    
    #endregion
    
    #region Enumerations
    
    /// <summary>
    /// Training intensity levels
    /// </summary>
    public enum TrainingIntensity
    {
        Light = 1,
        Moderate = 2,
        High = 3,
        VeryHigh = 4
    }
    
    /// <summary>
    /// Type of training load
    /// </summary>
    public enum LoadType
    {
        Training,
        Match,
        Recovery,
        Other
    }
    
    /// <summary>
    /// Source of fatigue accumulation
    /// </summary>
    public enum FatigueSource
    {
        Training,
        Match,
        Travel,
        Stress,
        Illness,
        Other
    }
    
    /// <summary>
    /// Types of recovery activities
    /// </summary>
    public enum RecoveryType
    {
        PassiveRest,
        ActiveRecovery,
        Sleep,
        Massage,
        IceBath,
        Nutrition,
        Stretching,
        Meditation,
        Physiotherapy,
        Other
    }
    
    /// <summary>
    /// Player fitness assessment for training
    /// </summary>
    public enum FitnessForTraining
    {
        FullTraining,         // Can handle full training load
        LimitedTraining,      // Should have reduced training load
        LightTrainingOnly,    // Only light training recommended
        RecoveryOnly,         // No training, recovery activities only
        CompleteRest          // Complete rest required
    }
    
    /// <summary>
    /// Load management status classification
    /// </summary>
    public enum LoadManagementStatus
    {
        Optimal,              // Training load is in optimal range
        Moderate,             // Load is manageable but approaching limits
        High,                 // Load is high, careful monitoring required
        Excessive,            // Load is too high, reduction required
        Critical              // Immediate intervention required
    }
    
    /// <summary>
    /// Types of load management recommendations
    /// </summary>
    public enum LoadManagementType
    {
        LoadReduction,
        IntensityReduction,
        RecoveryIncrease,
        RestDay,
        MedicalAttention,
        ModifiedTraining,
        RotationRecommendation
    }
    
    /// <summary>
    /// Priority levels for recommendations
    /// </summary>
    public enum RecommendationPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
    
    /// <summary>
    /// Types of fatigue alerts
    /// </summary>
    public enum FatigueAlertType
    {
        HighFatigue,
        RapidFatigueIncrease,
        PoorRecovery,
        ExcessiveLoad,
        InjuryRisk,
        PerformanceDecline
    }
    
    /// <summary>
    /// Alert severity levels
    /// </summary>
    public enum AlertSeverity
    {
        Info = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        Critical = 5
    }
    
    /// <summary>
    /// Recovery session priority
    /// </summary>
    public enum RecoveryPriority
    {
        Optional = 1,
        Recommended = 2,
        Important = 3,
        Critical = 4
    }
    
    #endregion
    
    #region Extension Methods
    
    /// <summary>
    /// Extension methods for training intensity
    /// </summary>
    public static class TrainingIntensityExtensions
    {
        public static float GetMultiplier(this TrainingIntensity intensity)
        {
            return intensity switch
            {
                TrainingIntensity.Light => 0.6f,
                TrainingIntensity.Moderate => 1.0f,
                TrainingIntensity.High => 1.4f,
                TrainingIntensity.VeryHigh => 1.8f,
                _ => 1.0f
            };
        }
        
        public static Color GetColor(this TrainingIntensity intensity)
        {
            return intensity switch
            {
                TrainingIntensity.Light => Color.green,
                TrainingIntensity.Moderate => Color.yellow,
                TrainingIntensity.High => new Color(1f, 0.5f, 0f), // Orange
                TrainingIntensity.VeryHigh => Color.red,
                _ => Color.white
            };
        }
        
        public static string GetDescription(this TrainingIntensity intensity)
        {
            return intensity switch
            {
                TrainingIntensity.Light => "Light - Recovery and skill work",
                TrainingIntensity.Moderate => "Moderate - Standard training load",
                TrainingIntensity.High => "High - Intense training session",
                TrainingIntensity.VeryHigh => "Very High - Maximum intensity",
                _ => "Unknown intensity level"
            };
        }
    }
    
    /// <summary>
    /// Extension methods for fatigue status
    /// </summary>
    public static class FatigueStatusExtensions
    {
        public static Color GetStatusColor(this LoadManagementStatus status)
        {
            return status switch
            {
                LoadManagementStatus.Optimal => Color.green,
                LoadManagementStatus.Moderate => Color.yellow,
                LoadManagementStatus.High => new Color(1f, 0.5f, 0f), // Orange
                LoadManagementStatus.Excessive => Color.red,
                LoadManagementStatus.Critical => Color.magenta,
                _ => Color.white
            };
        }
        
        public static string GetStatusDescription(this LoadManagementStatus status)
        {
            return status switch
            {
                LoadManagementStatus.Optimal => "Training load is optimal",
                LoadManagementStatus.Moderate => "Load is manageable",
                LoadManagementStatus.High => "High load - monitor carefully",
                LoadManagementStatus.Excessive => "Load too high - reduce immediately",
                LoadManagementStatus.Critical => "Critical - immediate intervention required",
                _ => "Status unknown"
            };
        }
        
        public static bool RequiresAttention(this LoadManagementStatus status)
        {
            return status >= LoadManagementStatus.High;
        }
    }
    
    #endregion
}