using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.DTO;

public class FixtureListView : MonoBehaviour
{
    [Header("Prefabs & Root")]
    public Transform contentRoot;     // VerticalLayoutGroup
    public GameObject rowPrefab;      // Prefab with: TextMeshProUGUI lineText
    public int showCount = 20;

    public void Render(List<(int round, TeamId home, TeamId away)> fixtures,
                       List<MatchResultDTO> results,
                       TeamDirectory dir)
    {
        foreach (Transform child in contentRoot) Destroy(child.gameObject);

        // Merge fixtures with results (by round + teams)
        var byKey = results.ToDictionary(
            r => (r.Round, r.Home.Value, r.Away.Value),
            r => r);

        int count = 0;
        foreach (var fx in fixtures.OrderBy(f => f.round))
        {
            if (count++ >= showCount) break;

            var go = Instantiate(rowPrefab, contentRoot);
            var text = go.GetComponentInChildren<TextMeshProUGUI>();

            var hn = dir.NameOf(fx.home);
            var an = dir.NameOf(fx.away);

            if (byKey.TryGetValue((fx.round, fx.home.Value, fx.away.Value), out var r))
                text.text = $"R{fx.round}: {hn} {r.HomeScore} – {r.AwayScore} {an}";
            else
                text.text = $"R{fx.round}: {hn} vs {an}";
        }
    }
}