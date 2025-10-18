using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AFLManager.UI
{
    /// <summary>
    /// UI component for displaying event history entries
    /// </summary>
    public class EventHistoryEntryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI eventNameText;
        [SerializeField] private TextMeshProUGUI eventDescriptionText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private Image eventIcon;
        [SerializeField] private Image backgroundImage;
        
        [Header("Colors")]
        [SerializeField] private Color positiveEventColor = Color.green;
        [SerializeField] private Color negativeEventColor = Color.red;
        [SerializeField] private Color neutralEventColor = Color.gray;
        
        /// <summary>
        /// Set the event information for this entry
        /// </summary>
        public void SetEventInfo(string eventName, string description, bool isPositive, System.DateTime? date = null)
        {
            eventNameText.text = eventName;
            eventDescriptionText.text = description;
            
            if (date.HasValue)
            {
                dateText.text = date.Value.ToString("MMM dd");
            }
            else
            {
                dateText.text = "Recent";
            }
            
            // Set colors based on event type
            Color eventColor = isPositive ? positiveEventColor : negativeEventColor;
            
            if (eventIcon != null)
                eventIcon.color = eventColor;
                
            if (backgroundImage != null)
            {
                var color = eventColor;
                color.a = 0.1f; // Very transparent background
                backgroundImage.color = color;
            }
        }
    }

    /// <summary>
    /// UI component for displaying attribute weight information
    /// </summary>
    public class AttributeWeightUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI attributeNameText;
        [SerializeField] private TextMeshProUGUI weightValueText;
        [SerializeField] private Slider weightSlider;
        [SerializeField] private Image weightBar;
        
        [Header("Colors")]
        [SerializeField] private Color lowWeightColor = Color.red;
        [SerializeField] private Color mediumWeightColor = Color.yellow;
        [SerializeField] private Color highWeightColor = Color.green;
        
        /// <summary>
        /// Set the attribute weight information
        /// </summary>
        public void SetWeight(string attributeName, float weight)
        {
            attributeNameText.text = attributeName;
            weightValueText.text = $"x{weight:F1}";
            
            // Normalize weight for display (assuming 0.5 to 2.0 range)
            float normalizedWeight = Mathf.Clamp01((weight - 0.5f) / 1.5f);
            
            if (weightSlider != null)
            {
                weightSlider.value = normalizedWeight;
            }
            
            // Color coding based on weight strength
            Color weightColor = GetWeightColor(weight);
            
            if (weightBar != null)
                weightBar.color = weightColor;
                
            if (weightValueText != null)
                weightValueText.color = weightColor;
        }
        
        private Color GetWeightColor(float weight)
        {
            if (weight >= 1.5f)
                return highWeightColor;
            else if (weight >= 1.0f)
                return mediumWeightColor;
            else
                return lowWeightColor;
        }
    }

    /// <summary>
    /// UI component for displaying breakthrough effect entries
    /// </summary>
    public class BreakthroughEffectEntryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI effectNameText;
        [SerializeField] private TextMeshProUGUI effectValueText;
        [SerializeField] private Image effectIcon;
        [SerializeField] private Image effectArrow; // Up/down arrow
        
        [Header("Icons")]
        [SerializeField] private Sprite upArrowSprite;
        [SerializeField] private Sprite downArrowSprite;
        
        [Header("Colors")]
        [SerializeField] private Color positiveColor = Color.green;
        [SerializeField] private Color negativeColor = Color.red;
        
        /// <summary>
        /// Set the breakthrough effect information
        /// </summary>
        public void SetEffect(string attributeName, float multiplier, bool isPositive)
        {
            effectNameText.text = GetDisplayName(attributeName);
            
            // Calculate percentage change
            float percentage = Mathf.Abs((multiplier - 1.0f) * 100f);
            string sign = multiplier > 1.0f ? "+" : "-";
            effectValueText.text = $"{sign}{percentage:F0}%";
            
            // Set colors and arrow
            Color effectColor = isPositive ? positiveColor : negativeColor;
            
            effectValueText.color = effectColor;
            
            if (effectIcon != null)
                effectIcon.color = effectColor;
                
            if (effectArrow != null)
            {
                effectArrow.sprite = multiplier > 1.0f ? upArrowSprite : downArrowSprite;
                effectArrow.color = effectColor;
            }
        }
        
        private string GetDisplayName(string attributeName)
        {
            return attributeName switch
            {
                "All" => "All Development",
                "Physical" => "Physical Attributes",
                "Mental" => "Mental Attributes",
                "DecisionMaking" => "Decision Making",
                "GameReading" => "Game Reading",
                "Positioning" => "Positioning",
                "Leadership" => "Leadership",
                "Composure" => "Composure",
                "Pressure" => "Pressure Handling",
                _ => attributeName
            };
        }
    }

    /// <summary>
    /// UI component for displaying specialization connections (lines between nodes)
    /// </summary>
    public class SpecializationConnectionUI : MonoBehaviour
    {
        [Header("Line Rendering")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private RectTransform lineRect;
        [SerializeField] private Image lineImage;
        
        [Header("Visual Properties")]
        [SerializeField] private Color availableConnectionColor = Color.white;
        [SerializeField] private Color completedConnectionColor = Color.gold;
        [SerializeField] private Color lockedConnectionColor = Color.gray;
        [SerializeField] private float lineWidth = 2f;
        
        /// <summary>
        /// Set up the connection line between two specialization nodes
        /// </summary>
        public void SetConnection(Transform fromNode, Transform toNode)
        {
            if (fromNode == null || toNode == null) return;
            
            // Simple line connection using UI Image
            if (lineImage != null && lineRect != null)
            {
                SetupUILineConnection(fromNode, toNode);
            }
            // Alternative: Use LineRenderer for more complex lines
            else if (lineRenderer != null)
            {
                SetupLineRendererConnection(fromNode, toNode);
            }
        }
        
        private void SetupUILineConnection(Transform fromNode, Transform toNode)
        {
            // Calculate line position and rotation
            Vector3 fromPos = fromNode.position;
            Vector3 toPos = toNode.position;
            Vector3 direction = (toPos - fromPos).normalized;
            
            // Position line at midpoint
            Vector3 midpoint = (fromPos + toPos) / 2f;
            lineRect.position = midpoint;
            
            // Rotate line to point from source to target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // Scale line to match distance
            float distance = Vector3.Distance(fromPos, toPos);
            lineRect.sizeDelta = new Vector2(distance, lineWidth);
        }
        
        private void SetupLineRendererConnection(Transform fromNode, Transform toNode)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, fromNode.position);
            lineRenderer.SetPosition(1, toNode.position);
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
        }
        
        /// <summary>
        /// Update the visual state of the connection
        /// </summary>
        public void UpdateConnectionState(bool isFromCompleted, bool isToAvailable)
        {
            Color connectionColor;
            
            if (isFromCompleted)
                connectionColor = completedConnectionColor;
            else if (isToAvailable)
                connectionColor = availableConnectionColor;
            else
                connectionColor = lockedConnectionColor;
                
            if (lineImage != null)
                lineImage.color = connectionColor;
                
            if (lineRenderer != null)
                lineRenderer.color = connectionColor;
        }
    }

    /// <summary>
    /// Simple development timeline UI placeholder
    /// </summary>
    public class DevelopmentTimelineUI : MonoBehaviour
    {
        [Header("Timeline Elements")]
        [SerializeField] private Transform timelineContainer;
        [SerializeField] private GameObject timelineEntryPrefab;
        
        public void DisplayTimeline(AFLManager.Models.Player player, AFLCoachSim.Core.Development.PlayerDevelopmentProfile profile)
        {
            // Clear existing entries
            foreach (Transform child in timelineContainer)
                Destroy(child.gameObject);
                
            // Create timeline entries for major development events
            CreateTimelineEntry("Career Start", $"Began as {profile.CurrentSpecialization?.Name ?? "General Player"}");
            
            if (profile.CareerExperience > 100)
                CreateTimelineEntry("Developing", "Gained substantial experience");
                
            if (profile.PreviousSpecializations?.Count > 0)
                CreateTimelineEntry("Specialization", $"Mastered {profile.PreviousSpecializations.Count} specialization(s)");
        }
        
        private void CreateTimelineEntry(string title, string description)
        {
            if (timelineEntryPrefab == null || timelineContainer == null) return;
            
            var entry = Instantiate(timelineEntryPrefab, timelineContainer);
            var titleText = entry.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var descText = entry.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            
            if (titleText) titleText.text = title;
            if (descText) descText.text = description;
        }
    }

    /// <summary>
    /// Development planner UI placeholder
    /// </summary>
    public class DevelopmentPlannerUI : MonoBehaviour
    {
        [Header("Planner Elements")]
        [SerializeField] private Transform plannerContainer;
        [SerializeField] private TextMeshProUGUI plannerStatusText;
        
        public void DisplayPlanningOptions(AFLManager.Models.Player player, AFLCoachSim.Core.Development.PlayerDevelopmentProfile profile)
        {
            if (plannerStatusText != null)
            {
                plannerStatusText.text = $"Development planning for {player.Name}\n" +
                                       $"Current Stage: {profile.DevelopmentStage}\n" +
                                       $"Next Goal: Advance specialization";
            }
        }
    }
}