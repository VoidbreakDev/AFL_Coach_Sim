// File: Assets/Scripts/Managers/SaveLoadManager.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Managers
{
    public static class SaveLoadManager
    {
        private static string DataFolder => Application.persistentDataPath;

        public static void SavePlayer(Player player)
        {
            string json = JsonSerialization.ToJson(player, true);
            File.WriteAllText(Path.Combine(DataFolder, $"player_{player.Id}.json"), json);
            Debug.Log($"Player saved: {player.Id}");
        }

        public static Player LoadPlayer(string playerId)
        {
            var path = Path.Combine(DataFolder, $"player_{playerId}.json");
            if (!File.Exists(path)) return null;
            return JsonSerialization.FromJson<Player>(File.ReadAllText(path));
        }

        // original team save/load by team.Id
        public static void SaveTeam(Team team)
        {
            string json = JsonSerialization.ToJson(team, true);
            File.WriteAllText(Path.Combine(DataFolder, $"team_{team.Id}.json"), json);
            Debug.Log($"Team saved: {team.Id}");
        }

        public static Team LoadTeam(string teamId)
        {
            var path = Path.Combine(DataFolder, $"team_{teamId}.json");
            if (!File.Exists(path)) return null;
            return JsonSerialization.FromJson<Team>(File.ReadAllText(path));
        }

        // new overload: save by arbitrary key (e.g. coach name)
        public static void SaveTeam(string key, Team team)
        {
            string json = JsonSerialization.ToJson(team, true);
            File.WriteAllText(Path.Combine(DataFolder, $"team_{key}.json"), json);
            Debug.Log($"Team saved for key '{key}'");
        }
        /// <summary>
        /// Serializes and writes a SeasonSchedule to JSON under schedule_{key}.json
        /// </summary>
        public static void SaveSchedule(string key, SeasonSchedule schedule)
        {
            if (schedule == null)
            {
                Debug.LogError($"[SaveLoadManager] Cannot save null schedule for key '{key}'");
                return;
            }
            
            if (schedule.Fixtures == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Schedule has null Fixtures list, initializing empty list for key '{key}'");
                schedule.Fixtures = new List<Match>();
            }
            
            string fileName = $"schedule_{key}.json";
            string filePath = Path.Combine(DataFolder, fileName);
            string json = JsonSerialization.ToJson(schedule, true);

            File.WriteAllText(filePath, json);
            Debug.Log($"[SaveLoadManager] Schedule saved to: {filePath} ({schedule.Fixtures.Count} fixtures)");
        }

        /// <summary>
        /// Reads schedule_{key}.json and deserializes into a SeasonSchedule, or null if missing.
        /// </summary>
        public static SeasonSchedule LoadSchedule(string key)
        {
            string fileName = $"schedule_{key}.json";
            string filePath = Path.Combine(DataFolder, fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveLoadManager] No schedule file found at: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            var schedule = JsonSerialization.FromJson<SeasonSchedule>(json);
            
            // Check if deserialization was successful and Fixtures is not null
            if (schedule == null)
            {
                Debug.LogError($"[SaveLoadManager] Failed to deserialize schedule from: {filePath}");
                return null;
            }
            
            if (schedule.Fixtures == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Schedule loaded but Fixtures is null from: {filePath}");
                schedule.Fixtures = new List<Match>(); // Initialize empty list to prevent future null reference errors
            }
            
            Debug.Log($"[SaveLoadManager] Schedule loaded from: {filePath} ({schedule.Fixtures.Count} fixtures)");
            return schedule;
        }
    }
}