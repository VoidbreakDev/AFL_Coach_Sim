// File: Assets/Scripts/Managers/SeasonScreenManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Managers;   // SaveLoadManager, SeasonScheduler
using AFLManager.UI;         // MatchEntryUI
using AFLManager.Simulation;

namespace AFLManager.Managers
{
    public class SeasonScreenManager : MonoBehaviour
    {
        [Header("Season Settings")]
        public int daysBetweenMatches = 7;

        [Header("UI References")]
        public GameObject matchEntryPrefab;     // MatchEntryPanel prefab with MatchEntryUI
        public Transform fixtureContainer;      // ScrollView -> Viewport -> Content
        [SerializeField] private LadderMiniWidget miniLadderWidget;

        private SeasonSchedule schedule;
        private List<Team> leagueTeams;
        private Dictionary<string, string> teamNameLookup;
        private string coachKey;

        private void Start()
        {
            coachKey = PlayerPrefs.GetString("CoachName", "DefaultCoach");
            Debug.Log($"[SeasonScreen] Coach key: {coachKey}");
            Debug.Log($"[SeasonScreen] persistentDataPath: {Application.persistentDataPath}");

            leagueTeams = LoadAllTeams();
            BuildTeamNameLookup();

            schedule = SaveLoadManager.LoadSchedule("testSeason");
            if (schedule == null)
            {
                Debug.Log("[SeasonScreen] No saved schedule—generating new one");
                schedule = SeasonScheduler.GenerateSeason(leagueTeams, DateTime.Today, daysBetweenMatches);
                if (schedule != null && schedule.Fixtures?.Count > 0)
                {
                    SaveLoadManager.SaveSchedule("testSeason", schedule);
                    Debug.Log($"[SeasonScreen] Saved new schedule with {schedule.Fixtures.Count} fixtures");
                }
            }

            if (schedule?.Fixtures == null)
            {
                Debug.LogError("[SeasonScreen] Schedule is null or has no fixtures.");
                return;
            }

            Debug.Log($"[SeasonScreen] Using schedule with {schedule.Fixtures.Count} fixtures");
            RenderSchedule();          // builds fixture list
            RebuildMiniLadder();       // build mini ladder ONCE after rows exist
        }

        private List<Team> LoadAllTeams()
        {
            var teams = new List<Team>();
            var teamFiles = Directory.GetFiles(Application.persistentDataPath, "team_*.json");
            Debug.Log($"[SeasonScreen] Found {teamFiles.Length} team files");
            foreach (var file in teamFiles)
            {
                var key = Path.GetFileNameWithoutExtension(file).Replace("team_", "");
                var team = SaveLoadManager.LoadTeam(key);
                if (team != null)
                {
                    team.Roster ??= new List<Player>();
                    teams.Add(team);
                }
                else
                {
                    Debug.LogWarning($"[SeasonScreen] Failed to load team with key: '{key}'");
                }
            }
            Debug.Log($"[SeasonScreen] Loaded {teams.Count} teams from JSON");
            return teams;
        }

        private void BuildTeamNameLookup()
        {
            teamNameLookup = new Dictionary<string, string>();
            foreach (var t in leagueTeams)
                if (!string.IsNullOrEmpty(t.Id) && !string.IsNullOrEmpty(t.Name))
                    teamNameLookup[t.Id] = t.Name;
            Debug.Log($"[SeasonScreen] Built team name lookup with {teamNameLookup.Count} entries");
        }

        private void RenderSchedule()
        {
            foreach (Transform c in fixtureContainer) Destroy(c.gameObject);

            foreach (var match in schedule.Fixtures)
            {
                var go = Instantiate(matchEntryPrefab, fixtureContainer, false);
                var ui = go.GetComponent<MatchEntryUI>();
                if (!ui) { Debug.LogError("[SeasonScreen] MatchEntryUI missing on prefab."); continue; }
                ui.Initialize(match, SimulateMatch, teamNameLookup);
            }
        }

        private string GetMatchId(Match m)
        {
            int index = schedule.Fixtures.IndexOf(m);
            return $"{index}_{m.HomeTeamId}_{m.AwayTeamId}";
        }

        private void SimulateMatch(Match match)
        {
            if (match == null) { Debug.LogError("[SeasonScreen] SimulateMatch null"); return; }

            string matchId = GetMatchId(match);
            var result = MatchSimulator.SimulateMatch(
                matchId, "R?", match.HomeTeamId, match.AwayTeamId,
                new MatchSimulator.DefaultRatingProvider(
                    id => GetTeamAverage(id),
                    id => new[] { $"{id}_P1", $"{id}_P2", $"{id}_P3", $"{id}_P4", $"{id}_P5", $"{id}_P6" }),
                seed: matchId.GetHashCode());

            match.Result = $"{result.HomeScore}–{result.AwayScore}";
            SaveLoadManager.SaveMatchResult(result);
            RebuildMiniLadder();
        }

        private float GetTeamAverage(string teamId)
        {
            var team = leagueTeams.Find(t => t.Id == teamId);
            if (team == null || team.Roster == null || team.Roster.Count == 0) return 0f;
            float sum = 0f;
            foreach (var p in team.Roster) sum += p?.Stats?.GetAverage() ?? 0f;
            return sum / team.Roster.Count;
        }

        private void RebuildMiniLadder()
        {
            if (!miniLadderWidget) { Debug.LogWarning("[SeasonScreen] miniLadderWidget not assigned."); return; }

            var results = SaveLoadManager.LoadAllResults();
            Debug.Log($"[SeasonScreen] RebuildMiniLadder: loaded {results.Count} results.");

            var teamIds = (leagueTeams ?? new List<Team>())
                .Where(t => !string.IsNullOrEmpty(t.Id))
                .Select(t => t.Id)
                .Distinct()
                .ToList();

            if (teamIds.Count == 0)
            {
                teamIds = results
                    .SelectMany(r => new[] { r.HomeTeamId, r.AwayTeamId })
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();
                Debug.LogWarning($"[SeasonScreen] No leagueTeams found; derived {teamIds.Count} team IDs from results.");
            }

            var teamNames = new Dictionary<string, string>();
            if (leagueTeams != null)
                foreach (var t in leagueTeams)
                    if (!string.IsNullOrEmpty(t.Id))
                        teamNames[t.Id] = string.IsNullOrEmpty(t.Name) ? t.Id : t.Name;

            var ladder = LadderCalculator.BuildShortLadder(teamIds, teamNames, results);
            Debug.Log($"[SeasonScreen] Ladder entries: {ladder.Count}");
            miniLadderWidget.Render(ladder);
        }
    }
}
