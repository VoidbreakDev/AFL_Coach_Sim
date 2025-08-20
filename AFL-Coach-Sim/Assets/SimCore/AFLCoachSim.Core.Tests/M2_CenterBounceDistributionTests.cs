using NUnit.Framework;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Domain.Entities;

namespace AFLCoachSim.Core.Tests
{
    public class M2_CenterBounceDistributionTests
    {
        [Test]
        public void Better_Midfield_Tends_To_Win()
        {
            var teams = new Dictionary<TeamId, Team>
            {
                [new TeamId(1)] = new Team(new TeamId(1), "Home", 60, 60),
                [new TeamId(2)] = new Team(new TeamId(2), "Away", 60, 60)
            };

            var strongMid = new List<Player> {
                new Player { Attr = new Attributes { Clearance = 90, Strength = 80, Positioning = 75, DecisionMaking = 80 } },
                new Player { Attr = new Attributes { Clearance = 85, Strength = 78, Positioning = 72, DecisionMaking = 78 } },
                new Player { Attr = new Attributes { Clearance = 82, Strength = 76, Positioning = 70, DecisionMaking = 75 } },
                new Player { Attr = new Attributes { Clearance = 80, Strength = 75, Positioning = 70, DecisionMaking = 74 } },
                new Player { Attr = new Attributes { Clearance = 78, Strength = 72, Positioning = 68, DecisionMaking = 72 } },
            };

            var weakMid = new List<Player> {
                new Player { Attr = new Attributes { Clearance = 60, Strength = 55, Positioning = 55, DecisionMaking = 55 } },
                new Player { Attr = new Attributes { Clearance = 58, Strength = 54, Positioning = 54, DecisionMaking = 54 } },
                new Player { Attr = new Attributes { Clearance = 57, Strength = 53, Positioning = 53, DecisionMaking = 53 } },
                new Player { Attr = new Attributes { Clearance = 56, Strength = 52, Positioning = 52, DecisionMaking = 52 } },
                new Player { Attr = new Attributes { Clearance = 55, Strength = 51, Positioning = 51, DecisionMaking = 51 } },
            };

            var rosters = new Dictionary<TeamId, List<Player>>
            {
                [new TeamId(1)] = strongMid,
                [new TeamId(2)] = weakMid
            };

            int trials = 200, homeWins = 0;
            for (int i = 0; i < trials; i++)
            {
                var result = MatchEngine.PlayMatch(
                    round: 1, homeId: new TeamId(1), awayId: new TeamId(2),
                    teams: teams, rosters: rosters, tactics: null,
                    weather: Weather.Clear, ground: new Ground(),
                    quarterSeconds: 2 * 60, rng: new DeterministicRandom(42 + i));

                if (result.HomeScore > result.AwayScore) homeWins++;
            }

            Assert.That(homeWins, Is.GreaterThan(trials * 0.55), "Strong midfield should win >55% in short sims");
        }
    }
}