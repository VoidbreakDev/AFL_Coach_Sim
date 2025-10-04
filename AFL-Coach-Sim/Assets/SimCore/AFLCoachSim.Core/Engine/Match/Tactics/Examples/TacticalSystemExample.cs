using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Tactics;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Tactics.Examples
{
    /// <summary>
    /// Example demonstrating how to integrate the advanced tactical system with the match engine
    /// </summary>
    public class TacticalSystemExample
    {
        /// <summary>
        /// Example of setting up and using the tactical system in a match
        /// </summary>
        public static void RunTacticalSystemExample()
        {
            CoreLogger.Log("=== Advanced Tactical System Example ===");

            // 1. Create team IDs
            var homeTeam = new TeamId { Value = Guid.NewGuid() };
            var awayTeam = new TeamId { Value = Guid.NewGuid() };

            // 2. Set up coaching profiles
            var homeCoach = CoachingProfileFactory.CreateTacticalGenius();
            var awayCoach = CoachingProfileFactory.CreateDefensiveMinded();

            // 3. Initialize the tactical integration manager
            var tacticalManager = new TacticalIntegrationManager(seed: 12345);
            tacticalManager.InitializeMatch(homeTeam, awayTeam, homeCoach, awayCoach);

            // 4. Simulate match progression with tactical decisions
            var matchState = CreateExampleMatchState(homeTeam, awayTeam);
            var homePlayers = CreateExamplePlayers();
            var awayPlayers = CreateExamplePlayers();

            CoreLogger.Log("\n--- Match Simulation with Tactical Updates ---");

            // Simulate different match situations
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                CoreLogger.Log($"\nQUARTER {quarter}:");
                
                for (int minute = 0; minute < 25; minute += 5) // Every 5 minutes
                {
                    // Update match state
                    float elapsedTime = (quarter - 1) * 1500f + minute * 60f; // Convert to seconds
                    UpdateMatchState(matchState, quarter, minute, homeTeam, awayTeam);

                    // Process tactical updates
                    var tacticalImpacts = tacticalManager.ProcessTacticalUpdates(
                        matchState, homeTeam, awayTeam, elapsedTime, homePlayers, awayPlayers);

                    // Display tactical information
                    if (tacticalImpacts.HasSignificantImpacts())
                    {
                        LogTacticalImpacts(tacticalImpacts, minute);
                    }

                    // Show tactical summaries
                    if (minute % 10 == 0) // Every 10 minutes
                    {
                        LogTacticalSummaries(tacticalManager, homeTeam, awayTeam, minute);
                    }
                }
            }

            // 5. Post-match tactical analysis
            CoreLogger.Log("\n=== Post-Match Tactical Analysis ===");
            var homeAnalytics = tacticalManager.GetMatchTacticalAnalytics(homeTeam);
            var awayAnalytics = tacticalManager.GetMatchTacticalAnalytics(awayTeam);

            LogTacticalAnalytics("Home Team", homeAnalytics);
            LogTacticalAnalytics("Away Team", awayAnalytics);

            // 6. Formation effectiveness comparison
            var comparison = tacticalManager.CompareTacticalEffectiveness(homeTeam, awayTeam, Phase.OpenPlay);
            CoreLogger.Log($"\nFinal Tactical Comparison: {comparison.GetAdvantageDescription()}");
        }

        /// <summary>
        /// Example of how the tactical system would integrate with existing MatchEngine
        /// </summary>
        public static void DemonstrateMatchEngineIntegration()
        {
            CoreLogger.Log("\n=== Match Engine Integration Example ===");

            // This shows how you would modify the existing MatchEngine to use tactical system
            CoreLogger.Log("In your MatchEngine.SimulatePhase method, you would:");
            
            CoreLogger.Log("1. Call tacticalManager.ProcessTacticalUpdates() at the start of each phase");
            CoreLogger.Log("2. Apply tactical impacts to phase calculations:");
            
            // Example of applying tactical modifiers
            var exampleImpacts = new TacticalImpacts
            {
                HomeFormationEffectiveness = new FormationEffectiveness
                {
                    OpenPlayAdvantage = 0.12f,
                    CenterBounceAdvantage = 0.08f
                },
                HomePressureRating = 1.15f,
                AwayPressureRating = 0.95f
            };

            CoreLogger.Log($"   - Home team gets {exampleImpacts.HomeFormationEffectiveness.OpenPlayAdvantage:P1} advantage in open play");
            CoreLogger.Log($"   - Home pressure rating: {exampleImpacts.HomePressureRating:F2}x");
            CoreLogger.Log($"   - Away pressure rating: {exampleImpacts.AwayPressureRating:F2}x");

            CoreLogger.Log("3. Apply player-specific modifiers from tactical positioning");
            CoreLogger.Log("4. Factor in tactical decision disruption/adaptation time");

            // Example integration code snippet
            CoreLogger.Log("\nExample MatchEngine integration code:");
            CoreLogger.Log(@"
// In MatchEngine.SimulatePhase():
var tacticalImpacts = _tacticalManager.ProcessTacticalUpdates(
    matchState, homeTeam, awayTeam, elapsedTime, homePlayers, awayPlayers);

// Apply formation effectiveness
float homeAdvantage = tacticalImpacts.GetHomeTacticalAdvantage(currentPhase);
homeTeamStrength *= (1.0f + homeAdvantage);

// Apply pressure ratings
homePossessionChance *= tacticalImpacts.HomePressureRating;
awayPossessionChance *= tacticalImpacts.AwayPressureRating;

// Apply player modifiers
foreach (var player in homePlayers) {
    if (tacticalImpacts.HomePlayerModifiers.TryGetValue(player.Name, out var modifier)) {
        ApplyPlayerModifier(player, modifier);
    }
}");
        }

        #region Helper Methods

        private static MatchState CreateExampleMatchState(TeamId homeTeam, TeamId awayTeam)
        {
            return new MatchState
            {
                HomeScore = 0,
                AwayScore = 0,
                ElapsedTime = 0f,
                TotalGameTime = 6000f, // 100 minutes
                CurrentPhase = Phase.CenterBounce,
                Weather = Weather.Clear,
                TeamMomentum = new Dictionary<TeamId, float>
                {
                    [homeTeam] = 0f,
                    [awayTeam] = 0f
                },
                TeamStats = new Dictionary<TeamId, Dictionary<string, float>>
                {
                    [homeTeam] = new Dictionary<string, float> { ["TurnoverRate"] = 0.35f },
                    [awayTeam] = new Dictionary<string, float> { ["TurnoverRate"] = 0.42f }
                }
            };
        }

        private static List<PlayerRuntime> CreateExamplePlayers()
        {
            // Create example players - in real usage these would come from actual teams
            var players = new List<PlayerRuntime>();
            
            for (int i = 0; i < 18; i++)
            {
                var player = new Player
                {
                    Id = Guid.NewGuid(),
                    Name = $"Player {i + 1}",
                    PrimaryRole = (Role)(i % Enum.GetValues(typeof(Role)).Length)
                };
                
                players.Add(new PlayerRuntime(player));
            }
            
            return players;
        }

        private static void UpdateMatchState(MatchState matchState, int quarter, int minute, 
            TeamId homeTeam, TeamId awayTeam)
        {
            // Simulate score progression
            var random = new Random(quarter * 100 + minute);
            
            if (random.NextDouble() < 0.3) // 30% chance of score change
            {
                if (random.NextDouble() < 0.5)
                    matchState.HomeScore += 6;
                else
                    matchState.AwayScore += 6;
            }

            // Update momentum based on recent scoring
            float scoreDiff = matchState.HomeScore - matchState.AwayScore;
            matchState.TeamMomentum[homeTeam] = Math.Max(-1f, Math.Min(1f, scoreDiff / 30f));
            matchState.TeamMomentum[awayTeam] = -matchState.TeamMomentum[homeTeam];

            // Simulate phase changes
            matchState.CurrentPhase = (Phase)(random.Next(0, 4));
            
            // Update elapsed time
            matchState.ElapsedTime = (quarter - 1) * 1500f + minute * 60f;
            
            // Simulate turnover rate changes
            var homeStats = matchState.TeamStats[homeTeam];
            var awayStats = matchState.TeamStats[awayTeam];
            
            homeStats["TurnoverRate"] = 0.3f + (float)random.NextDouble() * 0.3f;
            awayStats["TurnoverRate"] = 0.3f + (float)random.NextDouble() * 0.3f;
        }

        private static void LogTacticalImpacts(TacticalImpacts impacts, int minute)
        {
            CoreLogger.Log($"  [{minute:D2}min] Tactical Activity:");
            
            if (impacts.HomeTacticalDecision?.ShouldAdjust == true)
            {
                CoreLogger.Log($"    Home: {impacts.HomeTacticalDecision.GetDescription()}");
            }
            
            if (impacts.AwayTacticalDecision?.ShouldAdjust == true)
            {
                CoreLogger.Log($"    Away: {impacts.AwayTacticalDecision.GetDescription()}");
            }

            if (impacts.HomeFormationEffectiveness != null || impacts.AwayFormationEffectiveness != null)
            {
                float homeAdv = impacts.HomeFormationEffectiveness?.OverallAdvantage ?? 0f;
                float awayAdv = impacts.AwayFormationEffectiveness?.OverallAdvantage ?? 0f;
                float netAdvantage = homeAdv - awayAdv;
                
                if (Math.Abs(netAdvantage) > 0.05f)
                {
                    string team = netAdvantage > 0 ? "Home" : "Away";
                    CoreLogger.Log($"    {team} team has tactical advantage: {Math.Abs(netAdvantage):P1}");
                }
            }
        }

        private static void LogTacticalSummaries(TacticalIntegrationManager tacticalManager, 
            TeamId homeTeam, TeamId awayTeam, int minute)
        {
            var homeSummary = tacticalManager.GetTacticalSummary(homeTeam);
            var awaySummary = tacticalManager.GetTacticalSummary(awayTeam);

            CoreLogger.Log($"  [{minute:D2}min] Tactical Status:");
            CoreLogger.Log($"    Home: {homeSummary.CurrentFormation} formation, " +
                         $"{homeSummary.OffensiveStyle} offense, {homeSummary.DefensiveStyle} defense " +
                         $"({homeSummary.TotalAdjustments} adjustments)");
            CoreLogger.Log($"    Away: {awaySummary.CurrentFormation} formation, " +
                         $"{awaySummary.OffensiveStyle} offense, {awaySummary.DefensiveStyle} defense " +
                         $"({awaySummary.TotalAdjustments} adjustments)");
        }

        private static void LogTacticalAnalytics(string teamName, TacticalAnalytics analytics)
        {
            CoreLogger.Log($"\n{teamName} Tactical Performance:");
            CoreLogger.Log($"  Final Formation: {analytics.FinalFormation}");
            CoreLogger.Log($"  Final Styles: {analytics.FinalOffensiveStyle} / {analytics.FinalDefensiveStyle}");
            CoreLogger.Log($"  Tactical Adjustments: {analytics.SuccessfulAdjustments}/{analytics.TotalAdjustmentAttempts} successful");
            CoreLogger.Log($"  Performance Score: {analytics.CalculateTacticalPerformanceScore():F1}/100");
            CoreLogger.Log($"  Adaptability Rating: {analytics.GetTacticalAdaptabilityRating()}");
            
            if (analytics.AverageAdjustmentEffect > 0)
            {
                CoreLogger.Log($"  Average Effect: {analytics.AverageAdjustmentEffect:P1} per adjustment");
            }
        }

        #endregion

        /// <summary>
        /// Run the tactical system example
        /// </summary>
        public static void Main()
        {
            try
            {
                RunTacticalSystemExample();
                DemonstrateMatchEngineIntegration();
                
                CoreLogger.Log("\n=== Tactical System Example Complete ===");
                CoreLogger.Log("The advanced tactical system provides:");
                CoreLogger.Log("• Dynamic formation changes based on match situation");
                CoreLogger.Log("• AI-driven coaching decisions with realistic success/failure");
                CoreLogger.Log("• Formation effectiveness calculations for all match phases");
                CoreLogger.Log("• Player positioning modifiers from tactical setup");
                CoreLogger.Log("• Comprehensive tactical analytics and reporting");
                CoreLogger.Log("• Easy integration with existing match engine");
            }
            catch (Exception ex)
            {
                CoreLogger.LogError($"Error in tactical system example: {ex.Message}");
            }
        }
    }
}