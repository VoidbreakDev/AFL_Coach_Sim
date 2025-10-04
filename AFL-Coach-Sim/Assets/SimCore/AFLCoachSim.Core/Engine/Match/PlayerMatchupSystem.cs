using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Models;

namespace AFLCoachSim.Core.Engine.Match
{
    /// <summary>
    /// Player matchup system for analyzing and simulating specific player vs player interactions
    /// Tracks individual matchups, their historical performance, and dynamic effects on gameplay
    /// </summary>
    public class PlayerMatchupSystem
    {
        private readonly Dictionary<string, PlayerMatchup> _activeMatchups;
        private readonly Dictionary<string, List<MatchupHistory>> _matchupHistory;
        private readonly MatchupAnalyzer _matchupAnalyzer;
        private readonly PerformanceModifier _performanceModifier;
        private readonly List<MatchupEvent> _matchupEvents;
        
        // Matchup configuration
        private readonly float _matchupInfluenceStrength = 0.3f; // How much matchups affect performance
        private readonly int _historyTracking = 50; // Number of previous encounters to track
        
        public PlayerMatchupSystem()
        {
            _activeMatchups = new Dictionary<string, PlayerMatchup>();
            _matchupHistory = new Dictionary<string, List<MatchupHistory>>();
            _matchupAnalyzer = new MatchupAnalyzer();
            _performanceModifier = new PerformanceModifier();
            _matchupEvents = new List<MatchupEvent>();
        }
        
        /// <summary>
        /// Initialize matchup system for a match
        /// </summary>
        public void InitializeMatchups(List<Player> team1Players, List<Player> team2Players)
        {
            _activeMatchups.Clear();
            _matchupEvents.Clear();
            
            // Create key positional matchups
            CreatePositionalMatchups(team1Players, team2Players);
            
            // Create dynamic situational matchups
            CreateSituationalMatchups(team1Players, team2Players);
            
            // Apply historical matchup data
            ApplyHistoricalContext();
        }
        
        /// <summary>
        /// Update matchups during gameplay
        /// </summary>
        public void UpdateMatchups(MatchContext context)
        {
            // Update existing matchup performance
            foreach (var matchup in _activeMatchups.Values)
            {
                UpdateMatchupPerformance(matchup, context);
            }
            
            // Create new situational matchups based on current play
            CreateDynamicMatchups(context);
            
            // Apply performance modifiers based on matchups
            ApplyMatchupEffects(context);
            
            // Clean up expired matchups
            CleanupExpiredMatchups();
        }
        
        /// <summary>
        /// Process a specific contest between two players
        /// </summary>
        public MatchupResult ProcessPlayerContest(string player1Id, string player2Id, 
            ContestType contestType, MatchContext context)
        {
            var matchupKey = GetMatchupKey(player1Id, player2Id);
            var matchup = GetOrCreateMatchup(player1Id, player2Id, MatchupType.Contest, context);
            
            // Analyze the contest based on player attributes and matchup history
            var result = _matchupAnalyzer.AnalyzeContest(matchup, contestType, context);
            
            // Update matchup statistics
            UpdateMatchupStatistics(matchup, result, contestType);
            
            // Record matchup event
            RecordMatchupEvent(new MatchupEvent
            {
                MatchupId = matchup.Id,
                EventType = MatchupEventType.Contest,
                Timestamp = DateTime.Now,
                ContestType = contestType,
                Winner = result.WinnerId,
                Confidence = result.Confidence,
                Context = new Dictionary<string, object>
                {
                    ["location"] = context.CurrentLocation,
                    ["pressure"] = context.CurrentPressure,
                    ["fatigue"] = GetAverageFatigue(player1Id, player2Id, context)
                }
            });
            
            return result;
        }
        
        /// <summary>
        /// Get performance modifier for a player based on current matchups
        /// </summary>
        public PlayerPerformanceModifier GetPerformanceModifier(string playerId, MatchContext context)
        {
            var modifier = new PlayerPerformanceModifier(playerId);
            
            // Find all active matchups involving this player
            var playerMatchups = _activeMatchups.Values
                .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
                .ToList();
            
            foreach (var matchup in playerMatchups)
            {
                var opponentId = matchup.Player1Id == playerId ? matchup.Player2Id : matchup.Player1Id;
                var matchupModifier = CalculateMatchupModifier(playerId, opponentId, matchup, context);
                
                modifier.ApplyModifier(matchupModifier);
            }
            
            return modifier;
        }
        
        /// <summary>
        /// Predict contest outcome between two players
        /// </summary>
        public ContestPrediction PredictContest(string player1Id, string player2Id, 
            ContestType contestType, MatchContext context)
        {
            var matchup = GetExistingMatchup(player1Id, player2Id);
            return _matchupAnalyzer.PredictContest(player1Id, player2Id, contestType, matchup, context);
        }
        
        /// <summary>
        /// Get detailed matchup analysis between two players
        /// </summary>
        public MatchupAnalysis GetMatchupAnalysis(string player1Id, string player2Id)
        {
            var matchupKey = GetMatchupKey(player1Id, player2Id);
            var currentMatchup = _activeMatchups.GetValueOrDefault(matchupKey);
            var history = _matchupHistory.GetValueOrDefault(matchupKey, new List<MatchupHistory>());
            
            return _matchupAnalyzer.GenerateAnalysis(player1Id, player2Id, currentMatchup, history);
        }
        
        /// <summary>
        /// Get all active matchups for a player
        /// </summary>
        public List<PlayerMatchup> GetPlayerMatchups(string playerId)
        {
            return _activeMatchups.Values
                .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
                .ToList();
        }
        
        /// <summary>
        /// Get matchup events for analysis
        /// </summary>
        public List<MatchupEvent> GetMatchupEvents(TimeSpan? timeWindow = null)
        {
            if (timeWindow.HasValue)
            {
                var cutoff = DateTime.Now - timeWindow.Value;
                return _matchupEvents.Where(e => e.Timestamp >= cutoff).ToList();
            }
            return _matchupEvents.ToList();
        }
        
        // Private methods for matchup management
        private void CreatePositionalMatchups(List<Player> team1Players, List<Player> team2Players)
        {
            // Create key positional matchups (forwards vs defenders, etc.)
            var team1Forwards = team1Players.Where(p => p.Position.Contains("Forward")).ToList();
            var team2Defenders = team2Players.Where(p => p.Position.Contains("Defender")).ToList();
            
            CreateMatchupsForPositions(team1Forwards, team2Defenders, MatchupType.Positional);
            
            var team2Forwards = team2Players.Where(p => p.Position.Contains("Forward")).ToList();
            var team1Defenders = team1Players.Where(p => p.Position.Contains("Defender")).ToList();
            
            CreateMatchupsForPositions(team2Forwards, team1Defenders, MatchupType.Positional);
            
            // Create midfield matchups
            var team1Mids = team1Players.Where(p => p.Position.Contains("Midfielder")).ToList();
            var team2Mids = team2Players.Where(p => p.Position.Contains("Midfielder")).ToList();
            
            CreateMatchupsForPositions(team1Mids, team2Mids, MatchupType.Positional);
        }
        
        private void CreateMatchupsForPositions(List<Player> attackers, List<Player> defenders, 
            MatchupType type)
        {
            foreach (var attacker in attackers.Take(Math.Min(attackers.Count, 3))) // Limit key matchups
            {
                var bestDefender = FindBestMatchupOpponent(attacker, defenders);
                if (bestDefender != null)
                {
                    var matchup = CreateMatchup(attacker.Id, bestDefender.Id, type);
                    _activeMatchups[matchup.MatchupKey] = matchup;
                }
            }
        }
        
        private Player FindBestMatchupOpponent(Player player, List<Player> opponents)
        {
            return opponents
                .OrderByDescending(o => CalculateMatchupQuality(player, o))
                .FirstOrDefault();
        }
        
        private float CalculateMatchupQuality(Player player1, Player player2)
        {
            // Calculate how interesting/competitive this matchup would be
            float quality = 0.5f;
            
            // Similar skill levels make for better matchups
            float skillDifference = Math.Abs(player1.OverallRating - player2.OverallRating);
            quality += Math.Max(0f, (20f - skillDifference) / 20f) * 0.3f;
            
            // Position compatibility
            quality += GetPositionMatchupCompatibility(player1.Position, player2.Position) * 0.2f;
            
            // Playing style contrast
            quality += CalculateStyleContrast(player1, player2) * 0.3f;
            
            // Historical significance (if any)
            quality += GetHistoricalSignificance(player1.Id, player2.Id) * 0.2f;
            
            return quality;
        }
        
        private float GetPositionMatchupCompatibility(string position1, string position2)
        {
            var compatibilityMap = new Dictionary<string, Dictionary<string, float>>
            {
                ["Forward"] = new Dictionary<string, float>
                {
                    ["Defender"] = 1.0f,
                    ["Midfielder"] = 0.6f,
                    ["Forward"] = 0.3f,
                    ["Ruck"] = 0.4f
                },
                ["Defender"] = new Dictionary<string, float>
                {
                    ["Forward"] = 1.0f,
                    ["Midfielder"] = 0.7f,
                    ["Defender"] = 0.2f,
                    ["Ruck"] = 0.3f
                },
                ["Midfielder"] = new Dictionary<string, float>
                {
                    ["Midfielder"] = 1.0f,
                    ["Forward"] = 0.6f,
                    ["Defender"] = 0.7f,
                    ["Ruck"] = 0.8f
                },
                ["Ruck"] = new Dictionary<string, float>
                {
                    ["Ruck"] = 1.0f,
                    ["Midfielder"] = 0.8f,
                    ["Forward"] = 0.4f,
                    ["Defender"] = 0.3f
                }
            };
            
            return compatibilityMap.GetValueOrDefault(position1, new Dictionary<string, float>())
                .GetValueOrDefault(position2, 0.3f);
        }
        
        private float CalculateStyleContrast(Player player1, Player player2)
        {
            // Calculate how contrasting their playing styles are (makes for interesting matchups)
            float contrast = 0f;
            
            contrast += Math.Abs(player1.Aggression - player2.Aggression) * 0.3f;
            contrast += Math.Abs(player1.PlayingStyle - player2.PlayingStyle) * 0.4f;
            contrast += Math.Abs(player1.RiskTaking - player2.RiskTaking) * 0.3f;
            
            return Math.Min(1f, contrast);
        }
        
        private float GetHistoricalSignificance(string player1Id, string player2Id)
        {
            var matchupKey = GetMatchupKey(player1Id, player2Id);
            var history = _matchupHistory.GetValueOrDefault(matchupKey, new List<MatchupHistory>());
            
            if (!history.Any()) return 0f;
            
            // More history = more significance, up to a point
            float significance = Math.Min(1f, history.Count / 20f) * 0.5f;
            
            // Close historical record makes it more significant
            var totalContests = history.Sum(h => h.TotalContests);
            var player1Wins = history.Sum(h => h.Player1Wins);
            
            if (totalContests > 0)
            {
                var winRate = (float)player1Wins / totalContests;
                var balance = 1f - Math.Abs(0.5f - winRate) * 2f; // Closer to 50% = more balanced
                significance += balance * 0.5f;
            }
            
            return significance;
        }
        
        private void CreateSituationalMatchups(List<Player> team1Players, List<Player> team2Players)
        {
            // Create situational matchups for key contests (ruck, ball-winning midfielders, etc.)
            CreateRuckMatchups(team1Players, team2Players);
            CreateBallWinnerMatchups(team1Players, team2Players);
        }
        
        private void CreateRuckMatchups(List<Player> team1Players, List<Player> team2Players)
        {
            var team1Rucks = team1Players.Where(p => p.Position == "Ruck").ToList();
            var team2Rucks = team2Players.Where(p => p.Position == "Ruck").ToList();
            
            foreach (var ruck1 in team1Rucks)
            {
                foreach (var ruck2 in team2Rucks)
                {
                    var matchup = CreateMatchup(ruck1.Id, ruck2.Id, MatchupType.Ruck);
                    _activeMatchups[matchup.MatchupKey] = matchup;
                }
            }
        }
        
        private void CreateBallWinnerMatchups(List<Player> team1Players, List<Player> team2Players)
        {
            var team1BallWinners = team1Players
                .Where(p => p.Position.Contains("Midfielder") && p.ContestedBall > 75)
                .OrderByDescending(p => p.ContestedBall)
                .Take(3);
            
            var team2BallWinners = team2Players
                .Where(p => p.Position.Contains("Midfielder") && p.ContestedBall > 75)
                .OrderByDescending(p => p.ContestedBall)
                .Take(3);
            
            foreach (var player1 in team1BallWinners)
            {
                foreach (var player2 in team2BallWinners)
                {
                    var matchup = CreateMatchup(player1.Id, player2.Id, MatchupType.BallContest);
                    _activeMatchups[matchup.MatchupKey] = matchup;
                }
            }
        }
        
        private void CreateDynamicMatchups(MatchContext context)
        {
            // Create new matchups based on current game situations
            if (context.CurrentPhase == MatchPhase.Inside50)
            {
                CreateInside50Matchups(context);
            }
            else if (context.CurrentPhase == MatchPhase.OpenPlay)
            {
                CreateOpenPlayMatchups(context);
            }
        }
        
        private void CreateInside50Matchups(MatchContext context)
        {
            var attackingPlayers = context.GetPlayersInArea("Forward50");
            var defendingPlayers = context.GetPlayersInArea("Defensive50");
            
            foreach (var attacker in attackingPlayers.Take(2))
            {
                var nearestDefender = defendingPlayers
                    .OrderBy(d => context.GetDistanceBetweenPlayers(attacker.Id, d.Id))
                    .FirstOrDefault();
                
                if (nearestDefender != null)
                {
                    var matchup = GetOrCreateMatchup(attacker.Id, nearestDefender.Id, 
                        MatchupType.OneOnOne, context);
                    matchup.Context = "Inside 50 contest";
                    matchup.Intensity = 0.8f;
                }
            }
        }
        
        private void CreateOpenPlayMatchups(MatchContext context)
        {
            var ballCarrier = context.GetBallCarrier();
            if (ballCarrier != null)
            {
                var nearestOpponent = context.GetNearestOpponent(ballCarrier.Id);
                if (nearestOpponent != null && 
                    context.GetDistanceBetweenPlayers(ballCarrier.Id, nearestOpponent.Id) < 5f)
                {
                    var matchup = GetOrCreateMatchup(ballCarrier.Id, nearestOpponent.Id,
                        MatchupType.PressureContest, context);
                    matchup.Context = "Ball carrier under pressure";
                    matchup.Intensity = 0.9f;
                }
            }
        }
        
        private PlayerMatchup CreateMatchup(string player1Id, string player2Id, MatchupType type)
        {
            return new PlayerMatchup
            {
                Id = Guid.NewGuid().ToString(),
                MatchupKey = GetMatchupKey(player1Id, player2Id),
                Player1Id = player1Id,
                Player2Id = player2Id,
                Type = type,
                CreatedTime = DateTime.Now,
                LastUpdated = DateTime.Now,
                IsActive = true,
                Intensity = DetermineInitialIntensity(type),
                Statistics = new MatchupStatistics()
            };
        }
        
        private PlayerMatchup GetOrCreateMatchup(string player1Id, string player2Id, 
            MatchupType type, MatchContext context)
        {
            var matchupKey = GetMatchupKey(player1Id, player2Id);
            
            if (_activeMatchups.TryGetValue(matchupKey, out var existingMatchup))
            {
                existingMatchup.LastUpdated = DateTime.Now;
                return existingMatchup;
            }
            
            var newMatchup = CreateMatchup(player1Id, player2Id, type);
            _activeMatchups[matchupKey] = newMatchup;
            
            return newMatchup;
        }
        
        private PlayerMatchup GetExistingMatchup(string player1Id, string player2Id)
        {
            var matchupKey = GetMatchupKey(player1Id, player2Id);
            return _activeMatchups.GetValueOrDefault(matchupKey);
        }
        
        private string GetMatchupKey(string player1Id, string player2Id)
        {
            // Ensure consistent key regardless of player order
            var sortedIds = new[] { player1Id, player2Id }.OrderBy(id => id).ToArray();
            return $"{sortedIds[0]}_{sortedIds[1]}";
        }
        
        private float DetermineInitialIntensity(MatchupType type)
        {
            return type switch
            {
                MatchupType.Positional => 0.6f,
                MatchupType.Ruck => 0.8f,
                MatchupType.BallContest => 0.7f,
                MatchupType.OneOnOne => 0.9f,
                MatchupType.PressureContest => 0.95f,
                MatchupType.Contest => 0.7f,
                _ => 0.5f
            };
        }
        
        private void UpdateMatchupPerformance(PlayerMatchup matchup, MatchContext context)
        {
            // Update matchup intensity based on current game state
            UpdateMatchupIntensity(matchup, context);
            
            // Update performance tracking
            var player1 = context.GetPlayer(matchup.Player1Id);
            var player2 = context.GetPlayer(matchup.Player2Id);
            
            if (player1 != null && player2 != null)
            {
                UpdatePlayerPerformanceInMatchup(matchup, player1, player2, context);
            }
        }
        
        private void UpdateMatchupIntensity(PlayerMatchup matchup, MatchContext context)
        {
            var baseIntensity = DetermineInitialIntensity(matchup.Type);
            var situationalModifier = 0f;
            
            // Increase intensity in pressure situations
            if (context.CurrentPressure > 0.8f) situationalModifier += 0.2f;
            
            // Increase intensity near goals
            var player1 = context.GetPlayer(matchup.Player1Id);
            var player2 = context.GetPlayer(matchup.Player2Id);
            
            if (player1 != null && player2 != null)
            {
                var nearGoal = context.IsNearGoal(player1.Id) || context.IsNearGoal(player2.Id);
                if (nearGoal) situationalModifier += 0.15f;
            }
            
            // Fade intensity if players are far apart
            if (player1 != null && player2 != null)
            {
                var distance = context.GetDistanceBetweenPlayers(player1.Id, player2.Id);
                if (distance > 20f) situationalModifier -= 0.3f;
            }
            
            matchup.Intensity = Math.Max(0.1f, Math.Min(1f, baseIntensity + situationalModifier));
        }
        
        private void UpdatePlayerPerformanceInMatchup(PlayerMatchup matchup, Player player1, 
            Player player2, MatchContext context)
        {
            // Track relative performance in this matchup
            var timeSinceUpdate = DateTime.Now - matchup.LastUpdated;
            if (timeSinceUpdate.TotalMinutes < 1) return; // Don't update too frequently
            
            var player1Performance = CalculatePlayerPerformanceScore(player1, context);
            var player2Performance = CalculatePlayerPerformanceScore(player2, context);
            
            matchup.Statistics.Player1PerformanceSum += player1Performance;
            matchup.Statistics.Player2PerformanceSum += player2Performance;
            matchup.Statistics.UpdateCount++;
            
            matchup.LastUpdated = DateTime.Now;
        }
        
        private float CalculatePlayerPerformanceScore(Player player, MatchContext context)
        {
            // Simple performance score based on recent actions
            var recentActions = context.GetRecentPlayerActions(player.Id, TimeSpan.FromMinutes(2));
            
            float score = 0f;
            foreach (var action in recentActions)
            {
                score += GetActionValue(action);
            }
            
            return Math.Max(0f, score);
        }
        
        private float GetActionValue(PlayerAction action)
        {
            return action.Type switch
            {
                ActionType.EffectiveDisposal => 1.0f,
                ActionType.Goal => 5.0f,
                ActionType.Behind => 2.0f,
                ActionType.Mark => 2.0f,
                ActionType.Tackle => 2.0f,
                ActionType.ContestedPossession => 2.5f,
                ActionType.Turnover => -1.5f,
                ActionType.MissedShot => -1.0f,
                _ => 0f
            };
        }
        
        private MatchupModifier CalculateMatchupModifier(string playerId, string opponentId, 
            PlayerMatchup matchup, MatchContext context)
        {
            var modifier = new MatchupModifier();
            
            // Base matchup effects
            var matchupAdvantage = CalculateMatchupAdvantage(playerId, opponentId, matchup, context);
            
            // Apply modifiers based on matchup type and intensity
            var baseEffect = matchupAdvantage * matchup.Intensity * _matchupInfluenceStrength;
            
            switch (matchup.Type)
            {
                case MatchupType.Positional:
                    modifier.OverallRating = baseEffect * 0.1f;
                    modifier.Confidence = baseEffect * 0.15f;
                    break;
                    
                case MatchupType.Ruck:
                    modifier.JumpReach = baseEffect * 0.2f;
                    modifier.Strength = baseEffect * 0.1f;
                    break;
                    
                case MatchupType.BallContest:
                    modifier.ContestedBall = baseEffect * 0.15f;
                    modifier.Aggression = baseEffect * 0.1f;
                    break;
                    
                case MatchupType.OneOnOne:
                    modifier.OneOnOne = baseEffect * 0.2f;
                    modifier.Pressure = baseEffect * 0.1f;
                    break;
                    
                case MatchupType.PressureContest:
                    modifier.PressureHandling = baseEffect * 0.15f;
                    modifier.DecisionMaking = baseEffect * 0.1f;
                    break;
            }
            
            return modifier;
        }
        
        private float CalculateMatchupAdvantage(string playerId, string opponentId, 
            PlayerMatchup matchup, MatchContext context)
        {
            var player = context.GetPlayer(playerId);
            var opponent = context.GetPlayer(opponentId);
            
            if (player == null || opponent == null) return 0f;
            
            // Calculate advantage based on relevant attributes for matchup type
            float advantage = 0f;
            
            switch (matchup.Type)
            {
                case MatchupType.Ruck:
                    advantage = (player.JumpReach - opponent.JumpReach) * 0.01f;
                    advantage += (player.Strength - opponent.Strength) * 0.01f;
                    break;
                    
                case MatchupType.BallContest:
                    advantage = (player.ContestedBall - opponent.ContestedBall) * 0.01f;
                    advantage += (player.Strength - opponent.Strength) * 0.005f;
                    break;
                    
                case MatchupType.OneOnOne:
                    if (player.Position.Contains("Forward"))
                    {
                        advantage = (player.Leading - opponent.OneOnOne) * 0.01f;
                        advantage += (player.GroundBall - opponent.Spoiling) * 0.005f;
                    }
                    else
                    {
                        advantage = (player.OneOnOne - opponent.Leading) * 0.01f;
                        advantage += (player.Spoiling - opponent.GroundBall) * 0.005f;
                    }
                    break;
                    
                default:
                    advantage = (player.OverallRating - opponent.OverallRating) * 0.005f;
                    break;
            }
            
            // Factor in form and fatigue
            advantage += (player.FormRating - opponent.FormRating) * 0.002f;
            advantage -= (player.FatigueLevel - opponent.FatigueLevel) * 0.05f;
            
            // Factor in historical performance
            var historicalAdvantage = GetHistoricalAdvantage(playerId, opponentId);
            advantage += historicalAdvantage * 0.3f;
            
            return Math.Max(-0.5f, Math.Min(0.5f, advantage)); // Clamp between -0.5 and 0.5
        }
        
        private float GetHistoricalAdvantage(string player1Id, string player2Id)
        {
            var matchupKey = GetMatchupKey(player1Id, player2Id);
            var history = _matchupHistory.GetValueOrDefault(matchupKey, new List<MatchupHistory>());
            
            if (!history.Any()) return 0f;
            
            var recentHistory = history.TakeLast(10).ToList(); // Focus on recent history
            var totalContests = recentHistory.Sum(h => h.TotalContests);
            
            if (totalContests == 0) return 0f;
            
            var player1Wins = recentHistory.Sum(h => h.Player1Wins);
            var winRate = (float)player1Wins / totalContests;
            
            // Convert win rate to advantage (-0.2 to +0.2)
            return (winRate - 0.5f) * 0.4f;
        }
        
        private void UpdateMatchupStatistics(PlayerMatchup matchup, MatchupResult result, ContestType contestType)
        {
            matchup.Statistics.TotalContests++;
            
            if (result.WinnerId == matchup.Player1Id)
            {
                matchup.Statistics.Player1Wins++;
            }
            else if (result.WinnerId == matchup.Player2Id)
            {
                matchup.Statistics.Player2Wins++;
            }
            
            // Update contest type specific stats
            if (!matchup.Statistics.ContestTypeStats.ContainsKey(contestType))
            {
                matchup.Statistics.ContestTypeStats[contestType] = new ContestStats();
            }
            
            var contestStats = matchup.Statistics.ContestTypeStats[contestType];
            contestStats.Total++;
            
            if (result.WinnerId == matchup.Player1Id)
            {
                contestStats.Player1Wins++;
            }
            else if (result.WinnerId == matchup.Player2Id)
            {
                contestStats.Player2Wins++;
            }
        }
        
        private void RecordMatchupEvent(MatchupEvent matchupEvent)
        {
            _matchupEvents.Add(matchupEvent);
            
            // Maintain event history limit
            if (_matchupEvents.Count > 1000)
            {
                _matchupEvents.RemoveRange(0, 200); // Remove oldest 200 events
            }
        }
        
        private void ApplyHistoricalContext()
        {
            // Load and apply historical matchup data to current matchups
            foreach (var matchup in _activeMatchups.Values)
            {
                var history = _matchupHistory.GetValueOrDefault(matchup.MatchupKey, 
                    new List<MatchupHistory>());
                
                if (history.Any())
                {
                    // Set initial statistics based on historical data
                    var recentHistory = history.TakeLast(5).ToList();
                    matchup.Statistics.TotalContests = recentHistory.Sum(h => h.TotalContests);
                    matchup.Statistics.Player1Wins = recentHistory.Sum(h => h.Player1Wins);
                    matchup.Statistics.Player2Wins = recentHistory.Sum(h => h.Player2Wins);
                    
                    // Adjust intensity based on historical rivalry
                    var rivalry = CalculateRivalryIntensity(history);
                    matchup.Intensity = Math.Min(1f, matchup.Intensity + rivalry * 0.1f);
                }
            }
        }
        
        private float CalculateRivalryIntensity(List<MatchupHistory> history)
        {
            if (history.Count < 5) return 0f;
            
            var totalContests = history.Sum(h => h.TotalContests);
            var intensity = Math.Min(1f, totalContests / 50f); // More contests = more rivalry
            
            // Close record increases rivalry
            var totalWins1 = history.Sum(h => h.Player1Wins);
            var totalWins2 = history.Sum(h => h.Player2Wins);
            var totalDecided = totalWins1 + totalWins2;
            
            if (totalDecided > 0)
            {
                var balance = 1f - Math.Abs((float)totalWins1 / totalDecided - 0.5f) * 2f;
                intensity *= balance; // More balanced = more intense rivalry
            }
            
            return intensity;
        }
        
        private void ApplyMatchupEffects(MatchContext context)
        {
            foreach (var matchup in _activeMatchups.Values)
            {
                if (!matchup.IsActive || matchup.Intensity < 0.1f) continue;
                
                var player1 = context.GetPlayer(matchup.Player1Id);
                var player2 = context.GetPlayer(matchup.Player2Id);
                
                if (player1 != null)
                {
                    var modifier = CalculateMatchupModifier(matchup.Player1Id, matchup.Player2Id, 
                        matchup, context);
                    _performanceModifier.ApplyModifier(player1, modifier);
                }
                
                if (player2 != null)
                {
                    var modifier = CalculateMatchupModifier(matchup.Player2Id, matchup.Player1Id, 
                        matchup, context);
                    _performanceModifier.ApplyModifier(player2, modifier);
                }
            }
        }
        
        private void CleanupExpiredMatchups()
        {
            var cutoff = DateTime.Now.AddMinutes(-10); // Remove matchups older than 10 minutes
            
            var expiredKeys = _activeMatchups
                .Where(kvp => kvp.Value.LastUpdated < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                _activeMatchups.Remove(key);
            }
        }
        
        private float GetAverageFatigue(string player1Id, string player2Id, MatchContext context)
        {
            var player1 = context.GetPlayer(player1Id);
            var player2 = context.GetPlayer(player2Id);
            
            if (player1 == null || player2 == null) return 0.5f;
            
            return (player1.FatigueLevel + player2.FatigueLevel) / 2f;
        }
        
        /// <summary>
        /// Save current matchup data to history
        /// </summary>
        public void SaveMatchupHistory()
        {
            foreach (var matchup in _activeMatchups.Values)
            {
                if (!_matchupHistory.ContainsKey(matchup.MatchupKey))
                {
                    _matchupHistory[matchup.MatchupKey] = new List<MatchupHistory>();
                }
                
                var historyList = _matchupHistory[matchup.MatchupKey];
                historyList.Add(new MatchupHistory
                {
                    MatchDate = DateTime.Now,
                    TotalContests = matchup.Statistics.TotalContests,
                    Player1Wins = matchup.Statistics.Player1Wins,
                    Player2Wins = matchup.Statistics.Player2Wins,
                    AverageIntensity = matchup.Intensity,
                    MatchupType = matchup.Type
                });
                
                // Maintain history limit
                if (historyList.Count > _historyTracking)
                {
                    historyList.RemoveAt(0);
                }
            }
        }
    }
}