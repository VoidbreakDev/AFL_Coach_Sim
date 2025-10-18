using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Weather;
using WeatherCondition = AFLCoachSim.Core.Engine.Match.Weather.Weather;

namespace AFLCoachSim.Core.Engine.Match.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating the Advanced Fatigue System integration with match simulation
    /// This shows how fatigue affects player performance, team tactics, and match outcomes
    /// </summary>
    public class FatigueSystemExample
    {
        private readonly AdvancedFatigueSystem _fatigueSystem;
        private readonly WeatherImpactSystem _weatherSystem;

        public FatigueSystemExample()
        {
            // Initialize fatigue system with deterministic seed
            _fatigueSystem = new AdvancedFatigueSystem(42); // Use seed for deterministic behavior
            _weatherSystem = new WeatherImpactSystem();
        }

        /// <summary>
        /// Example 1: Initialize fatigue system for a match with both teams
        /// </summary>
        public void InitializeFatigueSystem()
        {
            Console.WriteLine("=== Fatigue System Initialization Example ===");
            
            // Create sample teams with players
            var homeTeam = CreateSampleTeam("Melbourne Demons", isHome: true);
            var awayTeam = CreateSampleTeam("Richmond Tigers", isHome: false);
            
            // Initialize fatigue system for match
            _fatigueSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList());
            
            // Set up starting conditions  
            var weatherConditions = new WeatherConditions(WeatherCondition.Hot)
            {
                Temperature = 28f,
                Humidity = 65f, // Percentage, not decimal
                WindSpeed = 15f,
                WindDirection = WindDirection.North,
                Intensity = 0.8f // Use intensity instead of rain intensity
            };
            
            _fatigueSystem.UpdateWeatherConditions(weatherConditions);
            
            Console.WriteLine($"Fatigue system initialized for {homeTeam.Players.Count + awayTeam.Players.Count} players");
            Console.WriteLine($"Weather conditions: {weatherConditions.WeatherType}, {weatherConditions.Temperature}°C, {weatherConditions.WindSpeed} km/h wind");
            
            // Display initial player states
            foreach (var player in homeTeam.Players.Take(3))
            {
                var state = _fatigueSystem.GetPlayerFatigueState(player.Id);
                Console.WriteLine($"Player {player.Name}: {state.CurrentZone} ({state.CurrentFatigue:F1}% fatigue)");
            }
        }

        /// <summary>
        /// Example 2: Simulate fatigue during different match activities
        /// </summary>
        public void SimulateMatchActivities()
        {
            Console.WriteLine("\n=== Match Activity Simulation Example ===");
            
            var homeTeam = CreateSampleTeam("Adelaide Crows", isHome: true);
            _fatigueSystem.InitializeForMatch(homeTeam.Players.ToList(), new List<Player>());
            
            var midfielder = homeTeam.Players.First(p => p.Role == Role.Midfielder);
            var forward = homeTeam.Players.First(p => p.Role == Role.Forward);
            
            Console.WriteLine($"Tracking {midfielder.Name} (Midfielder) and {forward.Name} (Forward)");
            
            // Simulate first quarter activities
            Console.WriteLine("\nFirst Quarter Activities:");
            
            // Midfielder has more intensive activities
            _fatigueSystem.UpdateFatigue(midfielder.Id, FatigueActivity.Running, 30f);
            _fatigueSystem.UpdateFatigue(midfielder.Id, FatigueActivity.Contest, 5f);
            _fatigueSystem.UpdateFatigue(midfielder.Id, FatigueActivity.Sprinting, 8f);
            _fatigueSystem.UpdateFatigue(midfielder.Id, FatigueActivity.Tackling, 3f);
            
            // Forward has less running but more marking contests
            _fatigueSystem.UpdateFatigue(forward.Id, FatigueActivity.Walking, 45f);
            _fatigueSystem.UpdateFatigue(forward.Id, FatigueActivity.Marking, 8f);
            _fatigueSystem.UpdateFatigue(forward.Id, FatigueActivity.Sprinting, 12f);
            
            DisplayPlayerFatigueStates(new[] { midfielder, forward });
            
            // Quarter time break
            Console.WriteLine("\nQuarter Time Break Recovery:");
            _fatigueSystem.ApplyRecovery(midfielder.Id, RecoveryType.QuarterBreak, 5f * 60f); // 5 minutes
            _fatigueSystem.ApplyRecovery(forward.Id, RecoveryType.QuarterBreak, 5f * 60f);
            
            DisplayPlayerFatigueStates(new[] { midfielder, forward });
            
            // Continue with more intensive activities in second quarter
            Console.WriteLine("\nSecond Quarter - Increased Intensity:");
            
            _fatigueSystem.UpdateFatigue(midfielder.Id, FatigueActivity.Sprinting, 15f);
            _fatigueSystem.UpdateFatigue(midfielder.Id, FatigueActivity.Contest, 8f);
            _fatigueSystem.UpdateFatigue(midfielder.Id, FatigueActivity.Running, 25f);
            
            _fatigueSystem.UpdateFatigue(forward.Id, FatigueActivity.Sprinting, 18f);
            _fatigueSystem.UpdateFatigue(forward.Id, FatigueActivity.Marking, 6f);
            
            DisplayPlayerFatigueStates(new[] { midfielder, forward });
            
            // Check performance impacts
            var midfielderImpact = _fatigueSystem.CalculatePerformanceImpact(midfielder.Id);
            var forwardImpact = _fatigueSystem.CalculatePerformanceImpact(forward.Id);
            
            Console.WriteLine($"\nPerformance Impacts:");
            Console.WriteLine($"{midfielder.Name}: {midfielderImpact.GetImpactSeverity()} impact (-{midfielderImpact.SpeedReduction:P1} speed, -{midfielderImpact.AccuracyReduction:P1} accuracy)");
            Console.WriteLine($"{forward.Name}: {forwardImpact.GetImpactSeverity()} impact (-{forwardImpact.SpeedReduction:P1} speed, -{forwardImpact.AccuracyReduction:P1} accuracy)");
        }

        /// <summary>
        /// Example 3: Demonstrate team fatigue analysis and tactical recommendations
        /// </summary>
        public void AnalyzeTeamFatigue()
        {
            Console.WriteLine("\n=== Team Fatigue Analysis Example ===");
            
            var team = CreateSampleTeam("Collingwood Magpies", isHome: true);
            _fatigueSystem.InitializeForMatch(team.Players.ToList(), new List<Player>());
            
            // Simulate varied fatigue levels across the team
            var random = new Random(42);
            foreach (var player in team.Players)
            {
                // Generate different fatigue levels
                float fatigueLevel = random.Next(0, 90);
                _fatigueSystem.SetPlayerFatigue(player.Id, fatigueLevel);
            }
            
            // Analyze team fatigue
            var teamAnalysis = _fatigueSystem.AnalyzeTeamFatigue(team.Players.Select(p => (Guid)p.Id).ToList());
            
            Console.WriteLine($"Team Fatigue Analysis:");
            Console.WriteLine($"- Status: {teamAnalysis.GetTeamFatigueStatus()}");
            Console.WriteLine($"- Average Fatigue: {teamAnalysis.AverageFatigue:F1}%");
            Console.WriteLine($"- Max Fatigue: {teamAnalysis.MaxFatigue:F1}%");
            Console.WriteLine($"- Players needing substitution: {teamAnalysis.PlayersNeedingSubstitution.Count}");
            Console.WriteLine($"- Substitution Priority: {teamAnalysis.GetSubstitutionPriority()}/5");
            
            // Fatigue zone distribution
            Console.WriteLine($"\nFatigue Zone Distribution:");
            foreach (FatigueZone zone in (FatigueZone[])Enum.GetValues(typeof(FatigueZone)))
            {
                int count = teamAnalysis.ZoneDistribution.GetValueOrDefault(zone, 0);
                Console.WriteLine($"- {zone}: {count} players");
            }
            
            // Get tactical recommendations
            var recommendations = _fatigueSystem.GetTacticalRecommendations(team.Players.Select(p => (Guid)p.Id).ToList());
            
            Console.WriteLine($"\nTactical Recommendations ({recommendations.Count}):");
            foreach (var recommendation in recommendations.Take(3))
            {
                Console.WriteLine($"- {recommendation.GetFormattedRecommendation()}");
                Console.WriteLine($"  Affects {recommendation.AffectedPlayers.Count} players");
            }
        }

        /// <summary>
        /// Example 4: Weather impact on fatigue
        /// </summary>
        public void DemonstrateWeatherImpactOnFatigue()
        {
            Console.WriteLine("\n=== Weather Impact on Fatigue Example ===");
            
            var team = CreateSampleTeam("West Coast Eagles", isHome: true);
            _fatigueSystem.InitializeForMatch(team.Players.ToList(), new List<Player>());
            
            var testPlayer = team.Players.First();
            
            // Test different weather conditions
            var weatherScenarios = new[]
            {
                new WeatherConditions(WeatherCondition.Hot) { Temperature = 35f, Humidity = 80f, Intensity = 1.2f },
                new WeatherConditions(WeatherCondition.Clear) { Temperature = 15f, Humidity = 40f, Intensity = 0.5f },
                new WeatherConditions(WeatherCondition.Wet) { Temperature = 25f, Intensity = 0.6f },
                new WeatherConditions(WeatherCondition.Cold) { Temperature = 8f, WindSpeed = 25f, Intensity = 1.0f }
            };
            
            foreach (var weather in weatherScenarios)
            {
                Console.WriteLine($"\nWeather: {weather.WeatherType} ({weather.Temperature}°C, {weather.Humidity:P0} humidity)");
                
                _fatigueSystem.UpdateWeatherConditions(weather);
                
                // Reset player fatigue for fair comparison
                _fatigueSystem.SetPlayerFatigue(testPlayer.Id, 30f);
                
                // Simulate same activities under different weather
                _fatigueSystem.UpdateFatigue(testPlayer.Id, FatigueActivity.Running, 60f);
                _fatigueSystem.UpdateFatigue(testPlayer.Id, FatigueActivity.Sprinting, 10f);
                
                var state = _fatigueSystem.GetPlayerFatigueState(testPlayer.Id);
                var impact = _fatigueSystem.CalculatePerformanceImpact(testPlayer.Id);
                
                Console.WriteLine($"- Final Fatigue: {state.CurrentFatigue:F1}% ({state.CurrentZone})");
                Console.WriteLine($"- Performance Impact: {impact.GetImpactSeverity()}");
                Console.WriteLine($"- Speed Reduction: {impact.SpeedReduction:P1}");
            }
        }

        /// <summary>
        /// Example 5: Position-specific fatigue patterns
        /// </summary>
        public void ShowPositionSpecificFatigue()
        {
            Console.WriteLine("\n=== Position-Specific Fatigue Example ===");
            
            var team = CreateSampleTeam("Geelong Cats", isHome: true);
            _fatigueSystem.InitializeForMatch(team.Players.ToList(), new List<Player>());
            
            // Get one player from each position
            var playersByPosition = team.Players.GroupBy(p => p.Role).ToDictionary(g => g.Key, g => g.First());
            
            Console.WriteLine("Simulating identical activities for different positions:");
            
            // Apply same activities to all positions
            foreach (var kvp in playersByPosition)
            {
                var position = kvp.Key;
                var player = kvp.Value;
                
                // Standard activities
                _fatigueSystem.UpdateFatigue(player.Id, FatigueActivity.Running, 120f);
                _fatigueSystem.UpdateFatigue(player.Id, FatigueActivity.Contest, 15f);
                _fatigueSystem.UpdateFatigue(player.Id, FatigueActivity.Sprinting, 20f);
            }
            
            // Compare resulting fatigue levels
            Console.WriteLine("\nFatigue Results by Position:");
            foreach (var kvp in playersByPosition.OrderBy(p => p.Key.ToString()))
            {
                var position = kvp.Key;
                var player = kvp.Value;
                var state = _fatigueSystem.GetPlayerFatigueState(player.Id);
                var impact = _fatigueSystem.CalculatePerformanceImpact(player.Id);
                
                Console.WriteLine($"{position,-12}: {state.CurrentFatigue:F1}% fatigue ({state.CurrentZone}), {impact.GetImpactSeverity()} impact");
            }
            
            // Show position profiles
            Console.WriteLine("\nPosition Fatigue Profiles:");
            foreach (Role position in (Role[])Enum.GetValues(typeof(Role)))
            {
                var profile = _fatigueSystem.GetPositionProfile(position);
                if (profile != null)
                {
                    Console.WriteLine($"{position,-12}: Difficulty {profile.GetDifficultyRating()}/5, Load Factor {profile.CalculateRelativeFatigueLoad():F2}");
                }
            }
        }

        /// <summary>
        /// Example 6: Advanced fatigue recovery patterns
        /// </summary>
        public void DemonstrateRecoveryPatterns()
        {
            Console.WriteLine("\n=== Recovery Patterns Example ===");
            
            var team = CreateSampleTeam("St Kilda Saints", isHome: true);
            _fatigueSystem.InitializeForMatch(team.Players.ToList(), new List<Player>());
            
            var testPlayer = team.Players.First();
            
            // Build up significant fatigue
            _fatigueSystem.UpdateFatigue(testPlayer.Id, FatigueActivity.Sprinting, 60f);
            _fatigueSystem.UpdateFatigue(testPlayer.Id, FatigueActivity.Contest, 30f);
            
            var initialState = _fatigueSystem.GetPlayerFatigueState(testPlayer.Id);
            Console.WriteLine($"Initial Fatigue: {initialState.CurrentFatigue:F1}% ({initialState.CurrentZone})");
            
            // Test different recovery types
            var recoveryScenarios = new[]
            {
                new { Type = RecoveryType.PassiveRest, Duration = 60f, Description = "1 min passive rest" },
                new { Type = RecoveryType.ActiveRecovery, Duration = 120f, Description = "2 min active recovery" },
                new { Type = RecoveryType.QuarterBreak, Duration = 300f, Description = "5 min quarter break" },
                new { Type = RecoveryType.HalfTimeBreak, Duration = 1200f, Description = "20 min half time" }
            };
            
            foreach (var scenario in recoveryScenarios)
            {
                // Reset to initial fatigue level
                _fatigueSystem.SetPlayerFatigue(testPlayer.Id, initialState.CurrentFatigue);
                
                _fatigueSystem.ApplyRecovery(testPlayer.Id, scenario.Type, scenario.Duration);
                
                var afterState = _fatigueSystem.GetPlayerFatigueState(testPlayer.Id);
                float recovery = initialState.CurrentFatigue - afterState.CurrentFatigue;
                
                Console.WriteLine($"{scenario.Description,-20}: {recovery:F1}% recovery ({afterState.CurrentFatigue:F1}% remaining)");
            }
        }

        /// <summary>
        /// Example 7: Match statistics and analytics
        /// </summary>
        public void GenerateMatchStatistics()
        {
            Console.WriteLine("\n=== Match Statistics Example ===");
            
            var homeTeam = CreateSampleTeam("Hawthorn Hawks", isHome: true);
            var awayTeam = CreateSampleTeam("North Melbourne Kangaroos", isHome: false);
            
            _fatigueSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList());
            
            // Simulate a full match with varied activities
            var random = new Random(123);
            var allPlayers = homeTeam.Players.Concat(awayTeam.Players).ToList();
            
            // Simulate match activities for statistics
            foreach (var player in allPlayers)
            {
                var activityCount = random.Next(15, 35);
                for (int i = 0; i < activityCount; i++)
                {
                    var activity = (FatigueActivity)random.Next(0, Enum.GetValues(typeof(FatigueActivity)).Length);
                    var duration = random.Next(5, 30);
                    _fatigueSystem.UpdateFatigue(player.Id, activity, duration);
                }
                
                // Apply some recovery
                if (random.NextDouble() > 0.5)
                {
                    var recoveryType = (RecoveryType)random.Next(0, 4);
                    var recoveryDuration = random.Next(30, 180);
                    _fatigueSystem.ApplyRecovery(player.Id, recoveryType, recoveryDuration);
                }
            }
            
            // Generate and display statistics
            var matchStats = _fatigueSystem.GetMatchStatistics();
            
            Console.WriteLine($"Match Fatigue Statistics:");
            Console.WriteLine($"- Total Fatigue Generated: {matchStats.TotalFatigueGenerated:F1} points");
            Console.WriteLine($"- Total Recovery Applied: {matchStats.TotalRecoveryApplied:F1} points");
            Console.WriteLine($"- Match Fatigue Efficiency: {matchStats.CalculateMatchFatigueEfficiency():P1}");
            
            var mostFatiguing = matchStats.GetMostFatiguingActivities().Take(3);
            Console.WriteLine($"\nMost Fatiguing Activities:");
            foreach (var activity in mostFatiguing)
            {
                Console.WriteLine($"- {activity.Key}: {activity.Value:F1} total fatigue points");
            }
            
            // Player statistics
            var topFatigued = matchStats.PlayerStats.Values
                .OrderByDescending(s => s.PeakFatigue)
                .Take(3);
                
            Console.WriteLine($"\nMost Fatigued Players:");
            foreach (var playerStat in topFatigued)
            {
                var player = allPlayers.First(p => p.Id == playerStat.PlayerId);
                var rating = playerStat.CalculateFatigueManagementRating();
                Console.WriteLine($"- {player.Name}: Peak {playerStat.PeakFatigue:F1}%, Avg {playerStat.AverageFatigue:F1}%, Rating {rating:F1}/100");
            }
        }

        #region Helper Methods

        private Team CreateSampleTeam(string teamName, bool isHome)
        {
            var team = new Team(Guid.NewGuid(), teamName)
            {
                AttackRating = 75,
                DefenseRating = 72
            };

            var positions = new[]
            {
                Role.Forward, Role.Forward, Role.Forward, Role.Forward, Role.Forward, Role.Forward,
                Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder,
                Role.Defender, Role.Defender, Role.Defender, Role.Defender, Role.Defender, Role.Defender,
                Role.Ruck, Role.Ruck
            };

            var names = new[]
            {
                "Connor Smith", "Jake Wilson", "Tom Anderson", "Luke Davis", "Ben Taylor", "Matt Johnson",
                "Sam Brown", "Josh Miller", "Alex White", "Ryan Jones", "Daniel Garcia", "Michael Martinez",
                "Chris Rodriguez", "David Lewis", "James Lee", "Nick Walker", "Adam Hall", "Mark Allen",
                "Paul Young", "Steve King", "Peter Wright", "Tony Lopez"
            };

            for (int i = 0; i < Math.Min(positions.Length, names.Length); i++)
            {
                var random = new Random(i + (isHome ? 1000 : 2000));
                var player = new Player(Guid.NewGuid(), names[i], positions[i])
                {
                    Age = random.Next(18, 35),
                    Speed = random.Next(60, 95),
                    Accuracy = random.Next(65, 90),
                    Endurance = random.Next(70, 95)
                };
                
                team.AddPlayer(player);
            }

            return team;
        }

        private void DisplayPlayerFatigueStates(IEnumerable<Player> players)
        {
            foreach (var player in players)
            {
                var state = _fatigueSystem.GetPlayerFatigueState(player.Id);
                Console.WriteLine($"{player.Name}: {state.CurrentZone} ({state.CurrentFatigue:F1}% fatigue, {state.GetEnergyPercentage():F1}% energy)");
            }
        }

        #endregion
    }
}