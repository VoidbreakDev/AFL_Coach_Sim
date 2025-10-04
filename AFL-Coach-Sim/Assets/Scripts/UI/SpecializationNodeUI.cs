using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLCoachSim.Core.Development;

namespace AFLManager.UI
{
    /// <summary>
    /// Individual node in the specialization tree representing one specialization
    /// </summary>
    public class SpecializationNodeUI : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Button nodeButton;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Image nodeIcon;
        [SerializeField] private TextMeshProUGUI nodeTitle;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private GameObject progressContainer;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Image completedCheckmark;
        
        [Header("State Colors")]
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color currentColor = Color.cyan;
        [SerializeField] private Color completedColor = Color.gold;
        
        [Header("Animations")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private bool animateCurrentNode = true;
        
        private PlayerSpecialization specialization;
        private SpecializationTreeUI parentTree;
        private SpecializationNodeState currentState;
        private bool isAnimating;
        
        private void Start()
        {
            nodeButton?.onClick.AddListener(OnNodeClicked);
        }
        
        /// <summary>
        /// Setup the node with specialization data
        /// </summary>
        public void Setup(PlayerSpecialization spec, SpecializationTreeUI parent)
        {
            specialization = spec;
            parentTree = parent;
            
            // Set basic info
            nodeTitle.text = spec.Name;
            tierText.text = $"T{spec.TierLevel}";
            
            // Set icon based on specialization type
            SetSpecializationIcon(spec);
        }
        
        /// <summary>
        /// Update the visual state of the node
        /// </summary>
        public void UpdateVisualState(SpecializationNodeState state, float progress)
        {
            currentState = state;
            
            // Update background color
            Color targetColor = state switch
            {
                SpecializationNodeState.Locked => lockedColor,
                SpecializationNodeState.Available => availableColor,
                SpecializationNodeState.Current => currentColor,
                SpecializationNodeState.Completed => completedColor,
                _ => Color.white
            };
            
            nodeBackground.color = targetColor;
            
            // Update progress display
            UpdateProgressDisplay(state, progress);
            
            // Update interactive state
            nodeButton.interactable = state != SpecializationNodeState.Locked;
            
            // Show/hide elements based on state
            lockOverlay?.SetActive(state == SpecializationNodeState.Locked);
            completedCheckmark?.gameObject.SetActive(state == SpecializationNodeState.Completed);
            
            // Start animation for current node
            if (state == SpecializationNodeState.Current && animateCurrentNode)
            {
                StartPulseAnimation();
            }
            else
            {
                StopPulseAnimation();
            }
        }
        
        private void UpdateProgressDisplay(SpecializationNodeState state, float progress)
        {
            bool showProgress = state == SpecializationNodeState.Current || 
                               (state == SpecializationNodeState.Completed && progress > 0);
            
            progressContainer?.SetActive(showProgress);
            
            if (showProgress && progressSlider != null)
            {
                progressSlider.value = progress;
            }
        }
        
        private void SetSpecializationIcon(PlayerSpecialization spec)
        {
            if (nodeIcon == null) return;
            
            // Set icon based on specialization type
            // This would be connected to your icon system
            var iconName = GetIconNameForSpecialization(spec.Type);
            
            // Load icon sprite - you'd implement this based on your asset system
            // nodeIcon.sprite = ResourceManager.LoadIcon(iconName);
        }
        
        private string GetIconNameForSpecialization(PlayerSpecializationType type)
        {
            return type switch
            {
                PlayerSpecializationType.DefensiveGeneral => "icon_defense_general",
                PlayerSpecializationType.KeyDefender => "icon_key_defender",
                PlayerSpecializationType.MidfieldGeneral => "icon_midfield_general",
                PlayerSpecializationType.InsideMidfielder => "icon_inside_mid",
                PlayerSpecializationType.ForwardGeneral => "icon_forward_general",
                PlayerSpecializationType.KeyForward => "icon_key_forward",
                PlayerSpecializationType.RuckGeneral => "icon_ruck_general",
                PlayerSpecializationType.Superstar => "icon_superstar",
                PlayerSpecializationType.HallOfFamer => "icon_hall_of_fame",
                _ => "icon_default"
            };
        }
        
        private void StartPulseAnimation()
        {
            if (isAnimating) return;
            
            isAnimating = true;
            StartCoroutine(PulseCoroutine());
        }
        
        private void StopPulseAnimation()
        {
            isAnimating = false;
            StopAllCoroutines();
            
            // Reset to normal scale
            transform.localScale = Vector3.one;
        }
        
        private System.Collections.IEnumerator PulseCoroutine()
        {
            while (isAnimating)
            {
                // Pulse from 1.0 to 1.1 scale
                float pulseValue = 1.0f + 0.1f * Mathf.Sin(Time.time * pulseSpeed);
                transform.localScale = Vector3.one * pulseValue;
                
                yield return null;
            }
        }
        
        private void OnNodeClicked()
        {
            if (specialization == null || parentTree == null) return;
            
            parentTree.OnSpecializationNodeClicked(specialization);
            
            // Play click sound
            // AudioManager.PlayClickSound();
        }
        
        /// <summary>
        /// Highlight this node (called from external systems)
        /// </summary>
        public void HighlightNode(bool highlight)
        {
            if (highlight)
            {
                // Add highlight effect
                nodeBackground.color = Color.yellow;
            }
            else
            {
                // Restore original color based on state
                UpdateVisualState(currentState, progressSlider?.value ?? 0f);
            }
        }
        
        private void OnDestroy()
        {
            StopPulseAnimation();
        }
    }
}