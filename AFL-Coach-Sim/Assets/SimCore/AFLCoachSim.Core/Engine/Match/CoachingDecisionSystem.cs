using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Engine.Match.Tactics;

namespace AFLCoachSim.Core.Engine.Match
{
    /// <summary>
    /// Comprehensive coaching decision system for dynamic match management
    /// Handles tactical adjustments, substitutions, rotations, and strategic coaching decisions
    /// </summary>
    public class CoachingDecisionSystem
    {
        private readonly Dictionary<int, CoachProfile> _coaches;
        private readonly Dictionary<int, List<CoachingDecision>> _decisionHistory;
        private readonly Dictionary<int, TeamTacticalState> _teamStates;
        private readonly TacticalCoachingAI _coachingAI;
        private readonly AdvancedTacticalSystem _tacticalEngine;
        private readonly SubstitutionManager _substitutionManager;
        
        // Decision timing and constraints
        private readonly Dictionary<int, DateTime> _lastMajorDecision;
        private readonly Dictionary<int, int> _interchangesRemaining;
        private readonly Dictionary<int, List<string>> _availableSubstitutes;
        
        public CoachingDecisionSystem()
        {
            _coaches = new Dictionary<int, CoachProfile>();
            _decisionHistory = new Dictionary<int, List<CoachingDecision>>();
            _teamStates = new Dictionary<int, TeamTacticalState>();
            _tacticalEngine = new AdvancedTacticalSystem();
            _coachingAI = new TacticalCoachingAI(_tacticalEngine);
            _substitutionManager = new SubstitutionManager();
            _lastMajorDecision = new Dictionary<int, DateTime>();
            _interchangesRemaining = new Dictionary<int, int>();
            _availableSubstitutes = new Dictionary<int, List<string>>();
        }
        
        /// <summary>
        /// Initialize coaching system for a match
        /// </summary>
        public void InitializeMatch(List<Team> teams, Dictionary<int, CoachProfile> coaches)
        {
            foreach (var team in teams)
            {
                _coaches[team.Id] = coaches.GetValueOrDefault(team.Id, GenerateDefaultCoach());
                _decisionHistory[team.Id] = new List<CoachingDecision>();
                _teamStates[team.Id] = new TeamTacticalState
                {
                    TeamId = team.Id,
                    CurrentFormation = FormationLibrary.GetFormation("Standard"),
                    CurrentStrategy = OffensiveStyle.Balanced,
                    CurrentDefensiveApproach = DefensiveStyle.Zoning,
                    MatchPlan = GenerateMatchPlan(team, _coaches[team.Id])
                };
                
                _lastMajorDecision[team.Id] = DateTime.Now;
                _interchangesRemaining[team.Id] = 4; // Standard AFL interchange limit
                _availableSubstitutes[team.Id] = team.Players
                    .Where(p => !p.IsStartingPlayer)
                    .Select(p => p.Id.ToString())
                    .ToList();
            }
        }
        
        /// <summary>
        /// Process coaching decisions during match
        /// </summary>
        public List<CoachingDecision> ProcessCoachingDecisions(MatchContext context)
        {
            var decisions = new List<CoachingDecision>();
            
            foreach (var teamId in _teamStates.Keys)
            {
                var coach = _coaches[teamId];
                var teamState = _teamStates[teamId];
                var teamContext = GetTeamContext(context, teamId);
                
                // Evaluate need for tactical changes
                var tacticalDecisions = EvaluateTacticalDecisions(coach, teamState, teamContext);
                decisions.AddRange(tacticalDecisions);
                
                // Evaluate substitution opportunities
                var substitutionDecisions = EvaluateSubstitutions(coach, teamState, teamContext);
                decisions.AddRange(substitutionDecisions);
                
                // Evaluate player rotations
                var rotationDecisions = EvaluateRotations(coach, teamState, teamContext);
                decisions.AddRange(rotationDecisions);
                
                // Evaluate motivational interventions
                var motivationalDecisions = EvaluateMotivationalInterventions(coach, teamState, teamContext);
                decisions.AddRange(motivationalDecisions);
            }
            
            // Execute approved decisions
            foreach (var decision in decisions)
            {
                ExecuteDecision(decision, context);
            }
            
            return decisions;
        }
        
        /// <summary>
        /// Evaluate tactical adjustment opportunities
        /// </summary>
        private List<CoachingDecision> EvaluateTacticalDecisions(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Check if enough time has passed since last major tactical change
            if ((DateTime.Now - _lastMajorDecision[teamState.TeamId]).TotalMinutes < 10)
                return decisions;
            
            // Use simplified tactical evaluation for now
            // TODO: Implement proper tactical AI integration
            if (ShouldConsiderTacticalChange(coach, teamContext))
            {
                var tacticalDecision = CreateTacticalDecision(coach, teamState, teamContext);
                if (tacticalDecision != null)
                {
                    decisions.Add(tacticalDecision);
                }
            }
            
            return decisions;
        }
        
        private bool ShouldConsiderTacticalChange(CoachProfile coach, TeamMatchContext teamContext)
        {
            // Simple evaluation based on score differential and momentum
            var scoreDiff = Math.Abs(teamContext.ScoreDifferential);
            var lowMomentum = teamContext.MomentumRating < 0.4f;
            
            return scoreDiff > 15 || lowMomentum;
        }
        
        private CoachingDecision CreateTacticalDecision(CoachProfile coach, TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            // Simple tactical decision creation
            return new CoachingDecision
            {
                DecisionType = DecisionType.TacticalAdjustment,
                TeamId = teamState.TeamId,
                Priority = DecisionPriority.Medium,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["AdjustmentType"] = "Formation",
                    ["NewFormation"] = teamContext.ScoreDifferential < -10 ? "Attacking" : "Defensive"
                },
                ExpectedImpact = 0.5f,
                Confidence = coach.TacticalKnowledge
            };
        }
        
        /// <summary>
        /// Evaluate substitution opportunities
        /// </summary>
        private List<CoachingDecision> EvaluateSubstitutions(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Check if interchanges are available
            if (_interchangesRemaining[teamState.TeamId] <= 0)
                return decisions;
            
            // Check for injured players requiring immediate substitution
            var injuredSubstitutions = EvaluateInjurySubstitutions(teamState, teamContext);
            decisions.AddRange(injuredSubstitutions);
            
            // Check for fatigued players
            var fatigueSubstitutions = EvaluateFatigueSubstitutions(coach, teamState, teamContext);
            decisions.AddRange(fatigueSubstitutions);
            
            // Check for tactical substitutions
            var tacticalSubstitutions = EvaluateTacticalSubstitutions(coach, teamState, teamContext);
            decisions.AddRange(tacticalSubstitutions);
            
            // Check for performance-based substitutions
            var performanceSubstitutions = EvaluatePerformanceSubstitutions(coach, teamState, teamContext);
            decisions.AddRange(performanceSubstitutions);
            
            return decisions;
        }
        
        /// <summary>
        /// Evaluate player rotation opportunities
        /// </summary>
        private List<CoachingDecision> EvaluateRotations(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Evaluate positional rotations (doesn't use interchanges)
            var positionalRotations = EvaluatePositionalRotations(coach, teamState, teamContext);
            decisions.AddRange(positionalRotations);
            
            // Evaluate role rotations
            var roleRotations = EvaluateRoleRotations(coach, teamState, teamContext);
            decisions.AddRange(roleRotations);
            
            return decisions;
        }
        
        /// <summary>
        /// Evaluate motivational and psychological interventions
        /// </summary>
        private List<CoachingDecision> EvaluateMotivationalInterventions(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Team talks during breaks
            if (ShouldGiveTeamTalk(coach, teamContext))
            {
                decisions.Add(new CoachingDecision
                {
                    DecisionType = DecisionType.TeamTalk,
                    TeamId = teamState.TeamId,
                    Timestamp = DateTime.Now,
                    Details = GenerateTeamTalkDecision(coach, teamContext),
                    ExpectedImpact = CalculateTeamTalkImpact(coach, teamContext),
                    Confidence = coach.MotivationalSkill * 0.8f
                });
            }
            
            // Individual player encouragement
            var playerEncouragement = EvaluatePlayerEncouragement(coach, teamState, teamContext);
            decisions.AddRange(playerEncouragement);
            
            return decisions;
        }
        
        /// <summary>
        /// Execute a coaching decision
        /// </summary>
        private void ExecuteDecision(CoachingDecision decision, MatchContext context)
        {
            try
            {
                switch (decision.DecisionType)
                {
                    case DecisionType.FormationChange:
                        ExecuteFormationChange(decision, context);
                        break;
                    case DecisionType.OffensiveStrategyChange:
                        ExecuteOffensiveStrategyChange(decision, context);
                        break;
                    case DecisionType.DefensiveStrategyChange:
                        ExecuteDefensiveStrategyChange(decision, context);
                        break;
                    case DecisionType.Substitution:
                        ExecuteSubstitution(decision, context);
                        break;
                    case DecisionType.PositionalRotation:
                        ExecutePositionalRotation(decision, context);
                        break;
                    case DecisionType.RoleChange:
                        ExecuteRoleChange(decision, context);
                        break;
                    case DecisionType.TeamTalk:
                        ExecuteTeamTalk(decision, context);
                        break;
                    case DecisionType.PlayerEncouragement:
                        ExecutePlayerEncouragement(decision, context);
                        break;
                }
                
                // Record successful decision
                _decisionHistory[decision.TeamId].Add(decision);
                decision.ExecutionResult = DecisionResult.Success;
                
                // Update last major decision time for significant changes
                if (IsMajorDecision(decision.DecisionType))
                {
                    _lastMajorDecision[decision.TeamId] = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                decision.ExecutionResult = DecisionResult.Failed;
                decision.FailureReason = ex.Message;
            }
        }
        
        // Execution methods for different decision types
        private void ExecuteFormationChange(CoachingDecision decision, MatchContext context)
        {
            var newFormation = (Formation)decision.Details["NewFormation"];
            _teamStates[decision.TeamId].CurrentFormation = newFormation;
            
            // Apply formation effects through tactical engine
            var tacticsContext = ConvertToTacticsContext(context);
            _tacticalEngine.ApplyFormationChange(decision.TeamId, newFormation, tacticsContext);
        }
        
        private void ExecuteOffensiveStrategyChange(CoachingDecision decision, MatchContext context)
        {
            var newStrategy = (OffensiveStrategy)decision.Details["NewStrategy"];
            _teamStates[decision.TeamId].CurrentStrategy = newStrategy.Style;
            
            var tacticsContext = ConvertToTacticsContext(context);
            _tacticalEngine.ApplyOffensiveStrategyChange(decision.TeamId, newStrategy.Style, tacticsContext);
        }
        
        private void ExecuteDefensiveStrategyChange(CoachingDecision decision, MatchContext context)
        {
            var newStrategy = (DefensiveStrategy)decision.Details["NewStrategy"];
            _teamStates[decision.TeamId].CurrentDefensiveApproach = newStrategy.Style;
            
            var tacticsContext = ConvertToTacticsContext(context);
            _tacticalEngine.ApplyDefensiveStrategyChange(decision.TeamId, newStrategy.Style, tacticsContext);
        }
        
        private void ExecuteSubstitution(CoachingDecision decision, MatchContext context)
        {
            var playerOut = (string)decision.Details["PlayerOut"];
            var playerIn = (string)decision.Details["PlayerIn"];
            
            _substitutionManager.ExecuteSubstitution(playerOut, playerIn, context);
            
            // Update interchange count
            _interchangesRemaining[decision.TeamId]--;
            
            // Remove substituted player from available list
            _availableSubstitutes[decision.TeamId].Remove(playerIn);
            _availableSubstitutes[decision.TeamId].Add(playerOut);
        }
        
        private void ExecutePositionalRotation(CoachingDecision decision, MatchContext context)
        {
            var playerId = (string)decision.Details["PlayerId"];
            var newPosition = (string)decision.Details["NewPosition"];
            
            // Update player position in match context
            var player = context.GetPlayer(playerId);
            if (player != null)
            {
                player.CurrentMatchPosition = newPosition;
            }
        }
        
        private void ExecuteRoleChange(CoachingDecision decision, MatchContext context)
        {
            var playerId = (string)decision.Details["PlayerId"];
            var newRole = (Role)decision.Details["NewRole"];
            
            var player = context.GetPlayer(playerId);
            if (player != null)
            {
                player.CurrentRole = newRole;
            }
        }
        
        private void ExecuteTeamTalk(CoachingDecision decision, MatchContext context)
        {
            var talkType = (TeamTalkType)decision.Details["TalkType"];
            var intensity = (float)decision.Details["Intensity"];
            
            // Apply motivational effects to all team players
            var teamPlayers = context.GetTeamPlayers(decision.TeamId);
            foreach (var player in teamPlayers)
            {
                ApplyMotivationalEffect(player, talkType, intensity, context);
            }
        }
        
        private void ExecutePlayerEncouragement(CoachingDecision decision, MatchContext context)
        {
            var playerId = (string)decision.Details["PlayerId"];
            var encouragementType = (EncouragementType)decision.Details["EncouragementType"];
            var intensity = (float)decision.Details["Intensity"];
            
            var player = context.GetPlayer(playerId);
            if (player != null)
            {
                ApplyIndividualEncouragement(player, encouragementType, intensity, context);
            }
        }
        
        // Helper methods for specific evaluations
        private List<CoachingDecision> EvaluateInjurySubstitutions(TeamTacticalState teamState, 
            TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            var injuredPlayers = teamContext.Players
                .Where(p => p.InjuryStatus != InjuryStatus.Healthy && 
                           p.InjuryStatus != InjuryStatus.Minor)
                .ToList();
            
            foreach (var injured in injuredPlayers)
            {
                var substitute = FindBestSubstitute(injured, teamContext);
                if (substitute != null)
                {
                    decisions.Add(new CoachingDecision
                    {
                        DecisionType = DecisionType.Substitution,
                        TeamId = teamState.TeamId,
                        Priority = DecisionPriority.Critical,
                        Timestamp = DateTime.Now,
                        Details = new Dictionary<string, object>
                        {
                            ["PlayerOut"] = injured.Id,
                            ["PlayerIn"] = substitute.Id,
                            ["Reason"] = "Injury substitution",
                            ["InjuryType"] = injured.InjuryStatus
                        },
                        ExpectedImpact = 0.8f, // High impact to maintain team strength
                        Confidence = 0.95f // Very confident in injury subs
                    });
                }
            }
            
            return decisions;
        }
        
        private List<CoachingDecision> EvaluateFatigueSubstitutions(CoachProfile coach,
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Only consider fatigue subs if coach is proactive about rotation
            if (coach.RotationStrategy < 0.6f) return decisions;
            
            var fatiguedPlayers = teamContext.Players
                .Where(p => p.FatigueLevel > 0.8f && p.TimeOnGround > 60)
                .OrderByDescending(p => p.FatigueLevel)
                .Take(2) // Limit to 2 at a time
                .ToList();
            
            foreach (var fatigued in fatiguedPlayers)
            {
                var substitute = FindBestSubstitute(fatigued, teamContext);
                if (substitute != null)
                {
                    decisions.Add(new CoachingDecision
                    {
                        DecisionType = DecisionType.Substitution,
                        TeamId = teamState.TeamId,
                        Priority = DecisionPriority.Medium,
                        Timestamp = DateTime.Now,
                        Details = new Dictionary<string, object>
                        {
                            ["PlayerOut"] = fatigued.Id,
                            ["PlayerIn"] = substitute.Id,
                            ["Reason"] = "Fatigue management",
                            ["FatigueLevel"] = fatigued.FatigueLevel
                        },
                        ExpectedImpact = 0.6f,
                        Confidence = coach.RotationStrategy
                    });
                }
            }
            
            return decisions;
        }
        
        private Player FindBestSubstitute(Player playerOut, TeamMatchContext teamContext)
        {
            var availableSubstitutes = _availableSubstitutes[teamContext.TeamId];
            var substitutes = teamContext.AvailableSubstitutes
                .Where(p => availableSubstitutes.Contains(p.Id.ToString()))
                .ToList();
            
            if (!substitutes.Any()) return null;
            
            // Find substitute that best matches outgoing player's position and role
            return substitutes
                .OrderBy(s => CalculatePositionMismatch(playerOut, s))
                .ThenByDescending(s => s.OverallRating)
                .First();
        }
        
        private float CalculatePositionMismatch(Player playerOut, Player substitute)
        {
            if (playerOut.Position == substitute.Position) return 0f;
            
            // Calculate position compatibility score
            var compatibility = GetPositionCompatibility(playerOut.Position.ToString(), substitute.Position.ToString());
            return 1f - compatibility;
        }
        
        private float GetPositionCompatibility(string position1, string position2)
        {
            var compatibilityMatrix = new Dictionary<string, Dictionary<string, float>>
            {
                ["Forward"] = new Dictionary<string, float>
                {
                    ["Forward"] = 1.0f,
                    ["Midfielder"] = 0.7f,
                    ["Defender"] = 0.3f,
                    ["Ruck"] = 0.5f
                },
                ["Midfielder"] = new Dictionary<string, float>
                {
                    ["Forward"] = 0.7f,
                    ["Midfielder"] = 1.0f,
                    ["Defender"] = 0.7f,
                    ["Ruck"] = 0.4f
                },
                ["Defender"] = new Dictionary<string, float>
                {
                    ["Forward"] = 0.3f,
                    ["Midfielder"] = 0.7f,
                    ["Defender"] = 1.0f,
                    ["Ruck"] = 0.4f
                },
                ["Ruck"] = new Dictionary<string, float>
                {
                    ["Forward"] = 0.5f,
                    ["Midfielder"] = 0.4f,
                    ["Defender"] = 0.4f,
                    ["Ruck"] = 1.0f
                }
            };
            
            return compatibilityMatrix.GetValueOrDefault(position1, new Dictionary<string, float>())
                .GetValueOrDefault(position2, 0.2f);
        }
        
        private bool ShouldGiveTeamTalk(CoachProfile coach, TeamMatchContext teamContext)
        {
            // Check timing (quarter breaks, significant momentum shifts)
            if (!IsAppropriateTimeForTeamTalk(teamContext)) return false;
            
            // Check if team needs motivation
            var teamMorale = CalculateTeamMorale(teamContext);
            var scoreDifferential = teamContext.ScoreDifferential;
            
            // More likely to give team talk when losing or low morale
            var probability = coach.MotivationalSkill * 
                             (1.0f - teamMorale) * 
                             Math.Max(0.3f, 1.0f + scoreDifferential * 0.1f);
            
            return (float)new System.Random().NextDouble() < probability;
        }
        
        private bool IsAppropriateTimeForTeamTalk(TeamMatchContext teamContext)
        {
            // Team talks typically happen during quarter breaks
            return teamContext.IsQuarterBreak || 
                   teamContext.TimeRemaining.TotalMinutes % 25 < 1; // Near quarter time
        }
        
        private float CalculateTeamMorale(TeamMatchContext teamContext)
        {
            return (float)teamContext.Players.Average(p => p.Confidence);
        }
        
        private Dictionary<string, object> GenerateTeamTalkDecision(CoachProfile coach,
            TeamMatchContext teamContext)
        {
            var talkType = DetermineTeamTalkType(coach, teamContext);
            var intensity = DetermineTeamTalkIntensity(coach, teamContext);
            
            return new Dictionary<string, object>
            {
                ["TalkType"] = talkType,
                ["Intensity"] = intensity,
                ["Focus"] = DetermineTeamTalkFocus(teamContext),
                ["Duration"] = coach.CommunicationStyle == CommunicationStyle.Concise ? 2f : 5f
            };
        }
        
        private TeamTalkType DetermineTeamTalkType(CoachProfile coach, TeamMatchContext teamContext)
        {
            var scoreDiff = teamContext.ScoreDifferential;
            
            if (scoreDiff < -20) return TeamTalkType.Motivational;
            if (scoreDiff > 20) return TeamTalkType.Tactical;
            if (teamContext.MomentumRating < 0.4f) return TeamTalkType.Motivational;
            
            return coach.PreferredApproach == CoachingApproach.Tactical 
                ? TeamTalkType.Tactical 
                : TeamTalkType.Motivational;
        }
        
        private float DetermineTeamTalkIntensity(CoachProfile coach, TeamMatchContext teamContext)
        {
            var baseIntensity = coach.Intensity;
            var situationModifier = Math.Abs(teamContext.ScoreDifferential) * 0.01f;
            
            return Math.Min(1f, baseIntensity + situationModifier);
        }
        
        private string DetermineTeamTalkFocus(TeamMatchContext teamContext)
        {
            if (teamContext.TurnoverDifferential < -5) return "Ball Security";
            if (teamContext.ContestedPossessionDifferential < -10) return "Contested Ball";
            if (teamContext.ScoreDifferential < -15) return "Scoring Opportunities";
            
            return "General Performance";
        }
        
        private float CalculateTeamTalkImpact(CoachProfile coach, TeamMatchContext teamContext)
        {
            var baseImpact = coach.MotivationalSkill * 0.6f;
            var situationMultiplier = 1f + (Math.Abs(teamContext.ScoreDifferential) * 0.005f);
            
            return Math.Min(1f, baseImpact * situationMultiplier);
        }
        
        
        private bool IsMajorDecision(DecisionType decisionType)
        {
            return decisionType == DecisionType.FormationChange ||
                   decisionType == DecisionType.OffensiveStrategyChange ||
                   decisionType == DecisionType.DefensiveStrategyChange;
        }
        
        private void ApplyMotivationalEffect(Player player, TeamTalkType talkType, 
            float intensity, MatchContext context)
        {
            var effectMultiplier = intensity * 0.2f;
            
            switch (talkType)
            {
                case TeamTalkType.Motivational:
                    player.Confidence += (int)(effectMultiplier * 100); // Convert to 0-100 scale
                    player.Aggression += (int)(effectMultiplier * 50); // Smaller effect
                    break;
                case TeamTalkType.Tactical:
                    player.Focus += (int)(effectMultiplier * 100);
                    // DecisionMaking is likely in Attributes, skip for now
                    break;
            }
            
            // Clamp values to 0-100 range
            player.Confidence = Math.Min(100, Math.Max(0, player.Confidence));
            player.Aggression = Math.Min(100, Math.Max(0, player.Aggression));
            player.Focus = Math.Min(100, Math.Max(0, player.Focus));
        }
        
        private void ApplyIndividualEncouragement(Player player, EncouragementType type, 
            float intensity, MatchContext context)
        {
            var effect = intensity * 0.15f;
            
            switch (type)
            {
                case EncouragementType.Confidence:
                    player.Confidence += (int)(effect * 100);
                    break;
                case EncouragementType.Focus:
                    player.Focus += (int)(effect * 100);
                    break;
                case EncouragementType.Aggression:
                    player.Aggression += (int)(effect * 100);
                    break;
            }
        }
        
        private CoachProfile GenerateDefaultCoach()
        {
            return new CoachProfile
            {
                Name = "Default Coach",
                TacticalKnowledge = 0.7f,
                MotivationalSkill = 0.6f,
                RotationStrategy = 0.5f,
                RiskTolerance = 0.4f,
                Adaptability = 0.6f,
                Intensity = 0.5f,
                PreferredApproach = CoachingApproach.Balanced,
                CommunicationStyle = CommunicationStyle.Clear
            };
        }
        
        private MatchPlan GenerateMatchPlan(Team team, CoachProfile coach)
        {
            return new MatchPlan
            {
                PrimaryFormation = FormationLibrary.GetFormation("Standard"),
                PrimaryOffensiveStrategy = OffensiveStyle.Balanced,
                PrimaryDefensiveStrategy = DefensiveStyle.Zoning,
                TargetPossessionStyle = coach.PreferredApproach == CoachingApproach.Attacking 
                    ? PossessionStyle.FastBreak 
                    : PossessionStyle.Controlled,
                EmergencyTactics = new List<TacticalOption>
                {
                    new TacticalOption { Name = "Defensive Flood", Type = TacticalOptionType.DefensiveFlood, EffectivenessRating = 0.7f },
                    new TacticalOption { Name = "Offensive Press", Type = TacticalOptionType.OffensivePress, EffectivenessRating = 0.6f }
                }
            };
        }
        
        private TeamMatchContext GetTeamContext(MatchContext context, int teamId)
        {
            // Extract team-specific context from match context
            // Using simplified context extraction for now
            return new TeamMatchContext
            {
                TeamId = teamId,
                Players = new List<Player>(), // TODO: Get from context
                AvailableSubstitutes = new List<Player>(), // TODO: Get from context
                ScoreDifferential = 0f, // TODO: Calculate from context
                MomentumRating = 0.5f, // TODO: Get from context
                TurnoverDifferential = 0f, // TODO: Calculate from context
                ContestedPossessionDifferential = 0f, // TODO: Calculate from context
                TimeRemaining = TimeSpan.FromMinutes(20), // TODO: Get from context
                IsQuarterBreak = false // TODO: Get from context
            };
        }
        
        // Additional evaluation methods implementation
        
        private List<CoachingDecision> EvaluateTacticalSubstitutions(CoachProfile coach, 
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Check if coach is tactical and situation warrants tactical change
            if (coach.PreferredApproach == CoachingApproach.Tactical && _interchangesRemaining[teamState.TeamId] > 0)
            {
                if (Math.Abs(teamContext.ScoreDifferential) > 30)
                {
                    decisions.Add(new CoachingDecision
                    {
                        DecisionType = DecisionType.Substitution,
                        TeamId = teamState.TeamId,
                        Priority = DecisionPriority.Medium,
                        Timestamp = DateTime.Now,
                        Details = new Dictionary<string, object>
                        {
                            ["SubstitutionType"] = "Tactical",
                            ["Reason"] = "Tactical adjustment needed"
                        },
                        ExpectedImpact = 0.6f,
                        Confidence = coach.TacticalKnowledge
                    });
                }
            }
            
            return decisions;
        }
        
        private List<CoachingDecision> EvaluatePerformanceSubstitutions(CoachProfile coach, 
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Look for underperforming players
            if (_interchangesRemaining[teamState.TeamId] > 0 && coach.RiskTolerance > 0.6f)
            {
                var underPerformers = teamContext.Players.Where(p => p.MatchRating < 4.0f).Take(1);
                
                foreach (var player in underPerformers)
                {
                    var substitute = FindBestSubstitute(player, teamContext);
                    if (substitute != null)
                    {
                        decisions.Add(new CoachingDecision
                        {
                            DecisionType = DecisionType.Substitution,
                            TeamId = teamState.TeamId,
                            Priority = DecisionPriority.Low,
                            Timestamp = DateTime.Now,
                            Details = new Dictionary<string, object>
                            {
                                ["PlayerOut"] = player.Id,
                                ["PlayerIn"] = substitute.Id,
                                ["Reason"] = "Performance substitution",
                                ["PlayerRating"] = player.MatchRating
                            },
                            ExpectedImpact = 0.5f,
                            Confidence = coach.RiskTolerance
                        });
                    }
                }
            }
            
            return decisions;
        }
        
        private List<CoachingDecision> EvaluatePositionalRotations(CoachProfile coach, 
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Simple positional rotation logic
            if (coach.Adaptability > 0.7f && teamContext.MomentumRating < 0.3f)
            {
                decisions.Add(new CoachingDecision
                {
                    DecisionType = DecisionType.PositionalRotation,
                    TeamId = teamState.TeamId,
                    Priority = DecisionPriority.Low,
                    Timestamp = DateTime.Now,
                    Details = new Dictionary<string, object>
                    {
                        ["RotationType"] = "Positional",
                        ["Reason"] = "Momentum adjustment"
                    },
                    ExpectedImpact = 0.3f,
                    Confidence = coach.Adaptability
                });
            }
            
            return decisions;
        }
        
        private List<CoachingDecision> EvaluateRoleRotations(CoachProfile coach, 
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            var decisions = new List<CoachingDecision>();
            
            // Simple role rotation logic
            if (coach.TacticalKnowledge > 0.8f && Math.Abs(teamContext.ScoreDifferential) > 20)
            {
                decisions.Add(new CoachingDecision
                {
                    DecisionType = DecisionType.RoleChange,
                    TeamId = teamState.TeamId,
                    Priority = DecisionPriority.Medium,
                    Timestamp = DateTime.Now,
                    Details = new Dictionary<string, object>
                    {
                        ["RotationType"] = "Role",
                        ["Reason"] = "Score differential adjustment"
                    },
                    ExpectedImpact = 0.4f,
                    Confidence = coach.TacticalKnowledge
                });
            }
            
            return decisions;
        }
        
        private List<CoachingDecision> EvaluatePlayerEncouragement(CoachProfile coach, 
            TeamTacticalState teamState, TeamMatchContext teamContext)
        {
            // TODO: Implement player encouragement logic
            return new List<CoachingDecision>();
        }
        
        /// <summary>
        /// Convert main MatchContext to tactics-specific MatchContext
        /// </summary>
        private Tactics.MatchContext ConvertToTacticsContext(MatchContext mainContext)
        {
            return new Tactics.MatchContext
            {
                Quarter = mainContext.Quarter,
                TimeRemaining = mainContext.TimeRemaining,
                Venue = mainContext.Venue ?? "Unknown",
                CrowdSize = mainContext.CrowdSize,
                IsNightGame = mainContext.IsNightGame,
                IsFinalSeries = mainContext.IsFinalSeries
            };
        }
        
        /// <summary>
        /// Get coaching decision history for a team
        /// </summary>
        public List<CoachingDecision> GetDecisionHistory(int teamId)
        {
            return _decisionHistory.GetValueOrDefault(teamId, new List<CoachingDecision>());
        }
        
        /// <summary>
        /// Get current team tactical state
        /// </summary>
        public TeamTacticalState GetTeamTacticalState(int teamId)
        {
            return _teamStates.GetValueOrDefault(teamId);
        }
        
        /// <summary>
        /// Force a specific coaching decision (for manual control)
        /// </summary>
        public bool ForceDecision(CoachingDecision decision, MatchContext context)
        {
            try
            {
                ExecuteDecision(decision, context);
                return decision.ExecutionResult == DecisionResult.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}