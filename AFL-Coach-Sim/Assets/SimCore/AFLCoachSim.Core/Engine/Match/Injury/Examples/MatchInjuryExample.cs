using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Match.Tuning;
using AFLCoachSim.Core.Engine.Match.Weather;
using WeatherCondition = AFLCoachSim.Core.Engine.Match.Weather.Weather;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Persistence;

namespace AFLCoachSim.Core.Engine.Match.Injury.Examples
{
    /// <summary>
    /// Example demonstrating how to use the modern injury system with match simulation
    /// </summary>
    public static class MatchInjuryExample
    {
        /// <summary>
        /// Example of running a match with the modern injury system
        /// </summary>
        public static void RunMatchExample()
        {
            Console.WriteLine("=== Modern Match Injury System Example ===");
            
            // 1. Set up the unified injury system
            var injuryManager = CreateInjuryManager();
            
            // 2. Create teams and players
            var teams = CreateExampleTeams();
            var rosters = CreateExampleRosters();
            
            // 3. Set up some pre-existing injuries to demonstrate the system
            SetupPreExistingInjuries(injuryManager, rosters);
            
            // 4. Configure match parameters
            var tuning = MatchTuning.Default;
            var rng = new DeterministicRandom(42); // Fixed seed for reproducible results
            
            // 5. Run the match with enhanced injury system
            Console.WriteLine("Starting match with modern injury system...");
            
            var result = MatchEngine.PlayMatch(
                round: 1,
                homeId: TeamId.Adelaide,
                awayId: TeamId.Brisbane,
                teams: teams,
                injuryManager: injuryManager, // Required parameter
                rosters: rosters,
                tactics: null,
                weather: WeatherCondition.Clear,
                ground: null,
                quarterSeconds: 5 * 60, // Shorter match for demo
                rng: rng,
                tuning: tuning,
                sink: null
            );
            
            // 6. Display results
            Console.WriteLine($"Match Result: {result.Home} {result.HomeScore} - {result.AwayScore} {result.Away}");
            
            // 7. Show injury summary
            DisplayInjurySummary(injuryManager, rosters);
            
            Console.WriteLine("=== Modern Match Example Complete ===");
        }
        
        
        #region Helper Methods
        
        private static InjuryManager CreateInjuryManager()
        {
            // Create in-memory persistence for the example
            var repository = new InMemoryInjuryRepository();
            
            // Create injury manager with repository directly
            var injuryManager = new InjuryManager(repository);
            
            Console.WriteLine("Created injury management system");
            return injuryManager;
        }
        
        private static Dictionary<TeamId, Team> CreateExampleTeams()
        {
            return new Dictionary<TeamId, Team>
            {
                [TeamId.Adelaide] = new Team(TeamId.Adelaide, "Adelaide Crows", 85, 82),
                [TeamId.Brisbane] = new Team(TeamId.Brisbane, "Brisbane Lions", 88, 80)
            };
        }
        
        private static Dictionary<TeamId, List<Domain.Entities.Player>> CreateExampleRosters()
        {
            var adelaideRoster = CreateTeamRoster("ADE", 25);
            var brisbaneRoster = CreateTeamRoster("BRI", 25);
            
            return new Dictionary<TeamId, List<Domain.Entities.Player>>
            {
                [TeamId.Adelaide] = adelaideRoster,
                [TeamId.Brisbane] = brisbaneRoster
            };
        }
        
        private static List<Domain.Entities.Player> CreateTeamRoster(string teamPrefix, int playerCount)
        {
            var roster = new List<Domain.Entities.Player>();
            var random = new System.Random(42);
            
            for (int i = 1; i <= playerCount; i++)
            {
                var player = new Domain.Entities.Player
                {
                    Id = new PlayerId(i + (teamPrefix == "ADE" ? 0 : 100)), // Offset Brisbane IDs
                    // Add other required player properties here
                    Durability = random.Next(70, 95) // Random durability for variety
                };
                
                roster.Add(player);
            }
            
            return roster;
        }
        
        private static void SetupPreExistingInjuries(InjuryManager injuryManager, Dictionary<TeamId, List<Domain.Entities.Player>> rosters)
        {
            Console.WriteLine("Setting up some pre-existing injuries for demonstration...");
            
            // Give Adelaide player #5 a niggle
            injuryManager.RecordInjury(5, InjuryType.Muscle, InjurySeverity.Niggle, InjurySource.Training, "Upper leg muscle strain");
            
            // Give Brisbane player #110 a minor injury
            injuryManager.RecordInjury(110, InjuryType.Joint, InjurySeverity.Minor, InjurySource.Training, "Lower leg joint injury");
            
            Console.WriteLine("Pre-existing injuries set up");
        }
        
        private static void DisplayInjurySummary(InjuryManager injuryManager, Dictionary<TeamId, List<Domain.Entities.Player>> rosters)
        {
            Console.WriteLine("=== Post-Match Injury Summary ===");
            
            var allPlayers = rosters.Values.SelectMany(r => r).ToList();
            
            foreach (var player in allPlayers)
            {
                var injuries = injuryManager.GetActiveInjuries(player.Id).ToList();
                if (injuries.Any())
                {
                    Console.WriteLine($"Player {player.Id}:");
                    foreach (var injury in injuries)
                    {
                        var description = injuryManager.GetInjuryDescription(injury.Id);
                        Console.WriteLine($"  - {injury.Severity} {injury.Type} ({injury.BodyPart}): {description}");
                    }
                    
                    var canPlay = injuryManager.CanPlayerPlay(player.Id);
                    var performance = injuryManager.GetPlayerPerformanceMultiplier(player.Id);
                    Console.WriteLine($"    Can play: {canPlay}, Performance: {performance:P0}");
                }
            }
            
            var totalActiveInjuries = allPlayers.Sum(p => injuryManager.GetActiveInjuries(p.Id).Count());
            Console.WriteLine($"Total active injuries: {totalActiveInjuries}");
        }
        
        #endregion
        
        /// <summary>
        /// Simple in-memory repository for the example
        /// </summary>
        private class InMemoryInjuryRepository : IInjuryRepository
        {
            private readonly List<InjuryDTO> _injuries = new List<InjuryDTO>();
            private readonly List<PlayerInjuryHistoryDTO> _history = new List<PlayerInjuryHistoryDTO>();
            private int _nextId = 1;
            
            // Note: This is a simplified example repository - real implementation would need proper DTO mappings
            
            // Placeholder implementations to satisfy interface - needs proper DTO conversion
            public void SavePlayerInjuryHistory(AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory history) { }
            public AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory LoadPlayerInjuryHistory(int playerId) { return null; }
            public IDictionary<int, AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory> LoadAllPlayerInjuryHistories() { return new Dictionary<int, AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory>(); }
            public void RemovePlayerInjuryHistory(int playerId) { }
            public void SaveInjury(AFLCoachSim.Core.Injuries.Domain.Injury injury) { }
            public AFLCoachSim.Core.Injuries.Domain.Injury LoadInjury(AFLCoachSim.Core.Injuries.Domain.InjuryId injuryId) { return null; }
            public IEnumerable<AFLCoachSim.Core.Injuries.Domain.Injury> LoadPlayerInjuries(int playerId) { return new List<AFLCoachSim.Core.Injuries.Domain.Injury>(); }
            public IEnumerable<AFLCoachSim.Core.Injuries.Domain.Injury> LoadActiveInjuries() { return new List<AFLCoachSim.Core.Injuries.Domain.Injury>(); }
            public void SaveAllInjuryData(InjuryDataDTO data) { }
            public InjuryDataDTO LoadAllInjuryData() { return new InjuryDataDTO(); }
            public void ClearAllInjuryData() { }
            public void ClearPlayerInjuryData(int playerId) { }
            public bool HasInjuryData() { return false; }
            public void BackupInjuryData(string backupSuffix) { }
            public bool RestoreInjuryData(string backupSuffix) { return true; }
        }
    }
}