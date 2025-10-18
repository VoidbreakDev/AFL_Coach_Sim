using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System;

namespace CodeSculptLabs.UIFramework.Editor
{
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : UnityEditor.Editor
    {
        private UIManager uiManager;
        private Dictionary<Transform, bool> foldoutStates = new Dictionary<Transform, bool>();
        private string searchQuery = string.Empty;

        private void OnEnable()
        {
            uiManager = (UIManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Manager Editor", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            searchQuery = EditorGUILayout.TextField("Search", searchQuery);

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh UI Hierarchy"))
            {
                RefreshUIHierarchy();
            }

            EditorGUILayout.Space();
            DisplayUIHierarchy();

            EditorGUILayout.Space();
            if (GUILayout.Button("Update UI Library"))
            {
                UpdateUILibrary();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Open UI Library"))
            {
                OpenUILibrary();
            }
        }

        private void RefreshUIHierarchy()
        {
            foldoutStates.Clear();
        }

        private void DisplayUIHierarchy()
        {
            var canvases = FindObjectsOfType<Canvas>();

            if (canvases.Length == 0)
            {
                EditorGUILayout.HelpBox("No Canvases found in the scene.", MessageType.Info);
                return;
            }

            foreach (var canvas in canvases)
            {
                DrawTransformHierarchy(canvas.transform);
            }
        }

        private void DrawTransformHierarchy(Transform transform, int indent = 0)
        {
            bool isMatchingSearch = string.IsNullOrEmpty(searchQuery) || transform.name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isMatchingSearch)
            {
                foreach (Transform child in transform)
                {
                    DrawTransformHierarchy(child, indent + 1);
                }
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 15);

            bool foldout = foldoutStates.ContainsKey(transform) ? foldoutStates[transform] : false;
            if (transform.childCount > 0)
            {
                foldout = EditorGUILayout.Foldout(foldout, transform.name, true);
                foldoutStates[transform] = foldout;
            }
            else
            {
                EditorGUILayout.LabelField(transform.name);
            }

            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                uiManager.AddUIReference(transform.gameObject);
                EditorUtility.SetDirty(uiManager);
            }

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                foreach (Transform child in transform)
                {
                    DrawTransformHierarchy(child, indent + 1);
                }
            }
        }

        private void UpdateUILibrary()
        {
            try
            {
                string filePath = GetLibraryFilePath();
                string fileContent = File.ReadAllText(filePath);

                var uiElements = uiManager.GetAllUICategories()
                    .SelectMany(category => category.references.Select(reference => new { Category = category.name.ToUpper(), Reference = reference }))
                    .GroupBy(item => item.Category);

                foreach (var group in uiElements)
                {
                    string regionNamePath = $"{group.Key}_PATH";
                    string regionNameID = $"{group.Key}_ID";

                    string pathContent = string.Join("\n", group.Select(item => GenerateConstantDeclaration(item.Reference.name + "_Path", item.Reference.fullPath)));
                    string idContent = string.Join("\n", group.Select(item => GenerateConstantDeclaration(item.Reference.name + "_ID", item.Reference.instanceID)));

                    fileContent = InsertContentIntoRegion(fileContent, regionNamePath, pathContent);
                    fileContent = InsertContentIntoRegion(fileContent, regionNameID, idContent);
                }

                File.WriteAllText(filePath, fileContent);
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(target);

                Debug.Log("UI Library updated successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update UI Library: {ex.Message}");
            }
        }

        private void OpenUILibrary()
        {
            string filePath = GetLibraryFilePath();
            EditorUtility.RevealInFinder(filePath);
        }

        private string GenerateConstantDeclaration(string constantName, string value)
        {
            return $"        public const string {constantName} = \"{value}\";";
        }

        private string GetLibraryFilePath()
        {
            string[] guids = AssetDatabase.FindAssets("UI_Library");
            if (guids.Length == 0)
            {
                throw new FileNotFoundException("UI_Library.cs not found in the project.");
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return Path.Combine(Application.dataPath.Replace("Assets", ""), path);
        }

        private string InsertContentIntoRegion(string content, string regionName, string insertion)
        {
            string pattern = $@"(#region {Regex.Escape(regionName)})(.*?)(#endregion)";
            string replacement = $"$1\n{insertion}\n    $3";
            return Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
        }
    }
}
