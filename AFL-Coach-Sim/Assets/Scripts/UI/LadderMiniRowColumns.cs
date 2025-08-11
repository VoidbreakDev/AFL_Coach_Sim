// File: Assets/Scripts/UI/LadderMiniRowColumns.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class LadderMiniRowColumns : MonoBehaviour
{
    [SerializeField] TMP_Text rankText, teamText, gamesText, pointsText;

    void Reset() { TryWire(); }
    void OnValidate() { TryWire(); Apply(); }
    void Awake() { Apply(); }

    void TryWire()
    {
        if (!rankText || !teamText || !gamesText || !pointsText)
        {
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (!rankText  && n.Contains("rank"))  rankText  = t;
                else if (!teamText  && n.Contains("team"))  teamText  = t;
                else if (!gamesText && n.Contains("game"))  gamesText = t;
                else if (!pointsText&& (n.Contains("point") || n=="pts")) pointsText = t;
            }
        }
    }

    void Apply()
    {
        // Ensure a HorizontalLayoutGroup exists on the row root
        var h = GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;  h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        h.spacing = 8;

        // Give each column a LayoutElement with sane widths
        SetLE(rankText,   min:28, pref:36, flex:0);    // rank
        SetLE(teamText,   min:80, pref:0,  flex:1);    // team name takes remaining space
        SetLE(gamesText,  min:28, pref:36, flex:0);    // games
        SetLE(pointsText, min:36, pref:44, flex:0);    // points
    }

    static void SetLE(TMP_Text t, float min, float pref, float flex)
    {
        if (!t) return;
        var le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
        le.minWidth = min;
        le.preferredWidth = pref;        // 0 = use text preferred
        le.flexibleWidth = flex;
        // Make sure the text is definitely visible and dark enough
        var c = t.color; t.color = new Color(0.15f,0.15f,0.15f,1f);
        t.enableAutoSizing = false;
        if (t.fontSize < 16) t.fontSize = 16;
        t.gameObject.SetActive(true);
    }
}
