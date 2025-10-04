using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems.Development;
using AFLCoachSim.Core.Season.Domain.Entities;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Manages player potential ceilings and prevents training from breaking coach foresight predictions.
    /// Provides dynamic potential adjustments while maintaining realistic development limits.
    /// </summary>
    public class PlayerPotentialManager
    {
        #region Core Data Structures

        /// <summary>
        /// Comprehensive potential profile for each player
        /// </summary>
        public class PlayerPotentialProfile
        {
            // Core Potential Values
            public int PlayerId { get; set; }
            public int NaturalCeiling { get; set; }           // Base genetic potential (never changes)
            public int CurrentCeiling { get; set; }           // Current achievable ceiling (can be modified)
            public int CoachPerceivedCeiling { get; set; }    // What the coach can see/predict
            
            // Potential Modifiers
            public float TrainingEffectiveness { get; set; } = 1.0f;  // How well player responds to training
            public float PotentialRealization { get; set; } = 0.0f;   // % of potential currently achieved
            public int HiddenPotentialReserve { get; set; } = 0;      // Additional potential not visible to coach
            
            // Breakthrough Potential
            public bool CanBreakthroughCeiling { get; set; } = false;
            public int BreakthroughThreshold { get; set; } = 0;       // When breakthrough becomes possible
            public float BreakthroughProbability { get; set; } = 0.0f;
            
            // Development State
            public DevelopmentPhase CurrentPhase { get; set; }
            public float DevelopmentMomentum { get; set; } = 1.0f;
            public DateTime LastPotentialUpdate { get; set; }
            
            // Tracking
            public int HighestOverallAchieved { get; set; } = 0;
            public List<PotentialBreakthroughEvent> BreakthroughHistory { get; set; } = new();
            public Dictionary<string, int> AttributeCeilings { get; set; } = new();  // Individual attribute caps
            
            // Coach Insight Data
            public CoachInsightLevel CoachInsightLevel { get; set; }
            public int CoachVisibleCeiling => CalculateCoachVisibleCeiling();
            public float CoachCertainty { get; set; } = 0.5f;         // How certain coach is about the assessment
            
            private int CalculateCoachVisibleCeiling()
            {
                // Coach can see base potential + some variance based on insight level
                var variance = CoachInsightLevel switch
                {
                    CoachInsightLevel.Poor => UnityEngine.Random.Range(-8, 5),      // -8 to +4
                    CoachInsightLevel.Average => UnityEngine.Random.Range(-5, 8),   // -5 to +7
                    CoachInsightLevel.Good => UnityEngine.Random.Range(-3, 10),     // -3 to +9
                    CoachInsightLevel.Excellent => UnityEngine.Random.Range(-2, 12), // -2 to +11
                    CoachInsightLevel.Legendary => UnityEngine.Random.Range(0, 15),  // 0 to +14
                    _ => 0
                };
                
                return Mathf.Clamp(CurrentCeiling + variance, 50, 99);
            }
        }

        /// <summary>
        /// Potential breakthrough event that can raise a player's ceiling
        /// </summary>
        public class PotentialBreakthroughEvent
        {
            public DateTime Date { get; set; }
            public BreakthroughType Type { get; set; }
            public int PotentialGained { get; set; }
            public string Description { get; set; }
            public bool WasVisible { get; set; } // Was this predictable by the coach?
        }

        /// <summary>
        /// Development phases that affect potential realization
        /// </summary>
        public enum DevelopmentPhase
        {
            EarlyDevelopment,    // 16-20: High potential for growth
            RapidGrowth,         // 21-24: Peak development years
            Consolidation,       // 25-28: Realizing existing potential
            PeakYears,          // 29-32: Maintaining peak performance
            GradualDecline,     // 33-35: Slow decline
            Veteran             // 36+: Experience over physical ability
        }

        /// <summary>
        /// Coach insight levels affecting potential prediction accuracy
        /// </summary>
        public enum CoachInsightLevel
        {
            Poor,       // Basic coaching background
            Average,    // Standard coaching experience
            Good,       // Experienced with good eye for talent
            Excellent,  // Top-tier talent identification
            Legendary   // Generational talent scout ability
        }

        /// <summary>
        /// Types of potential breakthroughs
        /// </summary>
        public enum BreakthroughType
        {
            TrainingBreakthrough,    // Intensive training unlocks hidden potential
            PositionalMastery,       // Mastering a new position reveals new abilities
            MentalBreakthrough,      // Overcoming mental barriers
            PhysicalMaturation,      // Late physical development
            SystemMastery,          // Understanding team system unlocks potential
            LeadershipGrowth,       // Developing leadership qualities
            InjuryRecovery,         // Coming back stronger from injury
            MotivationalSpark,      // Life event creates new drive
            TechnicalRefinement,    // Perfecting technique opens new levels
            GameInsightEvolution    // Deep understanding of game develops
        }

        #endregion

        #region Core System

        private readonly Dictionary<int, PlayerPotentialProfile> _potentialProfiles = new();
        private readonly PotentialSystemConfig _config;
        private readonly System.Random _random;

        public PlayerPotentialManager(PotentialSystemConfig config = null, int randomSeed = 0)
        {
            _config = config ?? PotentialSystemConfig.CreateDefault();
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }

        /// <summary>
        /// Initialize potential profile for a new player
        /// </summary>
        public PlayerPotentialProfile InitializePlayerPotential(Player player, CoachInsightLevel coachInsight = CoachInsightLevel.Average)
        {
            if (_potentialProfiles.ContainsKey(player.ID))
                return _potentialProfiles[player.ID];

            var profile = CreateInitialPotentialProfile(player, coachInsight);
            _potentialProfiles[player.ID] = profile;
            
            return profile;
        }

        /// <summary>
        /// Main method to check and enforce potential limits during player development
        /// </summary>
        public PlayerStatsDelta ApplyPotentialLimits(Player player, PlayerStatsDelta proposedDelta)
        {
            var profile = GetOrCreateProfile(player);
            var limitedDelta = new PlayerStatsDelta();
            
            // Calculate current overall rating
            int currentOverall = CalculateOverallRating(player.Stats);
            
            // Check if player is approaching their ceiling
            if (currentOverall >= profile.CurrentCeiling - 5) // Within 5 points of ceiling
            {
                // Apply diminishing returns
                return ApplyDiminishingReturns(proposedDelta, profile, currentOverall);
            }
            
            // Check for potential breakthrough opportunity
            if (ShouldCheckForBreakthrough(profile, currentOverall))
            {
                var breakthrough = CheckForPotentialBreakthrough(profile, player, proposedDelta);
                if (breakthrough != null)
                {
                    ApplyBreakthrough(profile, breakthrough);
                }
            }
            
            // Apply individual attribute limits
            limitedDelta = ApplyAttributeLimits(player.Stats, proposedDelta, profile);
            
            // Update potential realization tracking
            UpdatePotentialRealization(profile, player, limitedDelta);
            
            return limitedDelta;
        }

        /// <summary>
        /// Get coach's visible assessment of player potential
        /// </summary>
        public PlayerPotentialAssessment GetCoachAssessment(Player player, CoachInsightLevel insightLevel)
        {
            var profile = GetOrCreateProfile(player);
            profile.CoachInsightLevel = insightLevel;

            return new PlayerPotentialAssessment
            {
                PlayerId = player.ID,
                CurrentOverall = CalculateOverallRating(player.Stats),
                PredictedCeiling = profile.CoachVisibleCeiling,
                Certainty = CalculateCoachCertainty(profile, player),
                DevelopmentPhase = profile.CurrentPhase,
                EstimatedYearsToReachPotential = EstimateYearsToReachPotential(profile, player),
                KeyStrengthsForDevelopment = IdentifyKeyStrengths(player, profile),
                PotentialWeaknesses = IdentifyPotentialWeaknesses(player, profile),
                SpecialNotes = GenerateSpecialNotes(player, profile)
            };
        }

        #endregion

        #region Breakthrough System

        /// <summary>
        /// Check if player should have a potential breakthrough
        /// </summary>
        private PotentialBreakthroughEvent CheckForPotentialBreakthrough(PlayerPotentialProfile profile, 
            Player player, PlayerStatsDelta proposedDelta)
        {
            // Calculate breakthrough probability
            float baseProb = _config.BaseBreakthroughProbability;
            
            // Factors that increase breakthrough probability
            if (proposedDelta.GetTotalChange() > 2.0f) baseProb *= 1.5f; // High development week
            if (profile.DevelopmentMomentum > 1.2f) baseProb *= 1.3f;    // Good momentum
            if (player.Age < 23) baseProb *= 1.4f;                       // Young age bonus
            if (profile.PotentialRealization > 0.8f) baseProb *= 0.3f;   // Harder when near ceiling
            
            if (_random.NextDouble() > baseProb) return null;

            // Determine breakthrough type and magnitude
            var breakthroughType = DetermineBreakthroughType(profile, player);
            int potentialGain = CalculateBreakthroughMagnitude(breakthroughType, profile);

            return new PotentialBreakthroughEvent
            {
                Date = DateTime.UtcNow,
                Type = breakthroughType,
                PotentialGained = potentialGain,
                Description = GenerateBreakthroughDescription(breakthroughType, potentialGain),
                WasVisible = WasBreakthroughPredictable(breakthroughType, profile.CoachInsightLevel)
            };
        }

        /// <summary>
        /// Apply a breakthrough event to the player's potential
        /// </summary>
        private void ApplyBreakthrough(PlayerPotentialProfile profile, PotentialBreakthroughEvent breakthrough)
        {
            // Increase current ceiling
            profile.CurrentCeiling = Mathf.Min(99, profile.CurrentCeiling + breakthrough.PotentialGained);
            
            // Update breakthrough tracking
            profile.BreakthroughHistory.Add(breakthrough);
            profile.LastPotentialUpdate = DateTime.UtcNow;
            
            // Adjust breakthrough probability for future (diminishing returns)
            profile.BreakthroughProbability *= 0.7f;
            
            Debug.Log($"Player {profile.PlayerId} breakthrough: {breakthrough.Description}");
        }

        /// <summary>
        /// Determine what type of breakthrough should occur
        /// </summary>
        private BreakthroughType DetermineBreakthroughType(PlayerPotentialProfile profile, Player player)
        {
            var possibleTypes = new List<(BreakthroughType type, float weight)>();
            
            // Training-based breakthrough (common for intensive training)
            possibleTypes.Add((BreakthroughType.TrainingBreakthrough, 30f));
            
            // Age-based breakthroughs
            if (player.Age < 21)
            {
                possibleTypes.Add((BreakthroughType.PhysicalMaturation, 25f));
                possibleTypes.Add((BreakthroughType.MentalBreakthrough, 20f));
            }
            else if (player.Age < 25)
            {
                possibleTypes.Add((BreakthroughType.PositionalMastery, 20f));
                possibleTypes.Add((BreakthroughType.SystemMastery, 15f));
            }
            else
            {
                possibleTypes.Add((BreakthroughType.LeadershipGrowth, 20f));
                possibleTypes.Add((BreakthroughType.GameInsightEvolution, 25f));
            }
            
            // Universal types
            possibleTypes.Add((BreakthroughType.TechnicalRefinement, 15f));
            possibleTypes.Add((BreakthroughType.MotivationalSpark, 10f));
            
            return WeightedRandomSelection(possibleTypes);
        }

        #endregion

        #region Diminishing Returns & Limits

        /// <summary>
        /// Apply diminishing returns when approaching potential ceiling
        /// </summary>
        private PlayerStatsDelta ApplyDiminishingReturns(PlayerStatsDelta original, 
            PlayerPotentialProfile profile, int currentOverall)
        {
            var limited = new PlayerStatsDelta();
            
            // Calculate how close to ceiling (0.0 = far away, 1.0 = at ceiling)
            float proximityToCeiling = Mathf.Clamp01((currentOverall - (profile.CurrentCeiling - 10)) / 10f);
            
            // Diminishing factor: closer to ceiling = more reduction
            float diminishingFactor = Mathf.Lerp(1.0f, 0.1f, proximityToCeiling);
            
            // Apply diminishing returns to all attributes
            limited.Kicking = original.Kicking * diminishingFactor;
            limited.Handballing = original.Handballing * diminishingFactor;
            limited.Tackling = original.Tackling * diminishingFactor;
            limited.Speed = original.Speed * diminishingFactor;
            limited.Stamina = original.Stamina * diminishingFactor;
            limited.Knowledge = original.Knowledge * diminishingFactor;
            limited.Playmaking = original.Playmaking * diminishingFactor;
            
            return limited;
        }

        /// <summary>
        /// Apply individual attribute ceiling limits
        /// </summary>
        private PlayerStatsDelta ApplyAttributeLimits(PlayerStats currentStats, 
            PlayerStatsDelta proposedDelta, PlayerPotentialProfile profile)
        {
            var limitedDelta = new PlayerStatsDelta();
            
            // Set individual attribute ceilings if not already set
            if (!profile.AttributeCeilings.ContainsKey("Kicking"))
                SetIndividualAttributeCeilings(profile);
            
            // Apply limits to each attribute
            limitedDelta.Kicking = ApplyIndividualAttributeLimit(currentStats.Kicking, 
                proposedDelta.Kicking, profile.AttributeCeilings["Kicking"]);
            limitedDelta.Handballing = ApplyIndividualAttributeLimit(currentStats.Handballing, 
                proposedDelta.Handballing, profile.AttributeCeilings["Handballing"]);
            limitedDelta.Tackling = ApplyIndividualAttributeLimit(currentStats.Tackling, 
                proposedDelta.Tackling, profile.AttributeCeilings["Tackling"]);
            limitedDelta.Speed = ApplyIndividualAttributeLimit(currentStats.Speed, 
                proposedDelta.Speed, profile.AttributeCeilings["Speed"]);
            limitedDelta.Stamina = ApplyIndividualAttributeLimit(currentStats.Stamina, 
                proposedDelta.Stamina, profile.AttributeCeilings["Stamina"]);
            limitedDelta.Knowledge = ApplyIndividualAttributeLimit(currentStats.Knowledge, 
                proposedDelta.Knowledge, profile.AttributeCeilings["Knowledge"]);
            limitedDelta.Playmaking = ApplyIndividualAttributeLimit(currentStats.Playmaking, 
                proposedDelta.Playmaking, profile.AttributeCeilings["Playmaking"]);
            
            return limitedDelta;
        }

        /// <summary>
        /// Apply limit to individual attribute
        /// </summary>
        private float ApplyIndividualAttributeLimit(int currentValue, float proposedGain, int ceiling)
        {
            if (currentValue >= ceiling) return 0f; // Already at or above ceiling
            
            float maxPossibleGain = ceiling - currentValue;
            return Mathf.Min(proposedGain, maxPossibleGain);
        }

        #endregion

        #region Configuration & Helpers

        /// <summary>
        /// Configuration for the potential management system
        /// </summary>
        public class PotentialSystemConfig
        {
            public float BaseBreakthroughProbability { get; set; } = 0.02f;  // 2% per development cycle
            public int MaxHiddenPotential { get; set; } = 10;                // Max hidden potential points
            public float TrainingEffectivenessMean { get; set; } = 1.0f;     // Average training response
            public float TrainingEffectivenessStdDev { get; set; } = 0.3f;   // Variation in training response
            public bool AllowCeilingBreakthroughs { get; set; } = true;      // Can players exceed natural ceiling?
            public int MaxBreakthroughGain { get; set; } = 5;                // Max potential gain per breakthrough
            
            public static PotentialSystemConfig CreateDefault() => new();
            
            public static PotentialSystemConfig CreateConservative() => new()
            {
                BaseBreakthroughProbability = 0.01f,
                MaxHiddenPotential = 5,
                AllowCeilingBreakthroughs = false,
                MaxBreakthroughGain = 3
            };
            
            public static PotentialSystemConfig CreateProgressive() => new()
            {
                BaseBreakthroughProbability = 0.04f,
                MaxHiddenPotential = 15,
                AllowCeilingBreakthroughs = true,
                MaxBreakthroughGain = 8
            };
        }

        /// <summary>
        /// Coach's assessment of player potential
        /// </summary>
        public class PlayerPotentialAssessment
        {
            public int PlayerId { get; set; }
            public int CurrentOverall { get; set; }
            public int PredictedCeiling { get; set; }
            public float Certainty { get; set; }                    // 0-1, how certain coach is
            public DevelopmentPhase DevelopmentPhase { get; set; }
            public int EstimatedYearsToReachPotential { get; set; }
            public List<string> KeyStrengthsForDevelopment { get; set; } = new();
            public List<string> PotentialWeaknesses { get; set; } = new();
            public List<string> SpecialNotes { get; set; } = new();
            
            public string GetPotentialDescription()
            {
                return PredictedCeiling switch
                {
                    >= 90 => "Generational Talent",
                    >= 85 => "Elite Potential", 
                    >= 80 => "High Potential",
                    >= 75 => "Good Potential",
                    >= 70 => "Solid Potential",
                    >= 65 => "Average Potential",
                    _ => "Limited Potential"
                };
            }
        }

        /// <summary>
        /// Create initial potential profile for a player
        /// </summary>
        private PlayerPotentialProfile CreateInitialPotentialProfile(Player player, CoachInsightLevel coachInsight)
        {
            // Use existing potential ceiling if available, otherwise generate
            int naturalCeiling = player.Development?.PotentialCeiling ?? GenerateNaturalCeiling(player);
            int hiddenReserve = _random.Next(0, _config.MaxHiddenPotential + 1);
            
            var profile = new PlayerPotentialProfile
            {
                PlayerId = player.ID,
                NaturalCeiling = naturalCeiling,
                CurrentCeiling = naturalCeiling,
                CoachPerceivedCeiling = naturalCeiling,
                HiddenPotentialReserve = hiddenReserve,
                TrainingEffectiveness = GenerateTrainingEffectiveness(),
                CurrentPhase = GetDevelopmentPhase(player.Age),
                CoachInsightLevel = coachInsight,
                LastPotentialUpdate = DateTime.UtcNow,
                CanBreakthroughCeiling = _config.AllowCeilingBreakthroughs && hiddenReserve > 3
            };
            
            SetIndividualAttributeCeilings(profile);
            
            return profile;
        }

        /// <summary>
        /// Generate individual attribute ceilings based on overall potential
        /// </summary>
        private void SetIndividualAttributeCeilings(PlayerPotentialProfile profile)
        {
            int baseCeiling = profile.CurrentCeiling;
            
            // Add some variation to individual attributes (±5 points)
            profile.AttributeCeilings["Kicking"] = Mathf.Clamp(baseCeiling + _random.Next(-5, 6), 50, 99);
            profile.AttributeCeilings["Handballing"] = Mathf.Clamp(baseCeiling + _random.Next(-5, 6), 50, 99);
            profile.AttributeCeilings["Tackling"] = Mathf.Clamp(baseCeiling + _random.Next(-5, 6), 50, 99);
            profile.AttributeCeilings["Speed"] = Mathf.Clamp(baseCeiling + _random.Next(-5, 6), 50, 99);
            profile.AttributeCeilings["Stamina"] = Mathf.Clamp(baseCeiling + _random.Next(-5, 6), 50, 99);
            profile.AttributeCeilings["Knowledge"] = Mathf.Clamp(baseCeiling + _random.Next(-5, 6), 50, 99);
            profile.AttributeCeilings["Playmaking"] = Mathf.Clamp(baseCeiling + _random.Next(-5, 6), 50, 99);
        }

        /// <summary>
        /// Get or create potential profile for player
        /// </summary>
        private PlayerPotentialProfile GetOrCreateProfile(Player player)
        {
            if (_potentialProfiles.ContainsKey(player.ID))
                return _potentialProfiles[player.ID];
                
            return InitializePlayerPotential(player);
        }

        /// <summary>
        /// Calculate overall rating from player stats
        /// </summary>
        private int CalculateOverallRating(PlayerStats stats)
        {
            return Mathf.RoundToInt((stats.Kicking + stats.Handballing + stats.Tackling + 
                                   stats.Speed + stats.Stamina + stats.Knowledge + stats.Playmaking) / 7f);
        }

        /// <summary>
        /// Weighted random selection helper
        /// </summary>
        private T WeightedRandomSelection<T>(List<(T item, float weight)> weightedItems)
        {
            float totalWeight = weightedItems.Sum(x => x.weight);
            float randomValue = (float)_random.NextDouble() * totalWeight;
            float currentWeight = 0f;
            
            foreach (var (item, weight) in weightedItems)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                    return item;
            }
            
            return weightedItems.First().item; // Fallback
        }

        #region Additional Helper Methods (Simplified for brevity)

        private int GenerateNaturalCeiling(Player player) => 
            Mathf.Clamp(65 + _random.Next(0, 30), 60, 95); // 60-95 range

        private float GenerateTrainingEffectiveness() => 
            Mathf.Clamp((float)(_random.NextGaussian(_config.TrainingEffectivenessMean, _config.TrainingEffectivenessStdDev)), 0.3f, 2.0f);

        private DevelopmentPhase GetDevelopmentPhase(int age) => age switch
        {
            <= 20 => DevelopmentPhase.EarlyDevelopment,
            <= 24 => DevelopmentPhase.RapidGrowth,
            <= 28 => DevelopmentPhase.Consolidation,
            <= 32 => DevelopmentPhase.PeakYears,
            <= 35 => DevelopmentPhase.GradualDecline,
            _ => DevelopmentPhase.Veteran
        };

        private bool ShouldCheckForBreakthrough(PlayerPotentialProfile profile, int currentOverall) =>
            profile.CanBreakthroughCeiling && currentOverall >= profile.CurrentCeiling - 3;

        private void UpdatePotentialRealization(PlayerPotentialProfile profile, Player player, PlayerStatsDelta delta)
        {
            int newOverall = CalculateOverallRating(player.Stats);
            profile.PotentialRealization = (float)newOverall / profile.CurrentCeiling;
            if (newOverall > profile.HighestOverallAchieved)
                profile.HighestOverallAchieved = newOverall;
        }

        private float CalculateCoachCertainty(PlayerPotentialProfile profile, Player player) =>
            Mathf.Clamp01(0.3f + (profile.CoachInsightLevel switch 
            {
                CoachInsightLevel.Poor => 0.2f,
                CoachInsightLevel.Average => 0.3f,
                CoachInsightLevel.Good => 0.4f,
                CoachInsightLevel.Excellent => 0.5f,
                CoachInsightLevel.Legendary => 0.6f,
                _ => 0.3f
            }));

        private int EstimateYearsToReachPotential(PlayerPotentialProfile profile, Player player)
        {
            float remainingPotential = profile.CurrentCeiling - CalculateOverallRating(player.Stats);
            float developmentRate = 3.0f; // Assume ~3 points per year on average
            return Mathf.RoundToInt(Mathf.Max(1, remainingPotential / developmentRate));
        }

        private List<string> IdentifyKeyStrengths(Player player, PlayerPotentialProfile profile)
        {
            var strengths = new List<string>();
            var stats = player.Stats;
            
            if (stats.Kicking > 75) strengths.Add("Excellent kicking ability");
            if (stats.Speed > 75) strengths.Add("Good pace and agility");
            if (stats.Knowledge > 75) strengths.Add("Strong game understanding");
            if (profile.TrainingEffectiveness > 1.2f) strengths.Add("Responds well to training");
            
            return strengths;
        }

        private List<string> IdentifyPotentialWeaknesses(Player player, PlayerPotentialProfile profile)
        {
            var weaknesses = new List<string>();
            var stats = player.Stats;
            
            if (stats.Tackling < 65) weaknesses.Add("Defensive skills need work");
            if (stats.Stamina < 65) weaknesses.Add("Fitness could be improved");
            if (profile.TrainingEffectiveness < 0.8f) weaknesses.Add("May struggle with training load");
            
            return weaknesses;
        }

        private List<string> GenerateSpecialNotes(Player player, PlayerPotentialProfile profile)
        {
            var notes = new List<string>();
            
            if (profile.HiddenPotentialReserve > 5)
                notes.Add("May have untapped potential");
            
            if (profile.BreakthroughHistory.Count > 0)
                notes.Add($"Has had {profile.BreakthroughHistory.Count} breakthrough(s)");
                
            if (player.Age < 20 && profile.CurrentCeiling > 80)
                notes.Add("Exceptional young talent");
                
            return notes;
        }

        private int CalculateBreakthroughMagnitude(BreakthroughType type, PlayerPotentialProfile profile) =>
            type switch
            {
                BreakthroughType.TrainingBreakthrough => _random.Next(1, 4),
                BreakthroughType.PhysicalMaturation => _random.Next(2, 6),
                BreakthroughType.MentalBreakthrough => _random.Next(3, 7),
                BreakthroughType.PositionalMastery => _random.Next(2, 5),
                _ => _random.Next(1, _config.MaxBreakthroughGain + 1)
            };

        private string GenerateBreakthroughDescription(BreakthroughType type, int magnitude) =>
            $"{type} breakthrough (+{magnitude} potential)";

        private bool WasBreakthroughPredictable(BreakthroughType type, CoachInsightLevel insightLevel) =>
            type switch
            {
                BreakthroughType.TrainingBreakthrough => insightLevel >= CoachInsightLevel.Good,
                BreakthroughType.PhysicalMaturation => insightLevel >= CoachInsightLevel.Average,
                BreakthroughType.MentalBreakthrough => insightLevel >= CoachInsightLevel.Excellent,
                _ => false
            };

        #endregion
    }

    // Extension method for generating Gaussian random numbers
    public static class RandomExtensions
    {
        public static double NextGaussian(this System.Random random, double mean = 0.0, double stdDev = 1.0)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }
}