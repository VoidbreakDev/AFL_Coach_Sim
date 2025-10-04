using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.UI.Training;

namespace AFLManager.UI.Training
{
    /// <summary>
    /// Weekly trend UI component for displaying training trends over time
    /// </summary>
    public class WeeklyTrendUI : MonoBehaviour
    {
        [Header("Week Display")]
        [SerializeField] private TextMeshProUGUI weekLabelText;
        [SerializeField] private TextMeshProUGUI sessionCountText;
        [SerializeField] private TextMeshProUGUI completionRateText;
        [SerializeField] private TextMeshProUGUI averageLoadText;
        
        [Header("Visual Elements")]
        [SerializeField] private Slider completionBarSlider;
        [SerializeField] private Image completionBarFill;
        [SerializeField] private Image trendIndicator;
        [SerializeField] private Image backgroundImage;
        
        public void DisplayWeeklyTrend(WeeklySessionCount weekData)
        {
            weekLabelText.text = weekData.WeekLabel;
            sessionCountText.text = $"{weekData.SessionsCompleted}/{weekData.SessionsScheduled}";
            completionRateText.text = $"{weekData.CompletionRate * 100:F0}%";
            averageLoadText.text = $"Load: {weekData.AverageLoad:F1}";
            
            // Update completion bar
            completionBarSlider.value = weekData.CompletionRate;
            
            // Color-code completion rate
            var completionColor = GetCompletionColor(weekData.CompletionRate);
            completionBarFill.color = completionColor;
            completionRateText.color = completionColor;
            
            // Update trend indicator
            UpdateTrendIndicator(weekData);
            
            // Update background based on performance
            UpdateBackground(weekData);
        }
        
        private void UpdateTrendIndicator(WeeklySessionCount weekData)
        {
            // Simple trend indication based on completion rate
            if (weekData.CompletionRate >= 0.9f)
            {
                trendIndicator.color = Color.green;
            }
            else if (weekData.CompletionRate >= 0.7f)
            {
                trendIndicator.color = Color.yellow;
            }
            else
            {
                trendIndicator.color = Color.red;
            }
        }
        
        private void UpdateBackground(WeeklySessionCount weekData)
        {
            var baseColor = weekData.CompletionRate >= 0.8f ? Color.green : 
                           weekData.CompletionRate >= 0.6f ? Color.yellow : Color.red;
            
            backgroundImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.1f);
        }
        
        private Color GetCompletionColor(float completionRate)
        {
            if (completionRate >= 0.9f) return Color.green;
            if (completionRate >= 0.7f) return Color.yellow;
            if (completionRate >= 0.5f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }
    }
    
    /// <summary>
    /// Training analytics chart point for visualizing data trends
    /// </summary>
    public class TrainingAnalyticsChartPoint : MonoBehaviour
    {
        [Header("Point Display")]
        [SerializeField] private Image pointImage;
        [SerializeField] private TextMeshProUGUI valueLabel;
        [SerializeField] private TextMeshProUGUI dateLabel;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        
        [Header("Point Styling")]
        [SerializeField] private float pointSize = 10f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.cyan;
        
        private AnalyticsDataPoint dataPoint;
        private bool isHighlighted = false;
        
        public void SetDataPoint(AnalyticsDataPoint point)
        {
            dataPoint = point;
            UpdateDisplay();
        }
        
        public void SetPosition(Vector3 position)
        {
            transform.localPosition = position;
        }
        
        public void SetHighlight(bool highlighted)
        {
            isHighlighted = highlighted;
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (dataPoint == null) return;
            
            // Update point appearance
            pointImage.color = isHighlighted ? highlightColor : GetPointColor(dataPoint.Value);
            
            var rectTransform = pointImage.GetComponent<RectTransform>();
            rectTransform.sizeDelta = Vector2.one * (isHighlighted ? pointSize * 1.2f : pointSize);
            
            // Update labels
            if (valueLabel != null)
                valueLabel.text = dataPoint.Value.ToString("F1");
                
            if (dateLabel != null)
                dateLabel.text = dataPoint.Date.ToString("MM/dd");
            
            // Update tooltip
            if (tooltipText != null)
                tooltipText.text = $"{dataPoint.Label}: {dataPoint.Value:F1}\n{dataPoint.Date:MMM dd, yyyy}";
        }
        
        private Color GetPointColor(float value)
        {
            if (value >= 80) return Color.green;
            if (value >= 60) return Color.yellow;
            if (value >= 40) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }
        
        private void OnMouseEnter()
        {
            ShowTooltip();
        }
        
        private void OnMouseExit()
        {
            HideTooltip();
        }
        
        private void ShowTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(true);
        }
        
        private void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Progress indicator with animated fill and status colors
    /// </summary>
    public class AnalyticsProgressIndicator : MonoBehaviour
    {
        [Header("Progress Display")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Animation")]
        [SerializeField] private bool animateProgress = true;
        [SerializeField] private float animationDuration = 1f;
        
        private float targetProgress = 0f;
        private float currentProgress = 0f;
        private bool isAnimating = false;
        
        public void SetProgress(float progress, string status = "")
        {
            targetProgress = Mathf.Clamp01(progress);
            
            if (animateProgress && gameObject.activeInHierarchy)
            {
                StartProgressAnimation();
            }
            else
            {
                SetProgressImmediate(targetProgress);
            }
            
            if (statusText != null && !string.IsNullOrEmpty(status))
                statusText.text = status;
        }
        
        public void SetProgressImmediate(float progress)
        {
            currentProgress = Mathf.Clamp01(progress);
            UpdateProgressDisplay();
        }
        
        private void StartProgressAnimation()
        {
            if (isAnimating) return;
            
            isAnimating = true;
            StartCoroutine(AnimateProgressCoroutine());
        }
        
        private System.Collections.IEnumerator AnimateProgressCoroutine()
        {
            float startProgress = currentProgress;
            float elapsedTime = 0f;
            
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / animationDuration;
                
                // Smooth animation curve
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                currentProgress = Mathf.Lerp(startProgress, targetProgress, easedTime);
                
                UpdateProgressDisplay();
                yield return null;
            }
            
            currentProgress = targetProgress;
            UpdateProgressDisplay();
            isAnimating = false;
        }
        
        private void UpdateProgressDisplay()
        {
            if (progressSlider != null)
                progressSlider.value = currentProgress;
            
            if (progressText != null)
                progressText.text = $"{currentProgress * 100:F0}%";
            
            // Color-code the fill
            if (fillImage != null)
                fillImage.color = GetProgressColor(currentProgress);
            
            // Color-code the text
            if (progressText != null)
                progressText.color = GetProgressColor(currentProgress);
        }
        
        private Color GetProgressColor(float progress)
        {
            if (progress >= 0.8f) return Color.green;
            if (progress >= 0.6f) return Color.yellow;
            if (progress >= 0.4f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }
    }
    
    /// <summary>
    /// Status badge UI component for displaying quick status information
    /// </summary>
    public class AnalyticsStatusBadge : MonoBehaviour
    {
        [Header("Badge Display")]
        [SerializeField] private Image badgeBackground;
        [SerializeField] private TextMeshProUGUI badgeText;
        [SerializeField] private Image statusIcon;
        
        [Header("Badge Styling")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private bool enablePulse = false;
        
        private StatusBadgeType currentStatus = StatusBadgeType.Normal;
        private Color originalColor;
        
        private void Start()
        {
            if (badgeBackground != null)
                originalColor = badgeBackground.color;
        }
        
        private void Update()
        {
            if (enablePulse && currentStatus == StatusBadgeType.Alert)
            {
                AnimatePulse();
            }
        }
        
        public void SetStatus(StatusBadgeType status, string text)
        {
            currentStatus = status;
            
            if (badgeText != null)
                badgeText.text = text;
            
            var statusColor = GetStatusColor(status);
            
            if (badgeBackground != null)
                badgeBackground.color = statusColor;
            
            if (statusIcon != null)
                statusIcon.color = statusColor;
            
            enablePulse = (status == StatusBadgeType.Alert || status == StatusBadgeType.Critical);
        }
        
        private void AnimatePulse()
        {
            if (badgeBackground == null) return;
            
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            var baseColor = GetStatusColor(currentStatus);
            var pulseColor = Color.Lerp(baseColor, Color.white, pulse * 0.3f);
            badgeBackground.color = pulseColor;
        }
        
        private Color GetStatusColor(StatusBadgeType status)
        {
            return status switch
            {
                StatusBadgeType.Success => Color.green,
                StatusBadgeType.Warning => Color.yellow,
                StatusBadgeType.Alert => new Color(1f, 0.5f, 0f), // Orange
                StatusBadgeType.Critical => Color.red,
                StatusBadgeType.Info => Color.cyan,
                _ => originalColor
            };
        }
    }
    
    /// <summary>
    /// Metric comparison UI for showing before/after or comparison values
    /// </summary>
    public class MetricComparisonUI : MonoBehaviour
    {
        [Header("Comparison Display")]
        [SerializeField] private TextMeshProUGUI metricNameText;
        [SerializeField] private TextMeshProUGUI currentValueText;
        [SerializeField] private TextMeshProUGUI previousValueText;
        [SerializeField] private TextMeshProUGUI changeText;
        [SerializeField] private Image changeIndicator;
        
        [Header("Visual Elements")]
        [SerializeField] private GameObject improvementIcon;
        [SerializeField] private GameObject declineIcon;
        [SerializeField] private GameObject stableIcon;
        
        public void DisplayComparison(string metricName, float currentValue, float previousValue, string unit = "")
        {
            metricNameText.text = metricName;
            currentValueText.text = $"{currentValue:F1}{unit}";
            previousValueText.text = $"(was {previousValue:F1}{unit})";
            
            float change = currentValue - previousValue;
            float changePercent = previousValue != 0 ? (change / previousValue) * 100 : 0;
            
            // Update change display
            string changeSign = change > 0 ? "+" : "";
            changeText.text = $"{changeSign}{change:F1}{unit} ({changeSign}{changePercent:F1}%)";
            
            // Update visual indicators
            UpdateChangeIndicators(change);
        }
        
        private void UpdateChangeIndicators(float change)
        {
            Color changeColor;
            
            if (change > 0)
            {
                changeColor = Color.green;
                ShowIcon(improvementIcon);
            }
            else if (change < 0)
            {
                changeColor = Color.red;
                ShowIcon(declineIcon);
            }
            else
            {
                changeColor = Color.white;
                ShowIcon(stableIcon);
            }
            
            changeText.color = changeColor;
            if (changeIndicator != null)
                changeIndicator.color = changeColor;
        }
        
        private void ShowIcon(GameObject iconToShow)
        {
            improvementIcon?.SetActive(iconToShow == improvementIcon);
            declineIcon?.SetActive(iconToShow == declineIcon);
            stableIcon?.SetActive(iconToShow == stableIcon);
        }
    }
    
    #region Data Classes
    
    /// <summary>
    /// Data point for analytics charts
    /// </summary>
    [System.Serializable]
    public class AnalyticsDataPoint
    {
        public System.DateTime Date { get; set; }
        public float Value { get; set; }
        public string Label { get; set; }
        public string Category { get; set; }
        
        public AnalyticsDataPoint(System.DateTime date, float value, string label)
        {
            Date = date;
            Value = value;
            Label = label;
        }
    }
    
    /// <summary>
    /// Status badge types for different alert levels
    /// </summary>
    public enum StatusBadgeType
    {
        Normal,
        Success,
        Info,
        Warning,
        Alert,
        Critical
    }
    
    #endregion
}

/// <summary>
/// Simple loading spinner for analytics data
/// </summary>
public class AnalyticsLoadingSpinner : MonoBehaviour
{
    [Header("Spinner Settings")]
    [SerializeField] private RectTransform spinnerIcon;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private float rotationSpeed = 180f;
    
    private bool isSpinning = false;
    
    public void ShowLoading(string message = "Loading analytics...")
    {
        isSpinning = true;
        gameObject.SetActive(true);
        
        if (loadingText != null)
            loadingText.text = message;
    }
    
    public void HideLoading()
    {
        isSpinning = false;
        gameObject.SetActive(false);
    }
    
    private void Update()
    {
        if (isSpinning && spinnerIcon != null)
        {
            spinnerIcon.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
}

/// <summary>
/// Analytics summary card for key metrics
/// </summary>
public class AnalyticsSummaryCard : MonoBehaviour
{
    [Header("Card Display")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image iconImage;
    
    [Header("Card Styling")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.cyan;
    
    public void SetCardData(string title, string value, string subtitle = "", Sprite icon = null)
    {
        titleText.text = title;
        valueText.text = value;
        subtitleText.text = subtitle;
        
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
    }
    
    public void SetCardStatus(AnalyticsCardStatus status)
    {
        var statusColor = status switch
        {
            AnalyticsCardStatus.Excellent => Color.green,
            AnalyticsCardStatus.Good => Color.cyan,
            AnalyticsCardStatus.Warning => Color.yellow,
            AnalyticsCardStatus.Poor => Color.red,
            _ => normalColor
        };
        
        if (cardBackground != null)
            cardBackground.color = new Color(statusColor.r, statusColor.g, statusColor.b, 0.2f);
        
        if (valueText != null)
            valueText.color = statusColor;
    }
    
    public void SetHighlight(bool highlighted)
    {
        if (cardBackground != null)
        {
            cardBackground.color = highlighted ? highlightColor : normalColor;
        }
    }
}

/// <summary>
/// Analytics card status levels
/// </summary>
public enum AnalyticsCardStatus
{
    Normal,
    Excellent,
    Good,
    Warning,
    Poor
}