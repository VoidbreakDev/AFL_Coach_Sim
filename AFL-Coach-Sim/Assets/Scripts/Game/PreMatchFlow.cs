// Assets/Scripts/Game/PreMatchFlow.cs
using System.Collections.Generic;
using System.Linq;
using AFLManager.Models;
using AFLManager.Managers;
using AFLManager.Simulation; // if your MatchSimulator is under this
using UnityEngine;

public static class PreMatchFlow
{
    public static void Begin(TeamMainScreenManager root, string teamId, SeasonSchedule schedule, List<Team> teams)
    {
        var results = SaveLoadManager.LoadAllResults();
        var next = schedule.Fixtures
            .Where(m => m.InvolvesTeam(teamId))
            .FirstOrDefault(m => !results.Any(r => r.MatchId == m.StableId(schedule)));

        if (next == null) { Debug.Log("[PreMatch] No upcoming match."); return; }

        SimulateYourMatchThenAutosimRound(root, schedule, next, teams);
    }

    static void SimulateYourMatchThenAutosimRound(TeamMainScreenManager root, SeasonSchedule schedule, Match yourMatch, List<Team> teams)
    {
        var matchId = yourMatch.StableId(schedule);
        var result = MatchSimulator.SimulateMatch(
            matchId, "R?", yourMatch.HomeTeamId, yourMatch.AwayTeamId,
            new MatchSimulator.DefaultRatingProvider(
                id => TeamAverage(teams, id),
                id => new[] { "P1","P2","P3","P4","P5","P6" }),
            seed: matchId.GetHashCode()
        );
        SaveLoadManager.SaveMatchResult(result);

        // Autosim matches in the same round bucket
        int perRound = GameExtensions.MatchesPerRound(teams?.Count ?? 0);
        var bucket = yourMatch.RoundBucketOf(schedule, teams?.Count ?? 0);
        var existing = SaveLoadManager.LoadAllResults().Select(r => r.MatchId).ToHashSet();

        for (int i = 0; i < bucket.count; i++)
        {
            int idx = bucket.start + i;
            if (idx < 0 || idx >= schedule.Fixtures.Count) continue;
            var m = schedule.Fixtures[idx];
            if (m == yourMatch) continue;

            var mid = m.StableId(schedule);
            if (existing.Contains(mid)) continue;

            var rr = MatchSimulator.SimulateMatch(
                mid, "R?", m.HomeTeamId, m.AwayTeamId,
                new MatchSimulator.DefaultRatingProvider(
                    id => TeamAverage(teams, id),
                    id => new[] { "P1","P2","P3","P4","P5","P6" }),
                seed: mid.GetHashCode()
            );
            SaveLoadManager.SaveMatchResult(rr);
        }

        root.RefreshDashboard();
    }

    static float TeamAverage(List<Team> teams, string teamId)
    {
        var t = teams?.FirstOrDefault(x => x.Id == teamId);
        if (t == null || t.Roster == null || t.Roster.Count == 0) return 60f;
        float sum = 0f; int n = 0;
        foreach (var p in t.Roster) { sum += p?.Stats?.GetAverage() ?? 60f; n++; }
        return n == 0 ? 60f : sum / n;
    }
}
