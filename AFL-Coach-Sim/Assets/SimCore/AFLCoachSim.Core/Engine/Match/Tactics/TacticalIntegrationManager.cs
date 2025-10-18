using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Tactics
{
    /// <summary>
    /// Integrates the advanced tactical system with the main match engine
    /// </summary>
    public class TacticalIntegrationManager
    {
        private readonly AdvancedTacticalSystem _tacticalSystem;
        private readonly TacticalCoachingAI _coachingAI;
        private readonly Dictionary<TeamId, TacticalGamePlan> _gameStartPlans;
        private readonly Dictionary<TeamId, List<TacticalAdjustmentResult>> _adjustmentHistory;
        private DateTime _lastTacticalCheck;
        private readonly float _tacticalUpdateInterval = 30f; // Check every 30 seconds

        public TacticalIntegrationManager(int seed = 0)
        {
            _tacticalSystem = new AdvancedTacticalSystem(seed);
            _coachingAI = new TacticalCoachingAI(_tacticalSystem, seed);
            _gameStartPlans = new Dictionary<TeamId, TacticalGamePlan>();
            _adjustmentHistory = new Dictionary<TeamId, List<TacticalAdjustmentResult>>();
            _lastTacticalCheck = DateTime.Now;
        }

        #region Match Integration

        /// <summary>
        /// Initialize tactical systems for a match
        /// </summary>
        public void InitializeMatch(TeamId homeTeam, TeamId awayTeam, CoachingProfile homeCoach, CoachingProfile awayCoach)
        {
            // Set up coaching profiles
            _coachingAI.SetCoachingProfile(homeTeam, homeCoach);
            _coachingAI.SetCoachingProfile(awayTeam, awayCoach);

            // Create initial game plans based on coach preferences
            var homeGamePlan = CreateInitialGamePlan(homeCoach);
            var awayGamePlan = CreateInitialGamePlan(awayCoach);

            _tacticalSystem.SetGamePlan(homeTeam, homeGamePlan);
            _tacticalSystem.SetGamePlan(awayTeam, awayGamePlan);

            // Store initial plans for comparison
            _gameStartPlans[homeTeam] = homeGamePlan;
            _gameStartPlans[awayTeam] = awayGamePlan;

            // Initialize adjustment history
            _adjustmentHistory[homeTeam] = new List<TacticalAdjustmentResult>();
            _adjustmentHistory[awayTeam] = new List<TacticalAdjustmentResult>();

            CoreLogger.Log($"[TacticalIntegration] Match initialized - {homeTeam} vs {awayTeam}");
            CoreLogger.Log($"[TacticalIntegration] {homeTeam} formation: {homeGamePlan.Formation.Name}");
            CoreLogger.Log($"[TacticalIntegration] {awayTeam} formation: {awayGamePlan.Formation.Name}");
        }

        /// <summary>
        /// Process tactical updates during the match
        /// </summary>
        public TacticalImpacts ProcessTacticalUpdates(MatchState matchState, TeamId homeTeam, TeamId awayTeam, 
            float elapsedTime, IList<PlayerRuntime> homePlayers, IList<PlayerRuntime> awayPlayers)
        {
            var impacts = new TacticalImpacts();

            // Only check tactical updates at specified intervals
            if ((DateTime.Now - _lastTacticalCheck).TotalSeconds < _tacticalUpdateInterval)
                return impacts;

            _lastTacticalCheck = DateTime.Now;

            try
            {
                // Create match situations for each team
                var homeSituation = CreateMatchSituation(homeTeam, matchState, homeTeam);
                var awaySituation = CreateMatchSituation(awayTeam, matchState, homeTeam);

                // Process tactical decisions for both teams
                ProcessTeamTacticalDecisions(homeTeam, matchState, homeSituation, elapsedTime, impacts, homeTeam);
                ProcessTeamTacticalDecisions(awayTeam, matchState, awaySituation, elapsedTime, impacts, homeTeam);

                // Calculate formation effectiveness
                impacts.HomeFormationEffectiveness = _tacticalSystem.CalculateFormationEffectiveness(
                    homeTeam, awayTeam, matchState.CurrentPhase);
                impacts.AwayFormationEffectiveness = _tacticalSystem.CalculateFormationEffectiveness(
                    awayTeam, homeTeam, matchState.CurrentPhase);

                // Get player positioning modifiers
                impacts.HomePlayerModifiers = _tacticalSystem.GetPlayerPositioningModifiers(homeTeam, homePlayers);
                impacts.AwayPlayerModifiers = _tacticalSystem.GetPlayerPositioningModifiers(awayTeam, awayPlayers);

                // Calculate pressure and momentum effects
                impacts.HomePressureRating = _tacticalSystem.CalculatePressureRating(homeTeam, homeSituation);
                impacts.AwayPressureRating = _tacticalSystem.CalculatePressureRating(awayTeam, awaySituation);
                
                impacts.HomeMomentumModifier = _tacticalSystem.CalculateMomentumModifier(homeTeam, 
                    homeSituation.TeamMomentum, homeSituation);
                impacts.AwayMomentumModifier = _tacticalSystem.CalculateMomentumModifier(awayTeam, 
                    awaySituation.TeamMomentum, awaySituation);

            }
            catch (Exception ex)
            {
                CoreLogger.LogError($"[TacticalIntegration] Error processing tactical updates: {ex.Message}");
            }

            return impacts;
        }

        /// <summary>
        /// Get current tactical summary for a team
        /// </summary>
        public TacticalSummary GetTacticalSummary(TeamId teamId)
        {
            var currentPlan = _tacticalSystem.GetGamePlan(teamId);
            var initialPlan = _gameStartPlans.GetValueOrDefault(teamId);
            var adjustmentHistory = _adjustmentHistory.GetValueOrDefault(teamId, new List<TacticalAdjustmentResult>());

            return new TacticalSummary
            {
                TeamId = teamId,
                CurrentFormation = currentPlan.Formation.Name,
                InitialFormation = initialPlan?.Formation.Name ?? "Unknown",
                OffensiveStyle = currentPlan.OffensiveStrategy.Style,
                DefensiveStyle = currentPlan.DefensiveStrategy.Style,
                TotalAdjustments = adjustmentHistory.Count,
                SuccessfulAdjustments = adjustmentHistory.Count(a => a.Success),
                LastAdjustmentTime = adjustmentHistory.LastOrDefault()?.PlayerAdaptationTime ?? 0
            };
        }

        #endregion

        #region Private Methods

        private void ProcessTeamTacticalDecisions(TeamId teamId, MatchState matchState, MatchSituation situation, 
            float elapsedTime, TacticalImpacts impacts, TeamId homeTeam)
        {
            // Evaluate tactical situation
            var decision = _coachingAI.EvaluateTacticalSituation(teamId, matchState, situation, elapsedTime);
            
            if (decision.ShouldAdjust && decision.Adjustment != null)
            {
                // Execute the tactical decision
                _coachingAI.ExecuteTacticalDecision(decision, situation);
                
                // Record the adjustment
                var adjustmentHistory = _adjustmentHistory[teamId];
                var fakeResult = new TacticalAdjustmentResult 
                { 
                    Success = true, // For now, assume success for integration
                    EffectMagnitude = 0.1f,
                    PlayerAdaptationTime = 120
                };
                adjustmentHistory.Add(fakeResult);

                // Add to impacts for this update - check if this team is the home team
                if (teamId == homeTeam)
                {
                    impacts.HomeTacticalDecision = decision;
                }
                else
                {
                    impacts.AwayTacticalDecision = decision;
                }

                CoreLogger.Log($"[TacticalIntegration] Tactical adjustment made for {teamId}: {decision.GetDescription()}");
            }
        }

        private MatchSituation CreateMatchSituation(TeamId teamId, MatchState matchState, TeamId homeTeam)
        {
            return new MatchSituation
            {
                ScoreDifferential = matchState.GetScoreDifferential(teamId, homeTeam),
                TimeRemainingPercent = matchState.GetTimeRemainingPercent(),
                PossessionTurnover = matchState.GetTeamStat(teamId, "TurnoverRate", 0.4f),
                CurrentPhase = matchState.CurrentPhase,
                TeamMomentum = matchState.GetTeamMomentum(teamId),
                Weather = matchState.Weather,
                TeamStats = matchState.TeamStats.GetValueOrDefault(teamId, new Dictionary<string, float>())
            };
        }

        private TacticalGamePlan CreateInitialGamePlan(CoachingProfile coach)
        {
            var formation = FormationLibrary.GetFormation(coach.PreferredFormations.FirstOrDefault() ?? "Standard");
            
            return new TacticalGamePlan
            {
                Name = $"{coach.Name}'s Game Plan",
                Formation = formation,
                OffensiveStrategy = new OffensiveStrategy
                {
                    Style = coach.PreferredOffensiveStyle,
                    PacePreference = 40f + coach.Aggressiveness * 0.4f,
                    RiskTolerance = coach.RiskTolerance,
                    CorridorUsage = 50f + (coach.TacticalKnowledge - 50f) * 0.3f
                },
                DefensiveStrategy = new DefensiveStrategy
                {
                    Style = coach.PreferredDefensiveStyle,
                    PressureIntensity = 30f + coach.Aggressiveness * 0.4f,
                    Compactness = coach.Defensiveness * 0.6f + 20f,
                    DespersionPressureMultiplier = 1.0f + coach.UrgencySensitivity / 100f
                }
            };
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Force a specific tactical adjustment for testing or external control
        /// </summary>
        public bool ForceTeamTacticalAdjustment(TeamId teamId, TacticalAdjustment adjustment, MatchSituation situation)
        {
            try
            {
                var result = _tacticalSystem.MakeTacticalAdjustment(teamId, adjustment, situation, 1.0f);
                _adjustmentHistory[teamId].Add(result);
                
                CoreLogger.Log($"[TacticalIntegration] Forced tactical adjustment for {teamId}: {adjustment.Type}");
                return result.Success;
            }
            catch (Exception ex)
            {
                CoreLogger.LogError($"[TacticalIntegration] Error forcing tactical adjustment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get tactical effectiveness comparison between teams
        /// </summary>
        public TacticalComparison CompareTacticalEffectiveness(TeamId team1, TeamId team2, Phase currentPhase)
        {
            var team1Effectiveness = _tacticalSystem.CalculateFormationEffectiveness(team1, team2, currentPhase);
            var team2Effectiveness = _tacticalSystem.CalculateFormationEffectiveness(team2, team1, currentPhase);

            return new TacticalComparison
            {
                Team1 = team1,
                Team2 = team2,
                Team1Advantage = team1Effectiveness.GetPhaseAdvantage(currentPhase),
                Team2Advantage = team2Effectiveness.GetPhaseAdvantage(currentPhase),
                OverallAdvantage = team1Effectiveness.OverallAdvantage - team2Effectiveness.OverallAdvantage,
                Phase = currentPhase
            };
        }

        /// <summary>
        /// Get detailed tactical analytics for post-match analysis
        /// </summary>
        public TacticalAnalytics GetMatchTacticalAnalytics(TeamId teamId)
        {
            var currentPlan = _tacticalSystem.GetGamePlan(teamId);
            var adjustments = _adjustmentHistory.GetValueOrDefault(teamId, new List<TacticalAdjustmentResult>());

            return new TacticalAnalytics
            {
                TeamId = teamId,
                FinalFormation = currentPlan.Formation.Name,
                FinalOffensiveStyle = currentPlan.OffensiveStrategy.Style,
                FinalDefensiveStyle = currentPlan.DefensiveStrategy.Style,
                TotalAdjustmentAttempts = adjustments.Count,
                SuccessfulAdjustments = adjustments.Count(a => a.Success),
                AverageAdjustmentEffect = adjustments.Where(a => a.Success).DefaultIfEmpty()
                    .Average(a => a?.EffectMagnitude ?? 0f),
                TotalDisruptionFromFailedAdjustments = adjustments.Where(a => !a.Success)
                    .Sum(a => Math.Abs(a.Disruption))
            };
        }

        #endregion
    }
}