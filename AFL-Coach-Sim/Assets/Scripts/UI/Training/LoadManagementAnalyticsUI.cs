using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.UI.Training;

namespace AFLManager.UI.Training
{
    /// <summary>
    /// Load management analytics UI showing load distributions, risk assessments, and recommendations
    /// </summary>
    public class LoadManagementAnalyticsUI : MonoBehaviour
    {
        [Header("Load Overview")]
        [SerializeField] private TextMeshProUGUI teamAverageLoadText;
        [SerializeField] private Slider teamAverageLoadSlider;
        [SerializeField] private TextMeshProUGUI loadStatusText;
        [SerializeField] private TextMeshProUGUI loadTrendText;
        
        [Header("Risk Assessment")]
        [SerializeField] private Transform riskLevelContainer;
        [SerializeField] private GameObject riskLevelBarPrefab;
        [SerializeField] private TextMeshProUGUI criticalPlayersText;
        [SerializeField] private TextMeshProUGUI highRiskPlayersText;
        [SerializeField] private TextMeshProUGUI moderateRiskPlayersText;
        
        [Header("Load Distribution Chart")]
        [SerializeField] private Transform loadChartContainer;
        [SerializeField] private GameObject loadChartBarPrefab;
        [SerializeField] private TextMeshProUGUI chartTitleText;
        
        [Header("Weekly Load Comparison")]
        [SerializeField] private Transform weeklyComparisonContainer;
        [SerializeField] private GameObject weeklyComparisonPrefab;
        [SerializeField] private TextMeshProUGUI weeklyTrendText;
        
        [Header("Action Recommendations")]
        [SerializeField] private Transform recommendationsContainer;
        [SerializeField] private GameObject recommendationPrefab;
        [SerializeField] private TextMeshProUGUI noRecommendationsText;
        
        private TrainingAnalyticsData currentAnalytics;
        
        /// <summary>
        /// Display load management analytics
        /// </summary>
        public void DisplayLoadAnalytics(TrainingAnalyticsData analyticsData)
        {
            if (analyticsData == null)
            {
                Debug.LogWarning("[LoadManagementAnalytics] No analytics data provided");
                return;
            }
            
            currentAnalytics = analyticsData;
            
            DisplayLoadOverview();
            DisplayRiskAssessment();
            DisplayLoadDistribution();
            DisplayWeeklyComparison();
            DisplayRecommendations();
        }
        
        private void DisplayLoadOverview()
        {
            var averageLoad = currentAnalytics.TeamAverageLoad;
            teamAverageLoadText.text = $"Team Avg Load: {averageLoad:F1}";
            teamAverageLoadSlider.value = averageLoad / 100f; // Assuming 100 is max load
            
            // Color-code the load status
            var loadColor = GetLoadColor(averageLoad);
            teamAverageLoadText.color = loadColor;
            
            // Determine load status
            var loadStatus = GetLoadStatus(averageLoad);
            loadStatusText.text = $"Status: {loadStatus}";
            loadStatusText.color = loadColor;
            
            // Show trend (mock for now)
            var loadTrend = CalculateLoadTrend();
            loadTrendText.text = $"Trend: {loadTrend}";
            loadTrendText.color = GetTrendColor(loadTrend);
        }
        
        private void DisplayRiskAssessment()
        {
            if (currentAnalytics.PlayerLoadStates?.Any() != true) return;
            
            var playerStates = currentAnalytics.PlayerLoadStates;
            
            var criticalCount = playerStates.Count(p => p.RiskLevel == FatigueRiskLevel.Critical);
            var highCount = playerStates.Count(p => p.RiskLevel == FatigueRiskLevel.High);
            var moderateCount = playerStates.Count(p => p.RiskLevel == FatigueRiskLevel.Moderate);
            
            criticalPlayersText.text = $"Critical Risk: {criticalCount}";
            criticalPlayersText.color = criticalCount > 0 ? Color.magenta : Color.white;
            
            highRiskPlayersText.text = $"High Risk: {highCount}";
            highRiskPlayersText.color = highCount > 0 ? Color.red : Color.white;
            
            moderateRiskPlayersText.text = $"Moderate Risk: {moderateCount}";
            moderateRiskPlayersText.color = moderateCount > 0 ? Color.yellow : Color.white;
            
            // Create risk level bars
            DisplayRiskLevelBars(playerStates);
        }
        
        private void DisplayRiskLevelBars(System.Collections.Generic.List<PlayerLoadAnalytics> playerStates)
        {
            // Clear existing bars
            foreach (Transform child in riskLevelContainer)
                Destroy(child.gameObject);
            
            var riskGroups = playerStates.GroupBy(p => p.RiskLevel);
            
            foreach (var group in riskGroups)
            {
                var barUI = Instantiate(riskLevelBarPrefab, riskLevelContainer);
                var component = barUI.GetComponent<RiskLevelBarUI>();
                component?.DisplayRiskLevel(group.Key, group.Count(), playerStates.Count);
            }
        }
        
        private void DisplayLoadDistribution()
        {
            if (currentAnalytics.LoadDistribution == null) return;
            
            chartTitleText.text = "Load Distribution";
            
            // Clear existing bars
            foreach (Transform child in loadChartContainer)
                Destroy(child.gameObject);
            
            var totalPlayers = currentAnalytics.LoadDistribution.Values.Sum();
            
            foreach (var distribution in currentAnalytics.LoadDistribution)
            {
                var barUI = Instantiate(loadChartBarPrefab, loadChartContainer);
                var component = barUI.GetComponent<LoadDistributionBarUI>();
                
                var percentage = totalPlayers > 0 ? (float)distribution.Value / totalPlayers * 100f : 0f;
                component?.DisplayDistribution(distribution.Key, distribution.Value, percentage);
            }
        }
        
        private void DisplayWeeklyComparison()
        {
            if (currentAnalytics.WeeklySessionCounts?.Any() != true) return;
            
            // Clear existing comparison items
            foreach (Transform child in weeklyComparisonContainer)
                Destroy(child.gameObject);
            
            foreach (var weekData in currentAnalytics.WeeklySessionCounts.Take(4)) // Show last 4 weeks
            {
                var comparisonUI = Instantiate(weeklyComparisonPrefab, weeklyComparisonContainer);
                var component = comparisonUI.GetComponent<WeeklyLoadComparisonUI>();
                component?.DisplayWeeklyComparison(weekData);
            }
            
            // Show overall weekly trend
            var trend = CalculateWeeklyLoadTrend();
            weeklyTrendText.text = $"Weekly Trend: {trend}";
            weeklyTrendText.color = GetTrendColor(trend);
        }
        
        private void DisplayRecommendations()
        {
            var loadRecommendations = currentAnalytics.TrainingRecommendations?
                .Where(r => r.Type == RecommendationType.LoadReduction || 
                           r.Type == RecommendationType.LoadIncrease ||
                           r.Type == RecommendationType.PlayerRotation)
                .ToList();
            
            if (loadRecommendations?.Any() != true)
            {
                noRecommendationsText.gameObject.SetActive(true);
                noRecommendationsText.text = "No load management recommendations at this time.";
                return;
            }
            
            noRecommendationsText.gameObject.SetActive(false);
            
            // Clear existing recommendations
            foreach (Transform child in recommendationsContainer)
                Destroy(child.gameObject);
            
            foreach (var recommendation in loadRecommendations)
            {
                var recUI = Instantiate(recommendationPrefab, recommendationsContainer);
                var component = recUI.GetComponent<LoadRecommendationUI>();
                component?.DisplayRecommendation(recommendation);
            }
        }
        
        #region Helper Methods
        
        private Color GetLoadColor(float load)
        {
            if (load >= 80) return Color.red;      // High load
            if (load >= 60) return Color.yellow;   // Moderate load
            if (load >= 40) return Color.green;    // Optimal load
            return Color.cyan;                     // Low load
        }
        
        private string GetLoadStatus(float load)
        {
            if (load >= 80) return "High Load";
            if (load >= 60) return "Moderate Load";
            if (load >= 40) return "Optimal";
            return "Under Load";
        }
        
        private string CalculateLoadTrend()
        {
            // Mock trend calculation
            // In real implementation, compare with previous period
            var trends = new[] { "↗ Increasing", "↘ Decreasing", "→ Stable" };
            return trends[Random.Range(0, trends.Length)];
        }
        
        private Color GetTrendColor(string trend)
        {
            if (trend.Contains("Increasing")) return Color.red;
            if (trend.Contains("Decreasing")) return Color.green;
            return Color.white;
        }
        
        private string CalculateWeeklyLoadTrend()
        {
            if (currentAnalytics.WeeklySessionCounts?.Count < 2) return "→ Insufficient Data";
            
            var recentWeeks = currentAnalytics.WeeklySessionCounts.TakeLast(2).ToList();
            var current = recentWeeks.Last().AverageLoad;
            var previous = recentWeeks.First().AverageLoad;
            
            var change = current - previous;
            
            if (change > 5) return "↗ Increasing";
            if (change < -5) return "↘ Decreasing";
            return "→ Stable";
        }
        
        #endregion
    }
}

/// <summary>
/// Individual risk level bar UI component
/// </summary>
public class RiskLevelBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI riskLevelText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Slider riskBarSlider;
    [SerializeField] private Image barFillImage;
    
    public void DisplayRiskLevel(FatigueRiskLevel riskLevel, int playerCount, int totalPlayers)
    {
        riskLevelText.text = riskLevel.ToString();
        playerCountText.text = $"{playerCount} players";
        
        var percentage = totalPlayers > 0 ? (float)playerCount / totalPlayers : 0f;
        riskBarSlider.value = percentage;
        
        // Color-code the bar
        var riskColor = GetRiskColor(riskLevel);
        barFillImage.color = riskColor;
        riskLevelText.color = riskColor;
    }
    
    private Color GetRiskColor(FatigueRiskLevel risk)
    {
        return risk switch
        {
            FatigueRiskLevel.Low => Color.green,
            FatigueRiskLevel.Moderate => Color.yellow,
            FatigueRiskLevel.High => Color.red,
            FatigueRiskLevel.Critical => Color.magenta,
            _ => Color.white
        };
    }
}

/// <summary>
/// Load distribution bar UI component
/// </summary>
public class LoadDistributionBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private Slider distributionBar;
    [SerializeField] private Image barFillImage;
    
    public void DisplayDistribution(string range, int count)
    {
        DisplayDistribution(range, count, 0f);
    }
    
    public void DisplayDistribution(string range, int count, float percentage)
    {
        rangeText.text = range;
        countText.text = $"{count} players";
        percentageText.text = $"{percentage:F1}%";
        
        distributionBar.value = percentage / 100f;
        
        // Color-code based on load range
        var barColor = GetLoadRangeColor(range);
        barFillImage.color = barColor;
    }
    
    private Color GetLoadRangeColor(string range)
    {
        if (range.Contains("Very High")) return Color.red;
        if (range.Contains("High")) return new Color(1f, 0.5f, 0f); // Orange
        if (range.Contains("Moderate")) return Color.yellow;
        return Color.green; // Low
    }
}

/// <summary>
/// Weekly load comparison UI component
/// </summary>
public class WeeklyLoadComparisonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI weekLabelText;
    [SerializeField] private TextMeshProUGUI loadValueText;
    [SerializeField] private Slider loadBar;
    [SerializeField] private TextMeshProUGUI completionRateText;
    
    public void DisplayWeeklyComparison(WeeklySessionCount weekData)
    {
        weekLabelText.text = weekData.WeekLabel;
        loadValueText.text = $"{weekData.AverageLoad:F1}";
        loadBar.value = weekData.AverageLoad / 100f; // Assuming 100 is max
        completionRateText.text = $"{weekData.CompletionRate * 100:F0}%";
        
        // Color-code completion rate
        var completionColor = weekData.CompletionRate >= 0.8f ? Color.green : 
                             weekData.CompletionRate >= 0.6f ? Color.yellow : Color.red;
        completionRateText.color = completionColor;
    }
}

/// <summary>
/// Load recommendation UI component
/// </summary>
public class LoadRecommendationUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI priorityText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Image priorityIcon;
    
    private TrainingRecommendation currentRecommendation;
    
    public void DisplayRecommendation(TrainingRecommendation recommendation)
    {
        currentRecommendation = recommendation;
        
        titleText.text = recommendation.Title;
        messageText.text = recommendation.Message;
        priorityText.text = recommendation.PriorityText;
        
        // Color-code priority
        var priorityColor = recommendation.PriorityColor;
        priorityText.color = priorityColor;
        priorityIcon.color = priorityColor;
        
        // Setup action button
        if (recommendation.IsActionable && !string.IsNullOrEmpty(recommendation.ActionButton))
        {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = recommendation.ActionButton;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }
        else
        {
            actionButton.gameObject.SetActive(false);
        }
    }
    
    private void OnActionButtonClicked()
    {
        // Handle recommendation action
        Debug.Log($"[LoadRecommendation] Action clicked: {currentRecommendation?.ActionButton}");
        
        // TODO: Implement specific actions based on recommendation type
        // This could trigger events or call methods on training managers
    }
}