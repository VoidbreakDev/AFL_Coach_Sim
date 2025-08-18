using System.Collections.Generic;
using UnityEngine;
using TMPro;
using AFLCoachSim.Core.DTO;

public class LadderTableView : MonoBehaviour
{
    [Header("Prefabs & Root")]
    public Transform contentRoot;      // Empty GameObject with VerticalLayoutGroup
    public GameObject rowPrefab;       // Prefab with: TextMeshProUGUI nameText, detailText

    public void Render(List<LadderEntryDTO> ladder, TeamDirectory dir)
    {
        foreach (Transform child in contentRoot) Destroy(child.gameObject);

        for (int i = 0; i < ladder.Count; i++)
        {
            var e = ladder[i];
            var go = Instantiate(rowPrefab, contentRoot);
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>();
            var name = dir.NameOf(e.Team);
            texts[0].text = $"{i + 1}. {name}";
            texts[1].text = $"Pts:{e.PremiershipPoints}  W:{e.Wins} L:{e.Losses} D:{e.Draws}  PF:{e.PointsFor}  PA:{e.PointsAgainst}  %:{e.Percentage / 100f:0.00}";
        }
    }
}