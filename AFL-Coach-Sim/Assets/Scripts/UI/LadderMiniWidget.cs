// File: Assets/Scripts/UI/LadderMiniWidget.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Serialization; // for renamed fields
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AFLManager.UI
{
    /// <summary>
    /// Mini ladder renderer with aggressive safety checks and clear logs.
    /// </summary>
    public class LadderMiniWidget : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Transform contentParent;   // Must point to a RectTransform that holds rows
        [SerializeField] private LadderMiniRow rowPrefab;   // Prefab with LadderMiniRow script

        [Header("Auto-fix (optional)")]
        [SerializeField] private bool autoFixLayout = true;
        [SerializeField] private float rowSpacing = 6f;

        RectTransform ContentRT => contentParent ? contentParent as RectTransform : null;

        void Awake()
        {
            if (autoFixLayout) EnsureLayout();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (autoFixLayout) EnsureLayout();
        }
#endif

        void EnsureLayout()
        {
            if (!ContentRT)
            {
                Debug.LogWarning($"[LadderMiniWidget] contentParent not set or not a RectTransform on {name}.", this);
                return;
            }

            // Make sure the Content stretches and sizes with its parent
            var t = ContentRT;
            t.anchorMin = new Vector2(0, 1);
            t.anchorMax = new Vector2(1, 1);
            t.pivot     = new Vector2(0.5f, 1f);
            // don't force offsets; respect designer's top inset

            var vlg = t.GetComponent<VerticalLayoutGroup>() ?? t.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = rowSpacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = t.GetComponent<ContentSizeFitter>() ?? t.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void Render(List<AFLManager.Models.LadderEntry> entries)
        {
            if (!contentParent)
            {
                Debug.LogError("[LadderMiniWidget] contentParent is NULL. Assign the Content transform.", this);
                return;
            }
            if (!rowPrefab)
            {
                Debug.LogError("[LadderMiniWidget] rowPrefab is NULL. Assign the LadderMiniRow prefab.", this);
                return;
            }

            // Clear
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);

            if (entries == null)
            {
                Debug.LogWarning("[LadderMiniWidget] Render called with null entries.", this);
                return;
            }

            Debug.Log($"[LadderMiniWidget] Rendering {entries.Count} entries into {GetPath(contentParent)}", this);

            int rank = 1;
            foreach (var e in entries)
            {
                var row = Instantiate(rowPrefab, contentParent, false);
                row.Bind(rank, e.TeamName, e.Games, e.Points);
                rank++;
            }

            // Force a layout pass so it becomes visible immediately
            var rt = contentParent as RectTransform;
            if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            Debug.Log($"[LadderMiniWidget] Children after render: {contentParent.childCount}", this);
        }

        static string GetPath(Transform t)
        {
            if (!t) return "<null>";
            var path = t.name;
            var p = t.parent;
            while (p != null) { path = p.name + "/" + path; p = p.parent; }
            return path;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug → Print Wiring")]
        void PrintWiring()
        {
            Debug.Log($"[LadderMiniWidget] contentParent={(contentParent?GetPath(contentParent):"<null>")} rowPrefab={(rowPrefab?rowPrefab.name:"<null>")}", this);
        }
#endif
    }
}
