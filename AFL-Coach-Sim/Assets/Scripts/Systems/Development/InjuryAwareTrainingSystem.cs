using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Persistence;
using AFLManager.Models;
using AFLManager.Systems.Development;
using UnityEngine;

namespace AFLManager.Systems.Development
{
    /// <summary>
    /// Enhanced training system that integrates with the unified injury management system
    /// </summary>
    public class InjuryAwareTrainingSystem : MonoBehaviour
    {
        private InjuryManager _injuryManager;
        private readonly Dictionary<int, DateTime> _lastTrainingDates;
        private readonly List<TrainingSession> sessionHistory;
        
        // Configuration
        [Header("Injury Management")]
        [SerializeField] private float injuryRiskMultiplier = 1.0f;
        [SerializeField] private bool enableInjuryPrevention = true;
        [SerializeField] private int maxConsecutiveTrainingDays = 7;
        
        public InjuryManager InjuryManager => _injuryManager;
        
        // Events
        public event System.Action<Player, Injury> OnPlayerInjuredInTraining;
        public event System.Action<Player> OnPlayerUnavailableForTraining;
        
        public InjuryAwareTrainingSystem()
        {
            _lastTrainingDates = new Dictionary<int, DateTime>();
            sessionHistory = new List<TrainingSession>();
        }
        
        /// <summary>
        /// Initialize the training system with injury management
        /// </summary>
        public void Initialize(IInjuryRepository injuryRepository)
        {
            _injuryManager = new InjuryManager(injuryRepository);
            
            // Subscribe to injury events
            _injuryManager.OnInjuryOccurred += OnInjuryOccurred;
            _injuryManager.OnInjuryRecovered += OnInjuryRecovered;
            _injuryManager.OnPlayerPerformanceImpactChanged += OnPerformanceImpactChanged;
            
            Debug.Log("[InjuryAwareTrainingSystem] Initialized with injury management integration");
        }
        
        /// <summary>
        /// Starts a training program with injury-aware participant filtering
        /// </summary>
        public bool StartTrainingProgram(TrainingProgram program, List<Player> participants)
        {
            if (_injuryManager == null)
            {
                Debug.LogWarning("[InjuryAwareTrainingSystem] Injury manager not initialized, using basic implementation");
                // Basic implementation without base class
                return participants != null && participants.Count > 0;
            }
            
            // Filter out players who cannot train due to injuries
            var availableParticipants = FilterAvailableParticipants(participants);
            
            if (availableParticipants.Count == 0)
            {
                Debug.LogWarning($"[InjuryAwareTrainingSystem] No players available for training program: {program.Name}");
                return false;
            }
            
            if (availableParticipants.Count < participants.Count)
            {
                Debug.Log($"[InjuryAwareTrainingSystem] Filtered out {participants.Count - availableParticipants.Count} injured players from {program.Name}");
            }
            
            // Start the training program with available participants
            return true;
        }
        
        /// <summary>
        /// Enhanced training processing with proper injury integration
        /// </summary>
        public TrainingResult ProcessProgramWeek(ActiveTrainingProgram activeProgram)
        {
            var result = new TrainingResult
            {
                ProgramName = activeProgram.Program.Name,
                Week = activeProgram.Program.DurationWeeks - activeProgram.WeeksRemaining + 1,
                PlayerImprovements = new List<PlayerTrainingResult>()
            };
            
            // Get available participants (injury check)
            var availableParticipants = FilterAvailableParticipants(activeProgram.Participants);
            
            foreach (var player in availableParticipants)
            {
                var playerResult = ProcessPlayerTraining(player, activeProgram.Program);
                result.PlayerImprovements.Add(playerResult);
                
                // Update last training date
                _lastTrainingDates[int.Parse(player.Id)] = DateTime.Now;
            }
            
            // Record training session
            var session = new TrainingSession
            {
                Date = DateTime.Now,
                ProgramName = activeProgram.Program.Name,
                ParticipantCount = availableParticipants.Count,
                InjuryCount = result.PlayerImprovements.Count(p => p.WasInjured),
                AverageEffectiveness = result.PlayerImprovements.Any() 
                    ? result.PlayerImprovements.Average(p => p.EffectivenessRating) 
                    : 0f
            };
            sessionHistory.Add(session);
            
            return result;
        }
        
        /// <summary>
        /// Processes training for an individual player with injury integration
        /// </summary>
        private PlayerTrainingResult ProcessPlayerTraining(Player player, TrainingProgram program)
        {
            // Calculate development for this player
            var development = player.Development?.CalculateDevelopment(player, program, 1f) ?? new PlayerStatsDelta();
            
            // Get player attributes for injury risk calculation
            int playerAge = GetPlayerAge(player);
            int durability = GetPlayerDurability(player);
            float currentFatigue = GetPlayerFatigue(player);
            
            // Calculate injury risk using the injury manager
            float injuryRisk = _injuryManager.CalculateActivityRisk(
                int.Parse(player.Id), 
                playerAge, 
                durability, 
                currentFatigue,
                GetTrainingExposureMinutes(program),
                program.InjuryRisk * injuryRiskMultiplier
            );
            
            // Check for training injuries
            bool injured = ShouldPlayerGetInjured(injuryRisk);
            Injury trainingInjury = null;
            
            var playerResult = new PlayerTrainingResult
            {
                Player = player,
                StatChanges = development,
                WasInjured = injured,
                EffectivenessRating = CalculateTrainingEffectiveness(player, program)
            };
            
            // Apply results
            if (injured)
            {
                // Record the injury using the injury manager
                trainingInjury = _injuryManager.RecordTrainingInjury(int.Parse(player.Id), injuryRisk, playerAge, durability);
                
                Debug.Log($"[InjuryAwareTrainingSystem] {player.Name} sustained {trainingInjury.Severity} {trainingInjury.Type} injury during {program.Name}");
                
                // Notify subscribers
                OnPlayerInjuredInTraining?.Invoke(player, trainingInjury);
                
                // Reduced training benefit when injured
                development = ScaleStatsDelta(development, 0.3f); // Only 30% benefit when injured
            }
            
            // Apply the improvements to the player (reduced if injured)
            development.ApplyTo(player.Stats);
            
            return playerResult;
        }
        
        /// <summary>
        /// Filters participants to only include those available for training
        /// </summary>
        private List<Player> FilterAvailableParticipants(List<Player> participants)
        {
            var available = new List<Player>();
            
            foreach (var player in participants)
            {
                if (IsPlayerAvailableForTraining(player))
                {
                    available.Add(player);
                }
                else
                {
                    OnPlayerUnavailableForTraining?.Invoke(player);
                }
            }
            
            return available;
        }
        
        /// <summary>
        /// Checks if a player is available for training considering injuries and rest needs
        /// </summary>
        private bool IsPlayerAvailableForTraining(Player player)
        {
            if (_injuryManager == null) return true;
            
            // Check injury status
            if (!_injuryManager.CanPlayerTrain(int.Parse(player.Id)))
            {
                return false;
            }
            
            // Check for overtraining prevention
            if (enableInjuryPrevention && IsPlayerOvertraining(player))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Checks if a player is at risk of overtraining
        /// </summary>
        private bool IsPlayerOvertraining(Player player)
        {
            if (!_lastTrainingDates.ContainsKey(int.Parse(player.Id)))
                return false;
                
            var daysSinceLastTraining = (DateTime.Now - _lastTrainingDates[int.Parse(player.Id)]).Days;
            
            // If they've trained recently and have high fatigue, they need rest
            if (daysSinceLastTraining < 2 && GetPlayerFatigue(player) > 80f)
            {
                return true;
            }
            
            // Check for consecutive training days
            if (daysSinceLastTraining == 0) // Training today
            {
                int consecutiveDays = GetConsecutiveTrainingDays(int.Parse(player.Id));
                return consecutiveDays >= maxConsecutiveTrainingDays;
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculates training effectiveness considering player condition and injuries
        /// </summary>
        private float CalculateTrainingEffectiveness(Player player, TrainingProgram program)
        {
            float baseEffectiveness = program.GetEffectivenessMultiplier(player);
            
            // Apply injury performance impact
            float injuryImpact = _injuryManager?.GetPlayerPerformanceMultiplier(int.Parse(player.Id)) ?? 1.0f;
            
            // Apply fatigue impact
            float fatigue = GetPlayerFatigue(player);
            float fatigueImpact = 1.0f - (fatigue / 200f); // Reduce effectiveness based on fatigue
            
            return baseEffectiveness * injuryImpact * fatigueImpact;
        }
        
        /// <summary>
        /// Enhanced injury check that uses the injury manager's risk calculation
        /// </summary>
        private bool ShouldPlayerGetInjured(float injuryRisk)
        {
            return UnityEngine.Random.Range(0f, 1f) < injuryRisk;
        }
        
        /// <summary>
        /// Gets effective training exposure time based on program intensity
        /// </summary>
        private float GetTrainingExposureMinutes(TrainingProgram program)
        {
            // Base training session is typically 90-120 minutes
            float baseMinutes = 105f;
            
            // Adjust based on program characteristics
            float intensityMultiplier = program.InjuryRisk; // Higher risk programs are typically longer/more intense
            
            return baseMinutes * intensityMultiplier;
        }
        
        /// <summary>
        /// Gets consecutive training days for overtraining prevention
        /// </summary>
        private int GetConsecutiveTrainingDays(int playerId)
        {
            if (!_lastTrainingDates.ContainsKey(playerId))
                return 0;
                
            var lastDate = _lastTrainingDates[playerId];
            var currentDate = DateTime.Now.Date;
            int consecutiveDays = 0;
            
            // Count backwards from today
            for (var date = currentDate; date >= lastDate.Date; date = date.AddDays(-1))
            {
                // This is a simplified check - in a real implementation you'd track daily training history
                if ((currentDate - date).Days < maxConsecutiveTrainingDays)
                {
                    consecutiveDays++;
                }
                else
                {
                    break;
                }
            }
            
            return consecutiveDays;
        }
        
        /// <summary>
        /// Processes daily recovery and injury updates
        /// </summary>
        public void ProcessDailyUpdates()
        {
            _injuryManager?.ProcessDailyRecovery();
        }
        
        /// <summary>
        /// Gets comprehensive training analytics including injury data
        /// </summary>
        public InjuryAwareTrainingAnalytics GetTrainingAnalytics(int weeksBack = 8)
        {
            // Create base analytics from session history
            var recentSessions = sessionHistory.Where(s => s.Date > DateTime.Now.AddDays(-weeksBack * 7)).ToList();
            var baseAnalytics = new TrainingAnalytics
            {
                TotalSessions = recentSessions.Count,
                AverageEffectiveness = recentSessions.Any() ? recentSessions.Average(s => s.AverageEffectiveness) : 0f,
                TotalInjuries = recentSessions.Sum(s => s.InjuryCount),
                InjuryRate = recentSessions.Any() ? recentSessions.Average(s => (float)s.InjuryCount / Math.Max(s.ParticipantCount, 1)) : 0f,
                MostUsedProgram = recentSessions.GroupBy(s => s.ProgramName).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "None"
            };
            
            var analytics = new InjuryAwareTrainingAnalytics
            {
                TotalSessions = baseAnalytics.TotalSessions,
                AverageEffectiveness = baseAnalytics.AverageEffectiveness,
                TotalInjuries = baseAnalytics.TotalInjuries,
                InjuryRate = baseAnalytics.InjuryRate,
                MostUsedProgram = baseAnalytics.MostUsedProgram
            };
            
            // Add injury-specific analytics
            if (_injuryManager != null)
            {
                var period = TimeSpan.FromDays(weeksBack * 7);
                var teamSummary = _injuryManager.GetTeamInjurySummary(period);
                
                analytics.CurrentlyInjured = teamSummary.CurrentlyInjured;
                analytics.TotalDaysLost = teamSummary.TotalDaysLost;
                analytics.AverageDaysLostPerInjury = teamSummary.AverageDaysLost;
                analytics.TrainingInjuries = teamSummary.InjuriesBySource.GetValueOrDefault(InjurySource.Training, 0);
                analytics.TrainingInjuryRate = analytics.TotalSessions > 0 ? (float)analytics.TrainingInjuries / analytics.TotalSessions : 0f;
            }
            
            return analytics;
        }
        
        #region Player Attribute Helpers
        
        private int GetPlayerAge(Player player)
        {
            return player.Age;
        }
        
        private int GetPlayerDurability(Player player)
        {
            // Durability can be estimated from player stats (average of physical attributes)
            // This is a placeholder - implement based on your player model
            return Mathf.RoundToInt((player.Stats.Stamina + player.Stats.Tackling + player.Stats.Speed) / 3f);
        }
        
        private float GetPlayerFatigue(Player player)
        {
            // Use stamina as inverse measure of fatigue (high stamina = low fatigue)
            // Convert from 0-100 stamina to 0-100 fatigue (inverted)
            return 100f - player.Stamina;
        }
        
        /// <summary>
        /// Scales a PlayerStatsDelta by a multiplier
        /// </summary>
        private PlayerStatsDelta ScaleStatsDelta(PlayerStatsDelta delta, float multiplier)
        {
            return new PlayerStatsDelta
            {
                Kicking = delta.Kicking * multiplier,
                Handballing = delta.Handballing * multiplier,
                Tackling = delta.Tackling * multiplier,
                Speed = delta.Speed * multiplier,
                Stamina = delta.Stamina * multiplier,
                Knowledge = delta.Knowledge * multiplier,
                Playmaking = delta.Playmaking * multiplier
            };
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnInjuryOccurred(Injury injury)
        {
            Debug.Log($"[InjuryAwareTrainingSystem] Injury occurred: Player {injury.PlayerId} - {injury.Description}");
        }
        
        private void OnInjuryRecovered(Injury injury)
        {
            Debug.Log($"[InjuryAwareTrainingSystem] Injury recovered: Player {injury.PlayerId} - {injury.Description}");
        }
        
        private void OnPerformanceImpactChanged(int playerId, float newMultiplier)
        {
            Debug.Log($"[InjuryAwareTrainingSystem] Performance impact changed for player {playerId}: {newMultiplier:F2}x");
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_injuryManager != null)
            {
                _injuryManager.OnInjuryOccurred -= OnInjuryOccurred;
                _injuryManager.OnInjuryRecovered -= OnInjuryRecovered;
                _injuryManager.OnPlayerPerformanceImpactChanged -= OnPerformanceImpactChanged;
            }
        }
    }
    
    /// <summary>
    /// Enhanced training analytics that include injury data
    /// </summary>
    [System.Serializable]
    public class InjuryAwareTrainingAnalytics : TrainingAnalytics
    {
        public int CurrentlyInjured;
        public int TotalDaysLost;
        public float AverageDaysLostPerInjury;
        public int TrainingInjuries;
        public float TrainingInjuryRate;
        
        public override string ToString()
        {
            return $"Training Analytics: {TotalSessions} sessions, {AverageEffectiveness:F2} avg effectiveness, " +
                   $"{TotalInjuries} total injuries ({InjuryRate:F3} rate), {CurrentlyInjured} currently injured, " +
                   $"{TrainingInjuries} training injuries ({TrainingInjuryRate:F3} rate)";
        }
    }
}