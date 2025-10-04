using System;
using System.Collections.Generic;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Training
{
    /// <summary>
    /// Handles migration and versioning of training data for backward compatibility
    /// </summary>
    public class TrainingDataMigrator
    {
        public const string CURRENT_VERSION = "1.0";
        
        /// <summary>
        /// Migrate training data to the current version if needed
        /// </summary>
        public static TrainingDataDTO MigrateToCurrentVersion(TrainingDataDTO data)
        {
            if (data == null)
            {
                CoreLogger.LogWarning("[TrainingDataMigrator] Cannot migrate null data");
                return new TrainingDataDTO();
            }
            
            string sourceVersion = data.Version ?? "0.0";
            
            if (sourceVersion == CURRENT_VERSION)
            {
                return data; // No migration needed
            }
            
            CoreLogger.Log($"[TrainingDataMigrator] Migrating training data from version {sourceVersion} to {CURRENT_VERSION}");
            
            var migratedData = data;
            
            // Apply migrations in order based on source version
            if (IsVersionLessThan(sourceVersion, "1.0"))
            {
                migratedData = MigrateFrom0ToV1(migratedData);
            }
            
            // Future migrations would be added here
            // if (IsVersionLessThan(sourceVersion, "2.0"))
            // {
            //     migratedData = MigrateFromV1ToV2(migratedData);
            // }
            
            migratedData.Version = CURRENT_VERSION;
            migratedData.SavedAt = DateTime.Now.ToString("O");
            
            CoreLogger.Log($"[TrainingDataMigrator] Successfully migrated training data to version {CURRENT_VERSION}");
            return migratedData;
        }
        
        /// <summary>
        /// Check if this migrator can handle the given data version
        /// </summary>
        public static bool CanMigrate(string version)
        {
            if (string.IsNullOrEmpty(version))
                return true; // Can migrate from null/empty version (pre-versioning)
                
            // List of supported source versions
            var supportedVersions = new[] { "0.0", "0.1", "0.9", "1.0" };
            
            foreach (var supported in supportedVersions)
            {
                if (version == supported)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get migration warnings for a specific version upgrade
        /// </summary>
        public static List<string> GetMigrationWarnings(string fromVersion, string toVersion)
        {
            var warnings = new List<string>();
            
            if (IsVersionLessThan(fromVersion, "1.0"))
            {
                warnings.Add("Migration from pre-1.0 versions may lose some training efficiency history detail");
                warnings.Add("Player development curves will be recalculated based on current system");
            }
            
            return warnings;
        }
        
        #region Version-Specific Migrations
        
        /// <summary>
        /// Migrate from pre-versioned or 0.x to version 1.0
        /// </summary>
        private static TrainingDataDTO MigrateFrom0ToV1(TrainingDataDTO data)
        {
            CoreLogger.Log("[TrainingDataMigrator] Applying migration from pre-1.0 to version 1.0");
            
            var migrated = new TrainingDataDTO
            {
                Version = "1.0",
                SavedAt = DateTime.Now.ToString("O")
            };
            
            // Migrate player potentials with data validation and cleanup
            foreach (var potential in data.PlayerPotentials)
            {
                var migratedPotential = MigrateDevelopmentPotential_0To1(potential);
                if (migratedPotential != null)
                {
                    migrated.PlayerPotentials.Add(migratedPotential);
                }
            }
            
            // Migrate enrollments with enhanced structure
            foreach (var enrollment in data.Enrollments)
            {
                var migratedEnrollment = MigrateEnrollment_0To1(enrollment);
                if (migratedEnrollment != null)
                {
                    migrated.Enrollments.Add(migratedEnrollment);
                }
            }
            
            // Migrate sessions with improved data structure
            foreach (var session in data.CompletedSessions)
            {
                var migratedSession = MigrateSession_0To1(session);
                if (migratedSession != null)
                {
                    migrated.CompletedSessions.Add(migratedSession);
                }
            }
            
            foreach (var session in data.ScheduledSessions)
            {
                var migratedSession = MigrateSession_0To1(session);
                if (migratedSession != null)
                {
                    migrated.ScheduledSessions.Add(migratedSession);
                }
            }
            
            // Migrate efficiency history with validation
            foreach (var efficiency in data.EfficiencyHistory)
            {
                var migratedEfficiency = MigrateEfficiency_0To1(efficiency);
                if (migratedEfficiency != null)
                {
                    migrated.EfficiencyHistory.Add(migratedEfficiency);
                }
            }
            
            CoreLogger.Log($"[TrainingDataMigrator] Migrated {migrated.PlayerPotentials.Count} potentials, " +
                     $"{migrated.Enrollments.Count} enrollments, " +
                     $"{migrated.CompletedSessions.Count + migrated.ScheduledSessions.Count} sessions");
            
            return migrated;
        }
        
        private static DevelopmentPotentialDTO MigrateDevelopmentPotential_0To1(DevelopmentPotentialDTO original)
        {
            if (original == null) return null;
            
            try
            {
                var migrated = new DevelopmentPotentialDTO
                {
                    PlayerId = original.PlayerId,
                    OverallPotential = Math.Max(0f, Math.Min(100f, original.OverallPotential)), // Clamp to valid range
                    DevelopmentRate = Math.Max(0.1f, Math.Min(3.0f, original.DevelopmentRate)), // Clamp to reasonable range
                    InjuryProneness = Math.Max(0.1f, Math.Min(5.0f, original.InjuryProneness)), // Clamp to reasonable range
                    LastUpdated = string.IsNullOrEmpty(original.LastUpdated) ? DateTime.Now.ToString("O") : original.LastUpdated
                };
                
                // Migrate attribute potentials with validation
                for (int i = 0; i < original.AttributePotentialKeys.Count && i < original.AttributePotentialValues.Count; i++)
                {
                    var key = original.AttributePotentialKeys[i];
                    var value = original.AttributePotentialValues[i];
                    
                    // Validate and clamp attribute values
                    if (!string.IsNullOrEmpty(key) && value >= 0f && value <= 100f)
                    {
                        migrated.AttributePotentialKeys.Add(key);
                        migrated.AttributePotentialValues.Add(value);
                    }
                }
                
                // Migrate preferred training with validation
                foreach (var focus in original.PreferredTrainingFoci)
                {
                    if (Enum.IsDefined(typeof(TrainingFocus), focus))
                    {
                        migrated.PreferredTrainingFoci.Add(focus);
                    }
                }
                
                return migrated;
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[TrainingDataMigrator] Failed to migrate development potential for player {original.PlayerId}: {e.Message}");
                return null;
            }
        }
        
        private static PlayerTrainingEnrollmentDTO MigrateEnrollment_0To1(PlayerTrainingEnrollmentDTO original)
        {
            if (original == null) return null;
            
            try
            {
                var migrated = new PlayerTrainingEnrollmentDTO
                {
                    PlayerId = original.PlayerId,
                    ProgramId = original.ProgramId ?? "unknown-program",
                    StartDate = string.IsNullOrEmpty(original.StartDate) ? DateTime.Now.ToString("O") : original.StartDate,
                    EndDate = original.EndDate,
                    ProgressPercentage = Math.Max(0f, Math.Min(100f, original.ProgressPercentage)),
                    SessionsCompleted = Math.Max(0, original.SessionsCompleted),
                    SessionsMissed = Math.Max(0, original.SessionsMissed),
                    TotalFatigueAccumulated = Math.Max(0f, original.TotalFatigueAccumulated),
                    TotalInjuryRisk = Math.Max(0f, Math.Min(1f, original.TotalInjuryRisk)),
                    IsActive = original.IsActive
                };
                
                // Migrate cumulative gains with validation
                for (int i = 0; i < original.CumulativeGainKeys.Count && i < original.CumulativeGainValues.Count; i++)
                {
                    var key = original.CumulativeGainKeys[i];
                    var value = original.CumulativeGainValues[i];
                    
                    if (!string.IsNullOrEmpty(key) && value >= 0f)
                    {
                        migrated.CumulativeGainKeys.Add(key);
                        migrated.CumulativeGainValues.Add(value);
                    }
                }
                
                return migrated;
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[TrainingDataMigrator] Failed to migrate enrollment for player {original.PlayerId}: {e.Message}");
                return null;
            }
        }
        
        private static TrainingSessionDTO MigrateSession_0To1(TrainingSessionDTO original)
        {
            if (original == null) return null;
            
            try
            {
                var migrated = new TrainingSessionDTO
                {
                    Id = original.Id ?? Guid.NewGuid().ToString(),
                    ProgramId = original.ProgramId ?? "unknown-program",
                    ScheduledDate = string.IsNullOrEmpty(original.ScheduledDate) ? DateTime.Now.ToString("O") : original.ScheduledDate,
                    CompletedDate = original.CompletedDate,
                    Intensity = Math.Max(1, Math.Min(4, original.Intensity)), // Clamp to valid intensity range
                    ParticipatingPlayers = new List<int>(original.ParticipatingPlayers ?? new List<int>()),
                    Notes = original.Notes ?? "",
                    IsCompleted = original.IsCompleted
                };
                
                // Migrate outcomes with validation
                for (int i = 0; i < original.OutcomePlayerIds.Count && 
                           i < original.OutcomeInjuryRisks.Count && 
                           i < original.OutcomeFatigueAccumulations.Count; i++)
                {
                    var playerId = original.OutcomePlayerIds[i];
                    var injuryRisk = Math.Max(0f, Math.Min(1f, original.OutcomeInjuryRisks[i]));
                    var fatigue = Math.Max(0f, original.OutcomeFatigueAccumulations[i]);
                    
                    migrated.OutcomePlayerIds.Add(playerId);
                    migrated.OutcomeInjuryRisks.Add(injuryRisk);
                    migrated.OutcomeFatigueAccumulations.Add(fatigue);
                }
                
                return migrated;
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[TrainingDataMigrator] Failed to migrate session {original.Id}: {e.Message}");
                return null;
            }
        }
        
        private static TrainingEfficiencyDTO MigrateEfficiency_0To1(TrainingEfficiencyDTO original)
        {
            if (original == null) return null;
            
            try
            {
                var migrated = new TrainingEfficiencyDTO
                {
                    PlayerId = original.PlayerId,
                    TrainingType = original.TrainingType,
                    Focus = original.Focus,
                    EfficiencyRating = Math.Max(0f, Math.Min(5f, original.EfficiencyRating)),
                    SessionsCompleted = Math.Max(0, original.SessionsCompleted),
                    AverageGain = Math.Max(0f, original.AverageGain),
                    InjuryIncidence = Math.Max(0f, Math.Min(1f, original.InjuryIncidence)),
                    LastMeasured = string.IsNullOrEmpty(original.LastMeasured) ? DateTime.Now.ToString("O") : original.LastMeasured
                };
                
                return migrated;
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[TrainingDataMigrator] Failed to migrate efficiency for player {original.PlayerId}: {e.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Version Comparison Utilities
        
        /// <summary>
        /// Compare two version strings to determine if first is less than second
        /// </summary>
        private static bool IsVersionLessThan(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1)) return true;
            if (string.IsNullOrEmpty(version2)) return false;
            
            try
            {
                var v1Parts = version1.Split('.');
                var v2Parts = version2.Split('.');
                
                int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
                
                for (int i = 0; i < maxLength; i++)
                {
                    int v1Part = i < v1Parts.Length && int.TryParse(v1Parts[i], out int p1) ? p1 : 0;
                    int v2Part = i < v2Parts.Length && int.TryParse(v2Parts[i], out int p2) ? p2 : 0;
                    
                    if (v1Part < v2Part) return true;
                    if (v1Part > v2Part) return false;
                }
                
                return false; // Versions are equal
            }
            catch (Exception e)
            {
                CoreLogger.LogError($"[TrainingDataMigrator] Failed to compare versions {version1} and {version2}: {e.Message}");
                return false;
            }
        }
        
        #endregion
    }
}