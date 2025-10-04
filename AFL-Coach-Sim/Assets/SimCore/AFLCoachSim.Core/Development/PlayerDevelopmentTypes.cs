using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Training;

namespace AFLCoachSim.Core.Development
{
    /// <summary>
    /// Player development profile tracking specialization, experience, and career progression
    /// </summary>
    public class PlayerDevelopmentProfile
    {
        public int PlayerId { get; set; }
        public float CareerExperience { get; set; }
        public float SpecializationAffinity { get; set; }  // How well they adapt to specializations
        public PlayerSpecialization CurrentSpecialization { get; set; }
        public float SpecializationProgress { get; set; }  // 0-100% mastery of current specialization
        public List<PlayerSpecialization> PreviousSpecializations { get; set; }
        public DevelopmentStage DevelopmentStage { get; set; }
        public DateTime LastSpecializationChange { get; set; }
        public float BreakthroughReadiness { get; set; }   // 0-100% chance of breakthrough event
        public Dictionary<string, float> CareerHighs { get; set; }  // Best rating achieved in each attribute
        public Dictionary<string, float> DevelopmentModifiers { get; set; } // Temporary modifiers from events
        
        public PlayerDevelopmentProfile()
        {
            PreviousSpecializations = new List<PlayerSpecialization>();
            CareerHighs = new Dictionary<string, float>();
            DevelopmentModifiers = new Dictionary<string, float>();
        }
    }

    /// <summary>
    /// Player specialization path defining focused development areas
    /// </summary>
    public class PlayerSpecialization
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public PlayerSpecializationType Type { get; set; }
        public int TierLevel { get; set; } // 1 = Basic, 2 = Advanced, 3 = Elite, 4 = Legendary
        public bool CanAdvance { get; set; }
        public Dictionary<string, float> AttributeWeights { get; set; } // Which attributes get boosted
        public List<string> RequiredPositions { get; set; } // Positions that can use this specialization
        public int MinimumAge { get; set; }
        public int MinimumExperience { get; set; }
        public List<string> PrerequisiteSpecializations { get; set; } // Previous specializations needed
        
        public PlayerSpecialization()
        {
            AttributeWeights = new Dictionary<string, float>();
            RequiredPositions = new List<string>();
            PrerequisiteSpecializations = new List<string>();
        }
    }

    /// <summary>
    /// Breakthrough events that can dramatically affect player development
    /// </summary>
    public class BreakthroughEvent
    {
        public BreakthroughEventType Type { get; set; }
        public int PlayerId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public Dictionary<string, float> AttributeMultipliers { get; set; } // Multipliers for development
        public int DurationWeeks { get; set; } // How long the effect lasts
        public bool IsPositive { get; set; }
        public bool IsActive { get; set; }
        
        public BreakthroughEvent()
        {
            AttributeMultipliers = new Dictionary<string, float>();
            IsActive = true;
        }
    }

    /// <summary>
    /// Record of a development event for tracking player progression
    /// </summary>
    public class DevelopmentEvent
    {
        public DateTime Date { get; set; }
        public float TotalGain { get; set; }
        public Dictionary<string, float> PrimaryGains { get; set; }
        public bool HasBreakthrough { get; set; }
        public BreakthroughEventType? BreakthroughType { get; set; }
        
        public DevelopmentEvent()
        {
            PrimaryGains = new Dictionary<string, float>();
        }
    }

    /// <summary>
    /// Types of player specializations
    /// </summary>
    public enum PlayerSpecializationType
    {
        // Defensive Specializations
        DefensiveGeneral,
        KeyDefender,
        SmallDefender,
        ReboundDefender,
        Sweeper,
        
        // Midfield Specializations
        MidfieldGeneral,
        InsideMidfielder,
        OutsideMidfielder,
        WingPlayer,
        TaggingMidfielder,
        BallWinner,
        CreativePlayer,
        
        // Forward Specializations
        ForwardGeneral,
        KeyForward,
        SmallForward,
        CrumbingForward,
        LeadUpForward,
        
        // Ruck Specializations
        RuckGeneral,
        MobileRuck,
        StayAtHomeRuck,
        RuckRover,
        
        // Utility Specializations
        Utility,
        Impact,
        Leadership,
        
        // Elite Specializations (Tier 4)
        Superstar,
        GameChanger,
        LegendaryLeader,
        HallOfFamer
    }

    /// <summary>
    /// Types of breakthrough events
    /// </summary>
    public enum BreakthroughEventType
    {
        // Positive Breakthroughs
        PhenomenalRising,      // Exceptional young talent breakthrough
        VeteranSurge,          // Older player finds new level
        PositionMastery,       // Masters their position completely
        LeadershipBloom,       // Develops exceptional leadership
        InspirationalForm,     // Career-defining form period
        MentalBreakthrough,    // Overcomes mental barriers
        PhysicalBreakthrough,  // Physical development surge
        
        // Negative Events
        ConfidenceCrisis,      // Loss of confidence affecting development
        InjurySetback,         // Injury impacts development ability
        MotivationLoss,        // Loss of drive to improve
        ComplacencyEffect,     // Success leads to reduced effort
        AgeReality,            // Reality of aging hits hard
        PressureCollapse,      // Cannot handle expectations
        
        // Neutral/Mixed
        RoleTransition,        // Adapting to new position/role
        SystemAdjustment,      // Learning new team system
        RecoveryPhase,         // Recovering from previous setback
        PotentialReassessment  // Re-evaluation of abilities
    }

    /// <summary>
    /// Factory class for creating specializations
    /// </summary>
    public static class PlayerSpecializationFactory
    {
        public static Dictionary<string, PlayerSpecialization> CreateAllSpecializations()
        {
            var specializations = new Dictionary<string, PlayerSpecialization>();
            
            // Tier 1 - Basic Specializations
            specializations["def_general"] = CreateDefensiveGeneral();
            specializations["mid_general"] = CreateMidfieldGeneral();
            specializations["fwd_general"] = CreateForwardGeneral();
            specializations["ruck_general"] = CreateRuckGeneral();
            
            // Tier 2 - Advanced Specializations
            specializations["key_defender"] = CreateKeyDefender();
            specializations["small_defender"] = CreateSmallDefender();
            specializations["rebound_defender"] = CreateReboundDefender();
            specializations["inside_mid"] = CreateInsideMidfielder();
            specializations["outside_mid"] = CreateOutsideMidfielder();
            specializations["wing_player"] = CreateWingPlayer();
            specializations["key_forward"] = CreateKeyForward();
            specializations["small_forward"] = CreateSmallForward();
            specializations["mobile_ruck"] = CreateMobileRuck();
            
            // Tier 3 - Elite Specializations
            specializations["sweeper"] = CreateSweeper();
            specializations["ball_winner"] = CreateBallWinner();
            specializations["creative_player"] = CreateCreativePlayer();
            specializations["crumbing_forward"] = CreateCrumbingForward();
            specializations["tagging_mid"] = CreateTaggingMidfielder();
            
            // Tier 4 - Legendary Specializations
            specializations["superstar"] = CreateSuperstar();
            specializations["game_changer"] = CreateGameChanger();
            specializations["legendary_leader"] = CreateLegendaryLeader();
            specializations["hall_of_famer"] = CreateHallOfFamer();
            
            return specializations;
        }

        #region Tier 1 Specializations
        
        private static PlayerSpecialization CreateDefensiveGeneral()
        {
            return new PlayerSpecialization
            {
                Id = "def_general",
                Name = "Defensive General",
                Description = "Well-rounded defensive player focusing on core defensive skills",
                Type = PlayerSpecializationType.DefensiveGeneral,
                TierLevel = 1,
                CanAdvance = true,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Marking"] = 1.3f,
                    ["Contested"] = 1.2f,
                    ["Kicking"] = 1.1f,
                    ["Positioning"] = 1.2f,
                    ["DecisionMaking"] = 1.1f
                },
                RequiredPositions = new List<string> { "FullBack", "HalfBack", "BackPocket", "CentreHalfBack" },
                MinimumAge = 18,
                MinimumExperience = 0
            };
        }
        
        private static PlayerSpecialization CreateMidfieldGeneral()
        {
            return new PlayerSpecialization
            {
                Id = "mid_general",
                Name = "Midfield General",
                Description = "Versatile midfielder with balanced skill development",
                Type = PlayerSpecializationType.MidfieldGeneral,
                TierLevel = 1,
                CanAdvance = true,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Endurance"] = 1.4f,
                    ["Handballing"] = 1.3f,
                    ["DecisionMaking"] = 1.2f,
                    ["Contested"] = 1.1f,
                    ["Kicking"] = 1.1f
                },
                RequiredPositions = new List<string> { "Centre", "Wing", "Rover", "RuckRover" },
                MinimumAge = 18,
                MinimumExperience = 0
            };
        }
        
        private static PlayerSpecialization CreateForwardGeneral()
        {
            return new PlayerSpecialization
            {
                Id = "fwd_general",
                Name = "Forward General",
                Description = "Complete forward focusing on goal-scoring fundamentals",
                Type = PlayerSpecializationType.ForwardGeneral,
                TierLevel = 1,
                CanAdvance = true,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Kicking"] = 1.5f,
                    ["Marking"] = 1.3f,
                    ["Speed"] = 1.2f,
                    ["Positioning"] = 1.2f,
                    ["Contested"] = 1.1f
                },
                RequiredPositions = new List<string> { "FullForward", "HalfForward", "ForwardPocket", "CentreHalfForward" },
                MinimumAge = 18,
                MinimumExperience = 0
            };
        }
        
        private static PlayerSpecialization CreateRuckGeneral()
        {
            return new PlayerSpecialization
            {
                Id = "ruck_general",
                Name = "Ruck General",
                Description = "Traditional ruck focusing on hitouts and contested work",
                Type = PlayerSpecializationType.RuckGeneral,
                TierLevel = 1,
                CanAdvance = true,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Contested"] = 1.5f,
                    ["Marking"] = 1.4f,
                    ["Strength"] = 1.3f,
                    ["Endurance"] = 1.2f,
                    ["Handballing"] = 1.1f
                },
                RequiredPositions = new List<string> { "Ruckman" },
                MinimumAge = 18,
                MinimumExperience = 0
            };
        }
        
        #endregion
        
        #region Tier 2 Specializations
        
        private static PlayerSpecialization CreateKeyDefender()
        {
            return new PlayerSpecialization
            {
                Id = "key_defender",
                Name = "Key Defender",
                Description = "Elite one-on-one defender specializing in stopping key forwards",
                Type = PlayerSpecializationType.KeyDefender,
                TierLevel = 2,
                CanAdvance = true,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Marking"] = 1.6f,
                    ["Contested"] = 1.5f,
                    ["Strength"] = 1.3f,
                    ["Positioning"] = 1.4f,
                    ["Composure"] = 1.2f
                },
                RequiredPositions = new List<string> { "FullBack", "CentreHalfBack" },
                MinimumAge = 20,
                MinimumExperience = 50,
                PrerequisiteSpecializations = new List<string> { "def_general" }
            };
        }
        
        private static PlayerSpecialization CreateInsideMidfielder()
        {
            return new PlayerSpecialization
            {
                Id = "inside_mid",
                Name = "Inside Midfielder",
                Description = "Hard-bodied midfielder who wins contested ball and clearances",
                Type = PlayerSpecializationType.InsideMidfielder,
                TierLevel = 2,
                CanAdvance = true,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Contested"] = 1.8f,
                    ["Strength"] = 1.4f,
                    ["Handballing"] = 1.3f,
                    ["DecisionMaking"] = 1.3f,
                    ["Endurance"] = 1.2f
                },
                RequiredPositions = new List<string> { "Centre", "Rover", "RuckRover" },
                MinimumAge = 19,
                MinimumExperience = 30,
                PrerequisiteSpecializations = new List<string> { "mid_general" }
            };
        }
        
        #endregion
        
        #region Tier 3 Elite Specializations
        
        private static PlayerSpecialization CreateBallWinner()
        {
            return new PlayerSpecialization
            {
                Id = "ball_winner",
                Name = "Elite Ball Winner",
                Description = "Exceptional contested ball winner who dominates stoppages",
                Type = PlayerSpecializationType.BallWinner,
                TierLevel = 3,
                CanAdvance = true,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Contested"] = 2.0f,
                    ["Clearance"] = 1.8f,
                    ["Strength"] = 1.5f,
                    ["DecisionMaking"] = 1.4f,
                    ["Handballing"] = 1.3f
                },
                RequiredPositions = new List<string> { "Centre", "Rover", "RuckRover" },
                MinimumAge = 22,
                MinimumExperience = 150,
                PrerequisiteSpecializations = new List<string> { "inside_mid" }
            };
        }
        
        #endregion
        
        #region Tier 4 Legendary Specializations
        
        private static PlayerSpecialization CreateSuperstar()
        {
            return new PlayerSpecialization
            {
                Id = "superstar",
                Name = "Superstar",
                Description = "Generational talent who excels in all aspects of the game",
                Type = PlayerSpecializationType.Superstar,
                TierLevel = 4,
                CanAdvance = false,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Kicking"] = 1.5f,
                    ["Marking"] = 1.5f,
                    ["Handballing"] = 1.5f,
                    ["Contested"] = 1.5f,
                    ["Speed"] = 1.4f,
                    ["Endurance"] = 1.4f,
                    ["DecisionMaking"] = 1.6f,
                    ["Leadership"] = 1.3f
                },
                RequiredPositions = new List<string>(), // Can be any position
                MinimumAge = 24,
                MinimumExperience = 200,
                PrerequisiteSpecializations = new List<string>() // Multiple paths can lead here
            };
        }
        
        private static PlayerSpecialization CreateHallOfFamer()
        {
            return new PlayerSpecialization
            {
                Id = "hall_of_famer",
                Name = "Hall of Famer",
                Description = "Legendary player whose impact transcends statistics",
                Type = PlayerSpecializationType.HallOfFamer,
                TierLevel = 4,
                CanAdvance = false,
                AttributeWeights = new Dictionary<string, float>
                {
                    ["Leadership"] = 2.0f,
                    ["DecisionMaking"] = 1.8f,
                    ["Composure"] = 1.7f,
                    ["GameImpact"] = 2.5f, // Special attribute for game-changing ability
                    ["Legacy"] = 2.0f      // Special attribute for lasting impact
                },
                RequiredPositions = new List<string>(), // Any position
                MinimumAge = 28,
                MinimumExperience = 400,
                PrerequisiteSpecializations = new List<string> { "superstar", "legendary_leader", "game_changer" }
            };
        }
        
        #endregion
        
        // Additional factory methods for other specializations would go here...
        // For brevity, I'm showing the key examples. The full implementation would include all specializations.
        
        private static PlayerSpecialization CreateSmallDefender() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateReboundDefender() => new PlayerSpecialization(); // Placeholder  
        private static PlayerSpecialization CreateOutsideMidfielder() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateWingPlayer() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateKeyForward() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateSmallForward() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateMobileRuck() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateSweeper() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateCreativePlayer() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateCrumbingForward() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateTaggingMidfielder() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateGameChanger() => new PlayerSpecialization(); // Placeholder
        private static PlayerSpecialization CreateLegendaryLeader() => new PlayerSpecialization(); // Placeholder
    }
}