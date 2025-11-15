using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLManager.Systems.Training;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLManager.UI.Training
{
    /// <summary>
    /// Main training analytics dashboard showing team-wide training metrics, performance trends, and insights
    /// </summary>
    public class TrainingAnalyticsDashboard : MonoBehaviour
    {
        [Header("Dashboard Control")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI dashboardTitleText;
        [SerializeField] private Button refreshButton;
        
        [Header("Navigation Tabs")]
        [SerializeField] private Button overviewTabButton;
        [SerializeField] private Button loadManagementTabButton;
        [SerializeField] private Button performanceTabButton;
        [SerializeField] private Button injuryTabButton;
        [SerializeField] private Button playersTabButton;
        [SerializeField] private GameObject[] tabPanels; // 0=Overview, 1=LoadMgmt, 2=Performance, 3=Injury, 4=Players
        
        [Header("Overview Tab")]
        [SerializeField] private TextMeshProUGUI totalTrainingSessionsText;
        [SerializeField] private TextMeshProUGUI averageWeeklyLoadText;
        [SerializeField] private TextMeshProUGUI teamConditionAverageText;
        [SerializeField] private Slider teamConditionSlider;
        [SerializeField] private TextMeshProUGUI highRiskPlayersText;
        [SerializeField] private Transform weeklyTrendsContainer;
        [SerializeField] private GameObject weeklyTrendPrefab;
        
        [Header("Load Management Tab")]
        [SerializeField] private LoadManagementAnalyticsUI loadManagementUI;
        [SerializeField] private Transform loadDistributionContainer;
        [SerializeField] private GameObject loadDistributionBarPrefab;
        [SerializeField] private TextMeshProUGUI loadRecommendationsText;
        
        [Header("Performance Tab")]
        [SerializeField] private PerformanceAnalyticsUI performanceUI;
        [SerializeField] private Transform performanceTrendsContainer;
        [SerializeField] private GameObject performanceTrendPrefab;
        [SerializeField] private TextMeshProUGUI effectivenessScoreText;
        
        [Header("Injury Tab")]
        [SerializeField] private InjuryAnalyticsUI injuryUI;
        [SerializeField] private Transform injuryRiskContainer;
        [SerializeField] private GameObject injuryRiskPlayerPrefab;
        [SerializeField] private TextMeshProUGUI injuryPreventionTipsText;
        
        [Header("Players Tab")]
        [SerializeField] private PlayerTrainingAnalyticsUI playerAnalyticsUI;
        [SerializeField] private Transform playerAnalyticsContainer;
        [SerializeField] private GameObject playerAnalyticsCardPrefab;
        [SerializeField] private Dropdown playerFilterDropdown;
        
        [Header("System Dependencies")]
        [SerializeField] private WeeklyTrainingScheduleManager scheduleManager;
        [SerializeField] private TrainingFatigueIntegrationManager fatigueManager;
        [SerializeField] private DailyTrainingSessionExecutor sessionExecutor;
        
        // Analytics data
        private TrainingAnalyticsData currentAnalytics;
        private List<Player> teamPlayers;
        private int activeTabIndex = 0;
        private DateTime analyticsStartDate = DateTime.Now.AddDays(-30); // Default: last 30 days
        
        // Events
        // public event System.Action<int> OnPlayerSelected; // TODO: Implement player selection event handler
        // public event System.Action<TrainingRecommendation> OnRecommendationGenerated; // TODO: Implement recommendation event handler
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            SetupEventListeners();
            SetupTabs();
            FindSystemDependencies();
            
            // Load team players
            LoadTeamPlayers();
            
            // Generate initial analytics
            RefreshAnalytics();
        }
        
        private void SetupEventListeners()
        {
            closeButton?.onClick.AddListener(CloseDashboard);
            refreshButton?.onClick.AddListener(RefreshAnalytics);
            
            overviewTabButton?.onClick.AddListener(() => SwitchTab(0));
            loadManagementTabButton?.onClick.AddListener(() => SwitchTab(1));
            performanceTabButton?.onClick.AddListener(() => SwitchTab(2));
            injuryTabButton?.onClick.AddListener(() => SwitchTab(3));
            playersTabButton?.onClick.AddListener(() => SwitchTab(4));
            
            playerFilterDropdown?.onValueChanged.AddListener(OnPlayerFilterChanged);
        }
        
        private void SetupTabs()
        {
            SwitchTab(0); // Start with overview
        }
        
        private void FindSystemDependencies()
        {
            if (scheduleManager == null)
                scheduleManager = FindFirstObjectByType<WeeklyTrainingScheduleManager>();
                
            if (fatigueManager == null)
                fatigueManager = FindFirstObjectByType<TrainingFatigueIntegrationManager>();
                
            if (sessionExecutor == null)
                sessionExecutor = FindFirstObjectByType<DailyTrainingSessionExecutor>();
        }
        
        /// <summary>
        /// Show the training analytics dashboard
        /// </summary>
        public void ShowDashboard()
        {
            RefreshAnalytics();
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Refresh all analytics data and update displays
        /// </summary>
        public void RefreshAnalytics()
        {
            GenerateAnalyticsData();
            RefreshCurrentTab();
            UpdateDashboardHeader();
        }
        
        private void GenerateAnalyticsData()
        {
            currentAnalytics = new TrainingAnalyticsData
            {
                AnalysisPeriodStart = analyticsStartDate,
                AnalysisPeriodEnd = DateTime.Now,
                TeamPlayers = teamPlayers
            };
            
            // Collect data from various systems
            CollectTrainingSessionData();
            CollectLoadManagementData();
            CollectPerformanceData();
            CollectInjuryData();
            CalculateDerivedMetrics();
            
            Debug.Log($"[TrainingAnalytics] Generated analytics for {teamPlayers?.Count ?? 0} players over {(DateTime.Now - analyticsStartDate).Days} days");
        }
        
        private void CollectTrainingSessionData()
        {
            if (scheduleManager == null) return;
            
            var analytics = currentAnalytics;
            analytics.TotalTrainingSessions = 0;
            analytics.WeeklySessionCounts = new List<WeeklySessionCount>();
            
            // Collect session data from the schedule manager
            var currentSchedule = scheduleManager.GetCurrentSchedule();
            if (currentSchedule != null)
            {
                analytics.TotalTrainingSessions = currentSchedule.DailySessions.Count(s => s.Status == TrainingSessionStatus.Completed);
                
                // Calculate weekly trends
                var weeklyData = CalculateWeeklyTrends(currentSchedule);
                analytics.WeeklySessionCounts = weeklyData;
            }
            
            // Calculate average weekly load
            analytics.AverageWeeklyLoad = CalculateAverageWeeklyLoad();
        }
        
        private void CollectLoadManagementData()
        {
            if (fatigueManager == null || teamPlayers == null) return;
            
            var analytics = currentAnalytics;
            analytics.PlayerLoadStates = new List<PlayerLoadAnalytics>();
            
            foreach (var player in teamPlayers)
            {
                var fatigueStatus = fatigueManager.GetPlayerFatigueStatus(int.Parse(player.Id));
                var playerAnalytics = new PlayerLoadAnalytics
                {
                    PlayerId = int.Parse(player.Id),
                    PlayerName = player.Name,
                    CurrentLoad = fatigueStatus.DailyLoadAccumulated,
                    FatigueLevel = fatigueStatus.CurrentFatigueLevel,
                    Condition = fatigueStatus.CurrentCondition,
                    RiskLevel = DetermineFatigueRiskLevel(fatigueStatus),
                    RecommendedAction = DetermineRecommendedAction(fatigueStatus)
                };
                
                analytics.PlayerLoadStates.Add(playerAnalytics);
            }
            
            // Calculate high-risk players count
            analytics.HighRiskPlayersCount = analytics.PlayerLoadStates.Count(p => p.RiskLevel >= FatigueRiskLevel.High);
        }
        
        private void CollectPerformanceData()
        {
            if (teamPlayers == null) return;
            
            var analytics = currentAnalytics;
            analytics.PerformanceMetrics = new TeamPerformanceMetrics
            {
                AverageCondition = teamPlayers.Average(p => p.Stamina),
                TrainingEffectivenessScore = CalculateTrainingEffectiveness(),
                DevelopmentProgressScore = CalculateDevelopmentProgress(),
                InjuryPreventionScore = CalculateInjuryPreventionScore()
            };
            
            // Calculate performance trends
            analytics.PerformanceTrends = CalculatePerformanceTrends();
        }
        
        private void CollectInjuryData()
        {
            var analytics = currentAnalytics;
            analytics.InjuryMetrics = new InjuryAnalyticsMetrics
            {
                TotalInjuries = 0, // TODO: Collect from injury system
                InjuryRate = 0f,
                AverageRecoveryTime = TimeSpan.Zero,
                InjuryPrevention = new List<InjuryPreventionTip>()
            };
            
            // Generate injury prevention tips based on current team state
            GenerateInjuryPreventionTips();
        }
        
        private void CalculateDerivedMetrics()
        {
            if (currentAnalytics.PlayerLoadStates?.Any() != true) return;
            
            // Calculate team averages and distributions
            var playerStates = currentAnalytics.PlayerLoadStates;
            
            currentAnalytics.TeamAverageLoad = playerStates.Average(p => p.CurrentLoad);
            currentAnalytics.LoadDistribution = CalculateLoadDistribution(playerStates);
            currentAnalytics.ConditionDistribution = CalculateConditionDistribution(playerStates);
            
            // Generate recommendations
            currentAnalytics.TrainingRecommendations = GenerateTrainingRecommendations();
        }
        
        private void SwitchTab(int tabIndex)
        {
            activeTabIndex = tabIndex;
            
            // Update tab button states
            UpdateTabButtonStates();
            
            // Show/hide tab panels
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null)
                    tabPanels[i].SetActive(i == tabIndex);
            }
            
            // Refresh current tab content
            RefreshCurrentTab();
        }
        
        private void UpdateTabButtonStates()
        {
            var buttons = new[] { overviewTabButton, loadManagementTabButton, performanceTabButton, injuryTabButton, playersTabButton };
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    var colors = buttons[i].colors;
                    colors.normalColor = i == activeTabIndex ? Color.cyan : Color.white;
                    buttons[i].colors = colors;
                }
            }
        }
        
        private void RefreshCurrentTab()
        {
            if (currentAnalytics == null) return;
            
            switch (activeTabIndex)
            {
                case 0: RefreshOverviewTab(); break;
                case 1: RefreshLoadManagementTab(); break;
                case 2: RefreshPerformanceTab(); break;
                case 3: RefreshInjuryTab(); break;
                case 4: RefreshPlayersTab(); break;
            }
        }
        
        private void RefreshOverviewTab()
        {
            totalTrainingSessionsText.text = $"Total Sessions: {currentAnalytics.TotalTrainingSessions}";
            averageWeeklyLoadText.text = $"Avg Weekly Load: {currentAnalytics.AverageWeeklyLoad:F1}";
            
            var avgCondition = currentAnalytics.PerformanceMetrics?.AverageCondition ?? 0f;
            teamConditionAverageText.text = $"Team Condition: {avgCondition:F1}%";
            teamConditionSlider.value = avgCondition / 100f;
            
            // Color-code condition
            var conditionColor = GetConditionColor(avgCondition);
            teamConditionAverageText.color = conditionColor;
            
            highRiskPlayersText.text = $"High Risk Players: {currentAnalytics.HighRiskPlayersCount}";
            
            // Display weekly trends
            DisplayWeeklyTrends();
        }
        
        private void RefreshLoadManagementTab()
        {
            loadManagementUI?.DisplayLoadAnalytics(currentAnalytics);
            DisplayLoadDistribution();
            DisplayLoadRecommendations();
        }
        
        private void RefreshPerformanceTab()
        {
            performanceUI?.DisplayPerformanceAnalytics(currentAnalytics);
            DisplayPerformanceTrends();
            
            var effectivenessScore = currentAnalytics.PerformanceMetrics?.TrainingEffectivenessScore ?? 0f;
            effectivenessScoreText.text = $"Training Effectiveness: {effectivenessScore:F1}%";
        }
        
        private void RefreshInjuryTab()
        {
            injuryUI?.DisplayInjuryAnalytics(currentAnalytics);
            DisplayInjuryRiskPlayers();
            DisplayInjuryPreventionTips();
        }
        
        private void RefreshPlayersTab()
        {
            playerAnalyticsUI?.DisplayPlayerAnalytics(currentAnalytics);
            DisplayPlayerAnalyticsCards();
        }
        
        private void UpdateDashboardHeader()
        {
            var dateRange = $"{analyticsStartDate:MMM dd} - {DateTime.Now:MMM dd}";
            dashboardTitleText.text = $"Training Analytics Dashboard ({dateRange})";
        }
        
        #region Helper Methods
        
        private void LoadTeamPlayers()
        {
            // TODO: Load from team manager or roster system
            // For now, create mock data
            teamPlayers = new List<Player>();
            Debug.Log("[TrainingAnalytics] Loaded team players for analytics");
        }
        
        private float CalculateAverageWeeklyLoad()
        {
            // Calculate from training schedule data
            return 75.5f; // Mock value
        }
        
        private List<WeeklySessionCount> CalculateWeeklyTrends(WeeklyTrainingSchedule schedule)
        {
            // Calculate weekly training trends
            return new List<WeeklySessionCount>(); // Mock for now
        }
        
        private float CalculateTrainingEffectiveness()
        {
            // Calculate based on player development vs training load
            return 82.3f; // Mock value
        }
        
        private float CalculateDevelopmentProgress()
        {
            // Calculate team development progress
            return 78.9f; // Mock value
        }
        
        private float CalculateInjuryPreventionScore()
        {
            // Calculate based on load management and injury risk
            return 85.2f; // Mock value
        }
        
        private List<PerformanceTrend> CalculatePerformanceTrends()
        {
            // Calculate performance trends over time
            return new List<PerformanceTrend>(); // Mock for now
        }
        
        private void GenerateInjuryPreventionTips()
        {
            // Generate contextual injury prevention advice
        }
        
        private Dictionary<string, int> CalculateLoadDistribution(List<PlayerLoadAnalytics> playerStates)
        {
            // Calculate load distribution across players
            return new Dictionary<string, int>
            {
                ["Low (0-25)"] = playerStates.Count(p => p.CurrentLoad <= 25),
                ["Moderate (26-50)"] = playerStates.Count(p => p.CurrentLoad > 25 && p.CurrentLoad <= 50),
                ["High (51-75)"] = playerStates.Count(p => p.CurrentLoad > 50 && p.CurrentLoad <= 75),
                ["Very High (76+)"] = playerStates.Count(p => p.CurrentLoad > 75)
            };
        }
        
        private Dictionary<string, int> CalculateConditionDistribution(List<PlayerLoadAnalytics> playerStates)
        {
            // Calculate condition distribution
            return new Dictionary<string, int>
            {
                ["Excellent (90+)"] = playerStates.Count(p => p.Condition >= 90),
                ["Good (70-89)"] = playerStates.Count(p => p.Condition >= 70 && p.Condition < 90),
                ["Fair (50-69)"] = playerStates.Count(p => p.Condition >= 50 && p.Condition < 70),
                ["Poor (<50)"] = playerStates.Count(p => p.Condition < 50)
            };
        }
        
        private List<TrainingRecommendation> GenerateTrainingRecommendations()
        {
            // Generate smart training recommendations based on analytics
            return new List<TrainingRecommendation>();
        }
        
        private Color GetConditionColor(float condition)
        {
            if (condition >= 80) return Color.green;
            if (condition >= 60) return Color.yellow;
            return Color.red;
        }
        
        #endregion
        
        #region Display Methods
        
        private void DisplayWeeklyTrends()
        {
            // Clear existing trends
            foreach (Transform child in weeklyTrendsContainer)
                Destroy(child.gameObject);
            
            if (currentAnalytics.WeeklySessionCounts?.Any() != true) return;
            
            foreach (var weekData in currentAnalytics.WeeklySessionCounts)
            {
                var trendUI = Instantiate(weeklyTrendPrefab, weeklyTrendsContainer);
                var component = trendUI.GetComponent<WeeklyTrendUI>();
                component?.DisplayWeeklyTrend(weekData);
            }
        }
        
        private void DisplayLoadDistribution()
        {
            if (currentAnalytics.LoadDistribution == null) return;
            
            // Clear existing bars
            foreach (Transform child in loadDistributionContainer)
                Destroy(child.gameObject);
            
            foreach (var distribution in currentAnalytics.LoadDistribution)
            {
                var barUI = Instantiate(loadDistributionBarPrefab, loadDistributionContainer);
                var component = barUI.GetComponent<LoadDistributionBarUI>();
                component?.DisplayDistribution(distribution.Key, distribution.Value);
            }
        }
        
        private void DisplayLoadRecommendations()
        {
            if (currentAnalytics.TrainingRecommendations?.Any() != true)
            {
                loadRecommendationsText.text = "No specific recommendations at this time.";
                return;
            }
            
            var recommendations = string.Join("\n", currentAnalytics.TrainingRecommendations.Select(r => $"• {r.Message}"));
            loadRecommendationsText.text = recommendations;
        }
        
        private void DisplayPerformanceTrends()
        {
            // Display performance trend charts
        }
        
        private void DisplayInjuryRiskPlayers()
        {
            if (currentAnalytics.PlayerLoadStates?.Any() != true) return;
            
            // Clear existing risk players
            foreach (Transform child in injuryRiskContainer)
                Destroy(child.gameObject);
            
            var highRiskPlayers = currentAnalytics.PlayerLoadStates.Where(p => p.RiskLevel >= FatigueRiskLevel.High);
            
            foreach (var player in highRiskPlayers)
            {
                var riskUI = Instantiate(injuryRiskPlayerPrefab, injuryRiskContainer);
                var component = riskUI.GetComponent<InjuryRiskPlayerUI>();
                component?.DisplayRiskPlayer(player);
            }
        }
        
        private void DisplayInjuryPreventionTips()
        {
            // Display contextual injury prevention advice
            injuryPreventionTipsText.text = "Focus on recovery protocols for high-load players.\nMonitor fatigue levels before intensive sessions.\nEnsure adequate rest between high-intensity days.";
        }
        
        private void DisplayPlayerAnalyticsCards()
        {
            if (teamPlayers?.Any() != true) return;
            
            // Clear existing cards
            foreach (Transform child in playerAnalyticsContainer)
                Destroy(child.gameObject);
            
            foreach (var player in teamPlayers)
            {
                var loadAnalytics = currentAnalytics.PlayerLoadStates?.FirstOrDefault(p => p.PlayerId == int.Parse(player.Id));
                var cardData = new PlayerAnalyticsCardData
                {
                    Player = player,
                    LoadAnalytics = loadAnalytics ?? CreateDefaultPlayerLoadAnalytics(player),
                    DevelopmentScore = 75f, // TODO: Calculate from development system
                    PerformanceTrend = "Steady",
                    RecentTrainingEffectiveness = 70f,
                    InjuryRiskScore = loadAnalytics?.RiskLevel ?? FatigueRiskLevel.Low,
                    RecommendedActions = new List<string> { "Continue current program" }
                };
                
                var cardUI = Instantiate(playerAnalyticsCardPrefab, playerAnalyticsContainer);
                var component = cardUI.GetComponent<PlayerAnalyticsCardUI>();
                component?.DisplayPlayerAnalytics(cardData);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnPlayerFilterChanged(int filterIndex)
        {
            // Apply player filtering
            RefreshPlayersTab();
        }
        
        private void CloseDashboard()
        {
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Helper Methods
        
        private FatigueRiskLevel DetermineFatigueRiskLevel(PlayerFatigueStatus status)
        {
            // Determine risk level based on fatigue and load
            if (status.CurrentFatigueLevel > 80f || status.WeeklyLoadAccumulated > 90f)
                return FatigueRiskLevel.Critical;
            if (status.CurrentFatigueLevel > 60f || status.WeeklyLoadAccumulated > 75f)
                return FatigueRiskLevel.High;
            if (status.CurrentFatigueLevel > 40f || status.WeeklyLoadAccumulated > 60f)
                return FatigueRiskLevel.Moderate;
            return FatigueRiskLevel.Low;
        }
        
        private string DetermineRecommendedAction(PlayerFatigueStatus status)
        {
            if (status.CurrentFatigueLevel > 80f)
                return "Rest required";
            if (status.CurrentFatigueLevel > 60f)
                return "Reduce training load";
            if (status.CurrentFatigueLevel > 40f)
                return "Monitor closely";
            return "Continue as planned";
        }
        
        private PlayerLoadAnalytics CreateDefaultPlayerLoadAnalytics(Player player)
        {
            return new PlayerLoadAnalytics
            {
                PlayerId = int.Parse(player.Id),
                PlayerName = player.Name,
                CurrentLoad = 50f,
                FatigueLevel = 30f,
                Condition = player.Stamina,
                RiskLevel = FatigueRiskLevel.Low,
                RecommendedAction = "Continue as planned"
            };
        }
        
        #endregion
    }
}
