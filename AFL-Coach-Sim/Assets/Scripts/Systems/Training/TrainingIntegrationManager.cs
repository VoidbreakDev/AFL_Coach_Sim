using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Injuries;
using AFLManager.Models;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Integration manager that connects weekly training scheduling with daily execution
    /// Provides a unified interface for managing the complete training system
    /// </summary>
    public class TrainingIntegrationManager : MonoBehaviour
    {
        [Header("System Dependencies")]
        [SerializeField] private WeeklyTrainingScheduleManager scheduleManager;
        [SerializeField] private DailyTrainingSessionExecutor sessionExecutor;
        [SerializeField] private TeamTrainingManager teamTrainingManager;
        
        [Header("Integration Configuration")]
        [SerializeField] private bool enableAutomaticExecution = true;
        [SerializeField] private bool enableRealTimeProgressTracking = true;
        [SerializeField] private float sessionExecutionInterval = 1f; // Hours between automatic executions
        
        [Header("Analytics Configuration")]
        [SerializeField] private bool trackComprehensiveAnalytics = true;
        [SerializeField] private int analyticsHistoryDays = 30;
        [SerializeField] private bool enablePerformancePrediction = true;
        
        // Integration state
        private Dictionary<int, TrainingSessionExecution> runningExecutions = new Dictionary<int, TrainingSessionExecution>();
        private List<SessionAnalyticsSummary> sessionHistory = new List<SessionAnalyticsSummary>();
        private DateTime lastAutomaticExecutionCheck = DateTime.MinValue;
        
        // Events for external systems
        public event System.Action<WeeklyTrainingSchedule> OnWeeklyScheduleReady;
        public event System.Action<TrainingSessionExecution> OnSessionExecutionStarted;
        public event System.Action<TrainingSessionExecution> OnSessionExecutionCompleted;
        public event System.Action<TrainingIntegrationEvent> OnTrainingSystemEvent;
        public event System.Action<WeeklyTrainingAnalytics> OnWeeklyAnalyticsUpdated;
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (enableAutomaticExecution)
            {
                CheckForAutomaticSessionExecution();
            }
            
            if (enableRealTimeProgressTracking)
            {
                UpdateRealTimeProgress();
            }
        }
        
        /// <summary>
        /// Initialize the integration manager and connect all systems
        /// </summary>
        public void Initialize()
        {
            // Initialize sub-systems
            if (scheduleManager != null)
            {
                scheduleManager.Initialize();
                
                // Subscribe to schedule events
                scheduleManager.OnWeeklyScheduleGenerated += OnScheduleGenerated;
                scheduleManager.OnDailySessionCompleted += OnScheduleSessionCompleted;
                scheduleManager.OnPlayerLoadExceeded += OnPlayerLoadExceeded;
            }
            
            if (sessionExecutor != null)
            {
                // Initialize executor with available dependencies
                var injuryManager = FindObjectOfType<InjuryManager>();
                var fatigueModel = FindObjectOfType<FatigueModel>();
                sessionExecutor.Initialize(injuryManager, fatigueModel);
                
                // Subscribe to execution events
                sessionExecutor.OnSessionStarted += OnExecutionStarted;
                sessionExecutor.OnSessionCompleted += OnExecutionCompleted;
                sessionExecutor.OnSessionCancelled += OnExecutionCancelled;
                sessionExecutor.OnPlayerSessionResult += OnPlayerSessionResult;
                sessionExecutor.OnTrainingInjuryOccurred += OnTrainingInjuryOccurred;
            }
            
            Debug.Log("[TrainingIntegration] Training integration manager initialized");
        }
        
        /// <summary>
        /// Generate and execute a complete training week
        /// </summary>
        public WeeklyTrainingExecution ExecuteTrainingWeek(DateTime? weekStart = null, List<Player> participants = null)
        {
            Debug.Log("[TrainingIntegration] Starting complete training week execution");
            
            var execution = new WeeklyTrainingExecution
            {
                WeekStartDate = weekStart ?? DateTime.Today,
                StartTime = DateTime.Now,
                Status = WeeklyExecutionStatus.Running
            };
            
            try
            {
                // Step 1: Generate weekly schedule
                var schedule = scheduleManager?.GenerateWeeklySchedule(weekStart);
                if (schedule == null)
                {
                    execution.Status = WeeklyExecutionStatus.Failed;
                    execution.ErrorMessage = "Failed to generate weekly schedule";
                    return execution;
                }
                
                execution.Schedule = schedule;
                execution.PlannedSessions = schedule.DailySessions.Count;
                
                // Step 2: Get available participants
                var availableParticipants = participants ?? GetDefaultParticipants();
                execution.TotalParticipants = availableParticipants.Count;
                
                // Step 3: Execute each daily session
                foreach (var dailySession in schedule.DailySessions.OrderBy(s => s.SessionDate))
                {
                    var sessionExecution = ExecuteDailySession(dailySession, availableParticipants);
                    if (sessionExecution != null)
                    {
                        execution.SessionExecutions.Add(sessionExecution);
                        
                        if (sessionExecution.Status == SessionExecutionStatus.Completed)
                        {
                            execution.CompletedSessions++;
                        }
                        else if (sessionExecution.Status == SessionExecutionStatus.Failed)
                        {
                            execution.FailedSessions++;
                        }
                    }
                    
                    // Update participants based on injuries and fatigue
                    availableParticipants = UpdateParticipantAvailability(availableParticipants, sessionExecution);
                }
                
                // Step 4: Complete week execution
                CompleteWeekExecution(execution);
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrainingIntegration] Error executing training week: {ex.Message}");
                execution.Status = WeeklyExecutionStatus.Failed;
                execution.ErrorMessage = ex.Message;
            }
            
            return execution;
        }
        
        /// <summary>
        /// Execute a single daily training session with full integration
        /// </summary>
        public TrainingSessionExecution ExecuteDailySession(DailyTrainingSession session, List<Player> participants)
        {
            if (sessionExecutor == null)
            {
                Debug.LogError("[TrainingIntegration] Session executor not available");
                return null;
            }
            
            // Pre-execution checks and filtering
            var eligibleParticipants = FilterParticipantsForSession(participants, session);
            
            if (eligibleParticipants.Count == 0)
            {
                Debug.LogWarning($"[TrainingIntegration] No eligible participants for session {session.SessionId}");
                return null;
            }
            
            // Execute the session
            var execution = sessionExecutor.ExecuteSession(session, eligibleParticipants);
            
            if (execution != null)
            {
                runningExecutions[execution.SessionId] = execution;
                
                // Track execution for analytics
                if (execution.Status == SessionExecutionStatus.Completed)
                {
                    RecordSessionAnalytics(execution);
                }
            }
            
            return execution;
        }
        
        /// <summary>
        /// Get comprehensive training analytics for a period
        /// </summary>
        public TrainingSystemAnalytics GetTrainingAnalytics(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.Today.AddDays(-analyticsHistoryDays);
            var end = endDate ?? DateTime.Today;
            
            var relevantSessions = sessionHistory.Where(s => s.SessionDate >= start && s.SessionDate <= end).ToList();
            
            var analytics = new TrainingSystemAnalytics
            {
                AnalysisPeriod = new DateRange { Start = start, End = end },
                TotalSessions = relevantSessions.Count,
                TotalParticipants = relevantSessions.Sum(s => s.TotalParticipants),
                TotalTrainingHours = relevantSessions.Sum(s => s.SessionDuration.TotalHours),
                AverageEffectiveness = relevantSessions.Any() ? relevantSessions.Average(s => s.AverageEffectiveness) : 0f,
                TotalInjuries = relevantSessions.Sum(s => s.TotalInjuries),
                TotalDevelopment = relevantSessions.Sum(s => s.TotalDevelopment)
            };
            
            // Calculate breakdown by session type
            analytics.SessionTypeBreakdown = relevantSessions
                .GroupBy(s => s.SessionType)
                .ToDictionary(g => g.Key, g => g.Count());
                
            // Calculate grade distribution
            analytics.GradeDistribution = relevantSessions
                .GroupBy(s => s.OverallGrade)
                .ToDictionary(g => g.Key, g => g.Count());
                
            // Calculate injury rate
            analytics.InjuryRate = analytics.TotalParticipants > 0 ? 
                (float)analytics.TotalInjuries / analytics.TotalParticipants : 0f;
                
            // Calculate trends
            CalculateAnalyticsTrends(analytics, relevantSessions);
            
            return analytics;
        }
        
        /// <summary>
        /// Get current week training status
        /// </summary>
        public WeeklyTrainingStatus GetCurrentWeekStatus()
        {
            var currentSchedule = scheduleManager?.GetCurrentSchedule();
            
            var status = new WeeklyTrainingStatus
            {
                WeekStartDate = currentSchedule?.WeekStartDate ?? DateTime.Today,
                TotalSessionsPlanned = currentSchedule?.DailySessions.Count ?? 0,
                SessionsCompleted = currentSchedule?.DailySessions.Count(s => s.Status == TrainingSessionStatus.Completed) ?? 0,
                SessionsInProgress = runningExecutions.Count(e => e.Value.IsActive),
                CurrentWeekLoad = CalculateCurrentWeekLoad(),
                HealthStatus = CalculateWeekHealthStatus(currentSchedule)
            };
            
            status.CompletionPercentage = status.TotalSessionsPlanned > 0 ? 
                (float)status.SessionsCompleted / status.TotalSessionsPlanned * 100f : 0f;
                
            return status;
        }
        
        /// <summary>
        /// Advance to next training week
        /// </summary>
        public void AdvanceToNextWeek()
        {
            // Complete any remaining sessions from current week
            FinalizePendingSessions();
            
            // Generate analytics for completed week
            var weeklyAnalytics = GenerateWeeklyAnalytics();
            OnWeeklyAnalyticsUpdated?.Invoke(weeklyAnalytics);
            
            // Advance schedule manager to next week
            scheduleManager?.AdvanceToNextWeek();
            
            // Reset tracking state
            runningExecutions.Clear();
            
            Debug.Log("[TrainingIntegration] Advanced to next training week");
        }
        
        /// <summary>
        /// Get training recommendations for upcoming sessions
        /// </summary>
        public List<TrainingRecommendation> GetTrainingRecommendations(List<Player> players)
        {
            var recommendations = new List<TrainingRecommendation>();
            
            if (scheduleManager == null) return recommendations;
            
            var currentSchedule = scheduleManager.GetCurrentSchedule();
            var upcomingSessions = currentSchedule?.DailySessions
                .Where(s => s.Status == TrainingSessionStatus.Scheduled && s.SessionDate >= DateTime.Today)
                .OrderBy(s => s.SessionDate)
                .Take(3)
                .ToList() ?? new List<DailyTrainingSession>();
                
            foreach (var session in upcomingSessions)
            {
                var recommendation = GenerateSessionRecommendation(session, players);
                if (recommendation != null)
                {
                    recommendations.Add(recommendation);
                }
            }
            
            return recommendations;
        }
        
        #region Private Methods
        
        private void CheckForAutomaticSessionExecution()
        {
            if ((DateTime.Now - lastAutomaticExecutionCheck).TotalHours < sessionExecutionInterval)
                return;
                
            lastAutomaticExecutionCheck = DateTime.Now;
            
            var currentSchedule = scheduleManager?.GetCurrentSchedule();
            if (currentSchedule == null) return;
            
            // Find sessions due for execution
            var dueForExecution = currentSchedule.DailySessions
                .Where(s => s.Status == TrainingSessionStatus.Scheduled &&
                           s.GetScheduledDateTime() <= DateTime.Now &&
                           !runningExecutions.ContainsKey(s.SessionId))
                .ToList();
                
            foreach (var session in dueForExecution)
            {
                var participants = GetDefaultParticipants();
                ExecuteDailySession(session, participants);
            }
        }
        
        private void UpdateRealTimeProgress()
        {
            // Update progress for running executions
            foreach (var execution in runningExecutions.Values.Where(e => e.IsActive).ToList())
            {
                // Real-time updates would be handled here
                // For now, just check if execution is still valid
                if (!sessionExecutor.GetActiveSessions().Any(s => s.SessionId == execution.SessionId))
                {
                    runningExecutions.Remove(execution.SessionId);
                }
            }
        }
        
        private List<Player> FilterParticipantsForSession(List<Player> participants, DailyTrainingSession session)
        {
            var filtered = new List<Player>();
            
            foreach (var player in participants)
            {
                // Check weekly load limits
                if (scheduleManager != null && !scheduleManager.CanPlayerTrain(player.ID, session.GetSessionLoad()))
                {
                    continue;
                }
                
                // Check if player is currently in another session
                var isInSession = runningExecutions.Values.Any(e => 
                    e.IsActive && e.EligibleParticipants.Any(p => p.ID == player.ID));
                if (isInSession)
                {
                    continue;
                }
                
                filtered.Add(player);
            }
            
            return filtered;
        }
        
        private List<Player> UpdateParticipantAvailability(List<Player> participants, TrainingSessionExecution execution)
        {
            if (execution?.ParticipantResults == null)
                return participants;
                
            var available = new List<Player>();
            
            foreach (var player in participants)
            {
                if (execution.ParticipantResults.ContainsKey(player.ID))
                {
                    var result = execution.ParticipantResults[player.ID];
                    
                    // Skip injured players
                    if (result.TotalInjuries > 0)
                        continue;
                        
                    // Check fatigue levels
                    var currentLoad = scheduleManager?.GetPlayerWeeklyLoad(player.ID).CurrentLoad ?? 0f;
                    if (currentLoad > 85f) // High fatigue threshold
                        continue;
                }
                
                available.Add(player);
            }
            
            return available;
        }
        
        private void CompleteWeekExecution(WeeklyTrainingExecution execution)
        {
            execution.Status = WeeklyExecutionStatus.Completed;
            execution.EndTime = DateTime.Now;
            execution.ActualDuration = execution.EndTime - execution.StartTime;
            
            // Calculate final metrics
            execution.TotalInjuries = execution.SessionExecutions.Sum(se => se.GetTotalInjuries());
            execution.AverageEffectiveness = execution.SessionExecutions.Any() ?
                execution.SessionExecutions.Average(se => se.GetAverageEffectiveness()) : 0f;
            execution.TotalDevelopment = execution.SessionExecutions
                .SelectMany(se => se.ParticipantResults.Values)
                .Sum(pr => pr.TotalStatChanges?.GetTotalChange() ?? 0f);
                
            Debug.Log($"[TrainingIntegration] Week execution completed: {execution.CompletedSessions}/{execution.PlannedSessions} sessions");
        }
        
        private void RecordSessionAnalytics(TrainingSessionExecution execution)
        {
            if (!trackComprehensiveAnalytics) return;
            
            var summary = new SessionAnalyticsSummary
            {
                SessionId = execution.SessionId,
                SessionDate = execution.Session.SessionDate,
                SessionName = execution.Session.SessionName,
                SessionType = execution.Session.SessionType,
                TotalParticipants = execution.EligibleParticipants.Count,
                ComponentsPlanned = execution.Session.TrainingComponents.Count,
                ComponentsCompleted = execution.ComponentResults.Count,
                AverageEffectiveness = execution.GetAverageEffectiveness(),
                TotalDevelopment = execution.ParticipantResults.Values.Sum(pr => pr.TotalStatChanges?.GetTotalChange() ?? 0f),
                TotalInjuries = execution.GetTotalInjuries(),
                AvgFatigueIncrease = execution.ParticipantResults.Values.Any() ? 
                    execution.ParticipantResults.Values.Average(pr => pr.TotalFatigueIncrease) : 0f,
                SessionDuration = execution.ActualDuration ?? TimeSpan.Zero
            };
            
            summary.OverallGrade = CalculateSessionGrade(summary);
            sessionHistory.Add(summary);
            
            // Keep history within limits
            if (sessionHistory.Count > 1000)
            {
                sessionHistory = sessionHistory.TakeLast(800).ToList();
            }
        }
        
        private SessionParticipationGrade CalculateSessionGrade(SessionAnalyticsSummary summary)
        {
            if (summary.TotalInjuries > 0)
                return SessionParticipationGrade.Injured;
                
            var completionRate = summary.ComponentsPlanned > 0 ? 
                (float)summary.ComponentsCompleted / summary.ComponentsPlanned : 0f;
                
            if (completionRate >= 0.95f && summary.AverageEffectiveness >= 1.1f)
                return SessionParticipationGrade.Excellent;
            else if (completionRate >= 0.85f && summary.AverageEffectiveness >= 0.9f)
                return SessionParticipationGrade.Good;
            else if (completionRate >= 0.7f)
                return SessionParticipationGrade.Fair;
            else
                return SessionParticipationGrade.Poor;
        }
        
        private void CalculateAnalyticsTrends(TrainingSystemAnalytics analytics, List<SessionAnalyticsSummary> sessions)
        {
            if (sessions.Count < 2) return;
            
            var orderedSessions = sessions.OrderBy(s => s.SessionDate).ToList();
            var midPoint = orderedSessions.Count / 2;
            
            var earlyPeriod = orderedSessions.Take(midPoint);
            var latePeriod = orderedSessions.Skip(midPoint);
            
            var earlyEffectiveness = earlyPeriod.Average(s => s.AverageEffectiveness);
            var lateEffectiveness = latePeriod.Average(s => s.AverageEffectiveness);
            
            analytics.EffectivenessTrend = lateEffectiveness - earlyEffectiveness;
            
            var earlyInjuryRate = earlyPeriod.Sum(s => s.TotalInjuries) / (float)Math.Max(1, earlyPeriod.Sum(s => s.TotalParticipants));
            var lateInjuryRate = latePeriod.Sum(s => s.TotalInjuries) / (float)Math.Max(1, latePeriod.Sum(s => s.TotalParticipants));
            
            analytics.InjuryRateTrend = lateInjuryRate - earlyInjuryRate;
        }
        
        private List<Player> GetDefaultParticipants()
        {
            // This would typically get players from team management system
            // For now, return empty list - would be populated by external systems
            return new List<Player>();
        }
        
        private float CalculateCurrentWeekLoad()
        {
            if (scheduleManager == null) return 0f;
            
            var schedule = scheduleManager.GetCurrentSchedule();
            return schedule?.GetTotalWeeklyLoad() ?? 0f;
        }
        
        private WeekHealthStatus CalculateWeekHealthStatus(WeeklyTrainingSchedule schedule)
        {
            if (schedule == null) return WeekHealthStatus.Unknown;
            
            var completedSessions = schedule.DailySessions.Where(s => s.Status == TrainingSessionStatus.Completed);
            var recentInjuries = sessionHistory
                .Where(s => s.SessionDate >= DateTime.Today.AddDays(-7))
                .Sum(s => s.TotalInjuries);
                
            if (recentInjuries >= 3)
                return WeekHealthStatus.Critical;
            else if (recentInjuries >= 1)
                return WeekHealthStatus.Warning;
            else
                return WeekHealthStatus.Good;
        }
        
        private void FinalizePendingSessions()
        {
            foreach (var execution in runningExecutions.Values.Where(e => e.IsActive).ToList())
            {
                sessionExecutor.CancelSession(execution.SessionId, "Week finalization");
            }
        }
        
        private WeeklyTrainingAnalytics GenerateWeeklyAnalytics()
        {
            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var weekSessions = sessionHistory
                .Where(s => s.SessionDate >= weekStart && s.SessionDate < weekStart.AddDays(7))
                .ToList();
                
            return new WeeklyTrainingAnalytics
            {
                WeekStartDate = weekStart,
                TotalSessionsScheduled = weekSessions.Count,
                TotalSessionsCompleted = weekSessions.Count(s => s.OverallGrade != SessionParticipationGrade.DidNotParticipate),
                AveragePlayerLoad = 0f, // Would calculate from player data
                AverageEffectiveness = weekSessions.Any() ? weekSessions.Average(s => s.AverageEffectiveness) : 0f,
                PlayersOverLoaded = 0 // Would calculate from player load data
            };
        }
        
        private TrainingRecommendation GenerateSessionRecommendation(DailyTrainingSession session, List<Player> players)
        {
            var eligibleCount = FilterParticipantsForSession(players, session).Count;
            var totalLoad = session.GetSessionLoad();
            
            var recommendation = new TrainingRecommendation
            {
                SessionId = session.SessionId,
                SessionName = session.SessionName,
                ScheduledDate = session.SessionDate,
                RecommendationType = RecommendationType.SessionOptimization,
                Priority = CalculateRecommendationPriority(session, eligibleCount),
                Description = GenerateRecommendationDescription(session, eligibleCount, totalLoad),
                SuggestedActions = GenerateSuggestedActions(session, eligibleCount, totalLoad)
            };
            
            return recommendation;
        }
        
        private RecommendationPriority CalculateRecommendationPriority(DailyTrainingSession session, int eligibleCount)
        {
            if (eligibleCount < 5)
                return RecommendationPriority.High; // Not enough participants
            else if (session.SessionType == DailySessionType.Main && eligibleCount < 12)
                return RecommendationPriority.Medium;
            else
                return RecommendationPriority.Low;
        }
        
        private string GenerateRecommendationDescription(DailyTrainingSession session, int eligibleCount, float totalLoad)
        {
            if (eligibleCount < 5)
                return $"Insufficient participants ({eligibleCount}) for effective {session.SessionName}";
            else if (totalLoad > 50f)
                return $"High training load ({totalLoad:F1}) - consider reducing intensity";
            else
                return $"Session ready for execution with {eligibleCount} participants";
        }
        
        private List<string> GenerateSuggestedActions(DailyTrainingSession session, int eligibleCount, float totalLoad)
        {
            var actions = new List<string>();
            
            if (eligibleCount < 5)
            {
                actions.Add("Consider postponing session");
                actions.Add("Recruit additional participants");
                actions.Add("Modify session for smaller group");
            }
            else if (totalLoad > 50f)
            {
                actions.Add("Reduce training intensity");
                actions.Add("Remove optional components");
                actions.Add("Increase recovery time");
            }
            else
            {
                actions.Add("Execute as planned");
                actions.Add("Monitor participant fatigue");
            }
            
            return actions;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnScheduleGenerated(WeeklyTrainingSchedule schedule)
        {
            OnWeeklyScheduleReady?.Invoke(schedule);
            
            var eventData = new TrainingIntegrationEvent
            {
                Type = IntegrationEventType.WeeklyScheduleGenerated,
                Timestamp = DateTime.Now,
                Message = $"Weekly schedule generated with {schedule.DailySessions.Count} sessions",
                Data = new Dictionary<string, object>
                {
                    ["sessionCount"] = schedule.DailySessions.Count,
                    ["totalLoad"] = schedule.GetTotalWeeklyLoad(),
                    ["weekStart"] = schedule.WeekStartDate
                }
            };
            
            OnTrainingSystemEvent?.Invoke(eventData);
        }
        
        private void OnScheduleSessionCompleted(DailyTrainingSession session, List<PlayerTrainingResult> results)
        {
            Debug.Log($"[TrainingIntegration] Schedule manager reported session completion: {session.SessionId}");
        }
        
        private void OnPlayerLoadExceeded(int playerId, float currentLoad)
        {
            var eventData = new TrainingIntegrationEvent
            {
                Type = IntegrationEventType.PlayerLoadExceeded,
                Timestamp = DateTime.Now,
                Message = $"Player {playerId} exceeded training load ({currentLoad:F1})",
                Data = new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["currentLoad"] = currentLoad
                }
            };
            
            OnTrainingSystemEvent?.Invoke(eventData);
        }
        
        private void OnExecutionStarted(TrainingSessionExecution execution)
        {
            OnSessionExecutionStarted?.Invoke(execution);
        }
        
        private void OnExecutionCompleted(TrainingSessionExecution execution)
        {
            runningExecutions.Remove(execution.SessionId);
            OnSessionExecutionCompleted?.Invoke(execution);
        }
        
        private void OnExecutionCancelled(TrainingSessionExecution execution)
        {
            runningExecutions.Remove(execution.SessionId);
        }
        
        private void OnPlayerSessionResult(int playerId, PlayerSessionResult result)
        {
            // Player-specific result handling could go here
        }
        
        private void OnTrainingInjuryOccurred(int playerId, AFLCoachSim.Core.Injuries.Domain.Injury injury)
        {
            var eventData = new TrainingIntegrationEvent
            {
                Type = IntegrationEventType.TrainingInjury,
                Timestamp = DateTime.Now,
                Message = $"Training injury occurred: Player {playerId}",
                Data = new Dictionary<string, object>
                {
                    ["playerId"] = playerId,
                    ["injuryType"] = injury.Type.ToString(),
                    ["severity"] = injury.Severity.ToString()
                }
            };
            
            OnTrainingSystemEvent?.Invoke(eventData);
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Clean up subscriptions
            if (scheduleManager != null)
            {
                scheduleManager.OnWeeklyScheduleGenerated -= OnScheduleGenerated;
                scheduleManager.OnDailySessionCompleted -= OnScheduleSessionCompleted;
                scheduleManager.OnPlayerLoadExceeded -= OnPlayerLoadExceeded;
            }
            
            if (sessionExecutor != null)
            {
                sessionExecutor.OnSessionStarted -= OnExecutionStarted;
                sessionExecutor.OnSessionCompleted -= OnExecutionCompleted;
                sessionExecutor.OnSessionCancelled -= OnExecutionCancelled;
                sessionExecutor.OnPlayerSessionResult -= OnPlayerSessionResult;
                sessionExecutor.OnTrainingInjuryOccurred -= OnTrainingInjuryOccurred;
            }
        }
    }
    
    /// <summary>
    /// Represents execution of an entire training week
    /// </summary>
    [System.Serializable]
    public class WeeklyTrainingExecution
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan ActualDuration { get; set; }
        public WeeklyExecutionStatus Status { get; set; }
        public WeeklyTrainingSchedule Schedule { get; set; }
        public List<TrainingSessionExecution> SessionExecutions { get; set; } = new List<TrainingSessionExecution>();
        public int PlannedSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int FailedSessions { get; set; }
        public int TotalParticipants { get; set; }
        public int TotalInjuries { get; set; }
        public float AverageEffectiveness { get; set; }
        public float TotalDevelopment { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// Current training status for the week
    /// </summary>
    public class WeeklyTrainingStatus
    {
        public DateTime WeekStartDate { get; set; }
        public int TotalSessionsPlanned { get; set; }
        public int SessionsCompleted { get; set; }
        public int SessionsInProgress { get; set; }
        public float CompletionPercentage { get; set; }
        public float CurrentWeekLoad { get; set; }
        public WeekHealthStatus HealthStatus { get; set; }
    }
    
    /// <summary>
    /// Comprehensive training system analytics
    /// </summary>
    public class TrainingSystemAnalytics
    {
        public DateRange AnalysisPeriod { get; set; }
        public int TotalSessions { get; set; }
        public int TotalParticipants { get; set; }
        public double TotalTrainingHours { get; set; }
        public float AverageEffectiveness { get; set; }
        public int TotalInjuries { get; set; }
        public float InjuryRate { get; set; }
        public float TotalDevelopment { get; set; }
        public Dictionary<DailySessionType, int> SessionTypeBreakdown { get; set; }
        public Dictionary<SessionParticipationGrade, int> GradeDistribution { get; set; }
        public float EffectivenessTrend { get; set; }
        public float InjuryRateTrend { get; set; }
    }
    
    /// <summary>
    /// Training recommendation for optimization
    /// </summary>
    public class TrainingRecommendation
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public RecommendationType RecommendationType { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Description { get; set; }
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Training integration event for system communication
    /// </summary>
    public class TrainingIntegrationEvent
    {
        public IntegrationEventType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Date range helper
    /// </summary>
    public class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        
        public TimeSpan Duration => End - Start;
        public int Days => (int)Duration.TotalDays;
    }
    
    // Enums
    public enum WeeklyExecutionStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled
    }
    
    public enum WeekHealthStatus
    {
        Good,
        Warning,
        Critical,
        Unknown
    }
    
    public enum RecommendationType
    {
        SessionOptimization,
        LoadManagement,
        InjuryPrevention,
        PerformanceImprovement,
        ResourceAllocation
    }
    
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum IntegrationEventType
    {
        WeeklyScheduleGenerated,
        SessionExecutionStarted,
        SessionExecutionCompleted,
        PlayerLoadExceeded,
        TrainingInjury,
        SystemError,
        OptimizationRecommendation
    }
}