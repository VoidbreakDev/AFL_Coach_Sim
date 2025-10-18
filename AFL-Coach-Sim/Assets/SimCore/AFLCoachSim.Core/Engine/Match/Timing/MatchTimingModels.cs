using System;
using System.Collections.Generic;
using System.Linq;

namespace AFLCoachSim.Core.Engine.Match.Timing
{
    /// <summary>
    /// Configuration for match timing system
    /// </summary>
    public class MatchTimingConfiguration
    {
        public float QuarterDurationSeconds { get; set; } = 20 * 60; // 20 minutes
        
        // Break durations
        public float QuarterBreakDuration { get; set; } = 6 * 60; // 6 minutes
        public float HalfTimeDuration { get; set; } = 20 * 60; // 20 minutes
        public float ThreeQuarterBreakDuration { get; set; } = 6 * 60; // 6 minutes
        
        // Phase time modifiers (how much real time passes per game time)
        public float ShotPhaseTimeModifier { get; set; } = 0.3f; // Shots take less real time
        public float Inside50TimeModifier { get; set; } = 0.7f; // Inside 50 is slightly slower
        public float CenterBounceTimeModifier { get; set; } = 0.5f; // Bounces are quick
        public float OpenPlayTimeModifier { get; set; } = 1.0f; // Normal pace
        public float StoppageTimeModifier { get; set; } = 0.2f; // Stoppages slow the clock
        public float KickInTimeModifier { get; set; } = 0.8f; // Kick-ins are relatively quick
        
        // Weather time modifiers
        public float HeavyRainTimeModifier { get; set; } = 0.85f; // Heavy rain slows the game
        public float LightRainTimeModifier { get; set; } = 0.95f; // Light rain slightly slower
        public float WindyTimeModifier { get; set; } = 0.98f; // Wind has minimal impact on time
        
        // Quarter-specific pacing
        public float FirstQuarterSettlingModifier { get; set; } = 0.95f; // Teams settling in
        public float FourthQuarterModifier { get; set; } = 1.05f; // Urgency speeds up play
        public float FourthQuarterFinalMinutesModifier { get; set; } = 1.2f; // Final minutes are frantic
        
        // Injury and stoppage modifiers
        public float InjuryTimeModifier { get; set; } = 0.1f; // Injuries slow the clock significantly
        
        // Time-on calculation parameters
        public float TimeOnPerInjury { get; set; } = 30f; // 30 seconds per injury
        public float TimeOnPerMajorStoppage { get; set; } = 15f; // 15 seconds per major stoppage
        public float TimeOnRandomVariation { get; set; } = 10f; // ±10 seconds random variation
        public float MaxTimeOnPerQuarter { get; set; } = 120f; // Max 2 minutes time-on per quarter
        
        public static MatchTimingConfiguration Default { get; } = new MatchTimingConfiguration();
    }
    
    /// <summary>
    /// Update information from timing system
    /// </summary>
    public class TimingUpdate
    {
        public int CurrentQuarter { get; set; }
        public float TimeRemaining { get; set; }
        public float TimeOnClock { get; set; }
        public bool ClockRunning { get; set; }
        public Phase CurrentPhase { get; set; }
        public float PhaseTimeSpent { get; set; }
        
        // Time-on information
        public bool InTimeOnPeriod { get; set; }
        public float TimeOnRemaining { get; set; }
        public bool TimeOnStarted { get; set; }
        public bool TimeOnEnded { get; set; }
        public float TimeOnDuration { get; set; }
        
        // Quarter transition information
        public bool QuarterEnded { get; set; }
        public int CompletedQuarter { get; set; }
        public bool QuarterBreakStarted { get; set; }
        public bool QuarterBreakEnded { get; set; }
        public bool NewQuarterStarted { get; set; }
        public bool InQuarterBreak { get; set; }
        public float QuarterBreakRemaining { get; set; }
        
        // Match completion
        public bool MatchEnded { get; set; }
        
        // Performance tracking
        public float RealTimeElapsed { get; set; }
        public float GameTimeElapsed { get; set; }
    }
    
    /// <summary>
    /// Detailed timing data for a specific quarter
    /// </summary>
    public class QuarterTimingData
    {
        public int Quarter { get; set; }
        public float PlannedDuration { get; set; }
        public float ActualDuration { get; set; }
        public float TimeOnDuration { get; set; }
        public Dictionary<Phase, float> PhaseBreakdown { get; set; }
        
        public QuarterTimingData()
        {
            PhaseBreakdown = new Dictionary<Phase, float>();
        }
        
        public float TotalDuration => ActualDuration + TimeOnDuration;
        public float TimeOnPercentage => TotalDuration > 0 ? TimeOnDuration / TotalDuration : 0f;
    }
    
    /// <summary>
    /// Timing event for detailed analysis
    /// </summary>
    public class TimingEvent
    {
        public TimingEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public float GameTime { get; set; }
        public float RealTime { get; set; }
        public int Quarter { get; set; }
        public string Description { get; set; }
    }
    
    /// <summary>
    /// Comprehensive timing statistics for match analysis
    /// </summary>
    public class MatchTimingStatistics
    {
        public float TotalRealTime { get; set; }
        public float TotalGameTime { get; set; }
        public int CurrentQuarter { get; set; }
        public float TimeRemaining { get; set; }
        public bool InTimeOnPeriod { get; set; }
        public float TimeOnRemaining { get; set; }
        
        public List<QuarterTimingData> QuarterData { get; set; }
        public Dictionary<Phase, float> PhaseBreakdown { get; set; }
        public List<TimingEvent> TimingEvents { get; set; }
        
        // Calculated statistics
        public Dictionary<Phase, float> AverageTimePerPhase { get; set; }
        public float GamePacing { get; set; }
        public float MatchCompletionPercentage { get; set; }
        
        public MatchTimingStatistics()
        {
            QuarterData = new List<QuarterTimingData>();
            PhaseBreakdown = new Dictionary<Phase, float>();
            TimingEvents = new List<TimingEvent>();
            AverageTimePerPhase = new Dictionary<Phase, float>();
        }
        
        // Convenience properties
        public float TotalTimeOn => QuarterData.Sum(q => q.TimeOnDuration);
        public float AverageQuarterLength => QuarterData.Count > 0 ? QuarterData.Average(q => q.TotalDuration) : 0f;
        public float LongestQuarter => QuarterData.Count > 0 ? QuarterData.Max(q => q.TotalDuration) : 0f;
        public float ShortestQuarter => QuarterData.Count > 0 ? QuarterData.Min(q => q.TotalDuration) : 0f;
    }
    
    /// <summary>
    /// Clock display information for UI
    /// </summary>
    public class ClockDisplay
    {
        public string TimeDisplay { get; set; }
        public string DetailedTimeDisplay { get; set; }
        public int Quarter { get; set; }
        public bool InTimeOnPeriod { get; set; }
        public bool ClockRunning { get; set; }
        public string QuarterDisplay => $"Q{Quarter}";
        public string FullDisplay => $"{QuarterDisplay} {DetailedTimeDisplay}";
    }
    
    /// <summary>
    /// Match pace analysis
    /// </summary>
    public class MatchPaceAnalysis
    {
        public float AveragePacePerQuarter { get; set; }
        public Dictionary<int, float> QuarterPacing { get; set; }
        public Dictionary<Phase, float> PhasePacing { get; set; }
        public float OverallGameSpeed { get; set; }
        public List<PaceChangeEvent> SignificantPaceChanges { get; set; }
        
        public MatchPaceAnalysis()
        {
            QuarterPacing = new Dictionary<int, float>();
            PhasePacing = new Dictionary<Phase, float>();
            SignificantPaceChanges = new List<PaceChangeEvent>();
        }
    }
    
    /// <summary>
    /// Significant pace change during the match
    /// </summary>
    public class PaceChangeEvent
    {
        public float GameTime { get; set; }
        public int Quarter { get; set; }
        public Phase Phase { get; set; }
        public float PaceBefore { get; set; }
        public float PaceAfter { get; set; }
        public string Reason { get; set; }
        public float Impact { get; set; }
    }
    
    /// <summary>
    /// Time-based match insights
    /// </summary>
    public class TimingInsights
    {
        public List<string> QuarterSummaries { get; set; }
        public string PacingAnalysis { get; set; }
        public string TimeOnAnalysis { get; set; }
        public List<string> NotableTimingMoments { get; set; }
        public Dictionary<string, float> TimingFactors { get; set; }
        
        public TimingInsights()
        {
            QuarterSummaries = new List<string>();
            NotableTimingMoments = new List<string>();
            TimingFactors = new Dictionary<string, float>();
        }
    }
    
    /// <summary>
    /// Enhanced match context with timing integration
    /// </summary>
    public class TimingMatchContext
    {
        public MatchContext BaseContext { get; set; }
        public EnhancedMatchTiming TimingSystem { get; set; }
        public ClockDisplay ClockDisplay { get; set; }
        public MatchPaceAnalysis PaceAnalysis { get; set; }
        
        public bool ShouldUpdateScoreboard => TimingSystem != null && 
            (ClockDisplay?.ClockRunning == true || ClockDisplay?.InTimeOnPeriod == true);
            
        public bool IsSignificantTimingEvent(TimingUpdate update)
        {
            return update.QuarterEnded || update.TimeOnStarted || update.MatchEnded ||
                   update.NewQuarterStarted || update.TimeOnEnded;
        }
    }
    
    // Supporting enums
    public enum TimingEventType
    {
        MatchStart,
        MatchEnd,
        QuarterStart,
        QuarterEnd,
        TimeOnStarted,
        TimeOnEnded,
        ClockPaused,
        ClockResumed,
        PaceChange,
        SignificantStoppage
    }
    
    /// <summary>
    /// Extensions for timing system integration
    /// </summary>
    public static class TimingExtensions
    {
        public static ClockDisplay ToClockDisplay(this TimingUpdate update)
        {
            return new ClockDisplay
            {
                TimeDisplay = FormatTime(update.TimeOnClock),
                DetailedTimeDisplay = update.InTimeOnPeriod 
                    ? $"{FormatTime(update.TimeOnClock)} + {FormatTime(update.TimeOnRemaining)}"
                    : FormatTime(update.TimeOnClock),
                Quarter = update.CurrentQuarter,
                InTimeOnPeriod = update.InTimeOnPeriod,
                ClockRunning = update.ClockRunning
            };
        }
        
        private static string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60);
            int remainingSeconds = (int)(seconds % 60);
            return $"{minutes:D2}:{remainingSeconds:D2}";
        }
        
        public static bool IsQuarterTime(this TimingUpdate update)
        {
            return update.TimeRemaining <= 0 && !update.InTimeOnPeriod;
        }
        
        public static bool IsHalfTime(this TimingUpdate update)
        {
            return update.QuarterEnded && update.CompletedQuarter == 2;
        }
        
        public static bool IsFinalQuarter(this TimingUpdate update)
        {
            return update.CurrentQuarter == 4;
        }
        
        public static bool IsMatchCriticalTime(this TimingUpdate update)
        {
            return update.IsFinalQuarter() && update.TimeRemaining <= 300f; // Last 5 minutes
        }
    }
}