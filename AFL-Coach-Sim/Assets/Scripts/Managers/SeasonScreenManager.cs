// File: Assets/Scripts/Managers/SeasonScreenManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AFLManager.Models;
using AFLManager.Managers;  // for SaveLoadManager, SeasonScheduler
using AFLManager.UI;        // for MatchEntryUI

namespace AFLManager.Managers
{
    public class SeasonScreenManager : MonoBehaviour
    {
        [Header("Season Settings")]
        public int daysBetweenMatches = 7;

        [Header("UI References")]
        public GameObject matchEntryPrefab;  // Assign your MatchEntryPanel prefab here
        public Transform fixtureContainer;  // Assign Scroll View → Viewport → Content

        private SeasonSchedule schedule;
        private List<Team> leagueTeams;
        private Dictionary<string, string> teamNameLookup;
        private string coachKey;

        void Start()
        {
            coachKey = PlayerPrefs.GetString("CoachName", "DefaultCoach");
            Debug.Log($"[SeasonScreen] Coach key: {coachKey}");
            Debug.Log($"[SeasonScreen] Persistent data path: {Application.persistentDataPath}");

            // Load all saved teams from JSON
            leagueTeams = new List<Team>();
            var teamFiles = Directory.GetFiles(Application.persistentDataPath, "team_*.json");
            Debug.Log($"[SeasonScreen] Found {teamFiles.Length} team files");
            
            foreach (var file in teamFiles)
            {
                Debug.Log($"[SeasonScreen] Processing file: {Path.GetFileName(file)}");
                var key = Path.GetFileNameWithoutExtension(file).Replace("team_", "");
                Debug.Log($"[SeasonScreen] Extracted key: '{key}'");
                var team = SaveLoadManager.LoadTeam(key);
                if (team != null) 
                {
                    Debug.Log($"[SeasonScreen] Loaded team: {team.Name} (ID: {team.Id})");
                    leagueTeams.Add(team);
                }
                else
                {
                    Debug.LogWarning($"[SeasonScreen] Failed to load team with key: '{key}'");
                }
            }

            Debug.Log($"[SeasonScreen] Loaded {leagueTeams.Count} teams from JSON");

            // Create team name lookup dictionary
            BuildTeamNameLookup();

            // attempt load
            schedule = SaveLoadManager.LoadSchedule("testSeason");
            if (schedule == null)
            {
                Debug.Log("[SeasonScreen] No saved schedule—generating new one");
                schedule = SeasonScheduler.GenerateSeason(leagueTeams, DateTime.Today, daysBetweenMatches);
                // Save the newly generated schedule
                if (schedule != null && schedule.Fixtures.Count > 0)
                {
                    SaveLoadManager.SaveSchedule("testSeason", schedule);
                    Debug.Log($"[SeasonScreen] Saved new schedule with {schedule.Fixtures.Count} fixtures");
                }
            }
            Debug.Log($"[SeasonScreen] Using schedule with {schedule.Fixtures.Count} fixtures");

            RenderSchedule();
        }

        private void RenderSchedule()
        {
            Debug.Log($"[SeasonScreen] Rendering {schedule.Fixtures.Count} fixture entries");
            foreach (Transform c in fixtureContainer) Destroy(c.gameObject);
            foreach (var match in schedule.Fixtures)
            {
                var go = Instantiate(matchEntryPrefab, fixtureContainer);
                go.GetComponent<MatchEntryUI>()
                  .Initialize(match, SimulateMatch, teamNameLookup);
            }
        }

        private void BuildTeamNameLookup()
        {
            teamNameLookup = new Dictionary<string, string>();
            foreach (var team in leagueTeams)
            {
                if (!string.IsNullOrEmpty(team.Id) && !string.IsNullOrEmpty(team.Name))
                {
                    teamNameLookup[team.Id] = team.Name;
                    Debug.Log($"[SeasonScreen] Added team lookup: {team.Id} -> {team.Name}");
                }
            }
            Debug.Log($"[SeasonScreen] Built team name lookup with {teamNameLookup.Count} entries");
        }

        // ← This method must exist to match the delegate passed to Initialize()
        private void SimulateMatch(Match match)
        {
            // Simple win/lose stub based on average stats
            float homeAvg = GetTeamAverage(match.HomeTeamId);
            float awayAvg = GetTeamAverage(match.AwayTeamId);
            bool homeWins = (homeAvg + UnityEngine.Random.Range(-5f, 5f))
                         > (awayAvg + UnityEngine.Random.Range(-5f, 5f));

            // Generate scores
            int homeScore = UnityEngine.Random.Range(50, 100);
            int awayScore = UnityEngine.Random.Range(50, 100);
            if (homeWins && homeScore <= awayScore)
                homeScore = awayScore + UnityEngine.Random.Range(1, 10);
            if (!homeWins && awayScore <= homeScore)
                awayScore = homeScore + UnityEngine.Random.Range(1, 10);

            match.Result = $"{homeScore}–{awayScore}";
        }

        private float GetTeamAverage(string teamId)
        {
            var team = leagueTeams.Find(t => t.Id == teamId);
            if (team == null || team.Roster.Count == 0) return 0f;

            float sum = 0f;
            foreach (var p in team.Roster)
                sum += p.Stats.GetAverage();
            return sum / team.Roster.Count;
        }
    }
}
