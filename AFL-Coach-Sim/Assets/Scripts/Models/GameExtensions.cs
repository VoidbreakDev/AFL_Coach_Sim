// Assets/Scripts/Models/GameExtensions.cs
using System.Collections.Generic;
using AFLManager.Models;

namespace AFLManager.Models
{
    public static class GameExtensions
    {
        public static bool InvolvesTeam(this Match m, string teamId)
            => m != null && (m.HomeTeamId == teamId || m.AwayTeamId == teamId);

        // Stable id from position + teams
        public static string StableId(this Match m, SeasonSchedule schedule)
        {
            int idx = (schedule?.Fixtures ?? new List<Match>()).IndexOf(m);
            if (idx < 0) idx = 0;
            return $"{idx}_{m.HomeTeamId}_{m.AwayTeamId}";
        }

        // Utilities to derive "rounds" without a Date/RoundKey
        public static int IndexIn(this Match m, SeasonSchedule schedule)
            => (schedule?.Fixtures ?? new List<Match>()).IndexOf(m);

        public static int MatchesPerRound(int teamCount) => teamCount > 1 ? teamCount / 2 : 1;

        public static (int roundIndex, int start, int count) RoundBucketOf(this Match m, SeasonSchedule schedule, int teamCount)
        {
            int perRound = MatchesPerRound(teamCount);
            int idx = m.IndexIn(schedule);
            if (idx < 0) idx = 0;
            int round = idx / perRound;
            int start = round * perRound;
            return (round, start, perRound);
        }
    }
}

