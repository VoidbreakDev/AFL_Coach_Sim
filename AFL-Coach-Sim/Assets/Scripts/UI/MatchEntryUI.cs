// File: Assets/Scripts/UI/MatchEntryUI.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;

namespace AFLManager.UI
{
    /// <summary>
    /// Fixture row UI.
    /// Supports the legacy Initialize(match, onPlay, teamNames) API used by SeasonScreenManager.
    /// Also exposes SetFinalScore(...) for future flows.
    /// </summary>
    public class MatchEntryUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text vsText;
        [SerializeField] private TMP_Text scoreText;

        // Backing state
        private Match _match;
        private Action<Match> _onPlay;
        private Dictionary<string, string> _teamNames;

        /// <summary>
        /// Legacy API expected by your SeasonScreenManager:
        /// Initializes the row, wires the Play button, and renders names + score.
        /// </summary>
        public void Initialize(Match match, Action<Match> onPlay, Dictionary<string, string> teamNames)
        {
            _match = match;
            _onPlay = onPlay;
            _teamNames = teamNames;

            if (vsText)
            {
                string home = NameOrId(match?.HomeTeamId);
                string away = NameOrId(match?.AwayTeamId);
                vsText.text = $"{home} vs {away}";
            }

            RefreshScoreUI();

            if (playButton)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(HandlePlayClicked);
                // Disable if already has a result
                playButton.interactable = string.IsNullOrEmpty(_match?.Result);
            }

            #if UNITY_EDITOR
                Debug.Log($"[MatchEntryUI] Bound. vsText={(vsText?vsText.name:"<null>")} scoreText={(scoreText?scoreText.name:"<null>")} play={(playButton?playButton.name:"<null>")}", this);
            #endif
            
            #if UNITY_EDITOR
                Debug.Log($"[MatchEntryUI] Bound OK | vsText={(vsText?vsText.name:"<null>")} scoreText={(scoreText?scoreText.name:"<null>")} play={(playButton?playButton.name:"<null>")}", this);
            #endif


        }

        /// <summary>
        /// Optional future API: set a final score and disable play.
        /// </summary>
        public void SetFinalScore(int home, int away)
        {
            if (_match != null)
                _match.Result = $"{home}–{away}";

            if (scoreText)
                scoreText.text = $"{home}–{away}";

            if (playButton)
                playButton.interactable = false;
        }

        private void HandlePlayClicked()
        {
            if (_match == null)
            {
                Debug.LogError("[MatchEntryUI] Play clicked with no match bound.");
                return;
            }
            _onPlay?.Invoke(_match); // SeasonScreenManager will set match.Result
            RefreshScoreUI();

            // Lock row after sim
            if (!string.IsNullOrEmpty(_match.Result) && playButton)
                playButton.interactable = false;
        }

        private void RefreshScoreUI()
        {
            if (scoreText == null) return;
            scoreText.text = string.IsNullOrEmpty(_match?.Result) ? "—" : _match.Result;
        }

        private string NameOrId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Unknown";
            if (_teamNames != null && _teamNames.TryGetValue(id, out var n))
                return n;
            return id;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Tiny guardrails in the editor
            if (playButton == null) Debug.LogWarning("[MatchEntryUI] playButton not assigned.", this);
            if (vsText == null) Debug.LogWarning("[MatchEntryUI] vsText not assigned.", this);
            if (scoreText == null) Debug.LogWarning("[MatchEntryUI] scoreText not assigned.", this);
        }
#endif
    }
}
