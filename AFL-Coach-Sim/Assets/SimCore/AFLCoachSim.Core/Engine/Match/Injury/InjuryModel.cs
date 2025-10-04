using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Engine.Match.Tuning;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Engine.Match.Injury
{
    /// <summary>
    /// Modern injury model that integrates directly with the unified injury management system
    /// </summary>
    public sealed class InjuryModel
    {
        private readonly InjuryManager _injuryManager;
        private readonly Dictionary<int, PlayerRuntimeInjuryState> _playerInjuryStates;
        private readonly ILogger _logger;
        
        public InjuryModel(InjuryManager injuryManager, ILogger logger = null)
        {
            _injuryManager = injuryManager;
            _playerInjuryStates = new Dictionary<int, PlayerRuntimeInjuryState>();
            _logger = logger ?? NullLogger.Instance;
        }
        
        /// <summary>
        /// Initialize player injury states at match start based on existing injuries
        /// </summary>
        public void InitializeMatchInjuryStates(IList<PlayerRuntime> allPlayers)
        {
            _playerInjuryStates.Clear();
            
            foreach (var playerRuntime in allPlayers)
            {
                var playerId = playerRuntime.Player.ID;
                
                // Get current injury status from unified system
                var performanceMultiplier = _injuryManager.GetPlayerPerformanceMultiplier(playerId);
                var canPlay = _injuryManager.CanPlayerPlay(playerId);
                var activeInjuries = _injuryManager.GetActiveInjuries(playerId).ToList();
                var mostSevere = _injuryManager.GetMostSevereInjury(playerId);
                
                // Create runtime injury state
                var injuryState = new PlayerRuntimeInjuryState
                {
                    PlayerId = playerId,
                    PreExistingPerformanceMultiplier = performanceMultiplier,
                    CanParticipate = canPlay,
                    PreExistingInjuries = activeInjuries,
                    MostSeverePreExisting = mostSevere
                };
                
                _playerInjuryStates[playerId] = injuryState;
                
                // Apply pre-existing injury effects to runtime
                ApplyPreExistingInjuriesToRuntime(playerRuntime, injuryState);
            }
            
            _logger.Log($"[InjuryModel] Initialized injury states for {allPlayers.Count} players, {_playerInjuryStates.Values.Count(s => s.PreExistingInjuries.Any())} with pre-existing injuries");
        }
        
        /// <summary>
        /// Process potential new injuries for players during match simulation
        /// </summary>
        public int Step(IList<PlayerRuntime> onField, IList<PlayerRuntime> bench,
                       Phase phase, int dtSeconds, DeterministicRandom rng,
                       int existingInjuries, int maxInjuries, MatchTuning tuning)
        {
            int newInjuries = 0;
            if (existingInjuries >= maxInjuries) return 0;
            
            // Calculate phase-specific risk multiplier
            float phaseRiskMultiplier = GetPhaseRiskMultiplier(phase, tuning);
            
            // Process injuries for active players
            foreach (var playerRuntime in onField)
            {
                if (existingInjuries + newInjuries >= maxInjuries) break;
                if (playerRuntime.InjuredOut) continue;
                
                var playerId = playerRuntime.Player.ID;
                
                // Calculate comprehensive injury risk
                if (ShouldPlayerSustainInjury(playerRuntime, phase, phaseRiskMultiplier, dtSeconds, rng, tuning))
                {
                    var newInjury = CreateMatchInjury(playerRuntime, phase, rng);
                    if (newInjury != null)
                    {
                        ApplyNewInjuryToRuntime(playerRuntime, newInjury);
                        UpdatePlayerInjuryState(playerId, newInjury);
                        newInjuries++;
                        
                        _logger.Log($"[InjuryModel] Player {playerId} sustained {newInjury.Severity} {newInjury.Type} injury during {phase}");
                    }
                }
            }
            
            // Process bench recovery countdowns (for temporary in-match injuries)
            ProcessBenchRecovery(bench, dtSeconds);
            
            return newInjuries;
        }
        
        /// <summary>
        /// Get injury analytics for the match
        /// </summary>
        public MatchInjuryAnalytics GetMatchAnalytics()
        {
            var analytics = new MatchInjuryAnalytics();
            
            foreach (var state in _playerInjuryStates.Values)
            {
                analytics.TotalPlayersTracked++;
                
                if (state.PreExistingInjuries.Any())
                {
                    analytics.PlayersWithPreExistingInjuries++;
                }
                
                if (state.NewMatchInjuries.Any())
                {
                    analytics.PlayersWithNewInjuries++;
                    analytics.NewInjuriesByType.AddRange(state.NewMatchInjuries.Select(i => i.Type));
                    analytics.NewInjuriesBySeverity.AddRange(state.NewMatchInjuries.Select(i => i.Severity));
                }
            }
            
            return analytics;
        }
        
        #region Private Methods
        
        private void ApplyPreExistingInjuriesToRuntime(PlayerRuntime playerRuntime, PlayerRuntimeInjuryState injuryState)
        {
            // Apply performance impact from existing injuries
            playerRuntime.InjuryMult = injuryState.PreExistingPerformanceMultiplier;
            
            // Mark player as unavailable if they shouldn't participate
            if (!injuryState.CanParticipate)
            {
                playerRuntime.InjuredOut = true;
                playerRuntime.ReturnInSeconds = int.MaxValue; // Cannot return this match
            }
            
            // Apply specific injury type effects
            if (injuryState.MostSeverePreExisting != null)
            {
                ApplyInjuryTypeSpecificEffects(playerRuntime, injuryState.MostSeverePreExisting);
            }
        }
        
        private bool ShouldPlayerSustainInjury(PlayerRuntime playerRuntime, Phase phase, 
                                             float phaseRiskMultiplier, int dtSeconds, 
                                             DeterministicRandom rng, MatchTuning tuning)
        {
            var playerId = playerRuntime.Player.ID;
            var player = playerRuntime.Player;
            
            // Get player attributes for risk calculation
            int playerAge = GetPlayerAge(player);
            int durability = player.Durability;
            float currentFatigue = (1.0f - playerRuntime.FatigueMult) * 100f;
            
            // Calculate injury risk using unified system
            var riskProfile = _injuryManager.CalculateInjuryRisk(playerId, playerAge, durability, currentFatigue);
            
            // Convert match exposure to risk probability
            float exposureMinutes = dtSeconds / 60f;
            float intensityMultiplier = phaseRiskMultiplier * GetPlayerSpecificRiskMultiplier(playerRuntime, phase);
            
            // Base per-second risk from tuning
            float baseRiskPerSecond = tuning.InjuryBasePerMinuteRisk / 60f;
            
            // Final injury probability for this time step
            float injuryProbability = baseRiskPerSecond * dtSeconds * intensityMultiplier * riskProfile.OverallRiskMultiplier;
            
            return rng.NextFloat() < injuryProbability;
        }
        
        private AFLCoachSim.Core.Injuries.Domain.Injury CreateMatchInjury(PlayerRuntime playerRuntime, Phase phase, DeterministicRandom rng)
        {
            var playerId = playerRuntime.Player.ID;
            var player = playerRuntime.Player;
            
            // Determine injury severity based on fatigue and durability
            var severity = DetermineInjurySeverity(playerRuntime, rng);
            
            // Determine injury type based on match phase and random factors
            var injuryType = DetermineInjuryType(phase, rng);
            
            // Create injury through unified system with match-specific context
            var injury = _injuryManager.RecordInjury(
                playerId,
                injuryType,
                severity,
                GetMatchSpecificBodyPart(injuryType, phase, rng),
                "Match",
                GetPhaseIntensityMultiplier(phase)
            );
            
            return injury;
        }
        
        private void ApplyNewInjuryToRuntime(PlayerRuntime playerRuntime, AFLCoachSim.Core.Injuries.Domain.Injury injury)
        {
            // Apply immediate performance impact
            var currentMultiplier = _injuryManager.GetPlayerPerformanceMultiplier(playerRuntime.Player.ID);
            playerRuntime.InjuryMult = currentMultiplier;
            
            // Determine if player needs to leave field
            switch (injury.Severity)
            {
                case InjurySeverity.Niggle:
                    // Can continue with reduced performance
                    playerRuntime.ReturnInSeconds = 30; // Brief assessment
                    break;
                    
                case InjurySeverity.Minor:
                    if (injury.Type == InjuryType.Concussion)
                    {
                        // Concussion protocol - immediate removal
                        playerRuntime.InjuredOut = true;
                        playerRuntime.ReturnInSeconds = int.MaxValue;
                    }
                    else
                    {
                        // Needs time off field
                        playerRuntime.ReturnInSeconds = 3 * 60; // 3 minutes
                    }
                    break;
                    
                case InjurySeverity.Moderate:
                case InjurySeverity.Major:
                case InjurySeverity.Severe:
                    // Out for the match
                    playerRuntime.InjuredOut = true;
                    playerRuntime.ReturnInSeconds = int.MaxValue;
                    break;
            }
            
            // Apply injury type-specific effects
            ApplyInjuryTypeSpecificEffects(playerRuntime, injury);
        }
        
        private void ApplyInjuryTypeSpecificEffects(PlayerRuntime playerRuntime, AFLCoachSim.Core.Injuries.Domain.Injury injury)
        {
            // Apply type-specific performance impacts
            switch (injury.Type)
            {
                case InjuryType.Muscle:
                    // Muscle injuries affect movement and agility
                    if (injury.Severity >= InjurySeverity.Minor)
                    {
                        playerRuntime.InjuryMult = System.Math.Min(playerRuntime.InjuryMult, 0.75f);
                    }
                    break;
                    
                case InjuryType.Joint:
                    // Joint injuries affect overall mobility
                    if (injury.Severity >= InjurySeverity.Minor)
                    {
                        playerRuntime.InjuryMult = System.Math.Min(playerRuntime.InjuryMult, 0.70f);
                    }
                    break;
                    
                case InjuryType.Ligament:
                    // Ligament injuries are serious - significant impact
                    playerRuntime.InjuryMult = System.Math.Min(playerRuntime.InjuryMult, 0.60f);
                    if (injury.Severity >= InjurySeverity.Moderate)
                    {
                        playerRuntime.InjuredOut = true;
                    }
                    break;
                    
                case InjuryType.Concussion:
                    // Concussion protocol - immediate removal regardless of severity
                    playerRuntime.InjuredOut = true;
                    playerRuntime.ReturnInSeconds = int.MaxValue;
                    playerRuntime.InjuryMult = 0.3f; // Significant cognitive/coordination impact
                    break;
                    
                case InjuryType.Bone:
                    // Bone injuries are very serious
                    if (injury.Severity >= InjurySeverity.Minor)
                    {
                        playerRuntime.InjuredOut = true;
                        playerRuntime.InjuryMult = System.Math.Min(playerRuntime.InjuryMult, 0.50f);
                    }
                    break;
            }
        }
        
        private void UpdatePlayerInjuryState(int playerId, AFLCoachSim.Core.Injuries.Domain.Injury newInjury)
        {
            if (_playerInjuryStates.TryGetValue(playerId, out var state))
            {
                state.NewMatchInjuries.Add(newInjury);
            }
        }
        
        private void ProcessBenchRecovery(IList<PlayerRuntime> bench, int dtSeconds)
        {
            foreach (var playerRuntime in bench)
            {
                if (playerRuntime.ReturnInSeconds > 0 && playerRuntime.ReturnInSeconds < int.MaxValue)
                {
                    playerRuntime.ReturnInSeconds -= dtSeconds;
                    if (playerRuntime.ReturnInSeconds <= 0)
                    {
                        playerRuntime.ReturnInSeconds = 0;
                        // Player may return to field (but still has injury performance impact)
                    }
                }
            }
        }
        
        private float GetPhaseRiskMultiplier(Phase phase, MatchTuning tuning)
        {
            return phase switch
            {
                Phase.Inside50 => tuning.InjuryInside50Mult,
                Phase.OpenPlay => tuning.InjuryOpenPlayMult,
                Phase.CenterBounce => 1.1f, // Slightly elevated risk at contests
                _ => 1.0f
            };
        }
        
        private float GetPhaseIntensityMultiplier(Phase phase)
        {
            return phase switch
            {
                Phase.Inside50 => 1.3f,    // High contact zone
                Phase.OpenPlay => 1.2f,    // Moderate contact
                Phase.CenterBounce => 1.1f, // Contest situations
                _ => 1.0f
            };
        }
        
        private float GetPlayerSpecificRiskMultiplier(PlayerRuntime playerRuntime, Phase phase)
        {
            float multiplier = 1.0f;
            
            // Fatigue increases risk
            float fatigueImpact = (1.0f - playerRuntime.FatigueMult);
            multiplier += fatigueImpact * 0.8f;
            
            // Previous injuries in this match increase risk slightly
            var playerId = playerRuntime.Player.ID;
            if (_playerInjuryStates.TryGetValue(playerId, out var state) && state.NewMatchInjuries.Any())
            {
                multiplier += 0.2f; // 20% increased risk if already injured this match
            }
            
            return multiplier;
        }
        
        private InjurySeverity DetermineInjurySeverity(PlayerRuntime playerRuntime, DeterministicRandom rng)
        {
            // Base severity weights (aligned with original system but using our enhanced enum)
            var severities = new[] { InjurySeverity.Niggle, InjurySeverity.Minor, InjurySeverity.Moderate, InjurySeverity.Major };
            var baseWeights = new[] { 55f, 30f, 11f, 4f };
            
            // Adjust weights based on fatigue (more fatigue = higher chance of severe injury)
            float fatigueLevel = 1.0f - playerRuntime.FatigueMult;
            
            // Shift probability toward more severe injuries when fatigued
            for (int i = 2; i < baseWeights.Length; i++) // Moderate and Major
            {
                baseWeights[i] *= (1.0f + fatigueLevel * 0.8f);
            }
            
            // Weighted random selection
            return WeightedRandomSelection(severities, baseWeights, rng);
        }
        
        private InjuryType DetermineInjuryType(Phase phase, DeterministicRandom rng)
        {
            // Define injury type probabilities based on match phase
            var injuryTypes = new[] { InjuryType.Muscle, InjuryType.Joint, InjuryType.Bone, InjuryType.Ligament, InjuryType.Concussion, InjuryType.Other };
            
            float[] weights = phase switch
            {
                Phase.Inside50 => new[] { 30f, 25f, 15f, 15f, 10f, 5f }, // Higher contact injuries
                Phase.OpenPlay => new[] { 40f, 30f, 8f, 12f, 5f, 5f },  // More running injuries
                Phase.CenterBounce => new[] { 25f, 20f, 20f, 15f, 15f, 5f }, // Contest injuries
                _ => new[] { 35f, 30f, 10f, 15f, 5f, 5f } // Default distribution
            };
            
            return WeightedRandomSelection(injuryTypes, weights, rng);
        }
        
        private T WeightedRandomSelection<T>(T[] items, float[] weights, DeterministicRandom rng)
        {
            float totalWeight = weights.Sum();
            float randomValue = rng.NextFloat() * totalWeight;
            
            float currentWeight = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return items[i];
                }
            }
            
            return items[items.Length - 1]; // Fallback
        }
        
        private int GetPlayerAge(Domain.Entities.Player player)
        {
            // Use actual player age from the player data
            return player.Age;
        }
        
        private BodyPart GetMatchSpecificBodyPart(InjuryType injuryType, Phase phase, DeterministicRandom rng)
        {
            // Determine body part based on injury type and match phase
            return injuryType switch
            {
                InjuryType.Muscle => GetMuscleInjuryBodyPart(phase, rng),
                InjuryType.Joint => GetJointInjuryBodyPart(phase, rng),
                InjuryType.Bone => GetBoneInjuryBodyPart(phase, rng),
                InjuryType.Ligament => GetLigamentInjuryBodyPart(phase, rng),
                InjuryType.Concussion => BodyPart.Head,
                InjuryType.Skin => GetSkinInjuryBodyPart(phase, rng),
                _ => BodyPart.Other
            };
        }
        
        private BodyPart GetMuscleInjuryBodyPart(Phase phase, DeterministicRandom rng)
        {
            // Muscle injuries are common in legs during running, arms/torso during contests
            var bodyParts = phase switch
            {
                Phase.OpenPlay => new[] { BodyPart.UpperLeg, BodyPart.LowerLeg, BodyPart.Foot }, // Running injuries
                Phase.Inside50 or Phase.CenterBounce => new[] { BodyPart.UpperLeg, BodyPart.LowerLeg, BodyPart.Back, BodyPart.Shoulder }, // Contest injuries
                _ => new[] { BodyPart.UpperLeg, BodyPart.LowerLeg, BodyPart.Back }
            };
            
            var weights = phase switch
            {
                Phase.OpenPlay => new[] { 40f, 35f, 25f }, // Hamstring, calf, foot
                Phase.Inside50 or Phase.CenterBounce => new[] { 25f, 20f, 30f, 25f }, // More varied in contests
                _ => new[] { 35f, 35f, 30f }
            };
            
            return WeightedRandomSelection(bodyParts, weights, rng);
        }
        
        private BodyPart GetJointInjuryBodyPart(Phase phase, DeterministicRandom rng)
        {
            // Joint injuries commonly affect knees, ankles, shoulders
            var bodyParts = new[] { BodyPart.UpperLeg, BodyPart.LowerLeg, BodyPart.Shoulder, BodyPart.Foot }; // Knee, ankle, shoulder, foot
            var weights = phase switch
            {
                Phase.OpenPlay => new[] { 35f, 30f, 20f, 15f }, // More knee/ankle in running
                Phase.Inside50 or Phase.CenterBounce => new[] { 30f, 25f, 35f, 10f }, // More shoulder in contests
                _ => new[] { 35f, 30f, 25f, 10f }
            };
            
            return WeightedRandomSelection(bodyParts, weights, rng);
        }
        
        private BodyPart GetBoneInjuryBodyPart(Phase phase, DeterministicRandom rng)
        {
            // Bone injuries are serious and can occur anywhere but commonly legs/arms
            var bodyParts = new[] { BodyPart.UpperArm, BodyPart.LowerArm, BodyPart.UpperLeg, BodyPart.LowerLeg, BodyPart.Foot, BodyPart.Hand };
            var weights = new[] { 15f, 20f, 25f, 25f, 10f, 5f };
            
            return WeightedRandomSelection(bodyParts, weights, rng);
        }
        
        private BodyPart GetLigamentInjuryBodyPart(Phase phase, DeterministicRandom rng)
        {
            // Ligament injuries commonly affect major joints
            var bodyParts = new[] { BodyPart.UpperLeg, BodyPart.LowerLeg, BodyPart.Shoulder, BodyPart.Foot }; // Knee, ankle, shoulder
            var weights = new[] { 45f, 35f, 15f, 5f }; // ACL/MCL most common
            
            return WeightedRandomSelection(bodyParts, weights, rng);
        }
        
        private BodyPart GetSkinInjuryBodyPart(Phase phase, DeterministicRandom rng)
        {
            // Cuts and abrasions can occur anywhere but commonly exposed areas
            var bodyParts = new[] { BodyPart.Head, BodyPart.UpperArm, BodyPart.LowerArm, BodyPart.UpperLeg, BodyPart.LowerLeg };
            var weights = new[] { 30f, 20f, 20f, 15f, 15f };
            
            return WeightedRandomSelection(bodyParts, weights, rng);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Tracks injury state for a player during a match
    /// </summary>
    public class PlayerRuntimeInjuryState
    {
        public int PlayerId { get; set; }
        public float PreExistingPerformanceMultiplier { get; set; } = 1.0f;
        public bool CanParticipate { get; set; } = true;
        public List<AFLCoachSim.Core.Injuries.Domain.Injury> PreExistingInjuries { get; set; } = new List<AFLCoachSim.Core.Injuries.Domain.Injury>();
        public List<AFLCoachSim.Core.Injuries.Domain.Injury> NewMatchInjuries { get; set; } = new List<AFLCoachSim.Core.Injuries.Domain.Injury>();
        public AFLCoachSim.Core.Injuries.Domain.Injury MostSeverePreExisting { get; set; }
    }
    
    /// <summary>
    /// Analytics data for injuries during a match
    /// </summary>
    public class MatchInjuryAnalytics
    {
        public int TotalPlayersTracked { get; set; }
        public int PlayersWithPreExistingInjuries { get; set; }
        public int PlayersWithNewInjuries { get; set; }
        public List<InjuryType> NewInjuriesByType { get; set; } = new List<InjuryType>();
        public List<InjurySeverity> NewInjuriesBySeverity { get; set; } = new List<InjurySeverity>();
        
        public int TotalNewInjuries => NewInjuriesByType.Count;
        public float InjuryRate => TotalPlayersTracked > 0 ? (float)PlayersWithNewInjuries / TotalPlayersTracked : 0f;
    }
}