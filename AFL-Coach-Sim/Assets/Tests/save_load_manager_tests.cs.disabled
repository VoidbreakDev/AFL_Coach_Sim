// File: Assets/Tests/SaveLoadManagerTests.cs
using System.IO;
using NUnit.Framework;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Managers;  // ← must match the above namespace

namespace AFLManager.Tests
{
    public class SaveLoadManagerTests
    {
        private string playerId;
        private string teamId;

        [SetUp]
        public void Setup()
        {
            var player = new Player {
                Name = "Test Player",
                Age = 30,
                State = "NSW",
                History = "Test Club",
                Role = PlayerRole.Wing,
                PotentialCeiling = 90,
                Morale = 1f,
                Stamina = 1f
            };
            player.Stats.Kicking = 50;
            player.Contract = new ContractDetails { Salary = 100000f, YearsRemaining = 3 };
            SaveLoadManager.SavePlayer(player);
            playerId = player.Id;

            var team = new Team {
                Name = "Test Team",
                Level = LeagueLevel.Local,
                Budget = 50000f,
                SalaryCap = 60000f
            };
            SaveLoadManager.SaveTeam(team);
            teamId = team.Id;
        }

        [TearDown]
        public void Teardown()
        {
            var dataFolder = Application.persistentDataPath;
            var playerPath = Path.Combine(dataFolder, $"player_{playerId}.json");
            if (File.Exists(playerPath)) File.Delete(playerPath);

            var teamPath = Path.Combine(dataFolder, $"team_{teamId}.json");
            if (File.Exists(teamPath)) File.Delete(teamPath);
        }

        [Test]
        public void SaveAndLoadPlayer_MatchesOriginal()
        {
            var loaded = SaveLoadManager.LoadPlayer(playerId);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Test Player", loaded.Name);
            Assert.AreEqual(30, loaded.Age);
            Assert.AreEqual("NSW", loaded.State);
            Assert.AreEqual(50, loaded.Stats.Kicking);
            Assert.AreEqual(100000f, loaded.Contract.Salary);
        }

        [Test]
        public void SaveAndLoadTeam_MatchesOriginal()
        {
            var loaded = SaveLoadManager.LoadTeam(teamId);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Test Team", loaded.Name);
            Assert.AreEqual(LeagueLevel.Local, loaded.Level);
            Assert.AreEqual(50000f, loaded.Budget);
            Assert.AreEqual(60000f, loaded.SalaryCap);
        }
    }
}
