using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems.Training;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLManager.Systems.Training.AI
{
    /// <summary>
    /// AI-powered training program recommendations engine that analyzes multiple factors
    /// to generate optimal, personalized training programs for players
    /// </summary>
    public class TrainingRecommendationEngine : MonoBehaviour
    {
        [Header("AI Configuration")]
        // [SerializeField] private float playerAttributeWeight = 0.25f; // TODO: Implement weighted scoring
        // [SerializeField] private float developmentPotentialWeight = 0.20f; // TODO: Implement weighted scoring
        // [SerializeField] private float injuryHistoryWeight = 0.15f; // TODO: Implement weighted scoring
        // [SerializeField] private float fixtureContextWeight = 0.20f; // TODO: Implement weighted scoring
        // [SerializeField] private float teamNeedsWeight = 0.20f; // TODO: Implement weighted scoring
        
        [Header("Learning Parameters")]
        [SerializeField] private bool enableMachineLearning = true;
        [SerializeField] private float learningRate = 0.1f;
        [SerializeField] private int historicalDataWindow = 30; // days
        [SerializeField] private float recommendationConfidenceThreshold = 0.7f;
        
        [Header("System Dependencies")]
        [SerializeField] private WeeklyTrainingScheduleManager scheduleManager;
        [SerializeField] private TrainingFatigueIntegrationManager fatigueManager;
        [SerializeField] private SeasonTrainingCalendarManager seasonCalendar;
        
        // AI Models and Data
        private TrainingEffectivenessModel effectivenessModel;
        private PlayerDevelopmentPredictor developmentPredictor;
        private InjuryRiskAssessment injuryRiskModel;
        private TeamBalanceAnalyzer teamAnalyzer;
        
        // Historical tracking for learning
        private Dictionary<int, List<TrainingOutcome>> playerOutcomeHistory = new Dictionary<int, List<TrainingOutcome>>();
        private Dictionary<TrainingProgramType, EffectivenessMetrics> programEffectiveness = new Dictionary<TrainingProgramType, EffectivenessMetrics>();
        
        // Events
        // public event System.Action<TrainingProgramRecommendation> OnRecommendationGenerated; // TODO: Implement recommendation event handler
        public event System.Action<int, List<TrainingProgramRecommendation>> OnPlayerRecommendationsUpdated;
        public event System.Action<TeamTrainingStrategy> OnTeamStrategyRecommended;
        
        private void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initialize the AI recommendation engine
        /// </summary>
        public void Initialize()
        {
            // Initialize AI models
            effectivenessModel = new TrainingEffectivenessModel();
            developmentPredictor = new PlayerDevelopmentPredictor();
            injuryRiskModel = new InjuryRiskAssessment();
            teamAnalyzer = new TeamBalanceAnalyzer();
            
            // Load historical data for machine learning
            LoadHistoricalData();
            
            // Initialize program effectiveness tracking
            InitializeProgramTracking();
            
            Debug.Log("[AI TrainingRecommendation] AI recommendation engine initialized");
        }
        
        /// <summary>
        /// Generate comprehensive training recommendations for a single player
        /// </summary>
        public PlayerTrainingRecommendations GeneratePlayerRecommendations(Player player, DateTime targetDate)
        {
            if (player == null)
            {
                Debug.LogWarning("[AI TrainingRecommendation] Cannot generate recommendations for null player");
                return null;
            }
            
            var context = BuildAnalysisContext(player, targetDate);
            var recommendations = new PlayerTrainingRecommendations
            {
                PlayerId = int.Parse(player.Id),
                PlayerName = player.Name,
                GeneratedDate = DateTime.Now,
                TargetDate = targetDate,
                Recommendations = new List<TrainingProgramRecommendation>()
            };
            
            // Generate different types of recommendations
            var programs = GenerateTrainingPrograms(player, context);
            recommendations.Recommendations.AddRange(programs);
            
            // Add specialized recommendations
            var developmentPrograms = GenerateDevelopmentRecommendations(player, context);
            recommendations.Recommendations.AddRange(developmentPrograms);
            
            var recoveryPrograms = GenerateRecoveryRecommendations(player, context);
            recommendations.Recommendations.AddRange(recoveryPrograms);
            
            var skillPrograms = GenerateSkillDevelopmentRecommendations(player, context);
            recommendations.Recommendations.AddRange(skillPrograms);
            
            // Rank and filter recommendations
            RankRecommendations(recommendations.Recommendations, context);
            FilterRecommendationsByConfidence(recommendations.Recommendations);
            
            // Calculate overall recommendation score
            recommendations.OverallConfidence = CalculateOverallConfidence(recommendations.Recommendations);
            recommendations.PriorityRecommendation = recommendations.Recommendations.FirstOrDefault();
            
            // Store for learning
            StoreRecommendationForLearning(int.Parse(player.Id), recommendations);
            
            OnPlayerRecommendationsUpdated?.Invoke(int.Parse(player.Id), recommendations.Recommendations);
            
            Debug.Log($"[AI TrainingRecommendation] Generated {recommendations.Recommendations.Count} recommendations for {player.Name}");
            
            return recommendations;
        }
        
        /// <summary>
        /// Generate team-wide training strategy recommendations
        /// </summary>
        public TeamTrainingStrategy GenerateTeamStrategy(List<Player> teamPlayers, DateTime targetDate)
        {
            if (teamPlayers?.Any() != true)
            {
                Debug.LogWarning("[AI TrainingRecommendation] Cannot generate team strategy for empty team");
                return null;
            }
            
            var teamContext = BuildTeamAnalysisContext(teamPlayers, targetDate);
            var strategy = new TeamTrainingStrategy
            {
                GeneratedDate = DateTime.Now,
                TargetDate = targetDate,
                TeamSize = teamPlayers.Count
            };
            
            // Analyze team composition and needs
            var teamAnalysis = teamAnalyzer.AnalyzeTeam(teamPlayers, teamContext);
            strategy.TeamAnalysis = teamAnalysis;
            
            // Generate strategic recommendations
            strategy.StrategicFocus = DetermineStrategicFocus(teamAnalysis, teamContext);
            strategy.TrainingEmphasis = GenerateTrainingEmphasis(teamAnalysis);
            strategy.LoadManagementStrategy = GenerateLoadManagementStrategy(teamPlayers, teamContext);
            
            // Generate position-specific programs
            strategy.PositionSpecificPrograms = GeneratePositionSpecificPrograms(teamPlayers, teamContext);
            
            // Calculate team readiness and recommendations
            strategy.TeamReadiness = CalculateTeamReadiness(teamPlayers, teamContext);
            strategy.WeeklyFocus = GenerateWeeklyFocus(teamContext);
            
            // Risk assessment
            strategy.RiskAssessment = GenerateTeamRiskAssessment(teamPlayers);
            
            strategy.Confidence = CalculateStrategyConfidence(strategy);
            
            OnTeamStrategyRecommended?.Invoke(strategy);
            
            Debug.Log($"[AI TrainingRecommendation] Generated team strategy with {strategy.StrategicFocus.Count} focus areas");
            
            return strategy;
        }
        
        /// <summary>
        /// Learn from training outcomes to improve future recommendations
        /// </summary>
        public void RecordTrainingOutcome(int playerId, TrainingProgramType programType, TrainingOutcome outcome)
        {
            if (!enableMachineLearning) return;
            
            // Store outcome for player-specific learning
            if (!playerOutcomeHistory.ContainsKey(playerId))
                playerOutcomeHistory[playerId] = new List<TrainingOutcome>();
            
            playerOutcomeHistory[playerId].Add(outcome);
            
            // Limit historical data to prevent memory bloat
            if (playerOutcomeHistory[playerId].Count > 100)
            {
                playerOutcomeHistory[playerId].RemoveAt(0);
            }
            
            // Update program effectiveness metrics
            UpdateProgramEffectiveness(programType, outcome);
            
            // Update AI models with new data
            effectivenessModel.UpdateModel(outcome);
            developmentPredictor.UpdateModel(playerId, outcome);
            injuryRiskModel.UpdateModel(playerId, outcome);
            
            Debug.Log($"[AI TrainingRecommendation] Recorded training outcome for player {playerId}, program {programType}");
        }
        
        /// <summary>
        /// Get AI insights about current training effectiveness
        /// </summary>
        public AITrainingInsights GenerateInsights(List<Player> players)
        {
            var insights = new AITrainingInsights
            {
                GeneratedDate = DateTime.Now,
                PlayerCount = players?.Count ?? 0
            };
            
            if (players?.Any() != true) return insights;
            
            // Analyze current training effectiveness
            insights.OverallEffectiveness = CalculateOverallTrainingEffectiveness(players);
            insights.EffectivenessTrend = CalculateEffectivenessTrend();
            
            // Identify patterns and opportunities
            insights.Patterns = IdentifyTrainingPatterns(players);
            insights.Opportunities = IdentifyImprovementOpportunities(players);
            insights.RiskFactors = IdentifyRiskFactors(players);
            
            // Generate strategic insights
            insights.StrategicInsights = GenerateStrategicInsights(players);
            insights.OptimizationSuggestions = GenerateOptimizationSuggestions(players);
            
            // Prediction insights
            insights.PredictedOutcomes = GeneratePredictions(players);
            
            insights.ConfidenceLevel = CalculateInsightsConfidence(insights);
            
            Debug.Log($"[AI TrainingRecommendation] Generated insights with {insights.Patterns.Count} patterns identified");
            
            return insights;
        }
        
        #region Core Analysis Methods
        
        private TrainingAnalysisContext BuildAnalysisContext(Player player, DateTime targetDate)
        {
            var context = new TrainingAnalysisContext
            {
                Player = player,
                TargetDate = targetDate,
                CurrentDate = DateTime.Now
            };
            
            // Get fixture context
            // NOTE: SeasonTrainingCalendarManager doesn't have GetUpcomingMatches method
            // This needs to be implemented or use a different approach
            context.UpcomingMatches = new List<ScheduledMatch>();
            context.DaysUntilNextMatch = 7; // Default placeholder
            
            // Get current training load and fatigue
            var fatigueStatus = fatigueManager?.GetPlayerFatigueStatus(int.Parse(player.Id));
            context.CurrentFatigueLevel = fatigueStatus?.CurrentFatigueLevel ?? 0f;
            context.CurrentLoad = fatigueStatus?.DailyLoadAccumulated ?? 0f;
            context.RiskLevel = DetermineRiskLevel(fatigueStatus);
            
            // Get historical performance
            context.HistoricalOutcomes = GetPlayerHistoricalOutcomes(int.Parse(player.Id));
            
            // Development analysis
            context.DevelopmentPotential = CalculateDevelopmentPotential(player);
            context.SkillGaps = IdentifySkillGaps(player);
            
            // Injury context
            context.InjuryHistory = GetPlayerInjuryHistory(int.Parse(player.Id));
            context.InjuryRisk = injuryRiskModel.AssessRisk(player, context);
            
            return context;
        }
        
        private TeamAnalysisContext BuildTeamAnalysisContext(List<Player> players, DateTime targetDate)
        {
            return new TeamAnalysisContext
            {
                Players = players,
                TargetDate = targetDate,
                CurrentDate = DateTime.Now,
                UpcomingMatches = new List<ScheduledMatch>(), // Placeholder until GetUpcomingMatches is implemented
                SeasonPhase = DetermineSeasonPhase(targetDate),
                TeamAverageCondition = (float)players.Average(p => p.Stamina),
                TeamAverageAge = (float)players.Average(p => p.Age),
                PositionDistribution = AnalyzePositionDistribution(players)
            };
        }
        
        private List<TrainingProgramRecommendation> GenerateTrainingPrograms(Player player, TrainingAnalysisContext context)
        {
            var recommendations = new List<TrainingProgramRecommendation>();
            
            // Generate base training program
            var baseProgram = GenerateBaseTrainingProgram(player, context);
            recommendations.Add(baseProgram);
            
            // Generate alternative programs
            if (context.RiskLevel >= FatigueRiskLevel.High)
            {
                var recoveryProgram = GenerateRecoveryFocusedProgram(player, context);
                recommendations.Add(recoveryProgram);
            }
            
            if (context.DaysUntilNextMatch <= 7)
            {
                var matchPrepProgram = GenerateMatchPreparationProgram(player, context);
                recommendations.Add(matchPrepProgram);
            }
            
            if (player.Age <= 23)
            {
                var developmentProgram = GenerateDevelopmentFocusedProgram(player, context);
                recommendations.Add(developmentProgram);
            }
            
            return recommendations;
        }
        
        private TrainingProgramRecommendation GenerateBaseTrainingProgram(Player player, TrainingAnalysisContext context)
        {
            var recommendation = new TrainingProgramRecommendation
            {
                ProgramType = TrainingProgramType.Balanced,
                ProgramName = $"Balanced Program for {player.Name}",
                Description = "Well-rounded training program focusing on overall fitness and skill maintenance",
                Duration = TimeSpan.FromDays(7),
                TargetPlayerId = int.Parse(player.Id)
            };
            
            // Analyze player attributes to determine focus areas
            var attributeAnalysis = AnalyzePlayerAttributes(player);
            recommendation.FocusAreas = DetermineFocusAreas(attributeAnalysis, context);
            
            // Generate weekly schedule
            recommendation.WeeklySchedule = GenerateWeeklySchedule(player, context, TrainingProgramType.Balanced);
            
            // Calculate load and intensity
            recommendation.EstimatedLoad = CalculateOptimalLoad(player, context);
            recommendation.AverageIntensity = DetermineOptimalIntensity(player, context);
            
            // Assess risks and benefits
            recommendation.Risks = AssessProgramRisks(player, recommendation, context);
            recommendation.ExpectedBenefits = PredictProgramBenefits(player, recommendation, context);
            
            // Calculate confidence using AI models
            recommendation.Confidence = CalculateProgramConfidence(player, recommendation, context);
            
            // Generate reasoning
            recommendation.AIReasoning = GenerateReasoningExplanation(player, recommendation, context);
            
            return recommendation;
        }
        
        #endregion
        
        #region AI Learning Methods
        
        private void LoadHistoricalData()
        {
            // In a real implementation, this would load from persistent storage
            // For now, we'll initialize with empty data
            playerOutcomeHistory.Clear();
            
            Debug.Log("[AI TrainingRecommendation] Historical data loaded for machine learning");
        }
        
        private void UpdateProgramEffectiveness(TrainingProgramType programType, TrainingOutcome outcome)
        {
            if (!programEffectiveness.ContainsKey(programType))
            {
                programEffectiveness[programType] = new EffectivenessMetrics();
            }
            
            var metrics = programEffectiveness[programType];
            metrics.TotalExecutions++;
            metrics.TotalEffectiveness += outcome.EffectivenessScore;
            metrics.TotalInjuries += outcome.InjuryOccurred ? 1 : 0;
            metrics.AverageEffectiveness = metrics.TotalEffectiveness / metrics.TotalExecutions;
            metrics.InjuryRate = (float)metrics.TotalInjuries / metrics.TotalExecutions;
            
            // Update with exponential smoothing for recent bias
            var alpha = learningRate;
            metrics.SmoothedEffectiveness = alpha * outcome.EffectivenessScore + (1 - alpha) * metrics.SmoothedEffectiveness;
            
            programEffectiveness[programType] = metrics;
        }
        
        private void RankRecommendations(List<TrainingProgramRecommendation> recommendations, TrainingAnalysisContext context)
        {
            // Multi-criteria ranking algorithm
            foreach (var rec in recommendations)
            {
                rec.RankingScore = CalculateRankingScore(rec, context);
            }
            
            // Sort by ranking score (highest first)
            recommendations.Sort((a, b) => b.RankingScore.CompareTo(a.RankingScore));
        }
        
        private float CalculateRankingScore(TrainingProgramRecommendation recommendation, TrainingAnalysisContext context)
        {
            float score = 0f;
            
            // Confidence weight (30%)
            score += recommendation.Confidence * 0.3f;
            
            // Expected benefit weight (25%)
            score += CalculateBenefitScore(recommendation.ExpectedBenefits) * 0.25f;
            
            // Risk penalty (20%)
            score -= CalculateRiskPenalty(recommendation.Risks) * 0.2f;
            
            // Context relevance (15%)
            score += CalculateContextRelevance(recommendation, context) * 0.15f;
            
            // Historical effectiveness (10%)
            score += GetHistoricalEffectiveness(recommendation.ProgramType) * 0.1f;
            
            return Mathf.Clamp01(score);
        }
        
        #endregion
        
        #region Helper Methods
        
        private void InitializeProgramTracking()
        {
            var programTypes = Enum.GetValues(typeof(TrainingProgramType)).Cast<TrainingProgramType>();
            foreach (var programType in programTypes)
            {
                if (!programEffectiveness.ContainsKey(programType))
                {
                    programEffectiveness[programType] = new EffectivenessMetrics
                    {
                        AverageEffectiveness = 0.7f, // Default baseline
                        SmoothedEffectiveness = 0.7f
                    };
                }
            }
        }
        
        private int GetDaysUntilNextMatch(List<ScheduledMatch> upcomingMatches)
        {
            var nextMatch = upcomingMatches.OrderBy(m => m.ScheduledDateTime).FirstOrDefault();
            return nextMatch != null ? (int)(nextMatch.ScheduledDateTime - DateTime.Now).TotalDays : 14;
        }
        
        private List<TrainingOutcome> GetPlayerHistoricalOutcomes(int playerId)
        {
            return playerOutcomeHistory.ContainsKey(playerId) ? 
                   playerOutcomeHistory[playerId].Where(o => o.Date >= DateTime.Now.AddDays(-historicalDataWindow)).ToList() :
                   new List<TrainingOutcome>();
        }
        
        private float CalculateDevelopmentPotential(Player player)
        {
            // Simple potential calculation based on age and current stats
            var ageFactor = player.Age <= 23 ? 1.0f : player.Age <= 27 ? 0.8f : player.Age <= 30 ? 0.6f : 0.4f;
            var statsFactor = (100f - player.Stats.GetAverage()) / 100f; // Room for improvement
            return ageFactor * statsFactor;
        }
        
        private List<string> IdentifySkillGaps(Player player)
        {
            var gaps = new List<string>();
            var stats = player.Stats;
            
            if (stats.Kicking < 70) gaps.Add("Kicking Accuracy");
            if (stats.Handballing < 70) gaps.Add("Handballing Precision");
            if (stats.Speed < 70) gaps.Add("Speed Development");
            if (stats.Stamina < 70) gaps.Add("Endurance Training");
            if (stats.Tackling < 70) gaps.Add("Defensive Skills");
            if (stats.Knowledge < 70) gaps.Add("Game Awareness");
            if (stats.Playmaking < 70) gaps.Add("Decision Making");
            
            return gaps;
        }
        
        private float CalculateOverallConfidence(List<TrainingProgramRecommendation> recommendations)
        {
            return recommendations.Any() ? recommendations.Average(r => r.Confidence) : 0f;
        }
        
        private void StoreRecommendationForLearning(int playerId, PlayerTrainingRecommendations recommendations)
        {
            // Store recommendations for future learning when outcomes are recorded
            // This would typically be persisted to a database
        }
        
        private void FilterRecommendationsByConfidence(List<TrainingProgramRecommendation> recommendations)
        {
            recommendations.RemoveAll(r => r.Confidence < recommendationConfidenceThreshold);
        }
        
        #endregion
        
        #region Placeholder Methods (to be implemented)
        
        private List<TrainingProgramRecommendation> GenerateDevelopmentRecommendations(Player player, TrainingAnalysisContext context) => new List<TrainingProgramRecommendation>();
        private List<TrainingProgramRecommendation> GenerateRecoveryRecommendations(Player player, TrainingAnalysisContext context) => new List<TrainingProgramRecommendation>();
        private List<TrainingProgramRecommendation> GenerateSkillDevelopmentRecommendations(Player player, TrainingAnalysisContext context) => new List<TrainingProgramRecommendation>();
        private TrainingProgramRecommendation GenerateRecoveryFocusedProgram(Player player, TrainingAnalysisContext context) => new TrainingProgramRecommendation();
        private TrainingProgramRecommendation GenerateMatchPreparationProgram(Player player, TrainingAnalysisContext context) => new TrainingProgramRecommendation();
        private TrainingProgramRecommendation GenerateDevelopmentFocusedProgram(Player player, TrainingAnalysisContext context) => new TrainingProgramRecommendation();
        private PlayerAttributeAnalysis AnalyzePlayerAttributes(Player player) => new PlayerAttributeAnalysis();
        private List<string> DetermineFocusAreas(PlayerAttributeAnalysis analysis, TrainingAnalysisContext context) => new List<string>();
        private List<DailyTrainingSession> GenerateWeeklySchedule(Player player, TrainingAnalysisContext context, TrainingProgramType type) => new List<DailyTrainingSession>();
        private float CalculateOptimalLoad(Player player, TrainingAnalysisContext context) => 50f;
        private TrainingIntensity DetermineOptimalIntensity(Player player, TrainingAnalysisContext context) => TrainingIntensity.Moderate;
        private List<string> AssessProgramRisks(Player player, TrainingProgramRecommendation rec, TrainingAnalysisContext context) => new List<string>();
        private List<string> PredictProgramBenefits(Player player, TrainingProgramRecommendation rec, TrainingAnalysisContext context) => new List<string>();
        private float CalculateProgramConfidence(Player player, TrainingProgramRecommendation rec, TrainingAnalysisContext context) => 0.8f;
        private string GenerateReasoningExplanation(Player player, TrainingProgramRecommendation rec, TrainingAnalysisContext context) => "AI-generated optimal training program";
        
        // Team strategy methods
        private List<string> DetermineStrategicFocus(TeamAnalysis analysis, TeamAnalysisContext context) => new List<string>();
        private Dictionary<string, float> GenerateTrainingEmphasis(TeamAnalysis analysis) => new Dictionary<string, float>();
        private TeamLoadManagementStrategy GenerateLoadManagementStrategy(List<Player> players, TeamAnalysisContext context) => new TeamLoadManagementStrategy();
        private Dictionary<string, TrainingProgramRecommendation> GeneratePositionSpecificPrograms(List<Player> players, TeamAnalysisContext context) => new Dictionary<string, TrainingProgramRecommendation>();
        private float CalculateTeamReadiness(List<Player> players, TeamAnalysisContext context) => 0.8f;
        private List<string> GenerateWeeklyFocus(TeamAnalysisContext context) => new List<string>();
        private TeamRiskAssessment GenerateTeamRiskAssessment(List<Player> players) => new TeamRiskAssessment();
        private float CalculateStrategyConfidence(TeamTrainingStrategy strategy) => 0.8f;
        
        // Insights methods
        private float CalculateOverallTrainingEffectiveness(List<Player> players) => 0.75f;
        private string CalculateEffectivenessTrend() => "Stable";
        private List<TrainingPattern> IdentifyTrainingPatterns(List<Player> players) => new List<TrainingPattern>();
        private List<ImprovementOpportunity> IdentifyImprovementOpportunities(List<Player> players) => new List<ImprovementOpportunity>();
        private List<string> IdentifyRiskFactors(List<Player> players) => new List<string>();
        private List<string> GenerateStrategicInsights(List<Player> players) => new List<string>();
        private List<string> GenerateOptimizationSuggestions(List<Player> players) => new List<string>();
        private List<PredictedOutcome> GeneratePredictions(List<Player> players) => new List<PredictedOutcome>();
        private float CalculateInsightsConfidence(AITrainingInsights insights) => 0.8f;
        
        // Utility methods
        private float CalculateBenefitScore(List<string> benefits) => benefits?.Count > 0 ? 0.8f : 0.5f;
        private float CalculateRiskPenalty(List<string> risks) => (risks?.Count ?? 0) * 0.1f;
        private float CalculateContextRelevance(TrainingProgramRecommendation rec, TrainingAnalysisContext context) => 0.7f;
        private float GetHistoricalEffectiveness(TrainingProgramType programType) => programEffectiveness.ContainsKey(programType) ? programEffectiveness[programType].SmoothedEffectiveness : 0.7f;
        private SeasonPhase DetermineSeasonPhase(DateTime date) => SeasonPhase.Regular;
        private Dictionary<string, int> AnalyzePositionDistribution(List<Player> players) => new Dictionary<string, int>();
        private List<InjuryRecord> GetPlayerInjuryHistory(int playerId) => new List<InjuryRecord>();
        
        private FatigueRiskLevel DetermineRiskLevel(PlayerFatigueStatus fatigueStatus)
        {
            if (fatigueStatus == null) return FatigueRiskLevel.Low;
            
            // Determine risk level based on fatigue and load
            if (fatigueStatus.CurrentFatigueLevel > 80f || fatigueStatus.WeeklyLoadAccumulated > 90f)
                return FatigueRiskLevel.Critical;
            if (fatigueStatus.CurrentFatigueLevel > 60f || fatigueStatus.WeeklyLoadAccumulated > 75f)
                return FatigueRiskLevel.High;
            if (fatigueStatus.CurrentFatigueLevel > 40f || fatigueStatus.WeeklyLoadAccumulated > 60f)
                return FatigueRiskLevel.Moderate;
            return FatigueRiskLevel.Low;
        }
        
        #endregion
    }
}
