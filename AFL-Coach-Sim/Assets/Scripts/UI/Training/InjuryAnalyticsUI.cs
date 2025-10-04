using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.UI.Training;
using System.Collections.Generic;

namespace AFLManager.UI.Training
{
    /// <summary>
    /// Injury analytics UI showing injury trends, risk analysis, prevention tips, and injury-related recommendations
    /// </summary>
    public class InjuryAnalyticsUI : MonoBehaviour
    {
        [Header("Injury Overview")]
        [SerializeField] private TextMeshProUGUI totalInjuriesText;
        [SerializeField] private TextMeshProUGUI injuryRateText;
        [SerializeField] private TextMeshProUGUI averageRecoveryText;
        [SerializeField] private TextMeshProUGUI daysWithoutInjuryText;
        
        [Header("Current Risk Assessment")]
        [SerializeField] private Transform highRiskPlayersContainer;
        [SerializeField] private GameObject highRiskPlayerPrefab;
        [SerializeField] private TextMeshProUGUI riskSummaryText;
        [SerializeField] private Slider overallRiskSlider;
        
        [Header("Injury Breakdown")]
        [SerializeField] private Transform injuryTypeContainer;
        [SerializeField] private GameObject injuryTypeBarPrefab;
        [SerializeField] private Transform bodyPartContainer;
        [SerializeField] private GameObject bodyPartBarPrefab;
        [SerializeField] private TextMeshProUGUI breakdownTitleText;
        
        [Header("Prevention Tips")]
        [SerializeField] private Transform preventionTipsContainer;
        [SerializeField] private GameObject preventionTipPrefab;
        [SerializeField] private TextMeshProUGUI noTipsText;
        
        [Header("Risk Factors")]
        [SerializeField] private Transform riskFactorsContainer;
        [SerializeField] private GameObject riskFactorPrefab;
        [SerializeField] private TextMeshProUGUI preventionScoreText;
        [SerializeField] private Slider preventionScoreSlider;
        
        [Header("Success Metrics")]
        [SerializeField] private Transform successMeasuresContainer;
        [SerializeField] private GameObject successMeasurePrefab;
        [SerializeField] private TextMeshProUGUI preventableInjuriesText;
        
        private TrainingAnalyticsData currentAnalytics;
        
        /// <summary>
        /// Display injury analytics
        /// </summary>
        public void DisplayInjuryAnalytics(TrainingAnalyticsData analyticsData)
        {
            if (analyticsData == null)
            {
                Debug.LogWarning("[InjuryAnalytics] No analytics data provided");
                return;
            }
            
            currentAnalytics = analyticsData;
            
            DisplayInjuryOverview();
            DisplayCurrentRiskAssessment();
            DisplayInjuryBreakdown();
            DisplayPreventionTips();
            DisplayRiskFactors();
            DisplaySuccessMetrics();
        }
        
        private void DisplayInjuryOverview()
        {
            var metrics = currentAnalytics.InjuryMetrics;
            if (metrics == null)
            {
                DisplayNoInjuryData();
                return;
            }
            
            totalInjuriesText.text = $"Total Injuries: {metrics.TotalInjuries}";
            totalInjuriesText.color = metrics.TotalInjuries > 5 ? Color.red : 
                                      metrics.TotalInjuries > 2 ? Color.yellow : Color.green;
            
            injuryRateText.text = $"Injury Rate: {metrics.InjuryRate:F1} per 100 hours";
            injuryRateText.color = GetInjuryRateColor(metrics.InjuryRate);
            
            var recoveryDays = metrics.AverageRecoveryTime.Days;
            averageRecoveryText.text = $"Avg Recovery: {recoveryDays} days";
            averageRecoveryText.color = recoveryDays > 14 ? Color.red : recoveryDays > 7 ? Color.yellow : Color.green;
            
            daysWithoutInjuryText.text = $"Days Without Injury: {metrics.DaysWithoutInjury}";
            daysWithoutInjuryText.color = metrics.DaysWithoutInjury >= 30 ? Color.green : 
                                          metrics.DaysWithoutInjury >= 14 ? Color.yellow : Color.red;
        }
        
        private void DisplayCurrentRiskAssessment()
        {
            if (currentAnalytics.PlayerLoadStates?.Any() != true)
            {
                riskSummaryText.text = "No player data available for risk assessment";
                return;
            }
            
            // Clear existing high-risk players
            foreach (Transform child in highRiskPlayersContainer)
                Destroy(child.gameObject);
            
            var highRiskPlayers = currentAnalytics.PlayerLoadStates
                .Where(p => p.RiskLevel >= FatigueRiskLevel.High)
                .OrderByDescending(p => p.RiskLevel)
                .ToList();
            
            if (!highRiskPlayers.Any())
            {
                riskSummaryText.text = "No players currently at high injury risk";
                riskSummaryText.color = Color.green;
                overallRiskSlider.value = 0.2f; // Low risk
                return;
            }
            
            // Display high-risk players
            foreach (var player in highRiskPlayers.Take(5)) // Show top 5 high-risk players
            {
                var playerUI = Instantiate(highRiskPlayerPrefab, highRiskPlayersContainer);
                var component = playerUI.GetComponent<InjuryRiskPlayerUI>();
                component?.DisplayRiskPlayer(player);
            }
            
            // Calculate overall risk level
            var overallRisk = CalculateOverallRisk(currentAnalytics.PlayerLoadStates);
            riskSummaryText.text = $"Team Injury Risk: {overallRisk}";
            riskSummaryText.color = GetRiskColor(overallRisk);
            overallRiskSlider.value = GetRiskSliderValue(overallRisk);
        }
        
        private void DisplayInjuryBreakdown()
        {
            var metrics = currentAnalytics.InjuryMetrics;
            if (metrics == null) return;
            
            breakdownTitleText.text = "Injury Analysis";
            
            // Display injury types
            DisplayInjuryTypes(metrics.InjuriesByType);
            
            // Display body parts
            DisplayBodyPartBreakdown(metrics.InjuriesByBodyPart);
        }
        
        private void DisplayInjuryTypes(Dictionary<string, int> injuriesByType)
        {
            // Clear existing type bars
            foreach (Transform child in injuryTypeContainer)
                Destroy(child.gameObject);
            
            if (!injuriesByType.Any())
            {
                var noDataUI = Instantiate(injuryTypeBarPrefab, injuryTypeContainer);
                var textComponent = noDataUI.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                    textComponent.text = "No injury type data available";
                return;
            }
            
            var totalInjuries = injuriesByType.Values.Sum();
            
            foreach (var injuryType in injuriesByType.OrderByDescending(x => x.Value))
            {
                var typeUI = Instantiate(injuryTypeBarPrefab, injuryTypeContainer);
                var component = typeUI.GetComponent<InjuryTypeBarUI>();
                var percentage = totalInjuries > 0 ? (float)injuryType.Value / totalInjuries * 100f : 0f;
                component?.DisplayInjuryType(injuryType.Key, injuryType.Value, percentage);
            }
        }
        
        private void DisplayBodyPartBreakdown(Dictionary<string, int> injuriesByBodyPart)
        {
            // Clear existing body part bars
            foreach (Transform child in bodyPartContainer)
                Destroy(child.gameObject);
            
            if (!injuriesByBodyPart.Any()) return;
            
            var totalInjuries = injuriesByBodyPart.Values.Sum();
            
            foreach (var bodyPart in injuriesByBodyPart.OrderByDescending(x => x.Value))
            {
                var partUI = Instantiate(bodyPartBarPrefab, bodyPartContainer);
                var component = partUI.GetComponent<InjuryBodyPartBarUI>();
                var percentage = totalInjuries > 0 ? (float)bodyPart.Value / totalInjuries * 100f : 0f;
                component?.DisplayBodyPart(bodyPart.Key, bodyPart.Value, percentage);
            }
        }
        
        private void DisplayPreventionTips()
        {
            var metrics = currentAnalytics.InjuryMetrics;
            var tips = metrics?.InjuryPrevention ?? new List<InjuryPreventionTip>();
            
            if (!tips.Any())
            {
                noTipsText.gameObject.SetActive(true);
                noTipsText.text = "No specific prevention tips available at this time.";
                return;
            }
            
            noTipsText.gameObject.SetActive(false);
            
            // Clear existing tips
            foreach (Transform child in preventionTipsContainer)
                Destroy(child.gameObject);
            
            // Sort tips by priority and display
            var sortedTips = tips.OrderByDescending(t => (int)t.Priority).ToList();
            
            foreach (var tip in sortedTips)
            {
                var tipUI = Instantiate(preventionTipPrefab, preventionTipsContainer);
                var component = tipUI.GetComponent<InjuryPreventionTipUI>();
                component?.DisplayPreventionTip(tip);
            }
        }
        
        private void DisplayRiskFactors()
        {
            var metrics = currentAnalytics.InjuryMetrics;
            var riskFactors = metrics?.RiskFactors ?? new List<InjuryRiskFactor>();
            
            // Display prevention effectiveness score
            var preventionScore = metrics?.PreventionEffectivenessScore ?? 0f;
            preventionScoreText.text = $"Prevention Effectiveness: {preventionScore:F1}%";
            preventionScoreSlider.value = preventionScore / 100f;
            preventionScoreText.color = GetScoreColor(preventionScore);
            
            // Clear existing risk factors
            foreach (Transform child in riskFactorsContainer)
                Destroy(child.gameObject);
            
            if (!riskFactors.Any())
            {
                // Add default risk factors with mock data
                riskFactors = GetDefaultRiskFactors();
            }
            
            foreach (var riskFactor in riskFactors.OrderByDescending(rf => rf.RiskMultiplier))
            {
                var factorUI = Instantiate(riskFactorPrefab, riskFactorsContainer);
                var component = factorUI.GetComponent<InjuryRiskFactorUI>();
                component?.DisplayRiskFactor(riskFactor);
            }
        }
        
        private void DisplaySuccessMetrics()
        {
            var metrics = currentAnalytics.InjuryMetrics;
            
            // Display preventable injury percentage
            var preventablePercentage = metrics?.PreventableInjuryPercentage ?? 0f;
            preventableInjuriesText.text = $"Preventable Injuries: {preventablePercentage:F1}%";
            preventableInjuriesText.color = preventablePercentage > 70f ? Color.red : 
                                           preventablePercentage > 40f ? Color.yellow : Color.green;
            
            // Display successful prevention measures
            var successMeasures = metrics?.SuccessfulPreventionMeasures ?? new List<string>();
            
            // Clear existing success measures
            foreach (Transform child in successMeasuresContainer)
                Destroy(child.gameObject);
            
            if (!successMeasures.Any())
            {
                successMeasures = GetDefaultSuccessMeasures();
            }
            
            foreach (var measure in successMeasures)
            {
                var measureUI = Instantiate(successMeasurePrefab, successMeasuresContainer);
                var textComponent = measureUI.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"✓ {measure}";
                    textComponent.color = Color.green;
                }
            }
        }
        
        #region Helper Methods
        
        private void DisplayNoInjuryData()
        {
            totalInjuriesText.text = "Total Injuries: 0";
            totalInjuriesText.color = Color.green;
            
            injuryRateText.text = "Injury Rate: 0.0 per 100 hours";
            injuryRateText.color = Color.green;
            
            averageRecoveryText.text = "Avg Recovery: N/A";
            averageRecoveryText.color = Color.white;
            
            daysWithoutInjuryText.text = "Days Without Injury: ∞";
            daysWithoutInjuryText.color = Color.green;
        }
        
        private Color GetInjuryRateColor(float rate)
        {
            if (rate > 5f) return Color.red;    // High injury rate
            if (rate > 2f) return Color.yellow; // Moderate injury rate
            return Color.green;                 // Low injury rate
        }
        
        private string CalculateOverallRisk(List<PlayerLoadAnalytics> playerStates)
        {
            var criticalCount = playerStates.Count(p => p.RiskLevel == FatigueRiskLevel.Critical);
            var highCount = playerStates.Count(p => p.RiskLevel == FatigueRiskLevel.High);
            var moderateCount = playerStates.Count(p => p.RiskLevel == FatigueRiskLevel.Moderate);
            
            if (criticalCount > 0) return "Critical";
            if (highCount > 3) return "High";
            if (highCount > 0 || moderateCount > 5) return "Moderate";
            return "Low";
        }
        
        private Color GetRiskColor(string risk)
        {
            return risk switch
            {
                "Critical" => Color.magenta,
                "High" => Color.red,
                "Moderate" => Color.yellow,
                "Low" => Color.green,
                _ => Color.white
            };
        }
        
        private float GetRiskSliderValue(string risk)
        {
            return risk switch
            {
                "Critical" => 0.95f,
                "High" => 0.75f,
                "Moderate" => 0.5f,
                "Low" => 0.2f,
                _ => 0f
            };
        }
        
        private Color GetScoreColor(float score)
        {
            if (score >= 80) return Color.green;
            if (score >= 60) return Color.yellow;
            if (score >= 40) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }
        
        private List<InjuryRiskFactor> GetDefaultRiskFactors()
        {
            return new List<InjuryRiskFactor>
            {
                new InjuryRiskFactor
                {
                    FactorName = "High Training Load",
                    RiskMultiplier = 1.8f,
                    AffectedPlayers = currentAnalytics.PlayerLoadStates?.Count(p => p.CurrentLoad > 70) ?? 0,
                    Mitigation = "Reduce training intensity, add recovery sessions",
                    IsModifiable = true
                },
                new InjuryRiskFactor
                {
                    FactorName = "Poor Recovery",
                    RiskMultiplier = 1.5f,
                    AffectedPlayers = currentAnalytics.PlayerLoadStates?.Count(p => p.RecoveryEfficiency < 70) ?? 0,
                    Mitigation = "Improve sleep quality, nutrition protocols",
                    IsModifiable = true
                },
                new InjuryRiskFactor
                {
                    FactorName = "Previous Injury History",
                    RiskMultiplier = 1.3f,
                    AffectedPlayers = 0, // Would come from injury history
                    Mitigation = "Targeted prevention exercises, monitoring",
                    IsModifiable = false
                }
            };
        }
        
        private List<string> GetDefaultSuccessMeasures()
        {
            return new List<string>
            {
                "Load monitoring protocols implemented",
                "Regular fitness testing in place",
                "Recovery session compliance tracking",
                "Injury prevention exercise programs",
                "Nutrition and hydration monitoring"
            };
        }
        
        #endregion
    }
}

/// <summary>
/// Individual high-risk player UI component
/// </summary>
public class InjuryRiskPlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI riskLevelText;
    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private Image riskIndicator;
    [SerializeField] private Button viewDetailsButton;
    
    private PlayerLoadAnalytics currentPlayer;
    
    public void DisplayRiskPlayer(PlayerLoadAnalytics player)
    {
        currentPlayer = player;
        
        playerNameText.text = player.PlayerName;
        riskLevelText.text = player.RiskLevelText;
        actionText.text = player.RecommendedAction;
        
        // Color-code risk level
        var riskColor = player.RiskLevelColor;
        riskLevelText.color = riskColor;
        riskIndicator.color = riskColor;
        
        // Setup view details button
        viewDetailsButton?.onClick.RemoveAllListeners();
        viewDetailsButton?.onClick.AddListener(() => OnViewDetailsClicked(player.PlayerId));
    }
    
    private void OnViewDetailsClicked(int playerId)
    {
        Debug.Log($"[InjuryRisk] View details for player {playerId}");
        // TODO: Implement detailed player injury risk view
    }
}

/// <summary>
/// Injury type bar UI component
/// </summary>
public class InjuryTypeBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI typeNameText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private Slider typeBar;
    [SerializeField] private Image barFillImage;
    
    public void DisplayInjuryType(string typeName, int count, float percentage)
    {
        typeNameText.text = typeName;
        countText.text = count.ToString();
        percentageText.text = $"{percentage:F1}%";
        
        typeBar.value = percentage / 100f;
        
        // Color-code based on frequency
        var barColor = GetInjuryTypeColor(percentage);
        barFillImage.color = barColor;
    }
    
    private Color GetInjuryTypeColor(float percentage)
    {
        if (percentage > 30f) return Color.red;      // High frequency
        if (percentage > 15f) return Color.yellow;   // Moderate frequency
        return Color.green;                          // Low frequency
    }
}

/// <summary>
/// Body part injury bar UI component
/// </summary>
public class InjuryBodyPartBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bodyPartText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private Slider bodyPartBar;
    [SerializeField] private Image barFillImage;
    
    public void DisplayBodyPart(string bodyPart, int count, float percentage)
    {
        bodyPartText.text = bodyPart;
        countText.text = count.ToString();
        percentageText.text = $"{percentage:F1}%";
        
        bodyPartBar.value = percentage / 100f;
        
        // Color-code based on body part criticality
        var barColor = GetBodyPartColor(bodyPart);
        barFillImage.color = barColor;
    }
    
    private Color GetBodyPartColor(string bodyPart)
    {
        var criticalParts = new[] { "Knee", "Ankle", "Shoulder", "Hamstring" };
        return criticalParts.Any(cp => bodyPart.Contains(cp)) ? Color.red : Color.yellow;
    }
}

/// <summary>
/// Injury prevention tip UI component
/// </summary>
public class InjuryPreventionTipUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI priorityText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private Image priorityIcon;
    [SerializeField] private Transform applicablePlayersContainer;
    [SerializeField] private GameObject playerTagPrefab;
    
    public void DisplayPreventionTip(InjuryPreventionTip tip)
    {
        titleText.text = tip.Title;
        descriptionText.text = tip.Description;
        priorityText.text = tip.Priority.ToString();
        categoryText.text = tip.Category;
        
        // Color-code priority
        var priorityColor = tip.PriorityColor;
        priorityText.color = priorityColor;
        priorityIcon.color = priorityColor;
        
        // Display applicable players
        DisplayApplicablePlayers(tip.ApplicablePlayers);
    }
    
    private void DisplayApplicablePlayers(List<string> playerNames)
    {
        // Clear existing player tags
        foreach (Transform child in applicablePlayersContainer)
            Destroy(child.gameObject);
        
        foreach (var playerName in playerNames.Take(3)) // Show max 3 players
        {
            var playerUI = Instantiate(playerTagPrefab, applicablePlayersContainer);
            var textComponent = playerUI.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = playerName;
                textComponent.color = Color.cyan;
            }
        }
        
        if (playerNames.Count > 3)
        {
            var moreUI = Instantiate(playerTagPrefab, applicablePlayersContainer);
            var textComponent = moreUI.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"+{playerNames.Count - 3} more";
                textComponent.color = Color.gray;
            }
        }
    }
}

/// <summary>
/// Injury risk factor UI component
/// </summary>
public class InjuryRiskFactorUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI factorNameText;
    [SerializeField] private TextMeshProUGUI riskMultiplierText;
    [SerializeField] private TextMeshProUGUI affectedPlayersText;
    [SerializeField] private TextMeshProUGUI mitigationText;
    [SerializeField] private Image modifiableIcon;
    [SerializeField] private Slider riskSlider;
    
    public void DisplayRiskFactor(InjuryRiskFactor riskFactor)
    {
        factorNameText.text = riskFactor.FactorName;
        riskMultiplierText.text = $"{riskFactor.RiskMultiplier:F1}x risk";
        affectedPlayersText.text = $"{riskFactor.AffectedPlayers} players";
        mitigationText.text = riskFactor.Mitigation;
        
        // Show risk level on slider (normalized)
        var normalizedRisk = Mathf.Clamp01((riskFactor.RiskMultiplier - 1f) / 1f); // 1.0-2.0 range
        riskSlider.value = normalizedRisk;
        
        // Color-code risk level
        var riskColor = GetRiskFactorColor(riskFactor.RiskMultiplier);
        riskMultiplierText.color = riskColor;
        
        // Show modifiable status
        modifiableIcon.color = riskFactor.IsModifiable ? Color.green : Color.gray;
    }
    
    private Color GetRiskFactorColor(float riskMultiplier)
    {
        if (riskMultiplier >= 2.0f) return Color.red;
        if (riskMultiplier >= 1.5f) return new Color(1f, 0.5f, 0f); // Orange
        if (riskMultiplier >= 1.2f) return Color.yellow;
        return Color.green;
    }
}