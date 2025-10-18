using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLCoachSim.Core.Engine.Match
{
    /// <summary>
    /// Represents an active matchup between two players
    /// </summary>
    public class PlayerMatchup
    {
        public string Id { get; set; }
        public string MatchupKey { get; set; }
        public string Player1Id { get; set; }
        public string Player2Id { get; set; }
        public MatchupType Type { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; }
        public float Intensity { get; set; }
        public string Context { get; set; }
        public MatchupStatistics Statistics { get; set; }
        
        public PlayerMatchup()
        {
            Statistics = new MatchupStatistics();
        }
    }
    
    /// <summary>
    /// Statistical tracking for a matchup
    /// </summary>
    public class MatchupStatistics
    {
        public int TotalContests { get; set; }
        public int Player1Wins { get; set; }
        public int Player2Wins { get; set; }
        public float Player1PerformanceSum { get; set; }
        public float Player2PerformanceSum { get; set; }
        public int UpdateCount { get; set; }
        
        public Dictionary<ContestType, ContestStats> ContestTypeStats { get; set; }
        
        public MatchupStatistics()
        {
            ContestTypeStats = new Dictionary<ContestType, ContestStats>();
        }
        
        public float Player1WinRate => TotalContests > 0 ? (float)Player1Wins / TotalContests : 0f;
        public float Player2WinRate => TotalContests > 0 ? (float)Player2Wins / TotalContests : 0f;
        public float Player1AveragePerformance => UpdateCount > 0 ? Player1PerformanceSum / UpdateCount : 0f;
        public float Player2AveragePerformance => UpdateCount > 0 ? Player2PerformanceSum / UpdateCount : 0f;
    }
    
    /// <summary>
    /// Contest statistics for specific contest types
    /// </summary>
    public class ContestStats
    {
        public int Total { get; set; }
        public int Player1Wins { get; set; }
        public int Player2Wins { get; set; }
        
        public float Player1WinRate => Total > 0 ? (float)Player1Wins / Total : 0f;
        public float Player2WinRate => Total > 0 ? (float)Player2Wins / Total : 0f;
    }
    
    /// <summary>
    /// Historical matchup data
    /// </summary>
    public class MatchupHistory
    {
        public DateTime MatchDate { get; set; }
        public int TotalContests { get; set; }
        public int Player1Wins { get; set; }
        public int Player2Wins { get; set; }
        public float AverageIntensity { get; set; }
        public MatchupType MatchupType { get; set; }
    }
    
    /// <summary>
    /// Result of a player contest
    /// </summary>
    public class MatchupResult
    {
        public string WinnerId { get; set; }
        public string LoserId { get; set; }
        public float Confidence { get; set; } // 0-1 how confident in this result
        public float Margin { get; set; } // How decisive the contest was
        public Dictionary<string, object> Details { get; set; }
        
        public MatchupResult()
        {
            Details = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Prediction for a contest outcome
    /// </summary>
    public class ContestPrediction
    {
        public string FavoredPlayerId { get; set; }
        public float Player1WinProbability { get; set; }
        public float Player2WinProbability { get; set; }
        public float DrawProbability { get; set; }
        public float Confidence { get; set; }
        public string Reasoning { get; set; }
        public Dictionary<string, float> FactorInfluences { get; set; }
        
        public ContestPrediction()
        {
            FactorInfluences = new Dictionary<string, float>();
        }
    }
    
    /// <summary>
    /// Comprehensive matchup analysis
    /// </summary>
    public class MatchupAnalysis
    {
        public string Player1Id { get; set; }
        public string Player2Id { get; set; }
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }
        
        // Historical analysis
        public int TotalHistoricalContests { get; set; }
        public float Player1HistoricalWinRate { get; set; }
        public float Player2HistoricalWinRate { get; set; }
        public List<string> KeyHistoricalMoments { get; set; }
        
        // Current form analysis
        public float Player1CurrentForm { get; set; }
        public float Player2CurrentForm { get; set; }
        public float FormAdvantage { get; set; }
        
        // Attribute matchup analysis
        public Dictionary<string, AttributeComparison> AttributeComparisons { get; set; }
        public string OverallAdvantage { get; set; }
        public float AdvantageMargin { get; set; }
        
        // Tactical analysis
        public List<string> TacticalConsiderations { get; set; }
        public List<string> KeyMatchupPoints { get; set; }
        
        // Predictions
        public ContestPrediction OverallPrediction { get; set; }
        public Dictionary<ContestType, ContestPrediction> ContestTypePredictions { get; set; }
        
        public MatchupAnalysis()
        {
            KeyHistoricalMoments = new List<string>();
            AttributeComparisons = new Dictionary<string, AttributeComparison>();
            TacticalConsiderations = new List<string>();
            KeyMatchupPoints = new List<string>();
            ContestTypePredictions = new Dictionary<ContestType, ContestPrediction>();
        }
    }
    
    /// <summary>
    /// Comparison of a specific attribute between two players
    /// </summary>
    public class AttributeComparison
    {
        public string AttributeName { get; set; }
        public float Player1Value { get; set; }
        public float Player2Value { get; set; }
        public float Difference { get; set; }
        public float Significance { get; set; }
        public string Advantage { get; set; }
    }
    
    /// <summary>
    /// Event that occurred in a matchup
    /// </summary>
    public class MatchupEvent
    {
        public string MatchupId { get; set; }
        public MatchupEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public ContestType? ContestType { get; set; }
        public string Winner { get; set; }
        public float Confidence { get; set; }
        public Dictionary<string, object> Context { get; set; }
        
        public MatchupEvent()
        {
            Context = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Performance modifier for a player based on matchups
    /// </summary>
    public class PlayerPerformanceModifier
    {
        public string PlayerId { get; set; }
        public float OverallRatingModifier { get; set; }
        public float ConfidenceModifier { get; set; }
        public float AggressionModifier { get; set; }
        public float FocusModifier { get; set; }
        public Dictionary<string, float> SkillModifiers { get; set; }
        
        public PlayerPerformanceModifier(string playerId)
        {
            PlayerId = playerId;
            SkillModifiers = new Dictionary<string, float>();
        }
        
        public void ApplyModifier(MatchupModifier matchupModifier)
        {
            OverallRatingModifier += matchupModifier.OverallRating;
            ConfidenceModifier += matchupModifier.Confidence;
            AggressionModifier += matchupModifier.Aggression;
            
            // Apply skill-specific modifiers
            if (matchupModifier.ContestedBall != 0)
                AddSkillModifier("ContestedBall", matchupModifier.ContestedBall);
            if (matchupModifier.JumpReach != 0)
                AddSkillModifier("JumpReach", matchupModifier.JumpReach);
            if (matchupModifier.OneOnOne != 0)
                AddSkillModifier("OneOnOne", matchupModifier.OneOnOne);
            if (matchupModifier.PressureHandling != 0)
                AddSkillModifier("PressureHandling", matchupModifier.PressureHandling);
        }
        
        private void AddSkillModifier(string skillName, float modifier)
        {
            if (SkillModifiers.ContainsKey(skillName))
                SkillModifiers[skillName] += modifier;
            else
                SkillModifiers[skillName] = modifier;
        }
    }
    
    /// <summary>
    /// Specific modifiers applied by a matchup
    /// </summary>
    public class MatchupModifier
    {
        public float OverallRating { get; set; }
        public float Confidence { get; set; }
        public float Aggression { get; set; }
        public float Focus { get; set; }
        public float Pressure { get; set; }
        
        // Skill-specific modifiers
        public float ContestedBall { get; set; }
        public float JumpReach { get; set; }
        public float Strength { get; set; }
        public float OneOnOne { get; set; }
        public float PressureHandling { get; set; }
        public float DecisionMaking { get; set; }
    }
    
    /// <summary>
    /// Analyzer for matchup-related calculations and predictions
    /// </summary>
    public class MatchupAnalyzer
    {
        public MatchupResult AnalyzeContest(PlayerMatchup matchup, ContestType contestType, MatchContext context)
        {
            var player1 = context.GetPlayer(matchup.Player1Id);
            var player2 = context.GetPlayer(matchup.Player2Id);
            
            if (player1 == null || player2 == null)
            {
                return new MatchupResult
                {
                    WinnerId = null,
                    Confidence = 0f
                };
            }
            
            // Calculate contest probabilities
            var player1Probability = CalculateContestProbability(player1, player2, contestType, matchup, context);
            var player2Probability = 1f - player1Probability;
            
            // Determine winner
            var random = (float)new System.Random().NextDouble();
            var winnerId = random < player1Probability ? player1.Id : player2.Id;
            var loserId = winnerId.Equals(player1.Id) ? player2.Id : player1.Id;
            
            // Calculate confidence and margin
            var margin = Math.Abs(player1Probability - player2Probability);
            var confidence = Math.Min(1f, margin + 0.3f);
            
            return new MatchupResult
            {
                WinnerId = winnerId,
                LoserId = loserId,
                Confidence = confidence,
                Margin = margin,
                Details = new Dictionary<string, object>
                {
                    ["player1Probability"] = player1Probability,
                    ["player2Probability"] = player2Probability,
                    ["contestType"] = contestType
                }
            };
        }
        
        public ContestPrediction PredictContest(string player1Id, string player2Id, 
            ContestType contestType, PlayerMatchup matchup, MatchContext context)
        {
            var player1 = context.GetPlayer(player1Id);
            var player2 = context.GetPlayer(player2Id);
            
            if (player1 == null || player2 == null)
            {
                return new ContestPrediction
                {
                    Player1WinProbability = 0.5f,
                    Player2WinProbability = 0.5f,
                    Confidence = 0f
                };
            }
            
            var player1Probability = CalculateContestProbability(player1, player2, contestType, matchup, context);
            var player2Probability = 1f - player1Probability;
            
            var prediction = new ContestPrediction
            {
                Player1WinProbability = player1Probability,
                Player2WinProbability = player2Probability,
                DrawProbability = 0f, // AFL doesn't have draws in individual contests
                FavoredPlayerId = player1Probability > player2Probability ? player1Id : player2Id,
                Confidence = Math.Abs(player1Probability - player2Probability),
                Reasoning = GenerateContestReasoning(player1, player2, contestType, matchup)
            };
            
            // Add factor influences
            CalculateFactorInfluences(prediction, player1, player2, contestType, matchup, context);
            
            return prediction;
        }
        
        public MatchupAnalysis GenerateAnalysis(string player1Id, string player2Id, 
            PlayerMatchup currentMatchup, List<MatchupHistory> history)
        {
            var analysis = new MatchupAnalysis
            {
                Player1Id = player1Id,
                Player2Id = player2Id
            };
            
            // Historical analysis
            if (history.Any())
            {
                analysis.TotalHistoricalContests = history.Sum(h => h.TotalContests);
                var totalPlayer1Wins = history.Sum(h => h.Player1Wins);
                var totalPlayer2Wins = history.Sum(h => h.Player2Wins);
                
                if (analysis.TotalHistoricalContests > 0)
                {
                    analysis.Player1HistoricalWinRate = (float)totalPlayer1Wins / analysis.TotalHistoricalContests;
                    analysis.Player2HistoricalWinRate = (float)totalPlayer2Wins / analysis.TotalHistoricalContests;
                }
            }
            
            // Current matchup analysis
            if (currentMatchup != null)
            {
                analysis.KeyMatchupPoints.Add($"Current matchup intensity: {currentMatchup.Intensity:P0}");
                analysis.KeyMatchupPoints.Add($"Matchup type: {currentMatchup.Type}");
                
                if (currentMatchup.Statistics.TotalContests > 0)
                {
                    analysis.KeyMatchupPoints.Add(
                        $"Current match record: {currentMatchup.Statistics.Player1Wins}-{currentMatchup.Statistics.Player2Wins}");
                }
            }
            
            // Generate tactical considerations
            GenerateTacticalConsiderations(analysis, currentMatchup);
            
            return analysis;
        }
        
        private float CalculateContestProbability(Player player1, Player player2, 
            ContestType contestType, PlayerMatchup matchup, MatchContext context)
        {
            float baseProbability = 0.5f;
            
            // Attribute-based probability adjustment
            float attributeAdvantage = GetAttributeAdvantage(player1, player2, contestType);
            baseProbability += attributeAdvantage * 0.3f;
            
            // Form advantage
            float formAdvantage = (player1.FormRating - player2.FormRating) * 0.1f;
            baseProbability += formAdvantage;
            
            // Fatigue factor
            float fatigueAdvantage = (player2.FatigueLevel - player1.FatigueLevel) * 0.2f;
            baseProbability += fatigueAdvantage;
            
            // Historical advantage
            if (matchup?.Statistics != null && matchup.Statistics.TotalContests > 0)
            {
                float historicalAdvantage = (matchup.Statistics.Player1WinRate - 0.5f) * 0.2f;
                baseProbability += historicalAdvantage;
            }
            
            // Confidence factor
            float confidenceAdvantage = (player1.Confidence - player2.Confidence) * 0.1f;
            baseProbability += confidenceAdvantage;
            
            // Clamp to reasonable bounds
            return Math.Max(0.1f, Math.Min(0.9f, baseProbability));
        }
        
        private float GetAttributeAdvantage(Player player1, Player player2, ContestType contestType)
        {
            switch (contestType)
            {
                case ContestType.Ruck:
                    return ((player1.JumpReach - player2.JumpReach) + 
                           (player1.Strength - player2.Strength)) * 0.005f;
                    
                case ContestType.GroundBall:
                    return ((player1.GroundBall - player2.GroundBall) + 
                           (player1.ContestedBall - player2.ContestedBall)) * 0.005f;
                    
                case ContestType.Marking:
                    return ((player1.Marking - player2.Marking) + 
                           (player1.JumpReach - player2.JumpReach)) * 0.005f;
                    
                case ContestType.OneOnOne:
                    if (player1.Position.ToString().Contains("Forward"))
                    {
                        return ((player1.Leading - player2.OneOnOne) + 
                               (player1.GroundBall - player2.Spoiling)) * 0.005f;
                    }
                    else
                    {
                        return ((player1.OneOnOne - player2.Leading) + 
                               (player1.Spoiling - player2.GroundBall)) * 0.005f;
                    }
                    
                case ContestType.Tackle:
                    return ((player1.Tackling - player2.Evasion) + 
                           (player1.Strength - player2.Agility)) * 0.005f;
                    
                default:
                    return (player1.OverallRating - player2.OverallRating) * 0.002f;
            }
        }
        
        private string GenerateContestReasoning(Player player1, Player player2, 
            ContestType contestType, PlayerMatchup matchup)
        {
            var reasons = new List<string>();
            
            // Attribute comparison
            var attributeAdvantage = GetAttributeAdvantage(player1, player2, contestType);
            if (Math.Abs(attributeAdvantage) > 0.05f)
            {
                var favoredPlayer = attributeAdvantage > 0 ? player1.Name : player2.Name;
                reasons.Add($"{favoredPlayer} has superior {GetContestTypeAttribute(contestType)}");
            }
            
            // Form comparison
            if (Math.Abs(player1.FormRating - player2.FormRating) > 0.1f)
            {
                var formLeader = player1.FormRating > player2.FormRating ? player1.Name : player2.Name;
                reasons.Add($"{formLeader} in better current form");
            }
            
            // Historical record
            if (matchup?.Statistics != null && matchup.Statistics.TotalContests >= 3)
            {
                if (matchup.Statistics.Player1WinRate > 0.6f)
                    reasons.Add($"{player1.Name} has dominated this matchup historically");
                else if (matchup.Statistics.Player2WinRate > 0.6f)
                    reasons.Add($"{player2.Name} has dominated this matchup historically");
                else
                    reasons.Add("Very even historical record between these players");
            }
            
            return reasons.Any() ? string.Join(", ", reasons) : "Evenly matched contest";
        }
        
        private string GetContestTypeAttribute(ContestType contestType)
        {
            return contestType switch
            {
                ContestType.Ruck => "ruck work",
                ContestType.GroundBall => "ground ball skills",
                ContestType.Marking => "marking ability",
                ContestType.OneOnOne => "one-on-one skills",
                ContestType.Tackle => "tackling technique",
                _ => "overall ability"
            };
        }
        
        private void CalculateFactorInfluences(ContestPrediction prediction, Player player1, Player player2, 
            ContestType contestType, PlayerMatchup matchup, MatchContext context)
        {
            // Calculate how much each factor influences the prediction
            prediction.FactorInfluences["Attributes"] = Math.Abs(GetAttributeAdvantage(player1, player2, contestType)) * 3f;
            prediction.FactorInfluences["Form"] = Math.Abs(player1.FormRating - player2.FormRating);
            prediction.FactorInfluences["Fatigue"] = Math.Abs(player1.FatigueLevel - player2.FatigueLevel) * 2f;
            prediction.FactorInfluences["Confidence"] = Math.Abs(player1.Confidence - player2.Confidence);
            
            if (matchup?.Statistics != null && matchup.Statistics.TotalContests > 0)
            {
                prediction.FactorInfluences["History"] = Math.Abs(matchup.Statistics.Player1WinRate - 0.5f) * 2f;
            }
        }
        
        private void GenerateTacticalConsiderations(MatchupAnalysis analysis, PlayerMatchup currentMatchup)
        {
            if (currentMatchup == null) return;
            
            switch (currentMatchup.Type)
            {
                case MatchupType.Positional:
                    analysis.TacticalConsiderations.Add("Key positional battle that could influence team structure");
                    break;
                    
                case MatchupType.Ruck:
                    analysis.TacticalConsiderations.Add("Ruck dominance will impact midfield clearances");
                    break;
                    
                case MatchupType.OneOnOne:
                    analysis.TacticalConsiderations.Add("Direct one-on-one contest requiring individual excellence");
                    break;
                    
                case MatchupType.BallContest:
                    analysis.TacticalConsiderations.Add("Ball-winning ability crucial for team possession");
                    break;
                    
                case MatchupType.PressureContest:
                    analysis.TacticalConsiderations.Add("High-pressure situation requiring composure");
                    break;
            }
            
            if (currentMatchup.Intensity > 0.8f)
            {
                analysis.TacticalConsiderations.Add("High-intensity matchup requiring mental strength");
            }
        }
    }
    
    /// <summary>
    /// Applies performance modifiers to players based on matchups
    /// </summary>
    public class PerformanceModifier
    {
        public void ApplyModifier(Player player, MatchupModifier modifier)
        {
            // Apply temporary modifiers (don't permanently change base attributes)
            player.CurrentMatchRating += modifier.OverallRating;
            player.Confidence = (int)Math.Max(0f, Math.Min(100f, player.Confidence + modifier.Confidence * 100));
            player.Aggression = (int)Math.Max(0f, Math.Min(100f, player.Aggression + modifier.Aggression * 100));
            
            // Apply skill-specific modifiers to temporary match attributes
            ApplySkillModifier(player, "ContestedBall", modifier.ContestedBall);
            ApplySkillModifier(player, "JumpReach", modifier.JumpReach);
            ApplySkillModifier(player, "OneOnOne", modifier.OneOnOne);
            ApplySkillModifier(player, "PressureHandling", modifier.PressureHandling);
        }
        
        private void ApplySkillModifier(Player player, string skillName, float modifier)
        {
            if (modifier == 0f) return;
            
            // Apply modifier to temporary match attributes
            if (!player.MatchAttributeModifiers.ContainsKey(skillName))
                player.MatchAttributeModifiers[skillName] = 0f;
            
            player.MatchAttributeModifiers[skillName] += modifier;
        }
    }
    
    /// <summary>
    /// Player action for tracking recent performance
    /// </summary>
    public class PlayerAction
    {
        public ActionType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
        public Dictionary<string, object> Context { get; set; }
        
        public PlayerAction()
        {
            Context = new Dictionary<string, object>();
        }
    }
    
    // Supporting enums
    public enum MatchupType
    {
        Positional,
        Ruck,
        BallContest,
        OneOnOne,
        PressureContest,
        Contest
    }
    
    public enum ContestType
    {
        Ruck,
        GroundBall,
        Marking,
        OneOnOne,
        Tackle,
        Intercept,
        Spoil,
        ContestedPossession,
        General
    }
    
    public enum MatchupEventType
    {
        Contest,
        PerformanceUpdate,
        IntensityChange,
        MatchupCreated,
        MatchupExpired
    }
    
    public enum ActionType
    {
        EffectiveDisposal,
        Goal,
        Behind,
        Mark,
        Tackle,
        ContestedPossession,
        Turnover,
        MissedShot,
        Intercept,
        Spoil,
        GroundBall,
        Assist
    }
}