using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.UI.Training;
using System.Collections.Generic;

namespace AFLManager.UI.Training
{
    /// <summary>
    /// Performance analytics UI showing training effectiveness, development progress, and performance trends
    /// </summary>
    public class PerformanceAnalyticsUI : MonoBehaviour
    {
        [Header("Overall Performance")]
        [SerializeField] private TextMeshProUGUI overallScoreText;
        [SerializeField] private TextMeshProUGUI performanceGradeText;
        [SerializeField] private Slider overallScoreSlider;
        [SerializeField] private Image gradeBackground;
        
        [Header("Key Metrics")]
        [SerializeField] private TextMeshProUGUI effectivenessText;
        [SerializeField] private Slider effectivenessSlider;
        [SerializeField] private TextMeshProUGUI developmentText;
        [SerializeField] private Slider developmentSlider;
        [SerializeField] private TextMeshProUGUI conditionText;
        [SerializeField] private Slider conditionSlider;
        [SerializeField] private TextMeshProUGUI preventionText;
        [SerializeField] private Slider preventionSlider;
        
        [Header("Performance Trends")]
        [SerializeField] private Transform trendsContainer;
        [SerializeField] private GameObject trendPointPrefab;
        [SerializeField] private TextMeshProUGUI trendSummaryText;
        [SerializeField] private Image trendLineImage;
        
        [Header("Strengths and Areas for Improvement")]
        [SerializeField] private Transform strengthsContainer;
        [SerializeField] private Transform improvementContainer;
        [SerializeField] private GameObject strengthTagPrefab;
        [SerializeField] private GameObject improvementTagPrefab;
        
        [Header("Player Performance Distribution")]
        [SerializeField] private Transform performanceDistributionContainer;
        [SerializeField] private GameObject distributionBarPrefab;
        [SerializeField] private TextMeshProUGUI distributionTitleText;
        
        [Header("Training Effectiveness Breakdown")]
        [SerializeField] private Transform effectivenessBreakdownContainer;
        [SerializeField] private GameObject effectivenessComponentPrefab;
        [SerializeField] private TextMeshProUGUI effectivenessInsightsText;
        
        private TrainingAnalyticsData currentAnalytics;
        
        /// <summary>
        /// Display performance analytics
        /// </summary>
        public void DisplayPerformanceAnalytics(TrainingAnalyticsData analyticsData)
        {
            if (analyticsData == null)
            {
                Debug.LogWarning("[PerformanceAnalytics] No analytics data provided");
                return;
            }
            
            currentAnalytics = analyticsData;
            
            DisplayOverallPerformance();
            DisplayKeyMetrics();
            DisplayPerformanceTrends();
            DisplayStrengthsAndImprovements();
            DisplayPerformanceDistribution();
            DisplayEffectivenessBreakdown();
        }
        
        private void DisplayOverallPerformance()
        {
            var metrics = currentAnalytics.PerformanceMetrics;
            if (metrics == null) return;
            
            // Calculate overall score if not already done
            if (metrics.OverallTeamHealthScore == 0)
            {
                metrics.CalculateDerivedMetrics();
            }
            
            var overallScore = metrics.OverallTeamHealthScore;
            overallScoreText.text = $"Overall Score: {overallScore:F1}";
            overallScoreSlider.value = overallScore / 100f;
            
            var grade = metrics.PerformanceGrade ?? "N/A";
            performanceGradeText.text = $"Grade: {grade}";
            
            // Color-code the grade
            var gradeColor = GetGradeColor(grade);
            performanceGradeText.color = gradeColor;
            gradeBackground.color = new Color(gradeColor.r, gradeColor.g, gradeColor.b, 0.2f);
        }
        
        private void DisplayKeyMetrics()
        {
            var metrics = currentAnalytics.PerformanceMetrics;
            if (metrics == null) return;
            
            // Training Effectiveness
            effectivenessText.text = $"Training Effectiveness: {metrics.TrainingEffectivenessScore:F1}%";
            effectivenessSlider.value = metrics.TrainingEffectivenessScore / 100f;
            effectivenessText.color = GetScoreColor(metrics.TrainingEffectivenessScore);
            
            // Development Progress
            developmentText.text = $"Development Progress: {metrics.DevelopmentProgressScore:F1}%";
            developmentSlider.value = metrics.DevelopmentProgressScore / 100f;
            developmentText.color = GetScoreColor(metrics.DevelopmentProgressScore);
            
            // Average Condition
            conditionText.text = $"Team Condition: {metrics.AverageCondition:F1}%";
            conditionSlider.value = metrics.AverageCondition / 100f;
            conditionText.color = GetScoreColor(metrics.AverageCondition);
            
            // Injury Prevention
            preventionText.text = $"Injury Prevention: {metrics.InjuryPreventionScore:F1}%";
            preventionSlider.value = metrics.InjuryPreventionScore / 100f;
            preventionText.color = GetScoreColor(metrics.InjuryPreventionScore);
        }
        
        private void DisplayPerformanceTrends()
        {
            if (currentAnalytics.PerformanceTrends?.Any() != true)
            {
                trendSummaryText.text = "Insufficient data for trend analysis";
                return;
            }
            
            // Clear existing trend points
            foreach (Transform child in trendsContainer)
                Destroy(child.gameObject);
            
            var trends = currentAnalytics.PerformanceTrends.OrderBy(t => t.Date).ToList();
            
            // Display trend points
            foreach (var trend in trends)
            {
                var pointUI = Instantiate(trendPointPrefab, trendsContainer);
                var component = pointUI.GetComponent<PerformanceTrendPointUI>();
                component?.DisplayTrendPoint(trend);
            }
            
            // Calculate and display trend summary
            var trendSummary = CalculateTrendSummary(trends);
            trendSummaryText.text = trendSummary;
            trendSummaryText.color = GetTrendSummaryColor(trends);
            
            // Update trend line visualization
            UpdateTrendLine(trends);
        }
        
        private void DisplayStrengthsAndImprovements()
        {
            var metrics = currentAnalytics.PerformanceMetrics;
            if (metrics == null) return;
            
            // Display strengths
            foreach (Transform child in strengthsContainer)
                Destroy(child.gameObject);
            
            foreach (var strength in metrics.StrengthAreas)
            {
                var strengthUI = Instantiate(strengthTagPrefab, strengthsContainer);
                var textComponent = strengthUI.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = strength;
                    textComponent.color = Color.green;
                }
                
                // Add strength icon/background
                var bgImage = strengthUI.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = new Color(0f, 1f, 0f, 0.1f);
                }
            }
            
            // Display improvement areas
            foreach (Transform child in improvementContainer)
                Destroy(child.gameObject);
            
            foreach (var improvement in metrics.ImprovementAreas)
            {
                var improvementUI = Instantiate(improvementTagPrefab, improvementContainer);
                var textComponent = improvementUI.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = improvement;
                    textComponent.color = new Color(1f, 0.5f, 0f); // Orange
                }
                
                // Add improvement icon/background
                var bgImage = improvementUI.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = new Color(1f, 0.5f, 0f, 0.1f);
                }
            }
        }
        
        private void DisplayPerformanceDistribution()
        {
            if (currentAnalytics.PlayerLoadStates?.Any() != true) return;
            
            distributionTitleText.text = "Player Performance Distribution";
            
            // Clear existing bars
            foreach (Transform child in performanceDistributionContainer)
                Destroy(child.gameObject);
            
            // Calculate performance distribution based on condition
            var distribution = CalculatePerformanceDistribution();
            
            foreach (var range in distribution)
            {
                var barUI = Instantiate(distributionBarPrefab, performanceDistributionContainer);
                var component = barUI.GetComponent<PerformanceDistributionBarUI>();
                component?.DisplayDistribution(range.Key, range.Value);
            }
        }
        
        private void DisplayEffectivenessBreakdown()
        {
            // Clear existing components
            foreach (Transform child in effectivenessBreakdownContainer)
                Destroy(child.gameObject);
            
            // Create effectiveness components (mock data for now)
            var effectivenessComponents = GetEffectivenessComponents();
            
            foreach (var component in effectivenessComponents)
            {
                var componentUI = Instantiate(effectivenessComponentPrefab, effectivenessBreakdownContainer);
                var uiComponent = componentUI.GetComponent<EffectivenessComponentUI>();
                uiComponent?.DisplayComponent(component.Key, component.Value);
            }
            
            // Display insights
            effectivenessInsightsText.text = GenerateEffectivenessInsights();
        }
        
        #region Helper Methods
        
        private Color GetGradeColor(string grade)
        {
            return grade switch
            {
                "A" => Color.green,
                "B" => Color.cyan,
                "C" => Color.yellow,
                "D" => new Color(1f, 0.5f, 0f), // Orange
                "F" => Color.red,
                _ => Color.white
            };
        }
        
        private Color GetScoreColor(float score)
        {
            if (score >= 80) return Color.green;
            if (score >= 60) return Color.yellow;
            if (score >= 40) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }
        
        private string CalculateTrendSummary(List<PerformanceTrend> trends)
        {
            if (trends.Count < 2) return "Insufficient data";
            
            var recent = trends.TakeLast(2).ToList();
            var current = recent.Last();
            var previous = recent.First();
            
            var conditionChange = current.ConditionAverage - previous.ConditionAverage;
            var effectivenessChange = current.EffectivenessScore - previous.EffectivenessScore;
            
            if (conditionChange > 2 && effectivenessChange > 2)
                return "↗ Strong Improvement";
            if (conditionChange > 0 && effectivenessChange > 0)
                return "↗ Improving";
            if (conditionChange < -2 || effectivenessChange < -2)
                return "↘ Declining";
            
            return "→ Stable";
        }
        
        private Color GetTrendSummaryColor(List<PerformanceTrend> trends)
        {
            var summary = CalculateTrendSummary(trends);
            if (summary.Contains("Improvement") || summary.Contains("Improving")) return Color.green;
            if (summary.Contains("Declining")) return Color.red;
            return Color.white;
        }
        
        private void UpdateTrendLine(List<PerformanceTrend> trends)
        {
            // Simple trend line visualization
            // In a real implementation, this would draw a line graph
            var isImproving = trends.Count >= 2 && trends.Last().IsImproving;
            trendLineImage.color = isImproving ? Color.green : Color.red;
        }
        
        private Dictionary<string, int> CalculatePerformanceDistribution()
        {
            var playerStates = currentAnalytics.PlayerLoadStates;
            
            return new Dictionary<string, int>
            {
                ["Excellent (80+)"] = playerStates.Count(p => p.Condition >= 80),
                ["Good (60-79)"] = playerStates.Count(p => p.Condition >= 60 && p.Condition < 80),
                ["Fair (40-59)"] = playerStates.Count(p => p.Condition >= 40 && p.Condition < 60),
                ["Poor (<40)"] = playerStates.Count(p => p.Condition < 40)
            };
        }
        
        private Dictionary<string, float> GetEffectivenessComponents()
        {
            // Mock effectiveness breakdown
            return new Dictionary<string, float>
            {
                ["Load Management"] = 85.2f,
                ["Session Planning"] = 78.6f,
                ["Player Engagement"] = 82.1f,
                ["Recovery Protocols"] = 79.4f,
                ["Skill Development"] = 81.8f,
                ["Injury Prevention"] = 87.3f
            };
        }
        
        private string GenerateEffectivenessInsights()
        {
            var metrics = currentAnalytics.PerformanceMetrics;
            if (metrics == null) return "No insights available.";
            
            var insights = new List<string>();
            
            if (metrics.TrainingEffectivenessScore >= 80)
                insights.Add("Training programs are highly effective");
            else if (metrics.TrainingEffectivenessScore < 60)
                insights.Add("Training effectiveness needs improvement");
            
            if (metrics.AverageCondition >= 80)
                insights.Add("Team condition is excellent");
            else if (metrics.AverageCondition < 60)
                insights.Add("Focus on player conditioning required");
            
            if (metrics.InjuryPreventionScore >= 85)
                insights.Add("Injury prevention protocols are working well");
            
            return insights.Any() ? string.Join(". ", insights) + "." : "Continue monitoring performance metrics.";
        }
        
        #endregion
    }
}

/// <summary>
/// Performance trend point UI component
/// </summary>
public class PerformanceTrendPointUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private TextMeshProUGUI effectivenessText;
    [SerializeField] private Image trendIcon;
    [SerializeField] private Image backgroundImage;
    
    public void DisplayTrendPoint(PerformanceTrend trend)
    {
        dateText.text = trend.TrendLabel;
        conditionText.text = $"Condition: {trend.ConditionAverage:F1}%";
        effectivenessText.text = $"Effectiveness: {trend.EffectivenessScore:F1}%";
        
        // Set trend icon and background color
        if (trend.IsImproving)
        {
            trendIcon.color = Color.green;
            backgroundImage.color = new Color(0f, 1f, 0f, 0.1f);
        }
        else
        {
            trendIcon.color = Color.red;
            backgroundImage.color = new Color(1f, 0f, 0f, 0.1f);
        }
    }
}

/// <summary>
/// Performance distribution bar UI component
/// </summary>
public class PerformanceDistributionBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Slider distributionBar;
    [SerializeField] private Image barFillImage;
    
    public void DisplayDistribution(string range, int count)
    {
        rangeText.text = range;
        countText.text = $"{count} players";
        
        // Calculate percentage (assuming max 30 players)
        var percentage = count / 30f;
        distributionBar.value = percentage;
        
        // Color-code based on performance range
        var barColor = GetPerformanceColor(range);
        barFillImage.color = barColor;
    }
    
    private Color GetPerformanceColor(string range)
    {
        if (range.Contains("Excellent")) return Color.green;
        if (range.Contains("Good")) return Color.cyan;
        if (range.Contains("Fair")) return Color.yellow;
        return Color.red; // Poor
    }
}

/// <summary>
/// Effectiveness component UI
/// </summary>
public class EffectivenessComponentUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI componentNameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Slider scoreSlider;
    [SerializeField] private Image statusIcon;
    
    public void DisplayComponent(string componentName, float score)
    {
        componentNameText.text = componentName;
        scoreText.text = $"{score:F1}%";
        scoreSlider.value = score / 100f;
        
        // Color-code the component
        var scoreColor = GetComponentColor(score);
        scoreText.color = scoreColor;
        statusIcon.color = scoreColor;
    }
    
    private Color GetComponentColor(float score)
    {
        if (score >= 85) return Color.green;
        if (score >= 75) return Color.cyan;
        if (score >= 65) return Color.yellow;
        return new Color(1f, 0.5f, 0f); // Orange
    }
}