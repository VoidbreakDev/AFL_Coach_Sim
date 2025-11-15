using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Systems.Coach;
using CoachSkills = AFLCoachSim.Core.Domain.Entities.CoachSkills;

namespace AFLManager.UI.Coach
{
    /// <summary>
    /// UI Manager for the Coach Development Points system
    /// </summary>
    public class CoachDevelopmentUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject developmentPointsPanel;
        [SerializeField] private TextMeshProUGUI currentPointsText;
        [SerializeField] private TextMeshProUGUI coachNameText;
        [SerializeField] private TextMeshProUGUI overallRatingText;
        
        [Header("Skill List")]
        [SerializeField] private Transform skillListParent;
        [SerializeField] private GameObject skillUpgradeItemPrefab;
        [SerializeField] private ScrollRect skillScrollRect;
        
        [Header("Skill Categories")]
        [SerializeField] private Toggle tacticalSkillsToggle;
        [SerializeField] private Toggle playerManagementToggle;
        [SerializeField] private Toggle communicationToggle;
        [SerializeField] private Toggle analyticsToggle;
        [SerializeField] private Toggle specializationToggle;
        [SerializeField] private Toggle showAllToggle;
        
        [Header("Filters")]
        [SerializeField] private Toggle upgradeableOnlyToggle;
        [SerializeField] private Toggle backstorySkillsToggle;
        [SerializeField] private Dropdown sortByDropdown;
        
        [Header("Confirmation")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Feedback")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDisplayTime = 3f;

        // System references
        private CoachDevelopmentPoints _developmentSystem;
        private CoachSkillsManager _skillsManager;
        
        // UI State
        private List<SkillUpgradeUIItem> _skillItems = new();
        private string _pendingUpgradeSkill;
        private SkillCategory _currentFilter = SkillCategory.All;
        
        // Cached data
        private List<CoachDevelopmentPoints.SkillUpgradeInfo> _allSkills = new();

        #region Unity Lifecycle

        private void Awake()
        {
            SetupEventListeners();
        }

        private void Start()
        {
            FindSystemReferences();
            InitializeUI();
        }

        private void OnDestroy()
        {
            RemoveEventListeners();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Show the development points panel
        /// </summary>
        public void ShowDevelopmentPanel()
        {
            developmentPointsPanel.SetActive(true);
            RefreshUI();
        }

        /// <summary>
        /// Hide the development points panel
        /// </summary>
        public void HideDevelopmentPanel()
        {
            developmentPointsPanel.SetActive(false);
        }

        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        public void RefreshUI()
        {
            if (_developmentSystem == null || _skillsManager == null) return;

            UpdateHeader();
            UpdateSkillsList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Find references to required systems
        /// </summary>
        private void FindSystemReferences()
        {
            _developmentSystem = FindFirstObjectByType<CoachDevelopmentPoints>();
            _skillsManager = FindFirstObjectByType<CoachSkillsManager>();
            
            if (_developmentSystem == null)
            {
                Debug.LogError("CoachDevelopmentPoints system not found!");
            }
            
            if (_skillsManager == null)
            {
                Debug.LogError("CoachSkillsManager system not found!");
            }
        }

        /// <summary>
        /// Setup UI event listeners
        /// </summary>
        private void SetupEventListeners()
        {
            // Category toggles
            if (tacticalSkillsToggle) tacticalSkillsToggle.onValueChanged.AddListener(OnCategoryToggleChanged);
            if (playerManagementToggle) playerManagementToggle.onValueChanged.AddListener(OnCategoryToggleChanged);
            if (communicationToggle) communicationToggle.onValueChanged.AddListener(OnCategoryToggleChanged);
            if (analyticsToggle) analyticsToggle.onValueChanged.AddListener(OnCategoryToggleChanged);
            if (specializationToggle) specializationToggle.onValueChanged.AddListener(OnCategoryToggleChanged);
            if (showAllToggle) showAllToggle.onValueChanged.AddListener(OnCategoryToggleChanged);
            
            // Filters
            if (upgradeableOnlyToggle) upgradeableOnlyToggle.onValueChanged.AddListener(OnFilterChanged);
            if (backstorySkillsToggle) backstorySkillsToggle.onValueChanged.AddListener(OnFilterChanged);
            if (sortByDropdown) sortByDropdown.onValueChanged.AddListener(OnSortChanged);
            
            // Confirmation buttons
            if (confirmButton) confirmButton.onClick.AddListener(ConfirmUpgrade);
            if (cancelButton) cancelButton.onClick.AddListener(CancelUpgrade);
        }

        /// <summary>
        /// Remove UI event listeners
        /// </summary>
        private void RemoveEventListeners()
        {
            if (_developmentSystem != null)
            {
                _developmentSystem.OnDevelopmentPointsChanged -= OnPointsChanged;
                _developmentSystem.OnSkillUpgraded -= OnSkillUpgraded;
                _developmentSystem.OnDevelopmentPointsEarned -= OnPointsEarned;
            }
        }

        /// <summary>
        /// Initialize UI components
        /// </summary>
        private void InitializeUI()
        {
            if (_developmentSystem != null)
            {
                _developmentSystem.OnDevelopmentPointsChanged += OnPointsChanged;
                _developmentSystem.OnSkillUpgraded += OnSkillUpgraded;
                _developmentSystem.OnDevelopmentPointsEarned += OnPointsEarned;
            }
            
            // Hide panels initially
            if (confirmationPanel) confirmationPanel.SetActive(false);
            if (feedbackPanel) feedbackPanel.SetActive(false);
            
            // Setup sort dropdown
            SetupSortDropdown();
            
            RefreshUI();
        }

        /// <summary>
        /// Setup the sort dropdown options
        /// </summary>
        private void SetupSortDropdown()
        {
            if (sortByDropdown == null) return;
            
            sortByDropdown.ClearOptions();
            sortByDropdown.AddOptions(new List<string>
            {
                "Skill Name",
                "Current Level",
                "Upgrade Cost",
                "Skill Tier",
                "Backstory Skills First"
            });
        }

        /// <summary>
        /// Update header information
        /// </summary>
        private void UpdateHeader()
        {
            // Development points
            if (currentPointsText)
            {
                currentPointsText.text = $"Development Points: {_developmentSystem.GetCurrentPoints()}";
            }
            
            // Coach info
            var coachSkills = _skillsManager.GetCoachSkills();
            if (coachSkills != null)
            {
                if (coachNameText) coachNameText.text = "Coach Development"; // Could get actual name from profile
                if (overallRatingText) overallRatingText.text = $"Overall Rating: {coachSkills.GetOverallRating()}";
            }
        }

        /// <summary>
        /// Update the skills list display
        /// </summary>
        private void UpdateSkillsList()
        {
            // Get all skills data
            _allSkills = _developmentSystem.GetUpgradeableSkills();
            
            // Apply filters
            var filteredSkills = ApplyFilters(_allSkills);
            
            // Apply sorting
            filteredSkills = ApplySorting(filteredSkills);
            
            // Clear existing items
            ClearSkillItems();
            
            // Create new items
            foreach (var skillInfo in filteredSkills)
            {
                CreateSkillItem(skillInfo);
            }
        }

        /// <summary>
        /// Apply current filters to skill list
        /// </summary>
        private List<CoachDevelopmentPoints.SkillUpgradeInfo> ApplyFilters(List<CoachDevelopmentPoints.SkillUpgradeInfo> skills)
        {
            var filtered = skills.ToList();
            
            // Category filter
            if (_currentFilter != SkillCategory.All)
            {
                filtered = filtered.Where(s => GetSkillCategory(s.SkillName) == _currentFilter).ToList();
            }
            
            // Upgradeable only filter
            if (upgradeableOnlyToggle && upgradeableOnlyToggle.isOn)
            {
                filtered = filtered.Where(s => s.CanUpgrade).ToList();
            }
            
            // Backstory skills filter
            if (backstorySkillsToggle && backstorySkillsToggle.isOn)
            {
                filtered = filtered.Where(s => s.IsBackstorySkill).ToList();
            }
            
            return filtered;
        }

        /// <summary>
        /// Apply current sorting to skill list
        /// </summary>
        private List<CoachDevelopmentPoints.SkillUpgradeInfo> ApplySorting(List<CoachDevelopmentPoints.SkillUpgradeInfo> skills)
        {
            if (sortByDropdown == null) return skills;
            
            return sortByDropdown.value switch
            {
                0 => skills.OrderBy(s => s.SkillName).ToList(), // Name
                1 => skills.OrderByDescending(s => s.CurrentLevel).ToList(), // Level
                2 => skills.OrderBy(s => s.UpgradeCost).ToList(), // Cost
                3 => skills.OrderByDescending(s => s.CurrentLevel).ToList(), // Tier (by level)
                4 => skills.OrderBy(s => s.IsBackstorySkill ? 0 : 1).ThenBy(s => s.SkillName).ToList(), // Backstory first
                _ => skills
            };
        }

        /// <summary>
        /// Get skill category for filtering
        /// </summary>
        private SkillCategory GetSkillCategory(string skillName)
        {
            return skillName switch
            {
                nameof(CoachSkills.TacticalKnowledge) or 
                nameof(CoachSkills.TacticalAdaptation) or 
                nameof(CoachSkills.SetPieceExpertise) or 
                nameof(CoachSkills.OppositionAnalysis) => SkillCategory.Tactical,
                
                nameof(CoachSkills.PlayerEvaluation) or 
                nameof(CoachSkills.PlayerDevelopment) or 
                nameof(CoachSkills.Motivation) or 
                nameof(CoachSkills.PlayerWelfare) => SkillCategory.PlayerManagement,
                
                nameof(CoachSkills.Communication) or 
                nameof(CoachSkills.Leadership) or 
                nameof(CoachSkills.MediaManagement) or 
                nameof(CoachSkills.ConflictResolution) => SkillCategory.Communication,
                
                nameof(CoachSkills.DataAnalysis) or 
                nameof(CoachSkills.Recruitment) or 
                nameof(CoachSkills.Innovation) or 
                nameof(CoachSkills.Adaptability) => SkillCategory.Analytics,
                
                nameof(CoachSkills.YouthDevelopment) or 
                nameof(CoachSkills.Networking) or 
                nameof(CoachSkills.GameDayComposure) or 
                nameof(CoachSkills.CommunityRelations) => SkillCategory.Specialization,
                
                _ => SkillCategory.All
            };
        }

        /// <summary>
        /// Clear all skill UI items
        /// </summary>
        private void ClearSkillItems()
        {
            foreach (var item in _skillItems)
            {
                if (item != null && item.gameObject != null)
                {
                    DestroyImmediate(item.gameObject);
                }
            }
            _skillItems.Clear();
        }

        /// <summary>
        /// Create a skill item UI element
        /// </summary>
        private void CreateSkillItem(CoachDevelopmentPoints.SkillUpgradeInfo skillInfo)
        {
            if (skillUpgradeItemPrefab == null || skillListParent == null) return;
            
            var itemObj = Instantiate(skillUpgradeItemPrefab, skillListParent);
            var itemComponent = itemObj.GetComponent<SkillUpgradeUIItem>();
            
            if (itemComponent != null)
            {
                itemComponent.Initialize(skillInfo, OnSkillUpgradeRequested);
                _skillItems.Add(itemComponent);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle category toggle changes
        /// </summary>
        private void OnCategoryToggleChanged(bool value)
        {
            if (!value) return; // Only respond to toggle on
            
            // Determine which category was selected
            if (tacticalSkillsToggle && tacticalSkillsToggle.isOn) _currentFilter = SkillCategory.Tactical;
            else if (playerManagementToggle && playerManagementToggle.isOn) _currentFilter = SkillCategory.PlayerManagement;
            else if (communicationToggle && communicationToggle.isOn) _currentFilter = SkillCategory.Communication;
            else if (analyticsToggle && analyticsToggle.isOn) _currentFilter = SkillCategory.Analytics;
            else if (specializationToggle && specializationToggle.isOn) _currentFilter = SkillCategory.Specialization;
            else _currentFilter = SkillCategory.All;
            
            UpdateSkillsList();
        }

        /// <summary>
        /// Handle filter toggle changes
        /// </summary>
        private void OnFilterChanged(bool value)
        {
            UpdateSkillsList();
        }

        /// <summary>
        /// Handle sort dropdown changes
        /// </summary>
        private void OnSortChanged(int value)
        {
            UpdateSkillsList();
        }

        /// <summary>
        /// Handle skill upgrade request from UI item
        /// </summary>
        private void OnSkillUpgradeRequested(string skillName)
        {
            _pendingUpgradeSkill = skillName;
            ShowUpgradeConfirmation(skillName);
        }

        /// <summary>
        /// Handle development points changed
        /// </summary>
        private void OnPointsChanged(int oldPoints, int newPoints)
        {
            if (currentPointsText)
            {
                currentPointsText.text = $"Development Points: {newPoints}";
            }
        }

        /// <summary>
        /// Handle skill upgraded
        /// </summary>
        private void OnSkillUpgraded(string skillName, int oldLevel, int newLevel)
        {
            ShowFeedback($"{skillName} upgraded from {oldLevel} to {newLevel}!", Color.green);
            RefreshUI();
        }

        /// <summary>
        /// Handle development points earned
        /// </summary>
        private void OnPointsEarned(int points)
        {
            ShowFeedback($"Earned {points} Development Points!", Color.yellow);
        }

        #endregion

        #region Confirmation System

        /// <summary>
        /// Show upgrade confirmation dialog
        /// </summary>
        private void ShowUpgradeConfirmation(string skillName)
        {
            if (confirmationPanel == null || confirmationText == null) return;
            
            var cost = _developmentSystem.GetUpgradeCost(skillName);
            confirmationText.text = $"Upgrade {skillName} for {cost} Development Points?";
            confirmationPanel.SetActive(true);
        }

        /// <summary>
        /// Confirm the pending upgrade
        /// </summary>
        private void ConfirmUpgrade()
        {
            if (string.IsNullOrEmpty(_pendingUpgradeSkill)) return;
            
            var success = _developmentSystem.TryUpgradeSkill(_pendingUpgradeSkill, out string errorMessage);
            
            if (!success)
            {
                ShowFeedback($"Upgrade failed: {errorMessage}", Color.red);
            }
            
            HideUpgradeConfirmation();
        }

        /// <summary>
        /// Cancel the pending upgrade
        /// </summary>
        private void CancelUpgrade()
        {
            HideUpgradeConfirmation();
        }

        /// <summary>
        /// Hide upgrade confirmation dialog
        /// </summary>
        private void HideUpgradeConfirmation()
        {
            _pendingUpgradeSkill = "";
            if (confirmationPanel) confirmationPanel.SetActive(false);
        }

        #endregion

        #region Feedback System

        /// <summary>
        /// Show feedback message to user
        /// </summary>
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackPanel == null || feedbackText == null) return;
            
            feedbackText.text = message;
            feedbackText.color = color;
            feedbackPanel.SetActive(true);
            
            // Hide after delay
            Invoke(nameof(HideFeedback), feedbackDisplayTime);
        }

        /// <summary>
        /// Hide feedback message
        /// </summary>
        private void HideFeedback()
        {
            if (feedbackPanel) feedbackPanel.SetActive(false);
        }

        #endregion

        #region Data Classes

        private enum SkillCategory
        {
            All,
            Tactical,
            PlayerManagement,
            Communication,
            Analytics,
            Specialization
        }

        #endregion
    }

    /// <summary>
    /// Individual skill upgrade item component
    /// </summary>
    public class SkillUpgradeUIItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI currentLevelText;
        [SerializeField] private TextMeshProUGUI skillTierText;
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image backstoryIndicator;
        [SerializeField] private Slider progressSlider;

        private CoachDevelopmentPoints.SkillUpgradeInfo _skillInfo;
        private System.Action<string> _onUpgradeRequested;

        /// <summary>
        /// Initialize the skill item with data
        /// </summary>
        public void Initialize(CoachDevelopmentPoints.SkillUpgradeInfo skillInfo, System.Action<string> onUpgradeRequested)
        {
            _skillInfo = skillInfo;
            _onUpgradeRequested = onUpgradeRequested;

            UpdateDisplay();
            SetupButton();
        }

        /// <summary>
        /// Update the display elements
        /// </summary>
        private void UpdateDisplay()
        {
            if (skillNameText) skillNameText.text = FormatSkillName(_skillInfo.SkillName);
            if (currentLevelText) currentLevelText.text = _skillInfo.CurrentLevel.ToString();
            if (skillTierText) skillTierText.text = _skillInfo.SkillTier;
            
            if (upgradeCostText)
            {
                upgradeCostText.text = _skillInfo.CanUpgrade ? $"{_skillInfo.UpgradeCost} pts" : "Max";
                upgradeCostText.color = _skillInfo.CanUpgrade ? Color.white : Color.gray;
            }

            if (backstoryIndicator)
            {
                backstoryIndicator.gameObject.SetActive(_skillInfo.IsBackstorySkill);
            }

            if (progressSlider)
            {
                // Show progress to next tier
                var currentTierMin = GetTierMinLevel(_skillInfo.SkillTier);
                var nextTierMin = _skillInfo.NextTierAt;
                var progress = (float)(_skillInfo.CurrentLevel - currentTierMin) / (nextTierMin - currentTierMin);
                progressSlider.value = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Setup the upgrade button
        /// </summary>
        private void SetupButton()
        {
            if (upgradeButton == null) return;

            upgradeButton.interactable = _skillInfo.CanUpgrade;
            upgradeButton.onClick.RemoveAllListeners();
            
            if (_skillInfo.CanUpgrade)
            {
                upgradeButton.onClick.AddListener(() => _onUpgradeRequested?.Invoke(_skillInfo.SkillName));
            }
            else
            {
                // Show tooltip or reason why can't upgrade
                var button = upgradeButton.GetComponent<Button>();
                // Could add tooltip component here
            }
        }

        /// <summary>
        /// Format skill name for display
        /// </summary>
        private string FormatSkillName(string skillName)
        {
            // Convert camelCase to Title Case with spaces
            return System.Text.RegularExpressions.Regex.Replace(skillName, "([A-Z])", " $1").Trim();
        }

        /// <summary>
        /// Get minimum level for a tier
        /// </summary>
        private int GetTierMinLevel(string tier)
        {
            return tier switch
            {
                "Elite" => 90,
                "Expert" => 80,
                "Proficient" => 70,
                "Competent" => 60,
                "Average" => 40,
                _ => 1
            };
        }
    }
}