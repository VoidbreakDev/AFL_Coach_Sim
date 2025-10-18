using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Timing;

namespace AFLCoachSim.Core.Engine.Match.Scoring
{
    /// <summary>
    /// Configuration for the scoreline system
    /// </summary>
    public class ScorelineConfiguration
    {
        public float RecentPeriodMinutes { get; set; } = 10f;
        public float SignificantScoringRateDifference { get; set; } = 2f; // Points per minute
        public int CloseMatchThreshold { get; set; } = 12; // Points
        public float MomentumDecayRate { get; set; } = 0.9f; // Per minute
        public int MomentumHistoryLength { get; set; } = 20; // Events to track
        
        public static ScorelineConfiguration Default { get; } = new ScorelineConfiguration();
    }
    
    /// <summary>
    /// Represents a scoring event in the match
    /// </summary>
    public class ScoreEvent
    {
        public ScoreEventType EventType { get; set; }
        public TeamId? ScoringTeam { get; set; }
        public bool IsHomeTeam { get; set; }
        public float GameTime { get; set; } // Seconds from match start
        public int Quarter { get; set; }
        public int HomeScore { get; set; } // Score after this event
        public int AwayScore { get; set; } // Score after this event
        public int PointsScored { get; set; } // Points from this event
        public int Margin { get; set; } // Absolute margin after this event
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public Phase Phase { get; set; }
        public string TimeDisplay { get; set; }
        public string PlayerId { get; set; } // If known
        public string PlayerName { get; set; } // If known
        
        // Additional properties for compatibility
        public TeamId? TeamId => ScoringTeam; // Alias for ScoringTeam
        public ScoreEventType ScoreType => EventType; // Alias for EventType
        public bool IsFirstScore { get; set; } // Whether this is the first score of the match
    }
    
    /// <summary>
    /// Tracks scoring statistics for a team
    /// </summary>
    public class TeamScoreTracker
    {
        public TeamId TeamId { get; set; }
        public bool IsHome { get; set; }
        public int Goals { get; set; }
        public int Behinds { get; set; }
        public int TotalScore => (Goals * 6) + Behinds;
        public List<ScoreEventRecord> ScoringEvents { get; set; }
        public float TotalScoringTime { get; set; }
        
        public TeamScoreTracker(TeamId teamId, bool isHome)
        {
            TeamId = teamId;
            IsHome = isHome;
            ScoringEvents = new List<ScoreEventRecord>();
        }
        
        public void Reset()
        {
            Goals = 0;
            Behinds = 0;
            ScoringEvents.Clear();
            TotalScoringTime = 0f;
        }
        
        public void AddScore(ScoreEventType eventType, float gameTime, int quarter)
        {
            int points = eventType == ScoreEventType.Goal ? 6 : 1;
            
            if (eventType == ScoreEventType.Goal)
                Goals++;
            else if (eventType == ScoreEventType.Behind)
                Behinds++;
            
            ScoringEvents.Add(new ScoreEventRecord
            {
                EventType = eventType,
                GameTime = gameTime,
                Quarter = quarter,
                Points = points,
                CumulativeScore = TotalScore
            });
            
            TotalScoringTime = gameTime;
        }
        
        public float AverageScoringRate => TotalScoringTime > 0 ? TotalScore / (TotalScoringTime / 60f) : 0f;
        public float GoalAccuracy => (Goals + Behinds) > 0 ? (float)Goals / (Goals + Behinds) : 0f;
        public int ScoringShots => Goals + Behinds;
        
        public List<ScoreEventRecord> GetQuarterEvents(int quarter)
        {
            return ScoringEvents.Where(e => e.Quarter == quarter).ToList();
        }
        
        public int GetQuarterScore(int quarter)
        {
            return GetQuarterEvents(quarter).Sum(e => e.Points);
        }
    }
    
    /// <summary>
    /// Individual scoring event record for detailed tracking
    /// </summary>
    public class ScoreEventRecord
    {
        public ScoreEventType EventType { get; set; }
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        public int Points { get; set; }
        public int CumulativeScore { get; set; }
    }
    
    /// <summary>
    /// Milestone achievement in the scoreline
    /// </summary>
    public class ScorelineMilestone
    {
        public MilestoneType Type { get; set; }
        public TeamId? Team { get; set; }
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        public string Description { get; set; }
        public MilestoneSignificance Significance { get; set; }
        public int? Value { get; set; } // Associated numeric value if relevant
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        // Additional property for compatibility
        public TeamId? TeamId => Team; // Alias for Team property
    }
    
    /// <summary>
    /// Insight about the scoreline
    /// </summary>
    public class ScorelineInsight
    {
        public InsightType Type { get; set; }
        public string Description { get; set; }
        public float Confidence { get; set; } // 0-1
        public string Context { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Update from the scoreline system
    /// </summary>
    public class ScorelineUpdate
    {
        public DateTime Timestamp { get; set; }
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        
        // Scoring information
        public bool NewScore { get; set; }
        public TeamId? ScoringTeam { get; set; }
        public ScoreEventType ScoreType { get; set; }
        public List<ScoreEvent> ScoringEvents { get; set; } = new List<ScoreEvent>();
        
        // Analysis
        public float HomeScoringRate { get; set; }
        public float AwayScoringRate { get; set; }
        public ScoringTrend RecentScoringTrend { get; set; }
        public List<ScoringPattern> ScoringPatterns { get; set; } = new List<ScoringPattern>();
        
        // Momentum
        public MomentumUpdate? MomentumUpdate { get; set; }
        
        // Milestones and insights
        public List<ScorelineMilestone> NewMilestones { get; set; } = new List<ScorelineMilestone>();
        public List<ScorelineInsight> Insights { get; set; } = new List<ScorelineInsight>();
    }
    
    /// <summary>
    /// Complete scoreline statistics
    /// </summary>
    public class ScorelineStatistics
    {
        public int TotalEvents { get; set; }
        public int TotalGoals { get; set; }
        public int TotalBehinds { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int CurrentMargin { get; set; }
        public TeamId? LeadingTeam { get; set; }
        
        public TeamScoreTracker HomeTracker { get; set; }
        public TeamScoreTracker AwayTracker { get; set; }
        
        public List<ScoreEvent> ScoreEvents { get; set; }
        public List<ScorelineMilestone> Milestones { get; set; }
        public List<ScorelineInsight> Insights { get; set; }
        
        public List<ScoringPattern> ScoringPatterns { get; set; }
        public List<MomentumHistoryPoint> MomentumData { get; set; }
        
        public float AveragePointsPerEvent { get; set; }
        public float TotalScoringTime { get; set; }
        public float GameCompletionPercentage { get; set; }
    }
    
    /// <summary>
    /// Scoreboard display information
    /// </summary>
    public class ScoreboardDisplay
    {
        public TeamScoreDisplay HomeTeamScore { get; set; }
        public TeamScoreDisplay AwayTeamScore { get; set; }
        public ClockDisplay Clock { get; set; }
        public int Margin { get; set; }
        public TeamId? MarginTeam { get; set; }
        public ScoreEvent? LastScoringEvent { get; set; }
        public MomentumIndicator MomentumIndicator { get; set; }
        
        public string MarginDisplay => Margin == 0 ? "TIED" : 
            MarginTeam.HasValue ? $"{GetTeamDisplayName(MarginTeam.Value)} by {Margin}" : "";
            
        private string GetTeamDisplayName(TeamId teamId)
        {
            // This would come from team data in a real implementation
            return HomeTeamScore.TeamId.Equals(teamId) ? HomeTeamScore.TeamName : AwayTeamScore.TeamName;
        }
    }
    
    /// <summary>
    /// Clock display information
    /// </summary>
    public class ClockDisplay
    {
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; }
        public string QuarterDisplay { get; set; }
        public string TimeDisplay { get; set; }
        public bool IsQuarterTime { get; set; }
        public bool IsHalfTime { get; set; }
        public bool IsThreeQuarterTime { get; set; }
        public bool IsFullTime { get; set; }
    }
    
    /// <summary>
    /// Individual team score display
    /// </summary>
    public class TeamScoreDisplay
    {
        public TeamId TeamId { get; set; }
        public string TeamName { get; set; }
        public int Goals { get; set; }
        public int Behinds { get; set; }
        public int TotalScore { get; set; }
        
        public string ScoreDisplay => $"{Goals}.{Behinds} ({TotalScore})";
        public string CompactDisplay => TotalScore.ToString();
    }
    
    /// <summary>
    /// Timeline visualization data
    /// </summary>
    public class TimelineVisualization
    {
        public List<ScoreProgressionPoint> ScoreProgression { get; set; }
        public List<MomentumFlowPoint> MomentumFlow { get; set; }
        public List<ScoreEvent> SignificantMoments { get; set; }
        public List<QuarterScoreBreakdown> QuarterBreakdowns { get; set; }
        public Dictionary<int, int> ScoringHeatMap { get; set; } // Time bucket -> score count
        public List<MarginHistoryPoint> MarginHistory { get; set; }
    }
    
    /// <summary>
    /// Point in score progression for visualization
    /// </summary>
    public class ScoreProgressionPoint
    {
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public ScoreEventType EventType { get; set; }
        public TeamId? ScoringTeam { get; set; }
    }
    
    /// <summary>
    /// Quarter-by-quarter score breakdown
    /// </summary>
    public class QuarterScoreBreakdown
    {
        public int Quarter { get; set; }
        public int HomePoints { get; set; }
        public int AwayPoints { get; set; }
        public int TotalScores { get; set; }
        public int Goals { get; set; }
        public int Behinds { get; set; }
        
        public TeamId? WinningTeam => HomePoints > AwayPoints ? null : // Would need actual team IDs
                                     AwayPoints > HomePoints ? null : null;
        public int Margin => Math.Abs(HomePoints - AwayPoints);
    }
    
    /// <summary>
    /// Margin history point for tracking lead changes
    /// </summary>
    public class MarginHistoryPoint
    {
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        public int Margin { get; set; }
        public TeamId? LeadingTeam { get; set; }
    }
    
    /// <summary>
    /// Tracks momentum based on scoring events
    /// </summary>
    public class ScoreMomentumTracker
    {
        private readonly List<MomentumHistoryPoint> _momentumHistory;
        private float _currentMomentum;
        private TeamId? _momentumTeam;
        private DateTime _lastUpdate;
        private readonly float _decayRate = 0.95f; // Per minute
        
        public ScoreMomentumTracker()
        {
            _momentumHistory = new List<MomentumHistoryPoint>();
        }
        
        public void Initialize(DateTime startTime)
        {
            _lastUpdate = startTime;
            _currentMomentum = 0f;
            _momentumTeam = null;
            _momentumHistory.Clear();
        }
        
        public MomentumUpdate UpdateMomentum(MatchSnapshot snapshot, TimingUpdate timingUpdate)
        {
            var now = DateTime.Now;
            var timeDiff = (float)(now - _lastUpdate).TotalMinutes;
            
            // Apply decay
            _currentMomentum *= (float)Math.Pow(_decayRate, timeDiff);
            
            var update = new MomentumUpdate
            {
                PreviousMomentum = _currentMomentum,
                PreviousMomentumTeam = _momentumTeam,
                MomentumStrength = Math.Abs(_currentMomentum),
                MomentumTeam = _currentMomentum > 0 ? _momentumTeam : null,
                DecayApplied = timeDiff * _decayRate
            };
            
            _lastUpdate = now;
            return update;
        }
        
        public void RecordScoringEvent(ScoreEvent scoreEvent)
        {
            float momentumImpact = CalculateMomentumImpact(scoreEvent);
            
            if (scoreEvent.ScoringTeam.HasValue)
            {
                if (_momentumTeam == null || _momentumTeam.Equals(scoreEvent.ScoringTeam))
                {
                    _currentMomentum += momentumImpact;
                    _momentumTeam = scoreEvent.ScoringTeam;
                }
                else
                {
                    // Momentum shift
                    _currentMomentum = momentumImpact - Math.Abs(_currentMomentum) * 0.5f;
                    _momentumTeam = scoreEvent.ScoringTeam;
                }
            }
            
            // Record in history
            _momentumHistory.Add(new MomentumHistoryPoint
            {
                Timestamp = scoreEvent.Timestamp,
                GameTime = scoreEvent.GameTime,
                Quarter = scoreEvent.Quarter,
                MomentumValue = _currentMomentum,
                MomentumTeam = _momentumTeam,
                TriggerEvent = scoreEvent.EventType,
                Impact = momentumImpact
            });
            
            // Maintain history size
            if (_momentumHistory.Count > 50)
            {
                _momentumHistory.RemoveAt(0);
            }
        }
        
        private float CalculateMomentumImpact(ScoreEvent scoreEvent)
        {
            float baseImpact = scoreEvent.EventType == ScoreEventType.Goal ? 0.3f : 0.1f;
            
            // Close games have more momentum impact
            if (scoreEvent.Margin <= 12) baseImpact *= 1.5f;
            
            // Fourth quarter scores have more impact
            if (scoreEvent.Quarter == 4) baseImpact *= 1.3f;
            
            // Late in quarters have more impact
            // This would require more detailed timing info
            
            return baseImpact;
        }
        
        public MomentumIndicator GetCurrentMomentum()
        {
            return new MomentumIndicator
            {
                Strength = Math.Abs(_currentMomentum),
                Team = _currentMomentum > 0 ? _momentumTeam : null,
                Direction = _currentMomentum > 0 ? MomentumDirection.Positive :
                           _currentMomentum < 0 ? MomentumDirection.Negative : MomentumDirection.Neutral,
                Trend = CalculateMomentumTrend()
            };
        }
        
        private MomentumTrend CalculateMomentumTrend()
        {
            if (_momentumHistory.Count < 3) return MomentumTrend.Stable;
            
            var recent = _momentumHistory.TakeLast(3).Select(h => Math.Abs(h.MomentumValue)).ToList();
            var trend = recent.Last() - recent.First();
            
            return trend > 0.1f ? MomentumTrend.Building :
                   trend < -0.1f ? MomentumTrend.Fading : MomentumTrend.Stable;
        }
        
        public List<MomentumHistoryPoint> GetMomentumHistory() => new List<MomentumHistoryPoint>(_momentumHistory);
        
        public List<MomentumFlowPoint> GetMomentumFlow()
        {
            return _momentumHistory.Select(h => new MomentumFlowPoint
            {
                GameTime = h.GameTime,
                Quarter = h.Quarter,
                MomentumValue = h.MomentumValue,
                MomentumTeam = h.MomentumTeam
            }).ToList();
        }
    }
    
    /// <summary>
    /// Analyzes scoring patterns
    /// </summary>
    public class ScoringPatternAnalyzer
    {
        private List<ScoringPattern> _currentPatterns = new List<ScoringPattern>();
        
        public List<ScoringPattern> AnalyzePatterns(List<ScoreEvent> scoreEvents, float currentGameTime)
        {
            _currentPatterns.Clear();
            
            // Analyze scoring bursts
            AnalyzeScoringBursts(scoreEvents);
            
            // Analyze droughts
            AnalyzeScoringDroughts(scoreEvents, currentGameTime);
            
            // Analyze alternating scores
            AnalyzeAlternatingScores(scoreEvents);
            
            return new List<ScoringPattern>(_currentPatterns);
        }
        
        private void AnalyzeScoringBursts(List<ScoreEvent> scoreEvents)
        {
            const float burstTimeWindow = 300f; // 5 minutes
            const int minBurstScores = 3;
            
            for (int i = 0; i < scoreEvents.Count; i++)
            {
                var startEvent = scoreEvents[i];
                if (startEvent.EventType == ScoreEventType.MatchStart) continue;
                
                var burstEvents = new List<ScoreEvent> { startEvent };
                
                for (int j = i + 1; j < scoreEvents.Count; j++)
                {
                    var nextEvent = scoreEvents[j];
                    if (nextEvent.GameTime - startEvent.GameTime > burstTimeWindow) break;
                    
                    if (nextEvent.ScoringTeam?.Equals(startEvent.ScoringTeam) == true)
                    {
                        burstEvents.Add(nextEvent);
                    }
                }
                
                if (burstEvents.Count >= minBurstScores)
                {
                    _currentPatterns.Add(new ScoringPattern
                    {
                        Type = PatternType.ScoringBurst,
                        Team = startEvent.ScoringTeam,
                        StartTime = startEvent.GameTime,
                        EndTime = burstEvents.Last().GameTime,
                        EventCount = burstEvents.Count,
                        TotalPoints = burstEvents.Sum(e => e.PointsScored),
                        Description = $"{burstEvents.Count} consecutive scores in {(burstEvents.Last().GameTime - startEvent.GameTime) / 60f:F1} minutes"
                    });
                    
                    i = scoreEvents.IndexOf(burstEvents.Last()); // Skip past this burst
                }
            }
        }
        
        private void AnalyzeScoringDroughts(List<ScoreEvent> scoreEvents, float currentGameTime)
        {
            const float droughtThreshold = 600f; // 10 minutes
            
            if (scoreEvents.Count < 2) return;
            
            for (int i = 1; i < scoreEvents.Count; i++)
            {
                var timeBetween = scoreEvents[i].GameTime - scoreEvents[i - 1].GameTime;
                if (timeBetween >= droughtThreshold)
                {
                    _currentPatterns.Add(new ScoringPattern
                    {
                        Type = PatternType.ScoringDrought,
                        StartTime = scoreEvents[i - 1].GameTime,
                        EndTime = scoreEvents[i].GameTime,
                        Duration = timeBetween,
                        Description = $"Scoring drought of {timeBetween / 60f:F1} minutes"
                    });
                }
            }
            
            // Check for current drought
            var lastScore = scoreEvents.Where(e => e.EventType != ScoreEventType.MatchStart).LastOrDefault();
            if (lastScore != null && currentGameTime - lastScore.GameTime >= droughtThreshold)
            {
                _currentPatterns.Add(new ScoringPattern
                {
                    Type = PatternType.ScoringDrought,
                    StartTime = lastScore.GameTime,
                    EndTime = currentGameTime,
                    Duration = currentGameTime - lastScore.GameTime,
                    Description = $"Current scoring drought of {(currentGameTime - lastScore.GameTime) / 60f:F1} minutes"
                });
            }
        }
        
        private void AnalyzeAlternatingScores(List<ScoreEvent> scoreEvents)
        {
            const int minAlternatingSequence = 4;
            
            var scoringEvents = scoreEvents.Where(e => e.EventType != ScoreEventType.MatchStart).ToList();
            if (scoringEvents.Count < minAlternatingSequence) return;
            
            for (int i = 0; i < scoringEvents.Count - minAlternatingSequence + 1; i++)
            {
                bool isAlternating = true;
                var sequence = scoringEvents.Skip(i).Take(minAlternatingSequence).ToList();
                
                for (int j = 1; j < sequence.Count; j++)
                {
                    if (sequence[j].ScoringTeam?.Equals(sequence[j - 1].ScoringTeam) == true)
                    {
                        isAlternating = false;
                        break;
                    }
                }
                
                if (isAlternating)
                {
                    _currentPatterns.Add(new ScoringPattern
                    {
                        Type = PatternType.AlternatingScores,
                        StartTime = sequence.First().GameTime,
                        EndTime = sequence.Last().GameTime,
                        EventCount = sequence.Count,
                        Description = $"Alternating scores between teams ({sequence.Count} events)"
                    });
                    
                    i += minAlternatingSequence - 1; // Skip past this sequence
                }
            }
        }
        
        public List<ScoringPattern> GetCurrentPatterns() => new List<ScoringPattern>(_currentPatterns);
    }
    
    /// <summary>
    /// Detected scoring pattern
    /// </summary>
    public class ScoringPattern
    {
        public PatternType Type { get; set; }
        public TeamId? Team { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public float Duration { get; set; } = 0f;
        public int EventCount { get; set; }
        public int TotalPoints { get; set; }
        public string Description { get; set; }
    }
    
    /// <summary>
    /// Momentum update information
    /// </summary>
    public class MomentumUpdate
    {
        public float PreviousMomentum { get; set; }
        public TeamId? PreviousMomentumTeam { get; set; }
        public float MomentumStrength { get; set; }
        public TeamId? MomentumTeam { get; set; }
        public float DecayApplied { get; set; }
        public bool MomentumShift => PreviousMomentumTeam != MomentumTeam && MomentumTeam != null;
    }
    
    /// <summary>
    /// Point in momentum history
    /// </summary>
    public class MomentumHistoryPoint
    {
        public DateTime Timestamp { get; set; }
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        public float MomentumValue { get; set; }
        public TeamId? MomentumTeam { get; set; }
        public ScoreEventType TriggerEvent { get; set; }
        public float Impact { get; set; }
    }
    
    /// <summary>
    /// Point in momentum flow for visualization
    /// </summary>
    public class MomentumFlowPoint
    {
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        public float MomentumValue { get; set; }
        public TeamId? MomentumTeam { get; set; }
    }
    
    /// <summary>
    /// Current momentum indicator
    /// </summary>
    public class MomentumIndicator
    {
        public float Strength { get; set; } // 0-1
        public TeamId? Team { get; set; }
        public MomentumDirection Direction { get; set; }
        public MomentumTrend Trend { get; set; }
        
        public bool HasMomentum => Strength > 0.1f;
        public bool StrongMomentum => Strength > 0.5f;
    }
    
    // Supporting enums
    public enum ScoreEventType
    {
        MatchStart,
        Goal,
        Behind,
        RushedBehind
    }
    
    public enum MilestoneType
    {
        FirstScore,
        CenturyScore,
        LargeMargin,
        TiedScore,
        LeadChange,
        QuarterStart,
        FinalMinutes,
        Comeback,
        // Additional milestone types
        LeadTaken,
        LeadRetaken,
        ScoreMilestone50,
        ScoreMilestone100,
        ScoringRun
    }
    
    public enum MilestoneSignificance
    {
        Minor,
        Major,
        Critical
    }
    
    public enum InsightType
    {
        Momentum,
        ScoringRate,
        CloseMatch,
        Blowout,
        Pattern,
        Trend
    }
    
    public enum ScoringTrend
    {
        Stable,
        Increasing,
        Decreasing,
        HomeIncreasing,
        AwayIncreasing
    }
    
    public enum PatternType
    {
        ScoringBurst,
        ScoringDrought,
        AlternatingScores,
        DominantQuarter,
        SlowStart,
        StrongFinish
    }
    
    public enum MomentumDirection
    {
        Positive,
        Negative,
        Neutral
    }
    
    public enum MomentumTrend
    {
        Building,
        Stable,
        Fading
    }
}