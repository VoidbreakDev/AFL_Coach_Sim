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
        private System.Collections.Generic.Dictionary<string, string> teamNames;

        public void Initialize(Match match, Action<Match> onPlayCallback, System.Collections.Generic.Dictionary<string, string> teamNameLookup = null)
        {
            matchData = match;
            onPlay    = onPlayCallback;
            teamNames = teamNameLookup;

            dateText.text   = match.FixtureDate.ToString("MMM dd, yyyy");
            
            // Use team names if available, otherwise fall back to IDs
            string homeTeamName = GetTeamName(match.HomeTeamId);
            string awayTeamName = GetTeamName(match.AwayTeamId);
            teamsText.text  = $"{homeTeamName} vs {awayTeamName}";
            
            resultText.text = string.IsNullOrEmpty(match.Result) ? "Unplayed" : match.Result;

            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
        
        private string GetTeamName(string teamId)
        {
            if (teamNames != null && teamNames.ContainsKey(teamId))
            {
                return teamNames[teamId];
            }
            return teamId; // Fallback to ID if name not found
        }

        private void OnPlayClicked()
        {
            onPlay?.Invoke(matchData);
            resultText.text       = matchData.Result;
            playButton.interactable = false;
        }
    }
}
