// File: Assets/Scripts/Systems/PlayerSkillsAdapter.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLManager.Models;

namespace AFLManager.Systems
{
    /// <summary>
    /// Adapts PlayerSkills.cs concepts to work with the existing Core domain model
    /// and Unity layer separation. Provides bridge between Core.Player and Unity PlayerData.
    /// </summary>
    public class PlayerSkillsAdapter
    {
        /// <summary>
        /// Mapping from PlayerSkills.cs attribute names to Core Attributes
        /// </summary>
        private static readonly Dictionary<string, string> SkillMapping = new Dictionary<string, string>
        {
            // Map PlayerSkills.cs names to Core attribute names
            {"kicking", "kicking"},
            {"handballing", "handball"}, 
            {"tackling", "tackling"},
            {"intelligence", "decisionmaking"},
            {"bravery", "composure"},
            {"teamwork", "leadership"},
            {"independence", "positioning"},
            {"marking", "marking"},
            {"speed", "speed"},
            {"strength", "strength"},
            {"endurance", "workrate"},
            {"leadership", "leadership"},
            {"footwork", "agility"},
            {"agility", "agility"},
            {"decisionMaking", "decisionmaking"}
        };

        /// <summary>
        /// Converts Core Player to Unity PlayerData format
        /// </summary>
        public static AFLManager.Models.Player ConvertFromCore(AFLCoachSim.Core.Domain.Entities.Player corePlayer)
        {
            var unityPlayer = new AFLManager.Models.Player
            {
                Name = corePlayer.Name,
                Age = corePlayer.Age,
                Role = MapCoreRoleToUnityRole(corePlayer.PrimaryRole),
                Stats = ConvertAttributesToPlayerStats(corePlayer.Attr),
                Morale = corePlayer.Form / 20f * 50f + 50f, // Convert -20/+20 to 0-100
                Stamina = corePlayer.Condition,
                Contract = new ContractDetails { Salary = 100000f, YearsRemaining = 2 } // Default values
            };

            return unityPlayer;
        }

        /// <summary>
        /// Updates Core Player from Unity PlayerData
        /// </summary>
        public static void UpdateCoreFromUnity(AFLCoachSim.Core.Domain.Entities.Player corePlayer, 
                                               AFLManager.Models.Player unityPlayer)
        {
            corePlayer.Name = unityPlayer.Name;
            corePlayer.Age = unityPlayer.Age;
            corePlayer.PrimaryRole = MapUnityRoleToCoreRole(unityPlayer.Role);
            
            // Update attributes from PlayerStats
            UpdateCoreAttributesFromPlayerStats(corePlayer.Attr, unityPlayer.Stats);
            
            // Convert morale back to form (-20 to +20)
            corePlayer.Form = (int)((unityPlayer.Morale - 50f) / 50f * 20f);
            corePlayer.Condition = (int)unityPlayer.Stamina;
        }

        /// <summary>
        /// Updates a specific skill in Core Player using PlayerSkills.cs naming
        /// </summary>
        public static void UpdateSkill(AFLCoachSim.Core.Domain.Entities.Player corePlayer, 
                                     string skillName, int value)
        {
            if (SkillMapping.TryGetValue(skillName.ToLower(), out string coreAttributeName))
            {
                corePlayer.Attr.UpdateAttribute(coreAttributeName, value);
            }
        }

        /// <summary>
        /// Gets a specific skill value using PlayerSkills.cs naming
        /// </summary>
        public static int GetSkill(AFLCoachSim.Core.Domain.Entities.Player corePlayer, string skillName)
        {
            if (SkillMapping.TryGetValue(skillName.ToLower(), out string coreAttributeName))
            {
                return corePlayer.Attr.GetAttribute(coreAttributeName);
            }
            return 0;
        }

        /// <summary>
        /// Calculates overall skill rating like PlayerSkills.cs but using Core attributes
        /// </summary>
        public static int CalculateOverallSkillRating(AFLCoachSim.Core.Domain.Entities.Player corePlayer)
        {
            return corePlayer.Attr.CalculateOverallRating();
        }

        /// <summary>
        /// Calculates position-specific rating
        /// </summary>
        public static int CalculatePositionRating(AFLCoachSim.Core.Domain.Entities.Player corePlayer)
        {
            return corePlayer.Attr.CalculatePositionRating(corePlayer.PrimaryRole);
        }

        #region Private Helper Methods

        public static AFLManager.Models.PlayerStats ConvertAttributesToPlayerStats(Attributes attr)
        {
            return new AFLManager.Models.PlayerStats
            {
                Kicking = attr.Kicking,
                Handballing = attr.Handball,
                Tackling = attr.Tackling,
                Speed = attr.Speed,
                Stamina = attr.WorkRate, // Map WorkRate to Stamina
                Knowledge = attr.DecisionMaking, // Map DecisionMaking to Knowledge
                Playmaking = attr.Composure // Map Composure to Playmaking
            };
        }

        private static void UpdateCoreAttributesFromPlayerStats(Attributes attr, AFLManager.Models.PlayerStats stats)
        {
            attr.Kicking = stats.Kicking;
            attr.Handball = stats.Handballing;
            attr.Tackling = stats.Tackling;
            attr.Speed = stats.Speed;
            attr.WorkRate = stats.Stamina;
            attr.DecisionMaking = stats.Knowledge;
            attr.Composure = stats.Playmaking;
        }

        private static AFLManager.Models.PlayerRole MapCoreRoleToUnityRole(Role coreRole)
        {
            switch (coreRole)
            {
                case Role.KPD: return AFLManager.Models.PlayerRole.FullBack;
                case Role.KPF: return AFLManager.Models.PlayerRole.FullForward;
                case Role.SMLF: return AFLManager.Models.PlayerRole.HalfForward;
                case Role.SMLB: return AFLManager.Models.PlayerRole.HalfBack;
                case Role.MID: return AFLManager.Models.PlayerRole.Centre;
                case Role.WING: return AFLManager.Models.PlayerRole.Wing;
                case Role.HBF: return AFLManager.Models.PlayerRole.HalfBackFlank;
                case Role.HFF: return AFLManager.Models.PlayerRole.HalfForwardFlank;
                case Role.RUC: return AFLManager.Models.PlayerRole.Ruckman;
                default: return AFLManager.Models.PlayerRole.Utility;
            }
        }

        public static Role MapUnityRoleToCoreRole(AFLManager.Models.PlayerRole unityRole)
        {
            switch (unityRole)
            {
                case AFLManager.Models.PlayerRole.FullBack: return Role.KPD;
                case AFLManager.Models.PlayerRole.FullForward: return Role.KPF;
                case AFLManager.Models.PlayerRole.HalfForward: return Role.SMLF;
                case AFLManager.Models.PlayerRole.HalfBack: return Role.SMLB;
                case AFLManager.Models.PlayerRole.Centre: return Role.MID;
                case AFLManager.Models.PlayerRole.Wing: return Role.WING;
                case AFLManager.Models.PlayerRole.HalfBackFlank: return Role.HBF;
                case AFLManager.Models.PlayerRole.HalfForwardFlank: return Role.HFF;
                case AFLManager.Models.PlayerRole.Ruckman: return Role.RUC;
                case AFLManager.Models.PlayerRole.Ruck: return Role.RUC;
                default: return Role.MID;
            }
        }

        #endregion
    }

    /// <summary>
    /// Enhanced Player management operations that respect the existing architecture
    /// while providing PlayerSkills.cs-like functionality
    /// </summary>
    public static class EnhancedPlayerOperations
    {
        /// <summary>
        /// Updates player skill using PlayerSkills.cs naming conventions
        /// </summary>
        public static void UpdatePlayerSkill(AFLManager.Models.Player player, string skillName, int value)
        {
            // Create temporary Core player for skill operations
            var corePlayer = ConvertToCorePlayer(player);
            PlayerSkillsAdapter.UpdateSkill(corePlayer, skillName, value);
            
            // Update Unity player stats from modified Core player
            player.Stats = PlayerSkillsAdapter.ConvertAttributesToPlayerStats(corePlayer.Attr);
        }

        /// <summary>
        /// Gets player skill using PlayerSkills.cs naming conventions
        /// </summary>
        public static int GetPlayerSkill(AFLManager.Models.Player player, string skillName)
        {
            var corePlayer = ConvertToCorePlayer(player);
            return PlayerSkillsAdapter.GetSkill(corePlayer, skillName);
        }

        /// <summary>
        /// Calculates overall rating like PlayerSkills.cs
        /// </summary>
        public static int CalculateOverallRating(AFLManager.Models.Player player)
        {
            var corePlayer = ConvertToCorePlayer(player);
            return PlayerSkillsAdapter.CalculateOverallSkillRating(corePlayer);
        }

        /// <summary>
        /// Gets detailed skill breakdown string similar to PlayerSkills.cs
        /// </summary>
        public static string GetPlayerSkillsBreakdown(AFLManager.Models.Player player)
        {
            var corePlayer = ConvertToCorePlayer(player);
            var attr = corePlayer.Attr;
            
            return $"Skills for {player.Name}:\n" +
                   $"Kicking: {attr.Kicking}\n" +
                   $"Handballing: {attr.Handball}\n" +
                   $"Tackling: {attr.Tackling}\n" +
                   $"Intelligence: {attr.DecisionMaking}\n" +
                   $"Bravery: {attr.Composure}\n" +
                   $"Teamwork: {attr.Leadership}\n" +
                   $"Marking: {attr.Marking}\n" +
                   $"Speed: {attr.Speed}\n" +
                   $"Strength: {attr.Strength}\n" +
                   $"Endurance: {attr.WorkRate}\n" +
                   $"Leadership: {attr.Leadership}\n" +
                   $"Footwork: {attr.Agility}\n" +
                   $"Agility: {attr.Agility}\n" +
                   $"Positioning: {attr.Positioning}\n" +
                   $"Overall Rating: {PlayerSkillsAdapter.CalculateOverallSkillRating(corePlayer)}";
        }

        private static AFLCoachSim.Core.Domain.Entities.Player ConvertToCorePlayer(AFLManager.Models.Player unityPlayer)
        {
            var corePlayer = new AFLCoachSim.Core.Domain.Entities.Player
            {
                Id = new PlayerId(1), // Temp ID
                Name = unityPlayer.Name,
                Age = unityPlayer.Age,
                PrimaryRole = PlayerSkillsAdapter.MapUnityRoleToCoreRole(unityPlayer.Role),
                Attr = new Attributes
                {
                    Kicking = unityPlayer.Stats.Kicking,
                    Handball = unityPlayer.Stats.Handballing,
                    Tackling = unityPlayer.Stats.Tackling,
                    Speed = unityPlayer.Stats.Speed,
                    WorkRate = unityPlayer.Stats.Stamina,
                    DecisionMaking = unityPlayer.Stats.Knowledge,
                    Composure = unityPlayer.Stats.Playmaking
                },
                Form = (int)((unityPlayer.Morale - 50f) / 50f * 20f),
                Condition = (int)unityPlayer.Stamina
            };
            
            return corePlayer;
        }
    }
}
