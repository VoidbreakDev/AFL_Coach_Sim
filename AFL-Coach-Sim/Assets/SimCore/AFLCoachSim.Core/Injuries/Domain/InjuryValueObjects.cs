using System;

namespace AFLCoachSim.Core.Injuries.Domain
{
    /// <summary>
    /// Unified injury severity that maps to both match-time and season-long injuries
    /// </summary>
    public enum InjurySeverity
    {
        /// <summary>
        /// Minor discomfort, can continue playing with slight performance impact (1-2 days)
        /// </summary>
        Niggle = 0,
        
        /// <summary>
        /// Mild injury requiring short break from activity (3-14 days)
        /// </summary>
        Minor = 1,
        
        /// <summary>
        /// Moderate injury requiring significant recovery time (2-6 weeks)
        /// </summary>
        Moderate = 2,
        
        /// <summary>
        /// Major injury requiring extended rehabilitation (6-16 weeks)
        /// </summary>
        Major = 3,
        
        /// <summary>
        /// Severe injury, potentially season-ending (16+ weeks)
        /// </summary>
        Severe = 4
    }
    
    /// <summary>
    /// Classification of injury by body system/type
    /// </summary>
    public enum InjuryType
    {
        Muscle,      // Strains, tears, cramps
        Joint,       // Sprains, dislocations
        Bone,        // Fractures, stress injuries
        Ligament,    // Ligament tears, ruptures
        Tendon,      // Tendon injuries, tendinitis
        Concussion,  // Head injuries, concussion protocol
        Laceration,  // Cuts, abrasions
        Other        // General injuries, fatigue-related
    }
    
    /// <summary>
    /// Source/context where injury occurred
    /// </summary>
    public enum InjurySource
    {
        Match,       // During match play
        Training,    // During training session
        Fitness,     // During fitness/gym work
        Recovery,    // During recovery/treatment
        Unknown      // Unknown or non-specific
    }
    
    /// <summary>
    /// Current status of the injury
    /// </summary>
    public enum InjuryStatus
    {
        Active,      // Currently injured and affecting performance
        Recovering,  // In rehabilitation phase
        Recovered,   // Fully healed
        Chronic      // Long-term ongoing issue
    }
    
    /// <summary>
    /// Strongly-typed identifier for injuries
    /// </summary>
    public struct InjuryId : IEquatable<InjuryId>
    {
        private readonly int _value;
        
        private InjuryId(int value)
        {
            _value = value;
        }
        
        public static InjuryId New() => new InjuryId(UnityEngine.Random.Range(100000, 999999));
        public static InjuryId From(int value) => new InjuryId(value);
        
        public override string ToString() => _value.ToString();
        public override int GetHashCode() => _value.GetHashCode();
        public override bool Equals(object obj) => obj is InjuryId other && Equals(other);
        public bool Equals(InjuryId other) => _value == other._value;
        
        public static bool operator ==(InjuryId left, InjuryId right) => left.Equals(right);
        public static bool operator !=(InjuryId left, InjuryId right) => !left.Equals(right);
        
        public static implicit operator int(InjuryId id) => id._value;
        public static explicit operator InjuryId(int value) => From(value);
    }
    
    /// <summary>
    /// Value object representing injury risk factors for a player
    /// </summary>
    public class InjuryRiskProfile
    {
        public float BaseInjuryRisk { get; }
        public float FatigueMultiplier { get; }
        public float AgeMultiplier { get; }
        public float DurabilityMultiplier { get; }
        public float RecurrenceMultiplier { get; }
        public float OverallRiskMultiplier { get; }
        
        public InjuryRiskProfile(int playerAge, int durability, float fatigue, float recurrenceRisk = 0f)
        {
            BaseInjuryRisk = 0.02f; // 2% base risk per exposure
            
            // Age factor (risk increases after 28)
            AgeMultiplier = playerAge switch
            {
                < 20 => 1.1f,  // Young players slightly more injury prone
                >= 20 and < 25 => 0.9f,  // Peak age, lower risk
                >= 25 and < 30 => 1.0f,  // Normal risk
                >= 30 and < 33 => 1.3f,  // Increasing risk
                _ => 1.6f      // High risk for older players
            };
            
            // Durability factor (lower durability = higher risk)
            DurabilityMultiplier = 2.0f - (durability / 100f); // 1.0-2.0 range
            
            // Fatigue factor (higher fatigue = higher risk)
            FatigueMultiplier = 1.0f + (fatigue / 100f) * 1.5f; // 1.0-2.5 range
            
            // Recurrence factor (previous injuries increase risk)
            RecurrenceMultiplier = 1.0f + recurrenceRisk;
            
            OverallRiskMultiplier = AgeMultiplier * DurabilityMultiplier * FatigueMultiplier * RecurrenceMultiplier;
        }
        
        /// <summary>
        /// Calculates injury probability for a given exposure period
        /// </summary>
        public float CalculateInjuryProbability(float exposureMinutes, float intensityMultiplier = 1.0f)
        {
            // Base probability scaled by time, intensity, and risk factors
            float baseProb = BaseInjuryRisk * (exposureMinutes / 90f); // Normalize to 90-minute match
            return Math.Min(1.0f, baseProb * intensityMultiplier * OverallRiskMultiplier);
        }
    }
    
    /// <summary>
    /// Value object representing recovery parameters for an injury
    /// </summary>
    public class RecoveryProfile
    {
        public int MinRecoveryDays { get; }
        public int MaxRecoveryDays { get; }
        public int ExpectedRecoveryDays { get; }
        public float TrainingReturnThreshold { get; }
        public float MatchReturnThreshold { get; }
        public bool RequiresMedicalClearance { get; }
        
        public RecoveryProfile(InjurySeverity severity, InjuryType type, int playerAge)
        {
            var baseDays = severity switch
            {
                InjurySeverity.Niggle => (1, 3),
                InjurySeverity.Minor => (3, 14),
                InjurySeverity.Moderate => (14, 42),
                InjurySeverity.Major => (42, 120),
                InjurySeverity.Severe => (120, 365),
                _ => (7, 21)
            };
            
            MinRecoveryDays = baseDays.Item1;
            MaxRecoveryDays = baseDays.Item2;
            
            // Adjust for injury type
            float typeMultiplier = type switch
            {
                InjuryType.Muscle => 0.8f,     // Muscles heal relatively quickly
                InjuryType.Joint => 1.2f,      // Joints take longer
                InjuryType.Bone => 1.5f,       // Bones take significant time
                InjuryType.Ligament => 1.8f,   // Ligaments heal slowly
                InjuryType.Concussion => 1.0f, // Variable but protocol-driven
                _ => 1.0f
            };
            
            // Adjust for player age (older players recover slower)
            float ageMultiplier = playerAge switch
            {
                < 25 => 0.9f,
                >= 25 and < 30 => 1.0f,
                >= 30 and < 33 => 1.2f,
                _ => 1.4f
            };
            
            ExpectedRecoveryDays = (int)((MinRecoveryDays + MaxRecoveryDays) / 2f * typeMultiplier * ageMultiplier);
            
            // Return thresholds (percentage of recovery needed)
            TrainingReturnThreshold = severity switch
            {
                InjurySeverity.Niggle => 0.5f,
                InjurySeverity.Minor => 0.7f,
                InjurySeverity.Moderate => 0.8f,
                InjurySeverity.Major => 0.9f,
                InjurySeverity.Severe => 0.95f,
                _ => 0.8f
            };
            
            MatchReturnThreshold = TrainingReturnThreshold + 0.1f;
            
            // Medical clearance requirements
            RequiresMedicalClearance = severity >= InjurySeverity.Moderate || type == InjuryType.Concussion;
        }
    }
}