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
    /// Mini ladder renderer with object pooling for zero-allocation updates
    /// PERFORMANCE: Reuses existing row GameObjects instead of destroying/recreating
    /// </summary>
    public class LadderMiniWidget : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Transform contentParent;   // Must point to a RectTransform that holds rows
        [SerializeField] private LadderMiniRow rowPrefab;   // Prefab with LadderMiniRow script

        [Header("Auto-fix (optional)")]
        [SerializeField] private bool autoFixLayout = true;
        [SerializeField] private float rowSpacing = 6f;

        [Header("Performance")]
        [SerializeField] private bool enableObjectPooling = true;  // Toggle pooling on/off
        [SerializeField] private bool logPoolingStats = false;      // Log allocation savings

        // Object pool - reuse rows instead of destroying/creating
        private List<LadderMiniRow> _pooledRows = new List<LadderMiniRow>();
        private int _poolingStatsCreated = 0;
        private int _poolingStatsReused = 0;

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

            if (entries == null)
            {
                Debug.LogWarning("[LadderMiniWidget] Render called with null entries.", this);
                return;
            }

            // Use optimized pooling path or fallback to original behavior
            if (enableObjectPooling)
            {
                RenderWithPooling(entries);
            }
            else
            {
                RenderWithoutPooling(entries);
            }
        }

        /// <summary>
        /// OPTIMIZED: Reuse existing row GameObjects instead of destroying/creating
        /// Reduces allocations from ~200 per update to nearly zero
        /// </summary>
        private void RenderWithPooling(List<AFLManager.Models.LadderEntry> entries)
        {
            int requiredRows = entries.Count;
            int currentRows = _pooledRows.Count;
            int rowsReused = 0;
            int rowsCreated = 0;

            // Step 1: Reuse existing rows
            for (int i = 0; i < requiredRows; i++)
            {
                LadderMiniRow row;

                if (i < currentRows)
                {
                    // Reuse existing row
                    row = _pooledRows[i];
                    row.gameObject.SetActive(true);
                    rowsReused++;
                }
                else
                {
                    // Create new row (only if pool is too small)
                    row = Instantiate(rowPrefab, contentParent, false);
                    _pooledRows.Add(row);
                    rowsCreated++;
                }

                // Update row data (same for reused and new rows)
                int rank = i + 1;
                row.Bind(rank, entries[i].TeamName, entries[i].Games, entries[i].Points);
            }

            // Step 2: Deactivate extra rows (if we have more than needed)
            for (int i = requiredRows; i < currentRows; i++)
            {
                _pooledRows[i].gameObject.SetActive(false);
            }

            // Update stats
            _poolingStatsReused += rowsReused;
            _poolingStatsCreated += rowsCreated;

            // Log pooling performance
            if (logPoolingStats)
            {
                Debug.Log($"[LadderMiniWidget] POOLING: Rendered {requiredRows} rows (Reused: {rowsReused}, Created: {rowsCreated}, Total in pool: {_pooledRows.Count})");
                Debug.Log($"[LadderMiniWidget] POOLING STATS: Total reused: {_poolingStatsReused}, Total created: {_poolingStatsCreated}, Allocation savings: {CalculateAllocationSavings()}%");
            }
        }

        /// <summary>
        /// FALLBACK: Original implementation without pooling (for comparison/debugging)
        /// Creates ~200 allocations per update - not recommended for production
        /// </summary>
        private void RenderWithoutPooling(List<AFLManager.Models.LadderEntry> entries)
        {
            Debug.LogWarning("[LadderMiniWidget] Using non-pooled rendering! Enable pooling for better performance.", this);

            // Clear all children
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);

            // Clear pool since we're not using it
            _pooledRows.Clear();

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

        /// <summary>
        /// Clear the object pool (useful when switching scenes or resetting)
        /// </summary>
        public void ClearPool()
        {
            foreach (var row in _pooledRows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            _pooledRows.Clear();
            _poolingStatsCreated = 0;
            _poolingStatsReused = 0;

            if (logPoolingStats)
                Debug.Log("[LadderMiniWidget] Pool cleared");
        }

        /// <summary>
        /// Calculate percentage of allocations saved by pooling
        /// </summary>
        private float CalculateAllocationSavings()
        {
            int totalOperations = _poolingStatsReused + _poolingStatsCreated;
            if (totalOperations == 0) return 0f;
            return (_poolingStatsReused / (float)totalOperations) * 100f;
        }

        void OnDestroy()
        {
            // Clean up pool when widget is destroyed
            if (_pooledRows != null)
            {
                foreach (var row in _pooledRows)
                {
                    if (row != null && row.gameObject != null)
                        Destroy(row.gameObject);
                }
                _pooledRows.Clear();
            }
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
