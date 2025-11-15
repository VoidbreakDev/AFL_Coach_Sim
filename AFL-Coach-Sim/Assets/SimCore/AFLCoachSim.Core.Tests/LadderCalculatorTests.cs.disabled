// LadderCalculatorTests.cs
using NUnit.Framework;
using System.Collections.Generic;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Ladder;

public class LadderCalculatorTests
{
    [Test]
    public void Ladder_SortsByPremiershipPointsThenPercentage()
    {
        var t1 = new TeamId(1); var t2 = new TeamId(2); var t3 = new TeamId(3);
        var results = new List<MatchResultDTO>
        {
            new() { Round=1, Home=t1, Away=t2, HomeScore=80, AwayScore=60 },
            new() { Round=1, Home=t3, Away=t1, HomeScore=70, AwayScore=70 }, // draw
            new() { Round=1, Home=t2, Away=t3, HomeScore=50, AwayScore=100 },
        };

        var ladder = LadderCalculator.BuildLadder(results);
        // t1: W1 D1 = 6 pts, t3: W1 D1 = 6 pts, t2: L2 = 0 pts
        // t3 has better percentage (100+70 / 50+70) than t1 (80+70 / 60+70)
        Assert.AreEqual(t3.Value, ladder[0].Team.Value);
        Assert.AreEqual(t1.Value, ladder[1].Team.Value);
        Assert.AreEqual(t2.Value, ladder[2].Team.Value);
    }
}