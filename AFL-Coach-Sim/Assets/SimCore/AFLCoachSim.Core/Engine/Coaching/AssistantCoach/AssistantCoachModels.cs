using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Coaching.AssistantCoach
{
    #region Core Models

    /// <summary>
    /// Unique identifier for assistant coaches
    /// </summary>
    public class AssistantCoachId
    {
        public Guid Value { get; set; } = Guid.NewGuid();
        
        public override bool Equals(object obj) => obj is AssistantCoachId other && Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Specializations available for assistant coaches
    /// </summary>
    public enum AssistantCoachSpecialization
    {
        ForwardCoach,        // Specializes in forward line tactics and goal scoring
        DefensiveCoach,      // Specializes in defensive structures and preventing scores  
        MidfielderCoach,     // Specializes in midfield play and ball movement
        FitnessCoach,        // Specializes in physical conditioning and injury prevention
        SkillsCoach,         // Specializes in technical skills development
        TacticalCoach,       // Specializes in game strategy and formations
        DevelopmentCoach,    // Specializes in young player development
        RecoveryCoach        // Specializes in player recovery and wellness
    }

    /// <summary>
    /// Training types for matching with specializations
    /// </summary>
    public enum TrainingType
    {
        ForwardCraft,
        DefensiveDrills,
        MidfieldWork,
        FitnessConditioning,
        SkillsDevelopment,
        TacticalAnalysis,
        YouthDevelopment,
        RecoverySession,
        MatchSimulation,
        SetPieceWork
    }

    /// <summary>
    /// Complete profile for an assistant coach
    /// </summary>
    public class AssistantCoachProfile
    {
        public AssistantCoachId Id { get; set; } = new AssistantCoachId();
        public string Name { get; set; } = "Unknown Assistant";
        public AssistantCoachSpecialization Specialization { get; set; }
        public float SkillLevel { get; set; } = 50f; // 0-100 skill in their specialization
        public int YearsExperience { get; set; } = 1;
        public int Age { get; set; } = 35;
        
        // Personality traits affecting their coaching style
        public float Innovation { get; set; } = 50f; // Willingness to try new approaches
        public float Communication { get; set; } = 50f; // How well they connect with players
        public float Intensity { get; set; } = 50f; // How demanding they are in training
        public float Patience { get; set; } = 50f; // How they handle player development
        
        // Reputation and background
        public float Reputation { get; set; } = 50f; // Industry reputation (affects hiring cost)
        public List<string> PreviousClubs { get; set; } = new List<string>();
        public List<string> Qualifications { get; set; } = new List<string>();
        public string FormerPlayerPosition { get; set; } = ""; // If they were a former player
        
        // Contract details
        public int ContractWeeksRemaining { get; set; } = 52; // 1 year default
        public float SalaryPerWeek { get; set; } = 1000f;
        public DateTime HireDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Calculate overall coaching value combining skill and experience
        /// </summary>
        public float CalculateOverallValue()
        {
            float skillWeight = 0.4f;
            float experienceWeight = 0.3f;
            float personalityWeight = 0.3f;
            
            float experienceScore = Math.Min(100f, YearsExperience * 5f);
            float personalityScore = (Communication + Patience) / 2f;
            
            return (SkillLevel * skillWeight) + (experienceScore * experienceWeight) + (personalityScore * personalityWeight);
        }
        
        /// <summary>
        /// Get specialization description for UI
        /// </summary>
        public string GetSpecializationDescription()
        {
            return Specialization switch
            {
                AssistantCoachSpecialization.ForwardCoach => "Improves goal scoring, forward positioning, and attacking plays",
                AssistantCoachSpecialization.DefensiveCoach => "Enhances defensive structures, tackling, and preventing scores",
                AssistantCoachSpecialization.MidfielderCoach => "Develops ball movement, endurance, and midfield positioning",
                AssistantCoachSpecialization.FitnessCoach => "Reduces injuries, improves stamina, and physical conditioning",
                AssistantCoachSpecialization.SkillsCoach => "Develops technical skills, accuracy, and ball handling",
                AssistantCoachSpecialization.TacticalCoach => "Provides advanced tactical insights and formation expertise",
                AssistantCoachSpecialization.DevelopmentCoach => "Accelerates young player development and potential unlocking",
                AssistantCoachSpecialization.RecoveryCoach => "Improves recovery times, reduces fatigue, and wellness management",
                _ => "Specialized coaching support"
            };
        }
    }

    #endregion

    #region Training System Integration

    /// <summary>
    /// Training session data for calculating assistant coach contributions
    /// </summary>
    public class TrainingSession
    {
        public TrainingType TrainingType { get; set; }
        public List<Guid> ParticipatingPlayers { get; set; } = new List<Guid>();
        public float IntensityLevel { get; set; } = 50f; // 0-100
        public int DurationMinutes { get; set; } = 90;
        public Weather WeatherConditions { get; set; } = Weather.Clear;
        public Dictionary<string, float> CustomParameters { get; set; } = new Dictionary<string, float>();
    }

    /// <summary>
    /// Player data for training and development calculations
    /// </summary>
    public class PlayerTrainingData
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = "";
        public Role PrimaryRole { get; set; }
        public int Age { get; set; }
        public float CurrentSkillLevel { get; set; } = 50f;
        public float Potential { get; set; } = 60f;
        public float FitnessLevel { get; set; } = 80f;
        public float InjuryProneness { get; set; } = 20f;
        public bool IsYoungPlayer => Age <= 22;
        public Dictionary<string, float> SpecializedSkills { get; set; } = new Dictionary<string, float>();
    }

    /// <summary>
    /// Bonuses provided by assistant coaches to training sessions
    /// </summary>
    public class TrainingBonuses
    {
        public float OverallEffectivenessBonus { get; set; } = 0f;
        public float InjuryReductionBonus { get; set; } = 0f;
        public float SkillDevelopmentBonus { get; set; } = 0f;
        public float FitnessGainBonus { get; set; } = 0f;
        public float RecoverySpeedBonus { get; set; } = 0f;
        
        // Specialized bonuses
        public Dictionary<Role, float> PositionalBonuses { get; set; } = new Dictionary<Role, float>();
        public Dictionary<TrainingType, float> TrainingTypeBonuses { get; set; } = new Dictionary<TrainingType, float>();
        public Dictionary<Guid, float> IndividualPlayerBonuses { get; set; } = new Dictionary<Guid, float>();
        
        /// <summary>
        /// Calculate total bonus for a specific player and training type
        /// </summary>
        public float GetTotalBonusForPlayer(Guid playerId, Role playerRole, TrainingType trainingType)
        {
            float total = OverallEffectivenessBonus;
            total += PositionalBonuses.GetValueOrDefault(playerRole, 0f);
            total += TrainingTypeBonuses.GetValueOrDefault(trainingType, 0f);
            total += IndividualPlayerBonuses.GetValueOrDefault(playerId, 0f);
            return total;
        }
    }

    /// <summary>
    /// Training recommendations from assistant coaches
    /// </summary>
    public class TrainingRecommendation
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public TrainingType RecommendedType { get; set; }
        public List<Guid> TargetPlayers { get; set; } = new List<Guid>();
        public float Priority { get; set; } = 1.0f; // 0-10 scale
        public float ExpectedImpact { get; set; } = 1.0f; // 0-10 scale
        public int RecommendedDuration { get; set; } = 90; // minutes
        public AssistantCoachSpecialization SourceSpecialization { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Get formatted recommendation for UI display
        /// </summary>
        public string GetFormattedRecommendation()
        {
            string urgency = Priority switch
            {
                >= 8f => "URGENT",
                >= 6f => "High Priority",
                >= 4f => "Medium Priority", 
                _ => "Low Priority"
            };
            
            return $"[{urgency}] {Title}: {Description}";
        }
    }

    #endregion

    #region Match Day Integration

    /// <summary>
    /// Insights provided by assistant coaches during matches
    /// </summary>
    public class MatchDayInsights
    {
        public List<string> TacticalRecommendations { get; set; } = new List<string>();
        public List<string> FormationSuggestions { get; set; } = new List<string>();
        public List<string> AttackingStrategySuggestions { get; set; } = new List<string>();
        public List<string> DefensiveStrategySuggestions { get; set; } = new List<string>();
        public List<string> RotationSuggestions { get; set; } = new List<string>();
        public List<string> FatigueManagementAdvice { get; set; } = new List<string>();
        
        public float InsightQuality { get; set; } = 0.5f; // 0-1 scale
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Check if there are any actionable insights
        /// </summary>
        public bool HasActionableInsights()
        {
            return TacticalRecommendations.Any() || FormationSuggestions.Any() || 
                   AttackingStrategySuggestions.Any() || DefensiveStrategySuggestions.Any() ||
                   RotationSuggestions.Any() || FatigueManagementAdvice.Any();
        }
        
        /// <summary>
        /// Get all insights as a formatted list
        /// </summary>
        public List<string> GetAllInsights()
        {
            var allInsights = new List<string>();
            
            if (TacticalRecommendations.Any())
                allInsights.AddRange(TacticalRecommendations.Select(r => $"[Tactical] {r}"));
            if (FormationSuggestions.Any())
                allInsights.AddRange(FormationSuggestions.Select(f => $"[Formation] {f}"));
            if (AttackingStrategySuggestions.Any())
                allInsights.AddRange(AttackingStrategySuggestions.Select(a => $"[Attack] {a}"));
            if (DefensiveStrategySuggestions.Any())
                allInsights.AddRange(DefensiveStrategySuggestions.Select(d => $"[Defense] {d}"));
            if (RotationSuggestions.Any())
                allInsights.AddRange(RotationSuggestions.Select(r => $"[Rotation] {r}"));
            if (FatigueManagementAdvice.Any())
                allInsights.AddRange(FatigueManagementAdvice.Select(f => $"[Fatigue] {f}"));
                
            return allInsights;
        }
    }

    #endregion

    #region Development and Performance

    /// <summary>
    /// Player development bonuses from assistant coaches
    /// </summary>
    public class PlayerDevelopmentBonuses
    {
        public Dictionary<Guid, PlayerDevelopmentBonus> PlayerBonuses { get; set; } = new Dictionary<Guid, PlayerDevelopmentBonus>();
        
        /// <summary>
        /// Get development bonus for specific player
        /// </summary>
        public PlayerDevelopmentBonus GetPlayerBonus(Guid playerId)
        {
            return PlayerBonuses.GetValueOrDefault(playerId, new PlayerDevelopmentBonus());
        }
    }

    /// <summary>
    /// Development bonus for individual player
    /// </summary>
    public class PlayerDevelopmentBonus
    {
        public float OverallDevelopmentRate { get; set; } = 0f; // Multiplier for general development
        public float SpecializedSkillBonus { get; set; } = 0f; // Bonus for skills matching coach specialization
        public float PhysicalAttributeBonus { get; set; } = 0f; // Bonus for physical development
        public float SkillAccuracyBonus { get; set; } = 0f; // Bonus for skill accuracy/consistency
        public float InjuryResistanceBonus { get; set; } = 0f; // Reduced injury risk
        public float PotentialUnlockRate { get; set; } = 0f; // Rate of unlocking hidden potential
        
        /// <summary>
        /// Calculate combined development multiplier
        /// </summary>
        public float GetCombinedDevelopmentMultiplier()
        {
            return 1f + OverallDevelopmentRate + SpecializedSkillBonus + PhysicalAttributeBonus;
        }
    }

    /// <summary>
    /// Contribution from an assistant coach to training/match
    /// </summary>
    public class AssistantCoachContribution
    {
        public float OverallEffectiveness { get; set; } = 0f;
        
        // Training-specific bonuses
        public float InjuryReductionBonus { get; set; } = 0f;
        public float EnduranceBonus { get; set; } = 0f;
        public float SkillDevelopmentBonus { get; set; } = 0f;
        public float AccuracyBonus { get; set; } = 0f;
        public float TacticalAwarenessBonus { get; set; } = 0f;
        public float PositioningBonus { get; set; } = 0f;
        public float YouthDevelopmentBonus { get; set; } = 0f;
        public float PotentialUnlockBonus { get; set; } = 0f;
        public float RecoverySpeedBonus { get; set; } = 0f;
        public float FatigueReductionBonus { get; set; } = 0f;
    }

    #endregion

    #region Performance Tracking

    /// <summary>
    /// Performance tracking for assistant coaches
    /// </summary>
    public class AssistantCoachPerformance
    {
        public AssistantCoachId AssistantId { get; set; }
        public int WeeksEmployed { get; set; } = 0;
        
        private readonly List<float> _weeklyTrainingContributions = new List<float>();
        private readonly List<float> _weeklyMatchContributions = new List<float>();
        private readonly List<float> _weeklyDevelopmentImpacts = new List<float>();

        public AssistantCoachPerformance(AssistantCoachId assistantId)
        {
            AssistantId = assistantId;
        }

        /// <summary>
        /// Record training contribution for the week
        /// </summary>
        public void RecordTrainingContribution(float effectiveness)
        {
            _weeklyTrainingContributions.Add(effectiveness);
        }

        /// <summary>
        /// Record match day contribution
        /// </summary>
        public void RecordMatchContribution(float insightQuality)
        {
            _weeklyMatchContributions.Add(insightQuality);
        }

        /// <summary>
        /// Record development impact
        /// </summary>
        public void RecordDevelopmentImpact(float impact)
        {
            _weeklyDevelopmentImpacts.Add(impact);
        }

        /// <summary>
        /// Complete a week and calculate averages
        /// </summary>
        public void CompleteWeek()
        {
            WeeksEmployed++;
            
            // Keep only last 26 weeks of data (6 months)
            if (_weeklyTrainingContributions.Count > 26)
                _weeklyTrainingContributions.RemoveAt(0);
            if (_weeklyMatchContributions.Count > 26)
                _weeklyMatchContributions.RemoveAt(0);
            if (_weeklyDevelopmentImpacts.Count > 26)
                _weeklyDevelopmentImpacts.RemoveAt(0);
        }

        public float AverageTrainingContribution => 
            _weeklyTrainingContributions.Any() ? _weeklyTrainingContributions.Average() : 0f;
        
        public float AverageMatchContribution =>
            _weeklyMatchContributions.Any() ? _weeklyMatchContributions.Average() : 0f;

        /// <summary>
        /// Calculate development impact score
        /// </summary>
        public float CalculateDevelopmentImpact()
        {
            return _weeklyDevelopmentImpacts.Any() ? _weeklyDevelopmentImpacts.Average() : 0f;
        }

        /// <summary>
        /// Calculate overall performance rating
        /// </summary>
        public float CalculateOverallRating()
        {
            float trainingWeight = 0.4f;
            float matchWeight = 0.35f;
            float developmentWeight = 0.25f;
            
            return (AverageTrainingContribution * trainingWeight) +
                   (AverageMatchContribution * matchWeight) +
                   (CalculateDevelopmentImpact() * developmentWeight);
        }
        
        /// <summary>
        /// Get performance trend (improving, declining, stable)
        /// </summary>
        public string GetPerformanceTrend()
        {
            if (_weeklyTrainingContributions.Count < 4) return "Insufficient Data";
            
            var recent = _weeklyTrainingContributions.TakeLast(4).Average();
            var previous = _weeklyTrainingContributions.Take(_weeklyTrainingContributions.Count - 4).TakeLast(4).Average();
            
            float difference = recent - previous;
            
            return difference switch
            {
                > 0.1f => "Improving",
                < -0.1f => "Declining",
                _ => "Stable"
            };
        }
    }

    /// <summary>
    /// Comprehensive performance report for assistant coach
    /// </summary>
    public class AssistantCoachReport
    {
        public AssistantCoachId AssistantId { get; set; }
        public float TrainingEffectiveness { get; set; }
        public float MatchContribution { get; set; }
        public float DevelopmentImpact { get; set; }
        public float OverallRating { get; set; }
        public int WeeksEmployed { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        
        /// <summary>
        /// Get letter grade for overall performance
        /// </summary>
        public string GetPerformanceGrade()
        {
            return OverallRating switch
            {
                >= 90f => "A+",
                >= 85f => "A",
                >= 80f => "A-",
                >= 75f => "B+", 
                >= 70f => "B",
                >= 65f => "B-",
                >= 60f => "C+",
                >= 55f => "C",
                >= 50f => "C-",
                >= 40f => "D",
                _ => "F"
            };
        }
        
        /// <summary>
        /// Determine if contract should be renewed
        /// </summary>
        public bool ShouldRenewContract()
        {
            return OverallRating >= 55f && WeeksEmployed >= 4; // Minimum 1 month trial
        }
    }

    #endregion
}