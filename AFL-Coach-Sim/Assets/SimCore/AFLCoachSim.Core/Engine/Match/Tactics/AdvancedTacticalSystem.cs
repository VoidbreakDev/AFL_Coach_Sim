using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Tactics
{
    /// <summary>
    /// Advanced tactical system that provides sophisticated team strategy and formation management
    /// </summary>
    public class AdvancedTacticalSystem
    {
        private readonly Dictionary<TeamId, TacticalGamePlan> _currentGamePlans;
        private readonly Dictionary<TeamId, TacticalHistory> _tacticalHistory;
        private readonly Random _random;

        public AdvancedTacticalSystem(int seed = 0)
        {
            _currentGamePlans = new Dictionary<TeamId, TacticalGamePlan>();
            _tacticalHistory = new Dictionary<TeamId, TacticalHistory>();
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        #region Game Plan Management

        /// <summary>
        /// Set the tactical game plan for a team
        /// </summary>
        public void SetGamePlan(TeamId teamId, TacticalGamePlan gamePlan)
        {
            _currentGamePlans[teamId] = gamePlan;
            
            if (!_tacticalHistory.ContainsKey(teamId))
                _tacticalHistory[teamId] = new TacticalHistory();
                
            _tacticalHistory[teamId].AddGamePlan(gamePlan);
            
            CoreLogger.Log($"[TacticalSystem] Set game plan for {teamId}: {gamePlan.Name} ({gamePlan.Formation.Name})");
        }

        /// <summary>
        /// Get current game plan for a team
        /// </summary>
        public TacticalGamePlan GetGamePlan(TeamId teamId)
        {
            return _currentGamePlans.GetValueOrDefault(teamId) ?? CreateDefaultGamePlan();
        }

        /// <summary>
        /// Make tactical adjustment during the match
        /// </summary>
        public TacticalAdjustmentResult MakeTacticalAdjustment(TeamId teamId, TacticalAdjustment adjustment, 
            MatchSituation currentSituation, float coachSkillMultiplier = 1.0f)
        {
            var gamePlan = GetGamePlan(teamId);
            var result = new TacticalAdjustmentResult();

            // Calculate success probability based on adjustment complexity and coach skill
            float baseProbability = CalculateAdjustmentSuccessProbability(adjustment, gamePlan, currentSituation);
            float finalProbability = Math.Min(0.95f, baseProbability * coachSkillMultiplier);

            bool success = _random.NextDouble() < finalProbability;
            
            if (success)
            {
                ApplyTacticalAdjustment(gamePlan, adjustment);
                result.Success = true;
                result.EffectMagnitude = CalculateAdjustmentEffect(adjustment, currentSituation);
                result.PlayerAdaptationTime = CalculateAdaptationTime(adjustment, gamePlan);
                
                CoreLogger.Log($"[TacticalSystem] Successful tactical adjustment for {teamId}: {adjustment.Type}");
            }
            else
            {
                result.Success = false;
                result.Disruption = CalculateAdjustmentDisruption(adjustment);
                
                CoreLogger.Log($"[TacticalSystem] Failed tactical adjustment for {teamId}: {adjustment.Type}");
            }

            // Record the adjustment attempt
            _tacticalHistory[teamId].AddAdjustment(adjustment, success, currentSituation);
            
            return result;
        }

        #endregion

        #region Formation Effects

        /// <summary>
        /// Calculate formation advantages in different match phases
        /// </summary>
        public FormationEffectiveness CalculateFormationEffectiveness(TeamId teamId, TeamId opponentId, Phase currentPhase)
        {
            var ourPlan = GetGamePlan(teamId);
            var opponentPlan = GetGamePlan(opponentId);

            var effectiveness = new FormationEffectiveness();

            // Calculate phase-specific advantages
            switch (currentPhase)
            {
                case Phase.CenterBounce:
                    effectiveness = CalculateCenterBounceAdvantage(ourPlan, opponentPlan);
                    break;
                case Phase.OpenPlay:
                    effectiveness = CalculateOpenPlayAdvantage(ourPlan, opponentPlan);
                    break;
                case Phase.Inside50:
                    effectiveness = CalculateInside50Advantage(ourPlan, opponentPlan);
                    break;
                case Phase.KickIn:
                    effectiveness = CalculateKickInAdvantage(ourPlan, opponentPlan);
                    break;
            }

            // Apply formation matchup bonuses/penalties
            ApplyFormationMatchups(effectiveness, ourPlan.Formation, opponentPlan.Formation);

            return effectiveness;
        }

        /// <summary>
        /// Get player positioning modifiers based on formation
        /// </summary>
        public Dictionary<string, PositionModifier> GetPlayerPositioningModifiers(TeamId teamId, 
            IList<PlayerRuntime> players)
        {
            var gamePlan = GetGamePlan(teamId);
            var modifiers = new Dictionary<string, PositionModifier>();

            foreach (var player in players)
            {
                var modifier = CalculatePlayerPositionModifier(player, gamePlan);
                modifiers[player.Player.Name] = modifier;
            }

            return modifiers;
        }

        #endregion

        #region Momentum and Pressure

        /// <summary>
        /// Calculate pressure rating based on tactical setup
        /// </summary>
        public float CalculatePressureRating(TeamId teamId, MatchSituation situation)
        {
            var gamePlan = GetGamePlan(teamId);
            float basePressure = gamePlan.DefensiveStrategy.PressureIntensity;

            // Adjust based on game situation
            if (situation.ScoreDifferential < -12) // Behind by 2+ goals
                basePressure *= gamePlan.DefensiveStrategy.DespersionPressureMultiplier;
            
            if (situation.TimeRemainingPercent < 0.25f) // Last quarter
                basePressure *= 1.2f;

            // Formation-specific pressure bonuses
            basePressure *= GetFormationPressureMultiplier(gamePlan.Formation);

            return Math.Max(0.1f, Math.Min(2.0f, basePressure));
        }

        /// <summary>
        /// Calculate team momentum modifier from tactical setup
        /// </summary>
        public float CalculateMomentumModifier(TeamId teamId, float currentMomentum, MatchSituation situation)
        {
            var gamePlan = GetGamePlan(teamId);
            float modifier = 1.0f;

            // Momentum-building strategies
            if (gamePlan.OffensiveStrategy.Style == OffensiveStyle.FastBreak && currentMomentum > 0.1f)
                modifier += 0.15f;

            if (gamePlan.OffensiveStrategy.Style == OffensiveStyle.Possession && currentMomentum < -0.1f)
                modifier += 0.10f; // Possession play helps steady the ship

            // Defensive strategies
            if (gamePlan.DefensiveStrategy.Style == DefensiveStyle.Pressing && situation.PossessionTurnover > 0.6f)
                modifier += 0.12f;

            return modifier;
        }

        #endregion

        #region Private Helper Methods

        private TacticalGamePlan CreateDefaultGamePlan()
        {
            return new TacticalGamePlan
            {
                Name = "Balanced",
                Formation = FormationLibrary.GetFormation("Standard"),
                OffensiveStrategy = new OffensiveStrategy
                {
                    Style = OffensiveStyle.Balanced,
                    PacePreference = 50f,
                    RiskTolerance = 50f,
                    CorridorUsage = 50f
                },
                DefensiveStrategy = new DefensiveStrategy
                {
                    Style = DefensiveStyle.Zoning,
                    PressureIntensity = 50f,
                    Compactness = 50f,
                    DespersionPressureMultiplier = 1.2f
                }
            };
        }

        private float CalculateAdjustmentSuccessProbability(TacticalAdjustment adjustment, 
            TacticalGamePlan gamePlan, MatchSituation situation)
        {
            float baseProbability = 0.7f;

            // Simpler adjustments are more likely to succeed
            switch (adjustment.Type)
            {
                case TacticalAdjustmentType.FormationChange:
                    baseProbability = 0.6f; // Complex change
                    break;
                case TacticalAdjustmentType.PressureIntensity:
                    baseProbability = 0.8f; // Relatively simple
                    break;
                case TacticalAdjustmentType.OffensiveStyle:
                    baseProbability = 0.65f;
                    break;
                case TacticalAdjustmentType.DefensiveStructure:
                    baseProbability = 0.7f;
                    break;
            }

            // Adjust based on game situation (panic adjustments less likely to work)
            if (Math.Abs(situation.ScoreDifferential) > 24) // 4+ goals difference
                baseProbability *= 0.85f;

            return baseProbability;
        }

        private void ApplyTacticalAdjustment(TacticalGamePlan gamePlan, TacticalAdjustment adjustment)
        {
            switch (adjustment.Type)
            {
                case TacticalAdjustmentType.FormationChange:
                    if (adjustment.NewFormation != null)
                        gamePlan.Formation = adjustment.NewFormation;
                    break;
                    
                case TacticalAdjustmentType.PressureIntensity:
                    gamePlan.DefensiveStrategy.PressureIntensity = adjustment.NewValue ?? gamePlan.DefensiveStrategy.PressureIntensity;
                    break;
                    
                case TacticalAdjustmentType.OffensiveStyle:
                    if (adjustment.NewOffensiveStyle.HasValue)
                        gamePlan.OffensiveStrategy.Style = adjustment.NewOffensiveStyle.Value;
                    break;
            }
        }

        private float CalculateAdjustmentEffect(TacticalAdjustment adjustment, MatchSituation situation)
        {
            // More dramatic situations allow for bigger tactical impacts
            float situationMultiplier = 1.0f + (Math.Abs(situation.ScoreDifferential) / 50f);
            
            return adjustment.Type switch
            {
                TacticalAdjustmentType.FormationChange => 0.15f * situationMultiplier,
                TacticalAdjustmentType.PressureIntensity => 0.10f * situationMultiplier,
                TacticalAdjustmentType.OffensiveStyle => 0.12f * situationMultiplier,
                TacticalAdjustmentType.DefensiveStructure => 0.08f * situationMultiplier,
                _ => 0.05f * situationMultiplier
            };
        }

        private int CalculateAdaptationTime(TacticalAdjustment adjustment, TacticalGamePlan gamePlan)
        {
            // Time in seconds for players to adapt to tactical changes
            return adjustment.Type switch
            {
                TacticalAdjustmentType.FormationChange => 120, // 2 minutes
                TacticalAdjustmentType.PressureIntensity => 60, // 1 minute
                TacticalAdjustmentType.OffensiveStyle => 90,    // 1.5 minutes
                TacticalAdjustmentType.DefensiveStructure => 75, // 1.25 minutes
                _ => 45
            };
        }

        private float CalculateAdjustmentDisruption(TacticalAdjustment adjustment)
        {
            // Failed adjustments cause temporary performance disruption
            return adjustment.Type switch
            {
                TacticalAdjustmentType.FormationChange => -0.15f, // Significant disruption
                TacticalAdjustmentType.PressureIntensity => -0.05f, // Minor disruption
                TacticalAdjustmentType.OffensiveStyle => -0.10f,
                TacticalAdjustmentType.DefensiveStructure => -0.08f,
                _ => -0.03f
            };
        }

        private FormationEffectiveness CalculateCenterBounceAdvantage(TacticalGamePlan ourPlan, TacticalGamePlan opponentPlan)
        {
            var effectiveness = new FormationEffectiveness();

            // Midfield-heavy formations have advantages at center bounces
            float ourMidfieldStrength = ourPlan.Formation.MidfieldPlayers;
            float opponentMidfieldStrength = opponentPlan.Formation.MidfieldPlayers;

            effectiveness.CenterBounceAdvantage = (ourMidfieldStrength - opponentMidfieldStrength) * 0.1f;
            return effectiveness;
        }

        private FormationEffectiveness CalculateOpenPlayAdvantage(TacticalGamePlan ourPlan, TacticalGamePlan opponentPlan)
        {
            var effectiveness = new FormationEffectiveness();
            
            // Balanced formations better in open play
            float ourBalance = CalculateFormationBalance(ourPlan.Formation);
            float opponentBalance = CalculateFormationBalance(opponentPlan.Formation);

            effectiveness.OpenPlayAdvantage = (ourBalance - opponentBalance) * 0.12f;
            return effectiveness;
        }

        private FormationEffectiveness CalculateInside50Advantage(TacticalGamePlan ourPlan, TacticalGamePlan opponentPlan)
        {
            var effectiveness = new FormationEffectiveness();

            // Forward-heavy formations have attacking advantages
            float ourForwardStrength = ourPlan.Formation.ForwardPlayers;
            float opponentDefenseStrength = opponentPlan.Formation.DefensivePlayers;

            effectiveness.Inside50Advantage = (ourForwardStrength - opponentDefenseStrength * 0.8f) * 0.08f;
            return effectiveness;
        }

        private FormationEffectiveness CalculateKickInAdvantage(TacticalGamePlan ourPlan, TacticalGamePlan opponentPlan)
        {
            var effectiveness = new FormationEffectiveness();

            // Defensive formations better at kick-ins (when defending)
            float ourDefenseStrength = ourPlan.Formation.DefensivePlayers;
            effectiveness.KickInAdvantage = ourDefenseStrength * 0.05f;

            return effectiveness;
        }

        private void ApplyFormationMatchups(FormationEffectiveness effectiveness, Formation ourFormation, Formation opponentFormation)
        {
            // Check for specific formation counter-matchups
            var matchupBonus = FormationMatchups.GetMatchupBonus(ourFormation.Name, opponentFormation.Name);
            effectiveness.OverallAdvantage += matchupBonus;
        }

        private PositionModifier CalculatePlayerPositionModifier(PlayerRuntime player, TacticalGamePlan gamePlan)
        {
            var modifier = new PositionModifier();

            // Formation affects positioning
            modifier.PositioningBonus = gamePlan.Formation.GetPositionBonus(player.Player.PrimaryRole);

            // Strategy affects movement patterns
            if (gamePlan.OffensiveStrategy.Style == OffensiveStyle.FastBreak)
                modifier.SpeedBonus = 0.05f;

            if (gamePlan.DefensiveStrategy.Style == DefensiveStyle.ManOnMan)
                modifier.TacklingBonus = 0.08f;

            return modifier;
        }

        private float GetFormationPressureMultiplier(Formation formation)
        {
            return formation.Name switch
            {
                "Attacking" => 1.1f,
                "Pressing" => 1.3f,
                "Defensive" => 0.9f,
                "Flooding" => 0.8f,
                _ => 1.0f
            };
        }

        private float CalculateFormationBalance(Formation formation)
        {
            // More balanced formations have values closer to 1.0
            float total = formation.DefensivePlayers + formation.MidfieldPlayers + formation.ForwardPlayers;
            if (total == 0) return 0.5f;

            float idealRatio = total / 3f; // Perfect balance
            float variance = Math.Abs(formation.DefensivePlayers - idealRatio) +
                           Math.Abs(formation.MidfieldPlayers - idealRatio) +
                           Math.Abs(formation.ForwardPlayers - idealRatio);

            return Math.Max(0.2f, 1.0f - (variance / total));
        }

        #endregion
    }
}