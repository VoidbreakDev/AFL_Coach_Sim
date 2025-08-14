// Assets/Scripts/UI/DashboardDataBuilder.cs
using System;
using System.Collections.Generic;
using System.Linq;
using AFLManager.Models;
using AFLManager.UI;
using AFLManager.Simulation;

public static class DashboardDataBuilder
{
    // NEW: pass teamCount so we can compute rounds
    public static void BindUpcoming(UpcomingMatchWidget card, SeasonSchedule schedule, List<MatchResult> results, string teamId, int teamCount)
    {
        if (!card || schedule?.Fixtures == null) return;

        var next = schedule.Fixtures
            .Where(m => m.InvolvesTeam(teamId))                                 // ok to use the Match extension
            .FirstOrDefault(m => !results.Any(r => r.MatchId == m.StableId(schedule)));

        if (next == null) { card.BindNoMatch(); return; }

        var bucket = next.RoundBucketOf(schedule, teamCount);
        var displayDate = System.DateTime.Today.AddDays(bucket.roundIndex * 7);
        card.Bind(next, displayDate);
    }

    public static void BindLastResult(LastResultWidget card, List<MatchResult> results, string teamId)
    {
        if (!card) return;

        var last = results
            .Where(r => r != null && (r.HomeTeamId == teamId || r.AwayTeamId == teamId))  // ← inline instead of r.InvolvesTeam(...)
            .OrderByDescending(r => r.SimulatedAtUtc)
            .FirstOrDefault();

        if (last == null) { card.BindNoResult(); return; }
        card.Bind(last);
    }

    public static void BindMiniLadder(LadderMiniWidget card, List<MatchResult> results, List<Team> teams)
    {
        if (!card) return;
        teams ??= new List<Team>();
        var ids = teams.Select(t => t.Id).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var names = teams.ToDictionary(t => t.Id, t => string.IsNullOrEmpty(t.Name) ? t.Id : t.Name);

        // LadderCalculator is now in AFLManager.Models (see below)
        var ladder = LadderCalculator.BuildShortLadder(ids, names, results);
        card.Render(ladder);
    }

    // Stubs
    public static void BindMorale(MoraleWidget card, string teamId)     { if (card) card.Bind(0.62f, "Good"); }
    public static void BindInjuries(InjuriesWidget card, string teamId) { if (card) card.BindNone(); }
    public static void BindTraining(TrainingWidget card, string teamId) { if (card) card.Bind("12 Apr 2025"); }
    public static void BindBudget(BudgetWidget card, string teamId)     { if (card) card.Bind("$1,200,000"); }
    public static void BindContracts(ContractsWidget card, string teamId){ if (card) card.Bind("3 Expiring"); }
}
