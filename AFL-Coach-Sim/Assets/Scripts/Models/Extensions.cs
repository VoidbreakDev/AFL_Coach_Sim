// Assets/Scripts/Models/Extensions.cs
using System.Linq;
using AFLManager.Models;

public static class ModelExtensions
{
    public static bool Involves(this Match m, string teamId)
        => m != null && (m.HomeTeamId == teamId || m.AwayTeamId == teamId);

    public static string GetStableId(this Match m, SeasonSchedule schedule)
    {
        int idx = (schedule?.Fixtures ?? new System.Collections.Generic.List<Match>()).IndexOf(m);
        if (idx < 0) idx = 0;
        return $"{idx}_{m.HomeTeamId}_{m.AwayTeamId}";
    }

    public static bool Involves(this MatchResult r, string teamId)
        => r != null && (r.HomeTeamId == teamId || r.AwayTeamId == teamId);
}
