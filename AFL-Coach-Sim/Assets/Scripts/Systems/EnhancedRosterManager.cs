// File: Assets/Scripts/Systems/EnhancedRosterManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems;

namespace AFLManager.Systems
{
    /// <summary>
    /// Enhanced roster management system that provides PlayerSkills.cs-like functionality
    /// while respecting the existing architecture and working with PlayerData ScriptableObjects.
    /// </summary>
    public class EnhancedRosterManager : MonoBehaviour
    {
        [Header("Roster Management")]
        [SerializeField] private List<Player> roster = new List<Player>();
        [SerializeField] private List<Player> availablePlayers = new List<Player>();
        
        [Header("Configuration")]
        [SerializeField] private float salaryCap = 10000000f;
        [SerializeField] private int maxRosterSize = 44; // Standard AFL list size

        private Dictionary<string, Player> playerDatabase = new Dictionary<string, Player>();

        #region Unity Events
        
        void Awake()
        {
            InitializeRoster();
        }

        #endregion

        #region Roster Management

        /// <summary>
        /// Initializes the roster system 
        /// </summary>
        private void InitializeRoster()
        {
            roster = new List<Player>();
            availablePlayers = new List<Player>();
            playerDatabase = new Dictionary<string, Player>();
            
            Debug.Log("Enhanced Roster Manager initialized");
        }

        /// <summary>
        /// Adds a player to the roster system
        /// </summary>
        public bool AddPlayer(Player player)
        {
            if (player == null)
            {
                Debug.LogWarning("Cannot add null player");
                return false;
            }

            if (roster.Count >= maxRosterSize)
            {
                Debug.LogWarning($"Roster at maximum capacity ({maxRosterSize})");
                return false;
            }

            if (playerDatabase.ContainsKey(player.Id))
            {
                Debug.LogWarning($"Player with ID {player.Id} already exists");
                return false;
            }

            playerDatabase.Add(player.Id, player);
            roster.Add(player);
            
            if (IsPlayerAvailable(player))
                availablePlayers.Add(player);
                
            Debug.Log($"Added player: {player.Name}");
            return true;
        }

        /// <summary>
        /// Removes a player from the roster system
        /// </summary>
        public bool RemovePlayer(string playerId)
        {
            if (!playerDatabase.TryGetValue(playerId, out Player player))
            {
                Debug.LogWarning($"Player with ID {playerId} not found");
                return false;
            }

            roster.Remove(player);
            availablePlayers.Remove(player);
            playerDatabase.Remove(playerId);
            
            Debug.Log($"Removed player: {player.Name}");
            return true;
        }

        /// <summary>
        /// Gets a specific player by ID
        /// </summary>
        public Player GetPlayer(string playerId)
        {
            playerDatabase.TryGetValue(playerId, out Player player);
            return player;
        }

        /// <summary>
        /// Gets all players in the roster
        /// </summary>
        public List<Player> GetAllPlayers()
        {
            return new List<Player>(roster);
        }

        /// <summary>
        /// Gets available players for selection
        /// </summary>
        public List<Player> GetAvailablePlayers()
        {
            return new List<Player>(availablePlayers);
        }

        #endregion

        #region Advanced Filtering (inspired by PlayerSkills.cs)

        /// <summary>
        /// Filters players by position/role
        /// </summary>
        public List<Player> GetPlayersByPosition(PlayerRole position)
        {
            return roster.Where(p => p.Role == position).ToList();
        }

        /// <summary>
        /// Gets players within rating range
        /// </summary>
        public List<Player> GetPlayersByRatingRange(int minRating, int maxRating)
        {
            return roster.Where(p => 
            {
                int rating = EnhancedPlayerOperations.CalculateOverallRating(p);
                return rating >= minRating && rating <= maxRating;
            }).ToList();
        }

        /// <summary>
        /// Gets players with high stamina
        /// </summary>
        public List<Player> GetPlayersWithHighStamina(float minStamina = 80f)
        {
            return roster.Where(p => p.Stamina >= minStamina).ToList();
        }

        /// <summary>
        /// Gets players with high morale
        /// </summary>
        public List<Player> GetPlayersWithHighMorale(float minMorale = 80f)
        {
            return roster.Where(p => p.Morale >= minMorale).ToList();
        }

        /// <summary>
        /// Gets players by specific skill level
        /// </summary>
        public List<Player> GetPlayersBySkillLevel(string skillName, int minValue)
        {
            return roster.Where(p => 
                EnhancedPlayerOperations.GetPlayerSkill(p, skillName) >= minValue
            ).ToList();
        }

        /// <summary>
        /// Gets top performers in a specific skill
        /// </summary>
        public List<Player> GetTopPerformersInSkill(string skillName, int count = 5)
        {
            return roster
                .OrderByDescending(p => EnhancedPlayerOperations.GetPlayerSkill(p, skillName))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets players suitable for a specific position based on attributes
        /// </summary>
        public List<Player> GetPlayersForPosition(PlayerRole position, int minRating = 0)
        {
            return roster.Where(p => 
            {
                // Check if player's role matches or if they're utility players
                bool roleMatch = p.Role == position || p.Role == PlayerRole.Utility;
                
                // Check if they meet minimum rating for this position
                var corePlayer = ConvertToCorePlayerForAnalysis(p);
                var coreRole = PlayerSkillsAdapter.MapUnityRoleToCoreRole(position);
                int positionRating = corePlayer.Attr.CalculatePositionRating(coreRole);
                
                return roleMatch && positionRating >= minRating;
            }).ToList();
        }

        #endregion

        #region Roster Analytics

        /// <summary>
        /// Calculates total roster salary
        /// </summary>
        public float CalculateTotalSalary()
        {
            return roster.Sum(p => p.Contract?.Salary ?? 0f);
        }

        /// <summary>
        /// Gets roster statistics summary
        /// </summary>
        public RosterStatistics GetRosterStatistics()
        {
            var stats = new RosterStatistics
            {
                TotalPlayers = roster.Count,
                AvailablePlayers = availablePlayers.Count,
                TotalSalary = CalculateTotalSalary(),
                SalaryCapRemaining = salaryCap - CalculateTotalSalary()
            };

            if (roster.Count > 0)
            {
                stats.AverageAge = (int)roster.Average(p => p.Age);
                stats.AverageMorale = (float)roster.Average(p => p.Morale);
                stats.AverageStamina = (float)roster.Average(p => p.Stamina);
                stats.AverageRating = (float)roster.Average(p => EnhancedPlayerOperations.CalculateOverallRating(p));
                
                // Position breakdown
                stats.PositionBreakdown = System.Enum.GetValues(typeof(PlayerRole))
                    .Cast<PlayerRole>()
                    .ToDictionary(
                        role => role.ToString(), 
                        role => roster.Count(p => p.Role == role)
                    );
            }

            return stats;
        }

        /// <summary>
        /// Gets detailed skill breakdown for entire roster
        /// </summary>
        public string GetRosterSkillsBreakdown()
        {
            if (roster.Count == 0)
                return "No players in roster";

            var breakdown = "Roster Skills Breakdown:\n\n";
            
            foreach (var player in roster.OrderByDescending(p => EnhancedPlayerOperations.CalculateOverallRating(p)))
            {
                breakdown += $"{player.Name} (Overall: {EnhancedPlayerOperations.CalculateOverallRating(player)})\n";
                breakdown += $"  Role: {player.Role}, Age: {player.Age}\n";
                breakdown += $"  Key Skills - Kicking: {player.Stats.Kicking}, " +
                           $"Handballing: {player.Stats.Handballing}, " +
                           $"Tackling: {player.Stats.Tackling}\n\n";
            }

            return breakdown;
        }

        #endregion

        #region Player Management (inspired by PlayerSkills.cs methods)

        /// <summary>
        /// Updates a player's specific skill
        /// </summary>
        public bool UpdatePlayerSkill(string playerId, string skillName, int value)
        {
            var player = GetPlayer(playerId);
            if (player == null) return false;

            EnhancedPlayerOperations.UpdatePlayerSkill(player, skillName, value);
            Debug.Log($"Updated {player.Name}'s {skillName} to: {value}");
            return true;
        }

        /// <summary>
        /// Updates player availability status
        /// </summary>
        public bool SetPlayerAvailability(string playerId, bool isAvailable)
        {
            var player = GetPlayer(playerId);
            if (player == null) return false;

            // Update available players list
            if (isAvailable && !availablePlayers.Contains(player))
                availablePlayers.Add(player);
            else if (!isAvailable && availablePlayers.Contains(player))
                availablePlayers.Remove(player);
                
            Debug.Log($"{player.Name} availability set to: {isAvailable}");
            return true;
        }

        /// <summary>
        /// Updates player morale
        /// </summary>
        public bool UpdatePlayerMorale(string playerId, float value)
        {
            var player = GetPlayer(playerId);
            if (player == null) return false;

            player.Morale = Mathf.Clamp(value, 0f, 100f);
            Debug.Log($"Updated {player.Name}'s morale to: {value:F1}");
            return true;
        }

        /// <summary>
        /// Updates player stamina
        /// </summary>
        public bool UpdatePlayerStamina(string playerId, float value)
        {
            var player = GetPlayer(playerId);
            if (player == null) return false;

            player.Stamina = Mathf.Clamp(value, 0f, 100f);
            Debug.Log($"Updated {player.Name}'s stamina to: {value:F1}");
            return true;
        }

        #endregion

        #region Helper Methods

        private bool IsPlayerAvailable(Player player)
        {
            // Basic availability check - can be extended with injury status, suspensions, etc.
            return player.Morale > 20f && player.Stamina > 30f;
        }

        private AFLCoachSim.Core.Domain.Entities.Player ConvertToCorePlayerForAnalysis(Player unityPlayer)
        {
            // Convert for analysis purposes only
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

        #endregion

        #region Context Menu Commands (for testing)

        [ContextMenu("Generate Sample Roster")]
        private void GenerateSampleRoster()
        {
            // Clear existing roster
            roster.Clear();
            availablePlayers.Clear();
            playerDatabase.Clear();

            // Generate sample players
            var samplePlayers = new[]
            {
                CreateSamplePlayer("Jack Riewoldt", PlayerRole.FullForward, 30, 85, 75, 90, 70, 65, 80, 85),
                CreateSamplePlayer("Dustin Martin", PlayerRole.Centre, 29, 90, 85, 85, 85, 90, 75, 95),
                CreateSamplePlayer("Alex Rance", PlayerRole.FullBack, 31, 75, 70, 95, 60, 85, 90, 80),
                CreateSamplePlayer("Patrick Dangerfield", PlayerRole.Centre, 30, 85, 90, 80, 95, 85, 80, 90),
                CreateSamplePlayer("Lance Franklin", PlayerRole.FullForward, 34, 95, 80, 85, 75, 70, 85, 85)
            };

            foreach (var player in samplePlayers)
            {
                AddPlayer(player);
            }

            Debug.Log($"Generated sample roster with {roster.Count} players");
        }

        private Player CreateSamplePlayer(string name, PlayerRole role, int age, 
            int kicking, int handballing, int tackling, int speed, int stamina, int knowledge, int playmaking)
        {
            var player = new Player
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
                Morale = Random.Range(70f, 95f),
                Stamina = Random.Range(80f, 100f),
                Contract = new ContractDetails
                {
                    Salary = Random.Range(200000f, 800000f),
                    YearsRemaining = Random.Range(1, 4)
                }
            };

            return player;
        }

        [ContextMenu("Show Roster Statistics")]
        private void ShowRosterStatistics()
        {
            var stats = GetRosterStatistics();
            Debug.Log($"Roster Statistics:\n" +
                     $"Players: {stats.TotalPlayers} (Available: {stats.AvailablePlayers})\n" +
                     $"Average Age: {stats.AverageAge}\n" +
                     $"Average Rating: {stats.AverageRating:F1}\n" +
                     $"Total Salary: ${stats.TotalSalary:F0}\n" +
                     $"Salary Cap Remaining: ${stats.SalaryCapRemaining:F0}");
        }

        #endregion
    }

    /// <summary>
    /// Data structure for roster statistics
    /// </summary>
    [System.Serializable]
    public class RosterStatistics
    {
        public int TotalPlayers;
        public int AvailablePlayers;
        public int AverageAge;
        public float AverageRating;
        public float AverageMorale;
        public float AverageStamina;
        public float TotalSalary;
        public float SalaryCapRemaining;
        public Dictionary<string, int> PositionBreakdown = new Dictionary<string, int>();
    }
}
