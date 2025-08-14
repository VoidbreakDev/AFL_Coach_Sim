// File: Assets/Scripts/Tests/MatchSimTest.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Managers;     // SaveLoadManager
using AFLManager.Simulation;   // MatchSimulator, LadderCalculator

public class MatchSimTest : MonoBehaviour
{
    [Header("Test Setup")]
    public int fallbackTeamCount = 8;   // used if no team_*.json are found
    public int matchesToSimulate = 12;  // how many random matches to simulate for the test
    public int seed = 12345;

    void Start()
    {
        // 1) Try to load real teams from disk
        var teams = LoadAllTeamsFromDisk();

        // Build teamIds/teamNames from real teams (read-only Ids are fine)
        List<string> teamIds;
        Dictionary<string, string> teamNames;

        if (teams.Count > 0)
        {
            teamIds = teams.Where(t => !string.IsNullOrEmpty(t.Id))
                           .Select(t => t.Id).Distinct().ToList();

            teamNames = teams.Where(t => !string.IsNullOrEmpty(t.Id))
                             .ToDictionary(t => t.Id, t => string.IsNullOrEmpty(t.Name) ? t.Id : t.Name);
        }
        else
        {
            // 2) No saved teams? Create IDs/names ONLY (no Team instances)
            Debug.LogWarning("[MatchSimTest] No saved teams found; using placeholder IDs.");
            int n = Mathf.Max(2, fallbackTeamCount);
            teamIds = new List<string>(n);
            teamNames = new Dictionary<string, string>(n);
            for (int i = 0; i < n; i++)
            {
                var id = $"TEAM_{i+1:00}";
                teamIds.Add(id);
                teamNames[id] = $"Team {i+1:00}";
            }
        }

        // 3) Simulate a handful of matches
        var prng = new System.Random(seed);
        var resultsList = new List<MatchResult>();

        for (int i = 0; i < matchesToSimulate; i++)
        {
            var a = teamIds[prng.Next(teamIds.Count)];
            string b;
            do { b = teamIds[prng.Next(teamIds.Count)]; } while (b == a);

            string matchId = $"TEST_{i}_{a}_{b}";

            var result = MatchSimulator.SimulateMatch(
                matchId, "TEST",
                homeTeamId: a,
                awayTeamId: b,
                ratingProvider: new MatchSimulator.DefaultRatingProvider(
                    id => TeamAverage(teams, id),                // falls back to 60f if not found
                    id => new[] { $"{id}_P1", $"{id}_P2", $"{id}_P3", $"{id}_P4", $"{id}_P5", $"{id}_P6" }
                ),
                seed: matchId.GetHashCode()
            );

            resultsList.Add(result);
        }

        // 4) Build and dump the ladder
        var ladder = LadderCalculator.BuildShortLadder(teamIds, teamNames, resultsList);

        Debug.Log($"[MatchSimTest] Ladder rows: {ladder.Count}");
        for (int i = 0; i < ladder.Count; i++)
        {
            var e = ladder[i];
            float pct = e.PointsAgainst <= 0 ? 0f : (e.PointsFor * 100f / e.PointsAgainst);
            Debug.Log($"{i+1,2}. {e.TeamName,-16}  GP:{e.Games,2}  Pts:{e.Points,2}  PF:{e.PointsFor,3}  PA:{e.PointsAgainst,3}  %:{pct:0.0}");
        }
    }

    // --- helpers ---

    static float TeamAverage(List<Team> teams, string teamId)
    {
        // If we have real teams loaded, use their roster averages; otherwise return a neutral baseline
        var t = teams?.FirstOrDefault(x => x.Id == teamId);
        if (t == null || t.Roster == null || t.Roster.Count == 0) return 60f;
        float sum = 0f; int n = 0;
        foreach (var p in t.Roster) { sum += p?.Stats?.GetAverage() ?? 60f; n++; }
        return n == 0 ? 60f : sum / n;
    }

    static List<Team> LoadAllTeamsFromDisk()
    {
        var list = new List<Team>();
        var dir = Application.persistentDataPath;
        if (!Directory.Exists(dir)) return list;

        foreach (var file in Directory.GetFiles(dir, "team_*.json"))
        {
            var key = Path.GetFileNameWithoutExtension(file).Replace("team_", "");
            var t = SaveLoadManager.LoadTeam(key);
            if (t != null)
            {
                t.Roster ??= new List<Player>();
                list.Add(t);
            }
        }
        return list;
    }
}
