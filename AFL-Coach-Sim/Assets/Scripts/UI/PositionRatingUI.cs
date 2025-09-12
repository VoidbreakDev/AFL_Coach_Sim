using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AFLManager.UI
{
    /// <summary>
    /// UI component that displays a player's rating for a specific position
    /// </summary>
    public class PositionRatingUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI positionNameText;
        [SerializeField] private TextMeshProUGUI ratingValueText;
        [SerializeField] private Slider ratingSlider;
        [SerializeField] private Image ratingColorBar;
        
        [Header("Color Coding")]
        [SerializeField] private Color excellentColor = Color.green;
        [SerializeField] private Color goodColor = Color.yellow;
        [SerializeField] private Color averageColor = Color.white;
        [SerializeField] private Color poorColor = Color.red;
        
        /// <summary>
        /// Sets the position name and rating value
        /// </summary>
        public void SetRating(string positionName, float rating)
        {
            // Clamp rating to 0-100 range
            rating = Mathf.Clamp(rating, 0f, 100f);
            
            // Set text values
            positionNameText.text = positionName;
            ratingValueText.text = $"{rating:F0}%";
            
            // Set slider value (0-1 range)
            ratingSlider.value = rating / 100f;
            
            // Set color based on rating
            var color = GetColorForRating(rating);
            ratingColorBar.color = color;
            ratingValueText.color = color;
        }
        
        /// <summary>
        /// Gets appropriate color based on rating value
        /// </summary>
        private Color GetColorForRating(float rating)
        {
            if (rating >= 80f) return excellentColor;
            if (rating >= 65f) return goodColor;
            if (rating >= 50f) return averageColor;
            return poorColor;
        }
    }
}
