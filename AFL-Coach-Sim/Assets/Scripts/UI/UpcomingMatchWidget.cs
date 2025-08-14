// Assets/Scripts/UI/UpcomingMatchWidget.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AFLManager.Models;

namespace AFLManager.UI
{
    public class UpcomingMatchWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text homeName;
        [SerializeField] TMP_Text awayName;
        [SerializeField] TMP_Text dateText;
        [SerializeField] Button playButton;

        public Action OnPlay;

        void Awake()
        {
            if (playButton) playButton.onClick.AddListener(() => OnPlay?.Invoke());
        }

        public void Bind(Match m, DateTime date)
        {
            if (!homeName || !awayName || !dateText) return;
            homeName.text = m.HomeTeamId;
            awayName.text = m.AwayTeamId;
            dateText.text = date.ToString("d MMM yyyy");
            if (playButton) playButton.interactable = true;
        }

        public void BindNoMatch()
        {
            if (homeName) homeName.text = "-";
            if (awayName) awayName.text = "-";
            if (dateText) dateText.text = "No upcoming match";
            if (playButton) playButton.interactable = false;
        }
    }
}
