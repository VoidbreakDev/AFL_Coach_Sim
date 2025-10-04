using System;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Services;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Season.Examples
{
    /// <summary>
    /// Example demonstrating how to use the season calendar system
    /// </summary>
    public static class SeasonCalendarExample
    {
        /// <summary>
        /// Generate and display a complete AFL season calendar
        /// </summary>
        public static void RunSeasonCalendarExample()
        {
            CoreLogger.Log("=== AFL Season Calendar Generation Example ===");
            
            // Step 1: Generate the season calendar
            var fixtureEngine = new FixtureGenerationEngine(seed: 2024); // Fixed seed for reproducible results
            var year = 2024;
            
            var options = new FixtureGenerationOptions
            {
                TotalRounds = 24,
                ByeRoundStart = 12,
                ByeRoundEnd = 15,
                IncludeSeasonOpener = true,
                IncludeAnzacDay = true,
                IncludeKingsBirthday = true,
                IncludeEasterMonday = true,
                IncludeRivalryMatches = true
            };
            
            CoreLogger.Log($"Generating {year} AFL season calendar...");
            var seasonCalendar = fixtureEngine.GenerateSeasonCalendar(year, options);
            
            // Step 2: Display key season information
            DisplaySeasonOverview(seasonCalendar);
            
            // Step 3: Display specialty matches
            DisplaySpecialtyMatches(seasonCalendar);
            
            // Step 4: Display bye rounds
            DisplayByeRounds(seasonCalendar);
            
            // Step 5: Set up season progression manager
            var progressionManager = new SeasonProgressionManager(seasonCalendar);
            
            // Step 6: Simulate some season progression
            SimulateSeasonProgression(progressionManager);
            
            CoreLogger.Log("=== Season Calendar Example Complete ===");
        }
        
        /// <summary>
        /// Display key season information
        /// </summary>
        private static void DisplaySeasonOverview(SeasonCalendar season)
        {
            CoreLogger.Log($"\n=== {season.Year} AFL Season Overview ===");
            CoreLogger.Log($"Season Start: {season.SeasonStart:dddd, MMMM dd, yyyy}");
            CoreLogger.Log($"Season End: {season.SeasonEnd:dddd, MMMM dd, yyyy}");
            CoreLogger.Log($"Total Rounds: {season.TotalRounds}");
            CoreLogger.Log($"Total Matches: {season.Rounds.Sum(r => r.Matches.Count)}");
            CoreLogger.Log($"Current State: {season.CurrentState}");
            CoreLogger.Log($"Current Round: {season.CurrentRound}");
            
            // Validation
            var validation = season.Validate();
            CoreLogger.Log($"Season Validation: {(validation.IsValid ? "✅ Valid" : "❌ Invalid")}");
            if (!validation.IsValid)
            {
                CoreLogger.LogWarning($"Validation Errors: {string.Join(", ", validation.Errors)}");
            }
        }
        
        /// <summary>
        /// Display specialty matches
        /// </summary>
        private static void DisplaySpecialtyMatches(SeasonCalendar season)
        {
            CoreLogger.Log($"\n=== Specialty Matches ({season.SpecialtyMatches.Count} total) ===");
            
            foreach (var specialtyMatch in season.SpecialtyMatches.OrderBy(sm => sm.TargetDate))
            {
                CoreLogger.Log($"{specialtyMatch.Name}: {specialtyMatch.HomeTeam} vs {specialtyMatch.AwayTeam}");
                CoreLogger.Log($"  Date: {specialtyMatch.TargetDate:dddd, MMMM dd, yyyy} (Round {specialtyMatch.RoundNumber})");
                CoreLogger.Log($"  Venue: {specialtyMatch.Venue}");
                CoreLogger.Log($"  Type: {specialtyMatch.Type}");
                CoreLogger.Log($"  Priority: {specialtyMatch.Priority}");
                CoreLogger.Log("");
            }
        }
        
        /// <summary>
        /// Display bye round configuration
        /// </summary>
        private static void DisplayByeRounds(SeasonCalendar season)
        {
            CoreLogger.Log($"\n=== Bye Rounds (Rounds {season.ByeConfiguration.StartRound}-{season.ByeConfiguration.EndRound}) ===");
            
            foreach (var byeRound in season.ByeConfiguration.ByeRoundAssignments)
            {
                CoreLogger.Log($"Round {byeRound.Key} Byes: {string.Join(", ", byeRound.Value)}");
            }
            
            var validation = season.ByeConfiguration.Validate();
            CoreLogger.Log($"Bye Configuration: {(validation.IsValid ? "✅ Valid" : "❌ Invalid")}");
        }
        
        /// <summary>
        /// Demonstrate season progression
        /// </summary>
        private static void SimulateSeasonProgression(SeasonProgressionManager progressionManager)
        {
            CoreLogger.Log($"\n=== Season Progression Simulation ===");
            
            // Display initial stats
            var initialStats = progressionManager.GetProgressStats();
            CoreLogger.Log($"Initial Progress: {initialStats.GetProgressSummary()}");
            
            // Show current round matches
            var currentMatches = progressionManager.GetCurrentRoundMatches();
            CoreLogger.Log($"Round 1 Matches ({currentMatches.Count} total):");
            foreach (var match in currentMatches.Take(3)) // Show first 3 matches
            {
                CoreLogger.Log($"  {match.HomeTeam} vs {match.AwayTeam} at {match.Venue} ({match.ScheduledDateTime:MMM dd, h:mm tt})");
            }
            
            // Simulate completing some matches
            CoreLogger.Log("\nSimulating match completions...");
            foreach (var match in currentMatches.Take(2)) // Complete first 2 matches
            {
                var homeScore = UnityEngine.Random.Range(60, 120);
                var awayScore = UnityEngine.Random.Range(60, 120);
                var result = progressionManager.CompleteMatch(match.MatchId, homeScore, awayScore);
                
                if (result.Success)
                {
                    CoreLogger.Log($"✅ {result.Message}");
                }
            }
            
            // Check team's next matches
            CoreLogger.Log("\nTeam upcoming matches:");
            var sampleTeams = new[] { TeamId.Carlton, TeamId.Richmond, TeamId.Collingwood };
            foreach (var team in sampleTeams)
            {
                var upcomingMatch = progressionManager.GetTeamUpcomingMatch(team);
                CoreLogger.Log($"{team}: {upcomingMatch.Message}");
            }
            
            // Show upcoming matches for next week
            var upcomingMatches = progressionManager.GetUpcomingMatches(7);
            CoreLogger.Log($"\nUpcoming matches (next 7 days): {upcomingMatches.Count}");
            
            // Display progress stats
            var finalStats = progressionManager.GetProgressStats();
            CoreLogger.Log($"Updated Progress: {finalStats.GetProgressSummary()}");
        }
        
        /// <summary>
        /// Demonstrate AFL calendar utilities
        /// </summary>
        public static void RunCalendarUtilitiesExample()
        {
            CoreLogger.Log("\n=== AFL Calendar Utilities Example ===");
            
            var year = 2024;
            
            CoreLogger.Log($"AFL Key Dates for {year}:");
            CoreLogger.Log($"Season Opener (2nd Thursday March): {AFLCalendarUtilities.GetSeasonOpenerDate(year):dddd, MMMM dd, yyyy}");
            CoreLogger.Log($"ANZAC Day: {AFLCalendarUtilities.GetAnzacDay(year):dddd, MMMM dd, yyyy}");
            CoreLogger.Log($"Easter Monday: {AFLCalendarUtilities.GetEasterMonday(year):dddd, MMMM dd, yyyy}");
            CoreLogger.Log($"King's Birthday (2nd Monday June): {AFLCalendarUtilities.GetKingsBirthday(year):dddd, MMMM dd, yyyy}");
            CoreLogger.Log($"Grand Final Weekend: {AFLCalendarUtilities.GetGrandFinalWeekend(year):dddd, MMMM dd, yyyy}");
            
            // Show typical match times
            CoreLogger.Log("\nTypical AFL Match Times:");
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                var time = AFLCalendarUtilities.GetTypicalMatchTime(day);
                CoreLogger.Log($"{day}: {time:h\\:mm} {(time.Hours >= 12 ? "PM" : "AM")}");
            }
            
            // Calculate weeks between season start and end
            var seasonStart = AFLCalendarUtilities.GetSeasonOpenerDate(year);
            var grandFinal = AFLCalendarUtilities.GetGrandFinalWeekend(year);
            var weeks = AFLCalendarUtilities.GetWeeksBetween(seasonStart, grandFinal);
            
            CoreLogger.Log($"\nSeason Duration: {weeks} weeks from {seasonStart:MMM dd} to {grandFinal:MMM dd}");
        }
        
        /// <summary>
        /// Display fixture balance analysis
        /// </summary>
        public static void RunFixtureBalanceExample()
        {
            CoreLogger.Log("\n=== Fixture Balance Analysis Example ===");
            
            var fixtureEngine = new FixtureGenerationEngine(seed: 2024);
            var season = fixtureEngine.GenerateSeasonCalendar(2024);
            
            // Analyze home/away balance
            CoreLogger.Log("Home/Away Balance:");
            var teams = Enum.GetValues<TeamId>().Where(t => t != TeamId.None).ToList();
            
            foreach (var team in teams.Take(6)) // Show first 6 teams
            {
                var teamMatches = season.GetTeamMatches(team).ToList();
                var homeMatches = teamMatches.Count(m => m.HomeTeam == team);
                var awayMatches = teamMatches.Count(m => m.AwayTeam == team);
                
                CoreLogger.Log($"{team}: {homeMatches} home, {awayMatches} away (total: {teamMatches.Count})");
            }
            
            // Analyze interstate travel
            CoreLogger.Log("\nInterstate Travel Analysis:");
            foreach (var team in new[] { TeamId.WestCoast, TeamId.Adelaide, TeamId.Brisbane }.Take(3))
            {
                var teamMatches = season.GetTeamMatches(team).ToList();
                var awayMatches = teamMatches.Where(m => m.AwayTeam == team);
                
                // This is simplified - in reality you'd check actual venue locations
                var interstateAway = awayMatches.Count(m => GetTeamState(m.HomeTeam) != GetTeamState(team));
                
                CoreLogger.Log($"{team}: {interstateAway} interstate away games out of {awayMatches.Count()} total away games");
            }
        }
        
        private static string GetTeamState(TeamId teamId)
        {
            return teamId switch
            {
                TeamId.Adelaide or TeamId.PortAdelaide => "SA",
                TeamId.Brisbane or TeamId.GoldCoast => "QLD",
                TeamId.WestCoast or TeamId.Fremantle => "WA",
                _ => "VIC"
            };
        }
    }
}