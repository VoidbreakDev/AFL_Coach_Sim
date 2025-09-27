using System;
using System.Collections.Generic;
using System.Linq;

namespace AFLCoachSim.Core.Injuries.Domain
{
    /// <summary>
    /// Aggregate root representing a player's complete injury history and current status
    /// </summary>
    public class PlayerInjuryHistory
    {
        private readonly List<Injury> _injuries;
        private readonly int _playerId;
        
        public int PlayerId => _playerId;
        public IReadOnlyList<Injury> AllInjuries => _injuries.AsReadOnly();
        public IReadOnlyList<Injury> ActiveInjuries => _injuries.Where(i => i.Status == InjuryStatus.Active).ToList().AsReadOnly();
        public IReadOnlyList<Injury> RecentInjuries => _injuries.Where(i => (DateTime.Now - i.OccurredDate).TotalDays <= 365).ToList().AsReadOnly();
        
        // Current status properties
        public bool IsCurrentlyInjured => ActiveInjuries.Any();
        public bool CanTrainToday => !ActiveInjuries.Any(i => !i.CanParticipateInTraining());
        public bool CanPlayMatches => !ActiveInjuries.Any(i => !i.CanReturnToMatch());
        public float CurrentPerformanceMultiplier => ActiveInjuries.Any() 
            ? ActiveInjuries.Min(i => i.GetCurrentPerformanceImpact()) 
            : 1.0f;
        
        private PlayerInjuryHistory() { } // For persistence
        
        public PlayerInjuryHistory(int playerId)
        {
            _playerId = playerId;
            _injuries = new List<Injury>();
        }
        
        /// <summary>
        /// Records a new injury for the player
        /// </summary>
        public Injury RecordInjury(InjuryType type, InjurySeverity severity, InjurySource source, string description = null)
        {
            // Check if this might be a recurrence
            var recentSimilar = GetRecentSimilarInjuries(type, TimeSpan.FromDays(365));
            bool isRecurring = recentSimilar.Any();
            int? originalInjuryId = isRecurring ? recentSimilar.OrderBy(i => i.OccurredDate).First().Id : null;
            
            var injury = new Injury(_playerId, type, severity, source, description, isRecurring, originalInjuryId);
            _injuries.Add(injury);
            
            return injury;
        }
        
        /// <summary>
        /// Updates recovery progress for all active injuries
        /// </summary>
        public void UpdateRecoveryProgress()
        {
            foreach (var injury in ActiveInjuries.ToList()) // ToList to avoid modification during iteration
            {
                injury.UpdateRecoveryProgress();
            }
        }
        
        /// <summary>
        /// Forces recovery of an injury (e.g., medical clearance)
        /// </summary>
        public bool ForceRecovery(InjuryId injuryId)
        {
            var injury = _injuries.FirstOrDefault(i => i.Id == injuryId);
            if (injury?.Status == InjuryStatus.Active)
            {
                injury.MarkRecovered();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Calculates current injury risk based on history and player factors
        /// </summary>
        public InjuryRiskProfile CalculateCurrentRisk(int playerAge, int durability, float currentFatigue)
        {
            // Calculate recurrence risk from recent injuries
            float recurrenceRisk = 0f;
            var recentInjuries = GetRecentInjuries(TimeSpan.FromDays(365));
            
            foreach (var injury in recentInjuries)
            {
                recurrenceRisk += injury.RecurrenceRisk * 0.3f; // Each recent injury adds to risk
            }
            
            // Additional risk from chronic patterns
            if (HasChronicInjuryPattern())
            {
                recurrenceRisk += 0.2f;
            }
            
            return new InjuryRiskProfile(playerAge, durability, currentFatigue, Math.Min(1.0f, recurrenceRisk));
        }
        
        /// <summary>
        /// Gets injury statistics for analytics
        /// </summary>
        public InjuryStatistics GetStatistics(TimeSpan? period = null)
        {
            var relevantInjuries = period.HasValue 
                ? GetRecentInjuries(period.Value) 
                : AllInjuries;
                
            if (!relevantInjuries.Any())
                return new InjuryStatistics();
            
            return new InjuryStatistics
            {
                TotalInjuries = relevantInjuries.Count,
                DaysLost = relevantInjuries.Where(i => i.ActualRecoveryDays.HasValue)
                                        .Sum(i => i.ActualRecoveryDays.Value),
                AverageRecoveryTime = relevantInjuries.Where(i => i.ActualRecoveryDays.HasValue)
                                                   .Average(i => i.ActualRecoveryDays.Value),
                MostCommonType = relevantInjuries.GroupBy(i => i.Type)
                                               .OrderByDescending(g => g.Count())
                                               .FirstOrDefault()?.Key ?? InjuryType.Other,
                RecurringInjuries = relevantInjuries.Count(i => i.IsRecurring),
                InjuriesBySource = relevantInjuries.GroupBy(i => i.Source)
                                                 .ToDictionary(g => g.Key, g => g.Count()),
                InjuriesBySeverity = relevantInjuries.GroupBy(i => i.Severity)
                                                   .ToDictionary(g => g.Key, g => g.Count())
            };
        }
        
        /// <summary>
        /// Checks if player has a pattern of recurring injuries
        /// </summary>
        public bool HasChronicInjuryPattern()
        {
            var recentYear = GetRecentInjuries(TimeSpan.FromDays(365));
            
            // More than 3 injuries in a year suggests chronic issues
            if (recentYear.Count >= 3) return true;
            
            // Multiple injuries of same type within 18 months
            var recentExtended = GetRecentInjuries(TimeSpan.FromDays(540));
            return recentExtended.GroupBy(i => i.Type).Any(g => g.Count() >= 2);
        }
        
        /// <summary>
        /// Determines if a specific injury type poses higher risk due to history
        /// </summary>
        public float GetRiskMultiplierFor(InjuryType injuryType)
        {
            float multiplier = 1.0f;
            
            // Check for similar injuries in the past year
            var similarRecent = GetRecentSimilarInjuries(injuryType, TimeSpan.FromDays(365));
            multiplier += similarRecent.Count * 0.3f;
            
            // Check for injuries that increase risk for this type
            var riskIncreasers = ActiveInjuries.Where(i => i.IncreasesRiskFor(injuryType));
            multiplier += riskIncreasers.Count() * 0.2f;
            
            return Math.Min(3.0f, multiplier); // Cap at 3x normal risk
        }
        
        /// <summary>
        /// Gets the most severe active injury
        /// </summary>
        public Injury GetMostSevereActiveInjury()
        {
            return ActiveInjuries.OrderByDescending(i => (int)i.Severity).FirstOrDefault();
        }
        
        /// <summary>
        /// Estimates days until player can return to full training
        /// </summary>
        public int GetDaysUntilTrainingReady()
        {
            if (!ActiveInjuries.Any()) return 0;
            
            return ActiveInjuries.Where(i => !i.CanParticipateInTraining())
                                .Max(i => i.DaysRemaining);
        }
        
        /// <summary>
        /// Estimates days until player can return to match play
        /// </summary>
        public int GetDaysUntilMatchReady()
        {
            if (!ActiveInjuries.Any()) return 0;
            
            return ActiveInjuries.Where(i => !i.CanReturnToMatch())
                                .Max(i => i.DaysRemaining);
        }
        
        private IReadOnlyList<Injury> GetRecentInjuries(TimeSpan period)
        {
            var cutoff = DateTime.Now - period;
            return _injuries.Where(i => i.OccurredDate >= cutoff).ToList().AsReadOnly();
        }
        
        private IReadOnlyList<Injury> GetRecentSimilarInjuries(InjuryType type, TimeSpan period)
        {
            var cutoff = DateTime.Now - period;
            return _injuries.Where(i => i.Type == type && i.OccurredDate >= cutoff).ToList().AsReadOnly();
        }
    }
    
    /// <summary>
    /// Statistics about a player's injury history
    /// </summary>
    public class InjuryStatistics
    {
        public int TotalInjuries { get; set; }
        public int DaysLost { get; set; }
        public double AverageRecoveryTime { get; set; }
        public InjuryType MostCommonType { get; set; }
        public int RecurringInjuries { get; set; }
        public Dictionary<InjurySource, int> InjuriesBySource { get; set; } = new Dictionary<InjurySource, int>();
        public Dictionary<InjurySeverity, int> InjuriesBySeverity { get; set; } = new Dictionary<InjurySeverity, int>();
        
        public float InjuryRate => TotalInjuries > 0 ? TotalInjuries / 365f : 0f; // Injuries per year
        public float RecurrenceRate => TotalInjuries > 0 ? (float)RecurringInjuries / TotalInjuries : 0f;
    }
}