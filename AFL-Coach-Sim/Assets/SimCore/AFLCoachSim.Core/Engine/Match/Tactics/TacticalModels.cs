using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Weather;

namespace AFLCoachSim.Core.Engine.Match.Tactics
{
    #region Game Plans and Strategies

    /// <summary>
    /// Complete tactical game plan for a team
    /// </summary>
    public class TacticalGamePlan
    {
        public string Name { get; set; } = "Default";
        public Formation Formation { get; set; } = new Formation();
        public OffensiveStrategy OffensiveStrategy { get; set; } = new OffensiveStrategy();
        public DefensiveStrategy DefensiveStrategy { get; set; } = new DefensiveStrategy();
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Offensive tactical strategy
    /// </summary>
    public class OffensiveStrategy
    {
        public OffensiveStyle Style { get; set; } = OffensiveStyle.Balanced;
        public float PacePreference { get; set; } = 50f; // 0-100, slow to fast
        public float RiskTolerance { get; set; } = 50f; // 0-100, conservative to risky
        public float CorridorUsage { get; set; } = 50f; // 0-100, wings to corridor
        public float ForwardPressure { get; set; } = 50f; // Forward line pressure
        public Dictionary<Phase, float> PhaseIntensity { get; set; } = new Dictionary<Phase, float>();
    }

    /// <summary>
    /// Defensive tactical strategy
    /// </summary>
    public class DefensiveStrategy
    {
        public DefensiveStyle Style { get; set; } = DefensiveStyle.Zoning;
        public float PressureIntensity { get; set; } = 50f; // 0-100
        public float Compactness { get; set; } = 50f; // How tight the defensive structure is
        public float DespersionPressureMultiplier { get; set; } = 1.2f; // Extra pressure when losing
        public float InterceptionFocus { get; set; } = 50f; // Focus on interceptions vs tackles
        public Dictionary<Phase, float> PhaseIntensity { get; set; } = new Dictionary<Phase, float>();
    }

    #endregion

    #region Formation System

    /// <summary>
    /// Team formation with player positioning and tactical roles
    /// </summary>
    public class Formation
    {
        public string Name { get; set; } = "Standard";
        public string Description { get; set; } = "";
        public int DefensivePlayers { get; set; } = 6; // Players primarily in defensive roles
        public int MidfieldPlayers { get; set; } = 8; // Players primarily in midfield roles
        public int ForwardPlayers { get; set; } = 4; // Players primarily in forward roles
        
        // Formation characteristics
        public float Width { get; set; } = 50f; // How spread the formation is
        public float Depth { get; set; } = 50f; // How much depth the formation has
        public float Flexibility { get; set; } = 50f; // How easily players can change roles
        
        // Position-specific bonuses
        public Dictionary<Role, float> RoleBonuses { get; set; } = new Dictionary<Role, float>();
        
        public float GetPositionBonus(Role role)
        {
            return RoleBonuses.GetValueOrDefault(role, 0f);
        }
    }

    /// <summary>
    /// Static library of predefined formations
    /// </summary>
    public static class FormationLibrary
    {
        private static readonly Dictionary<string, Formation> _formations = new Dictionary<string, Formation>
        {
            ["Standard"] = new Formation
            {
                Name = "Standard",
                Description = "Balanced formation with equal emphasis on all areas",
                DefensivePlayers = 6,
                MidfieldPlayers = 8,
                ForwardPlayers = 4,
                Width = 50f,
                Depth = 50f,
                Flexibility = 70f
            },
            
            ["Attacking"] = new Formation
            {
                Name = "Attacking",
                Description = "Forward-heavy formation for scoring pressure",
                DefensivePlayers = 5,
                MidfieldPlayers = 7,
                ForwardPlayers = 6,
                Width = 60f,
                Depth = 40f,
                Flexibility = 60f,
                RoleBonuses = new Dictionary<Role, float>
                {
                    [Role.KeyForward] = 0.1f,
                    [Role.SmallForward] = 0.08f,
                    [Role.ForwardPocket] = 0.12f
                }
            },
            
            ["Defensive"] = new Formation
            {
                Name = "Defensive",
                Description = "Defense-focused formation for protecting leads",
                DefensivePlayers = 8,
                MidfieldPlayers = 7,
                ForwardPlayers = 3,
                Width = 40f,
                Depth = 60f,
                Flexibility = 50f,
                RoleBonuses = new Dictionary<Role, float>
                {
                    [Role.KeyDefender] = 0.1f,
                    [Role.SmallDefender] = 0.08f,
                    [Role.Sweeper] = 0.12f
                }
            },
            
            ["Pressing"] = new Formation
            {
                Name = "Pressing",
                Description = "High-pressure formation to force turnovers",
                DefensivePlayers = 6,
                MidfieldPlayers = 9,
                ForwardPlayers = 3,
                Width = 55f,
                Depth = 45f,
                Flexibility = 80f,
                RoleBonuses = new Dictionary<Role, float>
                {
                    [Role.Midfielder] = 0.1f,
                    [Role.Tagger] = 0.15f,
                    [Role.HalfForward] = 0.08f
                }
            },
            
            ["Flooding"] = new Formation
            {
                Name = "Flooding",
                Description = "Ultra-defensive formation flooding the defensive 50",
                DefensivePlayers = 10,
                MidfieldPlayers = 6,
                ForwardPlayers = 2,
                Width = 35f,
                Depth = 70f,
                Flexibility = 40f,
                RoleBonuses = new Dictionary<Role, float>
                {
                    [Role.KeyDefender] = 0.15f,
                    [Role.SmallDefender] = 0.12f,
                    [Role.Sweeper] = 0.18f,
                    [Role.Midfielder] = -0.05f // Midfield suffers
                }
            }
        };

        public static Formation GetFormation(string name)
        {
            return _formations.GetValueOrDefault(name, _formations["Standard"]);
        }

        public static IEnumerable<string> GetFormationNames()
        {
            return _formations.Keys;
        }
    }

    /// <summary>
    /// Formation matchup bonuses and penalties
    /// </summary>
    public static class FormationMatchups
    {
        private static readonly Dictionary<string, float> _matchups = new Dictionary<string, float>
        {
            // Attacking vs other formations
            ["Attacking_Defensive"] = -0.08f, // Attacking struggles against defensive setups
            ["Attacking_Standard"] = 0.05f,   // Slight advantage vs balanced
            ["Attacking_Flooding"] = -0.12f,  // Very difficult against flooding
            ["Attacking_Pressing"] = 0.08f,   // Good against pressing (space behind)
            
            // Defensive vs other formations
            ["Defensive_Attacking"] = 0.08f,   // Good against attacking formations
            ["Defensive_Standard"] = 0.02f,    // Slight advantage
            ["Defensive_Pressing"] = -0.05f,   // Struggles with high pressure
            ["Defensive_Flooding"] = 0.0f,     // Neutral matchup
            
            // Pressing vs other formations
            ["Pressing_Attacking"] = -0.08f,   // Vulnerable to attacking formations
            ["Pressing_Defensive"] = 0.05f,    // Good at breaking down defensive teams
            ["Pressing_Standard"] = 0.06f,     // Generally effective
            ["Pressing_Flooding"] = -0.10f,    // Struggles against ultra-defensive setups
            
            // Flooding vs other formations
            ["Flooding_Attacking"] = 0.12f,    // Very effective against attacking teams
            ["Flooding_Defensive"] = 0.0f,     // Neutral
            ["Flooding_Standard"] = 0.08f,     // Good defensive advantage
            ["Flooding_Pressing"] = 0.10f      // Effective against high pressure
        };

        public static float GetMatchupBonus(string ourFormation, string opponentFormation)
        {
            string key = $"{ourFormation}_{opponentFormation}";
            return _matchups.GetValueOrDefault(key, 0f);
        }
    }

    #endregion

    #region Tactical Adjustments

    /// <summary>
    /// Types of tactical adjustments that can be made during a match
    /// </summary>
    public enum TacticalAdjustmentType
    {
        FormationChange,
        PressureIntensity,
        OffensiveStyle,
        DefensiveStructure,
        PlayerRoleSwitch,
        TempoChange
    }

    /// <summary>
    /// A tactical adjustment made during the match
    /// </summary>
    public class TacticalAdjustment
    {
        public TacticalAdjustmentType Type { get; set; }
        public string Description { get; set; } = "";
        public Formation? NewFormation { get; set; }
        public OffensiveStyle? NewOffensiveStyle { get; set; }
        public DefensiveStyle? NewDefensiveStyle { get; set; }
        public float? NewValue { get; set; } // For intensity/tempo changes
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Result of attempting a tactical adjustment
    /// </summary>
    public class TacticalAdjustmentResult
    {
        public bool Success { get; set; }
        public float EffectMagnitude { get; set; } // How much the adjustment affects performance
        public float Disruption { get; set; } // Negative impact if adjustment fails
        public int PlayerAdaptationTime { get; set; } // Time in seconds for players to adapt
        public string Message { get; set; } = "";
    }

    #endregion

    #region Strategy Enums

    /// <summary>
    /// Offensive playing styles
    /// </summary>
    public enum OffensiveStyle
    {
        Possession,   // Keep the ball, low risk
        Balanced,     // Mix of possession and direct play
        FastBreak,    // Quick, direct attacking
        Chaos,        // High risk, high reward
        Territory     // Focus on field position
    }

    /// <summary>
    /// Defensive playing styles
    /// </summary>
    public enum DefensiveStyle
    {
        Zoning,       // Zone defense, hold structure
        ManOnMan,     // Man-on-man marking
        Pressing,     // High pressure, force turnovers
        Flooding,     // Numbers behind the ball
        Sweeping      // One free defender sweeping up
    }

    #endregion

    #region Effectiveness and Modifiers

    /// <summary>
    /// Formation effectiveness in different game phases
    /// </summary>
    public class FormationEffectiveness
    {
        public float CenterBounceAdvantage { get; set; } = 0f;
        public float OpenPlayAdvantage { get; set; } = 0f;
        public float Inside50Advantage { get; set; } = 0f;
        public float KickInAdvantage { get; set; } = 0f;
        public float OverallAdvantage { get; set; } = 0f;
        
        // Additional properties for enhanced tactical integration
        public float OverallEffectiveness { get; set; } = 0f;
        public float OffensiveEffectiveness { get; set; } = 0f;
        public float DefensiveEffectiveness { get; set; } = 0f;
        public float Confidence { get; set; } = 0f;

        public float GetPhaseAdvantage(Phase phase)
        {
            return phase switch
            {
                Phase.CenterBounce => CenterBounceAdvantage,
                Phase.OpenPlay => OpenPlayAdvantage,
                Phase.Inside50 => Inside50Advantage,
                Phase.KickIn => KickInAdvantage,
                _ => OverallAdvantage
            };
        }
    }

    // Note: TacticalBalance is now defined in EnhancedTacticalModels.cs

    /// <summary>
    /// Player positioning and performance modifiers from tactical setup
    /// </summary>
    public class PositionModifier
    {
        public float PositioningBonus { get; set; } = 0f;
        public float SpeedBonus { get; set; } = 0f;
        public float TacklingBonus { get; set; } = 0f;
        public float KickingBonus { get; set; } = 0f;
        public float MarkingBonus { get; set; } = 0f;
        public float EnduranceMultiplier { get; set; } = 1.0f;

        public float GetTotalModifier()
        {
            return PositioningBonus + SpeedBonus + TacklingBonus + KickingBonus + MarkingBonus;
        }
    }

    #endregion

    #region Tactical History

    /// <summary>
    /// Historical record of tactical decisions and their outcomes
    /// </summary>
    public class TacticalHistory
    {
        private readonly List<TacticalGamePlan> _gamePlans = new List<TacticalGamePlan>();
        private readonly List<TacticalAdjustmentRecord> _adjustments = new List<TacticalAdjustmentRecord>();

        public IReadOnlyList<TacticalGamePlan> GamePlans => _gamePlans.AsReadOnly();
        public IReadOnlyList<TacticalAdjustmentRecord> Adjustments => _adjustments.AsReadOnly();

        public void AddGamePlan(TacticalGamePlan gamePlan)
        {
            _gamePlans.Add(gamePlan);
        }

        public void AddAdjustment(TacticalAdjustment adjustment, bool success, MatchSituation situation)
        {
            _adjustments.Add(new TacticalAdjustmentRecord
            {
                Adjustment = adjustment,
                Success = success,
                Situation = situation,
                Timestamp = DateTime.Now
            });
        }

        public float GetAdjustmentSuccessRate(TacticalAdjustmentType type)
        {
            var typeAdjustments = _adjustments.Where(a => a.Adjustment.Type == type).ToList();
            if (typeAdjustments.Count == 0) return 0.7f; // Default success rate

            return (float)typeAdjustments.Count(a => a.Success) / typeAdjustments.Count;
        }
    }

    /// <summary>
    /// Record of a tactical adjustment and its outcome
    /// </summary>
    public class TacticalAdjustmentRecord
    {
        public TacticalAdjustment Adjustment { get; set; } = new TacticalAdjustment();
        public bool Success { get; set; }
        public MatchSituation Situation { get; set; } = new MatchSituation();
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Match Situation

    /// <summary>
    /// Current match situation context for tactical decisions
    /// </summary>
    public class MatchSituation
    {
        public float ScoreDifferential { get; set; } // Positive if leading, negative if behind
        public float TimeRemainingPercent { get; set; } // 0.0 to 1.0
        public float PossessionTurnover { get; set; } // Rate of turnovers (0.0 to 1.0)
        public Phase CurrentPhase { get; set; }
        public float TeamMomentum { get; set; } // -1.0 to 1.0
        public AFLCoachSim.Core.Engine.Match.Weather.Weather Weather { get; set; } = AFLCoachSim.Core.Engine.Match.Weather.Weather.Clear;
        public Dictionary<string, float> TeamStats { get; set; } = new Dictionary<string, float>();
    }

    #endregion
}