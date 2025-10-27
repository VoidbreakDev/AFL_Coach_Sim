// Assets/Scripts/Systems/SeasonProgressionController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLManager.Managers;

namespace AFLManager.Systems
{
    /// <summary>
    /// Controls season progression with clear next match button and round tracking
    /// </summary>
    public class SeasonProgressionController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button nextMatchButton;
        [SerializeField] private Button advanceRoundButton;
        [SerializeField] private TextMeshProUGUI roundTracker;
        [SerializeField] private TextMeshProUGUI nextMatchInfo;
        [SerializeField] private TextMeshProUGUI seasonProgress;
        
        [Header("Progress Display")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        
        private SeasonSchedule schedule;
        private string playerTeamId;
        private int currentRound = 1;
        private const int TOTAL_ROUNDS = 23;
        
        public System.Action<Match> OnPlayMatch;
        public System.Action OnRoundAdvanced;
        public System.Action OnSeasonComplete;
        
        void Awake()
        {
            if (nextMatchButton)
                nextMatchButton.onClick.AddListener(PlayNextMatch);
            
            if (advanceRoundButton)
                advanceRoundButton.onClick.AddListener(AdvanceRound);
        }
        
        public void Initialize(SeasonSchedule seasonSchedule, string teamId)
        {
            schedule = seasonSchedule;
            playerTeamId = teamId;
            
            RefreshUI();
        }
        
        private void RefreshUI()
        {
            if (schedule?.Fixtures == null)
                return;
            
            var results = SaveLoadManager.LoadAllResults();
            
            // Calculate current round
            currentRound = CalculateCurrentRound(results);
            
            // Update round tracker
            if (roundTracker)
                roundTracker.text = $"Round {currentRound}/{TOTAL_ROUNDS}";
            
            // Update progress bar
            if (progressBar)
            {
                float progress = (float)(currentRound - 1) / TOTAL_ROUNDS;
                progressBar.value = progress;
            }
            
            if (progressText)
            {
                int completed = currentRound - 1;
                progressText.text = $"{completed}/{TOTAL_ROUNDS} rounds complete";
            }
            
            // Find next player match
            var nextPlayerMatch = GetNextPlayerMatch(results);
            
            if (nextPlayerMatch != null)
            {
                UpdateNextMatchUI(nextPlayerMatch);
                EnableNextMatchButton(true);
            }
            else
            {
                // Check if round can be advanced
                if (CanAdvanceRound(results))
                {
                    UpdateRoundAdvanceUI();
                    EnableRoundAdvanceButton(true);
                }
                else if (currentRound > TOTAL_ROUNDS)
                {
                    UpdateSeasonCompleteUI();
                    DisableAllButtons();
                }
                else
                {
                    UpdateWaitingUI();
                    DisableAllButtons();
                }
            }
        }
        
        private void UpdateNextMatchUI(Match match)
        {
            if (nextMatchInfo)
            {
                string opponent = match.HomeTeamId == playerTeamId ? match.AwayTeamId : match.HomeTeamId;
                string venue = match.HomeTeamId == playerTeamId ? "HOME" : "AWAY";
                nextMatchInfo.text = $"vs {opponent} ({venue})";
            }
            
            if (seasonProgress)
                seasonProgress.text = "Ready to play next match";
        }
        
        private void UpdateRoundAdvanceUI()
        {
            if (nextMatchInfo)
                nextMatchInfo.text = "All matches complete";
            
            if (seasonProgress)
                seasonProgress.text = $"Advance to Round {currentRound + 1}";
        }
        
        private void UpdateWaitingUI()
        {
            if (nextMatchInfo)
                nextMatchInfo.text = "Waiting for other matches...";
            
            if (seasonProgress)
                seasonProgress.text = "Complete current round first";
        }
        
        private void UpdateSeasonCompleteUI()
        {
            if (nextMatchInfo)
                nextMatchInfo.text = "Season Complete!";
            
            if (seasonProgress)
                seasonProgress.text = "Check final ladder standings";
        }
        
        private void EnableNextMatchButton(bool enabled)
        {
            if (nextMatchButton)
            {
                nextMatchButton.gameObject.SetActive(enabled);
                nextMatchButton.interactable = enabled;
            }
            
            if (advanceRoundButton)
                advanceRoundButton.gameObject.SetActive(false);
        }
        
        private void EnableRoundAdvanceButton(bool enabled)
        {
            if (advanceRoundButton)
            {
                advanceRoundButton.gameObject.SetActive(enabled);
                advanceRoundButton.interactable = enabled;
            }
            
            if (nextMatchButton)
                nextMatchButton.gameObject.SetActive(false);
        }
        
        private void DisableAllButtons()
        {
            if (nextMatchButton)
            {
                nextMatchButton.gameObject.SetActive(false);
                nextMatchButton.interactable = false;
            }
            
            if (advanceRoundButton)
            {
                advanceRoundButton.gameObject.SetActive(false);
                advanceRoundButton.interactable = false;
            }
        }
        
        private void PlayNextMatch()
        {
            var results = SaveLoadManager.LoadAllResults();
            var nextMatch = GetNextPlayerMatch(results);
            
            if (nextMatch != null)
            {
                OnPlayMatch?.Invoke(nextMatch);
            }
        }
        
        private void AdvanceRound()
        {
            var results = SaveLoadManager.LoadAllResults();
            
            // Simulate remaining matches in current round
            var currentRoundMatches = GetCurrentRoundMatches(currentRound);
            
            foreach (var match in currentRoundMatches)
            {
                if (!HasResult(match, results))
                {
                    SimulateAIMatch(match);
                }
            }
            
            currentRound++;
            OnRoundAdvanced?.Invoke();
            
            if (currentRound > TOTAL_ROUNDS)
            {
                OnSeasonComplete?.Invoke();
            }
            
            RefreshUI();
        }
        
        private Match GetNextPlayerMatch(List<MatchResult> results)
        {
            return schedule.Fixtures
                .Where(m => m.HomeTeamId == playerTeamId || m.AwayTeamId == playerTeamId)
                .FirstOrDefault(m => !HasResult(m, results));
        }
        
        private bool CanAdvanceRound(List<MatchResult> results)
        {
            // Check if all matches in current round are complete (except player matches)
            var roundMatches = GetCurrentRoundMatches(currentRound);
            var playerMatches = roundMatches.Where(m => m.HomeTeamId == playerTeamId || m.AwayTeamId == playerTeamId);
            var aiMatches = roundMatches.Except(playerMatches);
            
            // Player must have played all their matches
            bool playerMatchesComplete = playerMatches.All(m => HasResult(m, results));
            
            return playerMatchesComplete && currentRound <= TOTAL_ROUNDS;
        }
        
        private List<Match> GetCurrentRoundMatches(int round)
        {
            // Simple approximation - each round has ~9 matches (18 teams = 9 matches per round)
            int matchesPerRound = schedule.Fixtures.Count / TOTAL_ROUNDS;
            int startIndex = (round - 1) * matchesPerRound;
            int endIndex = Mathf.Min(startIndex + matchesPerRound, schedule.Fixtures.Count);
            
            var roundMatches = new List<Match>();
            for (int i = startIndex; i < endIndex; i++)
            {
                if (i >= 0 && i < schedule.Fixtures.Count)
                    roundMatches.Add(schedule.Fixtures[i]);
            }
            
            return roundMatches;
        }
        
        private bool HasResult(Match match, List<MatchResult> results)
        {
            string matchId = GetMatchId(match);
            return results.Any(r => r.MatchId == matchId);
        }
        
        private string GetMatchId(Match match)
        {
            int index = schedule.Fixtures.IndexOf(match);
            return $"{index}_{match.HomeTeamId}_{match.AwayTeamId}";
        }
        
        private void SimulateAIMatch(Match match)
        {
            string matchId = GetMatchId(match);
            
            var result = AFLManager.Simulation.MatchSimulator.SimulateMatch(
                matchId, "R?", match.HomeTeamId, match.AwayTeamId,
                new AFLManager.Simulation.MatchSimulator.DefaultRatingProvider(
                    id => GetTeamAverage(id),
                    id => new[] { $"{id}_P1", $"{id}_P2", $"{id}_P3", $"{id}_P4", $"{id}_P5", $"{id}_P6" }),
                seed: matchId.GetHashCode());
            
            match.Result = $"{result.HomeScore}–{result.AwayScore}";
            SaveLoadManager.SaveMatchResult(result);
        }
        
        private float GetTeamAverage(string teamId)
        {
            var team = LoadTeam(teamId);
            if (team?.Roster == null || team.Roster.Count == 0)
                return 65f;
            
            float sum = 0f;
            foreach (var p in team.Roster)
                sum += p?.Stats?.GetAverage() ?? 65f;
            
            return sum / team.Roster.Count;
        }
        
        private Team LoadTeam(string teamId)
        {
            return SaveLoadManager.LoadTeam(teamId);
        }
        
        private int CalculateCurrentRound(List<MatchResult> results)
        {
            // Count completed matches and estimate round
            int completedMatches = results.Count;
            int matchesPerRound = schedule.Fixtures.Count / TOTAL_ROUNDS;
            
            if (matchesPerRound == 0)
                return 1;
            
            int estimatedRound = (completedMatches / matchesPerRound) + 1;
            return Mathf.Clamp(estimatedRound, 1, TOTAL_ROUNDS + 1);
        }
        
        public void OnReturnFromMatch()
        {
            // Called when returning from match flow
            RefreshUI();
        }
    }
}