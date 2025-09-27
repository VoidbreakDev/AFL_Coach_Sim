using System.Collections.Generic;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.DTO;

namespace AFLCoachSim.Core.Persistence
{
    /// <summary>
    /// Repository interface for injury data persistence
    /// </summary>
    public interface IInjuryRepository
    {
        #region Player Injury History Operations
        
        /// <summary>
        /// Saves or updates a player's injury history
        /// </summary>
        void SavePlayerInjuryHistory(PlayerInjuryHistory history);
        
        /// <summary>
        /// Loads injury history for a specific player
        /// </summary>
        PlayerInjuryHistory LoadPlayerInjuryHistory(int playerId);
        
        /// <summary>
        /// Loads injury histories for all players
        /// </summary>
        IDictionary<int, PlayerInjuryHistory> LoadAllPlayerInjuryHistories();
        
        /// <summary>
        /// Removes injury history for a specific player
        /// </summary>
        void RemovePlayerInjuryHistory(int playerId);
        
        #endregion
        
        #region Individual Injury Operations
        
        /// <summary>
        /// Saves or updates a specific injury
        /// </summary>
        void SaveInjury(Injury injury);
        
        /// <summary>
        /// Loads a specific injury by ID
        /// </summary>
        Injury LoadInjury(InjuryId injuryId);
        
        /// <summary>
        /// Loads all injuries for a specific player
        /// </summary>
        IEnumerable<Injury> LoadPlayerInjuries(int playerId);
        
        /// <summary>
        /// Loads all active injuries across all players
        /// </summary>
        IEnumerable<Injury> LoadActiveInjuries();
        
        #endregion
        
        #region Bulk Operations
        
        /// <summary>
        /// Saves all injury data as a complete dataset
        /// </summary>
        void SaveAllInjuryData(InjuryDataDTO injuryData);
        
        /// <summary>
        /// Loads all injury data as a complete dataset
        /// </summary>
        InjuryDataDTO LoadAllInjuryData();
        
        /// <summary>
        /// Clears all injury data
        /// </summary>
        void ClearAllInjuryData();
        
        /// <summary>
        /// Clears injury data for a specific player
        /// </summary>
        void ClearPlayerInjuryData(int playerId);
        
        #endregion
        
        #region Maintenance Operations
        
        /// <summary>
        /// Checks if injury data exists
        /// </summary>
        bool HasInjuryData();
        
        /// <summary>
        /// Creates a backup of injury data
        /// </summary>
        void BackupInjuryData(string backupSuffix);
        
        /// <summary>
        /// Restores injury data from a backup
        /// </summary>
        bool RestoreInjuryData(string backupSuffix);
        
        #endregion
    }
}