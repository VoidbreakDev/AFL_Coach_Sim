using System;
using System.Collections.Generic;
using System.Linq;

namespace AFLCoachSim.Core.Domain.Entities
{
    /// <summary>
    /// Represents a coach's core skills that mechanically affect gameplay systems.
    /// These skills are derived from backstory specialties and can be improved through experience.
    /// </summary>
    public sealed class CoachSkills
    {
        // Core coaching competencies (1-100 scale)
        
        // === TACTICAL SKILLS ===
        /// <summary>Ability to create effective game plans and strategies</summary>
        public int TacticalKnowledge { get; set; } = 50;
        
        /// <summary>Speed and accuracy of in-game tactical adjustments</summary>
        public int TacticalAdaptation { get; set; } = 50;
        
        /// <summary>Expertise in set plays, structures, and special situations</summary>
        public int SetPieceExpertise { get; set; } = 50;
        
        /// <summary>Ability to analyze and counter opponent strategies</summary>
        public int OppositionAnalysis { get; set; } = 50;
        
        // === PLAYER MANAGEMENT ===
        /// <summary>Accuracy in assessing player potential and development paths</summary>
        public int PlayerEvaluation { get; set; } = 50;
        
        /// <summary>Ability to develop and improve player skills effectively</summary>
        public int PlayerDevelopment { get; set; } = 50;
        
        /// <summary>Skill in maintaining team morale and motivation</summary>
        public int Motivation { get; set; } = 50;
        
        /// <summary>Managing player welfare, mental health, and preventing burnout</summary>
        public int PlayerWelfare { get; set; } = 50;
        
        // === COMMUNICATION & LEADERSHIP ===
        /// <summary>Building rapport with players and staff</summary>
        public int Communication { get; set; } = 50;
        
        /// <summary>Personal charisma and leadership presence</summary>
        public int Leadership { get; set; } = 50;
        
        /// <summary>Handling media, public relations, and external pressure</summary>
        public int MediaManagement { get; set; } = 50;
        
        /// <summary>Managing conflicts and maintaining team harmony</summary>
        public int ConflictResolution { get; set; } = 50;
        
        // === ANALYTICAL & INNOVATION ===
        /// <summary>Using data and statistics to inform decisions</summary>
        public int DataAnalysis { get; set; } = 50;
        
        /// <summary>Scouting, recruitment, and identifying talent</summary>
        public int Recruitment { get; set; } = 50;
        
        /// <summary>Innovative thinking and trying unconventional approaches</summary>
        public int Innovation { get; set; } = 50;
        
        /// <summary>Adapting to new situations and changing circumstances</summary>
        public int Adaptability { get; set; } = 50;
        
        // === SPECIALIZED AREAS ===
        /// <summary>Expertise in developing young players and academy systems</summary>
        public int YouthDevelopment { get; set; } = 50;
        
        /// <summary>Building networks and maintaining industry relationships</summary>
        public int Networking { get; set; } = 50;
        
        /// <summary>Managing game day pressures and decision making</summary>
        public int GameDayComposure { get; set; } = 50;
        
        /// <summary>Building community relationships and fan support</summary>
        public int CommunityRelations { get; set; } = 50;

        /// <summary>
        /// Experience points in different skill categories for progression tracking
        /// </summary>
        public Dictionary<string, int> SkillExperience { get; set; } = new();

        /// <summary>
        /// Modifiers from backstory, items, or temporary effects
        /// </summary>
        public Dictionary<string, float> ActiveModifiers { get; set; } = new();

        public CoachSkills()
        {
            InitializeSkillExperience();
        }

        /// <summary>
        /// Initialize experience tracking for all skills
        /// </summary>
        private void InitializeSkillExperience()
        {
            var skillNames = GetAllSkillNames();
            foreach (var skill in skillNames)
            {
                SkillExperience[skill] = 0;
            }
        }

        /// <summary>
        /// Get all skill names for dynamic iteration
        /// </summary>
        public static List<string> GetAllSkillNames()
        {
            return new List<string>
            {
                nameof(TacticalKnowledge),
                nameof(TacticalAdaptation),
                nameof(SetPieceExpertise),
                nameof(OppositionAnalysis),
                nameof(PlayerEvaluation),
                nameof(PlayerDevelopment),
                nameof(Motivation),
                nameof(PlayerWelfare),
                nameof(Communication),
                nameof(Leadership),
                nameof(MediaManagement),
                nameof(ConflictResolution),
                nameof(DataAnalysis),
                nameof(Recruitment),
                nameof(Innovation),
                nameof(Adaptability),
                nameof(YouthDevelopment),
                nameof(Networking),
                nameof(GameDayComposure),
                nameof(CommunityRelations)
            };
        }

        /// <summary>
        /// Get the effective value of a skill including modifiers
        /// </summary>
        public float GetEffectiveSkill(string skillName)
        {
            var baseValue = GetBaseSkillValue(skillName);
            var modifier = ActiveModifiers.ContainsKey(skillName) ? ActiveModifiers[skillName] : 1.0f;
            
            // Apply modifier and clamp between 1-100
            return Math.Max(1, Math.Min(100, baseValue * modifier));
        }

        /// <summary>
        /// Get the base skill value without modifiers
        /// </summary>
        public int GetBaseSkillValue(string skillName)
        {
            return skillName switch
            {
                nameof(TacticalKnowledge) => TacticalKnowledge,
                nameof(TacticalAdaptation) => TacticalAdaptation,
                nameof(SetPieceExpertise) => SetPieceExpertise,
                nameof(OppositionAnalysis) => OppositionAnalysis,
                nameof(PlayerEvaluation) => PlayerEvaluation,
                nameof(PlayerDevelopment) => PlayerDevelopment,
                nameof(Motivation) => Motivation,
                nameof(PlayerWelfare) => PlayerWelfare,
                nameof(Communication) => Communication,
                nameof(Leadership) => Leadership,
                nameof(MediaManagement) => MediaManagement,
                nameof(ConflictResolution) => ConflictResolution,
                nameof(DataAnalysis) => DataAnalysis,
                nameof(Recruitment) => Recruitment,
                nameof(Innovation) => Innovation,
                nameof(Adaptability) => Adaptability,
                nameof(YouthDevelopment) => YouthDevelopment,
                nameof(Networking) => Networking,
                nameof(GameDayComposure) => GameDayComposure,
                nameof(CommunityRelations) => CommunityRelations,
                _ => 50 // Default value
            };
        }

        /// <summary>
        /// Set a skill value with bounds checking
        /// </summary>
        public void SetSkillValue(string skillName, int value)
        {
            // Clamp value between 1-100
            var clampedValue = Math.Max(1, Math.Min(100, value));

            switch (skillName)
            {
                case nameof(TacticalKnowledge): TacticalKnowledge = clampedValue; break;
                case nameof(TacticalAdaptation): TacticalAdaptation = clampedValue; break;
                case nameof(SetPieceExpertise): SetPieceExpertise = clampedValue; break;
                case nameof(OppositionAnalysis): OppositionAnalysis = clampedValue; break;
                case nameof(PlayerEvaluation): PlayerEvaluation = clampedValue; break;
                case nameof(PlayerDevelopment): PlayerDevelopment = clampedValue; break;
                case nameof(Motivation): Motivation = clampedValue; break;
                case nameof(PlayerWelfare): PlayerWelfare = clampedValue; break;
                case nameof(Communication): Communication = clampedValue; break;
                case nameof(Leadership): Leadership = clampedValue; break;
                case nameof(MediaManagement): MediaManagement = clampedValue; break;
                case nameof(ConflictResolution): ConflictResolution = clampedValue; break;
                case nameof(DataAnalysis): DataAnalysis = clampedValue; break;
                case nameof(Recruitment): Recruitment = clampedValue; break;
                case nameof(Innovation): Innovation = clampedValue; break;
                case nameof(Adaptability): Adaptability = clampedValue; break;
                case nameof(YouthDevelopment): YouthDevelopment = clampedValue; break;
                case nameof(Networking): Networking = clampedValue; break;
                case nameof(GameDayComposure): GameDayComposure = clampedValue; break;
                case nameof(CommunityRelations): CommunityRelations = clampedValue; break;
            }
        }

        /// <summary>
        /// Add experience points to a specific skill
        /// </summary>
        public bool AddSkillExperience(string skillName, int experiencePoints)
        {
            if (!SkillExperience.ContainsKey(skillName))
                return false;

            SkillExperience[skillName] += experiencePoints;
            
            // Check if skill should level up (every 100 XP = 1 skill point, max skill 100)
            var currentSkill = GetBaseSkillValue(skillName);
            var requiredXP = GetRequiredExperienceForNextLevel(currentSkill);
            
            if (SkillExperience[skillName] >= requiredXP && currentSkill < 100)
            {
                SetSkillValue(skillName, currentSkill + 1);
                SkillExperience[skillName] -= requiredXP; // Carry over excess XP
                return true; // Skill level increased
            }
            
            return false; // No level up
        }

        /// <summary>
        /// Calculate required experience for next skill level (gets harder at higher levels)
        /// </summary>
        private int GetRequiredExperienceForNextLevel(int currentLevel)
        {
            // Exponential scaling: early levels easier, later levels much harder
            return (int)(100 * Math.Pow(1.1, currentLevel - 50));
        }

        /// <summary>
        /// Apply a temporary modifier to a skill
        /// </summary>
        public void ApplyModifier(string skillName, float modifier, TimeSpan duration = default)
        {
            ActiveModifiers[skillName] = modifier;
            
            // TODO: If duration is specified, could implement a timer system
            // For now, modifiers are applied indefinitely until removed
        }

        /// <summary>
        /// Remove a modifier from a skill
        /// </summary>
        public void RemoveModifier(string skillName)
        {
            ActiveModifiers.Remove(skillName);
        }

        /// <summary>
        /// Calculate an overall coaching rating based on all skills
        /// </summary>
        public int GetOverallRating()
        {
            var skillNames = GetAllSkillNames();
            var totalSkill = 0f;
            
            foreach (var skillName in skillNames)
            {
                totalSkill += GetEffectiveSkill(skillName);
            }
            
            return (int)(totalSkill / skillNames.Count);
        }

        /// <summary>
        /// Get the coach's strongest skill areas (top 5)
        /// </summary>
        public List<(string SkillName, float Value)> GetStrongestSkills(int count = 5)
        {
            var skills = new List<(string, float)>();
            
            foreach (var skillName in GetAllSkillNames())
            {
                skills.Add((skillName, GetEffectiveSkill(skillName)));
            }
            
            skills.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return skills.Take(count).ToList();
        }

        /// <summary>
        /// Get the coach's weakest skill areas (bottom 5)
        /// </summary>
        public List<(string SkillName, float Value)> GetWeakestSkills(int count = 5)
        {
            var skills = new List<(string, float)>();
            
            foreach (var skillName in GetAllSkillNames())
            {
                skills.Add((skillName, GetEffectiveSkill(skillName)));
            }
            
            skills.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            return skills.Take(count).ToList();
        }
    }
}