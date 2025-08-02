// File: Assets/Scripts/UI/MatchEntryUI.cs
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AFLManager.Models;

namespace AFLManager.UI
{
    public class MatchEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text teamsText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button playButton;

        private Match matchData;
        private Action<Match> onPlay;

        public void Initialize(Match match, Action<Match> onPlayCallback)
        {
            matchData = match;
            onPlay    = onPlayCallback;

            dateText.text   = match.FixtureDate.ToString("yyyy-MM-dd");
            teamsText.text  = $"{match.HomeTeamId} vs {match.AwayTeamId}";
            resultText.text = string.IsNullOrEmpty(match.Result) ? "Unplayed" : match.Result;

            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            onPlay?.Invoke(matchData);
            resultText.text       = matchData.Result;
            playButton.interactable = false;
        }
    }
}
