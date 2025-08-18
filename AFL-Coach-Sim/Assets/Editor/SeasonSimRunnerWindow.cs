// Assets/Editor/SeasonSimRunnerWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

// Core (SimCore)
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Engine.Scheduling;
using AFLCoachSim.Core.Engine.Ladder;
using AFLCoachSim.Core.DTO;

public sealed class SeasonSimRunnerWindow : EditorWindow
{
    // UI state
    private enum SourceMode { QuickDemo, FromJsonTextAsset }
    private SourceMode mode = SourceMode.QuickDemo;

    // Quick demo config
    private string leagueName = "Demo League";
    private bool doubleRound = true;
    private int teamCount = 6;
    private int attackMin = 50, attackMax = 65;
    private int defenseMin = 50, defenseMax = 65;

    // JSON config
    private TextAsset jsonAsset;

    // Sim
    private int seed = 12345;

    // Output
    private Vector2 scroll;
    private List<(int round, TeamId home, TeamId away)> rawFixtures;
    private List<MatchResultDTO> results;
    private List<LadderEntryDTO> ladder;

    [MenuItem("AFL Coach Sim/Season Sim Runner")]
    public static void ShowWindow()
    {
        var win = GetWindow<SeasonSimRunnerWindow>("Season Sim Runner");
        win.minSize = new Vector2(520, 420);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Season Simulation – Sanity Check", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Source selector
        mode = (SourceMode)EditorGUILayout.EnumPopup("Config Source", mode);

        if (mode == SourceMode.QuickDemo)
        {
            DrawQuickDemoConfig();
        }
        else
        {
            DrawJsonConfig();
        }

        // Seed + Run
        seed = EditorGUILayout.IntField("Random Seed", seed);
        if (GUILayout.Button("Run Season", GUILayout.Height(28)))
        {
            RunSeason();
        }

        EditorGUILayout.Space(8);
        DrawOutput();
    }

    private void DrawQuickDemoConfig()
    {
        EditorGUILayout.BeginVertical("box");
        leagueName = EditorGUILayout.TextField("League Name", leagueName);
        doubleRound = EditorGUILayout.Toggle("Double Round Robin", doubleRound);
        teamCount = Mathf.Clamp(EditorGUILayout.IntField("Team Count", teamCount), 2, 40);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Generated Ratings (Inclusive Ranges)");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attack", GUILayout.Width(60));
        attackMin = EditorGUILayout.IntField(attackMin, GUILayout.Width(40));
        EditorGUILayout.LabelField("to", GUILayout.Width(18));
        attackMax = EditorGUILayout.IntField(attackMax, GUILayout.Width(40));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Defense", GUILayout.Width(60));
        defenseMin = EditorGUILayout.IntField(defenseMin, GUILayout.Width(40));
        EditorGUILayout.LabelField("to", GUILayout.Width(18));
        defenseMax = EditorGUILayout.IntField(defenseMax, GUILayout.Width(40));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawJsonConfig()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Drag in a JSON TextAsset that matches LeagueConfig fields:");
        EditorGUILayout.HelpBox(
            "Expected JSON:\n{\n  \"LeagueName\":\"AFL\",\n  \"DoubleRoundRobin\":true,\n  \"Teams\":[{\"Id\":1,\"Name\":\"...\",\"Attack\":60,\"Defense\":55}, ...]\n}",
            MessageType.Info
        );
        jsonAsset = (TextAsset)EditorGUILayout.ObjectField("League JSON", jsonAsset, typeof(TextAsset), false);
        EditorGUILayout.EndVertical();
    }

    private void RunSeason()
    {
        try
        {
            var config = mode == SourceMode.QuickDemo ? BuildQuickDemoConfig() : BuildFromJsonConfig();
            if (config == null)
            {
                Debug.LogError("LeagueConfig is null. Aborting.");
                return;
            }

            // Build teams
            var teams = config.Teams
                .Select(t => new Team(new TeamId(t.Id), t.Name, t.Attack, t.Defense))
                .ToDictionary(t => t.Id, t => t);

            rawFixtures = RoundRobinScheduler.Build(teams.Keys.ToList(), config.DoubleRoundRobin);

            var sim = new MatchSimulator(teams, new DeterministicRandom(seed));
            results = new List<MatchResultDTO>(rawFixtures.Count);

            foreach (var fx in rawFixtures)
            {
                results.Add(sim.Simulate(fx.round, fx.home, fx.away));
            }

            ladder = LadderCalculator.BuildLadder(results);

            Debug.Log($"[SeasonSimRunner] {config.LeagueName}: Simulated {results.Count} matches across {rawFixtures.Max(f => f.round)} rounds. Seed={seed}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SeasonSimRunner] Exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
        }
    }

    private LeagueConfig BuildQuickDemoConfig()
    {
        var rng = new System.Random(seed);
        var cfg = new LeagueConfig
        {
            LeagueName = leagueName,
            DoubleRoundRobin = doubleRound,
            Teams = new List<TeamConfig>()
        };

        for (int i = 1; i <= teamCount; i++)
        {
            cfg.Teams.Add(new TeamConfig
            {
                Id = i,
                Name = $"Team {i}",
                Attack = rng.Next(Math.Min(attackMin, attackMax), Math.Max(attackMin, attackMax) + 1),
                Defense = rng.Next(Math.Min(defenseMin, defenseMax), Math.Max(defenseMin, defenseMax) + 1)
            });
        }
        return cfg;
    }

    private LeagueConfig BuildFromJsonConfig()
    {
        if (jsonAsset == null)
        {
            EditorUtility.DisplayDialog("Missing JSON", "Please assign a JSON TextAsset.", "OK");
            return null;
        }

        // JsonUtility needs a mirror container; then map to LeagueConfig
        var mirror = JsonUtility.FromJson<LeagueConfigMirror>(jsonAsset.text);
        if (mirror == null)
        {
            EditorUtility.DisplayDialog("Parse Error", "Could not parse the JSON into a league config.", "OK");
            return null;
        }

        var cfg = new LeagueConfig
        {
            LeagueName = string.IsNullOrEmpty(mirror.LeagueName) ? "League" : mirror.LeagueName,
            DoubleRoundRobin = mirror.DoubleRoundRobin,
            Teams = new List<TeamConfig>()
        };

        if (mirror.Teams != null)
        {
            foreach (var t in mirror.Teams)
            {
                cfg.Teams.Add(new TeamConfig
                {
                    Id = t.Id,
                    Name = t.Name ?? $"Team {t.Id}",
                    Attack = t.Attack,
                    Defense = t.Defense
                });
            }
        }
        return cfg;
    }

    private void DrawOutput()
    {
        if (ladder == null || results == null || rawFixtures == null)
        {
            EditorGUILayout.HelpBox("Run a season to see results here.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Ladder (Top 10)", EditorStyles.boldLabel);
        int show = Mathf.Min(10, ladder.Count);
        for (int i = 0; i < show; i++)
        {
            var e = ladder[i];
            EditorGUILayout.LabelField($"{i + 1}. Team {e.Team.Value}  Pts:{e.PremiershipPoints}  W:{e.Wins} L:{e.Losses} D:{e.Draws}  PF:{e.PointsFor}  PA:{e.PointsAgainst}  %:{e.Percentage / 100f:0.00}");
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Sample Results (first 10)", EditorStyles.boldLabel);
        for (int i = 0; i < Mathf.Min(10, results.Count); i++)
        {
            var r = results[i];
            EditorGUILayout.LabelField($"R{r.Round}: {r.Home.Value} {r.HomeScore} – {r.AwayScore} {r.Away.Value}");
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Dump All Results to Console"))
        {
            foreach (var r in results.OrderBy(x => x.Round))
                Debug.Log($"[Result] R{r.Round}: {r.Home.Value} {r.HomeScore} – {r.AwayScore} {r.Away.Value}");
        }

        EditorGUILayout.EndScrollView();
    }

    // Mirror for JsonUtility
    [Serializable]
    private class LeagueConfigMirror
    {
        public string LeagueName = "League";
        public bool DoubleRoundRobin = true;
        public List<TeamConfigMirror> Teams = new List<TeamConfigMirror>();
    }

    [Serializable]
    private class TeamConfigMirror
    {
        public int Id;
        public string Name;
        public int Attack = 50;
        public int Defense = 50;
    }
}
#endif