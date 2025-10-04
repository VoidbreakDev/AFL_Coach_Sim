using System;
using System.Collections.Generic;
using UnityEngine;
using AFLCoachSim.Core.Domain.Entities;
using AFLManager.Systems.Development;

namespace AFLManager.Systems.Coach
{
    /// <summary>
    /// Static utility class providing coach skill effects on various gameplay systems
    /// </summary>
    public static class CoachSkillsEffects
    {
        #region Training Effects

        /// <summary>
        /// Apply coach skills to training session results
        /// </summary>
        public static PlayerStatsDelta ApplyTrainingEffects(CoachSkills coachSkills, PlayerStatsDelta originalDelta, CoachSkillsManager.TrainingType trainingType, bool isYouthPlayer = false)
        {
            var modifiedDelta = new PlayerStatsDelta
            {
                Kicking = originalDelta.Kicking,
                Handballing = originalDelta.Handballing,
                Tackling = originalDelta.Tackling,
                Speed = originalDelta.Speed,
                Stamina = originalDelta.Stamina,
                Knowledge = originalDelta.Knowledge,
                Playmaking = originalDelta.Playmaking
            };

            // Get base effectiveness multiplier from coach skills
            var effectivenessMultiplier = GetTrainingEffectivenessMultiplier(coachSkills, trainingType);
            
            // Apply base multiplier to all gains
            modifiedDelta.Kicking *= effectivenessMultiplier;
            modifiedDelta.Handballing *= effectivenessMultiplier;
            modifiedDelta.Tackling *= effectivenessMultiplier;
            modifiedDelta.Speed *= effectivenessMultiplier;
            modifiedDelta.Stamina *= effectivenessMultiplier;
            modifiedDelta.Knowledge *= effectivenessMultiplier;
            modifiedDelta.Playmaking *= effectivenessMultiplier;

            // Apply specific skill-based bonuses
            ApplySpecificTrainingBonuses(coachSkills, modifiedDelta, trainingType, isYouthPlayer);

            return modifiedDelta;
        }

        /// <summary>
        /// Calculate training effectiveness multiplier based on coach skills
        /// </summary>
        private static float GetTrainingEffectivenessMultiplier(CoachSkills coachSkills, CoachSkillsManager.TrainingType trainingType)
        {
            float baseMultiplier = 1.0f;
            float relevantSkillsAverage = 50f;

            switch (trainingType)
            {
                case CoachSkillsManager.TrainingType.Skills:
                    relevantSkillsAverage = (coachSkills.GetEffectiveSkill(nameof(CoachSkills.PlayerDevelopment)) +
                                           coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication))) / 2f;
                    break;

                case CoachSkillsManager.TrainingType.Tactics:
                    relevantSkillsAverage = (coachSkills.GetEffectiveSkill(nameof(CoachSkills.TacticalKnowledge)) +
                                           coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication))) / 2f;
                    break;

                case CoachSkillsManager.TrainingType.Fitness:
                    relevantSkillsAverage = (coachSkills.GetEffectiveSkill(nameof(CoachSkills.PlayerWelfare)) +
                                           coachSkills.GetEffectiveSkill(nameof(CoachSkills.PlayerDevelopment))) / 2f;
                    break;

                case CoachSkillsManager.TrainingType.Recovery:
                    relevantSkillsAverage = (coachSkills.GetEffectiveSkill(nameof(CoachSkills.PlayerWelfare)) +
                                           coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication))) / 2f;
                    break;
            }

            // Convert skill average (1-100) to multiplier (0.5 - 2.0)
            return Mathf.Lerp(0.5f, 2.0f, (relevantSkillsAverage - 1) / 99f);
        }

        /// <summary>
        /// Apply specific training bonuses based on coach expertise
        /// </summary>
        private static void ApplySpecificTrainingBonuses(CoachSkills coachSkills, PlayerStatsDelta delta, CoachSkillsManager.TrainingType trainingType, bool isYouthPlayer)
        {
            // Youth development bonus
            if (isYouthPlayer)
            {
                var youthBonus = coachSkills.GetEffectiveSkill(nameof(CoachSkills.YouthDevelopment)) / 100f; // 0-1 multiplier
                delta.Kicking += delta.Kicking * youthBonus * 0.2f;
                delta.Handballing += delta.Handballing * youthBonus * 0.2f;
                delta.Speed += delta.Speed * youthBonus * 0.15f;
                delta.Knowledge += delta.Knowledge * youthBonus * 0.25f;
            }

            // Communication bonus (helps with all training)
            var communicationBonus = (coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication)) - 50) / 100f; // -0.5 to +0.5
            if (communicationBonus > 0)
            {
                delta.Kicking += delta.Kicking * communicationBonus * 0.1f;
                delta.Handballing += delta.Handballing * communicationBonus * 0.1f;
                delta.Tackling += delta.Tackling * communicationBonus * 0.1f;
                delta.Playmaking += delta.Playmaking * communicationBonus * 0.15f;
            }

            // Player welfare reduces negative effects
            var welfareSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.PlayerWelfare));
            if (welfareSkill > 70)
            {
                // High welfare coaches prevent overtraining penalties
                var protectionFactor = (welfareSkill - 70) / 30f; // 0-1 for skills 70-100
                if (delta.Stamina < 0) delta.Stamina *= (1f - protectionFactor * 0.5f); // Reduce fatigue penalties
            }
        }

        #endregion

        #region Player Evaluation Effects

        /// <summary>
        /// Apply coach evaluation accuracy to player potential assessment
        /// </summary>
        public static PlayerPotentialAssessment ApplyEvaluationAccuracy(CoachSkills coachSkills, PlayerPotentialAssessment baseAssessment, Player player)
        {
            var evaluationSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.PlayerEvaluation));
            var dataAnalysisSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.DataAnalysis));
            
            var averageSkill = (evaluationSkill + dataAnalysisSkill) / 2f;
            
            // Higher skill = more accurate assessment
            var accuracyMultiplier = Mathf.Lerp(0.3f, 1.0f, averageSkill / 100f);
            
            return new PlayerPotentialAssessment
            {
                PlayerId = baseAssessment.PlayerId,
                EstimatedPotential = Mathf.RoundToInt(baseAssessment.EstimatedPotential * accuracyMultiplier + 
                                                     baseAssessment.ActualPotential * (1f - accuracyMultiplier)),
                ActualPotential = baseAssessment.ActualPotential,
                ConfidenceLevel = Mathf.Clamp01(accuracyMultiplier),
                AssessmentDate = DateTime.Now,
                AssessedByCoach = true
            };
        }

        /// <summary>
        /// Calculate scouting effectiveness for player discovery
        /// </summary>
        public static float GetScoutingEffectiveness(CoachSkills coachSkills)
        {
            var relevantSkills = new[]
            {
                nameof(CoachSkills.PlayerEvaluation),
                nameof(CoachSkills.DataAnalysis),
                nameof(CoachSkills.Networking),
                nameof(CoachSkills.Recruitment)
            };

            float totalSkill = 0f;
            foreach (var skillName in relevantSkills)
            {
                totalSkill += coachSkills.GetEffectiveSkill(skillName);
            }

            var averageSkill = totalSkill / relevantSkills.Length;
            
            // Convert to 0.5-2.0 effectiveness multiplier
            return Mathf.Lerp(0.5f, 2.0f, (averageSkill - 1) / 99f);
        }

        #endregion

        #region Match Day Effects

        /// <summary>
        /// Calculate tactical adjustments effectiveness during matches
        /// </summary>
        public static TacticalAdjustmentResult ApplyMatchDayTactics(CoachSkills coachSkills, TacticalSituation situation)
        {
            var tacticalKnowledge = coachSkills.GetEffectiveSkill(nameof(CoachSkills.TacticalKnowledge));
            var tacticalAdaptation = coachSkills.GetEffectiveSkill(nameof(CoachSkills.TacticalAdaptation));
            var gameDayComposure = coachSkills.GetEffectiveSkill(nameof(CoachSkills.GameDayComposure));
            var oppositionAnalysis = coachSkills.GetEffectiveSkill(nameof(CoachSkills.OppositionAnalysis));

            var averageSkill = (tacticalKnowledge + tacticalAdaptation + gameDayComposure + oppositionAnalysis) / 4f;

            return new TacticalAdjustmentResult
            {
                SuccessProbability = Mathf.Clamp01(averageSkill / 100f),
                AdjustmentSpeed = Mathf.Lerp(0.5f, 2.0f, tacticalAdaptation / 100f),
                EffectMagnitude = Mathf.Lerp(0.7f, 1.5f, tacticalKnowledge / 100f),
                PlayerAcceptance = Mathf.Lerp(0.6f, 1.0f, coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication)) / 100f)
            };
        }

        /// <summary>
        /// Calculate team morale effects from coach presence
        /// </summary>
        public static float CalculateMoraleEffect(CoachSkills coachSkills, TeamMoraleState currentMorale)
        {
            var motivationSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Motivation));
            var leadershipSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Leadership));
            var communicationSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication));

            var averageSkill = (motivationSkill + leadershipSkill + communicationSkill) / 3f;

            // Base morale effect
            var moraleBonus = (averageSkill - 50) / 50f; // -1.0 to +1.0

            // Adjust based on current morale (better coaches are more effective with struggling teams)
            if (currentMorale == TeamMoraleState.Low && averageSkill > 70)
            {
                moraleBonus *= 1.5f; // Excellent coaches can really lift struggling teams
            }
            else if (currentMorale == TeamMoraleState.High && averageSkill < 40)
            {
                moraleBonus *= 0.5f; // Poor coaches can negatively affect high-morale teams
            }

            return moraleBonus;
        }

        #endregion

        #region Recruitment and Networking Effects

        /// <summary>
        /// Calculate recruitment success probability
        /// </summary>
        public static RecruitmentResult CalculateRecruitmentSuccess(CoachSkills coachSkills, RecruitmentAttempt attempt)
        {
            var recruitmentSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Recruitment));
            var networkingSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Networking));
            var communicationSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication));
            var leadershipSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Leadership));

            var averageSkill = (recruitmentSkill + networkingSkill + communicationSkill + leadershipSkill) / 4f;

            var baseSuccessRate = attempt.BaseSuccessProbability;
            var skillBonus = (averageSkill - 50) / 100f; // -0.5 to +0.5
            
            var finalSuccessRate = Mathf.Clamp01(baseSuccessRate + skillBonus);

            return new RecruitmentResult
            {
                SuccessProbability = finalSuccessRate,
                NegotiationBonus = skillBonus * 0.2f, // Better terms
                ReputationGain = averageSkill > 75 ? 0.1f : 0.0f, // Good coaches build reputation
                TimeToComplete = Mathf.Lerp(1.5f, 0.7f, networkingSkill / 100f) // Better networking = faster deals
            };
        }

        #endregion

        #region Innovation and Adaptation Effects

        /// <summary>
        /// Calculate effectiveness of innovative training methods
        /// </summary>
        public static InnovationResult ApplyInnovativeTraining(CoachSkills coachSkills, InnovationAttempt attempt)
        {
            var innovationSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Innovation));
            var adaptabilitySkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.Adaptability));
            var dataAnalysisSkill = coachSkills.GetEffectiveSkill(nameof(CoachSkills.DataAnalysis));

            var averageSkill = (innovationSkill + adaptabilitySkill + dataAnalysisSkill) / 3f;

            var successProbability = Mathf.Clamp01((averageSkill - 30) / 70f); // Requires above-average skills
            var potentialBenefit = Mathf.Lerp(1.1f, 2.0f, innovationSkill / 100f);
            var riskFactor = Mathf.Lerp(0.8f, 0.3f, adaptabilitySkill / 100f); // Better adaptability = lower risk

            return new InnovationResult
            {
                SuccessProbability = successProbability,
                PotentialBenefit = potentialBenefit,
                RiskOfBackfire = riskFactor,
                PlayerAcceptance = Mathf.Clamp01(coachSkills.GetEffectiveSkill(nameof(CoachSkills.Communication)) / 100f)
            };
        }

        #endregion

        #region Data Classes for Effects

        public class PlayerPotentialAssessment
        {
            public int PlayerId { get; set; }
            public int EstimatedPotential { get; set; }
            public int ActualPotential { get; set; }
            public float ConfidenceLevel { get; set; }
            public DateTime AssessmentDate { get; set; }
            public bool AssessedByCoach { get; set; }
        }

        public class TacticalSituation
        {
            public float ScoreDifference { get; set; }
            public float TimeRemaining { get; set; }
            public float OpponentStrength { get; set; }
            public TeamMoraleState TeamMorale { get; set; }
        }

        public class TacticalAdjustmentResult
        {
            public float SuccessProbability { get; set; }
            public float AdjustmentSpeed { get; set; }
            public float EffectMagnitude { get; set; }
            public float PlayerAcceptance { get; set; }
        }

        public class RecruitmentAttempt
        {
            public int TargetPlayerId { get; set; }
            public float BaseSuccessProbability { get; set; }
            public float PlayerDesirability { get; set; }
            public float CompetitionLevel { get; set; }
        }

        public class RecruitmentResult
        {
            public float SuccessProbability { get; set; }
            public float NegotiationBonus { get; set; }
            public float ReputationGain { get; set; }
            public float TimeToComplete { get; set; }
        }

        public class InnovationAttempt
        {
            public string MethodName { get; set; }
            public float BaseRisk { get; set; }
            public float PotentialReward { get; set; }
        }

        public class InnovationResult
        {
            public float SuccessProbability { get; set; }
            public float PotentialBenefit { get; set; }
            public float RiskOfBackfire { get; set; }
            public float PlayerAcceptance { get; set; }
        }

        public enum TeamMoraleState
        {
            VeryLow,
            Low,
            Average,
            High,
            VeryHigh
        }

        #endregion
    }

    /// <summary>
    /// Extensions to make coach skill effects easier to use
    /// </summary>
    public static class CoachSkillsExtensions
    {
        /// <summary>
        /// Apply coach effects to a training session result
        /// </summary>
        public static PlayerStatsDelta WithCoachEffects(this PlayerStatsDelta delta, CoachSkills coachSkills, CoachSkillsManager.TrainingType trainingType, bool isYouthPlayer = false)
        {
            return CoachSkillsEffects.ApplyTrainingEffects(coachSkills, delta, trainingType, isYouthPlayer);
        }

        /// <summary>
        /// Check if coach skill is above threshold
        /// </summary>
        public static bool IsSkillAboveThreshold(this CoachSkills skills, string skillName, float threshold = 70f)
        {
            return skills.GetEffectiveSkill(skillName) >= threshold;
        }

        /// <summary>
        /// Get skill tier (1-5) for display purposes
        /// </summary>
        public static int GetSkillTier(this CoachSkills skills, string skillName)
        {
            var skillValue = skills.GetEffectiveSkill(skillName);
            return skillValue switch
            {
                >= 90 => 5, // Elite
                >= 75 => 4, // Excellent
                >= 60 => 3, // Good
                >= 40 => 2, // Average
                _ => 1      // Poor
            };
        }

        /// <summary>
        /// Get readable skill description
        /// </summary>
        public static string GetSkillDescription(this CoachSkills skills, string skillName)
        {
            var tier = skills.GetSkillTier(skillName);
            return tier switch
            {
                5 => "Elite",
                4 => "Excellent", 
                3 => "Good",
                2 => "Average",
                1 => "Poor",
                _ => "Unknown"
            };
        }
    }
}