using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLCoachSim.Core.Development;
using System.Collections.Generic;
using System.Linq;

namespace AFLManager.UI
{
    /// <summary>
    /// UI component for displaying breakthrough events and their effects
    /// </summary>
    public class BreakthroughEventDisplayUI : MonoBehaviour
    {
        [Header("Active Event Display")]
        [SerializeField] private GameObject activeEventContainer;
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescriptionText;
        [SerializeField] private Image eventIcon;
        [SerializeField] private Slider timeRemainingSlider;
        [SerializeField] private TextMeshProUGUI timeRemainingText;
        [SerializeField] private GameObject noActiveEventMessage;
        
        [Header("Event Effects Display")]
        [SerializeField] private Transform effectsContainer;
        [SerializeField] private GameObject effectEntryPrefab;
        [SerializeField] private Color positiveEffectColor = Color.green;
        [SerializeField] private Color negativeEffectColor = Color.red;
        [SerializeField] private Color neutralEffectColor = Color.white;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem positiveEventParticles;
        [SerializeField] private ParticleSystem negativeEventParticles;
        [SerializeField] private Animator eventAnimator;
        
        private BreakthroughEvent currentActiveEvent;
        private float eventStartTime;
        
        /// <summary>
        /// Display an active breakthrough event
        /// </summary>
        public void DisplayActiveEvent(BreakthroughEvent breakthroughEvent)
        {
            if (breakthroughEvent == null)
            {
                ShowNoActiveEvent();
                return;
            }
            
            currentActiveEvent = breakthroughEvent;
            eventStartTime = Time.time;
            
            // Show active event container
            activeEventContainer?.SetActive(true);
            noActiveEventMessage?.SetActive(false);
            
            // Set event info
            eventTitleText.text = GetEventDisplayName(breakthroughEvent.Type);
            eventDescriptionText.text = breakthroughEvent.Description;
            
            // Set event icon
            SetEventIcon(breakthroughEvent);
            
            // Display event effects
            DisplayEventEffects(breakthroughEvent);
            
            // Setup time tracking
            SetupTimeTracking(breakthroughEvent);
            
            // Trigger visual effects
            TriggerVisualEffects(breakthroughEvent);
        }
        
        /// <summary>
        /// Show that there are no active events
        /// </summary>
        public void ShowNoActiveEvent()
        {
            activeEventContainer?.SetActive(false);
            noActiveEventMessage?.SetActive(true);
            currentActiveEvent = null;
            
            // Clear effects
            ClearEffectsDisplay();
        }
        
        private void Update()
        {
            if (currentActiveEvent != null)
            {
                UpdateTimeRemaining();
            }
        }
        
        private void UpdateTimeRemaining()
        {
            if (currentActiveEvent == null) return;
            
            float elapsedTime = Time.time - eventStartTime;
            float totalDuration = currentActiveEvent.DurationWeeks * 7 * 24 * 60 * 60; // Convert weeks to seconds (for demo)
            float remainingTime = Mathf.Max(0, totalDuration - elapsedTime);
            
            // Update slider
            if (timeRemainingSlider != null)
            {
                timeRemainingSlider.value = remainingTime / totalDuration;
            }
            
            // Update text (convert back to weeks for display)
            if (timeRemainingText != null)
            {
                float weeksRemaining = remainingTime / (7 * 24 * 60 * 60);
                timeRemainingText.text = $"{weeksRemaining:F1} weeks remaining";
            }
            
            // Check if event should expire
            if (remainingTime <= 0)
            {
                ExpireCurrentEvent();
            }
        }
        
        private void ExpireCurrentEvent()
        {
            if (currentActiveEvent != null)
            {
                // Trigger expiration animation
                TriggerEventExpiration();
                
                // Clear after animation
                Invoke(nameof(ShowNoActiveEvent), 1f);
            }
        }
        
        private void SetEventIcon(BreakthroughEvent breakthroughEvent)
        {
            if (eventIcon == null) return;
            
            string iconName = GetEventIconName(breakthroughEvent.Type);
            
            // Load icon - you'd implement this based on your asset system
            // eventIcon.sprite = ResourceManager.LoadEventIcon(iconName);
            
            // Set icon color based on event type
            eventIcon.color = breakthroughEvent.IsPositive ? positiveEffectColor : negativeEffectColor;
        }
        
        private string GetEventIconName(BreakthroughEventType type)
        {
            return type switch
            {
                BreakthroughEventType.PhenomenalRising => "icon_phenomenal_rising",
                BreakthroughEventType.VeteranSurge => "icon_veteran_surge",
                BreakthroughEventType.PositionMastery => "icon_position_mastery",
                BreakthroughEventType.LeadershipBloom => "icon_leadership_bloom",
                BreakthroughEventType.InspirationalForm => "icon_inspirational_form",
                BreakthroughEventType.MentalBreakthrough => "icon_mental_breakthrough",
                BreakthroughEventType.PhysicalBreakthrough => "icon_physical_breakthrough",
                BreakthroughEventType.ConfidenceCrisis => "icon_confidence_crisis",
                BreakthroughEventType.InjurySetback => "icon_injury_setback",
                BreakthroughEventType.MotivationLoss => "icon_motivation_loss",
                BreakthroughEventType.AgeReality => "icon_age_reality",
                _ => "icon_breakthrough_default"
            };
        }
        
        private string GetEventDisplayName(BreakthroughEventType type)
        {
            return type switch
            {
                BreakthroughEventType.PhenomenalRising => "Phenomenal Rising ⭐",
                BreakthroughEventType.VeteranSurge => "Veteran Surge 🔥",
                BreakthroughEventType.PositionMastery => "Position Mastery 🎯",
                BreakthroughEventType.LeadershipBloom => "Leadership Bloom 👑",
                BreakthroughEventType.InspirationalForm => "Inspirational Form ✨",
                BreakthroughEventType.MentalBreakthrough => "Mental Breakthrough 🧠",
                BreakthroughEventType.PhysicalBreakthrough => "Physical Breakthrough 💪",
                BreakthroughEventType.ConfidenceCrisis => "Confidence Crisis ⚠️",
                BreakthroughEventType.InjurySetback => "Injury Setback 🏥",
                BreakthroughEventType.MotivationLoss => "Motivation Loss 😞",
                BreakthroughEventType.ComplacencyEffect => "Complacency Effect 😴",
                BreakthroughEventType.AgeReality => "Age Reality Check ⏰",
                BreakthroughEventType.PressureCollapse => "Pressure Collapse 😰",
                _ => type.ToString()
            };
        }
        
        private void DisplayEventEffects(BreakthroughEvent breakthroughEvent)
        {
            ClearEffectsDisplay();
            
            if (breakthroughEvent.AttributeMultipliers == null || !breakthroughEvent.AttributeMultipliers.Any())
                return;
                
            foreach (var effect in breakthroughEvent.AttributeMultipliers)
            {
                CreateEffectEntry(effect.Key, effect.Value, breakthroughEvent.IsPositive);
            }
        }
        
        private void ClearEffectsDisplay()
        {
            if (effectsContainer == null) return;
            
            foreach (Transform child in effectsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void CreateEffectEntry(string attributeName, float multiplier, bool isPositive)
        {
            var effectEntry = Instantiate(effectEntryPrefab, effectsContainer);
            var ui = effectEntry.GetComponent<BreakthroughEffectEntryUI>();
            
            if (ui != null)
            {
                ui.SetEffect(attributeName, multiplier, isPositive);
            }
            else
            {
                // Fallback if specific component doesn't exist
                var text = effectEntry.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    string effectText = GetEffectDisplayText(attributeName, multiplier);
                    text.text = effectText;
                    text.color = isPositive ? positiveEffectColor : negativeEffectColor;
                }
            }
        }
        
        private string GetEffectDisplayText(string attributeName, float multiplier)
        {
            string effectType = multiplier > 1.0f ? "Boost" : "Reduction";
            float percentage = Mathf.Abs((multiplier - 1.0f) * 100f);
            
            if (attributeName == "All")
            {
                return $"{effectType}: {percentage:F0}% to all development";
            }
            else
            {
                return $"{attributeName}: {percentage:F0}% {effectType.ToLower()}";
            }
        }
        
        private void SetupTimeTracking(BreakthroughEvent breakthroughEvent)
        {
            if (timeRemainingSlider != null)
            {
                timeRemainingSlider.maxValue = 1f;
                timeRemainingSlider.value = 1f; // Start at full
            }
            
            if (timeRemainingText != null)
            {
                timeRemainingText.text = $"{breakthroughEvent.DurationWeeks} weeks remaining";
            }
        }
        
        private void TriggerVisualEffects(BreakthroughEvent breakthroughEvent)
        {
            // Particle effects
            if (breakthroughEvent.IsPositive && positiveEventParticles != null)
            {
                positiveEventParticles.Play();
            }
            else if (!breakthroughEvent.IsPositive && negativeEventParticles != null)
            {
                negativeEventParticles.Play();
            }
            
            // Animation
            if (eventAnimator != null)
            {
                string animationTrigger = breakthroughEvent.IsPositive ? "PositiveEvent" : "NegativeEvent";
                eventAnimator.SetTrigger(animationTrigger);
            }
        }
        
        private void TriggerEventExpiration()
        {
            if (eventAnimator != null)
            {
                eventAnimator.SetTrigger("EventExpired");
            }
        }
        
        /// <summary>
        /// Called when the event display is clicked (for more details)
        /// </summary>
        public void OnEventDisplayClicked()
        {
            if (currentActiveEvent == null) return;
            
            // Show detailed event information
            ShowEventDetails(currentActiveEvent);
        }
        
        private void ShowEventDetails(BreakthroughEvent breakthroughEvent)
        {
            // Create or show a detailed event popup
            // This could show full event history, detailed effects, etc.
            Debug.Log($"Showing details for: {breakthroughEvent.Description}");
        }
    }
}