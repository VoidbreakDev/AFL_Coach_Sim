using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Tactics
{
    #region Match Impact Models

    /// <summary>
    /// Represents the tactical impacts that affect match simulation
    /// </summary>
    public class TacticalImpacts
    {
        // Formation effectiveness
        public FormationEffectiveness? HomeFormationEffectiveness { get; set; }
        public FormationEffectiveness? AwayFormationEffectiveness { get; set; }

        // Player positioning modifiers
        public Dictionary<string, PositionModifier> HomePlayerModifiers { get; set; } = new Dictionary<string, PositionModifier>();
        public Dictionary<string, PositionModifier> AwayPlayerModifiers { get; set; } = new Dictionary<string, PositionModifier>();

        // Team-wide tactical effects
        public float HomePressureRating { get; set; } = 1.0f;
        public float AwayPressureRating { get; set; } = 1.0f;
        public float HomeMomentumModifier { get; set; } = 1.0f;
        public float AwayMomentumModifier { get; set; } = 1.0f;

        // Tactical decisions made this update
        public TacticalDecision? HomeTacticalDecision { get; set; }
        public TacticalDecision? AwayTacticalDecision { get; set; }

        /// <summary>
        /// Check if any significant tactical impacts are present
        /// </summary>
        public bool HasSignificantImpacts()
        {
            return (HomeFormationEffectiveness?.OverallAdvantage ?? 0f) != 0f ||
                   (AwayFormationEffectiveness?.OverallAdvantage ?? 0f) != 0f ||
                   HomeTacticalDecision?.ShouldAdjust == true ||
                   AwayTacticalDecision?.ShouldAdjust == true;
        }

        /// <summary>
        /// Get the net tactical advantage for the home team
        /// </summary>
        public float GetHomeTacticalAdvantage(Phase currentPhase)
        {
            float homeAdvantage = HomeFormationEffectiveness?.GetPhaseAdvantage(currentPhase) ?? 0f;
            float awayAdvantage = AwayFormationEffectiveness?.GetPhaseAdvantage(currentPhase) ?? 0f;
            return homeAdvantage - awayAdvantage;
        }
    }

    #endregion

    #region Summary and Analysis Models

    /// <summary>
    /// Current tactical summary for a team
    /// </summary>
    public class TacticalSummary
    {
        public TeamId TeamId { get; set; }
        public string CurrentFormation { get; set; } = "Standard";
        public string InitialFormation { get; set; } = "Standard";
        public OffensiveStyle OffensiveStyle { get; set; }
        public DefensiveStyle DefensiveStyle { get; set; }
        public int TotalAdjustments { get; set; }
        public int SuccessfulAdjustments { get; set; }
        public int LastAdjustmentTime { get; set; } // Adaptation time in seconds

        /// <summary>
        /// Calculate adjustment success rate as percentage
        /// </summary>
        public float GetAdjustmentSuccessRate()
        {
            return TotalAdjustments == 0 ? 0f : (float)SuccessfulAdjustments / TotalAdjustments * 100f;
        }

        /// <summary>
        /// Check if formation has changed from initial
        /// </summary>
        public bool HasFormationChanged()
        {
            return CurrentFormation != InitialFormation;
        }
    }

    /// <summary>
    /// Detailed tactical analytics for post-match analysis
    /// </summary>
    public class TacticalAnalytics
    {
        public TeamId TeamId { get; set; }
        public string FinalFormation { get; set; } = "Standard";
        public OffensiveStyle FinalOffensiveStyle { get; set; }
        public DefensiveStyle FinalDefensiveStyle { get; set; }
        public int TotalAdjustmentAttempts { get; set; }
        public int SuccessfulAdjustments { get; set; }
        public float AverageAdjustmentEffect { get; set; }
        public float TotalDisruptionFromFailedAdjustments { get; set; }
        public Dictionary<TacticalAdjustmentType, int> AdjustmentTypeBreakdown { get; set; } = new Dictionary<TacticalAdjustmentType, int>();

        /// <summary>
        /// Calculate overall tactical performance score
        /// </summary>
        public float CalculateTacticalPerformanceScore()
        {
            float successRate = TotalAdjustmentAttempts == 0 ? 0.5f : (float)SuccessfulAdjustments / TotalAdjustmentAttempts;
            float effectivenessScore = AverageAdjustmentEffect * 10f; // Scale to 0-1 range
            float disruptionPenalty = TotalDisruptionFromFailedAdjustments * -5f;

            return Math.Max(0f, Math.Min(100f, (successRate * 50f) + effectivenessScore + disruptionPenalty + 25f));
        }

        /// <summary>
        /// Get tactical adaptability rating
        /// </summary>
        public string GetTacticalAdaptabilityRating()
        {
            float score = CalculateTacticalPerformanceScore();
            return score switch
            {
                >= 80f => "Excellent",
                >= 65f => "Good", 
                >= 50f => "Average",
                >= 35f => "Poor",
                _ => "Very Poor"
            };
        }
    }

    /// <summary>
    /// Comparison of tactical effectiveness between two teams
    /// </summary>
    public class TacticalComparison
    {
        public TeamId Team1 { get; set; }
        public TeamId Team2 { get; set; }
        public float Team1Advantage { get; set; }
        public float Team2Advantage { get; set; }
        public float OverallAdvantage { get; set; } // Positive = Team1 advantage, Negative = Team2 advantage
        public Phase Phase { get; set; }

        /// <summary>
        /// Get the team with the tactical advantage
        /// </summary>
        public TeamId? GetAdvantageousTeam()
        {
            if (Math.Abs(OverallAdvantage) < 0.02f) // Close to neutral
                return null;
            
            return OverallAdvantage > 0 ? Team1 : Team2;
        }

        /// <summary>
        /// Get a description of the tactical situation
        /// </summary>
        public string GetAdvantageDescription()
        {
            var advantageTeam = GetAdvantageousTeam();
            if (advantageTeam == null)
                return "Tactical stalemate - no significant advantage";

            float magnitude = Math.Abs(OverallAdvantage);
            string intensity = magnitude switch
            {
                >= 0.15f => "significant",
                >= 0.08f => "moderate", 
                >= 0.03f => "slight",
                _ => "minimal"
            };

            return $"{advantageTeam} has a {intensity} tactical advantage in {Phase} phase";
        }
    }

    #endregion

    #region Match Context Models

    /// <summary>
    /// Tactical snapshot at a specific point in the match
    /// </summary>
    public class TacticalSnapshot
    {
        public float TimeStamp { get; set; } // Match time in seconds
        public TeamId HomeTeam { get; set; }
        public TeamId AwayTeam { get; set; }
        public string HomeFormation { get; set; } = "Standard";
        public string AwayFormation { get; set; } = "Standard";
        public OffensiveStyle HomeOffensiveStyle { get; set; }
        public OffensiveStyle AwayOffensiveStyle { get; set; }
        public DefensiveStyle HomeDefensiveStyle { get; set; }
        public DefensiveStyle AwayDefensiveStyle { get; set; }
        public float HomePressure { get; set; }
        public float AwayPressure { get; set; }
        public Phase CurrentPhase { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }

        /// <summary>
        /// Create a tactical snapshot from current match state
        /// </summary>
        public static TacticalSnapshot Create(float timeStamp, TacticalSummary homeSummary, 
            TacticalSummary awaySummary, MatchState matchState, float homePressure, float awayPressure)
        {
            return new TacticalSnapshot
            {
                TimeStamp = timeStamp,
                HomeTeam = homeSummary.TeamId,
                AwayTeam = awaySummary.TeamId,
                HomeFormation = homeSummary.CurrentFormation,
                AwayFormation = awaySummary.CurrentFormation,
                HomeOffensiveStyle = homeSummary.OffensiveStyle,
                AwayOffensiveStyle = awaySummary.OffensiveStyle,
                HomeDefensiveStyle = homeSummary.DefensiveStyle,
                AwayDefensiveStyle = awaySummary.DefensiveStyle,
                HomePressure = homePressure,
                AwayPressure = awayPressure,
                CurrentPhase = matchState.CurrentPhase,
                HomeScore = matchState.HomeScore,
                AwayScore = matchState.AwayScore
            };
        }
    }

    /// <summary>
    /// Historical tactical evolution throughout a match
    /// </summary>
    public class TacticalEvolution
    {
        public List<TacticalSnapshot> Snapshots { get; set; } = new List<TacticalSnapshot>();
        public List<TacticalAdjustmentRecord> AllAdjustments { get; set; } = new List<TacticalAdjustmentRecord>();

        /// <summary>
        /// Add a tactical snapshot to the evolution history
        /// </summary>
        public void AddSnapshot(TacticalSnapshot snapshot)
        {
            Snapshots.Add(snapshot);
        }

        /// <summary>
        /// Add a tactical adjustment to the history
        /// </summary>
        public void AddAdjustment(TeamId teamId, TacticalAdjustment adjustment, bool success, MatchSituation situation)
        {
            AllAdjustments.Add(new TacticalAdjustmentRecord
            {
                Adjustment = adjustment,
                Success = success,
                Situation = situation,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Get tactical changes for a specific team
        /// </summary>
        public List<TacticalSnapshot> GetTeamTacticalChanges(TeamId teamId)
        {
            var changes = new List<TacticalSnapshot>();
            string lastFormation = null;
            OffensiveStyle? lastOffensive = null;
            DefensiveStyle? lastDefensive = null;

            foreach (var snapshot in Snapshots)
            {
                bool isHome = snapshot.HomeTeam == teamId;
                string currentFormation = isHome ? snapshot.HomeFormation : snapshot.AwayFormation;
                OffensiveStyle currentOffensive = isHome ? snapshot.HomeOffensiveStyle : snapshot.AwayOffensiveStyle;
                DefensiveStyle currentDefensive = isHome ? snapshot.HomeDefensiveStyle : snapshot.AwayDefensiveStyle;

                if (lastFormation != currentFormation || 
                    lastOffensive != currentOffensive || 
                    lastDefensive != currentDefensive)
                {
                    changes.Add(snapshot);
                    lastFormation = currentFormation;
                    lastOffensive = currentOffensive;
                    lastDefensive = currentDefensive;
                }
            }

            return changes;
        }

        /// <summary>
        /// Calculate tactical stability score for a team (0 = very unstable, 100 = very stable)
        /// </summary>
        public float CalculateTacticalStability(TeamId teamId)
        {
            var changes = GetTeamTacticalChanges(teamId);
            if (Snapshots.Count == 0) return 50f;

            float changeRatio = (float)changes.Count / Snapshots.Count;
            return Math.Max(0f, 100f * (1f - changeRatio * 2f)); // More changes = less stability
        }

        /// <summary>
        /// Get summary of tactical activity
        /// </summary>
        public string GetTacticalActivitySummary()
        {
            int totalSnapshots = Snapshots.Count;
            int totalAdjustments = AllAdjustments.Count;
            int successfulAdjustments = AllAdjustments.Count(a => a.Success);

            if (totalAdjustments == 0)
                return "No tactical adjustments made during the match";

            float successRate = (float)successfulAdjustments / totalAdjustments * 100f;
            
            return $"Tactical Summary: {totalAdjustments} adjustments attempted " +
                   $"({successfulAdjustments} successful, {successRate:F1}% success rate)";
        }
    }

    #endregion
}