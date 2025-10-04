using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Weather;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Fatigue
{
    /// <summary>
    /// Advanced fatigue modeling system with position-specific rates, recovery patterns, and performance impacts
    /// </summary>
    public class AdvancedFatigueSystem
    {
        private readonly Dictionary<Guid, PlayerFatigueState> _playerFatigueStates;
        private readonly Dictionary<Role, PositionFatigueProfile> _positionProfiles;
        private readonly FatigueConfiguration _configuration;
        private readonly Random _random;

        public AdvancedFatigueSystem(int seed = 0)
        {
            _random = seed == 0 ? new Random() : new Random(seed);
            _playerFatigueStates = new Dictionary<Guid, PlayerFatigueState>();
            _positionProfiles = InitializePositionFatigueProfiles();
            _configuration = new FatigueConfiguration();
        }

        #region Player Fatigue Management

        /// <summary>
        /// Initialize fatigue state for a player
        /// </summary>
        public void InitializePlayer(Player player, float initialFitness = 100f)
        {
            var fatigueState = new PlayerFatigueState(player.Id)
            {
                CurrentFitness = initialFitness,
                MaxFitness = initialFitness,
                BaseEndurance = CalculateBaseEndurance(player),
                Position = player.PrimaryRole,
                Age = GetPlayerAge(player),
                FitnessLevel = initialFitness
            };

            _playerFatigueStates[player.Id] = fatigueState;
            CoreLogger.Log($"[Fatigue] Initialized {player.Name} ({player.PrimaryRole}) - Endurance: {fatigueState.BaseEndurance:F0}");
        }

        /// <summary>
        /// Update player fatigue based on match activity
        /// </summary>
        public void UpdatePlayerFatigue(Guid playerId, FatigueActivity activity, float duration, 
            WeatherConditions? weatherConditions = null, float intensityMultiplier = 1.0f)
        {
            if (!_playerFatigueStates.TryGetValue(playerId, out var fatigueState))
            {
                CoreLogger.LogWarning($"[Fatigue] Player {playerId} not initialized in fatigue system");
                return;
            }

            var profile = _positionProfiles[fatigueState.Position];
            float fatigueRate = CalculateFatigueRate(fatigueState, profile, activity, weatherConditions, intensityMultiplier);
            float fatigueDelta = fatigueRate * duration;

            // Apply fatigue
            fatigueState.CurrentFatigue = Math.Min(100f, fatigueState.CurrentFatigue + fatigueDelta);
            fatigueState.TotalFatigueAccumulated += fatigueDelta;

            // Track activity
            fatigueState.ActivityHistory.Add(new FatigueActivityRecord
            {
                Activity = activity,
                Duration = duration,
                FatigueCost = fatigueDelta,
                Timestamp = DateTime.Now
            });

            // Update fatigue zones
            UpdateFatigueZone(fatigueState);

            // Check for fatigue-related risks
            CheckFatigueRisks(fatigueState);

            if (fatigueDelta > 1.0f) // Only log significant fatigue increases
            {
                CoreLogger.Log($"[Fatigue] Player {playerId} - {activity}: +{fatigueDelta:F1} fatigue " +
                             $"(Total: {fatigueState.CurrentFatigue:F1}%, Zone: {fatigueState.CurrentZone})");
            }
        }

        /// <summary>
        /// Apply recovery to player fatigue
        /// </summary>
        public void ApplyRecovery(Guid playerId, RecoveryType recoveryType, float duration)
        {
            if (!_playerFatigueStates.TryGetValue(playerId, out var fatigueState))
                return;

            var profile = _positionProfiles[fatigueState.Position];
            float recoveryRate = CalculateRecoveryRate(fatigueState, profile, recoveryType);
            float recoveryAmount = recoveryRate * duration;

            fatigueState.CurrentFatigue = Math.Max(0f, fatigueState.CurrentFatigue - recoveryAmount);
            fatigueState.TotalRecovery += recoveryAmount;

            UpdateFatigueZone(fatigueState);

            if (recoveryAmount > 0.5f)
            {
                CoreLogger.Log($"[Fatigue] Player {playerId} - {recoveryType}: -{recoveryAmount:F1} fatigue " +
                             $"(Remaining: {fatigueState.CurrentFatigue:F1}%)");
            }
        }

        /// <summary>
        /// Get current fatigue state for a player
        /// </summary>
        public PlayerFatigueState GetPlayerFatigueState(Guid playerId)
        {
            return _playerFatigueStates.GetValueOrDefault(playerId);
        }

        #endregion

        #region Performance Impact Calculations

        /// <summary>
        /// Calculate performance modifiers based on current fatigue state
        /// </summary>
        public FatiguePerformanceImpact CalculatePerformanceImpact(Guid playerId)
        {
            var fatigueState = GetPlayerFatigueState(playerId);
            if (fatigueState == null)
                return new FatiguePerformanceImpact();

            var profile = _positionProfiles[fatigueState.Position];
            var impact = new FatiguePerformanceImpact();

            // Base fatigue impact
            float fatigueRatio = fatigueState.CurrentFatigue / 100f;
            
            // Apply zone-specific impacts
            switch (fatigueState.CurrentZone)
            {
                case FatigueZone.Fresh:
                    impact.OverallPerformanceModifier = 0.05f; // Slight bonus when fresh
                    break;
                    
                case FatigueZone.Light:
                    impact.OverallPerformanceModifier = 0f; // No significant impact
                    break;
                    
                case FatigueZone.Moderate:
                    impact.OverallPerformanceModifier = -0.05f - (fatigueRatio * 0.10f);
                    impact.SpeedReduction = fatigueRatio * 0.08f;
                    impact.AccuracyReduction = fatigueRatio * 0.06f;
                    break;
                    
                case FatigueZone.Heavy:
                    impact.OverallPerformanceModifier = -0.15f - (fatigueRatio * 0.15f);
                    impact.SpeedReduction = fatigueRatio * 0.15f;
                    impact.AccuracyReduction = fatigueRatio * 0.12f;
                    impact.EnduranceReduction = fatigueRatio * 0.20f;
                    impact.DecisionMakingImpact = fatigueRatio * 0.10f;
                    break;
                    
                case FatigueZone.Exhausted:
                    impact.OverallPerformanceModifier = -0.30f - (fatigueRatio * 0.20f);
                    impact.SpeedReduction = fatigueRatio * 0.25f;
                    impact.AccuracyReduction = fatigueRatio * 0.20f;
                    impact.EnduranceReduction = fatigueRatio * 0.35f;
                    impact.DecisionMakingImpact = fatigueRatio * 0.18f;
                    impact.InjuryRiskMultiplier = 1.5f + (fatigueRatio * 0.5f);
                    break;
            }

            // Position-specific fatigue impacts
            ApplyPositionSpecificImpacts(impact, profile, fatigueRatio);

            // Age-related fatigue impacts
            if (fatigueState.Age > 30)
            {
                float ageFactor = (fatigueState.Age - 30) / 10f; // 0.0 to ~0.5
                impact.OverallPerformanceModifier -= ageFactor * 0.05f;
                impact.RecoveryRateReduction = ageFactor * 0.15f;
            }

            return impact;
        }

        /// <summary>
        /// Calculate substitution urgency based on fatigue
        /// </summary>
        public SubstitutionUrgency CalculateSubstitutionUrgency(Guid playerId)
        {
            var fatigueState = GetPlayerFatigueState(playerId);
            if (fatigueState == null)
                return SubstitutionUrgency.None;

            return fatigueState.CurrentZone switch
            {
                FatigueZone.Fresh or FatigueZone.Light => SubstitutionUrgency.None,
                FatigueZone.Moderate => SubstitutionUrgency.Consider,
                FatigueZone.Heavy => SubstitutionUrgency.Recommended,
                FatigueZone.Exhausted => SubstitutionUrgency.Urgent,
                _ => SubstitutionUrgency.None
            };
        }

        #endregion

        #region Team Fatigue Analysis

        /// <summary>
        /// Calculate team-wide fatigue statistics
        /// </summary>
        public TeamFatigueAnalysis CalculateTeamFatigue(IEnumerable<Guid> playerIds)
        {
            var analysis = new TeamFatigueAnalysis();
            var fatigueStates = playerIds.Select(id => GetPlayerFatigueState(id))
                                        .Where(state => state != null)
                                        .ToList();

            if (!fatigueStates.Any())
                return analysis;

            analysis.AverageFatigue = fatigueStates.Average(s => s.CurrentFatigue);
            analysis.MaxFatigue = fatigueStates.Max(s => s.CurrentFatigue);
            analysis.MinFatigue = fatigueStates.Min(s => s.CurrentFatigue);
            
            // Zone distribution
            analysis.ZoneDistribution = fatigueStates.GroupBy(s => s.CurrentZone)
                                                    .ToDictionary(g => g.Key, g => g.Count());

            // Players needing substitution
            analysis.PlayersNeedingSubstitution = fatigueStates.Where(s => s.CurrentZone >= FatigueZone.Heavy)
                                                              .Select(s => s.PlayerId)
                                                              .ToList();

            // Team performance impact
            var impacts = fatigueStates.Select(s => CalculatePerformanceImpact(s.PlayerId));
            analysis.TeamPerformanceImpact = impacts.Average(i => i.OverallPerformanceModifier);
            analysis.TeamSpeedImpact = impacts.Average(i => i.SpeedReduction);
            analysis.TeamAccuracyImpact = impacts.Average(i => i.AccuracyReduction);

            return analysis;
        }

        /// <summary>
        /// Get fatigue-based tactical recommendations
        /// </summary>
        public List<FatigueTacticalRecommendation> GetTacticalRecommendations(IEnumerable<Guid> playerIds)
        {
            var recommendations = new List<FatigueTacticalRecommendation>();
            var teamAnalysis = CalculateTeamFatigue(playerIds);

            // High team fatigue
            if (teamAnalysis.AverageFatigue > 60f)
            {
                recommendations.Add(new FatigueTacticalRecommendation
                {
                    Type = RecommendationType.TacticChange,
                    Priority = RecommendationPriority.High,
                    Description = "Team showing high fatigue - consider more conservative play style",
                    Suggestion = "Reduce game intensity, focus on ball retention"
                });
            }

            // Multiple players need substitution
            if (teamAnalysis.PlayersNeedingSubstitution.Count >= 3)
            {
                recommendations.Add(new FatigueTacticalRecommendation
                {
                    Type = RecommendationType.MultipleSubstitutions,
                    Priority = RecommendationPriority.Urgent,
                    Description = "Multiple players in heavy fatigue zones",
                    Suggestion = "Plan strategic substitutions to maintain performance"
                });
            }

            // Position-specific recommendations
            var positionFatigue = AnalyzePositionFatigue(playerIds);
            foreach (var kvp in positionFatigue)
            {
                if (kvp.Value.AverageFatigue > 70f)
                {
                    recommendations.Add(new FatigueTacticalRecommendation
                    {
                        Type = RecommendationType.PositionRotation,
                        Priority = RecommendationPriority.Medium,
                        Description = $"{kvp.Key} players showing high fatigue",
                        Suggestion = $"Consider rotating {kvp.Key} players or reducing their workload"
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }

        #endregion

        #region Private Helper Methods

        private Dictionary<Role, PositionFatigueProfile> InitializePositionFatigueProfiles()
        {
            return new Dictionary<Role, PositionFatigueProfile>
            {
                [Role.Midfielder] = new PositionFatigueProfile
                {
                    BaseFatigueRate = 1.8f, // High - lots of running
                    RunningFatigueMultiplier = 1.4f,
                    ContestFatigueMultiplier = 1.2f,
                    BaseRecoveryRate = 1.0f,
                    PrimaryAttributes = new[] { "Speed", "Endurance", "DecisionMaking" },
                    FatigueResistance = 0.8f
                },
                
                [Role.Ruck] = new PositionFatigueProfile
                {
                    BaseFatigueRate = 2.2f, // Highest - intense physical contests
                    RunningFatigueMultiplier = 1.1f,
                    ContestFatigueMultiplier = 1.6f,
                    BaseRecoveryRate = 0.8f,
                    PrimaryAttributes = new[] { "Strength", "Endurance", "Marking" },
                    FatigueResistance = 0.6f
                },
                
                [Role.KeyForward] = new PositionFatigueProfile
                {
                    BaseFatigueRate = 1.5f,
                    RunningFatigueMultiplier = 1.1f,
                    ContestFatigueMultiplier = 1.3f,
                    BaseRecoveryRate = 1.1f,
                    PrimaryAttributes = new[] { "Strength", "Marking", "Accuracy" },
                    FatigueResistance = 0.9f
                },
                
                [Role.KeyDefender] = new PositionFatigueProfile
                {
                    BaseFatigueRate = 1.4f,
                    RunningFatigueMultiplier = 1.0f,
                    ContestFatigueMultiplier = 1.2f,
                    BaseRecoveryRate = 1.2f,
                    PrimaryAttributes = new[] { "Strength", "Marking", "DecisionMaking" },
                    FatigueResistance = 1.0f
                },
                
                [Role.SmallForward] = new PositionFatigueProfile
                {
                    BaseFatigueRate = 1.6f,
                    RunningFatigueMultiplier = 1.3f,
                    ContestFatigueMultiplier = 1.1f,
                    BaseRecoveryRate = 1.3f,
                    PrimaryAttributes = new[] { "Speed", "Agility", "Pressure" },
                    FatigueResistance = 1.1f
                },
                
                [Role.SmallDefender] = new PositionFatigueProfile
                {
                    BaseFatigueRate = 1.5f,
                    RunningFatigueMultiplier = 1.2f,
                    ContestFatigueMultiplier = 1.0f,
                    BaseRecoveryRate = 1.2f,
                    PrimaryAttributes = new[] { "Speed", "Agility", "DecisionMaking" },
                    FatigueResistance = 1.0f
                }
            };

            // Add default profile for any missing roles
            var defaultProfile = new PositionFatigueProfile
            {
                BaseFatigueRate = 1.5f,
                RunningFatigueMultiplier = 1.2f,
                ContestFatigueMultiplier = 1.1f,
                BaseRecoveryRate = 1.0f,
                PrimaryAttributes = new[] { "Endurance" },
                FatigueResistance = 1.0f
            };

            foreach (var role in Enum.GetValues<Role>())
            {
                if (!_positionProfiles.ContainsKey(role))
                {
                    _positionProfiles[role] = defaultProfile;
                }
            }

            return _positionProfiles;
        }

        private float CalculateFatigueRate(PlayerFatigueState fatigueState, PositionFatigueProfile profile,
            FatigueActivity activity, WeatherConditions? weatherConditions, float intensityMultiplier)
        {
            float baseRate = profile.BaseFatigueRate;
            
            // Activity-specific multipliers
            float activityMultiplier = activity switch
            {
                FatigueActivity.Running => profile.RunningFatigueMultiplier,
                FatigueActivity.Contest => profile.ContestFatigueMultiplier,
                FatigueActivity.Tackling => profile.ContestFatigueMultiplier * 1.1f,
                FatigueActivity.Marking => profile.ContestFatigueMultiplier * 0.9f,
                FatigueActivity.Kicking => 0.3f,
                FatigueActivity.Walking => 0.1f,
                FatigueActivity.Sprinting => profile.RunningFatigueMultiplier * 1.8f,
                _ => 1.0f
            };

            // Fitness level impact
            float fitnessMultiplier = 1.0f + ((100f - fatigueState.FitnessLevel) / 200f);
            
            // Current fatigue impact (more tired = faster fatigue accumulation)
            float currentFatigueMultiplier = 1.0f + (fatigueState.CurrentFatigue / 200f);
            
            // Age impact
            float ageMultiplier = fatigueState.Age > 25 ? 1.0f + ((fatigueState.Age - 25) * 0.02f) : 1.0f;
            
            // Weather impact
            float weatherMultiplier = 1.0f;
            if (weatherConditions != null)
            {
                weatherMultiplier = weatherConditions.WeatherType switch
                {
                    Weather.Hot => 1.3f + (weatherConditions.Intensity * 0.2f),
                    Weather.Wet => 1.1f + (weatherConditions.Intensity * 0.1f),
                    Weather.Cold => 0.95f,
                    _ => 1.0f
                };
            }

            return baseRate * activityMultiplier * fitnessMultiplier * currentFatigueMultiplier * 
                   ageMultiplier * weatherMultiplier * intensityMultiplier * profile.FatigueResistance;
        }

        private float CalculateRecoveryRate(PlayerFatigueState fatigueState, PositionFatigueProfile profile,
            RecoveryType recoveryType)
        {
            float baseRate = profile.BaseRecoveryRate * _configuration.BaseRecoveryRate;
            
            // Recovery type multipliers
            float recoveryMultiplier = recoveryType switch
            {
                RecoveryType.ActiveRecovery => 1.5f,
                RecoveryType.PassiveRest => 1.0f,
                RecoveryType.QuarterBreak => 3.0f,
                RecoveryType.HalfTimeBreak => 8.0f,
                RecoveryType.Substitution => 0f, // No recovery while on field
                _ => 1.0f
            };

            // Fitness level impact on recovery
            float fitnessMultiplier = 0.8f + (fatigueState.FitnessLevel / 200f);
            
            // Age impact on recovery
            float ageMultiplier = fatigueState.Age > 25 ? 1.0f - ((fatigueState.Age - 25) * 0.015f) : 1.0f;
            
            // Current fatigue impact (harder to recover when very tired)
            float fatigueMultiplier = Math.Max(0.5f, 1.0f - (fatigueState.CurrentFatigue / 150f));

            return baseRate * recoveryMultiplier * fitnessMultiplier * ageMultiplier * fatigueMultiplier;
        }

        private void UpdateFatigueZone(PlayerFatigueState fatigueState)
        {
            var previousZone = fatigueState.CurrentZone;
            
            fatigueState.CurrentZone = fatigueState.CurrentFatigue switch
            {
                < 20f => FatigueZone.Fresh,
                < 40f => FatigueZone.Light,
                < 65f => FatigueZone.Moderate,
                < 85f => FatigueZone.Heavy,
                _ => FatigueZone.Exhausted
            };

            if (previousZone != fatigueState.CurrentZone)
            {
                fatigueState.ZoneTransitions.Add(new FatigueZoneTransition
                {
                    FromZone = previousZone,
                    ToZone = fatigueState.CurrentZone,
                    Timestamp = DateTime.Now,
                    FatigueLevel = fatigueState.CurrentFatigue
                });
            }
        }

        private void CheckFatigueRisks(PlayerFatigueState fatigueState)
        {
            if (fatigueState.CurrentZone >= FatigueZone.Heavy)
            {
                // Increased injury risk
                if (_random.NextDouble() < _configuration.InjuryRiskThreshold)
                {
                    CoreLogger.LogWarning($"[Fatigue] Player {fatigueState.PlayerId} at high injury risk due to fatigue");
                }
                
                // Performance warnings
                if (fatigueState.CurrentZone == FatigueZone.Exhausted)
                {
                    CoreLogger.LogWarning($"[Fatigue] Player {fatigueState.PlayerId} is exhausted - urgent substitution recommended");
                }
            }
        }

        private void ApplyPositionSpecificImpacts(FatiguePerformanceImpact impact, 
            PositionFatigueProfile profile, float fatigueRatio)
        {
            // Position-specific performance degradation patterns
            foreach (var attribute in profile.PrimaryAttributes)
            {
                float attributeImpact = fatigueRatio * 0.15f;
                
                switch (attribute)
                {
                    case "Speed":
                        impact.SpeedReduction += attributeImpact;
                        break;
                    case "Accuracy":
                        impact.AccuracyReduction += attributeImpact;
                        break;
                    case "DecisionMaking":
                        impact.DecisionMakingImpact += attributeImpact;
                        break;
                    case "Endurance":
                        impact.EnduranceReduction += attributeImpact;
                        break;
                }
            }
        }

        private float CalculateBaseEndurance(Player player)
        {
            // Base endurance calculation - would use actual player attributes in real implementation
            return 70f + _random.Next(-15, 16); // 55-85 range
        }

        private int GetPlayerAge(Player player)
        {
            // Would use actual player age in real implementation
            return 25 + _random.Next(-5, 11); // 20-35 range
        }

        private Dictionary<Role, PositionFatigueData> AnalyzePositionFatigue(IEnumerable<Guid> playerIds)
        {
            return _playerFatigueStates.Values
                .Where(state => playerIds.Contains(state.PlayerId))
                .GroupBy(state => state.Position)
                .ToDictionary(
                    g => g.Key,
                    g => new PositionFatigueData
                    {
                        AverageFatigue = g.Average(s => s.CurrentFatigue),
                        MaxFatigue = g.Max(s => s.CurrentFatigue),
                        PlayerCount = g.Count()
                    });
        }

        #endregion
    }
}