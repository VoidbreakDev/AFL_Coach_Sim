using NUnit.Framework;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Persistence;
using WeatherCondition = AFLCoachSim.Core.Engine.Match.Weather.Weather;

namespace AFLCoachSim.Core.Tests
{
    public class M3_InjuryRiskTests
    {
        private static List<Player> MakeRoster(int endurance, int durability)
        {
            var list = new List<Player>();
            for (int i = 0; i < 26; i++)
            {
                list.Add(new Player
                {
                    Endurance = endurance,
                    Durability = durability,
                    Attr = new Attributes { WorkRate = 60, DecisionMaking = 60, Kicking = 60, Marking = 60, Clearance = 60, Strength = 60, Positioning = 60, Tackling = 60 }
                });
            }
            return list;
        }

        [Test]
        public void Low_Durability_Increases_Injury_Events()
        {
            var teams = new Dictionary<TeamId, Team>
            {
                [new TeamId(1)] = new Team(new TeamId(1), "Home", 60, 60),
                [new TeamId(2)] = new Team(new TeamId(2), "Away", 60, 60)
            };

            var good = MakeRoster(80, 90);
            var poor = MakeRoster(80, 40);

            var rostersGood = new Dictionary<TeamId, List<Player>> { [new TeamId(1)] = good, [new TeamId(2)] = good };
            var rostersPoor = new Dictionary<TeamId, List<Player>> { [new TeamId(1)] = poor, [new TeamId(2)] = poor };

            // run many short sims and proxy "injury count" by #players ending with Condition < 60
            int injProxyGood = 0, injProxyPoor = 0, trials = 60;
            var injuryManager = new InjuryManager(new JsonInjuryRepository());

            for (int i = 0; i < trials; i++)
            {
                var res1 = MatchEngine.PlayMatch(1, new TeamId(1), new TeamId(2), teams, injuryManager, rostersGood,
                    null, WeatherCondition.Clear, new Ground(), 8 * 60, new DeterministicRandom(10 + i));

                var res2 = MatchEngine.PlayMatch(1, new TeamId(1), new TeamId(2), teams, injuryManager, rostersPoor,
                    null, WeatherCondition.Clear, new Ground(), 8 * 60, new DeterministicRandom(200 + i));

                // crude proxy: worse durability should lead to lower average condition at end
                injProxyGood += AverageCondition(good);
                injProxyPoor += AverageCondition(poor);
            }

            Assert.That(injProxyPoor, Is.LessThan(injProxyGood));
        }

        private static int AverageCondition(List<Player> roster)
        {
            if (roster.Count == 0) return 0;
            int sum = 0;
            int cnt = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                sum += roster[i].Condition;
                cnt++;
            }
            return sum / cnt;
        }
    }
}