using System;
using System.Collections.Generic;

namespace AFLCoachSim.Core.Training
{
    /// <summary>
    /// Core training program types available in AFL coaching
    /// </summary>
    public enum TrainingType
    {
        Fitness,        // Cardiovascular and strength training
        Skills,         // Ball handling, kicking, marking
        Tactical,       // Game plan and positional training
        Recovery,       // Rest, rehabilitation, injury prevention
        Leadership,     // Captaincy and mentorship development
        Specialized     // Position-specific or unique training
    }

    /// <summary>
    /// Intensity levels for training sessions
    /// </summary>
    public enum TrainingIntensity
    {
        Light = 1,      // Low impact, recovery-focused
        Moderate = 2,   // Standard training intensity
        High = 3,       // Demanding training with higher gains
        Elite = 4       // Maximum intensity for peak development
    }

    /// <summary>
    /// Focus areas for specialized training programs
    /// </summary>
    public enum TrainingFocus
    {
        // Physical Development
        Endurance,
        Strength, 
        Speed,
        Agility,
        
        // Skill Development  
        Kicking,
        Marking,
        Handballing,
        Contested,
        
        // Tactical Development
        Positioning,
        GamePlan,
        SetPieces,
        Pressure,
        
        // Mental/Leadership
        Leadership,
        Composure,
        DecisionMaking,
        Communication,
        
        // Recovery/Prevention
        InjuryPrevention,
        Recovery,
        LoadManagement,
        Flexibility
    }

    /// <summary>
    /// Development stage categories for age-appropriate training
    /// </summary>
    public enum DevelopmentStage
    {
        Rookie,         // Ages 18-20, high learning potential
        Developing,     // Ages 21-25, steady improvement
        Prime,          // Ages 26-29, peak performance maintenance  
        Veteran,        // Ages 30+, experience-focused development
        Declining       // Late career, injury management focused
    }

    /// <summary>
    /// Training outcome results and effectiveness metrics
    /// </summary>
    public class TrainingOutcome
    {
        public Dictionary<string, float> AttributeGains { get; set; }
        public float InjuryRisk { get; set; }
        public float FatigueAccumulation { get; set; }
        public float MoraleImpact { get; set; }
        public float TeamChemistryImpact { get; set; }
        public List<string> SpecialEffects { get; set; }

        public TrainingOutcome()
        {
            AttributeGains = new Dictionary<string, float>();
            SpecialEffects = new List<string>();
        }
    }

    /// <summary>
    /// Player development potential and tracking
    /// </summary>
    public class DevelopmentPotential
    {
        public int PlayerId { get; set; }
        public float OverallPotential { get; set; }          // 0-100 scale
        public Dictionary<string, float> AttributePotentials { get; set; }
        public float DevelopmentRate { get; set; }           // Affected by age, training history
        public float InjuryProneness { get; set; }           // Injury susceptibility
        public List<TrainingFocus> PreferredTraining { get; set; }
        public DateTime LastUpdated { get; set; }

        public DevelopmentPotential()
        {
            AttributePotentials = new Dictionary<string, float>();
            PreferredTraining = new List<TrainingFocus>();
            LastUpdated = DateTime.Now;
        }
    }

    /// <summary>
    /// Training efficiency metrics for analytics
    /// </summary>
    public class TrainingEfficiency
    {
        public int PlayerId { get; set; }
        public TrainingType TrainingType { get; set; }
        public TrainingFocus Focus { get; set; }
        public float EfficiencyRating { get; set; }          // How well player responds to this training
        public int SessionsCompleted { get; set; }
        public float AverageGain { get; set; }
        public float InjuryIncidence { get; set; }
        public DateTime LastMeasured { get; set; }

        public TrainingEfficiency()
        {
            LastMeasured = DateTime.Now;
        }
    }
}