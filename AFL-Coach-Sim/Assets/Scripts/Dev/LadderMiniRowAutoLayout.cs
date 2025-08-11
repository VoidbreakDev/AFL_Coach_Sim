using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AFLManager.Dev
{
    [ExecuteAlways]
    public class LadderMiniRowAutoLayout : MonoBehaviour
    {
        [Header("Refs (optional)")]
        public TMP_Text rankText;
        public TMP_Text teamText;
        public TMP_Text gamesText;
        public TMP_Text pointsText;

        [Header("Sizing")]
        public float rowPreferredHeight = 30f;
        public float rankWidth = 34f;
        public float gamesWidth = 42f;
        public float pointsWidth = 54f;
        public float spacing = 8f;

        [Header("Padding")]
        public int padLeft = 8, padRight = 8, padTop = 4, padBottom = 4;

        void Awake()     { Apply(); }
#if UNITY_EDITOR
        void OnValidate(){ Apply(); }
#endif
        void Apply()
        {
            var rt = GetComponent<RectTransform>();       // ✅ correct way
            if (!rt) return;

            var le = GetOrAdd<LayoutElement>(gameObject);
            le.preferredHeight = rowPreferredHeight;
            le.flexibleHeight  = 0;

            var hlg = GetOrAdd<HorizontalLayoutGroup>(gameObject);
            hlg.padding = new RectOffset(padLeft, padRight, padTop, padBottom);
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth  = true;   // team column stretches
            hlg.childForceExpandHeight = false;

            // Rank
            if (rankText)
            {
                var rle = GetOrAdd<LayoutElement>(rankText.gameObject);
                rle.preferredWidth = rankWidth; rle.flexibleWidth = 0;
                rankText.enableAutoSizing = false;
                rankText.alignment = TextAlignmentOptions.MidlineRight;
            }

            // Team (flex)
            if (teamText)
            {
                var tle = GetOrAdd<LayoutElement>(teamText.gameObject);
                tle.preferredWidth = 0; tle.flexibleWidth = 1;
                teamText.enableAutoSizing = false;
                teamText.alignment = TextAlignmentOptions.MidlineLeft;
                teamText.overflowMode = TextOverflowModes.Ellipsis;
            }

            // Games
            if (gamesText)
            {
                var gle = GetOrAdd<LayoutElement>(gamesText.gameObject);
                gle.preferredWidth = gamesWidth; gle.flexibleWidth = 0;
                gamesText.enableAutoSizing = false;
                gamesText.alignment = TextAlignmentOptions.MidlineRight;
            }

            // Points
            if (pointsText)
            {
                var ple = GetOrAdd<LayoutElement>(pointsText.gameObject);
                ple.preferredWidth = pointsWidth; ple.flexibleWidth = 0;
                pointsText.enableAutoSizing = false;
                pointsText.alignment = TextAlignmentOptions.MidlineRight;
            }
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c ? c : go.AddComponent<T>();
        }
    }
}
