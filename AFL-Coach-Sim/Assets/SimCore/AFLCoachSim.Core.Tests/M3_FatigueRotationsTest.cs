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
    public class M3_FatigueRotationTests
    {
        private static List<Player> MakeUniformRoster(int rating, int count)
        {
            var list = new List<Player>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new Player
                {
                    Name = "P" + i,
                    Endurance = rating,
                    Durability = 60,
                    Attr = new Attributes
                    {
                        WorkRate = rating,
                        DecisionMaking = rating,
                        Kicking = rating,
                        Marking = rating,
                        Clearance = rating,
                        Strength = rating,
                        Positioning = rating,
                        Tackling = rating
                    }
                });
            }
            return list;
        }

        [Test]
        public void More_Rotations_Tends_To_Preserve_Scoring()
        {
            var teams = new Dictionary<TeamId, Team>
            {
                [new TeamId(1)] = new Team(new TeamId(1), "Home", 60, 60),
                [new TeamId(2)] = new Team(new TeamId(2), "Away", 60, 60)
            };

            var roster = MakeUniformRoster(70, 30);
            var rosters = new Dictionary<TeamId, List<Player>>
            {
                [new TeamId(1)] = roster,
                [new TeamId(2)] = roster
            };

            var lowRot = new TeamTactics { TargetInterchangesPerGame = 30, ContestBias = 50, KickingRisk = 50 };
            var highRot = new TeamTactics { TargetInterchangesPerGame = 90, ContestBias = 50, KickingRisk = 50 };

            int trials = 40;
            int lowTotal = 0, highTotal = 0;

            for (int i = 0; i < trials; i++)
            {
                var resLow = MatchEngine.PlayMatch(1, new TeamId(1), new TeamId(2), teams, rosters,
                    new Dictionary<TeamId, TeamTactics> { [new TeamId(1)] = lowRot, [new TeamId(2)] = lowRot },
                    Weather.Clear, new Ground(), 10 * 60, new DeterministicRandom(100 + i));
                lowTotal += resLow.HomeScore + resLow.AwayScore;

                var resHigh = MatchEngine.PlayMatch(1, new TeamId(1), new TeamId(2), teams, rosters,
                    new Dictionary<TeamId, TeamTactics> { [new TeamId(1)] = highRot, [new TeamId(2)] = highRot },
                    Weather.Clear, new Ground(), 10 * 60, new DeterministicRandom(200 + i));
                highTotal += resHigh.HomeScore + resHigh.AwayScore;
            }

            // With more rotations, fatigue is mitigated → slightly higher total scoring expected
            Assert.That(highTotal, Is.GreaterThan(lowTotal));
        }
    }
}