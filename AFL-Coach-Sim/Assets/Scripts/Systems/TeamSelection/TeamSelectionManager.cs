// Assets/Scripts/Systems/TeamSelection/TeamSelectionManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using AFLManager.Managers;
using AFLManager.Utilities;

namespace AFLManager.Systems.TeamSelection
{
    /// <summary>
    /// Manages team lineup selection with position validation
    /// </summary>
    public class TeamSelectionManager : MonoBehaviour
    {
        [Header("Team Data")]
        [SerializeField] private string teamId;
        private Team currentTeam;
        private List<Player> selectedLineup = new List<Player>();
        
        [Header("UI References")]
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private Transform lineupDisplayContainer;
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private GameObject lineupSlotPrefab;
        
        [Header("Filters")]
        [SerializeField] private TMP_Dropdown positionFilter;
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private Toggle showSelectedOnly;
        
        [Header("Info Display")]
        [SerializeField] private TextMeshProUGUI selectionCountText;
        [SerializeField] private TextMeshProUGUI validationText;
        [SerializeField] private TextMeshProUGUI lineupRatingText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button autoBest22Button;
        [SerializeField] private Button clearButton;
        
        [Header("Position Requirements Display")]
        [SerializeField] private TextMeshProUGUI defendersCountText;
        [SerializeField] private TextMeshProUGUI midfieldersCountText;
        [SerializeField] private TextMeshProUGUI forwardsCountText;
        [SerializeField] private TextMeshProUGUI rucksCountText;
        
        private List<PlayerSelectionCard> playerCards = new List<PlayerSelectionCard>();
        private PositionCategory currentFilter = PositionCategory.Utility; // All
        
        public System.Action<List<Player>> OnLineupConfirmed;
        
        void Start()
        {
            LoadTeamData();
            SetupUI();
            RefreshPlayerList();
            UpdateLineupDisplay();
        }
        
        private void LoadTeamData()
        {
            teamId = PlayerPrefs.GetString("PlayerTeamId", "TEAM_001");
            currentTeam = SaveLoadManager.LoadTeam(teamId);
            
            if (currentTeam == null)
            {
                Debug.LogError("[TeamSelection] Failed to load team data");
                return;
            }
            
            // Load saved lineup if exists
            string savedLineup = PlayerPrefs.GetString($"Lineup_{teamId}", "");
            if (!string.IsNullOrEmpty(savedLineup))
            {
                LoadSavedLineup(savedLineup);
            }
            else
            {
                // Auto-select best 22 on first load
                AutoSelectBest22();
            }
        }
        
        private void SetupUI()
        {
            if (positionFilter)
            {
                positionFilter.ClearOptions();
                positionFilter.AddOptions(new List<string>
                {
                    "All Positions",
                    "Defenders",
                    "Midfielders",
                    "Forwards",
                    "Rucks"
                });
                positionFilter.onValueChanged.AddListener(OnFilterChanged);
            }
            
            if (searchField)
                searchField.onValueChanged.AddListener(_ => RefreshPlayerList());
            
            if (showSelectedOnly)
                showSelectedOnly.onValueChanged.AddListener(_ => RefreshPlayerList());
            
            if (confirmButton)
                confirmButton.onClick.AddListener(ConfirmLineup);
            
            if (autoBest22Button)
                autoBest22Button.onClick.AddListener(AutoSelectBest22);
            
            if (clearButton)
                clearButton.onClick.AddListener(ClearSelection);
        }
        
        private void RefreshPlayerList()
        {
            // Clear existing cards
            foreach (var card in playerCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            playerCards.Clear();
            
            if (playerListContainer == null || playerCardPrefab == null)
                return;
            
            // Filter players
            var filteredPlayers = GetFilteredPlayers();
            
            // Create player cards
            foreach (var player in filteredPlayers)
            {
                var cardObj = Instantiate(playerCardPrefab, playerListContainer);
                var card = cardObj.GetComponent<PlayerSelectionCard>();
                
                if (card != null)
                {
                    bool isSelected = selectedLineup.Contains(player);
                    card.Initialize(player, isSelected, OnPlayerCardClicked);
                    playerCards.Add(card);
                }
            }
            
            UpdateSelectionInfo();
        }
        
        private List<Player> GetFilteredPlayers()
        {
            if (currentTeam?.Roster == null)
                return new List<Player>();
            
            var players = currentTeam.Roster.AsEnumerable();
            
            // Filter by position
            if (currentFilter != PositionCategory.Utility)
            {
                players = players.Where(p => TeamDataHelper.GetPositionCategory(p.Role) == currentFilter);
            }
            
            // Filter by search
            if (searchField != null && !string.IsNullOrEmpty(searchField.text))
            {
                string search = searchField.text.ToLower();
                players = players.Where(p => p.Name.ToLower().Contains(search));
            }
            
            // Filter by selected
            if (showSelectedOnly != null && showSelectedOnly.isOn)
            {
                players = players.Where(p => selectedLineup.Contains(p));
            }
            
            return players.OrderByDescending(p => p.Stats?.GetAverage() ?? 0).ToList();
        }
        
        private void OnFilterChanged(int index)
        {
            currentFilter = index switch
            {
                1 => PositionCategory.Defender,
                2 => PositionCategory.Midfielder,
                3 => PositionCategory.Forward,
                4 => PositionCategory.Ruck,
                _ => PositionCategory.Utility
            };
            
            RefreshPlayerList();
        }
        
        private void OnPlayerCardClicked(Player player)
        {
            if (selectedLineup.Contains(player))
            {
                // Deselect
                selectedLineup.Remove(player);
                UIFeedbackHelper.ShowToast($"Removed {player.Name}");
            }
            else
            {
                // Select
                if (selectedLineup.Count >= 22)
                {
                    UIFeedbackHelper.ShowError("Lineup already has 22 players");
                    return;
                }
                
                selectedLineup.Add(player);
                UIFeedbackHelper.ShowToast($"Added {player.Name}");
            }
            
            RefreshPlayerList();
            UpdateLineupDisplay();
        }
        
        private void UpdateLineupDisplay()
        {
            UpdateSelectionInfo();
            UpdatePositionCounts();
            UpdateLineupVisual();
        }
        
        private void UpdateSelectionInfo()
        {
            // Selection count
            if (selectionCountText)
            {
                selectionCountText.text = $"{selectedLineup.Count}/22 selected";
            }
            
            // Validation
            var validation = TeamDataHelper.ValidateLineup(selectedLineup);
            if (validationText)
            {
                validationText.text = validation.isValid ? "✓ Valid Lineup" : validation.error;
                validationText.color = validation.isValid ? Color.green : Color.red;
            }
            
            // Rating
            if (lineupRatingText)
            {
                float rating = TeamDataHelper.GetLineupAverageRating(selectedLineup);
                lineupRatingText.text = $"Avg Rating: {rating:F1}";
            }
            
            // Confirm button
            if (confirmButton)
            {
                confirmButton.interactable = validation.isValid;
            }
        }
        
        private void UpdatePositionCounts()
        {
            var categories = selectedLineup.GroupBy(p => TeamDataHelper.GetPositionCategory(p.Role));
            
            int defenders = categories.FirstOrDefault(g => g.Key == PositionCategory.Defender)?.Count() ?? 0;
            int midfielders = categories.FirstOrDefault(g => g.Key == PositionCategory.Midfielder)?.Count() ?? 0;
            int forwards = categories.FirstOrDefault(g => g.Key == PositionCategory.Forward)?.Count() ?? 0;
            int rucks = categories.FirstOrDefault(g => g.Key == PositionCategory.Ruck)?.Count() ?? 0;
            
            if (defendersCountText)
            {
                defendersCountText.text = $"{defenders}/6";
                defendersCountText.color = defenders >= 6 ? Color.green : Color.red;
            }
            
            if (midfieldersCountText)
            {
                midfieldersCountText.text = $"{midfielders}/6";
                midfieldersCountText.color = midfielders >= 6 ? Color.green : Color.red;
            }
            
            if (forwardsCountText)
            {
                forwardsCountText.text = $"{forwards}/6";
                forwardsCountText.color = forwards >= 6 ? Color.green : Color.red;
            }
            
            if (rucksCountText)
            {
                rucksCountText.text = $"{rucks}/2";
                rucksCountText.color = rucks >= 2 ? Color.green : Color.red;
            }
        }
        
        private void UpdateLineupVisual()
        {
            // Clear existing lineup display
            if (lineupDisplayContainer != null)
            {
                foreach (Transform child in lineupDisplayContainer)
                    Destroy(child.gameObject);
                
                // Create lineup slots by position
                var defenders = selectedLineup.Where(p => TeamDataHelper.GetPositionCategory(p.Role) == PositionCategory.Defender).ToList();
                var midfielders = selectedLineup.Where(p => TeamDataHelper.GetPositionCategory(p.Role) == PositionCategory.Midfielder).ToList();
                var forwards = selectedLineup.Where(p => TeamDataHelper.GetPositionCategory(p.Role) == PositionCategory.Forward).ToList();
                var rucks = selectedLineup.Where(p => TeamDataHelper.GetPositionCategory(p.Role) == PositionCategory.Ruck).ToList();
                
                CreateLineupSection("Defenders", defenders, PositionCategory.Defender);
                CreateLineupSection("Midfielders", midfielders, PositionCategory.Midfielder);
                CreateLineupSection("Forwards", forwards, PositionCategory.Forward);
                CreateLineupSection("Rucks", rucks, PositionCategory.Ruck);
            }
        }
        
        private void CreateLineupSection(string title, List<Player> players, PositionCategory category)
        {
            if (lineupSlotPrefab == null || lineupDisplayContainer == null)
                return;
            
            // Section header
            var header = new GameObject($"{title} Header");
            header.transform.SetParent(lineupDisplayContainer);
            var headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = $"<b>{title}</b>";
            headerText.fontSize = 18;
            
            // Player slots
            foreach (var player in players)
            {
                var slot = Instantiate(lineupSlotPrefab, lineupDisplayContainer);
                var slotUI = slot.GetComponent<LineupSlot>();
                if (slotUI != null)
                {
                    slotUI.Initialize(player, category);
                }
            }
        }
        
        public void AutoSelectBest22()
        {
            selectedLineup.Clear();
            selectedLineup = TeamDataHelper.GetBest22(currentTeam);
            
            RefreshPlayerList();
            UpdateLineupDisplay();
            
            UIFeedbackHelper.ShowSuccess("Best 22 players selected");
        }
        
        public void ClearSelection()
        {
            UIFeedbackHelper.ShowConfirmation(
                "Clear all selections?",
                () =>
                {
                    selectedLineup.Clear();
                    RefreshPlayerList();
                    UpdateLineupDisplay();
                    UIFeedbackHelper.ShowToast("Selection cleared");
                }
            );
        }
        
        public void ConfirmLineup()
        {
            var validation = TeamDataHelper.ValidateLineup(selectedLineup);
            
            if (!validation.isValid)
            {
                UIFeedbackHelper.ShowError(validation.error);
                return;
            }
            
            // Save lineup
            SaveLineup();
            
            // Notify
            OnLineupConfirmed?.Invoke(selectedLineup);
            UIFeedbackHelper.ShowSuccess("Lineup confirmed!");
        }
        
        private void SaveLineup()
        {
            // Save player IDs
            string lineupData = string.Join(",", selectedLineup.Select(p => p.Id));
            PlayerPrefs.SetString($"Lineup_{teamId}", lineupData);
            PlayerPrefs.Save();
        }
        
        private void LoadSavedLineup(string lineupData)
        {
            if (currentTeam?.Roster == null)
                return;
            
            var playerIds = lineupData.Split(',');
            selectedLineup.Clear();
            
            foreach (var id in playerIds)
            {
                var player = currentTeam.Roster.FirstOrDefault(p => p.Id == id);
                if (player != null)
                    selectedLineup.Add(player);
            }
        }
    }
}
