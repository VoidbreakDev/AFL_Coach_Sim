using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Weather;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Tactics
{
    /// <summary>
    /// AI system that makes tactical coaching decisions during matches
    /// </summary>
    public class TacticalCoachingAI
    {
        private readonly AdvancedTacticalSystem _tacticalSystem;
        private readonly Random _random;
        private readonly Dictionary<TeamId, CoachingProfile> _coachProfiles;
        private readonly Dictionary<TeamId, DateTime> _lastAdjustmentTime;
        private readonly Dictionary<TeamId, List<TacticalTrigger>> _activeTriggers;

        public TacticalCoachingAI(AdvancedTacticalSystem tacticalSystem, int seed = 0)
        {
            _tacticalSystem = tacticalSystem;
            _random = seed == 0 ? new Random() : new Random(seed);
            _coachProfiles = new Dictionary<TeamId, CoachingProfile>();
            _lastAdjustmentTime = new Dictionary<TeamId, DateTime>();
            _activeTriggers = new Dictionary<TeamId, List<TacticalTrigger>>();
        }

        #region Public Interface

        /// <summary>
        /// Set the coaching profile for a team's coach
        /// </summary>
        public void SetCoachingProfile(TeamId teamId, CoachingProfile profile)
        {
            _coachProfiles[teamId] = profile;
            _activeTriggers[teamId] = CreateTacticalTriggers(profile);
            
            CoreLogger.Log($"[TacticalAI] Set coaching profile for {teamId}: {profile.Name} (Aggression: {profile.Aggressiveness})");
        }

        /// <summary>
        /// Evaluate if tactical adjustments should be made
        /// </summary>
        public TacticalDecision EvaluateTacticalSituation(TeamId teamId, MatchState currentState, 
            MatchSituation situation, float elapsedTime)
        {
            var profile = GetCoachProfile(teamId);
            var decision = new TacticalDecision { TeamId = teamId };

            // Check cooldown period
            if (IsInCooldownPeriod(teamId, elapsedTime, profile.MinTimeBetweenAdjustments))
            {
                decision.ShouldAdjust = false;
                decision.Reason = "Coach in cooldown period";
                return decision;
            }

            // Evaluate triggers
            var triggeredAdjustments = EvaluateTriggers(teamId, situation, currentState, profile);
            
            if (triggeredAdjustments.Any())
            {
                // Select best adjustment based on situation and coach profile
                var selectedAdjustment = SelectBestAdjustment(triggeredAdjustments, situation, profile);
                
                decision.ShouldAdjust = true;
                decision.Adjustment = selectedAdjustment;
                decision.Confidence = CalculateAdjustmentConfidence(selectedAdjustment, situation, profile);
                decision.Reason = $"Triggered by: {selectedAdjustment.Description}";

                CoreLogger.Log($"[TacticalAI] {teamId} coach decided on {selectedAdjustment.Type}: {decision.Reason}");
            }
            else
            {
                decision.ShouldAdjust = false;
                decision.Reason = "No tactical triggers activated";
            }

            return decision;
        }

        /// <summary>
        /// Execute a tactical decision and record the outcome
        /// </summary>
        public void ExecuteTacticalDecision(TacticalDecision decision, MatchSituation situation)
        {
            if (!decision.ShouldAdjust || decision.Adjustment == null)
                return;

            var profile = GetCoachProfile(decision.TeamId);
            
            // Apply coach skill multiplier
            float coachSkillMultiplier = CalculateCoachSkillMultiplier(profile, decision.Adjustment);
            
            // Make the adjustment
            var result = _tacticalSystem.MakeTacticalAdjustment(
                decision.TeamId, 
                decision.Adjustment, 
                situation, 
                coachSkillMultiplier);

            // Record the adjustment timing
            _lastAdjustmentTime[decision.TeamId] = DateTime.Now;

            // Update coach learning
            UpdateCoachLearning(decision.TeamId, decision.Adjustment, result.Success, situation);

            CoreLogger.Log($"[TacticalAI] Executed tactical decision for {decision.TeamId}: " +
                         $"{decision.Adjustment.Type} - {(result.Success ? "Success" : "Failed")}");
        }

        #endregion

        #region Trigger Evaluation

        private List<TacticalAdjustment> EvaluateTriggers(TeamId teamId, MatchSituation situation, 
            MatchState matchState, CoachingProfile profile)
        {
            var triggeredAdjustments = new List<TacticalAdjustment>();
            var triggers = _activeTriggers.GetValueOrDefault(teamId, new List<TacticalTrigger>());

            foreach (var trigger in triggers)
            {
                if (EvaluateTriggerCondition(trigger, situation, matchState, profile))
                {
                    var adjustment = CreateAdjustmentFromTrigger(trigger, situation, profile);
                    if (adjustment != null)
                    {
                        triggeredAdjustments.Add(adjustment);
                    }
                }
            }

            return triggeredAdjustments;
        }

        private bool EvaluateTriggerCondition(TacticalTrigger trigger, MatchSituation situation, 
            MatchState matchState, CoachingProfile profile)
        {
            switch (trigger.Type)
            {
                case TriggerType.ScoreDifferential:
                    return EvaluateScoreTrigger(trigger, situation.ScoreDifferential);
                    
                case TriggerType.TimeRemaining:
                    return EvaluateTimeTrigger(trigger, situation.TimeRemainingPercent);
                    
                case TriggerType.Momentum:
                    return EvaluateMomentumTrigger(trigger, situation.TeamMomentum);
                    
                case TriggerType.OpponentFormation:
                    return EvaluateFormationTrigger(trigger, matchState);
                    
                case TriggerType.PossessionTurnover:
                    return EvaluateTurnoverTrigger(trigger, situation.PossessionTurnover);
                    
                case TriggerType.WeatherChange:
                    return EvaluateWeatherTrigger(trigger, situation.Weather);
                    
                default:
                    return false;
            }
        }

        #endregion

        #region Trigger Conditions

        private bool EvaluateScoreTrigger(TacticalTrigger trigger, float scoreDifferential)
        {
            float threshold = (float)trigger.Parameters.GetValueOrDefault("threshold", 0f);
            string condition = trigger.Parameters.GetValueOrDefault("condition", "behind").ToString();

            return condition.ToLower() switch
            {
                "behind" => scoreDifferential <= -threshold,
                "ahead" => scoreDifferential >= threshold,
                "close" => Math.Abs(scoreDifferential) <= threshold,
                _ => false
            };
        }

        private bool EvaluateTimeTrigger(TacticalTrigger trigger, float timeRemainingPercent)
        {
            float threshold = (float)trigger.Parameters.GetValueOrDefault("threshold", 0.25f);
            return timeRemainingPercent <= threshold;
        }

        private bool EvaluateMomentumTrigger(TacticalTrigger trigger, float momentum)
        {
            float threshold = (float)trigger.Parameters.GetValueOrDefault("threshold", -0.3f);
            string condition = trigger.Parameters.GetValueOrDefault("condition", "negative").ToString();

            return condition.ToLower() switch
            {
                "negative" => momentum <= threshold,
                "positive" => momentum >= Math.Abs(threshold),
                _ => false
            };
        }

        private bool EvaluateFormationTrigger(TacticalTrigger trigger, MatchState matchState)
        {
            // This would need access to opponent formation data
            // For now, return a probability-based trigger
            float probability = (float)trigger.Parameters.GetValueOrDefault("probability", 0.1f);
            return _random.NextDouble() < probability;
        }

        private bool EvaluateTurnoverTrigger(TacticalTrigger trigger, float turnoverRate)
        {
            float threshold = (float)trigger.Parameters.GetValueOrDefault("threshold", 0.6f);
            return turnoverRate >= threshold;
        }

        private bool EvaluateWeatherTrigger(TacticalTrigger trigger, AFLCoachSim.Core.Engine.Match.Weather.Weather weather)
        {
            var targetWeather = (AFLCoachSim.Core.Engine.Match.Weather.Weather)trigger.Parameters.GetValueOrDefault("weather", (int)AFLCoachSim.Core.Engine.Match.Weather.Weather.Clear);
            return weather == targetWeather && weather != AFLCoachSim.Core.Engine.Match.Weather.Weather.Clear;
        }

        #endregion

        #region Adjustment Creation and Selection

        private TacticalAdjustment CreateAdjustmentFromTrigger(TacticalTrigger trigger, MatchSituation situation, 
            CoachingProfile profile)
        {
            var adjustment = new TacticalAdjustment
            {
                Type = trigger.PreferredAdjustmentType,
                Description = trigger.Description,
                RequestedAt = DateTime.Now
            };

            switch (trigger.PreferredAdjustmentType)
            {
                case TacticalAdjustmentType.FormationChange:
                    adjustment.NewFormation = SelectFormationForSituation(situation, profile);
                    break;
                    
                case TacticalAdjustmentType.PressureIntensity:
                    adjustment.NewValue = CalculatePressureAdjustment(situation, profile);
                    break;
                    
                case TacticalAdjustmentType.OffensiveStyle:
                    adjustment.NewOffensiveStyle = SelectOffensiveStyleForSituation(situation, profile);
                    break;
                    
                case TacticalAdjustmentType.DefensiveStructure:
                    adjustment.NewDefensiveStyle = SelectDefensiveStyleForSituation(situation, profile);
                    break;
            }

            return adjustment;
        }

        private TacticalAdjustment SelectBestAdjustment(List<TacticalAdjustment> adjustments, MatchSituation situation, 
            CoachingProfile profile)
        {
            // Score each adjustment based on situation appropriateness and coach style
            var scoredAdjustments = adjustments.Select(adj => new
            {
                Adjustment = adj,
                Score = CalculateAdjustmentScore(adj, situation, profile)
            }).OrderByDescending(x => x.Score);

            return scoredAdjustments.First().Adjustment;
        }

        private float CalculateAdjustmentScore(TacticalAdjustment adjustment, MatchSituation situation, 
            CoachingProfile profile)
        {
            float score = 0.5f; // Base score

            // Situation appropriateness
            score += CalculateSituationAppropriatenessScore(adjustment, situation);
            
            // Coach style alignment
            score += CalculateCoachStyleAlignmentScore(adjustment, profile);
            
            // Urgency factor
            score += CalculateUrgencyScore(adjustment, situation);

            return Math.Max(0f, Math.Min(1f, score));
        }

        #endregion

        #region Formation and Style Selection

        private Formation SelectFormationForSituation(MatchSituation situation, CoachingProfile profile)
        {
            if (situation.ScoreDifferential < -18) // Behind by 3+ goals
            {
                return profile.Aggressiveness > 70 ? FormationLibrary.GetFormation("Attacking") : 
                       FormationLibrary.GetFormation("Pressing");
            }
            else if (situation.ScoreDifferential > 18) // Ahead by 3+ goals
            {
                return profile.Defensiveness > 70 ? FormationLibrary.GetFormation("Flooding") :
                       FormationLibrary.GetFormation("Defensive");
            }
            else if (situation.TimeRemainingPercent < 0.15f) // Last 10 minutes
            {
                return situation.ScoreDifferential < 0 ? 
                       FormationLibrary.GetFormation("Attacking") : 
                       FormationLibrary.GetFormation("Defensive");
            }

            // Default to balanced approach
            return FormationLibrary.GetFormation("Standard");
        }

        private OffensiveStyle SelectOffensiveStyleForSituation(MatchSituation situation, CoachingProfile profile)
        {
            if (situation.ScoreDifferential < -12 && situation.TimeRemainingPercent < 0.25f)
                return OffensiveStyle.FastBreak; // Need quick scores
                
            if (situation.TeamMomentum < -0.2f)
                return OffensiveStyle.Possession; // Steady the ship
                
            if (profile.Aggressiveness > 80)
                return OffensiveStyle.Chaos; // High-risk coach
                
            return OffensiveStyle.Balanced;
        }

        private DefensiveStyle SelectDefensiveStyleForSituation(MatchSituation situation, CoachingProfile profile)
        {
            if (situation.ScoreDifferential > 12 && situation.TimeRemainingPercent < 0.33f)
                return DefensiveStyle.Flooding; // Protect the lead
                
            if (situation.PossessionTurnover > 0.6f)
                return DefensiveStyle.Pressing; // Force more turnovers
                
            if (profile.Defensiveness > 80)
                return DefensiveStyle.ManOnMan; // Defensive-minded coach
                
            return DefensiveStyle.Zoning;
        }

        private float CalculatePressureAdjustment(MatchSituation situation, CoachingProfile profile)
        {
            float basePressure = 50f; // Default pressure level
            
            // Adjust based on game situation
            if (situation.ScoreDifferential < -12) // Behind, increase pressure
                basePressure += 20f;
            else if (situation.ScoreDifferential > 12) // Ahead, may decrease pressure
                basePressure -= 10f;
                
            // Time-based adjustments
            if (situation.TimeRemainingPercent < 0.25f) // Final quarter
                basePressure += 15f;
                
            // Coach personality influence
            basePressure += (profile.Aggressiveness - 50f) * 0.3f;
            
            return Math.Max(10f, Math.Min(90f, basePressure));
        }

        #endregion

        #region Helper Methods

        private CoachingProfile GetCoachProfile(TeamId teamId)
        {
            return _coachProfiles.GetValueOrDefault(teamId, CreateDefaultCoachProfile());
        }

        private CoachingProfile CreateDefaultCoachProfile()
        {
            return new CoachingProfile
            {
                Name = "Default Coach",
                Aggressiveness = 50f,
                Defensiveness = 50f,
                TacticalKnowledge = 50f,
                Adaptability = 50f,
                MinTimeBetweenAdjustments = 300f // 5 minutes
            };
        }

        private bool IsInCooldownPeriod(TeamId teamId, float currentTime, float cooldownSeconds)
        {
            if (!_lastAdjustmentTime.ContainsKey(teamId))
                return false;

            var timeSinceLastAdjustment = DateTime.Now - _lastAdjustmentTime[teamId];
            return timeSinceLastAdjustment.TotalSeconds < cooldownSeconds;
        }

        private float CalculateCoachSkillMultiplier(CoachingProfile profile, TacticalAdjustment adjustment)
        {
            float baseMultiplier = (profile.TacticalKnowledge + profile.Adaptability) / 100f;
            
            // Certain adjustments benefit from specific coach traits
            switch (adjustment.Type)
            {
                case TacticalAdjustmentType.FormationChange:
                    baseMultiplier += profile.TacticalKnowledge / 200f; // Extra bonus for tactical knowledge
                    break;
                case TacticalAdjustmentType.PressureIntensity:
                    baseMultiplier += profile.Aggressiveness / 200f;
                    break;
            }

            return Math.Max(0.5f, Math.Min(1.5f, baseMultiplier));
        }

        private float CalculateAdjustmentConfidence(TacticalAdjustment adjustment, MatchSituation situation, 
            CoachingProfile profile)
        {
            float confidence = 0.7f; // Base confidence
            
            // Increase confidence for situations the coach is good at
            if (adjustment.Type == TacticalAdjustmentType.FormationChange)
                confidence += profile.TacticalKnowledge / 200f;
                
            // Decrease confidence in high-pressure situations
            if (Math.Abs(situation.ScoreDifferential) > 24)
                confidence *= 0.85f;
                
            return Math.Max(0.2f, Math.Min(1.0f, confidence));
        }

        private List<TacticalTrigger> CreateTacticalTriggers(CoachingProfile profile)
        {
            var triggers = new List<TacticalTrigger>();

            // Behind by significant margin
            triggers.Add(new TacticalTrigger
            {
                Type = TriggerType.ScoreDifferential,
                Description = "Behind by 3+ goals, switch to attacking",
                PreferredAdjustmentType = TacticalAdjustmentType.FormationChange,
                Parameters = new Dictionary<string, object> { ["threshold"] = 18f, ["condition"] = "behind" }
            });

            // Late in game and close
            triggers.Add(new TacticalTrigger
            {
                Type = TriggerType.TimeRemaining,
                Description = "Final quarter tactical adjustment",
                PreferredAdjustmentType = TacticalAdjustmentType.OffensiveStyle,
                Parameters = new Dictionary<string, object> { ["threshold"] = 0.25f }
            });

            // Momentum swinging against us
            triggers.Add(new TacticalTrigger
            {
                Type = TriggerType.Momentum,
                Description = "Counter negative momentum",
                PreferredAdjustmentType = TacticalAdjustmentType.PressureIntensity,
                Parameters = new Dictionary<string, object> { ["threshold"] = -0.3f, ["condition"] = "negative" }
            });

            // High turnover rate
            triggers.Add(new TacticalTrigger
            {
                Type = TriggerType.PossessionTurnover,
                Description = "High turnovers, increase pressure",
                PreferredAdjustmentType = TacticalAdjustmentType.DefensiveStructure,
                Parameters = new Dictionary<string, object> { ["threshold"] = 0.65f }
            });

            return triggers;
        }

        private float CalculateSituationAppropriatenessScore(TacticalAdjustment adjustment, MatchSituation situation)
        {
            // This would contain logic to score how appropriate an adjustment is for the current situation
            // For now, return a base score with some situation-specific modifiers
            float score = 0.5f;
            
            if (adjustment.Type == TacticalAdjustmentType.FormationChange && Math.Abs(situation.ScoreDifferential) > 12)
                score += 0.2f;
                
            if (adjustment.Type == TacticalAdjustmentType.PressureIntensity && situation.TimeRemainingPercent < 0.25f)
                score += 0.15f;
                
            return score;
        }

        private float CalculateCoachStyleAlignmentScore(TacticalAdjustment adjustment, CoachingProfile profile)
        {
            float score = 0f;
            
            switch (adjustment.Type)
            {
                case TacticalAdjustmentType.FormationChange:
                    score = profile.TacticalKnowledge / 100f * 0.3f;
                    break;
                case TacticalAdjustmentType.PressureIntensity:
                    score = profile.Aggressiveness / 100f * 0.2f;
                    break;
                case TacticalAdjustmentType.DefensiveStructure:
                    score = profile.Defensiveness / 100f * 0.2f;
                    break;
            }
            
            return score;
        }

        private float CalculateUrgencyScore(TacticalAdjustment adjustment, MatchSituation situation)
        {
            float urgency = 0f;
            
            // More urgent if losing and time running out
            if (situation.ScoreDifferential < 0 && situation.TimeRemainingPercent < 0.25f)
                urgency += 0.3f;
                
            // More urgent if momentum strongly against
            if (situation.TeamMomentum < -0.4f)
                urgency += 0.2f;
                
            return urgency;
        }

        private void UpdateCoachLearning(TeamId teamId, TacticalAdjustment adjustment, bool success, 
            MatchSituation situation)
        {
            // This could implement a learning system where coaches get better at making
            // certain types of adjustments based on their historical success
            // For now, just log the outcome
            
            CoreLogger.Log($"[TacticalAI] Coach learning update - {teamId} {adjustment.Type}: " +
                         $"{(success ? "Success" : "Failure")} in situation {situation.ScoreDifferential}pts");
        }

        #endregion
    }
}