using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Ratings
{
    #region Enums

    /// <summary>
    /// Types of performance events that affect player form and ratings
    /// </summary>
    public enum PerformanceEventType
    {
        Goal,               // Scored a goal
        Assist,             // Assisted a goal
        Mark,               // Took a mark
        Tackle,             // Made a tackle
        Intercept,          // Intercepted the ball
        Turnover,           // Lost possession/bad decision
        MissedShot,         // Missed a shot on goal
        FreeKickFor,        // Earned a free kick
        FreeKickAgainst,    // Gave away a free kick
        Handball,           // Successful handball
        Kick,               // Successful kick
        Contest,            // Won a contest
        ContestLost,        // Lost a contest
        CleanPossession,    // Clean pick up
        Fumble              // Dropped the ball
    }

    /// <summary>
    /// Types of momentum events that affect match flow
    /// </summary>
    public enum MomentumEventType
    {
        Goal,               // Single goal scored
        QuickGoals,         // Multiple goals in quick succession
        DefensiveStop,      // Prevented a scoring opportunity
        Turnover,           // Lost possession in dangerous area
        Injury,             // Player injury
        FreeKickSpray,      // Multiple free kicks against
        TimeWasting,        // Deliberate time wasting
        CrowdBoost,         // Crowd energy boost
        WeatherChange       // Weather condition change
    }

    /// <summary>
    /// Form trend indicators
    /// </summary>
    public enum FormTrend
    {
        Improving,      // Form is trending upward
        Stable,         // Form is consistent
        Declining,      // Form is trending downward
        Volatile        // Form is inconsistent
    }

    /// <summary>
    /// Player confidence levels
    /// </summary>
    public enum ConfidenceLevel
    {
        VeryLow,        // 0-40%
        Low,            // 40-60%
        Average,        // 60-75%
        High,           // 75-90%
        VeryHigh        // 90-100%
    }

    /// <summary>
    /// Pressure situations that affect performance
    /// </summary>
    public enum PressureSituation
    {
        Normal,         // Standard game situation
        CloseGame,      // Score differential within 2 goals
        FinalQuarter,   // 4th quarter pressure
        FinalMinutes,   // Last 5 minutes
        BehindByLot,    // Team trailing by significant margin
        AheadByLot,     // Team leading by significant margin
        HomeAdvantage,  // Playing at home ground
        AwayPressure    // Playing away from home
    }

    #endregion

    #region Core Rating Classes

    /// <summary>
    /// Dynamic rating state for an individual player
    /// </summary>
    public class PlayerDynamicRating
    {
        public Guid PlayerId { get; set; }
        
        // Base attributes (don't change during match)
        public float BaseSpeed { get; set; }
        public float BaseAccuracy { get; set; }
        public float BaseEndurance { get; set; }
        public Role Position { get; set; }
        public int Age { get; set; }
        public bool IsHomePlayer { get; set; }
        
        // Current dynamic ratings (change during match)
        public float CurrentSpeed { get; private set; }
        public float CurrentAccuracy { get; private set; }
        public float CurrentEndurance { get; private set; }
        
        // Rating change tracking
        public float SpeedChange => CurrentSpeed - BaseSpeed;
        public float AccuracyChange => CurrentAccuracy - BaseAccuracy;
        public float EnduranceChange => CurrentEndurance - BaseEndurance;
        
        // Analysis data
        public RatingModifierBreakdown LastUpdateModifiers { get; set; }
        public List<PerformanceSnapshot> PerformanceHistory { get; set; } = new List<PerformanceSnapshot>();
        public DateTime LastRatingUpdate { get; set; }

        public PlayerDynamicRating(Guid playerId)
        {
            PlayerId = playerId;
            LastRatingUpdate = DateTime.Now;
        }

        /// <summary>
        /// Update current ratings
        /// </summary>
        public void UpdateCurrentRatings(float speed, float accuracy, float endurance)
        {
            CurrentSpeed = speed;
            CurrentAccuracy = accuracy;
            CurrentEndurance = endurance;
            LastRatingUpdate = DateTime.Now;
        }

        /// <summary>
        /// Calculate overall performance rating (0-100)
        /// </summary>
        public float GetCurrentPerformanceRating()
        {
            return (CurrentSpeed + CurrentAccuracy + CurrentEndurance) / 3f;
        }

        /// <summary>
        /// Get performance rating relative to base (can be negative)
        /// </summary>
        public float GetRelativePerformanceRating()
        {
            float baseRating = (BaseSpeed + BaseAccuracy + BaseEndurance) / 3f;
            return GetCurrentPerformanceRating() - baseRating;
        }

        /// <summary>
        /// Get performance trend over recent history
        /// </summary>
        public FormTrend GetPerformanceTrend()
        {
            if (PerformanceHistory.Count < 5) return FormTrend.Stable;

            var recentPerformance = PerformanceHistory.TakeLast(5).ToList();
            var ratings = recentPerformance.Select(p => (p.CurrentSpeed + p.CurrentAccuracy + p.CurrentEndurance) / 3f).ToList();
            
            // Calculate trend using linear regression
            float avgRating = ratings.Average();
            float variance = ratings.Sum(r => (r - avgRating) * (r - avgRating)) / ratings.Count;
            
            if (variance < 4f) // Low variance
            {
                return FormTrend.Stable;
            }
            
            // Check if trending up or down
            float firstHalf = ratings.Take(2).Average();
            float secondHalf = ratings.TakeLast(2).Average();
            
            if (secondHalf > firstHalf + 2f) return FormTrend.Improving;
            if (secondHalf < firstHalf - 2f) return FormTrend.Declining;
            if (variance > 16f) return FormTrend.Volatile;
            
            return FormTrend.Stable;
        }
    }

    /// <summary>
    /// Player form and confidence state
    /// </summary>
    public class PlayerFormState
    {
        public Guid PlayerId { get; set; }
        public float CurrentForm { get; set; } = 75f; // 0-100, where 75 is average
        public float Confidence { get; set; } = 75f; // 0-100
        public float Composure { get; set; } = 70f; // How well player handles pressure
        public List<PerformanceEvent> RecentPerformances { get; set; } = new List<PerformanceEvent>();
        public DateTime LastUpdated { get; set; }

        public PlayerFormState(Guid playerId)
        {
            PlayerId = playerId;
            LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Get confidence level category
        /// </summary>
        public ConfidenceLevel GetConfidenceLevel()
        {
            return Confidence switch
            {
                < 40f => ConfidenceLevel.VeryLow,
                < 60f => ConfidenceLevel.Low,
                < 75f => ConfidenceLevel.Average,
                < 90f => ConfidenceLevel.High,
                _ => ConfidenceLevel.VeryHigh
            };
        }

        /// <summary>
        /// Get recent performance trend
        /// </summary>
        public FormTrend GetFormTrend()
        {
            if (RecentPerformances.Count < 3) return FormTrend.Stable;

            var positiveEvents = RecentPerformances.Count(e => IsPositiveEvent(e.EventType));
            var negativeEvents = RecentPerformances.Count(e => IsNegativeEvent(e.EventType));
            
            if (positiveEvents > negativeEvents * 2) return FormTrend.Improving;
            if (negativeEvents > positiveEvents * 2) return FormTrend.Declining;
            
            return FormTrend.Stable;
        }

        private bool IsPositiveEvent(PerformanceEventType eventType)
        {
            return eventType == PerformanceEventType.Goal ||
                   eventType == PerformanceEventType.Assist ||
                   eventType == PerformanceEventType.Mark ||
                   eventType == PerformanceEventType.Tackle ||
                   eventType == PerformanceEventType.Intercept;
        }

        private bool IsNegativeEvent(PerformanceEventType eventType)
        {
            return eventType == PerformanceEventType.Turnover ||
                   eventType == PerformanceEventType.MissedShot ||
                   eventType == PerformanceEventType.FreeKickAgainst ||
                   eventType == PerformanceEventType.Fumble;
        }
    }

    /// <summary>
    /// Player pressure state and handling
    /// </summary>
    public class PlayerPressureState
    {
        public Guid PlayerId { get; set; }
        public float CurrentPressure { get; set; } = 0f; // 0-1.0 current pressure level
        public float AveragePressureHandling { get; set; } = 0.5f; // How well player typically handles pressure
        public List<PressureSituation> ActivePressures { get; set; } = new List<PressureSituation>();
        public Dictionary<PressureSituation, float> PressureHistory { get; set; } = new Dictionary<PressureSituation, float>();

        public PlayerPressureState(Guid playerId)
        {
            PlayerId = playerId;
        }

        /// <summary>
        /// Add a pressure situation
        /// </summary>
        public void AddPressure(PressureSituation pressure)
        {
            if (!ActivePressures.Contains(pressure))
            {
                ActivePressures.Add(pressure);
            }
        }

        /// <summary>
        /// Remove a pressure situation
        /// </summary>
        public void RemovePressure(PressureSituation pressure)
        {
            ActivePressures.Remove(pressure);
        }

        /// <summary>
        /// Calculate combined pressure level
        /// </summary>
        public float GetCombinedPressureLevel()
        {
            if (!ActivePressures.Any()) return 0f;

            float totalPressure = 0f;
            foreach (var pressure in ActivePressures)
            {
                totalPressure += GetPressureWeight(pressure);
            }

            return Math.Min(1f, totalPressure);
        }

        private float GetPressureWeight(PressureSituation pressure)
        {
            return pressure switch
            {
                PressureSituation.FinalMinutes => 0.5f,
                PressureSituation.FinalQuarter => 0.3f,
                PressureSituation.CloseGame => 0.4f,
                PressureSituation.BehindByLot => 0.2f,
                PressureSituation.AheadByLot => -0.1f,
                PressureSituation.HomeAdvantage => -0.05f,
                PressureSituation.AwayPressure => 0.1f,
                _ => 0f
            };
        }
    }

    /// <summary>
    /// Player vs Player matchup information
    /// </summary>
    public class PlayerMatchup
    {
        public Guid PlayerId { get; set; }
        public Guid OpponentId { get; set; }
        public Player Player { get; set; }
        public Player Opponent { get; set; }
        public float MatchupAdvantage { get; set; } = 0f; // -1.0 to +1.0
        public DateTime EstablishedAt { get; set; }
        public List<MatchupEvent> Events { get; set; } = new List<MatchupEvent>();

        public PlayerMatchup(Guid playerId, Guid opponentId, Player player, Player opponent)
        {
            PlayerId = playerId;
            OpponentId = opponentId;
            Player = player;
            Opponent = opponent;
            EstablishedAt = DateTime.Now;
            MatchupAdvantage = CalculateInitialAdvantage();
        }

        /// <summary>
        /// Calculate matchup advantage based on attributes
        /// </summary>
        public float CalculateAdvantage()
        {
            // Recalculate based on current performance and history
            float attributeAdvantage = CalculateAttributeAdvantage();
            float historicalAdvantage = CalculateHistoricalAdvantage();
            
            // Weight current attributes more heavily
            return attributeAdvantage * 0.7f + historicalAdvantage * 0.3f;
        }

        private float CalculateInitialAdvantage()
        {
            return CalculateAttributeAdvantage();
        }

        private float CalculateAttributeAdvantage()
        {
            // Position-specific matchup calculations
            return Player.Role switch
            {
                Role.Forward when Opponent.Role == Role.Defender => CalculateForwardDefenderAdvantage(),
                Role.Defender when Opponent.Role == Role.Forward => CalculateDefenderForwardAdvantage(),
                Role.Midfielder => CalculateMidfieldAdvantage(),
                Role.Ruck => CalculateRuckAdvantage(),
                _ => 0f
            };
        }

        private float CalculateForwardDefenderAdvantage()
        {
            // Forward advantages: speed and accuracy vs defender's defensive skill
            float speedAdvantage = (Player.Speed - Opponent.Speed) / 100f;
            float accuracyAdvantage = (Player.Accuracy - Opponent.Accuracy) / 200f; // Less weighted
            
            return Math.Max(-0.5f, Math.Min(0.5f, speedAdvantage * 0.6f + accuracyAdvantage * 0.4f));
        }

        private float CalculateDefenderForwardAdvantage()
        {
            // Inverse of forward vs defender
            return -CalculateForwardDefenderAdvantage();
        }

        private float CalculateMidfieldAdvantage()
        {
            // Midfield matchups are more balanced, focus on overall attributes
            float speedDiff = (Player.Speed - Opponent.Speed) / 100f;
            float accuracyDiff = (Player.Accuracy - Opponent.Accuracy) / 100f;
            float enduranceDiff = (Player.Endurance - Opponent.Endurance) / 100f;
            
            float advantage = (speedDiff * 0.4f + accuracyDiff * 0.3f + enduranceDiff * 0.3f);
            return Math.Max(-0.3f, Math.Min(0.3f, advantage));
        }

        private float CalculateRuckAdvantage()
        {
            // Ruck matchups favor physical attributes and experience
            float enduranceAdvantage = (Player.Endurance - Opponent.Endurance) / 100f;
            float experienceAdvantage = Math.Sign(Player.Age - Opponent.Age) * 0.1f;
            
            return Math.Max(-0.4f, Math.Min(0.4f, enduranceAdvantage * 0.7f + experienceAdvantage * 0.3f));
        }

        private float CalculateHistoricalAdvantage()
        {
            if (!Events.Any()) return 0f;

            int wins = Events.Count(e => e.PlayerWon);
            int total = Events.Count;
            
            float winRate = (float)wins / total;
            return (winRate - 0.5f) * 2f; // Convert to -1 to +1 range
        }
    }

    #endregion

    #region Context and Event Classes

    /// <summary>
    /// Performance event that affects player form
    /// </summary>
    public class PerformanceEvent
    {
        public PerformanceEventType EventType { get; set; }
        public float Quality { get; set; } = 1.0f; // 0-2.0, where 1.0 is average quality
        public DateTime Timestamp { get; set; }
        public string Context { get; set; } = ""; // Additional context
        public bool UnderPressure { get; set; } = false;

        public PerformanceEvent(PerformanceEventType eventType)
        {
            EventType = eventType;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Calculate the impact value of this event
        /// </summary>
        public float GetImpactValue()
        {
            float baseImpact = GetBaseImpact();
            float qualityMultiplier = Quality;
            float pressureMultiplier = UnderPressure ? 1.5f : 1.0f;
            
            return baseImpact * qualityMultiplier * pressureMultiplier;
        }

        private float GetBaseImpact()
        {
            return EventType switch
            {
                PerformanceEventType.Goal => 5f,
                PerformanceEventType.Assist => 3f,
                PerformanceEventType.Mark => 2f,
                PerformanceEventType.Tackle => 2f,
                PerformanceEventType.Intercept => 3f,
                PerformanceEventType.Turnover => -3f,
                PerformanceEventType.MissedShot => -2f,
                PerformanceEventType.FreeKickAgainst => -1f,
                PerformanceEventType.FreeKickFor => 1f,
                PerformanceEventType.Contest => 1f,
                PerformanceEventType.ContestLost => -1f,
                PerformanceEventType.Fumble => -2f,
                _ => 0f
            };
        }
    }

    /// <summary>
    /// Momentum event that affects match flow
    /// </summary>
    public class MomentumEvent
    {
        public MomentumEventType EventType { get; set; }
        public bool IsHomeTeam { get; set; }
        public float Intensity { get; set; } = 1.0f; // 0-2.0 intensity multiplier
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = "";

        public MomentumEvent(MomentumEventType eventType, bool isHomeTeam)
        {
            EventType = eventType;
            IsHomeTeam = isHomeTeam;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Context for rating updates
    /// </summary>
    public class RatingUpdateContext
    {
        public int Quarter { get; set; }
        public float TimeRemaining { get; set; } // Seconds
        public float ScoreDifferential { get; set; } // Positive = home leading
        public int CrowdSize { get; set; }
        public List<PressureSituation> ActivePressures { get; set; } = new List<PressureSituation>();

        public RatingUpdateContext Clone()
        {
            return new RatingUpdateContext
            {
                Quarter = Quarter,
                TimeRemaining = TimeRemaining,
                ScoreDifferential = ScoreDifferential,
                CrowdSize = CrowdSize,
                ActivePressures = new List<PressureSituation>(ActivePressures)
            };
        }
    }

    /// <summary>
    /// Match context information
    /// </summary>
    public class MatchContext
    {
        public Guid MatchId { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string Venue { get; set; }
        public int CrowdSize { get; set; }
        public bool IsNightGame { get; set; }
        public bool IsFinalSeries { get; set; }
        public float HomeGroundAdvantage { get; set; } = 0.05f; // Default 5% advantage
        public DateTime MatchStart { get; set; }
    }

    /// <summary>
    /// Individual matchup contest result
    /// </summary>
    public class MatchupEvent
    {
        public DateTime Timestamp { get; set; }
        public bool PlayerWon { get; set; }
        public string EventType { get; set; } // "Contest", "Mark", "Tackle", etc.
        public float MarginOfVictory { get; set; } = 0f; // How convincing the win was

        public MatchupEvent(bool playerWon, string eventType)
        {
            PlayerWon = playerWon;
            EventType = eventType;
            Timestamp = DateTime.Now;
        }
    }

    #endregion

    #region Modifier and Analysis Classes

    /// <summary>
    /// Rating modifier for a specific factor
    /// </summary>
    public class RatingModifier
    {
        public float SpeedImpact { get; set; } = 0f;
        public float AccuracyImpact { get; set; } = 0f;
        public float EnduranceImpact { get; set; } = 0f;

        /// <summary>
        /// Calculate overall impact magnitude
        /// </summary>
        public float GetOverallImpact()
        {
            return Math.Abs(SpeedImpact) + Math.Abs(AccuracyImpact) + Math.Abs(EnduranceImpact);
        }

        /// <summary>
        /// Check if this modifier has positive overall impact
        /// </summary>
        public bool IsPositive()
        {
            return (SpeedImpact + AccuracyImpact + EnduranceImpact) > 0f;
        }
    }

    /// <summary>
    /// Complete breakdown of all rating modifiers
    /// </summary>
    public class RatingModifierBreakdown
    {
        public RatingModifier FormModifier { get; set; } = new RatingModifier();
        public RatingModifier FatigueModifier { get; set; } = new RatingModifier();
        public RatingModifier PressureModifier { get; set; } = new RatingModifier();
        public RatingModifier MatchupModifier { get; set; } = new RatingModifier();
        public RatingModifier WeatherModifier { get; set; } = new RatingModifier();
        public RatingModifier SituationalModifier { get; set; } = new RatingModifier();
        public RatingModifier MomentumModifier { get; set; } = new RatingModifier();
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Get the most significant modifier
        /// </summary>
        public (string Name, RatingModifier Modifier) GetMostSignificantModifier()
        {
            var modifiers = new[]
            {
                ("Form", FormModifier),
                ("Fatigue", FatigueModifier),
                ("Pressure", PressureModifier),
                ("Matchup", MatchupModifier),
                ("Weather", WeatherModifier),
                ("Situational", SituationalModifier),
                ("Momentum", MomentumModifier)
            };

            return modifiers.OrderByDescending(m => m.Modifier.GetOverallImpact()).First();
        }
    }

    /// <summary>
    /// Performance snapshot at a point in time
    /// </summary>
    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public float CurrentSpeed { get; set; }
        public float CurrentAccuracy { get; set; }
        public float CurrentEndurance { get; set; }
        public RatingUpdateContext Context { get; set; }

        public float GetOverallRating()
        {
            return (CurrentSpeed + CurrentAccuracy + CurrentEndurance) / 3f;
        }
    }

    /// <summary>
    /// System analytics and statistics
    /// </summary>
    public class DynamicRatingsAnalytics
    {
        public int TotalPlayers { get; set; }
        public float AverageForm { get; set; }
        public float AverageConfidence { get; set; }
        public float CurrentMomentum { get; set; }
        public int ActiveMatchups { get; set; }
        public List<Guid> TopPerformers { get; set; } = new List<Guid>();
        public List<Guid> BottomPerformers { get; set; } = new List<Guid>();
        
        /// <summary>
        /// Get momentum description
        /// </summary>
        public string GetMomentumDescription()
        {
            return CurrentMomentum switch
            {
                > 0.7f => "Strong Home Momentum",
                > 0.3f => "Moderate Home Momentum",
                > 0.1f => "Slight Home Momentum", 
                > -0.1f => "Balanced",
                > -0.3f => "Slight Away Momentum",
                > -0.7f => "Moderate Away Momentum",
                _ => "Strong Away Momentum"
            };
        }
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Configuration for the dynamic ratings system
    /// </summary>
    public class DynamicRatingsConfiguration
    {
        // Update rates (how quickly ratings change)
        public float FormUpdateRate { get; set; } = 0.1f;
        public float ConfidenceUpdateRate { get; set; } = 0.15f;
        
        // Decay rates (how quickly things return to baseline)
        public float FormDecayRate { get; set; } = 0.005f;
        public float ConfidenceDecayRate { get; set; } = 0.01f;
        public float MomentumDecayRate { get; set; } = 0.95f; // Per update
        
        // Impact strengths
        public float FormImpactStrength { get; set; } = 0.3f;
        public float FatigueImpactStrength { get; set; } = 0.4f;
        public float PressureImpactStrength { get; set; } = 0.25f;
        public float MatchupImpactStrength { get; set; } = 0.2f;
        public float WeatherImpactStrength { get; set; } = 0.15f;
        public float MomentumImpactStrength { get; set; } = 0.2f;
        
        // Home/away effects
        public float HomeAdvantage { get; set; } = 0.05f; // 5% bonus
        public float AwayDisadvantage { get; set; } = 0.03f; // 3% penalty
        
        // Update intervals (in seconds)
        public float RatingUpdateInterval { get; set; } = 5f;
        public float FormUpdateInterval { get; set; } = 10f;
        public float PressureUpdateInterval { get; set; } = 15f;

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static DynamicRatingsConfiguration CreateDefault()
        {
            return new DynamicRatingsConfiguration();
        }

        /// <summary>
        /// Create configuration for more responsive ratings (arcade-style)
        /// </summary>
        public static DynamicRatingsConfiguration CreateResponsive()
        {
            return new DynamicRatingsConfiguration
            {
                FormUpdateRate = 0.2f,
                ConfidenceUpdateRate = 0.25f,
                FormImpactStrength = 0.4f,
                PressureImpactStrength = 0.3f,
                MomentumImpactStrength = 0.3f,
                RatingUpdateInterval = 2f
            };
        }

        /// <summary>
        /// Create configuration for more stable ratings (simulation-style)
        /// </summary>
        public static DynamicRatingsConfiguration CreateStable()
        {
            return new DynamicRatingsConfiguration
            {
                FormUpdateRate = 0.05f,
                ConfidenceUpdateRate = 0.08f,
                FormDecayRate = 0.002f,
                ConfidenceDecayRate = 0.005f,
                FormImpactStrength = 0.2f,
                PressureImpactStrength = 0.15f,
                MomentumImpactStrength = 0.1f,
                RatingUpdateInterval = 10f
            };
        }
    }

    #endregion
}