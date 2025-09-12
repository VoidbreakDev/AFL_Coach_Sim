using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Engine.Simulation;

namespace AFLManager.Demo
{
    /// <summary>
    /// Demonstrates the new commentary system integrated with match simulation
    /// </summary>
    public class CommentaryDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private int demoSeed = 12345;
        
        [Header("Output")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool showFullCommentary = false;
        [SerializeField] private bool showHighlightsOnly = true;
        [SerializeField] private bool showQuarterSummaries = false;
        
        void Start()
        {
            if (runOnStart)
            {
                RunCommentaryDemo();
            }
        }
        
        /// <summary>
        /// Demonstrates the commentary system with a sample match
        /// </summary>
        public void RunCommentaryDemo()
        {
            Debug.Log("=== AFL Coach Sim - Commentary System Demo ===");
            
            // Setup demo teams
            var homeId = new TeamId(1); // Home team ID
            var awayId = new TeamId(2); // Away team ID
            
            var teams = new Dictionary<TeamId, Team>
            {
                { homeId, new Team(homeId, "Melbourne Demons", 82, 78) },
                { awayId, new Team(awayId, "Collingwood Magpies", 79, 85) }
            };
            
            // Create demo rosters with AFL player names
            var rosters = new Dictionary<TeamId, List<AFLCoachSim.Core.Domain.Entities.Player>>
            {
                { homeId, CreateDemoRoster("Melbourne", GetMelbournePlayers()) },
                { awayId, CreateDemoRoster("Collingwood", GetCollingwoodPlayers()) }
            };
            
            var rng = new DeterministicRandom(demoSeed);
            
            // Run match with commentary
            var result = MatchEngineWithCommentary.PlayMatchWithCommentary(
                round: 15,
                homeId: homeId,
                awayId: awayId,
                teams: teams,
                rosters: rosters,
                weather: AFLCoachSim.Core.Engine.Match.Weather.Clear,
                quarterSeconds: 5 * 60, // Shortened quarters for demo
                rng: rng);
            
            if (logToConsole)
            {
                LogResults(result);
            }
        }
        
        private void LogResults(CommentatedMatchResult result)
        {
            Debug.Log($"\\n=== MATCH RESULT ===");
            Debug.Log($"Final Score: Home {result.HomeScore} - {result.AwayScore} Away");
            
            if (showFullCommentary)
            {
                Debug.Log($"\\n=== FULL MATCH COMMENTARY ({result.Commentary.Count} events) ===");
                foreach (var commentary in result.Commentary)
                {
                    Debug.Log($"  {commentary}");
                }
            }
            
            if (showHighlightsOnly)
            {
                var highlights = MatchEngineWithCommentary.GetMatchHighlights(result);
                Debug.Log($"\\n=== MATCH HIGHLIGHTS ({highlights.Count} key events) ===");
                foreach (var highlight in highlights)
                {
                    Debug.Log($"  🏈 {highlight}");
                }
            }
            
            if (showQuarterSummaries)
            {
                var summaries = MatchEngineWithCommentary.GetQuarterSummaries(result);
                Debug.Log($"\\n=== QUARTER BY QUARTER ===");
                
                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    Debug.Log($"Quarter {quarter} ({summaries[quarter].Count} events):");
                    foreach (var commentary in summaries[quarter].Take(5)) // Show first 5 events per quarter
                    {
                        Debug.Log($"    {commentary}");
                    }
                    if (summaries[quarter].Count > 5)
                    {
                        Debug.Log($"    ... and {summaries[quarter].Count - 5} more events");
                    }
                }
            }
            
            // Show some statistics
            var goalEvents = result.Events.Count(e => e.EventType == MatchEventType.Goal);
            var behindEvents = result.Events.Count(e => e.EventType == MatchEventType.Behind);
            var spectacularMarks = result.Events.Count(e => e.EventType == MatchEventType.SpectacularMark);
            var injuries = result.Events.Count(e => e.EventType == MatchEventType.Injury);
            
            Debug.Log($"\\n=== MATCH STATISTICS ===");
            Debug.Log($"Goals: {goalEvents}, Behinds: {behindEvents}");
            Debug.Log($"Spectacular Marks: {spectacularMarks}, Injuries: {injuries}");
            Debug.Log($"Total Commentary Events: {result.Commentary.Count}");
        }
        
        private List<AFLCoachSim.Core.Domain.Entities.Player> CreateDemoRoster(string teamName, string[] playerNames)
        {
            var roster = new List<AFLCoachSim.Core.Domain.Entities.Player>();
            
            for (int i = 0; i < playerNames.Length && i < 22; i++)
            {
                roster.Add(new AFLCoachSim.Core.Domain.Entities.Player
                {
                    Id = new PlayerId(i + 1), // Use sequential IDs starting from 1
                    Name = playerNames[i]
                });
            }
            
            return roster;
        }
        
        private string[] GetMelbournePlayers()
        {
            return new string[]
            {
                "Max Gawn", "Christian Petracca", "Clayton Oliver", "Jake Lever",
                "Steven May", "Angus Brayshaw", "Jack Viney", "Ed Langdon",
                "Bayley Fritsch", "Tom McDonald", "Alex Neal-Bullen", "Christian Salem",
                "Kysaiah Pickett", "Jake Melksham", "Ben Brown", "Luke Jackson",
                "James Harmes", "Trent Rivers", "Joel Smith", "Harrison Petty",
                "Adam Tomlinson", "Lachie Hunter"
            };
        }
        
        private string[] GetCollingwoodPlayers()
        {
            return new string[]
            {
                "Scott Pendlebury", "Steele Sidebottom", "Jordan De Goey", "Darcy Moore",
                "Jeremy Howe", "Nick Daicos", "Jack Crisp", "Taylor Adams",
                "Jordan Roughead", "Mason Cox", "Jamie Elliott", "Will Hoskin-Elliott",
                "Josh Daicos", "John Noble", "Brayden Maynard", "Nathan Murphy",
                "Isaac Quaynor", "Patrick Lipinski", "Bobby Hill", "Dan McStay",
                "Reef McInnes", "Ash Johnson"
            };
        }
    }
}
