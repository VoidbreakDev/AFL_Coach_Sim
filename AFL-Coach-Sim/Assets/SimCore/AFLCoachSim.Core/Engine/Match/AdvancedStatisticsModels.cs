using System;
using System.Collections.Generic;

namespace AFLCoachSim.Core.Engine.Match
{
    /// <summary>
    /// Comprehensive player statistics tracking all performance metrics
    /// </summary>
    public class PlayerStatistics
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Position { get; set; }
        public int TeamId { get; set; }
        public DateTime MatchStartTime { get; set; }
        public TimeSpan TimeOnGround { get; set; }
        
        // Basic disposal stats
        public int TotalDisposals { get; set; }
        public int EffectiveDisposals { get; set; }
        public int Kicks { get; set; }
        public int Handballs { get; set; }
        public int Turnovers { get; set; }
        public int ContestedDisposals { get; set; }
        public int UncontestedDisposals { get; set; }
        
        // Advanced disposal metrics
        public float DisposalAccuracy { get; set; }
        public float AveragePressure { get; set; }
        
        // Contested possession stats
        public int ContestedPossessions { get; set; }
        public int ContestedPossessionsWon { get; set; }
        public int HitOuts { get; set; }
        public int GroundBallGets { get; set; }
        public float ContestedPossessionIntensity { get; set; }
        
        // Marking stats
        public int Marks { get; set; }
        public int ContestedMarks { get; set; }
        public int UncontestedMarks { get; set; }
        public int InterceptMarks { get; set; }
        public int MarkingContests { get; set; }
        public int Intercepts { get; set; }
        public float AverageMarkingHeight { get; set; }
        
        // Pressure and defensive stats
        public int PressureActs { get; set; }
        public int Tackles { get; set; }
        public int EffectiveTackles { get; set; }
        public int ChaseDownTackles { get; set; }
        public int Spoils { get; set; }
        public int Smothers { get; set; }
        public float PressureIntensity { get; set; }
        
        // Scoring stats
        public int Goals { get; set; }
        public int Behinds { get; set; }
        public int GoalAssists { get; set; }
        public int Shots { get; set; }
        public int ShotsUnderPressure { get; set; }
        public float AverageShotDistance { get; set; }
    }
    
    /// <summary>
    /// Team-level statistics aggregating all team performance metrics
    /// </summary>
    public class TeamStatistics
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public DateTime MatchStartTime { get; set; }
        
        // Aggregate team stats
        public int TotalDisposals { get; set; }
        public int EffectiveDisposals { get; set; }
        public int Kicks { get; set; }
        public int Handballs { get; set; }
        public int Turnovers { get; set; }
        public int ContestedDisposals { get; set; }
        public int UncontestedDisposals { get; set; }
        
        public int ContestedPossessions { get; set; }
        public int ContestedPossessionsWon { get; set; }
        public int Marks { get; set; }
        public int Intercepts { get; set; }
        public int PressureActs { get; set; }
        public int Goals { get; set; }
        public int Behinds { get; set; }
    }
    
    /// <summary>
    /// Statistical event record for detailed match analysis
    /// </summary>
    public class StatisticalEvent
    {
        public string PlayerId { get; set; }
        public StatEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }
    
    /// <summary>
    /// Performance snapshot for tracking player performance over time
    /// </summary>
    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public TimeSpan TimeOnGround { get; set; }
        public float CurrentRating { get; set; }
        public float FatigueLevel { get; set; }
        public float PerformanceIndex { get; set; }
        public float CumulativeImpact { get; set; }
    }
    
    /// <summary>
    /// Advanced player performance metrics with calculated insights
    /// </summary>
    public class AdvancedPlayerMetrics
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        
        // Efficiency metrics
        public float DisposalEfficiency { get; set; }
        public float ContestedPossessionWinRate { get; set; }
        public float MarkingConversionRate { get; set; }
        public float TackleEfficiency { get; set; }
        public float ShotAccuracy { get; set; }
        
        // Impact metrics
        public float OverallPerformanceRating { get; set; }
        public float OffensiveImpact { get; set; }
        public float DefensiveImpact { get; set; }
        public float LeadershipIndex { get; set; }
        public float ClutchPerformance { get; set; }
        
        // Pressure metrics
        public float PressureHandling { get; set; }
        public float PressureApplication { get; set; }
        public float DecisionMakingUnderPressure { get; set; }
        
        // Consistency metrics
        public float PerformanceConsistency { get; set; }
        public float FatigueResistance { get; set; }
        public float AdaptabilityIndex { get; set; }
        
        // Advanced analytics
        public float PlayerRating { get; set; }
        public float ExpectedImpact { get; set; }
        public float ActualVsExpected { get; set; }
        public Dictionary<string, float> PositionSpecificMetrics { get; set; }
    }
    
    /// <summary>
    /// Advanced team performance metrics
    /// </summary>
    public class AdvancedTeamMetrics
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        
        // Team efficiency metrics
        public float TeamDisposalEfficiency { get; set; }
        public float ContestedPossessionRate { get; set; }
        public float TurnoverRate { get; set; }
        public float PressureRating { get; set; }
        public float FieldPositionControl { get; set; }
        
        // Team cohesion metrics
        public float TeamChemistry { get; set; }
        public float CommunicationIndex { get; set; }
        public float SupportPlayRating { get; set; }
        
        // Tactical metrics
        public float TacticalDiscipline { get; set; }
        public float AdaptabilityRating { get; set; }
        public float FormationEffectiveness { get; set; }
        
        // Momentum metrics
        public float MomentumGeneration { get; set; }
        public float MomentumSustaining { get; set; }
        public float CrunchTimePerformance { get; set; }
        
        public Dictionary<string, float> PositionalBalance { get; set; }
    }
    
    /// <summary>
    /// Key moment identification for match highlights and analysis
    /// </summary>
    public class KeyMoment
    {
        public DateTime Timestamp { get; set; }
        public KeyMomentType Type { get; set; }
        public string PlayerId { get; set; }
        public string Description { get; set; }
        public float ImpactLevel { get; set; } // 0-10 scale
        public Dictionary<string, object> Context { get; set; }
    }
    
    /// <summary>
    /// Performance trend analysis for individual players
    /// </summary>
    public class PerformanceTrend
    {
        public string PlayerId { get; set; }
        public TrendDirection TrendDirection { get; set; }
        public float PerformanceVariability { get; set; }
        public float PeakPerformance { get; set; }
        public float AveragePerformance { get; set; }
        public List<TrendPoint> TrendPoints { get; set; }
    }
    
    public class TrendPoint
    {
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
        public string Context { get; set; }
    }
    
    /// <summary>
    /// Comprehensive match analytics summary
    /// </summary>
    public class MatchAnalytics
    {
        public TimeSpan MatchDuration { get; set; }
        public int TotalEvents { get; set; }
        
        public Dictionary<string, AdvancedPlayerMetrics> PlayerMetrics { get; set; }
        public Dictionary<int, AdvancedTeamMetrics> TeamMetrics { get; set; }
        
        public List<KeyMoment> KeyMoments { get; set; }
        public Dictionary<string, PerformanceTrend> PerformanceTrends { get; set; }
        public StatisticalSummary StatisticalSummary { get; set; }
        
        public Dictionary<string, float> MomentumFlow { get; set; }
        public Dictionary<string, MatchPhaseAnalysis> PhaseAnalysis { get; set; }
    }
    
    /// <summary>
    /// Statistical summary with key match statistics
    /// </summary>
    public class StatisticalSummary
    {
        public int TotalDisposals { get; set; }
        public int TotalGoals { get; set; }
        public int TotalMarks { get; set; }
        public int TotalTackles { get; set; }
        public int TotalContestedPossessions { get; set; }
        
        public float AverageDisposalEfficiency { get; set; }
        public float AverageContestedPossessionRate { get; set; }
        public float AveragePressureRating { get; set; }
        
        public Dictionary<string, int> EventCounts { get; set; }
        public Dictionary<string, float> AverageRatings { get; set; }
    }
    
    /// <summary>
    /// Match phase analysis for tactical insights
    /// </summary>
    public class MatchPhaseAnalysis
    {
        public string PhaseName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public float DominanceRating { get; set; }
        public Dictionary<string, float> KeyMetrics { get; set; }
        public List<string> SignificantEvents { get; set; }
    }
    
    /// <summary>
    /// Advanced metrics calculator with sophisticated algorithms
    /// </summary>
    public class AdvancedMetricsCalculator
    {
        /// <summary>
        /// Calculate comprehensive player metrics
        /// </summary>
        public AdvancedPlayerMetrics CalculatePlayerMetrics(PlayerStatistics stats, 
            List<PerformanceSnapshot> performanceHistory)
        {
            var metrics = new AdvancedPlayerMetrics
            {
                PlayerId = stats.PlayerId,
                PlayerName = stats.PlayerName,
                PositionSpecificMetrics = new Dictionary<string, float>()
            };
            
            // Calculate efficiency metrics
            metrics.DisposalEfficiency = stats.TotalDisposals > 0 
                ? (float)stats.EffectiveDisposals / stats.TotalDisposals 
                : 0f;
                
            metrics.ContestedPossessionWinRate = stats.ContestedPossessions > 0
                ? (float)stats.ContestedPossessionsWon / stats.ContestedPossessions
                : 0f;
                
            metrics.MarkingConversionRate = stats.MarkingContests > 0
                ? (float)stats.Marks / stats.MarkingContests
                : 0f;
                
            metrics.TackleEfficiency = stats.Tackles > 0
                ? (float)stats.EffectiveTackles / stats.Tackles
                : 0f;
                
            metrics.ShotAccuracy = stats.Shots > 0
                ? (float)stats.Goals / stats.Shots
                : 0f;
            
            // Calculate impact metrics
            var minutesPlayed = Math.Max(1, stats.TimeOnGround.TotalMinutes);
            
            metrics.OffensiveImpact = (float)((stats.EffectiveDisposals * 1.2 + 
                                             stats.Goals * 6 + 
                                             stats.GoalAssists * 3 + 
                                             stats.ContestedMarks * 2) / minutesPlayed);
                                             
            metrics.DefensiveImpact = (float)((stats.EffectiveTackles * 2 + 
                                             stats.Intercepts * 3 + 
                                             stats.Spoils * 1.5 + 
                                             stats.PressureActs * 1) / minutesPlayed);
            
            // Calculate overall performance rating
            metrics.OverallPerformanceRating = CalculateOverallRating(stats, performanceHistory);
            
            // Calculate pressure metrics
            metrics.PressureHandling = CalculatePressureHandling(stats);
            metrics.PressureApplication = CalculatePressureApplication(stats);
            
            // Calculate consistency metrics
            if (performanceHistory.Count > 1)
            {
                var performances = performanceHistory.Select(p => p.PerformanceIndex).ToArray();
                metrics.PerformanceConsistency = CalculateConsistency(performances);
                metrics.FatigueResistance = CalculateFatigueResistance(performanceHistory);
            }
            
            // Position-specific calculations
            CalculatePositionSpecificMetrics(metrics, stats);
            
            return metrics;
        }
        
        /// <summary>
        /// Calculate comprehensive team metrics
        /// </summary>
        public AdvancedTeamMetrics CalculateTeamMetrics(TeamStatistics teamStats, 
            List<PlayerStatistics> playerStats)
        {
            var metrics = new AdvancedTeamMetrics
            {
                TeamId = teamStats.TeamId,
                TeamName = teamStats.TeamName,
                PositionalBalance = new Dictionary<string, float>()
            };
            
            // Calculate team efficiency
            metrics.TeamDisposalEfficiency = teamStats.TotalDisposals > 0
                ? (float)teamStats.EffectiveDisposals / teamStats.TotalDisposals
                : 0f;
                
            metrics.ContestedPossessionRate = teamStats.TotalDisposals > 0
                ? (float)teamStats.ContestedDisposals / teamStats.TotalDisposals
                : 0f;
                
            metrics.TurnoverRate = teamStats.TotalDisposals > 0
                ? (float)teamStats.Turnovers / teamStats.TotalDisposals
                : 0f;
            
            // Calculate team cohesion metrics
            metrics.TeamChemistry = CalculateTeamChemistry(playerStats);
            metrics.SupportPlayRating = CalculateSupportPlayRating(playerStats);
            
            // Calculate tactical metrics
            metrics.TacticalDiscipline = CalculateTacticalDiscipline(playerStats);
            metrics.AdaptabilityRating = CalculateAdaptabilityRating(playerStats);
            
            // Calculate positional balance
            CalculatePositionalBalance(metrics, playerStats);
            
            return metrics;
        }
        
        // Private calculation methods
        private float CalculateOverallRating(PlayerStatistics stats, 
            List<PerformanceSnapshot> performanceHistory)
        {
            // Complex algorithm combining multiple factors
            float baseRating = 50f;
            
            // Disposal impact
            if (stats.TotalDisposals > 0)
            {
                baseRating += (stats.DisposalAccuracy * 20f);
                baseRating += ((float)stats.EffectiveDisposals / stats.TotalDisposals * 15f);
            }
            
            // Contested work
            if (stats.ContestedPossessions > 0)
            {
                baseRating += ((float)stats.ContestedPossessionsWon / stats.ContestedPossessions * 10f);
            }
            
            // Scoring impact
            baseRating += (stats.Goals * 2f);
            baseRating += (stats.GoalAssists * 1f);
            
            // Defensive work
            if (stats.Tackles > 0)
            {
                baseRating += ((float)stats.EffectiveTackles / stats.Tackles * 8f);
            }
            
            // Performance trend adjustment
            if (performanceHistory.Count >= 3)
            {
                var recentPerformance = performanceHistory.TakeLast(3).Average(p => p.PerformanceIndex);
                baseRating += (recentPerformance - 0.5f) * 10f;
            }
            
            return Math.Min(100f, Math.Max(0f, baseRating));
        }
        
        private float CalculatePressureHandling(PlayerStatistics stats)
        {
            if (stats.TotalDisposals == 0) return 50f;
            
            float pressureRating = 50f;
            
            // Factor in contested vs uncontested ratio
            float contestedRatio = (float)stats.ContestedDisposals / stats.TotalDisposals;
            pressureRating += contestedRatio * 25f;
            
            // Factor in disposal efficiency under pressure
            if (stats.ContestedDisposals > 0)
            {
                float contestedEfficiency = (float)stats.EffectiveDisposals / stats.ContestedDisposals;
                pressureRating += contestedEfficiency * 25f;
            }
            
            return Math.Min(100f, Math.Max(0f, pressureRating));
        }
        
        private float CalculatePressureApplication(PlayerStatistics stats)
        {
            float pressureRating = 50f;
            
            // Factor in tackle efficiency
            if (stats.Tackles > 0)
            {
                pressureRating += ((float)stats.EffectiveTackles / stats.Tackles * 25f);
            }
            
            // Factor in pressure acts per minute
            var minutesPlayed = Math.Max(1, stats.TimeOnGround.TotalMinutes);
            float pressureActsPerMinute = (float)stats.PressureActs / minutesPlayed;
            pressureRating += Math.Min(25f, pressureActsPerMinute * 10f);
            
            return Math.Min(100f, Math.Max(0f, pressureRating));
        }
        
        private float CalculateConsistency(float[] performances)
        {
            if (performances.Length < 2) return 50f;
            
            var mean = performances.Average();
            var variance = performances.Select(p => Math.Pow(p - mean, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);
            
            // Lower standard deviation = higher consistency
            return (float)(100f - (standardDeviation * 100f));
        }
        
        private float CalculateFatigueResistance(List<PerformanceSnapshot> history)
        {
            if (history.Count < 3) return 50f;
            
            // Analyze performance decline over time
            var early = history.Take(history.Count / 3).Average(p => p.PerformanceIndex);
            var late = history.TakeLast(history.Count / 3).Average(p => p.PerformanceIndex);
            
            float resistance = 50f + ((late - early) * 50f);
            return Math.Min(100f, Math.Max(0f, resistance));
        }
        
        private void CalculatePositionSpecificMetrics(AdvancedPlayerMetrics metrics, 
            PlayerStatistics stats)
        {
            switch (stats.Position?.ToLower())
            {
                case "forward":
                    metrics.PositionSpecificMetrics["GoalsPerMinute"] = 
                        (float)stats.Goals / Math.Max(1, stats.TimeOnGround.TotalMinutes);
                    metrics.PositionSpecificMetrics["ShotConversion"] = 
                        stats.Shots > 0 ? (float)stats.Goals / stats.Shots : 0f;
                    break;
                    
                case "defender":
                    metrics.PositionSpecificMetrics["InterceptsPerMinute"] = 
                        (float)stats.Intercepts / Math.Max(1, stats.TimeOnGround.TotalMinutes);
                    metrics.PositionSpecificMetrics["SpoilsPerMinute"] = 
                        (float)stats.Spoils / Math.Max(1, stats.TimeOnGround.TotalMinutes);
                    break;
                    
                case "midfielder":
                    metrics.PositionSpecificMetrics["DisposalsPerMinute"] = 
                        (float)stats.TotalDisposals / Math.Max(1, stats.TimeOnGround.TotalMinutes);
                    metrics.PositionSpecificMetrics["ContestedRate"] = 
                        stats.TotalDisposals > 0 ? (float)stats.ContestedDisposals / stats.TotalDisposals : 0f;
                    break;
                    
                case "ruck":
                    metrics.PositionSpecificMetrics["HitOutRate"] = 
                        (float)stats.HitOuts / Math.Max(1, stats.TimeOnGround.TotalMinutes);
                    break;
            }
        }
        
        private float CalculateTeamChemistry(List<PlayerStatistics> playerStats)
        {
            // Complex algorithm factoring in assists, support play, etc.
            var totalAssists = playerStats.Sum(p => p.GoalAssists);
            var totalGoals = playerStats.Sum(p => p.Goals);
            
            if (totalGoals == 0) return 50f;
            
            float assistRatio = (float)totalAssists / totalGoals;
            return Math.Min(100f, 50f + (assistRatio * 50f));
        }
        
        private float CalculateSupportPlayRating(List<PlayerStatistics> playerStats)
        {
            var totalMinutes = playerStats.Sum(p => p.TimeOnGround.TotalMinutes);
            if (totalMinutes == 0) return 50f;
            
            var supportActions = playerStats.Sum(p => p.GoalAssists + p.EffectiveDisposals);
            return Math.Min(100f, (float)(supportActions / totalMinutes * 10f));
        }
        
        private float CalculateTacticalDiscipline(List<PlayerStatistics> playerStats)
        {
            var totalDisposals = playerStats.Sum(p => p.TotalDisposals);
            if (totalDisposals == 0) return 50f;
            
            var turnovers = playerStats.Sum(p => p.Turnovers);
            float turnoverRate = (float)turnovers / totalDisposals;
            
            return Math.Min(100f, 100f - (turnoverRate * 100f));
        }
        
        private float CalculateAdaptabilityRating(List<PlayerStatistics> playerStats)
        {
            // Placeholder for complex adaptability calculation
            return 75f; // Would need historical tactical data
        }
        
        private void CalculatePositionalBalance(AdvancedTeamMetrics metrics, 
            List<PlayerStatistics> playerStats)
        {
            var positions = playerStats.GroupBy(p => p.Position);
            
            foreach (var positionGroup in positions)
            {
                var players = positionGroup.ToList();
                var avgRating = players.Average(p => p.TimeOnGround.TotalMinutes > 0 
                    ? (p.EffectiveDisposals + p.Goals * 3) / p.TimeOnGround.TotalMinutes 
                    : 0);
                
                metrics.PositionalBalance[positionGroup.Key] = (float)avgRating;
            }
        }
    }
    
    // Supporting enums
    public enum StatEventType
    {
        Disposal,
        ContestedPossession,
        PressureAct,
        MarkingContest,
        Score,
        Substitution,
        Injury,
        TacticalChange
    }
    
    public enum DisposalType
    {
        Kick,
        Handball
    }
    
    public enum DisposalOutcome
    {
        Effective,
        Ineffective,
        Turnover
    }
    
    public enum ContestedType
    {
        Ruck,
        GroundBall,
        Marking,
        Tackle
    }
    
    public enum PressureActType
    {
        Tackle,
        Chase,
        Spoil,
        Smother,
        Block
    }
    
    public enum MarkType
    {
        Contested,
        Uncontested,
        Intercept
    }
    
    public enum ScoreType
    {
        Goal,
        Behind
    }
    
    public enum KeyMomentType
    {
        Goal,
        ContestedMark,
        BigTackle,
        Turnover,
        MomentumShift,
        Injury,
        TacticalChange
    }
    
    public enum TrendDirection
    {
        Improving,
        Stable,
        Declining
    }
}