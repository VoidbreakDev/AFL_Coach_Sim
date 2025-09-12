using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AFLManager.UI
{
    /// <summary>
    /// UI component that displays current vs ideal player count for a position
    /// </summary>
    public class PositionBarUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI positionNameText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Slider currentBar;
        [SerializeField] private Slider idealBar;
        [SerializeField] private Image statusIcon;
        
        [Header("Status Colors")]
        [SerializeField] private Color perfectColor = Color.green;
        [SerializeField] private Color goodColor = Color.yellow;
        [SerializeField] private Color poorColor = Color.red;
        
        [Header("Status Icons")]
        [SerializeField] private Sprite perfectIcon;
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite problemIcon;
        
        /// <summary>
        /// Sets the position data and updates the display
        /// </summary>
        public void SetData(string positionName, int current, int ideal)
        {
            positionNameText.text = positionName;
            countText.text = $"{current}/{ideal}";
            
            // Set bar values (normalize to reasonable scale)
            var maxScale = Mathf.Max(current, ideal, 10);
            currentBar.value = (float)current / maxScale;
            idealBar.value = (float)ideal / maxScale;
            
            // Set colors based on balance
            var status = GetPositionStatus(current, ideal);
            var color = GetStatusColor(status);
            
            currentBar.fillRect.GetComponent<Image>().color = color;
            countText.color = color;
            
            // Set status icon
            SetStatusIcon(status);
        }
        
        private PositionStatus GetPositionStatus(int current, int ideal)
        {
            if (current == ideal) return PositionStatus.Perfect;
            if (Mathf.Abs(current - ideal) <= 1) return PositionStatus.Good;
            return PositionStatus.Poor;
        }
        
        private Color GetStatusColor(PositionStatus status)
        {
            switch (status)
            {
                case PositionStatus.Perfect:
                    return perfectColor;
                case PositionStatus.Good:
                    return goodColor;
                case PositionStatus.Poor:
                    return poorColor;
                default:
                    return Color.white;
            }
        }
        
        private void SetStatusIcon(PositionStatus status)
        {
            if (statusIcon == null) return;
            
            switch (status)
            {
                case PositionStatus.Perfect:
                    statusIcon.sprite = perfectIcon;
                    break;
                case PositionStatus.Good:
                    statusIcon.sprite = warningIcon;
                    break;
                case PositionStatus.Poor:
                    statusIcon.sprite = problemIcon;
                    break;
                default:
                    statusIcon.sprite = null;
                    break;
            }
        }
        
        private enum PositionStatus
        {
            Perfect,
            Good, 
            Poor
        }
    }
}
