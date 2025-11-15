// Assets/Scripts/Managers/MatchFlowManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using AFLManager.Models;
using AFLManager.Simulation;
using AFLManager.Managers;

namespace AFLManager.Managers
{
    /// <summary>
    /// Manages the complete match flow: pre-match -> simulation -> post-match
    /// </summary>
    public class MatchFlowManager : MonoBehaviour
    {
        [Header("Match Data")]
        public Match currentMatch;
        public List<Team> allTeams;
        public string playerTeamId;
        
        [Header("UI Screens")]
        [SerializeField] private GameObject preMatchScreen;
        [SerializeField] private GameObject simulationScreen;
        [SerializeField] private GameObject postMatchScreen;
        
        [Header("UI Components")]
        [SerializeField] private MatchPreviewUI preMatchUI;
        [SerializeField] private MatchSimulationUI simulationUI;
        [SerializeField] private MatchResultsUI resultsUI;
        
        private MatchResult currentResult;
        private string returnScene = "SeasonScreen";
        
        void Start()
        {
            LoadMatchData();
            ShowPreMatch();
        }
        
        private void LoadMatchData()
        {
            // Load match data from PlayerPrefs (set by calling scene)
            playerTeamId = PlayerPrefs.GetString("CurrentMatchPlayerTeam", "TEAM_001");
            returnScene = PlayerPrefs.GetString("MatchFlowReturnScene", "SeasonScreen");
            
            // Load match details (stored as JSON in PlayerPrefs)
            string matchJson = PlayerPrefs.GetString("CurrentMatchData", "");
            if (!string.IsNullOrEmpty(matchJson))
            {
                currentMatch = JsonUtility.FromJson<Match>(matchJson);
            }
            
            // Load all teams for roster info
            allTeams = LoadAllTeams();
        }
        
        private List<Team> LoadAllTeams()
        {
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
        
        public void ShowPreMatch()
        {
            SetActiveScreen(preMatchScreen);
            
            var homeTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.HomeTeamId);
            var awayTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.AwayTeamId);
            
            if (preMatchUI != null)
            {
                preMatchUI.Initialize(currentMatch, homeTeam, awayTeam, playerTeamId);
                preMatchUI.OnStartMatch = StartMatchSimulation;
            }
        }
        
        public void StartMatchSimulation()
        {
            SetActiveScreen(simulationScreen);
            
            if (simulationUI != null)
            {
                simulationUI.Initialize(currentMatch, playerTeamId);
                StartCoroutine(SimulateMatchAsync());
            }
        }
        
        private IEnumerator SimulateMatchAsync()
        {
            // Show simulation progress
            yield return new WaitForSeconds(0.5f);
            
            var homeTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.HomeTeamId);
            var awayTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.AwayTeamId);
            
            // Run the actual simulation
            var matchId = currentMatch.StableId(GetSeasonSchedule());
            // Use combination of match ID hash + current time to ensure unique simulations
            int seed = matchId.GetHashCode() ^ (int)(System.DateTime.Now.Ticks & 0xFFFFFFFF);
            currentResult = MatchSimulator.SimulateMatch(
                matchId,
                "R?",
                currentMatch.HomeTeamId,
                currentMatch.AwayTeamId,
                new MatchSimulator.DefaultRatingProvider(
                    id => GetTeamAverage(id),
                    id => GetPlayerIds(id)),
                seed: seed
            );
            
            // Update simulation UI with progress
            if (simulationUI != null)
            {
                yield return simulationUI.ShowSimulationProgress(currentResult);
            }
            
            // Save the result
            SaveLoadManager.SaveMatchResult(currentResult);
            
            // Update match result in schedule
            currentMatch.Result = $"{currentResult.HomeScore}–{currentResult.AwayScore}";
            
            yield return new WaitForSeconds(1f);
            
            ShowPostMatch();
        }
        
        public void ShowPostMatch()
        {
            SetActiveScreen(postMatchScreen);
            
            if (resultsUI != null)
            {
                var homeTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.HomeTeamId);
                var awayTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.AwayTeamId);
                
                resultsUI.Initialize(currentResult, currentMatch, homeTeam, awayTeam, playerTeamId);
                resultsUI.OnContinue = ReturnToSeason;
            }
        }
        
        public void ReturnToSeason()
        {
            SceneManager.LoadScene(returnScene);
        }
        
        private void SetActiveScreen(GameObject screen)
        {
            if (preMatchScreen) preMatchScreen.SetActive(screen == preMatchScreen);
            if (simulationScreen) simulationScreen.SetActive(screen == simulationScreen);
            if (postMatchScreen) postMatchScreen.SetActive(screen == postMatchScreen);
        }
        
        private float GetTeamAverage(string teamId)
        {
            var team = allTeams.FirstOrDefault(t => t.Id == teamId);
            if (team == null || team.Roster == null || team.Roster.Count == 0)
                return 60f;
            
            float sum = 0f;
            foreach (var p in team.Roster)
                sum += p?.Stats?.GetAverage() ?? 60f;
            
            return sum / team.Roster.Count;
        }
        
        private string[] GetPlayerIds(string teamId)
        {
            var team = allTeams.FirstOrDefault(t => t.Id == teamId);
            if (team?.Roster == null)
                return new[] { "P1", "P2", "P3", "P4", "P5", "P6" };
            
            return team.Roster.Take(6).Select(p => p?.Id ?? "P?").ToArray();
        }
        
        private SeasonSchedule GetSeasonSchedule()
        {
            return SaveLoadManager.LoadSchedule("testSeason");
        }
    }
}
