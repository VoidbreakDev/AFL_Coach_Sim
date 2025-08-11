// File: Assets/Scripts/Dev/RowLayoutVerifier.cs
using UnityEngine;
using UnityEngine.UI;

namespace AFLManager.Dev
{
    /// <summary>
    /// Attach to the row prefab root (MatchEntryPanel).
    /// Ensures a LayoutElement exists with a reasonable preferred height,
    /// and the RectTransform is set to stretch horizontally.
    /// </summary>
    [ExecuteAlways]
    public class RowLayoutVerifier : MonoBehaviour
    {
        [Min(20)] public float preferredHeight = 110f;

        void Awake()  { EnsureRow("Awake"); }
#if UNITY_EDITOR
        void OnValidate() { EnsureRow("OnValidate"); }
#endif

        private void EnsureRow(string who)
        {
            var t = transform as RectTransform;
            if (!t) { Debug.LogError("[RowLayoutVerifier] Row is not a RectTransform.", this); return; }

            var le = GetComponent<LayoutElement>();
            if (!le)
            {
                le = gameObject.AddComponent<LayoutElement>();
                Debug.LogWarning($"[RowLayoutVerifier] Added LayoutElement ({who}).", this);
            }
            if (le.preferredHeight <= 0f) le.preferredHeight = preferredHeight;
            le.flexibleHeight = 0f;

            // Stretch horizontally under a VerticalLayoutGroup
            t.anchorMin = new Vector2(0, 1);
            t.anchorMax = new Vector2(1, 1);
            t.pivot     = new Vector2(0.5f, 1f);
            t.offsetMin = new Vector2(0, t.offsetMin.y);
            t.offsetMax = new Vector2(0, t.offsetMax.y);
        }
    }
}
