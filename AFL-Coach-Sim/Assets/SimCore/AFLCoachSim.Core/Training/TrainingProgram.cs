using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Training;

namespace AFLCoachSim.Core.Training
{
    /// <summary>
    /// Represents a structured training program with specific goals and methods
    /// </summary>
    public class TrainingProgram
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TrainingType Type { get; set; }
        public TrainingFocus PrimaryFocus { get; set; }
        public List<TrainingFocus> SecondaryFoci { get; set; }
        
        // Target attributes and their improvement factors
        public Dictionary<string, float> AttributeTargets { get; set; }
        
        // Prerequisites and restrictions
        public int MinimumAge { get; set; }
        public int MaximumAge { get; set; }
        public List<Role> SuitableRoles { get; set; }
        public List<Role> SuitablePositions { get; set; }
        public DevelopmentStage TargetStage { get; set; }
        
        // Program parameters
        public int DurationDays { get; set; }
        public TrainingIntensity BaseIntensity { get; set; }
        public float InjuryRiskModifier { get; set; }
        public float FatigueRateModifier { get; set; }
        
        // Effectiveness factors
        public float BaseEffectiveness { get; set; }
        public Dictionary<DevelopmentStage, float> StageMultipliers { get; set; }
        public Dictionary<Role, float> RoleMultipliers { get; set; }
        public Dictionary<Role, float> PositionMultipliers { get; set; }

        public TrainingProgram()
        {
            Id = Guid.NewGuid().ToString();
            SecondaryFoci = new List<TrainingFocus>();
            AttributeTargets = new Dictionary<string, float>();
            SuitableRoles = new List<Role>();
            SuitablePositions = new List<Role>();
            StageMultipliers = new Dictionary<DevelopmentStage, float>();
            RoleMultipliers = new Dictionary<Role, float>();
            PositionMultipliers = new Dictionary<Role, float>();
            BaseEffectiveness = 1.0f;
            InjuryRiskModifier = 1.0f;
            FatigueRateModifier = 1.0f;
        }

        /// <summary>
        /// Calculate the effectiveness of this program for a specific player
        /// </summary>
        public float CalculateEffectiveness(Player player, DevelopmentStage stage, DevelopmentPotential potential)
        {
            float effectiveness = BaseEffectiveness;
            
            // Apply stage multiplier
            if (StageMultipliers.ContainsKey(stage))
                effectiveness *= StageMultipliers[stage];
            
            // Apply role multiplier
            if (RoleMultipliers.ContainsKey(player.PrimaryRole))
                effectiveness *= RoleMultipliers[player.PrimaryRole];
                
            // Apply age factors
            int age = player.Age;
            if (age < MinimumAge || age > MaximumAge)
                effectiveness *= 0.7f; // Reduced effectiveness outside ideal age range
                
            // Apply potential alignment - players respond better to training they have potential for
            float potentialAlignment = 0f;
            int targetCount = 0;
            foreach (var target in AttributeTargets.Keys)
            {
                if (potential.AttributePotentials.ContainsKey(target))
                {
                    potentialAlignment += potential.AttributePotentials[target];
                    targetCount++;
                }
            }
            
            if (targetCount > 0)
            {
                float avgPotential = potentialAlignment / targetCount / 100f; // Normalize to 0-1
                effectiveness *= (0.7f + (avgPotential * 0.6f)); // Scale between 0.7x and 1.3x
            }
            
            return Math.Max(0.1f, effectiveness); // Minimum 10% effectiveness
        }

        /// <summary>
        /// Check if this program is suitable for the given player
        /// </summary>
        public bool IsSuitableFor(Player player, DevelopmentStage stage)
        {
            int age = player.Age;
            
            // Check age constraints
            if (age < MinimumAge || age > MaximumAge)
                return false;
                
            // Check role suitability (empty list means suitable for all)
            if (SuitableRoles.Any() && !SuitableRoles.Contains(player.PrimaryRole))
                return false;
                
            // Check development stage
            if (TargetStage != DevelopmentStage.Rookie && TargetStage != stage)
                return false;
                
            return true;
        }

    }

    /// <summary>
    /// Represents an individual training session within a program
    /// </summary>
    public class TrainingSession
    {
        public string Id { get; set; }
        public string ProgramId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public TrainingIntensity Intensity { get; set; }
        public List<int> ParticipatingPlayers { get; set; }
        public Dictionary<int, TrainingOutcome> Outcomes { get; set; }
        public string Notes { get; set; }
        public bool IsCompleted { get; set; }

        public TrainingSession()
        {
            Id = Guid.NewGuid().ToString();
            ParticipatingPlayers = new List<int>();
            Outcomes = new Dictionary<int, TrainingOutcome>();
        }

        /// <summary>
        /// Mark session as completed with outcomes for each player
        /// </summary>
        public void Complete(Dictionary<int, TrainingOutcome> playerOutcomes, string sessionNotes = "")
        {
            IsCompleted = true;
            CompletedDate = DateTime.Now;
            Outcomes = playerOutcomes;
            Notes = sessionNotes;
        }
    }

    /// <summary>
    /// Tracks a player's enrollment in a training program
    /// </summary>
    public class PlayerTrainingEnrollment
    {
        public int PlayerId { get; set; }
        public string ProgramId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public float ProgressPercentage { get; set; }
        public int SessionsCompleted { get; set; }
        public int SessionsMissed { get; set; }
        public Dictionary<string, float> CumulativeGains { get; set; }
        public float TotalFatigueAccumulated { get; set; }
        public float TotalInjuryRisk { get; set; }
        public bool IsActive { get; set; }

        public PlayerTrainingEnrollment()
        {
            CumulativeGains = new Dictionary<string, float>();
            IsActive = true;
        }

        /// <summary>
        /// Update enrollment with session outcome
        /// </summary>
        public void ProcessSessionOutcome(TrainingOutcome outcome)
        {
            SessionsCompleted++;
            
            // Accumulate attribute gains
            foreach (var gain in outcome.AttributeGains)
            {
                if (CumulativeGains.ContainsKey(gain.Key))
                    CumulativeGains[gain.Key] += gain.Value;
                else
                    CumulativeGains[gain.Key] = gain.Value;
            }
            
            // Accumulate risk factors
            TotalFatigueAccumulated += outcome.FatigueAccumulation;
            TotalInjuryRisk += outcome.InjuryRisk;
        }

        /// <summary>
        /// Calculate completion percentage based on program duration
        /// </summary>
        public float CalculateProgress(TrainingProgram program)
        {
            if (program == null) return 0f;
            
            int daysSinceStart = (DateTime.Now - StartDate).Days;
            ProgressPercentage = Math.Min(100f, (daysSinceStart / (float)program.DurationDays) * 100f);
            
            return ProgressPercentage;
        }
    }
}