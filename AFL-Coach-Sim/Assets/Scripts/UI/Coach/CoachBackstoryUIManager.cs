using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Systems.Coach;
using AFLManager.Systems.Development;
using static AFLManager.Systems.Coach.CoachBackstorySystem;

namespace AFLManager.UI.Coach
{
    /// <summary>
    /// UI Management system for coach backstory selection, progression, and ability usage
    /// </summary>
    public class CoachBackstoryUIManager : MonoBehaviour
    {
        #region UI References

        [Header("Coach Creation UI")]
        [SerializeField] private GameObject coachCreationPanel;
        [SerializeField] private TMP_InputField coachNameInput;
        [SerializeField] private TMP_Dropdown backstoryDropdown;
        [SerializeField] private TextMeshProUGUI backstoryDescriptionText;
        [SerializeField] private TextMeshProUGUI uniqueFeatureText;
        [SerializeField] private GameObject specialtyBonusesContainer;
        [SerializeField] private GameObject specialtyBonusItemPrefab;
        [SerializeField] private Button createCoachButton;
        [SerializeField] private Button cancelButton;

        [Header("Coach Profile UI")]
        [SerializeField] private GameObject coachProfilePanel;
        [SerializeField] private TextMeshProUGUI coachNameText;
        [SerializeField] private TextMeshProUGUI coachAgeText;
        [SerializeField] private TextMeshProUGUI coachExperienceText;
        [SerializeField] private TextMeshProUGUI coachLevelText;
        [SerializeField] private TextMeshProUGUI insightLevelText;
        [SerializeField] private Image coachPortrait;
        [SerializeField] private GameObject specialtyStatsContainer;
        [SerializeField] private GameObject specialtyStatPrefab;

        [Header("Coach Abilities UI")]
        [SerializeField] private GameObject abilitiesPanel;
        [SerializeField] private GameObject availableAbilitiesContainer;
        [SerializeField] private GameObject cooldownAbilitiesContainer;
        [SerializeField] private GameObject abilityItemPrefab;
        [SerializeField] private Button refreshAbilitiesButton;

        [Header("Specialty Progression UI")]
        [SerializeField] private GameObject progressionPanel;
        [SerializeField] private GameObject specialtyProgressContainer;
        [SerializeField] private GameObject specialtyProgressPrefab;
        [SerializeField] private TextMeshProUGUI experiencePointsText;
        [SerializeField] private Slider experienceProgressSlider;
        [SerializeField] private TextMeshProUGUI nextLevelText;

        [Header("Coach Foresight UI")]
        [SerializeField] private GameObject foresightPanel;
        [SerializeField] private TextMeshProUGUI foresightLevelText;
        [SerializeField] private TextMeshProUGUI foresightDescriptionText;
        [SerializeField] private Button assessPlayerButton;
        [SerializeField] private GameObject playerAssessmentResultPanel;
        [SerializeField] private TextMeshProUGUI assessmentResultText;

        #endregion

        #region Private Fields

        private CoachBackstorySystem _backstorySystem;
        private CoachProfile _currentCoach;
        private Dictionary<CoachBackstory, CoachBackstoryTemplate> _backstoryTemplates;
        private Action<CoachProfile> _onCoachCreated;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _backstorySystem = new CoachBackstorySystem();
            InitializeBackstoryTemplates();
            SetupUIEvents();
        }

        private void Start()
        {
            InitializeBackstoryDropdown();
            RefreshUI();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize backstory templates for UI display
        /// </summary>
        private void InitializeBackstoryTemplates()
        {
            _backstoryTemplates = new Dictionary<CoachBackstory, CoachBackstoryTemplate>();
            
            // This would normally be populated from your CoachBackstorySystem
            // For now, we'll create simplified display templates
            foreach (CoachBackstory backstory in Enum.GetValues<CoachBackstory>())
            {
                var template = CreateDisplayTemplate(backstory);
                _backstoryTemplates[backstory] = template;
            }
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupUIEvents()
        {
            if (createCoachButton != null)
                createCoachButton.onClick.AddListener(OnCreateCoachClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
            
            if (backstoryDropdown != null)
                backstoryDropdown.onValueChanged.AddListener(OnBackstorySelectionChanged);
            
            if (refreshAbilitiesButton != null)
                refreshAbilitiesButton.onClick.AddListener(RefreshAbilitiesUI);
            
            if (assessPlayerButton != null)
                assessPlayerButton.onClick.AddListener(OnAssessPlayerClicked);
        }

        /// <summary>
        /// Initialize the backstory dropdown
        /// </summary>
        private void InitializeBackstoryDropdown()
        {
            if (backstoryDropdown == null) return;

            backstoryDropdown.ClearOptions();
            var options = new List<string>();

            foreach (var backstory in _backstoryTemplates.Keys)
            {
                options.Add(GetBackstoryDisplayName(backstory));
            }

            backstoryDropdown.AddOptions(options);
            
            // Set initial selection
            if (options.Count > 0)
            {
                OnBackstorySelectionChanged(0);
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Show the coach creation UI
        /// </summary>
        public void ShowCoachCreation(Action<CoachProfile> onCoachCreated = null)
        {
            _onCoachCreated = onCoachCreated;
            
            if (coachCreationPanel != null)
                coachCreationPanel.SetActive(true);
            
            if (coachProfilePanel != null)
                coachProfilePanel.SetActive(false);
                
            ResetCoachCreationUI();
        }

        /// <summary>
        /// Show the coach profile UI
        /// </summary>
        public void ShowCoachProfile(CoachProfile coach)
        {
            _currentCoach = coach;
            
            if (coachCreationPanel != null)
                coachCreationPanel.SetActive(false);
            
            if (coachProfilePanel != null)
                coachProfilePanel.SetActive(true);
                
            RefreshCoachProfileUI();
        }

        /// <summary>
        /// Show the coach abilities UI
        /// </summary>
        public void ShowCoachAbilities(CoachProfile coach)
        {
            _currentCoach = coach;
            
            if (abilitiesPanel != null)
                abilitiesPanel.SetActive(true);
                
            RefreshAbilitiesUI();
        }

        /// <summary>
        /// Show the coach progression UI
        /// </summary>
        public void ShowCoachProgression(CoachProfile coach)
        {
            _currentCoach = coach;
            
            if (progressionPanel != null)
                progressionPanel.SetActive(true);
                
            RefreshProgressionUI();
        }

        /// <summary>
        /// Show the coach foresight UI
        /// </summary>
        public void ShowCoachForesight(CoachProfile coach)
        {
            _currentCoach = coach;
            
            if (foresightPanel != null)
                foresightPanel.SetActive(true);
                
            RefreshForesightUI();
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Handle create coach button click
        /// </summary>
        private void OnCreateCoachClicked()
        {
            if (string.IsNullOrEmpty(coachNameInput.text))
            {
                ShowMessage("Please enter a coach name.");
                return;
            }

            var selectedBackstory = GetSelectedBackstory();
            var coach = _backstorySystem.CreateCoach(coachNameInput.text, selectedBackstory);
            
            _onCoachCreated?.Invoke(coach);
            
            if (coachCreationPanel != null)
                coachCreationPanel.SetActive(false);
                
            ShowMessage($"Coach {coach.Name} created successfully!");
        }

        /// <summary>
        /// Handle cancel button click
        /// </summary>
        private void OnCancelClicked()
        {
            if (coachCreationPanel != null)
                coachCreationPanel.SetActive(false);
        }

        /// <summary>
        /// Handle backstory selection change
        /// </summary>
        private void OnBackstorySelectionChanged(int selectionIndex)
        {
            if (selectionIndex < 0 || selectionIndex >= _backstoryTemplates.Count)
                return;

            var selectedBackstory = _backstoryTemplates.Keys.ElementAt(selectionIndex);
            var template = _backstoryTemplates[selectedBackstory];
            
            UpdateBackstoryDisplay(template);
        }

        /// <summary>
        /// Handle assess player button click
        /// </summary>
        private void OnAssessPlayerClicked()
        {
            // This would typically open a player selection dialog
            // For now, we'll show a placeholder message
            ShowPlayerAssessmentDialog();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Refresh all UI elements
        /// </summary>
        private void RefreshUI()
        {
            if (_currentCoach != null)
            {
                RefreshCoachProfileUI();
                RefreshAbilitiesUI();
                RefreshProgressionUI();
                RefreshForesightUI();
            }
        }

        /// <summary>
        /// Refresh the coach profile UI
        /// </summary>
        private void RefreshCoachProfileUI()
        {
            if (_currentCoach == null) return;

            if (coachNameText != null)
                coachNameText.text = _currentCoach.Name;
            
            if (coachAgeText != null)
                coachAgeText.text = $"Age: {_currentCoach.Age}";
            
            if (coachExperienceText != null)
                coachExperienceText.text = $"Experience: {_currentCoach.ExperienceYears} years";
            
            if (coachLevelText != null)
                coachLevelText.text = $"Level: {_currentCoach.Level}";
            
            if (insightLevelText != null)
                insightLevelText.text = $"Insight: {_currentCoach.InsightLevel}";
            
            RefreshSpecialtyStats();
        }

        /// <summary>
        /// Refresh the specialty stats display
        /// </summary>
        private void RefreshSpecialtyStats()
        {
            if (specialtyStatsContainer == null || specialtyStatPrefab == null || _currentCoach == null)
                return;

            // Clear existing specialty items
            for (int i = specialtyStatsContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(specialtyStatsContainer.transform.GetChild(i).gameObject);
            }

            // Create specialty stat items
            foreach (var specialty in _currentCoach.SpecialtyLevels)
            {
                var statItem = Instantiate(specialtyStatPrefab, specialtyStatsContainer.transform);
                var statComponent = statItem.GetComponent<SpecialtyStatItem>();
                
                if (statComponent != null)
                {
                    statComponent.Setup(specialty.Key, specialty.Value);
                }
            }
        }

        /// <summary>
        /// Refresh the coach abilities UI
        /// </summary>
        private void RefreshAbilitiesUI()
        {
            if (_currentCoach == null) return;

            RefreshAvailableAbilities();
            RefreshCooldownAbilities();
        }

        /// <summary>
        /// Refresh available abilities
        /// </summary>
        private void RefreshAvailableAbilities()
        {
            if (availableAbilitiesContainer == null || abilityItemPrefab == null)
                return;

            // Clear existing ability items
            for (int i = availableAbilitiesContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(availableAbilitiesContainer.transform.GetChild(i).gameObject);
            }

            // Get available abilities
            var availableAbilities = _backstorySystem.GetAvailableAbilities(_currentCoach.CoachId);

            // Create ability items
            foreach (var ability in availableAbilities)
            {
                var abilityItem = Instantiate(abilityItemPrefab, availableAbilitiesContainer.transform);
                var abilityComponent = abilityItem.GetComponent<CoachAbilityItem>();
                
                if (abilityComponent != null)
                {
                    abilityComponent.Setup(ability, true, OnUseAbility);
                }
            }
        }

        /// <summary>
        /// Refresh abilities on cooldown
        /// </summary>
        private void RefreshCooldownAbilities()
        {
            if (cooldownAbilitiesContainer == null || abilityItemPrefab == null || _currentCoach == null)
                return;

            // Clear existing cooldown items
            for (int i = cooldownAbilitiesContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(cooldownAbilitiesContainer.transform.GetChild(i).gameObject);
            }

            // Create cooldown ability items
            foreach (var ability in _currentCoach.UnlockedAbilities)
            {
                if (_currentCoach.CooldownTimers.ContainsKey(ability.Id) && 
                    DateTime.UtcNow < _currentCoach.CooldownTimers[ability.Id])
                {
                    var abilityItem = Instantiate(abilityItemPrefab, cooldownAbilitiesContainer.transform);
                    var abilityComponent = abilityItem.GetComponent<CoachAbilityItem>();
                    
                    if (abilityComponent != null)
                    {
                        var remainingCooldown = _currentCoach.CooldownTimers[ability.Id] - DateTime.UtcNow;
                        abilityComponent.Setup(ability, false, null, remainingCooldown);
                    }
                }
            }
        }

        /// <summary>
        /// Refresh the progression UI
        /// </summary>
        private void RefreshProgressionUI()
        {
            if (_currentCoach == null) return;

            if (experiencePointsText != null)
                experiencePointsText.text = $"XP: {_currentCoach.ExperiencePoints}";
            
            // Calculate XP for next level
            int xpForNextLevel = CalculateXPForLevel(_currentCoach.Level + 1);
            int currentLevelXP = CalculateXPForLevel(_currentCoach.Level);
            int xpProgress = _currentCoach.ExperiencePoints - currentLevelXP;
            int xpNeeded = xpForNextLevel - currentLevelXP;
            
            if (experienceProgressSlider != null)
            {
                experienceProgressSlider.value = (float)xpProgress / xpNeeded;
            }
            
            if (nextLevelText != null)
            {
                nextLevelText.text = $"Next Level: {xpNeeded - xpProgress} XP";
            }
            
            RefreshSpecialtyProgression();
        }

        /// <summary>
        /// Refresh specialty progression display
        /// </summary>
        private void RefreshSpecialtyProgression()
        {
            if (specialtyProgressContainer == null || specialtyProgressPrefab == null || _currentCoach == null)
                return;

            // Clear existing progress items
            for (int i = specialtyProgressContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(specialtyProgressContainer.transform.GetChild(i).gameObject);
            }

            // Create specialty progress items
            foreach (var specialty in _currentCoach.SpecialtyLevels)
            {
                var progressItem = Instantiate(specialtyProgressPrefab, specialtyProgressContainer.transform);
                var progressComponent = progressItem.GetComponent<SpecialtyProgressItem>();
                
                if (progressComponent != null)
                {
                    progressComponent.Setup(specialty.Key, specialty.Value);
                }
            }
        }

        /// <summary>
        /// Refresh the foresight UI
        /// </summary>
        private void RefreshForesightUI()
        {
            if (_currentCoach == null) return;

            if (foresightLevelText != null)
                foresightLevelText.text = $"Insight Level: {_currentCoach.InsightLevel}";
            
            if (foresightDescriptionText != null)
            {
                foresightDescriptionText.text = GetInsightLevelDescription(_currentCoach.InsightLevel);
            }
        }

        /// <summary>
        /// Update backstory display information
        /// </summary>
        private void UpdateBackstoryDisplay(CoachBackstoryTemplate template)
        {
            if (backstoryDescriptionText != null)
                backstoryDescriptionText.text = template.Description;
            
            if (uniqueFeatureText != null)
                uniqueFeatureText.text = $"Unique Feature: {template.UniqueFeature}";
            
            UpdateSpecialtyBonusesDisplay(template);
        }

        /// <summary>
        /// Update specialty bonuses display
        /// </summary>
        private void UpdateSpecialtyBonusesDisplay(CoachBackstoryTemplate template)
        {
            if (specialtyBonusesContainer == null || specialtyBonusItemPrefab == null)
                return;

            // Clear existing bonus items
            for (int i = specialtyBonusesContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(specialtyBonusesContainer.transform.GetChild(i).gameObject);
            }

            // Create specialty bonus items
            foreach (var bonus in template.SpecialtyBonuses)
            {
                var bonusItem = Instantiate(specialtyBonusItemPrefab, specialtyBonusesContainer.transform);
                var bonusComponent = bonusItem.GetComponent<SpecialtyBonusItem>();
                
                if (bonusComponent != null)
                {
                    bonusComponent.Setup(bonus.Key, bonus.Value);
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Reset the coach creation UI to default state
        /// </summary>
        private void ResetCoachCreationUI()
        {
            if (coachNameInput != null)
                coachNameInput.text = "";
            
            if (backstoryDropdown != null && backstoryDropdown.options.Count > 0)
            {
                backstoryDropdown.value = 0;
                OnBackstorySelectionChanged(0);
            }
        }

        /// <summary>
        /// Get the currently selected backstory
        /// </summary>
        private CoachBackstory GetSelectedBackstory()
        {
            if (backstoryDropdown == null || backstoryDropdown.value < 0)
                return CoachBackstory.PlayerDevelopmentSpecialist;

            return _backstoryTemplates.Keys.ElementAt(backstoryDropdown.value);
        }

        /// <summary>
        /// Get display name for backstory
        /// </summary>
        private string GetBackstoryDisplayName(CoachBackstory backstory)
        {
            return backstory switch
            {
                CoachBackstory.PlayerDevelopmentSpecialist => "Player Development Specialist",
                CoachBackstory.TacticalMastermind => "Tactical Mastermind",
                CoachBackstory.MotivationalLeader => "Motivational Leader",
                CoachBackstory.FormerElitePPlayer => "Former Elite Player",
                CoachBackstory.ScoutingLegend => "Scouting Legend",
                CoachBackstory.InjuryRehabilitationExpert => "Injury Rehabilitation Expert",
                CoachBackstory.DataAnalyticsGuru => "Data Analytics Guru",
                CoachBackstory.InternationalCoach => "International Coach",
                CoachBackstory.ClubLegend => "Club Legend",
                CoachBackstory.YoungProdigy => "Young Prodigy",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Create a display template for backstory
        /// </summary>
        private CoachBackstoryTemplate CreateDisplayTemplate(CoachBackstory backstory)
        {
            // This would normally fetch from your CoachBackstorySystem
            // For now, return a basic template
            return new CoachBackstoryTemplate
            {
                Backstory = backstory,
                Name = GetBackstoryDisplayName(backstory),
                Description = $"A {GetBackstoryDisplayName(backstory).ToLower()} with unique coaching abilities.",
                BaseInsightLevel = PlayerPotentialManager.CoachInsightLevel.Average,
                UniqueFeature = "Special coaching abilities and bonuses"
            };
        }

        /// <summary>
        /// Get description for insight level
        /// </summary>
        private string GetInsightLevelDescription(PlayerPotentialManager.CoachInsightLevel insightLevel)
        {
            return insightLevel switch
            {
                PlayerPotentialManager.CoachInsightLevel.Poor => "Basic ability to assess player potential",
                PlayerPotentialManager.CoachInsightLevel.Average => "Standard coaching ability to evaluate talent",
                PlayerPotentialManager.CoachInsightLevel.Good => "Good eye for identifying player potential",
                PlayerPotentialManager.CoachInsightLevel.Excellent => "Exceptional talent identification skills",
                PlayerPotentialManager.CoachInsightLevel.Legendary => "Legendary foresight and talent assessment",
                _ => "Unknown insight level"
            };
        }

        /// <summary>
        /// Calculate XP required for a specific level
        /// </summary>
        private int CalculateXPForLevel(int level)
        {
            return level * level * 100; // Simple quadratic progression
        }

        /// <summary>
        /// Handle ability usage
        /// </summary>
        private void OnUseAbility(CoachAbility ability)
        {
            if (_currentCoach == null) return;

            bool success = _backstorySystem.UseCoachAbility(_currentCoach.CoachId, ability.Id);
            
            if (success)
            {
                ShowMessage($"Used ability: {ability.Name}");
                RefreshAbilitiesUI();
            }
            else
            {
                ShowMessage("Ability is not available or on cooldown.");
            }
        }

        /// <summary>
        /// Show player assessment dialog
        /// </summary>
        private void ShowPlayerAssessmentDialog()
        {
            // This would typically show a player selection dialog
            // For now, show a placeholder
            if (playerAssessmentResultPanel != null)
                playerAssessmentResultPanel.SetActive(true);
            
            if (assessmentResultText != null)
            {
                assessmentResultText.text = $"Coach {_currentCoach?.Name} can assess player potential with " +
                                          $"{_currentCoach?.InsightLevel} level accuracy.";
            }
        }

        /// <summary>
        /// Show a message to the user
        /// </summary>
        private void ShowMessage(string message)
        {
            Debug.Log($"UI Message: {message}");
            // In a real implementation, this would show a toast/popup message
        }

        #endregion
    }

    #region Helper Components

    /// <summary>
    /// Component for displaying specialty statistics
    /// </summary>
    public class SpecialtyStatItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI specialtyNameText;
        [SerializeField] private TextMeshProUGUI specialtyValueText;
        [SerializeField] private Slider specialtySlider;

        public void Setup(CoachSpecialty specialty, float value)
        {
            if (specialtyNameText != null)
                specialtyNameText.text = specialty.ToString();
            
            if (specialtyValueText != null)
                specialtyValueText.text = $"{value:F1}x";
            
            if (specialtySlider != null)
                specialtySlider.value = Mathf.Clamp01((value - 0.5f) / 2.0f); // Map 0.5-2.5 to 0-1
        }
    }

    /// <summary>
    /// Component for displaying coach abilities
    /// </summary>
    public class CoachAbilityItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI abilityNameText;
        [SerializeField] private TextMeshProUGUI abilityDescriptionText;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private Button useAbilityButton;

        private CoachAbility _ability;
        private Action<CoachAbility> _onUseAbility;

        public void Setup(CoachAbility ability, bool isAvailable, Action<CoachAbility> onUseAbility, TimeSpan? remainingCooldown = null)
        {
            _ability = ability;
            _onUseAbility = onUseAbility;

            if (abilityNameText != null)
                abilityNameText.text = ability.Name;
            
            if (abilityDescriptionText != null)
                abilityDescriptionText.text = ability.Description;
            
            if (useAbilityButton != null)
            {
                useAbilityButton.interactable = isAvailable;
                useAbilityButton.onClick.RemoveAllListeners();
                if (isAvailable)
                    useAbilityButton.onClick.AddListener(() => _onUseAbility?.Invoke(_ability));
            }
            
            if (cooldownText != null)
            {
                if (remainingCooldown.HasValue)
                {
                    cooldownText.text = $"Cooldown: {remainingCooldown.Value.Hours:D2}:{remainingCooldown.Value.Minutes:D2}:{remainingCooldown.Value.Seconds:D2}";
                    cooldownText.gameObject.SetActive(true);
                }
                else
                {
                    cooldownText.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Component for displaying specialty bonuses
    /// </summary>
    public class SpecialtyBonusItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI specialtyNameText;
        [SerializeField] private TextMeshProUGUI bonusValueText;

        public void Setup(CoachSpecialty specialty, float bonus)
        {
            if (specialtyNameText != null)
                specialtyNameText.text = specialty.ToString();
            
            if (bonusValueText != null)
            {
                string sign = bonus >= 1.0f ? "+" : "";
                bonusValueText.text = $"{sign}{((bonus - 1.0f) * 100):F0}%";
            }
        }
    }

    /// <summary>
    /// Component for displaying specialty progression
    /// </summary>
    public class SpecialtyProgressItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI specialtyNameText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;

        public void Setup(CoachSpecialty specialty, float currentLevel)
        {
            if (specialtyNameText != null)
                specialtyNameText.text = specialty.ToString();
            
            if (progressSlider != null)
                progressSlider.value = Mathf.Clamp01((currentLevel - 1.0f) / 2.0f); // Map 1-3 to 0-1
            
            if (progressText != null)
                progressText.text = $"Level {currentLevel:F1}";
        }
    }

    #endregion
}