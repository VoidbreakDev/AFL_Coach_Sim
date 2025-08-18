// MatchSimulatorTests.cs
using NUnit.Framework;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Simulation;

public class MatchSimulatorTests
{
    [Test]
    public void Sim_Seeded_IsDeterministic()
    {
        var teams = new Dictionary<TeamId, Team>
        {
            [new TeamId(1)] = new Team(new TeamId(1), "A", 60, 55),
            [new TeamId(2)] = new Team(new TeamId(2), "B", 58, 52),
        };

        var simA = new MatchSimulator(teams, new DeterministicRandom(1234));
        var simB = new MatchSimulator(teams, new DeterministicRandom(1234));

        var r1 = simA.Simulate(1, new TeamId(1), new TeamId(2));
        var r2 = simB.Simulate(1, new TeamId(1), new TeamId(2));

        Assert.AreEqual(r1.HomeScore, r2.HomeScore);
        Assert.AreEqual(r1.AwayScore, r2.AwayScore);
    }
}