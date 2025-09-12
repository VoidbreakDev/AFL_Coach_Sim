using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using AFLManager.Models;
using AFLCoachSim.Integration;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Selection;

namespace AFLManager.UI
{
    /// <summary>
    /// Smart Team Builder - Advanced UI for analyzing roster balance and optimizing team selection
    /// </summary>
    public class SmartTeamBuilderUI : MonoBehaviour
    {
        [Header("Team Data")]
        [SerializeField] private Team currentTeam;
        
        [Header("Roster Analysis")]
        [SerializeField] private TextMeshProUGUI teamNameText;
        [SerializeField] private TextMeshProUGUI balanceScoreText;
        [SerializeField] private TextMeshProUGUI balanceDescriptionText;
        [SerializeField] private Transform recommendationsContainer;
        [SerializeField] private GameObject recommendationItemPrefab;
        
        [Header("Position Distribution")]
        [SerializeField] private Transform positionBarsContainer;
        [SerializeField] private GameObject positionBarPrefab;
        
        [Header("Best 22 Selection")]
        [SerializeField] private Transform best22Container;
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private Button autoSelectButton;
        [SerializeField] private Button optimizeButton;
        [SerializeField] private Button resetButton;
        
        [Header("Player Pool")]
        [SerializeField] private Transform playerPoolContainer;
        [SerializeField] private Dropdown positionFilterDropdown;
        [SerializeField] private Dropdown sortingDropdown;
        [SerializeField] private TMP_InputField searchInput;
        
        [Header("Quick Actions")]
        [SerializeField] private Button generateRosterButton;
        [SerializeField] private Button analyzeOpponentButton;
        [SerializeField] private Button exportTeamButton;
        
        private List<Player> selectedBest22 = new List<Player>();
        private PositionalAnalysis currentAnalysis;

        private void Start()
        {
            SetupUI();
            
            if (currentTeam != null)
            {
                AnalyzeRoster(currentTeam);
            }
        }

        /// <summary>
        /// Sets a team to analyze and manage
        /// </summary>
        public void SetTeam(Team team)
        {
            currentTeam = team;
            if (currentTeam != null)
            {
                AnalyzeRoster(currentTeam);
            }
        }

        /// <summary>
        /// Performs comprehensive roster analysis
        /// </summary>
        public void AnalyzeRoster(Team team)
        {
            if (team == null || team.Roster == null) return;

            teamNameText.text = team.Name;
            currentAnalysis = PlayerModelBridge.AnalyzeRoster(team);
            
            DisplayBalanceAnalysis();
            DisplayPositionDistribution();
            DisplayRecommendations();
            UpdatePlayerPool();
            
            // Auto-select best 22 if none selected
            if (selectedBest22.Count == 0)
            {
                AutoSelectBest22();
            }
            
            UpdateBest22Display();
        }

        private void DisplayBalanceAnalysis()
        {
            if (currentAnalysis == null) return;
            
            balanceScoreText.text = $"Balance Score: {currentAnalysis.BalanceScore:F0}/100";
            balanceDescriptionText.text = currentAnalysis.GetBalanceDescription();
            
            // Color-code the balance score
            var color = GetColorForBalance(currentAnalysis.BalanceScore);
            balanceScoreText.color = color;
            balanceDescriptionText.color = color;
        }

        private void DisplayPositionDistribution()
        {
            // Clear existing bars
            foreach (Transform child in positionBarsContainer)
                Destroy(child.gameObject);

            if (currentAnalysis == null) return;

            var ideal = PositionUtils.GetIdealStructure();
            var positions = new Dictionary<string, (int current, int ideal)>
            {
                ["Defenders"] = (currentAnalysis.Defenders, ideal.Defenders),
                ["Midfielders"] = (currentAnalysis.Midfielders, ideal.Midfielders),
                ["Forwards"] = (currentAnalysis.Forwards, ideal.Forwards),
                ["Ruckmen"] = (currentAnalysis.Ruckmen, ideal.Ruckmen),
                ["Utility"] = (currentAnalysis.Utility, 2) // Ideal utility count
            };

            foreach (var position in positions)
            {
                var bar = Instantiate(positionBarPrefab, positionBarsContainer);
                var component = bar.GetComponent<PositionBarUI>();
                component.SetData(position.Key, position.Value.current, position.Value.ideal);
            }
        }

        private void DisplayRecommendations()
        {
            // Clear existing recommendations
            foreach (Transform child in recommendationsContainer)
                Destroy(child.gameObject);

            if (currentAnalysis == null) return;

            var recommendations = currentAnalysis.GetRecommendations();
            foreach (var recommendation in recommendations)
            {
                var item = Instantiate(recommendationItemPrefab, recommendationsContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                text.text = $"• {recommendation}";
            }
        }

        private void UpdatePlayerPool()
        {
            // Clear existing player cards
            foreach (Transform child in playerPoolContainer)
                Destroy(child.gameObject);

            if (currentTeam?.Roster == null) return;

            var filteredPlayers = FilterAndSortPlayers(currentTeam.Roster);
            
            foreach (var player in filteredPlayers)
            {
                var card = Instantiate(playerCardPrefab, playerPoolContainer);
                var component = card.GetComponent<PlayerCardUI>();
                component.SetPlayer(player);
                component.SetSelectionCallback(() => TogglePlayerSelection(player));
                
                // Highlight if selected for best 22
                var isSelected = selectedBest22.Contains(player);
                component.SetSelected(isSelected);
            }
        }

        private void UpdateBest22Display()
        {
            // Clear existing cards
            foreach (Transform child in best22Container)
                Destroy(child.gameObject);

            var sortedBest22 = SortBest22ByPosition(selectedBest22);
            
            foreach (var player in sortedBest22)
            {
                var card = Instantiate(playerCardPrefab, best22Container);
                var component = card.GetComponent<PlayerCardUI>();
                component.SetPlayer(player);
                component.SetSelectionCallback(() => RemoveFromBest22(player));
                component.SetSelected(true);
            }
            
            // Show count
            var countText = best22Container.parent.GetComponentInChildren<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = $"Selected: {selectedBest22.Count}/22";
            }
        }

        private List<Player> FilterAndSortPlayers(List<Player> roster)
        {
            var filtered = roster.AsEnumerable();
            
            // Apply position filter
            var selectedPosition = positionFilterDropdown.value;
            if (selectedPosition > 0) // 0 = "All Positions"
            {
                var targetGroup = (PositionGroup)(selectedPosition - 1);
                filtered = filtered.Where(p => 
                {
                    var coreRole = PlayerModelBridge.ToCore(p.Role);
                    return PositionUtils.GetPositionGroup(coreRole) == targetGroup;
                });
            }
            
            // Apply search filter
            var searchTerm = searchInput.text?.ToLower();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filtered = filtered.Where(p => p.Name.ToLower().Contains(searchTerm));
            }
            
            // Apply sorting
            var sortOption = sortingDropdown.value;
            switch (sortOption)
            {
                case 0: filtered = filtered.OrderBy(p => p.Name); break; // Name A-Z
                case 1: filtered = filtered.OrderByDescending(p => p.Stats.GetAverage()); break; // Overall Rating
                case 2: filtered = filtered.OrderBy(p => p.Age); break; // Age (Youngest first)
                case 3: filtered = filtered.OrderByDescending(p => p.Age); break; // Age (Oldest first)
                case 4: filtered = filtered.OrderBy(p => p.Role.ToString()); break; // Position
                default: filtered = filtered.OrderBy(p => p.Name); break;
            }
            
            return filtered.ToList();
        }

        private List<Player> SortBest22ByPosition(List<Player> players)
        {
            return players.OrderBy(p =>
            {
                var coreRole = PlayerModelBridge.ToCore(p.Role);
                var group = PositionUtils.GetPositionGroup(coreRole);
                return (int)group; // Sort by position group
            }).ThenBy(p => p.Role.ToString()).ToList();
        }

        private void TogglePlayerSelection(Player player)
        {
            if (selectedBest22.Contains(player))
            {
                RemoveFromBest22(player);
            }
            else
            {
                AddToBest22(player);
            }
        }

        private void AddToBest22(Player player)
        {
            if (selectedBest22.Count >= 22)
            {
                Debug.LogWarning("Cannot add more than 22 players to the team!");
                return;
            }
            
            selectedBest22.Add(player);
            UpdateBest22Display();
            UpdatePlayerPool(); // Refresh to show selection
        }

        private void RemoveFromBest22(Player player)
        {
            selectedBest22.Remove(player);
            UpdateBest22Display();
            UpdatePlayerPool(); // Refresh to show deselection
        }

        private void AutoSelectBest22()
        {
            if (currentTeam?.Roster == null) return;
            
            selectedBest22.Clear();
            
            // Use the position-aware auto selector
            var dummyOnField = new List<Player>();
            var dummyBench = new List<Player>();
            
            // Convert to Core players for selection
            var coreRoster = new List<AFLCoachSim.Core.Domain.Entities.Player>();
            int idCounter = 1;
            foreach (var unityPlayer in currentTeam.Roster)
            {
                coreRoster.Add(PlayerModelBridge.ToCore(unityPlayer, idCounter++));
            }
            
            // Use the AutoSelector logic - simplified version
            var bestPlayers = currentTeam.Roster
                .OrderByDescending(p => CalculateOverallEffectiveness(p))
                .Take(22)
                .ToList();
            
            selectedBest22.AddRange(bestPlayers);
            UpdateBest22Display();
        }

        private float CalculateOverallEffectiveness(Player player)
        {
            // Calculate how effective the player is in their assigned role
            var coreRole = PlayerModelBridge.ToCore(player.Role);
            var group = PositionUtils.GetPositionGroup(coreRole);
            
            switch (group)
            {
                case PositionGroup.Defense:
                    return player.Stats.Tackling * 0.3f + player.Stats.Knowledge * 0.25f + 
                           player.Stats.Kicking * 0.2f + player.Stats.Stamina * 0.15f + player.Stats.Speed * 0.1f;
                case PositionGroup.Midfield:
                    return player.Stats.Stamina * 0.25f + player.Stats.Playmaking * 0.2f + 
                           player.Stats.Handballing * 0.2f + player.Stats.Speed * 0.15f + 
                           player.Stats.Kicking * 0.1f + player.Stats.Tackling * 0.1f;
                case PositionGroup.Forward:
                    return player.Stats.Kicking * 0.3f + player.Stats.Speed * 0.25f + 
                           player.Stats.Playmaking * 0.2f + player.Stats.Handballing * 0.15f + player.Stats.Stamina * 0.1f;
                case PositionGroup.Ruck:
                    return player.Stats.Tackling * 0.25f + player.Stats.Knowledge * 0.2f + 
                           player.Stats.Stamina * 0.2f + player.Stats.Kicking * 0.15f + 
                           (100 - player.Stats.Speed) * 0.1f + player.Stats.Handballing * 0.1f;
                default:
                    return player.Stats.GetAverage();
            }
        }

        private void OptimizeTeam()
        {
            if (currentTeam?.Roster == null) return;
            
            // Implement smart optimization algorithm
            selectedBest22.Clear();
            
            var ideal = PositionUtils.GetIdealStructure();
            var roster = new List<Player>(currentTeam.Roster);
            
            // Select by position group to ensure balance
            AddBestByPositionGroup(roster, PositionGroup.Defense, ideal.Defenders);
            AddBestByPositionGroup(roster, PositionGroup.Midfield, ideal.Midfielders);
            AddBestByPositionGroup(roster, PositionGroup.Forward, ideal.Forwards);
            AddBestByPositionGroup(roster, PositionGroup.Ruck, ideal.Ruckmen);
            
            // Fill remaining spots with best available
            var remaining = 22 - selectedBest22.Count;
            if (remaining > 0)
            {
                var available = roster.Except(selectedBest22)
                    .OrderByDescending(p => p.Stats.GetAverage())
                    .Take(remaining);
                selectedBest22.AddRange(available);
            }
            
            UpdateBest22Display();
        }

        private void AddBestByPositionGroup(List<Player> roster, PositionGroup group, int count)
        {
            var candidates = roster.Where(p =>
            {
                var coreRole = PlayerModelBridge.ToCore(p.Role);
                return PositionUtils.GetPositionGroup(coreRole) == group;
            }).OrderByDescending(p => CalculateOverallEffectiveness(p))
              .Take(count);
            
            selectedBest22.AddRange(candidates);
        }

        private Color GetColorForBalance(float score)
        {
            if (score >= 80f) return Color.green;
            if (score >= 65f) return Color.yellow;
            if (score >= 50f) return Color.white;
            return Color.red;
        }

        private void SetupUI()
        {
            // Setup dropdown options
            SetupDropdowns();
            
            // Setup button listeners
            autoSelectButton.onClick.AddListener(AutoSelectBest22);
            optimizeButton.onClick.AddListener(OptimizeTeam);
            resetButton.onClick.AddListener(() => { selectedBest22.Clear(); UpdateBest22Display(); });
            
            generateRosterButton.onClick.AddListener(GenerateNewRoster);
            exportTeamButton.onClick.AddListener(ExportTeam);
            
            // Setup search/filter listeners
            positionFilterDropdown.onValueChanged.AddListener(_ => UpdatePlayerPool());
            sortingDropdown.onValueChanged.AddListener(_ => UpdatePlayerPool());
            searchInput.onValueChanged.AddListener(_ => UpdatePlayerPool());
        }

        private void SetupDropdowns()
        {
            // Position filter dropdown
            if (positionFilterDropdown != null)
            {
                positionFilterDropdown.ClearOptions();
                var positionOptions = new List<string> { "All Positions", "Defense", "Midfield", "Forward", "Ruck" };
                positionFilterDropdown.AddOptions(positionOptions);
            }
            
            // Sorting dropdown
            if (sortingDropdown != null)
            {
                sortingDropdown.ClearOptions();
                var sortOptions = new List<string> { "Name A-Z", "Overall Rating", "Age (Youngest)", "Age (Oldest)", "Position" };
                sortingDropdown.AddOptions(sortOptions);
            }
        }

        private void GenerateNewRoster()
        {
            if (currentTeam == null) return;
            
            // Generate a new balanced roster
            var newRoster = PositionalRosterGenerator.GenerateBalancedRoster(currentTeam.Level, 30);
            currentTeam.Roster = newRoster;
            
            selectedBest22.Clear();
            AnalyzeRoster(currentTeam);
            
            Debug.Log($"Generated new balanced roster for {currentTeam.Name}");
        }

        private void ExportTeam()
        {
            if (selectedBest22.Count != 22)
            {
                Debug.LogWarning("Please select exactly 22 players before exporting!");
                return;
            }
            
            // TODO: Implement team export functionality
            Debug.Log($"Exporting team: {string.Join(", ", selectedBest22.Select(p => p.Name))}");
        }

        [ContextMenu("Test with Current Team")]
        private void TestWithCurrentTeam()
        {
            if (currentTeam != null)
            {
                AnalyzeRoster(currentTeam);
            }
        }
    }
}
