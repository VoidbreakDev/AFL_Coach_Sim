// Assets/Scripts/Game/PreMatchFlow.cs
using UnityEngine;
using System.Linq;
using AFLManager.Models;
using System.Collections.Generic;

public static class PreMatchFlow
{
    public static void Begin(TeamMainScreenManager root, string teamId, SeasonSchedule schedule)
    {
        var results = SaveLoadManager.LoadAllResults();
        var next = schedule.Fixtures
            .Where(m => m.Involves(teamId))
            .FirstOrDefault(m => !results.Any(r => r.MatchId == m.GetStableId(schedule)));

        if (next == null) { Debug.Log("No upcoming match."); return; }

        // TODO: open Panel_Tactics/PreMatch where user sets lineup & tactics
        // For MVP, jump straight to sim:
        SimulateYourMatchThenAutosimRound(root, schedule, next, teamId);
    }

    static void SimulateYourMatchThenAutosimRound(TeamMainScreenManager root, SeasonSchedule schedule, Match yourMatch, string teamId)
    {
        // simulate your match (could swap to a dedicated scene later)
        var matchId = yourMatch.GetStableId(schedule);
        var result = MatchSimulator.SimulateMatch(
            matchId, "R?", yourMatch.HomeTeamId, yourMatch.AwayTeamId,
            new MatchSimulator.DefaultRatingProvider(
                id => 60f, // TODO: plug your team averages here
                id => new []{"P1","P2","P3","P4","P5","P6"}),
            seed: matchId.GetHashCode()
        );
        SaveLoadManager.SaveMatchResult(result);

        // autosim rest of round (same round date)
        var date = yourMatch.Date.Date;
        var roundMatches = schedule.Fixtures.Where(m => m.Date.Date == date).ToList();
        foreach (var m in roundMatches)
        {
            if (m == yourMatch) continue;
            var mid = m.GetStableId(schedule);
            if (SaveLoadManager.LoadAllResults().Any(r => r.MatchId == mid)) continue;

            var rr = MatchSimulator.SimulateMatch(
                mid, "R?", m.HomeTeamId, m.AwayTeamId,
                new MatchSimulator.DefaultRatingProvider(
                    id => 60f, 
                    id => new []{"P1","P2","P3","P4","P5","P6"}),
                seed: mid.GetHashCode()
            );
            SaveLoadManager.SaveMatchResult(rr);
        }

        root.RefreshDashboard();
    }
}
