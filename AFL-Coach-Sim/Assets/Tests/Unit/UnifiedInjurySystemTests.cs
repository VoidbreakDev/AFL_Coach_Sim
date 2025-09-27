using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace AFLCoachSim.Tests.Injuries
{
    /// <summary>
    /// Comprehensive test suite for the unified injury management system
    /// </summary>
    [TestFixture]
    public class UnifiedInjurySystemTests
    {
        private InjuryManager _injuryManager;
        private MockInjuryRepository _mockRepository;
        
        [SetUp]
        public void Setup()
        {
            _mockRepository = new MockInjuryRepository();
            _injuryManager = new InjuryManager(_mockRepository);
        }
        
        [TearDown]
        public void TearDown()
        {
            _mockRepository = null;
            _injuryManager = null;
        }
        
        #region Injury Domain Model Tests
        
        [Test]
        public void Injury_Creation_SetsCorrectProperties()
        {
            // Arrange
            int playerId = 1;
            var injuryType = InjuryType.Muscle;
            var severity = InjurySeverity.Minor;
            var source = InjurySource.Training;
            string description = "Hamstring strain";
            
            // Act
            var injury = new Injury(playerId, injuryType, severity, source, description);
            
            // Assert
            Assert.That(injury.PlayerId, Is.EqualTo(playerId));
            Assert.That(injury.Type, Is.EqualTo(injuryType));
            Assert.That(injury.Severity, Is.EqualTo(severity));
            Assert.That(injury.Source, Is.EqualTo(source));
            Assert.That(injury.Description, Is.EqualTo(description));
            Assert.That(injury.Status, Is.EqualTo(InjuryStatus.Active));
            Assert.That(injury.ExpectedRecoveryDays, Is.GreaterThan(0));
            Assert.That(injury.PerformanceImpactMultiplier, Is.LessThan(1.0f));
        }
        
        [Test]
        public void Injury_RecoveryProgress_UpdatesCorrectly()
        {
            // Arrange
            var injury = new Injury(1, InjuryType.Joint, InjurySeverity.Minor, InjurySource.Match);
            
            // Set expected recovery to a small value for testing
            typeof(Injury).GetProperty("ExpectedRecoveryDays")?.SetValue(injury, 5);
            typeof(Injury).GetProperty("OccurredDate")?.SetValue(injury, DateTime.Now.AddDays(-6));
            
            // Act
            bool updated = injury.UpdateRecoveryProgress();
            
            // Assert
            Assert.That(updated, Is.True);
            Assert.That(injury.Status, Is.EqualTo(InjuryStatus.Recovered));
        }
        
        [Test]
        public void InjuryRiskProfile_CalculatesCorrectRisk()
        {
            // Arrange
            int playerAge = 25;
            int durability = 80;
            float fatigue = 60f;
            float recurrenceRisk = 0.2f;
            
            // Act
            var riskProfile = new InjuryRiskProfile(playerAge, durability, fatigue, recurrenceRisk);
            
            // Assert
            Assert.That(riskProfile.BaseInjuryRisk, Is.EqualTo(0.02f));
            Assert.That(riskProfile.AgeMultiplier, Is.EqualTo(1.0f)); // Peak age
            Assert.That(riskProfile.DurabilityMultiplier, Is.EqualTo(1.2f)); // 80 durability
            Assert.That(riskProfile.OverallRiskMultiplier, Is.GreaterThan(1.0f));
            
            // Test injury probability calculation
            float probability = riskProfile.CalculateInjuryProbability(90f, 1.0f);
            Assert.That(probability, Is.GreaterThan(0f).And.LessThan(1f));
        }
        
        [Test]
        public void PlayerInjuryHistory_RecordsInjuries()
        {
            // Arrange
            var history = new PlayerInjuryHistory(1);
            
            // Act
            var injury1 = history.RecordInjury(InjuryType.Muscle, InjurySeverity.Minor, InjurySource.Training);
            var injury2 = history.RecordInjury(InjuryType.Joint, InjurySeverity.Moderate, InjurySource.Match);
            
            // Assert
            Assert.That(history.AllInjuries.Count, Is.EqualTo(2));
            Assert.That(history.ActiveInjuries.Count, Is.EqualTo(2));
            Assert.That(history.IsCurrentlyInjured, Is.True);
            Assert.That(history.CanTrainToday, Is.False);
            Assert.That(history.CanPlayMatches, Is.False);
        }
        
        [Test]
        public void PlayerInjuryHistory_DetectsRecurringInjuries()
        {
            // Arrange
            var history = new PlayerInjuryHistory(1);
            
            // Record initial muscle injury
            var injury1 = history.RecordInjury(InjuryType.Muscle, InjurySeverity.Minor, InjurySource.Training);
            
            // Act - Record another muscle injury (should be flagged as recurring)
            var injury2 = history.RecordInjury(InjuryType.Muscle, InjurySeverity.Minor, InjurySource.Match);
            
            // Assert
            Assert.That(injury2.IsRecurring, Is.True);
            Assert.That(injury2.OriginalInjuryId, Is.EqualTo(injury1.Id));
        }
        
        #endregion
        
        #region Injury Manager Tests
        
        [Test]
        public void InjuryManager_RecordTrainingInjury_CreatesAndPersistsInjury()
        {
            // Arrange
            int playerId = 1;
            float injuryRisk = 0.5f;
            int playerAge = 27;
            int durability = 75;
            
            // Act
            var injury = _injuryManager.RecordTrainingInjury(playerId, injuryRisk, playerAge, durability);
            
            // Assert
            Assert.That(injury, Is.Not.Null);
            Assert.That(injury.PlayerId, Is.EqualTo(playerId));
            Assert.That(injury.Source, Is.EqualTo(InjurySource.Training));
            Assert.That(injury.Type, Is.OneOf(InjuryType.Muscle, InjuryType.Joint, InjuryType.Tendon, InjuryType.Other));
            Assert.That(_mockRepository.SavedHistories.ContainsKey(playerId), Is.True);
        }
        
        [Test]
        public void InjuryManager_RecordMatchInjury_CreatesCorrectInjury()
        {
            // Arrange
            int playerId = 2;
            var matchSeverity = InjurySeverity.Moderate;
            float gameContext = 1.3f; // High intensity context
            
            // Act
            var injury = _injuryManager.RecordMatchInjury(playerId, matchSeverity, gameContext);
            
            // Assert
            Assert.That(injury, Is.Not.Null);
            Assert.That(injury.PlayerId, Is.EqualTo(playerId));
            Assert.That(injury.Source, Is.EqualTo(InjurySource.Match));
            Assert.That(injury.Severity, Is.EqualTo(matchSeverity));
        }
        
        [Test]
        public void InjuryManager_CalculateInjuryRisk_ReturnsValidRisk()
        {
            // Arrange
            int playerId = 3;
            int playerAge = 32; // Older player
            int durability = 60; // Lower durability
            float currentFatigue = 80f; // High fatigue
            
            // Act
            var riskProfile = _injuryManager.CalculateInjuryRisk(playerId, playerAge, durability, currentFatigue);
            
            // Assert
            Assert.That(riskProfile, Is.Not.Null);
            Assert.That(riskProfile.OverallRiskMultiplier, Is.GreaterThan(1.0f)); // Should be elevated
            Assert.That(riskProfile.AgeMultiplier, Is.GreaterThan(1.0f)); // Older player penalty
            Assert.That(riskProfile.FatigueMultiplier, Is.GreaterThan(1.0f)); // Fatigue penalty
        }
        
        [Test]
        public void InjuryManager_ProcessDailyRecovery_UpdatesInjuries()
        {
            // Arrange
            int playerId = 4;
            var injury = _injuryManager.RecordTrainingInjury(playerId, 0.3f, 25, 80);
            
            // Simulate injury recovery by setting occurred date in the past
            typeof(Injury).GetProperty("OccurredDate")?.SetValue(injury, DateTime.Now.AddDays(-10));
            typeof(Injury).GetProperty("ExpectedRecoveryDays")?.SetValue(injury, 7);
            
            // Act
            _injuryManager.ProcessDailyRecovery();
            
            // Assert - Should have updated recovery status
            Assert.That(_mockRepository.SavedHistories.ContainsKey(playerId), Is.True);
        }
        
        [Test]
        public void InjuryManager_GetTeamInjurySummary_ReturnsCorrectData()
        {
            // Arrange - Create some injuries
            _injuryManager.RecordTrainingInjury(1, 0.3f, 25, 80);
            _injuryManager.RecordMatchInjury(2, InjurySeverity.Minor, 1.2f);
            _injuryManager.RecordTrainingInjury(3, 0.4f, 30, 70);
            
            // Act
            var summary = _injuryManager.GetTeamInjurySummary();
            
            // Assert
            Assert.That(summary.PlayersTracked, Is.EqualTo(3));
            Assert.That(summary.CurrentlyInjured, Is.EqualTo(3));
            Assert.That(summary.TotalInjuries, Is.EqualTo(3));
            Assert.That(summary.InjuryRate, Is.EqualTo(1.0f)); // All players injured
            Assert.That(summary.InjuriesBySource[InjurySource.Training], Is.EqualTo(2));
            Assert.That(summary.InjuriesBySource[InjurySource.Match], Is.EqualTo(1));
        }
        
        #endregion
        
        #region Integration Tests
        
        [Test]
        public void Integration_TrainingInjury_AffectsPlayerAvailability()
        {
            // Arrange
            int playerId = 10;
            
            // Act - Record a significant injury
            var injury = _injuryManager.RecordInjury(playerId, InjuryType.Ligament, InjurySeverity.Major, InjurySource.Training);
            
            // Assert - Player should be unavailable
            Assert.That(_injuryManager.IsPlayerInjured(playerId), Is.True);
            Assert.That(_injuryManager.CanPlayerTrain(playerId), Is.False);
            Assert.That(_injuryManager.CanPlayerPlay(playerId), Is.False);
            Assert.That(_injuryManager.GetPlayerPerformanceMultiplier(playerId), Is.LessThan(1.0f));
            Assert.That(_injuryManager.GetDaysUntilMatchReady(playerId), Is.GreaterThan(0));
        }
        
        [Test]
        public void Integration_InjuryRecovery_RestoresAvailability()
        {
            // Arrange
            int playerId = 11;
            var injury = _injuryManager.RecordInjury(playerId, InjuryType.Muscle, InjurySeverity.Minor, InjurySource.Training);
            
            // Act - Force recovery
            bool recovered = _injuryManager.ForceRecovery(playerId, injury.Id);
            
            // Assert
            Assert.That(recovered, Is.True);
            Assert.That(_injuryManager.IsPlayerInjured(playerId), Is.False);
            Assert.That(_injuryManager.CanPlayerTrain(playerId), Is.True);
            Assert.That(_injuryManager.CanPlayerPlay(playerId), Is.True);
            Assert.That(_injuryManager.GetPlayerPerformanceMultiplier(playerId), Is.EqualTo(1.0f));
        }
        
        [Test]
        public void Integration_MultipleInjuries_CalculatesWorstImpact()
        {
            // Arrange
            int playerId = 12;
            
            // Act - Record multiple injuries of different severities
            var injury1 = _injuryManager.RecordInjury(playerId, InjuryType.Muscle, InjurySeverity.Niggle, InjurySource.Training);
            var injury2 = _injuryManager.RecordInjury(playerId, InjuryType.Joint, InjurySeverity.Minor, InjurySource.Match);
            
            // Assert - Performance should be affected by the most severe injury
            float multiplier = _injuryManager.GetPlayerPerformanceMultiplier(playerId);
            Assert.That(multiplier, Is.LessThan(0.95f)); // Should be worse than just a niggle
            
            var mostSevere = _injuryManager.GetMostSevereInjury(playerId);
            Assert.That(mostSevere.Severity, Is.EqualTo(InjurySeverity.Minor));
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void Performance_LargeNumberOfInjuries_HandledEfficiently()
        {
            // Arrange
            var startTime = DateTime.Now;
            int numPlayers = 100;
            int numInjuries = 500;
            
            // Act - Create many injuries
            for (int i = 0; i < numInjuries; i++)
            {
                int playerId = (i % numPlayers) + 1;
                var injuryType = (InjuryType)(i % 6);
                var severity = (InjurySeverity)(i % 5);
                var source = (InjurySource)(i % 3);
                
                _injuryManager.RecordInjury(playerId, injuryType, severity, source);
            }
            
            // Test queries
            var allActive = _injuryManager.GetAllActiveInjuries();
            var teamSummary = _injuryManager.GetTeamInjurySummary();
            var injuredPlayers = _injuryManager.GetInjuredPlayerIds();
            
            var elapsed = DateTime.Now - startTime;
            
            // Assert - Should handle large data efficiently
            Assert.That(elapsed.TotalSeconds, Is.LessThan(5.0), "Large injury dataset should be processed quickly");
            Assert.That(allActive.Count(), Is.EqualTo(numInjuries));
            Assert.That(teamSummary.TotalInjuries, Is.EqualTo(numInjuries));
            Assert.That(injuredPlayers.Count(), Is.EqualTo(numPlayers));
            
            Debug.Log($"[UnifiedInjurySystemTests] Processed {numInjuries} injuries for {numPlayers} players in {elapsed.TotalMilliseconds}ms");
        }
        
        #endregion
        
        #region Error Handling Tests
        
        [Test]
        public void ErrorHandling_InvalidPlayerId_HandlesGracefully()
        {
            // Arrange
            int invalidPlayerId = -1;
            
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() => {
                bool isInjured = _injuryManager.IsPlayerInjured(invalidPlayerId);
                bool canTrain = _injuryManager.CanPlayerTrain(invalidPlayerId);
                bool canPlay = _injuryManager.CanPlayerPlay(invalidPlayerId);
                float multiplier = _injuryManager.GetPlayerPerformanceMultiplier(invalidPlayerId);
            });
        }
        
        [Test]
        public void ErrorHandling_NullRepository_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new InjuryManager(null));
        }
        
        [Test]
        public void ErrorHandling_InvalidInjuryId_ReturnsFalse()
        {
            // Arrange
            int playerId = 15;
            var invalidInjuryId = InjuryId.From(999999);
            
            // Act
            bool recovered = _injuryManager.ForceRecovery(playerId, invalidInjuryId);
            
            // Assert
            Assert.That(recovered, Is.False);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Mock injury repository for testing
    /// </summary>
    public class MockInjuryRepository : IInjuryRepository
    {
        public Dictionary<int, PlayerInjuryHistory> SavedHistories = new Dictionary<int, PlayerInjuryHistory>();
        public Dictionary<InjuryId, Injury> SavedInjuries = new Dictionary<InjuryId, Injury>();
        
        public void SavePlayerInjuryHistory(PlayerInjuryHistory history)
        {
            SavedHistories[history.PlayerId] = history;
        }
        
        public PlayerInjuryHistory LoadPlayerInjuryHistory(int playerId)
        {
            return SavedHistories.GetValueOrDefault(playerId, new PlayerInjuryHistory(playerId));
        }
        
        public IDictionary<int, PlayerInjuryHistory> LoadAllPlayerInjuryHistories()
        {
            return SavedHistories;
        }
        
        public void RemovePlayerInjuryHistory(int playerId)
        {
            SavedHistories.Remove(playerId);
        }
        
        public void SaveInjury(Injury injury)
        {
            SavedInjuries[injury.Id] = injury;
        }
        
        public Injury LoadInjury(InjuryId injuryId)
        {
            return SavedInjuries.GetValueOrDefault(injuryId);
        }
        
        public IEnumerable<Injury> LoadPlayerInjuries(int playerId)
        {
            var history = LoadPlayerInjuryHistory(playerId);
            return history.AllInjuries;
        }
        
        public IEnumerable<Injury> LoadActiveInjuries()
        {
            var allActive = new List<Injury>();
            foreach (var history in SavedHistories.Values)
            {
                allActive.AddRange(history.ActiveInjuries);
            }
            return allActive;
        }
        
        public void SaveAllInjuryData(AFLCoachSim.Core.DTO.InjuryDataDTO injuryData)
        {
            // Mock implementation
        }
        
        public AFLCoachSim.Core.DTO.InjuryDataDTO LoadAllInjuryData()
        {
            return new AFLCoachSim.Core.DTO.InjuryDataDTO();
        }
        
        public void ClearAllInjuryData()
        {
            SavedHistories.Clear();
            SavedInjuries.Clear();
        }
        
        public void ClearPlayerInjuryData(int playerId)
        {
            SavedHistories.Remove(playerId);
        }
        
        public bool HasInjuryData()
        {
            return SavedHistories.Count > 0 || SavedInjuries.Count > 0;
        }
        
        public void BackupInjuryData(string backupSuffix)
        {
            // Mock implementation
        }
        
        public bool RestoreInjuryData(string backupSuffix)
        {
            return true; // Mock success
        }
    }
}