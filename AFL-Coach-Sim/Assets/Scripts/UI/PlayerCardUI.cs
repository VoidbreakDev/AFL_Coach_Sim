using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLManager.Scriptables;

namespace AFLManager.UI
{
    /// <summary>
    /// Compact card display for individual players in lists and grids
    /// </summary>
    public class PlayerCardUI : MonoBehaviour
    {
        [Header("Core Info")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private TextMeshProUGUI ratingText;
        
        [Header("Position Colors")]
        [SerializeField] private Image positionBackground;
        [SerializeField] private Color defenderColor = Color.blue;
        [SerializeField] private Color midfielderColor = Color.green;
        [SerializeField] private Color forwardColor = Color.red;
        [SerializeField] private Color ruckColor = Color.yellow;
        [SerializeField] private Color utilityColor = Color.gray;
        
        [Header("Rating Colors")]
        [SerializeField] private Color eliteColor = new Color(1f, 0.8f, 0f); // Gold
        [SerializeField] private Color premiumColor = Color.green;
        [SerializeField] private Color solidColor = Color.yellow;
        [SerializeField] private Color averageColor = Color.white;
        [SerializeField] private Color poorColor = Color.red;
        
        [Header("Status Indicators")]
        [SerializeField] private Image injuryIcon;
        [SerializeField] private Image suspendedIcon;
        [SerializeField] private Image captainIcon;
        [SerializeField] private GameObject selectedIndicator;
        
        [Header("Interactive")]
        [SerializeField] private Button cardButton;
        
        private AFLManager.Scriptables.PlayerData currentPlayer;
        
        /// <summary>
        /// Event fired when this player card is clicked
        /// </summary>
        public System.Action<AFLManager.Scriptables.PlayerData> OnPlayerSelected;
        
        /// <summary>
        /// Callback for selection interactions (used by SmartTeamBuilderUI)
        /// </summary>
        private System.Action selectionCallback;
        
        private void Start()
        {
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(() => 
                {
                    OnPlayerSelected?.Invoke(currentPlayer);
                    selectionCallback?.Invoke();
                });
            }
        }
        
        /// <summary>
        /// Sets the player data and updates all UI elements
        /// </summary>
        public void SetPlayer(AFLManager.Scriptables.PlayerData player, bool isSelected = false)
        {
            if (player == null) return;
            
            currentPlayer = player;
            
            // Core info
            nameText.text = player.Name;
            positionText.text = GetPositionAbbreviation(player.Role);
            ageText.text = $"{player.Age}y";
            ratingText.text = CalculateOverallRating(player).ToString();
            
            // Position-based background color
            if (positionBackground != null)
            {
                positionBackground.color = GetPositionColor(player.Role);
            }
            
            // Rating-based text color
            var rating = CalculateOverallRating(player);
            ratingText.color = GetRatingColor(rating);
            
            // Status indicators
            UpdateStatusIndicators(player);
            
            // Selection state
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }
        }
        
        private string GetPositionAbbreviation(PlayerRole role)
        {
            switch (role)
            {
                case PlayerRole.FullBack:
                    return "FB";
                case PlayerRole.BackPocket:
                    return "BP";
                case PlayerRole.HalfBack:
                    return "HB";
                case PlayerRole.Wing:
                    return "W";
                case PlayerRole.CentreHalfBack:
                    return "CHB";
                case PlayerRole.Centre:
                    return "C";
                case PlayerRole.CentreHalfForward:
                    return "CHF";
                case PlayerRole.HalfForward:
                    return "HF";
                case PlayerRole.ForwardPocket:
                    return "FP";
                case PlayerRole.FullForward:
                    return "FF";
                case PlayerRole.Ruckman:
                    return "R";
                case PlayerRole.RuckRover:
                    return "RR";
                case PlayerRole.Rover:
                    return "ROV";
                case PlayerRole.Utility:
                    return "UTL";
                default:
                    return "";
            }
        }
        
        private Color GetPositionColor(PlayerRole role)
        {
            switch (role)
            {
                case PlayerRole.FullBack:
                case PlayerRole.BackPocket:
                case PlayerRole.HalfBack:
                case PlayerRole.CentreHalfBack:
                    return defenderColor;
                    
                case PlayerRole.Wing:
                case PlayerRole.Centre:
                case PlayerRole.RuckRover:
                case PlayerRole.Rover:
                    return midfielderColor;
                    
                case PlayerRole.CentreHalfForward:
                case PlayerRole.HalfForward:
                case PlayerRole.ForwardPocket:
                case PlayerRole.FullForward:
                    return forwardColor;
                    
                case PlayerRole.Ruckman:
                    return ruckColor;
                    
                case PlayerRole.Utility:
                    return utilityColor;
                    
                default:
                    return Color.white;
            }
        }
        
        private Color GetRatingColor(int rating)
        {
            if (rating >= 90) return eliteColor;
            if (rating >= 80) return premiumColor;
            if (rating >= 70) return solidColor;
            if (rating >= 60) return averageColor;
            return poorColor;
        }
        
        private int CalculateOverallRating(AFLManager.Scriptables.PlayerData player)
        {
            var stats = player.Stats;
            // Simple average of available stats
            var total = stats.Kicking + stats.Handballing + stats.Tackling + 
                       stats.Speed + stats.Stamina + stats.Knowledge + stats.Playmaking;
            return total / 7;
        }
        
        private void UpdateStatusIndicators(AFLManager.Scriptables.PlayerData player)
        {
            // Note: These would connect to actual player status systems
            // For now, just hide all indicators
            if (injuryIcon != null) injuryIcon.gameObject.SetActive(false);
            if (suspendedIcon != null) suspendedIcon.gameObject.SetActive(false);
            if (captainIcon != null) captainIcon.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Overloaded method to work with AFLManager.Models.Player (for SmartTeamBuilderUI)
        /// </summary>
        public void SetPlayer(AFLManager.Models.Player player, bool isSelected = false)
        {
            if (player == null) return;
            
            // Convert Models.Player to ScriptableObject PlayerData
            var playerData = UnityEngine.ScriptableObject.CreateInstance<AFLManager.Scriptables.PlayerData>();
            playerData.Name = player.Name;
            playerData.Age = player.Age;
            playerData.Role = player.Role;
            playerData.Stats = player.Stats;
            playerData.Contract = player.Contract;
            
            SetPlayer(playerData, isSelected);
        }
        
        /// <summary>
        /// Sets a selection callback for interactive team building
        /// </summary>
        public void SetSelectionCallback(System.Action callback)
        {
            selectionCallback = callback;
        }
        
        /// <summary>
        /// Sets the visual selection state
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }
        }
        
        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
            }
        }
    }
}
