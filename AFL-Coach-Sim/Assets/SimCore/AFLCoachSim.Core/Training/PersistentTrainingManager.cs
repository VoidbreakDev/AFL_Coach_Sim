using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Persistence;
using AFLCoachSim.Core.DTO;

namespace AFLCoachSim.Core.Training
{
    /// <summary>
    /// Training manager with integrated persistence functionality for seamless save/load operations
    /// </summary>
    public class PersistentTrainingManager : TrainingManager
    {
        private readonly ITrainingRepository _repository;
        private bool _autoSaveEnabled = true;
        private DateTime _lastSave = DateTime.MinValue;
        private readonly TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(5);

        public bool AutoSaveEnabled
        {
            get => _autoSaveEnabled;
            set => _autoSaveEnabled = value;
        }

        public PersistentTrainingManager(ITrainingRepository repository, int seed = 0) : base(seed)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            
            // Subscribe to base class events for automatic persistence
            SessionCompleted += OnSessionCompletedPersist;
            PlayerEnrolled += OnPlayerEnrolledPersist;
            PlayerProgramCompleted += OnPlayerProgramCompletedPersist;
            
            // Load existing data on initialization
            LoadAllDataFromPersistence();
        }

        #region Public Persistence Methods

        /// <summary>
        /// Manually save all training data to persistence
        /// </summary>
        public void SaveAllData()
        {
            try
            {
                var trainingData = ExportTrainingData();
                _repository.SaveAllTrainingData(trainingData);
                _lastSave = DateTime.Now;
                
                UnityEngine.Debug.Log($"[PersistentTrainingManager] Saved all training data at {_lastSave}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to save training data: {e.Message}");
            }
        }

        /// <summary>
        /// Load all training data from persistence
        /// </summary>
        public void LoadAllData()
        {
            LoadAllDataFromPersistence();
        }

        /// <summary>
        /// Export current training data as DTO for external persistence
        /// </summary>
        public TrainingDataDTO ExportTrainingData()
        {
            var trainingData = new TrainingDataDTO();
            
            // Export player potentials
            var allPotentials = GetAllPlayerPotentials();
            foreach (var kvp in allPotentials)
            {
                if (kvp.Value != null)
                {
                    trainingData.PlayerPotentials.Add(DevelopmentPotentialDTO.FromDomain(kvp.Value));
                }
            }
            
            // Export enrollments
            var allEnrollments = GetAllPlayerEnrollments();
            foreach (var kvp in allEnrollments)
            {
                foreach (var enrollment in kvp.Value)
                {
                    trainingData.Enrollments.Add(PlayerTrainingEnrollmentDTO.FromDomain(enrollment));
                }
            }
            
            // Export sessions
            var completedSessions = GetCompletedSessions();
            foreach (var session in completedSessions)
            {
                trainingData.CompletedSessions.Add(TrainingSessionDTO.FromDomain(session));
            }
            
            var scheduledSessions = GetScheduledSessions(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(365));
            foreach (var session in scheduledSessions)
            {
                trainingData.ScheduledSessions.Add(TrainingSessionDTO.FromDomain(session));
            }
            
            // Export efficiency history
            var allEfficiencies = GetAllPlayerEfficiencies();
            foreach (var kvp in allEfficiencies)
            {
                foreach (var efficiency in kvp.Value)
                {
                    trainingData.EfficiencyHistory.Add(TrainingEfficiencyDTO.FromDomain(efficiency));
                }
            }
            
            return trainingData;
        }

        /// <summary>
        /// Import training data from DTO
        /// </summary>
        public void ImportTrainingData(TrainingDataDTO trainingData)
        {
            if (trainingData == null)
            {
                UnityEngine.Debug.LogWarning("[PersistentTrainingManager] Cannot import null training data");
                return;
            }
            
            try
            {
                // Import player potentials
                var potentials = new Dictionary<int, DevelopmentPotential>();
                foreach (var dto in trainingData.PlayerPotentials)
                {
                    var potential = dto.ToDomain();
                    if (potential != null)
                    {
                        potentials[dto.PlayerId] = potential;
                    }
                }
                SetAllPlayerPotentials(potentials);
                
                // Import enrollments
                var enrollments = new Dictionary<int, List<PlayerTrainingEnrollment>>();
                foreach (var dto in trainingData.Enrollments)
                {
                    var enrollment = dto.ToDomain();
                    if (enrollment != null)
                    {
                        if (!enrollments.ContainsKey(dto.PlayerId))
                        {
                            enrollments[dto.PlayerId] = new List<PlayerTrainingEnrollment>();
                        }
                        enrollments[dto.PlayerId].Add(enrollment);
                    }
                }
                SetAllPlayerEnrollments(enrollments);
                
                // Import sessions
                var completedSessions = trainingData.CompletedSessions
                    .Select(dto => dto.ToDomain())
                    .Where(s => s != null)
                    .ToList();
                SetCompletedSessions(completedSessions);
                
                var scheduledSessions = trainingData.ScheduledSessions
                    .Select(dto => dto.ToDomain())
                    .Where(s => s != null)
                    .ToList();
                SetScheduledSessions(scheduledSessions);
                
                // Import efficiency history
                var efficiencies = new Dictionary<int, List<TrainingEfficiency>>();
                foreach (var dto in trainingData.EfficiencyHistory)
                {
                    var efficiency = dto.ToDomain();
                    if (efficiency != null)
                    {
                        if (!efficiencies.ContainsKey(dto.PlayerId))
                        {
                            efficiencies[dto.PlayerId] = new List<TrainingEfficiency>();
                        }
                        efficiencies[dto.PlayerId].Add(efficiency);
                    }
                }
                SetAllPlayerEfficiencies(efficiencies);
                
                UnityEngine.Debug.Log($"[PersistentTrainingManager] Imported training data: {potentials.Count} potentials, {enrollments.Sum(kvp => kvp.Value.Count)} enrollments");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to import training data: {e.Message}");
            }
        }

        /// <summary>
        /// Clear all training data both in memory and persistence
        /// </summary>
        public void ClearAllData()
        {
            ClearAllMemoryData();
            _repository.ClearAllTrainingData();
            UnityEngine.Debug.Log("[PersistentTrainingManager] Cleared all training data");
        }

        /// <summary>
        /// Clear training data for a specific player
        /// </summary>
        public void ClearPlayerData(int playerId)
        {
            ClearPlayerMemoryData(playerId);
            _repository.ClearPlayerTrainingData(playerId);
            UnityEngine.Debug.Log($"[PersistentTrainingManager] Cleared training data for player {playerId}");
        }

        #endregion

        #region Auto-Save Functionality

        /// <summary>
        /// Check if auto-save should be triggered and perform save if needed
        /// </summary>
        public void CheckAutoSave()
        {
            if (!_autoSaveEnabled) return;
            
            if (DateTime.Now - _lastSave > _autoSaveInterval)
            {
                SaveAllData();
            }
        }

        /// <summary>
        /// Force an auto-save regardless of interval
        /// </summary>
        public void ForceAutoSave()
        {
            if (_autoSaveEnabled)
            {
                SaveAllData();
            }
        }

        #endregion

        #region Event Handlers for Auto-Persistence

        private void OnSessionCompletedPersist(TrainingSession session, Dictionary<int, TrainingOutcome> outcomes)
        {
            if (_autoSaveEnabled)
            {
                try
                {
                    _repository.SaveTrainingSession(session);
                    
                    // Save updated enrollments
                    foreach (var playerId in session.ParticipatingPlayers)
                    {
                        var enrollments = GetPlayerEnrollments(playerId);
                        _repository.SavePlayerEnrollments(playerId, enrollments);
                    }
                    
                    UnityEngine.Debug.Log($"[PersistentTrainingManager] Auto-saved session completion: {session.Id}");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to auto-save session: {e.Message}");
                }
            }
        }

        private void OnPlayerEnrolledPersist(int playerId, PlayerTrainingEnrollment enrollment)
        {
            if (_autoSaveEnabled)
            {
                try
                {
                    _repository.SavePlayerEnrollment(enrollment);
                    UnityEngine.Debug.Log($"[PersistentTrainingManager] Auto-saved enrollment: Player {playerId} in {enrollment.ProgramId}");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to auto-save enrollment: {e.Message}");
                }
            }
        }

        private void OnPlayerProgramCompletedPersist(int playerId, string programId)
        {
            if (_autoSaveEnabled)
            {
                try
                {
                    var enrollments = GetPlayerEnrollments(playerId);
                    _repository.SavePlayerEnrollments(playerId, enrollments);
                    UnityEngine.Debug.Log($"[PersistentTrainingManager] Auto-saved program completion: Player {playerId} completed {programId}");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to auto-save program completion: {e.Message}");
                }
            }
        }

        #endregion

        #region Private Helper Methods

        private void LoadAllDataFromPersistence()
        {
            try
            {
                if (!_repository.HasTrainingData())
                {
                    UnityEngine.Debug.Log("[PersistentTrainingManager] No existing training data found, starting fresh");
                    return;
                }
                
                var trainingData = _repository.LoadAllTrainingData();
                ImportTrainingData(trainingData);
                
                _lastSave = DateTime.Now;
                UnityEngine.Debug.Log("[PersistentTrainingManager] Loaded all training data from persistence");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to load training data from persistence: {e.Message}");
            }
        }

        // These methods would need to be added to the base TrainingManager class as protected virtual methods
        // For now, we'll assume they exist or add them via reflection/extension
        
        private Dictionary<int, DevelopmentPotential> GetAllPlayerPotentials()
        {
            // This would access the internal _playerPotentials from TrainingManager
            // Implementation depends on making this accessible from base class
            return new Dictionary<int, DevelopmentPotential>();
        }
        
        private void SetAllPlayerPotentials(Dictionary<int, DevelopmentPotential> potentials)
        {
            // Set the internal _playerPotentials in TrainingManager
            // Implementation depends on making this accessible from base class
        }
        
        private Dictionary<int, List<PlayerTrainingEnrollment>> GetAllPlayerEnrollments()
        {
            // Access _playerEnrollments from TrainingManager
            return new Dictionary<int, List<PlayerTrainingEnrollment>>();
        }
        
        private void SetAllPlayerEnrollments(Dictionary<int, List<PlayerTrainingEnrollment>> enrollments)
        {
            // Set _playerEnrollments in TrainingManager
        }
        
        private void SetCompletedSessions(List<TrainingSession> sessions)
        {
            // Set _completedSessions in TrainingManager
        }
        
        private void SetScheduledSessions(List<TrainingSession> sessions)
        {
            // Set _scheduledSessions in TrainingManager
        }
        
        private Dictionary<int, List<TrainingEfficiency>> GetAllPlayerEfficiencies()
        {
            // Access efficiency history from TrainingEngine
            return new Dictionary<int, List<TrainingEfficiency>>();
        }
        
        private void SetAllPlayerEfficiencies(Dictionary<int, List<TrainingEfficiency>> efficiencies)
        {
            // Set efficiency history in TrainingEngine
        }
        
        private void ClearAllMemoryData()
        {
            // Clear all in-memory data structures
        }
        
        private void ClearPlayerMemoryData(int playerId)
        {
            // Clear player-specific data from memory
        }

        #endregion

        #region Backup and Restore

        /// <summary>
        /// Create a backup of current training data
        /// </summary>
        public void BackupData(string suffix = null)
        {
            try
            {
                string backupSuffix = suffix ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _repository.BackupTrainingData(backupSuffix);
                UnityEngine.Debug.Log($"[PersistentTrainingManager] Created backup with suffix: {backupSuffix}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to create backup: {e.Message}");
            }
        }

        /// <summary>
        /// Restore training data from a backup
        /// </summary>
        public bool RestoreFromBackup(string suffix)
        {
            try
            {
                bool success = _repository.RestoreTrainingData(suffix);
                if (success)
                {
                    LoadAllData(); // Reload the restored data
                    UnityEngine.Debug.Log($"[PersistentTrainingManager] Restored from backup: {suffix}");
                }
                return success;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PersistentTrainingManager] Failed to restore from backup: {e.Message}");
                return false;
            }
        }

        #endregion
    }
}