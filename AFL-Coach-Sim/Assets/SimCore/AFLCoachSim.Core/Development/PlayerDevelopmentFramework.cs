using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Training;

namespace AFLCoachSim.Core.Development
{
    /// <summary>
    /// Enhanced Player Development Framework that integrates with existing training systems
    /// Adds specialization paths, experience systems, breakthrough events, and career progression
    /// </summary>
    public class PlayerDevelopmentFramework
    {
        private readonly Dictionary<int, PlayerDevelopmentProfile> _playerProfiles;
        private readonly Dictionary<int, List<DevelopmentEvent>> _developmentHistory;
        private readonly Random _random;

        public PlayerDevelopmentFramework(int seed = 0)
        {
            _playerProfiles = new Dictionary<int, PlayerDevelopmentProfile>();
            _developmentHistory = new Dictionary<int, List<DevelopmentEvent>>();
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        #region Core Development Processing

        /// <summary>
        /// Main development processing that integrates with existing training system
        /// </summary>
        public PlayerDevelopmentUpdate ProcessDevelopment(Player player, TrainingOutcome trainingOutcome, 
            int matchesPlayed, float averageMatchRating, float weeksElapsed = 1f)
        {
            var profile = GetOrCreateProfile(player);
            var update = new PlayerDevelopmentUpdate();

            // 1. Process training-based development (from existing system)
            update.TrainingGains = ProcessTrainingDevelopment(profile, trainingOutcome);

            // 2. Process experience-based development
            update.ExperienceGains = ProcessExperienceDevelopment(profile, player, matchesPlayed, averageMatchRating);

            // 3. Process specialization development
            update.SpecializationGains = ProcessSpecializationDevelopment(profile, player);

            // 4. Check for breakthrough events
            var breakthroughEvent = CheckForBreakthroughEvents(profile, player, update);
            if (breakthroughEvent != null)
            {
                update.BreakthroughEvent = breakthroughEvent;
                ApplyBreakthroughEffects(update, breakthroughEvent);
            }

            // 5. Process natural age progression
            update.AgeProgressionEffects = ProcessAgeProgression(profile, player, weeksElapsed);

            // 6. Update development profile
            UpdateProfile(profile, update, weeksElapsed);

            // 7. Record development event
            RecordDevelopmentEvent(player.Id, update);

            return update;
        }

        /// <summary>
        /// Initialize or get existing development profile for a player
        /// </summary>
        public PlayerDevelopmentProfile GetOrCreateProfile(Player player)
        {
            if (_playerProfiles.ContainsKey(player.Id))
                return _playerProfiles[player.Id];

            var profile = CreateInitialProfile(player);
            _playerProfiles[player.Id] = profile;
            return profile;
        }

        #endregion

        #region Specialization System

        /// <summary>
        /// Process development towards player's specialization path
        /// </summary>
        private Dictionary<string, float> ProcessSpecializationDevelopment(PlayerDevelopmentProfile profile, Player player)
        {
            var gains = new Dictionary<string, float>();
            
            if (profile.CurrentSpecialization == null)
                return gains;

            var specialization = profile.CurrentSpecialization;
            var progress = profile.SpecializationProgress;

            // Calculate specialization development rate
            float baseRate = 0.05f * profile.SpecializationAffinity;
            float experienceBonus = Math.Min(0.03f, profile.CareerExperience * 0.0001f);
            float totalRate = baseRate + experienceBonus;

            // Apply development to specialized attributes
            foreach (var attributeWeight in specialization.AttributeWeights)
            {
                string attribute = attributeWeight.Key;
                float weight = attributeWeight.Value;
                float gain = totalRate * weight;

                // Diminishing returns as player approaches specialization mastery
                float masteryFactor = Math.Max(0.1f, 1f - (progress / 100f));
                gain *= masteryFactor;

                gains[attribute] = gain;
            }

            // Update specialization progress
            profile.SpecializationProgress = Math.Min(100f, progress + (totalRate * 10f));

            // Check for specialization advancement
            CheckSpecializationAdvancement(profile, player);

            return gains;
        }

        /// <summary>
        /// Check if player can advance to next specialization tier
        /// </summary>
        private void CheckSpecializationAdvancement(PlayerDevelopmentProfile profile, Player player)
        {
            if (profile.SpecializationProgress >= 80f && profile.CurrentSpecialization.CanAdvance)
            {
                var nextSpecialization = GetNextSpecializationTier(profile.CurrentSpecialization, player);
                if (nextSpecialization != null)
                {
                    profile.PreviousSpecializations.Add(profile.CurrentSpecialization);
                    profile.CurrentSpecialization = nextSpecialization;
                    profile.SpecializationProgress = 0f;
                    profile.LastSpecializationChange = DateTime.UtcNow;
                }
            }
        }

        #endregion

        #region Experience System

        /// <summary>
        /// Process experience-based development from match performance
        /// </summary>
        private Dictionary<string, float> ProcessExperienceDevelopment(PlayerDevelopmentProfile profile, 
            Player player, int matchesPlayed, float averageRating)
        {
            var gains = new Dictionary<string, float>();

            if (matchesPlayed == 0) return gains;

            // Base experience gain
            float experienceGained = matchesPlayed * (averageRating / 10f);
            profile.CareerExperience += experienceGained;

            // Experience-based attribute improvements
            var experienceGains = CalculateExperienceGains(profile, player, experienceGained, averageRating);
            
            foreach (var gain in experienceGains)
            {
                gains[gain.Key] = gain.Value;
            }

            // Check for experience milestones
            CheckExperienceMilestones(profile, player);

            return gains;
        }

        /// <summary>
        /// Calculate attribute gains from match experience
        /// </summary>
        private Dictionary<string, float> CalculateExperienceGains(PlayerDevelopmentProfile profile, 
            Player player, float experienceGained, float averageRating)
        {
            var gains = new Dictionary<string, float>();
            
            // Experience primarily improves mental/tactical attributes
            float baseGain = experienceGained * 0.02f;
            
            // Better performance = more learning
            float performanceMultiplier = Math.Max(0.5f, averageRating / 10f);
            baseGain *= performanceMultiplier;

            // Age affects learning rate from experience
            int age = CalculateAge(player.DateOfBirth);
            float ageMultiplier = age < 23 ? 1.3f : age < 28 ? 1.0f : age < 32 ? 0.7f : 0.4f;
            baseGain *= ageMultiplier;

            // Distribute gains based on position and specialization
            var attributes = GetExperienceAttributes(player.Position, profile.CurrentSpecialization);
            foreach (var attr in attributes)
            {
                gains[attr.Key] = baseGain * attr.Value;
            }

            return gains;
        }

        #endregion

        #region Breakthrough Events

        /// <summary>
        /// Check for potential breakthrough development events
        /// </summary>
        private BreakthroughEvent CheckForBreakthroughEvents(PlayerDevelopmentProfile profile, 
            Player player, PlayerDevelopmentUpdate update)
        {
            // Calculate breakthrough probability based on various factors
            float probability = CalculateBreakthroughProbability(profile, player, update);
            
            if (_random.NextDouble() > probability) return null;

            // Determine breakthrough type
            var breakthroughType = DetermineBreakthroughType(profile, player);
            
            return new BreakthroughEvent
            {
                Type = breakthroughType,
                PlayerId = player.Id,
                Date = DateTime.UtcNow,
                Description = GetBreakthroughDescription(breakthroughType, player),
                AttributeMultipliers = GetBreakthroughMultipliers(breakthroughType),
                DurationWeeks = GetBreakthroughDuration(breakthroughType),
                IsPositive = IsPositiveBreakthrough(breakthroughType)
            };
        }

        /// <summary>
        /// Apply breakthrough effects to development update
        /// </summary>
        private void ApplyBreakthroughEffects(PlayerDevelopmentUpdate update, BreakthroughEvent breakthrough)
        {
            if (breakthrough.IsPositive)
            {
                // Amplify all gains
                foreach (var key in update.TrainingGains.Keys.ToList())
                {
                    update.TrainingGains[key] *= breakthrough.AttributeMultipliers.GetValueOrDefault(key, 1.5f);
                }
                foreach (var key in update.ExperienceGains.Keys.ToList())
                {
                    update.ExperienceGains[key] *= breakthrough.AttributeMultipliers.GetValueOrDefault(key, 1.3f);
                }
            }
            else
            {
                // Reduce gains (regression/setback)
                foreach (var key in update.TrainingGains.Keys.ToList())
                {
                    update.TrainingGains[key] *= breakthrough.AttributeMultipliers.GetValueOrDefault(key, 0.3f);
                }
            }
        }

        #endregion

        #region Age Progression

        /// <summary>
        /// Process natural age-based progression/regression
        /// </summary>
        private Dictionary<string, float> ProcessAgeProgression(PlayerDevelopmentProfile profile, 
            Player player, float weeksElapsed)
        {
            var effects = new Dictionary<string, float>();
            
            int age = CalculateAge(player.DateOfBirth);
            var stage = GetDevelopmentStage(age);

            // Natural progression/regression based on age
            float weeklyEffect = GetAgeProgressionRate(age, stage) * weeksElapsed;
            
            if (Math.Abs(weeklyEffect) < 0.01f) return effects; // Minimal effect

            // Different attributes affected differently by age
            var ageEffects = GetAgeAttributeEffects(age, stage);
            
            foreach (var effect in ageEffects)
            {
                effects[effect.Key] = weeklyEffect * effect.Value;
            }

            return effects;
        }

        #endregion

        #region Helper Methods

        private PlayerDevelopmentProfile CreateInitialProfile(Player player)
        {
            int age = CalculateAge(player.DateOfBirth);
            
            return new PlayerDevelopmentProfile
            {
                PlayerId = player.Id,
                CareerExperience = 0f,
                SpecializationAffinity = 0.8f + ((float)_random.NextDouble() * 0.4f), // 0.8-1.2
                CurrentSpecialization = DetermineInitialSpecialization(player),
                SpecializationProgress = 0f,
                PreviousSpecializations = new List<PlayerSpecialization>(),
                DevelopmentStage = GetDevelopmentStage(age),
                LastSpecializationChange = DateTime.UtcNow,
                BreakthroughReadiness = 0f,
                CareerHighs = new Dictionary<string, float>(),
                DevelopmentModifiers = new Dictionary<string, float>()
            };
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }

        private DevelopmentStage GetDevelopmentStage(int age)
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

        private void RecordDevelopmentEvent(int playerId, PlayerDevelopmentUpdate update)
        {
            if (!_developmentHistory.ContainsKey(playerId))
                _developmentHistory[playerId] = new List<DevelopmentEvent>();

            var developmentEvent = new DevelopmentEvent
            {
                Date = DateTime.UtcNow,
                TotalGain = CalculateTotalGain(update),
                PrimaryGains = GetPrimaryGains(update),
                HasBreakthrough = update.BreakthroughEvent != null,
                BreakthroughType = update.BreakthroughEvent?.Type
            };

            _developmentHistory[playerId].Add(developmentEvent);

            // Keep only last 52 weeks (1 year) of history
            if (_developmentHistory[playerId].Count > 52)
                _developmentHistory[playerId].RemoveAt(0);
        }

        #endregion

        #region Data Structures

        /// <summary>
        /// Comprehensive update result from development processing
        /// </summary>
        public class PlayerDevelopmentUpdate
        {
            public Dictionary<string, float> TrainingGains { get; set; } = new();
            public Dictionary<string, float> ExperienceGains { get; set; } = new();
            public Dictionary<string, float> SpecializationGains { get; set; } = new();
            public Dictionary<string, float> AgeProgressionEffects { get; set; } = new();
            public BreakthroughEvent BreakthroughEvent { get; set; }
            
            public Dictionary<string, float> GetTotalGains()
            {
                var total = new Dictionary<string, float>();
                var allSources = new[] { TrainingGains, ExperienceGains, SpecializationGains, AgeProgressionEffects };
                
                foreach (var source in allSources)
                {
                    foreach (var gain in source)
                    {
                        total[gain.Key] = total.GetValueOrDefault(gain.Key, 0f) + gain.Value;
                    }
                }
                
                return total;
            }
        }

        #endregion
    }
}