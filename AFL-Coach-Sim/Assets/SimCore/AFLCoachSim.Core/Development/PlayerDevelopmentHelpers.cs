using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Training;

namespace AFLCoachSim.Core.Development
{
    /// <summary>
    /// Helper methods and utilities for the Player Development Framework
    /// </summary>
    public static class PlayerDevelopmentHelpers
    {
        private static readonly Dictionary<string, PlayerSpecialization> _allSpecializations = 
            PlayerSpecializationFactory.CreateAllSpecializations();

        #region Specialization Helpers

        /// <summary>
        /// Determine initial specialization for a new player based on position and attributes
        /// </summary>
        public static PlayerSpecialization DetermineInitialSpecialization(Player player)
        {
            var positionSpecializations = GetSpecializationsForPosition(player.Position.ToString());
            
            // Start with Tier 1 (General) specializations
            var tier1Specializations = positionSpecializations.Where(s => s.TierLevel == 1).ToList();
            
            if (tier1Specializations.Any())
            {
                return tier1Specializations.First(); // Default to first applicable general specialization
            }
            
            // Fallback - shouldn't happen with proper data
            return _allSpecializations.Values.First(s => s.TierLevel == 1);
        }

        /// <summary>
        /// Get next tier specialization for advancement
        /// </summary>
        public static PlayerSpecialization GetNextSpecializationTier(PlayerSpecialization current, Player player)
        {
            if (!current.CanAdvance) return null;

            var positionSpecializations = GetSpecializationsForPosition(player.Position.ToString());
            var nextTierCandidates = positionSpecializations
                .Where(s => s.TierLevel == current.TierLevel + 1)
                .Where(s => s.PrerequisiteSpecializations.Contains(current.Id))
                .ToList();

            if (!nextTierCandidates.Any()) return null;

            // For now, return first valid candidate
            // Could add logic here to pick best fit based on attributes
            return nextTierCandidates.First();
        }

        /// <summary>
        /// Get all specializations available for a position
        /// </summary>
        public static List<PlayerSpecialization> GetSpecializationsForPosition(string position)
        {
            return _allSpecializations.Values
                .Where(s => s.RequiredPositions.Contains(position) || s.RequiredPositions.Count == 0)
                .ToList();
        }

        #endregion

        #region Experience System Helpers

        /// <summary>
        /// Get attributes that benefit from match experience based on position and specialization
        /// </summary>
        public static Dictionary<string, float> GetExperienceAttributes(object position, PlayerSpecialization specialization)
        {
            var baseAttributes = GetPositionExperienceAttributes(position.ToString());
            
            if (specialization != null)
            {
                // Blend position-based and specialization-based experience gains
                foreach (var specAttr in specialization.AttributeWeights)
                {
                    if (IsExperienceAttribute(specAttr.Key))
                    {
                        baseAttributes[specAttr.Key] = baseAttributes.GetValueOrDefault(specAttr.Key, 0.5f) * 1.2f;
                    }
                }
            }
            
            return baseAttributes;
        }

        /// <summary>
        /// Get base experience attributes for a position
        /// </summary>
        private static Dictionary<string, float> GetPositionExperienceAttributes(string position)
        {
            var experienceAttributes = new Dictionary<string, float>();
            
            switch (position)
            {
                case "FullBack":
                case "HalfBack":
                case "BackPocket":
                case "CentreHalfBack":
                    experienceAttributes["DecisionMaking"] = 1.2f;
                    experienceAttributes["Positioning"] = 1.3f;
                    experienceAttributes["Composure"] = 1.1f;
                    experienceAttributes["Leadership"] = 0.8f;
                    break;
                    
                case "Centre":
                case "Wing":
                case "Rover":
                case "RuckRover":
                    experienceAttributes["DecisionMaking"] = 1.4f;
                    experienceAttributes["GameReading"] = 1.3f;
                    experienceAttributes["Pressure"] = 1.1f;
                    experienceAttributes["Leadership"] = 1.0f;
                    break;
                    
                case "FullForward":
                case "HalfForward":
                case "ForwardPocket":
                case "CentreHalfForward":
                    experienceAttributes["DecisionMaking"] = 1.1f;
                    experienceAttributes["Positioning"] = 1.2f;
                    experienceAttributes["Composure"] = 1.3f;
                    experienceAttributes["Pressure"] = 0.9f;
                    break;
                    
                case "Ruckman":
                    experienceAttributes["DecisionMaking"] = 1.0f;
                    experienceAttributes["GameReading"] = 1.4f;
                    experienceAttributes["Leadership"] = 1.2f;
                    experienceAttributes["Positioning"] = 1.1f;
                    break;
                    
                default:
                    // Default experience gains for unknown positions
                    experienceAttributes["DecisionMaking"] = 1.0f;
                    experienceAttributes["GameReading"] = 1.0f;
                    break;
            }
            
            return experienceAttributes;
        }

        /// <summary>
        /// Check if an attribute benefits from experience (mental/tactical attributes primarily)
        /// </summary>
        private static bool IsExperienceAttribute(string attribute)
        {
            var experienceAttributes = new HashSet<string>
            {
                "DecisionMaking", "GameReading", "Positioning", "Composure", 
                "Leadership", "Pressure", "Tactical", "Communication"
            };
            
            return experienceAttributes.Contains(attribute);
        }

        /// <summary>
        /// Check for experience milestones and apply bonuses
        /// </summary>
        public static void CheckExperienceMilestones(PlayerDevelopmentProfile profile, Player player)
        {
            var experience = profile.CareerExperience;
            
            // Major milestones that provide development bonuses
            var milestones = new Dictionary<float, string>
            {
                { 50f, "Rookie Development Boost" },
                { 100f, "Establishing Player Boost" },
                { 200f, "Experienced Player Boost" },
                { 400f, "Veteran Leadership Boost" },
                { 600f, "Elite Career Milestone" }
            };
            
            foreach (var milestone in milestones)
            {
                if (experience >= milestone.Key && !profile.DevelopmentModifiers.ContainsKey(milestone.Value))
                {
                    // Add temporary development boost for reaching milestone
                    profile.DevelopmentModifiers[milestone.Value] = 1.15f; // 15% boost for next few weeks
                    // This would trigger some kind of notification system
                }
            }
        }

        #endregion

        #region Breakthrough Event System

        /// <summary>
        /// Calculate probability of a breakthrough event occurring
        /// </summary>
        public static float CalculateBreakthroughProbability(PlayerDevelopmentProfile profile, Player player, 
            PlayerDevelopmentFramework.PlayerDevelopmentUpdate update)
        {
            float baseProbability = 0.02f; // 2% base chance per development period
            
            // Age factors
            int age = CalculateAge(player.DateOfBirth);
            float ageFactor = age switch
            {
                < 21 => 1.5f,  // Young players more likely to have breakthroughs
                < 25 => 1.2f,  // Developing players
                < 29 => 1.0f,  // Prime years
                < 33 => 0.8f,  // Veteran years
                _ => 0.6f      // Late career
            };
            
            // Recent development factors
            float developmentFactor = 1.0f;
            float totalGains = CalculateTotalGain(update);
            if (totalGains > 2.0f) developmentFactor = 1.3f; // High development increases chance
            if (totalGains < 0.5f) developmentFactor = 0.7f; // Low development decreases chance
            
            // Breakthrough readiness
            float readinessFactor = 1.0f + (profile.BreakthroughReadiness / 100f);
            
            // Previous breakthrough timing (cooldown effect)
            // Could add logic here to prevent too frequent breakthroughs
            
            return Math.Min(0.15f, baseProbability * ageFactor * developmentFactor * readinessFactor);
        }

        /// <summary>
        /// Determine type of breakthrough event based on player characteristics
        /// </summary>
        public static BreakthroughEventType DetermineBreakthroughType(PlayerDevelopmentProfile profile, Player player)
        {
            int age = CalculateAge(player.DateOfBirth);
            var stage = GetDevelopmentStage(age);
            
            // Age-based breakthrough types
            var possibleTypes = new List<BreakthroughEventType>();
            
            switch (stage)
            {
                case DevelopmentStage.Rookie:
                case DevelopmentStage.Developing:
                    possibleTypes.AddRange(new[]
                    {
                        BreakthroughEventType.PhenomenalRising,
                        BreakthroughEventType.PhysicalBreakthrough,
                        BreakthroughEventType.MentalBreakthrough,
                        BreakthroughEventType.PositionMastery,
                        BreakthroughEventType.ConfidenceCrisis,
                        BreakthroughEventType.PressureCollapse
                    });
                    break;
                    
                case DevelopmentStage.Prime:
                    possibleTypes.AddRange(new[]
                    {
                        BreakthroughEventType.LeadershipBloom,
                        BreakthroughEventType.InspirationalForm,
                        BreakthroughEventType.VeteranSurge,
                        BreakthroughEventType.ComplacencyEffect,
                        BreakthroughEventType.MotivationLoss
                    });
                    break;
                    
                case DevelopmentStage.Veteran:
                case DevelopmentStage.Declining:
                    possibleTypes.AddRange(new[]
                    {
                        BreakthroughEventType.VeteranSurge,
                        BreakthroughEventType.LeadershipBloom,
                        BreakthroughEventType.AgeReality,
                        BreakthroughEventType.InjurySetback,
                        BreakthroughEventType.RecoveryPhase
                    });
                    break;
            }
            
            // Random selection from possible types
            var random = new Random();
            return possibleTypes[random.Next(possibleTypes.Count)];
        }

        /// <summary>
        /// Get description for breakthrough event
        /// </summary>
        public static string GetBreakthroughDescription(BreakthroughEventType type, Player player)
        {
            return type switch
            {
                BreakthroughEventType.PhenomenalRising => $"{player.Name} has experienced a phenomenal breakthrough in their development!",
                BreakthroughEventType.VeteranSurge => $"{player.Name} has found a new level late in their career!",
                BreakthroughEventType.PositionMastery => $"{player.Name} has mastered their position and elevated their game!",
                BreakthroughEventType.LeadershipBloom => $"{player.Name} has emerged as a true leader both on and off the field!",
                BreakthroughEventType.InspirationalForm => $"{player.Name} is playing career-defining football right now!",
                BreakthroughEventType.MentalBreakthrough => $"{player.Name} has overcome mental barriers and unlocked their potential!",
                BreakthroughEventType.PhysicalBreakthrough => $"{player.Name} has achieved a significant physical development milestone!",
                BreakthroughEventType.ConfidenceCrisis => $"{player.Name} is struggling with confidence issues affecting their development.",
                BreakthroughEventType.InjurySetback => $"{player.Name}'s development has been hampered by injury concerns.",
                BreakthroughEventType.MotivationLoss => $"{player.Name} appears to have lost some motivation to improve.",
                BreakthroughEventType.ComplacencyEffect => $"{player.Name}'s success may have led to complacency in training.",
                BreakthroughEventType.AgeReality => $"{player.Name} is coming to terms with the reality of aging.",
                BreakthroughEventType.PressureCollapse => $"{player.Name} is struggling to handle the pressure and expectations.",
                BreakthroughEventType.RoleTransition => $"{player.Name} is adapting to a new role within the team.",
                BreakthroughEventType.SystemAdjustment => $"{player.Name} is learning to excel in the new team system.",
                BreakthroughEventType.RecoveryPhase => $"{player.Name} is in a recovery phase, rebuilding after recent setbacks.",
                BreakthroughEventType.PotentialReassessment => $"{player.Name}'s abilities are being re-evaluated with new insights.",
                _ => $"{player.Name} is experiencing a significant change in their development trajectory."
            };
        }

        /// <summary>
        /// Get attribute multipliers for breakthrough events
        /// </summary>
        public static Dictionary<string, float> GetBreakthroughMultipliers(BreakthroughEventType type)
        {
            return type switch
            {
                BreakthroughEventType.PhenomenalRising => new Dictionary<string, float>
                {
                    ["All"] = 2.0f // Double all development for duration
                },
                BreakthroughEventType.PhysicalBreakthrough => new Dictionary<string, float>
                {
                    ["Speed"] = 2.5f,
                    ["Strength"] = 2.5f,
                    ["Endurance"] = 2.0f,
                    ["Agility"] = 2.0f
                },
                BreakthroughEventType.MentalBreakthrough => new Dictionary<string, float>
                {
                    ["DecisionMaking"] = 2.5f,
                    ["Composure"] = 2.0f,
                    ["Leadership"] = 2.0f,
                    ["Pressure"] = 2.0f
                },
                BreakthroughEventType.ConfidenceCrisis => new Dictionary<string, float>
                {
                    ["All"] = 0.3f // Severely reduced development
                },
                BreakthroughEventType.InjurySetback => new Dictionary<string, float>
                {
                    ["Physical"] = 0.2f, // Very limited physical development
                    ["Mental"] = 0.7f    // Mental attributes less affected
                },
                _ => new Dictionary<string, float>
                {
                    ["All"] = 1.0f // No change for unspecified types
                }
            };
        }

        /// <summary>
        /// Get duration of breakthrough effects in weeks
        /// </summary>
        public static int GetBreakthroughDuration(BreakthroughEventType type)
        {
            return type switch
            {
                BreakthroughEventType.PhenomenalRising => 12, // 3 months
                BreakthroughEventType.InspirationalForm => 8,  // 2 months
                BreakthroughEventType.PhysicalBreakthrough => 16, // 4 months
                BreakthroughEventType.MentalBreakthrough => 20,   // 5 months
                BreakthroughEventType.ConfidenceCrisis => 12,     // 3 months
                BreakthroughEventType.InjurySetback => 24,        // 6 months
                BreakthroughEventType.MotivationLoss => 16,       // 4 months
                _ => 8 // Default 2 months
            };
        }

        /// <summary>
        /// Check if breakthrough type is positive
        /// </summary>
        public static bool IsPositiveBreakthrough(BreakthroughEventType type)
        {
            var positiveTypes = new HashSet<BreakthroughEventType>
            {
                BreakthroughEventType.PhenomenalRising,
                BreakthroughEventType.VeteranSurge,
                BreakthroughEventType.PositionMastery,
                BreakthroughEventType.LeadershipBloom,
                BreakthroughEventType.InspirationalForm,
                BreakthroughEventType.MentalBreakthrough,
                BreakthroughEventType.PhysicalBreakthrough,
                BreakthroughEventType.RecoveryPhase
            };
            
            return positiveTypes.Contains(type);
        }

        #endregion

        #region Age Progression Helpers

        /// <summary>
        /// Get weekly age progression rate (positive = improvement, negative = decline)
        /// </summary>
        public static float GetAgeProgressionRate(int age, DevelopmentStage stage)
        {
            return stage switch
            {
                DevelopmentStage.Rookie => 0.05f,      // +0.05 per week natural improvement
                DevelopmentStage.Developing => 0.02f,  // +0.02 per week
                DevelopmentStage.Prime => 0.0f,        // No natural change
                DevelopmentStage.Veteran => -0.01f,    // -0.01 per week decline starts
                DevelopmentStage.Declining => -0.03f,  // -0.03 per week decline
                _ => 0.0f
            };
        }

        /// <summary>
        /// Get age-based effects on different attributes
        /// </summary>
        public static Dictionary<string, float> GetAgeAttributeEffects(int age, DevelopmentStage stage)
        {
            var effects = new Dictionary<string, float>();
            
            switch (stage)
            {
                case DevelopmentStage.Veteran:
                case DevelopmentStage.Declining:
                    // Physical decline
                    effects["Speed"] = -1.2f;
                    effects["Agility"] = -1.1f;
                    effects["Endurance"] = -1.0f;
                    effects["Recovery"] = -1.3f;
                    
                    // Mental improvement
                    effects["DecisionMaking"] = 0.5f;
                    effects["Leadership"] = 0.8f;
                    effects["GameReading"] = 0.6f;
                    break;
                    
                case DevelopmentStage.Rookie:
                case DevelopmentStage.Developing:
                    // Physical improvement
                    effects["Speed"] = 1.0f;
                    effects["Strength"] = 1.2f;
                    effects["Endurance"] = 1.1f;
                    
                    // Learning capacity
                    effects["DecisionMaking"] = 0.8f;
                    effects["Positioning"] = 0.9f;
                    break;
                    
                case DevelopmentStage.Prime:
                    // Peak years - minimal change
                    effects["Consistency"] = 0.3f;
                    effects["Leadership"] = 0.2f;
                    break;
            }
            
            return effects;
        }

        #endregion

        #region Utility Methods

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }

        private static DevelopmentStage GetDevelopmentStage(int age)
        {
            return age switch
            {
                <= 20 => DevelopmentStage.Rookie,
                <= 25 => DevelopmentStage.Developing,
                <= 29 => DevelopmentStage.Prime,
                <= 34 => DevelopmentStage.Veteran,
                _ => DevelopmentStage.Declining
            };
        }

        /// <summary>
        /// Calculate total development gain from an update
        /// </summary>
        public static float CalculateTotalGain(PlayerDevelopmentFramework.PlayerDevelopmentUpdate update)
        {
            return update.GetTotalGains().Values.Sum(Math.Abs);
        }

        /// <summary>
        /// Get primary attribute gains from development update
        /// </summary>
        public static Dictionary<string, float> GetPrimaryGains(PlayerDevelopmentFramework.PlayerDevelopmentUpdate update)
        {
            var totalGains = update.GetTotalGains();
            return totalGains.Where(g => Math.Abs(g.Value) > 0.1f)
                            .Take(3)
                            .ToDictionary(g => g.Key, g => g.Value);
        }

        #endregion
    }

    /// <summary>
    /// Extension class to add missing methods to PlayerDevelopmentFramework
    /// </summary>
    public static class PlayerDevelopmentExtensions
    {
        /// <summary>
        /// Process training development integrating with existing training system
        /// </summary>
        public static Dictionary<string, float> ProcessTrainingDevelopment(this PlayerDevelopmentFramework framework, 
            PlayerDevelopmentProfile profile, TrainingOutcome trainingOutcome)
        {
            var gains = new Dictionary<string, float>();
            
            if (trainingOutcome?.AttributeGains == null)
                return gains;
                
            // Apply specialization bonuses to training gains
            foreach (var trainingGain in trainingOutcome.AttributeGains)
            {
                string attribute = trainingGain.Key;
                float baseGain = trainingGain.Value;
                
                // Apply specialization multiplier if applicable
                float specializationMultiplier = profile.CurrentSpecialization?.AttributeWeights
                    .GetValueOrDefault(attribute, 1.0f) ?? 1.0f;
                
                // Apply development modifiers from breakthrough events etc.
                float modifierMultiplier = profile.DevelopmentModifiers.Values.DefaultIfEmpty(1.0f).Average();
                
                gains[attribute] = baseGain * specializationMultiplier * modifierMultiplier;
            }
            
            return gains;
        }

        /// <summary>
        /// Update development profile after processing
        /// </summary>
        public static void UpdateProfile(this PlayerDevelopmentFramework framework, 
            PlayerDevelopmentProfile profile, PlayerDevelopmentFramework.PlayerDevelopmentUpdate update, float weeksElapsed)
        {
            // Update breakthrough readiness
            profile.BreakthroughReadiness += 2f * weeksElapsed; // Slowly builds readiness
            profile.BreakthroughReadiness = Math.Min(100f, profile.BreakthroughReadiness);
            
            // If breakthrough occurred, reset readiness
            if (update.BreakthroughEvent != null)
            {
                profile.BreakthroughReadiness = 0f;
            }
            
            // Update career highs
            var totalGains = update.GetTotalGains();
            foreach (var gain in totalGains)
            {
                if (gain.Value > 0) // Only positive gains count towards career highs
                {
                    profile.CareerHighs[gain.Key] = Math.Max(
                        profile.CareerHighs.GetValueOrDefault(gain.Key, 0f),
                        gain.Value
                    );
                }
            }
            
            // Decay temporary modifiers
            var modifiersToRemove = new List<string>();
            var decayedModifiers = new Dictionary<string, float>();
            
            foreach (var modifier in profile.DevelopmentModifiers)
            {
                float decayedValue = modifier.Value * 0.95f; // 5% decay per week
                if (Math.Abs(decayedValue - 1.0f) < 0.05f) // Close enough to neutral
                {
                    modifiersToRemove.Add(modifier.Key);
                }
                else
                {
                    decayedModifiers[modifier.Key] = decayedValue;
                }
            }
            
            foreach (var key in modifiersToRemove)
            {
                profile.DevelopmentModifiers.Remove(key);
            }
            foreach (var kvp in decayedModifiers)
            {
                profile.DevelopmentModifiers[kvp.Key] = kvp.Value;
            }
        }
    }
}