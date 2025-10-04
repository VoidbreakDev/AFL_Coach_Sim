using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Tactics;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Weather.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating weather impact system integration
    /// </summary>
    public class WeatherImpactExample
    {
        public static void RunWeatherImpactExample()
        {
            CoreLogger.Log("=== Weather Impact System Example ===");
            
            // 1. Initialize weather system
            var weatherSystem = new WeatherImpactSystem(seed: 12345);
            
            // 2. Demonstrate different weather conditions
            DemonstrateWeatherConditions(weatherSystem);
            
            // 3. Show gameplay impact calculations
            DemonstrateGameplayImpacts(weatherSystem);
            
            // 4. Phase-specific weather effects
            DemonstratePhaseSpecificEffects(weatherSystem);
            
            // 5. Dynamic weather changes during match
            DemonstrateDynamicWeatherChanges(weatherSystem);
            
            // 6. Tactical advice from weather
            DemonstrateTacticalAdvice(weatherSystem);
            
            // 7. Integration with existing match engine
            DemonstrateMatchEngineIntegration(weatherSystem);
            
            CoreLogger.Log("\n=== Weather Impact Example Complete ===");
        }

        private static void DemonstrateWeatherConditions(WeatherImpactSystem weatherSystem)
        {
            CoreLogger.Log("\n--- WEATHER CONDITIONS EXAMPLES ---");
            
            var weatherScenarios = new[]
            {
                new { Weather = Weather.Clear, Intensity = 1.0f, Wind = 5f, Description = "Perfect conditions" },
                new { Weather = Weather.Wet, Intensity = 1.3f, Wind = 15f, Description = "Moderate rain" },
                new { Weather = Weather.Windy, Intensity = 1.1f, Wind = 32f, Description = "Strong crosswinds" },
                new { Weather = Weather.Hot, Intensity = 1.5f, Wind = 8f, Description = "Scorching heat" },
                new { Weather = Weather.Storm, Intensity = 1.8f, Wind = 45f, Description = "Severe thunderstorm" }
            };

            foreach (var scenario in weatherScenarios)
            {
                weatherSystem.SetWeatherConditions(scenario.Weather, scenario.Intensity, scenario.Wind, 
                    GetRandomWindDirection());
                
                var conditions = weatherSystem.GetCurrentConditions();
                CoreLogger.Log($"{scenario.Description}:");
                CoreLogger.Log($"  Weather: {conditions.GetDescription()}");
                CoreLogger.Log($"  Temperature: {conditions.Temperature:F0}°C, Humidity: {conditions.Humidity:F0}%");
                CoreLogger.Log($"  Suitable for play: {(conditions.IsSuitableForPlay() ? "Yes" : "NO")}");
                CoreLogger.Log("");
            }
        }

        private static void DemonstrateGameplayImpacts(WeatherImpactSystem weatherSystem)
        {
            CoreLogger.Log("\n--- GAMEPLAY IMPACT CALCULATIONS ---");
            
            // Test different weather conditions for kicking accuracy
            var kickScenarios = new[]
            {
                new { Weather = Weather.Clear, KickType = KickType.SetShot, Distance = 35f },
                new { Weather = Weather.Wet, KickType = KickType.SetShot, Distance = 35f },
                new { Weather = Weather.Windy, KickType = KickType.LongKick, Distance = 55f },
                new { Weather = Weather.Storm, KickType = KickType.ShortPass, Distance = 15f }
            };

            foreach (var scenario in kickScenarios)
            {
                weatherSystem.SetWeatherConditions(scenario.Weather, 1.2f, 25f, WindDirection.North);
                
                float accuracy = weatherSystem.CalculateKickingAccuracyModifier(scenario.KickType, scenario.Distance);
                float handling = weatherSystem.CalculateBallHandlingModifier();
                float fatigue = weatherSystem.CalculateFatigueRateModifier();
                float visibility = weatherSystem.CalculateVisibilityModifier();

                CoreLogger.Log($"{scenario.Weather} conditions impact on {scenario.KickType} ({scenario.Distance}m):");
                if (Math.Abs(accuracy) > 0.01f)
                    CoreLogger.Log($"  Kicking accuracy: {accuracy:P1}");
                if (Math.Abs(handling) > 0.01f)
                    CoreLogger.Log($"  Ball handling: {handling:P1}");
                if (Math.Abs(fatigue) > 0.01f)
                    CoreLogger.Log($"  Fatigue rate: {fatigue:+P1}");
                if (Math.Abs(visibility) > 0.01f)
                    CoreLogger.Log($"  Visibility: {visibility:P1}");

                // Marking contests
                float overheadMark = weatherSystem.CalculateMarkingModifier(MarkingType.Overhead);
                float groundBall = weatherSystem.CalculateMarkingModifier(MarkingType.Ground);
                
                if (Math.Abs(overheadMark) > 0.01f)
                    CoreLogger.Log($"  Overhead marks: {overheadMark:P1}");
                if (Math.Abs(groundBall) > 0.01f)
                    CoreLogger.Log($"  Ground balls: {groundBall:P1}");
                
                CoreLogger.Log("");
            }
        }

        private static void DemonstratePhaseSpecificEffects(WeatherImpactSystem weatherSystem)
        {
            CoreLogger.Log("\n--- PHASE-SPECIFIC WEATHER EFFECTS ---");
            
            weatherSystem.SetWeatherConditions(Weather.Wet, 1.4f, 20f, WindDirection.SouthWest);
            
            var phases = new[] { Phase.CenterBounce, Phase.OpenPlay, Phase.Inside50, Phase.ShotOnGoal };
            
            foreach (var phase in phases)
            {
                var impacts = weatherSystem.CalculatePhaseImpacts(phase);
                
                CoreLogger.Log($"{phase} phase in wet conditions:");
                
                if (Math.Abs(impacts.RuckContestModifier) > 0.01f)
                    CoreLogger.Log($"  Ruck contest: {impacts.RuckContestModifier:P1}");
                if (Math.Abs(impacts.PassingAccuracy) > 0.01f)
                    CoreLogger.Log($"  Passing accuracy: {impacts.PassingAccuracy:P1}");
                if (Math.Abs(impacts.RunningSpeed) > 0.01f)
                    CoreLogger.Log($"  Running speed: {impacts.RunningSpeed:P1}");
                if (Math.Abs(impacts.TurnoverRisk) > 0.01f)
                    CoreLogger.Log($"  Turnover risk: +{impacts.TurnoverRisk:P1}");
                if (Math.Abs(impacts.MarkingContest) > 0.01f)
                    CoreLogger.Log($"  Marking contests: {impacts.MarkingContest:P1}");
                if (Math.Abs(impacts.GroundBallAdvantage) > 0.01f)
                    CoreLogger.Log($"  Ground ball advantage: {impacts.GroundBallAdvantage:P1}");
                if (Math.Abs(impacts.GoalAccuracy) > 0.01f)
                    CoreLogger.Log($"  Goal accuracy: {impacts.GoalAccuracy:P1}");
                if (Math.Abs(impacts.WindEffect) > 0.01f)
                    CoreLogger.Log($"  Wind effect: {impacts.WindEffect:P1}");

                if (impacts.HasSignificantImpact())
                    CoreLogger.Log($"  → Significant weather impact on this phase");
                else
                    CoreLogger.Log($"  → Minimal weather impact");
                    
                CoreLogger.Log("");
            }
        }

        private static void DemonstrateDynamicWeatherChanges(WeatherImpactSystem weatherSystem)
        {
            CoreLogger.Log("\n--- DYNAMIC WEATHER CHANGES ---");
            
            // Start with clear conditions
            weatherSystem.SetWeatherConditions(Weather.Clear, 1.0f);
            CoreLogger.Log("Match starts with clear conditions");
            
            // Simulate match progression and weather updates
            var matchTimes = new[] { 900f, 1800f, 2700f, 4200f, 5400f }; // Various times during match
            
            foreach (var time in matchTimes)
            {
                weatherSystem.UpdateWeatherConditions(time);
                var conditions = weatherSystem.GetCurrentConditions();
                
                CoreLogger.Log($"At {time/60:F0} minutes: {conditions.GetDescription()}");
                
                // Show tactical advice if weather has changed significantly
                var tacticalAdvice = weatherSystem.GetTacticalAdvice();
                if (tacticalAdvice.ShouldAdjustTactics())
                {
                    CoreLogger.Log($"  Tactical priority level: {tacticalAdvice.GetPriorityLevel()}/5");
                    foreach (var recommendation in tacticalAdvice.Recommendations.Take(2))
                    {
                        CoreLogger.Log($"  → {recommendation}");
                    }
                }
            }
        }

        private static void DemonstrateTacticalAdvice(WeatherImpactSystem weatherSystem)
        {
            CoreLogger.Log("\n--- TACTICAL ADVICE FROM WEATHER ---");
            
            var tacticalScenarios = new[]
            {
                new { Weather = Weather.Wet, Intensity = 1.6f, Description = "Heavy rain" },
                new { Weather = Weather.Windy, Intensity = 1.3f, Description = "Strong winds" },
                new { Weather = Weather.Hot, Intensity = 1.7f, Description = "Extreme heat" },
                new { Weather = Weather.Storm, Intensity = 1.9f, Description = "Severe storm" }
            };

            foreach (var scenario in tacticalScenarios)
            {
                weatherSystem.SetWeatherConditions(scenario.Weather, scenario.Intensity, 28f, WindDirection.West);
                
                var advice = weatherSystem.GetTacticalAdvice();
                
                CoreLogger.Log($"{scenario.Description} conditions:");
                CoreLogger.Log($"  Priority level: {advice.GetPriorityLevel()}/5");
                CoreLogger.Log($"  Should adjust tactics: {(advice.ShouldAdjustTactics() ? "YES" : "No")}");
                
                if (advice.Recommendations.Any())
                {
                    CoreLogger.Log("  Recommendations:");
                    foreach (var recommendation in advice.Recommendations)
                    {
                        CoreLogger.Log($"    • {recommendation}");
                    }
                }
                CoreLogger.Log("");
            }
        }

        private static void DemonstrateMatchEngineIntegration(WeatherImpactSystem weatherSystem)
        {
            CoreLogger.Log("\n--- MATCH ENGINE INTEGRATION ---");
            
            CoreLogger.Log("How to integrate weather system with existing MatchEngine:");
            CoreLogger.Log("");
            CoreLogger.Log("1. Initialize weather system at match start:");
            CoreLogger.Log("   var weatherSystem = new WeatherImpactSystem(matchSeed);");
            CoreLogger.Log("   weatherSystem.SetWeatherConditions(matchWeather, intensity);");
            CoreLogger.Log("");
            
            CoreLogger.Log("2. Update weather during match simulation:");
            CoreLogger.Log("   // In your match loop");
            CoreLogger.Log("   weatherSystem.UpdateWeatherConditions(elapsedTime);");
            CoreLogger.Log("");
            
            CoreLogger.Log("3. Apply weather effects to gameplay calculations:");
            
            // Example integration code
            weatherSystem.SetWeatherConditions(Weather.Wet, 1.2f, 15f);
            
            float setShotModifier = weatherSystem.CalculateKickingAccuracyModifier(KickType.SetShot, 40f);
            float fatigueModifier = weatherSystem.CalculateFatigueRateModifier();
            float handlingModifier = weatherSystem.CalculateBallHandlingModifier();
            
            CoreLogger.Log($"   // Example: Set shot in wet conditions");
            CoreLogger.Log($"   float baseAccuracy = 0.75f;");
            CoreLogger.Log($"   float weatherModifier = {setShotModifier:F3}f; // From weather system");
            CoreLogger.Log($"   float finalAccuracy = baseAccuracy + weatherModifier;");
            CoreLogger.Log($"   // Result: {0.75f + setShotModifier:P1} accuracy");
            CoreLogger.Log("");
            
            CoreLogger.Log($"   // Player fatigue in wet conditions");
            CoreLogger.Log($"   float baseFatigueRate = 0.02f;");
            CoreLogger.Log($"   float weatherFatigueModifier = {fatigueModifier:F3}f;");
            CoreLogger.Log($"   float adjustedFatigueRate = baseFatigueRate * (1 + weatherFatigueModifier);");
            CoreLogger.Log($"   // Result: {0.02f * (1 + fatigueModifier):F4}f fatigue per second");
            CoreLogger.Log("");
            
            CoreLogger.Log("4. Get phase-specific impacts:");
            CoreLogger.Log("   var phaseImpacts = weatherSystem.CalculatePhaseImpacts(currentPhase);");
            CoreLogger.Log("   // Apply phaseImpacts to relevant calculations");
            CoreLogger.Log("");
            
            CoreLogger.Log("5. Integrate with tactical system:");
            CoreLogger.Log("   var weatherAdvice = weatherSystem.GetTacticalAdvice();");
            CoreLogger.Log("   if (weatherAdvice.ShouldAdjustTactics()) {");
            CoreLogger.Log("       // Consider weather in AI tactical decisions");
            CoreLogger.Log("   }");
        }

        #region Helper Methods

        private static WindDirection GetRandomWindDirection()
        {
            var directions = new[] 
            { 
                WindDirection.North, WindDirection.NorthEast, WindDirection.East, WindDirection.SouthEast,
                WindDirection.South, WindDirection.SouthWest, WindDirection.West, WindDirection.NorthWest
            };
            var random = new Random();
            return directions[random.Next(directions.Length)];
        }

        #endregion

        public static void Main()
        {
            RunWeatherImpactExample();
            
            CoreLogger.Log("\nThe Weather Impact System provides:");
            CoreLogger.Log("• Realistic weather effects on all gameplay mechanics");
            CoreLogger.Log("• Dynamic weather changes during matches");
            CoreLogger.Log("• Phase-specific weather impacts (center bounce, open play, etc.)");
            CoreLogger.Log("• Detailed wind effects including direction and strength");
            CoreLogger.Log("• Temperature-based fatigue modeling");
            CoreLogger.Log("• Tactical advice based on weather conditions");
            CoreLogger.Log("• Integration with existing tactical systems");
            CoreLogger.Log("• Comprehensive weather statistics tracking");
            CoreLogger.Log("• Player-specific weather preferences and effects");
        }
    }
}