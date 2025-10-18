using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.Aggregates;

namespace AFLCoachSim.Core.Engine.Match
{
    /// <summary>
    /// Advanced statistics tracking system for comprehensive match analytics
    /// Tracks detailed performance metrics, advanced statistics, and real-time analytics
    /// </summary>
    public class AdvancedStatisticsSystem
    {
        private readonly Dictionary<string, PlayerStatistics> _playerStats;
        private readonly Dictionary<int, TeamStatistics> _teamStats;
        private readonly List<StatisticalEvent> _eventLog;
        private readonly Dictionary<string, List<PerformanceSnapshot>> _performanceHistory;
        private readonly AdvancedMetricsCalculator _metricsCalculator;
        
        public AdvancedStatisticsSystem()
        {
            _playerStats = new Dictionary<string, PlayerStatistics>();
            _teamStats = new Dictionary<int, TeamStatistics>();
            _eventLog = new List<StatisticalEvent>();
            _performanceHistory = new Dictionary<string, List<PerformanceSnapshot>>();
            _metricsCalculator = new AdvancedMetricsCalculator();
        }
        
        /// <summary>
        /// Initialize statistics tracking for a match
        /// </summary>
        public void InitializeMatch(List<Player> allPlayers, List<Team> teams)
        {
            // Initialize player statistics
            foreach (var player in allPlayers)
            {
                _playerStats[player.Id] = new PlayerStatistics
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    Position = player.Position.ToString(),
                    TeamId = player.TeamId,
                    MatchStartTime = DateTime.Now,
                    TimeOnGround = TimeSpan.Zero
                };
                
                _performanceHistory[player.Id] = new List<PerformanceSnapshot>();
            }
            
            // Initialize team statistics
            foreach (var team in teams)
            {
                _teamStats[team.Id] = new TeamStatistics
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    MatchStartTime = DateTime.Now
                };
            }
        }
        
        /// <summary>
        /// Record a disposal event with comprehensive tracking
        /// </summary>
        public void RecordDisposal(string playerId, DisposalType type, DisposalOutcome outcome, 
            float accuracy, float pressure, bool contested, string targetPlayerId = null)
        {
            var playerStats = _playerStats[playerId];
            var teamStats = _teamStats[playerStats.TeamId];
            
            // Basic disposal tracking
            playerStats.TotalDisposals++;
            teamStats.TotalDisposals++;
            
            switch (type)
            {
                case DisposalType.Kick:
                    playerStats.Kicks++;
                    teamStats.Kicks++;
                    break;
                case DisposalType.Handball:
                    playerStats.Handballs++;
                    teamStats.Handballs++;
                    break;
            }
            
            // Outcome tracking
            if (outcome == DisposalOutcome.Effective)
            {
                playerStats.EffectiveDisposals++;
                teamStats.EffectiveDisposals++;
            }
            else if (outcome == DisposalOutcome.Turnover)
            {
                playerStats.Turnovers++;
                teamStats.Turnovers++;
            }
            
            // Advanced metrics
            if (contested)
            {
                playerStats.ContestedDisposals++;
                teamStats.ContestedDisposals++;
            }
            else
            {
                playerStats.UncontestedDisposals++;
                teamStats.UncontestedDisposals++;
            }
            
            // Pressure and accuracy tracking
            playerStats.AveragePressure = UpdateRunningAverage(playerStats.AveragePressure, 
                pressure, playerStats.TotalDisposals);
            playerStats.DisposalAccuracy = UpdateRunningAverage(playerStats.DisposalAccuracy, 
                accuracy, playerStats.TotalDisposals);
            
            // Record event
            RecordEvent(new StatisticalEvent
            {
                PlayerId = playerId,
                EventType = StatEventType.Disposal,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["type"] = type,
                    ["outcome"] = outcome,
                    ["accuracy"] = accuracy,
                    ["pressure"] = pressure,
                    ["contested"] = contested,
                    ["targetPlayerId"] = targetPlayerId
                }
            });
        }
        
        /// <summary>
        /// Record a contested possession
        /// </summary>
        public void RecordContestedPossession(string playerId, bool won, ContestedType type, 
            int playersInvolved, float intensity)
        {
            var playerStats = _playerStats[playerId];
            var teamStats = _teamStats[playerStats.TeamId];
            
            playerStats.ContestedPossessions++;
            teamStats.ContestedPossessions++;
            
            if (won)
            {
                playerStats.ContestedPossessionsWon++;
                teamStats.ContestedPossessionsWon++;
            }
            
            // Type-specific tracking
            switch (type)
            {
                case ContestedType.Ruck:
                    if (won) playerStats.HitOuts++;
                    break;
                case ContestedType.GroundBall:
                    playerStats.GroundBallGets++;
                    break;
                case ContestedType.Marking:
                    if (won) playerStats.ContestedMarks++;
                    break;
            }
            
            // Advanced metrics
            playerStats.ContestedPossessionIntensity = UpdateRunningAverage(
                playerStats.ContestedPossessionIntensity, intensity, playerStats.ContestedPossessions);
            
            RecordEvent(new StatisticalEvent
            {
                PlayerId = playerId,
                EventType = StatEventType.ContestedPossession,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["won"] = won,
                    ["type"] = type,
                    ["playersInvolved"] = playersInvolved,
                    ["intensity"] = intensity
                }
            });
        }
        
        /// <summary>
        /// Record a pressure act (tackle, chase, defensive action)
        /// </summary>
        public void RecordPressureAct(string playerId, PressureActType type, bool successful, 
            float intensity, string targetPlayerId = null)
        {
            var playerStats = _playerStats[playerId];
            var teamStats = _teamStats[playerStats.TeamId];
            
            playerStats.PressureActs++;
            teamStats.PressureActs++;
            
            switch (type)
            {
                case PressureActType.Tackle:
                    playerStats.Tackles++;
                    if (successful) playerStats.EffectiveTackles++;
                    break;
                case PressureActType.Chase:
                    playerStats.ChaseDownTackles++;
                    break;
                case PressureActType.Spoil:
                    playerStats.Spoils++;
                    break;
                case PressureActType.Smother:
                    playerStats.Smothers++;
                    break;
            }
            
            playerStats.PressureIntensity = UpdateRunningAverage(
                playerStats.PressureIntensity, intensity, playerStats.PressureActs);
            
            RecordEvent(new StatisticalEvent
            {
                PlayerId = playerId,
                EventType = StatEventType.PressureAct,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["type"] = type,
                    ["successful"] = successful,
                    ["intensity"] = intensity,
                    ["targetPlayerId"] = targetPlayerId
                }
            });
        }
        
        /// <summary>
        /// Record marking contest
        /// </summary>
        public void RecordMarkingContest(string playerId, bool marked, MarkType type, 
            float contestHeight, int contestersCount, bool intercept = false)
        {
            var playerStats = _playerStats[playerId];
            var teamStats = _teamStats[playerStats.TeamId];
            
            playerStats.MarkingContests++;
            
            if (marked)
            {
                playerStats.Marks++;
                teamStats.Marks++;
                
                switch (type)
                {
                    case MarkType.Contested:
                        playerStats.ContestedMarks++;
                        break;
                    case MarkType.Uncontested:
                        playerStats.UncontestedMarks++;
                        break;
                    case MarkType.Intercept:
                        playerStats.InterceptMarks++;
                        break;
                }
                
                if (intercept)
                {
                    playerStats.Intercepts++;
                    teamStats.Intercepts++;
                }
            }
            
            // Advanced metrics
            playerStats.AverageMarkingHeight = UpdateRunningAverage(
                playerStats.AverageMarkingHeight, contestHeight, playerStats.MarkingContests);
            
            RecordEvent(new StatisticalEvent
            {
                PlayerId = playerId,
                EventType = StatEventType.MarkingContest,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["marked"] = marked,
                    ["type"] = type,
                    ["height"] = contestHeight,
                    ["contestersCount"] = contestersCount,
                    ["intercept"] = intercept
                }
            });
        }
        
        /// <summary>
        /// Record a goal or behind
        /// </summary>
        public void RecordScore(string playerId, ScoreType scoreType, float distance, 
            float angle, bool underPressure, string assistPlayerId = null)
        {
            var playerStats = _playerStats[playerId];
            var teamStats = _teamStats[playerStats.TeamId];
            
            switch (scoreType)
            {
                case ScoreType.Goal:
                    playerStats.Goals++;
                    teamStats.Goals++;
                    break;
                case ScoreType.Behind:
                    playerStats.Behinds++;
                    teamStats.Behinds++;
                    break;
            }
            
            // Shot accuracy and pressure tracking
            playerStats.Shots++;
            if (underPressure) playerStats.ShotsUnderPressure++;
            
            playerStats.AverageShotDistance = UpdateRunningAverage(
                playerStats.AverageShotDistance, distance, playerStats.Shots);
            
            // Goal assists
            if (assistPlayerId != null && scoreType == ScoreType.Goal)
            {
                _playerStats[assistPlayerId].GoalAssists++;
            }
            
            RecordEvent(new StatisticalEvent
            {
                PlayerId = playerId,
                EventType = StatEventType.Score,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["scoreType"] = scoreType,
                    ["distance"] = distance,
                    ["angle"] = angle,
                    ["underPressure"] = underPressure,
                    ["assistPlayerId"] = assistPlayerId
                }
            });
        }
        
        /// <summary>
        /// Update player time on ground and record performance snapshot
        /// </summary>
        public void UpdatePlayerTimeOnGround(string playerId, TimeSpan additionalTime, 
            float currentRating, float fatigue, float performance)
        {
            var playerStats = _playerStats[playerId];
            playerStats.TimeOnGround += additionalTime;
            
            // Record performance snapshot every 5 minutes
            var snapshots = _performanceHistory[playerId];
            if (snapshots.Count == 0 || 
                (DateTime.Now - snapshots.Last().Timestamp).TotalMinutes >= 5)
            {
                snapshots.Add(new PerformanceSnapshot
                {
                    Timestamp = DateTime.Now,
                    TimeOnGround = playerStats.TimeOnGround,
                    CurrentRating = currentRating,
                    FatigueLevel = fatigue,
                    PerformanceIndex = performance,
                    CumulativeImpact = CalculateCumulativeImpact(playerId)
                });
            }
        }
        
        /// <summary>
        /// Calculate advanced metrics for a player
        /// </summary>
        public AdvancedPlayerMetrics CalculateAdvancedMetrics(string playerId)
        {
            var playerStats = _playerStats[playerId];
            return _metricsCalculator.CalculatePlayerMetrics(playerStats, _performanceHistory[playerId]);
        }
        
        /// <summary>
        /// Calculate advanced metrics for a team
        /// </summary>
        public AdvancedTeamMetrics CalculateTeamMetrics(int teamId)
        {
            var teamStats = _teamStats[teamId];
            var teamPlayers = _playerStats.Values.Where(p => p.TeamId == teamId).ToList();
            return _metricsCalculator.CalculateTeamMetrics(teamStats, teamPlayers);
        }
        
        /// <summary>
        /// Get comprehensive match analytics
        /// </summary>
        public MatchAnalytics GetMatchAnalytics()
        {
            var analytics = new MatchAnalytics
            {
                MatchDuration = DateTime.Now - _teamStats.Values.First().MatchStartTime,
                TotalEvents = _eventLog.Count,
                PlayerMetrics = new Dictionary<string, AdvancedPlayerMetrics>(),
                TeamMetrics = new Dictionary<int, AdvancedTeamMetrics>(),
                KeyMoments = IdentifyKeyMoments(),
                PerformanceTrends = AnalyzePerformanceTrends(),
                StatisticalSummary = GenerateStatisticalSummary()
            };
            
            // Calculate metrics for all players
            foreach (var playerId in _playerStats.Keys)
            {
                analytics.PlayerMetrics[playerId] = CalculateAdvancedMetrics(playerId);
            }
            
            // Calculate metrics for all teams
            foreach (var teamId in _teamStats.Keys)
            {
                analytics.TeamMetrics[teamId] = CalculateTeamMetrics(teamId);
            }
            
            return analytics;
        }
        
        /// <summary>
        /// Get real-time performance ratings
        /// </summary>
        public Dictionary<string, float> GetRealTimeRatings()
        {
            var ratings = new Dictionary<string, float>();
            
            foreach (var playerId in _playerStats.Keys)
            {
                var metrics = CalculateAdvancedMetrics(playerId);
                ratings[playerId] = metrics.OverallPerformanceRating;
            }
            
            return ratings;
        }
        
        // Private helper methods
        private void RecordEvent(StatisticalEvent eventData)
        {
            _eventLog.Add(eventData);
        }
        
        private float UpdateRunningAverage(float currentAverage, float newValue, int count)
        {
            if (count == 1) return newValue;
            return ((currentAverage * (count - 1)) + newValue) / count;
        }
        
        private float CalculateCumulativeImpact(string playerId)
        {
            var playerStats = _playerStats[playerId];
            
            // Weighted impact calculation
            float offensiveImpact = (playerStats.EffectiveDisposals * 1.2f + 
                                   playerStats.Goals * 6f + 
                                   playerStats.GoalAssists * 3f + 
                                   playerStats.ContestedMarks * 2f) / (float)Math.Max(1, playerStats.TimeOnGround.TotalMinutes);
            
            float defensiveImpact = (playerStats.EffectiveTackles * 2f + 
                                   playerStats.Intercepts * 3f + 
                                   playerStats.Spoils * 1.5f + 
                                   playerStats.PressureActs * 1f) / (float)Math.Max(1, playerStats.TimeOnGround.TotalMinutes);
            
            return (offensiveImpact + defensiveImpact) * 10f; // Scale to 0-100
        }
        
        private List<KeyMoment> IdentifyKeyMoments()
        {
            var keyMoments = new List<KeyMoment>();
            
            // Find momentum-shifting events
            var goals = _eventLog.Where(e => e.EventType == StatEventType.Score && 
                                           e.Details.ContainsKey("scoreType") && 
                                           (ScoreType)e.Details["scoreType"] == ScoreType.Goal)
                                .OrderBy(e => e.Timestamp);
            
            foreach (var goal in goals)
            {
                keyMoments.Add(new KeyMoment
                {
                    Timestamp = goal.Timestamp,
                    Type = KeyMomentType.Goal,
                    PlayerId = goal.PlayerId,
                    Description = $"Goal scored by {_playerStats[goal.PlayerId].PlayerName}",
                    ImpactLevel = 8.5f
                });
            }
            
            // Add other key moments (contested marks, big tackles, etc.)
            // Implementation would continue with other significant events
            
            return keyMoments.OrderByDescending(km => km.ImpactLevel).Take(10).ToList();
        }
        
        private Dictionary<string, PerformanceTrend> AnalyzePerformanceTrends()
        {
            var trends = new Dictionary<string, PerformanceTrend>();
            
            foreach (var playerId in _performanceHistory.Keys)
            {
                var history = _performanceHistory[playerId];
                if (history.Count >= 3)
                {
                    trends[playerId] = new PerformanceTrend
                    {
                        PlayerId = playerId,
                        TrendDirection = CalculateTrendDirection(history),
                        PerformanceVariability = CalculateVariability(history),
                        PeakPerformance = history.Max(h => h.PerformanceIndex),
                        AveragePerformance = history.Average(h => h.PerformanceIndex)
                    };
                }
            }
            
            return trends;
        }
        
        private TrendDirection CalculateTrendDirection(List<PerformanceSnapshot> history)
        {
            if (history.Count < 2) return TrendDirection.Stable;
            
            var recent = history.TakeLast(3).Average(h => h.PerformanceIndex);
            var earlier = history.Take(history.Count - 3).Average(h => h.PerformanceIndex);
            
            float difference = recent - earlier;
            
            if (difference > 0.1f) return TrendDirection.Improving;
            if (difference < -0.1f) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }
        
        private float CalculateVariability(List<PerformanceSnapshot> history)
        {
            var values = history.Select(h => h.PerformanceIndex).ToArray();
            var mean = values.Average();
            var squaredDifferences = values.Select(x => Math.Pow(x - mean, 2));
            return (float)Math.Sqrt(squaredDifferences.Average());
        }
        
        /// <summary>
        /// Get statistics for a specific player
        /// </summary>
        public PlayerStatistics GetPlayerStatistics(string playerId)
        {
            return _playerStats.GetValueOrDefault(playerId);
        }
        
        /// <summary>
        /// Get all player statistics for the current match
        /// </summary>
        public Dictionary<string, PlayerStatistics> GetAllPlayerStatistics()
        {
            return new Dictionary<string, PlayerStatistics>(_playerStats);
        }
        
        /// <summary>
        /// Get team statistics for a specific team
        /// </summary>
        public TeamStatistics GetTeamStatistics(int teamId)
        {
            return _teamStats.GetValueOrDefault(teamId);
        }
        
        private StatisticalSummary GenerateStatisticalSummary()
        {
            return new StatisticalSummary
            {
                TotalDisposals = _playerStats.Values.Sum(p => p.TotalDisposals),
                TotalGoals = _playerStats.Values.Sum(p => p.Goals),
                TotalMarks = _playerStats.Values.Sum(p => p.Marks),
                TotalTackles = _playerStats.Values.Sum(p => p.Tackles),
                TotalContestedPossessions = _playerStats.Values.Sum(p => p.ContestedPossessions),
                AverageDisposalEfficiency = _playerStats.Values.Average(p => 
                    p.TotalDisposals > 0 ? (float)p.EffectiveDisposals / p.TotalDisposals : 0),
                AverageContestedPossessionRate = _playerStats.Values.Average(p => 
                    p.TotalDisposals > 0 ? (float)p.ContestedDisposals / p.TotalDisposals : 0)
            };
        }
    }
}
