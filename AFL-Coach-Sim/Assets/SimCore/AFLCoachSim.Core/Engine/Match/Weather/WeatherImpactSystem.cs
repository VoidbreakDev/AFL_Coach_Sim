using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Tactics;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Weather
{
    /// <summary>
    /// Comprehensive system for modeling weather impacts on match gameplay
    /// </summary>
    public class WeatherImpactSystem
    {
        private readonly Dictionary<Weather, WeatherEffects> _weatherEffects;
        private readonly Random _random;
        private WeatherConditions _currentConditions;

        public WeatherImpactSystem(int seed = 0)
        {
            _random = seed == 0 ? new Random() : new Random(seed);
            _weatherEffects = InitializeWeatherEffects();
            _currentConditions = new WeatherConditions(Weather.Clear);
        }

        #region Weather Condition Management

        /// <summary>
        /// Set current weather conditions for the match
        /// </summary>
        public void SetWeatherConditions(Weather weather, float intensity = 1.0f, float windSpeed = 0f, 
            WindDirection windDirection = WindDirection.None)
        {
            _currentConditions = new WeatherConditions(weather)
            {
                Intensity = Math.Max(0.1f, Math.Min(2.0f, intensity)),
                WindSpeed = Math.Max(0f, Math.Min(50f, windSpeed)), // 0-50 km/h
                WindDirection = windDirection,
                Temperature = GenerateTemperatureForWeather(weather)
            };

            CoreLogger.Log($"[Weather] Conditions set: {weather} (Intensity: {intensity:F1}, Wind: {windSpeed:F0}km/h {windDirection})");
        }

        /// <summary>
        /// Update weather conditions during match (weather can change)
        /// </summary>
        public void UpdateWeatherConditions(float matchTimeSeconds)
        {
            // Weather can evolve during the match
            if (ShouldWeatherChange(matchTimeSeconds))
            {
                var newWeather = GenerateWeatherChange(_currentConditions.WeatherType);
                if (newWeather != _currentConditions.WeatherType)
                {
                    SetWeatherConditions(newWeather, 
                        _random.NextSingle() * 1.5f + 0.5f, // 0.5-2.0 intensity
                        _random.NextSingle() * 30f, // 0-30 km/h wind
                        GetRandomWindDirection());

                    CoreLogger.Log($"[Weather] Conditions changed to {newWeather} at {matchTimeSeconds:F0}s");
                }
            }

            // Update intensity and wind variations
            UpdateWeatherIntensity();
        }

        /// <summary>
        /// Get current weather conditions
        /// </summary>
        public WeatherConditions GetCurrentConditions()
        {
            return _currentConditions;
        }

        #endregion

        #region Gameplay Impact Calculations

        /// <summary>
        /// Calculate kicking accuracy modifier based on weather
        /// </summary>
        public float CalculateKickingAccuracyModifier(KickType kickType, float distance, 
            WindDirection kickDirection = WindDirection.None)
        {
            var effects = _weatherEffects[_currentConditions.WeatherType];
            float baseModifier = effects.KickingAccuracyModifier * _currentConditions.Intensity;

            // Distance penalty increases with bad weather
            float distancePenalty = (distance / 50f) * effects.DistancePenaltyMultiplier * _currentConditions.Intensity;
            baseModifier -= distancePenalty;

            // Wind effects
            float windModifier = CalculateWindEffect(kickDirection, kickType, distance);
            
            // Kick type specific effects
            float kickTypeModifier = kickType switch
            {
                KickType.SetShot => effects.SetShotAccuracyModifier,
                KickType.Snap => effects.SnapKickModifier,
                KickType.LongKick => effects.LongKickModifier,
                KickType.ShortPass => effects.ShortPassModifier,
                _ => 0f
            };

            float totalModifier = baseModifier + windModifier + (kickTypeModifier * _currentConditions.Intensity);
            
            // Log significant weather impacts
            if (Math.Abs(totalModifier) > 0.1f)
            {
                CoreLogger.Log($"[Weather] {kickType} accuracy modifier: {totalModifier:P1} (Base: {baseModifier:P1}, Wind: {windModifier:P1})");
            }

            return totalModifier;
        }

        /// <summary>
        /// Calculate marking contest modifier
        /// </summary>
        public float CalculateMarkingModifier(MarkingType markingType)
        {
            var effects = _weatherEffects[_currentConditions.WeatherType];
            
            return markingType switch
            {
                MarkingType.Overhead => effects.OverheadMarkModifier * _currentConditions.Intensity,
                MarkingType.Chest => effects.ChestMarkModifier * _currentConditions.Intensity,
                MarkingType.Ground => effects.GroundBallModifier * _currentConditions.Intensity,
                _ => 0f
            };
        }

        /// <summary>
        /// Calculate ball handling modifier
        /// </summary>
        public float CalculateBallHandlingModifier()
        {
            var effects = _weatherEffects[_currentConditions.WeatherType];
            return effects.BallHandlingModifier * _currentConditions.Intensity;
        }

        /// <summary>
        /// Calculate player fatigue rate modifier
        /// </summary>
        public float CalculateFatigueRateModifier()
        {
            var effects = _weatherEffects[_currentConditions.WeatherType];
            float temperatureEffect = CalculateTemperatureFatigueEffect();
            
            return (effects.FatigueRateModifier + temperatureEffect) * _currentConditions.Intensity;
        }

        /// <summary>
        /// Calculate visibility modifier affecting player decision making
        /// </summary>
        public float CalculateVisibilityModifier()
        {
            var effects = _weatherEffects[_currentConditions.WeatherType];
            return effects.VisibilityModifier * _currentConditions.Intensity;
        }

        /// <summary>
        /// Calculate ground conditions effect on running and agility
        /// </summary>
        public float CalculateGroundConditionsModifier()
        {
            var effects = _weatherEffects[_currentConditions.WeatherType];
            return effects.GroundConditionsModifier * _currentConditions.Intensity;
        }

        /// <summary>
        /// Get tactical recommendations based on weather conditions
        /// </summary>
        public WeatherTacticalAdvice GetTacticalAdvice()
        {
            var effects = _weatherEffects[_currentConditions.WeatherType];
            var advice = new WeatherTacticalAdvice
            {
                WeatherType = _currentConditions.WeatherType,
                Intensity = _currentConditions.Intensity
            };

            // Generate tactical recommendations
            if (_currentConditions.WeatherType == Weather.Wet)
            {
                advice.Recommendations.Add("Consider shorter passing game to reduce turnovers");
                advice.Recommendations.Add("Focus on ground ball work and tackling pressure");
                advice.Recommendations.Add("Avoid long kicks into forward 50");
                if (_currentConditions.Intensity > 1.2f)
                    advice.Recommendations.Add("Weather is severe - expect high turnover rate");
            }
            else if (_currentConditions.WeatherType == Weather.Windy)
            {
                advice.Recommendations.Add($"Strong {_currentConditions.WindDirection} wind affects kicking");
                advice.Recommendations.Add("Use wind advantage when kicking with the breeze");
                advice.Recommendations.Add("Consider wind direction for set shots");
                if (_currentConditions.WindSpeed > 25f)
                    advice.Recommendations.Add("Gale force winds - avoid long kicking");
            }
            else if (_currentConditions.WeatherType == Weather.Hot)
            {
                advice.Recommendations.Add("Rotate players more frequently to manage heat");
                advice.Recommendations.Add("Monitor player fatigue levels closely");
                advice.Recommendations.Add("Consider hydration breaks if extreme");
            }

            return advice;
        }

        #endregion

        #region Phase-Specific Weather Effects

        /// <summary>
        /// Calculate weather impacts specific to match phases
        /// </summary>
        public PhaseWeatherImpacts CalculatePhaseImpacts(Phase currentPhase)
        {
            var impacts = new PhaseWeatherImpacts();
            var effects = _weatherEffects[_currentConditions.WeatherType];

            switch (currentPhase)
            {
                case Phase.CenterBounce:
                    impacts.RuckContestModifier = effects.RuckContestModifier * _currentConditions.Intensity;
                    impacts.GroundBallAdvantage = effects.GroundBallModifier * _currentConditions.Intensity;
                    break;

                case Phase.OpenPlay:
                    impacts.PassingAccuracy = CalculateKickingAccuracyModifier(KickType.ShortPass, 25f);
                    impacts.RunningSpeed = CalculateGroundConditionsModifier();
                    impacts.TurnoverRisk = effects.TurnoverRiskModifier * _currentConditions.Intensity;
                    break;

                case Phase.Inside50:
                    impacts.MarkingContest = CalculateMarkingModifier(MarkingType.Overhead);
                    impacts.GroundBallAdvantage = effects.GroundBallModifier * _currentConditions.Intensity;
                    break;

                case Phase.ShotOnGoal:
                    impacts.GoalAccuracy = CalculateKickingAccuracyModifier(KickType.SetShot, 35f);
                    impacts.WindEffect = CalculateWindEffect(WindDirection.None, KickType.SetShot, 35f);
                    break;
            }

            return impacts;
        }

        #endregion

        #region Private Helper Methods

        private Dictionary<Weather, WeatherEffects> InitializeWeatherEffects()
        {
            return new Dictionary<Weather, WeatherEffects>
            {
                [Weather.Clear] = new WeatherEffects
                {
                    KickingAccuracyModifier = 0f,
                    BallHandlingModifier = 0f,
                    FatigueRateModifier = 0f,
                    VisibilityModifier = 0f,
                    GroundConditionsModifier = 0f
                },

                [Weather.Wet] = new WeatherEffects
                {
                    KickingAccuracyModifier = -0.15f,
                    BallHandlingModifier = -0.20f,
                    OverheadMarkModifier = -0.12f,
                    ChestMarkModifier = -0.08f,
                    GroundBallModifier = 0.08f, // Easier to pick up wet ball from ground
                    FatigueRateModifier = 0.10f,
                    TurnoverRiskModifier = 0.25f,
                    DistancePenaltyMultiplier = 1.5f,
                    SetShotAccuracyModifier = -0.18f,
                    LongKickModifier = -0.25f,
                    ShortPassModifier = -0.08f
                },

                [Weather.Windy] = new WeatherEffects
                {
                    KickingAccuracyModifier = -0.10f,
                    OverheadMarkModifier = -0.15f,
                    ChestMarkModifier = -0.05f,
                    VisibilityModifier = -0.05f,
                    SetShotAccuracyModifier = -0.12f,
                    LongKickModifier = -0.20f,
                    SnapKickModifier = -0.15f,
                    DistancePenaltyMultiplier = 1.3f,
                    RuckContestModifier = -0.08f
                },

                [Weather.Hot] = new WeatherEffects
                {
                    FatigueRateModifier = 0.30f,
                    GroundConditionsModifier = -0.05f, // Harder ground
                    KickingAccuracyModifier = -0.03f, // Slight concentration loss
                    BallHandlingModifier = -0.05f // Sweaty hands
                },

                [Weather.Cold] = new WeatherEffects
                {
                    BallHandlingModifier = -0.10f, // Cold hands
                    KickingAccuracyModifier = -0.05f,
                    FatigueRateModifier = -0.08f, // Less heat stress
                    GroundConditionsModifier = -0.03f // Firmer ground
                },

                [Weather.Storm] = new WeatherEffects
                {
                    KickingAccuracyModifier = -0.25f,
                    BallHandlingModifier = -0.30f,
                    OverheadMarkModifier = -0.25f,
                    ChestMarkModifier = -0.15f,
                    GroundBallModifier = -0.10f,
                    VisibilityModifier = -0.20f,
                    FatigueRateModifier = 0.15f,
                    TurnoverRiskModifier = 0.40f,
                    GroundConditionsModifier = -0.15f,
                    SetShotAccuracyModifier = -0.30f,
                    LongKickModifier = -0.35f,
                    DistancePenaltyMultiplier = 2.0f
                }
            };
        }

        private float CalculateWindEffect(WindDirection kickDirection, KickType kickType, float distance)
        {
            if (_currentConditions.WindSpeed < 5f) return 0f; // No significant wind

            float windStrength = _currentConditions.WindSpeed / 50f; // Normalize to 0-1
            float baseEffect = 0f;

            // Wind direction effects (simplified - in reality this would be more complex)
            if (kickDirection != WindDirection.None && _currentConditions.WindDirection != WindDirection.None)
            {
                if (kickDirection == _currentConditions.WindDirection) // Kicking with wind
                    baseEffect = windStrength * 0.15f; // Helpful
                else if (AreOppositeDirections(kickDirection, _currentConditions.WindDirection)) // Kicking against wind
                    baseEffect = -windStrength * 0.20f; // Hindrance
                else // Cross wind
                    baseEffect = -windStrength * 0.10f; // Slight hindrance
            }
            else
            {
                // General wind effect when direction isn't specified
                baseEffect = -windStrength * 0.08f;
            }

            // Distance amplifies wind effects
            float distanceMultiplier = Math.Min(2.0f, distance / 30f); // Up to 2x for long kicks
            
            // Kick type sensitivity to wind
            float kickTypeMultiplier = kickType switch
            {
                KickType.LongKick => 1.3f,
                KickType.SetShot => 1.1f,
                KickType.Snap => 0.7f, // Less affected due to lower trajectory
                KickType.ShortPass => 0.5f,
                _ => 1.0f
            };

            return baseEffect * distanceMultiplier * kickTypeMultiplier;
        }

        private float CalculateTemperatureFatigueEffect()
        {
            float optimalTemp = 20f; // Celsius
            float tempDifference = Math.Abs(_currentConditions.Temperature - optimalTemp);
            
            return tempDifference switch
            {
                < 5f => 0f, // Minimal effect
                < 10f => 0.05f, // Slight effect
                < 15f => 0.10f, // Moderate effect
                < 20f => 0.20f, // Significant effect
                _ => 0.30f // Extreme effect
            };
        }

        private bool ShouldWeatherChange(float matchTimeSeconds)
        {
            // Weather change probability increases over time, but is still rare
            float changeChance = (matchTimeSeconds / 6000f) * 0.02f; // Max 2% chance per update
            return _random.NextSingle() < changeChance;
        }

        private Weather GenerateWeatherChange(Weather currentWeather)
        {
            // Define logical weather transitions
            var transitions = new Dictionary<Weather, Weather[]>
            {
                [Weather.Clear] = new[] { Weather.Windy, Weather.Hot, Weather.Cold },
                [Weather.Windy] = new[] { Weather.Clear, Weather.Wet, Weather.Storm },
                [Weather.Wet] = new[] { Weather.Clear, Weather.Storm },
                [Weather.Hot] = new[] { Weather.Clear, Weather.Storm },
                [Weather.Cold] = new[] { Weather.Clear, Weather.Wet },
                [Weather.Storm] = new[] { Weather.Wet, Weather.Windy }
            };

            var possibleTransitions = transitions.GetValueOrDefault(currentWeather, new[] { Weather.Clear });
            return possibleTransitions[_random.Next(possibleTransitions.Length)];
        }

        private void UpdateWeatherIntensity()
        {
            // Small random variations in intensity
            float variation = (_random.NextSingle() - 0.5f) * 0.1f;
            _currentConditions.Intensity = Math.Max(0.1f, Math.Min(2.0f, _currentConditions.Intensity + variation));
            
            // Small random variations in wind speed
            float windVariation = (_random.NextSingle() - 0.5f) * 5f;
            _currentConditions.WindSpeed = Math.Max(0f, Math.Min(50f, _currentConditions.WindSpeed + windVariation));
        }

        private WindDirection GetRandomWindDirection()
        {
            var directions = Enum.GetValues(typeof(WindDirection)).Cast<WindDirection>()
                               .Where(d => d != WindDirection.None).ToArray();
            return directions[_random.Next(directions.Length)];
        }

        private float GenerateTemperatureForWeather(Weather weather)
        {
            return weather switch
            {
                Weather.Hot => _random.Next(28, 42), // 28-42°C
                Weather.Cold => _random.Next(2, 15), // 2-15°C
                Weather.Storm => _random.Next(12, 25), // 12-25°C
                Weather.Wet => _random.Next(8, 22), // 8-22°C
                Weather.Windy => _random.Next(10, 28), // 10-28°C
                _ => _random.Next(15, 25) // 15-25°C for clear
            };
        }

        private bool AreOppositeDirections(WindDirection dir1, WindDirection dir2)
        {
            return (dir1, dir2) switch
            {
                (WindDirection.North, WindDirection.South) or (WindDirection.South, WindDirection.North) => true,
                (WindDirection.East, WindDirection.West) or (WindDirection.West, WindDirection.East) => true,
                (WindDirection.NorthEast, WindDirection.SouthWest) or (WindDirection.SouthWest, WindDirection.NorthEast) => true,
                (WindDirection.NorthWest, WindDirection.SouthEast) or (WindDirection.SouthEast, WindDirection.NorthWest) => true,
                _ => false
            };
        }

        #endregion
    }
}