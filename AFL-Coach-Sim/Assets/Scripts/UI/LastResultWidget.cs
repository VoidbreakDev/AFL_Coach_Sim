// Assets/Scripts/UI/LastResultWidget.cs
using TMPro;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.UI
{
    public class LastResultWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text homeLine;
        [SerializeField] TMP_Text awayLine;
        [SerializeField] TMP_Text dateText;

        public void Bind(MatchResult r)
        {
            if (!homeLine || !awayLine || !dateText) return;
            homeLine.text = $"{r.HomeTeamId} {r.HomeScore}";
            awayLine.text = $"{r.AwayTeamId} {r.AwayScore}";
            dateText.text = r.SimulatedAtUtc.ToLocalTime().ToString("d MMM yyyy");
        }

        public void BindNoResult()
        {
            if (homeLine) homeLine.text = "-";
            if (awayLine) awayLine.text = "-";
            if (dateText) dateText.text = "No results yet";
        }
    }
}