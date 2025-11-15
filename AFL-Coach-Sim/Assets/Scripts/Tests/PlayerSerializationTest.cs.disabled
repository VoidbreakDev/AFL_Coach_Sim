using System.IO;
using NUnit.Framework;
using UnityEngine;
using AFLManager.Models;   // ← Make sure this is present

namespace AFLManager.Tests
{
    public class PlayerSerializationTest
    {
        private string testFilePath;

        [SetUp]
        public void SetUp()
        {
            // Prepare a path in the persistent data folder
            testFilePath = Path.Combine(Application.persistentDataPath, "player_test.json");
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after tests
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }

        [Test]
        public void RoundTripJson_MaintainsAllFields()
        {
            var original = new Player
            {
                Name = "Unit Tester",
                Age = 25,
                State = "VIC",
                History = "Test Club",
                Role = PlayerRole.Centre,
                PotentialCeiling = 85,
                Morale = 0.8f,
                Stamina = 0.9f
            };
            original.Stats.Kicking     = 60;
            original.Stats.Handballing = 55;
            original.Stats.Tackling    = 50;
            original.Stats.Speed       = 65;
            original.Stats.Stamina     = 70;
            original.Stats.Knowledge   = 75;
            original.Stats.Playmaking  = 80;
            original.Contract = new ContractDetails { Salary = 120000f, YearsRemaining = 2 };

            // Serialize to JSON
            string json = JsonSerialization.ToJson(original, prettyPrint: true);

            // Deserialize back
            var roundTripped = JsonSerialization.FromJson<Player>(json);

            // Verify key fields
            Assert.AreEqual(original.Name,           roundTripped.Name);
            Assert.AreEqual(original.Age,            roundTripped.Age);
            Assert.AreEqual(original.Stats.Kicking, roundTripped.Stats.Kicking);
            Assert.AreEqual(original.Contract.Salary, roundTripped.Contract.Salary);
        }

        [Test]
        public void FileWriteAndRead_LoadsIdenticalObject()
        {
            var player = new Player
            {
                Name = "File Tester",
                Age = 30,
                State = "NSW",
                History = "Another Club",
                Role = PlayerRole.Wing,
                PotentialCeiling = 90,
                Morale = 1.0f,
                Stamina = 1.0f
            };
            player.Stats.Kicking     = 70;
            player.Stats.Handballing = 65;
            player.Stats.Tackling    = 60;
            player.Stats.Speed       = 75;
            player.Stats.Stamina     = 80;
            player.Stats.Knowledge   = 85;
            player.Stats.Playmaking  = 90;
            player.Contract = new ContractDetails { Salary = 150000f, YearsRemaining = 3 };

            // Serialize & write
            string json = JsonSerialization.ToJson(player, prettyPrint: false);
            File.WriteAllText(testFilePath, json);

            // Read & deserialize
            string fromDisk = File.ReadAllText(testFilePath);
            var loaded = JsonSerialization.FromJson<Player>(fromDisk);

            // Assert round-trip fidelity
            Assert.AreEqual(player.Name,           loaded.Name);
            Assert.AreEqual(player.Stats.Playmaking, loaded.Stats.Playmaking);
            Assert.AreEqual(player.Contract.YearsRemaining, loaded.Contract.YearsRemaining);
        }
    }
}
