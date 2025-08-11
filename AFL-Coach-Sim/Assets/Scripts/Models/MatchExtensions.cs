// Assets/Scripts/Models/MatchExtensions.cs
using AFLManager.Models;
using System;

public static class MatchExtensions
{
    public static bool Involves(this Match m, string teamId)
        => m.HomeTeamId == teamId || m.AwayTeamId == teamId;

    public static string GetStableId(this Match m, SeasonSchedule schedule)
    {
        int i = schedule.Fixtures.IndexOf(m);
        return $"{i}_{m.HomeTeamId}_{m.AwayTeamId}";
    }
}