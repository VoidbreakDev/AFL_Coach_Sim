using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLCoachSim.Core.Engine.Match.Timing
{
    /// <summary>
    /// Minimal stubs for timing-related classes to resolve compilation errors
    /// Note: TimingUpdate is now defined in MatchTimingModels.cs
    /// </summary>
    
    public class MatchContext
    {
        public int Quarter { get; set; }
        public int TimeRemaining { get; set; }
        public Phase CurrentPhase { get; set; }
        public bool IsCloseMatch { get; set; }
        public bool IsPlayerEngaged { get; set; }
        public float PlayerFatigueLevel { get; set; }
        public MatchTelemetry Telemetry { get; set; }
        public MatchScore Score { get; set; }
        
        // Additional properties for compatibility
        public bool IsNightGame { get; set; } = false;
        public bool IsFinalSeries { get; set; } = false;
        public int CrowdSize { get; set; } = 25000; // Default crowd size
        public string Venue { get; set; } = "MCG"; // Default venue
        
        // Mock methods for PlayerMatchupSystem compatibility
        public Player GetPlayer(string playerId) => null; // Stub implementation
        public Player GetPlayer(PlayerId playerId) => null; // Stub implementation
        public List<Player> GetPlayersInArea(string area) => new List<Player>(); // Stub implementation
        public Player GetBallCarrier() => null; // Stub implementation
        public Player GetNearestOpponent(Player player) => null; // Stub implementation
        public Player GetNearestOpponent(string playerId) => null; // Stub implementation overload
        public float GetDistanceBetweenPlayers(Player p1, Player p2) => 0f; // Stub implementation
        public float GetDistanceBetweenPlayers(string p1Id, string p2Id) => 0f; // Stub implementation overload
        public bool IsNearGoal(Player player) => false; // Stub implementation
        public bool IsNearGoal(string playerId) => false; // Stub implementation overload
        public List<string> GetRecentPlayerActions(Player player) => new List<string>(); // Stub implementation
        public List<string> GetRecentPlayerActions(string playerId, System.TimeSpan timeSpan) => new List<string>(); // Stub implementation overload
        
        // Additional methods for CoachingDecisionSystem compatibility
        public List<Player> GetAvailableSubstitutes() => new List<Player>(); // Stub implementation
        public List<Player> GetAvailableSubstitutes(int teamId) => new List<Player>(); // Stub implementation
        public List<Player> GetTeamPlayers(int teamId) => new List<Player>(); // Stub implementation
        public float GetScoreDifferential() => 0f; // Stub implementation
        public float GetScoreDifferential(int teamId) => 0f; // Stub implementation
        public float GetMomentumRating() => 0f; // Stub implementation
        public float GetMomentumRating(int teamId) => 0f; // Stub implementation
        public float GetTurnoverDifferential() => 0f; // Stub implementation
        public float GetTurnoverDifferential(int teamId) => 0f; // Stub implementation
        public float GetContestedPossessionDifferential() => 0f; // Stub implementation
        public float GetContestedPossessionDifferential(int teamId) => 0f; // Stub implementation
        public bool IsQuarterBreak() => false; // Stub implementation
        public void LogEvent(string message) { } // Stub implementation
    }
    
    public class MatchScore
    {
        public int HomePoints { get; set; }
        public int AwayPoints { get; set; }
    }
    
    public class MatchTelemetry
    {
        public int TotalGoals { get; set; }
        public int HomeInjuryEvents { get; set; }
        public int AwayInjuryEvents { get; set; }
    }
    
    // Configuration classes (minimal stubs)
    public class CompressedTimingConfiguration
    {
        public static CompressedTimingConfiguration Default => new CompressedTimingConfiguration();
    }
    
    public class VariableSpeedConfiguration  
    {
        public static VariableSpeedConfiguration Default => new VariableSpeedConfiguration();
    }
    
    public class EnhancedTimingConfiguration
    {
        public static EnhancedTimingConfiguration Default => new EnhancedTimingConfiguration();
    }
}