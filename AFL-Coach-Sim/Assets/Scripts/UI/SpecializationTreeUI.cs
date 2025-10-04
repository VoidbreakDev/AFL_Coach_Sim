using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLCoachSim.Core.Development;
using System.Collections.Generic;
using System.Linq;

namespace AFLManager.UI
{
    /// <summary>
    /// Visual tree showing player's specialization progression path
    /// </summary>
    public class SpecializationTreeUI : MonoBehaviour
    {
        [Header("Tree Structure")]
        [SerializeField] private Transform tier1Container;
        [SerializeField] private Transform tier2Container;
        [SerializeField] private Transform tier3Container;
        [SerializeField] private Transform tier4Container;
        [SerializeField] private GameObject specializationNodePrefab;
        
        [Header("Visual Elements")]
        [SerializeField] private GameObject connectionLinePrefab;
        [SerializeField] private Transform connectionsContainer;
        [SerializeField] private Color currentSpecializationColor = Color.cyan;
        [SerializeField] private Color availableColor = Color.green;
        [SerializeField] private Color unavailableColor = Color.gray;
        [SerializeField] private Color completedColor = Color.gold;
        
        [Header("Info Panel")]
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private TextMeshProUGUI infoTitle;
        [SerializeField] private TextMeshProUGUI infoDescription;
        [SerializeField] private TextMeshProUGUI infoRequirements;
        [SerializeField] private Transform infoAttributesContainer;
        [SerializeField] private GameObject attributeWeightPrefab;
        [SerializeField] private Button closeInfoButton;
        
        private Dictionary<string, SpecializationNodeUI> specializationNodes;
        private Player currentPlayer;
        private PlayerDevelopmentProfile currentProfile;
        private List<PlayerSpecialization> availableSpecializations;
        
        private void Start()
        {
            specializationNodes = new Dictionary<string, SpecializationNodeUI>();
            closeInfoButton?.onClick.AddListener(() => infoPanel?.SetActive(false));
        }
        
        /// <summary>
        /// Display the specialization tree for a player
        /// </summary>
        public void DisplaySpecializationPath(Player player, PlayerDevelopmentProfile profile)
        {
            currentPlayer = player;
            currentProfile = profile;
            
            // Get available specializations for this player's position
            availableSpecializations = PlayerDevelopmentHelpers.GetSpecializationsForPosition(player.Role.ToString());
            
            // Clear existing display
            ClearTree();
            
            // Build the tree
            CreateSpecializationNodes();
            CreateConnectionLines();
            
            // Update visual states
            UpdateNodeStates();
        }
        
        private void ClearTree()
        {
            // Clear all containers
            ClearContainer(tier1Container);
            ClearContainer(tier2Container);
            ClearContainer(tier3Container);
            ClearContainer(tier4Container);
            ClearContainer(connectionsContainer);
            
            specializationNodes.Clear();
        }
        
        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            
            foreach (Transform child in container)
                Destroy(child.gameObject);
        }
        
        private void CreateSpecializationNodes()
        {
            // Group specializations by tier
            var tierGroups = availableSpecializations.GroupBy(s => s.TierLevel).ToList();
            
            foreach (var tierGroup in tierGroups)
            {
                var container = GetTierContainer(tierGroup.Key);
                if (container == null) continue;
                
                foreach (var specialization in tierGroup)
                {
                    CreateSpecializationNode(specialization, container);
                }
            }
        }
        
        private Transform GetTierContainer(int tier)
        {
            return tier switch
            {
                1 => tier1Container,
                2 => tier2Container,
                3 => tier3Container,
                4 => tier4Container,
                _ => null
            };
        }
        
        private void CreateSpecializationNode(PlayerSpecialization specialization, Transform container)
        {
            var nodeObj = Instantiate(specializationNodePrefab, container);
            var nodeUI = nodeObj.GetComponent<SpecializationNodeUI>();
            
            if (nodeUI != null)
            {
                nodeUI.Setup(specialization, this);
                specializationNodes[specialization.Id] = nodeUI;
            }
        }
        
        private void CreateConnectionLines()
        {
            foreach (var specialization in availableSpecializations)
            {
                // Create lines to prerequisite specializations
                foreach (var prerequisiteId in specialization.PrerequisiteSpecializations)
                {
                    if (specializationNodes.ContainsKey(prerequisiteId) && 
                        specializationNodes.ContainsKey(specialization.Id))
                    {
                        CreateConnectionLine(
                            specializationNodes[prerequisiteId].transform,
                            specializationNodes[specialization.Id].transform
                        );
                    }
                }
            }
        }
        
        private void CreateConnectionLine(Transform from, Transform to)
        {
            var lineObj = Instantiate(connectionLinePrefab, connectionsContainer);
            var lineUI = lineObj.GetComponent<SpecializationConnectionUI>();
            lineUI?.SetConnection(from, to);
        }
        
        private void UpdateNodeStates()
        {
            foreach (var kvp in specializationNodes)
            {
                var specializationId = kvp.Key;
                var nodeUI = kvp.Value;
                var specialization = availableSpecializations.First(s => s.Id == specializationId);
                
                var state = GetSpecializationState(specialization);
                nodeUI.UpdateVisualState(state, GetProgressValue(specialization));
            }
        }
        
        private SpecializationNodeState GetSpecializationState(PlayerSpecialization specialization)
        {
            // Current specialization
            if (currentProfile.CurrentSpecialization?.Id == specialization.Id)
                return SpecializationNodeState.Current;
                
            // Previously completed specializations
            if (currentProfile.PreviousSpecializations.Any(s => s.Id == specialization.Id))
                return SpecializationNodeState.Completed;
                
            // Check if available (meets requirements)
            if (IsSpecializationAvailable(specialization))
                return SpecializationNodeState.Available;
                
            return SpecializationNodeState.Locked;
        }
        
        private bool IsSpecializationAvailable(PlayerSpecialization specialization)
        {
            // Check age requirement
            if (currentPlayer.Age < specialization.MinimumAge)
                return false;
                
            // Check experience requirement
            if (currentProfile.CareerExperience < specialization.MinimumExperience)
                return false;
                
            // Check prerequisites
            foreach (var prerequisiteId in specialization.PrerequisiteSpecializations)
            {
                bool hasPrerequisite = currentProfile.CurrentSpecialization?.Id == prerequisiteId ||
                                      currentProfile.PreviousSpecializations.Any(s => s.Id == prerequisiteId);
                if (!hasPrerequisite)
                    return false;
            }
            
            return true;
        }
        
        private float GetProgressValue(PlayerSpecialization specialization)
        {
            if (currentProfile.CurrentSpecialization?.Id == specialization.Id)
                return currentProfile.SpecializationProgress / 100f;
                
            if (currentProfile.PreviousSpecializations.Any(s => s.Id == specialization.Id))
                return 1.0f; // Completed
                
            return 0.0f;
        }
        
        /// <summary>
        /// Called when a specialization node is clicked
        /// </summary>
        public void OnSpecializationNodeClicked(PlayerSpecialization specialization)
        {
            ShowSpecializationInfo(specialization);
        }
        
        private void ShowSpecializationInfo(PlayerSpecialization specialization)
        {
            if (infoPanel == null) return;
            
            infoTitle.text = $"{specialization.Name} (Tier {specialization.TierLevel})";
            infoDescription.text = specialization.Description;
            
            // Requirements text
            var requirements = new List<string>();
            if (specialization.MinimumAge > 18)
                requirements.Add($"Age: {specialization.MinimumAge}+");
            if (specialization.MinimumExperience > 0)
                requirements.Add($"Experience: {specialization.MinimumExperience}+");
            if (specialization.PrerequisiteSpecializations.Any())
                requirements.Add($"Requires: {string.Join(", ", specialization.PrerequisiteSpecializations)}");
                
            infoRequirements.text = requirements.Any() ? string.Join("\n", requirements) : "No special requirements";
            
            // Attribute weights
            DisplayAttributeWeights(specialization);
            
            infoPanel.SetActive(true);
        }
        
        private void DisplayAttributeWeights(PlayerSpecialization specialization)
        {
            // Clear existing weights
            foreach (Transform child in infoAttributesContainer)
                Destroy(child.gameObject);
                
            // Show attribute weights
            var sortedWeights = specialization.AttributeWeights
                .OrderByDescending(kvp => kvp.Value)
                .Take(5); // Show top 5
                
            foreach (var weight in sortedWeights)
            {
                var weightObj = Instantiate(attributeWeightPrefab, infoAttributesContainer);
                var ui = weightObj.GetComponent<AttributeWeightUI>();
                ui?.SetWeight(weight.Key, weight.Value);
            }
        }
    }
    
    /// <summary>
    /// States for specialization nodes
    /// </summary>
    public enum SpecializationNodeState
    {
        Locked,     // Cannot be selected (requirements not met)
        Available,  // Can be selected
        Current,    // Currently active specialization
        Completed   // Previously mastered specialization
    }
}