using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Simulation; // DeterministicRandom
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Rotations;
using AFLCoachSim.Core.Engine.Match.Injury;
using AFLCoachSim.Core.Engine.Match.Tuning;
using WeatherCondition = AFLCoachSim.Core.Engine.Match.Weather.Weather;

namespace AFLCoachSim.Core.Engine.Match
{
    public sealed class MatchContext
    {
        public MatchTuning Tuning;
        public Phase Phase = Phase.CenterBounce;
        public Score Score = new Score();
        public int Quarter = 1;
        public int TimeRemaining; // seconds in quarter
        public WeatherCondition Weather = WeatherCondition.Clear;
        public Ground Ground = new Ground();
        public TeamState Home, Away;
        public BallState Ball;
        public DeterministicRandom Rng;
        public TeamId LeadingTeamId => Score.HomePoints >= Score.AwayPoints ? Home.TeamId : Away.TeamId;

        // Runtime squads
        public List<PlayerRuntime> HomeOnField, HomeBench;
        public List<PlayerRuntime> AwayOnField, AwayBench;

        // Models
        public FatigueModel FatigueModel;
        public RotationManager RotationManager;
        public InjuryModel InjuryModel; // Modern injury model with unified injury system
        public Injury.MatchInjuryContextProvider InjuryContextProvider; // Provides match context to injury manager
        public MatchTelemetry Telemetry = new MatchTelemetry();
        
        // Additional properties for compatibility
        public int CrowdSize { get; set; } = 25000; // Default crowd size
        public string Venue { get; set; } = "MCG"; // Default venue
        public bool IsNightGame { get; set; } = false; // Day game by default
        public bool IsFinalSeries { get; set; } = false; // Regular season by default
        
        /// <summary>
        /// Get a player by ID from either team
        /// </summary>
        public Player GetPlayer(Guid playerId)
        {
            // Search home team players
            var homePlayer = HomeOnField?.FirstOrDefault(pr => pr.Player.Id == playerId)?.Player;
            if (homePlayer != null) return homePlayer;
            
            homePlayer = HomeBench?.FirstOrDefault(pr => pr.Player.Id == playerId)?.Player;
            if (homePlayer != null) return homePlayer;
            
            // Search away team players
            var awayPlayer = AwayOnField?.FirstOrDefault(pr => pr.Player.Id == playerId)?.Player;
            if (awayPlayer != null) return awayPlayer;
            
            awayPlayer = AwayBench?.FirstOrDefault(pr => pr.Player.Id == playerId)?.Player;
            return awayPlayer;
        }
        
        /// <summary>
        /// Get a player by PlayerId (overload for compatibility)
        /// </summary>
        public Player GetPlayer(PlayerId playerId)
        {
            return GetPlayer((Guid)playerId);
        }
        
        // Additional compatibility properties for legacy code
        public string CurrentLocation => GetCurrentLocation();
        public float CurrentPressure => GetCurrentPressure();
        public Guid MatchId { get; set; } = Guid.NewGuid();
        
        // Team name compatibility properties
        public string HomeTeam => Home?.TeamName;
        public string AwayTeam => Away?.TeamName;
        
        // Additional properties found in usage - trigger recompile
        public float HomeGroundAdvantage { get; set; } = 0.05f;
        public DateTime MatchStart { get; set; } = DateTime.Now;
        
        private string GetCurrentLocation()
        {
            if (Ground != null)
                return Ground.Name ?? Venue ?? "Unknown Ground";
            return Venue ?? "Unknown Ground";
        }
        
        private float GetCurrentPressure()
        {
            // Calculate pressure based on score differential and time remaining
            float pressureMultiplier = 1.0f;
            
            if (Score != null)
            {
                int scoreDiff = Math.Abs(Score.HomePoints - Score.AwayPoints);
                if (scoreDiff <= 6) // Close game
                    pressureMultiplier += 0.3f;
                else if (scoreDiff <= 18) // Moderate gap
                    pressureMultiplier += 0.1f;
            }
            
            // Increase pressure in final quarter
            if (Quarter == 4)
                pressureMultiplier += 0.2f;
                
            // Increase pressure with less time remaining
            if (TimeRemaining < 300) // Less than 5 minutes
                pressureMultiplier += 0.2f;
                
            return pressureMultiplier;
        }
        
        /// <summary>
        /// Log an event to the match telemetry - force recompile
        /// </summary>
        public void LogEvent(string eventDescription)
        {
            if (Telemetry != null)
            {
                // Add to telemetry if available
                Telemetry.Events = Telemetry.Events ?? new List<string>();
                Telemetry.Events.Add($"{Quarter}Q {TimeRemaining / 60:F0}:{TimeRemaining % 60:00} - {eventDescription}");
            }
        }
        
        /// <summary>
        /// Get all players for a specific team
        /// </summary>
        public List<Player> GetTeamPlayers(TeamId teamId)
        {
            var players = new List<Player>();
            
            if (Home?.TeamId == teamId)
            {
                if (HomeOnField != null)
                    players.AddRange(HomeOnField.Select(pr => pr.Player));
                if (HomeBench != null)
                    players.AddRange(HomeBench.Select(pr => pr.Player));
            }
            else if (Away?.TeamId == teamId)
            {
                if (AwayOnField != null)
                    players.AddRange(AwayOnField.Select(pr => pr.Player));
                if (AwayBench != null)
                    players.AddRange(AwayBench.Select(pr => pr.Player));
            }
            
            return players;
        }
        
        /// <summary>
        /// Get all players for a specific team (int overload)
        /// </summary>
        public List<Player> GetTeamPlayers(int teamId)
        {
            return GetTeamPlayers(new TeamId(teamId));
        }
        
        /// <summary>
        /// Get available substitutes for a team
        /// </summary>
        public List<Player> GetAvailableSubstitutes(int teamId)
        {
            var teamPlayers = GetTeamPlayers(teamId);
            return teamPlayers.Where(p => !p.IsOnField && !p.IsStartingPlayer).ToList();
        }
        
        /// <summary>
        /// Get score differential for a team (positive if ahead, negative if behind)
        /// </summary>
        public int GetScoreDifferential(int teamId)
        {
            if (Score == null) return 0;
            
            if (Home?.TeamId == teamId)
                return Score.HomePoints - Score.AwayPoints;
            else if (Away?.TeamId == teamId)
                return Score.AwayPoints - Score.HomePoints;
                
            return 0;
        }
        
        /// <summary>
        /// Get momentum rating for a team (0.0 to 1.0)
        /// </summary>
        public float GetMomentumRating(int teamId)
        {
            // TODO: Implement proper momentum calculation based on recent events
            // For now, return a neutral momentum value
            return 0.5f;
        }
        
        /// <summary>
        /// Get turnover differential for a team
        /// </summary>
        public int GetTurnoverDifferential(int teamId)
        {
            // TODO: Implement turnover tracking
            // For now, return 0 as placeholder
            return 0;
        }
        
        /// <summary>
        /// Get contested possession differential for a team
        /// </summary>
        public int GetContestedPossessionDifferential(int teamId)
        {
            // TODO: Implement contested possession tracking
            // For now, return 0 as placeholder
            return 0;
        }
        
        /// <summary>
        /// Check if currently at quarter break
        /// </summary>
        public bool IsQuarterBreak
        {
            get
            {
                // Quarter break if time remaining is 0 and not in final quarter
                return TimeRemaining <= 0 && Quarter < 4;
            }
        }
        
        /// <summary>
        /// Get current phase - compatibility property
        /// </summary>
        public Phase CurrentPhase => Phase;
        
        /// <summary>
        /// Get players in a specific area of the field
        /// </summary>
        public List<Player> GetPlayersInArea(string area)
        {
            var allPlayers = new List<Player>();
            
            // Add all on-field players
            if (HomeOnField != null)
                allPlayers.AddRange(HomeOnField.Select(pr => pr.Player));
            if (AwayOnField != null)
                allPlayers.AddRange(AwayOnField.Select(pr => pr.Player));
            
            // Filter by area - simplified implementation
            // In a full implementation, this would use actual field positioning
            switch (area)
            {
                case "Forward50":
                case "Attacking50":
                    return allPlayers.Where(p => p.Position.ToString().Contains("Forward")).ToList();
                case "Defensive50":
                case "Defence50":
                    return allPlayers.Where(p => p.Position.ToString().Contains("Back")).ToList();
                case "Midfield":
                    return allPlayers.Where(p => IsMiddleFieldPosition(p.Position)).ToList();
                default:
                    return allPlayers; // Return all players if area not recognized
            }
        }
        
        /// <summary>
        /// Get distance between two players (simplified implementation)
        /// </summary>
        public float GetDistanceBetweenPlayers(string player1Id, string player2Id)
        {
            // Simplified distance calculation based on positions
            // In a full implementation, this would use actual field coordinates
            var player1 = GetPlayer(Guid.Parse(player1Id));
            var player2 = GetPlayer(Guid.Parse(player2Id));
            
            if (player1 == null || player2 == null)
                return float.MaxValue;
            
            // Simple position-based distance estimation
            var pos1Weight = GetPositionWeight(player1.Position);
            var pos2Weight = GetPositionWeight(player2.Position);
            
            return Math.Abs(pos1Weight - pos2Weight) * 10f; // Arbitrary scaling
        }
        
        /// <summary>
        /// Get the current ball carrier
        /// </summary>
        public Player GetBallCarrier()
        {
            // In a full implementation, this would track actual ball possession
            // For now, return null to indicate no specific carrier is tracked
            return null;
        }
        
        /// <summary>
        /// Get the nearest opponent to a player
        /// </summary>
        public Player GetNearestOpponent(string playerId)
        {
            var player = GetPlayer(Guid.Parse(playerId));
            if (player == null) return null;
            
            var playerTeamId = GetPlayerTeamId(player);
            if (playerTeamId == null) return null;
            
            // Get opposing team players
            var opposingTeamId = playerTeamId == Home?.TeamId ? Away?.TeamId : Home?.TeamId;
            if (opposingTeamId == null) return null;
            
            var opponents = GetTeamPlayers(opposingTeamId.Value);
            
            // Return first opponent (simplified - in real implementation would calculate actual distances)
            return opponents.FirstOrDefault();
        }
        
        /// <summary>
        /// Check if player is near a goal
        /// </summary>
        public bool IsNearGoal(string playerId)
        {
            var player = GetPlayer(Guid.Parse(playerId));
            if (player == null) return false;
            
            // Simplified check based on position
            return player.Position.ToString().Contains("Forward") || player.Position.ToString().Contains("Back");
        }
        
        /// <summary>
        /// Get recent actions for a player
        /// </summary>
        public List<string> GetRecentPlayerActions(string playerId)
        {
            // Placeholder implementation - return empty list
            // In full implementation, would return actual player actions from telemetry
            return new List<string>();
        }
        
        // Helper methods
        private bool IsMiddleFieldPosition(Role position)
        {
            return position == Role.C || position == Role.RW || position == Role.LW || 
                   position == Role.RUCK || position == Role.RUC;
        }
        
        private float GetPositionWeight(Role position)
        {
            // Assign weights to positions for distance calculation
            return position switch
            {
                Role.FB => 1f,  // Full Back
                Role.CHB => 2f, // Center Half Back  
                Role.HBF => 3f, // Half Back Flank
                Role.C => 4f,   // Center
                Role.RW => 5f,  // Right Wing
                Role.LW => 5f,  // Left Wing
                Role.HFF => 6f, // Half Forward Flank
                Role.CHF => 7f, // Center Half Forward
                Role.FF => 8f,  // Full Forward
                Role.RUCK => 4f, // Ruck
                Role.RUC => 4f,  // Ruckman
                _ => 4f
            };
        }
        
        private TeamId? GetPlayerTeamId(Player player)
        {
            // Check if player is in home team
            if (HomeOnField?.Any(pr => pr.Player.Id == player.Id) == true ||
                HomeBench?.Any(pr => pr.Player.Id == player.Id) == true)
                return Home?.TeamId;
            
            // Check if player is in away team
            if (AwayOnField?.Any(pr => pr.Player.Id == player.Id) == true ||
                AwayBench?.Any(pr => pr.Player.Id == player.Id) == true)
                return Away?.TeamId;
            
            return null;
        }
    }
}
