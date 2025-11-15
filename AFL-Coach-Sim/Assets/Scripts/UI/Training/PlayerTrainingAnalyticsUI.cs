using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.UI.Training;
using AFLManager.Models;
using System.Collections.Generic;

namespace AFLManager.UI.Training
{
    /// <summary>
    /// Player training analytics UI showing individual player analytics cards with training performance, 
    /// load management, and development metrics
    /// </summary>
    public class PlayerTrainingAnalyticsUI : MonoBehaviour
    {
        [Header("Player Selection")]
        [SerializeField] private Dropdown playerFilterDropdown;
        [SerializeField] private TextMeshProUGUI selectedPlayerText;
        [SerializeField] private Button viewAllButton;
        [SerializeField] private Button sortButton;
        
        [Header("Player Cards Container")]
        [SerializeField] private Transform playerCardsContainer;
        [SerializeField] private GameObject playerAnalyticsCardPrefab;
        [SerializeField] private ScrollRect playerCardsScrollRect;
        
        [Header("Summary Statistics")]
        [SerializeField] private TextMeshProUGUI totalPlayersText;
        [SerializeField] private TextMeshProUGUI averageLoadText;
        [SerializeField] private TextMeshProUGUI averageConditionText;
        [SerializeField] private TextMeshProUGUI topPerformersText;
        
        [Header("Filter Options")]
        [SerializeField] private Toggle highRiskOnlyToggle;
        [SerializeField] private Toggle developmentFocusToggle;
        [SerializeField] private Toggle injuredOnlyToggle;
        [SerializeField] private Dropdown positionFilterDropdown;
        
        [Header("Sorting Options")]
        [SerializeField] private GameObject sortingPanel;
        [SerializeField] private Dropdown sortByDropdown;
        [SerializeField] private Toggle sortAscendingToggle;
        
        private TrainingAnalyticsData currentAnalytics;
        private List<PlayerAnalyticsCardData> playerCardData;
        private PlayerAnalyticsSortOption currentSortOption = PlayerAnalyticsSortOption.Name;
        private bool sortAscending = true;
        private PlayerAnalyticsFilter currentFilter = new PlayerAnalyticsFilter();
        
        /// <summary>
        /// Display player analytics
        /// </summary>
        public void DisplayPlayerAnalytics(TrainingAnalyticsData analyticsData)
        {
            if (analyticsData == null)
            {
                Debug.LogWarning("[PlayerTrainingAnalytics] No analytics data provided");
                return;
            }
            
            currentAnalytics = analyticsData;
            
            PreparePlayerCardData();
            UpdateSummaryStatistics();
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
            UpdatePlayerFilterDropdown();
        }
        
        private void Start()
        {
            SetupEventListeners();
            InitializeFilterOptions();
        }
        
        private void SetupEventListeners()
        {
            playerFilterDropdown?.onValueChanged.AddListener(OnPlayerFilterChanged);
            viewAllButton?.onClick.AddListener(OnViewAllClicked);
            sortButton?.onClick.AddListener(OnSortButtonClicked);
            
            highRiskOnlyToggle?.onValueChanged.AddListener(OnHighRiskFilterChanged);
            developmentFocusToggle?.onValueChanged.AddListener(OnDevelopmentFilterChanged);
            injuredOnlyToggle?.onValueChanged.AddListener(OnInjuredFilterChanged);
            positionFilterDropdown?.onValueChanged.AddListener(OnPositionFilterChanged);
            
            sortByDropdown?.onValueChanged.AddListener(OnSortByChanged);
            sortAscendingToggle?.onValueChanged.AddListener(OnSortOrderChanged);
        }
        
        private void InitializeFilterOptions()
        {
            // Initialize position filter dropdown
            if (positionFilterDropdown != null)
            {
                positionFilterDropdown.ClearOptions();
                var positionOptions = new List<string> { "All Positions", "Forwards", "Midfielders", "Defenders", "Rucks" };
                positionFilterDropdown.AddOptions(positionOptions);
            }
            
            // Initialize sort options
            if (sortByDropdown != null)
            {
                sortByDropdown.ClearOptions();
                var sortOptions = new List<string> { "Name", "Load", "Condition", "Risk Level", "Development" };
                sortByDropdown.AddOptions(sortOptions);
            }
        }
        
        private void PreparePlayerCardData()
        {
            playerCardData = new List<PlayerAnalyticsCardData>();
            
            if (currentAnalytics.TeamPlayers?.Any() != true) return;
            
            foreach (var player in currentAnalytics.TeamPlayers)
            {
                var loadAnalytics = currentAnalytics.PlayerLoadStates?.FirstOrDefault(p => p.PlayerId == int.Parse(player.Id));
                
                var cardData = new PlayerAnalyticsCardData
                {
                    Player = player,
                    LoadAnalytics = loadAnalytics ?? CreateDefaultLoadAnalytics(player),
                    DevelopmentScore = CalculatePlayerDevelopmentScore(player),
                    PerformanceTrend = CalculatePlayerPerformanceTrend(player),
                    RecentTrainingEffectiveness = CalculateTrainingEffectiveness(player),
                    InjuryRiskScore = loadAnalytics?.RiskLevel ?? FatigueRiskLevel.Low,
                    RecommendedActions = GeneratePlayerRecommendations(player, loadAnalytics)
                };
                
                playerCardData.Add(cardData);
            }
        }
        
        private void UpdateSummaryStatistics()
        {
            if (!playerCardData.Any())
            {
                totalPlayersText.text = "Total Players: 0";
                return;
            }
            
            var totalPlayers = playerCardData.Count;
            var averageLoad = playerCardData.Average(p => p.LoadAnalytics.CurrentLoad);
            var averageCondition = playerCardData.Average(p => p.LoadAnalytics.Condition);
            var topPerformers = playerCardData.Count(p => p.DevelopmentScore >= 80);
            
            totalPlayersText.text = $"Total Players: {totalPlayers}";
            averageLoadText.text = $"Avg Load: {averageLoad:F1}";
            averageConditionText.text = $"Avg Condition: {averageCondition:F1}%";
            topPerformersText.text = $"Top Performers: {topPerformers}";
            
            // Color-code statistics
            averageLoadText.color = GetLoadColor(averageLoad);
            averageConditionText.color = GetConditionColor(averageCondition);
            topPerformersText.color = topPerformers > totalPlayers * 0.3f ? Color.green : Color.yellow;
        }
        
        private void ApplyFiltersAndSorting()
        {
            var filteredData = ApplyFilters(playerCardData);
            var sortedData = ApplySorting(filteredData);
            playerCardData = sortedData;
        }
        
        private List<PlayerAnalyticsCardData> ApplyFilters(List<PlayerAnalyticsCardData> data)
        {
            var filtered = data.AsEnumerable();
            
            // High risk filter
            if (currentFilter.HighRiskOnly)
            {
                filtered = filtered.Where(p => p.InjuryRiskScore >= FatigueRiskLevel.High);
            }
            
            // Development focus filter
            if (currentFilter.DevelopmentFocus)
            {
                filtered = filtered.Where(p => p.DevelopmentScore < 70); // Players who need development
            }
            
            // Injured only filter
            if (currentFilter.InjuredOnly)
            {
                filtered = filtered.Where(p => p.Player.Stamina < 70); // Assuming low condition = injured
            }
            
            // Position filter
            if (!string.IsNullOrEmpty(currentFilter.Position) && currentFilter.Position != "All Positions")
            {
                filtered = filtered.Where(p => GetPlayerPositionGroup(p.Player.Role.ToString()) == currentFilter.Position);
            }
            
            return filtered.ToList();
        }
        
        private List<PlayerAnalyticsCardData> ApplySorting(List<PlayerAnalyticsCardData> data)
        {
            return currentSortOption switch
            {
                PlayerAnalyticsSortOption.Name => sortAscending ? 
                    data.OrderBy(p => p.Player.Name).ToList() : 
                    data.OrderByDescending(p => p.Player.Name).ToList(),
                    
                PlayerAnalyticsSortOption.Load => sortAscending ? 
                    data.OrderBy(p => p.LoadAnalytics.CurrentLoad).ToList() : 
                    data.OrderByDescending(p => p.LoadAnalytics.CurrentLoad).ToList(),
                    
                PlayerAnalyticsSortOption.Condition => sortAscending ? 
                    data.OrderBy(p => p.LoadAnalytics.Condition).ToList() : 
                    data.OrderByDescending(p => p.LoadAnalytics.Condition).ToList(),
                    
                PlayerAnalyticsSortOption.RiskLevel => sortAscending ? 
                    data.OrderBy(p => (int)p.InjuryRiskScore).ToList() : 
                    data.OrderByDescending(p => (int)p.InjuryRiskScore).ToList(),
                    
                PlayerAnalyticsSortOption.Development => sortAscending ? 
                    data.OrderBy(p => p.DevelopmentScore).ToList() : 
                    data.OrderByDescending(p => p.DevelopmentScore).ToList(),
                    
                _ => data
            };
        }
        
        private void DisplayPlayerCards()
        {
            // Clear existing cards
            foreach (Transform child in playerCardsContainer)
                Destroy(child.gameObject);
            
            if (!playerCardData.Any())
            {
                DisplayNoPlayersMessage();
                return;
            }
            
            foreach (var cardData in playerCardData)
            {
                var cardUI = Instantiate(playerAnalyticsCardPrefab, playerCardsContainer);
                var component = cardUI.GetComponent<PlayerAnalyticsCardUI>();
                component?.DisplayPlayerAnalytics(cardData);
            }
            
            // Reset scroll to top
            if (playerCardsScrollRect != null)
            {
                playerCardsScrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        private void DisplayNoPlayersMessage()
        {
            var messageObj = new GameObject("No Players Message");
            messageObj.transform.SetParent(playerCardsContainer);
            
            var textComponent = messageObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "No players match the current filter criteria.";
            textComponent.color = Color.gray;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 16;
            
            var rectTransform = messageObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 50);
        }
        
        private void UpdatePlayerFilterDropdown()
        {
            if (playerFilterDropdown == null) return;
            
            playerFilterDropdown.ClearOptions();
            
            var playerOptions = new List<string> { "All Players" };
            playerOptions.AddRange(playerCardData.Select(p => p.Player.Name));
            
            playerFilterDropdown.AddOptions(playerOptions);
        }
        
        #region Helper Methods
        
        private PlayerLoadAnalytics CreateDefaultLoadAnalytics(Player player)
        {
            return new PlayerLoadAnalytics
            {
                PlayerId = int.Parse(player.Id),
                PlayerName = player.Name,
                CurrentLoad = Random.Range(20f, 80f),
                FatigueLevel = Random.Range(10f, 60f),
                Condition = player.Stamina,
                RiskLevel = FatigueRiskLevel.Low,
                RecommendedAction = "Continue current training"
            };
        }
        
        private float CalculatePlayerDevelopmentScore(Player player)
        {
            // Mock development score calculation
            var baseScore = player.Stats.GetAverage();
            var potentialFactor = (player.Age <= 25) ? 1.2f : (player.Age <= 30) ? 1.0f : 0.8f;
            return Mathf.Clamp(baseScore * potentialFactor, 0f, 100f);
        }
        
        private string CalculatePlayerPerformanceTrend(Player player)
        {
            var trends = new[] { "↗ Improving", "↘ Declining", "→ Stable" };
            return trends[Random.Range(0, trends.Length)];
        }
        
        private float CalculateTrainingEffectiveness(Player player)
        {
            return Random.Range(60f, 95f); // Mock effectiveness
        }
        
        private List<string> GeneratePlayerRecommendations(Player player, PlayerLoadAnalytics loadAnalytics)
        {
            var recommendations = new List<string>();
            
            if (loadAnalytics != null)
            {
                if (loadAnalytics.CurrentLoad > 75)
                    recommendations.Add("Reduce training load");
                    
                if (loadAnalytics.Condition < 60)
                    recommendations.Add("Focus on recovery");
                    
                if (loadAnalytics.RiskLevel >= FatigueRiskLevel.High)
                    recommendations.Add("Monitor injury risk");
            }
            
            if (player.Age <= 23)
                recommendations.Add("Development focus");
            else if (player.Age >= 32)
                recommendations.Add("Load management priority");
            
            if (!recommendations.Any())
                recommendations.Add("Continue current program");
                
            return recommendations;
        }
        
        private string GetPlayerPositionGroup(string role)
        {
            // Simplified position grouping
            var forwards = new[] { "Full Forward", "Forward Pocket", "Half Forward" };
            var midfielders = new[] { "Centre", "Wing", "Rover" };
            var defenders = new[] { "Full Back", "Back Pocket", "Half Back" };
            
            if (forwards.Any(f => role.Contains(f))) return "Forwards";
            if (midfielders.Any(m => role.Contains(m))) return "Midfielders";
            if (defenders.Any(d => role.Contains(d))) return "Defenders";
            if (role.Contains("Ruck")) return "Rucks";
            
            return "Other";
        }
        
        private Color GetLoadColor(float load)
        {
            if (load >= 80) return Color.red;
            if (load >= 60) return Color.yellow;
            return Color.green;
        }
        
        private Color GetConditionColor(float condition)
        {
            if (condition >= 80) return Color.green;
            if (condition >= 60) return Color.yellow;
            return Color.red;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnPlayerFilterChanged(int index)
        {
            if (index == 0) // "All Players"
            {
                selectedPlayerText.text = "All Players";
                // Show all cards
            }
            else
            {
                var selectedPlayer = playerCardData[index - 1];
                selectedPlayerText.text = selectedPlayer.Player.Name;
                // Filter to show only selected player
            }
        }
        
        private void OnViewAllClicked()
        {
            // Reset all filters
            currentFilter = new PlayerAnalyticsFilter();
            highRiskOnlyToggle.isOn = false;
            developmentFocusToggle.isOn = false;
            injuredOnlyToggle.isOn = false;
            positionFilterDropdown.value = 0;
            
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
        }
        
        private void OnSortButtonClicked()
        {
            sortingPanel.SetActive(!sortingPanel.activeSelf);
        }
        
        private void OnHighRiskFilterChanged(bool value)
        {
            currentFilter.HighRiskOnly = value;
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
        }
        
        private void OnDevelopmentFilterChanged(bool value)
        {
            currentFilter.DevelopmentFocus = value;
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
        }
        
        private void OnInjuredFilterChanged(bool value)
        {
            currentFilter.InjuredOnly = value;
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
        }
        
        private void OnPositionFilterChanged(int index)
        {
            var positions = new[] { "All Positions", "Forwards", "Midfielders", "Defenders", "Rucks" };
            currentFilter.Position = positions[index];
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
        }
        
        private void OnSortByChanged(int index)
        {
            currentSortOption = (PlayerAnalyticsSortOption)index;
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
        }
        
        private void OnSortOrderChanged(bool ascending)
        {
            sortAscending = ascending;
            ApplyFiltersAndSorting();
            DisplayPlayerCards();
        }
        
        #endregion
    }
    
    #region Data Classes
    
    /// <summary>
    /// Player analytics card data
    /// </summary>
    public class PlayerAnalyticsCardData
    {
        public Player Player { get; set; }
        public PlayerLoadAnalytics LoadAnalytics { get; set; }
        public float DevelopmentScore { get; set; }
        public string PerformanceTrend { get; set; }
        public float RecentTrainingEffectiveness { get; set; }
        public FatigueRiskLevel InjuryRiskScore { get; set; }
        public List<string> RecommendedActions { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Player analytics filter
    /// </summary>
    public class PlayerAnalyticsFilter
    {
        public bool HighRiskOnly { get; set; } = false;
        public bool DevelopmentFocus { get; set; } = false;
        public bool InjuredOnly { get; set; } = false;
        public string Position { get; set; } = "All Positions";
    }
    
    /// <summary>
    /// Player analytics sort options
    /// </summary>
    public enum PlayerAnalyticsSortOption
    {
        Name = 0,
        Load = 1,
        Condition = 2,
        RiskLevel = 3,
        Development = 4
    }
    
    #endregion
}

/// <summary>
/// Individual player analytics card UI component
/// </summary>
public class PlayerAnalyticsCardUI : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerAgeText;
    [SerializeField] private TextMeshProUGUI playerPositionText;
    [SerializeField] private Image playerPortrait;
    
    [Header("Performance Metrics")]
    [SerializeField] private TextMeshProUGUI loadText;
    [SerializeField] private Slider loadSlider;
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private Slider conditionSlider;
    [SerializeField] private TextMeshProUGUI riskLevelText;
    [SerializeField] private Image riskIndicator;
    
    [Header("Development & Trends")]
    [SerializeField] private TextMeshProUGUI developmentScoreText;
    [SerializeField] private TextMeshProUGUI performanceTrendText;
    [SerializeField] private TextMeshProUGUI effectivenessText;
    [SerializeField] private Slider effectivenessSlider;
    
    [Header("Recommendations")]
    [SerializeField] private Transform recommendationsContainer;
    [SerializeField] private GameObject recommendationTagPrefab;
    
    [Header("Actions")]
    [SerializeField] private Button viewDetailsButton;
    [SerializeField] private Button adjustProgramButton;
    
    private PlayerAnalyticsCardData currentCardData;
    
    public void DisplayPlayerAnalytics(PlayerAnalyticsCardData cardData)
    {
        currentCardData = cardData;
        
        DisplayPlayerInfo();
        DisplayPerformanceMetrics();
        DisplayDevelopmentMetrics();
        DisplayRecommendations();
        SetupActionButtons();
    }
    
    private void DisplayPlayerInfo()
    {
        var player = currentCardData.Player;
        
        playerNameText.text = player.Name;
        playerAgeText.text = $"Age: {player.Age}";
        playerPositionText.text = player.Role.ToString();
        
        // Set player portrait (placeholder)
        // playerPortrait.sprite = GetPlayerPortrait(player);
    }
    
    private void DisplayPerformanceMetrics()
    {
        var loadAnalytics = currentCardData.LoadAnalytics;
        
        // Load metrics
        loadText.text = $"Load: {loadAnalytics.CurrentLoad:F1}";
        loadSlider.value = loadAnalytics.CurrentLoad / 100f;
        loadText.color = GetLoadColor(loadAnalytics.CurrentLoad);
        
        // Condition metrics
        conditionText.text = $"Condition: {loadAnalytics.Condition:F1}%";
        conditionSlider.value = loadAnalytics.Condition / 100f;
        conditionText.color = GetConditionColor(loadAnalytics.Condition);
        
        // Risk level
        riskLevelText.text = loadAnalytics.RiskLevelText;
        riskLevelText.color = loadAnalytics.RiskLevelColor;
        riskIndicator.color = loadAnalytics.RiskLevelColor;
    }
    
    private void DisplayDevelopmentMetrics()
    {
        developmentScoreText.text = $"Development: {currentCardData.DevelopmentScore:F1}";
        developmentScoreText.color = GetDevelopmentColor(currentCardData.DevelopmentScore);
        
        performanceTrendText.text = $"Trend: {currentCardData.PerformanceTrend}";
        performanceTrendText.color = GetTrendColor(currentCardData.PerformanceTrend);
        
        effectivenessText.text = $"Training Effectiveness: {currentCardData.RecentTrainingEffectiveness:F1}%";
        effectivenessSlider.value = currentCardData.RecentTrainingEffectiveness / 100f;
        effectivenessText.color = GetEffectivenessColor(currentCardData.RecentTrainingEffectiveness);
    }
    
    private void DisplayRecommendations()
    {
        // Clear existing recommendations
        foreach (Transform child in recommendationsContainer)
            Destroy(child.gameObject);
        
        foreach (var recommendation in currentCardData.RecommendedActions.Take(3)) // Show max 3
        {
            var recUI = Instantiate(recommendationTagPrefab, recommendationsContainer);
            var textComponent = recUI.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"• {recommendation}";
                textComponent.color = GetRecommendationColor(recommendation);
            }
        }
    }
    
    private void SetupActionButtons()
    {
        viewDetailsButton?.onClick.RemoveAllListeners();
        viewDetailsButton?.onClick.AddListener(() => OnViewDetailsClicked(int.Parse(currentCardData.Player.Id)));
        
        adjustProgramButton?.onClick.RemoveAllListeners();
        adjustProgramButton?.onClick.AddListener(() => OnAdjustProgramClicked(int.Parse(currentCardData.Player.Id)));
    }
    
    #region Helper Methods
    
    private Color GetLoadColor(float load)
    {
        if (load >= 80) return Color.red;
        if (load >= 60) return Color.yellow;
        return Color.green;
    }
    
    private Color GetConditionColor(float condition)
    {
        if (condition >= 80) return Color.green;
        if (condition >= 60) return Color.yellow;
        return Color.red;
    }
    
    private Color GetDevelopmentColor(float score)
    {
        if (score >= 80) return Color.green;
        if (score >= 60) return Color.cyan;
        return Color.yellow;
    }
    
    private Color GetTrendColor(string trend)
    {
        if (trend.Contains("Improving")) return Color.green;
        if (trend.Contains("Declining")) return Color.red;
        return Color.white;
    }
    
    private Color GetEffectivenessColor(float effectiveness)
    {
        if (effectiveness >= 80) return Color.green;
        if (effectiveness >= 65) return Color.yellow;
        return new Color(1f, 0.5f, 0f); // Orange
    }
    
    private Color GetRecommendationColor(string recommendation)
    {
        if (recommendation.Contains("Reduce") || recommendation.Contains("Monitor")) return Color.red;
        if (recommendation.Contains("Focus") || recommendation.Contains("Development")) return Color.yellow;
        return Color.cyan;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnViewDetailsClicked(int playerId)
    {
        Debug.Log($"[PlayerAnalytics] View details for player {playerId}");
        // TODO: Open detailed player analytics view
    }
    
    private void OnAdjustProgramClicked(int playerId)
    {
        Debug.Log($"[PlayerAnalytics] Adjust training program for player {playerId}");
        // TODO: Open training program adjustment interface
    }
    
    #endregion
}