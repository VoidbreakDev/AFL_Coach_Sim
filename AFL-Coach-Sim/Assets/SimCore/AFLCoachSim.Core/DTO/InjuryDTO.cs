using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Injuries.Domain;

namespace AFLCoachSim.Core.DTO
{
    /// <summary>
    /// Data Transfer Object for individual injuries - serializable for Unity JsonUtility
    /// </summary>
    [System.Serializable]
    public class InjuryDTO
    {
        public int InjuryId;
        public int PlayerId;
        public int Type; // InjuryType as int
        public int Severity; // InjurySeverity as int
        public int Source; // InjurySource as int
        public string OccurredDate;
        public int ExpectedRecoveryDays;
        public int ActualRecoveryDays = -1; // -1 indicates null
        public string Description;
        public int Status; // InjuryStatus as int
        public float PerformanceImpactMultiplier;
        public string ReturnToTrainingDate; // null if not set
        public string ReturnToMatchDate; // null if not set
        public bool IsRecurring;
        public int OriginalInjuryId = -1; // -1 indicates null
        public float RecurrenceRisk;
        
        /// <summary>
        /// Creates DTO from domain injury
        /// </summary>
        public static InjuryDTO FromDomain(Injury injury)
        {
            if (injury == null) return null;
            
            return new InjuryDTO
            {
                InjuryId = injury.Id,
                PlayerId = injury.PlayerId,
                Type = (int)injury.Type,
                Severity = (int)injury.Severity,
                Source = (int)injury.Source,
                OccurredDate = injury.OccurredDate.ToString("O"),
                ExpectedRecoveryDays = injury.ExpectedRecoveryDays,
                ActualRecoveryDays = injury.ActualRecoveryDays ?? -1,
                Description = injury.Description,
                Status = (int)injury.Status,
                PerformanceImpactMultiplier = injury.PerformanceImpactMultiplier,
                ReturnToTrainingDate = injury.ReturnToTrainingDate?.ToString("O"),
                ReturnToMatchDate = injury.ReturnToMatchDate?.ToString("O"),
                IsRecurring = injury.IsRecurring,
                OriginalInjuryId = injury.OriginalInjuryId ?? -1,
                RecurrenceRisk = injury.RecurrenceRisk
            };
        }
        
        /// <summary>
        /// Converts DTO back to domain injury
        /// </summary>
        public Injury ToDomain()
        {
            try
            {
                var injury = new Injury(
                    PlayerId,
                    (InjuryType)Type,
                    (InjurySeverity)Severity,
                    (InjurySource)Source,
                    Description,
                    IsRecurring,
                    OriginalInjuryId == -1 ? null : OriginalInjuryId
                );
                
                // Use reflection to set private fields for persistence
                var injuryType = typeof(Injury);
                
                injuryType.GetProperty("Id")?.SetValue(injury, InjuryId);
                injuryType.GetProperty("OccurredDate")?.SetValue(injury, DateTime.Parse(OccurredDate));
                injuryType.GetProperty("ExpectedRecoveryDays")?.SetValue(injury, ExpectedRecoveryDays);
                injuryType.GetProperty("Status")?.SetValue(injury, (InjuryStatus)Status);
                injuryType.GetProperty("PerformanceImpactMultiplier")?.SetValue(injury, PerformanceImpactMultiplier);
                injuryType.GetProperty("RecurrenceRisk")?.SetValue(injury, RecurrenceRisk);
                
                if (ActualRecoveryDays != -1)
                {
                    injuryType.GetProperty("ActualRecoveryDays")?.SetValue(injury, ActualRecoveryDays);
                }
                
                if (!string.IsNullOrEmpty(ReturnToTrainingDate))
                {
                    injuryType.GetProperty("ReturnToTrainingDate")?.SetValue(injury, DateTime.Parse(ReturnToTrainingDate));
                }
                
                if (!string.IsNullOrEmpty(ReturnToMatchDate))
                {
                    injuryType.GetProperty("ReturnToMatchDate")?.SetValue(injury, DateTime.Parse(ReturnToMatchDate));
                }
                
                return injury;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[InjuryDTO] Failed to convert injury {InjuryId} to domain: {e.Message}");
                return null;
            }
        }
    }
    
    /// <summary>
    /// Data Transfer Object for player injury history - serializable for Unity JsonUtility
    /// </summary>
    [System.Serializable]
    public class PlayerInjuryHistoryDTO
    {
        public int PlayerId;
        public List<InjuryDTO> Injuries;
        public string LastUpdated;
        
        public PlayerInjuryHistoryDTO()
        {
            Injuries = new List<InjuryDTO>();
        }
        
        /// <summary>
        /// Creates DTO from domain injury history
        /// </summary>
        public static PlayerInjuryHistoryDTO FromDomain(PlayerInjuryHistory history)
        {
            if (history == null) return null;
            
            var dto = new PlayerInjuryHistoryDTO
            {
                PlayerId = history.PlayerId,
                LastUpdated = DateTime.Now.ToString("O")
            };
            
            foreach (var injury in history.AllInjuries)
            {
                var injuryDto = InjuryDTO.FromDomain(injury);
                if (injuryDto != null)
                {
                    dto.Injuries.Add(injuryDto);
                }
            }
            
            return dto;
        }
        
        /// <summary>
        /// Converts DTO back to domain injury history
        /// </summary>
        public PlayerInjuryHistory ToDomain()
        {
            try
            {
                var history = new PlayerInjuryHistory(PlayerId);
                
                foreach (var injuryDto in Injuries)
                {
                    var injury = injuryDto.ToDomain();
                    if (injury != null)
                    {
                        // Use reflection to add injury to private collection
                        var historyType = typeof(PlayerInjuryHistory);
                        var injuriesField = historyType.GetField("_injuries", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var injuriesList = injuriesField?.GetValue(history) as List<Injury>;
                        injuriesList?.Add(injury);
                    }
                }
                
                return history;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[PlayerInjuryHistoryDTO] Failed to convert injury history for player {PlayerId}: {e.Message}");
                return new PlayerInjuryHistory(PlayerId);
            }
        }
    }
    
    /// <summary>
    /// Container DTO for all injury data - integrates with existing training persistence
    /// </summary>
    [System.Serializable]
    public class InjuryDataDTO
    {
        public string Version;
        public string SavedAt;
        public List<PlayerInjuryHistoryDTO> PlayerInjuryHistories;
        
        public InjuryDataDTO()
        {
            Version = "1.0";
            SavedAt = DateTime.Now.ToString("O");
            PlayerInjuryHistories = new List<PlayerInjuryHistoryDTO>();
        }
    }
}