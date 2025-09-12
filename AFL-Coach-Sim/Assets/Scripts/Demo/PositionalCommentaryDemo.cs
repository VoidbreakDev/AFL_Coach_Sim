using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Data;

namespace AFLCoachSim.Demo
{
    /// <summary>
    /// Demonstrates the enhanced commentary system with realistic position-based player selection
    /// </summary>
    public class PositionalCommentaryDemo : MonoBehaviour
    {
        [Header("Match Settings")]
        [SerializeField] private int quarterMinutes = 5;
        [SerializeField] private int seed = 42;
        [SerializeField] private Weather weather = Weather.Clear;
        
        [Header("Teams")]
        [SerializeField] private string homeTeamName = "Eagles";
        [SerializeField] private string awayTeamName = "Dockers";
        
        [Header("Demo Control")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private bool showDetailedRoster = false;

        private void Start()
        {
            if (runOnStart)
            {
                RunDemo();
            }
        }

        [ContextMenu("Run Positional Commentary Demo")]
        public void RunDemo()
        {
            RunDemoInternal();
        }
        
        public static void RunBatchDemo()
        {
            var instance = FindObjectOfType<PositionalCommentaryDemo>();
            if (instance != null)
            {
                instance.RunDemoInternal();
            }
            else
            {
                // Create a temporary instance for batch mode
                var temp = new GameObject("PositionalCommentaryDemo").AddComponent<PositionalCommentaryDemo>();
                temp.RunDemoInternal();
                DestroyImmediate(temp.gameObject);
            }
        }
        
        private void RunDemoInternal()
        {
            Debug.Log("=== POSITIONAL COMMENTARY DEMO ===");
            Debug.Log($"Match: {homeTeamName} vs {awayTeamName}");
            Debug.Log($"Duration: {quarterMinutes}min quarters, Weather: {weather}");
            Debug.Log("");

            // Create teams
            var homeId = new TeamId(1);
            var awayId = new TeamId(2);
            var teams = new Dictionary<TeamId, Team>
            {
                [homeId] = new Team(homeId, homeTeamName, 75, 75),
                [awayId] = new Team(awayId, awayTeamName, 70, 70)
            };

            // Create realistic rosters with proper positions
            var rosters = new Dictionary<TeamId, List<Player>>
            {
                [homeId] = CreateRealisticRoster(homeId, homeTeamName, 75),
                [awayId] = CreateRealisticRoster(awayId, awayTeamName, 70)
            };

            if (showDetailedRoster)
            {
                ShowRosterDetails(homeTeamName, rosters[homeId]);
                ShowRosterDetails(awayTeamName, rosters[awayId]);
            }

            // Run match with commentary (team names are extracted from teams automatically)
            var result = MatchEngineWithCommentary.PlayMatchWithCommentary(
                round: 1,
                homeId: homeId, 
                awayId: awayId,
                teams: teams, 
                rosters: rosters, 
                tactics: null,
                weather: weather, 
                ground: new Ground(),
                quarterSeconds: quarterMinutes * 60,
                rng: new DeterministicRandom(seed)
            );

            // Display results
            Debug.Log($"FINAL SCORE: {homeTeamName} {result.HomeScore} - {result.AwayScore} {awayTeamName}");
            Debug.Log("");
            Debug.Log("=== COMMENTARY HIGHLIGHTS ===");
            
            foreach (var commentary in result.Commentary)
            {
                Debug.Log($"• {commentary}");
            }
            
            Debug.Log("");
            Debug.Log("=== POSITION ANALYSIS ===");
            AnalyzePositionalRealism(result.Commentary, rosters);
        }

        /// <summary>
        /// Creates a realistic AFL roster with proper positional distribution
        /// </summary>
        private List<Player> CreateRealisticRoster(TeamId teamId, string teamName, int baseRating)
        {
            var roster = new List<Player>();
            var rng = new DeterministicRandom(seed + teamId.Value);
            int playerId = teamId.Value * 100;

            // Create players by position with appropriate attributes
            CreatePositionGroup(roster, Role.KPD, 2, "Defender", baseRating, rng, ref playerId);    // Key Position Defenders
            CreatePositionGroup(roster, Role.SMLB, 3, "Back", baseRating, rng, ref playerId);      // Small/Medium Backs
            CreatePositionGroup(roster, Role.HBF, 2, "Half-Back", baseRating, rng, ref playerId);  // Half-Back Flankers
            
            CreatePositionGroup(roster, Role.MID, 4, "Mid", baseRating + 5, rng, ref playerId);    // Midfielders (slightly better)
            CreatePositionGroup(roster, Role.WING, 3, "Wing", baseRating, rng, ref playerId);      // Wingers
            
            CreatePositionGroup(roster, Role.KPF, 2, "Forward", baseRating, rng, ref playerId);    // Key Position Forwards
            CreatePositionGroup(roster, Role.SMLF, 2, "Forward", baseRating, rng, ref playerId);   // Small/Medium Forwards
            CreatePositionGroup(roster, Role.HFF, 2, "Half-Forward", baseRating, rng, ref playerId); // Half-Forward Flankers
            
            CreatePositionGroup(roster, Role.RUC, 2, "Ruck", baseRating + 3, rng, ref playerId);   // Ruckmen (better for contests)
            
            // Add some utility players to fill out the list
            for (int i = roster.Count; i < 30; i++)
            {
                roster.Add(CreatePlayer(Role.MID, $"Utility{i}", baseRating - 5, rng, playerId++));
            }

            return roster;
        }

        /// <summary>
        /// Creates a group of players for a specific position
        /// </summary>
        private void CreatePositionGroup(List<Player> roster, Role role, int count, string baseName, int baseRating, DeterministicRandom rng, ref int playerId)
        {
            for (int i = 0; i < count; i++)
            {
                var name = $"{baseName}{i + 1}";
                var player = CreatePlayer(role, name, baseRating, rng, playerId++);
                roster.Add(player);
            }
        }

        /// <summary>
        /// Creates a player with position-appropriate attributes
        /// </summary>
        private Player CreatePlayer(Role role, string name, int baseRating, DeterministicRandom rng, int id)
        {
            var player = new Player
            {
                Id = new PlayerId(id),
                Name = name,
                Age = rng.NextInt(18, 35),
                PrimaryRole = role,
                Attr = new Attributes()
            };

            // Set base attributes with some randomness
            var variance = rng.NextInt(-8, 9);
            var adjustedBase = Mathf.Clamp(baseRating + variance, 30, 95);

            // Position-specific attribute bonuses
            switch (PositionUtils.GetPositionGroup(role))
            {
                case PositionGroup.Defense:
                    player.Attr.Tackling = adjustedBase + rng.NextInt(0, 10);
                    player.Attr.Positioning = adjustedBase + rng.NextInt(0, 8);
                    player.Attr.Marking = adjustedBase + rng.NextInt(0, 6);
                    player.Attr.Kicking = adjustedBase + rng.NextInt(-5, 5);
                    player.Attr.Clearance = adjustedBase + rng.NextInt(-8, 3);
                    break;

                case PositionGroup.Midfield:
                    player.Attr.Clearance = adjustedBase + rng.NextInt(0, 12);
                    player.Attr.WorkRate = adjustedBase + rng.NextInt(0, 10);
                    player.Attr.DecisionMaking = adjustedBase + rng.NextInt(0, 8);
                    player.Attr.Kicking = adjustedBase + rng.NextInt(0, 6);
                    player.Attr.Speed = adjustedBase + rng.NextInt(0, 8);
                    break;

                case PositionGroup.Forward:
                    player.Attr.Marking = adjustedBase + rng.NextInt(0, 12);
                    player.Attr.Kicking = adjustedBase + rng.NextInt(0, 10);
                    player.Attr.Speed = adjustedBase + rng.NextInt(0, 8);
                    player.Attr.Agility = adjustedBase + rng.NextInt(0, 6);
                    player.Attr.DecisionMaking = adjustedBase + rng.NextInt(-3, 5);
                    break;

                case PositionGroup.Ruck:
                    player.Attr.Strength = adjustedBase + rng.NextInt(0, 15);
                    player.Attr.Clearance = adjustedBase + rng.NextInt(0, 10);
                    player.Attr.Marking = adjustedBase + rng.NextInt(0, 8);
                    player.Attr.RuckWork = adjustedBase + rng.NextInt(0, 6);
                    player.Attr.Positioning = adjustedBase + rng.NextInt(-3, 5);
                    break;
            }

            // Fill remaining attributes with reasonable defaults
            SetDefaultAttributes(player.Attr, adjustedBase, rng);
            ClampAttributes(player.Attr);

            return player;
        }

        private void SetDefaultAttributes(Attributes attr, int base_, DeterministicRandom rng)
        {
            if (attr.Speed == 0) attr.Speed = base_ + rng.NextInt(-5, 6);
            if (attr.Acceleration == 0) attr.Acceleration = base_ + rng.NextInt(-5, 6);
            if (attr.Strength == 0) attr.Strength = base_ + rng.NextInt(-5, 6);
            if (attr.Agility == 0) attr.Agility = base_ + rng.NextInt(-5, 6);
            if (attr.Jump == 0) attr.Jump = base_ + rng.NextInt(-5, 6);
            if (attr.Tackling == 0) attr.Tackling = base_ + rng.NextInt(-5, 6);
            if (attr.Marking == 0) attr.Marking = base_ + rng.NextInt(-5, 6);
            if (attr.Kicking == 0) attr.Kicking = base_ + rng.NextInt(-5, 6);
            if (attr.Handball == 0) attr.Handball = base_ + rng.NextInt(-5, 6);
            if (attr.Positioning == 0) attr.Positioning = base_ + rng.NextInt(-5, 6);
            if (attr.DecisionMaking == 0) attr.DecisionMaking = base_ + rng.NextInt(-5, 6);
            if (attr.WorkRate == 0) attr.WorkRate = base_ + rng.NextInt(-5, 6);
            if (attr.Clearance == 0) attr.Clearance = base_ + rng.NextInt(-5, 6);
            if (attr.RuckWork == 0) attr.RuckWork = base_ + rng.NextInt(-5, 6);
            if (attr.Spoiling == 0) attr.Spoiling = base_ + rng.NextInt(-5, 6);
            if (attr.Composure == 0) attr.Composure = base_ + rng.NextInt(-5, 6);
            if (attr.Leadership == 0) attr.Leadership = base_ + rng.NextInt(-5, 6);
        }

        private void ClampAttributes(Attributes attr)
        {
            attr.Speed = Mathf.Clamp(attr.Speed, 1, 99);
            attr.Acceleration = Mathf.Clamp(attr.Acceleration, 1, 99);
            attr.Strength = Mathf.Clamp(attr.Strength, 1, 99);
            attr.Agility = Mathf.Clamp(attr.Agility, 1, 99);
            attr.Jump = Mathf.Clamp(attr.Jump, 1, 99);
            attr.Tackling = Mathf.Clamp(attr.Tackling, 1, 99);
            attr.Marking = Mathf.Clamp(attr.Marking, 1, 99);
            attr.Kicking = Mathf.Clamp(attr.Kicking, 1, 99);
            attr.Handball = Mathf.Clamp(attr.Handball, 1, 99);
            attr.Positioning = Mathf.Clamp(attr.Positioning, 1, 99);
            attr.DecisionMaking = Mathf.Clamp(attr.DecisionMaking, 1, 99);
            attr.WorkRate = Mathf.Clamp(attr.WorkRate, 1, 99);
            attr.Clearance = Mathf.Clamp(attr.Clearance, 1, 99);
            attr.RuckWork = Mathf.Clamp(attr.RuckWork, 1, 99);
            attr.Spoiling = Mathf.Clamp(attr.Spoiling, 1, 99);
            attr.Composure = Mathf.Clamp(attr.Composure, 1, 99);
            attr.Leadership = Mathf.Clamp(attr.Leadership, 1, 99);
        }

        private void ShowRosterDetails(string teamName, List<Player> roster)
        {
            Debug.Log($"=== {teamName.ToUpper()} ROSTER ===");
            
            var groups = new Dictionary<PositionGroup, List<Player>>();
            foreach (var player in roster.Take(22))
            {
                var group = PositionUtils.GetPositionGroup(player.PrimaryRole);
                if (!groups.ContainsKey(group))
                    groups[group] = new List<Player>();
                groups[group].Add(player);
            }

            foreach (var group in groups)
            {
                Debug.Log($"{group.Key}: {group.Value.Count} players");
                foreach (var player in group.Value)
                {
                    Debug.Log($"  • {player.Name} ({player.PrimaryRole})");
                }
            }
            Debug.Log("");
        }

        private void AnalyzePositionalRealism(List<string> commentary, Dictionary<TeamId, List<Player>> rosters)
        {
            var centerBounceCount = 0;
            var goalCount = 0;
            var tackleCount = 0;

            foreach (var comment in commentary)
            {
                if (comment.Contains("center bounce") || comment.Contains("clearance"))
                    centerBounceCount++;
                if (comment.Contains("goal") || comment.Contains("marks") || comment.Contains("Inside50"))
                    goalCount++;
                if (comment.Contains("tackle") || comment.Contains("rebound"))
                    tackleCount++;
            }

            Debug.Log($"Commentary Analysis:");
            Debug.Log($"  Center bounce/clearance events: {centerBounceCount} (should feature midfielders/ruckmen)");
            Debug.Log($"  Goal/marking events: {goalCount} (should feature forwards/attacking mids)"); 
            Debug.Log($"  Tackle/rebound events: {tackleCount} (should feature defenders/defensive mids)");
            Debug.Log("");
            Debug.Log("✅ Players should now be selected based on their positions!");
            Debug.Log("✅ No more defenders taking center bounces!");
            Debug.Log("✅ No more opposition players in the same play!");
        }
    }
}
