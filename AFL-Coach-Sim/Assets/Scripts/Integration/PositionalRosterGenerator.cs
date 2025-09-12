using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Integration
{
    /// <summary>
    /// Advanced roster generator that creates balanced teams with proper positional distribution
    /// and position-appropriate attributes
    /// </summary>
    public static class PositionalRosterGenerator
    {
        /// <summary>
        /// Generates a balanced roster with proper positional distribution
        /// </summary>
        public static List<Player> GenerateBalancedRoster(LeagueLevel leagueLevel, int targetSize = 30)
        {
            var roster = new List<Player>();
            var ideal = PositionUtils.GetIdealStructure();
            
            // Create position distribution for full roster (including depth)
            var distribution = new PositionDistribution
            {
                Defenders = Mathf.RoundToInt(ideal.Defenders * 1.4f), // Extra depth
                Midfielders = Mathf.RoundToInt(ideal.Midfielders * 1.3f),
                Forwards = Mathf.RoundToInt(ideal.Forwards * 1.3f),
                Ruckmen = ideal.Ruckmen + 1, // At least 3 rucks for depth
                Utility = 3 // Some versatile players
            };
            
            // Adjust if we need more/fewer players
            AdjustDistributionToTarget(distribution, targetSize);
            
            // Generate players by position
            roster.AddRange(GenerateDefenders(distribution.Defenders, leagueLevel));
            roster.AddRange(GenerateMidfielders(distribution.Midfielders, leagueLevel));
            roster.AddRange(GenerateForwards(distribution.Forwards, leagueLevel));
            roster.AddRange(GenerateRuckmen(distribution.Ruckmen, leagueLevel));
            roster.AddRange(GenerateUtilityPlayers(distribution.Utility, leagueLevel));
            
            // Fill any remaining spots with random positions
            while (roster.Count < targetSize)
            {
                var randomRole = GetRandomRole();
                roster.Add(GeneratePlayerForRole(randomRole, leagueLevel));
            }
            
            return roster.Take(targetSize).ToList();
        }
        
        /// <summary>
        /// Generates players for specific defensive roles
        /// </summary>
        private static List<Player> GenerateDefenders(int count, LeagueLevel level)
        {
            var defenders = new List<Player>();
            var roles = new[] { PlayerRole.FullBack, PlayerRole.FullBackFlank, PlayerRole.HalfBackFlank, PlayerRole.CentreHalfBack };
            
            for (int i = 0; i < count; i++)
            {
                var role = roles[i % roles.Length];
                defenders.Add(GeneratePlayerForRole(role, level));
            }
            
            return defenders;
        }
        
        /// <summary>
        /// Generates players for midfield roles
        /// </summary>
        private static List<Player> GenerateMidfielders(int count, LeagueLevel level)
        {
            var midfielders = new List<Player>();
            var roles = new[] { PlayerRole.Centre, PlayerRole.Wing };
            
            for (int i = 0; i < count; i++)
            {
                // Favor centres over wings (2:1 ratio)
                var role = i % 3 == 2 ? PlayerRole.Wing : PlayerRole.Centre;
                midfielders.Add(GeneratePlayerForRole(role, level));
            }
            
            return midfielders;
        }
        
        /// <summary>
        /// Generates players for forward roles
        /// </summary>
        private static List<Player> GenerateForwards(int count, LeagueLevel level)
        {
            var forwards = new List<Player>();
            var roles = new[] { PlayerRole.FullForward, PlayerRole.FullForwardFlank, PlayerRole.HalfForwardFlank, PlayerRole.CentreHalfForward };
            
            for (int i = 0; i < count; i++)
            {
                var role = roles[i % roles.Length];
                forwards.Add(GeneratePlayerForRole(role, level));
            }
            
            return forwards;
        }
        
        /// <summary>
        /// Generates ruckmen
        /// </summary>
        private static List<Player> GenerateRuckmen(int count, LeagueLevel level)
        {
            var ruckmen = new List<Player>();
            
            for (int i = 0; i < count; i++)
            {
                ruckmen.Add(GeneratePlayerForRole(PlayerRole.Ruck, level));
            }
            
            return ruckmen;
        }
        
        /// <summary>
        /// Generates utility players who can play multiple positions
        /// </summary>
        private static List<Player> GenerateUtilityPlayers(int count, LeagueLevel level)
        {
            var utility = new List<Player>();
            var roles = new[] { PlayerRole.HalfBackFlank, PlayerRole.Wing, PlayerRole.CentreHalfForward };
            
            for (int i = 0; i < count; i++)
            {
                var role = roles[i % roles.Length];
                var player = GeneratePlayerForRole(role, level);
                // Utility players have more balanced stats
                BalanceStats(player.Stats);
                utility.Add(player);
            }
            
            return utility;
        }
        
        /// <summary>
        /// Generates a player optimized for a specific role
        /// </summary>
        public static Player GeneratePlayerForRole(PlayerRole role, LeagueLevel level)
        {
            var player = new Player
            {
                Name = GeneratePlayerName(),
                Age = Random.Range(18, 35),
                State = GetRandomState(),
                Role = role,
                Morale = Random.Range(0.7f, 1.0f),
                Stamina = Random.Range(0.8f, 1.0f),
                PotentialCeiling = GetPotentialCeiling(level),
                Stats = GenerateStatsForRole(role, level),
                Contract = GenerateContract(level)
            };
            
            return player;
        }
        
        /// <summary>
        /// Generates stats optimized for a specific role
        /// </summary>
        private static PlayerStats GenerateStatsForRole(PlayerRole role, LeagueLevel level)
        {
            var baseRating = GetBaseRatingForLevel(level);
            var stats = new PlayerStats();
            
            // Add some randomness to base rating
            var variance = Random.Range(-8, 9);
            var adjustedBase = Mathf.Clamp(baseRating + variance, 30, 95);
            
            // Set role-appropriate stat bonuses
            switch (GetPositionGroupForRole(role))
            {
                case PositionGroup.Defense:
                    stats.Tackling = adjustedBase + Random.Range(5, 15);
                    stats.Knowledge = adjustedBase + Random.Range(3, 10);
                    stats.Kicking = adjustedBase + Random.Range(0, 8);
                    stats.Speed = adjustedBase + Random.Range(-5, 5);
                    stats.Stamina = adjustedBase + Random.Range(-3, 8);
                    stats.Handballing = adjustedBase + Random.Range(-3, 5);
                    stats.Playmaking = adjustedBase + Random.Range(-5, 5);
                    break;
                    
                case PositionGroup.Midfield:
                    stats.Stamina = adjustedBase + Random.Range(8, 18);
                    stats.Playmaking = adjustedBase + Random.Range(5, 12);
                    stats.Handballing = adjustedBase + Random.Range(5, 12);
                    stats.Speed = adjustedBase + Random.Range(3, 10);
                    stats.Kicking = adjustedBase + Random.Range(0, 8);
                    stats.Tackling = adjustedBase + Random.Range(-3, 8);
                    stats.Knowledge = adjustedBase + Random.Range(0, 8);
                    break;
                    
                case PositionGroup.Forward:
                    stats.Kicking = adjustedBase + Random.Range(8, 15);
                    stats.Speed = adjustedBase + Random.Range(5, 12);
                    stats.Playmaking = adjustedBase + Random.Range(3, 10);
                    stats.Handballing = adjustedBase + Random.Range(0, 8);
                    stats.Tackling = adjustedBase + Random.Range(-5, 5);
                    stats.Stamina = adjustedBase + Random.Range(-3, 8);
                    stats.Knowledge = adjustedBase + Random.Range(-3, 5);
                    break;
                    
                case PositionGroup.Ruck:
                    stats.Tackling = adjustedBase + Random.Range(5, 15);
                    stats.Knowledge = adjustedBase + Random.Range(3, 10);
                    stats.Stamina = adjustedBase + Random.Range(3, 12);
                    stats.Kicking = adjustedBase + Random.Range(-3, 8);
                    stats.Speed = adjustedBase + Random.Range(-10, -2); // Rucks are typically slower
                    stats.Handballing = adjustedBase + Random.Range(-5, 5);
                    stats.Playmaking = adjustedBase + Random.Range(-5, 5);
                    break;
                    
                default: // Utility
                    stats.Kicking = adjustedBase + Random.Range(-3, 8);
                    stats.Handballing = adjustedBase + Random.Range(-3, 8);
                    stats.Tackling = adjustedBase + Random.Range(-3, 8);
                    stats.Speed = adjustedBase + Random.Range(-3, 8);
                    stats.Stamina = adjustedBase + Random.Range(-3, 8);
                    stats.Knowledge = adjustedBase + Random.Range(-3, 8);
                    stats.Playmaking = adjustedBase + Random.Range(-3, 8);
                    break;
            }
            
            // Clamp all stats to valid ranges
            ClampStats(stats);
            
            return stats;
        }
        
        /// <summary>
        /// Balances stats for utility players
        /// </summary>
        private static void BalanceStats(PlayerStats stats)
        {
            var average = stats.GetAverage();
            var factor = 0.3f; // How much to balance (0 = no change, 1 = completely average)
            
            stats.Kicking = Mathf.RoundToInt(Mathf.Lerp(stats.Kicking, average, factor));
            stats.Handballing = Mathf.RoundToInt(Mathf.Lerp(stats.Handballing, average, factor));
            stats.Tackling = Mathf.RoundToInt(Mathf.Lerp(stats.Tackling, average, factor));
            stats.Speed = Mathf.RoundToInt(Mathf.Lerp(stats.Speed, average, factor));
            stats.Stamina = Mathf.RoundToInt(Mathf.Lerp(stats.Stamina, average, factor));
            stats.Knowledge = Mathf.RoundToInt(Mathf.Lerp(stats.Knowledge, average, factor));
            stats.Playmaking = Mathf.RoundToInt(Mathf.Lerp(stats.Playmaking, average, factor));
            
            ClampStats(stats);
        }
        
        /// <summary>
        /// Clamps all stats to 1-99 range
        /// </summary>
        private static void ClampStats(PlayerStats stats)
        {
            stats.Kicking = Mathf.Clamp(stats.Kicking, 1, 99);
            stats.Handballing = Mathf.Clamp(stats.Handballing, 1, 99);
            stats.Tackling = Mathf.Clamp(stats.Tackling, 1, 99);
            stats.Speed = Mathf.Clamp(stats.Speed, 1, 99);
            stats.Stamina = Mathf.Clamp(stats.Stamina, 1, 99);
            stats.Knowledge = Mathf.Clamp(stats.Knowledge, 1, 99);
            stats.Playmaking = Mathf.Clamp(stats.Playmaking, 1, 99);
        }
        
        // Helper methods
        private static void AdjustDistributionToTarget(PositionDistribution dist, int target)
        {
            var current = dist.TotalPlayers;
            var diff = target - current;
            
            if (diff == 0) return;
            
            if (diff > 0)
            {
                // Add players - distribute evenly
                for (int i = 0; i < diff; i++)
                {
                    switch (i % 4)
                    {
                        case 0: dist.Defenders++; break;
                        case 1: dist.Midfielders++; break;
                        case 2: dist.Forwards++; break;
                        case 3: dist.Utility++; break;
                    }
                }
            }
            else
            {
                // Remove players - remove from largest groups first
                for (int i = 0; i < -diff; i++)
                {
                    if (dist.Midfielders > 0) dist.Midfielders--;
                    else if (dist.Defenders > 0) dist.Defenders--;
                    else if (dist.Forwards > 0) dist.Forwards--;
                    else if (dist.Utility > 0) dist.Utility--;
                }
            }
        }
        
        private static PositionGroup GetPositionGroupForRole(PlayerRole role)
        {
            var coreRole = PlayerModelBridge.ToCore(role);
            return PositionUtils.GetPositionGroup(coreRole);
        }
        
        private static int GetBaseRatingForLevel(LeagueLevel level)
        {
            switch (level)
            {
                case LeagueLevel.Local: return 45;
                case LeagueLevel.Regional: return 55;
                case LeagueLevel.State: return 65;
                case LeagueLevel.Interstate: return 75;
                case LeagueLevel.AFL: return 85;
                default: return 60;
            }
        }
        
        private static float GetPotentialCeiling(LeagueLevel level)
        {
            var basePotential = GetBaseRatingForLevel(level);
            return basePotential + Random.Range(-10f, 20f);
        }
        
        private static PlayerRole GetRandomRole()
        {
            var roles = System.Enum.GetValues(typeof(PlayerRole)) as PlayerRole[];
            return roles[Random.Range(0, roles.Length)];
        }
        
        private static string GeneratePlayerName()
        {
            var firstNames = new[] { "Jack", "Sam", "Josh", "Tom", "Luke", "Ben", "Matt", "Jake", "Dan", "Alex", "Connor", "Lachlan", "Ryan", "James", "Michael" };
            var lastNames = new[] { "Smith", "Brown", "Williams", "Jones", "Taylor", "Davis", "Wilson", "Johnson", "Anderson", "Thompson", "Walker", "White", "Harris", "Martin", "Clark" };
            
            return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
        }
        
        private static string GetRandomState()
        {
            var states = new[] { "NSW", "VIC", "QLD", "WA", "SA", "TAS", "NT", "ACT" };
            return states[Random.Range(0, states.Length)];
        }
        
        private static ContractDetails GenerateContract(LeagueLevel level)
        {
            var baseWage = GetBaseRatingForLevel(level) * 1000f;
            return new ContractDetails
            {
                Salary = baseWage + Random.Range(-baseWage * 0.3f, baseWage * 0.5f),
                YearsRemaining = Random.Range(1, 4)
            };
        }
        
        /// <summary>
        /// Helper class for tracking positional distribution
        /// </summary>
        private class PositionDistribution
        {
            public int Defenders { get; set; }
            public int Midfielders { get; set; }
            public int Forwards { get; set; }
            public int Ruckmen { get; set; }
            public int Utility { get; set; }
            
            public int TotalPlayers => Defenders + Midfielders + Forwards + Ruckmen + Utility;
        }
    }
}
