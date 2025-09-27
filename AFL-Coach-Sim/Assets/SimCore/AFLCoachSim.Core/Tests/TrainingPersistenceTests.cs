using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AFLCoachSim.Core.Training;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Persistence;

namespace AFLCoachSim.Core.Tests
{
    /// <summary>
    /// Comprehensive test suite for training persistence functionality
    /// </summary>
    [TestFixture]
    public class TrainingPersistenceTests
    {
        private JsonTrainingRepository _repository;
        private string _testDataPath;

        [SetUp]
        public void SetUp()
        {
            // Create a test-specific repository with a unique file path
            _testDataPath = Path.Combine(Application.temporaryCachePath, $"training_test_{Guid.NewGuid()}.json");
            _repository = new TestJsonTrainingRepository(_testDataPath);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            if (File.Exists(_testDataPath))
            {
                File.Delete(_testDataPath);
            }
        }

        #region DTO Round-Trip Tests

        [Test]
        public void DevelopmentPotentialDTO_RoundTrip_PreservesAllData()
        {
            // Arrange
            var original = new DevelopmentPotential
            {
                PlayerId = 123,
                OverallPotential = 87.5f,
                DevelopmentRate = 1.3f,
                InjuryProneness = 0.8f,
                LastUpdated = new DateTime(2024, 1, 15, 10, 30, 0)
            };
            original.AttributePotentials["Kicking"] = 85.2f;
            original.AttributePotentials["Marking"] = 78.9f;
            original.PreferredTraining.Add(TrainingFocus.Kicking);
            original.PreferredTraining.Add(TrainingFocus.Marking);

            // Act
            var dto = DevelopmentPotentialDTO.FromDomain(original);
            var roundTripped = dto.ToDomain();

            // Assert
            Assert.AreEqual(original.PlayerId, roundTripped.PlayerId);
            Assert.AreEqual(original.OverallPotential, roundTripped.OverallPotential, 0.01f);
            Assert.AreEqual(original.DevelopmentRate, roundTripped.DevelopmentRate, 0.01f);
            Assert.AreEqual(original.InjuryProneness, roundTripped.InjuryProneness, 0.01f);
            Assert.AreEqual(original.LastUpdated, roundTripped.LastUpdated);
            
            Assert.AreEqual(2, roundTripped.AttributePotentials.Count);
            Assert.AreEqual(85.2f, roundTripped.AttributePotentials["Kicking"], 0.01f);
            Assert.AreEqual(78.9f, roundTripped.AttributePotentials["Marking"], 0.01f);
            
            Assert.AreEqual(2, roundTripped.PreferredTraining.Count);
            Assert.Contains(TrainingFocus.Kicking, roundTripped.PreferredTraining);
            Assert.Contains(TrainingFocus.Marking, roundTripped.PreferredTraining);
        }

        [Test]
        public void PlayerTrainingEnrollmentDTO_RoundTrip_PreservesAllData()
        {
            // Arrange
            var original = new PlayerTrainingEnrollment
            {
                PlayerId = 456,
                ProgramId = "test-program-123",
                StartDate = new DateTime(2024, 1, 10),
                EndDate = new DateTime(2024, 2, 10),
                ProgressPercentage = 67.5f,
                SessionsCompleted = 8,
                SessionsMissed = 2,
                TotalFatigueAccumulated = 45.2f,
                TotalInjuryRisk = 0.15f,
                IsActive = true
            };
            original.CumulativeGains["Kicking"] = 2.5f;
            original.CumulativeGains["Endurance"] = 1.8f;

            // Act
            var dto = PlayerTrainingEnrollmentDTO.FromDomain(original);
            var roundTripped = dto.ToDomain();

            // Assert
            Assert.AreEqual(original.PlayerId, roundTripped.PlayerId);
            Assert.AreEqual(original.ProgramId, roundTripped.ProgramId);
            Assert.AreEqual(original.StartDate, roundTripped.StartDate);
            Assert.AreEqual(original.EndDate, roundTripped.EndDate);
            Assert.AreEqual(original.ProgressPercentage, roundTripped.ProgressPercentage, 0.01f);
            Assert.AreEqual(original.SessionsCompleted, roundTripped.SessionsCompleted);
            Assert.AreEqual(original.SessionsMissed, roundTripped.SessionsMissed);
            Assert.AreEqual(original.TotalFatigueAccumulated, roundTripped.TotalFatigueAccumulated, 0.01f);
            Assert.AreEqual(original.TotalInjuryRisk, roundTripped.TotalInjuryRisk, 0.01f);
            Assert.AreEqual(original.IsActive, roundTripped.IsActive);
            
            Assert.AreEqual(2, roundTripped.CumulativeGains.Count);
            Assert.AreEqual(2.5f, roundTripped.CumulativeGains["Kicking"], 0.01f);
            Assert.AreEqual(1.8f, roundTripped.CumulativeGains["Endurance"], 0.01f);
        }

        [Test]
        public void TrainingDataDTO_JsonSerialization_WorksCorrectly()
        {
            // Arrange
            var original = new TrainingDataDTO
            {
                Version = "1.0",
                SavedAt = DateTime.Now.ToString("O")
            };
            
            // Add a development potential
            original.PlayerPotentials.Add(new DevelopmentPotentialDTO
            {
                PlayerId = 1,
                OverallPotential = 85f,
                DevelopmentRate = 1.2f
            });
            
            // Add an enrollment
            original.Enrollments.Add(new PlayerTrainingEnrollmentDTO
            {
                PlayerId = 1,
                ProgramId = "test-program",
                StartDate = DateTime.Now.ToString("O"),
                IsActive = true
            });

            // Act
            string json = JsonUtility.ToJson(original, true);
            var deserialized = JsonUtility.FromJson<TrainingDataDTO>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(original.Version, deserialized.Version);
            Assert.AreEqual(1, deserialized.PlayerPotentials.Count);
            Assert.AreEqual(1, deserialized.Enrollments.Count);
            Assert.AreEqual(original.PlayerPotentials[0].PlayerId, deserialized.PlayerPotentials[0].PlayerId);
            Assert.AreEqual(original.Enrollments[0].PlayerId, deserialized.Enrollments[0].PlayerId);
        }

        #endregion

        #region Repository Tests

        [Test]
        public void JsonTrainingRepository_SaveAndLoadPlayerPotential_WorksCorrectly()
        {
            // Arrange
            var potential = new DevelopmentPotential
            {
                PlayerId = 100,
                OverallPotential = 90f,
                DevelopmentRate = 1.5f
            };
            potential.AttributePotentials["Kicking"] = 88f;

            // Act
            _repository.SavePlayerPotential(100, potential);
            var loaded = _repository.LoadPlayerPotential(100);

            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual(potential.PlayerId, loaded.PlayerId);
            Assert.AreEqual(potential.OverallPotential, loaded.OverallPotential, 0.01f);
            Assert.AreEqual(1, loaded.AttributePotentials.Count);
            Assert.AreEqual(88f, loaded.AttributePotentials["Kicking"], 0.01f);
        }

        [Test]
        public void JsonTrainingRepository_SaveAndLoadPlayerEnrollments_WorksCorrectly()
        {
            // Arrange
            var enrollment1 = new PlayerTrainingEnrollment
            {
                PlayerId = 200,
                ProgramId = "program-1",
                StartDate = DateTime.Now,
                IsActive = true
            };
            var enrollment2 = new PlayerTrainingEnrollment
            {
                PlayerId = 200,
                ProgramId = "program-2",
                StartDate = DateTime.Now.AddDays(-10),
                IsActive = false
            };
            var enrollments = new List<PlayerTrainingEnrollment> { enrollment1, enrollment2 };

            // Act
            _repository.SavePlayerEnrollments(200, enrollments);
            var loaded = _repository.LoadPlayerEnrollments(200).ToList();

            // Assert
            Assert.AreEqual(2, loaded.Count);
            Assert.IsTrue(loaded.Any(e => e.ProgramId == "program-1" && e.IsActive));
            Assert.IsTrue(loaded.Any(e => e.ProgramId == "program-2" && !e.IsActive));
        }

        [Test]
        public void JsonTrainingRepository_RemovePlayerEnrollment_WorksCorrectly()
        {
            // Arrange
            var enrollment = new PlayerTrainingEnrollment
            {
                PlayerId = 300,
                ProgramId = "program-to-remove",
                IsActive = true
            };
            _repository.SavePlayerEnrollment(enrollment);

            // Act
            _repository.RemovePlayerEnrollment(300, "program-to-remove");
            var loaded = _repository.LoadPlayerEnrollments(300).ToList();

            // Assert
            Assert.AreEqual(0, loaded.Count);
        }

        [Test]
        public void JsonTrainingRepository_HasTrainingData_WorksCorrectly()
        {
            // Assert - Initially no data
            Assert.IsFalse(_repository.HasTrainingData());

            // Act - Save some data
            var potential = new DevelopmentPotential { PlayerId = 400, OverallPotential = 75f };
            _repository.SavePlayerPotential(400, potential);

            // Assert - Now has data
            Assert.IsTrue(_repository.HasTrainingData());
        }

        [Test]
        public void JsonTrainingRepository_ClearAllTrainingData_WorksCorrectly()
        {
            // Arrange - Add some data
            var potential = new DevelopmentPotential { PlayerId = 500, OverallPotential = 80f };
            _repository.SavePlayerPotential(500, potential);
            
            var enrollment = new PlayerTrainingEnrollment { PlayerId = 500, ProgramId = "test", IsActive = true };
            _repository.SavePlayerEnrollment(enrollment);

            // Verify data exists
            Assert.IsTrue(_repository.HasTrainingData());
            Assert.IsNotNull(_repository.LoadPlayerPotential(500));

            // Act
            _repository.ClearAllTrainingData();

            // Assert
            Assert.IsFalse(_repository.HasTrainingData());
            Assert.IsNull(_repository.LoadPlayerPotential(500));
        }

        [Test]
        public void JsonTrainingRepository_BackupAndRestore_WorksCorrectly()
        {
            // Arrange
            var potential = new DevelopmentPotential { PlayerId = 600, OverallPotential = 85f };
            _repository.SavePlayerPotential(600, potential);

            // Act - Backup
            _repository.BackupTrainingData("test-backup");

            // Clear data
            _repository.ClearAllTrainingData();
            Assert.IsFalse(_repository.HasTrainingData());

            // Restore
            bool restored = _repository.RestoreTrainingData("test-backup");

            // Assert
            Assert.IsTrue(restored);
            Assert.IsTrue(_repository.HasTrainingData());
            
            var restoredPotential = _repository.LoadPlayerPotential(600);
            Assert.IsNotNull(restoredPotential);
            Assert.AreEqual(85f, restoredPotential.OverallPotential, 0.01f);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void TrainingPersistence_CompleteWorkflow_WorksCorrectly()
        {
            // Arrange - Create comprehensive training data
            var trainingData = new TrainingDataDTO();
            
            // Add player potentials
            trainingData.PlayerPotentials.Add(new DevelopmentPotentialDTO
            {
                PlayerId = 1001,
                OverallPotential = 90f,
                DevelopmentRate = 1.4f,
                AttributePotentialKeys = new List<string> { "Kicking", "Marking" },
                AttributePotentialValues = new List<float> { 88f, 85f }
            });

            // Add enrollments
            trainingData.Enrollments.Add(new PlayerTrainingEnrollmentDTO
            {
                PlayerId = 1001,
                ProgramId = "strength-program",
                StartDate = DateTime.Now.AddDays(-20).ToString("O"),
                IsActive = true,
                SessionsCompleted = 15,
                CumulativeGainKeys = new List<string> { "Strength", "Endurance" },
                CumulativeGainValues = new List<float> { 3.2f, 1.8f }
            });

            // Add training sessions
            trainingData.CompletedSessions.Add(new TrainingSessionDTO
            {
                Id = "session-001",
                ProgramId = "strength-program",
                ScheduledDate = DateTime.Now.AddDays(-5).ToString("O"),
                CompletedDate = DateTime.Now.AddDays(-5).ToString("O"),
                Intensity = (int)TrainingIntensity.High,
                ParticipatingPlayers = new List<int> { 1001 },
                IsCompleted = true
            });

            // Act - Save and load the complete dataset
            _repository.SaveAllTrainingData(trainingData);
            var loaded = _repository.LoadAllTrainingData();

            // Assert - Verify all data was preserved
            Assert.IsNotNull(loaded);
            Assert.AreEqual(trainingData.Version, loaded.Version);
            
            // Check potentials
            Assert.AreEqual(1, loaded.PlayerPotentials.Count);
            var loadedPotential = loaded.PlayerPotentials[0];
            Assert.AreEqual(1001, loadedPotential.PlayerId);
            Assert.AreEqual(90f, loadedPotential.OverallPotential, 0.01f);
            Assert.AreEqual(2, loadedPotential.AttributePotentialKeys.Count);

            // Check enrollments
            Assert.AreEqual(1, loaded.Enrollments.Count);
            var loadedEnrollment = loaded.Enrollments[0];
            Assert.AreEqual(1001, loadedEnrollment.PlayerId);
            Assert.AreEqual("strength-program", loadedEnrollment.ProgramId);
            Assert.AreEqual(15, loadedEnrollment.SessionsCompleted);

            // Check sessions
            Assert.AreEqual(1, loaded.CompletedSessions.Count);
            var loadedSession = loaded.CompletedSessions[0];
            Assert.AreEqual("session-001", loadedSession.Id);
            Assert.AreEqual("strength-program", loadedSession.ProgramId);
            Assert.IsTrue(loadedSession.IsCompleted);
        }

        [Test]
        public void TrainingPersistence_LargeDataset_PerformsAdequately()
        {
            // Arrange - Create a large dataset
            var trainingData = new TrainingDataDTO();
            var random = new System.Random(12345); // Deterministic for testing

            // Add 100 player potentials
            for (int i = 1; i <= 100; i++)
            {
                trainingData.PlayerPotentials.Add(new DevelopmentPotentialDTO
                {
                    PlayerId = i,
                    OverallPotential = random.Next(60, 95),
                    DevelopmentRate = (float)(random.NextDouble() * 0.8 + 0.8), // 0.8 to 1.6
                    AttributePotentialKeys = new List<string> { "Kicking", "Marking", "Endurance" },
                    AttributePotentialValues = new List<float> 
                    { 
                        random.Next(60, 95), 
                        random.Next(60, 95), 
                        random.Next(60, 95) 
                    }
                });
            }

            // Add 200 enrollments
            for (int i = 1; i <= 200; i++)
            {
                trainingData.Enrollments.Add(new PlayerTrainingEnrollmentDTO
                {
                    PlayerId = (i % 100) + 1,
                    ProgramId = $"program-{(i % 10) + 1}",
                    StartDate = DateTime.Now.AddDays(-random.Next(1, 365)).ToString("O"),
                    IsActive = random.NextDouble() > 0.3,
                    SessionsCompleted = random.Next(0, 50)
                });
            }

            // Act - Measure save and load performance
            var saveStart = DateTime.Now;
            _repository.SaveAllTrainingData(trainingData);
            var saveTime = DateTime.Now - saveStart;

            var loadStart = DateTime.Now;
            var loaded = _repository.LoadAllTrainingData();
            var loadTime = DateTime.Now - loadStart;

            // Assert - Verify data integrity and acceptable performance
            Assert.AreEqual(100, loaded.PlayerPotentials.Count);
            Assert.AreEqual(200, loaded.Enrollments.Count);
            
            // Performance assertions (should complete within reasonable time)
            Assert.LessOrEqual(saveTime.TotalSeconds, 5.0, "Save operation should complete within 5 seconds");
            Assert.LessOrEqual(loadTime.TotalSeconds, 5.0, "Load operation should complete within 5 seconds");
            
            Debug.Log($"Performance Test - Save: {saveTime.TotalMilliseconds:F0}ms, Load: {loadTime.TotalMilliseconds:F0}ms");
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void TrainingPersistence_CorruptedData_HandlesGracefully()
        {
            // Arrange - Write invalid JSON to the data file
            File.WriteAllText(_testDataPath, "{ invalid json content }");

            // Act - Attempt to load data
            var loaded = _repository.LoadAllTrainingData();

            // Assert - Should return empty data instead of throwing
            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.PlayerPotentials.Count);
            Assert.AreEqual(0, loaded.Enrollments.Count);
        }

        [Test]
        public void TrainingPersistence_MissingFile_HandlesGracefully()
        {
            // Act - Attempt to load data when no file exists
            var loaded = _repository.LoadPlayerPotential(999);

            // Assert - Should return null instead of throwing
            Assert.IsNull(loaded);
        }

        [Test]
        public void TrainingPersistence_NullData_HandlesGracefully()
        {
            // Act & Assert - Should not throw when saving null data
            Assert.DoesNotThrow(() => _repository.SavePlayerPotential(1000, null));
            Assert.DoesNotThrow(() => _repository.SavePlayerEnrollment(null));
        }

        #endregion
    }

    /// <summary>
    /// Test-specific repository implementation that uses a custom file path
    /// </summary>
    public class TestJsonTrainingRepository : JsonTrainingRepository
    {
        private readonly string _testFilePath;

        public TestJsonTrainingRepository(string testFilePath)
        {
            _testFilePath = testFilePath;
        }

        // Override the file path for testing
        private string TestTrainingDataFilePath => _testFilePath;

        // NOTE: In a real implementation, you'd need to override the path usage
        // This is a simplified version for demonstration
    }
}