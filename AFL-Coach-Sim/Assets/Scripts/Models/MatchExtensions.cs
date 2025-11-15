// Assets/Scripts/Models/MatchExtensions.cs
// Note: Involves() and GetStableId() are already defined in ModelExtensions (Extensions.cs)
using AFLManager.Models;
using System;

public static class MatchExtensions
{
    // Alias for consistency - uses ModelExtensions.Involves()
    public static bool InvolvesTeam(this Match m, string teamId)
        => ModelExtensions.Involves(m, teamId);

    // Alias for consistency - uses ModelExtensions.GetStableId()
    public static string StableId(this Match m, SeasonSchedule schedule)
        => ModelExtensions.GetStableId(m, schedule);
}
