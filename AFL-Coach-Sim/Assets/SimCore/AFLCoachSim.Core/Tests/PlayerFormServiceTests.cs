using System;
using NUnit.Framework;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.Services;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Tests
{
    [TestFixture]
    public class PlayerFormServiceTests
    {
        private PlayerFormService _formService;
        private Player _testPlayer;

        [SetUp]
        public void Setup()
        {
            _formService = new PlayerFormService(42); // Fixed seed for predictable tests
            _testPlayer = new Player
            {
                Id = new PlayerId(1),
                Name = "Test Player",
                Age = 25,
                PrimaryRole = Role.MID,
                Condition = 100,
                Form = 0,
                Endurance = 70,
                Durability = 60,
                Discipline = 50
            };
        }

        [Test]
        public void UpdateAfterMatch_ExcellentPerformance_IncreasesForm()
        {
            // Arrange
            int initialForm = _testPlayer.Form;
            
            // Act
            _formService.UpdateAfterMatch(_testPlayer, performance: 9, minutesPlayed: 90);
            
            // Assert
            Assert.Greater(_testPlayer.Form, initialForm, "Excellent performance should increase form");
            Assert.Less(_testPlayer.Condition, 100, "Playing 90 minutes should reduce condition");
        }

        [Test]
        public void UpdateAfterMatch_PoorPerformance_DecreasesForm()
        {
            // Arrange
            int initialForm = _testPlayer.Form;
            
            // Act
            _formService.UpdateAfterMatch(_testPlayer, performance: 2, minutesPlayed: 45);
            
            // Assert
            Assert.Less(_testPlayer.Form, initialForm, "Poor performance should decrease form");
            Assert.Less(_testPlayer.Condition, 100, "Playing should reduce condition");
        }

        [Test]
        public void UpdateAfterMatch_WithInjury_ReducesConditionMore()
        {
            // Arrange
            int initialCondition = _testPlayer.Condition;
            
            // Act - same performance, one with injury
            var playerWithoutInjury = new Player { Condition = 100, Form = 0 };
            var playerWithInjury = new Player { Condition = 100, Form = 0 };
            
            _formService.UpdateAfterMatch(playerWithoutInjury, performance: 6, minutesPlayed: 90, wasInjured: false);
            _formService.UpdateAfterMatch(playerWithInjury, performance: 6, minutesPlayed: 90, wasInjured: true);
            
            // Assert
            Assert.Less(playerWithInjury.Condition, playerWithoutInjury.Condition, 
                "Injury should cause more condition loss");
        }

        [Test]
        public void ProcessDailyRecovery_ImprovesCondition()
        {
            // Arrange
            _testPlayer.Condition = 60; // Reduced condition
            int initialCondition = _testPlayer.Condition;
            
            // Act
            _formService.ProcessDailyRecovery(_testPlayer);
            
            // Assert
            Assert.Greater(_testPlayer.Condition, initialCondition, "Daily recovery should improve condition");
        }

        [Test]
        public void GetPerformanceModifier_HighForm_ReturnsPositiveModifier()
        {
            // Arrange
            _testPlayer.Form = 18; // High form
            _testPlayer.Condition = 90;
            
            // Act
            float modifier = _formService.GetPerformanceModifier(_testPlayer);
            
            // Assert
            Assert.Greater(modifier, 1.0f, "High form should provide performance boost");
        }

        [Test]
        public void GetPerformanceModifier_LowForm_ReturnsNegativeModifier()
        {
            // Arrange
            _testPlayer.Form = -18; // Low form
            _testPlayer.Condition = 40; // Low condition
            
            // Act
            float modifier = _formService.GetPerformanceModifier(_testPlayer);
            
            // Assert
            Assert.Less(modifier, 1.0f, "Low form and condition should reduce performance");
        }

        [Test]
        public void GetPerformanceModifier_ClampedToReasonableRange()
        {
            // Arrange - extreme values
            _testPlayer.Form = 20; // Max form
            _testPlayer.Condition = 100; // Max condition
            _testPlayer.Age = 20; // Young
            
            // Act
            float modifier = _formService.GetPerformanceModifier(_testPlayer);
            
            // Assert
            Assert.LessOrEqual(modifier, 1.3f, "Performance modifier should be clamped to max 1.3");
            Assert.GreaterOrEqual(modifier, 0.6f, "Performance modifier should be clamped to min 0.6");
        }

        [Test]
        public void GetPlayerStatus_ExhaustedPlayer_ReturnsExhausted()
        {
            // Arrange
            _testPlayer.Condition = 25; // Very low condition
            
            // Act
            var status = _formService.GetPlayerStatus(_testPlayer);
            
            // Assert
            Assert.AreEqual(PlayerFormStatus.Exhausted, status);
        }

        [Test]
        public void GetPlayerStatus_OutOfFormPlayer_ReturnsOutOfForm()
        {
            // Arrange
            _testPlayer.Form = -18; // Very poor form
            _testPlayer.Condition = 80; // Good condition
            
            // Act
            var status = _formService.GetPlayerStatus(_testPlayer);
            
            // Assert
            Assert.AreEqual(PlayerFormStatus.OutOfForm, status);
        }

        [Test]
        public void GetPlayerStatus_ExcellentFormPlayer_ReturnsInExcellentForm()
        {
            // Arrange
            _testPlayer.Form = 18; // Excellent form
            _testPlayer.Condition = 90; // High condition
            
            // Act
            var status = _formService.GetPlayerStatus(_testPlayer);
            
            // Assert
            Assert.AreEqual(PlayerFormStatus.InExcellentForm, status);
        }

        [Test]
        public void FormBounds_NeverExceedLimits()
        {
            // Arrange - Start at extreme
            _testPlayer.Form = 19;
            
            // Act - Multiple excellent performances
            for (int i = 0; i < 10; i++)
            {
                _formService.UpdateAfterMatch(_testPlayer, performance: 10, minutesPlayed: 90);
            }
            
            // Assert
            Assert.LessOrEqual(_testPlayer.Form, 20, "Form should never exceed +20");
            Assert.GreaterOrEqual(_testPlayer.Form, -20, "Form should never go below -20");
        }

        [Test]
        public void ConditionBounds_NeverExceedLimits()
        {
            // Arrange
            _testPlayer.Condition = 15; // Very low
            
            // Act - Multiple recovery days
            for (int i = 0; i < 50; i++)
            {
                _formService.ProcessDailyRecovery(_testPlayer);
            }
            
            // Assert
            Assert.LessOrEqual(_testPlayer.Condition, 100, "Condition should never exceed 100");
            Assert.GreaterOrEqual(_testPlayer.Condition, 10, "Condition should never go below 10");
        }

        [Test]
        public void OlderPlayers_RecoverSlower()
        {
            // Arrange
            var youngPlayer = new Player { Age = 22, Condition = 50, Durability = 60 };
            var oldPlayer = new Player { Age = 35, Condition = 50, Durability = 60 };
            
            // Act - Same recovery time
            for (int i = 0; i < 10; i++)
            {
                _formService.ProcessDailyRecovery(youngPlayer);
                _formService.ProcessDailyRecovery(oldPlayer);
            }
            
            // Assert
            Assert.Greater(youngPlayer.Condition, oldPlayer.Condition, 
                "Younger players should recover faster than older players");
        }
    }
}
