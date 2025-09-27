using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Training;
using AFLCoachSim.Core.DTO;
using NUnit.Framework;
using UnityEngine;

namespace AFLCoachSim.Tests.Training
{
    /// <summary>
    /// Tests for the TrainingDataMigrator and data migration functionality
    /// </summary>
    [TestFixture]
    public class TrainingDataMigrationTests
    {
        #region Setup & Helpers
        
        private TrainingDataDTO CreateLegacyTestData(string version = null)
        {
            return new TrainingDataDTO
            {
                Version = version,
                SavedAt = "2024-01-15T12:00:00.0000000Z",
                PlayerPotentials = new List<DevelopmentPotentialDTO>
                {
                    new DevelopmentPotentialDTO
                    {
                        PlayerId = 1,
                        OverallPotential = 85.5f,
                        DevelopmentRate = 1.2f,
                        InjuryProneness = 2.1f,
                        AttributePotentialKeys = new List<string> { "Speed", "Marking" },
                        AttributePotentialValues = new List<float> { 90.0f, 80.0f },
                        PreferredTrainingFoci = new List<TrainingFocus> { TrainingFocus.Fitness, TrainingFocus.Skills },
                        LastUpdated = "2024-01-01T10:00:00Z"
                    }
                },
                Enrollments = new List<PlayerTrainingEnrollmentDTO>
                {
                    new PlayerTrainingEnrollmentDTO
                    {
                        PlayerId = 1,
                        ProgramId = "basic-fitness",
                        StartDate = "2024-01-01T00:00:00Z",
                        ProgressPercentage = 45.0f,
                        SessionsCompleted = 3,
                        SessionsMissed = 1,
                        TotalFatigueAccumulated = 15.5f,
                        TotalInjuryRisk = 0.2f,
                        IsActive = true,
                        CumulativeGainKeys = new List<string> { "Fitness" },
                        CumulativeGainValues = new List<float> { 2.5f }
                    }
                },
                CompletedSessions = new List<TrainingSessionDTO>
                {
                    new TrainingSessionDTO
                    {
                        Id = "session-1",
                        ProgramId = "basic-fitness",
                        ScheduledDate = "2024-01-01T14:00:00Z",
                        CompletedDate = "2024-01-01T15:30:00Z",
                        Intensity = 2,
                        ParticipatingPlayers = new List<int> { 1, 2 },
                        IsCompleted = true,
                        OutcomePlayerIds = new List<int> { 1, 2 },
                        OutcomeInjuryRisks = new List<float> { 0.1f, 0.15f },
                        OutcomeFatigueAccumulations = new List<float> { 5.0f, 6.0f }
                    }
                },
                ScheduledSessions = new List<TrainingSessionDTO>
                {
                    new TrainingSessionDTO
                    {
                        Id = "session-2",
                        ProgramId = "basic-fitness",
                        ScheduledDate = "2024-01-02T14:00:00Z",
                        Intensity = 3,
                        ParticipatingPlayers = new List<int> { 1 },
                        IsCompleted = false
                    }
                },
                EfficiencyHistory = new List<TrainingEfficiencyDTO>
                {
                    new TrainingEfficiencyDTO
                    {
                        PlayerId = 1,
                        TrainingType = TrainingType.Individual,
                        Focus = TrainingFocus.Fitness,
                        EfficiencyRating = 3.2f,
                        SessionsCompleted = 5,
                        AverageGain = 1.8f,
                        InjuryIncidence = 0.1f,
                        LastMeasured = "2024-01-15T10:00:00Z"
                    }
                }
            };
        }
        
        #endregion
        
        #region Version Detection & Migration Tests
        
        [Test]
        public void MigrateToCurrentVersion_WithCurrentVersion_ReturnsUnchanged()
        {
            // Arrange
            var currentData = CreateLegacyTestData(TrainingDataMigrator.CURRENT_VERSION);
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(currentData);
            
            // Assert
            Assert.That(result.Version, Is.EqualTo(TrainingDataMigrator.CURRENT_VERSION));
            Assert.That(result, Is.EqualTo(currentData)); // Should be the same reference
        }
        
        [Test]
        public void MigrateToCurrentVersion_WithNullData_ReturnsEmptyData()
        {
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(null);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PlayerPotentials, Is.Empty);
            Assert.That(result.Enrollments, Is.Empty);
            Assert.That(result.CompletedSessions, Is.Empty);
            Assert.That(result.ScheduledSessions, Is.Empty);
            Assert.That(result.EfficiencyHistory, Is.Empty);
        }
        
        [Test]
        public void MigrateToCurrentVersion_FromLegacyVersion_MigratesSuccessfully()
        {
            // Arrange
            var legacyData = CreateLegacyTestData("0.9");
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            
            // Assert
            Assert.That(result.Version, Is.EqualTo(TrainingDataMigrator.CURRENT_VERSION));
            Assert.That(result.PlayerPotentials, Has.Count.EqualTo(1));
            Assert.That(result.Enrollments, Has.Count.EqualTo(1));
            Assert.That(result.CompletedSessions, Has.Count.EqualTo(1));
            Assert.That(result.ScheduledSessions, Has.Count.EqualTo(1));
            Assert.That(result.EfficiencyHistory, Has.Count.EqualTo(1));
        }
        
        [Test]
        public void MigrateToCurrentVersion_FromNullVersion_MigratesSuccessfully()
        {
            // Arrange
            var legacyData = CreateLegacyTestData(null);
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            
            // Assert  
            Assert.That(result.Version, Is.EqualTo(TrainingDataMigrator.CURRENT_VERSION));
            Assert.That(result.SavedAt, Is.Not.Null.And.Not.Empty);
            Assert.That(result.PlayerPotentials, Has.Count.EqualTo(1));
        }
        
        #endregion
        
        #region Data Validation During Migration Tests
        
        [Test]
        public void MigrateToCurrentVersion_ClampsInvalidValues()
        {
            // Arrange
            var legacyData = new TrainingDataDTO
            {
                Version = "0.5",
                PlayerPotentials = new List<DevelopmentPotentialDTO>
                {
                    new DevelopmentPotentialDTO
                    {
                        PlayerId = 1,
                        OverallPotential = -10f, // Invalid negative value
                        DevelopmentRate = 10.0f, // Invalid high value
                        InjuryProneness = 0.05f, // Invalid low value
                        AttributePotentialKeys = new List<string> { "Speed", "", "Marking" }, // Empty key
                        AttributePotentialValues = new List<float> { 120f, 50f, -5f }, // Out of range values
                        PreferredTrainingFoci = new List<TrainingFocus> { TrainingFocus.Fitness }
                    }
                }
            };
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            
            // Assert
            var potential = result.PlayerPotentials[0];
            Assert.That(potential.OverallPotential, Is.EqualTo(0f)); // Clamped to minimum
            Assert.That(potential.DevelopmentRate, Is.EqualTo(3.0f)); // Clamped to maximum
            Assert.That(potential.InjuryProneness, Is.EqualTo(0.1f)); // Clamped to minimum
            Assert.That(potential.AttributePotentialKeys, Has.Count.EqualTo(1)); // Invalid entries filtered out
            Assert.That(potential.AttributePotentialKeys[0], Is.EqualTo("Marking"));
            Assert.That(potential.AttributePotentialValues[0], Is.EqualTo(50f)); // Only valid value preserved
        }
        
        [Test]
        public void MigrateToCurrentVersion_FiltersInvalidEnumValues()
        {
            // Arrange
            var legacyData = new TrainingDataDTO
            {
                Version = "0.8",
                PlayerPotentials = new List<DevelopmentPotentialDTO>
                {
                    new DevelopmentPotentialDTO
                    {
                        PlayerId = 1,
                        OverallPotential = 80f,
                        DevelopmentRate = 1.0f,
                        InjuryProneness = 1.5f,
                        PreferredTrainingFoci = new List<TrainingFocus> 
                        { 
                            TrainingFocus.Fitness,
                            (TrainingFocus)999, // Invalid enum value
                            TrainingFocus.Skills 
                        }
                    }
                }
            };
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            
            // Assert
            var potential = result.PlayerPotentials[0];
            Assert.That(potential.PreferredTrainingFoci, Has.Count.EqualTo(2));
            Assert.That(potential.PreferredTrainingFoci, Contains.Item(TrainingFocus.Fitness));
            Assert.That(potential.PreferredTrainingFoci, Contains.Item(TrainingFocus.Skills));
            Assert.That(potential.PreferredTrainingFoci, Does.Not.Contain((TrainingFocus)999));
        }
        
        [Test]
        public void MigrateToCurrentVersion_HandlesCorruptedData()
        {
            // Arrange
            var legacyData = new TrainingDataDTO
            {
                Version = "0.7",
                Enrollments = new List<PlayerTrainingEnrollmentDTO>
                {
                    new PlayerTrainingEnrollmentDTO
                    {
                        PlayerId = 1,
                        ProgramId = null, // Will be defaulted
                        StartDate = "", // Will be defaulted
                        ProgressPercentage = 150f, // Invalid, will be clamped
                        SessionsCompleted = -5, // Invalid, will be clamped
                        TotalInjuryRisk = 2.0f, // Invalid, will be clamped
                        IsActive = true
                    }
                }
            };
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            
            // Assert
            var enrollment = result.Enrollments[0];
            Assert.That(enrollment.ProgramId, Is.EqualTo("unknown-program"));
            Assert.That(enrollment.StartDate, Is.Not.Null.And.Not.Empty);
            Assert.That(enrollment.ProgressPercentage, Is.EqualTo(100f)); // Clamped to max
            Assert.That(enrollment.SessionsCompleted, Is.EqualTo(0)); // Clamped to min
            Assert.That(enrollment.TotalInjuryRisk, Is.EqualTo(1.0f)); // Clamped to max
        }
        
        #endregion
        
        #region Version Compatibility Tests
        
        [Test]
        public void CanMigrate_WithSupportedVersions_ReturnsTrue()
        {
            // Test supported versions
            Assert.That(TrainingDataMigrator.CanMigrate("0.0"), Is.True);
            Assert.That(TrainingDataMigrator.CanMigrate("0.1"), Is.True);
            Assert.That(TrainingDataMigrator.CanMigrate("0.9"), Is.True);
            Assert.That(TrainingDataMigrator.CanMigrate("1.0"), Is.True);
            Assert.That(TrainingDataMigrator.CanMigrate(null), Is.True);
            Assert.That(TrainingDataMigrator.CanMigrate(""), Is.True);
        }
        
        [Test]
        public void CanMigrate_WithUnsupportedVersions_ReturnsFalse()
        {
            // Test unsupported versions
            Assert.That(TrainingDataMigrator.CanMigrate("2.0"), Is.False);
            Assert.That(TrainingDataMigrator.CanMigrate("1.5"), Is.False);
            Assert.That(TrainingDataMigrator.CanMigrate("invalid"), Is.False);
        }
        
        [Test]
        public void GetMigrationWarnings_ForLegacyVersions_ReturnsWarnings()
        {
            // Act
            var warnings = TrainingDataMigrator.GetMigrationWarnings("0.9", "1.0");
            
            // Assert
            Assert.That(warnings, Is.Not.Null);
            Assert.That(warnings, Is.Not.Empty);
            Assert.That(warnings, Has.Some.Contains("efficiency history"));
            Assert.That(warnings, Has.Some.Contains("development curves"));
        }
        
        [Test]
        public void GetMigrationWarnings_ForCurrentVersion_ReturnsEmptyWarnings()
        {
            // Act
            var warnings = TrainingDataMigrator.GetMigrationWarnings("1.0", "1.0");
            
            // Assert
            Assert.That(warnings, Is.Not.Null);
            Assert.That(warnings, Is.Empty);
        }
        
        #endregion
        
        #region Session Data Migration Tests
        
        [Test]
        public void MigrateToCurrentVersion_ValidatesSessionData()
        {
            // Arrange
            var legacyData = new TrainingDataDTO
            {
                Version = "0.6",
                CompletedSessions = new List<TrainingSessionDTO>
                {
                    new TrainingSessionDTO
                    {
                        Id = null, // Will be generated
                        ProgramId = "", // Will be defaulted
                        ScheduledDate = "", // Will be defaulted
                        Intensity = 10, // Invalid, will be clamped
                        ParticipatingPlayers = new List<int> { 1, 2 },
                        OutcomePlayerIds = new List<int> { 1, 2, 3 }, // Mismatched with other outcome arrays
                        OutcomeInjuryRisks = new List<float> { 2.0f }, // Invalid risk and mismatched length
                        OutcomeFatigueAccumulations = new List<float> { -5.0f, 10.0f } // One invalid negative value
                    }
                }
            };
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            
            // Assert
            var session = result.CompletedSessions[0];
            Assert.That(session.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(session.ProgramId, Is.EqualTo("unknown-program"));
            Assert.That(session.ScheduledDate, Is.Not.Null.And.Not.Empty);
            Assert.That(session.Intensity, Is.InRange(1, 4)); // Clamped to valid range
            Assert.That(session.OutcomeInjuryRisks[0], Is.InRange(0f, 1f)); // Clamped injury risk
            Assert.That(session.OutcomeFatigueAccumulations[0], Is.GreaterThanOrEqualTo(0f)); // Negative fatigue clamped
        }
        
        #endregion
        
        #region Performance and Edge Case Tests
        
        [Test]
        public void MigrateToCurrentVersion_WithLargeDataset_PerformsReasonably()
        {
            // Arrange
            var legacyData = new TrainingDataDTO
            {
                Version = "0.5",
                PlayerPotentials = new List<DevelopmentPotentialDTO>(),
                Enrollments = new List<PlayerTrainingEnrollmentDTO>(),
                EfficiencyHistory = new List<TrainingEfficiencyDTO>()
            };
            
            // Create a large dataset
            for (int i = 1; i <= 1000; i++)
            {
                legacyData.PlayerPotentials.Add(new DevelopmentPotentialDTO
                {
                    PlayerId = i,
                    OverallPotential = 75f + (i % 25),
                    DevelopmentRate = 1.0f + (i % 3) * 0.5f,
                    InjuryProneness = 1.0f + (i % 5) * 0.5f
                });
                
                legacyData.Enrollments.Add(new PlayerTrainingEnrollmentDTO
                {
                    PlayerId = i,
                    ProgramId = $"program-{i % 10}",
                    StartDate = DateTime.Now.AddDays(-i).ToString("O"),
                    ProgressPercentage = (i % 100),
                    IsActive = i % 3 == 0
                });
                
                legacyData.EfficiencyHistory.Add(new TrainingEfficiencyDTO
                {
                    PlayerId = i,
                    TrainingType = (TrainingType)(i % 3),
                    Focus = (TrainingFocus)(i % 4),
                    EfficiencyRating = 1.0f + (i % 4),
                    SessionsCompleted = i % 20
                });
            }
            
            // Act
            var startTime = DateTime.Now;
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            var migrationTime = DateTime.Now - startTime;
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Version, Is.EqualTo(TrainingDataMigrator.CURRENT_VERSION));
            Assert.That(result.PlayerPotentials, Has.Count.EqualTo(1000));
            Assert.That(result.Enrollments, Has.Count.EqualTo(1000));
            Assert.That(result.EfficiencyHistory, Has.Count.EqualTo(1000));
            Assert.That(migrationTime.TotalSeconds, Is.LessThan(5), "Migration should complete within 5 seconds");
            
            Debug.Log($"[TrainingDataMigrationTests] Large dataset migration completed in {migrationTime.TotalMilliseconds}ms");
        }
        
        [Test]
        public void MigrateToCurrentVersion_WithEmptyCollections_HandlesGracefully()
        {
            // Arrange
            var legacyData = new TrainingDataDTO
            {
                Version = "0.3",
                PlayerPotentials = new List<DevelopmentPotentialDTO>(),
                Enrollments = new List<PlayerTrainingEnrollmentDTO>(),
                CompletedSessions = new List<TrainingSessionDTO>(),
                ScheduledSessions = new List<TrainingSessionDTO>(),
                EfficiencyHistory = new List<TrainingEfficiencyDTO>()
            };
            
            // Act
            var result = TrainingDataMigrator.MigrateToCurrentVersion(legacyData);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Version, Is.EqualTo(TrainingDataMigrator.CURRENT_VERSION));
            Assert.That(result.SavedAt, Is.Not.Null);
            Assert.That(result.PlayerPotentials, Is.Empty);
            Assert.That(result.Enrollments, Is.Empty);
            Assert.That(result.CompletedSessions, Is.Empty);
            Assert.That(result.ScheduledSessions, Is.Empty);
            Assert.That(result.EfficiencyHistory, Is.Empty);
        }
        
        #endregion
    }
}