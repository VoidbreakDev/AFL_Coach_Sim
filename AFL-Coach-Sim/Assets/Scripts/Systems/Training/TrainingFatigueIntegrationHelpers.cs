using System;
using System.Collections.Generic;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Helper methods for the Training Fatigue Integration Manager
    /// Contains implementations for calculation and assessment methods
    /// </summary>
    public static class TrainingFatigueIntegrationHelpers
    {
        #region Load Capacity Calculations
        
        public static float CalculateRemainingCapacity(int playerId, PlayerLoadState loadState, float maxDailyLoad, float maxWeeklyLoad)
        {
            var dailyRemaining = Math.Max(0, maxDailyLoad - loadState.GetDailyLoad());
            var weeklyRemaining = Math.Max(0, maxWeeklyLoad - loadState.GetWeeklyLoad());
            
            // Return the more restrictive limit
            return Math.Min(dailyRemaining, weeklyRemaining / 7f); // Convert weekly to daily equivalent
        }
        
        public static float CalculateRecommendedRest(FatigueTrackingData fatigueData)
        {
            float baseSleepHours = 8f;
            float fatigueMultiplier = fatigueData.CurrentFatigueLevel / 100f;
            
            // High fatigue requires more rest
            float additionalRest = fatigueMultiplier * 4f; // Up to 4 additional hours
            
            // Factor in minimum recovery time
            float minimumRestFromTime = (float)fatigueData.MinimumRecoveryTimeRemaining.TotalHours;
            
            return Math.Max(baseSleepHours + additionalRest, minimumRestFromTime);
        }
        
        public static TimeSpan CalculateRecoveryTime(FatigueTrackingData fatigueData)
        {
            // Base recovery calculation
            float hoursNeeded = fatigueData.CurrentFatigueLevel / 15f; // 15 points per hour baseline
            
            // Add minimum recovery time
            hoursNeeded = Math.Max(hoursNeeded, (float)fatigueData.MinimumRecoveryTimeRemaining.TotalHours);
            
            return TimeSpan.FromHours(hoursNeeded);
        }
        
        public static TimeSpan CalculateFullRecoveryTime(FatigueTrackingData fatigueData)
        {
            // Full recovery to zero fatigue
            float baseRecoveryRate = 15f; // Points per hour
            float hoursForFullRecovery = fatigueData.CurrentFatigueLevel / baseRecoveryRate;
            
            // Add buffer time for complete restoration
            hoursForFullRecovery *= 1.2f;
            
            // Consider minimum recovery time
            hoursForFullRecovery = Math.Max(hoursForFullRecovery, (float)fatigueData.MinimumRecoveryTimeRemaining.TotalHours);
            
            return TimeSpan.FromHours(hoursForFullRecovery);
        }
        
        #endregion
        
        #region Assessment Methods
        
        public static FitnessForTraining DetermineFitnessForTraining(Player player, FatigueTrackingData fatigueData)
        {
            if (player == null) return FitnessForTraining.CompleteRest;
            
            var condition = player.Condition;
            var fatigueLevel = fatigueData.CurrentFatigueLevel;
            
            // Critical thresholds
            if (condition <= 40 || fatigueLevel >= 90)
                return FitnessForTraining.CompleteRest;
            
            if (condition <= 50 || fatigueLevel >= 75)
                return FitnessForTraining.RecoveryOnly;
            
            if (condition <= 65 || fatigueLevel >= 60)
                return FitnessForTraining.LightTrainingOnly;
            
            if (condition <= 75 || fatigueLevel >= 45)
                return FitnessForTraining.LimitedTraining;
            
            return FitnessForTraining.FullTraining;
        }
        
        public static LoadManagementStatus DetermineLoadManagementStatus(PlayerLoadState loadState, FatigueTrackingData fatigueData)
        {
            var weeklyLoad = loadState.GetWeeklyLoad();
            var dailyLoad = loadState.GetDailyLoad();
            var fatigueLevel = fatigueData.CurrentFatigueLevel;
            
            // Critical conditions
            if (fatigueLevel >= 90 || weeklyLoad >= 250 || dailyLoad >= 60)
                return LoadManagementStatus.Critical;
            
            // Excessive conditions
            if (fatigueLevel >= 75 || weeklyLoad >= 220 || dailyLoad >= 50)
                return LoadManagementStatus.Excessive;
            
            // High load conditions
            if (fatigueLevel >= 60 || weeklyLoad >= 180 || dailyLoad >= 40)
                return LoadManagementStatus.High;
            
            // Moderate conditions
            if (fatigueLevel >= 40 || weeklyLoad >= 140 || dailyLoad >= 30)
                return LoadManagementStatus.Moderate;
            
            return LoadManagementStatus.Optimal;
        }
        
        public static List<TrainingRestriction> GetTrainingRestrictions(int playerId, PlayerLoadState loadState, FatigueTrackingData fatigueData, Player player)
        {
            var restrictions = new List<TrainingRestriction>();
            
            if (player == null) return restrictions;
            
            var fitness = DetermineFitnessForTraining(player, fatigueData);
            var loadStatus = DetermineLoadManagementStatus(loadState, fatigueData);
            
            // Generate restrictions based on fitness level
            switch (fitness)
            {
                case FitnessForTraining.CompleteRest:
                    restrictions.Add(new TrainingRestriction
                    {
                        MaxAllowedIntensity = TrainingIntensity.Light,
                        MaxAllowedLoad = 0f,
                        MaxAllowedDuration = TimeSpan.Zero,
                        Reason = "Complete rest required due to high fatigue/low condition",
                        ExpiresAt = DateTime.Now.AddDays(1),
                        RestrictedActivities = new List<string> { "All training activities", "Match participation" },
                        RecommendedActivities = new List<string> { "Sleep", "Passive rest", "Light stretching" }
                    });
                    break;
                    
                case FitnessForTraining.RecoveryOnly:
                    restrictions.Add(new TrainingRestriction
                    {
                        MaxAllowedIntensity = TrainingIntensity.Light,
                        MaxAllowedLoad = 5f,
                        MaxAllowedDuration = TimeSpan.FromMinutes(30),
                        Reason = "Recovery activities only",
                        ExpiresAt = DateTime.Now.AddHours(12),
                        RestrictedActivities = new List<string> { "High intensity training", "Contact drills" },
                        RecommendedActivities = new List<string> { "Active recovery", "Light movement", "Recovery protocols" }
                    });
                    break;
                    
                case FitnessForTraining.LightTrainingOnly:
                    restrictions.Add(new TrainingRestriction
                    {
                        MaxAllowedIntensity = TrainingIntensity.Light,
                        MaxAllowedLoad = 15f,
                        MaxAllowedDuration = TimeSpan.FromHours(1),
                        Reason = "Light training only due to fatigue",
                        ExpiresAt = DateTime.Now.AddHours(8),
                        RestrictedActivities = new List<string> { "High intensity drills", "Heavy contact", "Sprint training" },
                        RecommendedActivities = new List<string> { "Skills training", "Light jogging", "Technical work" }
                    });
                    break;
                    
                case FitnessForTraining.LimitedTraining:
                    restrictions.Add(new TrainingRestriction
                    {
                        MaxAllowedIntensity = TrainingIntensity.Moderate,
                        MaxAllowedLoad = 25f,
                        MaxAllowedDuration = TimeSpan.FromHours(1.5),
                        Reason = "Limited training load recommended",
                        ExpiresAt = DateTime.Now.AddHours(6),
                        RestrictedActivities = new List<string> { "Maximum intensity training", "Extended sessions" },
                        RecommendedActivities = new List<string> { "Moderate training", "Skills focus", "Tactical work" }
                    });
                    break;
            }
            
            // Add load-specific restrictions
            if (loadStatus >= LoadManagementStatus.High)
            {
                restrictions.Add(new TrainingRestriction
                {
                    MaxAllowedIntensity = TrainingIntensity.Moderate,
                    MaxAllowedLoad = Math.Max(10f, 35f - loadState.GetDailyLoad()),
                    MaxAllowedDuration = TimeSpan.FromHours(1),
                    Reason = $"High training load status: {loadStatus}",
                    ExpiresAt = DateTime.Now.AddHours(4),
                    RestrictedActivities = new List<string> { "Additional training beyond current session" },
                    RecommendedActivities = new List<string> { "Recovery protocols", "Rest" }
                });
            }
            
            return restrictions;
        }
        
        #endregion
        
        #region Recommendation Generation
        
        public static string GeneratePostTrainingRecommendation(int playerId, PlayerLoadState loadState, FatigueTrackingData fatigueData, Player player = null)
        {
            var recommendations = new List<string>();
            
            var dailyLoad = loadState.GetDailyLoad();
            var weeklyLoad = loadState.GetWeeklyLoad();
            var fatigueLevel = fatigueData.CurrentFatigueLevel;
            var condition = player?.Condition ?? 70;
            
            // Immediate post-training recommendations
            if (fatigueLevel > 70)
            {
                recommendations.Add("🧊 Ice bath or cold water immersion within 30 minutes");
                recommendations.Add("💧 Prioritize hydration and electrolyte replacement");
            }
            
            if (dailyLoad > 35)
            {
                recommendations.Add("🍽️ Consume protein and carbohydrates within 2 hours");
                recommendations.Add("😴 Ensure 8+ hours of sleep tonight");
            }
            
            if (condition < 60)
            {
                recommendations.Add("⚠️ Consider skipping evening training session");
                recommendations.Add("🧘 Light stretching and relaxation only");
            }
            
            // Recovery timeframe recommendations
            var recoveryTime = CalculateRecoveryTime(fatigueData);
            if (recoveryTime.TotalHours > 12)
            {
                recommendations.Add($"⏰ Full recovery expected in {recoveryTime.TotalHours:F1} hours");
                recommendations.Add("📅 Plan lighter training load tomorrow");
            }
            
            // Weekly load management
            if (weeklyLoad > 150)
            {
                recommendations.Add("📊 Weekly load is elevated - monitor carefully");
                if (weeklyLoad > 200)
                {
                    recommendations.Add("🔴 Consider rest day within 48 hours");
                }
            }
            
            // Default recommendation if none triggered
            if (recommendations.Count == 0)
            {
                recommendations.Add("✅ Standard post-training recovery protocols");
                recommendations.Add("💪 Ready for next scheduled training");
            }
            
            return string.Join(Environment.NewLine, recommendations);
        }
        
        public static LoadManagementRecommendation GeneratePlayerRecommendation(int playerId, PlayerFatigueStatus status, PlayerLoadState loadState, FatigueTrackingData fatigueData, Player player = null)
        {
            // Only generate recommendations for players who need attention
            if (status.LoadManagementStatus < LoadManagementStatus.High)
                return null;
            
            var recommendation = new LoadManagementRecommendation
            {
                PlayerId = playerId,
                PlayerName = status.PlayerName
            };
            
            // Determine recommendation type and priority based on status
            switch (status.LoadManagementStatus)
            {
                case LoadManagementStatus.Critical:
                    recommendation.Type = LoadManagementType.RestDay;
                    recommendation.Priority = RecommendationPriority.Critical;
                    recommendation.Message = $"{status.PlayerName} requires immediate rest - critical fatigue/load levels";
                    recommendation.RecommendedAction = "Complete rest for 24-48 hours. Medical check if symptoms persist.";
                    recommendation.EstimatedBenefit = "Prevent injury and long-term performance decline";
                    recommendation.IsUrgent = true;
                    recommendation.AlternativeActions.Add("Light recovery activities only");
                    recommendation.AlternativeActions.Add("Consider sports medicine consultation");
                    break;
                    
                case LoadManagementStatus.Excessive:
                    recommendation.Type = LoadManagementType.LoadReduction;
                    recommendation.Priority = RecommendationPriority.High;
                    recommendation.Message = $"{status.PlayerName} has excessive training load";
                    recommendation.RecommendedAction = "Reduce training load by 50% for next 2-3 days";
                    recommendation.EstimatedBenefit = "Restore optimal training capacity and prevent overreaching";
                    recommendation.IsUrgent = true;
                    recommendation.AlternativeActions.Add("Switch to active recovery sessions");
                    recommendation.AlternativeActions.Add("Focus on skill-based low-intensity work");
                    break;
                    
                case LoadManagementStatus.High:
                    recommendation.Type = LoadManagementType.ModifiedTraining;
                    recommendation.Priority = RecommendationPriority.Medium;
                    recommendation.Message = $"{status.PlayerName} approaching load limits";
                    recommendation.RecommendedAction = "Monitor closely and consider modified training intensity";
                    recommendation.EstimatedBenefit = "Maintain performance while managing load accumulation";
                    recommendation.AlternativeActions.Add("Increase recovery activities");
                    recommendation.AlternativeActions.Add("Consider earlier rest day");
                    break;
                    
                default:
                    return null; // No recommendation needed
            }
            
            // Add specific fitness-based recommendations
            switch (status.FitnessForTraining)
            {
                case FitnessForTraining.CompleteRest:
                    recommendation.Type = LoadManagementType.RestDay;
                    recommendation.Priority = RecommendationPriority.Critical;
                    break;
                    
                case FitnessForTraining.RecoveryOnly:
                    recommendation.Type = LoadManagementType.RecoveryIncrease;
                    if (recommendation.Priority < RecommendationPriority.High)
                        recommendation.Priority = RecommendationPriority.High;
                    break;
                    
                case FitnessForTraining.LightTrainingOnly:
                    recommendation.Type = LoadManagementType.IntensityReduction;
                    break;
            }
            
            return recommendation;
        }
        
        public static void CheckForFatigueAlert(int playerId, FatigueTrackingData fatigueData, TrainingLoadResult result, System.Action<int, FatigueAlert> alertCallback, Player player = null)
        {
            var alerts = new List<FatigueAlert>();
            
            // High fatigue alert
            if (fatigueData.CurrentFatigueLevel >= 85f)
            {
                alerts.Add(new FatigueAlert
                {
                    PlayerId = playerId,
                    PlayerName = player?.Name ?? $"Player {playerId}",
                    AlertType = FatigueAlertType.HighFatigue,
                    Severity = AlertSeverity.High,
                    Message = $"High fatigue level detected ({fatigueData.CurrentFatigueLevel:F1})",
                    RecommendedAction = "Immediate rest and recovery required",
                    RequiresImmediateAttention = true,
                    FatigueLevel = fatigueData.CurrentFatigueLevel,
                    ConditionLevel = player?.Condition ?? 0
                });
            }
            
            // Rapid fatigue increase
            var recentFatigueRate = fatigueData.GetFatigueRate(6); // Last 6 hours
            if (recentFatigueRate > 20f) // More than 20 fatigue points per hour
            {
                alerts.Add(new FatigueAlert
                {
                    PlayerId = playerId,
                    PlayerName = player?.Name ?? $"Player {playerId}",
                    AlertType = FatigueAlertType.RapidFatigueIncrease,
                    Severity = AlertSeverity.Medium,
                    Message = $"Rapid fatigue accumulation detected ({recentFatigueRate:F1} points/hour)",
                    RecommendedAction = "Monitor closely and consider reducing training intensity",
                    FatigueLevel = fatigueData.CurrentFatigueLevel,
                    ConditionLevel = player?.Condition ?? 0
                });
            }
            
            // Poor recovery detection
            var recoveryRate = fatigueData.GetRecoveryRate(24); // Last 24 hours
            if (recoveryRate < 5f && fatigueData.CurrentFatigueLevel > 30f)
            {
                alerts.Add(new FatigueAlert
                {
                    PlayerId = playerId,
                    PlayerName = player?.Name ?? $"Player {playerId}",
                    AlertType = FatigueAlertType.PoorRecovery,
                    Severity = AlertSeverity.Medium,
                    Message = $"Poor recovery rate detected ({recoveryRate:F1} points/hour)",
                    RecommendedAction = "Investigate recovery practices and consider extended rest",
                    FatigueLevel = fatigueData.CurrentFatigueLevel,
                    ConditionLevel = player?.Condition ?? 0
                });
            }
            
            // Training effectiveness decline
            if (result.EffectivenessMultiplier < 0.7f)
            {
                alerts.Add(new FatigueAlert
                {
                    PlayerId = playerId,
                    PlayerName = player?.Name ?? $"Player {playerId}",
                    AlertType = FatigueAlertType.PerformanceDecline,
                    Severity = AlertSeverity.Low,
                    Message = $"Training effectiveness reduced to {result.EffectivenessMultiplier:P1}",
                    RecommendedAction = "Consider modified training approach or additional recovery",
                    FatigueLevel = fatigueData.CurrentFatigueLevel,
                    ConditionLevel = player?.Condition ?? 0
                });
            }
            
            // Send alerts
            foreach (var alert in alerts)
            {
                alertCallback?.Invoke(playerId, alert);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{timeSpan.Days}d {timeSpan.Hours}h";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            }
            else
            {
                return $"{timeSpan.Minutes}m";
            }
        }
        
        public static Color GetFatigueColor(float fatigueLevel)
        {
            if (fatigueLevel >= 80f) return Color.red;
            if (fatigueLevel >= 60f) return new Color(1f, 0.5f, 0f); // Orange
            if (fatigueLevel >= 40f) return Color.yellow;
            if (fatigueLevel >= 20f) return Color.green;
            return new Color(0f, 1f, 0f, 0.7f); // Light green
        }
        
        public static string GetConditionDescription(int condition)
        {
            return condition switch
            {
                >= 90 => "Excellent",
                >= 80 => "Very Good",
                >= 70 => "Good",
                >= 60 => "Fair",
                >= 50 => "Poor",
                >= 40 => "Very Poor",
                _ => "Critical"
            };
        }
        
        public static string GetFatigueDescription(float fatigueLevel)
        {
            return fatigueLevel switch
            {
                >= 90f => "Severely Fatigued",
                >= 75f => "Highly Fatigued",
                >= 60f => "Moderately Fatigued",
                >= 40f => "Lightly Fatigued",
                >= 20f => "Minimal Fatigue",
                _ => "Fresh"
            };
        }
        
        #endregion
    }
}