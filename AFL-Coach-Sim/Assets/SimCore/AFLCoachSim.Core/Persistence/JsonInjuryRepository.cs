using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.DTO;
using UnityEngine;

namespace AFLCoachSim.Core.Persistence
{
    /// <summary>
    /// JSON file-based implementation of injury data persistence
    /// </summary>
    public class JsonInjuryRepository : IInjuryRepository
    {
        private static string DataFolder => Application.persistentDataPath;
        private const string InjuryDataFileName = "injury_data.json";
        private static string InjuryDataFilePath => Path.Combine(DataFolder, InjuryDataFileName);
        
        #region Player Injury History Operations
        
        public void SavePlayerInjuryHistory(PlayerInjuryHistory history)
        {
            if (history == null) return;
            
            var allData = LoadAllInjuryDataInternal();
            
            // Remove existing history for this player
            allData.PlayerInjuryHistories.RemoveAll(h => h.PlayerId == history.PlayerId);
            
            // Add new/updated history
            var historyDto = PlayerInjuryHistoryDTO.FromDomain(history);
            if (historyDto != null)
            {
                allData.PlayerInjuryHistories.Add(historyDto);
            }
            
            SaveAllInjuryDataInternal(allData);
        }
        
        public PlayerInjuryHistory LoadPlayerInjuryHistory(int playerId)
        {
            var allData = LoadAllInjuryDataInternal();
            var historyDto = allData.PlayerInjuryHistories.FirstOrDefault(h => h.PlayerId == playerId);
            return historyDto?.ToDomain() ?? new PlayerInjuryHistory(playerId);
        }
        
        public IDictionary<int, PlayerInjuryHistory> LoadAllPlayerInjuryHistories()
        {
            var allData = LoadAllInjuryDataInternal();
            var result = new Dictionary<int, PlayerInjuryHistory>();
            
            foreach (var historyDto in allData.PlayerInjuryHistories)
            {
                var history = historyDto.ToDomain();
                if (history != null)
                {
                    result[historyDto.PlayerId] = history;
                }
            }
            
            return result;
        }
        
        public void RemovePlayerInjuryHistory(int playerId)
        {
            var allData = LoadAllInjuryDataInternal();
            allData.PlayerInjuryHistories.RemoveAll(h => h.PlayerId == playerId);
            SaveAllInjuryDataInternal(allData);
        }
        
        #endregion
        
        #region Individual Injury Operations
        
        public void SaveInjury(Injury injury)
        {
            if (injury == null) return;
            
            // Load player's history and update it
            var history = LoadPlayerInjuryHistory(injury.PlayerId);
            
            // Find and replace existing injury or add new one
            var historyType = typeof(PlayerInjuryHistory);
            var injuriesField = historyType.GetField("_injuries", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var injuriesList = injuriesField?.GetValue(history) as List<Injury>;
            
            if (injuriesList != null)
            {
                // Remove existing injury with same ID
                injuriesList.RemoveAll(i => i.Id == injury.Id);
                // Add the injury
                injuriesList.Add(injury);
                
                // Save the updated history
                SavePlayerInjuryHistory(history);
            }
        }
        
        public Injury LoadInjury(InjuryId injuryId)
        {
            var allHistories = LoadAllPlayerInjuryHistories();
            
            foreach (var history in allHistories.Values)
            {
                var injury = history.AllInjuries.FirstOrDefault(i => i.Id == injuryId);
                if (injury != null) return injury;
            }
            
            return null;
        }
        
        public IEnumerable<Injury> LoadPlayerInjuries(int playerId)
        {
            var history = LoadPlayerInjuryHistory(playerId);
            return history.AllInjuries;
        }
        
        public IEnumerable<Injury> LoadActiveInjuries()
        {
            var allHistories = LoadAllPlayerInjuryHistories();
            var activeInjuries = new List<Injury>();
            
            foreach (var history in allHistories.Values)
            {
                activeInjuries.AddRange(history.ActiveInjuries);
            }
            
            return activeInjuries;
        }
        
        #endregion
        
        #region Bulk Operations
        
        public void SaveAllInjuryData(InjuryDataDTO injuryData)
        {
            SaveAllInjuryDataInternal(injuryData);
        }
        
        public InjuryDataDTO LoadAllInjuryData()
        {
            return LoadAllInjuryDataInternal();
        }
        
        public void ClearAllInjuryData()
        {
            try
            {
                if (File.Exists(InjuryDataFilePath))
                {
                    File.Delete(InjuryDataFilePath);
                }
                Debug.Log("[JsonInjuryRepository] Cleared all injury data");
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonInjuryRepository] Failed to clear injury data: {e.Message}");
            }
        }
        
        public void ClearPlayerInjuryData(int playerId)
        {
            var allData = LoadAllInjuryDataInternal();
            allData.PlayerInjuryHistories.RemoveAll(h => h.PlayerId == playerId);
            SaveAllInjuryDataInternal(allData);
            
            Debug.Log($"[JsonInjuryRepository] Cleared injury data for player {playerId}");
        }
        
        #endregion
        
        #region Maintenance Operations
        
        public bool HasInjuryData()
        {
            return File.Exists(InjuryDataFilePath);
        }
        
        public void BackupInjuryData(string backupSuffix)
        {
            try
            {
                if (!File.Exists(InjuryDataFilePath))
                {
                    Debug.LogWarning("[JsonInjuryRepository] No injury data file to backup");
                    return;
                }
                
                string backupPath = Path.Combine(DataFolder, $"injury_data_backup_{backupSuffix}.json");
                File.Copy(InjuryDataFilePath, backupPath, overwrite: true);
                Debug.Log($"[JsonInjuryRepository] Injury data backed up to: {backupPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonInjuryRepository] Failed to backup injury data: {e.Message}");
            }
        }
        
        public bool RestoreInjuryData(string backupSuffix)
        {
            try
            {
                string backupPath = Path.Combine(DataFolder, $"injury_data_backup_{backupSuffix}.json");
                if (!File.Exists(backupPath))
                {
                    Debug.LogWarning($"[JsonInjuryRepository] Backup file not found: {backupPath}");
                    return false;
                }
                
                File.Copy(backupPath, InjuryDataFilePath, overwrite: true);
                Debug.Log($"[JsonInjuryRepository] Injury data restored from: {backupPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonInjuryRepository] Failed to restore injury data: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Internal Methods
        
        private InjuryDataDTO LoadAllInjuryDataInternal()
        {
            try
            {
                if (!File.Exists(InjuryDataFilePath))
                {
                    return new InjuryDataDTO();
                }
                
                string json = File.ReadAllText(InjuryDataFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new InjuryDataDTO();
                }
                
                var rawData = JsonUtility.FromJson<InjuryDataDTO>(json);
                if (rawData == null)
                {
                    return new InjuryDataDTO();
                }
                
                // Note: Future injury data migration would happen here, similar to training data
                // For now, we start with version 1.0
                
                return rawData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonInjuryRepository] Failed to load injury data: {e.Message}");
                return new InjuryDataDTO();
            }
        }
        
        private void SaveAllInjuryDataInternal(InjuryDataDTO data)
        {
            try
            {
                if (data == null)
                {
                    Debug.LogError("[JsonInjuryRepository] Cannot save null injury data");
                    return;
                }
                
                data.SavedAt = DateTime.Now.ToString("O");
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(InjuryDataFilePath, json);
                
                #if UNITY_EDITOR
                var fileInfo = new FileInfo(InjuryDataFilePath);
                Debug.Log($"[JsonInjuryRepository] Injury data saved: {fileInfo.Length} bytes to {InjuryDataFilePath}");
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonInjuryRepository] Failed to save injury data: {e.Message}");
            }
        }
        
        #endregion
    }
}