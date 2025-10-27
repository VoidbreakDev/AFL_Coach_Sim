// Assets/Scripts/UI/MatchPreviewUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AFLManager.Models;
using System.Linq;

namespace AFLManager.Managers
{
    /// <summary>
    /// Pre-match screen showing lineups and team comparison
    /// </summary>
    public class MatchPreviewUI : MonoBehaviour
    {
        [Header("Match Info")]
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI venueText;
        [SerializeField] private TextMeshProUGUI dateText;
        
        [Header("Teams")]
        [SerializeField] private TextMeshProUGUI homeTeamName;
        [SerializeField] private TextMeshProUGUI awayTeamName;
        [SerializeField] private Image homeTeamLogo;
        [SerializeField] private Image awayTeamLogo;
        
        [Header("Team Stats Comparison")]
        [SerializeField] private TextMeshProUGUI homeRating;
        [SerializeField] private TextMeshProUGUI awayRating;
        [SerializeField] private Slider comparisonSlider;
        [SerializeField] private TextMeshProUGUI homeForm;
        [SerializeField] private TextMeshProUGUI awayForm;
        
        [Header("Lineup Lists")]
        [SerializeField] private Transform homeLineupContainer;
        [SerializeField] private Transform awayLineupContainer;
        [SerializeField] private GameObject playerLineupEntryPrefab;
        
        [Header("Controls")]
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Button backButton;
        
        private Match match;
        private string playerTeamId;
        
        public System.Action OnStartMatch;
        public System.Action OnBack;
        
        void Awake()
        {
            if (startMatchButton)
                startMatchButton.onClick.AddListener(() => OnStartMatch?.Invoke());
            
            if (backButton)
                backButton.onClick.AddListener(() => OnBack?.Invoke());
        }
        
        public void Initialize(Match matchData, Team homeTeam, Team awayTeam, string userTeamId)
        {
            match = matchData;
            playerTeamId = userTeamId;
            
            // Match info
            if (roundText) roundText.text = $"Round {GetRoundNumber()}";
            if (venueText) venueText.text = GetVenueName(homeTeam);
            if (dateText) dateText.text = matchData.FixtureDate.ToString("dddd, MMMM dd yyyy");
            
            // Team names
            if (homeTeamName) homeTeamName.text = homeTeam?.Name ?? matchData.HomeTeamId;
            if (awayTeamName) awayTeamName.text = awayTeam?.Name ?? matchData.AwayTeamId;
            
            // Team comparison
            float homeAvg = GetTeamRating(homeTeam);
            float awayAvg = GetTeamRating(awayTeam);
            
            if (homeRating) homeRating.text = $"{homeAvg:F1}";
            if (awayRating) awayRating.text = $"{awayAvg:F1}";
            
            if (comparisonSlider)
            {
                float total = homeAvg + awayAvg;
                comparisonSlider.value = total > 0 ? homeAvg / total : 0.5f;
            }
            
            // Form (placeholder - could be calculated from recent results)
            if (homeForm) homeForm.text = "WWLWD";
            if (awayForm) awayForm.text = "LWLWL";
            
            // Lineups
            DisplayLineup(homeTeam, homeLineupContainer);
            DisplayLineup(awayTeam, awayLineupContainer);
        }
        
        private void DisplayLineup(Team team, Transform container)
        {
            if (container == null || team?.Roster == null)
                return;
            
            // Clear existing
            foreach (Transform child in container)
                Destroy(child.gameObject);
            
            // Get starting 22 (or all players if less)
            var players = team.Roster.OrderByDescending(p => p.Stats?.GetAverage() ?? 0).Take(22).ToList();
            
            // Group by position
            var defenders = players.Where(p => IsDefender(p.Role)).ToList();
            var midfielders = players.Where(p => IsMidfielder(p.Role)).ToList();
            var forwards = players.Where(p => IsForward(p.Role)).ToList();
            var rucks = players.Where(p => IsRuck(p.Role)).ToList();
            
            AddPositionHeader(container, "Defenders");
            foreach (var player in defenders)
                AddPlayerEntry(container, player);
            
            AddPositionHeader(container, "Midfielders");
            foreach (var player in midfielders)
                AddPlayerEntry(container, player);
            
            AddPositionHeader(container, "Forwards");
            foreach (var player in forwards)
                AddPlayerEntry(container, player);
            
            AddPositionHeader(container, "Rucks");
            foreach (var player in rucks)
                AddPlayerEntry(container, player);
        }
        
        private void AddPositionHeader(Transform container, string positionName)
        {
            if (playerLineupEntryPrefab == null)
                return;
            
            var entry = Instantiate(playerLineupEntryPrefab, container);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text) text.text = $"<b>{positionName}</b>";
        }
        
        private void AddPlayerEntry(Transform container, Player player)
        {
            if (playerLineupEntryPrefab == null)
                return;
            
            var entry = Instantiate(playerLineupEntryPrefab, container);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text)
            {
                float rating = player.Stats?.GetAverage() ?? 0;
                text.text = $"{player.Name} - {rating:F0}";
            }
        }
        
        private float GetTeamRating(Team team)
        {
            if (team?.Roster == null || team.Roster.Count == 0)
                return 65f;
            
            float sum = 0f;
            foreach (var p in team.Roster)
                sum += p?.Stats?.GetAverage() ?? 65f;
            
            return sum / team.Roster.Count;
        }
        
        private int GetRoundNumber()
        {
            // Could be calculated from schedule position
            return 1;
        }
        
        private string GetVenueName(Team homeTeam)
        {
            return homeTeam?.Name != null ? $"{homeTeam.Name} Home Ground" : "MCG";
        }
        
        private bool IsDefender(PlayerRole role)
        {
            return role == PlayerRole.FullBack || role == PlayerRole.BackPocket || 
                   role == PlayerRole.HalfBack || role == PlayerRole.FullBackFlank ||
                   role == PlayerRole.HalfBackFlank || role == PlayerRole.CentreHalfBack;
        }
        
        private bool IsMidfielder(PlayerRole role)
        {
            return role == PlayerRole.Wing || role == PlayerRole.Centre || 
                   role == PlayerRole.RuckRover || role == PlayerRole.Rover;
        }
        
        private bool IsForward(PlayerRole role)
        {
            return role == PlayerRole.HalfForward || role == PlayerRole.HalfForwardFlank ||
                   role == PlayerRole.ForwardPocket || role == PlayerRole.FullForwardFlank ||
                   role == PlayerRole.CentreHalfForward || role == PlayerRole.FullForward;
        }
        
        private bool IsRuck(PlayerRole role)
        {
            return role == PlayerRole.Ruckman || role == PlayerRole.Ruck;
        }
    }
}
