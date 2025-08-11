// File: Assets/Scripts/Dev/MatchEntryRowAutoLayout.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AFLManager.Dev
{
    [ExecuteAlways]
    public class MatchEntryRowAutoLayout : MonoBehaviour
    {
        [Header("Column Refs")]
        public TMP_Text datesText;   // left
        public TMP_Text vsText;      // middle (flex)
        public TMP_Text scoreText;   // right of middle
        public Button playButton;    // far right (must be Button)

        [Header("Sizing")]
        public float datesPreferred = 100f;
        public float scorePreferred = 80f;
        public float buttonPreferred = 130f;
        public float rowPreferredHeight = 110f;

        [Header("Padding & Spacing")]
        public int padLeft = 24, padRight = 24, padTop = 8, padBottom = 8;
        public float spacing = 12f;

        void Awake()  { Apply(); }
#if UNITY_EDITOR
        void OnValidate() { Apply(); }
#endif
        void Apply()
        {
            var rt = transform as RectTransform;
            if (!rt) return;

            // Row height
            var rowLE = GetOrAdd<LayoutElement>(gameObject);
            rowLE.preferredHeight = rowPreferredHeight;
            rowLE.flexibleHeight = 0f;

            // Horizontal layout with padding
            var hlg = GetOrAdd<HorizontalLayoutGroup>(gameObject);
            hlg.padding = new RectOffset(padLeft, padRight, padTop, padBottom);
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            // Dates
            if (datesText)
            {
                var le = GetOrAdd<LayoutElement>(datesText.gameObject);
                le.preferredWidth = datesPreferred; le.flexibleWidth = 0f;
                datesText.enableAutoSizing = false;
                datesText.alignment = TextAlignmentOptions.MidlineLeft;
            }

            // VsText (flex)
            if (vsText)
            {
                var le = GetOrAdd<LayoutElement>(vsText.gameObject);
                le.preferredWidth = 0f; le.flexibleWidth = 1f; // flex column
                vsText.enableAutoSizing = false;
                vsText.alignment = TextAlignmentOptions.MidlineLeft;
                vsText.overflowMode = TextOverflowModes.Ellipsis;
            }

            // Score
            if (scoreText)
            {
                var le = GetOrAdd<LayoutElement>(scoreText.gameObject);
                le.preferredWidth = scorePreferred; le.flexibleWidth = 0f;
                scoreText.enableAutoSizing = false;
                scoreText.alignment = TextAlignmentOptions.MidlineRight;
            }

            // Play button (ensure it's a Button, not a Slider)
            if (playButton)
            {
                if (playButton.GetComponent<Slider>())
                {
                    Debug.LogWarning("[MatchEntryRowAutoLayout] 'Play' has a Slider component. Replace with a Button.", playButton);
                }
                var le = GetOrAdd<LayoutElement>(playButton.gameObject);
                le.preferredWidth = buttonPreferred; le.flexibleWidth = 0f;
                le.preferredHeight = 40f; le.flexibleHeight = 0f;

                // Nice background setup if present
                var img = playButton.GetComponent<Image>();
                if (img) img.raycastTarget = true;
            }
        }

        T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c ? c : go.AddComponent<T>();
        }
    }
}
