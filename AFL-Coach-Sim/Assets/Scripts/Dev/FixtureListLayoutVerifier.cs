// File: Assets/Scripts/Dev/FixtureListLayoutVerifier.cs
using UnityEngine;
using UnityEngine.UI;

namespace AFLManager.Dev
{
    /// <summary>
    /// Attach to the ScrollView Content (your fixtureContainer).
    /// Ensures a VerticalLayoutGroup + ContentSizeFitter are present and configured.
    /// Will auto-fix at runtime (Awake) and also warn in-editor (OnValidate).
    /// </summary>
    [ExecuteAlways]
    public class FixtureListLayoutVerifier : MonoBehaviour
    {
        [Header("Layout Settings")]
        [Min(0)] public float spacing = 12f;
        public bool childControlWidth = true;
        public bool childControlHeight = true;
        public bool childForceExpandWidth = true;
        public bool childForceExpandHeight = false;

        [Header("Auto Fix")]
        public bool applyFixOnAwake = true;
        public bool applyFixOnValidate = true;

        void Awake()
        {
            if (applyFixOnAwake) EnsureLayout("Awake");
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (applyFixOnValidate) EnsureLayout("OnValidate");
        }
#endif

        private void EnsureLayout(string who)
        {
            var t = transform as RectTransform;
            if (!t) { Debug.LogError("[FixtureListLayoutVerifier] Content is not a RectTransform.", this); return; }

            // 1) Vertical Layout Group
            var vlg = gameObject.GetComponent<VerticalLayoutGroup>();
            if (!vlg)
            {
                vlg = gameObject.AddComponent<VerticalLayoutGroup>();
                Debug.LogWarning($"[FixtureListLayoutVerifier] Added VerticalLayoutGroup ({who}).", this);
            }
            vlg.spacing = spacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = childControlWidth;
            vlg.childControlHeight = childControlHeight;
            vlg.childForceExpandWidth = childForceExpandWidth;
            vlg.childForceExpandHeight = childForceExpandHeight;

            // 2) Content Size Fitter
            var csf = gameObject.GetComponent<ContentSizeFitter>();
            if (!csf)
            {
                csf = gameObject.AddComponent<ContentSizeFitter>();
                Debug.LogWarning($"[FixtureListLayoutVerifier] Added ContentSizeFitter ({who}).", this);
            }
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 3) Ensure ScrollRect points to this as Content (if there is a ScrollRect above)
            var sr = GetComponentInParent<ScrollRect>();
            if (sr && sr.content != t)
            {
                sr.content = t;
                Debug.LogWarning("[FixtureListLayoutVerifier] ScrollRect.content was not set to this Content. Fixed.", this);
            }

            // 4) Nice anchors so rows stretch horizontally
            t.anchorMin = new Vector2(0, 1);
            t.anchorMax = new Vector2(1, 1);
            t.pivot     = new Vector2(0.5f, 1f);
            t.offsetMin = new Vector2(0, t.offsetMin.y);
            t.offsetMax = new Vector2(0, t.offsetMax.y);
        }
    }
}
