// Engine/Scheduling/RoundRobinScheduler.cs
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Scheduling
{
    /// <summary> Simple Berger-table style round robin with optional double round. </summary>
    public static class RoundRobinScheduler
    {
        public static List<(int round, TeamId home, TeamId away)> Build(IReadOnlyList<TeamId> teams, bool doubleRound)
        {
            var list = new List<(int, TeamId, TeamId)>();
            var n = teams.Count;
            var evenTeams = new List<TeamId>(teams);

            if (n % 2 == 1) evenTeams.Add(new TeamId(-1)); // bye marker
            int m = evenTeams.Count;

            var arr = evenTeams.ToArray();
            int rounds = m - 1;
            for (int r = 0; r < rounds; r++)
            {
                for (int i = 0; i < m / 2; i++)
                {
                    var t1 = arr[i];
                    var t2 = arr[m - 1 - i];
                    if (t1.Value != -1 && t2.Value != -1)
                        list.Add((r + 1, t1, t2)); // home t1 vs away t2
                }

                // rotate except first
                var first = arr[0];
                var last = arr[m - 1];
                for (int i = m - 1; i > 1; i--) arr[i] = arr[i - 1];
                arr[1] = last;
                arr[0] = first;
            }

            if (doubleRound)
            {
                var extra = new List<(int, TeamId, TeamId)>(list.Count);
                int startRound = rounds + 1;
                foreach (var (round, home, away) in list)
                    extra.Add((startRound + round - 1, away, home));
                list.AddRange(extra);
            }

            return list;
        }
    }
}