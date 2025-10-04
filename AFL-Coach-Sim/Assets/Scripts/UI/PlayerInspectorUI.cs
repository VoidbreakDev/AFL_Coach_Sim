using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLCoachSim.Integration;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Selection;
using System.Collections.Generic;

namespace AFLManager.UI
{
    /// <summary>
    /// Enhanced player inspector that shows positional analysis, role effectiveness, and tactical fit
    /// </summary>
    public class PlayerInspectorUI : MonoBehaviour
    {
        [Header("Basic Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI currentRoleText;
        
        [Header("Stats Display")]
        [SerializeField] private Slider kickingSlider;
        [SerializeField] private Slider handballingSlider;
        [SerializeField] private Slider tacklingSlider;
        [SerializeField] private Slider speedSlider;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Slider knowledgeSlider;
        [SerializeField] private Slider playmakingSlider;
        [SerializeField] private TextMeshProUGUI overallRatingText;
        
        [Header("Positional Analysis")]
        [SerializeField] private Transform positionRatingsContainer;
        [SerializeField] private GameObject positionRatingPrefab;
        [SerializeField] private TextMeshProUGUI recommendedRoleText;
        [SerializeField] private TextMeshProUGUI roleEffectivenessText;
        
        [Header("Tactical Fit")]
        [SerializeField] private TextMeshProUGUI tacticalDescriptionText;
        [SerializeField] private Transform strengthsContainer;
        [SerializeField] private Transform weaknessesContainer;
        [SerializeField] private GameObject attributeTagPrefab;
        
        [Header("Contract & Morale")]
        [SerializeField] private TextMeshProUGUI salaryText;
        [SerializeField] private TextMeshProUGUI yearsRemainingText;
        [SerializeField] private Slider moraleSlider;
        [SerializeField] private Slider staminaConditionSlider;
        
        [Header("Actions")]
        [SerializeField] private Button changeRoleButton;
        [SerializeField] private Button viewDevelopmentButton;
        [SerializeField] private Dropdown roleChangeDropdown;
        
        private Player currentPlayer;

        private void Start()
        {
            SetupRoleChangeDropdown();
            changeRoleButton.onClick.AddListener(OnChangeRoleClicked);
            viewDevelopmentButton.onClick.AddListener(OnViewDevelopmentClicked);
        }

        /// <summary>
        /// Displays detailed information about a player
        /// </summary>
        public void DisplayPlayer(Player player)
        {
            currentPlayer = player;
            if (player == null) return;

            DisplayBasicInfo(player);
            DisplayStats(player);
            DisplayPositionalAnalysis(player);
            DisplayTacticalFit(player);
            DisplayContractInfo(player);
        }

        private void DisplayBasicInfo(Player player)
        {
            playerNameText.text = player.Name;
            ageText.text = $"Age: {player.Age}";
            stateText.text = $"State: {player.State}";
            currentRoleText.text = $"Position: {player.Role}";
        }

        private void DisplayStats(Player player)
        {
            var stats = player.Stats;
            
            kickingSlider.value = stats.Kicking / 100f;
            handballingSlider.value = stats.Handballing / 100f;
            tacklingSlider.value = stats.Tackling / 100f;
            speedSlider.value = stats.Speed / 100f;
            staminaSlider.value = stats.Stamina / 100f;
            knowledgeSlider.value = stats.Knowledge / 100f;
            playmakingSlider.value = stats.Playmaking / 100f;
            
            var overall = Mathf.RoundToInt(stats.GetAverage());
            overallRatingText.text = $"Overall: {overall}";
            
            // Color-code the overall rating
            if (overall >= 80) overallRatingText.color = Color.green;
            else if (overall >= 65) overallRatingText.color = Color.yellow;
            else overallRatingText.color = Color.red;
        }

        private void DisplayPositionalAnalysis(Player player)
        {
            // Clear existing position ratings
            foreach (Transform child in positionRatingsContainer)
                Destroy(child.gameObject);

            // Calculate effectiveness for each position group
            var positionRatings = CalculatePositionRatings(player);
            
            foreach (var rating in positionRatings)
            {
                var ratingUI = Instantiate(positionRatingPrefab, positionRatingsContainer);
                var component = ratingUI.GetComponent<PositionRatingUI>();
                component.SetRating(rating.Key, rating.Value);
            }

            // Show recommended role
            var recommendedRole = PlayerModelBridge.GetRecommendedRole(player);
            recommendedRoleText.text = $"Recommended: {recommendedRole}";
            
            if (recommendedRole != player.Role)
            {
                recommendedRoleText.color = Color.cyan;
            }
            else
            {
                recommendedRoleText.color = Color.green;
            }

            // Show current role effectiveness
            var currentEffectiveness = GetRoleEffectiveness(player, player.Role);
            roleEffectivenessText.text = $"Current Role Fit: {currentEffectiveness:F1}%";
            
            if (currentEffectiveness >= 80) roleEffectivenessText.color = Color.green;
            else if (currentEffectiveness >= 60) roleEffectivenessText.color = Color.yellow;
            else roleEffectivenessText.color = Color.red;
        }

        private void DisplayTacticalFit(Player player)
        {
            var coreRole = PlayerModelBridge.ToCore(player.Role);
            var positionGroup = PositionUtils.GetPositionGroup(coreRole);
            
            tacticalDescriptionText.text = GetTacticalDescription(player, positionGroup);
            
            DisplayPlayerStrengths(player);
            DisplayPlayerWeaknesses(player);
        }

        private void DisplayPlayerStrengths(Player player)
        {
            // Clear existing strength tags
            foreach (Transform child in strengthsContainer)
                Destroy(child.gameObject);

            var strengths = GetPlayerStrengths(player);
            foreach (var strength in strengths)
            {
                var tag = Instantiate(attributeTagPrefab, strengthsContainer);
                var text = tag.GetComponentInChildren<TextMeshProUGUI>();
                text.text = strength;
                text.color = Color.green;
            }
        }

        private void DisplayPlayerWeaknesses(Player player)
        {
            // Clear existing weakness tags
            foreach (Transform child in weaknessesContainer)
                Destroy(child.gameObject);

            var weaknesses = GetPlayerWeaknesses(player);
            foreach (var weakness in weaknesses)
            {
                var tag = Instantiate(attributeTagPrefab, weaknessesContainer);
                var text = tag.GetComponentInChildren<TextMeshProUGUI>();
                text.text = weakness;
                text.color = Color.red;
            }
        }

        private void DisplayContractInfo(Player player)
        {
            if (player.Contract != null)
            {
                salaryText.text = $"${player.Contract.Salary:N0}";
                yearsRemainingText.text = $"{player.Contract.YearsRemaining} years";
            }
            
            moraleSlider.value = player.Morale;
            staminaConditionSlider.value = player.Stamina;
        }

        private Dictionary<string, float> CalculatePositionRatings(Player player)
        {
            var ratings = new Dictionary<string, float>();
            
            // Calculate suitability for each position group
            ratings["Defense"] = CalculateDefensiveRating(player.Stats);
            ratings["Midfield"] = CalculateMidfieldRating(player.Stats);
            ratings["Forward"] = CalculateForwardRating(player.Stats);
            ratings["Ruck"] = CalculateRuckRating(player.Stats);
            
            return ratings;
        }

        private float CalculateDefensiveRating(PlayerStats stats)
        {
            var rating = (stats.Tackling * 0.3f + stats.Knowledge * 0.25f + 
                         stats.Kicking * 0.2f + stats.Stamina * 0.15f + stats.Speed * 0.1f);
            return Mathf.Clamp(rating, 0, 100);
        }

        private float CalculateMidfieldRating(PlayerStats stats)
        {
            var rating = (stats.Stamina * 0.25f + stats.Playmaking * 0.2f + 
                         stats.Handballing * 0.2f + stats.Speed * 0.15f + 
                         stats.Kicking * 0.1f + stats.Tackling * 0.1f);
            return Mathf.Clamp(rating, 0, 100);
        }

        private float CalculateForwardRating(PlayerStats stats)
        {
            var rating = (stats.Kicking * 0.3f + stats.Speed * 0.25f + 
                         stats.Playmaking * 0.2f + stats.Handballing * 0.15f + stats.Stamina * 0.1f);
            return Mathf.Clamp(rating, 0, 100);
        }

        private float CalculateRuckRating(PlayerStats stats)
        {
            var rating = (stats.Tackling * 0.25f + stats.Knowledge * 0.2f + 
                         stats.Stamina * 0.2f + stats.Kicking * 0.15f + 
                         (100 - stats.Speed) * 0.1f + stats.Handballing * 0.1f); // Rucks typically slower
            return Mathf.Clamp(rating, 0, 100);
        }

        private float GetRoleEffectiveness(Player player, PlayerRole role)
        {
            var coreRole = PlayerModelBridge.ToCore(role);
            var group = PositionUtils.GetPositionGroup(coreRole);
            
            switch (group)
            {
                case PositionGroup.Defense: return CalculateDefensiveRating(player.Stats);
                case PositionGroup.Midfield: return CalculateMidfieldRating(player.Stats);
                case PositionGroup.Forward: return CalculateForwardRating(player.Stats);
                case PositionGroup.Ruck: return CalculateRuckRating(player.Stats);
                default: return player.Stats.GetAverage();
            }
        }

        private string GetTacticalDescription(Player player, PositionGroup group)
        {
            var stats = player.Stats;
            
            switch (group)
            {
                case PositionGroup.Defense:
                    return $"Defensive anchor with {stats.Tackling}/100 tackling and {stats.Knowledge}/100 game reading. " +
                           (stats.Kicking > 70 ? "Strong rebounding kicks." : "May struggle with ball use.");
                case PositionGroup.Midfield:
                    return $"Engine room player with {stats.Stamina}/100 endurance and {stats.Playmaking}/100 playmaking. " +
                           (stats.Speed > 70 ? "Mobile around the ground." : "Better suited to inside work.");
                case PositionGroup.Forward:
                    return $"Goal threat with {stats.Kicking}/100 kicking and {stats.Speed}/100 pace. " +
                           (stats.Playmaking > 70 ? "Can create for teammates." : "Pure goal scorer.");
                case PositionGroup.Ruck:
                    return $"Contest specialist with {stats.Tackling}/100 strength in contests. " +
                           (stats.Kicking > 65 ? "Mobile ruck who can distribute." : "Traditional big body.");
                default:
                    return "Utility player who can fill multiple roles as needed.";
            }
        }

        private List<string> GetPlayerStrengths(Player player)
        {
            var strengths = new List<string>();
            var stats = player.Stats;
            
            if (stats.Kicking >= 80) strengths.Add("Elite Kicking");
            if (stats.Handballing >= 80) strengths.Add("Elite Handballing");
            if (stats.Tackling >= 80) strengths.Add("Elite Tackling");
            if (stats.Speed >= 80) strengths.Add("Elite Speed");
            if (stats.Stamina >= 80) strengths.Add("Elite Endurance");
            if (stats.Knowledge >= 80) strengths.Add("Elite Game Sense");
            if (stats.Playmaking >= 80) strengths.Add("Elite Playmaker");
            
            // Add moderate strengths if no elite ones
            if (strengths.Count == 0)
            {
                if (stats.Kicking >= 70) strengths.Add("Good Kicking");
                if (stats.Tackling >= 70) strengths.Add("Good Defender");
                if (stats.Stamina >= 70) strengths.Add("Good Endurance");
                if (stats.Speed >= 70) strengths.Add("Good Pace");
            }
            
            return strengths;
        }

        private List<string> GetPlayerWeaknesses(Player player)
        {
            var weaknesses = new List<string>();
            var stats = player.Stats;
            
            if (stats.Kicking <= 50) weaknesses.Add("Poor Kicking");
            if (stats.Handballing <= 50) weaknesses.Add("Poor Handballing");
            if (stats.Tackling <= 50) weaknesses.Add("Poor Tackling");
            if (stats.Speed <= 50) weaknesses.Add("Lacks Pace");
            if (stats.Stamina <= 50) weaknesses.Add("Poor Endurance");
            if (stats.Knowledge <= 50) weaknesses.Add("Poor Game Sense");
            if (stats.Playmaking <= 50) weaknesses.Add("Limited Playmaking");
            
            return weaknesses;
        }

        private void SetupRoleChangeDropdown()
        {
            if (roleChangeDropdown == null) return;
            
            roleChangeDropdown.ClearOptions();
            var options = new List<string>();
            
            foreach (PlayerRole role in System.Enum.GetValues(typeof(PlayerRole)))
            {
                options.Add(role.ToString());
            }
            
            roleChangeDropdown.AddOptions(options);
        }

        private void OnChangeRoleClicked()
        {
            if (currentPlayer == null || roleChangeDropdown == null) return;
            
            var selectedRole = (PlayerRole)roleChangeDropdown.value;
            currentPlayer.Role = selectedRole;
            
            // Refresh the display
            DisplayPlayer(currentPlayer);
            
            Debug.Log($"Changed {currentPlayer.Name} to {selectedRole}");
        }

        private void OnViewDevelopmentClicked()
        {
            if (currentPlayer == null) return;
            
            // Find and open the player development panel
            var developmentPanel = FindObjectOfType<PlayerDevelopmentPanel>();
            if (developmentPanel != null)
            {
                developmentPanel.ShowPlayerDevelopment(currentPlayer);
            }
            else
            {
                Debug.LogWarning("PlayerDevelopmentPanel not found in scene");
            }
        }
    }
}
