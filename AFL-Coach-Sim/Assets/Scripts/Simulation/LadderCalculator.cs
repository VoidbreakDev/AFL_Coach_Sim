using System.Collections.Generic;
using System.Linq;
using AFLManager.Models;

namespace AFLManager.Simulation
{
    public static class LadderCalculator
    {
        public const int WinPoints = 4;
        public const int DrawPoints = 2;

        private static string NameOrId(IDictionary<string, string> teamNames, string teamId)
        {
            if (teamNames == null) return teamId;
            string name;
            return teamNames.TryGetValue(teamId, out name) ? name : teamId;
        }

        public static List<LadderEntry> BuildLadder(IEnumerable<string> teamIds,
                                                    IDictionary<string, string> teamNames,
                                                    IEnumerable<MatchResult> results)
        {
            var table = teamIds.ToDictionary(
                id => id,
                id => new LadderEntry { TeamId = id, TeamName = NameOrId(teamNames, id) });

            foreach (var r in results)
            {
                if (!table.ContainsKey(r.HomeTeamId))
                    table[r.HomeTeamId] = new LadderEntry { TeamId = r.HomeTeamId, TeamName = NameOrId(teamNames, r.HomeTeamId) };
                if (!table.ContainsKey(r.AwayTeamId))
                    table[r.AwayTeamId] = new LadderEntry { TeamId = r.AwayTeamId, TeamName = NameOrId(teamNames, r.AwayTeamId) };

                var h = table[r.HomeTeamId];
                var a = table[r.AwayTeamId];

                h.Games++; a.Games++;
                h.PointsFor += r.HomeScore; h.PointsAgainst += r.AwayScore;
                a.PointsFor += r.AwayScore; a.PointsAgainst += r.HomeScore;

                if (r.HomeScore > r.AwayScore) { h.Wins++; a.Losses++; h.Points += WinPoints; }
                else if (r.HomeScore < r.AwayScore) { a.Wins++; h.Losses++; a.Points += WinPoints; }
                else { h.Draws++; a.Draws++; h.Points += DrawPoints; a.Points += DrawPoints; }
            }

            return table.Values
                        .OrderByDescending(e => e.Points)
                        .ThenByDescending(e => e.Percentage)
                        .ThenByDescending(e => e.PointsFor)
                        .ThenBy(e => e.TeamName)
                        .ToList();
        }

        public static List<LadderEntry> BuildShortLadder(IEnumerable<string> teamIds,
                                                         IDictionary<string, string> teamNames,
                                                         IEnumerable<MatchResult> results)
            => BuildLadder(teamIds, teamNames, results);
    }
}
