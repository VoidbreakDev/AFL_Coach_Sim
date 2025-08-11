// Assets/Scripts/UI/DashboardDataBuilder.cs
using System.Linq;
using System.Collections.Generic;
using AFLManager.Models;

public static class DashboardDataBuilder
{
    public static void BindUpcoming(UpcomingMatchWidget card, SeasonSchedule schedule, List<MatchResult> results, string teamId)
    {
        var next = schedule.Fixtures
            .Where(m => m.Involves(teamId))
            .FirstOrDefault(m => !results.Any(r => r.MatchId == m.GetStableId(schedule)));

        if (next == null) { card.BindNoMatch(); return; }
        card.Bind(next, /*date*/ next.Date);
    }

    public static void BindLastResult(LastResultWidget card, List<MatchResult> results, string teamId)
    {
        var last = results.Where(r => r.Involves(teamId))
                          .OrderByDescending(r => r.SimulatedAtUtc)
                          .FirstOrDefault();
        if (last == null) { card.BindNoResult(); return; }
        card.Bind(last);
    }

    public static void BindMiniLadder(LadderMiniWidget card, List<MatchResult> results, List<Team> teams)
    {
        var ids = teams.Select(t => t.Id).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var nameMap = teams.ToDictionary(t => t.Id, t => string.IsNullOrEmpty(t.Name) ? t.Id : t.Name);
        var ladder = LadderCalculator.BuildShortLadder(ids, nameMap, results);
        card.Render(ladder);
    }

    // Stubs – wire to your data later
    public static void BindMorale(MoraleWidget card, string teamId)     => card.Bind(0.62f, "Good");
    public static void BindInjuries(InjuriesWidget card, string teamId) => card.BindNone();
    public static void BindTraining(TrainingWidget card, string teamId) => card.Bind("12 Apr 2025");
    public static void BindBudget(BudgetWidget card, string teamId)     => card.Bind("$1,200,000");
    public static void BindContracts(ContractsWidget card, string teamId)=> card.Bind("3 Expiring");
}
