// File: Assets/Scripts/UI/LadderMiniRow.cs
using UnityEngine;
using TMPro;

namespace AFLManager.UI
{
    public class LadderMiniRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text teamText;
        [SerializeField] private TMP_Text gamesText;
        [SerializeField] private TMP_Text pointsText;

        void Awake()  { EnsureRefs(); }
#if UNITY_EDITOR
        void OnValidate() { EnsureRefs(); }
#endif

        void EnsureRefs()
        {
            // If any field is missing, try to find by common child names
            if (!rankText  || !teamText || !gamesText || !pointsText)
            {
                foreach (var t in GetComponentsInChildren<TMP_Text>(true))
                {
                    var n = t.gameObject.name.ToLowerInvariant();
                    if (!rankText   && (n.Contains("rank")))   rankText = t;
                    else if (!teamText   && (n.Contains("team")))   teamText = t;
                    else if (!gamesText  && (n.Contains("game")))   gamesText = t;
                    else if (!pointsText && (n.Contains("point") || n == "pts")) pointsText = t;
                }
            }
        }

        public void Bind(int rank, string team, int games, int points)
        {
            // last‑chance ensure
            EnsureRefs();

            if (rankText)   rankText.text   = rank.ToString();
            if (teamText)   teamText.text   = string.IsNullOrEmpty(team) ? "-" : team;
            if (gamesText)  gamesText.text  = games.ToString();
            if (pointsText) pointsText.text = points.ToString();
        }
    }
}
