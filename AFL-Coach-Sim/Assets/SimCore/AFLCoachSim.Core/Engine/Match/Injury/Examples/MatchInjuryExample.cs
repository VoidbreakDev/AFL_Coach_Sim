using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Match.Tuning;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Injuries.Persistence;
using AFLCoachSim.Core.Injuries.Services;
using UnityEngine;

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
            Debug.Log("=== Modern Match Injury System Example ===");
            
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
            Debug.Log("Starting match with modern injury system...");
            
            var result = MatchEngine.PlayMatch(
                round: 1,
                homeId: TeamId.Adelaide,
                awayId: TeamId.Brisbane,
                teams: teams,
                injuryManager: injuryManager, // Required parameter
                rosters: rosters,
                tactics: null,
                weather: Weather.Clear,
                ground: null,
                quarterSeconds: 5 * 60, // Shorter match for demo
                rng: rng,
                tuning: tuning,
                sink: null
            );
            
            // 6. Display results
            Debug.Log($"Match Result: {result.Home} {result.HomeScore} - {result.AwayScore} {result.Away}");
            
            // 7. Show injury summary
            DisplayInjurySummary(injuryManager, rosters);
            
            Debug.Log("=== Modern Match Example Complete ===");
        }
        
        
        #region Helper Methods
        
        private static InjuryManager CreateInjuryManager()
        {
            // Create in-memory persistence for the example
            var repository = new InMemoryInjuryRepository();
            var service = new InjuryService(repository);
            
            // Create injury manager with default configuration
            var injuryManager = new InjuryManager(service);
            
            Debug.Log("Created injury management system");
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
                    ID = i + (teamPrefix == "ADE" ? 0 : 100), // Offset Brisbane IDs
                    // Add other required player properties here
                    Durability = random.Next(70, 95) // Random durability for variety
                };
                
                roster.Add(player);
            }
            
            return roster;
        }
        
        private static void SetupPreExistingInjuries(InjuryManager injuryManager, Dictionary<TeamId, List<Domain.Entities.Player>> rosters)
        {
            Debug.Log("Setting up some pre-existing injuries for demonstration...");
            
            // Give Adelaide player #5 a niggle
            injuryManager.RecordInjury(5, InjuryType.Muscle, InjurySeverity.Niggle, BodyPart.UpperLeg, "Training", 1.0f);
            
            // Give Brisbane player #110 a minor injury
            injuryManager.RecordInjury(110, InjuryType.Joint, InjurySeverity.Minor, BodyPart.LowerLeg, "Training", 1.2f);
            
            Debug.Log("Pre-existing injuries set up");
        }
        
        private static void DisplayInjurySummary(InjuryManager injuryManager, Dictionary<TeamId, List<Domain.Entities.Player>> rosters)
        {
            Debug.Log("=== Post-Match Injury Summary ===");
            
            var allPlayers = rosters.Values.SelectMany(r => r).ToList();
            
            foreach (var player in allPlayers)
            {
                var injuries = injuryManager.GetActiveInjuries(player.ID).ToList();
                if (injuries.Any())
                {
                    Debug.Log($"Player {player.ID}:");
                    foreach (var injury in injuries)
                    {
                        var description = injuryManager.GetInjuryDescription(injury.Id);
                        Debug.Log($"  - {injury.Severity} {injury.Type} ({injury.BodyPart}): {description}");
                    }
                    
                    var canPlay = injuryManager.CanPlayerPlay(player.ID);
                    var performance = injuryManager.GetPlayerPerformanceMultiplier(player.ID);
                    Debug.Log($"    Can play: {canPlay}, Performance: {performance:P0}");
                }
            }
            
            var totalActiveInjuries = allPlayers.Sum(p => injuryManager.GetActiveInjuries(p.ID).Count());
            Debug.Log($"Total active injuries: {totalActiveInjuries}");
        }
        
        #endregion
        
        /// <summary>
        /// Simple in-memory repository for the example
        /// </summary>
        private class InMemoryInjuryRepository : IInjuryRepository
        {
            private readonly List<InjuryDto> _injuries = new List<InjuryDto>();
            private readonly List<InjuryHistoryDto> _history = new List<InjuryHistoryDto>();
            private int _nextId = 1;
            
            public void SaveInjury(InjuryDto injury)
            {
                injury.Id = _nextId++;
                _injuries.Add(injury);
            }
            
            public void UpdateInjury(InjuryDto injury)
            {
                var existing = _injuries.FirstOrDefault(i => i.Id == injury.Id);
                if (existing != null)
                {
                    _injuries.Remove(existing);
                    _injuries.Add(injury);
                }
            }
            
            public InjuryDto GetInjury(int injuryId)
            {
                return _injuries.FirstOrDefault(i => i.Id == injuryId);
            }
            
            public List<InjuryDto> GetActiveInjuries(int playerId)
            {
                return _injuries.Where(i => i.PlayerId == playerId && i.IsActive).ToList();
            }
            
            public List<InjuryDto> GetAllInjuries(int playerId)
            {
                return _injuries.Where(i => i.PlayerId == playerId).ToList();
            }
            
            public void SaveInjuryHistory(InjuryHistoryDto historyEntry)
            {
                _history.Add(historyEntry);
            }
            
            public List<InjuryHistoryDto> GetInjuryHistory(int injuryId)
            {
                return _history.Where(h => h.InjuryId == injuryId).ToList();
            }
        }
    }
}