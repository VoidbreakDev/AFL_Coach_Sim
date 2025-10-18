using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Persistence;
using AFLCoachSim.Core.Engine.Match.Injury;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Injuries
{
    /// <summary>
    /// Central service for managing all injury-related operations
    /// </summary>
    public class InjuryManager
    {
        private readonly IInjuryRepository _repository;
        private readonly Dictionary<int, PlayerInjuryHistory> _playerHistories;
        
        // Events for injury notifications
        public event System.Action<Injury> OnInjuryOccurred;
        public event System.Action<Injury> OnInjuryRecovered;
        public event System.Action<int, float> OnPlayerPerformanceImpactChanged;
        
        public InjuryManager(IInjuryRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _playerHistories = new Dictionary<int, PlayerInjuryHistory>();
            
            LoadAllPlayerHistories();
        }
        
        #region Injury Creation and Management
        
        /// <summary>
        /// Records a new injury from training
        /// </summary>
        public Injury RecordTrainingInjury(int playerId, float injuryRisk, int playerAge, int durability)
        {
            var injuryType = DetermineTrainingInjuryType();
            var severity = DetermineInjurySeverity(injuryRisk, playerAge, durability);
            
            return RecordInjury(playerId, injuryType, severity, InjurySource.Training);
        }
        
        /// <summary>
        /// Records a new injury from match play, converting from match engine severity
        /// </summary>
        public Injury RecordMatchInjury(int playerId, InjurySeverity matchSeverity, float gameContext = 1.0f)
        {
            // Convert match injury severity to domain severity (they're aligned)
            var severity = matchSeverity;
            var injuryType = DetermineMatchInjuryType(gameContext);
            
            return RecordInjury(playerId, injuryType, severity, InjurySource.Match);
        }
        
        /// <summary>
        /// Records a generic injury
        /// </summary>
        public Injury RecordInjury(int playerId, InjuryType type, InjurySeverity severity, InjurySource source, string description = null)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            var injury = history.RecordInjury(type, severity, source, description);
            
            // Save to repository
            _repository.SavePlayerInjuryHistory(history);
            
            // Notify subscribers
            OnInjuryOccurred?.Invoke(injury);
            OnPlayerPerformanceImpactChanged?.Invoke(playerId, history.CurrentPerformanceMultiplier);
            
            Console.WriteLine($"[InjuryManager] Player {playerId} sustained {severity} {type} injury from {source}");
            
            return injury;
        }
        
        /// <summary>
        /// Forces recovery of an injury (e.g., medical clearance)
        /// </summary>
        public bool ForceRecovery(int playerId, InjuryId injuryId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            bool recovered = history.ForceRecovery(injuryId);
            
            if (recovered)
            {
                _repository.SavePlayerInjuryHistory(history);
                var injury = history.AllInjuries.FirstOrDefault(i => i.Id == injuryId);
                if (injury != null)
                {
                    OnInjuryRecovered?.Invoke(injury);
                    OnPlayerPerformanceImpactChanged?.Invoke(playerId, history.CurrentPerformanceMultiplier);
                }
            }
            
            return recovered;
        }
        
        #endregion
        
        #region Daily Processing and Recovery
        
        /// <summary>
        /// Processes daily recovery for all players
        /// </summary>
        public void ProcessDailyRecovery()
        {
            var recoveredPlayers = new List<int>();
            
            foreach (var kvp in _playerHistories)
            {
                var playerId = kvp.Key;
                var history = kvp.Value;
                var previousMultiplier = history.CurrentPerformanceMultiplier;
                
                history.UpdateRecoveryProgress();
                
                // Check if performance impact changed
                var newMultiplier = history.CurrentPerformanceMultiplier;
                if (Math.Abs(previousMultiplier - newMultiplier) > 0.01f)
                {
                    OnPlayerPerformanceImpactChanged?.Invoke(playerId, newMultiplier);
                }
                
                // Check for newly recovered injuries
                var newlyRecovered = history.AllInjuries.Where(i => 
                    i.Status == InjuryStatus.Recovered && 
                    i.ReturnToMatchDate.HasValue && 
                    i.ReturnToMatchDate.Value.Date == DateTime.Now.Date
                );
                
                foreach (var injury in newlyRecovered)
                {
                    OnInjuryRecovered?.Invoke(injury);
                    recoveredPlayers.Add(playerId);
                }
            }
            
            // Save updated histories
            foreach (var playerId in recoveredPlayers.Distinct())
            {
                _repository.SavePlayerInjuryHistory(_playerHistories[playerId]);
            }
            
            if (recoveredPlayers.Any())
            {
                Console.WriteLine($"[InjuryManager] {recoveredPlayers.Count} players had injury status changes during daily processing");
            }
        }
        
        #endregion
        
        #region Player Status Queries
        
        /// <summary>
        /// Checks if a player is currently injured
        /// </summary>
        public bool IsPlayerInjured(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.IsCurrentlyInjured;
        }
        
        /// <summary>
        /// Checks if a player can participate in training
        /// </summary>
        public bool CanPlayerTrain(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.CanTrainToday;
        }
        
        /// <summary>
        /// Checks if a player can play in matches
        /// </summary>
        public bool CanPlayerPlay(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.CanPlayMatches;
        }
        
        /// <summary>
        /// Gets the current performance multiplier for a player
        /// </summary>
        public float GetPlayerPerformanceMultiplier(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.CurrentPerformanceMultiplier;
        }
        
        /// <summary>
        /// Gets the most severe active injury for a player
        /// </summary>
        public Injury GetMostSevereInjury(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.GetMostSevereActiveInjury();
        }
        
        /// <summary>
        /// Gets days until a player can return to training
        /// </summary>
        public int GetDaysUntilTrainingReady(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.GetDaysUntilTrainingReady();
        }
        
        /// <summary>
        /// Gets days until a player can return to matches
        /// </summary>
        public int GetDaysUntilMatchReady(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.GetDaysUntilMatchReady();
        }
        
        /// <summary>
        /// Gets all active injuries for a player
        /// </summary>
        public IEnumerable<Injury> GetActiveInjuries(int playerId)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.ActiveInjuries;
        }
        
        /// <summary>
        /// Gets injury statistics for a player
        /// </summary>
        public InjuryStatistics GetPlayerInjuryStatistics(int playerId, TimeSpan? period = null)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.GetStatistics(period);
        }
        
        #endregion
        
        #region Risk Assessment
        
        /// <summary>
        /// Calculates injury risk profile for a player
        /// </summary>
        public InjuryRiskProfile CalculateInjuryRisk(int playerId, int playerAge, int durability, float currentFatigue)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.CalculateCurrentRisk(playerAge, durability, currentFatigue);
        }
        
        /// <summary>
        /// Calculates specific injury risk for training or match participation
        /// </summary>
        public float CalculateActivityRisk(int playerId, int playerAge, int durability, float fatigue, 
                                         float exposureMinutes, float intensityMultiplier = 1.0f)
        {
            var riskProfile = CalculateInjuryRisk(playerId, playerAge, durability, fatigue);
            return riskProfile.CalculateInjuryProbability(exposureMinutes, intensityMultiplier);
        }
        
        /// <summary>
        /// Gets the risk multiplier for a specific injury type based on player history
        /// </summary>
        public float GetRiskMultiplierFor(int playerId, InjuryType injuryType)
        {
            var history = GetOrCreatePlayerHistory(playerId);
            return history.GetRiskMultiplierFor(injuryType);
        }
        
        /// <summary>
        /// Gets a human-readable description of an injury
        /// </summary>
        public string GetInjuryDescription(InjuryId injuryId)
        {
            // Find the injury across all player histories
            foreach (var history in _playerHistories.Values)
            {
                var injury = history.AllInjuries.FirstOrDefault(i => i.Id == injuryId);
                if (injury != null)
                {
                    return $"{injury.Severity} {injury.Type} injury from {injury.Source}";
                }
            }
            return "Unknown injury";
        }
        
        #endregion
        
        #region Team-wide Operations
        
        /// <summary>
        /// Gets all currently injured players
        /// </summary>
        public IEnumerable<int> GetInjuredPlayerIds()
        {
            return _playerHistories.Where(kvp => kvp.Value.IsCurrentlyInjured).Select(kvp => kvp.Key);
        }
        
        /// <summary>
        /// Gets all active injuries across the entire team/squad
        /// </summary>
        public IEnumerable<Injury> GetAllActiveInjuries()
        {
            var allActive = new List<Injury>();
            foreach (var history in _playerHistories.Values)
            {
                allActive.AddRange(history.ActiveInjuries);
            }
            return allActive;
        }
        
        /// <summary>
        /// Gets team injury summary statistics
        /// </summary>
        public TeamInjurySummary GetTeamInjurySummary(TimeSpan? period = null)
        {
            var summary = new TeamInjurySummary();
            
            foreach (var history in _playerHistories.Values)
            {
                var playerStats = history.GetStatistics(period);
                summary.TotalInjuries += playerStats.TotalInjuries;
                summary.TotalDaysLost += playerStats.DaysLost;
                summary.CurrentlyInjured += history.IsCurrentlyInjured ? 1 : 0;
                
                foreach (var sourceCount in playerStats.InjuriesBySource)
                {
                    summary.InjuriesBySource.TryGetValue(sourceCount.Key, out var current);
                    summary.InjuriesBySource[sourceCount.Key] = current + sourceCount.Value;
                }
            }
            
            summary.PlayersTracked = _playerHistories.Count;
            return summary;
        }
        
        #endregion
        
        #region Persistence and Data Management
        
        /// <summary>
        /// Saves all player injury histories
        /// </summary>
        public void SaveAllData()
        {
            foreach (var history in _playerHistories.Values)
            {
                _repository.SavePlayerInjuryHistory(history);
            }
        }
        
        /// <summary>
        /// Loads injury history for a specific player
        /// </summary>
        public void LoadPlayerHistory(int playerId)
        {
            var history = _repository.LoadPlayerInjuryHistory(playerId);
            _playerHistories[playerId] = history;
        }
        
        /// <summary>
        /// Clears all injury data for a player
        /// </summary>
        public void ClearPlayerData(int playerId)
        {
            _playerHistories.Remove(playerId);
            _repository.ClearPlayerInjuryData(playerId);
        }
        
        #endregion
        
        #region Context Provider Integration
        
        private IInjuryContextProvider _contextProvider;
        
        /// <summary>
        /// Sets the injury context provider for enhanced injury calculations
        /// </summary>
        public void SetContextProvider(IInjuryContextProvider contextProvider)
        {
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            CoreLogger.Log($"[InjuryManager] Context provider set: {contextProvider.GetType().Name}");
        }
        
        /// <summary>
        /// Gets the current context provider
        /// </summary>
        public IInjuryContextProvider GetContextProvider()
        {
            return _contextProvider;
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private void LoadAllPlayerHistories()
        {
            var allHistories = _repository.LoadAllPlayerInjuryHistories();
            _playerHistories.Clear();
            
            foreach (var kvp in allHistories)
            {
                _playerHistories[kvp.Key] = kvp.Value;
            }
            
            CoreLogger.Log($"[InjuryManager] Loaded injury histories for {_playerHistories.Count} players");
        }
        
        private PlayerInjuryHistory GetOrCreatePlayerHistory(int playerId)
        {
            if (!_playerHistories.ContainsKey(playerId))
            {
                _playerHistories[playerId] = new PlayerInjuryHistory(playerId);
            }
            return _playerHistories[playerId];
        }
        
        private InjuryType DetermineTrainingInjuryType()
        {
            // Training injuries are typically muscle or joint related
            var trainingTypes = new[] { InjuryType.Muscle, InjuryType.Joint, InjuryType.Tendon, InjuryType.Other };
            var weights = new[] { 50f, 30f, 15f, 5f }; // Muscle injuries most common in training
            
            return WeightedRandom(trainingTypes, weights);
        }
        
        private InjuryType DetermineMatchInjuryType(float gameContext)
        {
            // Match injuries have more variety including contact injuries
            var matchTypes = new[] { InjuryType.Muscle, InjuryType.Joint, InjuryType.Bone, InjuryType.Ligament, InjuryType.Concussion, InjuryType.Other };
            var weights = new[] { 35f, 25f, 10f, 15f, 5f, 10f }; // More varied than training
            
            // Increase bone/concussion risk for high-intensity phases (like Inside50)
            if (gameContext > 1.2f)
            {
                weights[2] *= 1.5f; // Bone injuries
                weights[4] *= 2.0f; // Concussion
            }
            
            return WeightedRandom(matchTypes, weights);
        }
        
        private InjurySeverity DetermineInjurySeverity(float injuryRisk, int playerAge, int durability)
        {
            // Base severity weights
            var severities = new[] { InjurySeverity.Niggle, InjurySeverity.Minor, InjurySeverity.Moderate, InjurySeverity.Major, InjurySeverity.Severe };
            var weights = new[] { 40f, 35f, 15f, 8f, 2f }; // Most injuries are minor
            
            // Adjust based on risk factors
            float riskMultiplier = injuryRisk;
            
            // Age factor
            if (playerAge > 30) riskMultiplier *= 1.2f;
            else if (playerAge < 22) riskMultiplier *= 1.1f;
            
            // Durability factor
            riskMultiplier *= (100f - durability) / 50f; // Lower durability = higher severity risk
            
            // Shift weights toward more severe injuries based on risk
            for (int i = 2; i < weights.Length; i++) // Start from Moderate
            {
                weights[i] *= (1.0f + riskMultiplier * 0.5f);
            }
            
            return WeightedRandom(severities, weights);
        }
        
        private T WeightedRandom<T>(T[] items, float[] weights)
        {
            float totalWeight = weights.Sum();
            float randomValue = (float)new System.Random().NextDouble() * totalWeight;
            
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
        
        #endregion
    }
    
    /// <summary>
    /// Summary of team-wide injury statistics
    /// </summary>
    public class TeamInjurySummary
    {
        public int PlayersTracked { get; set; }
        public int CurrentlyInjured { get; set; }
        public int TotalInjuries { get; set; }
        public int TotalDaysLost { get; set; }
        public Dictionary<InjurySource, int> InjuriesBySource { get; set; } = new Dictionary<InjurySource, int>();
        
        public float InjuryRate => PlayersTracked > 0 ? (float)CurrentlyInjured / PlayersTracked : 0f;
        public float AverageDaysLost => TotalInjuries > 0 ? (float)TotalDaysLost / TotalInjuries : 0f;
    }
}