using System.Collections.Generic;
using System.Linq;

namespace AFLCoachSim.Core.Domain.ValueObjects
{
    /// <summary>
    /// Utilities for working with AFL player positions and roles
    /// </summary>
    public static class PositionUtils
    {
        /// <summary>
        /// Returns true if the role is primarily a midfielder (center bounces, clearances)
        /// </summary>
        public static bool IsMidfielder(Role role)
        {
            return role == Role.MID || role == Role.WING;
        }

        /// <summary>
        /// Returns true if the role is primarily a forward (scoring, Inside50)
        /// </summary>
        public static bool IsForward(Role role)
        {
            return role == Role.KPF || role == Role.SMLF || role == Role.HFF;
        }

        /// <summary>
        /// Returns true if the role is primarily a defender (rebounding, defense)
        /// </summary>
        public static bool IsDefender(Role role)
        {
            return role == Role.KPD || role == Role.SMLB || role == Role.HBF;
        }

        /// <summary>
        /// Returns true if the role is a ruckman (center bounces, contests)
        /// </summary>
        public static bool IsRuckman(Role role)
        {
            return role == Role.RUC;
        }

        /// <summary>
        /// Gets the positional group for tactical purposes
        /// </summary>
        public static PositionGroup GetPositionGroup(Role role)
        {
            if (IsDefender(role)) return PositionGroup.Defense;
            if (IsMidfielder(role)) return PositionGroup.Midfield;
            if (IsForward(role)) return PositionGroup.Forward;
            if (IsRuckman(role)) return PositionGroup.Ruck;
            return PositionGroup.Utility;
        }

        /// <summary>
        /// Gets players by position group from a list
        /// </summary>
        public static List<T> GetByPositionGroup<T>(IEnumerable<T> players, PositionGroup group, System.Func<T, Role> roleSelector)
        {
            return players.Where(p => GetPositionGroup(roleSelector(p)) == group).ToList();
        }

        /// <summary>
        /// Gets the ideal number of players for each position group in a 22-player team
        /// </summary>
        public static PositionStructure GetIdealStructure()
        {
            return new PositionStructure
            {
                Defenders = 7,   // 2 KP + 3 Small/Med + 2 HBF
                Midfielders = 7, // 4 MID + 3 WING  
                Forwards = 6,    // 2 KP + 2 Small + 2 HFF
                Ruckmen = 2      // 1 Primary + 1 Backup/Utility
            };
        }

        /// <summary>
        /// Gets likely players who would be involved in center bounces
        /// </summary>
        public static List<T> GetCenterBounceGroup<T>(IEnumerable<T> players, System.Func<T, Role> roleSelector)
        {
            return players.Where(p => 
            {
                var role = roleSelector(p);
                return IsMidfielder(role) || IsRuckman(role);
            }).ToList();
        }

        /// <summary>
        /// Gets likely players who would be involved in Inside50 entries
        /// </summary>
        public static List<T> GetInside50Group<T>(IEnumerable<T> players, System.Func<T, Role> roleSelector)
        {
            return players.Where(p => 
            {
                var role = roleSelector(p);
                return IsForward(role) || IsMidfielder(role);
            }).ToList();
        }

        /// <summary>
        /// Gets likely players who would be involved in defensive rebounds
        /// </summary>
        public static List<T> GetDefensiveGroup<T>(IEnumerable<T> players, System.Func<T, Role> roleSelector)
        {
            return players.Where(p => 
            {
                var role = roleSelector(p);
                return IsDefender(role) || IsMidfielder(role);
            }).ToList();
        }
    }

    /// <summary>
    /// Main positional groups in AFL
    /// </summary>
    public enum PositionGroup
    {
        Defense,
        Midfield, 
        Forward,
        Ruck,
        Utility
    }

    /// <summary>
    /// Ideal team structure for 22 players
    /// </summary>
    public sealed class PositionStructure
    {
        public int Defenders { get; set; }
        public int Midfielders { get; set; }
        public int Forwards { get; set; }
        public int Ruckmen { get; set; }
    }
}
