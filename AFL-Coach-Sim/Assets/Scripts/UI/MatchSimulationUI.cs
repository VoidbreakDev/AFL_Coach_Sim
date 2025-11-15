// Assets/Scripts/UI/MatchSimulationUI.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Commentary;

namespace AFLManager.Managers
{
    /// <summary>
    /// Live match simulation screen with real-time telemetry and commentary
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
        [SerializeField] private int maxCommentaryEntries = 10;
        
        [Header("Animation")]
        [SerializeField] private float simulationSpeed = 2f;
        [SerializeField] private float updateInterval = 0.1f;
        
        private Match match;
        private string playerTeamId;
        private List<MatchSnapshot> snapshotHistory = new List<MatchSnapshot>();
        private List<string> commentaryHistory = new List<string>();
        private int totalQuarterTicks = 0;
        
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
        
        /// <summary>
        /// Show simulation with live telemetry updates
        /// </summary>
        public IEnumerator ShowSimulationProgress(List<MatchSnapshot> snapshots, List<string> commentary)
        {
            snapshotHistory = snapshots ?? new List<MatchSnapshot>();
            commentaryHistory = commentary ?? new List<string>();
            
            if (snapshotHistory.Count == 0)
            {
                Debug.LogWarning("[MatchSimulationUI] No snapshots provided");
                yield break;
            }
            
            // Calculate total ticks for progress tracking
            totalQuarterTicks = snapshotHistory.Count / 4;
            int currentCommentaryIndex = 0;
            
            // Play through snapshots with animation
            for (int i = 0; i < snapshotHistory.Count; i++)
            {
                var snapshot = snapshotHistory[i];
                
                // Update UI from snapshot
                UpdateUIFromSnapshot(snapshot);
                
                // Show commentary for this moment (with throttling)
                if (currentCommentaryIndex < commentaryHistory.Count)
                {
                    // Show commentary at key moments (goals, quarters, etc)
                    if (ShouldShowCommentary(i, snapshot))
                    {
                        AddCommentaryEvent(commentaryHistory[currentCommentaryIndex]);
                        currentCommentaryIndex++;
                    }
                }
                
                // Control speed
                yield return new WaitForSeconds(updateInterval / simulationSpeed);
            }
            
            // Show final state
            if (snapshotHistory.Count > 0)
            {
                var final = snapshotHistory[snapshotHistory.Count - 1];
                UpdateUIFromSnapshot(final);
            }
            
            if (progressText) progressText.text = "Match complete!";
            if (progressBar) progressBar.value = 1f;
        }
        
        /// <summary>
        /// Fallback method for old MatchResult format (backward compatibility)
        /// </summary>
        public IEnumerator ShowSimulationProgress(MatchResult result)
        {
            // Simulate quarters with progress (old method)
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
        
        private void UpdateUIFromSnapshot(MatchSnapshot snapshot)
        {
            // Quarter display
            if (quarterText) quarterText.text = $"Q{snapshot.Quarter}";
            
            // Score display
            if (homeScore) homeScore.text = snapshot.HomePoints.ToString();
            if (awayScore) awayScore.text = snapshot.AwayPoints.ToString();
            
            // Progress bar (based on time in match)
            float matchProgress = CalculateMatchProgress(snapshot);
            if (progressBar) progressBar.value = matchProgress;
            
            // Time remaining display
            if (progressText)
            {
                int minutes = snapshot.TimeRemaining / 60;
                int seconds = snapshot.TimeRemaining % 60;
                progressText.text = $"{minutes}:{seconds:D2} remaining";
            }
        }
        
        private float CalculateMatchProgress(MatchSnapshot snapshot)
        {
            // Calculate overall match progress (0 to 1)
            const int QUARTER_LENGTH = 1200; // 20 minutes in seconds
            int totalTime = 4 * QUARTER_LENGTH;
            int elapsed = (snapshot.Quarter - 1) * QUARTER_LENGTH + (QUARTER_LENGTH - snapshot.TimeRemaining);
            return Mathf.Clamp01((float)elapsed / totalTime);
        }
        
        private bool ShouldShowCommentary(int snapshotIndex, MatchSnapshot snapshot)
        {
            // Show commentary at scoring events, quarter changes, or periodically
            if (snapshotIndex == 0) return true; // First snapshot
            if (snapshotIndex >= snapshotHistory.Count - 1) return true; // Last snapshot
            
            var prev = snapshotIndex > 0 ? snapshotHistory[snapshotIndex - 1] : null;
            if (prev == null) return false;
            
            // Score changed
            if (snapshot.HomePoints != prev.HomePoints || snapshot.AwayPoints != prev.AwayPoints)
                return true;
            
            // Quarter changed
            if (snapshot.Quarter != prev.Quarter)
                return true;
            
            // Periodic updates (every ~30 snapshots)
            return snapshotIndex % 30 == 0;
        }
        
        private void AddCommentaryEvent(string commentary)
        {
            if (string.IsNullOrEmpty(commentary)) return;
            
            if (commentaryText)
            {
                commentaryText.text = commentary;
            }
            
            if (commentaryFeedContainer != null && commentaryEntryPrefab != null)
            {
                var entry = Instantiate(commentaryEntryPrefab, commentaryFeedContainer);
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text) text.text = commentary;
                
                // Limit feed to configured max entries
                if (commentaryFeedContainer.childCount > maxCommentaryEntries)
                {
                    Destroy(commentaryFeedContainer.GetChild(0).gameObject);
                }
            }
        }
        
        /// <summary>
        /// Get all commentary events for post-match display
        /// </summary>
        public List<string> GetCommentaryHistory()
        {
            return new List<string>(commentaryHistory);
        }
        
        /// <summary>
        /// Get all match snapshots for analysis
        /// </summary>
        public List<MatchSnapshot> GetSnapshotHistory()
        {
            return new List<MatchSnapshot>(snapshotHistory);
        }
    }
}
