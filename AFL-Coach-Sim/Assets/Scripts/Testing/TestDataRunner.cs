// File: Assets/Scripts/Testing/TestDataRunner.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using AFLManager.Models;
using AFLManager.Managers;

namespace AFLManager.Testing
{
    public class TestDataRunner : MonoBehaviour
    {
        [Header("Test Settings")]
        public LeagueLevel testLeagueLevel = LeagueLevel.Local;
        public int teamCount    = 6;
        public int rosterSize   = 22;
        public int daysBetweenMatches = 7;

        void Start()
        {
            // 1) Generate N teams
            List<Team> teams = TestDataGenerator
                .GenerateTeams(testLeagueLevel, teamCount, rosterSize);

            // 2) Persist each to JSON
            for (int i = 0; i < teams.Count; i++)
            {
                string key = $"team{i+1}";
                SaveLoadManager.SaveTeam(key, teams[i]);
            }

            // 3) (Optional) Save a schedule too
            var schedule = SeasonScheduler.GenerateSeason(
                teams, DateTime.Today, daysBetweenMatches);
            SaveLoadManager.SaveSchedule("testSeason", schedule);

            // 4) Tell the game which “coach” to load
            PlayerPrefs.SetString("CoachName", "team1");

            Debug.Log($"[TestDataRunner] persistentDataPath = {Application.persistentDataPath}");
            var files = Directory.GetFiles(Application.persistentDataPath);
            foreach (var f in files)
                Debug.Log($"[TestDataRunner] Found file: {Path.GetFileName(f)}");

            string schedulePath = Path.Combine(Application.persistentDataPath, "schedule_testSeason.json");
            if (File.Exists(schedulePath))
            {
                string txt = File.ReadAllText(schedulePath);
                Debug.Log($"[TestDataRunner] schedule JSON:\n{txt}");
            }

            // jump to RosterScreen…
            SceneManager.LoadScene("RosterScreen");
        }
    }
}
