using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Ratings;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Weather;

namespace AFLCoachSim.Core.Engine.Match.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating the Dynamic Ratings System integration with match simulation
    /// This shows how player ratings change in real-time based on form, fatigue, pressure, matchups, weather, and momentum
    /// </summary>
    public class DynamicRatingsExample
    {
        private readonly DynamicRatingsSystem _ratingsSystem;
        private readonly AdvancedFatigueSystem _fatigueSystem;
        private readonly WeatherImpactSystem _weatherSystem;

        public DynamicRatingsExample()
        {
            // Initialize systems with different configurations for demonstration
            var ratingsConfig = DynamicRatingsConfiguration.CreateResponsive(); // More dynamic for example
            var fatigueConfig = FatigueConfiguration.CreateDefault();
            
            _ratingsSystem = new DynamicRatingsSystem(ratingsConfig);
            _fatigueSystem = new AdvancedFatigueSystem(fatigueConfig);
            _weatherSystem = new WeatherImpactSystem();
        }

        /// <summary>
        /// Example 1: Initialize and demonstrate basic rating calculations
        /// </summary>
        public void InitializeAndDemonstrateBasics()
        {
            Console.WriteLine("=== Dynamic Ratings System Initialization Example ===");
            
            // Create teams
            var homeTeam = CreateSampleTeam("Carlton Blues", isHome: true);
            var awayTeam = CreateSampleTeam("Essendon Bombers", isHome: false);
            
            // Create match context
            var matchContext = new MatchContext
            {
                MatchId = Guid.NewGuid(),
                HomeTeam = homeTeam.Name,
                AwayTeam = awayTeam.Name,
                Venue = "MCG",
                CrowdSize = 85000,
                IsNightGame = false,
                IsFinalSeries = false,
                MatchStart = DateTime.Now
            };

            // Initialize systems
            _fatigueSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList());
            _ratingsSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList(), 
                matchContext, _fatigueSystem, _weatherSystem);

            Console.WriteLine($"Systems initialized for match: {homeTeam.Name} vs {awayTeam.Name}");
            Console.WriteLine($"Venue: {matchContext.Venue}, Crowd: {matchContext.CrowdSize:N0}");

            // Show initial player ratings
            var testPlayer = homeTeam.Players.First();
            var initialRating = _ratingsSystem.GetPlayerRating(testPlayer.Id);
            var initialForm = _ratingsSystem.GetPlayerForm(testPlayer.Id);

            Console.WriteLine($"\nInitial State for {testPlayer.Name} ({testPlayer.Role}):");
            Console.WriteLine($"- Base Ratings: Speed {initialRating.BaseSpeed:F1}, Accuracy {initialRating.BaseAccuracy:F1}, Endurance {initialRating.BaseEndurance:F1}");
            Console.WriteLine($"- Current Ratings: Speed {initialRating.CurrentSpeed:F1}, Accuracy {initialRating.CurrentAccuracy:F1}, Endurance {initialRating.CurrentEndurance:F1}");
            Console.WriteLine($"- Form: {initialForm.CurrentForm:F1}, Confidence: {initialForm.Confidence:F1} ({initialForm.GetConfidenceLevel()})");
            Console.WriteLine($"- Overall Rating: {initialRating.GetCurrentPerformanceRating():F1}");
        }

        /// <summary>
        /// Example 2: Demonstrate how performance events affect ratings
        /// </summary>
        public void DemonstratePerformanceImpact()
        {
            Console.WriteLine("\n=== Performance Event Impact Example ===");
            
            var homeTeam = CreateSampleTeam("Western Bulldogs", isHome: true);
            var awayTeam = CreateSampleTeam("GWS Giants", isHome: false);
            var matchContext = CreateSampleMatchContext(homeTeam.Name, awayTeam.Name);

            _ratingsSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList(), matchContext);

            var testPlayer = homeTeam.Players.First(p => p.Role == Role.Forward);
            var beforeRating = _ratingsSystem.GetPlayerRating(testPlayer.Id);
            var beforeForm = _ratingsSystem.GetPlayerForm(testPlayer.Id);

            Console.WriteLine($"Testing performance impacts for {testPlayer.Name} (Forward):");
            Console.WriteLine($"Before: Form {beforeForm.CurrentForm:F1}, Confidence {beforeForm.Confidence:F1}");

            // Simulate a series of positive events
            var positiveEvents = new[]
            {
                new PerformanceEvent(PerformanceEventType.Goal) { Quality = 1.5f },
                new PerformanceEvent(PerformanceEventType.Mark) { Quality = 1.2f },
                new PerformanceEvent(PerformanceEventType.Assist) { Quality = 1.0f }
            };

            Console.WriteLine("\nApplying positive performance events:");
            foreach (var evt in positiveEvents)
            {
                _ratingsSystem.UpdatePlayerForm(testPlayer.Id, evt);
                Console.WriteLine($"- {evt.EventType} (Quality: {evt.Quality:F1})");
            }

            // Update ratings based on new form
            var updateContext = new RatingUpdateContext
            {
                Quarter = 1,
                TimeRemaining = 1200f, // 20 minutes
                ScoreDifferential = 6f, // Home leading by a goal
                CrowdSize = matchContext.CrowdSize
            };

            _ratingsSystem.UpdatePlayerRatings(testPlayer.Id, updateContext);

            var afterRating = _ratingsSystem.GetPlayerRating(testPlayer.Id);
            var afterForm = _ratingsSystem.GetPlayerForm(testPlayer.Id);

            Console.WriteLine($"\nAfter positive events:");
            Console.WriteLine($"- Form: {beforeForm.CurrentForm:F1} → {afterForm.CurrentForm:F1} ({afterForm.CurrentForm - beforeForm.CurrentForm:+F1})");
            Console.WriteLine($"- Confidence: {beforeForm.Confidence:F1} → {afterForm.Confidence:F1} ({afterForm.Confidence - beforeForm.Confidence:+F1})");
            Console.WriteLine($"- Overall Rating: {beforeRating.GetCurrentPerformanceRating():F1} → {afterRating.GetCurrentPerformanceRating():F1} ({afterRating.GetRelativePerformanceRating():+F1})");
            Console.WriteLine($"- Form Trend: {afterForm.GetFormTrend()}");

            // Now apply negative events
            Console.WriteLine("\nApplying negative performance events:");
            var negativeEvents = new[]
            {
                new PerformanceEvent(PerformanceEventType.Turnover) { Quality = 0.5f },
                new PerformanceEvent(PerformanceEventType.MissedShot) { Quality = 0.8f },
                new PerformanceEvent(PerformanceEventType.FreeKickAgainst) { Quality = 1.0f }
            };

            foreach (var evt in negativeEvents)
            {
                _ratingsSystem.UpdatePlayerForm(testPlayer.Id, evt);
                Console.WriteLine($"- {evt.EventType} (Quality: {evt.Quality:F1})");
            }

            _ratingsSystem.UpdatePlayerRatings(testPlayer.Id, updateContext);

            var finalRating = _ratingsSystem.GetPlayerRating(testPlayer.Id);
            var finalForm = _ratingsSystem.GetPlayerForm(testPlayer.Id);

            Console.WriteLine($"\nAfter negative events:");
            Console.WriteLine($"- Form: {afterForm.CurrentForm:F1} → {finalForm.CurrentForm:F1} ({finalForm.CurrentForm - afterForm.CurrentForm:+F1})");
            Console.WriteLine($"- Confidence: {afterForm.Confidence:F1} → {finalForm.Confidence:F1} ({finalForm.Confidence - afterForm.Confidence:+F1})");
            Console.WriteLine($"- Overall Rating: {afterRating.GetCurrentPerformanceRating():F1} → {finalRating.GetCurrentPerformanceRating():F1}");
            Console.WriteLine($"- Form Trend: {finalForm.GetFormTrend()}");

            // Show modifier breakdown
            if (finalRating.LastUpdateModifiers != null)
            {
                var mostSignificant = finalRating.LastUpdateModifiers.GetMostSignificantModifier();
                Console.WriteLine($"- Most significant modifier: {mostSignificant.Name} (Impact: {mostSignificant.Modifier.GetOverallImpact():F3})");
            }
        }

        /// <summary>
        /// Example 3: Demonstrate pressure situations and their effects
        /// </summary>
        public void DemonstratePressureEffects()
        {
            Console.WriteLine("\n=== Pressure Situation Effects Example ===");
            
            var homeTeam = CreateSampleTeam("Port Adelaide", isHome: true);
            var awayTeam = CreateSampleTeam("Sydney Swans", isHome: false);
            var matchContext = CreateSampleMatchContext(homeTeam.Name, awayTeam.Name);

            _ratingsSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList(), matchContext);

            var midfielder = homeTeam.Players.First(p => p.Role == Role.Midfielder);
            
            // Test different pressure scenarios
            var pressureScenarios = new[]
            {
                new { Name = "Normal Game", Context = new RatingUpdateContext { Quarter = 2, TimeRemaining = 900f, ScoreDifferential = 15f, CrowdSize = 45000 } },
                new { Name = "Close Game", Context = new RatingUpdateContext { Quarter = 3, TimeRemaining = 600f, ScoreDifferential = 3f, CrowdSize = 75000 } },
                new { Name = "Final Quarter", Context = new RatingUpdateContext { Quarter = 4, TimeRemaining = 900f, ScoreDifferential = 8f, CrowdSize = 85000 } },
                new { Name = "Final Minutes", Context = new RatingUpdateContext { Quarter = 4, TimeRemaining = 180f, ScoreDifferential = 2f, CrowdSize = 95000 } }
            };

            Console.WriteLine($"Testing pressure effects on {midfielder.Name} (Midfielder):");
            var baselineRating = _ratingsSystem.GetPlayerRating(midfielder.Id);
            Console.WriteLine($"Baseline: Speed {baselineRating.CurrentSpeed:F1}, Accuracy {baselineRating.CurrentAccuracy:F1}, Endurance {baselineRating.CurrentEndurance:F1}");

            foreach (var scenario in pressureScenarios)
            {
                _ratingsSystem.UpdatePlayerRatings(midfielder.Id, scenario.Context);
                var rating = _ratingsSystem.GetPlayerRating(midfielder.Id);
                var pressure = _ratingsSystem.GetPlayerPressure(midfielder.Id);

                Console.WriteLine($"\n{scenario.Name}:");
                Console.WriteLine($"- Pressure Level: {pressure.CurrentPressure:F2}");
                Console.WriteLine($"- Speed: {rating.CurrentSpeed:F1} ({rating.SpeedChange:+F1})");
                Console.WriteLine($"- Accuracy: {rating.CurrentAccuracy:F1} ({rating.AccuracyChange:+F1})");
                Console.WriteLine($"- Overall Impact: {rating.GetRelativePerformanceRating():+F1}");

                if (rating.LastUpdateModifiers != null)
                {
                    var pressureMod = rating.LastUpdateModifiers.PressureModifier;
                    Console.WriteLine($"- Pressure Modifier: Speed {pressureMod.SpeedImpact:+F3}, Accuracy {pressureMod.AccuracyImpact:+F3}");
                }
            }
        }

        /// <summary>
        /// Example 4: Demonstrate player matchups and their impact
        /// </summary>
        public void DemonstrateMatchupEffects()
        {
            Console.WriteLine("\n=== Player Matchup Effects Example ===");
            
            var homeTeam = CreateSampleTeam("Melbourne Demons", isHome: true);
            var awayTeam = CreateSampleTeam("Collingwood Magpies", isHome: false);
            var matchContext = CreateSampleMatchContext(homeTeam.Name, awayTeam.Name);

            _ratingsSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList(), matchContext);

            // Get forward/defender matchup
            var forward = homeTeam.Players.First(p => p.Role == Role.Forward);
            var defender = awayTeam.Players.First(p => p.Role == Role.Defender);

            var forwardMatchups = _ratingsSystem.GetPlayerMatchups(forward.Id);
            var defenderMatchups = _ratingsSystem.GetPlayerMatchups(defender.Id);

            Console.WriteLine($"Matchup Analysis: {forward.Name} (Forward) vs {defender.Name} (Defender)");
            Console.WriteLine($"\nPlayer Attributes:");
            Console.WriteLine($"Forward:  Speed {forward.Speed}, Accuracy {forward.Accuracy}, Endurance {forward.Endurance}");
            Console.WriteLine($"Defender: Speed {defender.Speed}, Accuracy {defender.Accuracy}, Endurance {defender.Endurance}");

            if (forwardMatchups.Any())
            {
                var matchup = forwardMatchups.First(m => m.OpponentId == defender.Id);
                Console.WriteLine($"\nMatchup Advantage: {matchup.CalculateAdvantage():+F3} (Forward perspective)");
                
                // Simulate some matchup events
                var events = new[]
                {
                    new MatchupEvent(true, "Mark") { MarginOfVictory = 0.7f },
                    new MatchupEvent(false, "Contest") { MarginOfVictory = 0.3f },
                    new MatchupEvent(true, "Sprint") { MarginOfVictory = 0.9f }
                };

                Console.WriteLine("\nSimulating matchup events:");
                foreach (var evt in events)
                {
                    matchup.Events.Add(evt);
                    Console.WriteLine($"- {evt.EventType}: Forward {(evt.PlayerWon ? "Won" : "Lost")} (Margin: {evt.MarginOfVictory:F1})");
                }

                Console.WriteLine($"Updated Advantage: {matchup.CalculateAdvantage():+F3}");
            }

            // Update ratings with matchup context
            var updateContext = new RatingUpdateContext
            {
                Quarter = 2,
                TimeRemaining = 800f,
                ScoreDifferential = 12f,
                CrowdSize = matchContext.CrowdSize
            };

            _ratingsSystem.UpdatePlayerRatings(forward.Id, updateContext);
            _ratingsSystem.UpdatePlayerRatings(defender.Id, updateContext);

            var forwardRating = _ratingsSystem.GetPlayerRating(forward.Id);
            var defenderRating = _ratingsSystem.GetPlayerRating(defender.Id);

            Console.WriteLine($"\nRating Impact from Matchups:");
            Console.WriteLine($"Forward Overall: {forwardRating.GetCurrentPerformanceRating():F1} ({forwardRating.GetRelativePerformanceRating():+F1})");
            Console.WriteLine($"Defender Overall: {defenderRating.GetCurrentPerformanceRating():F1} ({defenderRating.GetRelativePerformanceRating():+F1})");

            if (forwardRating.LastUpdateModifiers != null)
            {
                var matchupMod = forwardRating.LastUpdateModifiers.MatchupModifier;
                Console.WriteLine($"Forward Matchup Modifier: Speed {matchupMod.SpeedImpact:+F3}, Accuracy {matchupMod.AccuracyImpact:+F3}");
            }
        }

        /// <summary>
        /// Example 5: Demonstrate momentum effects on team performance
        /// </summary>
        public void DemonstrateMomentumEffects()
        {
            Console.WriteLine("\n=== Match Momentum Effects Example ===");
            
            var homeTeam = CreateSampleTeam("Brisbane Lions", isHome: true);
            var awayTeam = CreateSampleTeam("Gold Coast Suns", isHome: false);
            var matchContext = CreateSampleMatchContext(homeTeam.Name, awayTeam.Name);

            _ratingsSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList(), matchContext);

            var homePlayer = homeTeam.Players.First();
            var awayPlayer = awayTeam.Players.First();

            Console.WriteLine($"Testing momentum effects:");
            Console.WriteLine($"Home player: {homePlayer.Name}, Away player: {awayPlayer.Name}");

            // Simulate momentum-changing events
            var momentumEvents = new[]
            {
                new MomentumEvent(MomentumEventType.Goal, true) { Intensity = 1.0f, Description = "Home goal" },
                new MomentumEvent(MomentumEventType.QuickGoals, true) { Intensity = 1.5f, Description = "Quick home goals" },
                new MomentumEvent(MomentumEventType.DefensiveStop, false) { Intensity = 1.2f, Description = "Away defensive stop" },
                new MomentumEvent(MomentumEventType.Goal, false) { Intensity = 1.3f, Description = "Away goal against the run" }
            };

            var updateContext = new RatingUpdateContext
            {
                Quarter = 3,
                TimeRemaining = 600f,
                ScoreDifferential = 5f,
                CrowdSize = matchContext.CrowdSize
            };

            Console.WriteLine($"\nInitial Momentum: {_ratingsSystem.GetCurrentMomentum():+F3}");

            foreach (var momentumEvent in momentumEvents)
            {
                _ratingsSystem.UpdateMatchMomentum(momentumEvent);
                
                var currentMomentum = _ratingsSystem.GetCurrentMomentum();
                Console.WriteLine($"\nAfter {momentumEvent.Description}:");
                Console.WriteLine($"- Momentum: {currentMomentum:+F3}");

                // Update player ratings to show momentum impact
                _ratingsSystem.UpdatePlayerRatings(homePlayer.Id, updateContext);
                _ratingsSystem.UpdatePlayerRatings(awayPlayer.Id, updateContext);

                var homeRating = _ratingsSystem.GetPlayerRating(homePlayer.Id);
                var awayRating = _ratingsSystem.GetPlayerRating(awayPlayer.Id);

                Console.WriteLine($"- Home player rating: {homeRating.GetCurrentPerformanceRating():F1} ({homeRating.GetRelativePerformanceRating():+F1})");
                Console.WriteLine($"- Away player rating: {awayRating.GetCurrentPerformanceRating():F1} ({awayRating.GetRelativePerformanceRating():+F1})");

                if (homeRating.LastUpdateModifiers != null)
                {
                    var momentumMod = homeRating.LastUpdateModifiers.MomentumModifier;
                    Console.WriteLine($"- Home momentum modifier: {momentumMod.GetOverallImpact():+F3}");
                }
            }

            var analytics = _ratingsSystem.GetAnalytics();
            Console.WriteLine($"\nFinal Momentum State: {analytics.GetMomentumDescription()}");
        }

        /// <summary>
        /// Example 6: Demonstrate integrated system effects (fatigue + weather + ratings)
        /// </summary>
        public void DemonstrateIntegratedSystems()
        {
            Console.WriteLine("\n=== Integrated Systems Example ===");
            
            var homeTeam = CreateSampleTeam("Fremantle Dockers", isHome: true);
            var awayTeam = CreateSampleTeam("West Coast Eagles", isHome: false);
            var matchContext = CreateSampleMatchContext(homeTeam.Name, awayTeam.Name);

            // Initialize all systems
            _fatigueSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList());
            _ratingsSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList(), 
                matchContext, _fatigueSystem, _weatherSystem);

            // Set challenging weather conditions
            var weatherConditions = new WeatherConditions
            {
                Temperature = 32f,
                Humidity = 0.75f,
                WindSpeed = 20f,
                RainIntensity = 0.3f,
                WeatherType = WeatherType.Hot
            };
            
            _fatigueSystem.UpdateWeatherConditions(weatherConditions);

            var testPlayer = homeTeam.Players.First(p => p.Role == Role.Midfielder);
            Console.WriteLine($"Testing integrated effects on {testPlayer.Name} (Midfielder)");
            Console.WriteLine($"Weather: {weatherConditions.WeatherType}, {weatherConditions.Temperature}°C, {weatherConditions.Humidity:P0} humidity");

            // Simulate match progression
            var matchProgression = new[]
            {
                new { Quarter = 1, Time = 1200f, Fatigue = FatigueActivity.Running, Duration = 180f },
                new { Quarter = 2, Time = 900f, Fatigue = FatigueActivity.Sprinting, Duration = 45f },
                new { Quarter = 3, Time = 600f, Fatigue = FatigueActivity.Contest, Duration = 30f },
                new { Quarter = 4, Time = 300f, Fatigue = FatigueActivity.Sprinting, Duration = 60f }
            };

            var baselineRating = _ratingsSystem.GetPlayerRating(testPlayer.Id);
            Console.WriteLine($"\nBaseline Rating: {baselineRating.GetCurrentPerformanceRating():F1}");

            foreach (var period in matchProgression)
            {
                // Apply fatigue
                _fatigueSystem.UpdateFatigue(testPlayer.Id, period.Fatigue, period.Duration);
                
                // Create rating update context
                var context = new RatingUpdateContext
                {
                    Quarter = period.Quarter,
                    TimeRemaining = period.Time,
                    ScoreDifferential = 8f - (period.Quarter * 2f), // Score getting closer
                    CrowdSize = matchContext.CrowdSize
                };

                // Update ratings with all system integration
                _ratingsSystem.UpdatePlayerRatings(testPlayer.Id, context);

                var currentRating = _ratingsSystem.GetPlayerRating(testPlayer.Id);
                var fatigueState = _fatigueSystem.GetPlayerFatigueState(testPlayer.Id);
                var form = _ratingsSystem.GetPlayerForm(testPlayer.Id);

                Console.WriteLine($"\nQuarter {period.Quarter} - {period.Time / 60f:F1} mins remaining:");
                Console.WriteLine($"- Fatigue: {fatigueState.CurrentFatigue:F1}% ({fatigueState.CurrentZone})");
                Console.WriteLine($"- Overall Rating: {currentRating.GetCurrentPerformanceRating():F1} ({currentRating.GetRelativePerformanceRating():+F1})");
                Console.WriteLine($"- Form/Confidence: {form.CurrentForm:F1}/{form.Confidence:F1}");

                if (currentRating.LastUpdateModifiers != null)
                {
                    var modifiers = currentRating.LastUpdateModifiers;
                    Console.WriteLine($"- Key Modifiers:");
                    Console.WriteLine($"  Fatigue: {modifiers.FatigueModifier.GetOverallImpact():F3}");
                    Console.WriteLine($"  Weather: {modifiers.WeatherModifier.GetOverallImpact():F3}");
                    Console.WriteLine($"  Pressure: {modifiers.PressureModifier.GetOverallImpact():F3}");
                    
                    var mostSignificant = modifiers.GetMostSignificantModifier();
                    Console.WriteLine($"  Most significant: {mostSignificant.Name} ({mostSignificant.Modifier.GetOverallImpact():F3})");
                }
            }
        }

        /// <summary>
        /// Example 7: System analytics and performance tracking
        /// </summary>
        public void DemonstrateAnalytics()
        {
            Console.WriteLine("\n=== System Analytics Example ===");
            
            var homeTeam = CreateSampleTeam("North Melbourne", isHome: true);
            var awayTeam = CreateSampleTeam("St Kilda Saints", isHome: false);
            var matchContext = CreateSampleMatchContext(homeTeam.Name, awayTeam.Name);

            _ratingsSystem.InitializeForMatch(homeTeam.Players.ToList(), awayTeam.Players.ToList(), matchContext);

            // Simulate varied performance across players
            var random = new Random(456);
            var allPlayers = homeTeam.Players.Concat(awayTeam.Players).ToList();

            foreach (var player in allPlayers)
            {
                // Simulate random performance events
                int eventCount = random.Next(3, 8);
                for (int i = 0; i < eventCount; i++)
                {
                    var eventType = (PerformanceEventType)random.Next(0, Enum.GetValues<PerformanceEventType>().Length);
                    var quality = 0.5f + (float)random.NextDouble() * 1.5f;
                    var evt = new PerformanceEvent(eventType) { Quality = quality };
                    _ratingsSystem.UpdatePlayerForm(player.Id, evt);
                }

                // Update ratings
                var context = new RatingUpdateContext
                {
                    Quarter = random.Next(1, 5),
                    TimeRemaining = random.Next(300, 1800),
                    ScoreDifferential = random.Next(-30, 31),
                    CrowdSize = matchContext.CrowdSize
                };
                _ratingsSystem.UpdatePlayerRatings(player.Id, context);
            }

            // Generate analytics
            var analytics = _ratingsSystem.GetAnalytics();
            
            Console.WriteLine($"Match Analytics:");
            Console.WriteLine($"- Total Players: {analytics.TotalPlayers}");
            Console.WriteLine($"- Average Form: {analytics.AverageForm:F1}");
            Console.WriteLine($"- Average Confidence: {analytics.AverageConfidence:F1}");
            Console.WriteLine($"- Current Momentum: {analytics.CurrentMomentum:+F3} ({analytics.GetMomentumDescription()})");
            Console.WriteLine($"- Active Matchups: {analytics.ActiveMatchups}");

            Console.WriteLine($"\nTop Performers:");
            foreach (var playerId in analytics.TopPerformers.Take(3))
            {
                var player = allPlayers.First(p => p.Id == playerId);
                var rating = _ratingsSystem.GetPlayerRating(playerId);
                var form = _ratingsSystem.GetPlayerForm(playerId);
                Console.WriteLine($"- {player.Name} ({player.Role}): Rating {rating.GetCurrentPerformanceRating():F1}, Form {form.CurrentForm:F1}");
            }

            Console.WriteLine($"\nBottom Performers:");
            foreach (var playerId in analytics.BottomPerformers.Take(3))
            {
                var player = allPlayers.First(p => p.Id == playerId);
                var rating = _ratingsSystem.GetPlayerRating(playerId);
                var form = _ratingsSystem.GetPlayerForm(playerId);
                Console.WriteLine($"- {player.Name} ({player.Role}): Rating {rating.GetCurrentPerformanceRating():F1}, Form {form.CurrentForm:F1}");
            }

            // Show some detailed player analysis
            var analysisPlayer = analytics.TopPerformers.First();
            var analysisRating = _ratingsSystem.GetPlayerRating(analysisPlayer);
            var analysisForm = _ratingsSystem.GetPlayerForm(analysisPlayer);

            Console.WriteLine($"\nDetailed Analysis - {allPlayers.First(p => p.Id == analysisPlayer).Name}:");
            Console.WriteLine($"- Performance Trend: {analysisRating.GetPerformanceTrend()}");
            Console.WriteLine($"- Form Trend: {analysisForm.GetFormTrend()}");
            Console.WriteLine($"- Confidence Level: {analysisForm.GetConfidenceLevel()}");
            Console.WriteLine($"- Recent Performances: {analysisForm.RecentPerformances.Count}");
            Console.WriteLine($"- Performance History Samples: {analysisRating.PerformanceHistory.Count}");

            if (analysisRating.PerformanceHistory.Any())
            {
                var recentPerf = analysisRating.PerformanceHistory.TakeLast(5).ToList();
                var avgRecent = recentPerf.Average(p => p.GetOverallRating());
                Console.WriteLine($"- Recent Average Rating: {avgRecent:F1}");
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
                "Alex Johnson", "Blake Smith", "Charlie Wilson", "Dylan Brown", "Ethan Davis", "Felix Moore",
                "George Taylor", "Henry Clark", "Isaac White", "Jack Anderson", "Kyle Thomas", "Liam Jackson",
                "Mason Harris", "Noah Martin", "Owen Thompson", "Parker Garcia", "Quinn Rodriguez", "Ryan Lewis",
                "Sean Lee", "Tyler Walker", "Uriah Hall", "Victor Allen"
            };

            var random = new Random(isHome ? 100 : 200);
            
            for (int i = 0; i < Math.Min(positions.Length, names.Length); i++)
            {
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

        private MatchContext CreateSampleMatchContext(string homeTeam, string awayTeam)
        {
            return new MatchContext
            {
                MatchId = Guid.NewGuid(),
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                Venue = "Marvel Stadium",
                CrowdSize = 65000,
                IsNightGame = false,
                IsFinalSeries = false,
                HomeGroundAdvantage = 0.05f,
                MatchStart = DateTime.Now
            };
        }

        #endregion
    }
}