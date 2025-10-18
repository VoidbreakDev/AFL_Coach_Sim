using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Weather;
using AFLCoachSim.Core.Engine.Match;

namespace AFLCoachSim.Core.Engine.Match.Ratings
{
    /// <summary>
    /// Advanced dynamic player ratings system that adjusts performance in real-time based on:
    /// - Current form and confidence
    /// - Fatigue levels and physical state
    /// - Match pressure and momentum
    /// - Player vs player matchups
    /// - Weather conditions
    /// - Situational context (home/away, score differential, time remaining)
    /// </summary>
    public class DynamicRatingsSystem
    {
        private readonly Dictionary<Guid, PlayerDynamicRating> _playerRatings;
        private readonly Dictionary<Guid, PlayerFormState> _playerForm;
        private readonly Dictionary<Guid, PlayerPressureState> _playerPressure;
        private readonly Dictionary<Guid, List<PlayerMatchup>> _activeMatchups;
        private readonly DynamicRatingsConfiguration _configuration;
        private readonly Random _random;
        
        // Integration with other systems
        private AdvancedFatigueSystem _fatigueSystem;
        private WeatherImpactSystem _weatherSystem;
        
        // Match context
        private MatchContext _currentMatchContext;
        private float _currentMatchMomentum = 0f; // -1.0 (away advantage) to +1.0 (home advantage)

        public DynamicRatingsSystem(DynamicRatingsConfiguration configuration = null)
        {
            _configuration = configuration ?? DynamicRatingsConfiguration.CreateDefault();
            _playerRatings = new Dictionary<Guid, PlayerDynamicRating>();
            _playerForm = new Dictionary<Guid, PlayerFormState>();
            _playerPressure = new Dictionary<Guid, PlayerPressureState>();
            _activeMatchups = new Dictionary<Guid, List<PlayerMatchup>>();
            _random = new Random();
        }

        #region Initialization and Setup

        /// <summary>
        /// Initialize the ratings system for a match
        /// </summary>
        public void InitializeForMatch(List<Player> homePlayers, List<Player> awayPlayers, 
            MatchContext matchContext, AdvancedFatigueSystem fatigueSystem = null, 
            WeatherImpactSystem weatherSystem = null)
        {
            _currentMatchContext = matchContext;
            _fatigueSystem = fatigueSystem;
            _weatherSystem = weatherSystem;
            _currentMatchMomentum = 0f;

            // Initialize all players
            var allPlayers = homePlayers.Concat(awayPlayers);
            foreach (var player in allPlayers)
            {
                InitializePlayerRatings(player, homePlayers.Contains(player));
            }

            // Set up initial matchups
            EstablishInitialMatchups(homePlayers, awayPlayers);
        }

        private void InitializePlayerRatings(Player player, bool isHomePlayer)
        {
            // Create dynamic rating based on base attributes
            var dynamicRating = new PlayerDynamicRating(player.Id)
            {
                BaseSpeed = player.Speed,
                BaseAccuracy = player.Accuracy,
                BaseEndurance = player.Endurance,
                Position = player.Role,
                Age = player.Age,
                IsHomePlayer = isHomePlayer
            };

            // Initialize current ratings to base values
            dynamicRating.UpdateCurrentRatings(player.Speed, player.Accuracy, player.Endurance);

            _playerRatings[player.Id] = dynamicRating;

            // Initialize form state
            _playerForm[player.Id] = new PlayerFormState(player.Id)
            {
                CurrentForm = CalculateInitialForm(player),
                Confidence = _random.Next(70, 95), // Start with reasonable confidence
                Composure = CalculateComposure(player)
            };

            // Initialize pressure state
            _playerPressure[player.Id] = new PlayerPressureState(player.Id);

            // Initialize matchup list
            _activeMatchups[player.Id] = new List<PlayerMatchup>();
        }

        private float CalculateInitialForm(Player player)
        {
            // Base form calculation considering recent performance (simulated)
            float baseForm = 75f + _random.Next(-15, 16); // 60-90 range
            
            // Age adjustments
            if (player.Age < 22) baseForm -= 5f; // Young players less consistent
            if (player.Age > 32) baseForm -= 3f; // Older players may be declining
            if (player.Age >= 26 && player.Age <= 30) baseForm += 3f; // Peak years

            return Math.Max(50f, Math.Min(100f, baseForm));
        }

        private float CalculateComposure(Player player)
        {
            // Composure based on experience and attributes
            float composure = 70f;
            
            // Age/experience factor
            if (player.Age < 21) composure -= 10f;
            else if (player.Age > 28) composure += 10f;
            
            // Position adjustments (some positions require more composure)
            switch (player.Role)
            {
                case Role.Defender:
                    composure += 5f; // Defenders need composure under pressure
                    break;
                case Role.Midfielder:
                    composure += 3f; // Decision makers
                    break;
                case Role.Ruck:
                    composure += 7f; // Key position players
                    break;
            }

            return Math.Max(40f, Math.Min(100f, composure));
        }

        #endregion

        #region Real-time Rating Updates

        /// <summary>
        /// Update player ratings during match based on all factors
        /// </summary>
        public void UpdatePlayerRatings(Guid playerId, RatingUpdateContext context)
        {
            if (!_playerRatings.TryGetValue(playerId, out var rating)) return;

            // Calculate all modifier factors
            var formModifier = CalculateFormModifier(playerId);
            var fatigueModifier = CalculateFatigueModifier(playerId);
            var pressureModifier = CalculatePressureModifier(playerId, context);
            var matchupModifier = CalculateMatchupModifier(playerId);
            var weatherModifier = CalculateWeatherModifier(playerId);
            var situationalModifier = CalculateSituationalModifier(playerId, context);
            var momentumModifier = CalculateMomentumModifier(playerId);

            // Apply all modifiers to calculate current ratings
            var speedModifier = 1f + (formModifier.SpeedImpact + fatigueModifier.SpeedImpact + 
                                    pressureModifier.SpeedImpact + matchupModifier.SpeedImpact + 
                                    weatherModifier.SpeedImpact + situationalModifier.SpeedImpact + 
                                    momentumModifier.SpeedImpact);

            var accuracyModifier = 1f + (formModifier.AccuracyImpact + fatigueModifier.AccuracyImpact + 
                                        pressureModifier.AccuracyImpact + matchupModifier.AccuracyImpact + 
                                        weatherModifier.AccuracyImpact + situationalModifier.AccuracyImpact + 
                                        momentumModifier.AccuracyImpact);

            var enduranceModifier = 1f + (formModifier.EnduranceImpact + fatigueModifier.EnduranceImpact + 
                                          pressureModifier.EnduranceImpact + matchupModifier.EnduranceImpact + 
                                          weatherModifier.EnduranceImpact + situationalModifier.EnduranceImpact + 
                                          momentumModifier.EnduranceImpact);

            // Clamp modifiers to reasonable ranges
            speedModifier = Math.Max(0.3f, Math.Min(1.5f, speedModifier));
            accuracyModifier = Math.Max(0.3f, Math.Min(1.4f, accuracyModifier));
            enduranceModifier = Math.Max(0.4f, Math.Min(1.3f, enduranceModifier));

            // Calculate final ratings
            var currentSpeed = Math.Max(20f, Math.Min(100f, rating.BaseSpeed * speedModifier));
            var currentAccuracy = Math.Max(20f, Math.Min(100f, rating.BaseAccuracy * accuracyModifier));
            var currentEndurance = Math.Max(20f, Math.Min(100f, rating.BaseEndurance * enduranceModifier));

            // Update the rating
            rating.UpdateCurrentRatings(currentSpeed, currentAccuracy, currentEndurance);

            // Store modifier breakdown for analysis
            rating.LastUpdateModifiers = new RatingModifierBreakdown
            {
                FormModifier = formModifier,
                FatigueModifier = fatigueModifier,
                PressureModifier = pressureModifier,
                MatchupModifier = matchupModifier,
                WeatherModifier = weatherModifier,
                SituationalModifier = situationalModifier,
                MomentumModifier = momentumModifier,
                Timestamp = DateTime.Now
            };

            // Update performance tracking
            UpdatePerformanceTracking(playerId, context);
        }

        /// <summary>
        /// Update player form based on recent performance
        /// </summary>
        public void UpdatePlayerForm(Guid playerId, PerformanceEvent performanceEvent)
        {
            if (!_playerForm.TryGetValue(playerId, out var form)) return;

            // Calculate form impact based on event
            float formImpact = CalculateFormImpact(performanceEvent);
            float confidenceImpact = CalculateConfidenceImpact(performanceEvent);

            // Apply impacts with decay
            form.CurrentForm += formImpact * _configuration.FormUpdateRate;
            form.Confidence += confidenceImpact * _configuration.ConfidenceUpdateRate;

            // Apply natural decay towards mean
            form.CurrentForm += (75f - form.CurrentForm) * _configuration.FormDecayRate;
            form.Confidence += (75f - form.Confidence) * _configuration.ConfidenceDecayRate;

            // Clamp values
            form.CurrentForm = Math.Max(30f, Math.Min(100f, form.CurrentForm));
            form.Confidence = Math.Max(40f, Math.Min(100f, form.Confidence));

            // Track recent performance
            form.RecentPerformances.Add(performanceEvent);
            
            // Keep only recent events (last 10)
            if (form.RecentPerformances.Count > 10)
            {
                form.RecentPerformances.RemoveAt(0);
            }

            form.LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Update match momentum based on recent events
        /// </summary>
        public void UpdateMatchMomentum(MomentumEvent momentumEvent)
        {
            float momentumChange = CalculateMomentumChange(momentumEvent);
            
            _currentMatchMomentum += momentumChange;
            _currentMatchMomentum = Math.Max(-1f, Math.Min(1f, _currentMatchMomentum));

            // Apply momentum decay over time
            _currentMatchMomentum *= _configuration.MomentumDecayRate;
        }

        #endregion

        #region Modifier Calculations

        private RatingModifier CalculateFormModifier(Guid playerId)
        {
            if (!_playerForm.TryGetValue(playerId, out var form))
                return new RatingModifier();

            float formImpact = (form.CurrentForm - 75f) / 100f; // -0.45 to +0.25
            float confidenceImpact = (form.Confidence - 75f) / 200f; // -0.175 to +0.125

            return new RatingModifier
            {
                SpeedImpact = formImpact * 0.3f + confidenceImpact * 0.2f,
                AccuracyImpact = formImpact * 0.4f + confidenceImpact * 0.3f,
                EnduranceImpact = formImpact * 0.2f + confidenceImpact * 0.1f
            };
        }

        private RatingModifier CalculateFatigueModifier(Guid playerId)
        {
            if (_fatigueSystem == null)
                return new RatingModifier();

            var fatigueImpact = _fatigueSystem.CalculatePerformanceImpact(playerId);
            
            return new RatingModifier
            {
                SpeedImpact = -fatigueImpact.SpeedReduction,
                AccuracyImpact = -fatigueImpact.AccuracyReduction,
                EnduranceImpact = -fatigueImpact.EnduranceReduction
            };
        }

        private RatingModifier CalculatePressureModifier(Guid playerId, RatingUpdateContext context)
        {
            if (!_playerPressure.TryGetValue(playerId, out var pressure))
                return new RatingModifier();

            if (!_playerForm.TryGetValue(playerId, out var form))
                return new RatingModifier();

            // Calculate current pressure level
            float pressureLevel = CalculateCurrentPressure(context);
            pressure.CurrentPressure = pressureLevel;

            // Player's composure affects how they handle pressure
            float composureRatio = form.Composure / 100f;
            float pressureImpact = pressureLevel * (1f - composureRatio);

            return new RatingModifier
            {
                SpeedImpact = -pressureImpact * 0.15f,
                AccuracyImpact = -pressureImpact * 0.25f,
                EnduranceImpact = -pressureImpact * 0.1f
            };
        }

        private RatingModifier CalculateMatchupModifier(Guid playerId)
        {
            if (!_activeMatchups.TryGetValue(playerId, out var matchups))
                return new RatingModifier();

            if (!matchups.Any()) return new RatingModifier();

            // Calculate combined matchup advantage/disadvantage
            float totalAdvantage = 0f;
            foreach (var matchup in matchups)
            {
                totalAdvantage += matchup.CalculateAdvantage();
            }

            float averageAdvantage = totalAdvantage / matchups.Count;

            return new RatingModifier
            {
                SpeedImpact = averageAdvantage * 0.1f,
                AccuracyImpact = averageAdvantage * 0.15f,
                EnduranceImpact = averageAdvantage * 0.05f
            };
        }

        private RatingModifier CalculateWeatherModifier(Guid playerId)
        {
            if (_weatherSystem == null)
                return new RatingModifier();

            var weatherEffects = _weatherSystem.CalculatePlayerWeatherEffect(playerId);
            
            return new RatingModifier
            {
                SpeedImpact = weatherEffects.SpeedModifier - 1f,
                AccuracyImpact = weatherEffects.AccuracyModifier - 1f,
                EnduranceImpact = weatherEffects.EnduranceModifier - 1f
            };
        }

        private RatingModifier CalculateSituationalModifier(Guid playerId, RatingUpdateContext context)
        {
            if (!_playerRatings.TryGetValue(playerId, out var rating))
                return new RatingModifier();

            float situationalImpact = 0f;

            // Home/away advantage
            if (rating.IsHomePlayer)
            {
                situationalImpact += _configuration.HomeAdvantage;
            }
            else
            {
                situationalImpact -= _configuration.AwayDisadvantage;
            }

            // Score differential pressure
            if (context != null)
            {
                float scoreDiff = Math.Abs(context.ScoreDifferential);
                if (scoreDiff > 30) // Close game pressure
                {
                    situationalImpact -= 0.05f; // Slight negative impact under pressure
                }
                else if (scoreDiff > 60) // Blowout situations
                {
                    situationalImpact -= 0.1f; // Reduced intensity
                }
            }

            return new RatingModifier
            {
                SpeedImpact = situationalImpact * 0.3f,
                AccuracyImpact = situationalImpact * 0.2f,
                EnduranceImpact = situationalImpact * 0.1f
            };
        }

        private RatingModifier CalculateMomentumModifier(Guid playerId)
        {
            if (!_playerRatings.TryGetValue(playerId, out var rating))
                return new RatingModifier();

            float momentumImpact = rating.IsHomePlayer ? _currentMatchMomentum : -_currentMatchMomentum;
            momentumImpact *= _configuration.MomentumImpactStrength;

            return new RatingModifier
            {
                SpeedImpact = momentumImpact * 0.08f,
                AccuracyImpact = momentumImpact * 0.12f,
                EnduranceImpact = momentumImpact * 0.05f
            };
        }

        #endregion

        #region Helper Methods

        private float CalculateFormImpact(PerformanceEvent performanceEvent)
        {
            return performanceEvent.EventType switch
            {
                PerformanceEventType.Goal => 3f,
                PerformanceEventType.Assist => 2f,
                PerformanceEventType.Mark => 1f,
                PerformanceEventType.Tackle => 1f,
                PerformanceEventType.Turnover => -2f,
                PerformanceEventType.MissedShot => -1.5f,
                PerformanceEventType.FreeKickAgainst => -1f,
                _ => 0f
            };
        }

        private float CalculateConfidenceImpact(PerformanceEvent performanceEvent)
        {
            return performanceEvent.EventType switch
            {
                PerformanceEventType.Goal => 5f,
                PerformanceEventType.Assist => 3f,
                PerformanceEventType.Mark => 2f,
                PerformanceEventType.Tackle => 1.5f,
                PerformanceEventType.Turnover => -3f,
                PerformanceEventType.MissedShot => -4f,
                PerformanceEventType.FreeKickAgainst => -2f,
                _ => 0f
            };
        }

        private float CalculateCurrentPressure(RatingUpdateContext context)
        {
            if (context == null) return 0f;

            float pressure = 0f;

            // Time pressure (4th quarter)
            if (context.Quarter == 4)
            {
                pressure += 0.3f;
                if (context.TimeRemaining < 300) // Last 5 minutes
                {
                    pressure += 0.2f;
                }
            }

            // Score pressure
            float scoreDiff = Math.Abs(context.ScoreDifferential);
            if (scoreDiff < 12) // Within 2 goals
            {
                pressure += 0.4f;
            }
            else if (scoreDiff < 24) // Within 4 goals
            {
                pressure += 0.2f;
            }

            // Crowd pressure (varies by venue)
            pressure += context.CrowdSize / 100000f * 0.1f; // Max 0.1 for 100k crowd

            return Math.Min(1f, pressure);
        }

        private float CalculateMomentumChange(MomentumEvent momentumEvent)
        {
            return momentumEvent.EventType switch
            {
                MomentumEventType.Goal => momentumEvent.IsHomeTeam ? 0.2f : -0.2f,
                MomentumEventType.QuickGoals => momentumEvent.IsHomeTeam ? 0.4f : -0.4f,
                MomentumEventType.DefensiveStop => momentumEvent.IsHomeTeam ? 0.1f : -0.1f,
                MomentumEventType.Turnover => momentumEvent.IsHomeTeam ? -0.1f : 0.1f,
                MomentumEventType.Injury => momentumEvent.IsHomeTeam ? -0.15f : 0.15f,
                _ => 0f
            };
        }

        private void EstablishInitialMatchups(List<Player> homePlayers, List<Player> awayPlayers)
        {
            // Create position-based matchups
            var homeDefenders = homePlayers.Where(p => p.Role == Role.Defender).ToList();
            var awayForwards = awayPlayers.Where(p => p.Role == Role.Forward).ToList();
            var homeForwards = homePlayers.Where(p => p.Role == Role.Forward).ToList();
            var awayDefenders = awayPlayers.Where(p => p.Role == Role.Defender).ToList();
            var homeMiddies = homePlayers.Where(p => p.Role == Role.Midfielder).ToList();
            var awayMiddies = awayPlayers.Where(p => p.Role == Role.Midfielder).ToList();
            var homeRucks = homePlayers.Where(p => p.Role == Role.Ruck).ToList();
            var awayRucks = awayPlayers.Where(p => p.Role == Role.Ruck).ToList();

            // Create defensive matchups
            CreateMatchups(homeDefenders, awayForwards);
            CreateMatchups(awayDefenders, homeForwards);
            
            // Create midfield matchups
            CreateMatchups(homeMiddies, awayMiddies);
            
            // Create ruck matchups
            CreateMatchups(homeRucks, awayRucks);
        }

        private void CreateMatchups(List<Player> group1, List<Player> group2)
        {
            for (int i = 0; i < Math.Min(group1.Count, group2.Count); i++)
            {
                var player1 = group1[i];
                var player2 = group2[i];

                var matchup1 = new PlayerMatchup(player1.Id, player2.Id, player1, player2);
                var matchup2 = new PlayerMatchup(player2.Id, player1.Id, player2, player1);

                _activeMatchups[player1.Id].Add(matchup1);
                _activeMatchups[player2.Id].Add(matchup2);
            }
        }

        private void UpdatePerformanceTracking(Guid playerId, RatingUpdateContext context)
        {
            if (!_playerRatings.TryGetValue(playerId, out var rating)) return;

            // Track performance sample
            rating.PerformanceHistory.Add(new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                CurrentSpeed = rating.CurrentSpeed,
                CurrentAccuracy = rating.CurrentAccuracy,
                CurrentEndurance = rating.CurrentEndurance,
                Context = context?.Clone()
            });

            // Keep last 50 samples
            if (rating.PerformanceHistory.Count > 50)
            {
                rating.PerformanceHistory.RemoveAt(0);
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Get current dynamic rating for a player
        /// </summary>
        public PlayerDynamicRating GetPlayerRating(Guid playerId)
        {
            return _playerRatings.GetValueOrDefault(playerId);
        }

        /// <summary>
        /// Get current form state for a player
        /// </summary>
        public PlayerFormState GetPlayerForm(Guid playerId)
        {
            return _playerForm.GetValueOrDefault(playerId);
        }

        /// <summary>
        /// Get current pressure state for a player
        /// </summary>
        public PlayerPressureState GetPlayerPressure(Guid playerId)
        {
            return _playerPressure.GetValueOrDefault(playerId);
        }

        /// <summary>
        /// Get active matchups for a player
        /// </summary>
        public List<PlayerMatchup> GetPlayerMatchups(Guid playerId)
        {
            return _activeMatchups.GetValueOrDefault(playerId, new List<PlayerMatchup>());
        }

        /// <summary>
        /// Get current match momentum
        /// </summary>
        public float GetCurrentMomentum()
        {
            return _currentMatchMomentum;
        }

        /// <summary>
        /// Add or update a player matchup
        /// </summary>
        public void UpdateMatchup(PlayerMatchup matchup)
        {
            if (!_activeMatchups.TryGetValue(matchup.PlayerId, out var matchups))
            {
                matchups = new List<PlayerMatchup>();
                _activeMatchups[matchup.PlayerId] = matchups;
            }

            var existing = matchups.FirstOrDefault(m => m.OpponentId == matchup.OpponentId);
            if (existing != null)
            {
                matchups.Remove(existing);
            }
            
            matchups.Add(matchup);
        }

        /// <summary>
        /// Get system statistics and analytics
        /// </summary>
        public DynamicRatingsAnalytics GetAnalytics()
        {
            return new DynamicRatingsAnalytics
            {
                TotalPlayers = _playerRatings.Count,
                AverageForm = _playerForm.Values.Average(f => f.CurrentForm),
                AverageConfidence = _playerForm.Values.Average(f => f.Confidence),
                CurrentMomentum = _currentMatchMomentum,
                ActiveMatchups = _activeMatchups.Values.Sum(m => m.Count),
                TopPerformers = GetTopPerformers(5),
                BottomPerformers = GetBottomPerformers(5)
            };
        }

        private List<Guid> GetTopPerformers(int count)
        {
            return _playerRatings.Values
                .OrderByDescending(r => r.GetCurrentPerformanceRating())
                .Take(count)
                .Select(r => r.PlayerId)
                .ToList();
        }

        private List<Guid> GetBottomPerformers(int count)
        {
            return _playerRatings.Values
                .OrderBy(r => r.GetCurrentPerformanceRating())
                .Take(count)
                .Select(r => r.PlayerId)
                .ToList();
        }

        #endregion
    }
}