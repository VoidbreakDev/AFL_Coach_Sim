using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLManager.Models;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Integrates training load management with the existing fatigue model
    /// Provides sophisticated load tracking, recovery management, and fatigue-based training adjustments
    /// </summary>
    public class TrainingFatigueIntegrationManager : MonoBehaviour
    {
        [Header("System Dependencies")]
        [SerializeField] private FatigueModel fatigueModel;
        [SerializeField] private WeeklyTrainingScheduleManager scheduleManager;
        [SerializeField] private DailyTrainingSessionExecutor sessionExecutor;
        
        [Header("Load Management Configuration")]
        [SerializeField] private float maxDailyTrainingLoad = 50f;
        [SerializeField] private float maxWeeklyTrainingLoad = 200f;
        [SerializeField] private float fatigueRecoveryRate = 15f; // Points per hour of rest
        [SerializeField] private float trainingLoadDecayRate = 0.85f; // Daily decay multiplier
        
        [Header("Fatigue Thresholds")]
        [SerializeField] private float lightTrainingThreshold = 75f; // Condition below this = light training only
        [SerializeField] private float noTrainingThreshold = 50f; // Condition below this = no training
        // [SerializeField] private float highRiskThreshold = 60f; // Condition below this = high injury risk // TODO: Implement high risk threshold logic
        
        [Header("Recovery Configuration")]
        // [SerializeField] private bool enableActiveRecovery = true; // TODO: Implement active recovery toggle
        [SerializeField] private float activeRecoveryMultiplier = 1.2f;
        // [SerializeField] private int sleepRecoveryHours = 8; // TODO: Implement sleep recovery calculation
        [SerializeField] private float nutritionRecoveryBonus = 1.15f;
        
        // Player tracking data
        private Dictionary<int, PlayerLoadState> playerLoadStates = new Dictionary<int, PlayerLoadState>();
        private Dictionary<int, FatigueTrackingData> fatigueTrackingData = new Dictionary<int, FatigueTrackingData>();
        private DateTime lastUpdateTime = DateTime.Now;
        
        // Events for system integration
        public event System.Action<int, PlayerLoadState> OnPlayerLoadStateChanged;
        public event System.Action<int, FatigueAlert> OnFatigueAlert;
        public event System.Action<LoadManagementRecommendation> OnLoadManagementRecommendation;
        // public event System.Action<int, RecoverySession> OnRecoverySessionRecommended; // TODO: Implement recovery session recommendation system
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            ProcessContinuousRecovery();
            CheckFatigueAlerts();
            DecayTrainingLoads();
        }
        
        /// <summary>
        /// Initialize the fatigue integration system
        /// </summary>
        public void Initialize()
        {
            // Find fatigue model if not assigned
            // Note: FatigueModel is not a MonoBehaviour, so we can't use FindObjectOfType
            // It should be injected or created separately
            if (fatigueModel == null)
            {
                Debug.LogWarning("[TrainingFatigueIntegration] FatigueModel not assigned. Some features may be unavailable.");
            }
            
            // Subscribe to training system events if available
            if (scheduleManager != null)
            {
                scheduleManager.OnPlayerLoadExceeded += OnPlayerLoadExceeded;
            }
            
            if (sessionExecutor != null)
            {
                sessionExecutor.OnPlayerSessionResult += OnPlayerSessionResult;
                sessionExecutor.OnTrainingInjuryOccurred += OnTrainingInjury;
            }
            
            Debug.Log("[TrainingFatigueIntegration] Fatigue integration system initialized");
        }
        
        /// <summary>
        /// Apply training load to a player and update their fatigue state
        /// </summary>
        public TrainingLoadResult ApplyTrainingLoad(int playerId, float trainingLoad, TrainingIntensity intensity, TimeSpan duration)
        {
            var player = GetPlayer(playerId);
            if (player == null)
            {
                return new TrainingLoadResult 
                { 
                    Success = false, 
                    Message = "Player not found" 
                };
            }
            
            var loadState = GetPlayerLoadState(playerId);
            var fatigueData = GetFatigueTrackingData(playerId);
            
            // Check if player can handle additional load
            var preLoadCheck = CanPlayerHandleLoad(playerId, trainingLoad, intensity);
            if (!preLoadCheck.CanTrain)
            {
                return new TrainingLoadResult
                {
                    Success = false,
                    Message = preLoadCheck.Reason,
                    RecommendedAction = preLoadCheck.RecommendedAction
                };
            }
            
            // Calculate load impact on condition/fatigue
            var conditionImpact = CalculateConditionImpact(trainingLoad, intensity, duration, player);
            var fatigueImpact = CalculateFatigueImpact(trainingLoad, intensity, player);
            
            // Apply the load
            loadState.AddTrainingLoad(trainingLoad, intensity, DateTime.Now);
            fatigueData.AccumulateFatigue(fatigueImpact, conditionImpact);
            
            // Update player condition using fatigue model principles
            ApplyConditionChange(player, -conditionImpact); // Negative because it's reducing condition
            
            // Calculate effectiveness reduction due to fatigue
            var effectivenessMultiplier = CalculateTrainingEffectiveness(player, loadState);
            
            var result = new TrainingLoadResult
            {
                Success = true,
                Message = "Training load applied successfully",
                LoadApplied = trainingLoad,
                ConditionChange = -conditionImpact,
                EffectivenessMultiplier = effectivenessMultiplier,
                FatigueLevel = fatigueData.CurrentFatigueLevel,
                RecoveryTimeRequired = TrainingFatigueIntegrationHelpers.CalculateRecoveryTime(fatigueData),
                PostTrainingRecommendation = TrainingFatigueIntegrationHelpers.GeneratePostTrainingRecommendation(playerId, loadState, fatigueData, player)
            };
            
            // Trigger events
            OnPlayerLoadStateChanged?.Invoke(playerId, loadState);
            TrainingFatigueIntegrationHelpers.CheckForFatigueAlert(playerId, fatigueData, result, OnFatigueAlert, player);
            
            return result;
        }
        
        /// <summary>
        /// Process active recovery for a player
        /// </summary>
        public RecoveryResult ProcessActiveRecovery(int playerId, RecoveryType recoveryType, TimeSpan duration)
        {
            var player = GetPlayer(playerId);
            if (player == null)
            {
                return new RecoveryResult { Success = false, Message = "Player not found" };
            }
            
            var loadState = GetPlayerLoadState(playerId);
            var fatigueData = GetFatigueTrackingData(playerId);
            
            // Calculate recovery benefits
            var recoveryAmount = CalculateRecoveryAmount(recoveryType, duration, player);
            var conditionRecovery = CalculateConditionRecovery(recoveryType, duration, player);
            var loadReduction = CalculateLoadReduction(recoveryType, duration);
            
            // Apply recovery effects
            fatigueData.ApplyRecovery(recoveryAmount);
            loadState.ApplyRecovery(loadReduction);
            ApplyConditionChange(player, conditionRecovery); // Positive change for recovery
            
            var result = new RecoveryResult
            {
                Success = true,
                RecoveryType = recoveryType,
                FatigueReduction = recoveryAmount,
                ConditionImprovement = conditionRecovery,
                LoadReduction = loadReduction,
                NewFatigueLevel = fatigueData.CurrentFatigueLevel,
                NewCondition = (int)player.Stamina,
                Message = $"Recovery applied: -{recoveryAmount:F1} fatigue, +{conditionRecovery:F1} condition"
            };
            
            // Update tracking
            fatigueData.RecordRecoverySession(recoveryType, duration, recoveryAmount);
            OnPlayerLoadStateChanged?.Invoke(playerId, loadState);
            
            return result;
        }
        
        /// <summary>
        /// Get comprehensive load and fatigue status for a player
        /// </summary>
        public PlayerFatigueStatus GetPlayerFatigueStatus(int playerId)
        {
            var player = GetPlayer(playerId);
            var loadState = GetPlayerLoadState(playerId);
            var fatigueData = GetFatigueTrackingData(playerId);
            
            var status = new PlayerFatigueStatus
            {
                PlayerId = playerId,
                PlayerName = player?.Name ?? "Unknown",
                CurrentCondition = (int)(player?.Stamina ?? 0),
                CurrentFatigueLevel = fatigueData.CurrentFatigueLevel,
                DailyLoadAccumulated = loadState.GetDailyLoad(),
                WeeklyLoadAccumulated = loadState.GetWeeklyLoad(),
                TrainingCapacityRemaining = TrainingFatigueIntegrationHelpers.CalculateRemainingCapacity(playerId, loadState, maxDailyTrainingLoad, maxWeeklyTrainingLoad),
                RecommendedRestHours = TrainingFatigueIntegrationHelpers.CalculateRecommendedRest(fatigueData),
                FitnessForTraining = TrainingFatigueIntegrationHelpers.DetermineFitnessForTraining(player, fatigueData),
                LoadManagementStatus = TrainingFatigueIntegrationHelpers.DetermineLoadManagementStatus(loadState, fatigueData),
                NextTrainingRestrictions = TrainingFatigueIntegrationHelpers.GetTrainingRestrictions(playerId, loadState, fatigueData, player),
                EstimatedFullRecoveryTime = TrainingFatigueIntegrationHelpers.CalculateFullRecoveryTime(fatigueData)
            };
            
            return status;
        }
        
        /// <summary>
        /// Generate load management recommendations for the team
        /// </summary>
        public List<LoadManagementRecommendation> GenerateLoadManagementRecommendations(List<int> playerIds)
        {
            var recommendations = new List<LoadManagementRecommendation>();
            
            foreach (var playerId in playerIds)
            {
                var status = GetPlayerFatigueStatus(playerId);
                var loadState = GetPlayerLoadState(playerId);
                var fatigueData = GetFatigueTrackingData(playerId);
                var player = GetPlayer(playerId);
                
                var recommendation = TrainingFatigueIntegrationHelpers.GeneratePlayerRecommendation(playerId, status, loadState, fatigueData, player);
                if (recommendation != null)
                {
                    recommendations.Add(recommendation);
                }
            }
            
            // Sort by priority
            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }
        
        /// <summary>
        /// Simulate match fatigue impact on training capacity
        /// </summary>
        public void ApplyMatchFatigue(int playerId, int minutesPlayed, float matchIntensity, bool isVictory)
        {
            var player = GetPlayer(playerId);
            if (player == null) return;
            
            var loadState = GetPlayerLoadState(playerId);
            var fatigueData = GetFatigueTrackingData(playerId);
            
            // Calculate match load equivalent
            var matchLoad = CalculateMatchLoadEquivalent(minutesPlayed, matchIntensity);
            var conditionImpact = CalculateMatchConditionImpact(minutesPlayed, matchIntensity, player);
            var fatigueImpact = matchLoad * 1.5f; // Matches are more fatiguing than training
            
            // Apply psychological boost/penalty for result
            var psychologicalFactor = isVictory ? 0.9f : 1.1f; // Winners recover slightly faster
            fatigueImpact *= psychologicalFactor;
            
            // Apply the match effects
            loadState.AddMatchLoad(matchLoad, DateTime.Now);
            fatigueData.AccumulateMatchFatigue(fatigueImpact, conditionImpact);
            ApplyConditionChange(player, -conditionImpact);
            
            // Update post-match recovery recommendations
            var recoveryTime = CalculatePostMatchRecoveryTime(minutesPlayed, matchIntensity);
            fatigueData.SetMinimumRecoveryTime(recoveryTime);
            
            Debug.Log($"[TrainingFatigueIntegration] Applied match fatigue to {player.Name}: {matchLoad:F1} load, {conditionImpact:F1} condition impact");
            
            OnPlayerLoadStateChanged?.Invoke(playerId, loadState);
        }
        
        #region Private Methods
        
        private PlayerLoadCapacityCheck CanPlayerHandleLoad(int playerId, float additionalLoad, TrainingIntensity intensity)
        {
            var player = GetPlayer(playerId);
            var loadState = GetPlayerLoadState(playerId);
            var fatigueData = GetFatigueTrackingData(playerId);
            
            var check = new PlayerLoadCapacityCheck { PlayerId = playerId };
            
            // Check condition thresholds
            if (player.Stamina <= noTrainingThreshold)
            {
                check.CanTrain = false;
                check.Reason = $"Player condition too low ({player.Stamina}) for training";
                check.RecommendedAction = "Complete rest required";
                return check;
            }
            
            if (player.Stamina <= lightTrainingThreshold && intensity > TrainingIntensity.Light)
            {
                check.CanTrain = false;
                check.Reason = $"Player condition ({player.Stamina}) only suitable for light training";
                check.RecommendedAction = "Reduce training intensity to Light";
                return check;
            }
            
            // Check daily load limits
            var currentDailyLoad = loadState.GetDailyLoad();
            if (currentDailyLoad + additionalLoad > maxDailyTrainingLoad)
            {
                check.CanTrain = false;
                check.Reason = $"Daily training load limit exceeded ({currentDailyLoad + additionalLoad:F1}/{maxDailyTrainingLoad})";
                check.RecommendedAction = "Schedule training for tomorrow";
                return check;
            }
            
            // Check weekly load limits
            var currentWeeklyLoad = loadState.GetWeeklyLoad();
            if (currentWeeklyLoad + additionalLoad > maxWeeklyTrainingLoad)
            {
                check.CanTrain = false;
                check.Reason = $"Weekly training load limit exceeded ({currentWeeklyLoad + additionalLoad:F1}/{maxWeeklyTrainingLoad})";
                check.RecommendedAction = "Reduce training volume or intensity";
                return check;
            }
            
            // Check fatigue levels
            if (fatigueData.CurrentFatigueLevel > 80f)
            {
                check.CanTrain = false;
                check.Reason = $"Player fatigue level too high ({fatigueData.CurrentFatigueLevel:F1})";
                check.RecommendedAction = "Extended recovery period required";
                return check;
            }
            
            check.CanTrain = true;
            check.Reason = "Player ready for training";
            return check;
        }
        
        private float CalculateConditionImpact(float trainingLoad, TrainingIntensity intensity, TimeSpan duration, Player player)
        {
            // Base condition drain similar to fatigue model
            float baseImpact = trainingLoad * 0.8f; // Training is less intense than matches
            
            // Intensity multiplier
            float intensityMultiplier = intensity switch
            {
                TrainingIntensity.Light => 0.6f,
                TrainingIntensity.Moderate => 1.0f,
                TrainingIntensity.High => 1.4f,
                TrainingIntensity.VeryHigh => 1.8f,
                _ => 1.0f
            };
            
            // Player endurance reduces impact (similar to fatigue model)
            float endurance = player.Stats?.Stamina ?? 70f;
            float enduranceMultiplier = 1.15f - 0.5f * (endurance / 100f);
            
            // Duration factor
            float durationFactor = (float)duration.TotalHours / 2f; // 2 hours = baseline
            
            return baseImpact * intensityMultiplier * enduranceMultiplier * durationFactor;
        }
        
        private float CalculateFatigueImpact(float trainingLoad, TrainingIntensity intensity, Player player)
        {
            float baseFatigue = trainingLoad * 1.2f; // Training creates cumulative fatigue
            
            // Intensity affects fatigue accumulation
            float intensityMultiplier = intensity switch
            {
                TrainingIntensity.Light => 0.7f,
                TrainingIntensity.Moderate => 1.0f,
                TrainingIntensity.High => 1.3f,
                TrainingIntensity.VeryHigh => 1.6f,
                _ => 1.0f
            };
            
            // Player fitness affects fatigue accumulation
            float fitness = player.Stats?.Stamina ?? 70f;
            float fitnessMultiplier = (100f - fitness) / 100f + 0.5f;
            
            return baseFatigue * intensityMultiplier * fitnessMultiplier;
        }
        
        private float CalculateTrainingEffectiveness(Player player, PlayerLoadState loadState)
        {
            float baseEffectiveness = 1.0f;
            
            // Condition affects effectiveness (similar to fatigue model)
            float conditionMultiplier = 0.75f + 0.25f * (player.Stamina / 100f);
            
            // Accumulated load reduces effectiveness
            var dailyLoad = loadState.GetDailyLoad();
            float loadPenalty = dailyLoad > 30f ? (dailyLoad - 30f) / 50f : 0f; // Penalty starts after 30 load points
            float loadMultiplier = Math.Max(0.6f, 1f - loadPenalty);
            
            return baseEffectiveness * conditionMultiplier * loadMultiplier;
        }
        
        private float CalculateRecoveryAmount(RecoveryType recoveryType, TimeSpan duration, Player player)
        {
            float baseRecovery = fatigueRecoveryRate * (float)duration.TotalHours;
            
            float recoveryMultiplier = recoveryType switch
            {
                RecoveryType.PassiveRest => 1.0f,
                RecoveryType.ActiveRecovery => activeRecoveryMultiplier,
                RecoveryType.Sleep => 1.8f,
                RecoveryType.Massage => 1.3f,
                RecoveryType.IceBath => 1.5f,
                RecoveryType.Nutrition => nutritionRecoveryBonus,
                RecoveryType.Stretching => 1.1f,
                _ => 1.0f
            };
            
            // Player recovery rate (could be based on age, fitness, etc.)
            float playerRecoveryRate = CalculatePlayerRecoveryRate(player);
            
            return baseRecovery * recoveryMultiplier * playerRecoveryRate;
        }
        
        private float CalculateConditionRecovery(RecoveryType recoveryType, TimeSpan duration, Player player)
        {
            // Recovery improves condition back toward 100
            float maxRecovery = 100f - player.Stamina;
            float recoveryRate = recoveryType switch
            {
                RecoveryType.PassiveRest => 8f, // Points per hour
                RecoveryType.ActiveRecovery => 6f,
                RecoveryType.Sleep => 15f,
                RecoveryType.Massage => 10f,
                RecoveryType.IceBath => 8f,
                RecoveryType.Nutrition => 5f,
                RecoveryType.Stretching => 4f,
                _ => 6f
            };
            
            float potentialRecovery = recoveryRate * (float)duration.TotalHours;
            return Math.Min(maxRecovery, potentialRecovery);
        }
        
        private float CalculateLoadReduction(RecoveryType recoveryType, TimeSpan duration)
        {
            // Some recovery types reduce accumulated training load
            return recoveryType switch
            {
                RecoveryType.ActiveRecovery => 2f * (float)duration.TotalHours,
                RecoveryType.Sleep => 5f * (float)duration.TotalHours,
                RecoveryType.Massage => 3f * (float)duration.TotalHours,
                _ => 1f * (float)duration.TotalHours
            };
        }
        
        private void ApplyConditionChange(Player player, float conditionChange)
        {
            float newCondition = Mathf.Clamp(player.Stamina + conditionChange, 0f, 100f);
            player.Stamina = newCondition;
        }
        
        private float CalculatePlayerRecoveryRate(Player player)
        {
            // Younger players recover faster
            float ageMultiplier = player.Age <= 22 ? 1.2f : 
                                 player.Age <= 28 ? 1.0f : 
                                 player.Age <= 32 ? 0.9f : 0.8f;
            
            // Fitter players recover better
            float fitnessMultiplier = (player.Stats?.Stamina ?? 70f) / 100f + 0.5f;
            
            return ageMultiplier * fitnessMultiplier;
        }
        
        private float CalculateMatchLoadEquivalent(int minutesPlayed, float matchIntensity)
        {
            // Convert match time to training load equivalent
            float baseLoad = minutesPlayed / 90f * 45f; // 90min match = ~45 training load
            return baseLoad * matchIntensity;
        }
        
        private float CalculateMatchConditionImpact(int minutesPlayed, float matchIntensity, Player player)
        {
            // Matches drain more condition than training
            float baseImpact = minutesPlayed / 90f * 25f; // 90min match = ~25 condition points
            
            float endurance = player.Stats?.Stamina ?? 70f;
            float enduranceMultiplier = 1.15f - 0.5f * (endurance / 100f);
            
            return baseImpact * matchIntensity * enduranceMultiplier;
        }
        
        private TimeSpan CalculatePostMatchRecoveryTime(int minutesPlayed, float matchIntensity)
        {
            // Base recovery time after matches
            double baseHours = minutesPlayed / 90.0 * 24.0; // 90min match = 24 hours base recovery
            return TimeSpan.FromHours(baseHours * matchIntensity);
        }
        
        private void ProcessContinuousRecovery()
        {
            var currentTime = DateTime.Now;
            var timeDelta = currentTime - lastUpdateTime;
            
            if (timeDelta.TotalMinutes < 10) return; // Update every 10 minutes
            
            foreach (var kvp in fatigueTrackingData)
            {
                var playerId = kvp.Key;
                var fatigueData = kvp.Value;
                var player = GetPlayer(playerId);
                
                if (player != null)
                {
                    // Passive recovery
                    var recoveryAmount = fatigueRecoveryRate * (float)timeDelta.TotalHours * 0.3f; // Passive is slower
                    fatigueData.ApplyPassiveRecovery(recoveryAmount);
                    
                    // Condition recovery (when resting)
                    if (player.Stamina < 100)
                    {
                        var conditionRecovery = 6f * (float)timeDelta.TotalHours; // 6 points per hour passive
                        ApplyConditionChange(player, conditionRecovery);
                    }
                }
            }
            
            lastUpdateTime = currentTime;
        }
        
        private void CheckFatigueAlerts()
        {
            foreach (var kvp in fatigueTrackingData)
            {
                var playerId = kvp.Key;
                var fatigueData = kvp.Value;
                
                if (fatigueData.CurrentFatigueLevel > 85f && !fatigueData.HighFatigueAlertSent)
                {
                    var alert = new FatigueAlert
                    {
                        PlayerId = playerId,
                        AlertType = FatigueAlertType.HighFatigue,
                        Message = $"Player {playerId} has high fatigue level ({fatigueData.CurrentFatigueLevel:F1})",
                        Severity = AlertSeverity.High,
                        RecommendedAction = "Immediate rest required"
                    };
                    
                    OnFatigueAlert?.Invoke(playerId, alert);
                    fatigueData.HighFatigueAlertSent = true;
                }
            }
        }
        
        private void DecayTrainingLoads()
        {
            // Apply daily decay to training loads
            var currentDate = DateTime.Now.Date;
            
            foreach (var loadState in playerLoadStates.Values)
            {
                loadState.ApplyDailyDecay(currentDate, trainingLoadDecayRate);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private Player GetPlayer(int playerId)
        {
            // This would typically integrate with your player management system
            // For now, return null - would be populated by external systems
            return null;
        }
        
        private PlayerLoadState GetPlayerLoadState(int playerId)
        {
            if (!playerLoadStates.ContainsKey(playerId))
            {
                playerLoadStates[playerId] = new PlayerLoadState(playerId);
            }
            return playerLoadStates[playerId];
        }
        
        private FatigueTrackingData GetFatigueTrackingData(int playerId)
        {
            if (!fatigueTrackingData.ContainsKey(playerId))
            {
                fatigueTrackingData[playerId] = new FatigueTrackingData(playerId);
            }
            return fatigueTrackingData[playerId];
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnPlayerLoadExceeded(int playerId, float currentLoad)
        {
            var recommendation = new LoadManagementRecommendation
            {
                PlayerId = playerId,
                Type = LoadManagementType.LoadReduction,
                Priority = RecommendationPriority.High,
                Message = $"Player {playerId} exceeded safe training load",
                RecommendedAction = "Reduce training load immediately",
                EstimatedBenefit = "Prevent overtraining and injury risk"
            };
            
            OnLoadManagementRecommendation?.Invoke(recommendation);
        }
        
        private void OnPlayerSessionResult(int playerId, PlayerSessionResult result)
        {
            // Apply training load based on session results
            var totalLoad = result.TotalLoadContribution;
            var averageIntensity = EstimateIntensityFromEffectiveness(result.AverageEffectiveness);
            
            ApplyTrainingLoad(playerId, totalLoad, averageIntensity, result.SessionDuration);
        }
        
        private void OnTrainingInjury(int playerId, AFLCoachSim.Core.Injuries.Domain.Injury injury)
        {
            // Increase recovery time due to injury
            var fatigueData = GetFatigueTrackingData(playerId);
            var injuryRecoveryTime = CalculateInjuryRecoveryTime(injury.Severity);
            
            fatigueData.SetMinimumRecoveryTime(injuryRecoveryTime);
            
            Debug.Log($"[TrainingFatigueIntegration] Applied injury recovery time to player {playerId}: {injuryRecoveryTime}");
        }
        
        private TrainingIntensity EstimateIntensityFromEffectiveness(float effectiveness)
        {
            return effectiveness switch
            {
                >= 1.3f => TrainingIntensity.VeryHigh,
                >= 1.1f => TrainingIntensity.High,
                >= 0.9f => TrainingIntensity.Moderate,
                _ => TrainingIntensity.Light
            };
        }
        
        private TimeSpan CalculateInjuryRecoveryTime(AFLCoachSim.Core.Injuries.Domain.InjurySeverity severity)
        {
            return severity switch
            {
                AFLCoachSim.Core.Injuries.Domain.InjurySeverity.Niggle => TimeSpan.FromHours(12),
                AFLCoachSim.Core.Injuries.Domain.InjurySeverity.Minor => TimeSpan.FromDays(1),
                AFLCoachSim.Core.Injuries.Domain.InjurySeverity.Moderate => TimeSpan.FromDays(3),
                AFLCoachSim.Core.Injuries.Domain.InjurySeverity.Major => TimeSpan.FromDays(5),
                AFLCoachSim.Core.Injuries.Domain.InjurySeverity.Severe => TimeSpan.FromDays(7),
                _ => TimeSpan.FromHours(6)
            };
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (scheduleManager != null)
            {
                scheduleManager.OnPlayerLoadExceeded -= OnPlayerLoadExceeded;
            }
            
            if (sessionExecutor != null)
            {
                sessionExecutor.OnPlayerSessionResult -= OnPlayerSessionResult;
                sessionExecutor.OnTrainingInjuryOccurred -= OnTrainingInjury;
            }
        }
    }
}