using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLManager.Systems.Coach
{
    /// <summary>
    /// Manages Coach Development Points (CDP) - a currency for manually upgrading coaching skills
    /// </summary>
    public class CoachDevelopmentPoints : MonoBehaviour
    {
        [Header("Development Points Configuration")]
        [SerializeField] private int startingDevelopmentPoints = 5;
        [SerializeField] private int maxSkillLevelFromPoints = 85; // Can't reach elite (90+) through points alone
        [SerializeField] private bool allowBackstorySkillUpgrades = false; // Backstory skills upgrade passively only
        
        [Header("Point Costs")]
        [SerializeField] private DevelopmentPointCosts pointCosts = new DevelopmentPointCosts();
        
        [Header("Point Earning")]
        [SerializeField] private PointEarningRates earningRates = new PointEarningRates();

        // Events for UI updates
        public event Action<int, int> OnDevelopmentPointsChanged; // oldPoints, newPoints
        public event Action<string, int, int> OnSkillUpgraded; // skillName, oldLevel, newLevel, pointsCost
        public event Action<int> OnDevelopmentPointsEarned; // pointsEarned

        // Current state
        private int _currentDevelopmentPoints;
        private CoachSkills _coachSkills;
        private CoachBackstorySystem.CoachProfile _coachProfile;
        
        // Track manual upgrades vs backstory upgrades
        private Dictionary<string, int> _manualUpgrades = new();
        private Dictionary<string, int> _backstoryLevels = new();

        #region Unity Lifecycle

        private void Awake()
        {
            _currentDevelopmentPoints = startingDevelopmentPoints;
            InitializeManualUpgradeTracking();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Initialize the development points system with coach data
        /// </summary>
        public void Initialize(CoachSkills coachSkills, CoachBackstorySystem.CoachProfile coachProfile)
        {
            _coachSkills = coachSkills;
            _coachProfile = coachProfile;
            
            // Record initial backstory skill levels
            RecordBackstorySkillLevels();
            
            Debug.Log($"Coach Development Points initialized. Current points: {_currentDevelopmentPoints}");
        }

        /// <summary>
        /// Get current development points
        /// </summary>
        public int GetCurrentPoints()
        {
            return _currentDevelopmentPoints;
        }

        /// <summary>
        /// Award development points
        /// </summary>
        public void AwardDevelopmentPoints(int points, string reason)
        {
            if (points <= 0) return;

            var oldPoints = _currentDevelopmentPoints;
            _currentDevelopmentPoints += points;
            
            OnDevelopmentPointsChanged?.Invoke(oldPoints, _currentDevelopmentPoints);
            OnDevelopmentPointsEarned?.Invoke(points);
            
            Debug.Log($"Awarded {points} development points for: {reason}. Total: {_currentDevelopmentPoints}");
        }

        /// <summary>
        /// Attempt to upgrade a skill using development points
        /// </summary>
        public bool TryUpgradeSkill(string skillName, out string errorMessage)
        {
            errorMessage = "";
            
            if (_coachSkills == null)
            {
                errorMessage = "Coach skills not initialized";
                return false;
            }

            // Check if skill can be upgraded
            if (!CanUpgradeSkill(skillName, out errorMessage))
            {
                return false;
            }

            var currentLevel = _coachSkills.GetBaseSkillValue(skillName);
            var cost = CalculateUpgradeCost(skillName, currentLevel);
            
            if (_currentDevelopmentPoints < cost)
            {
                errorMessage = $"Not enough development points. Need {cost}, have {_currentDevelopmentPoints}";
                return false;
            }

            // Perform the upgrade
            var oldPoints = _currentDevelopmentPoints;
            _currentDevelopmentPoints -= cost;
            _coachSkills.SetSkillValue(skillName, currentLevel + 1);
            
            // Track manual upgrade
            _manualUpgrades[skillName] = _manualUpgrades.GetValueOrDefault(skillName, 0) + 1;
            
            OnDevelopmentPointsChanged?.Invoke(oldPoints, _currentDevelopmentPoints);
            OnSkillUpgraded?.Invoke(skillName, currentLevel, currentLevel + 1);
            
            Debug.Log($"Upgraded {skillName} from {currentLevel} to {currentLevel + 1} for {cost} points");
            return true;
        }

        /// <summary>
        /// Check if a skill can be upgraded and return the reason if not
        /// </summary>
        public bool CanUpgradeSkill(string skillName, out string reason)
        {
            reason = "";
            
            if (_coachSkills == null)
            {
                reason = "Coach skills not initialized";
                return false;
            }

            var currentLevel = _coachSkills.GetBaseSkillValue(skillName);
            
            // Check maximum level
            if (currentLevel >= maxSkillLevelFromPoints)
            {
                reason = $"Skill already at maximum development point level ({maxSkillLevelFromPoints}). Higher levels require experience.";
                return false;
            }

            // Check if this is a backstory-only skill
            if (!allowBackstorySkillUpgrades && IsBackstorySpecialtySkill(skillName))
            {
                reason = "This skill is improved through your coaching backstory and cannot be directly upgraded.";
                return false;
            }

            // Check if we have enough points
            var cost = CalculateUpgradeCost(skillName, currentLevel);
            if (_currentDevelopmentPoints < cost)
            {
                reason = $"Not enough development points. Need {cost}, have {_currentDevelopmentPoints}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get upgrade cost for a skill at current level
        /// </summary>
        public int GetUpgradeCost(string skillName)
        {
            if (_coachSkills == null) return 0;
            
            var currentLevel = _coachSkills.GetBaseSkillValue(skillName);
            return CalculateUpgradeCost(skillName, currentLevel);
        }

        /// <summary>
        /// Get all upgradeable skills with their costs and restrictions
        /// </summary>
        public List<SkillUpgradeInfo> GetUpgradeableSkills()
        {
            if (_coachSkills == null) return new List<SkillUpgradeInfo>();

            var upgradeableSkills = new List<SkillUpgradeInfo>();
            var allSkills = CoachSkills.GetAllSkillNames();

            foreach (var skillName in allSkills)
            {
                var currentLevel = _coachSkills.GetBaseSkillValue(skillName);
                var canUpgrade = CanUpgradeSkill(skillName, out string reason);
                var cost = canUpgrade ? CalculateUpgradeCost(skillName, currentLevel) : 0;
                var isBackstorySkill = IsBackstorySpecialtySkill(skillName);
                var manualUpgrades = _manualUpgrades.GetValueOrDefault(skillName, 0);

                upgradeableSkills.Add(new SkillUpgradeInfo
                {
                    SkillName = skillName,
                    CurrentLevel = currentLevel,
                    UpgradeCost = cost,
                    CanUpgrade = canUpgrade,
                    RestrictionReason = reason,
                    IsBackstorySkill = isBackstorySkill,
                    ManualUpgrades = manualUpgrades,
                    SkillTier = GetSkillTier(currentLevel),
                    NextTierAt = GetNextTierLevel(currentLevel)
                });
            }

            return upgradeableSkills.OrderBy(s => s.IsBackstorySkill ? 0 : 1)
                                   .ThenBy(s => s.UpgradeCost)
                                   .ToList();
        }

        /// <summary>
        /// Process season end - award development points based on performance
        /// </summary>
        public void ProcessSeasonEnd(SeasonPerformance performance)
        {
            var pointsEarned = CalculateSeasonEndPoints(performance);
            if (pointsEarned > 0)
            {
                AwardDevelopmentPoints(pointsEarned, "Season End Performance");
            }
        }

        /// <summary>
        /// Process milestone achievement - award bonus points
        /// </summary>
        public void ProcessMilestoneAchievement(MilestoneType milestoneType, int value)
        {
            var bonusPoints = milestoneType switch
            {
                MilestoneType.FirstWin => earningRates.firstWinBonus,
                MilestoneType.WinStreak => value >= 5 ? earningRates.winStreakBonus : 0,
                MilestoneType.SeasonWins => value >= 15 ? earningRates.excellentSeasonBonus : 0,
                MilestoneType.PlayerDevelopment => earningRates.playerDevelopmentMilestone,
                MilestoneType.Innovation => earningRates.innovationMilestone,
                _ => 0
            };

            if (bonusPoints > 0)
            {
                AwardDevelopmentPoints(bonusPoints, $"Milestone: {milestoneType}");
            }
        }

        /// <summary>
        /// Get development points earned per year based on experience
        /// </summary>
        public void ProcessYearlyDevelopmentPoints(int coachExperienceYears)
        {
            var yearlyPoints = earningRates.baseYearlyPoints;
            
            // Reduce points as coach gets more experienced (harder to improve)
            if (coachExperienceYears > 5)
            {
                yearlyPoints = Math.Max(1, yearlyPoints - (coachExperienceYears - 5));
            }

            AwardDevelopmentPoints(yearlyPoints, "Yearly Development Allowance");
        }

        /// <summary>
        /// Reset all manual upgrades (for testing or special events)
        /// </summary>
        [ContextMenu("Reset Manual Upgrades")]
        public void ResetManualUpgrades()
        {
            if (_coachSkills == null) return;

            // Reset skills to backstory base levels
            foreach (var skillName in CoachSkills.GetAllSkillNames())
            {
                var backstoryLevel = _backstoryLevels.GetValueOrDefault(skillName, 50);
                _coachSkills.SetSkillValue(skillName, backstoryLevel);
            }

            _manualUpgrades.Clear();
            _currentDevelopmentPoints = startingDevelopmentPoints;
            
            Debug.Log("Reset all manual skill upgrades to backstory base levels");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize tracking for manual upgrades
        /// </summary>
        private void InitializeManualUpgradeTracking()
        {
            var allSkills = CoachSkills.GetAllSkillNames();
            foreach (var skill in allSkills)
            {
                _manualUpgrades[skill] = 0;
            }
        }

        /// <summary>
        /// Record initial backstory skill levels
        /// </summary>
        private void RecordBackstorySkillLevels()
        {
            if (_coachSkills == null) return;

            var allSkills = CoachSkills.GetAllSkillNames();
            foreach (var skill in allSkills)
            {
                _backstoryLevels[skill] = _coachSkills.GetBaseSkillValue(skill);
            }
        }

        /// <summary>
        /// Check if a skill is a backstory specialty (gets passive bonuses)
        /// </summary>
        private bool IsBackstorySpecialtySkill(string skillName)
        {
            if (_coachProfile == null) return false;

            // Skills that are heavily influenced by backstory
            var backstorySpecialties = _coachProfile.Backstory switch
            {
                CoachBackstorySystem.CoachBackstory.VeteranPlayer => new[]
                {
                    nameof(CoachSkills.PlayerEvaluation),
                    nameof(CoachSkills.Leadership),
                    nameof(CoachSkills.Networking)
                },
                CoachBackstorySystem.CoachBackstory.TacticalExpert => new[]
                {
                    nameof(CoachSkills.DataAnalysis),
                    nameof(CoachSkills.OppositionAnalysis),
                    nameof(CoachSkills.TacticalKnowledge)
                },
                CoachBackstorySystem.CoachBackstory.PeoplePerson => new[]
                {
                    nameof(CoachSkills.Communication),
                    nameof(CoachSkills.Leadership),
                    nameof(CoachSkills.CommunityRelations),
                    nameof(CoachSkills.MediaManagement)
                },
                CoachBackstorySystem.CoachBackstory.GrassrootsBloomer => new[]
                {
                    nameof(CoachSkills.PlayerWelfare),
                    nameof(CoachSkills.YouthDevelopment),
                    nameof(CoachSkills.CommunityRelations)
                },
                CoachBackstorySystem.CoachBackstory.Outsider => new[]
                {
                    nameof(CoachSkills.Innovation),
                    nameof(CoachSkills.Adaptability)
                },
                _ => new string[0]
            };

            return backstorySpecialties.Contains(skillName);
        }

        /// <summary>
        /// Calculate cost to upgrade a skill from current level
        /// </summary>
        private int CalculateUpgradeCost(string skillName, int currentLevel)
        {
            // Base cost increases with level
            var baseCost = pointCosts.baseCost + (currentLevel - 50) * pointCosts.levelMultiplier;
            
            // Backstory skills cost more to upgrade manually
            if (IsBackstorySpecialtySkill(skillName))
            {
                baseCost = (int)(baseCost * pointCosts.backstorySkillMultiplier);
            }

            // Higher tiers cost significantly more
            var tierMultiplier = currentLevel switch
            {
                >= 80 => pointCosts.expertTierMultiplier,   // Expert tier (80-89)
                >= 70 => pointCosts.proficientTierMultiplier, // Proficient tier (70-79)
                >= 60 => pointCosts.competentTierMultiplier,  // Competent tier (60-69)
                _ => 1.0f // Basic tier (1-59)
            };

            return Mathf.Max(1, Mathf.RoundToInt(baseCost * tierMultiplier));
        }

        /// <summary>
        /// Calculate season end development points based on performance
        /// </summary>
        private int CalculateSeasonEndPoints(SeasonPerformance performance)
        {
            var points = earningRates.baseSeasonPoints;

            // Performance bonuses
            if (performance.WinPercentage >= 0.7f) points += earningRates.excellentSeasonBonus;
            else if (performance.WinPercentage >= 0.5f) points += earningRates.goodSeasonBonus;

            // Special achievements
            if (performance.MadeFinals) points += earningRates.finalsBonus;
            if (performance.WonChampionship) points += earningRates.championshipBonus;
            if (performance.PlayerOfYearWinner) points += earningRates.playerDevelopmentMilestone;

            // Innovation bonus
            if (performance.InnovationsTriedCount > 0) points += earningRates.innovationMilestone;

            return points;
        }

        /// <summary>
        /// Get skill tier for display
        /// </summary>
        private string GetSkillTier(int level)
        {
            return level switch
            {
                >= 90 => "Elite",
                >= 80 => "Expert", 
                >= 70 => "Proficient",
                >= 60 => "Competent",
                >= 40 => "Average",
                _ => "Developing"
            };
        }

        /// <summary>
        /// Get next tier threshold level
        /// </summary>
        private int GetNextTierLevel(int currentLevel)
        {
            return currentLevel switch
            {
                < 40 => 40,
                < 60 => 60,
                < 70 => 70,
                < 80 => 80,
                < 90 => 90,
                _ => 100
            };
        }

        #endregion

        #region Data Classes

        [Serializable]
        public class DevelopmentPointCosts
        {
            [Header("Base Costs")]
            public int baseCost = 2;
            public int levelMultiplier = 1; // Additional cost per level above 50

            [Header("Skill Type Multipliers")]
            public float backstorySkillMultiplier = 1.5f; // Backstory skills cost more

            [Header("Tier Multipliers")]
            public float competentTierMultiplier = 1.2f;   // 60-69
            public float proficientTierMultiplier = 1.5f;  // 70-79 
            public float expertTierMultiplier = 2.0f;      // 80-89
        }

        [Serializable]
        public class PointEarningRates
        {
            [Header("Regular Earning")]
            public int baseYearlyPoints = 3;
            public int baseSeasonPoints = 2;

            [Header("Performance Bonuses")]
            public int goodSeasonBonus = 1;      // 50%+ win rate
            public int excellentSeasonBonus = 3; // 70%+ win rate
            public int finalsBonus = 2;
            public int championshipBonus = 5;

            [Header("Milestone Bonuses")]
            public int firstWinBonus = 1;
            public int winStreakBonus = 2;       // 5+ wins in a row
            public int playerDevelopmentMilestone = 2;
            public int innovationMilestone = 3;
        }

        public class SkillUpgradeInfo
        {
            public string SkillName { get; set; }
            public int CurrentLevel { get; set; }
            public int UpgradeCost { get; set; }
            public bool CanUpgrade { get; set; }
            public string RestrictionReason { get; set; }
            public bool IsBackstorySkill { get; set; }
            public int ManualUpgrades { get; set; }
            public string SkillTier { get; set; }
            public int NextTierAt { get; set; }
        }

        public class SeasonPerformance
        {
            public float WinPercentage { get; set; }
            public bool MadeFinals { get; set; }
            public bool WonChampionship { get; set; }
            public bool PlayerOfYearWinner { get; set; }
            public int InnovationsTriedCount { get; set; }
        }

        public enum MilestoneType
        {
            FirstWin,
            WinStreak,
            SeasonWins,
            PlayerDevelopment,
            Innovation
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug: Add 10 Development Points")]
        private void DebugAdd10Points()
        {
            AwardDevelopmentPoints(10, "Debug Test");
        }

        [ContextMenu("Debug: Show All Upgrade Costs")]
        private void DebugShowUpgradeCosts()
        {
            var upgradeable = GetUpgradeableSkills();
            foreach (var skill in upgradeable)
            {
                Debug.Log($"{skill.SkillName}: Level {skill.CurrentLevel} → Cost {skill.UpgradeCost} " +
                         $"(Tier: {skill.SkillTier}, Backstory: {skill.IsBackstorySkill})");
            }
        }

        [ContextMenu("Debug: Simulate Excellent Season")]
        private void DebugSimulateExcellentSeason()
        {
            ProcessSeasonEnd(new SeasonPerformance
            {
                WinPercentage = 0.75f,
                MadeFinals = true,
                WonChampionship = false,
                PlayerOfYearWinner = true,
                InnovationsTriedCount = 2
            });
        }

        #endregion
    }
}