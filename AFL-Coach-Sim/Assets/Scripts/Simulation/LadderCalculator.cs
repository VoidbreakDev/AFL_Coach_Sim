// Assets/Scripts/Simulation/LadderCalculator.cs
using System.Collections.Generic;
using System.Linq;
using AFLManager.Models;   // ← use the existing LadderEntry model class
using AFLManager.Simulation; // �� use the existing MatchSimulator type

namespace AFLManager.Simulation
{
    public static class LadderCalculator
    {
        /// <summary>
        /// Builds a ladder using the existing AFLManager.Models.LadderEntry type.
        /// </summary>
        public static List<LadderEntry> BuildShortLadder(
            List<string> teamIds,
            Dictionary<string, string> teamNames,
            List<MatchResult> results)
        {
            teamIds = teamIds?.Distinct().Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>();
            results = results ?? new List<MatchResult>();

            // Create one LadderEntry per team (using your existing model type)
            var map = teamIds.ToDictionary(
                id => id,
                id => new LadderEntry
                {
                    TeamId = id,
                    TeamName = (teamNames != null && teamNames.TryGetValue(id, out var nm) && !string.IsNullOrEmpty(nm)) ? nm : id,
                    Games = 0,
                    Wins = 0,
                    Draws = 0,
                    Losses = 0,
                    PointsFor = 0,
                    PointsAgainst = 0,
                    Points = 0
                });

            foreach (var r in results)
            {
                if (r == null) continue;
                if (!map.TryGetValue(r.HomeTeamId, out var H)) continue;
                if (!map.TryGetValue(r.AwayTeamId, out var A)) continue;

                H.Games++; A.Games++;
                H.PointsFor += r.HomeScore; H.PointsAgainst += r.AwayScore;
                A.PointsFor += r.AwayScore; A.PointsAgainst += r.HomeScore;

                if (r.HomeScore > r.AwayScore) { H.Wins++; H.Points += 4; A.Losses++; }
                else if (r.HomeScore < r.AwayScore) { A.Wins++; A.Points += 4; H.Losses++; }
                else { H.Draws++; A.Draws++; H.Points += 2; A.Points += 2; }
            }

            var list = map.Values
                .OrderByDescending(e => e.Points)
                .ThenByDescending(e =>
                {
                    // Some models store Percentage, others compute it. Compute here if needed.
                    var against = e.PointsAgainst <= 0 ? 1 : e.PointsAgainst;
                    return (e.PointsFor * 100f) / against;
                })
                .ThenBy(e => e.TeamName)
                .ToList();

            // No Rank on your LadderEntry model; rank is implied by order.
            // for (int i = 0; i < list.Count; i++) list[i].Rank = i + 1;
            return list;
        }
        // Compatibility overloads for older callers (e.g., MatchSimTest)
        public static List<LadderEntry> BuildLadder(
        List<string> teamIds,
        Dictionary<string, string> teamNames,
        List<MatchResult> results)
        {
            return BuildShortLadder(teamIds, teamNames, results);
        }

        public static List<LadderEntry> BuildLadder(
            List<Team> teams,
            List<MatchResult> results)
        {
            teams ??= new List<Team>();
            var ids = teams.Where(t => !string.IsNullOrEmpty(t.Id)).Select(t => t.Id).Distinct().ToList();
            var names = teams.ToDictionary(t => t.Id, t => string.IsNullOrEmpty(t.Name) ? t.Id : t.Name);
            return BuildShortLadder(ids, names, results);
        }

        // Accept IEnumerable to be flexible
        public static List<LadderEntry> BuildShortLadder(
        IEnumerable<string> teamIds,
        Dictionary<string, string> teamNames,
        IEnumerable<MatchResult> results)
        {
            var ids  = (teamIds  ?? Enumerable.Empty<string>()).ToList();
            var res  = (results  ?? Enumerable.Empty<MatchResult>()).ToList();
            return BuildShortLadder(ids, teamNames, res);
        }

        // Optional: keep the array call sites happy without Linq in the caller
        public static List<LadderEntry> BuildShortLadder(
            string[] teamIds,
            Dictionary<string, string> teamNames,
            MatchResult[] results)
        {
            return BuildShortLadder((IEnumerable<string>)teamIds, teamNames, (IEnumerable<MatchResult>)results);
        }

    }
}

