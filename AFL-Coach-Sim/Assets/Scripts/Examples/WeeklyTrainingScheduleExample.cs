using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Services;
using AFLManager.Models;
using AFLManager.Systems.Development;
using AFLManager.Systems.Training;
using UnityEngine;

namespace AFLManager.Examples
{
    /// <summary>
    /// Example demonstrating the Weekly Training Schedule System integration
    /// This shows how the training system works with season management and player development
    /// </summary>
    public class WeeklyTrainingScheduleExample : MonoBehaviour
    {
        [Header("System Components")]
        [SerializeField] private WeeklyTrainingScheduleManager scheduleManager;
        [SerializeField] private TeamTrainingManager trainingManager;
        
        [Header("Example Data")]
        [SerializeField] private List<Player> examplePlayers = new List<Player>();
        [SerializeField] private bool autoRunExample = true;
        [SerializeField] private bool logDetailedOutput = true;
        
        // Example state
        private WeeklyTrainingSchedule currentWeekSchedule;
        private int currentWeekNumber = 1;
        
        private void Start()
        {
            if (autoRunExample)
            {
                StartCoroutine(RunExampleCoroutine());
            }
        }
        
        /// <summary>
        /// Run the complete weekly training example
        /// </summary>
        public void RunCompleteExample()
        {
            StartCoroutine(RunExampleCoroutine());
        }
        
        private System.Collections.IEnumerator RunExampleCoroutine()
        {
            Debug.Log("=== AFL Coach Sim: Weekly Training Schedule Example ===");
            yield return new WaitForSeconds(0.5f);
            
            // Step 1: Setup example data
            SetupExampleData();
            yield return new WaitForSeconds(1f);
            
            // Step 2: Demonstrate different schedule templates
            DemonstrateScheduleTemplates();
            yield return new WaitForSeconds(1f);
            
            // Step 3: Generate and analyze a standard week
            GenerateAndAnalyzeStandardWeek();
            yield return new WaitForSeconds(1f);
            
            // Step 4: Simulate executing training sessions
            yield return StartCoroutine(SimulateTrainingWeek());
            
            // Step 5: Generate match week and bye week examples
            GenerateSpecialWeekExamples();
            yield return new WaitForSeconds(1f);
            
            // Step 6: Show load management in action
            DemonstrateLoadManagement();
            yield return new WaitForSeconds(1f);
            
            Debug.Log("=== Weekly Training Schedule Example Complete ===");
        }
        
        /// <summary>
        /// Setup example players and data for the demonstration
        /// </summary>
        private void SetupExampleData()
        {
            Debug.Log("[Example] Setting up example data...");
            
            // Create some example players if none provided
            if (examplePlayers.Count == 0)
            {
                examplePlayers = CreateExamplePlayers();
            }
            
            // Initialize the training schedule manager
            if (scheduleManager != null)
            {
                scheduleManager.Initialize();
                
                // Subscribe to events for logging
                scheduleManager.OnWeeklyScheduleGenerated += OnScheduleGenerated;
                scheduleManager.OnDailySessionCompleted += OnSessionCompleted;
                scheduleManager.OnPlayerLoadExceeded += OnPlayerLoadExceeded;
            }
            
            Debug.Log($"[Example] Setup complete with {examplePlayers.Count} example players");
        }
        
        /// <summary>
        /// Demonstrate different schedule template types
        /// </summary>
        private void DemonstrateScheduleTemplates()
        {
            Debug.Log("[Example] Demonstrating schedule template types:");
            
            var templates = TrainingScheduleTemplateFactory.GetAllTemplates();
            
            foreach (var template in templates)
            {
                var activeDays = template.DayTemplates.Count(dt => !dt.IsRestDay);
                var totalLoad = template.GetTemplateLoad();
                
                Debug.Log($"  • {template.TemplateName} ({template.TemplateType}): {activeDays} training days, {totalLoad:F1} total load");
                
                if (logDetailedOutput)
                {
                    foreach (var dayTemplate in template.DayTemplates.Where(dt => !dt.IsRestDay))
                    {
                        Debug.Log($"    - {dayTemplate.DayOfWeek}: {dayTemplate.SessionName} ({dayTemplate.GetDayLoad():F1} load)");
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate and analyze a standard training week
        /// </summary>
        private void GenerateAndAnalyzeStandardWeek()
        {
            Debug.Log("[Example] Generating standard training week...");
            
            if (scheduleManager == null)
            {
                Debug.LogError("Schedule manager not assigned!");
                return;
            }
            
            // Generate schedule for current week
            currentWeekSchedule = scheduleManager.GenerateWeeklySchedule();
            
            if (currentWeekSchedule != null)
            {
                AnalyzeWeeklySchedule(currentWeekSchedule);
            }
        }
        
        /// <summary>
        /// Simulate executing training sessions throughout a week
        /// </summary>
        private System.Collections.IEnumerator SimulateTrainingWeek()
        {
            Debug.Log("[Example] Simulating training week execution...");
            
            if (currentWeekSchedule == null || examplePlayers.Count == 0)
            {
                Debug.LogWarning("No schedule or players available for simulation");
                yield break;
            }
            
            // Sort sessions by day
            var sessionsInOrder = currentWeekSchedule.DailySessions
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.ScheduledStartTime)
                .ToList();
            
            Debug.Log($"[Example] Executing {sessionsInOrder.Count} training sessions...");
            
            foreach (var session in sessionsInOrder)
            {
                yield return new WaitForSeconds(0.5f); // Simulate time passing
                
                Debug.Log($"[Example] Executing: {session.SessionName} on {session.SessionDate:dddd}");
                
                // Select participants (simulate some players not available)
                var participants = SelectSessionParticipants(session);
                
                // Execute the session
                if (scheduleManager != null && participants.Count > 0)
                {
                    var results = scheduleManager.ExecuteDailySession(session.SessionId, participants);
                    LogSessionResults(session, results);
                }
                else
                {
                    Debug.Log($"  - No participants available for {session.SessionName}");
                }
            }
            
            // Show weekly summary
            ShowWeeklySummary();
        }
        
        /// <summary>
        /// Generate examples of special week types (match week, bye week)
        /// </summary>
        private void GenerateSpecialWeekExamples()
        {
            Debug.Log("[Example] Generating special week examples...");
            
            // Generate match week
            var matchWeek = GenerateExampleSchedule(ScheduleTemplateType.MatchWeek, "Match Week Example");
            AnalyzeWeeklySchedule(matchWeek);
            
            // Generate bye week
            var byeWeek = GenerateExampleSchedule(ScheduleTemplateType.ByeWeek, "Bye Week Example");
            AnalyzeWeeklySchedule(byeWeek);
            
            // Generate finals week
            var finalsWeek = GenerateExampleSchedule(ScheduleTemplateType.Finals, "Finals Week Example");
            AnalyzeWeeklySchedule(finalsWeek);
        }
        
        /// <summary>
        /// Demonstrate load management features
        /// </summary>
        private void DemonstrateLoadManagement()
        {
            Debug.Log("[Example] Demonstrating load management:");
            
            if (scheduleManager == null || examplePlayers.Count == 0) return;
            
            foreach (var player in examplePlayers.Take(5)) // Show first 5 players
            {
                var load = scheduleManager.GetPlayerWeeklyLoad(player.ID);
                var canTrain = scheduleManager.CanPlayerTrain(player.ID, 25f); // Test with 25 load points
                
                Debug.Log($"  • {player.Name}: {load.CurrentLoad:F1}/{load.MaxLoad} load ({load.GetLoadUtilization():F1}%) - Can train more: {canTrain}");
                
                if (load.IsApproachingLimit())
                {
                    Debug.Log($"    ⚠️ {player.Name} is approaching training load limit!");
                }
            }
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Create example players for demonstration
        /// </summary>
        private List<Player> CreateExamplePlayers()
        {
            var players = new List<Player>();
            var positions = new[] { PlayerRole.Centre, PlayerRole.Wing, PlayerRole.FullForward, PlayerRole.FullBack, PlayerRole.Ruckman };
            var names = new[] { "Jack Smith", "Mike Johnson", "Tom Wilson", "Dan Brown", "Alex Davis", "Sam Miller", "Chris Lee", "Matt Taylor" };
            
            for (int i = 0; i < names.Length; i++)
            {
                var player = new Player
                {
                    ID = i + 1,
                    Name = names[i],
                    Role = positions[i % positions.Length],
                    Age = UnityEngine.Random.Range(19, 33),
                    Stamina = UnityEngine.Random.Range(65f, 85f),
                    Morale = UnityEngine.Random.Range(70f, 90f),
                    Stats = new PlayerStats
                    {
                        Kicking = UnityEngine.Random.Range(60f, 85f),
                        Marking = UnityEngine.Random.Range(55f, 80f),
                        Handballing = UnityEngine.Random.Range(65f, 85f),
                        Contested = UnityEngine.Random.Range(50f, 75f),
                        Endurance = UnityEngine.Random.Range(60f, 80f)
                    },
                    Development = new PlayerDevelopment(i + 1) // Basic development system
                };
                
                players.Add(player);
            }
            
            return players;
        }
        
        /// <summary>
        /// Generate example schedule using specific template
        /// </summary>
        private WeeklyTrainingSchedule GenerateExampleSchedule(ScheduleTemplateType templateType, string logTitle)
        {
            var template = TrainingScheduleTemplateFactory.GetTemplate(templateType);
            
            var schedule = new WeeklyTrainingSchedule
            {
                WeekStartDate = DateTime.Today.AddDays(7 * currentWeekNumber++),
                WeekEndDate = DateTime.Today.AddDays(7 * currentWeekNumber + 6),
                Template = template,
                DailySessions = new List<DailyTrainingSession>()
            };
            
            // Generate sessions from template (simplified)
            for (int day = 0; day < 7; day++)
            {
                var dayOfWeek = (DayOfWeek)((int)(schedule.WeekStartDate.DayOfWeek) + day) % 7;
                var dayTemplate = template.GetDayTemplate(dayOfWeek);
                
                if (dayTemplate != null && !dayTemplate.IsRestDay)
                {
                    var session = new DailyTrainingSession
                    {
                        SessionId = UnityEngine.Random.Range(10000, 99999),
                        SessionDate = schedule.WeekStartDate.AddDays(day),
                        SessionName = dayTemplate.SessionName,
                        ScheduledStartTime = dayTemplate.StartTime,
                        EstimatedDuration = dayTemplate.Duration,
                        SessionType = dayTemplate.SessionType,
                        Status = TrainingSessionStatus.Scheduled,
                        TrainingComponents = dayTemplate.Components.Select(ct => new TrainingComponent
                        {
                            ComponentType = ct.Type,
                            Duration = ct.Duration,
                            Intensity = ct.Intensity,
                            LoadMultiplier = ct.LoadMultiplier
                        }).ToList()
                    };
                    
                    schedule.DailySessions.Add(session);
                }
            }
            
            return schedule;
        }
        
        /// <summary>
        /// Analyze and log details about a weekly schedule
        /// </summary>
        private void AnalyzeWeeklySchedule(WeeklyTrainingSchedule schedule)
        {
            if (schedule == null) return;
            
            var templateName = schedule.Template?.TemplateName ?? "Unknown";
            var totalLoad = schedule.GetTotalWeeklyLoad();
            var sessionCount = schedule.DailySessions.Count;
            
            Debug.Log($"[Analysis] {templateName}: {sessionCount} sessions, {totalLoad:F1} total load");
            
            if (logDetailedOutput)
            {
                var loadByType = schedule.DailySessions
                    .GroupBy(s => s.SessionType)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.GetSessionLoad()));
                
                foreach (var kvp in loadByType)
                {
                    Debug.Log($"  - {kvp.Key} sessions: {kvp.Value:F1} load");
                }
                
                var intensityBreakdown = schedule.DailySessions
                    .SelectMany(s => s.TrainingComponents)
                    .GroupBy(c => c.Intensity)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                Debug.Log($"  - Intensity breakdown: {string.Join(", ", intensityBreakdown.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
            }
        }
        
        /// <summary>
        /// Select participants for a training session (simulate availability)
        /// </summary>
        private List<Player> SelectSessionParticipants(DailyTrainingSession session)
        {
            var participants = new List<Player>();
            
            foreach (var player in examplePlayers)
            {
                // Simulate 85% availability rate
                if (UnityEngine.Random.Range(0f, 1f) < 0.85f)
                {
                    // Check if player can handle the training load
                    if (scheduleManager != null && scheduleManager.CanPlayerTrain(player.ID, session.GetSessionLoad()))
                    {
                        participants.Add(player);
                    }
                }
            }
            
            return participants;
        }
        
        /// <summary>
        /// Log results from a completed training session
        /// </summary>
        private void LogSessionResults(DailyTrainingSession session, List<PlayerTrainingResult> results)
        {
            if (!logDetailedOutput) return;
            
            var totalImprovement = results.Sum(r => r.StatChanges.GetTotalChange());
            var averageEffectiveness = results.Any() ? results.Average(r => r.EffectivenessRating) : 0f;
            var injuries = results.Count(r => r.WasInjured);
            
            Debug.Log($"  - Results: {results.Count} participants, {totalImprovement:F2} total improvement, {averageEffectiveness:F2} avg effectiveness, {injuries} injuries");
            
            if (injuries > 0)
            {
                Debug.LogWarning($"    ⚕️ {injuries} training injuries occurred");
            }
        }
        
        /// <summary>
        /// Show summary of the week's training
        /// </summary>
        private void ShowWeeklySummary()
        {
            Debug.Log("[Example] Weekly Training Summary:");
            
            if (scheduleManager == null) return;
            
            var analytics = CalculateWeeklyAnalytics();
            
            Debug.Log($"  • Sessions completed: {analytics.TotalSessionsCompleted}/{analytics.TotalSessionsScheduled} ({analytics.GetCompletionRate():F1}%)");
            Debug.Log($"  • Average player load: {analytics.AveragePlayerLoad:F1}");
            Debug.Log($"  • Average effectiveness: {analytics.AverageEffectiveness:F2}");
            Debug.Log($"  • Players over-loaded: {analytics.PlayersOverLoaded}");
        }
        
        /// <summary>
        /// Calculate analytics for the current week
        /// </summary>
        private WeeklyTrainingAnalytics CalculateWeeklyAnalytics()
        {
            var analytics = new WeeklyTrainingAnalytics
            {
                WeekStartDate = currentWeekSchedule?.WeekStartDate ?? DateTime.Today
            };
            
            if (currentWeekSchedule != null)
            {
                analytics.TotalSessionsScheduled = currentWeekSchedule.DailySessions.Count;
                analytics.TotalSessionsCompleted = currentWeekSchedule.DailySessions.Count(s => s.Status == TrainingSessionStatus.Completed);
                
                // Calculate average load across all players
                var playerLoads = examplePlayers.Select(p => scheduleManager?.GetPlayerWeeklyLoad(p.ID).CurrentLoad ?? 0f).ToList();
                analytics.AveragePlayerLoad = playerLoads.Any() ? playerLoads.Average() : 0f;
                
                // Count overloaded players
                analytics.PlayersOverLoaded = playerLoads.Count(load => load > 80f); // Assuming 80+ is overloaded
                
                // Simulate some effectiveness (would be calculated from actual results)
                analytics.AverageEffectiveness = UnityEngine.Random.Range(0.75f, 1.25f);
            }
            
            return analytics;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnScheduleGenerated(WeeklyTrainingSchedule schedule)
        {
            Debug.Log($"[Event] Weekly schedule generated: {schedule.Template.TemplateName}");
        }
        
        private void OnSessionCompleted(DailyTrainingSession session, List<PlayerTrainingResult> results)
        {
            Debug.Log($"[Event] Session completed: {session.SessionName} with {results.Count} participants");
        }
        
        private void OnPlayerLoadExceeded(int playerId, float currentLoad)
        {
            var player = examplePlayers.FirstOrDefault(p => p.ID == playerId);
            var playerName = player?.Name ?? $"Player {playerId}";
            Debug.LogWarning($"[Event] Player load exceeded: {playerName} at {currentLoad:F1} load");
        }
        
        #endregion
        
        #region Unity Inspector Methods
        
        [ContextMenu("Run Example")]
        public void RunExampleFromInspector()
        {
            RunCompleteExample();
        }
        
        [ContextMenu("Generate Standard Week")]
        public void GenerateStandardWeekFromInspector()
        {
            GenerateAndAnalyzeStandardWeek();
        }
        
        [ContextMenu("Show Current Player Loads")]
        public void ShowCurrentPlayerLoadsFromInspector()
        {
            DemonstrateLoadManagement();
        }
        
        #endregion
    }
}