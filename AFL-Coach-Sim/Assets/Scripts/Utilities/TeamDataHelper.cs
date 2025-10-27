// Assets/Scripts/Utilities/TeamDataHelper.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Utilities
{
    /// <summary>
    /// Utility methods for working with team and player data
    /// </summary>
    public static class TeamDataHelper
    {
        /// <summary>
        /// Get all players from a team sorted by rating
        /// </summary>
        public static List<Player> GetPlayersByRating(Team team, bool descending = true)
        {
            if (team?.Roster == null) return new List<Player>();
            
            return descending
                ? team.Roster.OrderByDescending(p => p.Stats?.GetAverage() ?? 0).ToList()
                : team.Roster.OrderBy(p => p.Stats?.GetAverage() ?? 0).ToList();
        }
        
        /// <summary>
        /// Get players by position
        /// </summary>
        public static List<Player> GetPlayersByPosition(Team team, PlayerRole role)
        {
            if (team?.Roster == null) return new List<Player>();
            
            return team.Roster.Where(p => p.Role == role).ToList();
        }
        
        /// <summary>
        /// Get players by position category (defenders, midfielders, forwards, rucks)
        /// </summary>
        public static List<Player> GetPlayersByPositionCategory(Team team, PositionCategory category)
        {
            if (team?.Roster == null) return new List<Player>();
            
            return team.Roster.Where(p => GetPositionCategory(p.Role) == category).ToList();
        }
        
        /// <summary>
        /// Get position category for a role
        /// </summary>
        public static PositionCategory GetPositionCategory(PlayerRole role)
        {
            switch (role)
            {
                case PlayerRole.FullBack:
                case PlayerRole.BackPocket:
                case PlayerRole.HalfBack:
                case PlayerRole.FullBackFlank:
                case PlayerRole.HalfBackFlank:
                case PlayerRole.CentreHalfBack:
                    return PositionCategory.Defender;
                
                case PlayerRole.Wing:
                case PlayerRole.Centre:
                case PlayerRole.RuckRover:
                case PlayerRole.Rover:
                    return PositionCategory.Midfielder;
                
                case PlayerRole.HalfForward:
                case PlayerRole.HalfForwardFlank:
                case PlayerRole.ForwardPocket:
                case PlayerRole.FullForwardFlank:
                case PlayerRole.CentreHalfForward:
                case PlayerRole.FullForward:
                    return PositionCategory.Forward;
                
                case PlayerRole.Ruckman:
                case PlayerRole.Ruck:
                    return PositionCategory.Ruck;
                
                case PlayerRole.Utility:
                default:
                    return PositionCategory.Utility;
            }
        }
        
        /// <summary>
        /// Get best 22 players automatically
        /// </summary>
        public static List<Player> GetBest22(Team team)
        {
            if (team?.Roster == null) return new List<Player>();
            
            return team.Roster
                .OrderByDescending(p => p.Stats?.GetAverage() ?? 0)
                .Take(22)
                .ToList();
        }
        
        /// <summary>
        /// Validate if a lineup meets requirements
        /// </summary>
        public static (bool isValid, string error) ValidateLineup(List<Player> lineup)
        {
            if (lineup == null || lineup.Count != 22)
                return (false, "Lineup must have exactly 22 players");
            
            var categories = lineup.GroupBy(p => GetPositionCategory(p.Role));
            
            int defenders = categories.FirstOrDefault(g => g.Key == PositionCategory.Defender)?.Count() ?? 0;
            int midfielders = categories.FirstOrDefault(g => g.Key == PositionCategory.Midfielder)?.Count() ?? 0;
            int forwards = categories.FirstOrDefault(g => g.Key == PositionCategory.Forward)?.Count() ?? 0;
            int rucks = categories.FirstOrDefault(g => g.Key == PositionCategory.Ruck)?.Count() ?? 0;
            
            if (defenders < 6) return (false, $"Need at least 6 defenders (have {defenders})");
            if (midfielders < 6) return (false, $"Need at least 6 midfielders (have {midfielders})");
            if (forwards < 6) return (false, $"Need at least 6 forwards (have {forwards})");
            if (rucks < 2) return (false, $"Need at least 2 rucks (have {rucks})");
            
            return (true, "Lineup is valid");
        }
        
        /// <summary>
        /// Get team average rating
        /// </summary>
        public static float GetTeamAverageRating(Team team)
        {
            if (team?.Roster == null || team.Roster.Count == 0)
                return 0f;
            
            float sum = 0f;
            foreach (var player in team.Roster)
                sum += player.Stats?.GetAverage() ?? 0f;
            
            return sum / team.Roster.Count;
        }
        
        /// <summary>
        /// Get team average rating for specific lineup
        /// </summary>
        public static float GetLineupAverageRating(List<Player> lineup)
        {
            if (lineup == null || lineup.Count == 0)
                return 0f;
            
            float sum = 0f;
            foreach (var player in lineup)
                sum += player.Stats?.GetAverage() ?? 0f;
            
            return sum / lineup.Count;
        }
        
        /// <summary>
        /// Get color for position category
        /// </summary>
        public static Color GetPositionColor(PositionCategory category)
        {
            switch (category)
            {
                case PositionCategory.Defender:
                    return new Color(0.2f, 0.4f, 0.8f); // Blue
                case PositionCategory.Midfielder:
                    return new Color(0.2f, 0.7f, 0.3f); // Green
                case PositionCategory.Forward:
                    return new Color(0.9f, 0.3f, 0.2f); // Red
                case PositionCategory.Ruck:
                    return new Color(0.9f, 0.8f, 0.2f); // Yellow
                case PositionCategory.Utility:
                default:
                    return new Color(0.6f, 0.6f, 0.6f); // Gray
            }
        }
    }
    
    /// <summary>
    /// Position categories for lineup management
    /// </summary>
    public enum PositionCategory
    {
        Defender,
        Midfielder,
        Forward,
        Ruck,
        Utility
    }
}
