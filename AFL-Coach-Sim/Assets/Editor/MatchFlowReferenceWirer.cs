// Assets/Editor/MatchFlowReferenceWirer.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Reflection;

namespace AFLManager.Editor
{
    /// <summary>
    /// Automatically finds and assigns component references for MatchFlow scene
    /// Usage: Tools → AFL Coach Sim → Auto-Wire References
    /// </summary>
    public class MatchFlowReferenceWirer : EditorWindow
    {
        [MenuItem("Tools/AFL Coach Sim/Auto-Wire References")]
        [MenuItem("Window/AFL Coach Sim/Auto-Wire References")]
        public static void ShowWindow()
        {
            GetWindow<MatchFlowReferenceWirer>("Reference Auto-Wirer");
        }

        private void OnGUI()
        {
            GUILayout.Label("MatchFlow Reference Auto-Wirer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will automatically find and assign all component references.\n\n" +
                "Make sure the MatchFlow scene is open and UI hierarchy is built.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Wire All References", GUILayout.Height(40)))
            {
                WireAllReferences();
            }

            GUILayout.Space(20);
            GUILayout.Label("Individual Components:", EditorStyles.boldLabel);

            if (GUILayout.Button("Wire MatchFlowManager"))
            {
                WireMatchFlowManager();
            }

            if (GUILayout.Button("Wire MatchPreviewUI"))
            {
                WireMatchPreviewUI();
            }

            if (GUILayout.Button("Wire MatchSimulationUI"))
            {
                WireMatchSimulationUI();
            }

            if (GUILayout.Button("Wire MatchResultsUI"))
            {
                WireMatchResultsUI();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Verify All References"))
            {
                VerifyReferences();
            }
        }

        private static void WireAllReferences()
        {
            if (!EditorUtility.DisplayDialog("Auto-Wire References",
                "This will automatically assign all component references. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }

            int successCount = 0;
            successCount += WireMatchFlowManager();
            successCount += WireMatchPreviewUI();
            successCount += WireMatchSimulationUI();
            successCount += WireMatchResultsUI();

            Debug.Log($"[ReferenceWirer] Successfully wired {successCount} references");
            EditorUtility.DisplayDialog("Success", 
                $"Wired {successCount} references successfully!\n\nCheck Console for details.",
                "OK");
        }

        private static int WireMatchFlowManager()
        {
            // Try to find either MatchFlowManager or AdvancedMatchFlowManager
            MonoBehaviour manager = FindObjectOfType<AFLManager.Managers.MatchFlowManager>() as MonoBehaviour;
            
            if (manager == null)
            {
                // Try AdvancedMatchFlowManager
                manager = FindObjectOfType<AFLManager.Managers.AdvancedMatchFlowManager>() as MonoBehaviour;
            }
            
            if (manager == null)
            {
                Debug.LogWarning("[ReferenceWirer] MatchFlowManager or AdvancedMatchFlowManager not found in scene");
                return 0;
            }

            Debug.Log($"[ReferenceWirer] Found {manager.GetType().Name}");

            int count = 0;
            Canvas canvas = FindObjectOfType<Canvas>();

            // Find screens
            Transform preMatch = canvas.transform.Find("PreMatchScreen");
            Transform simulation = canvas.transform.Find("SimulationScreen");
            Transform postMatch = canvas.transform.Find("PostMatchScreen");

            if (preMatch != null)
            {
                SetField(manager, "preMatchScreen", preMatch.gameObject);
                SetField(manager, "preMatchUI", preMatch.GetComponent<AFLManager.Managers.MatchPreviewUI>());
                count += 2;
            }

            if (simulation != null)
            {
                SetField(manager, "simulationScreen", simulation.gameObject);
                SetField(manager, "simulationUI", simulation.GetComponent<AFLManager.Managers.MatchSimulationUI>());
                count += 2;
            }

            if (postMatch != null)
            {
                SetField(manager, "postMatchScreen", postMatch.gameObject);
                SetField(manager, "resultsUI", postMatch.GetComponent<AFLManager.Managers.MatchResultsUI>());
                count += 2;
            }

            EditorUtility.SetDirty(manager);
            Debug.Log($"[ReferenceWirer] {manager.GetType().Name}: {count} references wired");
            return count;
        }

        private static int WireMatchPreviewUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return 0;

            Transform preMatch = canvas.transform.Find("PreMatchScreen");
            if (preMatch == null)
            {
                Debug.LogWarning("[ReferenceWirer] PreMatchScreen not found");
                return 0;
            }

            var ui = preMatch.GetComponent<AFLManager.Managers.MatchPreviewUI>();
            if (ui == null)
            {
                Debug.LogWarning("[ReferenceWirer] MatchPreviewUI component not found");
                return 0;
            }

            int count = 0;

            // Match Info
            count += WireChild(ui, preMatch, "Header/RoundText", "roundText");
            count += WireChild(ui, preMatch, "Header/VenueText", "venueText");
            count += WireChild(ui, preMatch, "Header/DateText", "dateText");

            // Teams
            count += WireChild(ui, preMatch, "TeamComparison/LeftColumn/HomeTeamName", "homeTeamName");
            count += WireChild(ui, preMatch, "TeamComparison/RightColumn/AwayTeamName", "awayTeamName");

            // Comparison
            count += WireChild(ui, preMatch, "TeamComparison/LeftColumn/HomeRating", "homeRating");
            count += WireChild(ui, preMatch, "TeamComparison/RightColumn/AwayRating", "awayRating");
            count += WireChild(ui, preMatch, "TeamComparison/CenterColumn/ComparisonSlider", "comparisonSlider");
            count += WireChild(ui, preMatch, "TeamComparison/LeftColumn/HomeForm", "homeForm");
            count += WireChild(ui, preMatch, "TeamComparison/RightColumn/AwayForm", "awayForm");

            // Lineups
            count += WireChild(ui, preMatch, "Lineups/HomeLineupScrollView/Viewport/HomeLineupContainer", "homeLineupContainer");
            count += WireChild(ui, preMatch, "Lineups/AwayLineupScrollView/Viewport/AwayLineupContainer", "awayLineupContainer");

            // Prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PlayerLineupEntry.prefab");
            if (prefab != null)
            {
                SetField(ui, "playerLineupEntryPrefab", prefab);
                count++;
            }

            // Controls
            count += WireChild(ui, preMatch, "Controls/StartMatchButton", "startMatchButton");
            count += WireChild(ui, preMatch, "Controls/BackButton", "backButton");

            EditorUtility.SetDirty(ui);
            Debug.Log($"[ReferenceWirer] MatchPreviewUI: {count} references wired");
            return count;
        }

        private static int WireMatchSimulationUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return 0;

            Transform simulation = canvas.transform.Find("SimulationScreen");
            if (simulation == null)
            {
                Debug.LogWarning("[ReferenceWirer] SimulationScreen not found");
                return 0;
            }

            var ui = simulation.GetComponent<AFLManager.Managers.MatchSimulationUI>();
            if (ui == null)
            {
                Debug.LogWarning("[ReferenceWirer] MatchSimulationUI component not found");
                return 0;
            }

            int count = 0;

            // Team Display
            count += WireChild(ui, simulation, "ScoreDisplay/HomeTeamName", "homeTeamName");
            count += WireChild(ui, simulation, "ScoreDisplay/AwayTeamName", "awayTeamName");
            count += WireChild(ui, simulation, "ScoreDisplay/HomeScore", "homeScore");
            count += WireChild(ui, simulation, "ScoreDisplay/AwayScore", "awayScore");

            // Progress
            count += WireChild(ui, simulation, "ProgressSection/QuarterText", "quarterText");
            count += WireChild(ui, simulation, "ProgressSection/ProgressBar", "progressBar");
            count += WireChild(ui, simulation, "ProgressSection/ProgressText", "progressText");

            // Commentary
            count += WireChild(ui, simulation, "CommentarySection/CommentaryText", "commentaryText");
            count += WireChild(ui, simulation, "CommentarySection/CommentaryFeedScrollView/Viewport/CommentaryFeedContainer", "commentaryFeedContainer");

            // Prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CommentaryEntry.prefab");
            if (prefab != null)
            {
                SetField(ui, "commentaryEntryPrefab", prefab);
                count++;
            }

            // Animation settings (already have default values in script)
            SetField(ui, "simulationSpeed", 2f);
            SetField(ui, "updateInterval", 0.1f);
            SetField(ui, "maxCommentaryEntries", 10);
            count += 3;

            EditorUtility.SetDirty(ui);
            Debug.Log($"[ReferenceWirer] MatchSimulationUI: {count} references wired");
            return count;
        }

        private static int WireMatchResultsUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return 0;

            Transform postMatch = canvas.transform.Find("PostMatchScreen");
            if (postMatch == null)
            {
                Debug.LogWarning("[ReferenceWirer] PostMatchScreen not found");
                return 0;
            }

            var ui = postMatch.GetComponent<AFLManager.Managers.MatchResultsUI>();
            if (ui == null)
            {
                Debug.LogWarning("[ReferenceWirer] MatchResultsUI component not found");
                return 0;
            }

            int count = 0;

            // Match Result
            count += WireChild(ui, postMatch, "ResultHeaderText", "resultHeaderText");
            count += WireChild(ui, postMatch, "FinalScore/HomeTeamName", "homeTeamName");
            count += WireChild(ui, postMatch, "FinalScore/AwayTeamName", "awayTeamName");
            count += WireChild(ui, postMatch, "FinalScore/HomeScore", "homeScore");
            count += WireChild(ui, postMatch, "FinalScore/AwayScore", "awayScore");
            count += WireChild(ui, postMatch, "MarginText", "marginText");

            // Quarter Scores
            count += WireChild(ui, postMatch, "QuarterScores/HomeRow/HomeQ1", "homeQ1");
            count += WireChild(ui, postMatch, "QuarterScores/HomeRow/HomeQ2", "homeQ2");
            count += WireChild(ui, postMatch, "QuarterScores/HomeRow/HomeQ3", "homeQ3");
            count += WireChild(ui, postMatch, "QuarterScores/HomeRow/HomeQ4", "homeQ4");
            count += WireChild(ui, postMatch, "QuarterScores/AwayRow/AwayQ1", "awayQ1");
            count += WireChild(ui, postMatch, "QuarterScores/AwayRow/AwayQ2", "awayQ2");
            count += WireChild(ui, postMatch, "QuarterScores/AwayRow/AwayQ3", "awayQ3");
            count += WireChild(ui, postMatch, "QuarterScores/AwayRow/AwayQ4", "awayQ4");

            // Statistics
            count += WireChild(ui, postMatch, "Statistics/DisposalsRow/HomeDisposals", "homeDisposals");
            count += WireChild(ui, postMatch, "Statistics/DisposalsRow/AwayDisposals", "awayDisposals");
            count += WireChild(ui, postMatch, "Statistics/MarksRow/HomeMarks", "homeMarks");
            count += WireChild(ui, postMatch, "Statistics/MarksRow/AwayMarks", "awayMarks");
            count += WireChild(ui, postMatch, "Statistics/TacklesRow/HomeTackles", "homeTackles");
            count += WireChild(ui, postMatch, "Statistics/TacklesRow/AwayTackles", "awayTackles");

            // Highlights
            count += WireChild(ui, postMatch, "HighlightsSection/HighlightsScrollView/Viewport/HighlightsContainer", "highlightsContainer");

            // Prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HighlightEntry.prefab");
            if (prefab != null)
            {
                SetField(ui, "highlightEntryPrefab", prefab);
                count++;
            }

            // Controls
            count += WireChild(ui, postMatch, "Controls/ContinueButton", "continueButton");
            count += WireChild(ui, postMatch, "Controls/ViewStatsButton", "viewStatsButton");

            EditorUtility.SetDirty(ui);
            Debug.Log($"[ReferenceWirer] MatchResultsUI: {count} references wired");
            return count;
        }

        private static void VerifyReferences()
        {
            int totalMissing = 0;
            int totalAssigned = 0;

            totalMissing += VerifyComponent<AFLManager.Managers.MatchFlowManager>("MatchFlowManager", out int m1);
            totalAssigned += m1;

            totalMissing += VerifyComponent<AFLManager.Managers.MatchPreviewUI>("MatchPreviewUI", out int m2);
            totalAssigned += m2;

            totalMissing += VerifyComponent<AFLManager.Managers.MatchSimulationUI>("MatchSimulationUI", out int m3);
            totalAssigned += m3;

            totalMissing += VerifyComponent<AFLManager.Managers.MatchResultsUI>("MatchResultsUI", out int m4);
            totalAssigned += m4;

            string message = $"Reference Verification Complete!\n\n" +
                             $"✓ Assigned: {totalAssigned}\n" +
                             $"✗ Missing: {totalMissing}\n\n" +
                             (totalMissing > 0 ? "Check Console for details." : "All references look good!");

            EditorUtility.DisplayDialog("Verification Results", message, "OK");
        }

        private static int VerifyComponent<T>(string componentName, out int assignedCount) where T : MonoBehaviour
        {
            assignedCount = 0;
            int missingCount = 0;

            T component = FindObjectOfType<T>();
            if (component == null)
            {
                Debug.LogWarning($"[Verifier] {componentName} not found in scene");
                return 0;
            }

            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                // Check if field has SerializeField attribute
                bool isSerializeField = field.IsPublic || 
                    System.Attribute.IsDefined(field, typeof(SerializeField));

                if (!isSerializeField) continue;

                object value = field.GetValue(component);

                if (value == null || value.Equals(null))
                {
                    Debug.LogWarning($"[Verifier] {componentName}.{field.Name} is not assigned");
                    missingCount++;
                }
                else
                {
                    assignedCount++;
                }
            }

            if (missingCount == 0)
            {
                Debug.Log($"[Verifier] {componentName}: All {assignedCount} references assigned ✓");
            }
            else
            {
                Debug.LogWarning($"[Verifier] {componentName}: {missingCount} missing, {assignedCount} assigned");
            }

            return missingCount;
        }

        // ========================
        // UTILITY METHODS
        // ========================

        private static int WireChild<T>(MonoBehaviour target, Transform parent, string childPath, string fieldName) where T : Component
        {
            Transform child = parent.Find(childPath);
            if (child == null)
            {
                Debug.LogWarning($"[ReferenceWirer] Could not find: {childPath}");
                return 0;
            }

            T component = child.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"[ReferenceWirer] No {typeof(T).Name} on: {childPath}");
                return 0;
            }

            SetField(target, fieldName, component);
            return 1;
        }

        private static int WireChild(MonoBehaviour target, Transform parent, string childPath, string fieldName)
        {
            Transform child = parent.Find(childPath);
            if (child == null)
            {
                Debug.LogWarning($"[ReferenceWirer] Could not find: {childPath}");
                return 0;
            }

            // Try to find the right component type
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                SetField(target, fieldName, tmp);
                return 1;
            }

            var button = child.GetComponent<Button>();
            if (button != null)
            {
                SetField(target, fieldName, button);
                return 1;
            }

            var slider = child.GetComponent<Slider>();
            if (slider != null)
            {
                SetField(target, fieldName, slider);
                return 1;
            }

            var image = child.GetComponent<Image>();
            if (image != null)
            {
                SetField(target, fieldName, image);
                return 1;
            }

            // Just use the Transform
            SetField(target, fieldName, child);
            return 1;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            if (field == null)
            {
                Debug.LogWarning($"[ReferenceWirer] Field not found: {fieldName} on {target.GetType().Name}");
                return;
            }

            field.SetValue(target, value);
            Debug.Log($"[ReferenceWirer] Set {target.GetType().Name}.{fieldName} = {value}");
        }
    }
}
