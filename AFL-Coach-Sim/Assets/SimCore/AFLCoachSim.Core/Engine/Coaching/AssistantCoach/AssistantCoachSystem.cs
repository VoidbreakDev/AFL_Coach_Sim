using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Tactics;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Coaching.AssistantCoach
{
    /// <summary>
    /// System for managing assistant coaches and their specialized contributions
    /// </summary>
    public class AssistantCoachSystem
    {
        private readonly Dictionary<TeamId, List<AssistantCoachProfile>> _teamAssistants;
        private readonly Dictionary<AssistantCoachId, AssistantCoachPerformance> _performanceTracking;
        private readonly Random _random;

        public AssistantCoachSystem(int seed = 0)
        {
            _teamAssistants = new Dictionary<TeamId, List<AssistantCoachProfile>>();
            _performanceTracking = new Dictionary<AssistantCoachId, AssistantCoachPerformance>();
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        #region Assistant Coach Management

        /// <summary>
        /// Hire an assistant coach for a team
        /// </summary>
        public bool HireAssistantCoach(TeamId teamId, AssistantCoachProfile assistant)
        {
            if (!_teamAssistants.ContainsKey(teamId))
                _teamAssistants[teamId] = new List<AssistantCoachProfile>();

            var currentAssistants = _teamAssistants[teamId];
            
            // Check if team has reached maximum assistant limit
            int maxAssistants = CalculateMaxAssistants(teamId);
            if (currentAssistants.Count >= maxAssistants)
            {
                CoreLogger.LogWarning($"[AssistantCoach] Team {teamId} already has maximum assistants ({maxAssistants})");
                return false;
            }

            // Check for duplicate specializations (only one per type)
            if (currentAssistants.Any(a => a.Specialization == assistant.Specialization))
            {
                CoreLogger.LogWarning($"[AssistantCoach] Team {teamId} already has {assistant.Specialization} specialist");
                return false;
            }

            currentAssistants.Add(assistant);
            _performanceTracking[assistant.Id] = new AssistantCoachPerformance(assistant.Id);
            
            CoreLogger.Log($"[AssistantCoach] Hired {assistant.Name} ({assistant.Specialization}) for team {teamId}");
            return true;
        }

        /// <summary>
        /// Fire an assistant coach
        /// </summary>
        public bool FireAssistantCoach(TeamId teamId, AssistantCoachId assistantId)
        {
            if (!_teamAssistants.ContainsKey(teamId))
                return false;

            var assistants = _teamAssistants[teamId];
            var assistant = assistants.FirstOrDefault(a => a.Id == assistantId);
            
            if (assistant == null)
                return false;

            assistants.Remove(assistant);
            _performanceTracking.Remove(assistantId);
            
            CoreLogger.Log($"[AssistantCoach] Fired {assistant.Name} from team {teamId}");
            return true;
        }

        /// <summary>
        /// Get all assistant coaches for a team
        /// </summary>
        public List<AssistantCoachProfile> GetTeamAssistants(TeamId teamId)
        {
            return _teamAssistants.GetValueOrDefault(teamId, new List<AssistantCoachProfile>());
        }

        #endregion

        #region Training Contributions

        /// <summary>
        /// Calculate training bonuses provided by assistant coaches
        /// </summary>
        public TrainingBonuses CalculateTrainingBonuses(TeamId teamId, TrainingSession trainingSession)
        {
            var assistants = GetTeamAssistants(teamId);
            var bonuses = new TrainingBonuses();

            foreach (var assistant in assistants)
            {
                var contribution = CalculateAssistantTrainingContribution(assistant, trainingSession);
                ApplyTrainingContribution(bonuses, contribution);
                
                // Track performance
                if (_performanceTracking.TryGetValue(assistant.Id, out var performance))
                {
                    performance.RecordTrainingContribution(contribution.OverallEffectiveness);
                }
            }

            LogTrainingBonuses(teamId, bonuses, assistants.Count);
            return bonuses;
        }

        /// <summary>
        /// Get specialized training recommendations from assistant coaches
        /// </summary>
        public List<TrainingRecommendation> GetTrainingRecommendations(TeamId teamId, 
            List<PlayerTrainingData> playerData)
        {
            var assistants = GetTeamAssistants(teamId);
            var recommendations = new List<TrainingRecommendation>();

            foreach (var assistant in assistants)
            {
                var assistantRecommendations = GenerateSpecializedRecommendations(assistant, playerData);
                recommendations.AddRange(assistantRecommendations);
            }

            // Sort by priority and impact
            return recommendations.OrderByDescending(r => r.Priority * r.ExpectedImpact).ToList();
        }

        #endregion

        #region Match Day Contributions

        /// <summary>
        /// Get tactical insights from assistant coaches during match
        /// </summary>
        public MatchDayInsights GetMatchDayInsights(TeamId teamId, MatchSituation situation, 
            TacticalGamePlan currentGamePlan)
        {
            var assistants = GetTeamAssistants(teamId);
            var insights = new MatchDayInsights();

            foreach (var assistant in assistants)
            {
                var assistantInsights = GenerateSpecializedInsights(assistant, situation, currentGamePlan);
                CombineInsights(insights, assistantInsights);
                
                // Track match contributions
                if (_performanceTracking.TryGetValue(assistant.Id, out var performance))
                {
                    performance.RecordMatchContribution(assistantInsights.InsightQuality);
                }
            }

            return insights;
        }

        /// <summary>
        /// Calculate player development bonuses from specialized coaches
        /// </summary>
        public PlayerDevelopmentBonuses CalculateDevelopmentBonuses(TeamId teamId, 
            List<PlayerTrainingData> players)
        {
            var assistants = GetTeamAssistants(teamId);
            var bonuses = new PlayerDevelopmentBonuses();

            foreach (var assistant in assistants)
            {
                var assistantBonuses = CalculateSpecializedDevelopmentBonuses(assistant, players);
                CombineDevelopmentBonuses(bonuses, assistantBonuses);
            }

            return bonuses;
        }

        #endregion

        #region Performance Tracking

        /// <summary>
        /// Get performance report for an assistant coach
        /// </summary>
        public AssistantCoachReport GetPerformanceReport(AssistantCoachId assistantId)
        {
            if (!_performanceTracking.TryGetValue(assistantId, out var performance))
                return null;

            return new AssistantCoachReport
            {
                AssistantId = assistantId,
                TrainingEffectiveness = performance.AverageTrainingContribution,
                MatchContribution = performance.AverageMatchContribution,
                DevelopmentImpact = performance.CalculateDevelopmentImpact(),
                OverallRating = performance.CalculateOverallRating(),
                WeeksEmployed = performance.WeeksEmployed,
                Recommendations = GeneratePerformanceRecommendations(performance)
            };
        }

        /// <summary>
        /// Update assistant coach performance at end of week
        /// </summary>
        public void UpdateWeeklyPerformance(TeamId teamId)
        {
            var assistants = GetTeamAssistants(teamId);
            
            foreach (var assistant in assistants)
            {
                if (_performanceTracking.TryGetValue(assistant.Id, out var performance))
                {
                    performance.CompleteWeek();
                    
                    // Check for potential skill improvements
                    CheckSkillImprovement(assistant, performance);
                }
            }
        }

        #endregion

        #region Private Helper Methods

        private int CalculateMaxAssistants(TeamId teamId)
        {
            // In real implementation, this would check the team's league level
            // VFL/AFL teams get more assistants than local leagues
            
            // For now, assume AFL/VFL teams can have up to 4 assistants
            // Lower leagues might have 0-2
            return 4; // Maximum for top tier
        }

        private AssistantCoachContribution CalculateAssistantTrainingContribution(
            AssistantCoachProfile assistant, TrainingSession trainingSession)
        {
            var contribution = new AssistantCoachContribution();

            // Base effectiveness from assistant's skill level
            float baseEffectiveness = assistant.SkillLevel / 100f;
            
            // Specialization bonus if training matches their area
            float specializationBonus = CalculateSpecializationMatch(assistant.Specialization, trainingSession.TrainingType);
            
            // Experience modifier
            float experienceModifier = 1f + (assistant.YearsExperience * 0.02f);
            
            // Calculate overall effectiveness
            contribution.OverallEffectiveness = baseEffectiveness * (1f + specializationBonus) * experienceModifier;
            
            // Apply specialization-specific benefits
            ApplySpecializationBenefits(contribution, assistant, trainingSession);
            
            return contribution;
        }

        private float CalculateSpecializationMatch(AssistantCoachSpecialization specialization, 
            TrainingType trainingType)
        {
            return specialization switch
            {
                AssistantCoachSpecialization.ForwardCoach when IsOffensiveTraining(trainingType) => 0.5f,
                AssistantCoachSpecialization.DefensiveCoach when IsDefensiveTraining(trainingType) => 0.5f,
                AssistantCoachSpecialization.MidfielderCoach when IsMidfieldTraining(trainingType) => 0.5f,
                AssistantCoachSpecialization.FitnessCoach when IsFitnessTraining(trainingType) => 0.6f,
                AssistantCoachSpecialization.SkillsCoach when IsSkillsTraining(trainingType) => 0.4f,
                AssistantCoachSpecialization.TacticalCoach when IsTacticalTraining(trainingType) => 0.7f,
                AssistantCoachSpecialization.DevelopmentCoach when IsYouthTraining(trainingType) => 0.8f,
                AssistantCoachSpecialization.RecoveryCoach when IsRecoveryTraining(trainingType) => 0.9f,
                _ => 0f // No bonus for non-matching specializations
            };
        }

        private void ApplySpecializationBenefits(AssistantCoachContribution contribution,
            AssistantCoachProfile assistant, TrainingSession trainingSession)
        {
            switch (assistant.Specialization)
            {
                case AssistantCoachSpecialization.FitnessCoach:
                    contribution.InjuryReductionBonus = 0.15f;
                    contribution.EnduranceBonus = 0.20f;
                    break;
                    
                case AssistantCoachSpecialization.SkillsCoach:
                    contribution.SkillDevelopmentBonus = 0.25f;
                    contribution.AccuracyBonus = 0.18f;
                    break;
                    
                case AssistantCoachSpecialization.TacticalCoach:
                    contribution.TacticalAwarenessBonus = 0.30f;
                    contribution.PositioningBonus = 0.20f;
                    break;
                    
                case AssistantCoachSpecialization.DevelopmentCoach:
                    contribution.YouthDevelopmentBonus = 0.40f;
                    contribution.PotentialUnlockBonus = 0.25f;
                    break;
                    
                case AssistantCoachSpecialization.RecoveryCoach:
                    contribution.RecoverySpeedBonus = 0.35f;
                    contribution.FatigueReductionBonus = 0.25f;
                    break;
            }
        }

        private MatchDayInsights GenerateSpecializedInsights(AssistantCoachProfile assistant,
            MatchSituation situation, TacticalGamePlan currentGamePlan)
        {
            var insights = new MatchDayInsights();
            float insightQuality = assistant.SkillLevel / 100f;

            switch (assistant.Specialization)
            {
                case AssistantCoachSpecialization.TacticalCoach:
                    insights.TacticalRecommendations = GenerateTacticalRecommendations(assistant, situation);
                    insights.FormationSuggestions = GenerateFormationSuggestions(assistant, situation);
                    insights.InsightQuality = insightQuality * 1.2f; // Tactical coaches excel at insights
                    break;
                    
                case AssistantCoachSpecialization.ForwardCoach:
                    if (situation.ScoreDifferential < -6) // Behind, need to score
                    {
                        insights.AttackingStrategySuggestions = GenerateAttackingStrategies(assistant);
                        insights.InsightQuality = insightQuality;
                    }
                    break;
                    
                case AssistantCoachSpecialization.DefensiveCoach:
                    if (situation.ScoreDifferential > 6) // Ahead, need to defend
                    {
                        insights.DefensiveStrategySuggestions = GenerateDefensiveStrategies(assistant);
                        insights.InsightQuality = insightQuality;
                    }
                    break;
                    
                case AssistantCoachSpecialization.FitnessCoach:
                    insights.RotationSuggestions = GenerateRotationSuggestions(assistant, situation);
                    insights.FatigueManagementAdvice = GenerateFatigueAdvice(assistant);
                    insights.InsightQuality = insightQuality * 0.8f; // Less tactical, more physical
                    break;
            }

            return insights;
        }

        private PlayerDevelopmentBonuses CalculateSpecializedDevelopmentBonuses(
            AssistantCoachProfile assistant, List<PlayerTrainingData> players)
        {
            var bonuses = new PlayerDevelopmentBonuses();
            
            foreach (var player in players)
            {
                var playerBonus = CalculatePlayerSpecificBonus(assistant, player);
                bonuses.PlayerBonuses[player.PlayerId] = playerBonus;
            }
            
            return bonuses;
        }

        private PlayerDevelopmentBonus CalculatePlayerSpecificBonus(AssistantCoachProfile assistant,
            PlayerTrainingData player)
        {
            var bonus = new PlayerDevelopmentBonus();
            float baseBonus = assistant.SkillLevel / 200f; // 0-0.5 range

            switch (assistant.Specialization)
            {
                case AssistantCoachSpecialization.DevelopmentCoach:
                    if (player.Age <= 22) // Young players
                        bonus.OverallDevelopmentRate = baseBonus * 2f;
                    break;
                    
                case AssistantCoachSpecialization.ForwardCoach:
                    if (IsForwardPlayer(player.PrimaryRole))
                        bonus.SpecializedSkillBonus = baseBonus * 1.5f;
                    break;
                    
                case AssistantCoachSpecialization.DefensiveCoach:
                    if (IsDefensivePlayer(player.PrimaryRole))
                        bonus.SpecializedSkillBonus = baseBonus * 1.5f;
                    break;
                    
                case AssistantCoachSpecialization.MidfielderCoach:
                    if (IsMidfielderPlayer(player.PrimaryRole))
                        bonus.SpecializedSkillBonus = baseBonus * 1.5f;
                    break;
                    
                case AssistantCoachSpecialization.SkillsCoach:
                    bonus.SkillAccuracyBonus = baseBonus;
                    break;
                    
                case AssistantCoachSpecialization.FitnessCoach:
                    bonus.PhysicalAttributeBonus = baseBonus * 1.2f;
                    break;
            }
            
            return bonus;
        }

        private void CheckSkillImprovement(AssistantCoachProfile assistant, AssistantCoachPerformance performance)
        {
            // Assistant coaches can improve their skills over time based on performance
            if (performance.WeeksEmployed % 26 == 0) // Every 6 months
            {
                float improvementChance = performance.CalculateOverallRating() / 100f * 0.3f; // Max 30% chance
                
                if (_random.NextDouble() < improvementChance)
                {
                    float improvement = _random.Next(1, 4); // 1-3 point improvement
                    assistant.SkillLevel = Math.Min(100f, assistant.SkillLevel + improvement);
                    
                    CoreLogger.Log($"[AssistantCoach] {assistant.Name} improved skill level to {assistant.SkillLevel}");
                }
            }
        }

        // Training type helpers
        private bool IsOffensiveTraining(TrainingType type) => type.ToString().Contains("Forward") || type.ToString().Contains("Goal");
        private bool IsDefensiveTraining(TrainingType type) => type.ToString().Contains("Defense") || type.ToString().Contains("Tackle");
        private bool IsMidfieldTraining(TrainingType type) => type.ToString().Contains("Midfield") || type.ToString().Contains("Endurance");
        private bool IsFitnessTraining(TrainingType type) => type.ToString().Contains("Fitness") || type.ToString().Contains("Conditioning");
        private bool IsSkillsTraining(TrainingType type) => type.ToString().Contains("Skills") || type.ToString().Contains("Technique");
        private bool IsTacticalTraining(TrainingType type) => type.ToString().Contains("Tactical") || type.ToString().Contains("Strategy");
        private bool IsYouthTraining(TrainingType type) => type.ToString().Contains("Development") || type.ToString().Contains("Youth");
        private bool IsRecoveryTraining(TrainingType type) => type.ToString().Contains("Recovery") || type.ToString().Contains("Rest");

        // Player position helpers
        private bool IsForwardPlayer(Role role) => role.ToString().Contains("Forward") || role.ToString().Contains("Pocket");
        private bool IsDefensivePlayer(Role role) => role.ToString().Contains("Defender") || role.ToString().Contains("Sweeper");
        private bool IsMidfielderPlayer(Role role) => role.ToString().Contains("Midfielder") || role == Role.Ruck;

        #region Placeholder Methods (would be implemented based on specific requirements)
        
        private void ApplyTrainingContribution(TrainingBonuses bonuses, AssistantCoachContribution contribution) { }
        private List<TrainingRecommendation> GenerateSpecializedRecommendations(AssistantCoachProfile assistant, List<PlayerTrainingData> playerData) => new();
        private void CombineInsights(MatchDayInsights insights, MatchDayInsights assistantInsights) { }
        private void CombineDevelopmentBonuses(PlayerDevelopmentBonuses bonuses, PlayerDevelopmentBonuses assistantBonuses) { }
        private List<string> GeneratePerformanceRecommendations(AssistantCoachPerformance performance) => new();
        private void LogTrainingBonuses(TeamId teamId, TrainingBonuses bonuses, int assistantCount) { }
        private List<string> GenerateTacticalRecommendations(AssistantCoachProfile assistant, MatchSituation situation) => new();
        private List<string> GenerateFormationSuggestions(AssistantCoachProfile assistant, MatchSituation situation) => new();
        private List<string> GenerateAttackingStrategies(AssistantCoachProfile assistant) => new();
        private List<string> GenerateDefensiveStrategies(AssistantCoachProfile assistant) => new();
        private List<string> GenerateRotationSuggestions(AssistantCoachProfile assistant, MatchSituation situation) => new();
        private List<string> GenerateFatigueAdvice(AssistantCoachProfile assistant) => new();
        
        #endregion

        #endregion
    }
}