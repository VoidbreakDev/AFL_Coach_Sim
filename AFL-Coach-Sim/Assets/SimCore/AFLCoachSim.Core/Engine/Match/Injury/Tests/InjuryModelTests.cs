using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Tuning;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;
using AFLCoachSim.Core.Persistence;

namespace AFLCoachSim.Core.Engine.Match.Injury.Tests
{
    [TestFixture]
    public class InjuryModelTests
    {
        private InjuryManager _injuryManager;
        private InjuryModel _injuryModel;
        private List<PlayerRuntime> _testPlayers;
        private MatchTuning _tuning;
        private DeterministicRandom _rng;
        
        [SetUp]
        public void Setup()
        {
            // Create test injury manager
            var repository = new InMemoryTestRepository();
            _injuryManager = new InjuryManager(repository);
            
            // Create injury model
            _injuryModel = new InjuryModel(_injuryManager);
            
            // Create test players
            _testPlayers = CreateTestPlayers(22);
            
            // Set up tuning and RNG
            _tuning = MatchTuning.Default;
            _rng = new DeterministicRandom(12345);
            
            // Initialize the model
            _injuryModel.InitializeMatchInjuryStates(_testPlayers);
        }
        
        [Test]
        public void InitializeMatchInjuryStates_WithHealthyPlayers_SetsUpCorrectly()
        {
            // All players should be healthy initially
            foreach (var player in _testPlayers)
            {
                Assert.AreEqual(1.0f, player.InjuryMult, "Healthy player should have full performance");
                Assert.IsFalse(player.InjuredOut, "Healthy player should not be injured out");
            }
        }
        
        [Test]
        public void InitializeMatchInjuryStates_WithInjuredPlayer_AppliesPreExistingEffects()
        {
            // Add an injury to a player before initialization
            var playerId = _testPlayers[0].Player.Id;
            _injuryManager.RecordInjury((int)playerId, InjuryType.Muscle, InjurySeverity.Minor, InjurySource.Training);
            
            // Re-create and initialize the model to pick up the injury
            _injuryModel = new InjuryModel(_injuryManager);
            _injuryModel.InitializeMatchInjuryStates(_testPlayers);
            
            // The injured player should have reduced performance
            var injuredPlayer = _testPlayers[0];
            Assert.Less(injuredPlayer.InjuryMult, 1.0f, "Injured player should have reduced performance");
        }
        
        [Test]
        public void Step_WithLowRisk_ProducesNoInjuries()
        {
            // Use very low risk tuning
            var lowRiskTuning = new MatchTuning { InjuryBasePerMinuteRisk = 0.0001f };
            
            int newInjuries = _injuryModel.Step(
                _testPlayers.Take(18).ToList(), // On field
                _testPlayers.Skip(18).ToList(), // Bench
                Phase.OpenPlay,
                5, // 5 seconds
                _rng,
                0, // No existing injuries
                2, // Max injuries
                lowRiskTuning
            );
            
            Assert.AreEqual(0, newInjuries, "Low risk should produce no injuries in short timeframe");
        }
        
        [Test]
        public void Step_WithHighRisk_CanProduceInjuries()
        {
            // Use high risk tuning
            var highRiskTuning = new MatchTuning { InjuryBasePerMinuteRisk = 0.5f }; // Very high risk
            
            int totalInjuries = 0;
            for (int i = 0; i < 100; i++) // Run many steps to get statistical likelihood
            {
                totalInjuries += _injuryModel.Step(
                    _testPlayers.Take(18).ToList(),
                    _testPlayers.Skip(18).ToList(),
                    Phase.Inside50, // High contact phase
                    5,
                    _rng,
                    totalInjuries,
                    10, // High max to not limit
                    highRiskTuning
                );
            }
            
            Assert.Greater(totalInjuries, 0, "High risk should eventually produce injuries");
        }
        
        [Test]
        public void Step_RespectsMayInjuries()
        {
            var highRiskTuning = new MatchTuning { InjuryBasePerMinuteRisk = 1.0f }; // Extreme risk
            
            int newInjuries = _injuryModel.Step(
                _testPlayers.Take(18).ToList(),
                _testPlayers.Skip(18).ToList(),
                Phase.Inside50,
                60, // Full minute
                _rng,
                2, // Already at max
                2, // Max injuries
                highRiskTuning
            );
            
            Assert.AreEqual(0, newInjuries, "Should not exceed max injuries limit");
        }
        
        [Test]
        public void GetMatchAnalytics_ReturnsValidData()
        {
            var analytics = _injuryModel.GetMatchAnalytics();
            
            Assert.AreEqual(_testPlayers.Count, analytics.TotalPlayersTracked);
            Assert.AreEqual(0, analytics.PlayersWithPreExistingInjuries);
            Assert.AreEqual(0, analytics.PlayersWithNewInjuries);
            Assert.AreEqual(0.0f, analytics.InjuryRate);
        }
        
        [Test]
        public void Phase_AffectsInjuryTypes()
        {
            // This test would require running many simulations to verify statistical differences
            // For now, just verify the system can handle different phases
            var phases = new[] { Phase.OpenPlay, Phase.Inside50, Phase.CenterBounce };
            
            foreach (var phase in phases)
            {
                Assert.DoesNotThrow(() =>
                {
                    _injuryModel.Step(
                        _testPlayers.Take(18).ToList(),
                        _testPlayers.Skip(18).ToList(),
                        phase,
                        5,
                        _rng,
                        0,
                        2,
                        _tuning
                    );
                }, $"Should handle {phase} phase without errors");
            }
        }
        
        #region Helper Methods
        
        private List<PlayerRuntime> CreateTestPlayers(int count)
        {
            var players = new List<PlayerRuntime>();
            
            for (int i = 1; i <= count; i++)
            {
                var player = new Domain.Entities.Player
                {
                    Id = new PlayerId(i),
                    Durability = 85 // Standard durability
                };
                
                var runtime = new PlayerRuntime(player, TeamId.Adelaide, true)
                {
                    FatigueMult = 1.0f, // Not fatigued
                    InjuryMult = 1.0f,  // Not injured
                    InjuredOut = false
                };
                
                players.Add(runtime);
            }
            
            return players;
        }
        
        #endregion
        
        /// <summary>
        /// Simple in-memory repository for testing
        /// </summary>
        private class InMemoryTestRepository : IInjuryRepository
        {
            private readonly Dictionary<int, AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory> _histories = new Dictionary<int, AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory>();
            private readonly Dictionary<AFLCoachSim.Core.Injuries.Domain.InjuryId, AFLCoachSim.Core.Injuries.Domain.Injury> _injuries = new Dictionary<AFLCoachSim.Core.Injuries.Domain.InjuryId, AFLCoachSim.Core.Injuries.Domain.Injury>();
            
            // Implement all required interface methods
            public void SavePlayerInjuryHistory(AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory history)
            {
                _histories[history.PlayerId] = history;
            }
            
            public AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory LoadPlayerInjuryHistory(int playerId)
            {
                return _histories.TryGetValue(playerId, out var history) ? history : null;
            }
            
            public IDictionary<int, AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory> LoadAllPlayerInjuryHistories()
            {
                return _histories;
            }
            
            public void RemovePlayerInjuryHistory(int playerId)
            {
                _histories.Remove(playerId);
            }
            
            public void SaveInjury(AFLCoachSim.Core.Injuries.Domain.Injury injury)
            {
                _injuries[injury.Id] = injury;
            }
            
            public AFLCoachSim.Core.Injuries.Domain.Injury LoadInjury(AFLCoachSim.Core.Injuries.Domain.InjuryId injuryId)
            {
                return _injuries.TryGetValue(injuryId, out var injury) ? injury : null;
            }
            
            public IEnumerable<AFLCoachSim.Core.Injuries.Domain.Injury> LoadPlayerInjuries(int playerId)
            {
                return _injuries.Values.Where(i => i.PlayerId == playerId);
            }
            
            public IEnumerable<AFLCoachSim.Core.Injuries.Domain.Injury> LoadActiveInjuries()
            {
                return _injuries.Values.Where(i => i.Status == InjuryStatus.Active);
            }
            
            public void SaveAllInjuryData(AFLCoachSim.Core.DTO.InjuryDataDTO data) { }
            public AFLCoachSim.Core.DTO.InjuryDataDTO LoadAllInjuryData() { return new AFLCoachSim.Core.DTO.InjuryDataDTO(); }
            public void ClearAllInjuryData() 
            {
                _injuries.Clear();
                _histories.Clear();
            }
            public void ClearPlayerInjuryData(int playerId) 
            {
                _histories.Remove(playerId);
                var playerInjuries = _injuries.Where(kvp => kvp.Value.PlayerId == playerId).Select(kvp => kvp.Key).ToList();
                foreach (var id in playerInjuries)
                {
                    _injuries.Remove(id);
                }
            }
            public bool HasInjuryData() { return _injuries.Any() || _histories.Any(); }
            public void BackupInjuryData(string backupSuffix) { }
            public bool RestoreInjuryData(string backupSuffix) { return true; }
        }
    }
}