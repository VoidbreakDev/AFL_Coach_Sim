using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems.Development;
using AFLManager.Systems.Training;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Domain.Entities;
using CoachSkills = AFLCoachSim.Core.Domain.Entities.CoachSkills;

namespace AFLManager.Systems.Coach
{
    /// <summary>
    /// Comprehensive Coach Backstory System that provides unique gameplay benefits and specializations
    /// </summary>
    public class CoachBackstorySystem
    {
        #region Core Data Structures

        /// <summary>
        /// Complete coach profile with backstory benefits
        /// </summary>
        public class CoachProfile
        {
            // Basic Info
            public string CoachId { get; set; }
            public string Name { get; set; }
            public CoachBackstory Backstory { get; set; }
            public int ExperienceYears { get; set; }
            public int Age { get; set; }
            
            // Core Abilities
            public PlayerPotentialManager.CoachInsightLevel InsightLevel { get; set; }
            public Dictionary<CoachSpecialty, float> SpecialtyLevels { get; set; } = new();
            public List<CoachAbility> UnlockedAbilities { get; set; } = new();
            
            // NEW: Coach Skills Integration
            public AFLCoachSim.Core.Domain.Entities.CoachSkills Skills { get; set; } = new AFLCoachSim.Core.Domain.Entities.CoachSkills();
            
            // Progression System
            public int ExperiencePoints { get; set; } = 0;
            public int Level { get; set; } = 1;
            public Dictionary<string, float> StatModifiers { get; set; } = new();
            
            // Backstory-Specific Data
            public Dictionary<string, object> BackstoryData { get; set; } = new();
            
            // Unlockable Features
            public List<string> UnlockedFeatures { get; set; } = new();
            public Dictionary<string, DateTime> CooldownTimers { get; set; } = new();
            
            public CoachProfile()
            {
                InitializeSpecialtyLevels();
            }
            
            private void InitializeSpecialtyLevels()
            {
                foreach (CoachSpecialty specialty in Enum.GetValues<CoachSpecialty>())
                {
                    SpecialtyLevels[specialty] = 1.0f;
                }
            }
        }

        /// <summary>
        /// Available coach backstories based on authentic AFL pathways
        /// </summary>
        public enum CoachBackstory
        {
            VeteranPlayer,          // Recently retired player with extensive on-field knowledge
            TacticalExpert,         // Former assistant coach and brilliant strategist
            PeoplePerson,           // Charismatic relationship builder and motivator
            GrassrootsBloomer,      // Grounded developer focused on youth and values
            Outsider               // International coach bringing fresh perspective
        }

        /// <summary>
        /// Coach specialties that can be improved
        /// </summary>
        public enum CoachSpecialty
        {
            PlayerIntuition,       // Ability to assess player potential accurately
            Presence,              // On-field presence and morale impact
            Networking,            // Building and maintaining contacts
            TacticalAdaptation,    // Mid-game tactical adjustments
            DataAnalysis,          // Statistical analysis and scouting
            OppositionAnalysis,    // Identifying opponent weaknesses
            SetPieceExpertise,     // Specializing in set pieces
            Charisma,              // Building rapport and relationships
            Communication,         // Resolving conflicts and motivation
            CommunityRelations,    // Fan and sponsor relationships
            PlayerWelfare,         // Player wellbeing and mental health
            YouthDevelopment,      // Developing young talent
            Innovation,            // Unconventional thinking and tactics
            Adaptability,          // Adjusting to different situations
            MediaManagement,       // Public relations and interviews
            Morale                 // Team morale and motivation
        }

        /// <summary>
        /// Special abilities unlocked through backstory and progression
        /// </summary>
        public class CoachAbility
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public CoachBackstory RequiredBackstory { get; set; }
            public int RequiredLevel { get; set; }
            public TimeSpan Cooldown { get; set; }
            public int EnergyCost { get; set; }
            public AbilityType Type { get; set; }
            public Dictionary<string, float> Effects { get; set; } = new();
            public bool IsPassive { get; set; } = false;
            
            public enum AbilityType
            {
                PlayerDevelopment,
                TacticalAdvantage,
                Motivational,
                ScoutingBoost,
                RecoveryAcceleration,
                DataInsight,
                ContractBonus,
                MatchDayBoost
            }
        }

        #endregion

        #region Core System

        private readonly Dictionary<CoachBackstory, CoachBackstoryTemplate> _backstoryTemplates;
        private readonly List<CoachAbility> _allAbilities;
        private readonly Dictionary<string, CoachProfile> _coachProfiles = new();

        public CoachBackstorySystem()
        {
            _backstoryTemplates = CreateBackstoryTemplates();
            _allAbilities = CreateCoachAbilities();
        }

        /// <summary>
        /// Create a new coach with selected backstory
        /// </summary>
        public CoachProfile CreateCoach(string name, CoachBackstory backstory, int startingAge = 35)
        {
            var template = _backstoryTemplates[backstory];
            var coachId = Guid.NewGuid().ToString();
            
            var coach = new CoachProfile
            {
                CoachId = coachId,
                Name = name,
                Backstory = backstory,
                Age = startingAge,
                ExperienceYears = template.StartingExperience,
                InsightLevel = template.BaseInsightLevel,
                Level = 1,
                ExperiencePoints = 0
            };

            // Apply backstory-specific bonuses
            ApplyBackstoryBonuses(coach, template);
            
            // NEW: Initialize coach skills based on backstory
            InitializeCoachSkills(coach, template);
            
            // Unlock starting abilities
            UnlockStartingAbilities(coach);
            
            // Initialize backstory-specific data
            InitializeBackstoryData(coach);
            
            _coachProfiles[coachId] = coach;
            return coach;
        }

        #endregion

        #region Backstory Templates

        /// <summary>
        /// Template defining backstory characteristics
        /// </summary>
        public class CoachBackstoryTemplate
        {
            public CoachBackstory Backstory { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public PlayerPotentialManager.CoachInsightLevel BaseInsightLevel { get; set; }
            public int StartingExperience { get; set; }
            public Dictionary<CoachSpecialty, float> SpecialtyBonuses { get; set; } = new();
            public List<string> StartingAbilities { get; set; } = new();
            public Dictionary<string, float> StatModifiers { get; set; } = new();
            public string UniqueFeature { get; set; }
        }

        /// <summary>
        /// Create all backstory templates
        /// </summary>
        private Dictionary<CoachBackstory, CoachBackstoryTemplate> CreateBackstoryTemplates()
        {
            return new Dictionary<CoachBackstory, CoachBackstoryTemplate>
            {
                [CoachBackstory.VeteranPlayer] = new CoachBackstoryTemplate
                {
                    Backstory = CoachBackstory.VeteranPlayer,
                    Name = "The Veteran Player",
                    Description = "You are a recently retired player, who has extensive on-field knowledge and experience. You understand the game from a player's perspective and have garnered a deep network of contacts.",
                    BaseInsightLevel = PlayerPotentialManager.CoachInsightLevel.Good,
                    StartingExperience = 5,
                    SpecialtyBonuses = new Dictionary<CoachSpecialty, float>
                    {
                        [CoachSpecialty.PlayerIntuition] = 1.8f,
                        [CoachSpecialty.Presence] = 2.0f,
                        [CoachSpecialty.Networking] = 1.6f,
                        [CoachSpecialty.TacticalAdaptation] = 1.4f
                    },
                    StartingAbilities = new List<string> { "player_intuition", "on_field_presence", "deep_networking" },
                    StatModifiers = new Dictionary<string, float>
                    {
                        ["RecruitmentAccuracy"] = 1.3f,
                        ["MoraleBonus"] = 1.4f,
                        ["InjuryPreventionBonus"] = 1.2f,
                        ["PlayerRespect"] = 1.6f
                    },
                    UniqueFeature = "Legend Status: Significant morale boost and easier recruitment of high-profile players"
                },

                [CoachBackstory.TacticalExpert] = new CoachBackstoryTemplate
                {
                    Backstory = CoachBackstory.TacticalExpert,
                    Name = "The Tactical Expert (Former Assistant Coach)",
                    Description = "You are a brilliant strategist with a knack for analyzing data and identifying weaknesses. You have spent years studying the game and gathering knowledge from Head Coaches, but lack on-field experience.",
                    BaseInsightLevel = PlayerPotentialManager.CoachInsightLevel.Average,
                    StartingExperience = 8,
                    SpecialtyBonuses = new Dictionary<CoachSpecialty, float>
                    {
                        [CoachSpecialty.DataAnalysis] = 2.0f,
                        [CoachSpecialty.OppositionAnalysis] = 2.2f,
                        [CoachSpecialty.SetPieceExpertise] = 1.8f,
                        [CoachSpecialty.TacticalAdaptation] = 1.6f
                    },
                    StartingAbilities = new List<string> { "data_analysis", "opposition_analysis", "set_piece_expertise" },
                    StatModifiers = new Dictionary<string, float>
                    {
                        ["ScoutingReportDetail"] = 1.5f,
                        ["CounterStrategyEffectiveness"] = 1.4f,
                        ["SetPieceBonus"] = 1.3f,
                        ["AnalysisSkill"] = 2.0f
                    },
                    UniqueFeature = "Dynamic Tactics: Can create highly customized tactical systems that adapt to changing game conditions"
                },

                [CoachBackstory.PeoplePerson] = new CoachBackstoryTemplate
                {
                    Backstory = CoachBackstory.PeoplePerson,
                    Name = "The People Person",
                    Description = "You are known for your charisma and ability to connect with people. You excel at building relationships and motivating others around you, but you lack tactical depth.",
                    BaseInsightLevel = PlayerPotentialManager.CoachInsightLevel.Average,
                    StartingExperience = 6,
                    SpecialtyBonuses = new Dictionary<CoachSpecialty, float>
                    {
                        [CoachSpecialty.Charisma] = 2.0f,
                        [CoachSpecialty.Communication] = 1.9f,
                        [CoachSpecialty.CommunityRelations] = 1.7f,
                        [CoachSpecialty.MediaManagement] = 1.6f,
                        [CoachSpecialty.TacticalAdaptation] = 0.7f // Trade-off
                    },
                    StartingAbilities = new List<string> { "charisma", "communication", "community_relations" },
                    StatModifiers = new Dictionary<string, float>
                    {
                        ["MoraleSkill"] = 1.8f,
                        ["ConflictResolution"] = 1.6f,
                        ["FanSupport"] = 1.4f,
                        ["SponsorshipRevenue"] = 1.3f
                    },
                    UniqueFeature = "Club Icon: Significant boost to fan support and sponsorship revenue with unwavering board support"
                },

                [CoachBackstory.GrassrootsBloomer] = new CoachBackstoryTemplate
                {
                    Backstory = CoachBackstory.GrassrootsBloomer,
                    Name = "The Grassroots Bloomer",
                    Description = "You're a grounded individual with strong values and a focus on growing/developing younger talent. You prioritize the player's well-being and long-term growth.",
                    BaseInsightLevel = PlayerPotentialManager.CoachInsightLevel.Average,
                    StartingExperience = 4,
                    SpecialtyBonuses = new Dictionary<CoachSpecialty, float>
                    {
                        [CoachSpecialty.PlayerWelfare] = 2.2f,
                        [CoachSpecialty.YouthDevelopment] = 2.0f,
                        [CoachSpecialty.CommunityRelations] = 1.8f,
                        [CoachSpecialty.Morale] = 1.6f
                    },
                    StartingAbilities = new List<string> { "player_welfare", "youth_development", "strengthened_values" },
                    StatModifiers = new Dictionary<string, float>
                    {
                        ["WelfareSkill"] = 2.0f,
                        ["BurnoutReduction"] = 0.7f, // 30% less burnout
                        ["YouthDevelopmentBonus"] = 1.6f,
                        ["PlayerRetention"] = 1.5f
                    },
                    UniqueFeature = "Legacy Club: Well-known for strong values and commitment to developing young talent with board approval for long-term vision"
                },

                [CoachBackstory.Outsider] = new CoachBackstoryTemplate
                {
                    Backstory = CoachBackstory.Outsider,
                    Name = "The Outsider",
                    Description = "Having been a coach for an international sport, you bring a fresh and unique perspective to the game having learned it from a different system.",
                    BaseInsightLevel = PlayerPotentialManager.CoachInsightLevel.Average,
                    StartingExperience = 7,
                    SpecialtyBonuses = new Dictionary<CoachSpecialty, float>
                    {
                        [CoachSpecialty.Innovation] = 2.4f,
                        [CoachSpecialty.Adaptability] = 2.0f,
                        [CoachSpecialty.Networking] = 1.5f,
                        [CoachSpecialty.TacticalAdaptation] = 1.7f
                    },
                    StartingAbilities = new List<string> { "unconventional_thinking", "adaptability", "fresh_perspective" },
                    StatModifiers = new Dictionary<string, float>
                    {
                        ["InnovationSkill"] = 2.2f,
                        ["TacticalFlexibility"] = 1.8f,
                        ["PlayerExperimentation"] = 1.6f,
                        ["GlobalScoutingAccess"] = 1.4f
                    },
                    UniqueFeature = "Revolutionary Tactics: Unlocks powerful, game-changing tactical systems and breaking boundaries with innovative approach"
                }
            };
        }

        #endregion

        #region Coach Abilities System

        /// <summary>
        /// Create all available coach abilities based on authentic AFL backstories
        /// </summary>
        private List<CoachAbility> CreateCoachAbilities()
        {
            return new List<CoachAbility>
            {
                // Veteran Player Abilities
                new CoachAbility
                {
                    Id = "player_intuition",
                    Name = "Player Intuition",
                    Description = "Higher chance to accurately assess a player's potential during recruitment/drafting",
                    RequiredBackstory = CoachBackstory.VeteranPlayer,
                    RequiredLevel = 1,
                    Cooldown = TimeSpan.FromDays(7),
                    EnergyCost = 10,
                    Type = CoachAbility.AbilityType.ScoutingBoost,
                    Effects = new Dictionary<string, float> { ["RecruitmentAccuracy"] = 1.4f }
                },

                new CoachAbility
                {
                    Id = "tactical_adaptation",
                    Name = "Tactical Adaptation",
                    Description = "Faster reaction to adjust team tactics mid game based on opponent's gameplay",
                    RequiredBackstory = CoachBackstory.VeteranPlayer,
                    RequiredLevel = 5,
                    Cooldown = TimeSpan.FromHours(24),
                    EnergyCost = 15,
                    Type = CoachAbility.AbilityType.TacticalAdvantage,
                    Effects = new Dictionary<string, float> { ["MidGameAdjustmentSpeed"] = 1.5f }
                },

                new CoachAbility
                {
                    Id = "legend_status",
                    Name = "Legend Status",
                    Description = "Significant morale boost for the whole team. Easier to attract higher-profile players",
                    RequiredBackstory = CoachBackstory.VeteranPlayer,
                    RequiredLevel = 10,
                    Cooldown = TimeSpan.FromDays(30),
                    EnergyCost = 25,
                    Type = CoachAbility.AbilityType.Motivational,
                    Effects = new Dictionary<string, float> { ["TeamMoraleBoost"] = 1.6f, ["PlayerAttraction"] = 1.4f }
                },

                // Tactical Expert Abilities
                new CoachAbility
                {
                    Id = "match_simulation",
                    Name = "Match Simulation",
                    Description = "Run small simulations of upcoming matches to test different tactics",
                    RequiredBackstory = CoachBackstory.TacticalExpert,
                    RequiredLevel = 3,
                    Cooldown = TimeSpan.FromDays(3),
                    EnergyCost = 20,
                    Type = CoachAbility.AbilityType.TacticalAdvantage,
                    Effects = new Dictionary<string, float> { ["TacticalPreparation"] = 1.3f }
                },

                new CoachAbility
                {
                    Id = "dynamic_tactics",
                    Name = "Dynamic Tactics",
                    Description = "Create highly customized tactical systems that adapt to changing game conditions",
                    RequiredBackstory = CoachBackstory.TacticalExpert,
                    RequiredLevel = 8,
                    Cooldown = TimeSpan.FromDays(14),
                    EnergyCost = 30,
                    Type = CoachAbility.AbilityType.TacticalAdvantage,
                    Effects = new Dictionary<string, float> { ["TacticalAdaptability"] = 1.8f }
                },

                // People Person Abilities
                new CoachAbility
                {
                    Id = "team_inspiration",
                    Name = "Team Inspiration",
                    Description = "Inspire entire team to perform above their normal level",
                    RequiredBackstory = CoachBackstory.PeoplePerson,
                    RequiredLevel = 1,
                    Cooldown = TimeSpan.FromDays(14),
                    EnergyCost = 30,
                    Type = CoachAbility.AbilityType.Motivational,
                    Effects = new Dictionary<string, float> { ["TeamPerformanceBoost"] = 1.2f }
                },

                new CoachAbility
                {
                    Id = "confidence_boost",
                    Name = "Confidence Revival",
                    Description = "Restore confidence to players after poor performances",
                    RequiredBackstory = CoachBackstory.PeoplePerson,
                    RequiredLevel = 2,
                    Cooldown = TimeSpan.FromDays(30),
                    EnergyCost = 20,
                    Type = CoachAbility.AbilityType.Motivational,
                    Effects = new Dictionary<string, float> { ["ConfidenceRestore"] = 1.6f }
                },

                // Veteran Player Abilities (Additional)
                new CoachAbility
                {
                    Id = "veteran_wisdom",
                    Name = "Veteran Wisdom",
                    Description = "Share professional experience to improve player decision-making",
                    RequiredBackstory = CoachBackstory.VeteranPlayer,
                    RequiredLevel = 1,
                    Cooldown = TimeSpan.FromDays(21),
                    EnergyCost = 15,
                    Type = CoachAbility.AbilityType.PlayerDevelopment,
                    Effects = new Dictionary<string, float> { ["GameIntelligenceBoost"] = 1.4f }
                },

                new CoachAbility
                {
                    Id = "technique_mastery",
                    Name = "Advanced Technique Training",
                    Description = "Teach elite-level techniques to improve player skills",
                    RequiredBackstory = CoachBackstory.VeteranPlayer,
                    RequiredLevel = 3,
                    Cooldown = TimeSpan.FromDays(60),
                    EnergyCost = 25,
                    Type = CoachAbility.AbilityType.PlayerDevelopment,
                    Effects = new Dictionary<string, float> { ["TechnicalSkillBoost"] = 1.5f }
                },

                // Tactical Expert Abilities (Additional)
                new CoachAbility
                {
                    Id = "hidden_gem_detection",
                    Name = "Hidden Gem Detection",
                    Description = "Identify undervalued players in the transfer market",
                    RequiredBackstory = CoachBackstory.TacticalExpert,
                    RequiredLevel = 1,
                    Cooldown = TimeSpan.FromDays(30),
                    EnergyCost = 20,
                    Type = CoachAbility.AbilityType.ScoutingBoost,
                    Effects = new Dictionary<string, float> { ["HiddenTalentDetection"] = 2.0f }
                },

                new CoachAbility
                {
                    Id = "market_analysis",
                    Name = "Transfer Market Analysis",
                    Description = "Get detailed insights on player values and potential deals",
                    RequiredBackstory = CoachBackstory.TacticalExpert,
                    RequiredLevel = 2,
                    Cooldown = TimeSpan.FromDays(14),
                    EnergyCost = 15,
                    Type = CoachAbility.AbilityType.ScoutingBoost,
                    Effects = new Dictionary<string, float> { ["MarketInsight"] = 1.8f }
                },

                // Grassroots Bloomer Abilities
                new CoachAbility
                {
                    Id = "injury_prevention",
                    Name = "Advanced Injury Prevention",
                    Description = "Implement protocols to reduce team injury risk by 40%",
                    RequiredBackstory = CoachBackstory.GrassrootsBloomer,
                    RequiredLevel = 1,
                    Cooldown = TimeSpan.FromDays(60),
                    EnergyCost = 30,
                    Type = CoachAbility.AbilityType.RecoveryAcceleration,
                    Effects = new Dictionary<string, float> { ["InjuryRiskReduction"] = 0.6f }
                },

                new CoachAbility
                {
                    Id = "rapid_recovery",
                    Name = "Rapid Recovery Protocol",
                    Description = "Accelerate injured player recovery time by 50%",
                    RequiredBackstory = CoachBackstory.GrassrootsBloomer,
                    RequiredLevel = 2,
                    Cooldown = TimeSpan.FromDays(45),
                    EnergyCost = 25,
                    Type = CoachAbility.AbilityType.RecoveryAcceleration,
                    Effects = new Dictionary<string, float> { ["RecoverySpeedBonus"] = 1.5f }
                }

                // Add more abilities for other backstories...
            };
        }

        #endregion

        #region System Integration

        /// <summary>
        /// Initialize coach skills based on backstory specialties
        /// </summary>
        private void InitializeCoachSkills(CoachProfile coach, CoachBackstoryTemplate template)
        {
            // Map backstory specialties to coach skills
            var skillMapping = GetBackstorySkillMapping(coach.Backstory);
            
            foreach (var mapping in skillMapping)
            {
                var skillName = mapping.Key;
                var bonusValue = mapping.Value;
                
                // Set skill value based on backstory bonus (50 base + bonus)
                var finalValue = Mathf.Clamp(50 + bonusValue, 1, 100);
                coach.Skills.SetSkillValue(skillName, finalValue);
            }
            
            // Apply any specific modifiers from template
            foreach (var modifier in template.StatModifiers)
            {
                if (modifier.Key.EndsWith("Skill") || modifier.Key.Contains("Effectiveness"))
                {
                    // Find corresponding skill and apply modifier
                    var skillName = FindCorrespondingSkill(modifier.Key);
                    if (!string.IsNullOrEmpty(skillName))
                    {
                        coach.Skills.ApplyModifier(skillName, modifier.Value);
                    }
                }
            }
        }
        
        /// <summary>
        /// Get skill bonuses based on coach backstory
        /// </summary>
        private Dictionary<string, int> GetBackstorySkillMapping(CoachBackstory backstory)
        {
            return backstory switch
            {
                CoachBackstory.VeteranPlayer => new Dictionary<string, int>
                {
                    [nameof(CoachSkills.PlayerEvaluation)] = 30, // 80 total
                    [nameof(CoachSkills.Leadership)] = 25, // 75 total
                    [nameof(CoachSkills.Networking)] = 20, // 70 total
                    [nameof(CoachSkills.TacticalAdaptation)] = 15, // 65 total
                    [nameof(CoachSkills.Communication)] = 10, // 60 total
                    [nameof(CoachSkills.GameDayComposure)] = 20 // 70 total
                },
                
                CoachBackstory.TacticalExpert => new Dictionary<string, int>
                {
                    [nameof(CoachSkills.DataAnalysis)] = 35, // 85 total
                    [nameof(CoachSkills.OppositionAnalysis)] = 30, // 80 total
                    [nameof(CoachSkills.SetPieceExpertise)] = 25, // 75 total
                    [nameof(CoachSkills.TacticalKnowledge)] = 30, // 80 total
                    [nameof(CoachSkills.TacticalAdaptation)] = 20, // 70 total
                    [nameof(CoachSkills.Innovation)] = 15 // 65 total
                },
                
                CoachBackstory.PeoplePerson => new Dictionary<string, int>
                {
                    [nameof(CoachSkills.Communication)] = 35, // 85 total
                    [nameof(CoachSkills.Leadership)] = 30, // 80 total
                    [nameof(CoachSkills.CommunityRelations)] = 25, // 75 total
                    [nameof(CoachSkills.MediaManagement)] = 20, // 70 total
                    [nameof(CoachSkills.ConflictResolution)] = 25, // 75 total
                    [nameof(CoachSkills.Motivation)] = 30, // 80 total
                    [nameof(CoachSkills.TacticalKnowledge)] = -15 // 35 total (trade-off)
                },
                
                CoachBackstory.GrassrootsBloomer => new Dictionary<string, int>
                {
                    [nameof(CoachSkills.PlayerWelfare)] = 35, // 85 total
                    [nameof(CoachSkills.YouthDevelopment)] = 30, // 80 total
                    [nameof(CoachSkills.CommunityRelations)] = 25, // 75 total
                    [nameof(CoachSkills.PlayerDevelopment)] = 20, // 70 total
                    [nameof(CoachSkills.Motivation)] = 20, // 70 total
                    [nameof(CoachSkills.Communication)] = 15 // 65 total
                },
                
                CoachBackstory.Outsider => new Dictionary<string, int>
                {
                    [nameof(CoachSkills.Innovation)] = 40, // 90 total
                    [nameof(CoachSkills.Adaptability)] = 30, // 80 total
                    [nameof(CoachSkills.TacticalAdaptation)] = 25, // 75 total
                    [nameof(CoachSkills.Networking)] = 15, // 65 total
                    [nameof(CoachSkills.TacticalKnowledge)] = 20, // 70 total
                    [nameof(CoachSkills.DataAnalysis)] = 10 // 60 total
                },
                
                _ => new Dictionary<string, int>() // Default: no bonuses
            };
        }
        
        /// <summary>
        /// Find corresponding CoachSkills property for stat modifier
        /// </summary>
        private string FindCorrespondingSkill(string modifierKey)
        {
            return modifierKey switch
            {
                "RecruitmentAccuracy" => nameof(CoachSkills.Recruitment),
                "MoraleSkill" => nameof(CoachSkills.Motivation),
                "WelfareSkill" => nameof(CoachSkills.PlayerWelfare),
                "YouthDevelopmentBonus" => nameof(CoachSkills.YouthDevelopment),
                "InnovationSkill" => nameof(CoachSkills.Innovation),
                "TacticalFlexibility" => nameof(CoachSkills.TacticalAdaptation),
                "AnalysisSkill" => nameof(CoachSkills.DataAnalysis),
                "ConflictResolution" => nameof(CoachSkills.ConflictResolution),
                _ => null
            };
        }

        /// <summary>
        /// Apply backstory bonuses to coach
        /// </summary>
        private void ApplyBackstoryBonuses(CoachProfile coach, CoachBackstoryTemplate template)
        {
            // Apply specialty bonuses
            foreach (var bonus in template.SpecialtyBonuses)
            {
                coach.SpecialtyLevels[bonus.Key] = bonus.Value;
            }

            // Apply stat modifiers
            coach.StatModifiers = new Dictionary<string, float>(template.StatModifiers);

            // Store backstory data
            coach.BackstoryData["Template"] = template;
            coach.BackstoryData["UniqueFeature"] = template.UniqueFeature;
        }

        /// <summary>
        /// Unlock starting abilities for new coach
        /// </summary>
        private void UnlockStartingAbilities(CoachProfile coach)
        {
            var template = (CoachBackstoryTemplate)coach.BackstoryData["Template"];
            
            foreach (var abilityId in template.StartingAbilities)
            {
                var ability = _allAbilities.FirstOrDefault(a => a.Id == abilityId);
                if (ability != null)
                {
                    coach.UnlockedAbilities.Add(ability);
                }
            }
        }

        /// <summary>
        /// Initialize backstory-specific data
        /// </summary>
        private void InitializeBackstoryData(CoachProfile coach)
        {
            switch (coach.Backstory)
            {
                case CoachBackstory.VeteranPlayer:
                    coach.BackstoryData["LegendStatus"] = 0; // Builds over time
                    coach.BackstoryData["PlayerConnections"] = new List<int>(); // Player IDs with special relationships
                    coach.BackstoryData["NetworkContacts"] = new List<string>();
                    break;

                case CoachBackstory.TacticalExpert:
                    coach.BackstoryData["TacticalDatabase"] = new Dictionary<string, object>();
                    coach.BackstoryData["OpponentAnalyses"] = new Dictionary<string, DateTime>();
                    coach.BackstoryData["CustomTactics"] = new List<string>();
                    break;

                case CoachBackstory.PeoplePerson:
                    coach.BackstoryData["FanApprovalRating"] = 75; // Starts high
                    coach.BackstoryData["MediaRelationships"] = new Dictionary<string, float>();
                    coach.BackstoryData["SponsorDeals"] = new List<string>();
                    break;

                case CoachBackstory.GrassrootsBloomer:
                    coach.BackstoryData["YouthPrograms"] = new List<string>();
                    coach.BackstoryData["WelfareInitiatives"] = new Dictionary<int, DateTime>(); // Player ID -> Program start
                    coach.BackstoryData["CommunityProjects"] = new List<string>();
                    break;

                case CoachBackstory.Outsider:
                    coach.BackstoryData["InnovativeTactics"] = new List<string>();
                    coach.BackstoryData["InternationalContacts"] = new List<string>();
                    coach.BackstoryData["ExperimentalMethods"] = new Dictionary<string, object>();
                    break;
            }
        }

        /// <summary>
        /// Get coach's effectiveness for specific task
        /// </summary>
        public float GetCoachEffectiveness(string coachId, CoachSpecialty specialty)
        {
            if (!_coachProfiles.ContainsKey(coachId))
                return 1.0f;

            var coach = _coachProfiles[coachId];
            return coach.SpecialtyLevels.GetValueOrDefault(specialty, 1.0f);
        }

        /// <summary>
        /// Use coach ability (if available and not on cooldown)
        /// </summary>
        public bool UseCoachAbility(string coachId, string abilityId, object target = null)
        {
            if (!_coachProfiles.ContainsKey(coachId))
                return false;

            var coach = _coachProfiles[coachId];
            var ability = coach.UnlockedAbilities.FirstOrDefault(a => a.Id == abilityId);
            
            if (ability == null)
                return false;

            // Check cooldown
            if (coach.CooldownTimers.ContainsKey(abilityId) && 
                DateTime.UtcNow < coach.CooldownTimers[abilityId])
                return false;

            // Apply ability effects (implementation depends on your game systems)
            ApplyAbilityEffects(coach, ability, target);

            // Set cooldown
            coach.CooldownTimers[abilityId] = DateTime.UtcNow.Add(ability.Cooldown);

            return true;
        }

        /// <summary>
        /// Apply ability effects to game systems
        /// </summary>
        private void ApplyAbilityEffects(CoachProfile coach, CoachAbility ability, object target)
        {
            // This would integrate with your specific game systems
            // For example:
            switch (ability.Type)
            {
                case CoachAbility.AbilityType.PlayerDevelopment:
                    // Apply to training system
                    break;
                case CoachAbility.AbilityType.TacticalAdvantage:
                    // Apply to match simulation
                    break;
                case CoachAbility.AbilityType.ScoutingBoost:
                    // Apply to scouting system
                    break;
                // etc.
            }

            Debug.Log($"Coach {coach.Name} used ability: {ability.Name}");
        }

        /// <summary>
        /// Get all available abilities for a coach
        /// </summary>
        public List<CoachAbility> GetAvailableAbilities(string coachId)
        {
            if (!_coachProfiles.ContainsKey(coachId))
                return new List<CoachAbility>();

            var coach = _coachProfiles[coachId];
            return coach.UnlockedAbilities.Where(a => 
                !coach.CooldownTimers.ContainsKey(a.Id) || 
                DateTime.UtcNow >= coach.CooldownTimers[a.Id]
            ).ToList();
        }

        /// <summary>
        /// Get coach profile by ID
        /// </summary>
        public CoachProfile GetCoach(string coachId)
        {
            return _coachProfiles.GetValueOrDefault(coachId);
        }

        #endregion
    }
}