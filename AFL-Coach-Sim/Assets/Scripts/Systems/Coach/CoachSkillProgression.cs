using System;
using System.Collections.Generic;
using UnityEngine;
using AFLCoachSim.Core.Domain.Entities;
using AFLManager.Systems.Development;

namespace AFLManager.Systems.Coach
{
    /// <summary>
    /// Manages coach skill progression through various gameplay events and experiences
    /// </summary>
    public class CoachSkillProgression : MonoBehaviour
    {
        [Header("Experience Configuration")]
        [SerializeField] private float globalExperienceMultiplier = 1.0f;
        [SerializeField] private int maxSkillLevel = 100;
        [SerializeField] private bool enableSkillDecay = false;
        [SerializeField] private float skillDecayRate = 0.1f; // Skills slowly decay without use

        [Header("Experience Events")]
        [SerializeField] private ExperienceEventSettings eventSettings = new ExperienceEventSettings();

        // Events for UI and feedback
        public event Action<string, int, int> OnSkillImproved; // skillName, oldLevel, newLevel
        public event Action<string, int> OnExperienceGained; // skillName, experience
        public event Action<ProgressionMilestone> OnMilestoneReached;

        private CoachSkills _coachSkills;
        private CoachSkillsManager _skillsManager;
        private DateTime _lastProgressionUpdate;
        
        // Track activity for skill decay prevention
        private Dictionary<string, DateTime> _lastSkillUsage = new();

        #region Unity Lifecycle

        private void Awake()
        {
            _skillsManager = GetComponent<CoachSkillsManager>();
            if (_skillsManager == null)
            {
                Debug.LogError("CoachSkillProgression requires CoachSkillsManager component!");
            }
        }

        private void Start()
        {
            _lastProgressionUpdate = DateTime.Now;
            
            if (_skillsManager != null)
            {
                _coachSkills = _skillsManager.GetCoachSkills();
                InitializeSkillUsageTracking();
            }
        }

        private void Update()
        {
            // Process skill decay if enabled
            if (enableSkillDecay && Time.time % 86400f < Time.deltaTime) // Once per day
            {
                ProcessSkillDecay();
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Set the coach skills to track progression for
        /// </summary>
        public void SetCoachSkills(CoachSkills coachSkills)
        {
            _coachSkills = coachSkills;
            InitializeSkillUsageTracking();
        }

        /// <summary>
        /// Award experience for match results
        /// </summary>
        public void ProcessMatchResult(MatchResult result)
        {
            if (_coachSkills == null) return;

            var experience = CalculateMatchExperience(result);
            
            foreach (var skillExp in experience)
            {
                AwardExperience(skillExp.Key, skillExp.Value, $"Match: {result.ResultType}");
            }

            // Special milestone checks
            CheckMatchMilestones(result);
        }

        /// <summary>
        /// Award experience for training activities
        /// </summary>
        public void ProcessTrainingSession(TrainingSessionResult result)
        {
            if (_coachSkills == null) return;

            var experience = CalculateTrainingExperience(result);
            
            foreach (var skillExp in experience)
            {
                AwardExperience(skillExp.Key, skillExp.Value, $"Training: {result.TrainingType}");
            }
        }

        /// <summary>
        /// Award experience for player development outcomes
        /// </summary>
        public void ProcessPlayerDevelopment(PlayerDevelopmentResult result)
        {
            if (_coachSkills == null) return;

            var experience = CalculatePlayerDevelopmentExperience(result);
            
            foreach (var skillExp in experience)
            {
                AwardExperience(skillExp.Key, skillExp.Value, "Player Development");
            }

            // Youth development bonus
            if (result.IsYouthPlayer)
            {
                AwardExperience(nameof(CoachSkills.YouthDevelopment), 
                    Mathf.RoundToInt(result.ImprovementMagnitude * eventSettings.youthDevelopmentBonus), 
                    "Youth Development Bonus");
            }
        }

        /// <summary>
        /// Award experience for recruitment activities
        /// </summary>
        public void ProcessRecruitment(RecruitmentResult result)
        {
            if (_coachSkills == null) return;

            var baseExperience = result.WasSuccessful ? eventSettings.recruitmentSuccessExperience : eventSettings.recruitmentFailureExperience;
            
            AwardExperience(nameof(CoachSkills.Recruitment), baseExperience, "Recruitment Activity");
            AwardExperience(nameof(CoachSkills.Networking), baseExperience / 2, "Networking During Recruitment");
            
            if (result.RequiredNegotiation)
            {
                AwardExperience(nameof(CoachSkills.Communication), baseExperience / 3, "Recruitment Negotiation");
            }
        }

        /// <summary>
        /// Award experience for media and public relations activities
        /// </summary>
        public void ProcessMediaEvent(MediaEventResult result)
        {
            if (_coachSkills == null) return;

            var experience = result.Rating switch
            {
                >= 8 => eventSettings.mediaSuccessExperience,
                >= 6 => eventSettings.mediaSuccessExperience / 2,
                >= 4 => eventSettings.mediaSuccessExperience / 4,
                _ => 0
            };

            AwardExperience(nameof(CoachSkills.MediaManagement), experience, "Media Interview");
            AwardExperience(nameof(CoachSkills.Communication), experience / 2, "Public Communication");
        }

        /// <summary>
        /// Award experience for handling team conflicts
        /// </summary>
        public void ProcessConflictResolution(ConflictResolutionResult result)
        {
            if (_coachSkills == null) return;

            var experience = result.WasResolved ? eventSettings.conflictResolutionSuccessExperience : eventSettings.conflictResolutionFailureExperience;
            
            AwardExperience(nameof(CoachSkills.ConflictResolution), experience, "Conflict Resolution");
            AwardExperience(nameof(CoachSkills.Communication), experience / 2, "Team Communication");
            AwardExperience(nameof(CoachSkills.Leadership), experience / 3, "Leadership Challenge");
        }

        /// <summary>
        /// Award experience for tactical innovations or experiments
        /// </summary>
        public void ProcessTacticalInnovation(TacticalInnovationResult result)
        {
            if (_coachSkills == null) return;

            var experience = result.WasSuccessful ? eventSettings.innovationSuccessExperience : eventSettings.innovationFailureExperience;
            
            AwardExperience(nameof(CoachSkills.Innovation), experience, "Tactical Innovation");
            AwardExperience(nameof(CoachSkills.TacticalKnowledge), experience / 2, "Tactical Experimentation");
            AwardExperience(nameof(CoachSkills.Adaptability), experience / 3, "Adaptive Thinking");
        }

        /// <summary>
        /// Manual experience award for special events
        /// </summary>
        public void AwardSpecialExperience(string skillName, int experience, string reason)
        {
            if (_coachSkills == null) return;
            AwardExperience(skillName, experience, reason);
        }

        /// <summary>
        /// Get progression status for a skill
        /// </summary>
        public SkillProgressionInfo GetSkillProgression(string skillName)
        {
            if (_coachSkills == null) return new SkillProgressionInfo();

            var currentLevel = _coachSkills.GetBaseSkillValue(skillName);
            var currentExp = _coachSkills.SkillExperience.GetValueOrDefault(skillName, 0);
            var requiredExp = GetRequiredExperienceForLevel(currentLevel + 1);

            return new SkillProgressionInfo
            {
                SkillName = skillName,
                CurrentLevel = currentLevel,
                CurrentExperience = currentExp,
                RequiredExperience = requiredExp,
                ProgressPercentage = requiredExp > 0 ? (float)currentExp / requiredExp : 1.0f,
                CanLevelUp = currentExp >= requiredExp && currentLevel < maxSkillLevel
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize skill usage tracking for decay prevention
        /// </summary>
        private void InitializeSkillUsageTracking()
        {
            if (_coachSkills == null) return;

            var skillNames = CoachSkills.GetAllSkillNames();
            foreach (var skillName in skillNames)
            {
                _lastSkillUsage[skillName] = DateTime.Now;
            }
        }

        /// <summary>
        /// Award experience to a specific skill
        /// </summary>
        private void AwardExperience(string skillName, int experience, string reason)
        {
            if (experience <= 0 || _coachSkills == null) return;

            var modifiedExperience = Mathf.RoundToInt(experience * globalExperienceMultiplier);
            
            // Track skill usage
            _lastSkillUsage[skillName] = DateTime.Now;
            
            var oldLevel = _coachSkills.GetBaseSkillValue(skillName);
            var leveledUp = _coachSkills.AddSkillExperience(skillName, modifiedExperience);
            
            OnExperienceGained?.Invoke(skillName, modifiedExperience);
            
            if (leveledUp)
            {
                var newLevel = _coachSkills.GetBaseSkillValue(skillName);
                OnSkillImproved?.Invoke(skillName, oldLevel, newLevel);
                
                Debug.Log($"Coach skill improved: {skillName} {oldLevel} → {newLevel} (Reason: {reason})");
                
                // Check for milestones
                CheckSkillMilestones(skillName, newLevel);
            }
        }

        /// <summary>
        /// Calculate experience from match results
        /// </summary>
        private Dictionary<string, int> CalculateMatchExperience(MatchResult result)
        {
            var experience = new Dictionary<string, int>();
            
            var baseExp = result.ResultType switch
            {
                MatchResultType.Win => eventSettings.matchWinExperience,
                MatchResultType.Draw => eventSettings.matchDrawExperience,
                MatchResultType.Loss => eventSettings.matchLossExperience,
                _ => 0
            };

            // Core match skills
            experience[nameof(CoachSkills.GameDayComposure)] = baseExp;
            experience[nameof(CoachSkills.TacticalAdaptation)] = baseExp / 2;
            experience[nameof(CoachSkills.Leadership)] = baseExp / 3;

            // Performance-based bonuses
            if (result.TacticalChangesSuccessful)
            {
                experience[nameof(CoachSkills.TacticalAdaptation)] += baseExp / 2;
                experience[nameof(CoachSkills.TacticalKnowledge)] = baseExp / 3;
            }

            if (result.TeamMoraleHigh)
            {
                experience[nameof(CoachSkills.Motivation)] = baseExp / 2;
            }

            if (result.YouthPlayersExcelled)
            {
                experience[nameof(CoachSkills.YouthDevelopment)] = baseExp / 2;
            }

            if (result.OppositionQuality > 80) // Strong opponent
            {
                experience[nameof(CoachSkills.OppositionAnalysis)] = baseExp / 3;
            }

            return experience;
        }

        /// <summary>
        /// Calculate experience from training sessions
        /// </summary>
        private Dictionary<string, int> CalculateTrainingExperience(TrainingSessionResult result)
        {
            var experience = new Dictionary<string, int>();
            
            var baseExp = result.Quality switch
            {
                CoachSkillsManager.TrainingQuality.Poor => eventSettings.trainingExperience / 2,
                CoachSkillsManager.TrainingQuality.Below => eventSettings.trainingExperience / 2,
                CoachSkillsManager.TrainingQuality.Average => eventSettings.trainingExperience,
                CoachSkillsManager.TrainingQuality.Good => eventSettings.trainingExperience * 2,
                CoachSkillsManager.TrainingQuality.Excellent => eventSettings.trainingExperience * 3,
                _ => eventSettings.trainingExperience
            };

            switch (result.TrainingType)
            {
                case CoachSkillsManager.TrainingType.Skills:
                    experience[nameof(CoachSkills.PlayerDevelopment)] = baseExp;
                    experience[nameof(CoachSkills.Communication)] = baseExp / 3;
                    break;

                case CoachSkillsManager.TrainingType.Tactics:
                    experience[nameof(CoachSkills.TacticalKnowledge)] = baseExp;
                    experience[nameof(CoachSkills.SetPieceExpertise)] = baseExp / 2;
                    break;

                case CoachSkillsManager.TrainingType.Fitness:
                    experience[nameof(CoachSkills.PlayerWelfare)] = baseExp;
                    experience[nameof(CoachSkills.PlayerDevelopment)] = baseExp / 3;
                    break;

                case CoachSkillsManager.TrainingType.Recovery:
                    experience[nameof(CoachSkills.PlayerWelfare)] = baseExp;
                    experience[nameof(CoachSkills.Communication)] = baseExp / 3;
                    break;
            }

            return experience;
        }

        /// <summary>
        /// Calculate experience from player development outcomes
        /// </summary>
        private Dictionary<string, int> CalculatePlayerDevelopmentExperience(PlayerDevelopmentResult result)
        {
            var experience = new Dictionary<string, int>();
            
            var baseExp = Mathf.RoundToInt(result.ImprovementMagnitude * eventSettings.playerDevelopmentExperience);
            
            experience[nameof(CoachSkills.PlayerDevelopment)] = baseExp;
            
            if (result.IsYouthPlayer)
            {
                experience[nameof(CoachSkills.YouthDevelopment)] = baseExp;
            }

            if (result.RequiredSpecialAttention)
            {
                experience[nameof(CoachSkills.PlayerWelfare)] = baseExp / 2;
            }

            return experience;
        }

        /// <summary>
        /// Process skill decay for unused skills
        /// </summary>
        private void ProcessSkillDecay()
        {
            if (_coachSkills == null) return;

            var now = DateTime.Now;
            var skillNames = CoachSkills.GetAllSkillNames();

            foreach (var skillName in skillNames)
            {
                if (_lastSkillUsage.ContainsKey(skillName))
                {
                    var daysSinceUse = (now - _lastSkillUsage[skillName]).TotalDays;
                    
                    // Start decay after 30 days of non-use
                    if (daysSinceUse > 30)
                    {
                        var currentLevel = _coachSkills.GetBaseSkillValue(skillName);
                        var decayAmount = Mathf.FloorToInt((float)((daysSinceUse - 30) * skillDecayRate));
                        
                        if (decayAmount > 0)
                        {
                            var newLevel = Mathf.Max(1, currentLevel - decayAmount);
                            _coachSkills.SetSkillValue(skillName, newLevel);
                            
                            if (newLevel < currentLevel)
                            {
                                Debug.Log($"Skill decay: {skillName} reduced from {currentLevel} to {newLevel} due to lack of use");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check for skill-specific milestones
        /// </summary>
        private void CheckSkillMilestones(string skillName, int newLevel)
        {
            var milestones = new[]
            {
                new { Level = 25, Description = "Competent" },
                new { Level = 50, Description = "Proficient" },
                new { Level = 75, Description = "Expert" },
                new { Level = 90, Description = "Master" },
                new { Level = 100, Description = "Legendary" }
            };

            foreach (var milestone in milestones)
            {
                if (newLevel == milestone.Level)
                {
                    OnMilestoneReached?.Invoke(new ProgressionMilestone
                    {
                        Type = MilestoneType.SkillLevel,
                        SkillName = skillName,
                        Level = newLevel,
                        Description = $"{skillName} reached {milestone.Description} level ({newLevel})",
                        UnlockedFeature = GetUnlockedFeature(skillName, newLevel)
                    });
                    break;
                }
            }
        }

        /// <summary>
        /// Check for match-specific milestones
        /// </summary>
        private void CheckMatchMilestones(MatchResult result)
        {
            // Implementation would check for achievements like:
            // - First win, 10 wins, 100 wins
            // - Winning streak milestones
            // - Upset victories
            // - etc.
        }

        /// <summary>
        /// Get feature unlocked at specific skill level
        /// </summary>
        private string GetUnlockedFeature(string skillName, int level)
        {
            return (skillName, level) switch
            {
                (nameof(CoachSkills.TacticalKnowledge), 75) => "Advanced Tactical Options",
                (nameof(CoachSkills.PlayerDevelopment), 75) => "Accelerated Training Programs",
                (nameof(CoachSkills.YouthDevelopment), 75) => "Elite Youth Academy",
                (nameof(CoachSkills.Innovation), 75) => "Revolutionary Tactics",
                (nameof(CoachSkills.Networking), 75) => "International Scouting Network",
                _ => null
            };
        }

        /// <summary>
        /// Calculate experience required for specific level
        /// </summary>
        private int GetRequiredExperienceForLevel(int targetLevel)
        {
            if (targetLevel <= 1) return 0;
            return (int)(100 * Math.Pow(1.1, targetLevel - 50));
        }

        #endregion

        #region Data Classes

        [Serializable]
        public class ExperienceEventSettings
        {
            [Header("Match Experience")]
            public int matchWinExperience = 15;
            public int matchLossExperience = 8;
            public int matchDrawExperience = 10;

            [Header("Training Experience")]
            public int trainingExperience = 5;

            [Header("Development Experience")]
            public int playerDevelopmentExperience = 3;
            public int youthDevelopmentBonus = 2;

            [Header("Other Activities")]
            public int recruitmentSuccessExperience = 10;
            public int recruitmentFailureExperience = 3;
            public int mediaSuccessExperience = 8;
            public int conflictResolutionSuccessExperience = 12;
            public int conflictResolutionFailureExperience = 2;
            public int innovationSuccessExperience = 15;
            public int innovationFailureExperience = 5;
        }

        public class SkillProgressionInfo
        {
            public string SkillName { get; set; }
            public int CurrentLevel { get; set; }
            public int CurrentExperience { get; set; }
            public int RequiredExperience { get; set; }
            public float ProgressPercentage { get; set; }
            public bool CanLevelUp { get; set; }
        }

        public class ProgressionMilestone
        {
            public MilestoneType Type { get; set; }
            public string SkillName { get; set; }
            public int Level { get; set; }
            public string Description { get; set; }
            public string UnlockedFeature { get; set; }
        }

        public enum MilestoneType
        {
            SkillLevel,
            OverallRating,
            SpecialAchievement
        }

        // Result classes for different activities
        public class MatchResult
        {
            public MatchResultType ResultType { get; set; }
            public bool TacticalChangesSuccessful { get; set; }
            public bool TeamMoraleHigh { get; set; }
            public bool YouthPlayersExcelled { get; set; }
            public int OppositionQuality { get; set; }
            public float PerformanceRating { get; set; }
        }

        public class TrainingSessionResult
        {
            public CoachSkillsManager.TrainingType TrainingType { get; set; }
            public CoachSkillsManager.TrainingQuality Quality { get; set; }
            public float EffectivenessRating { get; set; }
        }

        public class PlayerDevelopmentResult
        {
            public int PlayerId { get; set; }
            public float ImprovementMagnitude { get; set; }
            public bool IsYouthPlayer { get; set; }
            public bool RequiredSpecialAttention { get; set; }
        }

        public class RecruitmentResult
        {
            public int PlayerId { get; set; }
            public bool WasSuccessful { get; set; }
            public bool RequiredNegotiation { get; set; }
            public float DifficultyRating { get; set; }
        }

        public class MediaEventResult
        {
            public int Rating { get; set; } // 1-10
            public string EventType { get; set; }
        }

        public class ConflictResolutionResult
        {
            public bool WasResolved { get; set; }
            public string ConflictType { get; set; }
            public float DifficultyRating { get; set; }
        }

        public class TacticalInnovationResult
        {
            public bool WasSuccessful { get; set; }
            public string InnovationType { get; set; }
            public float RiskLevel { get; set; }
        }

        public enum MatchResultType
        {
            Win,
            Draw,
            Loss
        }

        public enum TrainingQuality
        {
            Poor,
            Below,
            Average,
            Good,
            Excellent
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug: Simulate Match Win")]
        private void DebugSimulateMatchWin()
        {
            ProcessMatchResult(new MatchResult
            {
                ResultType = MatchResultType.Win,
                TacticalChangesSuccessful = true,
                TeamMoraleHigh = true,
                YouthPlayersExcelled = false,
                OppositionQuality = 75,
                PerformanceRating = 8.5f
            });
        }

        [ContextMenu("Debug: Simulate Training Session")]
        private void DebugSimulateTraining()
        {
            ProcessTrainingSession(new TrainingSessionResult
            {
                TrainingType = CoachSkillsManager.TrainingType.Skills,
                Quality = CoachSkillsManager.TrainingQuality.Good,
                EffectivenessRating = 7.8f
            });
        }

        #endregion
    }
}