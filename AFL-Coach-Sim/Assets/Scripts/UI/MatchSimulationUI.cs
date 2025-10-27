// Assets/Scripts/UI/MatchSimulationUI.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;

namespace AFLManager.Managers
{
    /// <summary>
    /// Live match simulation screen with score updates
    /// </summary>
    public class MatchSimulationUI : MonoBehaviour
    {
        [Header("Team Display")]
        [SerializeField] private TextMeshProUGUI homeTeamName;
        [SerializeField] private TextMeshProUGUI awayTeamName;
        [SerializeField] private TextMeshProUGUI homeScore;
        [SerializeField] private TextMeshProUGUI awayScore;
        
        [Header("Progress")]
        [SerializeField] private TextMeshProUGUI quarterText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Commentary")]
        [SerializeField] private TextMeshProUGUI commentaryText;
        [SerializeField] private Transform commentaryFeedContainer;
        [SerializeField] private GameObject commentaryEntryPrefab;
        
        [Header("Animation")]
        [SerializeField] private float simulationSpeed = 2f;
        
        private Match match;
        private string playerTeamId;
        
        public void Initialize(Match matchData, string userTeamId)
        {
            match = matchData;
            playerTeamId = userTeamId;
            
            if (homeTeamName) homeTeamName.text = matchData.HomeTeamId;
            if (awayTeamName) awayTeamName.text = matchData.AwayTeamId;
            if (homeScore) homeScore.text = "0";
            if (awayScore) awayScore.text = "0";
            if (quarterText) quarterText.text = "Q1";
            if (progressBar) progressBar.value = 0;
            if (progressText) progressText.text = "Starting match...";
        }
        
        public IEnumerator ShowSimulationProgress(MatchResult result)
        {
            // Simulate quarters with progress
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                if (quarterText) quarterText.text = $"Q{quarter}";
                
                // Calculate quarter scores (approximation)
                int homeQ = result.HomeScore / 4;
                int awayQ = result.AwayScore / 4;
                
                int homeStart = homeQ * (quarter - 1);
                int awayStart = awayQ * (quarter - 1);
                int homeEnd = quarter == 4 ? result.HomeScore : homeQ * quarter;
                int awayEnd = quarter == 4 ? result.AwayScore : awayQ * quarter;
                
                // Animate score progression
                float duration = 3f / simulationSpeed;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    
                    if (progressBar) progressBar.value = t;
                    
                    int currentHome = Mathf.RoundToInt(Mathf.Lerp(homeStart, homeEnd, t));
                    int currentAway = Mathf.RoundToInt(Mathf.Lerp(awayStart, awayEnd, t));
                    
                    if (homeScore) homeScore.text = currentHome.ToString();
                    if (awayScore) awayScore.text = currentAway.ToString();
                    
                    if (progressText)
                    {
                        int timeRemaining = Mathf.RoundToInt((1f - t) * 20f);
                        progressText.text = $"{timeRemaining}:00 remaining";
                    }
                    
                    // Random commentary events
                    if (Random.value < 0.1f * Time.deltaTime)
                    {
                        AddCommentaryEvent(GetRandomCommentary(quarter, currentHome, currentAway));
                    }
                    
                    yield return null;
                }
                
                // Quarter break
                if (quarter < 4)
                {
                    if (progressText) progressText.text = "Quarter break...";
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            // Final scores
            if (homeScore) homeScore.text = result.HomeScore.ToString();
            if (awayScore) awayScore.text = result.AwayScore.ToString();
            if (progressText) progressText.text = "Match complete!";
            if (progressBar) progressBar.value = 1f;
        }
        
        private void AddCommentaryEvent(string commentary)
        {
            if (commentaryText)
            {
                commentaryText.text = commentary;
            }
            
            if (commentaryFeedContainer != null && commentaryEntryPrefab != null)
            {
                var entry = Instantiate(commentaryEntryPrefab, commentaryFeedContainer);
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text) text.text = commentary;
                
                // Limit feed to last 10 entries
                if (commentaryFeedContainer.childCount > 10)
                {
                    Destroy(commentaryFeedContainer.GetChild(0).gameObject);
                }
            }
        }
        
        private string GetRandomCommentary(int quarter, int homeScore, int awayScore)
        {
            string[] events = new[]
            {
                "Great mark taken!",
                "Cleared from the center bounce",
                "Forward entry, looking dangerous",
                "Strong tackle prevents the score",
                "Long kick down the line",
                "Goal! What a finish!",
                "Behind, keeps the score ticking",
                "Turnover in the midfield",
                "Free kick awarded",
                "Ball spills loose in the contest"
            };
            
            return events[Random.Range(0, events.Length)];
        }
    }
}
