using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AFLCoachSim.Core.Training;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Persistence
{
    /// <summary>
    /// JSON file-based implementation of training data persistence
    /// </summary>
    public class JsonTrainingRepository : ITrainingRepository
    {
        private static string DataFolder => Application.persistentDataPath;
        private const string TrainingDataFileName = "training_data.json";
        private static string TrainingDataFilePath => Path.Combine(DataFolder, TrainingDataFileName);

        #region Development Potential

        public void SavePlayerPotential(int playerId, DevelopmentPotential potential)
        {
            var allData = LoadAllTrainingDataInternal();
            
            // Remove existing potential for this player
            allData.PlayerPotentials.RemoveAll(p => p.PlayerId == playerId);
            
            // Add new potential
            if (potential != null)
            {
                allData.PlayerPotentials.Add(DevelopmentPotentialDTO.FromDomain(potential));
            }
            
            SaveAllTrainingDataInternal(allData);
        }

        public DevelopmentPotential LoadPlayerPotential(int playerId)
        {
            var allData = LoadAllTrainingDataInternal();
            var dto = allData.PlayerPotentials.FirstOrDefault(p => p.PlayerId == playerId);
            return dto?.ToDomain();
        }

        public void SaveAllPlayerPotentials(IDictionary<int, DevelopmentPotential> potentials)
        {
            var allData = LoadAllTrainingDataInternal();
            allData.PlayerPotentials.Clear();
            
            foreach (var kvp in potentials)
            {
                if (kvp.Value != null)
                {
                    allData.PlayerPotentials.Add(DevelopmentPotentialDTO.FromDomain(kvp.Value));
                }
            }
            
            SaveAllTrainingDataInternal(allData);
        }

        public IDictionary<int, DevelopmentPotential> LoadAllPlayerPotentials()
        {
            var allData = LoadAllTrainingDataInternal();
            var result = new Dictionary<int, DevelopmentPotential>();
            
            foreach (var dto in allData.PlayerPotentials)
            {
                var potential = dto.ToDomain();
                if (potential != null)
                {
                    result[dto.PlayerId] = potential;
                }
            }
            
            return result;
        }

        #endregion

        #region Training Enrollments

        public void SavePlayerEnrollment(PlayerTrainingEnrollment enrollment)
        {
            if (enrollment == null) return;
            
            var allData = LoadAllTrainingDataInternal();
            
            // Remove existing enrollment with same player and program
            allData.Enrollments.RemoveAll(e => e.PlayerId == enrollment.PlayerId && e.ProgramId == enrollment.ProgramId);
            
            // Add new enrollment
            allData.Enrollments.Add(PlayerTrainingEnrollmentDTO.FromDomain(enrollment));
            
            SaveAllTrainingDataInternal(allData);
        }

        public void SavePlayerEnrollments(int playerId, IEnumerable<PlayerTrainingEnrollment> enrollments)
        {
            var allData = LoadAllTrainingDataInternal();
            
            // Remove all existing enrollments for this player
            allData.Enrollments.RemoveAll(e => e.PlayerId == playerId);
            
            // Add new enrollments
            foreach (var enrollment in enrollments.Where(e => e != null))
            {
                allData.Enrollments.Add(PlayerTrainingEnrollmentDTO.FromDomain(enrollment));
            }
            
            SaveAllTrainingDataInternal(allData);
        }

        public IEnumerable<PlayerTrainingEnrollment> LoadPlayerEnrollments(int playerId)
        {
            var allData = LoadAllTrainingDataInternal();
            return allData.Enrollments
                .Where(e => e.PlayerId == playerId)
                .Select(e => e.ToDomain())
                .Where(e => e != null)
                .ToList();
        }

        public IDictionary<int, IList<PlayerTrainingEnrollment>> LoadAllEnrollments()
        {
            var allData = LoadAllTrainingDataInternal();
            var result = new Dictionary<int, IList<PlayerTrainingEnrollment>>();
            
            var groups = allData.Enrollments.GroupBy(e => e.PlayerId);
            foreach (var group in groups)
            {
                var enrollments = group.Select(e => e.ToDomain()).Where(e => e != null).ToList();
                if (enrollments.Any())
                {
                    result[group.Key] = enrollments;
                }
            }
            
            return result;
        }

        public void RemovePlayerEnrollment(int playerId, string programId)
        {
            var allData = LoadAllTrainingDataInternal();
            allData.Enrollments.RemoveAll(e => e.PlayerId == playerId && e.ProgramId == programId);
            SaveAllTrainingDataInternal(allData);
        }

        #endregion

        #region Training Sessions

        public void SaveTrainingSession(TrainingSession session)
        {
            if (session == null) return;
            
            var allData = LoadAllTrainingDataInternal();
            var sessionDto = TrainingSessionDTO.FromDomain(session);
            
            if (session.IsCompleted)
            {
                // Remove from scheduled and add to completed
                allData.ScheduledSessions.RemoveAll(s => s.Id == session.Id);
                allData.CompletedSessions.RemoveAll(s => s.Id == session.Id);
                allData.CompletedSessions.Add(sessionDto);
            }
            else
            {
                // Remove from completed and add to scheduled
                allData.CompletedSessions.RemoveAll(s => s.Id == session.Id);
                allData.ScheduledSessions.RemoveAll(s => s.Id == session.Id);
                allData.ScheduledSessions.Add(sessionDto);
            }
            
            SaveAllTrainingDataInternal(allData);
        }

        public void SaveCompletedSessions(IEnumerable<TrainingSession> sessions)
        {
            var allData = LoadAllTrainingDataInternal();
            allData.CompletedSessions.Clear();
            
            foreach (var session in sessions.Where(s => s != null))
            {
                allData.CompletedSessions.Add(TrainingSessionDTO.FromDomain(session));
            }
            
            SaveAllTrainingDataInternal(allData);
        }

        public void SaveScheduledSessions(IEnumerable<TrainingSession> sessions)
        {
            var allData = LoadAllTrainingDataInternal();
            allData.ScheduledSessions.Clear();
            
            foreach (var session in sessions.Where(s => s != null))
            {
                allData.ScheduledSessions.Add(TrainingSessionDTO.FromDomain(session));
            }
            
            SaveAllTrainingDataInternal(allData);
        }

        public IEnumerable<TrainingSession> LoadCompletedSessions()
        {
            var allData = LoadAllTrainingDataInternal();
            return allData.CompletedSessions
                .Select(s => s.ToDomain())
                .Where(s => s != null)
                .ToList();
        }

        public IEnumerable<TrainingSession> LoadScheduledSessions()
        {
            var allData = LoadAllTrainingDataInternal();
            return allData.ScheduledSessions
                .Select(s => s.ToDomain())
                .Where(s => s != null)
                .ToList();
        }

        public TrainingSession LoadTrainingSession(string sessionId)
        {
            var allData = LoadAllTrainingDataInternal();
            
            // Check completed sessions first
            var completedDto = allData.CompletedSessions.FirstOrDefault(s => s.Id == sessionId);
            if (completedDto != null)
            {
                return completedDto.ToDomain();
            }
            
            // Check scheduled sessions
            var scheduledDto = allData.ScheduledSessions.FirstOrDefault(s => s.Id == sessionId);
            return scheduledDto?.ToDomain();
        }

        #endregion

        #region Training Efficiency

        public void SavePlayerEfficiency(int playerId, IEnumerable<TrainingEfficiency> efficiencies)
        {
            var allData = LoadAllTrainingDataInternal();
            
            // Remove existing efficiency records for this player
            allData.EfficiencyHistory.RemoveAll(e => e.PlayerId == playerId);
            
            // Add new efficiency records
            foreach (var efficiency in efficiencies.Where(e => e != null))
            {
                allData.EfficiencyHistory.Add(TrainingEfficiencyDTO.FromDomain(efficiency));
            }
            
            SaveAllTrainingDataInternal(allData);
        }

        public IEnumerable<TrainingEfficiency> LoadPlayerEfficiencies(int playerId)
        {
            var allData = LoadAllTrainingDataInternal();
            return allData.EfficiencyHistory
                .Where(e => e.PlayerId == playerId)
                .Select(e => e.ToDomain())
                .Where(e => e != null)
                .OrderBy(e => e.LastMeasured)
                .ToList();
        }

        public IDictionary<int, IList<TrainingEfficiency>> LoadAllEfficiencies()
        {
            var allData = LoadAllTrainingDataInternal();
            var result = new Dictionary<int, IList<TrainingEfficiency>>();
            
            var groups = allData.EfficiencyHistory.GroupBy(e => e.PlayerId);
            foreach (var group in groups)
            {
                var efficiencies = group.Select(e => e.ToDomain())
                    .Where(e => e != null)
                    .OrderBy(e => e.LastMeasured)
                    .ToList();
                if (efficiencies.Any())
                {
                    result[group.Key] = efficiencies;
                }
            }
            
            return result;
        }

        #endregion

        #region Bulk Operations

        public void SaveAllTrainingData(TrainingDataDTO trainingData)
        {
            SaveAllTrainingDataInternal(trainingData);
        }

        public TrainingDataDTO LoadAllTrainingData()
        {
            return LoadAllTrainingDataInternal();
        }

        public void ClearAllTrainingData()
        {
            try
            {
                if (File.Exists(TrainingDataFilePath))
                {
                    File.Delete(TrainingDataFilePath);
                }
                CoreLogger.Log("[JsonTrainingRepository] Cleared all training data");
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[JsonTrainingRepository] Failed to clear training data: {e.Message}");
            }
        }

        public void ClearPlayerTrainingData(int playerId)
        {
            var allData = LoadAllTrainingDataInternal();
            
            allData.PlayerPotentials.RemoveAll(p => p.PlayerId == playerId);
            allData.Enrollments.RemoveAll(e => e.PlayerId == playerId);
            allData.EfficiencyHistory.RemoveAll(e => e.PlayerId == playerId);
            
            // Remove sessions where this player was the only participant
            allData.ScheduledSessions.RemoveAll(s => s.ParticipatingPlayers.Count == 1 && s.ParticipatingPlayers.Contains(playerId));
            allData.CompletedSessions.RemoveAll(s => s.ParticipatingPlayers.Count == 1 && s.ParticipatingPlayers.Contains(playerId));
            
            // Remove player from sessions with multiple participants
            foreach (var session in allData.ScheduledSessions.Concat(allData.CompletedSessions))
            {
                session.ParticipatingPlayers.RemoveAll(p => p == playerId);
                session.OutcomePlayerIds.RemoveAll(p => p == playerId);
                // Note: This will leave mismatched arrays, but it's acceptable for the cleanup operation
            }
            
            SaveAllTrainingDataInternal(allData);
            CoreLogger.Log($"[JsonTrainingRepository] Cleared training data for player {playerId}");
        }

        #endregion

        #region Maintenance Operations

        public bool HasTrainingData()
        {
            return File.Exists(TrainingDataFilePath);
        }

        public void BackupTrainingData(string backupSuffix)
        {
            try
            {
                if (!File.Exists(TrainingDataFilePath))
                {
                    CoreLogger.LogWarning("[JsonTrainingRepository] No training data file to backup");
                    return;
                }
                
                string backupPath = Path.Combine(DataFolder, $"training_data_backup_{backupSuffix}.json");
                File.Copy(TrainingDataFilePath, backupPath, overwrite: true);
                CoreLogger.Log($"[JsonTrainingRepository] Training data backed up to: {backupPath}");
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[JsonTrainingRepository] Failed to backup training data: {e.Message}");
            }
        }

        public bool RestoreTrainingData(string backupSuffix)
        {
            try
            {
                string backupPath = Path.Combine(DataFolder, $"training_data_backup_{backupSuffix}.json");
                if (!File.Exists(backupPath))
                {
                    CoreLogger.LogWarning($"[JsonTrainingRepository] Backup file not found: {backupPath}");
                    return false;
                }
                
                File.Copy(backupPath, TrainingDataFilePath, overwrite: true);
                CoreLogger.Log($"[JsonTrainingRepository] Training data restored from: {backupPath}");
                return true;
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[JsonTrainingRepository] Failed to restore training data: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Internal Methods

        private TrainingDataDTO LoadAllTrainingDataInternal()
        {
            try
            {
                if (!File.Exists(TrainingDataFilePath))
                {
                    return new TrainingDataDTO();
                }
                
                string json = File.ReadAllText(TrainingDataFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new TrainingDataDTO();
                }
                
                var rawData = JsonUtility.FromJson<TrainingDataDTO>(json);
                if (rawData == null)
                {
                    return new TrainingDataDTO();
                }
                
                // Apply data migration if needed
                string originalVersion = rawData.Version;
                var migratedData = TrainingDataMigrator.MigrateToCurrentVersion(rawData);
                
                // If migration occurred, save the migrated data and create backup of original
                if (migratedData.Version != originalVersion)
                {
                    CoreLogger.Log($"[JsonTrainingRepository] Data migrated from {originalVersion} to {migratedData.Version}, saving migrated version");
                    
                    // Create backup of original version before saving migrated data
                    string backupName = $"pre_migration_{originalVersion ?? "unknown"}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    BackupTrainingData(backupName);
                    
                    // Save migrated data (but avoid recursive call)
                    SaveMigratedDataDirect(migratedData);
                }
                
                return migratedData;
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[JsonTrainingRepository] Failed to load training data: {e.Message}");
                return new TrainingDataDTO();
            }
        }

        private void SaveAllTrainingDataInternal(TrainingDataDTO data)
        {
            try
            {
                if (data == null)
                {
                    CoreLogger.LogError("[JsonTrainingRepository] Cannot save null training data");
                    return;
                }
                
                data.SavedAt = DateTime.Now.ToString("O");
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(TrainingDataFilePath, json);
                
                #if UNITY_EDITOR
                var fileInfo = new FileInfo(TrainingDataFilePath);
                CoreLogger.Log($"[JsonTrainingRepository] Training data saved: {fileInfo.Length} bytes to {TrainingDataFilePath}");
                #endif
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[JsonTrainingRepository] Failed to save training data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Save migrated data directly without triggering another load (to avoid recursion)
        /// </summary>
        private void SaveMigratedDataDirect(TrainingDataDTO data)
        {
            try
            {
                if (data == null)
                {
                    CoreLogger.LogError("[JsonTrainingRepository] Cannot save null migrated data");
                    return;
                }
                
                data.SavedAt = DateTime.Now.ToString("O");
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(TrainingDataFilePath, json);
                
                CoreLogger.Log($"[JsonTrainingRepository] Migrated training data saved to {TrainingDataFilePath}");
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[JsonTrainingRepository] Failed to save migrated data: {e.Message}");
            }
        }

        #endregion
    }
}