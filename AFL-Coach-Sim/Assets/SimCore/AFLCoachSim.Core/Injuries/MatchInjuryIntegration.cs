using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Engine.Match.Injury;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match;
using UnityEngine;

namespace AFLCoachSim.Core.Injuries
{
    /// <summary>
    /// Integration adapter that connects the match engine injury system with unified injury management
    /// </summary>
    public class MatchInjuryIntegration
    {
        private readonly InjuryManager _injuryManager;
        
        public MatchInjuryIntegration(InjuryManager injuryManager)
        {
            _injuryManager = injuryManager ?? throw new ArgumentNullException(nameof(injuryManager));
        }
        
        /// <summary>
        /// Processes match injuries from the match engine and converts them to the unified injury system
        /// </summary>
        public void ProcessMatchInjuries(IList<PlayerRuntime> players, Phase currentPhase)
        {
            foreach (var playerRuntime in players)
            {
                if (ShouldRecordInjury(playerRuntime))
                {
                    var injury = ConvertMatchInjuryToDomain(playerRuntime, currentPhase);
                    if (injury != null)
                    {
                        RecordMatchInjuryInSystem(playerRuntime, injury);
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies current injury status from unified system to match player runtime
        /// </summary>
        public void ApplyInjuryStatusToRuntime(PlayerRuntime playerRuntime)
        {
            var playerId = playerRuntime.Player.ID;
            
            // Get current injury impact from injury manager
            float performanceMultiplier = _injuryManager.GetPlayerPerformanceMultiplier(playerId);
            
            // Apply performance impact to player runtime
            playerRuntime.InjuryMult = performanceMultiplier;
            
            // Check if player should be unavailable due to serious injury
            if (!_injuryManager.CanPlayerPlay(playerId))
            {
                playerRuntime.InjuredOut = true;
            }
            
            // Apply additional restrictions based on injury status
            var mostSevereInjury = _injuryManager.GetMostSevereInjury(playerId);
            if (mostSevereInjury != null)
            {
                ApplySpecificInjuryEffects(playerRuntime, mostSevereInjury);
            }
        }
        
        /// <summary>
        /// Updates injury manager with latest player condition from match
        /// </summary>
        public void UpdatePlayerConditionFromMatch(PlayerRuntime playerRuntime, int minutesPlayed, float fatigueAccumulated)
        {
            var playerId = playerRuntime.Player.ID;
            
            // Calculate if match participation affected injury recovery
            if (_injuryManager.IsPlayerInjured(playerId))
            {
                var activeInjuries = _injuryManager.GetActiveInjuries(playerId);
                
                foreach (var injury in activeInjuries)
                {
                    // Playing with injuries may slow recovery
                    if (minutesPlayed > 0 && injury.Severity >= InjurySeverity.Moderate)
                    {
                        Debug.Log($"[MatchInjuryIntegration] Player {playerId} played {minutesPlayed} minutes with {injury.Severity} injury - may affect recovery");
                    }
                }
            }
        }
        
        /// <summary>
        /// Pre-match injury check that filters out unavailable players
        /// </summary>
        public bool IsPlayerAvailableForMatch(int playerId)
        {
            return _injuryManager.CanPlayerPlay(playerId);
        }
        
        /// <summary>
        /// Gets injury-adjusted performance multiplier for match simulation
        /// </summary>
        public float GetMatchPerformanceMultiplier(int playerId)
        {
            return _injuryManager.GetPlayerPerformanceMultiplier(playerId);
        }
        
        /// <summary>
        /// Gets injury risk modifier for match injury calculations
        /// </summary>
        public float GetInjuryRiskMultiplier(int playerId, int playerAge, int durability, float fatigue)
        {
            var riskProfile = _injuryManager.CalculateInjuryRisk(playerId, playerAge, durability, fatigue);
            return riskProfile.OverallRiskMultiplier;
        }
        
        #region Private Helper Methods
        
        private bool ShouldRecordInjury(PlayerRuntime playerRuntime)
        {
            // Check if this is a new injury that hasn't been recorded yet
            // This prevents double-recording injuries during match simulation
            
            // If player was just injured this step and it's significant enough to track
            return playerRuntime.InjuryMult < 1.0f && 
                   (playerRuntime.InjuryMult <= 0.9f || playerRuntime.InjuredOut);
        }
        
        private Injury ConvertMatchInjuryToDomain(PlayerRuntime playerRuntime, Phase currentPhase)
        {
            var playerId = playerRuntime.Player.ID;
            var matchSeverity = ConvertRuntimeToSeverity(playerRuntime);
            var gameContext = GetGameContextMultiplier(currentPhase);
            
            if (matchSeverity.HasValue)
            {
                return _injuryManager.RecordMatchInjury(playerId, matchSeverity.Value, gameContext);
            }
            
            return null;
        }
        
        private InjurySeverity? ConvertRuntimeToSeverity(PlayerRuntime playerRuntime)
        {
            // Convert match engine injury impact to domain severity
            if (playerRuntime.InjuredOut)
            {
                // Player is out for the match - likely Moderate or higher
                return playerRuntime.InjuryMult <= 0.5f ? InjurySeverity.Major : InjurySeverity.Moderate;
            }
            
            if (playerRuntime.InjuryMult <= 0.7f)
            {
                return InjurySeverity.Minor;
            }
            
            if (playerRuntime.InjuryMult <= 0.9f)
            {
                return InjurySeverity.Niggle;
            }
            
            return null; // No significant injury
        }
        
        private float GetGameContextMultiplier(Phase currentPhase)
        {
            return currentPhase switch
            {
                Phase.Inside50 => 1.3f,    // Higher contact, more injury risk
                Phase.OpenPlay => 1.2f,    // Moderate contact
                Phase.CenterBounce => 1.1f, // Some contact at bounces
                _ => 1.0f                   // Default
            };
        }
        
        private void RecordMatchInjuryInSystem(PlayerRuntime playerRuntime, Injury injury)
        {
            // The injury is already recorded by the injury manager in ConvertMatchInjuryToDomain
            // This method can be used for additional processing if needed
            
            Debug.Log($"[MatchInjuryIntegration] Match injury recorded: Player {injury.PlayerId} - {injury.Description}");
            
            // Update the runtime to reflect the recorded injury
            SyncRuntimeWithInjury(playerRuntime, injury);
        }
        
        private void SyncRuntimeWithInjury(PlayerRuntime playerRuntime, Injury injury)
        {
            // Ensure the runtime matches the injury system state
            var currentMultiplier = _injuryManager.GetPlayerPerformanceMultiplier(playerRuntime.Player.ID);
            playerRuntime.InjuryMult = currentMultiplier;
            
            if (injury.Severity >= InjurySeverity.Moderate)
            {
                playerRuntime.InjuredOut = true;
            }
        }
        
        private void ApplySpecificInjuryEffects(PlayerRuntime playerRuntime, Injury injury)
        {
            // Apply type-specific effects based on injury
            switch (injury.Type)
            {
                case InjuryType.Concussion:
                    // Concussion protocol - player should be removed immediately
                    playerRuntime.InjuredOut = true;
                    playerRuntime.ReturnInSeconds = int.MaxValue; // Cannot return this match
                    break;
                    
                case InjuryType.Muscle:
                    // Muscle injuries affect movement
                    if (injury.Severity >= InjurySeverity.Minor)
                    {
                        playerRuntime.InjuryMult = Math.Min(playerRuntime.InjuryMult, 0.8f);
                    }
                    break;
                    
                case InjuryType.Joint:
                    // Joint injuries affect overall performance
                    if (injury.Severity >= InjurySeverity.Moderate)
                    {
                        playerRuntime.InjuryMult = Math.Min(playerRuntime.InjuryMult, 0.7f);
                    }
                    break;
                    
                case InjuryType.Bone:
                    // Bone injuries are serious - player out
                    if (injury.Severity >= InjurySeverity.Minor)
                    {
                        playerRuntime.InjuredOut = true;
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Match Engine Integration Events
        
        /// <summary>
        /// Called at the start of each match to apply injury status to all players
        /// </summary>
        public void OnMatchStart(IList<PlayerRuntime> allPlayers)
        {
            foreach (var player in allPlayers)
            {
                ApplyInjuryStatusToRuntime(player);
            }
        }
        
        /// <summary>
        /// Called during match simulation to process new injuries
        /// </summary>
        public void OnMatchStep(IList<PlayerRuntime> activePlayers, Phase currentPhase)
        {
            ProcessMatchInjuries(activePlayers, currentPhase);
        }
        
        /// <summary>
        /// Called at the end of match to update player conditions
        /// </summary>
        public void OnMatchEnd(IList<PlayerRuntime> allPlayers, Dictionary<int, int> minutesPlayed)
        {
            foreach (var player in allPlayers)
            {
                var minutes = minutesPlayed.GetValueOrDefault(player.Player.ID, 0);
                var fatigue = (1.0f - player.FatigueMult) * 100f; // Convert to 0-100 scale
                
                UpdatePlayerConditionFromMatch(player, minutes, fatigue);
            }
        }
        
        #endregion
    }
}