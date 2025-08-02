// File: Assets/Tests/SeasonSchedulerTests.cs
using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using AFLManager.Models;
using AFLManager.Managers;

namespace AFLManager.Tests
{
    public class SeasonSchedulerTests
    {
        private DateTime startDate;

        [SetUp]
        public void Setup()
        {
            startDate = new DateTime(2025, 1, 1);
        }

        [Test]
        public void GenerateSeason_EvenNumberOfTeams_CreatesCorrectNumberOfFixtures()
        {
            var teams = new List<Team>
            {
                new Team { Name = "Team A", Level = LeagueLevel.Local },
                new Team { Name = "Team B", Level = LeagueLevel.Local },
                new Team { Name = "Team C", Level = LeagueLevel.Local },
                new Team { Name = "Team D", Level = LeagueLevel.Local }
            };

            var schedule = SeasonScheduler.GenerateSeason(teams, startDate, daysBetweenMatches: 7);

            int expectedRounds = teams.Count - 1;
            int expectedMatchesPerRound = teams.Count / 2;
            int expectedTotalFixtures = expectedRounds * expectedMatchesPerRound;

            Assert.AreEqual(expectedTotalFixtures, schedule.Fixtures.Count);
        }

        [Test]
        public void GenerateSeason_OddNumberOfTeams_ExcludesByeMatches()
        {
            var teams = new List<Team>
            {
                new Team { Name = "Team A", Level = LeagueLevel.Local },
                new Team { Name = "Team B", Level = LeagueLevel.Local },
                new Team { Name = "Team C", Level = LeagueLevel.Local }
            };

            var schedule = SeasonScheduler.GenerateSeason(teams, startDate, daysBetweenMatches: 7);

            // 3 teams → add one BYE → rounds = 3, matchesPerRound = 2 → raw fixtures = 6, minus 3 bye fixtures = 3
            Assert.AreEqual(3, schedule.Fixtures.Count);

            foreach (var match in schedule.Fixtures)
            {
                Assert.AreNotEqual("BYE", match.HomeTeamId);
                Assert.AreNotEqual("BYE", match.AwayTeamId);
            }
        }

        [Test]
        public void GenerateSeason_SingleFixture_DateIsStartDate()
        {
            var teams = new List<Team>
            {
                new Team { Name = "Team A", Level = LeagueLevel.Local },
                new Team { Name = "Team B", Level = LeagueLevel.Local }
            };

            var schedule = SeasonScheduler.GenerateSeason(teams, startDate, daysBetweenMatches: 7);

            Assert.AreEqual(1, schedule.Fixtures.Count);
            Assert.AreEqual(startDate, schedule.Fixtures[0].FixtureDate);
        }
    }
}
