using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Services;
using AFLManager.Models;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Manages weekly training schedules, integrating with season calendar and player load management
    /// </summary>
    public class WeeklyTrainingScheduleManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TeamTrainingManager trainingManager;
        [SerializeField] private SeasonProgressionManager seasonManager;
        
        [Header("Schedule Configuration")]
        [SerializeField] private WeeklyScheduleTemplate defaultScheduleTemplate;
        [SerializeField] private List<WeeklyScheduleTemplate> specialScheduleTemplates;
        
        [Header("Load Management")]
        [SerializeField] private float maxWeeklyTrainingLoad = 100f;
        [SerializeField] private float preMatchTaperMultiplier = 0.7f;
        [SerializeField] private float postMatchRecoveryMultiplier = 0.5f;
        
        // Current schedule state
        private WeeklyTrainingSchedule currentSchedule;
        private Dictionary<int, PlayerWeeklyLoad> playerWeeklyLoads;
        private DateTime currentWeekStart;
        
        // Events
        public event System.Action<WeeklyTrainingSchedule> OnWeeklyScheduleGenerated;
        public event System.Action<DailyTrainingSession, List<PlayerTrainingResult>> OnDailySessionCompleted;
        public event System.Action<int, float> OnPlayerLoadExceeded;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initialize the weekly training schedule manager
        /// </summary>
        public void Initialize()
        {
            playerWeeklyLoads = new Dictionary<int, PlayerWeeklyLoad>();
            currentWeekStart = GetCurrentWeekStart();
            
            // Create default template if not set
            if (defaultScheduleTemplate == null)
            {
                defaultScheduleTemplate = CreateDefaultScheduleTemplate();
            }
            
            // Generate initial schedule
            GenerateWeeklySchedule();
            
            Debug.Log("[WeeklyTrainingSchedule] Initialized training schedule manager");
        }
        
        /// <summary>
        /// Generate the training schedule for the current week
        /// </summary>
        public WeeklyTrainingSchedule GenerateWeeklySchedule(DateTime? weekStart = null)
        {
            var targetWeekStart = weekStart ?? currentWeekStart;
            var template = SelectAppropriateTemplate(targetWeekStart);
            
            var schedule = new WeeklyTrainingSchedule
            {
                WeekStartDate = targetWeekStart,
                WeekEndDate = targetWeekStart.AddDays(6),
                Template = template,
                DailySessions = new List<DailyTrainingSession>()
            };
            
            // Get match context for this week
            var weekMatches = seasonManager?.GetMatchesInDateRange(targetWeekStart, targetWeekStart.AddDays(6)) ?? new List<ScheduledMatch>();
            schedule.WeekMatches = weekMatches;
            
            // Generate daily sessions based on template and match schedule
            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var sessionDate = targetWeekStart.AddDays(dayOffset);
                var dayOfWeek = sessionDate.DayOfWeek;
                
                var dailySession = GenerateDailySession(sessionDate, dayOfWeek, template, weekMatches);
                if (dailySession != null)
                {
                    schedule.DailySessions.Add(dailySession);
                }
            }
            
            // Apply load balancing adjustments
            ApplyLoadBalancing(schedule);
            
            currentSchedule = schedule;
            OnWeeklyScheduleGenerated?.Invoke(schedule);
            
            Debug.Log($"[WeeklyTrainingSchedule] Generated schedule for week {targetWeekStart:yyyy-MM-dd} with {schedule.DailySessions.Count} training sessions");
            
            return schedule;
        }
        
        /// <summary>
        /// Execute a specific daily training session
        /// </summary>
        public List<PlayerTrainingResult> ExecuteDailySession(int sessionId, List<Player> participants)
        {
            var session = currentSchedule?.DailySessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session == null)
            {
                Debug.LogWarning($"[WeeklyTrainingSchedule] Session {sessionId} not found");
                return new List<PlayerTrainingResult>();
            }
            
            if (session.Status != TrainingSessionStatus.Scheduled)
            {
                Debug.LogWarning($"[WeeklyTrainingSchedule] Session {sessionId} is not scheduled (current status: {session.Status})");
                return new List<PlayerTrainingResult>();
            }
            
            // Filter participants based on availability and load limits
            var availableParticipants = FilterParticipantsByAvailability(participants, session);
            
            var results = new List<PlayerTrainingResult>();
            
            // Execute each training component in the session
            foreach (var component in session.TrainingComponents)
            {
                var componentResults = ExecuteTrainingComponent(component, availableParticipants, session.SessionDate);
                results.AddRange(componentResults);
                
                // Update player weekly loads
                UpdatePlayerLoads(componentResults, component.LoadMultiplier);
            }
            
            // Update session status
            session.Status = TrainingSessionStatus.Completed;
            session.ActualParticipants = availableParticipants.Select(p => p.Id).ToList();
            session.CompletionTime = DateTime.Now;
            
            OnDailySessionCompleted?.Invoke(session, results);
            
            Debug.Log($"[WeeklyTrainingSchedule] Completed session {sessionId} with {availableParticipants.Count} participants");
            
            return results;
        }
        
        /// <summary>
        /// Advance to next week and generate new schedule
        /// </summary>
        public void AdvanceToNextWeek()
        {
            currentWeekStart = currentWeekStart.AddDays(7);
            
            // Reset weekly loads
            foreach (var load in playerWeeklyLoads.Values)
            {
                load.ResetWeekly();
            }
            
            // Generate new schedule
            GenerateWeeklySchedule();
            
            Debug.Log($"[WeeklyTrainingSchedule] Advanced to week starting {currentWeekStart:yyyy-MM-dd}");
        }
        
        /// <summary>
        /// Get current weekly schedule
        /// </summary>
        public WeeklyTrainingSchedule GetCurrentSchedule()
        {
            return currentSchedule;
        }
        
        /// <summary>
        /// Get player's current weekly training load
        /// </summary>
        public PlayerWeeklyLoad GetPlayerWeeklyLoad(int playerId)
        {
            if (!playerWeeklyLoads.ContainsKey(playerId))
            {
                playerWeeklyLoads[playerId] = new PlayerWeeklyLoad { PlayerId = playerId };
            }
            
            return playerWeeklyLoads[playerId];
        }
        
        /// <summary>
        /// Check if player can participate in additional training
        /// </summary>
        public bool CanPlayerTrain(int playerId, float additionalLoad)
        {
            var currentLoad = GetPlayerWeeklyLoad(playerId);
            return (currentLoad.CurrentLoad + additionalLoad) <= maxWeeklyTrainingLoad;
        }
        
        /// <summary>
        /// Get upcoming training sessions for a specific day
        /// </summary>
        public List<DailyTrainingSession> GetDayTrainingSessions(DateTime date)
        {
            return currentSchedule?.DailySessions
                .Where(s => s.SessionDate.Date == date.Date)
                .OrderBy(s => s.ScheduledStartTime)
                .ToList() ?? new List<DailyTrainingSession>();
        }
        
        #region Private Methods
        
        private WeeklyScheduleTemplate SelectAppropriateTemplate(DateTime weekStart)
        {
            // Check for special circumstances
            var weekMatches = seasonManager?.GetMatchesInDateRange(weekStart, weekStart.AddDays(6)) ?? new List<ScheduledMatch>();
            
            // Pre-finals intensity
            if (seasonManager?.GetProgressStats().CurrentRound > 20)
            {
                var finalsTemplate = specialScheduleTemplates?.FirstOrDefault(t => t.TemplateType == ScheduleTemplateType.Finals);
                if (finalsTemplate != null) return finalsTemplate;
            }
            
            // Match week vs non-match week
            if (weekMatches.Any())
            {
                var matchWeekTemplate = specialScheduleTemplates?.FirstOrDefault(t => t.TemplateType == ScheduleTemplateType.MatchWeek);
                if (matchWeekTemplate != null) return matchWeekTemplate;
            }
            
            // Bye week
            var currentRound = seasonManager?.GetProgressStats().CurrentRound ?? 1;
            if (seasonManager != null && IsTeamOnByeThisWeek(weekStart))
            {
                var byeWeekTemplate = specialScheduleTemplates?.FirstOrDefault(t => t.TemplateType == ScheduleTemplateType.ByeWeek);
                if (byeWeekTemplate != null) return byeWeekTemplate;
            }
            
            return defaultScheduleTemplate;
        }
        
        private DailyTrainingSession GenerateDailySession(DateTime sessionDate, DayOfWeek dayOfWeek, WeeklyScheduleTemplate template, List<ScheduledMatch> weekMatches)
        {
            var dayTemplate = template.GetDayTemplate(dayOfWeek);
            if (dayTemplate == null || dayTemplate.IsRestDay) return null;
            
            // Check if there's a match this day
            var matchThisDay = weekMatches.FirstOrDefault(m => m.ScheduledDateTime.Date == sessionDate.Date);
            if (matchThisDay != null && dayTemplate.SkipOnMatchDay) return null;
            
            var session = new DailyTrainingSession
            {
                SessionId = GenerateSessionId(),
                SessionDate = sessionDate,
                SessionName = dayTemplate.SessionName,
                ScheduledStartTime = dayTemplate.StartTime,
                EstimatedDuration = dayTemplate.Duration,
                SessionType = dayTemplate.SessionType,
                TrainingComponents = new List<TrainingComponent>(),
                Status = TrainingSessionStatus.Scheduled
            };
            
            // Add training components based on day template
            foreach (var componentTemplate in dayTemplate.Components)
            {
                var component = new TrainingComponent
                {
                    ComponentType = componentTemplate.Type,
                    Program = trainingManager.GetAvailablePrograms()
                        .FirstOrDefault(p => p.FocusType == componentTemplate.Focus),
                    Duration = componentTemplate.Duration,
                    Intensity = ApplyContextualIntensity(componentTemplate.Intensity, sessionDate, weekMatches),
                    LoadMultiplier = componentTemplate.LoadMultiplier
                };
                
                if (component.Program != null)
                {
                    session.TrainingComponents.Add(component);
                }
            }
            
            return session;
        }
        
        private TrainingIntensity ApplyContextualIntensity(TrainingIntensity baseIntensity, DateTime sessionDate, List<ScheduledMatch> weekMatches)
        {
            // Reduce intensity before matches
            var nextMatch = weekMatches.FirstOrDefault(m => m.ScheduledDateTime.Date > sessionDate.Date);
            if (nextMatch != null)
            {
                var daysUntilMatch = (nextMatch.ScheduledDateTime.Date - sessionDate.Date).Days;
                if (daysUntilMatch <= 2) // Taper 2 days before match
                {
                    return ReduceIntensity(baseIntensity);
                }
            }
            
            // Reduce intensity after matches
            var recentMatch = weekMatches.FirstOrDefault(m => m.ScheduledDateTime.Date < sessionDate.Date && 
                                                         (sessionDate.Date - m.ScheduledDateTime.Date).Days <= 2);
            if (recentMatch != null)
            {
                return ReduceIntensity(baseIntensity);
            }
            
            return baseIntensity;
        }
        
        private TrainingIntensity ReduceIntensity(TrainingIntensity intensity)
        {
            return intensity switch
            {
                TrainingIntensity.VeryHigh => TrainingIntensity.High,
                TrainingIntensity.High => TrainingIntensity.Moderate,
                TrainingIntensity.Moderate => TrainingIntensity.Light,
                _ => intensity
            };
        }
        
        private void ApplyLoadBalancing(WeeklyTrainingSchedule schedule)
        {
            // Distribute load more evenly throughout the week
            var totalWeeklyLoad = schedule.DailySessions.Sum(s => s.TrainingComponents.Sum(c => c.LoadMultiplier));
            
            if (totalWeeklyLoad > maxWeeklyTrainingLoad * 0.9f) // If approaching max load
            {
                // Reduce load on least important sessions
                var sessionsToReduce = schedule.DailySessions
                    .Where(s => s.SessionType == DailySessionType.Supplementary)
                    .OrderBy(s => s.TrainingComponents.Count)
                    .Take(2);
                    
                foreach (var session in sessionsToReduce)
                {
                    foreach (var component in session.TrainingComponents)
                    {
                        component.LoadMultiplier *= 0.8f;
                    }
                }
            }
        }
        
        private List<Player> FilterParticipantsByAvailability(List<Player> participants, DailyTrainingSession session)
        {
            var available = new List<Player>();
            
            foreach (var player in participants)
            {
                // Check if player can handle additional training load
                var additionalLoad = session.TrainingComponents.Sum(c => c.LoadMultiplier);
                if (!CanPlayerTrain(player.Id, additionalLoad))
                {
                    OnPlayerLoadExceeded?.Invoke(player.Id, GetPlayerWeeklyLoad(player.Id).CurrentLoad);
                    continue;
                }
                
                // TODO: Integrate injury system check when injury awareness is properly wired up
                // For now, skip injury check to avoid compilation issues
                /*
                if (trainingManager is InjuryAwareTrainingSystem injuryAwareTraining)
                {
                    if (!injuryAwareTraining.InjuryManager.CanPlayerTrain(player.Id))
                    {
                        continue;
                    }
                }
                */
                
                available.Add(player);
            }
            
            return available;
        }
        
        private List<PlayerTrainingResult> ExecuteTrainingComponent(TrainingComponent component, List<Player> participants, DateTime sessionDate)
        {
            if (component.Program == null)
            {
                return new List<PlayerTrainingResult>();
            }
            
            // Use existing training manager to execute the component
            var results = new List<PlayerTrainingResult>();
            
            foreach (var player in participants)
            {
                // Calculate development for this specific component
                var development = player.Development?.CalculateDevelopment(player, component.Program, component.LoadMultiplier) ?? new PlayerStatsDelta();
                
                var result = new PlayerTrainingResult
                {
                    Player = player,
                    StatChanges = development,
                    WasInjured = false, // Basic implementation - would integrate with injury system
                    EffectivenessRating = component.Program.GetEffectivenessMultiplier(player)
                };
                
                // Apply improvements
                development.ApplyTo(player.Stats);
                results.Add(result);
            }
            
            return results;
        }
        
        private void UpdatePlayerLoads(List<PlayerTrainingResult> results, float loadMultiplier)
        {
            foreach (var result in results)
            {
                var load = GetPlayerWeeklyLoad(result.Player.Id);
                load.AddTrainingLoad(loadMultiplier, result.EffectivenessRating);
            }
        }
        
        private DateTime GetCurrentWeekStart()
        {
            var today = DateTime.Today;
            var daysSinceMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysSinceMonday < 0) daysSinceMonday += 7;
            return today.AddDays(-daysSinceMonday);
        }
        
        private bool IsTeamOnByeThisWeek(DateTime weekStart)
        {
            // This would integrate with the season manager to check bye rounds
            return false; // Simplified implementation
        }
        
        private int GenerateSessionId()
        {
            return UnityEngine.Random.Range(10000, 99999);
        }
        
        private WeeklyScheduleTemplate CreateDefaultScheduleTemplate()
        {
            return new WeeklyScheduleTemplate
            {
                TemplateName = "Standard Training Week",
                TemplateType = ScheduleTemplateType.Standard,
                DayTemplates = new List<DailySessionTemplate>
                {
                    // Monday - Skills focus
                    new DailySessionTemplate
                    {
                        DayOfWeek = DayOfWeek.Monday,
                        SessionName = "Skills Training",
                        StartTime = new TimeSpan(9, 0, 0),
                        Duration = TimeSpan.FromHours(2),
                        SessionType = DailySessionType.Main,
                        Components = new List<ComponentTemplate>
                        {
                            new ComponentTemplate { Type = TrainingComponentType.Skills, Focus = TrainingFocus.Kicking, Duration = TimeSpan.FromMinutes(90), Intensity = TrainingIntensity.Moderate, LoadMultiplier = 15f }
                        }
                    },
                    // Tuesday - Fitness
                    new DailySessionTemplate
                    {
                        DayOfWeek = DayOfWeek.Tuesday,
                        SessionName = "Fitness Training",
                        StartTime = new TimeSpan(9, 0, 0),
                        Duration = TimeSpan.FromHours(1.5f),
                        SessionType = DailySessionType.Main,
                        Components = new List<ComponentTemplate>
                        {
                            new ComponentTemplate { Type = TrainingComponentType.Fitness, Focus = TrainingFocus.Endurance, Duration = TimeSpan.FromMinutes(90), Intensity = TrainingIntensity.High, LoadMultiplier = 20f }
                        }
                    },
                    // Wednesday - Rest day
                    new DailySessionTemplate
                    {
                        DayOfWeek = DayOfWeek.Wednesday,
                        SessionName = "Rest Day",
                        IsRestDay = true
                    },
                    // Thursday - Tactical
                    new DailySessionTemplate
                    {
                        DayOfWeek = DayOfWeek.Thursday,
                        SessionName = "Tactical Training",
                        StartTime = new TimeSpan(9, 0, 0),
                        Duration = TimeSpan.FromHours(2),
                        SessionType = DailySessionType.Main,
                        Components = new List<ComponentTemplate>
                        {
                            new ComponentTemplate { Type = TrainingComponentType.Tactical, Focus = TrainingFocus.GamePlan, Duration = TimeSpan.FromMinutes(120), Intensity = TrainingIntensity.Moderate, LoadMultiplier = 12f }
                        }
                    },
                    // Friday - Light skills
                    new DailySessionTemplate
                    {
                        DayOfWeek = DayOfWeek.Friday,
                        SessionName = "Match Preparation",
                        StartTime = new TimeSpan(10, 0, 0),
                        Duration = TimeSpan.FromHours(1),
                        SessionType = DailySessionType.Supplementary,
                        SkipOnMatchDay = true,
                        Components = new List<ComponentTemplate>
                        {
                            new ComponentTemplate { Type = TrainingComponentType.Skills, Focus = TrainingFocus.Kicking, Duration = TimeSpan.FromMinutes(60), Intensity = TrainingIntensity.Light, LoadMultiplier = 8f }
                        }
                    }
                }
            };
        }
        
        #endregion
    }
}