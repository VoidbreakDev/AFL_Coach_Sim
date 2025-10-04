using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Services;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLManager.Systems.Training
{
    #region Core Season Training Context Types
    
    /// <summary>
    /// Comprehensive training context for a team based on season calendar
    /// </summary>
    [System.Serializable]
    public class TeamTrainingContext
    {
        public TeamId TeamId { get; set; }
        public DateTime ContextDate { get; set; }
        public bool HasValidContext { get; set; } = true;
        
        // Season context
        public SeasonPhase SeasonPhase { get; set; }
        public int CurrentRound { get; set; }
        public TeamUpcomingMatch UpcomingMatch { get; set; }
        
        // Match context
        public MatchContextType MatchContext { get; set; } = MatchContextType.Normal;
        public bool IsByeWeek { get; set; }
        public bool HasSpecialMatchPreparation { get; set; }
        
        // Training adjustments
        public TrainingAdjustments TrainingAdjustments { get; set; } = new TrainingAdjustments();
        
        // Recommendations
        public string RecommendedApproach { get; set; } = "";
        public List<string> SpecialConsiderations { get; set; } = new List<string>();
        
        public string GetContextSummary()
        {
            var summary = $"{TeamId} - Round {CurrentRound} ({SeasonPhase})";
            
            if (IsByeWeek)
                summary += " [BYE WEEK]";
            else if (UpcomingMatch?.HasUpcomingMatch == true)
                summary += $" [Next: vs {UpcomingMatch.Opponent} in {UpcomingMatch.DaysUntilMatch}d]";
                
            if (MatchContext != MatchContextType.Normal)
                summary += $" [{MatchContext}]";
                
            return summary;
        }
    }
    
    /// <summary>
    /// Season-wide training context information
    /// </summary>
    [System.Serializable]
    public class SeasonTrainingContext
    {
        public SeasonRound CurrentRound { get; set; }
        public SeasonPhase SeasonPhase { get; set; }
        public DateTime UpdateTime { get; set; }
        public Dictionary<TeamId, MatchContext> TeamMatchContexts { get; set; } = new Dictionary<TeamId, MatchContext>();
        public List<TeamId> TeamsOnBye { get; set; } = new List<TeamId>();
        
        public string GetSummary()
        {
            return $"Season Context - {CurrentRound?.RoundName ?? "Unknown Round"} ({SeasonPhase}) - {TeamsOnBye.Count} teams on bye";
        }
    }
    
    /// <summary>
    /// Tracking context for individual teams throughout season
    /// </summary>
    [System.Serializable]
    public class TeamSeasonContext
    {
        public TeamId TeamId { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<MatchContextHistory> RecentMatchHistory { get; set; } = new List<MatchContextHistory>();
        public int ConsecutiveMatchWeeks { get; set; }
        public DateTime LastByeWeek { get; set; }
        public float SeasonWorkloadAccumulated { get; set; }
        public List<string> SpecialMatchesUpcoming { get; set; } = new List<string>();
    }
    
    #endregion
    
    #region Training Adjustment Types
    
    /// <summary>
    /// Calculated training adjustments based on season context
    /// </summary>
    [System.Serializable]
    public class TrainingAdjustments
    {
        public float SeasonalIntensityMultiplier { get; set; } = 1.0f;
        public float MatchContextMultiplier { get; set; } = 1.0f;
        public float ByeWeekMultiplier { get; set; } = 1.0f;
        public float SpecialMatchMultiplier { get; set; } = 1.0f;
        
        public float GetCombinedMultiplier()
        {
            return SeasonalIntensityMultiplier * MatchContextMultiplier * ByeWeekMultiplier * SpecialMatchMultiplier;
        }
        
        public string GetAdjustmentBreakdown()
        {
            return $"Seasonal: {SeasonalIntensityMultiplier:F2}, Match: {MatchContextMultiplier:F2}, Bye: {ByeWeekMultiplier:F2}, Special: {SpecialMatchMultiplier:F2} = {GetCombinedMultiplier():F2}";
        }
    }
    
    /// <summary>
    /// Result of applying seasonal adjustments to a training schedule
    /// </summary>
    [System.Serializable]
    public class WeeklyTrainingAdjustment
    {
        public TeamId TeamId { get; set; }
        public DateTime WeekStartDate { get; set; }
        public TeamTrainingContext SeasonContext { get; set; }
        public WeeklyTrainingSchedule OriginalSchedule { get; set; }
        public WeeklyTrainingSchedule AdjustedSchedule { get; set; }
        
        public bool Applied { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.Now;
        
        // Adjustment tracking
        public List<string> MatchWeekAdjustments { get; set; } = new List<string>();
        public List<string> ByeWeekAdjustments { get; set; } = new List<string>();
        public List<string> SeasonalAdjustments { get; set; } = new List<string>();
        public List<string> SpecialMatchAdjustments { get; set; } = new List<string>();
        
        // Final multipliers applied
        public float FinalIntensityMultiplier { get; set; } = 1.0f;
        public float FinalLoadMultiplier { get; set; } = 1.0f;
        
        public string GetAdjustmentSummary()
        {
            var totalAdjustments = MatchWeekAdjustments.Count + ByeWeekAdjustments.Count + 
                                  SeasonalAdjustments.Count + SpecialMatchAdjustments.Count;
            
            return $"{totalAdjustments} adjustments applied - Intensity: {FinalIntensityMultiplier:F2}x, Load: {FinalLoadMultiplier:F2}x";
        }
        
        public List<string> GetAllAdjustments()
        {
            var all = new List<string>();
            all.AddRange(MatchWeekAdjustments);
            all.AddRange(ByeWeekAdjustments);
            all.AddRange(SeasonalAdjustments);
            all.AddRange(SpecialMatchAdjustments);
            return all;
        }
    }
    
    #endregion
    
    #region Recommendation Types
    
    /// <summary>
    /// Date-specific training recommendation
    /// </summary>
    [System.Serializable]
    public class DateBasedTrainingRecommendation
    {
        public DateTime Date { get; set; }
        public TeamId TeamId { get; set; }
        public TeamTrainingContext Context { get; set; }
        public RecommendationPriority Priority { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public string MainFocus { get; set; }
        public TrainingIntensityLevel RecommendedIntensity { get; set; }
        public TimeSpan RecommendedDuration { get; set; }
        
        public string GetRecommendationSummary()
        {
            return $"{Date.ToShortDateString()}: {Priority} priority - {MainFocus} ({RecommendedIntensity}, {RecommendedDuration.TotalMinutes}min)";
        }
    }
    
    /// <summary>
    /// Bye week opportunity for intensive training
    /// </summary>
    [System.Serializable]
    public class ByeWeekOpportunity
    {
        public TeamId TeamId { get; set; }
        public int ByeRound { get; set; }
        public DateTime ByeWeekStart { get; set; }
        public DateTime ByeWeekEnd { get; set; }
        public List<string> DevelopmentOpportunities { get; set; } = new List<string>();
        public List<string> RecommendedFocusAreas { get; set; } = new List<string>();
        public float SuggestedLoadIncrease { get; set; }
        public bool CanAddExtraSessions { get; set; }
        public List<TrainingSessionRecommendation> BonusSessionRecommendations { get; set; } = new List<TrainingSessionRecommendation>();
        
        public string GetOpportunitySummary()
        {
            return $"Bye Week R{ByeRound}: {DevelopmentOpportunities.Count} opportunities, {SuggestedLoadIncrease:P0} load increase suggested";
        }
    }
    
    /// <summary>
    /// Season-wide training overview and strategic planning
    /// </summary>
    [System.Serializable]
    public class SeasonTrainingOverview
    {
        public TeamId TeamId { get; set; }
        public int SeasonYear { get; set; }
        public int CurrentRound { get; set; }
        public int TotalRounds { get; set; }
        
        // Fixture analysis
        public List<ScheduledMatch> UpcomingMatches { get; set; } = new List<ScheduledMatch>();
        public List<int> ByeRounds { get; set; } = new List<int>();
        public List<SpecialtyMatch> SpecialMatches { get; set; } = new List<SpecialtyMatch>();
        
        // Training phases
        public List<TrainingPhase> TrainingPhases { get; set; } = new List<TrainingPhase>();
        public List<KeyPreparationPeriod> KeyPreparationPeriods { get; set; } = new List<KeyPreparationPeriod>();
        
        // Strategic recommendations
        public List<string> StrategicRecommendations { get; set; } = new List<string>();
        
        public string GetOverviewSummary()
        {
            return $"{TeamId} Season {SeasonYear}: {UpcomingMatches.Count} matches remaining, {ByeRounds.Count} bye rounds, {SpecialMatches.Count} special matches";
        }
        
        public float GetSeasonProgress()
        {
            return (float)CurrentRound / TotalRounds;
        }
    }
    
    /// <summary>
    /// Training phase for strategic season planning
    /// </summary>
    [System.Serializable]
    public class TrainingPhase
    {
        public string PhaseName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<int> Rounds { get; set; } = new List<int>();
        public TrainingPhaseType PhaseType { get; set; }
        public string FocusArea { get; set; }
        public float RecommendedIntensityLevel { get; set; }
        public List<string> KeyObjectives { get; set; } = new List<string>();
        
        public TimeSpan Duration => EndDate - StartDate;
        
        public string GetPhaseSummary()
        {
            return $"{PhaseName}: {FocusArea} (Rounds {Rounds.FirstOrDefault()}-{Rounds.LastOrDefault()}, {Duration.Days} days)";
        }
    }
    
    /// <summary>
    /// Key preparation period for important matches
    /// </summary>
    [System.Serializable]
    public class KeyPreparationPeriod
    {
        public string PeriodName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ScheduledMatch TargetMatch { get; set; }
        public string PreparationFocus { get; set; }
        public List<string> SpecialPreparationActivities { get; set; } = new List<string>();
        public int ImportanceLevel { get; set; } // 1-5 scale
        
        public TimeSpan PreparationTime => EndDate - StartDate;
        
        public string GetPreparationSummary()
        {
            return $"{PeriodName}: {PreparationFocus} for {TargetMatch?.HomeTeam} vs {TargetMatch?.AwayTeam} (Importance: {ImportanceLevel}/5)";
        }
    }
    
    /// <summary>
    /// Training session recommendation
    /// </summary>
    [System.Serializable]
    public class TrainingSessionRecommendation
    {
        public string SessionName { get; set; }
        public TrainingFocus RecommendedFocus { get; set; }
        public TrainingIntensityLevel RecommendedIntensity { get; set; }
        public TimeSpan RecommendedDuration { get; set; }
        public string Rationale { get; set; }
        public List<string> KeyActivities { get; set; } = new List<string>();
        public TrainingParticipationType ParticipationType { get; set; }
    }
    
    #endregion
    
    #region Match Context Types
    
    /// <summary>
    /// Match context information for training planning
    /// </summary>
    [System.Serializable]
    public class MatchContext
    {
        public TeamId TeamId { get; set; }
        public ScheduledMatch UpcomingMatch { get; set; }
        public MatchContextType ContextType { get; set; }
        public int DaysUntilMatch { get; set; }
        public bool IsSpecialMatch { get; set; }
        public string SpecialMatchType { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        public string GetContextDescription()
        {
            if (UpcomingMatch == null) return "No upcoming match";
            
            var description = $"vs {(UpcomingMatch.HomeTeam == TeamId ? UpcomingMatch.AwayTeam : UpcomingMatch.HomeTeam)} in {DaysUntilMatch} days";
            
            if (IsSpecialMatch)
                description += $" [{SpecialMatchType}]";
                
            return description;
        }
    }
    
    /// <summary>
    /// Historical match context for tracking patterns
    /// </summary>
    [System.Serializable]
    public class MatchContextHistory
    {
        public DateTime Date { get; set; }
        public MatchContextType ContextType { get; set; }
        public ScheduledMatch Match { get; set; }
        public List<string> TrainingAdjustmentsMade { get; set; } = new List<string>();
        public float PerformanceOutcome { get; set; } // Post-match assessment
        public string Notes { get; set; }
    }
    
    #endregion
    
    #region Enumerations
    
    /// <summary>
    /// Phases of the AFL season for training periodization
    /// </summary>
    public enum SeasonPhase
    {
        Unknown,
        PreSeason,
        EarlySeason,      // Rounds 1-8: Base building and skill development
        MidSeason,        // Rounds 9-17: Peak performance and maintenance
        LateSeason,       // Rounds 18+: Maintenance and injury prevention
        Finals,
        OffSeason
    }
    
    /// <summary>
    /// Match context types that affect training planning
    /// </summary>
    public enum MatchContextType
    {
        Normal,                   // Standard training week
        PreMatchPreparation,      // 1-3 days before match
        PostMatchRecovery,        // 1-2 days after match
        MatchWeek,               // Match week (within 7 days)
        ByeWeek,                 // No match this round
        FinalsSeries             // Finals preparation
    }
    
    /// <summary>
    /// Training phase types for strategic planning
    /// </summary>
    public enum TrainingPhaseType
    {
        BaseBuilding,            // Early season fitness and skill development
        Competition,             // In-season match performance focus
        Maintenance,             // Late season load management
        Peaking,                 // Finals preparation
        Recovery,                // Post-season recovery
        Development              // Individual player development periods
    }
    
    /// <summary>
    /// Training intensity levels
    /// </summary>
    public enum TrainingIntensityLevel
    {
        VeryLight = 1,
        Light = 2,
        Moderate = 3,
        High = 4,
        VeryHigh = 5
    }
    
    /// <summary>
    /// Training focus areas
    /// </summary>
    public enum TrainingFocus
    {
        Conditioning,
        SkillDevelopment,
        TacticalAwareness,
        Strength,
        Speed,
        Recovery,
        IndividualDevelopment,
        MatchSimulation,
        InjuryPrevention
    }
    
    /// <summary>
    /// Training participation types
    /// </summary>
    public enum TrainingParticipationType
    {
        Mandatory,
        Optional,
        Conditional,           // Based on player status/condition
        SelectedPlayersOnly
    }
    
    #endregion
    
    #region Extension Methods
    
    /// <summary>
    /// Extension methods for working with dates in training context
    /// </summary>
    public static class DateTimeTrainingExtensions
    {
        /// <summary>
        /// Get the start of week (Monday) for a given date
        /// </summary>
        public static DateTime GetStartOfWeek(this DateTime date)
        {
            var daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.AddDays(-daysFromMonday).Date;
        }
        
        /// <summary>
        /// Get the end of week (Sunday) for a given date
        /// </summary>
        public static DateTime GetEndOfWeek(this DateTime date)
        {
            return date.GetStartOfWeek().AddDays(6);
        }
        
        /// <summary>
        /// Check if a date falls within a training week
        /// </summary>
        public static bool IsInSameTrainingWeek(this DateTime date, DateTime otherDate)
        {
            return date.GetStartOfWeek() == otherDate.GetStartOfWeek();
        }
    }
    
    /// <summary>
    /// Extension methods for training intensity levels
    /// </summary>
    public static class TrainingIntensityExtensions
    {
        public static Color GetIntensityColor(this TrainingIntensityLevel intensity)
        {
            return intensity switch
            {
                TrainingIntensityLevel.VeryLight => new Color(0.6f, 1f, 0.6f),      // Light green
                TrainingIntensityLevel.Light => Color.green,
                TrainingIntensityLevel.Moderate => Color.yellow,
                TrainingIntensityLevel.High => new Color(1f, 0.5f, 0f),            // Orange
                TrainingIntensityLevel.VeryHigh => Color.red,
                _ => Color.white
            };
        }
        
        public static string GetIntensityDescription(this TrainingIntensityLevel intensity)
        {
            return intensity switch
            {
                TrainingIntensityLevel.VeryLight => "Recovery and light movement",
                TrainingIntensityLevel.Light => "Low intensity skill work",
                TrainingIntensityLevel.Moderate => "Standard training intensity",
                TrainingIntensityLevel.High => "High intensity conditioning",
                TrainingIntensityLevel.VeryHigh => "Maximum effort training",
                _ => "Unknown intensity"
            };
        }
        
        public static float GetIntensityMultiplier(this TrainingIntensityLevel intensity)
        {
            return intensity switch
            {
                TrainingIntensityLevel.VeryLight => 0.4f,
                TrainingIntensityLevel.Light => 0.6f,
                TrainingIntensityLevel.Moderate => 1.0f,
                TrainingIntensityLevel.High => 1.4f,
                TrainingIntensityLevel.VeryHigh => 1.8f,
                _ => 1.0f
            };
        }
    }
    
    /// <summary>
    /// Extension methods for season phases
    /// </summary>
    public static class SeasonPhaseExtensions
    {
        public static string GetPhaseDescription(this SeasonPhase phase)
        {
            return phase switch
            {
                SeasonPhase.PreSeason => "Pre-season preparation and conditioning",
                SeasonPhase.EarlySeason => "Base building and skill development",
                SeasonPhase.MidSeason => "Peak performance and competition",
                SeasonPhase.LateSeason => "Load management and maintenance",
                SeasonPhase.Finals => "Finals preparation and peaking",
                SeasonPhase.OffSeason => "Recovery and regeneration",
                _ => "Unknown phase"
            };
        }
        
        public static Color GetPhaseColor(this SeasonPhase phase)
        {
            return phase switch
            {
                SeasonPhase.PreSeason => new Color(0.8f, 0.8f, 1f),      // Light blue
                SeasonPhase.EarlySeason => Color.green,
                SeasonPhase.MidSeason => Color.yellow,
                SeasonPhase.LateSeason => new Color(1f, 0.5f, 0f),       // Orange
                SeasonPhase.Finals => Color.red,
                SeasonPhase.OffSeason => Color.gray,
                _ => Color.white
            };
        }
    }
    
    #endregion
    
    #region Utility Classes
    
    /// <summary>
    /// Utility class for generating missing method implementations
    /// </summary>
    public static class SeasonTrainingCalendarHelpers
    {
        /// <summary>
        /// Generate date-specific training recommendations
        /// </summary>
        public static List<string> GenerateDateSpecificRecommendations(TeamTrainingContext context, DateTime date)
        {
            var recommendations = new List<string>();
            
            // Day of week considerations
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    if (context.MatchContext == MatchContextType.PostMatchRecovery)
                        recommendations.Add("Focus on active recovery and light movement");
                    else
                        recommendations.Add("Start week with moderate intensity training");
                    break;
                    
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                    if (context.IsByeWeek)
                        recommendations.Add("Opportunity for high-intensity development work");
                    else
                        recommendations.Add("Main training days - focus on primary objectives");
                    break;
                    
                case DayOfWeek.Thursday:
                case DayOfWeek.Friday:
                    if (context.MatchContext == MatchContextType.PreMatchPreparation)
                        recommendations.Add("Match preparation - tactical focus, reduced load");
                    else
                        recommendations.Add("Week completion - moderate intensity");
                    break;
                    
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    recommendations.Add("Weekend - optional training or match day");
                    break;
            }
            
            // Context-specific recommendations
            if (context.HasSpecialMatchPreparation)
            {
                recommendations.Add("Extra tactical preparation for special match required");
            }
            
            return recommendations;
        }
        
        /// <summary>
        /// Calculate training phases for a team's remaining season
        /// </summary>
        public static List<TrainingPhase> CalculateTrainingPhases(TeamId teamId, List<ScheduledMatch> upcomingMatches)
        {
            var phases = new List<TrainingPhase>();
            
            if (!upcomingMatches.Any()) return phases;
            
            var seasonStart = upcomingMatches.First().ScheduledDateTime.GetStartOfWeek();
            var seasonEnd = upcomingMatches.Last().ScheduledDateTime.GetStartOfWeek().AddDays(7);
            
            // Calculate phases based on remaining matches
            var totalWeeks = (seasonEnd - seasonStart).Days / 7;
            var currentWeek = DateTime.Now.GetStartOfWeek();
            
            if (totalWeeks > 16) // Long season remaining
            {
                // Early phase (first third)
                var earlyPhase = new TrainingPhase
                {
                    PhaseName = "Base Building",
                    StartDate = currentWeek,
                    EndDate = currentWeek.AddWeeks(totalWeeks / 3),
                    PhaseType = TrainingPhaseType.BaseBuilding,
                    FocusArea = "Fitness and skill development",
                    RecommendedIntensityLevel = 1.2f,
                    KeyObjectives = new List<string> { "Build aerobic base", "Develop core skills", "Establish patterns" }
                };
                phases.Add(earlyPhase);
                
                // Competition phase (middle third)
                var competitionPhase = new TrainingPhase
                {
                    PhaseName = "Competition Focus",
                    StartDate = earlyPhase.EndDate,
                    EndDate = earlyPhase.EndDate.AddWeeks(totalWeeks / 3),
                    PhaseType = TrainingPhaseType.Competition,
                    FocusArea = "Match performance",
                    RecommendedIntensityLevel = 1.0f,
                    KeyObjectives = new List<string> { "Peak performance", "Tactical mastery", "Match simulation" }
                };
                phases.Add(competitionPhase);
                
                // Maintenance phase (final third)
                var maintenancePhase = new TrainingPhase
                {
                    PhaseName = "Maintenance",
                    StartDate = competitionPhase.EndDate,
                    EndDate = seasonEnd,
                    PhaseType = TrainingPhaseType.Maintenance,
                    FocusArea = "Load management",
                    RecommendedIntensityLevel = 0.8f,
                    KeyObjectives = new List<string> { "Maintain fitness", "Prevent injuries", "Manage fatigue" }
                };
                phases.Add(maintenancePhase);
            }
            else
            {
                // Shorter season - single competition phase
                var competitionPhase = new TrainingPhase
                {
                    PhaseName = "Competition Phase",
                    StartDate = currentWeek,
                    EndDate = seasonEnd,
                    PhaseType = TrainingPhaseType.Competition,
                    FocusArea = "Match performance and maintenance",
                    RecommendedIntensityLevel = 0.9f,
                    KeyObjectives = new List<string> { "Maintain performance", "Manage loads", "Prepare for matches" }
                };
                phases.Add(competitionPhase);
            }
            
            return phases;
        }
        
        /// <summary>
        /// Identify key preparation periods for important matches
        /// </summary>
        public static List<KeyPreparationPeriod> IdentifyKeyPreparationPeriods(List<ScheduledMatch> upcomingMatches)
        {
            var periods = new List<KeyPreparationPeriod>();
            
            // Look for matches that might need special preparation
            foreach (var match in upcomingMatches.Where(m => m.MatchTags?.Any() == true))
            {
                var importance = CalculateMatchImportance(match);
                if (importance >= 3) // Only high importance matches
                {
                    var period = new KeyPreparationPeriod
                    {
                        PeriodName = $"Preparation for {match.HomeTeam} vs {match.AwayTeam}",
                        StartDate = match.ScheduledDateTime.AddDays(-7).GetStartOfWeek(),
                        EndDate = match.ScheduledDateTime.Date,
                        TargetMatch = match,
                        PreparationFocus = GetPreparationFocus(match),
                        ImportanceLevel = importance,
                        SpecialPreparationActivities = GeneratePreparationActivities(match)
                    };
                    
                    periods.Add(period);
                }
            }
            
            return periods.OrderBy(p => p.StartDate).ToList();
        }
        
        /// <summary>
        /// Generate strategic recommendations for the season
        /// </summary>
        public static List<string> GenerateSeasonStrategicRecommendations(SeasonTrainingOverview overview)
        {
            var recommendations = new List<string>();
            
            var remainingMatches = overview.UpcomingMatches.Count;
            var byeRoundsRemaining = overview.ByeRounds.Count(br => br > overview.CurrentRound);
            var specialMatches = overview.SpecialMatches.Count;
            
            // Match load recommendations
            if (remainingMatches > 15)
            {
                recommendations.Add("Long season ahead - focus on building robust fitness base");
                recommendations.Add("Plan periodization carefully to avoid late-season fatigue");
            }
            else if (remainingMatches < 8)
            {
                recommendations.Add("Season ending - prioritize load management and injury prevention");
                recommendations.Add("Focus on maintaining rather than building fitness");
            }
            
            // Bye week recommendations
            if (byeRoundsRemaining > 0)
            {
                recommendations.Add($"{byeRoundsRemaining} bye week(s) remaining - plan development intensives");
                recommendations.Add("Use bye weeks for individual player development focus");
            }
            
            // Special match recommendations
            if (specialMatches > 0)
            {
                recommendations.Add($"{specialMatches} special matches require additional tactical preparation");
            }
            
            // Season progress recommendations
            var progress = overview.GetSeasonProgress();
            if (progress < 0.33f)
            {
                recommendations.Add("Early season - emphasize base building and skill development");
            }
            else if (progress < 0.75f)
            {
                recommendations.Add("Mid-season - balance performance with workload management");
            }
            else
            {
                recommendations.Add("Late season - prioritize player freshness and injury prevention");
            }
            
            return recommendations;
        }
        
        #region Private Helper Methods
        
        private static int CalculateMatchImportance(ScheduledMatch match)
        {
            int importance = 1; // Base importance
            
            if (match.MatchTags?.Contains("Finals") == true) importance += 3;
            if (match.MatchTags?.Contains("Derby") == true) importance += 2;
            if (match.MatchTags?.Contains("Rivalry") == true) importance += 2;
            if (match.MatchTags?.Contains("ANZAC") == true) importance += 2;
            if (match.MatchTags?.Contains("Special") == true) importance += 1;
            
            return Math.Min(5, importance);
        }
        
        private static string GetPreparationFocus(ScheduledMatch match)
        {
            if (match.MatchTags?.Contains("Finals") == true) return "Peak performance and tactical mastery";
            if (match.MatchTags?.Contains("Derby") == true) return "Intensity and mental preparation";
            if (match.MatchTags?.Contains("ANZAC") == true) return "Traditional preparation and respect";
            
            return "Standard match preparation";
        }
        
        private static List<string> GeneratePreparationActivities(ScheduledMatch match)
        {
            var activities = new List<string> { "Extended tactical sessions", "Video analysis", "Set piece practice" };
            
            if (match.MatchTags?.Contains("Finals") == true)
            {
                activities.AddRange(new[] { "Pressure situation training", "Mental skills coaching", "Recovery optimization" });
            }
            
            return activities;
        }
        
        private static DateTime AddWeeks(this DateTime date, int weeks)
        {
            return date.AddDays(weeks * 7);
        }
        
        #endregion
    }
    
    #endregion
}