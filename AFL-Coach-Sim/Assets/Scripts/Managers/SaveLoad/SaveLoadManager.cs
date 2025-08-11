// File: Assets/Scripts/Managers/SaveLoadManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Managers
{
    public static class SaveLoadManager
    {
        private static string DataFolder => Application.persistentDataPath;

        // ===== Players =====
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

        // ===== Teams =====
        public static void SaveTeam(Team team)
        {
            string json = JsonSerialization.ToJson(team, true);
            File.WriteAllText(Path.Combine(DataFolder, $"team_{team.Id}.json"), json);
            Debug.Log($"Team saved: {team.Id}");
        }

        public static Team LoadTeam(string teamId)
        {
            var path = Path.Combine(DataFolder, $"team_{teamId}.json");
            Debug.Log($"[SaveLoadManager] Attempting to load team from: {path}");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveLoadManager] Team file does not exist: {path}");
                return null;
            }
            var json = File.ReadAllText(path);
            var team = JsonSerialization.FromJson<Team>(json);
            if (team != null) Debug.Log($"[SaveLoadManager] Loaded team: {team.Name}");
            else Debug.LogError($"[SaveLoadManager] Failed to deserialize team from: {path}");
            return team;
        }

        public static void SaveTeam(string key, Team team)
        {
            string json = JsonSerialization.ToJson(team, true);
            File.WriteAllText(Path.Combine(DataFolder, $"team_{key}.json"), json);
            Debug.Log($"Team saved for key '{key}'");
        }

        // ===== Schedule =====
        public static void SaveSchedule(string key, SeasonSchedule schedule)
        {
            if (schedule == null) { Debug.LogError($"[SaveLoadManager] Cannot save null schedule ({key})"); return; }
            if (schedule.Fixtures == null) schedule.Fixtures = new List<Match>();

            string filePath = Path.Combine(DataFolder, $"schedule_{key}.json");
            File.WriteAllText(filePath, JsonSerialization.ToJson(schedule, true));
            Debug.Log($"[SaveLoadManager] Schedule saved to: {filePath} ({schedule.Fixtures.Count} fixtures)");
        }

        public static SeasonSchedule LoadSchedule(string key)
        {
            string filePath = Path.Combine(DataFolder, $"schedule_{key}.json");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveLoadManager] No schedule at: {filePath}");
                return null;
            }
            var schedule = JsonSerialization.FromJson<SeasonSchedule>(File.ReadAllText(filePath));
            if (schedule == null) { Debug.LogError($"[SaveLoadManager] Failed to deserialize schedule"); return null; }
            if (schedule.Fixtures == null) schedule.Fixtures = new List<Match>();
            return schedule;
        }

        // ===== Match Results (lean persist) =====
        private const string ResultsFileName = "results.json";
        private static string ResultsFilePath => Path.Combine(DataFolder, ResultsFileName);

        // Persist-friendly DTO (no dictionaries)
        [Serializable]
        private class MatchResultPersist
        {
            public string MatchId;
            public string RoundKey;
            public string HomeTeamId;
            public string AwayTeamId;
            public int HomeScore;
            public int AwayScore;
            public int HomeGoals;
            public int HomeBehinds;
            public int AwayGoals;
            public int AwayBehinds;
            public string BestOnGroundPlayerId;
            public DateTime FixtureDate;
            public DateTime SimulatedAtUtc;
        }

        [Serializable]
        private class MatchResultPersistList { public List<MatchResultPersist> Items = new List<MatchResultPersist>(); }

        private static MatchResultPersist ToPersist(MatchResult r) => new MatchResultPersist
        {
            MatchId = r.MatchId,
            RoundKey = r.RoundKey,
            HomeTeamId = r.HomeTeamId,
            AwayTeamId = r.AwayTeamId,
            HomeScore = r.HomeScore,
            AwayScore = r.AwayScore,
            HomeGoals = r.HomeGoals,
            HomeBehinds = r.HomeBehinds,
            AwayGoals = r.AwayGoals,
            AwayBehinds = r.AwayBehinds,
            BestOnGroundPlayerId = r.BestOnGroundPlayerId,
            FixtureDate = r.FixtureDate,
            SimulatedAtUtc = r.SimulatedAtUtc
        };

        private static MatchResult FromPersist(MatchResultPersist p) => new MatchResult
        {
            MatchId = p.MatchId,
            RoundKey = p.RoundKey,
            HomeTeamId = p.HomeTeamId,
            AwayTeamId = p.AwayTeamId,
            HomeScore = p.HomeScore,
            AwayScore = p.AwayScore,
            HomeGoals = p.HomeGoals,
            HomeBehinds = p.HomeBehinds,
            AwayGoals = p.AwayGoals,
            AwayBehinds = p.AwayBehinds,
            BestOnGroundPlayerId = p.BestOnGroundPlayerId,
            FixtureDate = p.FixtureDate,
            SimulatedAtUtc = p.SimulatedAtUtc,
            // PlayerStats intentionally omitted for persistence simplicity
        };

        public static void SaveMatchResult(MatchResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.MatchId))
            {
                Debug.LogError("[SaveLoadManager] SaveMatchResult called with null or missing MatchId");
                return;
            }
            var all = LoadAllResultsPersistInternal();
            int idx = all.Items.FindIndex(x => x.MatchId == result.MatchId);
            var dto = ToPersist(result);
            if (idx >= 0) all.Items[idx] = dto; else all.Items.Add(dto);
            WriteAllResultsPersist(all);

#if UNITY_EDITOR
            var path = ResultsFilePath;
            long len = File.Exists(path) ? new FileInfo(path).Length : 0;
            Debug.Log($"[SaveLoadManager] Saved result: {result.MatchId} {result.HomeTeamId} {result.HomeScore}-{result.AwayScore} {result.AwayTeamId} → {path} ({len} bytes)");
#endif
        }

        public static List<MatchResult> LoadAllResults()
        {
            var all = LoadAllResultsPersistInternal();
            var list = new List<MatchResult>();
            if (all.Items != null)
                foreach (var p in all.Items) list.Add(FromPersist(p));
            return list;
        }

        public static List<MatchResult> LoadResultsForRound(string roundKey)
        {
            var all = LoadAllResultsPersistInternal();
            var q = (all.Items ?? new List<MatchResultPersist>())
                    .Where(r => r.RoundKey == roundKey)
                    .Select(FromPersist)
                    .ToList();
            return q;
        }

        public static void ClearAllResults()
        {
            try
            {
                if (File.Exists(ResultsFilePath)) File.Delete(ResultsFilePath);
#if UNITY_EDITOR
                Debug.Log("[SaveLoadManager] Cleared results.json");
#endif
            }
            catch (Exception e) { Debug.LogError($"[SaveLoadManager] ClearAllResults failed: {e}"); }
        }

        // internals
        private static MatchResultPersistList LoadAllResultsPersistInternal()
        {
            try
            {
                if (!File.Exists(ResultsFilePath)) return new MatchResultPersistList();
                var json = File.ReadAllText(ResultsFilePath);
                if (string.IsNullOrWhiteSpace(json)) return new MatchResultPersistList();
                var list = JsonSerialization.FromJson<MatchResultPersistList>(json);
                return list ?? new MatchResultPersistList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadManager] LoadAllResults failed: {e}");
                return new MatchResultPersistList();
            }
        }

        private static void WriteAllResultsPersist(MatchResultPersistList list)
        {
            try
            {
                var json = JsonSerialization.ToJson(list, true);
                File.WriteAllText(ResultsFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadManager] WriteAllResults failed: {e}");
            }

        }
    }
}