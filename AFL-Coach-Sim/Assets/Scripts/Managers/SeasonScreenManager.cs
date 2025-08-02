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
        private string coachKey;

        void Start()
        {
            coachKey = PlayerPrefs.GetString("CoachName", "DefaultCoach");

            // Load all saved teams from JSON
            leagueTeams = new List<Team>();
            foreach (var file in Directory.GetFiles(Application.persistentDataPath, "team_*.json"))
            {
                var key = Path.GetFileNameWithoutExtension(file).Replace("team_", "");
                var team = SaveLoadManager.LoadTeam(key);
                if (team != null) leagueTeams.Add(team);
            }

            Debug.Log($"[SeasonScreen] Loaded {leagueTeams.Count} teams from JSON");

            // attempt load
            schedule = SaveLoadManager.LoadSchedule("testSeason");
            if (schedule == null)
            {
                Debug.Log("[SeasonScreen] No saved schedule—generating new one");
                schedule = SeasonScheduler.GenerateSeason(leagueTeams, DateTime.Today, daysBetweenMatches);
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
                  .Initialize(match, SimulateMatch);
            }
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
