using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Engine.Match.Scoring;
using AFLCoachSim.Core.Engine.Match.Timing;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Integration
{
    /// <summary>
    /// Configuration for commentary-scoreboard integration system
    /// </summary>
    public class CommentaryConfiguration
    {
        public int RecentCommentaryCount { get; set; } = 5;
        public float MomentumCommentaryDelay { get; set; } = 3.0f; // Seconds
        public int CloseMatchThreshold { get; set; } = 18; // Points
        public float CommentaryFrequencyLimit { get; set; } = 0.3f; // Per second max
        public bool EnableContextualCommentary { get; set; } = true;
        public bool EnableMomentumCommentary { get; set; } = true;
        public bool EnableMilestoneCommentary { get; set; } = true;
        public bool EnableTimingCommentary { get; set; } = true;
        public bool EnablePhaseCommentary { get; set; } = true;
        
        // Priority thresholds
        public TimeSpan HighPriorityDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MediumPriorityDelay { get; set; } = TimeSpan.FromSeconds(3);
        public TimeSpan LowPriorityDelay { get; set; } = TimeSpan.FromSeconds(5);
        
        public static CommentaryConfiguration Default => new CommentaryConfiguration();
    }
    
    /// <summary>
    /// Integrated match state that combines timing, scoring, and commentary context
    /// </summary>
    public class IntegratedMatchState
    {
        public TeamId HomeTeamId { get; }
        public TeamId AwayTeamId { get; }
        
        // Current state
        public Phase CurrentPhase { get; set; }
        public Phase PreviousPhase { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int CurrentQuarter { get; set; }
        public float GameTime { get; set; }
        public float TimeRemaining { get; set; }
        public bool InTimeOn { get; set; }
        
        // Commentary state tracking
        public bool HasFinalMinutesCommentary { get; set; }
        public DateTime LastMomentumCommentary { get; set; }
        public DateTime LastPhaseCommentary { get; set; }
        public DateTime LastSituationCommentary { get; set; }
        
        // Match context
        public int LeadChanges { get; set; }
        public float LargestLead { get; set; }
        public TeamId MomentumTeam { get; set; }
        public float MomentumStrength { get; set; }
        
        public IntegratedMatchState(TeamId homeTeamId, TeamId awayTeamId)
        {
            HomeTeamId = homeTeamId;
            AwayTeamId = awayTeamId;
            CurrentPhase = Phase.CenterBounce;
            PreviousPhase = Phase.CenterBounce;
            CurrentQuarter = 1;
        }
        
        public void UpdateState(MatchSnapshot snapshot, TimingUpdate timingUpdate)
        {
            PreviousPhase = CurrentPhase;
            CurrentPhase = snapshot.Phase;
            
            var prevHomeScore = HomeScore;
            var prevAwayScore = AwayScore;
            
            HomeScore = snapshot.HomePoints;
            AwayScore = snapshot.AwayPoints;
            CurrentQuarter = timingUpdate.CurrentQuarter;
            GameTime = timingUpdate.GameTimeElapsed;
            TimeRemaining = timingUpdate.TimeRemaining;
            InTimeOn = timingUpdate.InTimeOnPeriod;
            
            // Track lead changes
            var prevLeader = prevHomeScore > prevAwayScore ? HomeTeamId : 
                           prevAwayScore > prevHomeScore ? AwayTeamId : TeamId.None;
            var currentLeader = HomeScore > AwayScore ? HomeTeamId :
                              AwayScore > HomeScore ? AwayTeamId : TeamId.None;
                              
            if (prevLeader != currentLeader && currentLeader != TeamId.None)
            {
                LeadChanges++;
            }
            
            // Track largest lead
            var currentMargin = Math.Abs(HomeScore - AwayScore);
            if (currentMargin > LargestLead)
            {
                LargestLead = currentMargin;
            }
        }
        
        public TeamId GetLeadingTeam()
        {
            return HomeScore > AwayScore ? HomeTeamId :
                   AwayScore > HomeScore ? AwayTeamId : TeamId.None;
        }
        
        public int GetMargin()
        {
            return Math.Abs(HomeScore - AwayScore);
        }
        
        public bool IsCloseMatch(int threshold = 12)
        {
            return GetMargin() <= threshold;
        }
    }
    
    /// <summary>
    /// Priority levels for commentary events
    /// </summary>
    public enum CommentaryPriority
    {
        Critical,  // Must be shown immediately (goals, quarter transitions)
        High,      // Should be shown quickly (behinds, milestones)
        Medium,    // Can be delayed slightly (momentum, situational)
        Low        // Background commentary (phase changes, general)
    }
    
    /// <summary>
    /// Types of integrated commentary events
    /// </summary>
    public enum IntegratedEventType
    {
        Scoring,
        Milestone,
        QuarterTransition,
        Timing,
        Momentum,
        Situation,
        Phase,
        CriticalMoment
    }
    
    /// <summary>
    /// Pending commentary event awaiting delivery
    /// </summary>
    public class PendingCommentaryEvent
    {
        public string Commentary { get; set; }
        public CommentaryPriority Priority { get; set; }
        public IntegratedEventType Type { get; set; }
        public float DelaySeconds { get; set; } = 0f;
        public DateTime QueuedTime { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }
    
    /// <summary>
    /// Integrated commentary event with full context
    /// </summary>
    public class IntegratedCommentaryEvent
    {
        public string Commentary { get; set; }
        public DateTime Timestamp { get; set; }
        public CommentaryPriority Priority { get; set; }
        public IntegratedEventType Type { get; set; }
        public bool ShouldUpdateScoreboard { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        
        // Associated events
        public ScoreEvent ScoreEvent { get; set; }
        public ScorelineMilestone Milestone { get; set; }
        public TimingEvent TimingEvent { get; set; }
    }
    
    /// <summary>
    /// Update package containing all integrated information
    /// </summary>
    public class IntegratedUpdate
    {
        public DateTime Timestamp { get; set; }
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        
        // Updates
        public ScorelineUpdate ScorelineUpdate { get; set; }
        public ScoreboardDisplay ScoreboardDisplay { get; set; }
        public List<IntegratedCommentaryEvent> NewCommentaryEvents { get; set; } = new();
        public List<ScoreboardUpdate> ScoreboardUpdates { get; set; } = new();
        
        // State changes
        public bool HasNewScoring => ScorelineUpdate?.ScoringEvents.Count > 0;
        public bool HasNewMilestones => ScorelineUpdate?.NewMilestones.Count > 0;
        public bool HasNewCommentary => NewCommentaryEvents.Count > 0;
        public bool HasScoreboardChanges => ScoreboardUpdates.Count > 0;
    }
    
    /// <summary>
    /// Scoreboard update information
    /// </summary>
    public class ScoreboardUpdate
    {
        public DateTime Timestamp { get; set; }
        public ScoreboardUpdateType Type { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int Quarter { get; set; }
        public string TimeDisplay { get; set; }
        public string UpdateReason { get; set; }
        public bool RequiresAnimation { get; set; }
        public float AnimationDuration { get; set; } = 1.0f;
    }
    
    public enum ScoreboardUpdateType
    {
        ScoreChange,
        TimeUpdate,
        QuarterChange,
        PhaseChange,
        StatusUpdate
    }
    
    /// <summary>
    /// Complete integrated match data for analysis and display
    /// </summary>
    public class IntegratedMatchData
    {
        public IntegratedMatchState MatchState { get; set; }
        public ScorelineStatistics ScorelineStatistics { get; set; }
        public TimelineVisualization TimelineVisualization { get; set; }
        public List<IntegratedCommentaryEvent> CommentaryEvents { get; set; } = new();
        
        // Analytics
        public CommentaryMetrics CommentaryMetrics { get; set; }
        public SynchronizationMetrics SynchronizationMetrics { get; set; }
        public EngagementMetrics EngagementMetrics { get; set; }
        
        // Insights
        public List<string> MatchInsights { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();
    }
    
    /// <summary>
    /// Current display state for user interface
    /// </summary>
    public class IntegratedDisplayState
    {
        public ScoreboardDisplay ScoreboardDisplay { get; set; }
        public List<string> RecentCommentary { get; set; } = new();
        public string CurrentCommentary { get; set; }
        public Phase MatchPhase { get; set; }
        public MomentumIndicator MomentumIndicator { get; set; }
        public string SituationSummary { get; set; }
        public DateTime LastUpdate { get; set; }
        
        // Visual state
        public bool ShouldHighlightScore { get; set; }
        public bool ShouldAnimateCommentary { get; set; }
        public CommentaryPriority CurrentCommentaryPriority { get; set; }
    }
    
    /// <summary>
    /// Commentary system metrics
    /// </summary>
    public class CommentaryMetrics
    {
        public int TotalCommentaryEvents { get; set; }
        public Dictionary<IntegratedEventType, int> EventsByType { get; set; } = new();
        public float AverageEventsPerQuarter { get; set; }
        public DateTime? MostRecentEventTime { get; set; }
        public float CommentaryFrequency { get; set; } // Events per minute
        
        // Quality metrics
        public float ResponseTimeAccuracy { get; set; }
        public float ContextualRelevance { get; set; }
        public float EventCoverage { get; set; }
    }
    
    /// <summary>
    /// Synchronization quality metrics
    /// </summary>
    public class SynchronizationMetrics
    {
        public float AverageSyncDelay { get; set; } // Milliseconds
        public DateTime LastSyncTime { get; set; }
        public float SyncAccuracy { get; set; } // 0.0 to 1.0
        public int PendingCommentaryCount { get; set; }
        public int PendingScoreboardCount { get; set; }
        
        // Timing analysis
        public float AverageCommentaryDelay { get; set; }
        public float AverageScoreboardDelay { get; set; }
        public int MissedSyncOpportunities { get; set; }
    }
    
    /// <summary>
    /// User engagement metrics
    /// </summary>
    public class EngagementMetrics
    {
        public int HighPriorityEventCount { get; set; }
        public float EngagementScore { get; set; } // 0.0 to 1.0
        public float CommentaryVariety { get; set; } // Diversity of commentary types
        public float InteractivityLevel { get; set; } // How reactive the system is
        
        // Experience quality
        public float NarrativeFlow { get; set; }
        public float ContextualDepth { get; set; }
        public float EmotionalResonance { get; set; }
    }
    
    /// <summary>
    /// Enhanced commentary generator with context awareness
    /// </summary>
    public class EnhancedCommentaryGenerator
    {
        private readonly Dictionary<TeamId, List<Domain.Entities.Player>> _rosters;
        private readonly Dictionary<TeamId, string> _teamNames;
        private readonly CommentaryConfiguration _config;
        private readonly Random _random;
        
        public EnhancedCommentaryGenerator(
            Dictionary<TeamId, List<Domain.Entities.Player>> rosters,
            Dictionary<TeamId, string> teamNames,
            CommentaryConfiguration config)
        {
            _rosters = rosters ?? new Dictionary<TeamId, List<Domain.Entities.Player>>();
            _teamNames = teamNames ?? new Dictionary<TeamId, string>();
            _config = config ?? CommentaryConfiguration.Default;
            _random = new Random();
        }
        
        public string GenerateMatchStartCommentary()
        {
            var templates = new[]
            {
                "Welcome to today's match! Both teams are ready for what promises to be an exciting contest.",
                "The stage is set for another thrilling AFL encounter as the teams take the field.",
                "All the preparation is done - it's time for these two sides to battle it out on the field!"
            };
            
            return templates[_random.Next(templates.Length)];
        }
        
        public string GenerateScoringCommentary(ScoreEvent scoreEvent, IntegratedMatchState matchState)
        {
            var teamName = GetTeamName(scoreEvent.TeamId);
            var isGoal = scoreEvent.ScoreType == ScoreEventType.Goal;
            var margin = matchState.GetMargin();
            var quarter = scoreEvent.Quarter;
            
            if (isGoal)
            {
                if (margin <= 6)
                {
                    return $"GOAL! {teamName} strike back! It's neck and neck in the {GetQuarterName(quarter)}!";
                }
                else if (scoreEvent.IsFirstScore)
                {
                    return $"First blood to {teamName}! They get the early goal to start the {GetQuarterName(quarter)}!";
                }
                else
                {
                    return $"GOAL {teamName}! They extend their advantage with that major score!";
                }
            }
            else
            {
                if (margin <= 3)
                {
                    return $"Behind to {teamName}, but every point counts in this tight contest!";
                }
                else
                {
                    return $"A behind to {teamName} - they'll be frustrated not to convert that opportunity!";
                }
            }
        }
        
        public string GenerateMilestoneCommentary(ScorelineMilestone milestone, IntegratedMatchState matchState)
        {
            var teamName = GetTeamName(milestone.TeamId);
            
            return milestone.Type switch
            {
                MilestoneType.FirstScore => $"{teamName} get on the board first!",
                MilestoneType.LeadTaken => $"{teamName} take the lead for the first time!",
                MilestoneType.LeadRetaken => $"{teamName} fight back to regain the lead!",
                MilestoneType.ScoreMilestone50 => $"50 points to {teamName} - they're building a strong total!",
                MilestoneType.ScoreMilestone100 => $"Century on the board for {teamName}!",
                MilestoneType.ScoringRun => $"{teamName} are on fire with this scoring burst!",
                _ => $"Milestone moment for {teamName}!"
            };
        }
        
        public string GenerateMomentumCommentary(MomentumUpdate momentumUpdate, IntegratedMatchState matchState)
        {
            var teamName = GetTeamName(momentumUpdate.MomentumTeam);
            
            return momentumUpdate.MomentumStrength switch
            {
                > 0.8f => $"All the momentum is with {teamName} right now - they're dominating this period!",
                > 0.6f => $"{teamName} have seized the initiative and are controlling the tempo!",
                > 0.4f => $"The momentum is starting to swing toward {teamName}!",
                _ => $"The match is evenly poised with momentum shifting between both sides."
            };
        }
        
        public string GenerateCloseMatchCommentary(int margin, IntegratedMatchState matchState)
        {
            return margin switch
            {
                0 => "Dead level! This contest couldn't be any closer!",
                <= 6 => "A single goal the difference - this is heart-stopping stuff!",
                <= 12 => "Still anyone's game with this narrow margin!",
                _ => "The pressure is building with this tight scoreline!"
            };
        }
        
        public string GenerateQuarterStartCommentary(int quarter, IntegratedMatchState matchState)
        {
            return quarter switch
            {
                1 => "The opening bounce is away! The first quarter has begun!",
                2 => "Into the second quarter we go! Can the momentum continue?",
                3 => "The premiership quarter is underway! This is where matches can be won and lost!",
                4 => "Final quarter! Everything is on the line now!",
                _ => "The next phase of the match begins!"
            };
        }
        
        public string GenerateTimeOnCommentary(float timeOnDuration, IntegratedMatchState matchState)
        {
            return timeOnDuration > 5 
                ? "Significant time on being added after that stoppage - every second counts!"
                : "A bit of time on added to the clock.";
        }
        
        public string GenerateFinalMinutesCommentary(IntegratedMatchState matchState)
        {
            var margin = matchState.GetMargin();
            
            if (margin <= 6)
            {
                return "Final minutes now and this is anyone's game! The tension is unbearable!";
            }
            else if (margin <= 12)
            {
                return "Crunch time! These final minutes will determine the result!";
            }
            else
            {
                return "The final minutes are ticking away - can the trailing team mount one last charge?";
            }
        }
        
        public string GeneratePhaseCommentary(Phase phase, IntegratedMatchState matchState)
        {
            return phase switch
            {
                Phase.CenterBounce => "Back to the center for the next contest!",
                Phase.Inside50 => "Inside 50 - scoring opportunity developing!",
                Phase.ShotOnGoal => "Shot on goal! This could be crucial!",
                _ => ""
            };
        }
        
        public string GenerateEnhancedCommentary(MatchEvent matchEvent, MatchSnapshot snapshot, 
            TimingUpdate timingUpdate, IntegratedMatchState matchState)
        {
            // Generate contextual commentary based on the specific match event
            // This would be expanded with more sophisticated logic
            return $"Enhanced commentary for {matchEvent.GetType().Name} at {timingUpdate.GameTimeElapsed:F1}s";
        }
        
        private string GetTeamName(TeamId? teamId)
        {
            if (teamId.HasValue)
            {
                return _teamNames.TryGetValue(teamId.Value, out var name) ? name : teamId.Value.Value.ToString();
            }
            return "Unknown Team";
        }
        
        private string GetTeamName(TeamId teamId)
        {
            return _teamNames.TryGetValue(teamId, out var name) ? name : teamId.Value.ToString();
        }
        
        private string GetQuarterName(int quarter)
        {
            return quarter switch
            {
                1 => "opening quarter",
                2 => "second quarter", 
                3 => "third quarter",
                4 => "final quarter",
                _ => $"quarter {quarter}"
            };
        }
    }
}