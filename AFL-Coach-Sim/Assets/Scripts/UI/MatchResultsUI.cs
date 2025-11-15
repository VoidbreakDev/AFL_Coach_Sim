// Assets/Scripts/UI/MatchResultsUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using System.Linq;

namespace AFLManager.Managers
{
    /// <summary>
    /// Post-match results screen with stats and highlights
    /// </summary>
    public class MatchResultsUI : MonoBehaviour
    {
        [Header("Match Result")]
        [SerializeField] private TextMeshProUGUI resultHeaderText;
        [SerializeField] private TextMeshProUGUI homeTeamName;
        [SerializeField] private TextMeshProUGUI awayTeamName;
        [SerializeField] private TextMeshProUGUI homeScore;
        [SerializeField] private TextMeshProUGUI awayScore;
        [SerializeField] private TextMeshProUGUI marginText;
        
        [Header("Quarter Scores")]
        [SerializeField] private TextMeshProUGUI homeQ1;
        [SerializeField] private TextMeshProUGUI homeQ2;
        [SerializeField] private TextMeshProUGUI homeQ3;
        [SerializeField] private TextMeshProUGUI homeQ4;
        [SerializeField] private TextMeshProUGUI awayQ1;
        [SerializeField] private TextMeshProUGUI awayQ2;
        [SerializeField] private TextMeshProUGUI awayQ3;
        [SerializeField] private TextMeshProUGUI awayQ4;
        
        [Header("Commentary Highlights")]
        [SerializeField] private Transform highlightsContainer;
        [SerializeField] private GameObject highlightEntryPrefab;
        
        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI homeDisposals;
        [SerializeField] private TextMeshProUGUI awayDisposals;
        [SerializeField] private TextMeshProUGUI homeMarks;
        [SerializeField] private TextMeshProUGUI awayMarks;
        [SerializeField] private TextMeshProUGUI homeTackles;
        [SerializeField] private TextMeshProUGUI awayTackles;
        
        [Header("Controls")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button viewStatsButton;  // Optional - detailed stats not implemented yet
        
        private MatchResult result;
        private string playerTeamId;
        private System.Collections.Generic.List<string> commentary;
        
        public System.Action OnContinue;
        public System.Action OnViewStats;
        
        void Awake()
        {
            if (continueButton)
                continueButton.onClick.AddListener(() => OnContinue?.Invoke());
            
            if (viewStatsButton)
                viewStatsButton.onClick.AddListener(() => OnViewStats?.Invoke());
        }
        
        public void Initialize(MatchResult matchResult, Match match, Team homeTeam, Team awayTeam, string userTeamId, System.Collections.Generic.List<string> capturedCommentary = null)
        {
            result = matchResult;
            playerTeamId = userTeamId;
            commentary = capturedCommentary;
            
            bool playerWon = DeterminePlayerResult(matchResult, userTeamId);
            
            // Result header
            if (resultHeaderText)
            {
                if (playerWon)
                    resultHeaderText.text = "<color=green>VICTORY!</color>";
                else
                    resultHeaderText.text = "<color=red>DEFEAT</color>";
            }
            
            // Team names
            if (homeTeamName) homeTeamName.text = homeTeam?.Name ?? matchResult.HomeTeamId;
            if (awayTeamName) awayTeamName.text = awayTeam?.Name ?? matchResult.AwayTeamId;
            
            // Final scores
            if (homeScore) homeScore.text = matchResult.HomeScore.ToString();
            if (awayScore) awayScore.text = matchResult.AwayScore.ToString();
            
            // Margin
            if (marginText)
            {
                int margin = Mathf.Abs(matchResult.HomeScore - matchResult.AwayScore);
                string winner = matchResult.HomeScore > matchResult.AwayScore 
                    ? (homeTeam?.Name ?? matchResult.HomeTeamId)
                    : (awayTeam?.Name ?? matchResult.AwayTeamId);
                marginText.text = $"{winner} by {margin} points";
            }
            
            // Quarter scores (approximation since we don't store them)
            DisplayQuarterScores(matchResult);
            
            // Statistics (placeholder values)
            DisplayStatistics(matchResult);
            
            // Commentary highlights
            DisplayHighlights(matchResult);
        }
        
        private void DisplayQuarterScores(MatchResult result)
        {
            // Approximate quarter-by-quarter breakdown
            int homeQ = result.HomeScore / 4;
            int awayQ = result.AwayScore / 4;
            
            if (homeQ1) homeQ1.text = homeQ.ToString();
            if (homeQ2) homeQ2.text = homeQ.ToString();
            if (homeQ3) homeQ3.text = homeQ.ToString();
            if (homeQ4) homeQ4.text = (result.HomeScore - homeQ * 3).ToString();
            
            if (awayQ1) awayQ1.text = awayQ.ToString();
            if (awayQ2) awayQ2.text = awayQ.ToString();
            if (awayQ3) awayQ3.text = awayQ.ToString();
            if (awayQ4) awayQ4.text = (result.AwayScore - awayQ * 3).ToString();
        }
        
        private void DisplayStatistics(MatchResult result)
        {
            // Generate approximate stats based on score
            int homeStats = result.HomeScore * 3;
            int awayStats = result.AwayScore * 3;
            
            if (homeDisposals) homeDisposals.text = homeStats.ToString();
            if (awayDisposals) awayDisposals.text = awayStats.ToString();
            
            if (homeMarks) homeMarks.text = (homeStats / 2).ToString();
            if (awayMarks) awayMarks.text = (awayStats / 2).ToString();
            
            if (homeTackles) homeTackles.text = (homeStats / 3).ToString();
            if (awayTackles) awayTackles.text = (awayStats / 3).ToString();
        }
        
        private void DisplayHighlights(MatchResult result)
        {
            if (highlightsContainer == null || highlightEntryPrefab == null)
                return;
            
            // Clear existing
            foreach (Transform child in highlightsContainer)
                Destroy(child.gameObject);
            
            // Use real commentary if available
            if (commentary != null && commentary.Count > 0)
            {
                // Take key highlights from commentary (goals, injuries, quarter starts)
                var highlights = commentary
                    .Where(c => c.Contains("Goal") || c.Contains("Q") || c.Contains("start") || c.Contains("Behind"))
                    .Take(10)  // Limit to 10 highlights
                    .ToList();
                
                foreach (var highlight in highlights)
                {
                    var entry = Instantiate(highlightEntryPrefab, highlightsContainer);
                    var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                    if (text) text.text = highlight;
                }
            }
            else
            {
                // Fallback to generic highlights
                string[] highlights = new[]
                {
                    $"Q1 - Strong start from {result.HomeTeamId}",
                    $"Q2 - Momentum swings as {result.AwayTeamId} fights back",
                    $"Q3 - Physical contest intensifies",
                    $"Q4 - {(result.HomeScore > result.AwayScore ? result.HomeTeamId : result.AwayTeamId)} seals the victory"
                };
                
                foreach (var highlight in highlights)
                {
                    var entry = Instantiate(highlightEntryPrefab, highlightsContainer);
                    var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                    if (text) text.text = highlight;
                }
            }
        }
        
        private bool DeterminePlayerResult(MatchResult result, string teamId)
        {
            if (result.HomeTeamId == teamId)
                return result.HomeScore > result.AwayScore;
            else if (result.AwayTeamId == teamId)
                return result.AwayScore > result.HomeScore;
            
            return false;
        }
    }
}
