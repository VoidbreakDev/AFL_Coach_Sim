using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match.Tactics;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLCoachSim.Core.Engine.Match
{
    /// <summary>
    /// Coach profile defining personality, skills, and decision-making preferences
    /// </summary>
    public class CoachProfile
    {
        public string Name { get; set; }
        public float TacticalKnowledge { get; set; } // 0-1 scale
        public float MotivationalSkill { get; set; }
        public float RotationStrategy { get; set; } // How proactive with rotations
        public float RiskTolerance { get; set; } // Willingness to try risky tactics
        public float Adaptability { get; set; } // Speed of tactical adjustments
        public float Intensity { get; set; } // Coaching style intensity
        
        public CoachingApproach PreferredApproach { get; set; }
        public CommunicationStyle CommunicationStyle { get; set; }
        public Dictionary<string, float> SpecialistKnowledge { get; set; } // Specific tactical expertise
        
        public CoachProfile()
        {
            SpecialistKnowledge = new Dictionary<string, float>();
        }
    }
    
    /// <summary>
    /// Coaching decision with full context and execution tracking
    /// </summary>
    public class CoachingDecision
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DecisionType DecisionType { get; set; }
        public int TeamId { get; set; }
        public DateTime Timestamp { get; set; }
        public DecisionPriority Priority { get; set; }
        
        // Decision details and parameters
        public Dictionary<string, object> Details { get; set; }
        public string Reasoning { get; set; }
        public float ExpectedImpact { get; set; } // 0-1 scale
        public float Confidence { get; set; } // Coach confidence in decision
        
        // Execution tracking
        public DecisionResult ExecutionResult { get; set; }
        public DateTime? ExecutionTime { get; set; }
        public string FailureReason { get; set; }
        public float ActualImpact { get; set; } // Measured post-execution
        
        public CoachingDecision()
        {
            Details = new Dictionary<string, object>();
            Priority = DecisionPriority.Medium;
            ExecutionResult = DecisionResult.Pending;
        }
    }
    
    /// <summary>
    /// Current tactical state of a team
    /// </summary>
    public class TeamTacticalState
    {
        public int TeamId { get; set; }
        public Formation CurrentFormation { get; set; }
        public OffensiveStyle CurrentStrategy { get; set; }
        public DefensiveStyle CurrentDefensiveApproach { get; set; }
        public MatchPlan MatchPlan { get; set; }
        
        // Dynamic adjustments
        public List<TacticalAdjustment> ActiveAdjustments { get; set; }
        public Dictionary<string, PlayerRole> PlayerRoleOverrides { get; set; }
        public Dictionary<string, string> PositionOverrides { get; set; }
        
        // State tracking
        public float TacticalEffectiveness { get; set; }
        public DateTime LastMajorChange { get; set; }
        public int SubstitutionsUsed { get; set; }
        
        public TeamTacticalState()
        {
            ActiveAdjustments = new List<TacticalAdjustment>();
            PlayerRoleOverrides = new Dictionary<string, PlayerRole>();
            PositionOverrides = new Dictionary<string, string>();
        }
    }
    
    /// <summary>
    /// Pre-match planning and tactical blueprint
    /// </summary>
    public class MatchPlan
    {
        public Formation PrimaryFormation { get; set; }
        public OffensiveStyle PrimaryOffensiveStrategy { get; set; }
        public DefensiveStyle PrimaryDefensiveStrategy { get; set; }
        public PossessionStyle TargetPossessionStyle { get; set; }
        
        // Contingency planning
        public List<TacticalOption> EmergencyTactics { get; set; }
        public Dictionary<string, Formation> OpponentSpecificFormations { get; set; }
        public Dictionary<MatchSituation, TacticalResponse> SituationalResponses { get; set; }
        
        // Player-specific instructions
        public Dictionary<string, List<PlayerInstruction>> PlayerInstructions { get; set; }
        public List<PlannedSubstitution> PlannedSubstitutions { get; set; }
        
        public MatchPlan()
        {
            EmergencyTactics = new List<TacticalOption>();
            OpponentSpecificFormations = new Dictionary<string, Formation>();
            SituationalResponses = new Dictionary<MatchSituation, TacticalResponse>();
            PlayerInstructions = new Dictionary<string, List<PlayerInstruction>>();
            PlannedSubstitutions = new List<PlannedSubstitution>();
        }
    }
    
    /// <summary>
    /// Tactical adjustment with specific parameters
    /// </summary>
    public class TacticalAdjustment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TacticalAdjustmentType Type { get; set; }
        public float Intensity { get; set; } // How strongly to apply
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> AffectedPlayers { get; set; }
        public Dictionary<string, float> Parameters { get; set; }
        
        public TacticalAdjustment()
        {
            AffectedPlayers = new List<string>();
            Parameters = new Dictionary<string, float>();
        }
    }
    
    /// <summary>
    /// Team-specific match context for coaching decisions
    /// </summary>
    public class TeamMatchContext
    {
        public int TeamId { get; set; }
        public List<Player> Players { get; set; }
        public List<Player> AvailableSubstitutes { get; set; }
        
        // Match situation
        public float ScoreDifferential { get; set; }
        public float MomentumRating { get; set; }
        public float TurnoverDifferential { get; set; }
        public float ContestedPossessionDifferential { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public bool IsQuarterBreak { get; set; }
        
        // Performance metrics
        public float TeamEfficiency { get; set; }
        public float DefensiveEffectiveness { get; set; }
        public float PressureRating { get; set; }
        
        public TeamMatchContext()
        {
            Players = new List<Player>();
            AvailableSubstitutes = new List<Player>();
        }
    }
    
    /// <summary>
    /// Planned substitution with timing and conditions
    /// </summary>
    public class PlannedSubstitution
    {
        public string PlayerOut { get; set; }
        public string PlayerIn { get; set; }
        public SubstitutionTrigger Trigger { get; set; }
        public float TriggerThreshold { get; set; }
        public TimeSpan EarliestTime { get; set; }
        public TimeSpan LatestTime { get; set; }
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// Player-specific instruction during match
    /// </summary>
    public class PlayerInstruction
    {
        public string PlayerId { get; set; }
        public InstructionType Type { get; set; }
        public string Description { get; set; }
        public float Intensity { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        
        public PlayerInstruction()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Tactical response to specific match situations
    /// </summary>
    public class TacticalResponse
    {
        public string ResponseName { get; set; }
        public List<TacticalAdjustment> Adjustments { get; set; }
        public List<string> PlayerInstructions { get; set; }
        public Formation? FormationChange { get; set; }
        public float Priority { get; set; }
        
        public TacticalResponse()
        {
            Adjustments = new List<TacticalAdjustment>();
            PlayerInstructions = new List<string>();
        }
    }
    
    /// <summary>
    /// AI component for evaluating coaching decisions
    /// </summary>
    public class CoachingAI
    {
        public CoachingDecision EvaluateFormationChange(CoachProfile coach, 
            TeamTacticalState teamState, TeamMatchContext context)
        {
            // Check if formation change is warranted
            var currentEffectiveness = EvaluateFormationEffectiveness(teamState.CurrentFormation, context);
            
            if (currentEffectiveness > 0.7f) return null; // Current formation working well
            
            // Find best alternative formation
            var bestFormation = FindOptimalFormation(coach, context);
            if (bestFormation == teamState.CurrentFormation) return null;
            
            return new CoachingDecision
            {
                DecisionType = DecisionType.FormationChange,
                TeamId = teamState.TeamId,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["NewFormation"] = bestFormation,
                    ["CurrentFormation"] = teamState.CurrentFormation,
                    ["ExpectedImprovement"] = CalculateFormationImpact(bestFormation, context)
                },
                ExpectedImpact = 0.6f,
                Confidence = coach.TacticalKnowledge * 0.8f,
                Reasoning = $"Formation change to {bestFormation} to improve tactical effectiveness"
            };
        }
        
        public CoachingDecision EvaluateOffensiveStrategyChange(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext context)
        {
            if (context.ScoreDifferential > 10) return null; // Winning comfortably
            
            var currentStrategy = teamState.CurrentStrategy;
            var optimalStrategy = DetermineOptimalOffensiveStrategy(context);
            
            if (optimalStrategy == currentStrategy) return null;
            
            return new CoachingDecision
            {
                DecisionType = DecisionType.OffensiveStrategyChange,
                TeamId = teamState.TeamId,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["NewStrategy"] = optimalStrategy,
                    ["CurrentStrategy"] = currentStrategy
                },
                ExpectedImpact = 0.5f,
                Confidence = coach.TacticalKnowledge,
                Reasoning = $"Offensive strategy change to {optimalStrategy} based on match situation"
            };
        }
        
        public CoachingDecision EvaluateDefensiveStrategyChange(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext context)
        {
            if (context.ScoreDifferential < -15) // Behind significantly
            {
                if (teamState.CurrentDefensiveApproach != DefensiveStyle.Pressing)
                {
                    return new CoachingDecision
                    {
                        DecisionType = DecisionType.DefensiveStrategyChange,
                        TeamId = teamState.TeamId,
                        Timestamp = DateTime.Now,
                        Details = new Dictionary<string, object>
                        {
                            ["NewStrategy"] = DefensiveStyle.Pressing,
                            ["CurrentStrategy"] = teamState.CurrentDefensiveApproach
                        },
                        ExpectedImpact = 0.4f,
                        Confidence = coach.RiskTolerance,
                        Reasoning = "Switching to attacking defense to create more turnovers"
                    };
                }
            }
            
            return null;
        }
        
        public CoachingDecision EvaluateSpecializedTactics(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext context)
        {
            // Emergency tactics when significantly behind
            if (context.ScoreDifferential < -25 && context.TimeRemaining.TotalMinutes < 30)
            {
                return new CoachingDecision
                {
                    DecisionType = DecisionType.SpecializedTactic,
                    TeamId = teamState.TeamId,
                    Timestamp = DateTime.Now,
                    Details = new Dictionary<string, object>
                    {
                        ["TacticType"] = TacticalOptionType.OffensivePress,
                        ["Intensity"] = 0.8f,
                        ["Duration"] = TimeSpan.FromMinutes(10)
                    },
                    ExpectedImpact = 0.7f,
                    Confidence = coach.RiskTolerance * coach.TacticalKnowledge,
                    Reasoning = "Desperate offensive press to create scoring opportunities"
                };
            }
            
            return null;
        }
        
        // Helper methods for AI evaluation
        private float EvaluateFormationEffectiveness(Formation formation, TeamMatchContext context)
        {
            // Complex algorithm evaluating formation performance
            float baseEffectiveness = 0.6f;
            
            // Factor in team performance metrics
            baseEffectiveness += context.TeamEfficiency * 0.3f;
            baseEffectiveness += context.DefensiveEffectiveness * 0.2f;
            
            // Adjust based on score situation
            if (context.ScoreDifferential < -15) baseEffectiveness -= 0.2f;
            if (context.ScoreDifferential > 15) baseEffectiveness += 0.1f;
            
            return Math.Min(1f, Math.Max(0f, baseEffectiveness));
        }
        
        private Formation FindOptimalFormation(CoachProfile coach, TeamMatchContext context)
        {
            var formationNames = FormationLibrary.GetFormationNames();
            
            return formationNames
                .Select(name => FormationLibrary.GetFormation(name))
                .OrderByDescending(f => EvaluateFormationSuitability(f, context, coach))
                .First();
        }
        
        private float EvaluateFormationSuitability(Formation formation, 
            TeamMatchContext context, CoachProfile coach)
        {
            float suitability = 0.5f;
            
            // Evaluate based on match situation
            switch (formation.Name)
            {
                case "Attacking":
                    suitability += context.ScoreDifferential < 0 ? 0.3f : -0.1f;
                    suitability += coach.RiskTolerance * 0.2f;
                    break;
                case "Defensive":
                    suitability += context.ScoreDifferential > 10 ? 0.3f : -0.1f;
                    suitability += (1f - coach.RiskTolerance) * 0.2f;
                    break;
                case "Standard":
                    suitability += 0.1f; // Always reasonable
                    break;
            }
            
            return Math.Min(1f, Math.Max(0f, suitability));
        }
        
        private float CalculateFormationImpact(Formation formation, TeamMatchContext context)
        {
            // Estimate the expected improvement from formation change
            return (float)(new System.Random().NextDouble() * (0.6f - 0.2f) + 0.2f);
        }
        
        private OffensiveStyle DetermineOptimalOffensiveStrategy(TeamMatchContext context)
        {
            if (context.ScoreDifferential < -15) return OffensiveStyle.FastBreak;
            if (context.ScoreDifferential > 15) return OffensiveStyle.Possession;
            return OffensiveStyle.Balanced;
        }
    }
    
    /// <summary>
    /// Manager for handling player substitutions
    /// </summary>
    public class SubstitutionManager
    {
        public void ExecuteSubstitution(string playerOut, string playerIn, MatchContext context)
        {
            var outPlayer = context.GetPlayer(playerOut);
            var inPlayer = context.GetPlayer(playerIn);
            
            if (outPlayer == null || inPlayer == null) return;
            
            // Record substitution time
            var substitutionTime = DateTime.Now;
            
            // Update player status
            outPlayer.IsOnField = false;
            outPlayer.SubstitutionTime = substitutionTime;
            
            inPlayer.IsOnField = true;
            inPlayer.FieldEntryTime = substitutionTime;
            
            // Log substitution event
            context.LogEvent($"Substitution: {outPlayer.Name} off, {inPlayer.Name} on");
        }
    }
    
    /// <summary>
    /// Engine for applying tactical adjustments
    /// </summary>
    public class TacticalAdjustmentEngine
    {
        public void ApplyFormationChange(int teamId, Formation newFormation, MatchContext context)
        {
            var teamPlayers = context.GetTeamPlayers(teamId);
            
            // Apply formation-specific positioning and role adjustments
            switch (newFormation.Name)
            {
                case "Attacking":
                    ApplyAttackingFormation(teamPlayers);
                    break;
                case "Defensive":
                    ApplyDefensiveFormation(teamPlayers);
                    break;
                case "Standard":
                    ApplyStandardFormation(teamPlayers);
                    break;
            }
            
            context.LogEvent($"Team {teamId} changed formation to {newFormation}");
        }
        
        public void ApplyOffensiveStrategyChange(int teamId, OffensiveStyle newStrategy, 
            MatchContext context)
        {
            var teamPlayers = context.GetTeamPlayers(teamId);
            
            foreach (var player in teamPlayers)
            {
                ApplyOffensiveStrategyToPlayer(player, newStrategy);
            }
            
            context.LogEvent($"Team {teamId} changed offensive strategy to {newStrategy}");
        }
        
        public void ApplyDefensiveStrategyChange(int teamId, DefensiveStyle newStrategy,
            MatchContext context)
        {
            var teamPlayers = context.GetTeamPlayers(teamId);
            
            foreach (var player in teamPlayers)
            {
                ApplyDefensiveStrategyToPlayer(player, newStrategy);
            }
            
            context.LogEvent($"Team {teamId} changed defensive strategy to {newStrategy}");
        }
        
        private void ApplyAttackingFormation(List<Player> players)
        {
            // Implementation would adjust player positions and roles for attacking formation
        }
        
        private void ApplyDefensiveFormation(List<Player> players)
        {
            // Implementation would adjust player positions and roles for defensive formation
        }
        
        private void ApplyStandardFormation(List<Player> players)
        {
            // Implementation would reset players to standard positions
        }
        
        private void ApplyOffensiveStrategyToPlayer(Player player, OffensiveStyle strategy)
        {
            switch (strategy)
            {
                case OffensiveStyle.FastBreak:
                    player.Aggression += (int)(0.1f * 100); // Convert to 0-100 scale
                    // RiskTaking and similar attributes don't exist on Player, skip
                    break;
                case OffensiveStyle.Possession:
                    // Patience and DecisionMaking attributes don't exist on Player, skip
                    break;
            }
        }
        
        private void ApplyDefensiveStrategyToPlayer(Player player, DefensiveStyle strategy)
        {
            switch (strategy)
            {
                case DefensiveStyle.Pressing:
                    player.Aggression += (int)(0.1f * 100); // Convert to 0-100 scale
                    // Pressure attribute doesn't exist on Player, skip
                    break;
                case DefensiveStyle.Zoning:
                    // Reset to baseline values
                    break;
            }
        }
    }
    
    // Supporting enums
    public enum DecisionType
    {
        FormationChange,
        OffensiveStrategyChange,
        DefensiveStrategyChange,
        Substitution,
        PositionalRotation,
        RoleChange,
        TeamTalk,
        PlayerEncouragement,
        SpecializedTactic,
        TimeOut,
        TacticalAdjustment
    }
    
    public enum DecisionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum DecisionResult
    {
        Pending,
        Success,
        Failed,
        Cancelled
    }
    
    public enum CoachingApproach
    {
        Attacking,
        Defensive,
        Balanced,
        Tactical,
        Motivational
    }
    
    public enum CommunicationStyle
    {
        Aggressive,
        Calm,
        Inspiring,
        Technical,
        Concise,
        Clear
    }
    
    public enum TeamTalkType
    {
        Motivational,
        Tactical,
        Disciplinary,
        Encouraging
    }
    
    public enum EncouragementType
    {
        Confidence,
        Focus,
        Aggression,
        Calmness
    }
    
    public enum TacticalAdjustmentType
    {
        Press,
        Flood,
        Spread,
        Compact,
        HighTempo,
        Slow
    }
    
    public enum SubstitutionTrigger
    {
        FatigueLevel,
        InjuryRisk,
        PerformanceRating,
        TacticalNeed,
        TimeElapsed,
        ScoreDifferential
    }
    
    public enum InstructionType
    {
        Positioning,
        Marking,
        PressureIntensity,
        RiskTaking,
        SupportPlay,
        LeadershipRole
    }
    
    public enum MatchSituation
    {
        LeadingComfortably,
        LeadingNarrowly,
        Tied,
        BehindNarrowly,
        BehindSignificantly,
        FinalQuarter,
        MomentumSwing
    }
    
    // TacticalOption is now defined as a class above, not an enum
    
    public enum PossessionStyle
    {
        FastBreak,
        Controlled,
        Direct,
        Patient
    }
    
    /// <summary>
    /// Represents a tactical option that can be applied
    /// </summary>
    public class TacticalOption
    {
        public string Name { get; set; }
        public TacticalOptionType Type { get; set; }
        public float EffectivenessRating { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
    
    
    /// <summary>
    /// Types of tactical options (renamed from conflicting enum)
    /// </summary>
    public enum TacticalOptionType
    {
        DefensiveFlood,
        OffensivePress,
        HighPress,
        LowBlock,
        WideSpread,
        NarrowFormation
    }
    
    
    /// <summary>
    /// Player tactical roles
    /// </summary>
    public enum PlayerRole
    {
        Defender,
        Midfielder,
        Forward,
        Ruckman,
        Utility
    }
}
