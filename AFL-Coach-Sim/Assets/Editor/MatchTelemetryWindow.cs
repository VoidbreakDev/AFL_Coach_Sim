#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Data; // TeamTactics
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Tuning;   // MatchTuningSO / Provider

public class MatchTelemetryWindow : EditorWindow
{
    // --- Live snapshot (from TelemetryHub) ---
    private MatchSnapshot _last;
    private bool _listening;
    private Vector2 _scroll;
    private DateTime _lastRepaint = DateTime.MinValue;

    // --- Quick Match controls ---
    private int seed = 12345;
    private int quarterMinutes = 20;         // 20 min quarters
    private int rosterRating = 70;           // player attribute baseline
    private int rosterCount = 30;            // players to generate
    private int targetInterchanges = 70;     // tactics slider
    private Weather weather = Weather.Clear; // engine enum
    private string homeName = "Home";
    private string awayName = "Away";

    // --- Tuning controls ---
    private MatchTuningSO tuningAsset;
    private MatchTuningSO localPreview;          // transient SO for local override edits
    private bool useLocalOverride = false;
    private bool tuningFoldout = true;

    [MenuItem("AFL Coach Sim/Debug/Match Telemetry")]
    public static void ShowWindow()
    {
        var w = GetWindow<MatchTelemetryWindow>("Match Telemetry");
        w.minSize = new Vector2(460, 360);
        w.StartListening();
    }

    void OnEnable()
    {
        // Load/create shared asset and a local editable copy
        tuningAsset = MatchTuningProvider.GetOrCreateAsset();
        if (localPreview == null)
        {
            localPreview = ScriptableObject.CreateInstance<MatchTuningSO>();
            if (tuningAsset != null)
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tuningAsset), localPreview);
        }

        if (_listening) StartListening();
    }

    void OnDisable()
    {
        StopListening();
    }

    private void StartListening()
    {
        if (_listening) return;
        TelemetryHub.OnSnapshot += OnSnap;
        TelemetryHub.OnComplete += OnComplete;
        _listening = true;
    }

    private void StopListening()
    {
        if (!_listening) return;
        TelemetryHub.OnSnapshot -= OnSnap;
        TelemetryHub.OnComplete -= OnComplete;
        _listening = false;
    }

    private void OnSnap(MatchSnapshot s)
    {
        _last = s;
        var now = DateTime.UtcNow;
        if ((now - _lastRepaint).TotalMilliseconds > 33) { _lastRepaint = now; Repaint(); }
    }

    private void OnComplete(MatchSnapshot s)
    {
        _last = s;
        Repaint();
    }

    void OnGUI()
    {
        DrawToolbar();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawQuickMatchControls();
        GUILayout.Space(6);
        DrawTuningSection();

        GUILayout.Space(8);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (_last == null)
        {
            EditorGUILayout.HelpBox("Waiting for telemetry...\nRun a match to see live updates.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawScoreCard(_last);
        GUILayout.Space(6);
        DrawFlowRow(_last);
        GUILayout.Space(6);
        DrawFatigueInjury(_last);
        GUILayout.Space(6);
        DrawMeta(_last);

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Toggle(_listening, _listening ? "Listening" : "Stopped", EditorStyles.toolbarButton) != _listening)
            {
                if (_listening) StopListening(); else StartListening();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _last = null;
            }

            // Run button in the toolbar for convenience
            if (GUILayout.Button("Run Quick Match", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                RunQuickMatch();
            }
        }
    }

    private void DrawQuickMatchControls()
    {
        GUILayout.Label("Quick Match", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                homeName = EditorGUILayout.TextField("Home Team Name", homeName);
                awayName = EditorGUILayout.TextField("Away Team Name", awayName);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                seed = EditorGUILayout.IntField(new GUIContent("Seed"), seed);
                quarterMinutes = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Quarter (min)"), quarterMinutes), 2, 30);
                weather = (Weather)EditorGUILayout.EnumPopup(new GUIContent("Weather"), weather);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                rosterRating = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Roster Rating"), rosterRating), 30, 95);
                rosterCount  = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Roster Size"), rosterCount), 24, 40);
                targetInterchanges = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Target INT/G"), targetInterchanges), 20, 110);
            }

            if (GUILayout.Button("Run Quick Match", GUILayout.Height(26)))
            {
                RunQuickMatch();
            }
        }
    }

    private void DrawTuningSection()
    {
        GUILayout.Label("Tuning", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create/Load Asset", GUILayout.Width(140)))
                {
                    tuningAsset = MatchTuningProvider.GetOrCreateAsset();
                }

                GUILayout.Space(8);
                useLocalOverride = EditorGUILayout.ToggleLeft(
                    new GUIContent("Use Local Override (editor only)"),
                    useLocalOverride, GUILayout.Width(230));

                GUILayout.FlexibleSpace();

                if (!useLocalOverride)
                {
                    GUI.enabled = tuningAsset != null;
                    if (GUILayout.Button("Save Asset", GUILayout.Width(100)))
                    {
                        if (tuningAsset != null)
                        {
                            EditorUtility.SetDirty(tuningAsset);
                            AssetDatabase.SaveAssets();
                            MatchTuningProvider.Invalidate();
                        }
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = tuningAsset != null;
                    if (GUILayout.Button("Copy From Asset", GUILayout.Width(130)))
                    {
                        if (tuningAsset != null)
                            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tuningAsset), localPreview);
                    }
                    if (GUILayout.Button("Apply Local → Asset", GUILayout.Width(140)))
                    {
                        if (tuningAsset != null)
                        {
                            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(localPreview), tuningAsset);
                            EditorUtility.SetDirty(tuningAsset);
                            AssetDatabase.SaveAssets();
                            MatchTuningProvider.Invalidate();
                        }
                    }
                    GUI.enabled = true;
                }
            }

            GUILayout.Space(4);

            var src = useLocalOverride ? localPreview : tuningAsset;
            if (src == null)
            {
                EditorGUILayout.HelpBox("No tuning asset found. Click 'Create/Load Asset' (stored at " + MatchTuningProvider.DefaultAssetPath + ")", MessageType.Warning);
                return;
            }

            tuningFoldout = EditorGUILayout.Foldout(tuningFoldout, "Fine‑tuning (injury & weather penalties, baselines)", true);
            if (tuningFoldout)
            {
                EditorGUI.indentLevel++;
                DrawTuningSliders(src);
                EditorGUI.indentLevel--;
            }
        }
    }

    private void RunQuickMatch()
    {
        try
        {
            // Minimal in-editor harness — uses your engine + TelemetryHub broadcasts (no sink needed)
            var teams = new Dictionary<TeamId, Team>
            {
                [new TeamId(1)] = new Team(new TeamId(1), string.IsNullOrEmpty(homeName) ? "Home" : homeName, 60, 60),
                [new TeamId(2)] = new Team(new TeamId(2), string.IsNullOrEmpty(awayName) ? "Away" : awayName, 60, 60)
            };

            var rosters = new Dictionary<TeamId, List<Player>>
            {
                [new TeamId(1)] = MakeUniformRoster(rosterRating, rosterCount),
                [new TeamId(2)] = MakeUniformRoster(rosterRating, rosterCount),
            };

            var tactics = new Dictionary<TeamId, TeamTactics>
            {
                [new TeamId(1)] = new TeamTactics { ContestBias = 50, KickingRisk = 50, TargetInterchangesPerGame = targetInterchanges },
                [new TeamId(2)] = new TeamTactics { ContestBias = 50, KickingRisk = 50, TargetInterchangesPerGame = targetInterchanges },
            };

            int qSeconds = Mathf.Max(2, quarterMinutes) * 60;

            // Pick tuning: asset by default, else local override (both ToRuntime())
            MatchTuning tuningRuntime = null;
            if (useLocalOverride)
            {
                if (localPreview != null) tuningRuntime = localPreview.ToRuntime();
            }
            else
            {
                if (tuningAsset != null)   tuningRuntime = tuningAsset.ToRuntime();
            }

            // Kick off a single blocking sim (fast). TelemetryHub will stream snapshots to the window.
            var dto = MatchEngine.PlayMatch(
                round: 1,
                homeId: new TeamId(1), awayId: new TeamId(2),
                teams: teams, rosters: rosters, tactics: tactics,
                weather: weather, ground: new Ground(),
                quarterSeconds: qSeconds,
                rng: new DeterministicRandom(seed),
                sink: null, // relying on TelemetryHub.Publish(...) in MatchEngine
                tuning: tuningRuntime // if null, engine will use provider default
            );

            Debug.Log($"[Telemetry] Finished Quick Match — Score H/A: {dto.HomeScore}/{dto.AwayScore}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[Telemetry] Quick Match failed: " + ex.Message);
        }
    }

    // ---------- UI sections ----------
    private static void DrawScoreCard(MatchSnapshot s)
    {
        GUILayout.Label("Score", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Home", GUILayout.Width(60));
            EditorGUILayout.LabelField($"{s.HomeGoals}.{s.HomeBehinds}  ({s.HomePoints})", GUILayout.Width(120));

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField("Away", GUILayout.Width(60));
            EditorGUILayout.LabelField($"{s.AwayGoals}.{s.AwayBehinds}  ({s.AwayPoints})", GUILayout.Width(120));
        }
    }

    private static void DrawFlowRow(MatchSnapshot s)
    {
        GUILayout.Label("Match State", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Quarter:", GUILayout.Width(70));
            EditorGUILayout.LabelField(s.Quarter.ToString(), GUILayout.Width(40));

            EditorGUILayout.LabelField("Phase:", GUILayout.Width(50));
            EditorGUILayout.LabelField(s.Phase.ToString(), GUILayout.Width(120));

            EditorGUILayout.LabelField("Time:", GUILayout.Width(40));
            var mm = s.TimeRemaining / 60;
            var ss = s.TimeRemaining % 60;
            EditorGUILayout.LabelField($"{mm:00}:{ss:00}", GUILayout.Width(60));
        }
    }

    private static void DrawFatigueInjury(MatchSnapshot s)
    {
        GUILayout.Label("Rotations & Injuries", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("INT H/A:", GUILayout.Width(70));
            EditorGUILayout.LabelField($"{s.HomeInterchanges}/{s.AwayInterchanges}", GUILayout.Width(80));

            GUILayout.Space(12);

            EditorGUILayout.LabelField("INJ H/A:", GUILayout.Width(70));
            EditorGUILayout.LabelField($"{s.HomeInjuryEvents}/{s.AwayInjuryEvents}", GUILayout.Width(80));
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("AvgCond H/A:", GUILayout.Width(90));
            EditorGUILayout.LabelField($"{s.HomeAvgConditionEnd}/{s.AwayAvgConditionEnd}", GUILayout.Width(80));
        }
    }

    private static void DrawMeta(MatchSnapshot s)
    {
        GUILayout.Label("Notes", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Counters update every engine tick. Adjust the knobs above and click “Run Quick Match”.", MessageType.None);
    }

    // ---------- Tuning Sliders ----------
    private static void DrawTuningSliders(MatchTuningSO t)
    {
        GUILayout.Label("Injuries", EditorStyles.miniBoldLabel);
        t.InjuryBasePerMinuteRisk = EditorGUILayout.Slider("Base per minute", t.InjuryBasePerMinuteRisk, 0f, 0.005f);
        t.InjuryOpenPlayMult      = EditorGUILayout.Slider("Open play ×",     t.InjuryOpenPlayMult, 0.5f, 1.5f);
        t.InjuryInside50Mult      = EditorGUILayout.Slider("Inside 50 ×",     t.InjuryInside50Mult, 0.5f, 1.5f);
        t.InjuryFatigueScale      = EditorGUILayout.Slider("Fatigue scale",    t.InjuryFatigueScale, 0f, 1f);
        t.InjuryDurabilityScale   = EditorGUILayout.Slider("Durability scale", t.InjuryDurabilityScale, 0f, 1f);
        t.InjuryMaxPerTeam        = EditorGUILayout.IntSlider("Max injuries/team", t.InjuryMaxPerTeam, 0, 6);

        GUILayout.Space(6);
        GUILayout.Label("Weather → Progress", EditorStyles.miniBoldLabel);
        t.WeatherProgressPenalty_Windy     = EditorGUILayout.Slider("Windy penalty",      t.WeatherProgressPenalty_Windy, 0f, 60f);
        t.WeatherProgressPenalty_LightRain = EditorGUILayout.Slider("Light rain penalty", t.WeatherProgressPenalty_LightRain, 0f, 60f);
        t.WeatherProgressPenalty_HeavyRain = EditorGUILayout.Slider("Heavy rain penalty", t.WeatherProgressPenalty_HeavyRain, 0f, 60f);

        GUILayout.Space(6);
        GUILayout.Label("Weather → Shot Accuracy", EditorStyles.miniBoldLabel);
        t.WeatherAccuracyPenalty_Windy     = EditorGUILayout.Slider("Windy penalty",      t.WeatherAccuracyPenalty_Windy, 0f, 0.8f);
        t.WeatherAccuracyPenalty_LightRain = EditorGUILayout.Slider("Light rain penalty", t.WeatherAccuracyPenalty_LightRain, 0f, 0.8f);
        t.WeatherAccuracyPenalty_HeavyRain = EditorGUILayout.Slider("Heavy rain penalty", t.WeatherAccuracyPenalty_HeavyRain, 0f, 0.8f);

        GUILayout.Space(6);
        GUILayout.Label("Engine Baselines", EditorStyles.miniBoldLabel);
        t.ProgressBase         = EditorGUILayout.Slider("Progress base",          t.ProgressBase, 0f, 1f);
        t.ProgressScaleDivisor = EditorGUILayout.Slider("Progress scale divisor", t.ProgressScaleDivisor, 1f, 800f);
        t.ShotBaseGoal         = EditorGUILayout.Slider("Shot base goal",         t.ShotBaseGoal, 0f, 1f);
        t.ShotScaleWithQual    = EditorGUILayout.Slider("Shot × with quality",    t.ShotScaleWithQual, 0f, 1f);
    }

    // ---------- Helpers ----------
    private static List<Player> MakeUniformRoster(int rating, int count)
    {
        var list = new List<Player>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new Player
            {
                Name = "P" + i,
                Endurance = rating,
                Durability = rating,
                Discipline = 60,
                Attr = new Attributes
                {
                    WorkRate = rating, DecisionMaking = rating, Kicking = rating, Marking = rating,
                    Clearance = rating, Strength = rating, Positioning = rating, Tackling = rating, Spoiling = rating
                }
            });
        }
        return list;
    }
}
#endif