using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Selection;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Engine.Simulation;

namespace AFLCoachSim.Core.Tests
{
    /// <summary>
    /// Tests for the position-aware player selection system
    /// </summary>
    public class PositionalSystemTests
    {
        [Test]
        public void PositionUtils_CorrectlyIdentifiesPositions()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(PositionUtils.IsMidfielder(Role.MID));
            Assert.IsTrue(PositionUtils.IsMidfielder(Role.WING));
            Assert.IsFalse(PositionUtils.IsMidfielder(Role.KPF));

            Assert.IsTrue(PositionUtils.IsForward(Role.KPF));
            Assert.IsTrue(PositionUtils.IsForward(Role.SMLF));
            Assert.IsTrue(PositionUtils.IsForward(Role.HFF));
            Assert.IsFalse(PositionUtils.IsForward(Role.MID));

            Assert.IsTrue(PositionUtils.IsDefender(Role.KPD));
            Assert.IsTrue(PositionUtils.IsDefender(Role.SMLB));
            Assert.IsTrue(PositionUtils.IsDefender(Role.HBF));
            Assert.IsFalse(PositionUtils.IsDefender(Role.RUC));

            Assert.IsTrue(PositionUtils.IsRuckman(Role.RUC));
            Assert.IsFalse(PositionUtils.IsRuckman(Role.MID));
        }

        [Test]
        public void PositionUtils_GetPositionGroup_ReturnsCorrectGroups()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(PositionGroup.Defense, PositionUtils.GetPositionGroup(Role.KPD));
            Assert.AreEqual(PositionGroup.Defense, PositionUtils.GetPositionGroup(Role.SMLB));
            Assert.AreEqual(PositionGroup.Defense, PositionUtils.GetPositionGroup(Role.HBF));

            Assert.AreEqual(PositionGroup.Midfield, PositionUtils.GetPositionGroup(Role.MID));
            Assert.AreEqual(PositionGroup.Midfield, PositionUtils.GetPositionGroup(Role.WING));

            Assert.AreEqual(PositionGroup.Forward, PositionUtils.GetPositionGroup(Role.KPF));
            Assert.AreEqual(PositionGroup.Forward, PositionUtils.GetPositionGroup(Role.SMLF));
            Assert.AreEqual(PositionGroup.Forward, PositionUtils.GetPositionGroup(Role.HFF));

            Assert.AreEqual(PositionGroup.Ruck, PositionUtils.GetPositionGroup(Role.RUC));
        }

        [Test]
        public void PositionalSelector_CenterBounceParticipants_SelectsMidfielders()
        {
            // Arrange
            var players = CreateTestPlayers();
            var rng = new DeterministicRandom(123);

            // Act
            var participants = PositionalSelector.GetCenterBounceParticipants(players, rng, 5);

            // Assert
            Assert.AreEqual(5, participants.Count);
            // Should prioritize midfielders and ruckmen
            var midfieldersSelected = participants.Count(p => PositionUtils.IsMidfielder(p.PrimaryRole) || PositionUtils.IsRuckman(p.PrimaryRole));
            Assert.Greater(midfieldersSelected, 2, "Center bounce should primarily select midfielders/ruckmen");
        }

        [Test]
        public void PositionalSelector_Inside50Participants_SelectsForwards()
        {
            // Arrange
            var players = CreateTestPlayers();
            var rng = new DeterministicRandom(123);

            // Act
            var participants = PositionalSelector.GetInside50Participants(players, rng, 6);

            // Assert
            Assert.AreEqual(6, participants.Count);
            // Should prioritize forwards and attacking midfielders
            var forwardsSelected = participants.Count(p => PositionUtils.IsForward(p.PrimaryRole) || PositionUtils.IsMidfielder(p.PrimaryRole));
            Assert.Greater(forwardsSelected, 3, "Inside50 should primarily select forwards/attacking mids");
        }

        [Test]
        public void PositionalSelector_DefensiveParticipants_SelectsDefenders()
        {
            // Arrange
            var players = CreateTestPlayers();
            var rng = new DeterministicRandom(123);

            // Act
            var participants = PositionalSelector.GetDefensiveParticipants(players, rng, 6);

            // Assert
            Assert.AreEqual(6, participants.Count);
            // Should prioritize defenders and defensive midfielders
            var defendersSelected = participants.Count(p => PositionUtils.IsDefender(p.PrimaryRole) || PositionUtils.IsMidfielder(p.PrimaryRole));
            Assert.Greater(defendersSelected, 3, "Defense should primarily select defenders/defensive mids");
        }

        [Test]
        public void PositionalSelector_GetPrimaryEventPlayer_ReturnsAppropriatePlayer()
        {
            // Arrange
            var players = CreateTestPlayers();
            var rng = new DeterministicRandom(123);

            // Act & Assert - Center bounce should select midfielder or ruckman
            var centerBouncePlayer = PositionalSelector.GetPrimaryEventPlayer(players, MatchEventType.CenterBounceWin, rng);
            Assert.IsTrue(PositionUtils.IsMidfielder(centerBouncePlayer.PrimaryRole) || PositionUtils.IsRuckman(centerBouncePlayer.PrimaryRole));

            // Goal should select forward or attacking mid
            var goalPlayer = PositionalSelector.GetPrimaryEventPlayer(players, MatchEventType.Goal, rng);
            Assert.IsTrue(PositionUtils.IsForward(goalPlayer.PrimaryRole) || PositionUtils.IsMidfielder(goalPlayer.PrimaryRole));

            // Tackle should select defender or mid
            var tacklePlayer = PositionalSelector.GetPrimaryEventPlayer(players, MatchEventType.Tackle, rng);
            Assert.IsTrue(PositionUtils.IsDefender(tacklePlayer.PrimaryRole) || PositionUtils.IsMidfielder(tacklePlayer.PrimaryRole));
        }

        [Test]
        public void AutoSelector_PositionalSelection_CreatesBalancedTeam()
        {
            // Arrange
            var roster = CreateLargeTestRoster(); // 30+ players with variety
            var onField = new List<Player>();
            var bench = new List<Player>();

            // Act
            AutoSelector.Select22(roster, new TeamId(1), onField, bench);

            // Assert
            Assert.AreEqual(22, onField.Count);
            Assert.LessOrEqual(bench.Count, 4);

            // Check positional balance
            var defenders = onField.Count(p => PositionUtils.IsDefender(p.PrimaryRole));
            var midfielders = onField.Count(p => PositionUtils.IsMidfielder(p.PrimaryRole));
            var forwards = onField.Count(p => PositionUtils.IsForward(p.PrimaryRole));
            var ruckmen = onField.Count(p => PositionUtils.IsRuckman(p.PrimaryRole));

            // Should have a reasonable spread (not all one position)
            Assert.Greater(defenders, 3, "Should have multiple defenders");
            Assert.Greater(midfielders, 3, "Should have multiple midfielders");
            Assert.Greater(forwards, 2, "Should have multiple forwards");
            Assert.Greater(ruckmen, 0, "Should have at least one ruckman");
        }

        // Helper methods
        private List<Player> CreateTestPlayers()
        {
            var players = new List<Player>
            {
                // Defenders
                CreatePlayer(Role.KPD, "KPD1", clearance: 50, marking: 85, tackling: 80),
                CreatePlayer(Role.SMLB, "SMLB1", clearance: 45, marking: 70, tackling: 85),
                CreatePlayer(Role.HBF, "HBF1", clearance: 55, marking: 75, tackling: 75),

                // Midfielders
                CreatePlayer(Role.MID, "MID1", clearance: 90, marking: 65, tackling: 70),
                CreatePlayer(Role.MID, "MID2", clearance: 85, marking: 60, tackling: 65),
                CreatePlayer(Role.WING, "WING1", clearance: 75, marking: 60, tackling: 60),

                // Forwards
                CreatePlayer(Role.KPF, "KPF1", clearance: 40, marking: 95, tackling: 50),
                CreatePlayer(Role.SMLF, "SMLF1", clearance: 50, marking: 85, tackling: 55),
                CreatePlayer(Role.HFF, "HFF1", clearance: 60, marking: 80, tackling: 60),

                // Ruckmen
                CreatePlayer(Role.RUC, "RUC1", clearance: 85, marking: 75, tackling: 60, strength: 95)
            };

            return players;
        }

        private List<Player> CreateLargeTestRoster()
        {
            var roster = new List<Player>();
            var id = 1;

            // Add variety of positions (like a real AFL list)
            for (int i = 0; i < 3; i++) roster.Add(CreatePlayer(Role.KPD, $"KPD{i}", id: id++));
            for (int i = 0; i < 4; i++) roster.Add(CreatePlayer(Role.SMLB, $"SMLB{i}", id: id++));
            for (int i = 0; i < 3; i++) roster.Add(CreatePlayer(Role.HBF, $"HBF{i}", id: id++));
            
            for (int i = 0; i < 6; i++) roster.Add(CreatePlayer(Role.MID, $"MID{i}", id: id++));
            for (int i = 0; i < 4; i++) roster.Add(CreatePlayer(Role.WING, $"WING{i}", id: id++));
            
            for (int i = 0; i < 3; i++) roster.Add(CreatePlayer(Role.KPF, $"KPF{i}", id: id++));
            for (int i = 0; i < 3; i++) roster.Add(CreatePlayer(Role.SMLF, $"SMLF{i}", id: id++));
            for (int i = 0; i < 3; i++) roster.Add(CreatePlayer(Role.HFF, $"HFF{i}", id: id++));
            
            for (int i = 0; i < 3; i++) roster.Add(CreatePlayer(Role.RUC, $"RUC{i}", id: id++));

            return roster;
        }

        private Player CreatePlayer(Role role, string name, int clearance = 70, int marking = 70, int tackling = 70, int strength = 70, int id = 1)
        {
            return new Player
            {
                Id = new PlayerId(id),
                Name = name,
                PrimaryRole = role,
                Attr = new Attributes
                {
                    Clearance = clearance,
                    Marking = marking,
                    Tackling = tackling,
                    Strength = strength,
                    Kicking = 70,
                    Positioning = 70,
                    DecisionMaking = 70,
                    WorkRate = 70,
                    Speed = 70,
                    Acceleration = 70,
                    Agility = 70,
                    Jump = 70,
                    Handball = 70,
                    RuckWork = 70,
                    Spoiling = 70,
                    Composure = 70,
                    Leadership = 70
                }
            };
        }
    }
}
