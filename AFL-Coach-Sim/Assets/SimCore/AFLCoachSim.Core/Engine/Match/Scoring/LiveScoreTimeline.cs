using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Timing;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Scoring
{
    /// <summary>
    /// Live score timeline system that tracks and displays score progression in real-time
    /// Provides detailed scoring events, momentum analysis, and dynamic scoreboard updates
    /// </summary>
    public class LiveScoreTimeline
    {
        private readonly List<ScoreEvent> _scoreEvents;
        private readonly Dictionary<TeamId, TeamScoreTracker> _teamScores;
        private readonly List<ScorelineMilestone> _milestones;
        private readonly ScorelineConfiguration _config;
        
        // Current state
        private int _currentHomeScore;
        private int _currentAwayScore;
        private TeamId _homeTeamId;
        private TeamId _awayTeamId;
        private float _currentGameTime;
        private int _currentQuarter;
        
        // Timeline analysis
        private readonly ScoreMomentumTracker _momentumTracker;
        private readonly ScoringPatternAnalyzer _patternAnalyzer;
        private readonly List<ScorelineInsight> _insights;
        
        public LiveScoreTimeline(TeamId homeTeamId, TeamId awayTeamId, ScorelineConfiguration config = null)
        {
            _homeTeamId = homeTeamId;
            _awayTeamId = awayTeamId;
            _config = config ?? ScorelineConfiguration.Default;
            
            _scoreEvents = new List<ScoreEvent>();
            _teamScores = new Dictionary<TeamId, TeamScoreTracker>
            {
                [homeTeamId] = new TeamScoreTracker(homeTeamId, true),
                [awayTeamId] = new TeamScoreTracker(awayTeamId, false)
            };
            _milestones = new List<ScorelineMilestone>();
            _insights = new List<ScorelineInsight>();
            
            _momentumTracker = new ScoreMomentumTracker();
            _patternAnalyzer = new ScoringPatternAnalyzer();
            
            InitializeTimeline();
        }
        
        /// <summary>
        /// Initialize the timeline at match start
        /// </summary>
        private void InitializeTimeline()
        {
            _currentHomeScore = 0;
            _currentAwayScore = 0;
            _currentGameTime = 0f;
            _currentQuarter = 1;
            
            // Create initial scoreline
            var initialEvent = new ScoreEvent
            {
                EventType = ScoreEventType.MatchStart,
                GameTime = 0f,
                Quarter = 1,
                HomeScore = 0,
                AwayScore = 0,
                Timestamp = DateTime.Now,
                Description = "Match commenced"
            };
            
            _scoreEvents.Add(initialEvent);
            
            // Initialize team trackers
            foreach (var tracker in _teamScores.Values)
            {
                tracker.Reset();
            }
            
            _momentumTracker.Initialize(DateTime.Now);
        }
        
        /// <summary>
        /// Update timeline with current match state
        /// </summary>
        public ScorelineUpdate UpdateTimeline(MatchSnapshot snapshot, TimingUpdate timingUpdate)
        {
            var update = new ScorelineUpdate
            {
                Timestamp = DateTime.Now,
                GameTime = timingUpdate.GameTimeElapsed,
                Quarter = timingUpdate.CurrentQuarter
            };
            
            _currentGameTime = timingUpdate.GameTimeElapsed;
            _currentQuarter = timingUpdate.CurrentQuarter;
            
            // Check for scoring changes
            bool homeScored = snapshot.HomePoints > _currentHomeScore;
            bool awayScored = snapshot.AwayPoints > _currentAwayScore;
            
            if (homeScored)
            {
                ProcessScoringEvent(_homeTeamId, snapshot, timingUpdate, update);
            }
            
            if (awayScored)
            {
                ProcessScoringEvent(_awayTeamId, snapshot, timingUpdate, update);
            }
            
            // Update current scores
            _currentHomeScore = snapshot.HomePoints;
            _currentAwayScore = snapshot.AwayPoints;
            
            // Analyze momentum
            update.MomentumUpdate = _momentumTracker.UpdateMomentum(snapshot, timingUpdate);
            
            // Check for milestones
            CheckForMilestones(snapshot, timingUpdate, update);
            
            // Analyze patterns
            AnalyzeScoringPatterns(update);
            
            // Generate insights
            GenerateInsights(update);
            
            return update;
        }
        
        /// <summary>
        /// Process a scoring event (goal or behind)
        /// </summary>
        private void ProcessScoringEvent(TeamId scoringTeam, MatchSnapshot snapshot, 
            TimingUpdate timingUpdate, ScorelineUpdate update)
        {
            var tracker = _teamScores[scoringTeam];
            bool isHome = scoringTeam.Equals(_homeTeamId);
            
            // Determine if goal or behind
            int newGoals = isHome ? snapshot.HomeGoals : snapshot.AwayGoals;
            int oldGoals = tracker.Goals;
            int newPoints = isHome ? snapshot.HomePoints : snapshot.AwayPoints;
            int oldPoints = tracker.TotalScore;
            
            ScoreEventType eventType;
            int pointsScored;
            
            if (newGoals > oldGoals)
            {
                eventType = ScoreEventType.Goal;
                pointsScored = 6;
            }
            else
            {
                eventType = ScoreEventType.Behind;
                pointsScored = 1;
            }
            
            // Create score event
            var scoreEvent = new ScoreEvent
            {
                EventType = eventType,
                ScoringTeam = scoringTeam,
                IsHomeTeam = isHome,
                GameTime = _currentGameTime,
                Quarter = _currentQuarter,
                HomeScore = _currentHomeScore + (isHome ? pointsScored : 0),
                AwayScore = _currentAwayScore + (!isHome ? pointsScored : 0),
                PointsScored = pointsScored,
                Margin = Math.Abs((_currentHomeScore + (isHome ? pointsScored : 0)) - 
                                 (_currentAwayScore + (!isHome ? pointsScored : 0))),
                Timestamp = DateTime.Now,
                Description = GenerateScoreDescription(eventType, scoringTeam, pointsScored),
                Phase = snapshot.Phase,
                TimeDisplay = timingUpdate.ToClockDisplay().FullDisplay
            };
            
            _scoreEvents.Add(scoreEvent);
            
            // Update team tracker
            tracker.AddScore(eventType, _currentGameTime, _currentQuarter);
            
            // Update scoreline update
            update.ScoringEvents.Add(scoreEvent);
            update.NewScore = true;
            update.ScoringTeam = scoringTeam;
            update.ScoreType = eventType;
            
            // Calculate scoring rate and trends
            CalculateScoringTrends(tracker, update);
            
            // Update momentum
            _momentumTracker.RecordScoringEvent(scoreEvent);
        }
        
        /// <summary>
        /// Check for significant milestones
        /// </summary>
        private void CheckForMilestones(MatchSnapshot snapshot, TimingUpdate timingUpdate, 
            ScorelineUpdate update)
        {
            var milestones = new List<ScorelineMilestone>();
            
            // Check score milestones
            CheckScoreMilestones(snapshot, milestones);
            
            // Check margin milestones
            CheckMarginMilestones(snapshot, milestones);
            
            // Check time milestones
            CheckTimeMilestones(timingUpdate, milestones);
            
            // Check comeback milestones
            CheckComebackMilestones(snapshot, milestones);
            
            if (milestones.Any())
            {
                _milestones.AddRange(milestones);
                update.NewMilestones.AddRange(milestones);
            }
        }
        
        /// <summary>
        /// Check for score-based milestones
        /// </summary>
        private void CheckScoreMilestones(MatchSnapshot snapshot, List<ScorelineMilestone> milestones)
        {
            // 100+ point milestones
            if (_currentHomeScore < 100 && snapshot.HomePoints >= 100)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.CenturyScore,
                    Team = _homeTeamId,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = $"{GetTeamName(_homeTeamId)} reaches 100 points",
                    Significance = MilestoneSignificance.Major
                });
            }
            
            if (_currentAwayScore < 100 && snapshot.AwayPoints >= 100)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.CenturyScore,
                    Team = _awayTeamId,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = $"{GetTeamName(_awayTeamId)} reaches 100 points",
                    Significance = MilestoneSignificance.Major
                });
            }
            
            // First score milestones
            if (_currentHomeScore == 0 && snapshot.HomePoints > 0)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.FirstScore,
                    Team = _homeTeamId,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = $"{GetTeamName(_homeTeamId)} opens the scoring",
                    Significance = MilestoneSignificance.Minor
                });
            }
            
            if (_currentAwayScore == 0 && snapshot.AwayPoints > 0)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.FirstScore,
                    Team = _awayTeamId,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = $"{GetTeamName(_awayTeamId)} gets on the board",
                    Significance = MilestoneSignificance.Minor
                });
            }
        }
        
        /// <summary>
        /// Check for margin-based milestones
        /// </summary>
        private void CheckMarginMilestones(MatchSnapshot snapshot, List<ScorelineMilestone> milestones)
        {
            int currentMargin = Math.Abs(_currentHomeScore - _currentAwayScore);
            int newMargin = Math.Abs(snapshot.HomePoints - snapshot.AwayPoints);
            
            // Large margin milestones
            foreach (int threshold in new[] { 30, 50, 70, 100 })
            {
                if (currentMargin < threshold && newMargin >= threshold)
                {
                    var leadingTeam = snapshot.HomePoints > snapshot.AwayPoints ? _homeTeamId : _awayTeamId;
                    milestones.Add(new ScorelineMilestone
                    {
                        Type = MilestoneType.LargeMargin,
                        Team = leadingTeam,
                        GameTime = _currentGameTime,
                        Quarter = _currentQuarter,
                        Description = $"{GetTeamName(leadingTeam)} leads by {threshold}+ points",
                        Significance = threshold >= 50 ? MilestoneSignificance.Major : MilestoneSignificance.Minor,
                        Value = threshold
                    });
                }
            }
            
            // Tied scores
            if (currentMargin > 0 && newMargin == 0)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.TiedScore,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = "Scores are tied",
                    Significance = MilestoneSignificance.Minor
                });
            }
        }
        
        /// <summary>
        /// Check for time-based milestones
        /// </summary>
        private void CheckTimeMilestones(TimingUpdate timingUpdate, List<ScorelineMilestone> milestones)
        {
            // Quarter milestones
            if (timingUpdate.NewQuarterStarted)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.QuarterStart,
                    GameTime = _currentGameTime,
                    Quarter = timingUpdate.CurrentQuarter,
                    Description = GetQuarterStartDescription(timingUpdate.CurrentQuarter),
                    Significance = timingUpdate.CurrentQuarter == 4 ? MilestoneSignificance.Major : MilestoneSignificance.Minor
                });
            }
            
            // Final minutes
            if (timingUpdate.IsMatchCriticalTime() && !HasMilestone(MilestoneType.FinalMinutes))
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.FinalMinutes,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = "Entering the final 5 minutes",
                    Significance = MilestoneSignificance.Major
                });
            }
        }
        
        /// <summary>
        /// Check for comeback milestones
        /// </summary>
        private void CheckComebackMilestones(MatchSnapshot snapshot, List<ScorelineMilestone> milestones)
        {
            // Detect significant lead changes
            bool homeWasLeading = _currentHomeScore > _currentAwayScore;
            bool awayWasLeading = _currentAwayScore > _currentHomeScore;
            bool homeNowLeading = snapshot.HomePoints > snapshot.AwayPoints;
            bool awayNowLeading = snapshot.AwayPoints > snapshot.HomePoints;
            
            if (homeWasLeading && awayNowLeading)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.LeadChange,
                    Team = _awayTeamId,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = $"{GetTeamName(_awayTeamId)} takes the lead",
                    Significance = _currentQuarter >= 3 ? MilestoneSignificance.Major : MilestoneSignificance.Minor
                });
            }
            else if (awayWasLeading && homeNowLeading)
            {
                milestones.Add(new ScorelineMilestone
                {
                    Type = MilestoneType.LeadChange,
                    Team = _homeTeamId,
                    GameTime = _currentGameTime,
                    Quarter = _currentQuarter,
                    Description = $"{GetTeamName(_homeTeamId)} takes the lead",
                    Significance = _currentQuarter >= 3 ? MilestoneSignificance.Major : MilestoneSignificance.Minor
                });
            }
        }
        
        /// <summary>
        /// Analyze scoring patterns and trends
        /// </summary>
        private void AnalyzeScoringPatterns(ScorelineUpdate update)
        {
            var homeTracker = _teamScores[_homeTeamId];
            var awayTracker = _teamScores[_awayTeamId];
            
            // Calculate scoring rates
            float totalTime = _currentGameTime / 60f; // Convert to minutes
            if (totalTime > 0)
            {
                update.HomeScoringRate = homeTracker.TotalScore / totalTime;
                update.AwayScoringRate = awayTracker.TotalScore / totalTime;
            }
            
            // Analyze recent scoring trends
            var recentEvents = _scoreEvents.Where(e => _currentGameTime - e.GameTime <= _config.RecentPeriodMinutes * 60f).ToList();
            update.RecentScoringTrend = CalculateScoringTrend(recentEvents);
            
            // Pattern analysis
            update.ScoringPatterns = _patternAnalyzer.AnalyzePatterns(_scoreEvents, _currentGameTime);
        }
        
        /// <summary>
        /// Generate insights about the scoreline
        /// </summary>
        private void GenerateInsights(ScorelineUpdate update)
        {
            var insights = new List<ScorelineInsight>();
            
            // Momentum insights
            if (update.MomentumUpdate?.MomentumStrength > 0.7f)
            {
                insights.Add(new ScorelineInsight
                {
                    Type = InsightType.Momentum,
                    Description = $"Strong momentum with {(update.MomentumUpdate.MomentumTeam.HasValue ? GetTeamName(update.MomentumUpdate.MomentumTeam.Value) : "Unknown Team")}",
                    Confidence = update.MomentumUpdate.MomentumStrength,
                    Context = "Recent scoring activity suggests significant momentum shift"
                });
            }
            
            // Scoring rate insights
            var homeRate = update.HomeScoringRate;
            var awayRate = update.AwayScoringRate;
            
            if (Math.Abs(homeRate - awayRate) > _config.SignificantScoringRateDifference)
            {
                var dominantTeam = homeRate > awayRate ? _homeTeamId : _awayTeamId;
                var dominantRate = Math.Max(homeRate, awayRate);
                
                insights.Add(new ScorelineInsight
                {
                    Type = InsightType.ScoringRate,
                    Description = $"{GetTeamName(dominantTeam)} scoring at {dominantRate:F1} points per minute",
                    Confidence = 0.8f,
                    Context = "Significant difference in scoring efficiency"
                });
            }
            
            // Close match insights
            int margin = Math.Abs(_currentHomeScore - _currentAwayScore);
            if (margin <= _config.CloseMatchThreshold && _currentGameTime > 30 * 60) // After 30 minutes
            {
                insights.Add(new ScorelineInsight
                {
                    Type = InsightType.CloseMatch,
                    Description = $"Very close contest - margin only {margin} points",
                    Confidence = 0.9f,
                    Context = "Match remains highly competitive"
                });
            }
            
            update.Insights.AddRange(insights);
            _insights.AddRange(insights);
        }
        
        /// <summary>
        /// Get comprehensive scoreline statistics
        /// </summary>
        public ScorelineStatistics GetScorelineStatistics()
        {
            return new ScorelineStatistics
            {
                TotalEvents = _scoreEvents.Count,
                TotalGoals = _scoreEvents.Count(e => e.EventType == ScoreEventType.Goal),
                TotalBehinds = _scoreEvents.Count(e => e.EventType == ScoreEventType.Behind),
                HomeScore = _currentHomeScore,
                AwayScore = _currentAwayScore,
                CurrentMargin = Math.Abs(_currentHomeScore - _currentAwayScore),
                LeadingTeam = _currentHomeScore > _currentAwayScore ? _homeTeamId : 
                             _currentAwayScore > _currentHomeScore ? _awayTeamId : null,
                
                // Team-specific stats
                HomeTracker = _teamScores[_homeTeamId],
                AwayTracker = _teamScores[_awayTeamId],
                
                // Timeline data
                ScoreEvents = new List<ScoreEvent>(_scoreEvents),
                Milestones = new List<ScorelineMilestone>(_milestones),
                Insights = new List<ScorelineInsight>(_insights),
                
                // Analysis
                ScoringPatterns = _patternAnalyzer.GetCurrentPatterns(),
                MomentumData = _momentumTracker.GetMomentumHistory(),
                
                // Calculated metrics
                AveragePointsPerEvent = _scoreEvents.Count > 0 ? 
                    (float)(_currentHomeScore + _currentAwayScore) / _scoreEvents.Count(e => e.EventType != ScoreEventType.MatchStart) : 0f,
                TotalScoringTime = _currentGameTime,
                GameCompletionPercentage = CalculateGameCompletionPercentage()
            };
        }
        
        /// <summary>
        /// Get current scoreboard display
        /// </summary>
        public ScoreboardDisplay GetScoreboardDisplay(TimingUpdate timingUpdate)
        {
            return new ScoreboardDisplay
            {
                HomeTeamScore = new TeamScoreDisplay
                {
                    TeamId = _homeTeamId,
                    TeamName = GetTeamName(_homeTeamId),
                    Goals = _teamScores[_homeTeamId].Goals,
                    Behinds = _teamScores[_homeTeamId].Behinds,
                    TotalScore = _currentHomeScore
                },
                AwayTeamScore = new TeamScoreDisplay
                {
                    TeamId = _awayTeamId,
                    TeamName = GetTeamName(_awayTeamId),
                    Goals = _teamScores[_awayTeamId].Goals,
                    Behinds = _teamScores[_awayTeamId].Behinds,
                    TotalScore = _currentAwayScore
                },
                Clock = new ClockDisplay
                {
                    Quarter = timingUpdate.CurrentQuarter,
                    TimeRemaining = timingUpdate.TimeRemaining,
                    QuarterDisplay = $"Q{timingUpdate.CurrentQuarter}",
                    TimeDisplay = timingUpdate.ToClockDisplay().TimeDisplay,
                    IsQuarterTime = timingUpdate.TimeRemaining <= 0 && timingUpdate.CurrentQuarter < 4,
                    IsHalfTime = timingUpdate.CurrentQuarter == 2 && timingUpdate.TimeRemaining <= 0,
                    IsThreeQuarterTime = timingUpdate.CurrentQuarter == 3 && timingUpdate.TimeRemaining <= 0,
                    IsFullTime = timingUpdate.CurrentQuarter == 4 && timingUpdate.TimeRemaining <= 0
                },
                Margin = Math.Abs(_currentHomeScore - _currentAwayScore),
                MarginTeam = _currentHomeScore > _currentAwayScore ? _homeTeamId :
                            _currentAwayScore > _currentHomeScore ? _awayTeamId : null,
                LastScoringEvent = _scoreEvents.LastOrDefault(e => e.EventType != ScoreEventType.MatchStart),
                MomentumIndicator = _momentumTracker.GetCurrentMomentum()
            };
        }
        
        /// <summary>
        /// Get timeline visualization data
        /// </summary>
        public TimelineVisualization GetTimelineVisualization()
        {
            return new TimelineVisualization
            {
                ScoreProgression = GenerateScoreProgressionPoints(),
                MomentumFlow = _momentumTracker.GetMomentumFlow(),
                SignificantMoments = _scoreEvents.Where(e => IsSignificantScoringEvent(e)).ToList(),
                QuarterBreakdowns = GenerateQuarterBreakdowns(),
                ScoringHeatMap = GenerateScoringHeatMap(),
                MarginHistory = GenerateMarginHistory()
            };
        }
        
        // Helper methods
        private void CalculateScoringTrends(TeamScoreTracker tracker, ScorelineUpdate update)
        {
            // Calculate recent scoring rate vs overall rate
            var recentEvents = tracker.ScoringEvents.Where(e => _currentGameTime - e.GameTime <= 300f).ToList(); // Last 5 minutes
            if (recentEvents.Any())
            {
                float recentRate = recentEvents.Sum(e => e.Points) / 5f; // Points per minute
                update.RecentScoringTrend = recentRate > tracker.AverageScoringRate ? ScoringTrend.Increasing : ScoringTrend.Decreasing;
            }
        }
        
        private ScoringTrend CalculateScoringTrend(List<ScoreEvent> events)
        {
            if (events.Count < 2) return ScoringTrend.Stable;
            
            var homeEvents = events.Where(e => e.IsHomeTeam).Sum(e => e.PointsScored);
            var awayEvents = events.Where(e => !e.IsHomeTeam).Sum(e => e.PointsScored);
            
            if (homeEvents > awayEvents * 1.5f) return ScoringTrend.HomeIncreasing;
            if (awayEvents > homeEvents * 1.5f) return ScoringTrend.AwayIncreasing;
            return ScoringTrend.Stable;
        }
        
        private string GenerateScoreDescription(ScoreEventType eventType, TeamId team, int points)
        {
            string teamName = GetTeamName(team);
            return eventType switch
            {
                ScoreEventType.Goal => $"{teamName} goal! (+{points} points)",
                ScoreEventType.Behind => $"{teamName} behind (+{points} point)",
                _ => $"{teamName} scores {points} point(s)"
            };
        }
        
        private string GetTeamName(TeamId teamId)
        {
            // This would typically come from team data
            return teamId.Equals(_homeTeamId) ? "Home" : "Away";
        }
        
        private string GetQuarterStartDescription(int quarter)
        {
            return quarter switch
            {
                2 => "Second quarter begins",
                3 => "Second half underway",
                4 => "Final quarter begins",
                _ => $"Quarter {quarter} begins"
            };
        }
        
        private bool HasMilestone(MilestoneType type)
        {
            return _milestones.Any(m => m.Type == type);
        }
        
        private bool IsSignificantScoringEvent(ScoreEvent scoreEvent)
        {
            return scoreEvent.EventType == ScoreEventType.Goal ||
                   scoreEvent.Margin <= 6 || // Close scores
                   scoreEvent.Quarter == 4; // Fourth quarter scores
        }
        
        private float CalculateGameCompletionPercentage()
        {
            // Assuming 4 quarters of 20 minutes each
            float totalExpectedTime = 4 * 20 * 60; // 80 minutes in seconds
            return Math.Min(1.0f, _currentGameTime / totalExpectedTime);
        }
        
        // Visualization helper methods
        private List<ScoreProgressionPoint> GenerateScoreProgressionPoints()
        {
            var points = new List<ScoreProgressionPoint>();
            
            foreach (var scoreEvent in _scoreEvents)
            {
                points.Add(new ScoreProgressionPoint
                {
                    GameTime = scoreEvent.GameTime,
                    Quarter = scoreEvent.Quarter,
                    HomeScore = scoreEvent.HomeScore,
                    AwayScore = scoreEvent.AwayScore,
                    EventType = scoreEvent.EventType,
                    ScoringTeam = scoreEvent.ScoringTeam
                });
            }
            
            return points;
        }
        
        private List<QuarterScoreBreakdown> GenerateQuarterBreakdowns()
        {
            var breakdowns = new List<QuarterScoreBreakdown>();
            
            for (int q = 1; q <= 4; q++)
            {
                var quarterEvents = _scoreEvents.Where(e => e.Quarter == q && e.EventType != ScoreEventType.MatchStart).ToList();
                
                breakdowns.Add(new QuarterScoreBreakdown
                {
                    Quarter = q,
                    HomePoints = quarterEvents.Where(e => e.IsHomeTeam).Sum(e => e.PointsScored),
                    AwayPoints = quarterEvents.Where(e => !e.IsHomeTeam).Sum(e => e.PointsScored),
                    TotalScores = quarterEvents.Count,
                    Goals = quarterEvents.Count(e => e.EventType == ScoreEventType.Goal),
                    Behinds = quarterEvents.Count(e => e.EventType == ScoreEventType.Behind)
                });
            }
            
            return breakdowns;
        }
        
        private Dictionary<int, int> GenerateScoringHeatMap()
        {
            var heatMap = new Dictionary<int, int>();
            
            // Create 5-minute time buckets
            foreach (var scoreEvent in _scoreEvents.Where(e => e.EventType != ScoreEventType.MatchStart))
            {
                int timeBucket = (int)(scoreEvent.GameTime / 300); // 5-minute buckets
                heatMap[timeBucket] = heatMap.GetValueOrDefault(timeBucket, 0) + 1;
            }
            
            return heatMap;
        }
        
        private List<MarginHistoryPoint> GenerateMarginHistory()
        {
            var history = new List<MarginHistoryPoint>();
            
            foreach (var scoreEvent in _scoreEvents)
            {
                int margin = scoreEvent.HomeScore - scoreEvent.AwayScore;
                history.Add(new MarginHistoryPoint
                {
                    GameTime = scoreEvent.GameTime,
                    Quarter = scoreEvent.Quarter,
                    Margin = margin,
                    LeadingTeam = margin > 0 ? _homeTeamId : margin < 0 ? _awayTeamId : null
                });
            }
            
            return history;
        }
    }
}