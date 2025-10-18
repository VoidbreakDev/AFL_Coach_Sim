// Domain/Aggregates/Team.cs
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLCoachSim.Core.Domain.Aggregates
{
    public sealed class Team
    {
        public TeamId Id { get; }
        public string Name { get; }
        public int Attack { get; }   // 0..100 simple starter ratings
        public int Defense { get; }
        public List<Player> Players { get; set; } = new List<Player>(); // Team roster

        public Team(TeamId id, string name, int attack, int defense)
        {
            Id = id; Name = name;
            Attack = attack; Defense = defense;
            Players = new List<Player>();
        }
        
        public Team(TeamId id, string name, int attack, int defense, List<Player> players)
        {
            Id = id; Name = name;
            Attack = attack; Defense = defense;
            Players = players ?? new List<Player>();
        }

        /// <summary>
        /// Constructor accepting Guid for compatibility
        /// </summary>
        public Team(System.Guid id, string name)
        {
            Id = new TeamId(id.GetHashCode()); // Convert Guid to int using hash code
            Name = name;
            Attack = 75; // Default values
            Defense = 75;
            Players = new List<Player>();
        }

        // Properties for compatibility with example code
        public int AttackRating 
        { 
            get => Attack;
            set { /* Read-only in this implementation */ }
        }
        
        public int DefenseRating
        {
            get => Defense;
            set { /* Read-only in this implementation */ }
        }

        /// <summary>
        /// Add a player to the team roster
        /// </summary>
        public void AddPlayer(Player player)
        {
            if (player != null && !Players.Contains(player))
            {
                Players.Add(player);
                player.TeamId = Id; // Set the player's team ID
            }
        }

        /// <summary>
        /// Remove a player from the team roster
        /// </summary>
        public void RemovePlayer(Player player)
        {
            Players.Remove(player);
        }
    }
}
