// Engine/Ladder/LadderCalculator.cs
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Ladder
{
    public static class LadderCalculator
    {
        public static List<LadderEntryDTO> BuildLadder(IEnumerable<MatchResultDTO> results)
        {
            var dict = new Dictionary<int, LadderEntryDTO>();

            void Ensure(TeamId t)
            {
                if (!dict.ContainsKey(t.Value))
                    dict[t.Value] = new LadderEntryDTO { Team = t };
            }

            foreach (var r in results)
            {
                Ensure(r.Home); Ensure(r.Away);
                var h = dict[r.Home.Value];
                var a = dict[r.Away.Value];

                h.Played++; a.Played++;
                h.PointsFor += r.HomeScore; h.PointsAgainst += r.AwayScore;
                a.PointsFor += r.AwayScore; a.PointsAgainst += r.HomeScore;

                if (r.HomeScore > r.AwayScore) { h.Wins++; a.Losses++; }
                else if (r.HomeScore < r.AwayScore) { a.Wins++; h.Losses++; }
                else { h.Draws++; a.Draws++; }
            }

            // Sort by premiership points, then percentage, then points for
            return dict.Values
                .OrderByDescending(e => e.PremiershipPoints)
                .ThenByDescending(e => e.Percentage)
                .ThenByDescending(e => e.PointsFor)
                .ToList();
        }
    }
}