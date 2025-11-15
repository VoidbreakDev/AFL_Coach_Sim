using System;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Core player development framework handling growth curves, potential, and age-based progression
    /// </summary>
    [System.Serializable]
    public class PlayerDevelopment
    {
        [Header("Development Potential")]
        public int PotentialCeiling;    // Max overall rating this player can reach (60-99)
        public float GrowthRate;        // How quickly they develop (0.5-2.0, 1.0 = average)
        public DevelopmentCurve CurveType; // What type of development curve they follow
        
        [Header("Current Progress")]
        public float ExperiencePoints;  // Accumulated XP from training and matches
        public float DevelopmentMomentum; // Current rate of improvement (affected by training/form)
        public int PeakAgeStart = 26;   // When they start reaching peak performance
        public int PeakAgeEnd = 30;     // When they start declining
        
        [Header("Position-Specific Development")]
        public PositionDevelopmentWeights Weights; // How they develop based on their role
        
        public PlayerDevelopment()
        {
            // Default values for a new player (safe defaults, randomized in Initialize())
            PotentialCeiling = 75;  // Average potential
            GrowthRate = 1.0f;      // Average growth
            CurveType = DevelopmentCurve.Steady;
            ExperiencePoints = 0f;
            DevelopmentMomentum = 1.0f;
            Weights = new PositionDevelopmentWeights();
        }
        
        /// <summary>
        /// Initialize with randomized values - call this after deserialization
        /// </summary>
        public void InitializeRandomValues()
        {
            if (PotentialCeiling == 75 && GrowthRate == 1.0f && CurveType == DevelopmentCurve.Steady)
            {
                // Only randomize if still at defaults (not yet randomized)
                PotentialCeiling = UnityEngine.Random.Range(65, 85);
                GrowthRate = UnityEngine.Random.Range(0.8f, 1.2f);
                CurveType = (DevelopmentCurve)UnityEngine.Random.Range(0, 4);
            }
        }
        
        /// <summary>
        /// Calculates how much a player should improve based on age, potential, and training
        /// </summary>
        public PlayerStatsDelta CalculateDevelopment(Player player, TrainingProgram training, float weeksElapsed = 1f)
        {
            var delta = new PlayerStatsDelta();
            
            // Base development rate affected by age
            float ageFactor = CalculateAgeFactor(player.Age);
            float potentialFactor = CalculatePotentialFactor(player);
            float trainingBonus = training?.GetEffectivenessMultiplier(player) ?? 1.0f;
            
            // Calculate total development multiplier
            float developmentRate = ageFactor * potentialFactor * trainingBonus * GrowthRate * DevelopmentMomentum;
            
            // Apply curve-based adjustments
            developmentRate *= GetCurveMultiplier(player.Age);
            
            // Calculate individual stat improvements
            var baseImprovement = CalculateBaseImprovement(developmentRate, weeksElapsed);
            
            // Apply position-specific weightings
            delta.Kicking = ApplyPositionWeight(baseImprovement, Weights.KickingWeight, training?.FocusType == TrainingFocus.Skills);
            delta.Handballing = ApplyPositionWeight(baseImprovement, Weights.HandballingWeight, training?.FocusType == TrainingFocus.Skills);
            delta.Tackling = ApplyPositionWeight(baseImprovement, Weights.TacklingWeight, training?.FocusType == TrainingFocus.Defense);
            delta.Speed = ApplyPositionWeight(baseImprovement, Weights.SpeedWeight, training?.FocusType == TrainingFocus.Fitness);
            delta.Stamina = ApplyPositionWeight(baseImprovement, Weights.StaminaWeight, training?.FocusType == TrainingFocus.Fitness);
            delta.Knowledge = ApplyPositionWeight(baseImprovement, Weights.KnowledgeWeight, training?.FocusType == TrainingFocus.Tactical);
            delta.Playmaking = ApplyPositionWeight(baseImprovement, Weights.PlaymakingWeight, training?.FocusType == TrainingFocus.Tactical);
            
            // Add experience points
            ExperiencePoints += CalculateExperienceGain(player, training, weeksElapsed);
            
            return delta;
        }
        
        /// <summary>
        /// Applies development from match performance (separate from training)
        /// </summary>
        public PlayerStatsDelta ApplyMatchExperience(Player player, MatchPerformanceRating performance)
        {
            var delta = new PlayerStatsDelta();
            
            // Better performances lead to more development
            float performanceMultiplier = (float)performance / 10f; // 0.1 to 1.0 range
            float baseGain = 0.1f * performanceMultiplier * GrowthRate;
            
            // Age affects how much players learn from matches
            float ageFactor = CalculateMatchLearningFactor(player.Age);
            baseGain *= ageFactor;
            
            // Apply smaller, more targeted improvements based on match performance
            if (performance >= MatchPerformanceRating.Good)
            {
                // Good/excellent performances improve relevant stats slightly
                delta.Knowledge += baseGain * 0.5f;
                delta.Playmaking += baseGain * 0.3f;
                
                // Position-specific improvements
                if (IsDefensiveRole(player.Role))
                    delta.Tackling += baseGain * 0.4f;
                else if (IsForwardRole(player.Role))
                    delta.Kicking += baseGain * 0.4f;
                else if (IsMidfieldRole(player.Role))
                {
                    delta.Stamina += baseGain * 0.3f;
                    delta.Handballing += baseGain * 0.3f;
                }
            }
            
            // Add match experience points
            ExperiencePoints += (float)performance * 2.0f;
            
            return delta;
        }
        
        private float CalculateAgeFactor(int age)
        {
            if (age < 18) return 1.5f;      // Very high development
            if (age < 22) return 1.2f;      // High development  
            if (age < 26) return 1.0f;      // Normal development
            if (age < PeakAgeStart) return 0.8f;  // Slower development
            if (age < PeakAgeEnd) return 0.3f;    // Minimal development (peak years)
            if (age < 34) return -0.2f;     // Decline starts
            return -0.5f;                   // Significant decline
        }
        
        private float CalculatePotentialFactor(Player player)
        {
            float currentOverall = player.Stats.GetAverage();
            float potentialRemaining = Mathf.Max(0, PotentialCeiling - currentOverall);
            
            // More potential remaining = faster development
            return Mathf.Clamp(potentialRemaining / 20f, 0.1f, 2.0f);
        }
        
        private float GetCurveMultiplier(int age)
        {
            switch (CurveType)
            {
                case DevelopmentCurve.EarlyBloom:
                    return age < 21 ? 1.3f : (age < 25 ? 1.0f : 0.7f);
                    
                case DevelopmentCurve.LateDeveloper:
                    return age < 21 ? 0.7f : (age < 28 ? 1.2f : 0.9f);
                    
                case DevelopmentCurve.Steady:
                    return 1.0f; // Consistent development
                    
                case DevelopmentCurve.Explosive:
                    return UnityEngine.Random.Range(0.5f, 1.8f); // Unpredictable bursts
                    
                default:
                    return 1.0f;
            }
        }
        
        private float CalculateBaseImprovement(float rate, float weeks)
        {
            // Base improvement per week, scaled by development rate
            return rate * 0.15f * weeks;
        }
        
        private float ApplyPositionWeight(float baseValue, float weight, bool trainingBonus = false)
        {
            float result = baseValue * weight;
            if (trainingBonus) result *= 1.5f; // Bonus if training matches stat type
            return result;
        }
        
        private float CalculateExperienceGain(Player player, TrainingProgram training, float weeks)
        {
            float baseXP = 10f * weeks;
            if (training != null) baseXP *= training.Intensity;
            return baseXP;
        }
        
        private float CalculateMatchLearningFactor(int age)
        {
            if (age < 20) return 1.5f;       // Young players learn a lot from games
            if (age < 25) return 1.0f;       // Normal learning
            if (age < 30) return 0.7f;       // Less learning from experience
            return 0.4f;                     // Veterans learn very little
        }
        
        private bool IsDefensiveRole(PlayerRole role) => 
            role == PlayerRole.FullBack || role == PlayerRole.BackPocket || 
            role == PlayerRole.HalfBack || role == PlayerRole.CentreHalfBack;
            
        private bool IsForwardRole(PlayerRole role) => 
            role == PlayerRole.FullForward || role == PlayerRole.ForwardPocket || 
            role == PlayerRole.HalfForward || role == PlayerRole.CentreHalfForward;
            
        private bool IsMidfieldRole(PlayerRole role) => 
            role == PlayerRole.Centre || role == PlayerRole.Wing || 
            role == PlayerRole.Rover || role == PlayerRole.RuckRover;
    }
    
    /// <summary>
    /// Different development curve types for player progression
    /// </summary>
    public enum DevelopmentCurve
    {
        EarlyBloom,     // Develops quickly when young, plateaus early
        LateDeveloper,  // Slow early development, continues improving longer
        Steady,         // Consistent development throughout career
        Explosive       // Unpredictable bursts of improvement
    }
    
    /// <summary>
    /// Position-specific weights for how different attributes develop
    /// </summary>
    [System.Serializable]
    public class PositionDevelopmentWeights
    {
        [Range(0.1f, 2.0f)] public float KickingWeight = 1.0f;
        [Range(0.1f, 2.0f)] public float HandballingWeight = 1.0f;
        [Range(0.1f, 2.0f)] public float TacklingWeight = 1.0f;
        [Range(0.1f, 2.0f)] public float SpeedWeight = 1.0f;
        [Range(0.1f, 2.0f)] public float StaminaWeight = 1.0f;
        [Range(0.1f, 2.0f)] public float KnowledgeWeight = 1.0f;
        [Range(0.1f, 2.0f)] public float PlaymakingWeight = 1.0f;
        
        public PositionDevelopmentWeights()
        {
            // Default balanced weights
            KickingWeight = 1.0f;
            HandballingWeight = 1.0f;
            TacklingWeight = 1.0f;
            SpeedWeight = 1.0f;
            StaminaWeight = 1.0f;
            KnowledgeWeight = 1.0f;
            PlaymakingWeight = 1.0f;
        }
        
        /// <summary>
        /// Creates position-specific development weights
        /// </summary>
        public static PositionDevelopmentWeights CreateForPosition(PlayerRole role)
        {
            var weights = new PositionDevelopmentWeights();
            
            switch (role)
            {
                case PlayerRole.FullBack:
                case PlayerRole.BackPocket:
                case PlayerRole.HalfBack:
                case PlayerRole.CentreHalfBack:
                    // Defenders focus on tackling, knowledge, and kicking
                    weights.TacklingWeight = 1.5f;
                    weights.KnowledgeWeight = 1.3f;
                    weights.KickingWeight = 1.2f;
                    weights.PlaymakingWeight = 0.8f;
                    weights.SpeedWeight = 0.9f;
                    break;
                    
                case PlayerRole.Centre:
                case PlayerRole.Wing:
                case PlayerRole.Rover:
                case PlayerRole.RuckRover:
                    // Midfielders focus on stamina, handballing, and playmaking
                    weights.StaminaWeight = 1.5f;
                    weights.HandballingWeight = 1.4f;
                    weights.PlaymakingWeight = 1.3f;
                    weights.SpeedWeight = 1.2f;
                    weights.TacklingWeight = 0.9f;
                    break;
                    
                case PlayerRole.FullForward:
                case PlayerRole.ForwardPocket:
                case PlayerRole.HalfForward:
                case PlayerRole.CentreHalfForward:
                    // Forwards focus on kicking, speed, and playmaking
                    weights.KickingWeight = 1.6f;
                    weights.SpeedWeight = 1.3f;
                    weights.PlaymakingWeight = 1.2f;
                    weights.HandballingWeight = 1.1f;
                    weights.TacklingWeight = 0.7f;
                    break;
                    
                case PlayerRole.Ruckman:
                    // Rucks focus on tackling, knowledge, and stamina
                    weights.TacklingWeight = 1.4f;
                    weights.KnowledgeWeight = 1.3f;
                    weights.StaminaWeight = 1.2f;
                    weights.SpeedWeight = 0.6f;
                    weights.HandballingWeight = 1.1f;
                    break;
                    
                default: // Utility
                    // Balanced development
                    break;
            }
            
            return weights;
        }
    }
    
    /// <summary>
    /// Represents changes to player stats from development
    /// </summary>
    [System.Serializable]
    public class PlayerStatsDelta
    {
        public float Kicking;
        public float Handballing;
        public float Tackling;
        public float Speed;
        public float Stamina;
        public float Knowledge;
        public float Playmaking;
        
        /// <summary>
        /// Applies this delta to a player's stats, respecting caps and minimums
        /// </summary>
        public void ApplyTo(PlayerStats stats, int maxRating = 99, int minRating = 1)
        {
            stats.Kicking = Mathf.Clamp(Mathf.RoundToInt(stats.Kicking + Kicking), minRating, maxRating);
            stats.Handballing = Mathf.Clamp(Mathf.RoundToInt(stats.Handballing + Handballing), minRating, maxRating);
            stats.Tackling = Mathf.Clamp(Mathf.RoundToInt(stats.Tackling + Tackling), minRating, maxRating);
            stats.Speed = Mathf.Clamp(Mathf.RoundToInt(stats.Speed + Speed), minRating, maxRating);
            stats.Stamina = Mathf.Clamp(Mathf.RoundToInt(stats.Stamina + Stamina), minRating, maxRating);
            stats.Knowledge = Mathf.Clamp(Mathf.RoundToInt(stats.Knowledge + Knowledge), minRating, maxRating);
            stats.Playmaking = Mathf.Clamp(Mathf.RoundToInt(stats.Playmaking + Playmaking), minRating, maxRating);
        }
        
        public float GetTotalChange()
        {
            return Mathf.Abs(Kicking) + Mathf.Abs(Handballing) + Mathf.Abs(Tackling) + 
                   Mathf.Abs(Speed) + Mathf.Abs(Stamina) + Mathf.Abs(Knowledge) + Mathf.Abs(Playmaking);
        }
    }
    
    /// <summary>
    /// Enum for rating match performance to influence development
    /// </summary>
    public enum MatchPerformanceRating
    {
        Poor = 1,       // 1-2: Significantly below expectations
        Below = 3,      // 3-4: Below average performance
        Average = 5,    // 5-6: Met expectations
        Good = 7,       // 7-8: Above average performance
        Excellent = 9,  // 9-10: Outstanding performance
        Exceptional = 10 // 10: Career-defining performance
    }
}
