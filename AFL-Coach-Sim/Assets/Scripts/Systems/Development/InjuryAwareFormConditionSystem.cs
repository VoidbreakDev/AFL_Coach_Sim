using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLManager.Models;
using UnityEngine;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Enhanced form/condition system that integrates with the unified injury management system
    /// </summary>
    [System.Serializable]
    public class InjuryAwareFormCondition : PlayerFormCondition
    {
        [Header("Injury Integration")]
        public bool UseUnifiedInjurySystem = true;
        
        private InjuryManager _injuryManager;
        private int _playerId;
        
        // Override injury-related functionality from base class
        public new List<InjuryAwarePlayerIssue> ActiveIssues;
        
        public InjuryAwareFormCondition() : base()
        {
            ActiveIssues = new List<InjuryAwarePlayerIssue>();
        }
        
        public void Initialize(int playerId, InjuryManager injuryManager)
        {
            _playerId = playerId;
            _injuryManager = injuryManager;
            UseUnifiedInjurySystem = injuryManager != null;
            
            if (UseUnifiedInjurySystem)
            {
                SyncWithInjuryManager();
            }
        }
        
        /// <summary>
        /// Enhanced match update that integrates with injury system
        /// </summary>
        public override void UpdateAfterMatch(MatchPerformanceRating performance, int minutesPlayed, bool injured = false)
        {
            // Update form based on performance
            UpdateForm(performance);
            
            // Update condition and fatigue
            UpdateCondition(minutesPlayed, injured);
            
            // Update confidence
            UpdateConfidence(performance);
            
            // Record the performance
            RecordPerformance(performance, minutesPlayed);
            
            // Update match tracking
            LastMatchDate = DateTime.Now;
            ConsecutiveMatches++;
            
            // Handle injuries through unified system if available
            if (injured)
            {
                HandleMatchInjury(performance, minutesPlayed);
            }
            
            // Sync with injury manager
            if (UseUnifiedInjurySystem)
            {
                SyncWithInjuryManager();
            }
        }
        
        /// <summary>
        /// Enhanced daily recovery that considers unified injury system
        /// </summary>
        public override void ProcessDailyRecovery()
        {
            // Standard recovery processing
            base.ProcessDailyRecovery();
            
            // Sync with unified injury system
            if (UseUnifiedInjurySystem && _injuryManager != null)
            {
                SyncWithInjuryManager();
                UpdateConditionBasedOnInjuries();
            }
        }
        
        /// <summary>
        /// Gets performance modifier including unified injury system impact
        /// </summary>
        public override float GetPerformanceModifier()
        {
            float baseModifier = base.GetPerformanceModifier();
            
            // Apply unified injury system impact if available
            if (UseUnifiedInjurySystem && _injuryManager != null)
            {
                float injuryMultiplier = _injuryManager.GetPlayerPerformanceMultiplier(_playerId);
                return baseModifier * injuryMultiplier;
            }
            
            return baseModifier;
        }
        
        /// <summary>
        /// Enhanced player status that considers unified injury system
        /// </summary>
        public override PlayerStatus GetCurrentStatus()
        {
            // Check unified injury system first if available
            if (UseUnifiedInjurySystem && _injuryManager != null)
            {
                if (!_injuryManager.CanPlayerPlay(_playerId))
                {
                    return PlayerStatus.Injured;
                }
                
                if (!_injuryManager.CanPlayerTrain(_playerId))
                {
                    return PlayerStatus.Injured; // Can't train but might play - still consider injured
                }
                
                // Get most severe injury for status determination
                var mostSevereInjury = _injuryManager.GetMostSevereInjury(_playerId);
                if (mostSevereInjury != null)
                {
                    return PlayerStatus.Injured;
                }
            }
            
            // Fall back to base status determination
            return base.GetCurrentStatus();
        }
        
        /// <summary>
        /// Checks if player can participate in training considering injuries
        /// </summary>
        public bool CanParticipateInTraining()
        {
            if (UseUnifiedInjurySystem && _injuryManager != null)
            {
                return _injuryManager.CanPlayerTrain(_playerId);
            }
            
            // Fall back to base class logic
            return IsAvailableForSelection();
        }
        
        /// <summary>
        /// Checks if player is available for match selection
        /// </summary>
        public bool CanPlayMatches()
        {
            if (UseUnifiedInjurySystem && _injuryManager != null)
            {
                return _injuryManager.CanPlayerPlay(_playerId);
            }
            
            // Fall back to base class logic
            return IsAvailableForSelection();
        }
        
        /// <summary>
        /// Gets comprehensive injury information
        /// </summary>
        public InjuryAwareStatusInfo GetDetailedStatus()
        {
            var status = new InjuryAwareStatusInfo
            {
                BaseStatus = base.GetCurrentStatus(),
                Form = Form,
                Condition = Condition,
                Confidence = Confidence,
                Fatigue = Fatigue,
                CanTrain = CanParticipateInTraining(),
                CanPlay = CanPlayMatches(),
                PerformanceModifier = GetPerformanceModifier()
            };
            
            if (UseUnifiedInjurySystem && _injuryManager != null)
            {
                status.ActiveInjuries = _injuryManager.GetActiveInjuries(_playerId).ToList();
                status.DaysUntilTrainingReady = _injuryManager.GetDaysUntilTrainingReady(_playerId);
                status.DaysUntilMatchReady = _injuryManager.GetDaysUntilMatchReady(_playerId);
                
                var mostSevere = _injuryManager.GetMostSevereInjury(_playerId);
                status.MostSevereInjury = mostSevere?.Description;
                status.InjuryPerformanceImpact = _injuryManager.GetPlayerPerformanceMultiplier(_playerId);
            }
            
            return status;
        }
        
        #region Private Methods
        
        private void HandleMatchInjury(MatchPerformanceRating performance, int minutesPlayed)
        {
            if (UseUnifiedInjurySystem && _injuryManager != null)
            {
                // The unified injury system should handle match injuries
                // This method is mainly for backward compatibility
                Debug.Log($"[InjuryAwareFormCondition] Match injury for player {_playerId} handled by unified injury system");
            }
            else
            {
                // Fall back to original injury generation
                var issue = GenerateInjury(performance, minutesPlayed);
                AddLegacyIssue(issue);
            }
        }
        
        private void SyncWithInjuryManager()
        {
            if (_injuryManager == null) return;
            
            // Clear legacy issues and sync with unified system
            ActiveIssues.Clear();
            
            var activeInjuries = _injuryManager.GetActiveInjuries(_playerId);
            foreach (var injury in activeInjuries)
            {
                var issue = new InjuryAwarePlayerIssue
                {
                    UnifiedInjury = injury,
                    Type = PlayerIssueType.Injury,
                    Severity = ConvertToLegacySeverity(injury.Severity),
                    DaysRemaining = injury.DaysRemaining,
                    PerformanceImpact = injury.GetCurrentPerformanceImpact() - 1.0f, // Convert multiplier to impact
                    Description = injury.Description
                };
                
                ActiveIssues.Add(issue);
            }
        }
        
        private void UpdateConditionBasedOnInjuries()
        {
            if (_injuryManager == null) return;
            
            var activeInjuries = _injuryManager.GetActiveInjuries(_playerId);
            
            foreach (var injury in activeInjuries)
            {
                // Adjust condition based on injury severity and recovery progress
                float conditionImpact = injury.Severity switch
                {
                    InjurySeverity.Niggle => -2f,
                    InjurySeverity.Minor => -5f,
                    InjurySeverity.Moderate => -15f,
                    InjurySeverity.Major => -30f,
                    InjurySeverity.Severe => -50f,
                    _ => 0f
                };
                
                // Apply condition impact gradually
                float currentImpact = conditionImpact * (1.0f - injury.GetCurrentPerformanceImpact() + 0.1f);
                Condition = Mathf.Max(10f, Condition + currentImpact * 0.1f); // Gradual daily impact
            }
        }
        
        private IssueSeverity ConvertToLegacySeverity(InjurySeverity unified)
        {
            return unified switch
            {
                InjurySeverity.Niggle => IssueSeverity.Minor,
                InjurySeverity.Minor => IssueSeverity.Minor,
                InjurySeverity.Moderate => IssueSeverity.Moderate,
                InjurySeverity.Major => IssueSeverity.Major,
                InjurySeverity.Severe => IssueSeverity.Severe,
                _ => IssueSeverity.Minor
            };
        }
        
        private void AddLegacyIssue(PlayerIssue issue)
        {
            var injuryAwareIssue = new InjuryAwarePlayerIssue
            {
                Type = issue.Type,
                Severity = issue.Severity,
                DaysRemaining = issue.DaysRemaining,
                PerformanceImpact = issue.PerformanceImpact,
                Description = issue.Description,
                UnifiedInjury = null // No unified injury for legacy issues
            };
            
            ActiveIssues.Add(injuryAwareIssue);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enhanced player issue that can link to unified injury system
    /// </summary>
    [System.Serializable]
    public class InjuryAwarePlayerIssue : PlayerIssue
    {
        public Injury UnifiedInjury; // Reference to unified injury system
        public bool IsFromUnifiedSystem => UnifiedInjury != null;
        
        public override string ToString()
        {
            if (IsFromUnifiedSystem)
            {
                return $"{UnifiedInjury.Type} ({UnifiedInjury.Severity}) - {UnifiedInjury.DaysRemaining} days remaining";
            }
            
            return base.ToString();
        }
    }
    
    /// <summary>
    /// Comprehensive status information including injury details
    /// </summary>
    public class InjuryAwareStatusInfo
    {
        public PlayerStatus BaseStatus { get; set; }
        public float Form { get; set; }
        public float Condition { get; set; }
        public float Confidence { get; set; }
        public float Fatigue { get; set; }
        public bool CanTrain { get; set; }
        public bool CanPlay { get; set; }
        public float PerformanceModifier { get; set; }
        
        // Injury-specific information
        public List<Injury> ActiveInjuries { get; set; } = new List<Injury>();
        public string MostSevereInjury { get; set; }
        public int DaysUntilTrainingReady { get; set; }
        public int DaysUntilMatchReady { get; set; }
        public float InjuryPerformanceImpact { get; set; } = 1.0f;
        
        public bool HasActiveInjuries => ActiveInjuries.Any();
        public string StatusSummary => $"Form: {Form:F0}, Condition: {Condition:F0}, " +
                                      $"Injuries: {ActiveInjuries.Count}, Can Play: {CanPlay}";
    }
    
    /// <summary>
    /// Manager class for handling multiple player form/condition states with injury integration
    /// </summary>
    public class InjuryAwareFormConditionManager
    {
        private readonly Dictionary<int, InjuryAwareFormCondition> _playerConditions;
        private readonly InjuryManager _injuryManager;
        
        public InjuryAwareFormConditionManager(InjuryManager injuryManager)
        {
            _playerConditions = new Dictionary<int, InjuryAwareFormCondition>();
            _injuryManager = injuryManager;
        }
        
        /// <summary>
        /// Gets or creates form/condition state for a player
        /// </summary>
        public InjuryAwareFormCondition GetPlayerCondition(int playerId)
        {
            if (!_playerConditions.ContainsKey(playerId))
            {
                var condition = new InjuryAwareFormCondition();
                condition.Initialize(playerId, _injuryManager);
                _playerConditions[playerId] = condition;
            }
            
            return _playerConditions[playerId];
        }
        
        /// <summary>
        /// Processes daily recovery for all players
        /// </summary>
        public void ProcessDailyRecovery()
        {
            foreach (var condition in _playerConditions.Values)
            {
                condition.ProcessDailyRecovery();
            }
        }
        
        /// <summary>
        /// Gets team-wide status summary
        /// </summary>
        public TeamConditionSummary GetTeamSummary()
        {
            var summary = new TeamConditionSummary();
            
            foreach (var kvp in _playerConditions)
            {
                var condition = kvp.Value;
                var status = condition.GetDetailedStatus();
                
                summary.TotalPlayers++;
                summary.AverageForm += status.Form;
                summary.AverageCondition += status.Condition;
                
                if (status.HasActiveInjuries) summary.InjuredPlayers++;
                if (!status.CanPlay) summary.UnavailableForMatch++;
                if (!status.CanTrain) summary.UnavailableForTraining++;
            }
            
            if (summary.TotalPlayers > 0)
            {
                summary.AverageForm /= summary.TotalPlayers;
                summary.AverageCondition /= summary.TotalPlayers;
            }
            
            return summary;
        }
    }
    
    /// <summary>
    /// Team-wide condition and injury summary
    /// </summary>
    public class TeamConditionSummary
    {
        public int TotalPlayers { get; set; }
        public float AverageForm { get; set; }
        public float AverageCondition { get; set; }
        public int InjuredPlayers { get; set; }
        public int UnavailableForMatch { get; set; }
        public int UnavailableForTraining { get; set; }
        
        public float InjuryRate => TotalPlayers > 0 ? (float)InjuredPlayers / TotalPlayers : 0f;
        public float AvailabilityRate => TotalPlayers > 0 ? (float)(TotalPlayers - UnavailableForMatch) / TotalPlayers : 0f;
    }
}