// Assets/Editor/MatchFlowUIBuilder.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace AFLManager.Editor
{
    /// <summary>
    /// Unity Editor script to automatically build the MatchFlow scene UI
    /// Usage: Tools → AFL Coach Sim → Build MatchFlow UI
    /// </summary>
    public class MatchFlowUIBuilder : EditorWindow
    {
        [MenuItem("Tools/AFL Coach Sim/Build MatchFlow UI")]
        [MenuItem("Window/AFL Coach Sim/Build MatchFlow UI")]
        public static void ShowWindow()
        {
            GetWindow<MatchFlowUIBuilder>("MatchFlow UI Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("MatchFlow Scene UI Builder", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will automatically create the UI hierarchy for the MatchFlow scene.\n\n" +
                "Make sure you have the MatchFlow scene open before clicking Build.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Build Complete UI Hierarchy", GUILayout.Height(40)))
            {
                BuildCompleteUI();
            }

            GUILayout.Space(20);
            GUILayout.Label("Individual Components:", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Setup Canvas & EventSystem"))
            {
                SetupCanvasAndEventSystem();
            }

            if (GUILayout.Button("2. Build Pre-Match Screen"))
            {
                BuildPreMatchScreen();
            }

            if (GUILayout.Button("3. Build Simulation Screen"))
            {
                BuildSimulationScreen();
            }

            if (GUILayout.Button("4. Build Post-Match Screen"))
            {
                BuildPostMatchScreen();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Create UI Prefabs"))
            {
                CreateUIPrefabs();
            }
        }

        private static void BuildCompleteUI()
        {
            if (!EditorUtility.DisplayDialog("Build MatchFlow UI",
                "This will create the complete UI hierarchy. Any existing UI elements may be affected. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }

            SetupCanvasAndEventSystem();
            BuildPreMatchScreen();
            BuildSimulationScreen();
            BuildPostMatchScreen();

            Debug.Log("[MatchFlowUIBuilder] Complete UI hierarchy built successfully!");
            EditorUtility.DisplayDialog("Success", "MatchFlow UI built successfully!\n\nNext steps:\n1. Create prefabs\n2. Assign references using Auto-Wiring tool", "OK");
        }

        private static void SetupCanvasAndEventSystem()
        {
            // Check for existing Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();

                Debug.Log("[MatchFlowUIBuilder] Canvas created");
            }

            // Check for EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                Debug.Log("[MatchFlowUIBuilder] EventSystem created");
            }
        }

        private static void BuildPreMatchScreen()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MatchFlowUIBuilder] Canvas not found! Create canvas first.");
                return;
            }

            // Create or find PreMatchScreen
            Transform preMatchScreen = canvas.transform.Find("PreMatchScreen");
            GameObject preMatchObj;

            if (preMatchScreen == null)
            {
                preMatchObj = CreatePanel(canvas.transform, "PreMatchScreen");
            }
            else
            {
                preMatchObj = preMatchScreen.gameObject;
                Debug.Log("[MatchFlowUIBuilder] Using existing PreMatchScreen");
            }

            // Add MatchPreviewUI component if not present
            if (preMatchObj.GetComponent<AFLManager.Managers.MatchPreviewUI>() == null)
            {
                preMatchObj.AddComponent<AFLManager.Managers.MatchPreviewUI>();
            }

            RectTransform preMatchRect = preMatchObj.GetComponent<RectTransform>();
            SetStretchAnchors(preMatchRect);

            // Background
            CreateBackground(preMatchObj.transform);

            // Header Section
            CreatePreMatchHeader(preMatchObj.transform);

            // Team Comparison Section
            CreateTeamComparison(preMatchObj.transform);

            // Lineups Section
            CreateLineups(preMatchObj.transform);

            // Controls
            CreatePreMatchControls(preMatchObj.transform);

            Debug.Log("[MatchFlowUIBuilder] Pre-Match Screen built");
        }

        private static void BuildSimulationScreen()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MatchFlowUIBuilder] Canvas not found!");
                return;
            }

            Transform simScreen = canvas.transform.Find("SimulationScreen");
            GameObject simObj;

            if (simScreen == null)
            {
                simObj = CreatePanel(canvas.transform, "SimulationScreen");
            }
            else
            {
                simObj = simScreen.gameObject;
            }

            // Add MatchSimulationUI component
            if (simObj.GetComponent<AFLManager.Managers.MatchSimulationUI>() == null)
            {
                simObj.AddComponent<AFLManager.Managers.MatchSimulationUI>();
            }

            RectTransform simRect = simObj.GetComponent<RectTransform>();
            SetStretchAnchors(simRect);
            simObj.SetActive(false); // Initially inactive

            // Background
            CreateBackground(simObj.transform);

            // Score Display
            CreateScoreDisplay(simObj.transform);

            // Progress Section
            CreateProgressSection(simObj.transform);

            // Commentary Section
            CreateCommentarySection(simObj.transform);

            Debug.Log("[MatchFlowUIBuilder] Simulation Screen built");
        }

        private static void BuildPostMatchScreen()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MatchFlowUIBuilder] Canvas not found!");
                return;
            }

            Transform postScreen = canvas.transform.Find("PostMatchScreen");
            GameObject postObj;

            if (postScreen == null)
            {
                postObj = CreatePanel(canvas.transform, "PostMatchScreen");
            }
            else
            {
                postObj = postScreen.gameObject;
            }

            // Add MatchResultsUI component
            if (postObj.GetComponent<AFLManager.Managers.MatchResultsUI>() == null)
            {
                postObj.AddComponent<AFLManager.Managers.MatchResultsUI>();
            }

            RectTransform postRect = postObj.GetComponent<RectTransform>();
            SetStretchAnchors(postRect);
            postObj.SetActive(false); // Initially inactive

            // Background
            CreateBackground(postObj.transform);

            // Result Header
            CreateResultHeader(postObj.transform);

            // Final Score
            CreateFinalScoreSection(postObj.transform);

            // Quarter Scores
            CreateQuarterScoresSection(postObj.transform);

            // Statistics
            CreateStatisticsSection(postObj.transform);

            // Highlights
            CreateHighlightsSection(postObj.transform);

            // Controls
            CreatePostMatchControls(postObj.transform);

            Debug.Log("[MatchFlowUIBuilder] Post-Match Screen built");
        }

        // ========================
        // PRE-MATCH SCREEN HELPERS
        // ========================

        private static void CreatePreMatchHeader(Transform parent)
        {
            GameObject header = CreateEmpty(parent, "Header");
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0, -50);
            headerRect.sizeDelta = new Vector2(1800, 100);

            VerticalLayoutGroup vlg = header.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 5;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateText(header.transform, "RoundText", "Round 1", 24);
            CreateText(header.transform, "VenueText", "MCG", 24);
            CreateText(header.transform, "DateText", "Saturday, June 1 2024", 24);
        }

        private static void CreateTeamComparison(Transform parent)
        {
            GameObject comparison = CreateEmpty(parent, "TeamComparison");
            RectTransform compRect = comparison.GetComponent<RectTransform>();
            compRect.anchorMin = new Vector2(0.5f, 1f);
            compRect.anchorMax = new Vector2(0.5f, 1f);
            compRect.pivot = new Vector2(0.5f, 1f);
            compRect.anchoredPosition = new Vector2(0, -200);
            compRect.sizeDelta = new Vector2(1600, 200);

            HorizontalLayoutGroup hlg = comparison.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 20;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Left Column (Home Team)
            GameObject leftCol = CreateEmpty(comparison.transform, "LeftColumn");
            VerticalLayoutGroup leftVlg = leftCol.AddComponent<VerticalLayoutGroup>();
            leftVlg.childAlignment = TextAnchor.MiddleCenter;
            CreateText(leftCol.transform, "HomeTeamName", "Home Team", 32, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(leftCol.transform, "HomeRating", "75.0", 28);
            CreateText(leftCol.transform, "HomeForm", "WWLWD", 20, TextAlignmentOptions.Center, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));

            // Center Column
            GameObject centerCol = CreateEmpty(comparison.transform, "CenterColumn");
            VerticalLayoutGroup centerVlg = centerCol.AddComponent<VerticalLayoutGroup>();
            centerVlg.childAlignment = TextAnchor.MiddleCenter;
            CreateText(centerCol.transform, "VSText", "VS", 36, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateSlider(centerCol.transform, "ComparisonSlider");

            // Right Column (Away Team)
            GameObject rightCol = CreateEmpty(comparison.transform, "RightColumn");
            VerticalLayoutGroup rightVlg = rightCol.AddComponent<VerticalLayoutGroup>();
            rightVlg.childAlignment = TextAnchor.MiddleCenter;
            CreateText(rightCol.transform, "AwayTeamName", "Away Team", 32, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(rightCol.transform, "AwayRating", "72.0", 28);
            CreateText(rightCol.transform, "AwayForm", "LWLWL", 20, TextAlignmentOptions.Center, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
        }

        private static void CreateLineups(Transform parent)
        {
            GameObject lineups = CreateEmpty(parent, "Lineups");
            RectTransform lineupsRect = lineups.GetComponent<RectTransform>();
            lineupsRect.anchorMin = new Vector2(0, 0);
            lineupsRect.anchorMax = new Vector2(1, 1);
            lineupsRect.offsetMin = new Vector2(50, 100);
            lineupsRect.offsetMax = new Vector2(-50, -400);

            HorizontalLayoutGroup hlg = lineups.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateScrollView(lineups.transform, "HomeLineupScrollView", "HomeLineupContainer");
            CreateScrollView(lineups.transform, "AwayLineupScrollView", "AwayLineupContainer");
        }

        private static void CreatePreMatchControls(Transform parent)
        {
            GameObject controls = CreateEmpty(parent, "Controls");
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.5f, 0f);
            controlsRect.anchorMax = new Vector2(0.5f, 0f);
            controlsRect.pivot = new Vector2(0.5f, 0f);
            controlsRect.anchoredPosition = new Vector2(0, 50);
            controlsRect.sizeDelta = new Vector2(800, 60);

            HorizontalLayoutGroup hlg = controls.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            CreateButton(controls.transform, "BackButton", "Back", new Color(0.24f, 0.24f, 0.24f));
            CreateButton(controls.transform, "StartMatchButton", "Start Match", new Color(0.3f, 0.69f, 0.31f));
        }

        // ========================
        // SIMULATION SCREEN HELPERS
        // ========================

        private static void CreateScoreDisplay(Transform parent)
        {
            GameObject scoreDisplay = CreateEmpty(parent, "ScoreDisplay");
            RectTransform scoreRect = scoreDisplay.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0, 1);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.pivot = new Vector2(0.5f, 1);
            scoreRect.anchoredPosition = Vector2.zero;
            scoreRect.sizeDelta = new Vector2(0, 120);

            HorizontalLayoutGroup hlg = scoreDisplay.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 30;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateText(scoreDisplay.transform, "HomeTeamName", "Home", 28, TextAlignmentOptions.Right, FontStyles.Bold);
            CreateText(scoreDisplay.transform, "HomeScore", "0", 48, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(scoreDisplay.transform, "VSText", "vs", 24, TextAlignmentOptions.Center, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
            CreateText(scoreDisplay.transform, "AwayScore", "0", 48, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(scoreDisplay.transform, "AwayTeamName", "Away", 28, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        private static void CreateProgressSection(Transform parent)
        {
            GameObject progress = CreateEmpty(parent, "ProgressSection");
            RectTransform progressRect = progress.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.5f, 1f);
            progressRect.anchorMax = new Vector2(0.5f, 1f);
            progressRect.pivot = new Vector2(0.5f, 1f);
            progressRect.anchoredPosition = new Vector2(0, -180);
            progressRect.sizeDelta = new Vector2(1400, 150);

            VerticalLayoutGroup vlg = progress.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            CreateText(progress.transform, "QuarterText", "Q1", 36, TextAlignmentOptions.Center, FontStyles.Bold, Color.yellow);
            CreateSlider(progress.transform, "ProgressBar");
            CreateText(progress.transform, "ProgressText", "20:00 remaining", 20);
        }

        private static void CreateCommentarySection(Transform parent)
        {
            GameObject commentary = CreateEmpty(parent, "CommentarySection");
            RectTransform commentaryRect = commentary.GetComponent<RectTransform>();
            commentaryRect.anchorMin = new Vector2(0, 0);
            commentaryRect.anchorMax = new Vector2(1, 1);
            commentaryRect.offsetMin = new Vector2(100, 0);
            commentaryRect.offsetMax = new Vector2(-100, -350);

            VerticalLayoutGroup vlg = commentary.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Current commentary (large)
            GameObject currentText = CreateText(commentary.transform, "CommentaryText", "Match is about to begin...", 24);
            RectTransform currentRect = currentText.GetComponent<RectTransform>();
            currentRect.sizeDelta = new Vector2(0, 100);
            currentText.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

            // Commentary feed
            CreateScrollView(commentary.transform, "CommentaryFeedScrollView", "CommentaryFeedContainer");
        }

        // ========================
        // POST-MATCH SCREEN HELPERS
        // ========================

        private static void CreateResultHeader(Transform parent)
        {
            GameObject header = CreateText(parent, "ResultHeaderText", "VICTORY!", 48, TextAlignmentOptions.Center, FontStyles.Bold, new Color(0.3f, 0.69f, 0.31f));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0, -50);
            headerRect.sizeDelta = new Vector2(600, 80);
        }

        private static void CreateFinalScoreSection(Transform parent)
        {
            GameObject finalScore = CreateEmpty(parent, "FinalScore");
            RectTransform scoreRect = finalScore.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 1f);
            scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0, -150);
            scoreRect.sizeDelta = new Vector2(1200, 100);

            HorizontalLayoutGroup hlg = finalScore.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateText(finalScore.transform, "HomeTeamName", "Home Team", 28, TextAlignmentOptions.Right, FontStyles.Bold);
            CreateText(finalScore.transform, "HomeScore", "100", 42, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(finalScore.transform, "VSText", "vs", 24, TextAlignmentOptions.Center);
            CreateText(finalScore.transform, "AwayScore", "85", 42, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(finalScore.transform, "AwayTeamName", "Away Team", 28, TextAlignmentOptions.Left, FontStyles.Bold);

            // Margin text
            GameObject margin = CreateText(parent, "MarginText", "Home Team by 15 points", 22, TextAlignmentOptions.Center, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
            RectTransform marginRect = margin.GetComponent<RectTransform>();
            marginRect.anchorMin = new Vector2(0.5f, 1f);
            marginRect.anchorMax = new Vector2(0.5f, 1f);
            marginRect.pivot = new Vector2(0.5f, 1f);
            marginRect.anchoredPosition = new Vector2(0, -260);
            marginRect.sizeDelta = new Vector2(800, 40);
        }

        private static void CreateQuarterScoresSection(Transform parent)
        {
            GameObject quarterScores = CreateEmpty(parent, "QuarterScores");
            RectTransform qsRect = quarterScores.GetComponent<RectTransform>();
            qsRect.anchorMin = new Vector2(0.5f, 1f);
            qsRect.anchorMax = new Vector2(0.5f, 1f);
            qsRect.pivot = new Vector2(0.5f, 1f);
            qsRect.anchoredPosition = new Vector2(0, -320);
            qsRect.sizeDelta = new Vector2(800, 120);

            VerticalLayoutGroup vlg = quarterScores.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;

            // Header row
            GameObject headerRow = CreateEmpty(quarterScores.transform, "HeaderRow");
            HorizontalLayoutGroup headerHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            CreateText(headerRow.transform, "Q1Header", "Q1", 18, TextAlignmentOptions.Center, FontStyles.Bold, new Color(0.7f, 0.7f, 0.7f));
            CreateText(headerRow.transform, "Q2Header", "Q2", 18, TextAlignmentOptions.Center, FontStyles.Bold, new Color(0.7f, 0.7f, 0.7f));
            CreateText(headerRow.transform, "Q3Header", "Q3", 18, TextAlignmentOptions.Center, FontStyles.Bold, new Color(0.7f, 0.7f, 0.7f));
            CreateText(headerRow.transform, "Q4Header", "Q4", 18, TextAlignmentOptions.Center, FontStyles.Bold, new Color(0.7f, 0.7f, 0.7f));

            // Home row
            GameObject homeRow = CreateEmpty(quarterScores.transform, "HomeRow");
            HorizontalLayoutGroup homeHlg = homeRow.AddComponent<HorizontalLayoutGroup>();
            CreateText(homeRow.transform, "HomeQ1", "25", 20, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(homeRow.transform, "HomeQ2", "25", 20, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(homeRow.transform, "HomeQ3", "25", 20, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(homeRow.transform, "HomeQ4", "25", 20, TextAlignmentOptions.Center, FontStyles.Bold);

            // Away row
            GameObject awayRow = CreateEmpty(quarterScores.transform, "AwayRow");
            HorizontalLayoutGroup awayHlg = awayRow.AddComponent<HorizontalLayoutGroup>();
            CreateText(awayRow.transform, "AwayQ1", "20", 20, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(awayRow.transform, "AwayQ2", "20", 20, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(awayRow.transform, "AwayQ3", "22", 20, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateText(awayRow.transform, "AwayQ4", "23", 20, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private static void CreateStatisticsSection(Transform parent)
        {
            GameObject stats = CreateEmpty(parent, "Statistics");
            RectTransform statsRect = stats.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 1f);
            statsRect.anchorMax = new Vector2(0.5f, 1f);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.anchoredPosition = new Vector2(0, -480);
            statsRect.sizeDelta = new Vector2(800, 150);

            VerticalLayoutGroup vlg = stats.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;

            CreateStatRow(stats.transform, "DisposalsRow", "Disposals", "HomeDisposals", "AwayDisposals");
            CreateStatRow(stats.transform, "MarksRow", "Marks", "HomeMarks", "AwayMarks");
            CreateStatRow(stats.transform, "TacklesRow", "Tackles", "HomeTackles", "AwayTackles");
        }

        private static void CreateHighlightsSection(Transform parent)
        {
            GameObject highlights = CreateEmpty(parent, "HighlightsSection");
            RectTransform highlightsRect = highlights.GetComponent<RectTransform>();
            highlightsRect.anchorMin = new Vector2(0, 0);
            highlightsRect.anchorMax = new Vector2(1, 1);
            highlightsRect.offsetMin = new Vector2(100, 100);
            highlightsRect.offsetMax = new Vector2(-100, -650);

            VerticalLayoutGroup vlg = highlights.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;

            CreateText(highlights.transform, "HighlightsTitle", "Match Highlights", 24, TextAlignmentOptions.Center, FontStyles.Bold);
            CreateScrollView(highlights.transform, "HighlightsScrollView", "HighlightsContainer");
        }

        private static void CreatePostMatchControls(Transform parent)
        {
            GameObject controls = CreateEmpty(parent, "Controls");
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.5f, 0f);
            controlsRect.anchorMax = new Vector2(0.5f, 0f);
            controlsRect.pivot = new Vector2(0.5f, 0f);
            controlsRect.anchoredPosition = new Vector2(0, 50);
            controlsRect.sizeDelta = new Vector2(600, 60);

            HorizontalLayoutGroup hlg = controls.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            CreateButton(controls.transform, "ContinueButton", "Continue", new Color(0.3f, 0.69f, 0.31f));
            CreateButton(controls.transform, "ViewStatsButton", "View Detailed Stats", new Color(0.4f, 0.4f, 0.4f));
        }

        // ========================
        // UTILITY METHODS
        // ========================

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            panel.AddComponent<CanvasRenderer>();
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0.17f, 0.17f, 0.17f, 1f); // Dark gray
            return panel;
        }

        private static GameObject CreateEmpty(Transform parent, string name)
        {
            GameObject empty = new GameObject(name);
            empty.transform.SetParent(parent, false);
            empty.AddComponent<RectTransform>();
            return empty;
        }

        private static void CreateBackground(Transform parent)
        {
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(parent, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            SetStretchAnchors(bgRect);
            bg.AddComponent<CanvasRenderer>();
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.17f, 0.17f, 0.17f, 1f);
            bg.transform.SetAsFirstSibling(); // Render behind
        }

        private static GameObject CreateText(Transform parent, string name, string text, float fontSize = 16,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal, Color? color = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.color = color ?? Color.white;

            return textObj;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Color color)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            Image img = buttonObj.AddComponent<Image>();
            img.color = color;

            Button btn = buttonObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            SetStretchAnchors(textRect);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return buttonObj;
        }

        private static GameObject CreateSlider(Transform parent, string name)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 30);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.5f;
            slider.interactable = false;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            SetStretchAnchors(bgRect);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            SetStretchAnchors(fillAreaRect);

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.69f, 0.31f, 1f);

            slider.fillRect = fillRect;

            return sliderObj;
        }

        private static void CreateScrollView(Transform parent, string name, string contentName)
        {
            GameObject scrollView = new GameObject(name);
            scrollView.transform.SetParent(parent, false);
            RectTransform svRect = scrollView.AddComponent<RectTransform>();
            svRect.sizeDelta = new Vector2(400, 400);

            Image svImg = scrollView.AddComponent<Image>();
            svImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            ScrollRect sr = scrollView.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            SetStretchAnchors(vpRect);
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = new GameObject(contentName);
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRect;
            sr.content = contentRect;

            // Scrollbar
            GameObject scrollbar = new GameObject("Scrollbar Vertical");
            scrollbar.transform.SetParent(scrollView.transform, false);
            RectTransform sbRect = scrollbar.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1, 0);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(1, 1);
            sbRect.sizeDelta = new Vector2(20, 0);

            Image sbImg = scrollbar.AddComponent<Image>();
            sbImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Scrollbar sb = scrollbar.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;

            GameObject sbHandle = new GameObject("Sliding Area");
            sbHandle.transform.SetParent(scrollbar.transform, false);
            RectTransform sbhRect = sbHandle.AddComponent<RectTransform>();
            SetStretchAnchors(sbhRect);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(sbHandle.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            SetStretchAnchors(handleRect);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);

            sb.handleRect = handleRect;
            sr.verticalScrollbar = sb;
        }

        private static void CreateStatRow(Transform parent, string rowName, string label, string homeStatName, string awayStatName)
        {
            GameObject row = CreateEmpty(parent, rowName);
            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateText(row.transform, homeStatName, "100", 20, TextAlignmentOptions.Right, FontStyles.Bold);
            CreateText(row.transform, "StatLabel", label, 18, TextAlignmentOptions.Center, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
            CreateText(row.transform, awayStatName, "90", 20, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        private static void SetStretchAnchors(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateUIPrefabs()
        {
            string prefabPath = "Assets/Prefabs/UI";
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
            }

            // PlayerLineupEntry
            CreatePlayerLineupEntryPrefab(prefabPath);

            // CommentaryEntry
            CreateCommentaryEntryPrefab(prefabPath);

            // HighlightEntry
            CreateHighlightEntryPrefab(prefabPath);

            AssetDatabase.Refresh();
            Debug.Log("[MatchFlowUIBuilder] UI Prefabs created successfully!");
            EditorUtility.DisplayDialog("Success", "UI Prefabs created in Assets/Prefabs/UI/", "OK");
        }

        private static void CreatePlayerLineupEntryPrefab(string path)
        {
            GameObject entry = new GameObject("PlayerLineupEntry");
            RectTransform rect = entry.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 30);

            Image img = entry.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            LayoutElement le = entry.AddComponent<LayoutElement>();
            le.minHeight = 30;
            le.preferredHeight = 30;

            GameObject text = new GameObject("PlayerText");
            text.transform.SetParent(entry.transform, false);
            RectTransform textRect = text.AddComponent<RectTransform>();
            SetStretchAnchors(textRect);
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "Player Name - 75";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            PrefabUtility.SaveAsPrefabAsset(entry, $"{path}/PlayerLineupEntry.prefab");
            DestroyImmediate(entry);
        }

        private static void CreateCommentaryEntryPrefab(string path)
        {
            GameObject entry = new GameObject("CommentaryEntry");
            RectTransform rect = entry.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 40);

            Image img = entry.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            LayoutElement le = entry.AddComponent<LayoutElement>();
            le.minHeight = 40;
            le.preferredHeight = 40;

            GameObject text = new GameObject("CommentaryText");
            text.transform.SetParent(entry.transform, false);
            RectTransform textRect = text.AddComponent<RectTransform>();
            SetStretchAnchors(textRect);
            textRect.offsetMin = new Vector2(15, 0);
            textRect.offsetMax = new Vector2(-15, 0);

            TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "Commentary text goes here...";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;
            tmp.color = Color.white;

            PrefabUtility.SaveAsPrefabAsset(entry, $"{path}/CommentaryEntry.prefab");
            DestroyImmediate(entry);
        }

        private static void CreateHighlightEntryPrefab(string path)
        {
            GameObject entry = new GameObject("HighlightEntry");
            RectTransform rect = entry.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            Image img = entry.AddComponent<Image>();
            img.color = new Color(0.24f, 0.24f, 0.24f, 1f);

            LayoutElement le = entry.AddComponent<LayoutElement>();
            le.minHeight = 50;
            le.preferredHeight = 50;

            GameObject text = new GameObject("HighlightText");
            text.transform.SetParent(entry.transform, false);
            RectTransform textRect = text.AddComponent<RectTransform>();
            SetStretchAnchors(textRect);
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "Q1 - Highlight moment description";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;
            tmp.color = new Color(1f, 1f, 0.78f, 1f); // Light yellow

            PrefabUtility.SaveAsPrefabAsset(entry, $"{path}/HighlightEntry.prefab");
            DestroyImmediate(entry);
        }
    }
}
