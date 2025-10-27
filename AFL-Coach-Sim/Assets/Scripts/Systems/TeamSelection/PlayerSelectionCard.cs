// Assets/Scripts/Systems/TeamSelection/PlayerSelectionCard.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLManager.Utilities;

namespace AFLManager.Systems.TeamSelection
{
    /// <summary>
    /// UI card for displaying and selecting a player
    /// </summary>
    public class PlayerSelectionCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI ratingText;
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private Image positionColorBar;
        [SerializeField] private Image selectionIndicator;
        [SerializeField] private Button selectButton;
        
        [Header("Colors")]
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color unselectedColor = new Color(0.2f, 0.2f, 0.2f);
        
        private Player player;
        private bool isSelected;
        private System.Action<Player> onClicked;
        
        void Awake()
        {
            if (selectButton)
                selectButton.onClick.AddListener(OnCardClicked);
        }
        
        public void Initialize(Player playerData, bool selected, System.Action<Player> clickCallback)
        {
            player = playerData;
            isSelected = selected;
            onClicked = clickCallback;
            
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (player == null)
                return;
            
            // Player info
            if (playerNameText)
                playerNameText.text = player.Name;
            
            if (positionText)
                positionText.text = player.Role.ToString();
            
            if (ratingText)
            {
                float rating = player.Stats?.GetAverage() ?? 0;
                ratingText.text = $"{rating:F0}";
            }
            
            if (ageText)
                ageText.text = $"Age {player.Age}";
            
            // Position color
            if (positionColorBar)
            {
                var category = TeamDataHelper.GetPositionCategory(player.Role);
                positionColorBar.color = TeamDataHelper.GetPositionColor(category);
            }
            
            // Selection state
            if (selectionIndicator)
            {
                selectionIndicator.gameObject.SetActive(isSelected);
                selectionIndicator.color = selectedColor;
            }
            
            // Background color
            var image = GetComponent<Image>();
            if (image)
            {
                image.color = isSelected ? selectedColor * 0.3f : unselectedColor;
            }
        }
        
        private void OnCardClicked()
        {
            onClicked?.Invoke(player);
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateDisplay();
        }
    }
}
