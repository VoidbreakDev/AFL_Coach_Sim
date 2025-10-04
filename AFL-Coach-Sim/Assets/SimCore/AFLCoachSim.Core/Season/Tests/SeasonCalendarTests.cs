using System;
using System.Linq;
using NUnit.Framework;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Services;

namespace AFLCoachSim.Core.Season.Tests
{
    [TestFixture]
    public class SeasonCalendarTests
    {
        private FixtureGenerationEngine _fixtureEngine;
        
        [SetUp]
        public void Setup()
        {
            _fixtureEngine = new FixtureGenerationEngine(seed: 12345); // Fixed seed for reproducible tests
        }
        
        #region AFL Calendar Utilities Tests
        
        [Test]
        public void GetSeasonOpenerDate_2024_ReturnsSecondThursdayOfMarch()
        {
            var result = AFLCalendarUtilities.GetSeasonOpenerDate(2024);
            
            Assert.AreEqual(2024, result.Year);
            Assert.AreEqual(3, result.Month); // March
            Assert.AreEqual(DayOfWeek.Thursday, result.DayOfWeek);
            Assert.AreEqual(14, result.Day); // Second Thursday of March 2024
        }
        
        [Test]
        public void GetAnzacDay_2024_ReturnsApril25()
        {
            var result = AFLCalendarUtilities.GetAnzacDay(2024);
            
            Assert.AreEqual(2024, result.Year);
            Assert.AreEqual(4, result.Month); // April
            Assert.AreEqual(25, result.Day);
        }
        
        [Test]
        public void GetEasterMonday_2024_ReturnsCorrectDate()
        {
            var result = AFLCalendarUtilities.GetEasterMonday(2024);
            
            Assert.AreEqual(2024, result.Year);
            Assert.AreEqual(DayOfWeek.Monday, result.DayOfWeek);
            // Easter Monday 2024 is April 1st
            Assert.AreEqual(4, result.Month);
            Assert.AreEqual(1, result.Day);
        }
        
        [Test]
        public void GetKingsBirthday_2024_ReturnsSecondMondayOfJune()
        {
            var result = AFLCalendarUtilities.GetKingsBirthday(2024);
            
            Assert.AreEqual(2024, result.Year);
            Assert.AreEqual(6, result.Month); // June
            Assert.AreEqual(DayOfWeek.Monday, result.DayOfWeek);
            Assert.AreEqual(10, result.Day); // Second Monday of June 2024
        }
        
        [Test]
        public void GetGrandFinalWeekend_2024_ReturnsFinalSaturdayOfSeptember()
        {
            var result = AFLCalendarUtilities.GetGrandFinalWeekend(2024);
            
            Assert.AreEqual(2024, result.Year);
            Assert.AreEqual(9, result.Month); // September
            Assert.AreEqual(DayOfWeek.Saturday, result.DayOfWeek);
            Assert.AreEqual(28, result.Day); // Last Saturday of September 2024
        }
        
        [Test]
        [TestCase(DayOfWeek.Thursday, 19, 25)] // 7:25 PM
        [TestCase(DayOfWeek.Friday, 19, 50)]   // 7:50 PM
        [TestCase(DayOfWeek.Saturday, 14, 10)] // 2:10 PM
        [TestCase(DayOfWeek.Sunday, 13, 20)]   // 1:20 PM
        public void GetTypicalMatchTime_ReturnsCorrectTimes(DayOfWeek day, int expectedHour, int expectedMinute)
        {
            var result = AFLCalendarUtilities.GetTypicalMatchTime(day);
            
            Assert.AreEqual(expectedHour, result.Hours);
            Assert.AreEqual(expectedMinute, result.Minutes);
        }
        
        #endregion
        
        #region Fixture Generation Tests
        
        [Test]
        public void GenerateSeasonCalendar_Default_CreatesValidSeason()
        {
            var season = _fixtureEngine.GenerateSeasonCalendar(2024);
            
            Assert.IsNotNull(season);
            Assert.AreEqual(2024, season.Year);
            Assert.AreEqual(24, season.TotalRounds);
            Assert.AreEqual(24, season.Rounds.Count);
            Assert.AreEqual(SeasonState.NotStarted, season.CurrentState);
            Assert.AreEqual(1, season.CurrentRound);
            
            // Verify season dates
            Assert.AreEqual(AFLCalendarUtilities.GetSeasonOpenerDate(2024), season.SeasonStart);
            Assert.AreEqual(AFLCalendarUtilities.GetGrandFinalWeekend(2024), season.SeasonEnd);
        }
        
        [Test]
        public void GenerateSeasonCalendar_ValidatesCorrectly()
        {
            var season = _fixtureEngine.GenerateSeasonCalendar(2024);
            var validation = season.Validate();
            
            Assert.IsTrue(validation.IsValid, $"Season validation failed: {string.Join(", ", validation.Errors)}");
        }
        
        [Test]
        public void GenerateSeasonCalendar_IncludesSpecialtyMatches()
        {
            var season = _fixtureEngine.GenerateSeasonCalendar(2024);
            
            Assert.IsTrue(season.SpecialtyMatches.Any(), "No specialty matches generated");
            
            // Check for specific specialty matches
            var seasonOpener = season.SpecialtyMatches.FirstOrDefault(sm => sm.Type == SpecialtyMatchType.SeasonOpener);
            Assert.IsNotNull(seasonOpener, "Season opener not found");
            Assert.AreEqual(TeamId.Carlton, seasonOpener.HomeTeam);
            Assert.AreEqual(TeamId.Richmond, seasonOpener.AwayTeam);
            Assert.AreEqual(1, seasonOpener.RoundNumber);
            
            var anzacDay = season.SpecialtyMatches.FirstOrDefault(sm => sm.Type == SpecialtyMatchType.AnzacDay);
            Assert.IsNotNull(anzacDay, "ANZAC Day match not found");
            Assert.AreEqual(TeamId.Collingwood, anzacDay.HomeTeam);
            Assert.AreEqual(TeamId.Essendon, anzacDay.AwayTeam);
        }
        
        [Test]
        public void GenerateSeasonCalendar_HasCorrectByeConfiguration()
        {
            var season = _fixtureEngine.GenerateSeasonCalendar(2024);
            var byeConfig = season.ByeConfiguration;
            
            Assert.IsNotNull(byeConfig);
            Assert.AreEqual(12, byeConfig.StartRound);
            Assert.AreEqual(15, byeConfig.EndRound);
            Assert.AreEqual(6, byeConfig.TeamsPerByeRound);
            
            // Validate bye configuration
            var validation = byeConfig.Validate();
            Assert.IsTrue(validation.IsValid, $"Bye configuration invalid: {string.Join(", ", validation.Errors)}");
            
            // Check all teams have exactly one bye
            var allTeams = Enum.GetValues<TeamId>().Where(t => t != TeamId.None).ToList();
            var allByeTeams = byeConfig.ByeRoundAssignments.Values.SelectMany(teams => teams).ToList();
            
            foreach (var team in allTeams)
            {
                var byeCount = allByeTeams.Count(t => t == team);
                Assert.AreEqual(1, byeCount, $"Team {team} has {byeCount} byes, expected 1");
            }
        }
        
        [Test]
        public void GenerateSeasonCalendar_EachTeamPlaysCorrectNumberOfMatches()
        {
            var season = _fixtureEngine.GenerateSeasonCalendar(2024);
            var allTeams = Enum.GetValues<TeamId>().Where(t => t != TeamId.None).ToList();
            
            foreach (var team in allTeams)
            {
                var teamMatches = season.GetTeamMatches(team).Count();
                var expectedMatches = season.TotalRounds - 1; // Account for bye round
                
                Assert.AreEqual(expectedMatches, teamMatches, $"Team {team} has {teamMatches} matches, expected {expectedMatches}");
            }
        }
        
        #endregion
    }
}
