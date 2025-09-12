using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Integration
{
    /// <summary>
    /// Bridge between Unity player management models and Core simulation models
    /// Handles position mapping and attribute conversion
    /// </summary>
    public static class PlayerModelBridge
    {
        /// <summary>
        /// Maps AFLManager PlayerRole to Core Role enum
        /// </summary>
        public static Role ToCore(PlayerRole unityRole)
        {
            switch (unityRole)
            {
                case PlayerRole.FullBack: return Role.KPD;
                case PlayerRole.FullBackFlank: return Role.SMLB;
                case PlayerRole.HalfBackFlank: return Role.HBF;
                case PlayerRole.CentreHalfBack: return Role.HBF;
                case PlayerRole.Ruck: return Role.RUC;
                case PlayerRole.Centre: return Role.MID;
                case PlayerRole.Wing: return Role.WING;
                case PlayerRole.CentreHalfForward: return Role.HFF;
                case PlayerRole.HalfForwardFlank: return Role.HFF;
                case PlayerRole.FullForwardFlank: return Role.SMLF;
                case PlayerRole.FullForward: return Role.KPF;
                default: return Role.MID; // Default fallback
            }
        }

        /// <summary>
        /// Maps Core Role to AFLManager PlayerRole enum
        /// </summary>
        public static PlayerRole ToUnity(Role coreRole)
        {
            switch (coreRole)
            {
                case Role.KPD: return PlayerRole.FullBack;
                case Role.KPF: return PlayerRole.FullForward;
                case Role.SMLF: return PlayerRole.FullForwardFlank;
                case Role.SMLB: return PlayerRole.FullBackFlank;
                case Role.MID: return PlayerRole.Centre;
                case Role.WING: return PlayerRole.Wing;
                case Role.HBF: return PlayerRole.HalfBackFlank;
                case Role.HFF: return PlayerRole.CentreHalfForward;
                case Role.RUC: return PlayerRole.Ruck;
                default: return PlayerRole.Centre; // Default fallback
            }
        }

        /// <summary>
        /// Converts AFLManager PlayerStats to Core Attributes
        /// Maps similar attributes and provides reasonable defaults for missing ones
        /// </summary>
        public static Attributes ToCore(PlayerStats unityStats)
        {
            // Base conversion with some interpretation
            var baseValue = Mathf.RoundToInt(unityStats.GetAverage());
            
            return new Attributes
            {
                // Direct mappings where possible
                Kicking = unityStats.Kicking,
                Tackling = unityStats.Tackling,
                Speed = unityStats.Speed,
                
                // Interpreted mappings
                Handball = unityStats.Handballing,
                DecisionMaking = unityStats.Playmaking,
                WorkRate = unityStats.Stamina,
                Positioning = unityStats.Knowledge,
                
                // Derived values (based on other stats + some randomness)
                Acceleration = Mathf.Clamp(unityStats.Speed + Random.Range(-5, 6), 1, 99),
                Strength = Mathf.Clamp(baseValue + Random.Range(-10, 11), 1, 99),
                Agility = Mathf.Clamp((unityStats.Speed + unityStats.Handballing) / 2 + Random.Range(-5, 6), 1, 99),
                Jump = Mathf.Clamp(baseValue + Random.Range(-8, 9), 1, 99),
                Marking = Mathf.Clamp((unityStats.Knowledge + baseValue) / 2 + Random.Range(-5, 6), 1, 99),
                Clearance = Mathf.Clamp((unityStats.Kicking + unityStats.Tackling) / 2 + Random.Range(-5, 6), 1, 99),
                RuckWork = Mathf.Clamp(baseValue + Random.Range(-8, 9), 1, 99),
                Spoiling = Mathf.Clamp((unityStats.Tackling + unityStats.Knowledge) / 2 + Random.Range(-3, 4), 1, 99),
                Composure = Mathf.Clamp((unityStats.Playmaking + unityStats.Knowledge) / 2 + Random.Range(-3, 4), 1, 99),
                Leadership = Mathf.Clamp(baseValue + Random.Range(-10, 11), 1, 99)
            };
        }

        /// <summary>
        /// Converts Core Attributes back to AFLManager PlayerStats
        /// </summary>
        public static PlayerStats ToUnity(Attributes coreAttrs)
        {
            return new PlayerStats
            {
                Kicking = coreAttrs.Kicking,
                Handballing = coreAttrs.Handball,
                Tackling = coreAttrs.Tackling,
                Speed = coreAttrs.Speed,
                Stamina = coreAttrs.WorkRate,
                Knowledge = coreAttrs.Positioning,
                Playmaking = coreAttrs.DecisionMaking
            };
        }

        /// <summary>
        /// Converts Unity Player to Core Player for match simulation
        /// </summary>
        public static AFLCoachSim.Core.Domain.Entities.Player ToCore(AFLManager.Models.Player unityPlayer, int coreId)
        {
            return new AFLCoachSim.Core.Domain.Entities.Player
            {
                Id = new PlayerId(coreId),
                Name = unityPlayer.Name,
                Age = unityPlayer.Age,
                PrimaryRole = ToCore(unityPlayer.Role),
                Attr = ToCore(unityPlayer.Stats),
                
                // Convert Unity-specific properties
                Endurance = Mathf.RoundToInt(unityPlayer.Stamina * 100),
                Durability = Mathf.Clamp(Mathf.RoundToInt(unityPlayer.Stats.GetAverage()), 30, 95),
                Discipline = Mathf.Clamp(75 + Random.Range(-15, 16), 40, 95),
                Condition = 100, // Start fresh
                Form = Random.Range(-5, 6) // Slight random form variation
            };
        }

        /// <summary>
        /// Converts Core Player back to Unity Player (for displaying simulation results)
        /// </summary>
        public static void UpdateFromCore(AFLManager.Models.Player unityPlayer, AFLCoachSim.Core.Domain.Entities.Player corePlayer)
        {
            // Update stats that might have changed during simulation
            unityPlayer.Stats = ToUnity(corePlayer.Attr);
            unityPlayer.Role = ToUnity(corePlayer.PrimaryRole);
            unityPlayer.Stamina = corePlayer.Condition / 100f;
            
            // Note: We don't update name, age, etc. as these are managed by the Unity side
        }

        /// <summary>
        /// Converts an entire Unity roster to Core roster for match simulation
        /// </summary>
        public static Dictionary<TeamId, List<AFLCoachSim.Core.Domain.Entities.Player>> ToCore(
            Dictionary<string, AFLManager.Models.Team> unityTeams)
        {
            var coreRosters = new Dictionary<TeamId, List<AFLCoachSim.Core.Domain.Entities.Player>>();
            int playerIdCounter = 1;

            foreach (var kvp in unityTeams)
            {
                var teamId = new TeamId(kvp.Key.GetHashCode()); // Simple ID mapping
                var coreRoster = new List<AFLCoachSim.Core.Domain.Entities.Player>();

                foreach (var unityPlayer in kvp.Value.Roster)
                {
                    coreRoster.Add(ToCore(unityPlayer, playerIdCounter++));
                }

                coreRosters[teamId] = coreRoster;
            }

            return coreRosters;
        }

        /// <summary>
        /// Analyzes positional balance of a Unity team roster
        /// </summary>
        public static PositionalAnalysis AnalyzeRoster(AFLManager.Models.Team team)
        {
            if (team.Roster == null || team.Roster.Count == 0)
                return new PositionalAnalysis();

            var analysis = new PositionalAnalysis();
            
            foreach (var player in team.Roster)
            {
                var coreRole = ToCore(player.Role);
                var group = PositionUtils.GetPositionGroup(coreRole);
                
                switch (group)
                {
                    case PositionGroup.Defense:
                        analysis.Defenders++;
                        break;
                    case PositionGroup.Midfield:
                        analysis.Midfielders++;
                        break;
                    case PositionGroup.Forward:
                        analysis.Forwards++;
                        break;
                    case PositionGroup.Ruck:
                        analysis.Ruckmen++;
                        break;
                    case PositionGroup.Utility:
                        analysis.Utility++;
                        break;
                }
            }

            // Calculate balance score (0-100, higher is better)
            var ideal = PositionUtils.GetIdealStructure();
            var defDiff = Mathf.Abs(analysis.Defenders - ideal.Defenders);
            var midDiff = Mathf.Abs(analysis.Midfielders - ideal.Midfielders);
            var fwdDiff = Mathf.Abs(analysis.Forwards - ideal.Forwards);
            var ruckDiff = Mathf.Abs(analysis.Ruckmen - ideal.Ruckmen);
            
            var totalDeviation = defDiff + midDiff + fwdDiff + ruckDiff;
            analysis.BalanceScore = Mathf.Clamp(100 - (totalDeviation * 5), 0, 100);

            return analysis;
        }

        /// <summary>
        /// Gets recommended role for a player based on their attributes
        /// </summary>
        public static PlayerRole GetRecommendedRole(AFLManager.Models.Player player)
        {
            var stats = player.Stats;
            
            // Calculate suitability scores for each positional group
            var defenseScore = stats.Tackling + stats.Knowledge + (stats.Stamina * 0.7f);
            var midScore = (stats.Stamina * 1.2f) + stats.Playmaking + stats.Handballing + (stats.Speed * 0.8f);
            var forwardScore = stats.Kicking + (stats.Speed * 0.9f) + stats.Playmaking;
            var ruckScore = stats.Tackling + stats.Knowledge + (100 - stats.Speed * 0.3f); // Rucks typically slower
            
            // Find the highest scoring group
            var maxScore = Mathf.Max(defenseScore, midScore, forwardScore, ruckScore);
            
            if (maxScore == defenseScore)
            {
                // Choose specific defender role based on attributes
                return stats.Tackling > 75 ? PlayerRole.FullBack : PlayerRole.HalfBackFlank;
            }
            else if (maxScore == midScore)
            {
                return stats.Speed > 70 ? PlayerRole.Wing : PlayerRole.Centre;
            }
            else if (maxScore == forwardScore)
            {
                return stats.Kicking > 75 ? PlayerRole.FullForward : PlayerRole.HalfForwardFlank;
            }
            else
            {
                return PlayerRole.Ruck;
            }
        }
    }

    /// <summary>
    /// Analysis results for roster positional balance
    /// </summary>
    public class PositionalAnalysis
    {
        public int Defenders { get; set; }
        public int Midfielders { get; set; }
        public int Forwards { get; set; }
        public int Ruckmen { get; set; }
        public int Utility { get; set; }
        public float BalanceScore { get; set; } // 0-100, higher is better balanced
        
        public int TotalPlayers => Defenders + Midfielders + Forwards + Ruckmen + Utility;
        
        public string GetBalanceDescription()
        {
            if (BalanceScore >= 90) return "Excellent Balance";
            if (BalanceScore >= 80) return "Well Balanced";
            if (BalanceScore >= 70) return "Good Balance";
            if (BalanceScore >= 60) return "Adequate Balance";
            if (BalanceScore >= 50) return "Poor Balance";
            return "Very Unbalanced";
        }
        
        public List<string> GetRecommendations()
        {
            var ideal = PositionUtils.GetIdealStructure();
            var recommendations = new List<string>();
            
            if (Defenders < ideal.Defenders - 1)
                recommendations.Add($"Need {ideal.Defenders - Defenders} more defenders");
            if (Midfielders < ideal.Midfielders - 1)
                recommendations.Add($"Need {ideal.Midfielders - Midfielders} more midfielders");
            if (Forwards < ideal.Forwards - 1)
                recommendations.Add($"Need {ideal.Forwards - Forwards} more forwards");
            if (Ruckmen < 1)
                recommendations.Add("Need at least 1 ruckman");
            
            if (recommendations.Count == 0)
                recommendations.Add("Roster has good positional balance");
            
            return recommendations;
        }
    }
}
