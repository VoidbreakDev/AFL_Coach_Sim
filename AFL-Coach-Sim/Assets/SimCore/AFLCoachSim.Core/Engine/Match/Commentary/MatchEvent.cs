using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Commentary
{
    /// <summary>
    /// Represents a significant event during a match that can be narrated
    /// </summary>
    public sealed class MatchEvent
    {
        public MatchEventType EventType { get; set; }
        public int Quarter { get; set; }
        public int TimeRemaining { get; set; }  // seconds remaining in quarter
        
        // Players involved
        public string PrimaryPlayerName { get; set; }
        public string SecondaryPlayerName { get; set; }
        public TeamId TeamId { get; set; }
        
        // Context
        public Phase Phase { get; set; }
        public Weather Weather { get; set; }
        public bool IsHomeTeam { get; set; }
        
        // Event-specific data
        public int? ScoreValue { get; set; }  // For goals/behinds
        public string ZoneDescription { get; set; }  // "from 40m out", "in the goal square"
        
        /// <summary>
        /// Formatted time display (e.g., "Q1, 17:22")
        /// </summary>
        public string TimeDisplay => $"Q{Quarter}, {TimeRemaining / 60}:{TimeRemaining % 60:D2}";
    }
    
    public enum MatchEventType
    {
        // Scoring
        Goal,
        Behind,
        RushedBehind,
        
        // Ball Movement
        CenterBounceWin,
        Clearance,
        Inside50Entry,
        Rebound50,
        
        // Individual Actions  
        Mark,
        SpectacularMark,
        Handball,
        Kick,
        Tackle,
        Turnover,
        
        // Set Pieces
        FreeKickFor,
        FreeKickAgainst,
        
        // Match Flow
        QuarterStart,
        QuarterEnd,
        
        // Player Events
        Injury,
        Substitution,
        
        // Weather/Conditions
        WeatherImpact
    }
}
