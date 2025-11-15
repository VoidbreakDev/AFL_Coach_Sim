using System;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Systems.Development;
using AFLManager.Systems.Training;
using AFLCoachSim.Core.Season.Domain.Entities;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Integration system that ensures training respects player potential limits while maintaining coach foresight accuracy.
    /// This is the main integration point between your existing training system and the new potential management.
    /// </summary>
    public class PotentialAwareTrainingIntegration
    {
        #region Core Components

        private readonly PlayerPotentialManager _potentialManager;
        private readonly CoachProfile _coachProfile;

        public PotentialAwareTrainingIntegration(CoachProfile coach, PlayerPotentialManager.PotentialSystemConfig config = null)
        {
            _potentialManager = new PlayerPotentialManager(config);
            _coachProfile = coach;
        }

        #endregion

        #region Main Integration Methods

        /// <summary>
        /// This is the main method that should wrap your existing player development logic.
        /// Call this instead of directly applying training gains to players.
        /// </summary>
        public PlayerStatsDelta ProcessTrainingWithPotentialLimits(Player player, PlayerStatsDelta originalTrainingGains)
        {
            // 1. Get the player's potential profile
            var potentialProfile = _potentialManager.InitializePlayerPotential(player, _coachProfile.InsightLevel);
            
            // 2. Apply potential limits to the training gains
            var limitedGains = _potentialManager.ApplyPotentialLimits(player, originalTrainingGains);
            
            // 3. Log the difference (for debugging/balancing)
            LogTrainingAdjustments(player, originalTrainingGains, limitedGains);
            
            // 4. Apply the limited gains to the player
            limitedGains.ApplyTo(player.Stats);
            
            // 5. Update player development data
            UpdatePlayerDevelopment(player, limitedGains);
            
            return limitedGains;
        }

        /// <summary>
        /// Get the coach's assessment of a player's potential (this is what the coach foresight feature would show)
        /// </summary>
        public PlayerPotentialManager.PlayerPotentialAssessment GetCoachAssessment(Player player)
        {
            return _potentialManager.GetCoachAssessment(player, _coachProfile.InsightLevel);
        }

        /// <summary>
        /// Check if a player is approaching their visible ceiling (for UI warnings)
        /// </summary>
        public PotentialStatus CheckPotentialStatus(Player player)
        {
            var assessment = GetCoachAssessment(player);
            int gapToCeiling = assessment.PredictedCeiling - assessment.CurrentOverall;
            
            return new PotentialStatus
            {
                PlayerId = int.Parse(player.Id),
                CurrentOverall = assessment.CurrentOverall,
                PredictedCeiling = assessment.PredictedCeiling,
                PointsToReachCeiling = gapToCeiling,
                Status = gapToCeiling switch
                {
                    <= 2 => PotentialStatusLevel.NearCeiling,
                    <= 5 => PotentialStatusLevel.ApproachingCeiling,
                    <= 10 => PotentialStatusLevel.ModerateRoom,
                    _ => PotentialStatusLevel.PlentRoom
                },
                EstimatedMonthsToReachCeiling = assessment.EstimatedYearsToReachPotential * 12,
                WarningMessage = GenerateWarningMessage(gapToCeiling, assessment.Certainty),
                Recommendations = GenerateTrainingRecommendations(assessment)
            };
        }

        #endregion

        #region Coach Profile Integration

        /// <summary>
        /// Coach profile that determines foresight ability
        /// </summary>
        public class CoachProfile
        {
            public string CoachId { get; set; }
            public string Name { get; set; }
            public PlayerPotentialManager.CoachInsightLevel InsightLevel { get; set; }
            public CoachingBackground Background { get; set; }
            public int ExperienceYears { get; set; }
            public System.Collections.Generic.Dictionary<string, float> SpecialtyModifiers { get; set; } = new();
            
            /// <summary>
            /// Create coach profile based on selected coaching background
            /// </summary>
            public static CoachProfile CreateFromBackground(CoachingBackground background)
            {
                return background switch
                {
                    CoachingBackground.PlayerDevelopment => new CoachProfile
                    {
                        Background = background,
                        InsightLevel = PlayerPotentialManager.CoachInsightLevel.Good,
                        SpecialtyModifiers = new System.Collections.Generic.Dictionary<string, float>
                        {
                            ["YoungPlayerDevelopment"] = 1.5f,
                            ["PotentialIdentification"] = 1.3f,
                            ["BreakthroughPrediction"] = 1.2f
                        }
                    },
                    
                    CoachingBackground.TacticalMastermind => new CoachProfile
                    {
                        Background = background,
                        InsightLevel = PlayerPotentialManager.CoachInsightLevel.Average,
                        SpecialtyModifiers = new System.Collections.Generic.Dictionary<string, float>
                        {
                            ["TacticalDevelopment"] = 1.4f,
                            ["SystemFit"] = 1.3f,
                            ["PositionalMastery"] = 1.2f
                        }
                    },
                    
                    CoachingBackground.Motivator => new CoachProfile
                    {
                        Background = background,
                        InsightLevel = PlayerPotentialManager.CoachInsightLevel.Average,
                        SpecialtyModifiers = new System.Collections.Generic.Dictionary<string, float>
                        {
                            ["MotivationalBreakthroughs"] = 1.6f,
                            ["MentalDevelopment"] = 1.3f,
                            ["PlayerRelations"] = 1.4f
                        }
                    },
                    
                    CoachingBackground.FormierPlayer => new CoachProfile
                    {
                        Background = background,
                        InsightLevel = PlayerPotentialManager.CoachInsightLevel.Good,
                        SpecialtyModifiers = new System.Collections.Generic.Dictionary<string, float>
                        {
                            ["TechnicalSkills"] = 1.3f,
                            ["GameReading"] = 1.4f,
                            ["ExperienceSharing"] = 1.2f
                        }
                    },
                    
                    CoachingBackground.ScoutingSpecialist => new CoachProfile
                    {
                        Background = background,
                        InsightLevel = PlayerPotentialManager.CoachInsightLevel.Excellent,
                        SpecialtyModifiers = new System.Collections.Generic.Dictionary<string, float>
                        {
                            ["PotentialIdentification"] = 1.8f,
                            ["HiddenAttributes"] = 1.5f,
                            ["LongTermProjections"] = 1.4f
                        }
                    },
                    
                    CoachingBackground.LegendaryCoach => new CoachProfile
                    {
                        Background = background,
                        InsightLevel = PlayerPotentialManager.CoachInsightLevel.Legendary,
                        SpecialtyModifiers = new System.Collections.Generic.Dictionary<string, float>
                        {
                            ["AllAspects"] = 1.3f,
                            ["Intuition"] = 1.5f,
                            ["LegacyWisdom"] = 1.4f
                        }
                    },
                    
                    _ => new CoachProfile
                    {
                        Background = CoachingBackground.Generic,
                        InsightLevel = PlayerPotentialManager.CoachInsightLevel.Average
                    }
                };
            }
        }

        /// <summary>
        /// Coaching backgrounds that affect foresight ability
        /// </summary>
        public enum CoachingBackground
        {
            Generic,                // Standard coaching ability
            PlayerDevelopment,      // Specializes in developing young talent
            TacticalMastermind,     // Great tactical coach, average at spotting potential
            Motivator,             // Excellent at getting best out of players
            FormierPlayer,         // Former elite player with good game knowledge
            ScoutingSpecialist,    // Exceptional eye for talent identification
            LegendaryCoach         // Hall of fame coach with legendary insight
        }

        #endregion

        #region Status Tracking

        /// <summary>
        /// Current potential status for a player
        /// </summary>
        public class PotentialStatus
        {
            public int PlayerId { get; set; }
            public int CurrentOverall { get; set; }
            public int PredictedCeiling { get; set; }
            public int PointsToReachCeiling { get; set; }
            public PotentialStatusLevel Status { get; set; }
            public int EstimatedMonthsToReachCeiling { get; set; }
            public string WarningMessage { get; set; }
            public System.Collections.Generic.List<string> Recommendations { get; set; } = new();
        }

        /// <summary>
        /// Potential status levels
        /// </summary>
        public enum PotentialStatusLevel
        {
            PlentRoom,              // Player has lots of room to grow
            ModerateRoom,           // Player has moderate room to grow  
            ApproachingCeiling,     // Player is getting close to ceiling
            NearCeiling,           // Player is very close to ceiling
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Log training adjustments for balancing purposes
        /// </summary>
        private void LogTrainingAdjustments(Player player, PlayerStatsDelta original, PlayerStatsDelta limited)
        {
            float originalTotal = original.GetTotalChange();
            float limitedTotal = limited.GetTotalChange();
            
            if (Math.Abs(originalTotal - limitedTotal) > 0.1f)
            {
                float reductionPercentage = ((originalTotal - limitedTotal) / originalTotal) * 100f;
                Debug.Log($"Player {player.Name} training gains reduced by {reductionPercentage:F1}% due to potential limits. " +
                         $"Original: {originalTotal:F2}, Limited: {limitedTotal:F2}");
            }
        }

        /// <summary>
        /// Update player development tracking
        /// </summary>
        private void UpdatePlayerDevelopment(Player player, PlayerStatsDelta gains)
        {
            if (player.Development != null)
            {
                // Update existing development system
                player.Development.ExperiencePoints += gains.GetTotalChange() * 10f;
                
                // Adjust momentum based on gains
                if (gains.GetTotalChange() > 1.0f)
                    player.Development.DevelopmentMomentum = Math.Min(2.0f, player.Development.DevelopmentMomentum + 0.1f);
                else if (gains.GetTotalChange() < 0.5f)
                    player.Development.DevelopmentMomentum = Math.Max(0.5f, player.Development.DevelopmentMomentum - 0.05f);
            }
        }

        /// <summary>
        /// Generate warning message based on potential status
        /// </summary>
        private string GenerateWarningMessage(int pointsToCeiling, float certainty)
        {
            string certaintyText = certainty switch
            {
                >= 0.8f => "very confident",
                >= 0.6f => "confident", 
                >= 0.4f => "moderately certain",
                _ => "uncertain"
            };

            return pointsToCeiling switch
            {
                <= 2 => $"This player appears to have reached their ceiling. I'm {certaintyText} about this assessment.",
                <= 5 => $"This player is approaching their potential limit. I'm {certaintyText} they have {pointsToCeiling} points of growth left.",
                <= 10 => $"This player has moderate room for growth. I'm {certaintyText} they can improve by roughly {pointsToCeiling} points.",
                _ => $"This player has significant potential remaining. I'm {certaintyText} about their ability to develop further."
            };
        }

        /// <summary>
        /// Generate training recommendations based on potential status
        /// </summary>
        private System.Collections.Generic.List<string> GenerateTrainingRecommendations(PlayerPotentialManager.PlayerPotentialAssessment assessment)
        {
            var recommendations = new System.Collections.Generic.List<string>();
            
            int gapToCeiling = assessment.PredictedCeiling - assessment.CurrentOverall;
            
            if (gapToCeiling <= 3)
            {
                recommendations.Add("Focus on maintenance training to preserve current abilities");
                recommendations.Add("Consider rest and recovery to prevent burnout");
                recommendations.Add("Look for breakthrough training opportunities");
            }
            else if (gapToCeiling <= 7)
            {
                recommendations.Add("Moderate training intensity to maximize remaining potential");
                recommendations.Add("Focus on position-specific skills");
                recommendations.Add("Include mental training to unlock hidden potential");
            }
            else
            {
                recommendations.Add("Intensive development programs will be most effective");
                recommendations.Add("Focus on fundamental weaknesses");
                recommendations.Add("Consistent training routine will yield best results");
            }
            
            // Add specific recommendations based on development phase
            switch (assessment.DevelopmentPhase)
            {
                case PlayerPotentialManager.DevelopmentPhase.EarlyDevelopment:
                    recommendations.Add("Emphasize fundamental skills development");
                    break;
                case PlayerPotentialManager.DevelopmentPhase.RapidGrowth:
                    recommendations.Add("This is the optimal time for intensive training");
                    break;
                case PlayerPotentialManager.DevelopmentPhase.Consolidation:
                    recommendations.Add("Focus on refining existing skills rather than learning new ones");
                    break;
                case PlayerPotentialManager.DevelopmentPhase.PeakYears:
                    recommendations.Add("Maintain peak condition through strategic training");
                    break;
            }
            
            return recommendations;
        }

        #endregion

        #region Usage Example

        /// <summary>
        /// Example of how to integrate this with your existing training system
        /// </summary>
        public void ExampleUsage()
        {
            // 1. Create coach profile (you'd do this once at career start)
            var coach = CoachProfile.CreateFromBackground(CoachingBackground.PlayerDevelopment);
            var integration = new PotentialAwareTrainingIntegration(coach);
            
            // 2. In your existing training processing method, wrap the development logic:
            /*
            // OLD WAY (what you might currently have):
            var trainingGains = CalculateTrainingGains(player, trainingProgram);
            trainingGains.ApplyTo(player.Stats);
            
            // NEW WAY (with potential limits):
            var originalGains = CalculateTrainingGains(player, trainingProgram);
            var limitedGains = integration.ProcessTrainingWithPotentialLimits(player, originalGains);
            // Player stats are automatically updated with limits applied
            */
            
            // 3. For coach foresight feature, get assessment:
            /*
            var assessment = integration.GetCoachAssessment(player);
            ShowCoachAssessmentUI(assessment);
            */
            
            // 4. For warning players approaching their ceiling:
            /*
            var status = integration.CheckPotentialStatus(player);
            if (status.Status == PotentialStatusLevel.ApproachingCeiling)
            {
                ShowPotentialWarningUI(status);
            }
            */
        }

        #endregion
    }
}

/// <summary>
/// Example extension methods to integrate with your existing systems
/// </summary>
public static class PotentialIntegrationExtensions
{
    /// <summary>
    /// Extension method to check if a player can still develop significantly
    /// </summary>
    public static bool HasSignificantPotentialRemaining(this Player player, PotentialAwareTrainingIntegration integration)
    {
        var status = integration.CheckPotentialStatus(player);
        return status.PointsToReachCeiling > 5;
    }
    
    /// <summary>
    /// Extension method to get recommended training focus based on potential
    /// </summary>
    public static string GetRecommendedTrainingFocus(this Player player, PotentialAwareTrainingIntegration integration)
    {
        var assessment = integration.GetCoachAssessment(player);
        
        // Return training recommendation based on development phase and remaining potential
        return assessment.DevelopmentPhase switch
        {
            PlayerPotentialManager.DevelopmentPhase.EarlyDevelopment => "Fundamental Skills",
            PlayerPotentialManager.DevelopmentPhase.RapidGrowth => "Intensive Development", 
            PlayerPotentialManager.DevelopmentPhase.Consolidation => "Skill Refinement",
            PlayerPotentialManager.DevelopmentPhase.PeakYears => "Maintenance Training",
            _ => "General Development"
        };
    }
}