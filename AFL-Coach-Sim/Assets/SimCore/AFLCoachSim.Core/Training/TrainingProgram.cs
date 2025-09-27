using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Models;

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
        public List<Position> SuitablePositions { get; set; }
        public DevelopmentStage TargetStage { get; set; }
        
        // Program parameters
        public int DurationDays { get; set; }
        public TrainingIntensity BaseIntensity { get; set; }
        public float InjuryRiskModifier { get; set; }
        public float FatigueRateModifier { get; set; }
        
        // Effectiveness factors
        public float BaseEffectiveness { get; set; }
        public Dictionary<DevelopmentStage, float> StageMultipliers { get; set; }
        public Dictionary<Position, float> PositionMultipliers { get; set; }

        public TrainingProgram()
        {
            Id = Guid.NewGuid().ToString();
            SecondaryFoci = new List<TrainingFocus>();
            AttributeTargets = new Dictionary<string, float>();
            SuitablePositions = new List<Position>();
            StageMultipliers = new Dictionary<DevelopmentStage, float>();
            PositionMultipliers = new Dictionary<Position, float>();
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
            
            // Apply position multiplier
            if (PositionMultipliers.ContainsKey(player.Position))
                effectiveness *= PositionMultipliers[player.Position];
                
            // Apply age factors
            int age = CalculateAge(player.DateOfBirth);
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
            int age = CalculateAge(player.DateOfBirth);
            
            // Check age constraints
            if (age < MinimumAge || age > MaximumAge)
                return false;
                
            // Check position suitability (empty list means suitable for all)
            if (SuitablePositions.Any() && !SuitablePositions.Contains(player.Position))
                return false;
                
            // Check development stage
            if (TargetStage != DevelopmentStage.Rookie && TargetStage != stage)
                return false;
                
            return true;
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
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