// Assets/Scripts/Models/ResultExtensions.cs
using AFLManager.Models;

public static class ResultExtensions
{
    public static bool Involves(this MatchResult r, string teamId)
        => r.HomeTeamId == teamId || r.AwayTeamId == teamId;
}