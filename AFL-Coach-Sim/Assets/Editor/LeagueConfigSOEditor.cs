#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// SimCore engine bits for preview sim:
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Scheduling;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Engine.Ladder;
using AFLCoachSim.Core.DTO;

// SO type
using AFLCoachSim.Unity.Data;

[CustomEditor(typeof(LeagueConfigSO))]
public class LeagueConfigSOEditor : Editor
{
    private LeagueConfigSO so;
    private int generateCount = 8;
    private int seed = 12345;
    private int attackMin = 50, attackMax = 65;
    private int defenseMin = 50, defenseMax = 65;
    private Vector2 teamScroll;

    // Preview output (runtime preview only, not persisted)
    private List<(int round, TeamId home, TeamId away)> fixtures;
    private List<MatchResultDTO> results;
    private List<LadderEntryDTO> ladder;
    private Dictionary<int, string> teamNames; // TeamId.Value -> Team.Name

    private string search = string.Empty;
    private bool searchById = false;

    private void OnEnable()
    {
        so = (LeagueConfigSO)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("League Config", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LeagueName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DoubleRoundRobin"));

        EditorGUILayout.Space(6);
        DrawTeamsList();

        EditorGUILayout.Space(10);
        DrawUtilities();

        EditorGUILayout.Space(10);
        DrawPreviewSim();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTeamsList()
    {
        EditorGUILayout.LabelField($"Teams ({so.Teams.Count})", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            search = EditorGUILayout.TextField("Search", search);
            searchById = GUILayout.Toggle(searchById, "By ID", "Button", GUILayout.Width(60));
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                search = string.Empty;
            }
        }
        using (new EditorGUILayout.VerticalScope("box"))
        {
            teamScroll = EditorGUILayout.BeginScrollView(teamScroll, GUILayout.MinHeight(150));
            for (int i = 0; i < so.Teams.Count; i++)
            {
                var t = so.Teams[i];
                if (!Matches(t))
                    continue;
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(30));
                        t.Id = EditorGUILayout.IntField("Id", t.Id);
                        if (GUILayout.Button("X", GUILayout.Width(22)))
                        {
                            Undo.RecordObject(so, "Remove Team");
                            so.Teams.RemoveAt(i);
                            EditorUtility.SetDirty(so);
                            break;
                        }
                    }
                    t.Name = EditorGUILayout.TextField("Name", t.Name);
                    t.Attack = EditorGUILayout.IntSlider("Attack", t.Attack, 0, 100);
                    t.Defense = EditorGUILayout.IntSlider("Defense", t.Defense, 0, 100);
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Team"))
                {
                    Undo.RecordObject(so, "Add Team");
                    so.Teams.Add(new LeagueConfigSO.TeamEntry
                    {
                        Id = so.Teams.Count + 1,
                        Name = $"Team {so.Teams.Count + 1}",
                        Attack = 50, Defense = 50
                    });
                    EditorUtility.SetDirty(so);
                }

                if (GUILayout.Button("Reassign IDs (1..N)"))
                {
                    Undo.RecordObject(so, "Reassign IDs");
                    so.ReassignSequentialIds();
                    EditorUtility.SetDirty(so);
                }

                if (GUILayout.Button("Sort by Id"))
                {
                    Undo.RecordObject(so, "Sort Teams");
                    so.Teams = so.Teams.OrderBy(t => t.Id).ToList();
                    EditorUtility.SetDirty(so);
                }
            }
        }
    }

    private void DrawUtilities()
    {
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            // Random ratings tool
            EditorGUILayout.LabelField("Randomize Ratings (Inclusive Ranges)");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Attack", GUILayout.Width(60));
                attackMin = EditorGUILayout.IntField(attackMin, GUILayout.Width(44));
                EditorGUILayout.LabelField("to", GUILayout.Width(16));
                attackMax = EditorGUILayout.IntField(attackMax, GUILayout.Width(44));
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Defense", GUILayout.Width(60));
                defenseMin = EditorGUILayout.IntField(defenseMin, GUILayout.Width(44));
                EditorGUILayout.LabelField("to", GUILayout.Width(16));
                defenseMax = EditorGUILayout.IntField(defenseMax, GUILayout.Width(44));
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                seed = EditorGUILayout.IntField("Seed", seed);
                if (GUILayout.Button("Randomize Existing"))
                {
                    Undo.RecordObject(so, "Randomize Ratings");
                    var rng = new System.Random(seed);
                    foreach (var t in so.Teams)
                    {
                        t.Attack = Mathf.Clamp(rng.Next(Mathf.Min(attackMin, attackMax), Mathf.Max(attackMin, attackMax) + 1), 0, 100);
                        t.Defense = Mathf.Clamp(rng.Next(Mathf.Min(defenseMin, defenseMax), Mathf.Max(defenseMin, defenseMax) + 1), 0, 100);
                    }
                    EditorUtility.SetDirty(so);
                }
            }

            EditorGUILayout.Space(6);
            // Generate N teams
            using (new EditorGUILayout.HorizontalScope())
            {
                generateCount = Mathf.Clamp(EditorGUILayout.IntField("Generate Teams (N)", generateCount), 2, 40);
                if (GUILayout.Button("Generate Fresh"))
                {
                    Undo.RecordObject(so, "Generate Teams");
                    so.Teams.Clear();
                    var rng = new System.Random(seed);
                    for (int i = 1; i <= generateCount; i++)
                    {
                        so.Teams.Add(new LeagueConfigSO.TeamEntry
                        {
                            Id = i,
                            Name = $"Team {i}",
                            Attack = Mathf.Clamp(rng.Next(Mathf.Min(attackMin, attackMax), Mathf.Max(attackMin, attackMax) + 1), 0, 100),
                            Defense = Mathf.Clamp(rng.Next(Mathf.Min(defenseMin, defenseMax), Mathf.Max(defenseMin, defenseMax) + 1), 0, 100),
                        });
                    }
                    EditorUtility.SetDirty(so);
                }
            }

            // Validation
            if (GUILayout.Button("Validate"))
            {
                if (so.Validate(out var msg))
                    EditorUtility.DisplayDialog("League Validation", "OK ✓", "Great");
                else
                    EditorUtility.DisplayDialog("League Validation", msg, "Fix");
            }
        }
    }

    private void DrawPreviewSim()
    {
        EditorGUILayout.LabelField("Preview Simulation", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            seed = EditorGUILayout.IntField("Sim Seed", seed);
            if (GUILayout.Button("Run Preview Sim"))
            {
                RunPreviewSim();
            }

            if (ladder != null && results != null && fixtures != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Ladder (Top 10)", EditorStyles.boldLabel);
                int show = Mathf.Min(10, ladder.Count);
                for (int i = 0; i < show; i++)
                {
                    var e = ladder[i];
                    var name = (teamNames != null && teamNames.TryGetValue(e.Team.Value, out var nm)) ? nm : $"Team {e.Team.Value}";
                    EditorGUILayout.LabelField($"{i + 1}. {name}  Pts:{e.PremiershipPoints}  W:{e.Wins} L:{e.Losses} D:{e.Draws}  PF:{e.PointsFor}  PA:{e.PointsAgainst}  %:{e.Percentage / 100f:0.00}");
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Sample Results (first 10)", EditorStyles.boldLabel);
                for (int i = 0; i < Mathf.Min(10, results.Count); i++)
                {
                    var r = results[i];
                    var hn = (teamNames != null && teamNames.TryGetValue(r.Home.Value, out var hnm)) ? hnm : $"Team {r.Home.Value}";
                    var an = (teamNames != null && teamNames.TryGetValue(r.Away.Value, out var anm)) ? anm : $"Team {r.Away.Value}";
                    EditorGUILayout.LabelField($"R{r.Round}: {hn} {r.HomeScore} – {r.AwayScore} {an}");
                }

                EditorGUILayout.Space(6);
                if (GUILayout.Button("Dump All Results to Console"))
                {
                    foreach (var r in results.OrderBy(x => x.Round))
                    {
                        var hn = (teamNames != null && teamNames.TryGetValue(r.Home.Value, out var hnm)) ? hnm : $"Team {r.Home.Value}";
                        var an = (teamNames != null && teamNames.TryGetValue(r.Away.Value, out var anm)) ? anm : $"Team {r.Away.Value}";
                        Debug.Log($"[LeagueConfigSO Preview] R{r.Round}: {hn} {r.HomeScore} – {r.AwayScore} {an}");
                    }
                }
            }
        }
    }

    private bool Matches(LeagueConfigSO.TeamEntry t)
    {
        if (string.IsNullOrEmpty(search)) return true;
        if (searchById)
        {
            return t.Id.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        var name = t.Name ?? string.Empty;
        return name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RunPreviewSim()
    {
        try
        {
            if (!so.Validate(out var msg))
            {
                EditorUtility.DisplayDialog("League invalid", msg, "OK");
                return;
            }

            var coreCfg = so.ToCore();

            // Build teams dictionary
            var teams = coreCfg.Teams
                .Select(t => new Team(new TeamId(t.Id), t.Name, t.Attack, t.Defense))
                .ToDictionary(t => t.Id, t => t);
            
            teamNames = teams.ToDictionary(kv => kv.Key.Value, kv => kv.Value.Name);

            fixtures = RoundRobinScheduler.Build(teams.Keys.ToList(), coreCfg.DoubleRoundRobin);

            var sim = new MatchSimulator(teams, new DeterministicRandom(seed));
            results = new List<MatchResultDTO>(fixtures.Count);
            foreach (var fx in fixtures)
                results.Add(sim.Simulate(fx.round, fx.home, fx.away));

            ladder = LadderCalculator.BuildLadder(results);

            Debug.Log($"[LeagueConfigSO Preview] {coreCfg.LeagueName}: {results.Count} matches across {fixtures.Max(f => f.round)} rounds. Seed={seed}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LeagueConfigSO Preview] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
#endif