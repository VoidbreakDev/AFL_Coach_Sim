using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Awards.Brownlow
{
    /// <summary>
    /// Configuration for the Brownlow voting system
    /// </summary>
    public class BrownlowVotingConfiguration
    {
        public int MinimumPlayingTime { get; set; } = 20; // Minimum minutes to be eligible for votes
        public float CloseMatchThreshold { get; set; } = 12f; // Points difference for close match bonus
        public float FinalQuarterWeighting { get; set; } = 1.1f; // Extra weight for final quarter performance
        public float BigMatchBonus { get; set; } = 0.05f; // Bonus for big matches
        public float FairnessThreshold { get; set; } = 0.8f; // Minimum fairness rating
        public float SportsmanshipThreshold { get; set; } = 0.9f; // Minimum sportsmanship rating
        
        // Position modifiers for Brownlow voting tendencies
        public float MidfielderBonus { get; set; } = 1.05f;
        public float ForwardBonus { get; set; } = 1.02f;
        public float DefenderBonus { get; set; } = 0.98f;
        public float RuckBonus { get; set; } = 0.95f;
        
        // Voting weights
        public float StatisticalWeight { get; set; } = 0.4f;
        public float RatingWeight { get; set; } = 0.25f;
        public float ImpactWeight { get; set; } = 0.2f;
        public float ConsistencyWeight { get; set; } = 0.15f;
        
        public static BrownlowVotingConfiguration Default => new BrownlowVotingConfiguration();
    }
    
    /// <summary>
    /// Player profile for Brownlow voting tracking
    /// </summary>
    public class BrownlowPlayerProfile
    {
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public Role Position { get; set; }
        public TeamId TeamId { get; set; }
        public int Age { get; set; }
        
        // Historical Brownlow performance
        public List<int> PreviousSeasonVotes { get; set; } = new();
        public int CareerVotes { get; set; }
        public int MedalsWon { get; set; }
        public int Top3Finishes { get; set; }
        
        // Performance characteristics
        public float HistoricalVotingRate { get; set; } // Votes per game historically
        public float PeakVotingForm { get; set; } // Best voting period
        public BrownlowTrend CareerTrend { get; set; }
        
        // Voting patterns
        public Dictionary<Role, float> PositionalVotingRates { get; set; } = new();
        public Dictionary<TeamId, float> VsTeamPerformance { get; set; } = new();
        public float HomeGroundVotingRate { get; set; }
        public float AwayGroundVotingRate { get; set; }
    }
    
    /// <summary>
    /// Complete evaluation of a player's match performance for Brownlow voting
    /// </summary>
    public class BrownlowPlayerEvaluation
    {
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public Role Position { get; set; }
        public TeamId TeamId { get; set; }
        
        // Performance scores
        public float RawPerformanceScore { get; set; }
        public float FinalBrownlowScore { get; set; }
        
        // Detailed metrics
        public float StatisticalImpact { get; set; }
        public float ContextualPerformance { get; set; }
        public float LeadershipFactor { get; set; }
        public float ClutchPerformance { get; set; }
        public float MomentumInfluence { get; set; }
        
        // Fairness and sportsmanship
        public float FairnessRating { get; set; }
        public float SportsmanshipDeductions { get; set; }
        
        // Vote allocation results
        public int VotesAwarded { get; set; }
        public int VotingRank { get; set; }
        
        // Supporting information
        public Dictionary<string, object> DetailedBreakdown { get; set; } = new();
        public string VotingJustification { get; set; }
        public DateTime EvaluationTimestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Complete voting record for a single match
    /// </summary>
    public class BrownlowMatchVoting
    {
        public Guid MatchId { get; set; }
        public TeamId HomeTeamId { get; set; }
        public TeamId AwayTeamId { get; set; }
        public DateTime MatchDate { get; set; }
        public DateTime VotingTimestamp { get; set; }
        
        // Vote winners
        public PlayerId ThreeVoteWinner { get; set; }
        public PlayerId TwoVoteWinner { get; set; }
        public PlayerId OneVoteWinner { get; set; }
        public PlayerId BestOnGroundPlayer { get; set; }
        
        // Complete evaluations
        public Dictionary<PlayerId, BrownlowPlayerEvaluation> PlayerEvaluations { get; set; } = new();
        
        // Match context
        public BrownlowMatchContext MatchContext { get; set; }
        
        // Voting quality metrics
        public float VotingConfidence { get; set; } // How confident the system is in the allocation
        public bool HasTiedVotes { get; set; }
        public string VotingNotes { get; set; }
    }
    
    /// <summary>
    /// Context information for match voting
    /// </summary>
    public class BrownlowMatchContext
    {
        public string FinalScore { get; set; }
        public int Margin { get; set; }
        public float MatchDuration { get; set; }
        public bool WasCloseMatch { get; set; }
        public string Weather { get; set; }
        public string Venue { get; set; }
        public bool IsFinalsMatch { get; set; }
        public bool IsRivalryMatch { get; set; }
        public bool IsBigMatch { get; set; }
        public int CrowdSize { get; set; }
        public List<string> SpecialCircumstances { get; set; } = new();
    }
    
    /// <summary>
    /// Season-long tracking for individual players
    /// </summary>
    public class BrownlowSeasonTracker
    {
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public TeamId TeamId { get; set; }
        public Role Position { get; set; }
        
        // Season statistics
        public int TotalVotes { get; set; }
        public int GamesPlayed { get; set; }
        public int ThreeVoteGames { get; set; }
        public int TwoVoteGames { get; set; }
        public int OneVoteGames { get; set; }
        
        // Performance tracking
        public float TotalMatchScore { get; set; }
        public float AverageMatchScore { get; set; }
        public float HighestMatchScore { get; set; }
        public float ConsistencyRating { get; set; }
        
        // Recent performance
        public List<float> RecentScores { get; set; } = new();
        public List<int> RecentVotes { get; set; } = new();
        
        // Timeline
        public DateTime LastUpdated { get; set; }
        public List<DateTime> VotingDates { get; set; } = new();
    }
    
    /// <summary>
    /// Individual player standing on the Brownlow leaderboard
    /// </summary>
    public class BrownlowPlayerStanding
    {
        public int Rank { get; set; }
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public TeamId TeamId { get; set; }
        public Role Position { get; set; }
        
        // Vote statistics
        public int TotalVotes { get; set; }
        public int ThreeVoteGames { get; set; }
        public int TwoVoteGames { get; set; }
        public int OneVoteGames { get; set; }
        public int TotalGamesEligible { get; set; }
        
        // Performance metrics
        public float AverageMatchScore { get; set; }
        public float HighestMatchScore { get; set; }
        public float ConsistencyRating { get; set; }
        
        // Form and trends
        public float RecentForm { get; set; }
        public BrownlowTrend VotingTrend { get; set; }
        
        // Eligibility
        public bool IsEligible { get; set; }
        public string EligibilityNotes { get; set; }
        
        // Statistics
        public float VotesPerGame => TotalGamesEligible > 0 ? (float)TotalVotes / TotalGamesEligible : 0f;
        public float VotingPercentage => TotalGamesEligible > 0 ? (float)(ThreeVoteGames + TwoVoteGames + OneVoteGames) / TotalGamesEligible : 0f;
    }
    
    /// <summary>
    /// Complete Brownlow Medal leaderboard
    /// </summary>
    public class BrownlowLeaderboard
    {
        public int Season { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<BrownlowPlayerStanding> PlayerStandings { get; set; } = new();
        public BrownlowStatisticalSummary StatisticalSummary { get; set; }
        
        // Leaderboard insights
        public List<string> KeyInsights { get; set; } = new();
        public List<BrownlowMilestone> RecentMilestones { get; set; } = new();
        public Dictionary<string, object> SeasonStatistics { get; set; } = new();
    }
    
    /// <summary>
    /// Statistical summary for the leaderboard
    /// </summary>
    public class BrownlowStatisticalSummary
    {
        public int TotalPlayersWithVotes { get; set; }
        public int TotalVotesAwarded { get; set; }
        public float HighestSingleMatchScore { get; set; }
        public float AverageVotesPerMatch { get; set; }
        
        // Distribution analysis
        public Dictionary<Role, int> VotesByPosition { get; set; } = new();
        public Dictionary<TeamId, int> VotesByTeam { get; set; } = new();
        public Dictionary<int, int> VoteDistribution { get; set; } = new(); // [votes] -> [count of players]
        
        // Trends
        public float CompetitivenessIndex { get; set; } // How spread out the votes are
        public BrownlowTrend SeasonTrend { get; set; }
    }
    
    /// <summary>
    /// Detailed player analysis for Brownlow performance
    /// </summary>
    public class BrownlowPlayerAnalysis
    {
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Season { get; set; }
        
        // Season summary
        public BrownlowSeasonSummary SeasonSummary { get; set; }
        
        // Performance analysis
        public BrownlowPlayerPerformanceMetrics PerformanceMetrics { get; set; }
        
        // Historical data
        public List<BrownlowMatchHistory> MatchHistory { get; set; } = new();
        
        // Voting analysis
        public BrownlowVotingPatterns VotingPatterns { get; set; }
        
        // Comparative rankings
        public int LeagueRanking { get; set; }
        public int PositionRanking { get; set; }
        
        // Projections
        public BrownlowSeasonProjection SeasonProjection { get; set; }
        public float MedalChances { get; set; } // 0-1 probability
        
        // Insights
        public List<string> KeyInsights { get; set; } = new();
        public List<string> ImprovementAreas { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
    }
    
    /// <summary>
    /// Season summary for a player
    /// </summary>
    public class BrownlowSeasonSummary
    {
        public int TotalVotes { get; set; }
        public int GamesPlayed { get; set; }
        public float VotesPerGame { get; set; }
        
        public int ThreeVoteGames { get; set; }
        public int TwoVoteGames { get; set; }
        public int OneVoteGames { get; set; }
        public int ZeroVoteGames { get; set; }
        
        public float VotingRate => GamesPlayed > 0 ? (float)(ThreeVoteGames + TwoVoteGames + OneVoteGames) / GamesPlayed : 0f;
        public float AverageVotesWhenVoting => (ThreeVoteGames + TwoVoteGames + OneVoteGames) > 0 
            ? (float)(ThreeVoteGames * 3 + TwoVoteGames * 2 + OneVoteGames * 1) / (ThreeVoteGames + TwoVoteGames + OneVoteGames) : 0f;
    }
    
    /// <summary>
    /// Performance metrics specific to Brownlow analysis
    /// </summary>
    public class BrownlowPlayerPerformanceMetrics
    {
        // Core performance
        public float OverallPerformanceRating { get; set; }
        public float ConsistencyIndex { get; set; }
        public float ImpactFactor { get; set; }
        public float ClutchRating { get; set; }
        
        // Voting-specific metrics
        public float VotingEfficiency { get; set; } // How often strong performances translate to votes
        public float PeakPerformanceFrequency { get; set; } // How often they reach peak performance
        public float BigMatchPerformance { get; set; } // Performance in important matches
        
        // Comparative metrics
        public float VsPositionAverage { get; set; }
        public float VsTeamAverage { get; set; }
        public float VsLeagueAverage { get; set; }
        
        // Situational performance
        public float HomeGroundPerformance { get; set; }
        public float AwayGroundPerformance { get; set; }
        public float CloseMatchPerformance { get; set; }
        public float FinalQuarterPerformance { get; set; }
    }
    
    /// <summary>
    /// Individual match history record
    /// </summary>
    public class BrownlowMatchHistory
    {
        public DateTime MatchDate { get; set; }
        public TeamId OppositionTeam { get; set; }
        public string Venue { get; set; }
        public bool IsHomeMatch { get; set; }
        
        public int VotesReceived { get; set; }
        public float MatchScore { get; set; }
        public int MatchRank { get; set; }
        
        public string Result { get; set; } // "Win", "Loss", "Draw"
        public int TeamScore { get; set; }
        public int OppositionScore { get; set; }
        
        public List<string> KeyPerformances { get; set; } = new();
        public Dictionary<string, float> DetailedStats { get; set; } = new();
    }
    
    /// <summary>
    /// Voting patterns analysis
    /// </summary>
    public class BrownlowVotingPatterns
    {
        // Voting frequency patterns
        public float HomeVsAwayVotingRate { get; set; }
        public Dictionary<Role, float> VsPositionVotingRates { get; set; } = new();
        public Dictionary<TeamId, float> VsTeamVotingRates { get; set; } = new();
        public Dictionary<string, float> VsVenueVotingRates { get; set; } = new();
        
        // Performance patterns
        public float WinLossVotingDifference { get; set; }
        public float CloseMatchVotingBonus { get; set; }
        public float FinalQuarterVotingBonus { get; set; }
        
        // Temporal patterns
        public Dictionary<int, float> QuarterlyVotingRates { get; set; } = new(); // Season quarters
        public Dictionary<int, float> MonthlyVotingRates { get; set; } = new();
        public BrownlowTrend SeasonTrend { get; set; }
    }
    
    /// <summary>
    /// Season outcome projections
    /// </summary>
    public class BrownlowSeasonProjection
    {
        public float ProjectedTotalVotes { get; set; }
        public float ProjectedFinalRanking { get; set; }
        public float MedalChances { get; set; }
        public float Top3Chances { get; set; }
        public float Top10Chances { get; set; }
        
        // Scenario analysis
        public Dictionary<string, float> ScenarioOutcomes { get; set; } = new();
        public List<BrownlowProjectionScenario> DetailedScenarios { get; set; } = new();
        
        // Required performance for goals
        public float VotesNeededForMedal { get; set; }
        public float VotesNeededForTop3 { get; set; }
        public float PerformanceLevelRequired { get; set; }
    }
    
    /// <summary>
    /// Projection scenario details
    /// </summary>
    public class BrownlowProjectionScenario
    {
        public string ScenarioName { get; set; }
        public float Probability { get; set; }
        public float ProjectedVotes { get; set; }
        public int ProjectedRank { get; set; }
        public string Description { get; set; }
        public List<string> KeyAssumptions { get; set; } = new();
    }
    
    /// <summary>
    /// Complete Brownlow Medal ceremony results
    /// </summary>
    public class BrownlowMedalResults
    {
        public int Season { get; set; }
        public DateTime CeremonyDate { get; set; }
        
        // Winners
        public BrownlowPlayerStanding Winner { get; set; }
        public BrownlowPlayerStanding RunnerUp { get; set; }
        public BrownlowPlayerStanding ThirdPlace { get; set; }
        
        // Statistics
        public int TotalVotes { get; set; }
        public int WinningMargin { get; set; }
        public List<BrownlowTiedPosition> TiedPositions { get; set; } = new();
        
        // Records and achievements
        public List<BrownlowSeasonRecord> SeasonRecords { get; set; } = new();
        public List<BrownlowAchievement> NotableAchievements { get; set; } = new();
        
        // Analysis
        public BrownlowSeasonVotingAnalysis VotingAnalysis { get; set; }
        public Dictionary<TeamId, BrownlowTeamSummary> TeamBreakdown { get; set; } = new();
        public Dictionary<Role, BrownlowPositionSummary> PositionBreakdown { get; set; } = new();
        
        // Historical context
        public BrownlowHistoricalComparison HistoricalComparisons { get; set; }
        
        // Ceremony details
        public List<string> CeremonyHighlights { get; set; } = new();
        public Dictionary<string, object> CeremonyStats { get; set; } = new();
    }
    
    /// <summary>
    /// Information about tied positions
    /// </summary>
    public class BrownlowTiedPosition
    {
        public int Position { get; set; }
        public int Votes { get; set; }
        public List<BrownlowPlayerStanding> TiedPlayers { get; set; } = new();
        public string TieBreaker { get; set; }
        public string Resolution { get; set; }
    }
    
    /// <summary>
    /// Season records broken or achieved
    /// </summary>
    public class BrownlowSeasonRecord
    {
        public string RecordType { get; set; }
        public string RecordDescription { get; set; }
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public object RecordValue { get; set; }
        public object PreviousRecord { get; set; }
        public bool IsNewRecord { get; set; }
        public string Significance { get; set; }
    }
    
    /// <summary>
    /// Notable achievements during the season
    /// </summary>
    public class BrownlowAchievement
    {
        public string AchievementType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public DateTime DateAchieved { get; set; }
        public string Significance { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }
    
    /// <summary>
    /// Season-wide voting analysis
    /// </summary>
    public class BrownlowSeasonVotingAnalysis
    {
        public float AverageWinningVotes { get; set; }
        public float VoteSpreadIndex { get; set; } // How spread out votes were
        public float CompetitivenessRating { get; set; }
        
        // Voting patterns
        public Dictionary<string, float> VotingTrends { get; set; } = new();
        public List<string> KeyObservations { get; set; } = new();
        
        // Quality indicators
        public float PredictabilityIndex { get; set; }
        public float ConsistencyIndex { get; set; }
        public float SurpriseIndex { get; set; }
        
        // Comparative analysis
        public BrownlowSeasonComparison YearOnYearComparison { get; set; }
    }
    
    /// <summary>
    /// Team performance in Brownlow voting
    /// </summary>
    public class BrownlowTeamSummary
    {
        public TeamId TeamId { get; set; }
        public string TeamName { get; set; }
        
        public int TotalVotes { get; set; }
        public int PlayersWithVotes { get; set; }
        public int ThreeVoteGames { get; set; }
        public int TwoVoteGames { get; set; }
        public int OneVoteGames { get; set; }
        
        public float AverageVotesPerPlayer { get; set; }
        public float VotingRate { get; set; } // Percentage of eligible games that resulted in votes
        
        public BrownlowPlayerStanding TopPerformer { get; set; }
        public List<string> TeamInsights { get; set; } = new();
    }
    
    /// <summary>
    /// Position-based voting analysis
    /// </summary>
    public class BrownlowPositionSummary
    {
        public Role Position { get; set; }
        public string PositionName { get; set; }
        
        public int TotalVotes { get; set; }
        public int TotalPlayers { get; set; }
        public float AverageVotesPerPlayer { get; set; }
        
        public BrownlowPlayerStanding TopPerformer { get; set; }
        public List<BrownlowPlayerStanding> TopPerformers { get; set; } = new();
        
        public float PositionVotingRate { get; set; }
        public float HistoricalComparison { get; set; }
        
        public List<string> PositionInsights { get; set; } = new();
    }
    
    /// <summary>
    /// Historical comparisons and context
    /// </summary>
    public class BrownlowHistoricalComparison
    {
        public List<BrownlowSeasonComparison> RecentSeasons { get; set; } = new();
        public List<BrownlowHistoricalRecord> AllTimeRecords { get; set; } = new();
        public List<string> HistoricalContext { get; set; } = new();
        
        // Notable comparisons
        public bool IsWinningVotesTotalUnusual { get; set; }
        public bool IsWinnerPositionUnusual { get; set; }
        public bool IsWinnerAgeUnusual { get; set; }
        
        public float HistoricalRankingOfWinner { get; set; } // Percentile among all winners
    }
    
    /// <summary>
    /// Comparison with previous seasons
    /// </summary>
    public class BrownlowSeasonComparison
    {
        public int Season { get; set; }
        public int WinningVotes { get; set; }
        public string WinnerPosition { get; set; }
        public string WinnerTeam { get; set; }
        public float CompetitivenessIndex { get; set; }
        public List<string> NotableFeatures { get; set; } = new();
    }
    
    /// <summary>
    /// Historical records in Brownlow Medal history
    /// </summary>
    public class BrownlowHistoricalRecord
    {
        public string RecordType { get; set; }
        public string RecordHolder { get; set; }
        public object RecordValue { get; set; }
        public int SeasonSet { get; set; }
        public bool IsCurrentRecord { get; set; }
        public string Context { get; set; }
    }
    
    /// <summary>
    /// Notable milestones reached during the season
    /// </summary>
    public class BrownlowMilestone
    {
        public PlayerId PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string MilestoneType { get; set; }
        public string Description { get; set; }
        public DateTime DateReached { get; set; }
        public string Significance { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }
    
    /// <summary>
    /// Trends in Brownlow voting
    /// </summary>
    public enum BrownlowTrend
    {
        Improving,      // Performance/votes trending upward
        Stable,         // Consistent performance/votes
        Declining,      // Performance/votes trending downward
        Volatile,       // Inconsistent, up and down
        Emerging,       // New player gaining momentum
        Fading          // Previously strong performer declining
    }
    
    /// <summary>
    /// Exception for Brownlow voting system errors
    /// </summary>
    public class BrownlowVotingException : Exception
    {
        public BrownlowVotingException(string message) : base(message) { }
        public BrownlowVotingException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Utility class for Brownlow calculations and helper functions
    /// </summary>
    public static class BrownlowUtilities
    {
        /// <summary>
        /// Calculate votes needed for various finishing positions
        /// </summary>
        public static Dictionary<string, int> CalculateVotesNeeded(List<BrownlowPlayerStanding> currentStandings, 
            int remainingMatches)
        {
            var result = new Dictionary<string, int>();
            
            if (currentStandings.Count == 0) return result;
            
            var leader = currentStandings.First();
            var maxPossibleVotes = remainingMatches * 3; // Best possible case
            
            result["Medal"] = Math.Max(0, leader.TotalVotes + 1);
            result["Top3"] = currentStandings.Count >= 3 ? Math.Max(0, currentStandings[2].TotalVotes + 1) : 1;
            result["Top10"] = currentStandings.Count >= 10 ? Math.Max(0, currentStandings[9].TotalVotes + 1) : 1;
            
            return result;
        }
        
        /// <summary>
        /// Calculate medal chances based on current position and remaining matches
        /// </summary>
        public static float CalculateMedalChances(BrownlowPlayerStanding player, List<BrownlowPlayerStanding> allStandings, 
            int remainingMatches)
        {
            if (allStandings.Count == 0) return 0f;
            
            var leader = allStandings.First();
            var votesGap = leader.TotalVotes - player.TotalVotes;
            var maxPossibleVotes = remainingMatches * 3;
            
            if (votesGap > maxPossibleVotes) return 0f; // Mathematically impossible
            if (player.Rank == 1) return 0.9f; // Current leader has high chance
            
            // Simple probability calculation - would be more sophisticated in practice
            var chanceReduction = Math.Min(1f, votesGap / (float)maxPossibleVotes);
            return Math.Max(0.05f, (1f - chanceReduction) * 0.8f);
        }
        
        /// <summary>
        /// Determine if votes should be tied or split
        /// </summary>
        public static Dictionary<PlayerId, int> HandleVoteTies(Dictionary<PlayerId, float> scores, float tieThreshold = 0.01f)
        {
            var allocations = new Dictionary<PlayerId, int>();
            var sortedScores = scores.OrderByDescending(kvp => kvp.Value).ToList();
            
            if (sortedScores.Count == 0) return allocations;
            
            // Implementation of tie-handling logic as shown in main class
            // This would be extracted from the main voting method
            
            return allocations;
        }
        
        /// <summary>
        /// Generate insights for a player's performance
        /// </summary>
        public static List<string> GeneratePlayerInsights(BrownlowSeasonTracker tracker, 
            List<BrownlowMatchHistory> matchHistory)
        {
            var insights = new List<string>();
            
            if (tracker.TotalVotes == 0)
            {
                insights.Add("Yet to receive Brownlow votes this season");
            }
            else
            {
                if (tracker.ThreeVoteGames > 0)
                    insights.Add($"Has been best on ground {tracker.ThreeVoteGames} times");
                
                var votingRate = (float)(tracker.ThreeVoteGames + tracker.TwoVoteGames + tracker.OneVoteGames) / tracker.GamesPlayed;
                if (votingRate > 0.5f)
                    insights.Add($"Polling in {votingRate:P1} of games - excellent consistency");
                else if (votingRate > 0.3f)
                    insights.Add($"Polling in {votingRate:P1} of games - good consistency");
            }
            
            return insights;
        }
    }
}