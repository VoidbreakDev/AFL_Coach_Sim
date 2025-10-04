using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Ratings;
using AFLCoachSim.Core.Engine.Match.Fatigue;

namespace AFLCoachSim.Core.Engine.Match.Momentum
{
    /// <summary>
    /// Comprehensive momentum and pressure system that tracks match flow, crowd influence, 
    /// and pressure situations affecting player decision-making and performance
    /// </summary>
    public class MomentumAndPressureSystem
    {
        private readonly Dictionary<Guid, PlayerPressureProfile> _playerPressureProfiles;
        private readonly Dictionary<Guid, TeamMomentumState> _teamMomentumStates;
        private readonly List<MomentumEvent> _momentumHistory;
        private readonly List<PressureEvent> _pressureHistory;
        private readonly MomentumConfiguration _configuration;
        private readonly Random _random;

        // Current system state
        private float _globalMomentum = 0f; // -1.0 (away advantage) to +1.0 (home advantage)
        private float _globalPressure = 0f; // 0.0 to 1.0
        private CrowdState _crowdState;
        private MatchPressureContext _currentContext;
        
        // Integration with other systems
        private DynamicRatingsSystem _ratingsSystem;
        private AdvancedFatigueSystem _fatigueSystem;

        public MomentumAndPressureSystem(MomentumConfiguration configuration = null)
        {
            _configuration = configuration ?? MomentumConfiguration.CreateDefault();
            _playerPressureProfiles = new Dictionary<Guid, PlayerPressureProfile>();
            _teamMomentumStates = new Dictionary<Guid, TeamMomentumState>();
            _momentumHistory = new List<MomentumEvent>();
            _pressureHistory = new List<PressureEvent>();
            _crowdState = new CrowdState();
            _random = new Random();
        }

        #region Initialization

        /// <summary>
        /// Initialize the momentum and pressure system for a match
        /// </summary>
        public void InitializeForMatch(List<Player> homePlayers, List<Player> awayPlayers, 
            MatchContext matchContext, DynamicRatingsSystem ratingsSystem = null, 
            AdvancedFatigueSystem fatigueSystem = null)
        {
            _ratingsSystem = ratingsSystem;
            _fatigueSystem = fatigueSystem;
            _currentContext = CreateMatchContext(matchContext);
            
            // Initialize crowd state
            InitializeCrowdState(matchContext);
            
            // Initialize team momentum states
            var homeTeamId = homePlayers.First().TeamId ?? Guid.NewGuid();
            var awayTeamId = awayPlayers.First().TeamId ?? Guid.NewGuid();
            
            _teamMomentumStates[homeTeamId] = new TeamMomentumState(homeTeamId, true, homePlayers.Count);
            _teamMomentumStates[awayTeamId] = new TeamMomentumState(awayTeamId, false, awayPlayers.Count);

            // Initialize player pressure profiles
            foreach (var player in homePlayers.Concat(awayPlayers))
            {
                _playerPressureProfiles[player.Id] = CreatePlayerPressureProfile(player, homeTeamId);
            }

            // Set initial momentum and pressure
            _globalMomentum = CalculateInitialMomentum();
            _globalPressure = CalculateInitialPressure();
        }

        private void InitializeCrowdState(MatchContext matchContext)
        {
            _crowdState = new CrowdState
            {
                TotalSize = matchContext.CrowdSize,
                HomeSupport = CalculateHomeSupportRatio(matchContext),
                BaseEnergy = CalculateBaseEnergy(matchContext),
                CurrentEnergy = CalculateBaseEnergy(matchContext),
                Venue = matchContext.Venue,
                IsNightGame = matchContext.IsNightGame,
                IsFinalSeries = matchContext.IsFinalSeries
            };
        }

        private PlayerPressureProfile CreatePlayerPressureProfile(Player player, Guid homeTeamId)
        {
            bool isHomePlayer = player.TeamId == homeTeamId;
            
            return new PlayerPressureProfile(player.Id)
            {
                IsHomePlayer = isHomePlayer,
                PressureResistance = CalculatePressureResistance(player),
                CrowdSensitivity = CalculateCrowdSensitivity(player),
                ExperienceLevel = CalculateExperienceLevel(player),
                Position = player.Role,
                MomentumSensitivity = CalculateMomentumSensitivity(player)
            };
        }

        #endregion

        #region Momentum Events Processing

        /// <summary>
        /// Process a momentum-changing event during the match
        /// </summary>
        public void ProcessMomentumEvent(MomentumEventData eventData)
        {
            var momentumEvent = new MomentumEvent
            {
                EventType = eventData.EventType,
                IsHomeTeam = eventData.IsHomeTeam,
                PlayerId = eventData.PlayerId,
                Intensity = eventData.Intensity,
                Quarter = eventData.Quarter,
                TimeRemaining = eventData.TimeRemaining,
                ScoreImpact = eventData.ScoreImpact,
                Timestamp = DateTime.Now,
                Description = eventData.Description
            };

            // Calculate momentum change
            float momentumChange = CalculateMomentumChange(momentumEvent);
            float previousMomentum = _globalMomentum;
            
            // Apply momentum change with dampening for extreme swings
            _globalMomentum += momentumChange;
            _globalMomentum = ApplyMomentumDampening(_globalMomentum);
            
            // Update team-specific momentum
            UpdateTeamMomentum(eventData.IsHomeTeam, momentumChange);
            
            // Update crowd state
            UpdateCrowdState(momentumEvent, momentumChange);
            
            // Create cascade effects
            ProcessMomentumCascade(momentumEvent, momentumChange);
            
            // Store event
            momentumEvent.MomentumChange = momentumChange;
            momentumEvent.NewMomentumLevel = _globalMomentum;
            _momentumHistory.Add(momentumEvent);
            
            // Trigger integrations
            if (_ratingsSystem != null)
            {
                TriggerRatingsSystemUpdate(momentumEvent);
            }

            // Clean old events
            CleanOldEvents();
        }

        /// <summary>
        /// Process a pressure-changing event during the match
        /// </summary>
        public void ProcessPressureEvent(PressureEventData eventData)
        {
            var pressureEvent = new PressureEvent
            {
                EventType = eventData.EventType,
                AffectedPlayers = eventData.AffectedPlayers,
                Intensity = eventData.Intensity,
                Quarter = eventData.Quarter,
                TimeRemaining = eventData.TimeRemaining,
                ScoreDifferential = eventData.ScoreDifferential,
                Timestamp = DateTime.Now,
                Description = eventData.Description
            };

            // Calculate pressure change
            float pressureChange = CalculatePressureChange(pressureEvent);
            _globalPressure = Math.Max(0f, Math.Min(1f, _globalPressure + pressureChange));
            
            // Update match context
            UpdateMatchPressureContext(pressureEvent);
            
            // Apply pressure to specific players
            foreach (var playerId in eventData.AffectedPlayers)
            {
                ApplyPlayerPressure(playerId, pressureEvent);
            }
            
            // Update crowd pressure response
            UpdateCrowdPressureResponse(pressureEvent);
            
            // Store event
            pressureEvent.PressureChange = pressureChange;
            pressureEvent.NewPressureLevel = _globalPressure;
            _pressureHistory.Add(pressureEvent);

            // Clean old events
            CleanOldEvents();
        }

        #endregion

        #region Momentum Calculations

        private float CalculateMomentumChange(MomentumEvent eventData)
        {
            float baseMomentum = GetBaseMomentumValue(eventData.EventType);
            float intensityMultiplier = eventData.Intensity;
            float timingMultiplier = CalculateTimingMultiplier(eventData.Quarter, eventData.TimeRemaining);
            float contextMultiplier = CalculateContextMultiplier(eventData);
            float crowdMultiplier = CalculateCrowdMomentumMultiplier(eventData.IsHomeTeam);
            
            // Apply team direction
            float direction = eventData.IsHomeTeam ? 1f : -1f;
            
            float totalChange = baseMomentum * intensityMultiplier * timingMultiplier * 
                               contextMultiplier * crowdMultiplier * direction;
            
            // Apply configuration scaling
            return totalChange * _configuration.MomentumSensitivity;
        }

        private float GetBaseMomentumValue(MomentumEventType eventType)
        {
            return eventType switch
            {
                MomentumEventType.Goal => 0.15f,
                MomentumEventType.QuickGoals => 0.35f,
                MomentumEventType.SpectacularMark => 0.12f,
                MomentumEventType.BrilliantGoal => 0.25f,
                MomentumEventType.DefensiveStop => 0.08f,
                MomentumEventType.Intercept => 0.06f,
                MomentumEventType.RunningGoal => 0.18f,
                MomentumEventType.FromBehind => 0.10f,
                MomentumEventType.Turnover => -0.08f,
                MomentumEventType.MissedEasy => -0.12f,
                MomentumEventType.FreeKickSpray => -0.15f,
                MomentumEventType.Injury => -0.20f,
                MomentumEventType.SinBin => -0.25f,
                MomentumEventType.FightBreakout => -0.30f,
                MomentumEventType.CrowdBoost => 0.08f,
                MomentumEventType.WeatherChange => 0.05f,
                MomentumEventType.UmpireDecision => 0.10f,
                MomentumEventType.CoachingChange => 0.12f,
                _ => 0f
            };
        }

        private float CalculateTimingMultiplier(int quarter, float timeRemaining)
        {
            // Events have more impact later in the match and in crucial moments
            float baseMultiplier = 1.0f;
            
            // Quarter-based scaling
            baseMultiplier *= quarter switch
            {
                1 => 0.8f,  // First quarter less impactful
                2 => 0.9f,  // Second quarter
                3 => 1.1f,  // Third quarter more impactful
                4 => 1.3f,  // Fourth quarter most impactful
                _ => 1.0f
            };
            
            // Final minutes scaling (last 5 minutes of any quarter)
            if (timeRemaining < 300f) // 5 minutes
            {
                baseMultiplier *= 1.2f;
                if (timeRemaining < 120f) // Last 2 minutes
                {
                    baseMultiplier *= 1.3f;
                }
            }
            
            return baseMultiplier;
        }

        private float CalculateContextMultiplier(MomentumEvent eventData)
        {
            float multiplier = 1.0f;
            
            // Score context - close games amplify momentum
            if (_currentContext != null)
            {
                float scoreDiff = Math.Abs(_currentContext.ScoreDifferential);
                if (scoreDiff < 12f) // Within 2 goals
                {
                    multiplier *= 1.4f;
                }
                else if (scoreDiff < 24f) // Within 4 goals
                {
                    multiplier *= 1.2f;
                }
                else if (scoreDiff > 60f) // Blowout
                {
                    multiplier *= 0.6f; // Less impact in blowouts
                }
            }
            
            // Momentum flow context - events against the flow have more impact
            if ((_globalMomentum > 0.3f && !eventData.IsHomeTeam) || 
                (_globalMomentum < -0.3f && eventData.IsHomeTeam))
            {
                multiplier *= 1.3f; // Against the flow bonus
            }
            
            return multiplier;
        }

        private float CalculateCrowdMomentumMultiplier(bool isHomeTeam)
        {
            if (_crowdState == null) return 1.0f;
            
            float crowdEnergy = _crowdState.CurrentEnergy;
            float crowdSize = Math.Min(_crowdState.TotalSize / 50000f, 2.0f); // Cap at 100k influence
            
            if (isHomeTeam)
            {
                // Home team benefits from crowd energy and support
                return 1.0f + (crowdEnergy * _crowdState.HomeSupport * crowdSize * 0.2f);
            }
            else
            {
                // Away team slightly diminished by hostile crowd
                return 1.0f - (crowdEnergy * _crowdState.HomeSupport * crowdSize * 0.1f);
            }
        }

        private float ApplyMomentumDampening(float momentum)
        {
            // Apply natural decay towards neutral
            momentum *= _configuration.MomentumDecayRate;
            
            // Hard limits with soft dampening at extremes
            if (momentum > 0.8f)
            {
                momentum = 0.8f + (momentum - 0.8f) * 0.3f;
            }
            else if (momentum < -0.8f)
            {
                momentum = -0.8f + (momentum + 0.8f) * 0.3f;
            }
            
            return Math.Max(-1f, Math.Min(1f, momentum));
        }

        #endregion

        #region Pressure Calculations

        private float CalculatePressureChange(PressureEvent eventData)
        {
            float basePressure = GetBasePressureValue(eventData.EventType);
            float intensityMultiplier = eventData.Intensity;
            float timingMultiplier = CalculatePressureTimingMultiplier(eventData.Quarter, eventData.TimeRemaining);
            float scoreMultiplier = CalculateScorePressureMultiplier(eventData.ScoreDifferential);
            float crowdMultiplier = CalculateCrowdPressureMultiplier();
            
            return basePressure * intensityMultiplier * timingMultiplier * scoreMultiplier * crowdMultiplier;
        }

        private float GetBasePressureValue(PressureEventType eventType)
        {
            return eventType switch
            {
                PressureEventType.CloseGameStart => 0.2f,
                PressureEventType.FinalQuarterStart => 0.25f,
                PressureEventType.FinalFiveMinutes => 0.35f,
                PressureEventType.FinalTwoMinutes => 0.5f,
                PressureEventType.ShotOnGoal => 0.15f,
                PressureEventType.SetShotPressure => 0.25f,
                PressureEventType.CrucialContest => 0.18f,
                PressureEventType.TurnoverInDefense => 0.20f,
                PressureEventType.InjuryToKeyPlayer => 0.30f,
                PressureEventType.UmpireControversy => 0.22f,
                PressureEventType.CrowdPressure => 0.12f,
                PressureEventType.CoachingDecision => 0.15f,
                PressureEventType.WeatherDeteriorating => 0.10f,
                PressureEventType.MediaScrutiny => 0.08f,
                PressureEventType.LegacyMoment => 0.25f,
                PressureEventType.RecordPressure => 0.20f,
                PressureEventType.PressureRelief => -0.15f,
                PressureEventType.BlowoutReduction => -0.25f,
                _ => 0f
            };
        }

        private float CalculatePressureTimingMultiplier(int quarter, float timeRemaining)
        {
            float multiplier = quarter switch
            {
                1 => 0.7f,
                2 => 0.8f,
                3 => 1.0f,
                4 => 1.5f,
                _ => 1.0f
            };
            
            // Final minutes exponential scaling
            if (quarter == 4)
            {
                if (timeRemaining < 300f) multiplier *= 1.3f;     // Last 5 minutes
                if (timeRemaining < 180f) multiplier *= 1.2f;     // Last 3 minutes
                if (timeRemaining < 60f) multiplier *= 1.5f;      // Last minute
            }
            
            return multiplier;
        }

        private float CalculateScorePressureMultiplier(float scoreDifferential)
        {
            float absDiff = Math.Abs(scoreDifferential);
            
            return absDiff switch
            {
                < 6f => 1.8f,      // Within a goal - maximum pressure
                < 12f => 1.5f,     // Within 2 goals - high pressure
                < 24f => 1.2f,     // Within 4 goals - moderate pressure
                < 36f => 1.0f,     // Within 6 goals - normal pressure
                < 60f => 0.7f,     // 6-10 goals - reduced pressure
                _ => 0.4f          // Blowout - minimal pressure
            };
        }

        private float CalculateCrowdPressureMultiplier()
        {
            if (_crowdState == null) return 1.0f;
            
            float crowdSize = Math.Min(_crowdState.TotalSize / 50000f, 2.0f);
            float crowdEnergy = _crowdState.CurrentEnergy;
            
            return 1.0f + (crowdSize * crowdEnergy * 0.15f);
        }

        #endregion

        #region Team and Player Updates

        private void UpdateTeamMomentum(bool isHomeTeam, float momentumChange)
        {
            var relevantTeam = _teamMomentumStates.Values.FirstOrDefault(t => t.IsHomeTeam == isHomeTeam);
            if (relevantTeam != null)
            {
                relevantTeam.CurrentMomentum += momentumChange;
                relevantTeam.CurrentMomentum = Math.Max(-1f, Math.Min(1f, relevantTeam.CurrentMomentum));
                relevantTeam.LastMomentumChange = DateTime.Now;
                
                // Update momentum streaks
                UpdateMomentumStreak(relevantTeam, momentumChange);
                
                // Update player confidence based on team momentum
                UpdateTeamPlayerConfidence(relevantTeam, momentumChange);
            }
        }

        private void UpdateMomentumStreak(TeamMomentumState team, float momentumChange)
        {
            if (Math.Abs(momentumChange) > 0.1f) // Significant momentum event
            {
                if ((momentumChange > 0 && team.MomentumStreak >= 0) || 
                    (momentumChange < 0 && team.MomentumStreak <= 0))
                {
                    // Continue streak
                    team.MomentumStreak += Math.Sign(momentumChange);
                }
                else
                {
                    // Reset streak
                    team.MomentumStreak = Math.Sign(momentumChange);
                }
                
                // Cap streaks
                team.MomentumStreak = Math.Max(-5, Math.Min(5, team.MomentumStreak));
            }
        }

        private void UpdateTeamPlayerConfidence(TeamMomentumState team, float momentumChange)
        {
            // Find all players on this team and update their momentum sensitivity
            var teamPlayers = _playerPressureProfiles.Values.Where(p => p.IsHomePlayer == team.IsHomeTeam);
            
            foreach (var player in teamPlayers)
            {
                float confidenceChange = momentumChange * player.MomentumSensitivity * 0.5f;
                player.CurrentConfidenceModifier += confidenceChange;
                player.CurrentConfidenceModifier = Math.Max(-0.3f, Math.Min(0.3f, player.CurrentConfidenceModifier));
            }
        }

        private void ApplyPlayerPressure(Guid playerId, PressureEvent pressureEvent)
        {
            if (!_playerPressureProfiles.TryGetValue(playerId, out var profile)) return;
            
            float pressureImpact = pressureEvent.PressureChange * (1f - profile.PressureResistance);
            profile.CurrentPressure += pressureImpact;
            profile.CurrentPressure = Math.Max(0f, Math.Min(1f, profile.CurrentPressure));
            
            // Track pressure events for this player
            profile.PressureEvents.Add(new PlayerPressureEvent
            {
                EventType = pressureEvent.EventType,
                Intensity = pressureImpact,
                Timestamp = DateTime.Now
            });
            
            // Keep only recent events
            if (profile.PressureEvents.Count > 10)
            {
                profile.PressureEvents.RemoveAt(0);
            }
        }

        #endregion

        #region Crowd Dynamics

        private void UpdateCrowdState(MomentumEvent momentumEvent, float momentumChange)
        {
            if (_crowdState == null) return;
            
            // Crowd energy responds to momentum events
            float energyChange = CalculateCrowdEnergyChange(momentumEvent, momentumChange);
            _crowdState.CurrentEnergy += energyChange;
            _crowdState.CurrentEnergy = Math.Max(0.2f, Math.Min(2.0f, _crowdState.CurrentEnergy));
            
            // Update crowd mood
            UpdateCrowdMood(momentumEvent, momentumChange);
            
            // Generate crowd events if energy is high enough
            if (_crowdState.CurrentEnergy > 1.5f && _random.NextDouble() < 0.1f)
            {
                GenerateCrowdEvent(momentumEvent.IsHomeTeam);
            }
        }

        private float CalculateCrowdEnergyChange(MomentumEvent momentumEvent, float momentumChange)
        {
            float baseChange = Math.Abs(momentumChange) * 0.3f;
            
            // Home crowd more responsive to home team events
            if (momentumEvent.IsHomeTeam)
            {
                baseChange *= _crowdState.HomeSupport;
            }
            else
            {
                baseChange *= (1f - _crowdState.HomeSupport) * 0.7f; // Away supporters less influential
            }
            
            // Event type modifiers
            baseChange *= momentumEvent.EventType switch
            {
                MomentumEventType.BrilliantGoal => 1.5f,
                MomentumEventType.SpectacularMark => 1.3f,
                MomentumEventType.QuickGoals => 1.8f,
                MomentumEventType.FightBreakout => 1.2f,
                MomentumEventType.UmpireDecision => 1.4f,
                _ => 1.0f
            };
            
            return momentumChange > 0 ? baseChange : -baseChange * 0.5f; // Negative events less impactful on energy
        }

        private void UpdateCrowdMood(MomentumEvent momentumEvent, float momentumChange)
        {
            // Determine new mood based on momentum and event
            var newMood = CalculateCrowdMood(momentumChange, momentumEvent);
            
            if (newMood != _crowdState.CurrentMood)
            {
                _crowdState.PreviousMood = _crowdState.CurrentMood;
                _crowdState.CurrentMood = newMood;
                _crowdState.MoodChangedAt = DateTime.Now;
            }
        }

        private CrowdMood CalculateCrowdMood(float momentumChange, MomentumEvent momentumEvent)
        {
            // Base mood on current momentum and energy
            float effectiveMomentum = _globalMomentum;
            if (!momentumEvent.IsHomeTeam) effectiveMomentum *= -1f; // Invert for away team perspective
            
            return effectiveMomentum switch
            {
                > 0.6f => CrowdMood.Ecstatic,
                > 0.3f => CrowdMood.Excited,
                > 0.1f => CrowdMood.Positive,
                > -0.1f => CrowdMood.Neutral,
                > -0.3f => CrowdMood.Restless,
                > -0.6f => CrowdMood.Frustrated,
                _ => CrowdMood.Hostile
            };
        }

        private void GenerateCrowdEvent(bool favoringHomeTeam)
        {
            // Generate spontaneous crowd events during high energy moments
            var crowdEventType = _random.Next(0, 4) switch
            {
                0 => "Mexican Wave",
                1 => "Coordinated Chant",
                2 => "Standing Ovation",
                _ => "Vocal Support"
            };
            
            // This could trigger additional momentum events
            var crowdMomentumEvent = new MomentumEventData
            {
                EventType = MomentumEventType.CrowdBoost,
                IsHomeTeam = favoringHomeTeam,
                Intensity = 0.8f + (float)_random.NextDouble() * 0.4f,
                Description = $"Crowd Event: {crowdEventType}"
            };
            
            // Process the crowd-generated momentum event
            ProcessMomentumEvent(crowdMomentumEvent);
        }

        private void UpdateCrowdPressureResponse(PressureEvent pressureEvent)
        {
            if (_crowdState == null) return;
            
            // Crowd responds to pressure by either adding to it or trying to relieve it
            float crowdPressureResponse = pressureEvent.PressureChange * _crowdState.CurrentEnergy * 0.3f;
            
            // Home crowd tries to help relieve pressure for home team
            if (_globalMomentum > 0) // Home team has momentum
            {
                crowdPressureResponse *= -0.5f; // Crowd helps reduce pressure
            }
        }

        #endregion

        #region Decision Making Impact

        /// <summary>
        /// Calculate decision-making impact for a player based on current pressure
        /// </summary>
        public DecisionMakingImpact CalculateDecisionImpact(Guid playerId)
        {
            if (!_playerPressureProfiles.TryGetValue(playerId, out var profile))
                return new DecisionMakingImpact();
            
            var impact = new DecisionMakingImpact
            {
                PlayerId = playerId,
                BaseDecisionSpeed = 1.0f,
                BaseDecisionAccuracy = 1.0f,
                BaseRiskTolerance = profile.BaseRiskTolerance
            };
            
            // Pressure effects
            float pressureLevel = profile.CurrentPressure;
            float pressureResistance = profile.PressureResistance;
            
            // Decision speed - high pressure can either slow down or speed up decisions
            if (pressureLevel > 0.7f)
            {
                // High pressure - decisions become rushed or delayed based on experience
                if (profile.ExperienceLevel > 0.7f)
                {
                    impact.BaseDecisionSpeed *= 1.1f; // Experienced players work faster under pressure
                }
                else
                {
                    impact.BaseDecisionSpeed *= 0.8f; // Inexperienced players slow down
                }
            }
            
            // Decision accuracy - always negatively affected by pressure
            float accuracyReduction = pressureLevel * (1f - pressureResistance) * 0.3f;
            impact.BaseDecisionAccuracy *= (1f - accuracyReduction);
            
            // Risk tolerance - pressure makes players more conservative or reckless
            float riskModifier = (pressureLevel - 0.5f) * (1f - pressureResistance);
            if (profile.ExperienceLevel > 0.6f)
            {
                // Experienced players become more conservative under pressure
                impact.BaseRiskTolerance *= (1f - Math.Abs(riskModifier) * 0.2f);
            }
            else
            {
                // Inexperienced players might become reckless
                impact.BaseRiskTolerance *= (1f + riskModifier * 0.3f);
            }
            
            // Momentum effects
            float relevantMomentum = profile.IsHomePlayer ? _globalMomentum : -_globalMomentum;
            float momentumImpact = relevantMomentum * profile.MomentumSensitivity;
            
            impact.BaseDecisionAccuracy *= (1f + momentumImpact * 0.15f);
            impact.BaseRiskTolerance *= (1f + momentumImpact * 0.1f);
            
            // Crowd effects
            if (_crowdState != null)
            {
                float crowdImpact = CalculateCrowdDecisionImpact(profile);
                impact.BaseDecisionAccuracy *= (1f + crowdImpact);
            }
            
            // Clamp values to reasonable ranges
            impact.BaseDecisionSpeed = Math.Max(0.5f, Math.Min(1.8f, impact.BaseDecisionSpeed));
            impact.BaseDecisionAccuracy = Math.Max(0.6f, Math.Min(1.3f, impact.BaseDecisionAccuracy));
            impact.BaseRiskTolerance = Math.Max(0.3f, Math.Min(1.8f, impact.BaseRiskTolerance));
            
            return impact;
        }

        private float CalculateCrowdDecisionImpact(PlayerPressureProfile profile)
        {
            float crowdEffect = _crowdState.CurrentEnergy * profile.CrowdSensitivity;
            
            if (profile.IsHomePlayer)
            {
                // Home players get positive boost from supportive crowd
                return crowdEffect * _crowdState.HomeSupport * 0.1f;
            }
            else
            {
                // Away players get negative impact from hostile crowd
                return -crowdEffect * _crowdState.HomeSupport * 0.08f;
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Get current global momentum (-1 to +1)
        /// </summary>
        public float GetCurrentMomentum() => _globalMomentum;

        /// <summary>
        /// Get current global pressure (0 to 1)
        /// </summary>
        public float GetCurrentPressure() => _globalPressure;

        /// <summary>
        /// Get team momentum state
        /// </summary>
        public TeamMomentumState GetTeamMomentum(Guid teamId)
        {
            return _teamMomentumStates.GetValueOrDefault(teamId);
        }

        /// <summary>
        /// Get player pressure profile
        /// </summary>
        public PlayerPressureProfile GetPlayerPressure(Guid playerId)
        {
            return _playerPressureProfiles.GetValueOrDefault(playerId);
        }

        /// <summary>
        /// Get current crowd state
        /// </summary>
        public CrowdState GetCrowdState() => _crowdState;

        /// <summary>
        /// Get momentum history
        /// </summary>
        public List<MomentumEvent> GetMomentumHistory(int? lastN = null)
        {
            return lastN.HasValue ? _momentumHistory.TakeLast(lastN.Value).ToList() : _momentumHistory.ToList();
        }

        /// <summary>
        /// Get pressure history
        /// </summary>
        public List<PressureEvent> GetPressureHistory(int? lastN = null)
        {
            return lastN.HasValue ? _pressureHistory.TakeLast(lastN.Value).ToList() : _pressureHistory.ToList();
        }

        /// <summary>
        /// Get system analytics
        /// </summary>
        public MomentumPressureAnalytics GetAnalytics()
        {
            return new MomentumPressureAnalytics
            {
                CurrentMomentum = _globalMomentum,
                CurrentPressure = _globalPressure,
                MomentumTrend = CalculateMomentumTrend(),
                PressureTrend = CalculatePressureTrend(),
                CrowdState = _crowdState?.Clone(),
                MostPressuredPlayers = GetMostPressuredPlayers(5),
                HighestMomentumEvents = GetHighestMomentumEvents(3),
                TeamMomentumComparison = CalculateTeamMomentumComparison()
            };
        }

        #endregion

        #region Helper Methods

        private float CalculateInitialMomentum()
        {
            // Start neutral with slight home advantage
            return _configuration.InitialHomeAdvantage;
        }

        private float CalculateInitialPressure()
        {
            // Start with base pressure level
            return _configuration.BasePressureLevel;
        }

        private float CalculateHomeSupportRatio(MatchContext matchContext)
        {
            // TODO: Could be enhanced with team popularity, rivalry, etc.
            return 0.75f; // 75% home support typical
        }

        private float CalculateBaseEnergy(MatchContext matchContext)
        {
            float energy = 1.0f;
            
            // Night games are more energetic
            if (matchContext.IsNightGame) energy *= 1.2f;
            
            // Finals are more energetic
            if (matchContext.IsFinalSeries) energy *= 1.4f;
            
            // Crowd size affects base energy
            energy *= Math.Min(matchContext.CrowdSize / 50000f, 1.5f);
            
            return energy;
        }

        private float CalculatePressureResistance(Player player)
        {
            // Based on age, experience, and attributes
            float resistance = 0.5f;
            
            // Age factor
            if (player.Age > 28) resistance += 0.2f; // Veterans handle pressure better
            if (player.Age < 22) resistance -= 0.1f; // Young players struggle more
            
            // TODO: Could factor in career games, big match experience, etc.
            
            return Math.Max(0.1f, Math.Min(0.9f, resistance));
        }

        private float CalculateCrowdSensitivity(Player player)
        {
            // Some players thrive on crowd energy, others are negatively affected
            float sensitivity = 0.4f + (float)_random.NextDouble() * 0.4f; // 0.4 to 0.8
            
            // Position adjustments
            if (player.Role == Role.Forward) sensitivity += 0.1f; // Forwards feel crowd more
            
            return sensitivity;
        }

        private float CalculateExperienceLevel(Player player)
        {
            // Simplified experience based on age
            return player.Age switch
            {
                < 20 => 0.2f,
                < 23 => 0.4f,
                < 26 => 0.6f,
                < 30 => 0.8f,
                _ => 1.0f
            };
        }

        private float CalculateMomentumSensitivity(Player player)
        {
            // How much momentum affects this player
            return 0.3f + (float)_random.NextDouble() * 0.4f; // 0.3 to 0.7
        }

        private MatchPressureContext CreateMatchContext(MatchContext matchContext)
        {
            return new MatchPressureContext
            {
                IsNightGame = matchContext.IsNightGame,
                IsFinalSeries = matchContext.IsFinalSeries,
                Venue = matchContext.Venue,
                CrowdSize = matchContext.CrowdSize
            };
        }

        private void ProcessMomentumCascade(MomentumEvent momentumEvent, float momentumChange)
        {
            // Large momentum events can trigger additional smaller events
            if (Math.Abs(momentumChange) > 0.3f)
            {
                // Could trigger coach reactions, player confidence changes, etc.
                // This is where additional systems integration would occur
            }
        }

        private void TriggerRatingsSystemUpdate(MomentumEvent momentumEvent)
        {
            // Convert to ratings system momentum event and trigger update
            var ratingsEvent = new Ratings.MomentumEvent(
                ConvertToRatingsMomentumType(momentumEvent.EventType), 
                momentumEvent.IsHomeTeam)
            {
                Intensity = momentumEvent.Intensity
            };
            
            _ratingsSystem?.UpdateMatchMomentum(ratingsEvent);
        }

        private Ratings.MomentumEventType ConvertToRatingsMomentumType(MomentumEventType eventType)
        {
            // Convert our momentum event type to ratings system event type
            return eventType switch
            {
                MomentumEventType.Goal => Ratings.MomentumEventType.Goal,
                MomentumEventType.QuickGoals => Ratings.MomentumEventType.QuickGoals,
                MomentumEventType.DefensiveStop => Ratings.MomentumEventType.DefensiveStop,
                MomentumEventType.Turnover => Ratings.MomentumEventType.Turnover,
                MomentumEventType.Injury => Ratings.MomentumEventType.Injury,
                _ => Ratings.MomentumEventType.Goal
            };
        }

        private void UpdateMatchPressureContext(PressureEvent pressureEvent)
        {
            // Update the match context with new pressure information
            if (_currentContext != null)
            {
                _currentContext.CurrentPressure = _globalPressure;
                _currentContext.LastPressureEvent = pressureEvent.EventType;
            }
        }

        private void CleanOldEvents()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-30); // Keep 30 minutes of history
            
            _momentumHistory.RemoveAll(e => e.Timestamp < cutoffTime);
            _pressureHistory.RemoveAll(e => e.Timestamp < cutoffTime);
        }

        private MomentumTrend CalculateMomentumTrend()
        {
            if (_momentumHistory.Count < 3) return MomentumTrend.Stable;
            
            var recentEvents = _momentumHistory.TakeLast(5).ToList();
            float totalChange = recentEvents.Sum(e => e.MomentumChange);
            
            return totalChange switch
            {
                > 0.3f => MomentumTrend.StronglyPositive,
                > 0.1f => MomentumTrend.Positive,
                < -0.3f => MomentumTrend.StronglyNegative,
                < -0.1f => MomentumTrend.Negative,
                _ => MomentumTrend.Stable
            };
        }

        private PressureTrend CalculatePressureTrend()
        {
            if (_pressureHistory.Count < 3) return PressureTrend.Stable;
            
            var recentEvents = _pressureHistory.TakeLast(5).ToList();
            float totalChange = recentEvents.Sum(e => e.PressureChange);
            
            return totalChange switch
            {
                > 0.3f => PressureTrend.Increasing,
                < -0.3f => PressureTrend.Decreasing,
                _ => PressureTrend.Stable
            };
        }

        private List<Guid> GetMostPressuredPlayers(int count)
        {
            return _playerPressureProfiles.Values
                .OrderByDescending(p => p.CurrentPressure)
                .Take(count)
                .Select(p => p.PlayerId)
                .ToList();
        }

        private List<MomentumEvent> GetHighestMomentumEvents(int count)
        {
            return _momentumHistory
                .OrderByDescending(e => Math.Abs(e.MomentumChange))
                .Take(count)
                .ToList();
        }

        private float CalculateTeamMomentumComparison()
        {
            var homeTeam = _teamMomentumStates.Values.FirstOrDefault(t => t.IsHomeTeam);
            var awayTeam = _teamMomentumStates.Values.FirstOrDefault(t => !t.IsHomeTeam);
            
            if (homeTeam == null || awayTeam == null) return 0f;
            
            return homeTeam.CurrentMomentum - awayTeam.CurrentMomentum;
        }

        #endregion
    }
}