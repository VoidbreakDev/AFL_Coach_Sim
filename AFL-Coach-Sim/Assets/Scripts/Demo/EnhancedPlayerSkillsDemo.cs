// File: Assets/Scripts/Demo/EnhancedPlayerSkillsDemo.cs
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems;
using System.Linq;

namespace AFLManager.Demo
{
    /// <summary>
    /// Demonstrates the integration of PlayerSkills.cs concepts with existing architecture.
    /// Shows how the new skill management system works with your existing Core domain model.
    /// </summary>
    public class EnhancedPlayerSkillsDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private EnhancedRosterManager rosterManager;

        void Start()
        {
            if (runOnStart)
            {
                RunSkillsDemo();
            }
        }

        [ContextMenu("Run Enhanced Skills Demo")]
        public void RunSkillsDemo()
        {
            Debug.Log("=== Enhanced Player Skills System Demo ===\n");

            // Ensure we have a roster manager
            if (rosterManager == null)
            {
                rosterManager = FindObjectOfType<EnhancedRosterManager>();
                if (rosterManager == null)
                {
                    Debug.LogError("No EnhancedRosterManager found. Please add one to the scene.");
                    return;
                }
            }

            DemoSkillManagement();
            DemoRosterFiltering();
            DemoPlayerAnalytics();
            DemoPositionAnalysis();
        }

        private void DemoSkillManagement()
        {
            Debug.Log("--- Skill Management Demo ---");

            // Create a test player
            var testPlayer = new Player
            {
                Name = "Test Player",
                Age = 25,
                Role = PlayerRole.Centre,
                Stats = new PlayerStats
                {
                    Kicking = 70,
                    Handballing = 75,
                    Tackling = 65,
                    Speed = 80,
                    Stamina = 85,
                    Knowledge = 70,
                    Playmaking = 75
                },
                Morale = 85f,
                Stamina = 90f,
                Contract = new ContractDetails { Salary = 300000f, YearsRemaining = 3 }
            };

            rosterManager.AddPlayer(testPlayer);

            // Demonstrate skill updates using PlayerSkills.cs naming conventions
            Debug.Log($"Original Overall Rating: {EnhancedPlayerOperations.CalculateOverallRating(testPlayer)}");

            // Update skills using PlayerSkills.cs method names
            EnhancedPlayerOperations.UpdatePlayerSkill(testPlayer, "kicking", 85);
            EnhancedPlayerOperations.UpdatePlayerSkill(testPlayer, "speed", 90);
            EnhancedPlayerOperations.UpdatePlayerSkill(testPlayer, "intelligence", 80); // Maps to DecisionMaking

            Debug.Log($"Updated Overall Rating: {EnhancedPlayerOperations.CalculateOverallRating(testPlayer)}");

            // Show detailed skill breakdown
            Debug.Log("Skill Breakdown:\n" + EnhancedPlayerOperations.GetPlayerSkillsBreakdown(testPlayer));
        }

        private void DemoRosterFiltering()
        {
            Debug.Log("--- Roster Filtering Demo ---");

            var allPlayers = rosterManager.GetAllPlayers();
            Debug.Log($"Total Players: {allPlayers.Count}");

            // Filter by position
            var midfielders = rosterManager.GetPlayersByPosition(PlayerRole.Centre);
            Debug.Log($"Midfielders: {midfielders.Count}");

            // Filter by skill level
            var goodKickers = rosterManager.GetPlayersBySkillLevel("kicking", 80);
            Debug.Log($"Players with Kicking >= 80: {goodKickers.Count}");

            // Top performers in specific skills
            var topKickers = rosterManager.GetTopPerformersInSkill("kicking", 3);
            Debug.Log($"Top 3 Kickers: {string.Join(", ", topKickers.Select(p => $"{p.Name} ({EnhancedPlayerOperations.GetPlayerSkill(p, "kicking")})"))}");

            // High morale players
            var highMoralePlayers = rosterManager.GetPlayersWithHighMorale(80f);
            Debug.Log($"High Morale Players: {highMoralePlayers.Count}");
        }

        private void DemoPlayerAnalytics()
        {
            Debug.Log("--- Player Analytics Demo ---");

            // Get roster statistics
            var stats = rosterManager.GetRosterStatistics();
            
            Debug.Log($"Roster Analytics:");
            Debug.Log($"  Total Players: {stats.TotalPlayers}");
            Debug.Log($"  Average Age: {stats.AverageAge}");
            Debug.Log($"  Average Rating: {stats.AverageRating:F1}");
            Debug.Log($"  Total Salary: ${stats.TotalSalary:F0}");
            Debug.Log($"  Salary Cap Remaining: ${stats.SalaryCapRemaining:F0}");

            // Show position breakdown
            Debug.Log("Position Breakdown:");
            foreach (var kvp in stats.PositionBreakdown)
            {
                if (kvp.Value > 0)
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }

            // Full roster skills breakdown
            Debug.Log("Full Roster Skills:\n" + rosterManager.GetRosterSkillsBreakdown());
        }

        private void DemoPositionAnalysis()
        {
            Debug.Log("--- Position Analysis Demo ---");

            // Analyze players for specific positions
            var centreOptions = rosterManager.GetPlayersForPosition(PlayerRole.Centre, 70);
            Debug.Log($"Players suitable for Centre position (min 70 rating): {centreOptions.Count}");

            foreach (var player in centreOptions.Take(3)) // Show top 3
            {
                var corePlayer = ConvertToCoreForDemo(player);
                int positionRating = PlayerSkillsAdapter.CalculatePositionRating(corePlayer);
                Debug.Log($"  {player.Name}: Position Rating {positionRating}, Overall {EnhancedPlayerOperations.CalculateOverallRating(player)}");
            }

            // Compare players across different positions
            var testPlayer = rosterManager.GetAllPlayers().FirstOrDefault();
            if (testPlayer != null)
            {
                var corePlayer = ConvertToCoreForDemo(testPlayer);
                Debug.Log($"Position suitability for {testPlayer.Name}:");
                
                foreach (PlayerRole role in System.Enum.GetValues(typeof(PlayerRole)))
                {
                    if (role != PlayerRole.Utility) // Skip utility for this demo
                    {
                        var coreRole = PlayerSkillsAdapter.MapUnityRoleToCoreRole(role);
                        int rating = corePlayer.Attr.CalculatePositionRating(coreRole);
                        Debug.Log($"  {role}: {rating}");
                    }
                }
            }
        }

        private AFLCoachSim.Core.Domain.Entities.Player ConvertToCoreForDemo(Player unityPlayer)
        {
            return new AFLCoachSim.Core.Domain.Entities.Player
            {
                Id = new AFLCoachSim.Core.Domain.ValueObjects.PlayerId(1),
                Name = unityPlayer.Name,
                Age = unityPlayer.Age,
                PrimaryRole = PlayerSkillsAdapter.MapUnityRoleToCoreRole(unityPlayer.Role),
                Attr = new AFLCoachSim.Core.Domain.Entities.Attributes
                {
                    Kicking = unityPlayer.Stats.Kicking,
                    Handball = unityPlayer.Stats.Handballing,
                    Tackling = unityPlayer.Stats.Tackling,
                    Speed = unityPlayer.Stats.Speed,
                    WorkRate = unityPlayer.Stats.Stamina,
                    DecisionMaking = unityPlayer.Stats.Knowledge,
                    Composure = unityPlayer.Stats.Playmaking
                }
            };
        }

        [ContextMenu("Create Sample Roster")]
        private void CreateSampleRosterForDemo()
        {
            var samplePlayers = new[]
            {
                CreateTestPlayer("Marcus Bontempelli", PlayerRole.Centre, 25, 88, 92, 75, 85, 90, 88, 85),
                CreateTestPlayer("Max Gawn", PlayerRole.Ruckman, 29, 65, 70, 80, 60, 85, 75, 70),
                CreateTestPlayer("Jeremy McGovern", PlayerRole.FullBack, 28, 70, 65, 85, 65, 80, 85, 75),
                CreateTestPlayer("Toby Greene", PlayerRole.FullForward, 27, 85, 80, 70, 75, 85, 80, 88),
                CreateTestPlayer("Lachie Neale", PlayerRole.Centre, 27, 85, 95, 80, 75, 88, 90, 92),
                CreateTestPlayer("Shannon Hurn", PlayerRole.HalfBackFlank, 32, 90, 75, 70, 65, 75, 88, 80),
                CreateTestPlayer("Nat Fyfe", PlayerRole.Centre, 29, 80, 85, 75, 80, 85, 85, 90),
                CreateTestPlayer("Tom Hawkins", PlayerRole.FullForward, 32, 85, 70, 75, 65, 80, 80, 85)
            };

            foreach (var player in samplePlayers)
            {
                rosterManager.AddPlayer(player);
            }

            Debug.Log($"Created sample roster with {samplePlayers.Length} star players");
        }

        private Player CreateTestPlayer(string name, PlayerRole role, int age,
            int kicking, int handballing, int tackling, int speed, int stamina, int knowledge, int playmaking)
        {
            return new Player
            {
                Name = name,
                Role = role,
                Age = age,
                Stats = new PlayerStats
                {
                    Kicking = kicking,
                    Handballing = handballing,
                    Tackling = tackling,
                    Speed = speed,
                    Stamina = stamina,
                    Knowledge = knowledge,
                    Playmaking = playmaking
                },
                Morale = Random.Range(75f, 95f),
                Stamina = Random.Range(85f, 100f),
                Contract = new ContractDetails
                {
                    Salary = Random.Range(400000f, 900000f),
                    YearsRemaining = Random.Range(2, 5)
                }
            };
        }

        [ContextMenu("Show Skill Comparisons")]
        private void ShowSkillComparisons()
        {
            Debug.Log("--- Skill Comparison Analysis ---");

            var players = rosterManager.GetAllPlayers().Take(5).ToList(); // Analyze first 5 players

            foreach (var player in players)
            {
                Debug.Log($"\n{player.Name} Skill Analysis:");
                Debug.Log($"  Overall Rating: {EnhancedPlayerOperations.CalculateOverallRating(player)}");
                Debug.Log($"  Position Rating: {GetPositionRating(player)}");
                Debug.Log($"  Key Strengths: {GetTopSkills(player, 3)}");
                Debug.Log($"  Areas for Improvement: {GetWeakestSkills(player, 2)}");
            }
        }

        private int GetPositionRating(Player player)
        {
            var corePlayer = ConvertToCoreForDemo(player);
            return PlayerSkillsAdapter.CalculatePositionRating(corePlayer);
        }

        private string GetTopSkills(Player player, int count)
        {
            var skills = new[]
            {
                ("Kicking", EnhancedPlayerOperations.GetPlayerSkill(player, "kicking")),
                ("Handballing", EnhancedPlayerOperations.GetPlayerSkill(player, "handballing")),
                ("Tackling", EnhancedPlayerOperations.GetPlayerSkill(player, "tackling")),
                ("Speed", EnhancedPlayerOperations.GetPlayerSkill(player, "speed")),
                ("Intelligence", EnhancedPlayerOperations.GetPlayerSkill(player, "intelligence"))
            };

            return string.Join(", ", skills
                .OrderByDescending(s => s.Item2)
                .Take(count)
                .Select(s => $"{s.Item1} ({s.Item2})"));
        }

        private string GetWeakestSkills(Player player, int count)
        {
            var skills = new[]
            {
                ("Kicking", EnhancedPlayerOperations.GetPlayerSkill(player, "kicking")),
                ("Handballing", EnhancedPlayerOperations.GetPlayerSkill(player, "handballing")),
                ("Tackling", EnhancedPlayerOperations.GetPlayerSkill(player, "tackling")),
                ("Speed", EnhancedPlayerOperations.GetPlayerSkill(player, "speed")),
                ("Intelligence", EnhancedPlayerOperations.GetPlayerSkill(player, "intelligence"))
            };

            return string.Join(", ", skills
                .OrderBy(s => s.Item2)
                .Take(count)
                .Select(s => $"{s.Item1} ({s.Item2})"));
        }
    }
}
