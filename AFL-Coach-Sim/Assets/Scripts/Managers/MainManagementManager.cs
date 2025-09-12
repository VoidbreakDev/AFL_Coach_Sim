using UnityEngine;
using UnityEngine.UI;
using AFLManager.UI;
using AFLManager.Models;
using AFLManager.Scriptables;
using AFLCoachSim.Integration;
using AFLManager.Managers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AFLManager.Managers
{
    /// <summary>
    /// Central manager for the main management interface hub
    /// Coordinates between roster management, dashboard, and other game systems
    /// </summary>
    public class MainManagementManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ViewSwitcher contentSwitcher;
        [SerializeField] private SmartTeamBuilderUI teamBuilderUI;
        [SerializeField] private PlayerInspectorUI playerInspectorUI;
        
        [Header("Navigation")]
        [SerializeField] private ViewSwitchButton[] navigationButtons;
        [SerializeField] private Button dashboardNavButton;
        [SerializeField] private Button rosterNavButton;
        [SerializeField] private Button trainingNavButton;
        [SerializeField] private Button budgetNavButton;
        [SerializeField] private Button contractsNavButton;
        [SerializeField] private Button scheduleNavButton;
        [SerializeField] private Button tradeMarketNavButton;
        [SerializeField] private Button clubNavButton;
        [SerializeField] private Button inboxNavButton;
        
        [Header("Dashboard Widgets")]
        [SerializeField] private BudgetWidget budgetWidget;
        [SerializeField] private ContractsWidget contractsWidget;
        [SerializeField] private InjuriesWidget injuriesWidget;
        [SerializeField] private TrainingWidget trainingWidget;
        [SerializeField] private LadderMiniWidget ladderWidget;
        [SerializeField] private UpcomingMatchWidget upcomingMatchWidget;
        [SerializeField] private LastResultWidget lastResultWidget;
        
        [Header("Top Bar")]
        [SerializeField] private TMPro.TextMeshProUGUI clubNameText;
        [SerializeField] private TMPro.TextMeshProUGUI seasonInfoText;
        [SerializeField] private TMPro.TextMeshProUGUI quickBudgetText;
        [SerializeField] private TMPro.TextMeshProUGUI quickMoraleText;
        [SerializeField] private Image clubLogoImage;
        
        [Header("Overlay Systems")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private GameObject playerInspectorDialog;
        [SerializeField] private GameObject loadingScreen;
        
        // Runtime data
        private string playerTeamId;
        private AFLManager.Scriptables.TeamData currentTeamData;
        private List<AFLManager.Scriptables.PlayerData> currentRoster;
        
        // Events
        public System.Action<string> OnSectionChanged;
        public System.Action<AFLManager.Scriptables.PlayerData> OnPlayerDataUpdated;
        
        void Start()
        {
            InitializeManager();
            LoadTeamData();
            SetupNavigation();
            InitializeUI();
            ShowDashboard();
        }
        
        private void InitializeManager()
        {
            playerTeamId = PlayerPrefs.GetString("PlayerTeamId", "TEAM_001");
            
            // Ensure overlay canvas is properly configured
            if (overlayCanvas != null)
            {
                overlayCanvas.sortingOrder = 100;
                overlayCanvas.gameObject.SetActive(true);
            }
        }
        
        private void LoadTeamData()
        {
            ShowLoading(true);
            
            try
            {
                // Load team data using existing save system
                var team = SaveLoadManager.LoadTeam(playerTeamId);
                if (team != null)
                {
                    currentTeamData = ConvertTeamToTeamData(team);
                    currentRoster = ConvertRosterToPlayerData(team.Roster);
                }
                else
                {
                    Debug.LogWarning($"Could not load team data for {playerTeamId}");
                    // Create default team data or show error
                    CreateDefaultTeamData();
                }
                
                UpdateTopBarInfo();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading team data: {e.Message}");
                CreateDefaultTeamData();
            }
            finally
            {
                ShowLoading(false);
            }
        }
        
        private void SetupNavigation()
        {
            // Setup individual navigation buttons
            if (dashboardNavButton != null)
                dashboardNavButton.onClick.AddListener(() => ShowSection("Dashboard"));
            if (rosterNavButton != null)
                rosterNavButton.onClick.AddListener(() => ShowSection("Roster"));
            if (trainingNavButton != null)
                trainingNavButton.onClick.AddListener(() => ShowSection("Training"));
            if (budgetNavButton != null)
                budgetNavButton.onClick.AddListener(() => ShowSection("Budget"));
            if (contractsNavButton != null)
                contractsNavButton.onClick.AddListener(() => ShowSection("Contracts"));
            if (scheduleNavButton != null)
                scheduleNavButton.onClick.AddListener(() => ShowSection("Schedule"));
            if (tradeMarketNavButton != null)
                tradeMarketNavButton.onClick.AddListener(() => ShowSection("TradeMarket"));
            if (clubNavButton != null)
                clubNavButton.onClick.AddListener(() => ShowSection("Club"));
            if (inboxNavButton != null)
                inboxNavButton.onClick.AddListener(() => ShowSection("Inbox"));
            
            // Setup ViewSwitchButton array if used
            if (navigationButtons != null)
            {
                foreach (var button in navigationButtons)
                {
                    // ViewSwitchButton handles its own click events via Activate()
                    button.switcher = contentSwitcher;
                }
            }
        }
        
        private void InitializeUI()
        {
            // Initialize roster management UI
            // Note: SmartTeamBuilderUI doesn't have events in current implementation
            // Events would need to be added to the UI class if needed
            
            // Initialize player inspector
            // Note: PlayerInspectorUI doesn't have events in current implementation  
            // Events would need to be added to the UI class if needed
            
            // Hide overlay dialogs initially
            if (playerInspectorDialog != null)
                playerInspectorDialog.SetActive(false);
        }
        
        public void ShowSection(string sectionKey)
        {
            if (contentSwitcher != null)
            {
                contentSwitcher.Show(sectionKey);
                OnSectionChanged?.Invoke(sectionKey);
                
                // Refresh section-specific data
                RefreshSectionData(sectionKey);
            }
        }
        
        public void ShowDashboard()
        {
            ShowSection("Dashboard");
            RefreshDashboardData();
        }
        
        public void ShowRosterManagement()
        {
            ShowSection("Roster");
            if (teamBuilderUI != null && currentTeamData != null)
            {
                // Convert ScriptableObject TeamData back to Team model for UI
                var team = ConvertTeamDataToTeam(currentTeamData);
                teamBuilderUI.SetTeam(team);
            }
        }
        
        private void RefreshSectionData(string section)
        {
            switch (section)
            {
                case "Dashboard":
                    RefreshDashboardData();
                    break;
                case "Roster":
                    RefreshRosterData();
                    break;
                case "Budget":
                    RefreshBudgetData();
                    break;
                case "Contracts":
                    RefreshContractsData();
                    break;
                // Add other sections as needed
            }
        }
        
        private void RefreshDashboardData()
        {
            // Use existing DashboardDataBuilder pattern
            if (budgetWidget != null)
                DashboardDataBuilder.BindBudget(budgetWidget, playerTeamId);
            if (contractsWidget != null)
                DashboardDataBuilder.BindContracts(contractsWidget, playerTeamId);
            if (injuriesWidget != null)
                DashboardDataBuilder.BindInjuries(injuriesWidget, playerTeamId);
            if (trainingWidget != null)
                DashboardDataBuilder.BindTraining(trainingWidget, playerTeamId);
            
            // Load and bind other dashboard data
            var results = SaveLoadManager.LoadAllResults();
            var cachedTeams = LoadAllTeams();
            var schedule = SaveLoadManager.LoadSchedule("testSeason");
            
            if (ladderWidget != null && results != null && cachedTeams != null)
                DashboardDataBuilder.BindMiniLadder(ladderWidget, results, cachedTeams);
            if (upcomingMatchWidget != null && schedule != null)
                DashboardDataBuilder.BindUpcoming(upcomingMatchWidget, schedule, results, playerTeamId, cachedTeams?.Count ?? 18);
            if (lastResultWidget != null && results != null)
                DashboardDataBuilder.BindLastResult(lastResultWidget, results, playerTeamId);
        }
        
        private void RefreshRosterData()
        {
            if (teamBuilderUI != null && currentTeamData != null)
            {
                // Convert ScriptableObject TeamData back to Team model for UI
                var team = ConvertTeamDataToTeam(currentTeamData);
                teamBuilderUI.AnalyzeRoster(team);
            }
        }
        
        private void RefreshBudgetData()
        {
            if (budgetWidget != null)
                DashboardDataBuilder.BindBudget(budgetWidget, playerTeamId);
        }
        
        private void RefreshContractsData()
        {
            if (contractsWidget != null)
                DashboardDataBuilder.BindContracts(contractsWidget, playerTeamId);
        }
        
        public void ShowPlayerInspector(Player player)
        {
            if (playerInspectorUI != null && playerInspectorDialog != null)
            {
                playerInspectorUI.DisplayPlayer(player);
                playerInspectorDialog.SetActive(true);
            }
        }
        
        private void HidePlayerInspector()
        {
            if (playerInspectorDialog != null)
            {
                playerInspectorDialog.SetActive(false);
            }
        }
        
        // Event handlers removed - would need to be re-added if UI classes implement events
        // Currently SmartTeamBuilderUI and PlayerInspectorUI don't expose events
        
        private void SaveTeamChanges()
        {
            if (currentTeamData != null)
            {
                // Convert back to core Team model and save
                var coreTeam = ConvertTeamDataToTeam(currentTeamData);
                SaveLoadManager.SaveTeam(playerTeamId, coreTeam);
            }
        }
        
        private void UpdateTopBarInfo()
        {
            if (currentTeamData != null)
            {
                if (clubNameText != null)
                    clubNameText.text = currentTeamData.Name;
                
                // Get current season info
                var season = SaveLoadManager.LoadSchedule("testSeason");
                if (seasonInfoText != null && season != null)
                {
                    seasonInfoText.text = $"Season 2024 - Round {GetCurrentRound(season)}";
                }
                
                // Quick stats
                if (quickBudgetText != null)
                {
                    // Calculate total salary cap usage
                    var totalSalary = CalculateTotalSalary(currentRoster);
                    quickBudgetText.text = $"${totalSalary:N0}";
                }
                
                if (quickMoraleText != null)
                {
                    // Calculate team morale average
                    var avgMorale = CalculateTeamMorale(currentRoster);
                    quickMoraleText.text = $"{avgMorale:F1}";
                }
            }
        }
        
        private void ShowLoading(bool show)
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(show);
            }
        }
        
        private void CreateDefaultTeamData()
        {
            // Create a basic team if none exists
            currentTeamData = ScriptableObject.CreateInstance<AFLManager.Scriptables.TeamData>();
            currentTeamData.Id = playerTeamId;
            currentTeamData.Name = "Default Team";
            currentTeamData.Level = LeagueLevel.Local;
            currentTeamData.RosterData = new List<AFLManager.Scriptables.PlayerData>();
            currentRoster = new List<AFLManager.Scriptables.PlayerData>();
        }
        
        // Helper conversion methods
        private AFLManager.Scriptables.TeamData ConvertTeamToTeamData(Team team)
        {
            var teamData = ScriptableObject.CreateInstance<AFLManager.Scriptables.TeamData>();
            teamData.Id = team.Id;
            teamData.Name = team.Name;
            teamData.Level = team.Level;
            teamData.Budget = team.Budget;
            teamData.SalaryCap = team.SalaryCap;
            teamData.RosterData = ConvertRosterToPlayerData(team.Roster);
            return teamData;
        }
        
        private List<AFLManager.Scriptables.PlayerData> ConvertRosterToPlayerData(List<Player> roster)
        {
            var playerDataList = new List<AFLManager.Scriptables.PlayerData>();
            if (roster != null)
            {
                foreach (var player in roster)
                {
                    // Convert using direct mapping
                    var playerData = ScriptableObject.CreateInstance<AFLManager.Scriptables.PlayerData>();
                    playerData.Name = player.Name;
                    playerData.Age = player.Age;
                    playerData.State = player.State;
                    playerData.History = player.History;
                    playerData.Role = player.Role;
                    playerData.PotentialCeiling = (int)player.PotentialCeiling;
                    playerData.Morale = player.Morale;
                    playerData.Stamina = player.Stamina;
                    playerData.Stats = player.Stats;
                    playerData.Contract = player.Contract;
                    playerDataList.Add(playerData);
                }
            }
            return playerDataList;
        }
        
        private Team ConvertTeamDataToTeam(AFLManager.Scriptables.TeamData teamData)
        {
            var team = new Team();
            team.Name = teamData.Name;
            team.Level = teamData.Level;
            team.Budget = teamData.Budget;
            team.SalaryCap = teamData.SalaryCap;
            team.Roster = new List<Player>();
            
            if (teamData.RosterData != null)
            {
                foreach (var playerData in teamData.RosterData)
                {
                    // Convert PlayerData back to Player
                    var player = new Player();
                    player.Name = playerData.Name;
                    player.Age = playerData.Age;
                    player.State = playerData.State;
                    player.History = playerData.History;
                    player.Role = playerData.Role;
                    player.PotentialCeiling = playerData.PotentialCeiling;
                    player.Morale = playerData.Morale;
                    player.Stamina = playerData.Stamina;
                    player.Stats = playerData.Stats;
                    player.Contract = playerData.Contract;
                    team.Roster.Add(player);
                }
            }
            
            return team;
        }
        
        private List<Team> LoadAllTeams()
        {
            // Use existing team loading logic from TeamMainScreenManager
            var teams = new List<Team>();
            var dir = Application.persistentDataPath;
            var files = System.IO.Directory.GetFiles(dir, "team_*.json");
            
            foreach (var file in files)
            {
                var key = System.IO.Path.GetFileNameWithoutExtension(file).Replace("team_", "");
                var team = SaveLoadManager.LoadTeam(key);
                if (team != null)
                {
                    team.Roster = team.Roster ?? new List<Player>();
                    teams.Add(team);
                }
            }
            
            return teams;
        }
        
        private int GetCurrentRound(SeasonSchedule schedule)
        {
            if (schedule?.Fixtures != null)
            {
                var results = SaveLoadManager.LoadAllResults();
                var completedMatches = 0;
                
                foreach (var match in schedule.Fixtures)
                {
                    // Use a generated match ID since Match doesn't have MatchId property
                    var matchId = $"{match.HomeTeamId}_vs_{match.AwayTeamId}_{match.FixtureDate:yyyyMMdd}";
                    
                    // Check if this match exists in the results list
                    var hasResult = results?.Any(r => r.MatchId == matchId) == true;
                    if (hasResult)
                    {
                        completedMatches++;
                    }
                }
                
                // Estimate current round based on completed matches
                // Assuming roughly 9 matches per round in AFL (18 teams = 9 matches per round)
                return (completedMatches / 9) + 1;
            }
            
            return 1;
        }
        
        private int CalculateTotalSalary(List<AFLManager.Scriptables.PlayerData> roster)
        {
            var total = 0;
            if (roster != null)
            {
                foreach (var player in roster)
                {
                    // Assuming salary is stored in player data or can be calculated
                    total += player.Age * 10000; // Placeholder calculation
                }
            }
            return total;
        }
        
        private float CalculateTeamMorale(List<AFLManager.Scriptables.PlayerData> roster)
        {
            if (roster == null || roster.Count == 0) return 50f;
            
            // Calculate average morale (placeholder - would connect to actual morale system)
            var totalMorale = 0f;
            foreach (var player in roster)
            {
                // Placeholder morale calculation based on age and performance
                var playerMorale = 100f - (player.Age - 18) * 2f;
                totalMorale += Mathf.Clamp(playerMorale, 20f, 100f);
            }
            
            return totalMorale / roster.Count;
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions if any are added in the future
            // Currently no events are subscribed to
        }
    }
}
