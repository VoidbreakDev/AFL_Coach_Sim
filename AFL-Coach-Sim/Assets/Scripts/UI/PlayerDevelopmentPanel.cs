using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLManager.Systems.Development;
using AFLCoachSim.Core.Development;
using System.Collections.Generic;
using System.Linq;

namespace AFLManager.UI
{
    /// <summary>
    /// Comprehensive player development panel showing specializations, breakthrough events, and progression
    /// </summary>
    public class PlayerDevelopmentPanel : MonoBehaviour
    {
        [Header("Panel Control")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI playerNameHeader;
        [SerializeField] private Image playerPortrait;
        
        [Header("Tab System")]
        [SerializeField] private Button specializationTabButton;
        [SerializeField] private Button eventsTabButton;
        [SerializeField] private Button historyTabButton;
        [SerializeField] private Button planningTabButton;
        [SerializeField] private GameObject[] tabPanels; // 0=Specialization, 1=Events, 2=History, 3=Planning
        
        [Header("Specialization Tab")]
        [SerializeField] private SpecializationTreeUI specializationTree;
        [SerializeField] private TextMeshProUGUI currentSpecializationText;
        [SerializeField] private Slider specializationProgressSlider;
        [SerializeField] private TextMeshProUGUI specializationProgressText;
        [SerializeField] private TextMeshProUGUI careerExperienceText;
        [SerializeField] private TextMeshProUGUI developmentStageText;
        
        [Header("Events Tab")]
        [SerializeField] private BreakthroughEventDisplayUI activeEventDisplay;
        [SerializeField] private Transform eventHistoryContainer;
        [SerializeField] private GameObject eventHistoryEntryPrefab;
        [SerializeField] private TextMeshProUGUI breakthroughReadinessText;
        [SerializeField] private Slider breakthroughReadinessSlider;
        
        [Header("History Tab")]
        [SerializeField] private DevelopmentTimelineUI developmentTimeline;
        [SerializeField] private Transform careerHighlightsContainer;
        [SerializeField] private GameObject careerHighlightPrefab;
        [SerializeField] private TextMeshProUGUI totalDevelopmentText;
        
        [Header("Planning Tab")]
        [SerializeField] private DevelopmentPlannerUI developmentPlanner;
        [SerializeField] private Transform recommendedFocusContainer;
        [SerializeField] private GameObject focusRecommendationPrefab;
        
        [Header("Integration")]
        [SerializeField] private PlayerDevelopmentIntegration developmentIntegration;
        
        private Player currentPlayer;
        private PlayerDevelopmentProfile currentProfile;
        private int activeTabIndex = 0;
        
        private void Start()
        {
            SetupEventListeners();
            SetupTabs();
            
            // Find development integration if not assigned
            if (developmentIntegration == null)
                developmentIntegration = FindObjectOfType<PlayerDevelopmentIntegration>();
        }
        
        private void SetupEventListeners()
        {
            closeButton?.onClick.AddListener(ClosePanel);
            specializationTabButton?.onClick.AddListener(() => SwitchTab(0));
            eventsTabButton?.onClick.AddListener(() => SwitchTab(1));
            historyTabButton?.onClick.AddListener(() => SwitchTab(2));
            planningTabButton?.onClick.AddListener(() => SwitchTab(3));
        }
        
        private void SetupTabs()
        {
            SwitchTab(0); // Start with specialization tab
        }
        
        /// <summary>
        /// Main method to display a player's development information
        /// </summary>
        public void ShowPlayerDevelopment(Player player)
        {
            if (player == null)
            {
                Debug.LogWarning("Cannot show development for null player");
                return;
            }
            
            currentPlayer = player;
            
            // Get development profile from integration
            if (developmentIntegration != null)
            {
                currentProfile = developmentIntegration.GetPlayerProfile(player);
            }
            
            // Update header
            UpdateHeader(player);
            
            // Refresh current tab content
            RefreshCurrentTab();
            
            // Show panel
            gameObject.SetActive(true);
        }
        
        private void UpdateHeader(Player player)
        {
            playerNameHeader.text = $"{player.Name} - Development Profile";
            
            // Set portrait if available
            // playerPortrait.sprite = GetPlayerPortrait(player);
        }
        
        private void SwitchTab(int tabIndex)
        {
            activeTabIndex = tabIndex;
            
            // Update tab button states
            UpdateTabButtonStates();
            
            // Show/hide tab panels
            for (int i = 0; i < tabPanels.Length; i++)
            {
                tabPanels[i].SetActive(i == tabIndex);
            }
            
            // Refresh tab content
            RefreshCurrentTab();
        }
        
        private void UpdateTabButtonStates()
        {
            // Visual feedback for active tab
            var buttons = new[] { specializationTabButton, eventsTabButton, historyTabButton, planningTabButton };
            
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
            if (currentPlayer == null || currentProfile == null) return;
            
            switch (activeTabIndex)
            {
                case 0: RefreshSpecializationTab(); break;
                case 1: RefreshEventsTab(); break;
                case 2: RefreshHistoryTab(); break;
                case 3: RefreshPlanningTab(); break;
            }
        }
        
        #region Specialization Tab
        
        private void RefreshSpecializationTab()
        {
            // Current specialization info
            if (currentProfile.CurrentSpecialization != null)
            {
                var spec = currentProfile.CurrentSpecialization;
                currentSpecializationText.text = $"{spec.Name} (Tier {spec.TierLevel})";
                
                specializationProgressSlider.value = currentProfile.SpecializationProgress / 100f;
                specializationProgressText.text = $"{currentProfile.SpecializationProgress:F1}% Mastery";
                
                // Update specialization tree
                specializationTree?.DisplaySpecializationPath(currentPlayer, currentProfile);
            }
            else
            {
                currentSpecializationText.text = "No Specialization";
                specializationProgressSlider.value = 0f;
                specializationProgressText.text = "0% Mastery";
            }
            
            // Career stats
            careerExperienceText.text = $"Career Experience: {currentProfile.CareerExperience:F0} points";
            developmentStageText.text = $"Development Stage: {currentProfile.DevelopmentStage}";
        }
        
        #endregion
        
        #region Events Tab
        
        private void RefreshEventsTab()
        {
            // Breakthrough readiness
            breakthroughReadinessSlider.value = currentProfile.BreakthroughReadiness / 100f;
            breakthroughReadinessText.text = $"Breakthrough Readiness: {currentProfile.BreakthroughReadiness:F1}%";
            
            // Active breakthrough events (would need to track these in profile)
            // activeEventDisplay?.DisplayActiveEvents(currentProfile.ActiveBreakthroughEvents);
            
            // Event history
            RefreshEventHistory();
        }
        
        private void RefreshEventHistory()
        {
            // Clear existing entries
            foreach (Transform child in eventHistoryContainer)
                Destroy(child.gameObject);
                
            // Get event history from development integration
            // var eventHistory = developmentIntegration?.GetDevelopmentHistory(currentPlayer.Id);
            
            // For now, show placeholder
            CreateEventHistoryEntry("Recent Development", "Steady progress in current specialization", true);
        }
        
        private void CreateEventHistoryEntry(string eventName, string description, bool isPositive)
        {
            var entry = Instantiate(eventHistoryEntryPrefab, eventHistoryContainer);
            var ui = entry.GetComponent<EventHistoryEntryUI>();
            ui?.SetEventInfo(eventName, description, isPositive);
        }
        
        #endregion
        
        #region History Tab
        
        private void RefreshHistoryTab()
        {
            // Development timeline
            developmentTimeline?.DisplayTimeline(currentPlayer, currentProfile);
            
            // Career highlights (career highs)
            RefreshCareerHighlights();
            
            // Total development summary
            var totalGains = currentProfile.CareerHighs.Values.Sum();
            totalDevelopmentText.text = $"Total Career Development: {totalGains:F1} points";
        }
        
        private void RefreshCareerHighlights()
        {
            // Clear existing highlights
            foreach (Transform child in careerHighlightsContainer)
                Destroy(child.gameObject);
                
            // Show top career highs
            var topHighs = currentProfile.CareerHighs
                .OrderByDescending(kvp => kvp.Value)
                .Take(5);
                
            foreach (var high in topHighs)
            {
                CreateCareerHighlightEntry(high.Key, high.Value);
            }
        }
        
        private void CreateCareerHighlightEntry(string attribute, float value)
        {
            var entry = Instantiate(careerHighlightPrefab, careerHighlightsContainer);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            text.text = $"{attribute}: +{value:F1}";
        }
        
        #endregion
        
        #region Planning Tab
        
        private void RefreshPlanningTab()
        {
            // Development planner
            developmentPlanner?.DisplayPlanningOptions(currentPlayer, currentProfile);
            
            // Recommended focus areas
            RefreshRecommendedFocus();
        }
        
        private void RefreshRecommendedFocus()
        {
            // Clear existing recommendations
            foreach (Transform child in recommendedFocusContainer)
                Destroy(child.gameObject);
                
            // Get recommended training focus based on specialization
            var recommendations = GetTrainingRecommendations();
            
            foreach (var recommendation in recommendations)
            {
                CreateFocusRecommendation(recommendation);
            }
        }
        
        private List<string> GetTrainingRecommendations()
        {
            var recommendations = new List<string>();
            
            if (currentProfile.CurrentSpecialization != null)
            {
                var spec = currentProfile.CurrentSpecialization;
                var topAttributes = spec.AttributeWeights
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(3)
                    .Select(kvp => kvp.Key);
                    
                foreach (var attr in topAttributes)
                {
                    recommendations.Add($"Focus on {attr} training");
                }
            }
            else
            {
                recommendations.Add("Establish a specialization path");
                recommendations.Add("Focus on fundamental skills");
            }
            
            return recommendations;
        }
        
        private void CreateFocusRecommendation(string recommendation)
        {
            var entry = Instantiate(focusRecommendationPrefab, recommendedFocusContainer);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            text.text = recommendation;
        }
        
        #endregion
        
        #region Panel Control
        
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
        
        public void OpenPanel()
        {
            gameObject.SetActive(true);
        }
        
        #endregion
        
        #region Context Menu Testing
        
        [ContextMenu("Test with Mock Player")]
        private void TestWithMockPlayer()
        {
            var mockPlayer = new Player
            {
                Name = "Test Development Player",
                Age = 24,
                Role = PlayerRole.Centre,
                Stats = new PlayerStats
                {
                    Kicking = 75,
                    Handballing = 82,
                    Tackling = 68,
                    Speed = 79,
                    Stamina = 85,
                    Knowledge = 71,
                    Playmaking = 73
                }
            };
            
            ShowPlayerDevelopment(mockPlayer);
        }
        
        #endregion
    }
}