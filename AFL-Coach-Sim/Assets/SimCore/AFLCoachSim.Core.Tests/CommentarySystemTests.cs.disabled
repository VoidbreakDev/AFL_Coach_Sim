using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Engine.Simulation;

namespace AFLCoachSim.Core.Tests
{
    [TestFixture]
    public class CommentarySystemTests
    {
        private Dictionary<TeamId, Team> _teams;
        private Dictionary<TeamId, List<Domain.Entities.Player>> _rosters;
        private DeterministicRandom _rng;
        
        [SetUp]
        public void Setup()
        {
            var homeId = new TeamId(1);
            var awayId = new TeamId(2);
            
            _teams = new Dictionary<TeamId, Team>
            {
                { homeId, new Team(homeId, "Home United", 75, 80) },
                { awayId, new Team(awayId, "Away Rangers", 70, 85) }
            };
            
            // Create simple rosters for testing
            _rosters = new Dictionary<TeamId, List<Domain.Entities.Player>>
            {
                { homeId, CreateTestRoster("Home") },
                { awayId, CreateTestRoster("Away") }
            };
            
            _rng = new DeterministicRandom(12345);
        }
        
        [Test]
        public void CommentarySystem_ShouldGenerateMatchCommentary()
        {
            // Arrange
            var homeId = new TeamId(1);
            var awayId = new TeamId(2);
            
            // Act
            var result = MatchEngineWithCommentary.PlayMatchWithCommentary(
                round: 1,
                homeId: homeId,
                awayId: awayId,
                teams: _teams,
                rosters: _rosters,
                rng: _rng);
            
            // Assert
            Assert.IsNotNull(result.Commentary);
            Assert.IsTrue(result.Commentary.Count > 0, "Should generate some commentary events");
            Assert.IsNotNull(result.Events);
            Assert.AreEqual(result.Commentary.Count, result.Events.Count, "Should have equal commentary and events");
            
            // Should have quarter start events
            var quarterStarts = result.Events.Where(e => e.EventType == MatchEventType.QuarterStart).ToList();
            Assert.AreEqual(4, quarterStarts.Count, "Should have 4 quarter starts");
            
            // Should have some scoring events
            var scoringEvents = result.Events.Where(e => 
                e.EventType == MatchEventType.Goal || e.EventType == MatchEventType.Behind).ToList();
            Assert.IsTrue(scoringEvents.Count > 0, "Should have some scoring events");
            
            // Print some sample commentary for verification
            TestContext.WriteLine("Sample Commentary:");
            foreach (var commentary in result.Commentary.Take(10))
            {
                TestContext.WriteLine($"  {commentary}");
            }
        }
        
        [Test]
        public void CommentarySystem_ShouldGenerateHighlights()
        {
            // Arrange & Act
            var result = MatchEngineWithCommentary.PlayMatchWithCommentary(
                round: 1,
                homeId: new TeamId(1),
                awayId: new TeamId(2),
                teams: _teams,
                rosters: _rosters,
                rng: _rng);
            
            var highlights = MatchEngineWithCommentary.GetMatchHighlights(result);
            
            // Assert
            Assert.IsNotNull(highlights);
            Assert.IsTrue(highlights.Count > 0, "Should generate some highlights");
            
            TestContext.WriteLine("Match Highlights:");
            foreach (var highlight in highlights)
            {
                TestContext.WriteLine($"  {highlight}");
            }
        }
        
        [Test]
        public void CommentarySystem_ShouldGenerateQuarterSummaries()
        {
            // Arrange & Act
            var result = MatchEngineWithCommentary.PlayMatchWithCommentary(
                round: 1,
                homeId: new TeamId(1),
                awayId: new TeamId(2),
                teams: _teams,
                rosters: _rosters,
                rng: _rng);
            
            var summaries = MatchEngineWithCommentary.GetQuarterSummaries(result);
            
            // Assert
            Assert.IsNotNull(summaries);
            Assert.AreEqual(4, summaries.Keys.Count, "Should have 4 quarters");
            
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                Assert.IsTrue(summaries[quarter].Count > 0, $"Quarter {quarter} should have events");
                TestContext.WriteLine($"Quarter {quarter} Summary:");
                foreach (var commentary in summaries[quarter].Take(3))
                {
                    TestContext.WriteLine($"  {commentary}");
                }
            }
        }
        
        [Test]
        public void CommentaryGenerator_ShouldGenerateVariedCommentary()
        {
            // Arrange
            var generator = new CommentaryGenerator(_rng);
            var matchEvent = new MatchEvent
            {
                EventType = MatchEventType.Goal,
                Quarter = 1,
                TimeRemaining = 1200, // 20:00
                PrimaryPlayerName = "Test Player",
                ZoneDescription = "from 30m out"
            };
            
            var commentaries = new HashSet<string>();
            
            // Act - Generate multiple commentaries for same event
            for (int i = 0; i < 20; i++)
            {
                var commentary = generator.GenerateCommentary(matchEvent);
                commentaries.Add(commentary);
            }
            
            // Assert
            Assert.IsTrue(commentaries.Count > 1, "Should generate varied commentary for same event type");
            
            TestContext.WriteLine("Sample Goal Commentary Variations:");
            foreach (var commentary in commentaries.Take(5))
            {
                TestContext.WriteLine($"  {commentary}");
            }
        }
        
        private List<Domain.Entities.Player> CreateTestRoster(string teamPrefix)
        {
            var roster = new List<Domain.Entities.Player>();
            var firstNames = new[] { "Jack", "Tom", "Sam", "Luke", "Ben", "Matt", "Josh", "Ryan", "Alex", "Nick" };
            var lastNames = new[] { "Smith", "Jones", "Brown", "Wilson", "Davis", "Miller", "Moore", "Taylor", "Anderson", "Thomas" };
            
            for (int i = 0; i < 22; i++)
            {
                var firstName = firstNames[i % firstNames.Length];
                var lastName = lastNames[i % lastNames.Length];
                
                roster.Add(new Domain.Entities.Player
                {
                    Id = new PlayerId(i + 1), // Sequential IDs starting from 1
                    Name = $"{firstName} {lastName}",
                    // Add basic attributes if your Player class has them
                });
            }
            
            return roster;
        }
    }
}
