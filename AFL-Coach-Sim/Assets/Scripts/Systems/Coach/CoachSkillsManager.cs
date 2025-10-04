using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLCoachSim.Core.Domain.Entities;
using AFLManager.Systems.Development;
using AFLManager.Models;

namespace AFLManager.Systems.Coach
{
    /// <summary>
    /// Manages coach skill progression, training effects, and integration with gameplay systems
    /// </summary>
    public class CoachSkillsManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableSkillProgression = true;
        [SerializeField] private float experienceMultiplier = 1.0f;
        [SerializeField] private int maxSkillLevel = 100;
        [SerializeField] private int minSkillLevel = 1;

        [Header("Experience Rates")]
        [SerializeField] private int matchWinExperience = 15;
        [SerializeField] private int matchLossExperience = 8;
        [SerializeField] private int trainingSessionExperience = 3;
        [SerializeField] private int playerDevelopmentExperience = 5;

        // Events for UI and other systems
        public event Action<string, int, int> OnSkillLevelChanged; // skillName, oldLevel, newLevel
        public event Action<string, int> OnExperienceGained; // skillName, experienceGained
        public event Action<CoachSkills> OnSkillsUpdated;

        // Active coach skills
        private CoachSkills _currentCoachSkills;
        private DateTime _lastSkillUpdate = DateTime.Now;

        // Cached effects for performance
        private Dictionary<string, float> _cachedTrainingEffects = new();
        private Dictionary<string, float> _cachedDevelopmentEffects = new();

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize with default coach skills if none exist
            if (_currentCoachSkills == null)
            {
                _currentCoachSkills = new CoachSkills();
            }
        }

        private void Start()
        {
            RefreshCachedEffects();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Set the active coach skills (called when loading a save or creating a new coach)
        /// </summary>
        public void SetCoachSkills(CoachSkills coachSkills)
        {
            _currentCoachSkills = coachSkills ?? new CoachSkills();
            RefreshCachedEffects();
            OnSkillsUpdated?.Invoke(_currentCoachSkills);
        }

        /// <summary>
        /// Get the current coach skills
        /// </summary>
        public CoachSkills GetCoachSkills()
        {
            return _currentCoachSkills;
        }

        /// <summary>
        /// Add experience to specific skills after a match
        /// </summary>
        public void ProcessMatchResult(bool isWin, MatchPerformanceData performance)
        {
            if (!enableSkillProgression) return;

            var baseExperience = isWin ? matchWinExperience : matchLossExperience;
            var modifiedExperience = Mathf.RoundToInt(baseExperience * experienceMultiplier);

            // Core skills always get experience
            AwardExperience(nameof(CoachSkills.GameDayComposure), modifiedExperience);
            AwardExperience(nameof(CoachSkills.TacticalAdaptation), modifiedExperience / 2);

            // Performance-based experience
            if (performance != null)
            {
                ProcessPerformanceBasedExperience(performance, modifiedExperience);
            }

            RefreshCachedEffects();
        }

        /// <summary>
        /// Add experience after training sessions
        /// </summary>
        public void ProcessTrainingSession(TrainingType trainingType, TrainingQuality quality)
        {
            if (!enableSkillProgression) return;

            var baseExperience = trainingSessionExperience;
            var qualityMultiplier = GetQualityMultiplier(quality);
            var finalExperience = Mathf.RoundToInt(baseExperience * qualityMultiplier * experienceMultiplier);

            // Award experience based on training type
            switch (trainingType)
            {
                case TrainingType.Skills:
                    AwardExperience(nameof(CoachSkills.PlayerDevelopment), finalExperience);
                    break;
                case TrainingType.Tactics:
                    AwardExperience(nameof(CoachSkills.TacticalKnowledge), finalExperience);
                    break;
                case TrainingType.Fitness:
                    AwardExperience(nameof(CoachSkills.PlayerWelfare), finalExperience);
                    break;
                case TrainingType.Recovery:
                    AwardExperience(nameof(CoachSkills.PlayerWelfare), finalExperience);
                    AwardExperience(nameof(CoachSkills.PlayerDevelopment), finalExperience / 2);
                    break;
            }
        }

        /// <summary>
        /// Add experience for successful player development
        /// </summary>
        public void ProcessPlayerDevelopment(PlayerStatsDelta statsDelta, bool isYouthPlayer = false)
        {
            if (!enableSkillProgression) return;

            var totalDevelopment = statsDelta.GetTotalChange();
            var experience = Mathf.RoundToInt(totalDevelopment * playerDevelopmentExperience * experienceMultiplier);

            AwardExperience(nameof(CoachSkills.PlayerDevelopment), experience);

            if (isYouthPlayer)
            {
                AwardExperience(nameof(CoachSkills.YouthDevelopment), experience);
            }
        }

        /// <summary>
        /// Get the effective training bonus for player development
        /// </summary>
        public float GetTrainingEffectiveness(TrainingType trainingType)
        {
            var key = $"Training_{trainingType}";
            if (_cachedTrainingEffects.ContainsKey(key))
                return _cachedTrainingEffects[key];

            // Calculate based on relevant coach skills
            float effectiveness = 1.0f;

            switch (trainingType)
            {
                case TrainingType.Skills:
                    effectiveness = CalculateSkillBasedEffectiveness(
                        nameof(CoachSkills.PlayerDevelopment),
                        nameof(CoachSkills.Communication));
                    break;

                case TrainingType.Tactics:
                    effectiveness = CalculateSkillBasedEffectiveness(
                        nameof(CoachSkills.TacticalKnowledge),
                        nameof(CoachSkills.Communication));
                    break;

                case TrainingType.Fitness:
                    effectiveness = CalculateSkillBasedEffectiveness(
                        nameof(CoachSkills.PlayerWelfare),
                        nameof(CoachSkills.PlayerDevelopment));
                    break;

                case TrainingType.Recovery:
                    effectiveness = CalculateSkillBasedEffectiveness(
                        nameof(CoachSkills.PlayerWelfare),
                        nameof(CoachSkills.Communication));
                    break;
            }

            _cachedTrainingEffects[key] = effectiveness;
            return effectiveness;
        }

        /// <summary>
        /// Get the coach's effectiveness at evaluating player potential
        /// </summary>
        public float GetPlayerEvaluationAccuracy()
        {
            return GetSkillEffectiveness(nameof(CoachSkills.PlayerEvaluation));
        }

        /// <summary>
        /// Get the coach's recruitment effectiveness
        /// </summary>
        public float GetRecruitmentEffectiveness()
        {
            return CalculateSkillBasedEffectiveness(
                nameof(CoachSkills.Recruitment),
                nameof(CoachSkills.Networking),
                nameof(CoachSkills.Communication));
        }

        /// <summary>
        /// Get the coach's tactical preparation effectiveness
        /// </summary>
        public float GetTacticalPreparationEffectiveness()
        {
            return CalculateSkillBasedEffectiveness(
                nameof(CoachSkills.TacticalKnowledge),
                nameof(CoachSkills.OppositionAnalysis),
                nameof(CoachSkills.DataAnalysis));
        }

        /// <summary>
        /// Get the coach's team morale bonus
        /// </summary>
        public float GetMoraleBonus()
        {
            return CalculateSkillBasedEffectiveness(
                nameof(CoachSkills.Motivation),
                nameof(CoachSkills.Leadership),
                nameof(CoachSkills.Communication));
        }

        /// <summary>
        /// Manual skill adjustment for debugging or special events
        /// </summary>
        public void SetSkillLevel(string skillName, int newLevel)
        {
            var oldLevel = _currentCoachSkills.GetBaseSkillValue(skillName);
            var clampedLevel = Mathf.Clamp(newLevel, minSkillLevel, maxSkillLevel);
            
            _currentCoachSkills.SetSkillValue(skillName, clampedLevel);
            
            if (oldLevel != clampedLevel)
            {
                OnSkillLevelChanged?.Invoke(skillName, oldLevel, clampedLevel);
                RefreshCachedEffects();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Award experience to a specific skill
        /// </summary>
        private void AwardExperience(string skillName, int experience)
        {
            if (experience <= 0) return;

            var leveledUp = _currentCoachSkills.AddSkillExperience(skillName, experience);
            
            OnExperienceGained?.Invoke(skillName, experience);

            if (leveledUp)
            {
                var currentLevel = _currentCoachSkills.GetBaseSkillValue(skillName);
                OnSkillLevelChanged?.Invoke(skillName, currentLevel - 1, currentLevel);
                
                Debug.Log($"Coach skill improved: {skillName} is now level {currentLevel}!");
            }
        }

        /// <summary>
        /// Process experience based on match performance
        /// </summary>
        private void ProcessPerformanceBasedExperience(MatchPerformanceData performance, int baseExperience)
        {
            // Tactical decisions
            if (performance.TacticalChangesSuccessful)
            {
                AwardExperience(nameof(CoachSkills.TacticalAdaptation), baseExperience);
            }

            // Team morale and motivation
            if (performance.TeamMoraleHigh)
            {
                AwardExperience(nameof(CoachSkills.Motivation), baseExperience / 2);
            }

            // Youth players performed well
            if (performance.YouthPlayersExcelled)
            {
                AwardExperience(nameof(CoachSkills.YouthDevelopment), baseExperience / 2);
            }

            // Innovation and adaptation
            if (performance.UnconventionalTacticsUsed)
            {
                AwardExperience(nameof(CoachSkills.Innovation), baseExperience / 2);
            }
        }

        /// <summary>
        /// Calculate quality multiplier for training
        /// </summary>
        private float GetQualityMultiplier(TrainingQuality quality)
        {
            return quality switch
            {
                TrainingQuality.Poor => 0.5f,
                TrainingQuality.Below => 0.7f,
                TrainingQuality.Average => 1.0f,
                TrainingQuality.Good => 1.3f,
                TrainingQuality.Excellent => 1.6f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Calculate effectiveness based on multiple skills
        /// </summary>
        private float CalculateSkillBasedEffectiveness(params string[] skillNames)
        {
            if (skillNames == null || skillNames.Length == 0)
                return 1.0f;

            float totalSkill = 0f;
            int validSkills = 0;

            foreach (var skillName in skillNames)
            {
                if (!string.IsNullOrEmpty(skillName))
                {
                    totalSkill += _currentCoachSkills.GetEffectiveSkill(skillName);
                    validSkills++;
                }
            }

            if (validSkills == 0) return 1.0f;

            var averageSkill = totalSkill / validSkills;
            
            // Convert 1-100 skill to 0.5-2.0 effectiveness multiplier
            return Mathf.Lerp(0.5f, 2.0f, (averageSkill - 1) / 99f);
        }

        /// <summary>
        /// Get effectiveness for a single skill
        /// </summary>
        private float GetSkillEffectiveness(string skillName)
        {
            return CalculateSkillBasedEffectiveness(skillName);
        }

        /// <summary>
        /// Refresh all cached effects after skills change
        /// </summary>
        private void RefreshCachedEffects()
        {
            _cachedTrainingEffects.Clear();
            _cachedDevelopmentEffects.Clear();
            _lastSkillUpdate = DateTime.Now;
        }

        #endregion

        #region Data Classes

        /// <summary>
        /// Training types that can benefit from coach skills
        /// </summary>
        public enum TrainingType
        {
            Skills,     // Technical skills training
            Tactics,    // Tactical and strategic training
            Fitness,    // Physical conditioning
            Recovery    // Rest and mental preparation
        }

        /// <summary>
        /// Quality of training sessions
        /// </summary>
        public enum TrainingQuality
        {
            Poor,
            Below,
            Average,
            Good,
            Excellent
        }

        /// <summary>
        /// Performance data from matches for experience calculation
        /// </summary>
        public class MatchPerformanceData
        {
            public bool TacticalChangesSuccessful { get; set; }
            public bool TeamMoraleHigh { get; set; }
            public bool YouthPlayersExcelled { get; set; }
            public bool UnconventionalTacticsUsed { get; set; }
            public float OverallTeamPerformance { get; set; }
            public int OppositionQuality { get; set; }
        }

        #endregion

        #region Debug and Testing

        [ContextMenu("Debug: Print Current Skills")]
        private void DebugPrintCurrentSkills()
        {
            if (_currentCoachSkills == null)
            {
                Debug.Log("No coach skills loaded");
                return;
            }

            Debug.Log($"Coach Overall Rating: {_currentCoachSkills.GetOverallRating()}");
            
            var strongest = _currentCoachSkills.GetStrongestSkills(3);
            Debug.Log($"Strongest Skills: {string.Join(", ", strongest.Select(s => $"{s.SkillName}:{s.Value:F0}"))}");
            
            var weakest = _currentCoachSkills.GetWeakestSkills(3);
            Debug.Log($"Weakest Skills: {string.Join(", ", weakest.Select(s => $"{s.SkillName}:{s.Value:F0}"))}");
        }

        [ContextMenu("Debug: Award Test Experience")]
        private void DebugAwardTestExperience()
        {
            AwardExperience(nameof(CoachSkills.TacticalKnowledge), 50);
            AwardExperience(nameof(CoachSkills.PlayerDevelopment), 30);
            AwardExperience(nameof(CoachSkills.Motivation), 40);
        }

        #endregion
    }
}