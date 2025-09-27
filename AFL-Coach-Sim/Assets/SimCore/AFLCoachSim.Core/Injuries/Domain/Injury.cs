using System;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Injuries.Domain
{
    /// <summary>
    /// Domain entity representing a player injury
    /// </summary>
    public class Injury
    {
        public InjuryId Id { get; private set; }
        public int PlayerId { get; private set; }
        public InjuryType Type { get; private set; }
        public InjurySeverity Severity { get; private set; }
        public InjurySource Source { get; private set; }
        public DateTime OccurredDate { get; private set; }
        public int ExpectedRecoveryDays { get; private set; }
        public int? ActualRecoveryDays { get; private set; }
        public string Description { get; private set; }
        public InjuryStatus Status { get; private set; }
        public float PerformanceImpactMultiplier { get; private set; }
        
        // Recovery tracking
        public DateTime? ReturnToTrainingDate { get; private set; }
        public DateTime? ReturnToMatchDate { get; private set; }
        public int DaysRemaining => Status == InjuryStatus.Active 
            ? Math.Max(0, ExpectedRecoveryDays - (DateTime.Now - OccurredDate).Days)
            : 0;
        
        // Risk factors
        public bool IsRecurring { get; private set; }
        public int? OriginalInjuryId { get; private set; } // Reference to original injury if recurring
        public float RecurrenceRisk { get; private set; }
        
        private Injury() { } // For persistence
        
        public Injury(int playerId, InjuryType type, InjurySeverity severity, InjurySource source, 
                     string description = null, bool isRecurring = false, int? originalInjuryId = null)
        {
            Id = InjuryId.New();
            PlayerId = playerId;
            Type = type;
            Severity = severity;
            Source = source;
            OccurredDate = DateTime.Now;
            Description = description ?? GenerateDescription(type, severity);
            Status = InjuryStatus.Active;
            IsRecurring = isRecurring;
            OriginalInjuryId = originalInjuryId;
            
            // Set recovery time and impact based on severity
            SetRecoveryParameters();
        }
        
        private void SetRecoveryParameters()
        {
            ExpectedRecoveryDays = Severity switch
            {
                InjurySeverity.Niggle => UnityEngine.Random.Range(1, 3),
                InjurySeverity.Minor => UnityEngine.Random.Range(3, 14),
                InjurySeverity.Moderate => UnityEngine.Random.Range(14, 42),
                InjurySeverity.Major => UnityEngine.Random.Range(42, 120),
                InjurySeverity.Severe => UnityEngine.Random.Range(120, 365),
                _ => 7
            };
            
            PerformanceImpactMultiplier = Severity switch
            {
                InjurySeverity.Niggle => 0.95f,
                InjurySeverity.Minor => 0.85f,
                InjurySeverity.Moderate => 0.70f,
                InjurySeverity.Major => 0.50f,
                InjurySeverity.Severe => 0.30f,
                _ => 1.0f
            };
            
            RecurrenceRisk = Type switch
            {
                InjuryType.Muscle => 0.25f,
                InjuryType.Joint => 0.35f,
                InjuryType.Bone => 0.15f,
                InjuryType.Ligament => 0.40f,
                InjuryType.Concussion => 0.50f,
                _ => 0.20f
            };
            
            // Increase risk and recovery time for recurring injuries
            if (IsRecurring)
            {
                ExpectedRecoveryDays = (int)(ExpectedRecoveryDays * 1.3f);
                RecurrenceRisk += 0.15f;
                PerformanceImpactMultiplier *= 0.9f; // Additional performance impact
            }
        }
        
        /// <summary>
        /// Marks the injury as recovered and sets return dates
        /// </summary>
        public void MarkRecovered(DateTime? returnToTrainingDate = null, DateTime? returnToMatchDate = null)
        {
            if (Status != InjuryStatus.Active)
                throw new InvalidOperationException($"Cannot mark non-active injury as recovered. Current status: {Status}");
                
            Status = InjuryStatus.Recovered;
            ActualRecoveryDays = (DateTime.Now - OccurredDate).Days;
            ReturnToTrainingDate = returnToTrainingDate ?? DateTime.Now;
            ReturnToMatchDate = returnToMatchDate ?? DateTime.Now.AddDays(GetReturnToMatchDelay());
        }
        
        /// <summary>
        /// Updates recovery progress and returns whether player can return to training
        /// </summary>
        public bool UpdateRecoveryProgress()
        {
            if (Status != InjuryStatus.Active) return false;
            
            int daysElapsed = (DateTime.Now - OccurredDate).Days;
            
            // Check if ready for training return (usually 80% of expected recovery)
            if (ReturnToTrainingDate == null && daysElapsed >= ExpectedRecoveryDays * 0.8f)
            {
                ReturnToTrainingDate = DateTime.Now;
                return true;
            }
            
            // Check if fully recovered
            if (daysElapsed >= ExpectedRecoveryDays)
            {
                MarkRecovered();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Assesses if player is ready to return to match play
        /// </summary>
        public bool CanReturnToMatch()
        {
            return Status == InjuryStatus.Recovered || 
                   (ReturnToMatchDate.HasValue && DateTime.Now >= ReturnToMatchDate.Value);
        }
        
        /// <summary>
        /// Assesses if player can participate in training
        /// </summary>
        public bool CanParticipateInTraining()
        {
            if (Status != InjuryStatus.Active) return true;
            
            return ReturnToTrainingDate.HasValue && DateTime.Now >= ReturnToTrainingDate.Value;
        }
        
        /// <summary>
        /// Gets current performance impact based on injury status and recovery progress
        /// </summary>
        public float GetCurrentPerformanceImpact()
        {
            if (Status != InjuryStatus.Active) return 1.0f;
            
            // Performance gradually improves as injury heals
            int daysElapsed = (DateTime.Now - OccurredDate).Days;
            float recoveryProgress = Math.Min(1.0f, daysElapsed / (float)ExpectedRecoveryDays);
            
            // Linear improvement from impact multiplier to full performance
            return PerformanceImpactMultiplier + (1.0f - PerformanceImpactMultiplier) * recoveryProgress;
        }
        
        /// <summary>
        /// Determines if this injury increases risk for future injuries
        /// </summary>
        public bool IncreasesRiskFor(InjuryType futureInjuryType)
        {
            // Same injury type has higher recurrence risk
            if (Type == futureInjuryType) return true;
            
            // Certain injury combinations increase risk
            return (Type, futureInjuryType) switch
            {
                (InjuryType.Muscle, InjuryType.Joint) => true,
                (InjuryType.Joint, InjuryType.Muscle) => true,
                (InjuryType.Ligament, InjuryType.Joint) => true,
                _ => false
            };
        }
        
        private int GetReturnToMatchDelay()
        {
            return Severity switch
            {
                InjurySeverity.Niggle => 0,
                InjurySeverity.Minor => UnityEngine.Random.Range(0, 2),
                InjurySeverity.Moderate => UnityEngine.Random.Range(1, 4),
                InjurySeverity.Major => UnityEngine.Random.Range(3, 7),
                InjurySeverity.Severe => UnityEngine.Random.Range(7, 14),
                _ => 1
            };
        }
        
        private static string GenerateDescription(InjuryType type, InjurySeverity severity)
        {
            string bodyPart = type switch
            {
                InjuryType.Muscle => GetRandomBodyPart("hamstring", "calf", "quadriceps", "groin"),
                InjuryType.Joint => GetRandomBodyPart("knee", "ankle", "shoulder", "hip"),
                InjuryType.Bone => GetRandomBodyPart("ribs", "wrist", "finger", "toe"),
                InjuryType.Ligament => GetRandomBodyPart("ACL", "MCL", "ankle ligaments"),
                InjuryType.Concussion => "concussion",
                InjuryType.Other => GetRandomBodyPart("back", "neck", "general"),
                _ => "unknown"
            };
            
            string severityDesc = severity switch
            {
                InjurySeverity.Niggle => "minor",
                InjurySeverity.Minor => "mild",
                InjurySeverity.Moderate => "moderate",
                InjurySeverity.Major => "serious",
                InjurySeverity.Severe => "severe",
                _ => ""
            };
            
            return $"{severityDesc} {bodyPart} {type.ToString().ToLower()}";
        }
        
        private static string GetRandomBodyPart(params string[] parts)
        {
            return parts[UnityEngine.Random.Range(0, parts.Length)];
        }
    }
}