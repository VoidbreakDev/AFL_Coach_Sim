using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match;

namespace AFLCoachSim.Core.Engine.Match.Weather
{
    #region Enums

    /// <summary>
    /// Weather conditions that can affect match gameplay
    /// </summary>
    public enum Weather 
    { 
        Clear, 
        Windy, 
        LightRain, 
        HeavyRain,
        // Additional weather types used by advanced weather system
        Wet,     // Alias for rainy conditions
        Hot,     // High temperature conditions
        Cold,    // Low temperature conditions  
        Storm    // Severe weather conditions
    }

    /// <summary>
    /// Types of kicks affected differently by weather
    /// </summary>
    public enum KickType
    {
        ShortPass,      // 5-20 meters
        LongKick,       // 40+ meters
        SetShot,        // Goal attempt from set position
        Snap,           // Quick snap kick
        Handball        // Handball disposal
    }

    /// <summary>
    /// Types of marking contests affected by weather
    /// </summary>
    public enum MarkingType
    {
        Overhead,       // High contested mark
        Chest,          // Chest mark
        Ground          // Ground ball pickup
    }

    /// <summary>
    /// Wind directions for calculating wind effects on kicking
    /// </summary>
    public enum WindDirection
    {
        None,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    #endregion

    #region Weather Conditions

    /// <summary>
    /// Detailed weather conditions for a match
    /// </summary>
    public class WeatherConditions
    {
        public Weather WeatherType { get; set; }
        public float Intensity { get; set; } = 1.0f; // 0.1-2.0 multiplier for weather effects
        public float WindSpeed { get; set; } = 0f; // km/h
        public WindDirection WindDirection { get; set; } = WindDirection.None;
        public float Temperature { get; set; } = 20f; // Celsius
        public float Humidity { get; set; } = 50f; // Percentage
        public float Visibility { get; set; } = 100f; // Percentage (100% = perfect visibility)
        
        // Additional property for backward compatibility
        public float RainIntensity 
        {
            get => WeatherType == Weather.Wet ? Intensity : 0f;
            set => Intensity = value;
        }

        public WeatherConditions(Weather weatherType)
        {
            WeatherType = weatherType;
            SetDefaultValuesForWeatherType();
        }

        /// <summary>
        /// Get a human-readable description of current conditions
        /// </summary>
        public string GetDescription()
        {
            string intensityDesc = Intensity switch
            {
                < 0.7f => "Light",
                < 1.3f => "Moderate", 
                < 1.7f => "Heavy",
                _ => "Severe"
            };

            string baseDesc = WeatherType switch
            {
                Weather.Clear => "Clear conditions",
                Weather.Wet => $"{intensityDesc} rain",
                Weather.Windy => $"{intensityDesc} winds from {WindDirection}",
                Weather.Hot => $"Hot weather ({Temperature:F0}°C)",
                Weather.Cold => $"Cold conditions ({Temperature:F0}°C)",
                Weather.Storm => $"{intensityDesc} thunderstorms",
                _ => "Unknown conditions"
            };

            if (WindSpeed > 10f && WeatherType != Weather.Windy)
            {
                baseDesc += $", {WindSpeed:F0}km/h winds";
            }

            return baseDesc;
        }

        /// <summary>
        /// Check if conditions are suitable for normal play
        /// </summary>
        public bool IsSuitableForPlay()
        {
            return WeatherType switch
            {
                Weather.Storm when Intensity > 1.5f => false, // Severe storms
                Weather.Wet when Intensity > 1.8f => false,   // Heavy rain
                Weather.Windy when WindSpeed > 40f => false,  // Dangerous winds
                _ => true
            };
        }

        private void SetDefaultValuesForWeatherType()
        {
            switch (WeatherType)
            {
                case Weather.Wet:
                    Humidity = 90f;
                    Visibility = 70f;
                    break;
                case Weather.Windy:
                    WindSpeed = 25f;
                    Humidity = 60f;
                    break;
                case Weather.Hot:
                    Temperature = 35f;
                    Humidity = 40f;
                    break;
                case Weather.Cold:
                    Temperature = 8f;
                    Humidity = 70f;
                    break;
                case Weather.Storm:
                    Humidity = 95f;
                    Visibility = 50f;
                    WindSpeed = 30f;
                    break;
                default:
                    Humidity = 50f;
                    Visibility = 100f;
                    break;
            }
        }
    }

    #endregion

    #region Weather Effects

    /// <summary>
    /// Player-specific weather effects
    /// </summary>
    public class PlayerWeatherEffect
    {
        public float SpeedModifier { get; set; } = 1.0f;
        public float AccuracyModifier { get; set; } = 1.0f;
        public float EnduranceModifier { get; set; } = 1.0f;
    }
    
    /// <summary>
    /// Defines how a specific weather type affects gameplay mechanics
    /// </summary>
    public class WeatherEffects
    {
        // Basic gameplay modifiers
        public float KickingAccuracyModifier { get; set; } = 0f;
        public float BallHandlingModifier { get; set; } = 0f;
        public float FatigueRateModifier { get; set; } = 0f;
        public float VisibilityModifier { get; set; } = 0f;
        public float GroundConditionsModifier { get; set; } = 0f;

        // Marking contest modifiers
        public float OverheadMarkModifier { get; set; } = 0f;
        public float ChestMarkModifier { get; set; } = 0f;
        public float GroundBallModifier { get; set; } = 0f;

        // Specific kick type modifiers
        public float SetShotAccuracyModifier { get; set; } = 0f;
        public float LongKickModifier { get; set; } = 0f;
        public float ShortPassModifier { get; set; } = 0f;
        public float SnapKickModifier { get; set; } = 0f;

        // Contest modifiers
        public float RuckContestModifier { get; set; } = 0f;
        public float TacklingEffectivenessModifier { get; set; } = 0f;

        // Risk modifiers
        public float TurnoverRiskModifier { get; set; } = 0f;
        public float InjuryRiskModifier { get; set; } = 0f;

        // Distance penalties
        public float DistancePenaltyMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Calculate overall gameplay difficulty multiplier
        /// </summary>
        public float GetDifficultyMultiplier()
        {
            float difficulty = 1.0f;
            
            // Negative modifiers increase difficulty
            difficulty += Math.Abs(Math.Min(0f, KickingAccuracyModifier));
            difficulty += Math.Abs(Math.Min(0f, BallHandlingModifier));
            difficulty += Math.Abs(Math.Min(0f, VisibilityModifier));
            difficulty += TurnoverRiskModifier;

            return Math.Max(1.0f, difficulty);
        }
    }

    /// <summary>
    /// Weather impacts specific to different match phases
    /// </summary>
    public class PhaseWeatherImpacts
    {
        // Center bounce impacts
        public float RuckContestModifier { get; set; } = 0f;
        
        // Open play impacts
        public float PassingAccuracy { get; set; } = 0f;
        public float RunningSpeed { get; set; } = 0f;
        public float TurnoverRisk { get; set; } = 0f;
        
        // Inside 50 impacts
        public float MarkingContest { get; set; } = 0f;
        public float GroundBallAdvantage { get; set; } = 0f;
        
        // Shot on goal impacts
        public float GoalAccuracy { get; set; } = 0f;
        public float WindEffect { get; set; } = 0f;
        
        /// <summary>
        /// Check if weather has significant impact on this phase
        /// </summary>
        public bool HasSignificantImpact()
        {
            var values = new[] { 
                RuckContestModifier, PassingAccuracy, RunningSpeed, TurnoverRisk,
                MarkingContest, GroundBallAdvantage, GoalAccuracy, WindEffect 
            };
            
            return values.Any(v => Math.Abs(v) > 0.05f);
        }
    }

    #endregion

    #region Tactical Integration

    /// <summary>
    /// Tactical advice based on current weather conditions
    /// </summary>
    public class WeatherTacticalAdvice
    {
        public Weather WeatherType { get; set; }
        public float Intensity { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, float> TacticalModifiers { get; set; } = new Dictionary<string, float>();

        /// <summary>
        /// Get priority level of weather tactical advice
        /// </summary>
        public int GetPriorityLevel()
        {
            return WeatherType switch
            {
                Weather.Storm => 5, // Highest priority
                Weather.Wet when Intensity > 1.2f => 4,
                Weather.Windy when Intensity > 1.0f => 3,
                Weather.Hot when Intensity > 1.3f => 3,
                Weather.Cold when Intensity > 1.0f => 2,
                _ => 1 // Low priority
            };
        }

        /// <summary>
        /// Check if weather conditions warrant tactical changes
        /// </summary>
        public bool ShouldAdjustTactics()
        {
            return GetPriorityLevel() >= 3 || Recommendations.Count > 2;
        }
    }

    #endregion

    #region Player-Specific Effects

    /// <summary>
    /// Weather effects on individual players based on their attributes
    /// </summary>
    public class PlayerWeatherEffects
    {
        public Guid PlayerId { get; set; }
        public Dictionary<string, float> AttributeModifiers { get; set; } = new Dictionary<string, float>();
        public float OverallPerformanceModifier { get; set; } = 0f;
        public float InjuryRiskMultiplier { get; set; } = 1.0f;
        public List<string> SpecialEffects { get; set; } = new List<string>();

        /// <summary>
        /// Calculate combined weather effect on player performance
        /// </summary>
        public float GetCombinedPerformanceEffect()
        {
            float totalEffect = OverallPerformanceModifier;
            
            // Add significant attribute modifiers
            foreach (var modifier in AttributeModifiers.Values)
            {
                if (Math.Abs(modifier) > 0.1f) // Only count significant effects
                {
                    totalEffect += modifier * 0.5f; // Weight individual attributes less
                }
            }

            return totalEffect;
        }
    }

    /// <summary>
    /// Weather preference profile for players
    /// </summary>
    public class PlayerWeatherPreference
    {
        public Guid PlayerId { get; set; }
        public Dictionary<Weather, float> WeatherPreferences { get; set; } = new Dictionary<Weather, float>();
        public float TemperatureOptimum { get; set; } = 20f; // Preferred temperature
        public float TemperatureTolerance { get; set; } = 10f; // Tolerance range
        public bool IsWeatherSensitive { get; set; } = false; // Some players more affected than others

        /// <summary>
        /// Calculate weather suitability for this player
        /// </summary>
        public float CalculateWeatherSuitability(WeatherConditions conditions)
        {
            float suitability = WeatherPreferences.GetValueOrDefault(conditions.WeatherType, 0.5f);
            
            // Temperature factor
            float tempDiff = Math.Abs(conditions.Temperature - TemperatureOptimum);
            float tempFactor = Math.Max(0f, 1f - (tempDiff / (TemperatureTolerance * 2f)));
            
            // Apply sensitivity multiplier
            float sensitivity = IsWeatherSensitive ? 1.5f : 1.0f;
            
            return (suitability + tempFactor) / 2f * sensitivity;
        }
    }

    #endregion

    #region Weather Statistics

    /// <summary>
    /// Statistics tracking for weather impact during matches
    /// </summary>
    public class WeatherMatchStatistics
    {
        public Weather InitialWeather { get; set; }
        public Weather FinalWeather { get; set; }
        public List<WeatherChange> WeatherChanges { get; set; } = new List<WeatherChange>();
        
        // Performance statistics
        public float AverageKickingAccuracy { get; set; }
        public float TurnoverCount { get; set; }
        public float TotalFatigueImpact { get; set; }
        public int WeatherRelatedEvents { get; set; } // Fumbles, missed kicks due to weather
        
        // Tactical statistics
        public int TacticalAdjustmentsDueToWeather { get; set; }
        public Dictionary<string, int> WeatherInfluencedDecisions { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Calculate overall weather impact score for the match
        /// </summary>
        public float CalculateWeatherImpactScore()
        {
            float impactScore = 0f;
            
            // Base impact from weather types
            impactScore += GetWeatherImpactValue(InitialWeather);
            impactScore += GetWeatherImpactValue(FinalWeather);
            
            // Changes during match increase impact
            impactScore += WeatherChanges.Count * 0.2f;
            
            // Performance impacts
            impactScore += WeatherRelatedEvents * 0.1f;
            impactScore += TacticalAdjustmentsDueToWeather * 0.3f;
            
            return Math.Min(10f, impactScore); // Cap at 10
        }

        private float GetWeatherImpactValue(Weather weather)
        {
            return weather switch
            {
                Weather.Clear => 0f,
                Weather.Hot => 1.5f,
                Weather.Cold => 1.0f,
                Weather.Windy => 2.0f,
                Weather.Wet => 2.5f,
                Weather.Storm => 4.0f,
                _ => 0f
            };
        }
    }

    /// <summary>
    /// Record of weather changes during a match
    /// </summary>
    public class WeatherChange
    {
        public float MatchTimeSeconds { get; set; }
        public Weather PreviousWeather { get; set; }
        public Weather NewWeather { get; set; }
        public string Description { get; set; } = "";
        public float IntensityChange { get; set; } = 0f;

        public override string ToString()
        {
            return $"{MatchTimeSeconds/60:F1}min: {PreviousWeather} → {NewWeather} ({Description})";
        }
    }

    #endregion
}