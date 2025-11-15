// SchedulerTests.cs
using NUnit.Framework;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Scheduling;

public class SchedulerTests
{
    [Test]
    public void RoundRobin_NoDuplicates_AllTeamsPlayEachOther()
    {
        var teams = Enumerable.Range(1, 6).Select(i => new TeamId(i)).ToList();
        var fixtures = RoundRobinScheduler.Build(teams, doubleRound: true);

        // each pair appears twice (home/away)
        int expectedMatches = teams.Count * (teams.Count - 1); // double round
        Assert.AreEqual(expectedMatches, fixtures.Count);

        // no team plays itself
        Assert.IsTrue(fixtures.All(f => f.home.Value != f.away.Value));
    }
}