using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Services;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLManager.Models;
using AFLManager.Systems.Development;

namespace AFLManager.Systems.Training
{
    /// <summary>
    /// Integrates season calendar and fixture management with training systems
    /// Provides context-aware training scheduling based on match fixtures, byes, and season progression
    /// </summary>
    public class SeasonTrainingCalendarManager : MonoBehaviour
    {
        [Header("System Dependencies")]
        [SerializeField] private SeasonProgressionManager seasonProgressionManager;
        [SerializeField] private WeeklyTrainingScheduleManager trainingScheduleManager;
        [SerializeField] private TrainingFatigueIntegrationManager fatigueIntegrationManager;
        
        [Header("Match Context Training Configuration")]
        [SerializeField] private int preMatchPrepDays = 3; // Days before match for match preparation
        [SerializeField] private int postMatchRecoveryDays = 2; // Days after match for recovery focus
        [SerializeField] private float matchWeekIntensityReduction = 0.7f; // Reduce intensity in match weeks
        [SerializeField] private float byeWeekIntensityIncrease = 1.3f; // Increase intensity in bye weeks
        
        [Header("Seasonal Periodization")]
        [SerializeField] private float earlySeasonIntensity = 1.2f; // Higher intensity early season
        [SerializeField] private float midSeasonIntensity = 1.0f; // Standard intensity mid season
        [SerializeField] private float lateSeasonIntensity = 0.8f; // Lower intensity late season to manage fatigue
        [SerializeField] private int earlySeasonCutoff = 8; // Rounds 1-8 are "early season"
        [SerializeField] private int lateSeasonStart = 18; // Round 18+ is "late season"
        
        [Header("Special Match Preparation")]
        [SerializeField] private bool enableSpecialMatchPreparation = true;
        [SerializeField] private float specialMatchIntensityBoost = 1.1f; // Extra intensity for special matches
        [SerializeField] private int specialMatchPrepExtension = 1; // Extra day of preparation
        
        // Current context tracking
        private SeasonCalendar currentSeason;
        private Dictionary<TeamId, TeamSeasonContext> teamContexts = new Dictionary<TeamId, TeamSeasonContext>();
        private DateTime lastContextUpdate = DateTime.MinValue;
        
        // Events for system integration
        public event System.Action<SeasonTrainingContext> OnSeasonContextChanged;
        public event System.Action<TeamId, MatchContext> OnMatchContextChanged;
        public event System.Action<WeeklyTrainingAdjustment> OnTrainingAdjustmentApplied;
        public event System.Action<TeamId, ByeWeekOpportunity> OnByeWeekOpportunity;
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            // Update context periodically (every hour)
            if (DateTime.Now - lastContextUpdate > TimeSpan.FromHours(1))
            {
                UpdateSeasonContext();
                lastContextUpdate = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Initialize the season training calendar integration
        /// </summary>
        public void Initialize()
        {
            // Find season progression manager if not assigned
            // Note: SeasonProgressionManager is not a MonoBehaviour, can't use FindObjectOfType
            if (seasonProgressionManager == null)
            {
                Debug.LogWarning("[SeasonTrainingCalendar] SeasonProgressionManager not assigned. Some features may be unavailable.");
            }
            
            if (seasonProgressionManager != null)
            {
                // Get current season calendar
                RefreshSeasonCalendar();
                
                // Initialize team contexts
                InitializeTeamContexts();
                
                Debug.Log("[SeasonTrainingCalendar] Season training integration initialized");
            }
            else
            {
                Debug.LogWarning("[SeasonTrainingCalendar] No SeasonProgressionManager found - season integration disabled");
            }
        }
        
        /// <summary>
        /// Get comprehensive training context for a team based on upcoming fixtures
        /// </summary>
        public TeamTrainingContext GetTeamTrainingContext(TeamId teamId, DateTime targetDate = default)
        {
            if (targetDate == default) targetDate = DateTime.Today;
            
            var context = new TeamTrainingContext
            {
                TeamId = teamId,
                ContextDate = targetDate,
                SeasonPhase = GetSeasonPhase(),
                CurrentRound = seasonProgressionManager?.GetCurrentRound()?.RoundNumber ?? 1
            };
            
            if (currentSeason == null || seasonProgressionManager == null)
            {
                context.HasValidContext = false;
                context.RecommendedApproach = "Standard training - no season context available";
                return context;
            }
            
            // Get upcoming match information
            var upcomingMatch = seasonProgressionManager.GetTeamUpcomingMatch(teamId);
            context.UpcomingMatch = upcomingMatch;
            
            // Determine match context
            context.MatchContext = DetermineMatchContext(upcomingMatch, targetDate);
            
            // Check for bye week
            context.IsByeWeek = seasonProgressionManager.IsTeamOnByeThisRound(teamId);
            
            // Calculate training adjustments
            context.TrainingAdjustments = CalculateTrainingAdjustments(context);
            
            // Generate recommendations
            context.RecommendedApproach = GenerateTrainingApproach(context);
            context.SpecialConsiderations = GenerateSpecialConsiderations(context);
            
            context.HasValidContext = true;
            return context;
        }
        
        /// <summary>
        /// Apply season-based adjustments to a weekly training schedule
        /// </summary>
        public WeeklyTrainingAdjustment ApplySeasonalAdjustments(WeeklyTrainingSchedule schedule, TeamId teamId)
        {
            var context = GetTeamTrainingContext(teamId);
            var adjustment = new WeeklyTrainingAdjustment
            {
                TeamId = teamId,
                OriginalSchedule = schedule,
                WeekStartDate = DateTime.Today.GetStartOfWeek(),
                SeasonContext = context
            };
            
            // Apply match-week adjustments
            if (context.MatchContext != MatchContextType.Normal)
            {
                ApplyMatchWeekAdjustments(schedule, context, adjustment);
            }
            
            // Apply bye week adjustments
            if (context.IsByeWeek)
            {
                ApplyByeWeekAdjustments(schedule, context, adjustment);
            }
            
            // Apply seasonal periodization
            ApplySeasonalPeriodization(schedule, context, adjustment);
            
            // Apply special match considerations
            if (context.HasSpecialMatchPreparation)
            {
                ApplySpecialMatchPreparation(schedule, context, adjustment);
            }
            
            // Calculate final intensity and load adjustments
            adjustment.FinalIntensityMultiplier = CalculateFinalIntensityMultiplier(context);
            adjustment.FinalLoadMultiplier = CalculateFinalLoadMultiplier(context);
            
            // Apply final adjustments to schedule
            ApplyFinalAdjustments(schedule, adjustment);
            
            adjustment.AdjustedSchedule = schedule;
            adjustment.Applied = true;
            
            // Trigger events
            OnTrainingAdjustmentApplied?.Invoke(adjustment);
            
            if (context.IsByeWeek)
            {
                OnByeWeekOpportunity?.Invoke(teamId, GenerateByeWeekOpportunity(context));
            }
            
            Debug.Log($"[SeasonTrainingCalendar] Applied seasonal adjustments to {teamId}: {adjustment.GetAdjustmentSummary()}");
            
            return adjustment;
        }
        
        /// <summary>
        /// Get training recommendations for a specific date range
        /// </summary>
        public List<DateBasedTrainingRecommendation> GetTrainingRecommendations(TeamId teamId, DateTime startDate, DateTime endDate)
        {
            var recommendations = new List<DateBasedTrainingRecommendation>();
            var currentDate = startDate;
            
            while (currentDate <= endDate)
            {
                var context = GetTeamTrainingContext(teamId, currentDate);
                var recommendation = new DateBasedTrainingRecommendation
                {
                    Date = currentDate,
                    TeamId = teamId,
                    Context = context,
                    Priority = CalculateRecommendationPriority(context),
                    Recommendations = new List<string>() // Placeholder - would be generated based on context
                };
                
                recommendations.Add(recommendation);
                currentDate = currentDate.AddDays(1);
            }
            
            return recommendations;
        }
        
        /// <summary>
        /// Get season-wide training overview for strategic planning
        /// </summary>
        public SeasonTrainingOverview GetSeasonTrainingOverview(TeamId teamId)
        {
            if (currentSeason == null) return null;
            
            var overview = new SeasonTrainingOverview
            {
                TeamId = teamId,
                SeasonYear = currentSeason.Year,
                CurrentRound = currentSeason.CurrentRound,
                TotalRounds = currentSeason.TotalRounds
            };
            
            // Analyze upcoming fixtures
            var teamMatches = currentSeason.GetTeamMatches(teamId);
            var upcomingMatches = teamMatches.Where(m => m.RoundNumber >= currentSeason.CurrentRound).ToList();
            
            overview.UpcomingMatches = upcomingMatches;
            overview.ByeRounds = GetTeamByeRounds(teamId);
            overview.SpecialMatches = GetTeamSpecialMatches(teamId);
            
            // Calculate training intensity phases
            overview.TrainingPhases = new List<TrainingPhase>(); // Placeholder
            
            // Identify key preparation periods
            overview.KeyPreparationPeriods = new List<KeyPreparationPeriod>(); // Placeholder
            
            // Generate strategic recommendations
            overview.StrategicRecommendations = new List<string>(); // Placeholder
            
            return overview;
        }
        
        #region Private Methods - Context Management
        
        private void RefreshSeasonCalendar()
        {
            // This would typically be injected or retrieved from a service
            // For now, assume we can get it from the progression manager
            // currentSeason = seasonProgressionManager.GetSeasonCalendar();
        }
        
        private void InitializeTeamContexts()
        {
            if (currentSeason == null) return;
            
            teamContexts.Clear();
            
            // Initialize context for all teams
            var allTeams = Enum.GetValues(typeof(TeamId)).Cast<TeamId>().Where(t => t != TeamId.None);
            foreach (var teamId in allTeams)
            {
                teamContexts[teamId] = new TeamSeasonContext
                {
                    TeamId = teamId,
                    LastUpdated = DateTime.Now
                };
            }
        }
        
        private void UpdateSeasonContext()
        {
            if (currentSeason == null || seasonProgressionManager == null) return;
            
            var newContext = new SeasonTrainingContext
            {
                CurrentRound = seasonProgressionManager.GetCurrentRound(),
                SeasonPhase = GetSeasonPhase(),
                UpdateTime = DateTime.Now
            };
            
            OnSeasonContextChanged?.Invoke(newContext);
        }
        
        private SeasonPhase GetSeasonPhase()
        {
            if (currentSeason == null) return SeasonPhase.Unknown;
            
            var currentRound = currentSeason.CurrentRound;
            
            if (currentRound <= earlySeasonCutoff)
                return SeasonPhase.EarlySeason;
            
            if (currentRound >= lateSeasonStart)
                return SeasonPhase.LateSeason;
            
            return SeasonPhase.MidSeason;
        }
        
        private MatchContextType DetermineMatchContext(TeamUpcomingMatch upcomingMatch, DateTime targetDate)
        {
            if (upcomingMatch == null || !upcomingMatch.HasUpcomingMatch)
                return MatchContextType.Normal;
            
            var daysUntilMatch = upcomingMatch.DaysUntilMatch;
            
            // Post-match recovery period
            if (daysUntilMatch < 0 && Math.Abs(daysUntilMatch) <= postMatchRecoveryDays)
                return MatchContextType.PostMatchRecovery;
            
            // Pre-match preparation period
            if (daysUntilMatch > 0 && daysUntilMatch <= preMatchPrepDays)
                return MatchContextType.PreMatchPreparation;
            
            // Match week (within 7 days)
            if (daysUntilMatch > 0 && daysUntilMatch <= 7)
                return MatchContextType.MatchWeek;
            
            return MatchContextType.Normal;
        }
        
        #endregion
        
        #region Private Methods - Training Adjustments
        
        private TrainingAdjustments CalculateTrainingAdjustments(TeamTrainingContext context)
        {
            var adjustments = new TrainingAdjustments();
            
            // Base seasonal adjustment
            adjustments.SeasonalIntensityMultiplier = context.SeasonPhase switch
            {
                SeasonPhase.EarlySeason => earlySeasonIntensity,
                SeasonPhase.MidSeason => midSeasonIntensity,
                SeasonPhase.LateSeason => lateSeasonIntensity,
                _ => 1.0f
            };
            
            // Match context adjustments
            adjustments.MatchContextMultiplier = context.MatchContext switch
            {
                MatchContextType.PreMatchPreparation => 0.8f, // Lower intensity before match
                MatchContextType.PostMatchRecovery => 0.6f, // Much lower intensity after match
                MatchContextType.MatchWeek => matchWeekIntensityReduction,
                _ => 1.0f
            };
            
            // Bye week adjustment
            if (context.IsByeWeek)
            {
                adjustments.ByeWeekMultiplier = byeWeekIntensityIncrease;
            }
            
            // Special match adjustment
            if (context.HasSpecialMatchPreparation)
            {
                adjustments.SpecialMatchMultiplier = specialMatchIntensityBoost;
            }
            
            return adjustments;
        }
        
        private void ApplyMatchWeekAdjustments(WeeklyTrainingSchedule schedule, TeamTrainingContext context, WeeklyTrainingAdjustment adjustment)
        {
            adjustment.MatchWeekAdjustments = new List<string>();
            
            // NOTE: DailyTrainingSession doesn't have PrimaryFocus/IntensityLevel properties
            // This needs to be refactored to work with TrainingComponents
            // For now, just adjust durations based on match context
            
            switch (context.MatchContext)
            {
                case MatchContextType.PreMatchPreparation:
                    foreach (var session in schedule.DailySessions)
                    {
                        session.EstimatedDuration = TimeSpan.FromMinutes(session.EstimatedDuration.TotalMinutes * 0.8);
                        adjustment.MatchWeekAdjustments.Add($"Reduced {session.SessionName} duration for pre-match preparation");
                    }
                    break;
                    
                case MatchContextType.PostMatchRecovery:
                    foreach (var session in schedule.DailySessions)
                    {
                        session.EstimatedDuration = TimeSpan.FromMinutes(session.EstimatedDuration.TotalMinutes * 0.6);
                        adjustment.MatchWeekAdjustments.Add($"Reduced {session.SessionName} to recovery focus");
                    }
                    break;
                    
                case MatchContextType.MatchWeek:
                    foreach (var session in schedule.DailySessions)
                    {
                        session.EstimatedDuration = TimeSpan.FromMinutes(session.EstimatedDuration.TotalMinutes * matchWeekIntensityReduction);
                        adjustment.MatchWeekAdjustments.Add($"Reduced {session.SessionName} duration for match week");
                    }
                    break;
            }
        }
        
        private void ApplyByeWeekAdjustments(WeeklyTrainingSchedule schedule, TeamTrainingContext context, WeeklyTrainingAdjustment adjustment)
        {
            adjustment.ByeWeekAdjustments = new List<string>();
            
            // Increase training duration for bye week (can't access intensity without refactor)
            foreach (var session in schedule.DailySessions)
            {
                session.EstimatedDuration = TimeSpan.FromMinutes(Math.Min(session.EstimatedDuration.TotalMinutes * byeWeekIntensityIncrease, 150));
                adjustment.ByeWeekAdjustments.Add($"Increased {session.SessionName} duration for bye week development focus");
            }
            
            // Add extra sessions if possible
            if (schedule.DailySessions.Count < 6)
            {
                var extraSession = CreateByeWeekBonusSession();
                schedule.DailySessions.Add(extraSession);
                adjustment.ByeWeekAdjustments.Add("Added bonus development session for bye week");
            }
        }
        
        private void ApplySeasonalPeriodization(WeeklyTrainingSchedule schedule, TeamTrainingContext context, WeeklyTrainingAdjustment adjustment)
        {
            adjustment.SeasonalAdjustments = new List<string>();
            
            // Adjust durations based on season phase (intensity adjustment requires component-level refactor)
            switch (context.SeasonPhase)
            {
                case SeasonPhase.EarlySeason:
                    foreach (var session in schedule.DailySessions)
                    {
                        session.EstimatedDuration = TimeSpan.FromMinutes(Math.Min(session.EstimatedDuration.TotalMinutes * earlySeasonIntensity, 150));
                        adjustment.SeasonalAdjustments.Add($"Increased {session.SessionName} duration for early season base building");
                    }
                    break;
                    
                case SeasonPhase.LateSeason:
                    foreach (var session in schedule.DailySessions)
                    {
                        session.EstimatedDuration = TimeSpan.FromMinutes(session.EstimatedDuration.TotalMinutes * lateSeasonIntensity);
                        adjustment.SeasonalAdjustments.Add($"Reduced {session.SessionName} duration for late season maintenance");
                    }
                    break;
            }
        }
        
        private void ApplySpecialMatchPreparation(WeeklyTrainingSchedule schedule, TeamTrainingContext context, WeeklyTrainingAdjustment adjustment)
        {
            if (!enableSpecialMatchPreparation) return;
            
            adjustment.SpecialMatchAdjustments = new List<string>();
            
            // Add extra duration for special match preparation
            foreach (var session in schedule.DailySessions)
            {
                if (session.SessionType == DailySessionType.Tactical)
                {
                    session.EstimatedDuration = TimeSpan.FromMinutes(Math.Min(session.EstimatedDuration.TotalMinutes * specialMatchIntensityBoost, 120));
                    adjustment.SpecialMatchAdjustments.Add($"Extended {session.SessionName} for special match preparation");
                }
            }
        }
        
        private float CalculateFinalIntensityMultiplier(TeamTrainingContext context)
        {
            var multiplier = context.TrainingAdjustments.SeasonalIntensityMultiplier *
                           context.TrainingAdjustments.MatchContextMultiplier *
                           context.TrainingAdjustments.ByeWeekMultiplier *
                           context.TrainingAdjustments.SpecialMatchMultiplier;
                           
            return Math.Max(0.3f, Math.Min(2.0f, multiplier)); // Clamp between 0.3 and 2.0
        }
        
        private float CalculateFinalLoadMultiplier(TeamTrainingContext context)
        {
            // Load follows intensity but with smaller variations
            var intensityMultiplier = CalculateFinalIntensityMultiplier(context);
            var loadMultiplier = 1.0f + (intensityMultiplier - 1.0f) * 0.7f; // 70% of intensity change
            
            return Math.Max(0.5f, Math.Min(1.5f, loadMultiplier)); // Clamp between 0.5 and 1.5
        }
        
        private void ApplyFinalAdjustments(WeeklyTrainingSchedule schedule, WeeklyTrainingAdjustment adjustment)
        {
            var loadMultiplier = adjustment.FinalLoadMultiplier;
            
            foreach (var session in schedule.DailySessions)
            {
                // Adjust duration based on load multiplier
                var newDuration = TimeSpan.FromMinutes(session.EstimatedDuration.TotalMinutes * loadMultiplier);
                session.EstimatedDuration = TimeSpan.FromMinutes(Math.Max(30, Math.Min(180, newDuration.TotalMinutes))); // Clamp between 30-180 minutes
                
                // NOTE: Intensity adjustment requires refactoring to work with TrainingComponents
                // For now, intensity changes are skipped
            }
        }
        
        #endregion
        
        #region Private Methods - Recommendations and Analysis
        
        private string GenerateTrainingApproach(TeamTrainingContext context)
        {
            var approaches = new List<string>();
            
            // Season-based approach
            approaches.Add(context.SeasonPhase switch
            {
                SeasonPhase.EarlySeason => "Focus on fitness base building and skill development",
                SeasonPhase.MidSeason => "Balanced approach with match performance emphasis", 
                SeasonPhase.LateSeason => "Maintenance mode with injury prevention priority",
                _ => "Standard training approach"
            });
            
            // Match context approach
            if (context.MatchContext != MatchContextType.Normal)
            {
                approaches.Add(context.MatchContext switch
                {
                    MatchContextType.PreMatchPreparation => "Tactical focus with reduced physical load",
                    MatchContextType.PostMatchRecovery => "Recovery and regeneration priority",
                    MatchContextType.MatchWeek => "Taper approach with match sharpening",
                    _ => ""
                });
            }
            
            // Bye week approach
            if (context.IsByeWeek)
            {
                approaches.Add("Opportunity for intensive development and conditioning work");
            }
            
            return string.Join(". ", approaches.Where(a => !string.IsNullOrEmpty(a)));
        }
        
        private List<string> GenerateSpecialConsiderations(TeamTrainingContext context)
        {
            var considerations = new List<string>();
            
            if (context.HasSpecialMatchPreparation)
            {
                considerations.Add("Special match requires additional tactical preparation");
            }
            
            if (context.IsByeWeek)
            {
                considerations.Add("Bye week allows for increased training load");
                considerations.Add("Focus on individual player development areas");
            }
            
            if (context.MatchContext == MatchContextType.PostMatchRecovery)
            {
                considerations.Add("Prioritize player recovery and injury assessment");
            }
            
            return considerations;
        }
        
        private DailyTrainingSession CreateByeWeekBonusSession()
        {
            return new DailyTrainingSession
            {
                SessionName = "Bye Week Development",
                SessionDate = DateTime.Today,
                ScheduledStartTime = TimeSpan.FromHours(10),
                EstimatedDuration = TimeSpan.FromMinutes(90),
                SessionType = DailySessionType.SkillsOnly,
                TrainingComponents = new List<TrainingComponent>()
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        private List<int> GetTeamByeRounds(TeamId teamId)
        {
            if (currentSeason == null) return new List<int>();
            
            return currentSeason.Rounds
                .Where(r => r.TeamsOnBye.Contains(teamId))
                .Select(r => r.RoundNumber)
                .ToList();
        }
        
        private List<SpecialtyMatch> GetTeamSpecialMatches(TeamId teamId)
        {
            if (currentSeason == null) return new List<SpecialtyMatch>();
            
            return currentSeason.SpecialtyMatches
                .Where(sm => sm.HomeTeam == teamId || sm.AwayTeam == teamId)
                .ToList();
        }
        
        private RecommendationPriority CalculateRecommendationPriority(TeamTrainingContext context)
        {
            if (context.MatchContext == MatchContextType.PreMatchPreparation)
                return RecommendationPriority.High;
                
            if (context.IsByeWeek)
                return RecommendationPriority.Medium;
                
            if (context.HasSpecialMatchPreparation)
                return RecommendationPriority.Medium;
                
            return RecommendationPriority.Low;
        }
        
        private ByeWeekOpportunity GenerateByeWeekOpportunity(TeamTrainingContext context)
        {
            var currentRound = seasonProgressionManager?.GetCurrentRound();
            
            return new ByeWeekOpportunity
            {
                TeamId = context.TeamId,
                ByeRound = currentRound?.RoundNumber ?? context.CurrentRound,
                ByeWeekStart = DateTime.Today.GetStartOfWeek(),
                ByeWeekEnd = DateTime.Today.GetEndOfWeek(),
                DevelopmentOpportunities = new List<string>
                {
                    "Extended skill development sessions",
                    "Individual player assessment and coaching",
                    "Tactical innovation and experimentation",
                    "Conditioning base building"
                },
                RecommendedFocusAreas = new List<string>
                {
                    "Individual player weaknesses",
                    "Team tactical systems",
                    "Physical conditioning gaps"
                },
                SuggestedLoadIncrease = byeWeekIntensityIncrease - 1.0f, // Convert multiplier to percentage
                CanAddExtraSessions = true,
                BonusSessionRecommendations = new List<TrainingSessionRecommendation>
                {
                    new TrainingSessionRecommendation
                    {
                        SessionName = "Individual Skills Clinic",
                        RecommendedFocus = TrainingFocus.IndividualDevelopment,
                        RecommendedIntensity = TrainingIntensityLevel.Moderate,
                        RecommendedDuration = TimeSpan.FromMinutes(90),
                        Rationale = "Bye week opportunity for focused individual skill work",
                        ParticipationType = TrainingParticipationType.Optional
                    }
                }
            };
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Clean up any subscriptions
        }
    }
}