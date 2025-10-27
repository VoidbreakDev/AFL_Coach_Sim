// Assets/Scripts/Systems/TeamSelection/LineupSlot.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLManager.Utilities;

namespace AFLManager.Systems.TeamSelection
{
    /// <summary>
    /// Display slot for a player in the lineup
    /// </summary>
    public class LineupSlot : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI ratingText;
        [SerializeField] private Image positionColorBar;
        [SerializeField] private Image backgroundImage;
        
        private Player player;
        private PositionCategory category;
        
        public void Initialize(Player playerData, PositionCategory positionCategory)
        {
            player = playerData;
            category = positionCategory;
            
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (player == null)
                return;
            
            // Player name
            if (playerNameText)
                playerNameText.text = player.Name;
            
            // Rating
            if (ratingText)
            {
                float rating = player.Stats?.GetAverage() ?? 0;
                ratingText.text = $"{rating:F0}";
            }
            
            // Position color
            if (positionColorBar)
            {
                positionColorBar.color = TeamDataHelper.GetPositionColor(category);
            }
            
            // Background
            if (backgroundImage)
            {
                backgroundImage.color = TeamDataHelper.GetPositionColor(category) * 0.2f;
            }
        }
    }
}
