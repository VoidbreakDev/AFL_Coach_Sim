using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Engine.Match.Scoring;
using AFLCoachSim.Core.Engine.Match.Timing;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Integration
{
    /// <summary>
    /// Integrates commentary system with scoreboard updates for synchronized,
    /// contextual commentary based on current match state and scoring events
    /// </summary>
    public class CommentaryScoreboardIntegration
    {
        private readonly CommentaryGenerator _commentaryGenerator;
        private readonly LiveScoreTimeline _scoreTimeline;
        private readonly EnhancedCommentaryGenerator _enhancedCommentary;
        private readonly IntegratedMatchState _matchState;
        private readonly List<IntegratedCommentaryEvent> _integratedEvents;
        private readonly CommentaryConfiguration _config;
        
        // Synchronization tracking
        private DateTime _lastCommentarySync;
        private DateTime _lastScoreboardSync;
        private float _lastGameTime;
        private int _lastQuarter;
        
        // Event queues for synchronized delivery
        private readonly Queue<PendingCommentaryEvent> _pendingCommentary;
        private readonly Queue<ScoreboardUpdate> _pendingScoreboardUpdates;
        
        public CommentaryScoreboardIntegration(
            TeamId homeTeamId,
            TeamId awayTeamId,
            Dictionary<TeamId, List<Domain.Entities.Player>> rosters,
            Dictionary<TeamId, string> teamNames,
            CommentaryConfiguration config = null)
        {
            _config = config ?? CommentaryConfiguration.Default;
            _commentaryGenerator = new CommentaryGenerator();
            _scoreTimeline = new LiveScoreTimeline(homeTeamId, awayTeamId);
            _enhancedCommentary = new EnhancedCommentaryGenerator(rosters, teamNames, _config);
            _matchState = new IntegratedMatchState(homeTeamId, awayTeamId);
            _integratedEvents = new List<IntegratedCommentaryEvent>();
            
            _pendingCommentary = new Queue<PendingCommentaryEvent>();
            _pendingScoreboardUpdates = new Queue<ScoreboardUpdate>();
            
            InitializeIntegration();
        }
        
        /// <summary>
        /// Initialize the integration system
        /// </summary>
        private void InitializeIntegration()
        {
            _lastCommentarySync = DateTime.Now;
            _lastScoreboardSync = DateTime.Now;
            _lastGameTime = 0f;
            _lastQuarter = 1;
            
            // Create initial commentary
            var initialCommentary = _enhancedCommentary.GenerateMatchStartCommentary();
            QueueCommentaryEvent(new PendingCommentaryEvent
            {
                Commentary = initialCommentary,
                Priority = CommentaryPriority.High,
                Type = IntegratedEventType.QuarterTransition
            });
        }
        
        /// <summary>
        /// Update the integrated system with current match state
        /// </summary>
        public IntegratedUpdate UpdateIntegration(MatchSnapshot snapshot, TimingUpdate timingUpdate)
        {
            var update = new IntegratedUpdate
            {
                Timestamp = DateTime.Now,
                GameTime = timingUpdate.GameTimeElapsed,
                Quarter = timingUpdate.CurrentQuarter
            };
            
            // Update internal match state
            _matchState.UpdateState(snapshot, timingUpdate);
            
            // Update score timeline and get scoreline update
            var scorelineUpdate = _scoreTimeline.UpdateTimeline(snapshot, timingUpdate);
            update.ScorelineUpdate = scorelineUpdate;
            
            // Generate commentary based on events
            ProcessCommentaryEvents(scorelineUpdate, timingUpdate, update);
            
            // Update scoreboard display
            var scoreboardDisplay = _scoreTimeline.GetScoreboardDisplay(timingUpdate);
            update.ScoreboardDisplay = scoreboardDisplay;
            
            // Generate contextual commentary
            GenerateContextualCommentary(snapshot, timingUpdate, scorelineUpdate, update);
            
            // Process synchronized events
            ProcessSynchronizedEvents(update);
            
            // Update timing
            _lastGameTime = timingUpdate.GameTimeElapsed;
            _lastQuarter = timingUpdate.CurrentQuarter;
            _lastCommentarySync = DateTime.Now;
            _lastScoreboardSync = DateTime.Now;
            
            return update;
        }
        
        /// <summary>
        /// Process commentary events from scoring and other match events
        /// </summary>
        private void ProcessCommentaryEvents(ScorelineUpdate scorelineUpdate, TimingUpdate timingUpdate, 
            IntegratedUpdate update)
        {
            // Handle scoring events
            foreach (var scoreEvent in scorelineUpdate.ScoringEvents)
            {
                var commentary = _enhancedCommentary.GenerateScoringCommentary(scoreEvent, _matchState);
                
                var integratedEvent = new IntegratedCommentaryEvent
                {
                    Commentary = commentary,
                    ScoreEvent = scoreEvent,
                    Timestamp = DateTime.Now,
                    Priority = CommentaryPriority.High,
                    Type = IntegratedEventType.Scoring,
                    ShouldUpdateScoreboard = true,
                    Context = CreateEventContext(scoreEvent, timingUpdate)
                };
                
                _integratedEvents.Add(integratedEvent);
                update.NewCommentaryEvents.Add(integratedEvent);
            }
            
            // Handle milestones
            foreach (var milestone in scorelineUpdate.NewMilestones)
            {
                var commentary = _enhancedCommentary.GenerateMilestoneCommentary(milestone, _matchState);
                
                var integratedEvent = new IntegratedCommentaryEvent
                {
                    Commentary = commentary,
                    Milestone = milestone,
                    Timestamp = DateTime.Now,
                    Priority = GetMilestonePriority(milestone),
                    Type = IntegratedEventType.Milestone,
                    ShouldUpdateScoreboard = false,
                    Context = CreateMilestoneContext(milestone, timingUpdate)
                };
                
                _integratedEvents.Add(integratedEvent);
                update.NewCommentaryEvents.Add(integratedEvent);
            }
            
            // Handle timing events
            ProcessTimingCommentary(timingUpdate, update);
        }
        
        /// <summary>
        /// Generate contextual commentary based on match situation
        /// </summary>
        private void GenerateContextualCommentary(MatchSnapshot snapshot, TimingUpdate timingUpdate,
            ScorelineUpdate scorelineUpdate, IntegratedUpdate update)
        {
            // Momentum commentary
            if (scorelineUpdate.MomentumUpdate?.MomentumShift == true)
            {
                var commentary = _enhancedCommentary.GenerateMomentumCommentary(
                    scorelineUpdate.MomentumUpdate, _matchState);
                
                QueueCommentaryEvent(new PendingCommentaryEvent
                {
                    Commentary = commentary,
                    Priority = CommentaryPriority.Medium,
                    Type = IntegratedEventType.Momentum,
                    DelaySeconds = _config.MomentumCommentaryDelay
                });
            }
            
            // Close match commentary
            var margin = Math.Abs(snapshot.HomePoints - snapshot.AwayPoints);
            if (margin <= _config.CloseMatchThreshold && timingUpdate.IsFinalQuarter())
            {
                if (ShouldGenerateCloseMatchCommentary())
                {
                    var commentary = _enhancedCommentary.GenerateCloseMatchCommentary(margin, _matchState);
                    QueueCommentaryEvent(new PendingCommentaryEvent
                    {
                        Commentary = commentary,
                        Priority = CommentaryPriority.Medium,
                        Type = IntegratedEventType.Situation
                    });
                }
            }
            
            // Phase-specific commentary
            if (HasPhaseChanged(snapshot.Phase))
            {
                var commentary = _enhancedCommentary.GeneratePhaseCommentary(snapshot.Phase, _matchState);
                if (!string.IsNullOrEmpty(commentary))
                {
                    QueueCommentaryEvent(new PendingCommentaryEvent
                    {
                        Commentary = commentary,
                        Priority = CommentaryPriority.Low,
                        Type = IntegratedEventType.Phase
                    });
                }
            }
        }
        
        /// <summary>
        /// Process timing-related commentary
        /// </summary>
        private void ProcessTimingCommentary(TimingUpdate timingUpdate, IntegratedUpdate update)
        {
            // Quarter transitions
            if (timingUpdate.NewQuarterStarted)
            {
                var commentary = _enhancedCommentary.GenerateQuarterStartCommentary(
                    timingUpdate.CurrentQuarter, _matchState);
                
                var integratedEvent = new IntegratedCommentaryEvent
                {
                    Commentary = commentary,
                    Timestamp = DateTime.Now,
                    Priority = GetQuarterStartPriority(timingUpdate.CurrentQuarter),
                    Type = IntegratedEventType.QuarterTransition,
                    ShouldUpdateScoreboard = true,
                    Context = CreateTimingContext(timingUpdate)
                };
                
                _integratedEvents.Add(integratedEvent);
                update.NewCommentaryEvents.Add(integratedEvent);
            }
            
            // Time-on periods
            if (timingUpdate.TimeOnStarted)
            {
                var commentary = _enhancedCommentary.GenerateTimeOnCommentary(
                    timingUpdate.TimeOnDuration, _matchState);
                
                QueueCommentaryEvent(new PendingCommentaryEvent
                {
                    Commentary = commentary,
                    Priority = CommentaryPriority.Medium,
                    Type = IntegratedEventType.Timing
                });
            }
            
            // Final minutes
            if (timingUpdate.IsMatchCriticalTime() && !_matchState.HasFinalMinutesCommentary)
            {
                var commentary = _enhancedCommentary.GenerateFinalMinutesCommentary(_matchState);
                
                _matchState.HasFinalMinutesCommentary = true;
                
                QueueCommentaryEvent(new PendingCommentaryEvent
                {
                    Commentary = commentary,
                    Priority = CommentaryPriority.High,
                    Type = IntegratedEventType.CriticalMoment
                });
            }
        }
        
        /// <summary>
        /// Process queued events for synchronized delivery
        /// </summary>
        private void ProcessSynchronizedEvents(IntegratedUpdate update)
        {
            // Process pending commentary events
            var now = DateTime.Now;
            var readyCommentary = new List<PendingCommentaryEvent>();
            
            while (_pendingCommentary.Count > 0)
            {
                var pending = _pendingCommentary.Peek();
                if ((now - pending.QueuedTime).TotalSeconds >= pending.DelaySeconds)
                {
                    readyCommentary.Add(_pendingCommentary.Dequeue());
                }
                else
                {
                    break; // Queue is ordered by time, so no more are ready
                }
            }
            
            // Convert ready commentary to integrated events
            foreach (var pendingEvent in readyCommentary)
            {
                var integratedEvent = new IntegratedCommentaryEvent
                {
                    Commentary = pendingEvent.Commentary,
                    Timestamp = now,
                    Priority = pendingEvent.Priority,
                    Type = pendingEvent.Type,
                    ShouldUpdateScoreboard = false,
                    Context = CreateDefaultContext()
                };
                
                _integratedEvents.Add(integratedEvent);
                update.NewCommentaryEvents.Add(integratedEvent);
            }
            
            // Process pending scoreboard updates
            while (_pendingScoreboardUpdates.Count > 0)
            {
                var scoreboardUpdate = _pendingScoreboardUpdates.Dequeue();
                update.ScoreboardUpdates.Add(scoreboardUpdate);
            }
        }
        
        /// <summary>
        /// Get comprehensive integrated data
        /// </summary>
        public IntegratedMatchData GetIntegratedData()
        {
            return new IntegratedMatchData
            {
                MatchState = _matchState,
                ScorelineStatistics = _scoreTimeline.GetScorelineStatistics(),
                TimelineVisualization = _scoreTimeline.GetTimelineVisualization(),
                CommentaryEvents = new List<IntegratedCommentaryEvent>(_integratedEvents),
                
                // Analytics
                CommentaryMetrics = CalculateCommentaryMetrics(),
                SynchronizationMetrics = CalculateSynchronizationMetrics(),
                EngagementMetrics = CalculateEngagementMetrics()
            };
        }
        
        /// <summary>
        /// Get current display state for UI
        /// </summary>
        public IntegratedDisplayState GetDisplayState(TimingUpdate timingUpdate)
        {
            var scoreboardDisplay = _scoreTimeline.GetScoreboardDisplay(timingUpdate);
            var recentCommentary = GetRecentCommentary(_config.RecentCommentaryCount);
            
            return new IntegratedDisplayState
            {
                ScoreboardDisplay = scoreboardDisplay,
                RecentCommentary = recentCommentary,
                CurrentCommentary = GetCurrentCommentary(),
                MatchPhase = _matchState.CurrentPhase,
                MomentumIndicator = scoreboardDisplay.MomentumIndicator,
                SituationSummary = GenerateSituationSummary(),
                LastUpdate = DateTime.Now
            };
        }
        
        /// <summary>
        /// Generate enhanced commentary with full context
        /// </summary>
        public string GenerateEnhancedCommentary(MatchEvent matchEvent, MatchSnapshot snapshot, 
            TimingUpdate timingUpdate)
        {
            return _enhancedCommentary.GenerateEnhancedCommentary(matchEvent, snapshot, timingUpdate, _matchState);
        }
        
        // Helper methods
        private void QueueCommentaryEvent(PendingCommentaryEvent commentaryEvent)
        {
            commentaryEvent.QueuedTime = DateTime.Now;
            _pendingCommentary.Enqueue(commentaryEvent);
        }
        
        private CommentaryPriority GetMilestonePriority(ScorelineMilestone milestone)
        {
            return milestone.Significance switch
            {
                MilestoneSignificance.Critical => CommentaryPriority.Critical,
                MilestoneSignificance.Major => CommentaryPriority.High,
                MilestoneSignificance.Minor => CommentaryPriority.Medium,
                _ => CommentaryPriority.Low
            };
        }
        
        private CommentaryPriority GetQuarterStartPriority(int quarter)
        {
            return quarter switch
            {
                4 => CommentaryPriority.Critical, // Final quarter
                3 => CommentaryPriority.High,    // Third quarter
                2 => CommentaryPriority.High,    // Second quarter  
                1 => CommentaryPriority.High,    // First quarter
                _ => CommentaryPriority.Medium
            };
        }
        
        private Dictionary<string, object> CreateEventContext(ScoreEvent scoreEvent, TimingUpdate timingUpdate)
        {
            return new Dictionary<string, object>
            {
                ["gameTime"] = scoreEvent.GameTime,
                ["quarter"] = scoreEvent.Quarter,
                ["phase"] = scoreEvent.Phase,
                ["margin"] = scoreEvent.Margin,
                ["clockRunning"] = timingUpdate.ClockRunning,
                ["timeRemaining"] = timingUpdate.TimeRemaining
            };
        }
        
        private Dictionary<string, object> CreateMilestoneContext(ScorelineMilestone milestone, 
            TimingUpdate timingUpdate)
        {
            return new Dictionary<string, object>
            {
                ["milestoneType"] = milestone.Type,
                ["significance"] = milestone.Significance,
                ["gameTime"] = milestone.GameTime,
                ["quarter"] = milestone.Quarter,
                ["timeRemaining"] = timingUpdate.TimeRemaining
            };
        }
        
        private Dictionary<string, object> CreateTimingContext(TimingUpdate timingUpdate)
        {
            return new Dictionary<string, object>
            {
                ["quarter"] = timingUpdate.CurrentQuarter,
                ["timeRemaining"] = timingUpdate.TimeRemaining,
                ["inTimeOn"] = timingUpdate.InTimeOnPeriod,
                ["clockRunning"] = timingUpdate.ClockRunning
            };
        }
        
        private Dictionary<string, object> CreateDefaultContext()
        {
            return new Dictionary<string, object>
            {
                ["gameTime"] = _lastGameTime,
                ["quarter"] = _lastQuarter,
                ["timestamp"] = DateTime.Now
            };
        }
        
        private bool ShouldGenerateCloseMatchCommentary()
        {
            // Only generate close match commentary occasionally to avoid spam
            var lastCloseCommentary = _integratedEvents
                .Where(e => e.Type == IntegratedEventType.Situation)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();
                
            return lastCloseCommentary == null || 
                   (DateTime.Now - lastCloseCommentary.Timestamp).TotalMinutes >= 5;
        }
        
        private bool HasPhaseChanged(Phase currentPhase)
        {
            return _matchState.PreviousPhase != currentPhase;
        }
        
        private List<string> GetRecentCommentary(int count)
        {
            return _integratedEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .Select(e => e.Commentary)
                .ToList();
        }
        
        private string GetCurrentCommentary()
        {
            return _integratedEvents
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault()?.Commentary ?? "";
        }
        
        private string GenerateSituationSummary()
        {
            var scoreStats = _scoreTimeline.GetScorelineStatistics();
            var margin = scoreStats.CurrentMargin;
            var leadingTeam = scoreStats.LeadingTeam;
            
            if (margin == 0)
            {
                return "Scores are tied in a thrilling contest";
            }
            else if (margin <= 6)
            {
                return $"A goal the difference in this tight contest";
            }
            else if (margin <= 18)
            {
                return $"Close match with {leadingTeam} holding a {margin}-point lead";
            }
            else
            {
                return $"{leadingTeam} leading by {margin} points";
            }
        }
        
        private CommentaryMetrics CalculateCommentaryMetrics()
        {
            var totalEvents = _integratedEvents.Count;
            var eventsByType = _integratedEvents.GroupBy(e => e.Type)
                .ToDictionary(g => g.Key, g => g.Count());
                
            return new CommentaryMetrics
            {
                TotalCommentaryEvents = totalEvents,
                EventsByType = eventsByType,
                AverageEventsPerQuarter = _lastQuarter > 0 ? (float)totalEvents / _lastQuarter : 0f,
                MostRecentEventTime = _integratedEvents.LastOrDefault()?.Timestamp,
                CommentaryFrequency = CalculateCommentaryFrequency()
            };
        }
        
        private SynchronizationMetrics CalculateSynchronizationMetrics()
        {
            var syncDelay = Math.Abs((_lastCommentarySync - _lastScoreboardSync).TotalMilliseconds);
            
            return new SynchronizationMetrics
            {
                AverageSyncDelay = (float)syncDelay,
                LastSyncTime = _lastScoreboardSync,
                SyncAccuracy = CalculateSyncAccuracy(),
                PendingCommentaryCount = _pendingCommentary.Count,
                PendingScoreboardCount = _pendingScoreboardUpdates.Count
            };
        }
        
        private EngagementMetrics CalculateEngagementMetrics()
        {
            var highPriorityEvents = _integratedEvents.Count(e => 
                e.Priority == CommentaryPriority.High || e.Priority == CommentaryPriority.Critical);
            
            return new EngagementMetrics
            {
                HighPriorityEventCount = highPriorityEvents,
                EngagementScore = CalculateEngagementScore(),
                CommentaryVariety = CalculateCommentaryVariety(),
                InteractivityLevel = CalculateInteractivityLevel()
            };
        }
        
        private float CalculateCommentaryFrequency()
        {
            if (_lastGameTime == 0) return 0f;
            return _integratedEvents.Count / (_lastGameTime / 60f); // Events per minute
        }
        
        private float CalculateSyncAccuracy()
        {
            // Calculate how well commentary is synchronized with events
            // This would be more sophisticated in a real implementation
            return 0.95f; // Placeholder
        }
        
        private float CalculateEngagementScore()
        {
            // Calculate engagement based on event diversity and timing
            var varietyScore = CalculateCommentaryVariety();
            var timingScore = CalculateTimingQuality();
            return (varietyScore + timingScore) / 2f;
        }
        
        private float CalculateCommentaryVariety()
        {
            var uniqueTypes = _integratedEvents.Select(e => e.Type).Distinct().Count();
            var totalTypes = Enum.GetValues(typeof(IntegratedEventType)).Length;
            return (float)uniqueTypes / totalTypes;
        }
        
        private float CalculateInteractivityLevel()
        {
            // Calculate how interactive the commentary feels
            var recentEvents = _integratedEvents
                .Where(e => (DateTime.Now - e.Timestamp).TotalMinutes <= 10)
                .Count();
            return Math.Min(1f, recentEvents / 10f);
        }
        
        private float CalculateTimingQuality()
        {
            // Measure how well-timed the commentary is
            // This would analyze response times to events
            return 0.88f; // Placeholder
        }
    }
}