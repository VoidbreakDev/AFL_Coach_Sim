namespace AFLCoachSim.Core.Domain.ValueObjects
{
    /// <summary>
    /// Detailed AFL player positions for training program specialization
    /// Maps to the more general Role enum for specific training targeting
    /// </summary>
    public enum Position
    {
        // Defensive positions
        FullBack,
        BackPocket,
        HalfBack,
        
        // Midfield positions
        Centre,
        Wing,
        Rover,
        RuckRover,
        
        // Forward positions
        FullForward,
        ForwardPocket,
        HalfForward,
        
        // Specialist positions
        Ruckman,
        
        // Utility
        Utility
    }
    
    /// <summary>
    /// Utility methods for converting between Position and Role enums
    /// </summary>
    public static class PositionRoleMapper
    {
        /// <summary>
        /// Map a Position to the corresponding Role(s)
        /// </summary>
        public static Role[] GetRolesForPosition(Position position)
        {
            return position switch
            {
                Position.FullBack => new[] { Role.KPD },
                Position.BackPocket => new[] { Role.SMLB },
                Position.HalfBack => new[] { Role.HBF },
                Position.Centre => new[] { Role.MID },
                Position.Wing => new[] { Role.WING },
                Position.Rover => new[] { Role.MID },
                Position.RuckRover => new[] { Role.MID, Role.RUC },
                Position.FullForward => new[] { Role.KPF },
                Position.ForwardPocket => new[] { Role.SMLF },
                Position.HalfForward => new[] { Role.HFF },
                Position.Ruckman => new[] { Role.RUC },
                Position.Utility => new[] { Role.MID, Role.WING, Role.HBF, Role.HFF },
                _ => new[] { Role.MID } // Default fallback
            };
        }
        
        /// <summary>
        /// Get the most likely Position for a given Role
        /// </summary>
        public static Position GetPositionForRole(Role role)
        {
            return role switch
            {
                Role.KPD => Position.FullBack,
                Role.SMLB => Position.BackPocket,
                Role.HBF => Position.HalfBack,
                Role.MID => Position.Centre,
                Role.WING => Position.Wing,
                Role.KPF => Position.FullForward,
                Role.SMLF => Position.ForwardPocket,
                Role.HFF => Position.HalfForward,
                Role.RUC => Position.Ruckman,
                _ => Position.Utility // Fallback
            };
        }
    }
    
    /// <summary>
    /// Extension methods for Position enum
    /// </summary>
    public static class PositionExtensions
    {
        /// <summary>
        /// Convert Position to string representation
        /// </summary>
        public static string ToPositionString(this Position position)
        {
            return position.ToString();
        }
        
        /// <summary>
        /// Check if a collection of positions contains a specific position string
        /// </summary>
        public static bool ContainsPosition(this Position position, string positionName)
        {
            return position.ToString().Equals(positionName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
