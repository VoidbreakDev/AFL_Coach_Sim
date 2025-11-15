// Assets/Scripts/Managers/AdvancedMatchFlowManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using AFLManager.Managers;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Commentary;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.DTO;

// Aliases to resolve ambiguous references
using UnityMatch = AFLManager.Models.Match;
using UnityTeam = AFLManager.Models.Team;
using UnityPlayer = AFLManager.Models.Player;
using UnityPlayerRole = AFLManager.Models.PlayerRole;
using MatchResult = AFLManager.Models.MatchResult;
using PlayerStatLine = AFLManager.Models.PlayerStatLine;
using SeasonSchedule = AFLManager.Models.SeasonSchedule;
using CoreMatch = AFLCoachSim.Core.Domain.Aggregates.Match;
using CoreTeam = AFLCoachSim.Core.Domain.Aggregates.Team;
using CorePlayer = AFLCoachSim.Core.Domain.Entities.Player;
using CorePlayerRole = AFLCoachSim.Core.Engine.Match.PlayerRole;

namespace AFLManager.Managers
{
    /// <summary>
    /// Enhanced match flow manager using the advanced MatchEngine with full telemetry
    /// </summary>
    public class AdvancedMatchFlowManager : MonoBehaviour
    {
        [Header("Match Data")]
        public UnityMatch currentMatch;
        public List<UnityTeam> allTeams;
        public string playerTeamId;
        
        [Header("UI Screens")]
        [SerializeField] private GameObject preMatchScreen;
        [SerializeField] private GameObject simulationScreen;
        [SerializeField] private GameObject postMatchScreen;
        
        [Header("UI Components")]
        [SerializeField] private MatchPreviewUI preMatchUI;
        [SerializeField] private MatchSimulationUI simulationUI;
        [SerializeField] private MatchResultsUI resultsUI;
        
        [Header("Simulation Settings")]
        [SerializeField] private int quarterSeconds = 1200; // 20 minutes per quarter
        [SerializeField] private bool useAdvancedEngine = true;
        
        private MatchResult currentResult;
        private CommentarySink commentarySink;
        private List<MatchSnapshot> matchSnapshots = new List<MatchSnapshot>();
        private List<string> capturedCommentary = new List<string>();
        private string returnScene = "SeasonScreen";
        
        void Start()
        {
            UnityEngine.Debug.Log("[AdvancedMatchFlow] Start() called");
            LoadMatchData();
            
            // Validate match data
            if (currentMatch == null || string.IsNullOrEmpty(currentMatch.HomeTeamId))
            {
                UnityEngine.Debug.LogError("[AdvancedMatchFlow] ERROR: No match data loaded! Did you load this scene from SeasonScreen?");
                UnityEngine.Debug.LogError("[AdvancedMatchFlow] HomeTeamId: " + (currentMatch?.HomeTeamId ?? "NULL"));
                UnityEngine.Debug.LogError("[AdvancedMatchFlow] AwayTeamId: " + (currentMatch?.AwayTeamId ?? "NULL"));
                return;
            }
            
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Match loaded: {currentMatch.HomeTeamId} vs {currentMatch.AwayTeamId}");
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Teams loaded: {allTeams?.Count ?? 0}");
            
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
                currentMatch = JsonUtility.FromJson<UnityMatch>(matchJson);
            }
            
            // Load all teams for roster info
            allTeams = LoadAllTeams();
        }
        
        private List<UnityTeam> LoadAllTeams()
        {
            var teams = new List<UnityTeam>();
            var dir = Application.persistentDataPath;
            var files = System.IO.Directory.GetFiles(dir, "team_*.json");
            
            foreach (var file in files)
            {
                var key = System.IO.Path.GetFileNameWithoutExtension(file).Replace("team_", "");
                var team = SaveLoadManager.LoadTeam(key);
                if (team != null)
                {
                    if (team.Roster == null)
                        team.Roster = new List<UnityPlayer>();
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
                StartCoroutine(SimulateMatchWithTelemetry());
            }
        }
        
        private IEnumerator SimulateMatchWithTelemetry()
        {
            yield return new WaitForSeconds(0.5f);
            
            var homeTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.HomeTeamId);
            var awayTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.AwayTeamId);
            
            if (useAdvancedEngine && homeTeam != null && awayTeam != null)
            {
                // Use advanced match engine with full telemetry
                yield return RunAdvancedSimulation(homeTeam, awayTeam);
            }
            else
            {
                // Fallback to simple simulation
                yield return RunSimpleSimulation();
            }
            
            // Save the result
            SaveLoadManager.SaveMatchResult(currentResult);
            
            // Update match result in schedule
            currentMatch.Result = $"{currentResult.HomeScore}–{currentResult.AwayScore}";
            
            yield return new WaitForSeconds(1f);
            
            ShowPostMatch();
        }
        
        private IEnumerator RunAdvancedSimulation(UnityTeam homeTeam, UnityTeam awayTeam)
        {
            // Prepare rosters
            var rosters = new Dictionary<TeamId, List<CorePlayer>>();
            var teams = new Dictionary<TeamId, CoreTeam>();
            var teamNames = new Dictionary<TeamId, string>();
            
            var homeId = new TeamId(homeTeam.Id.GetHashCode());
            var awayId = new TeamId(awayTeam.Id.GetHashCode());
            
            // Convert Unity Team/Player models to Core domain models
            rosters[homeId] = ConvertRosterToCoreModels(homeTeam.Roster);
            rosters[awayId] = ConvertRosterToCoreModels(awayTeam.Roster);
            
            teams[homeId] = ConvertTeamToCoreModel(homeTeam);
            teams[awayId] = ConvertTeamToCoreModel(awayTeam);
            
            teamNames[homeId] = homeTeam.Name;
            teamNames[awayId] = awayTeam.Name;
            
            // Set up commentary sink to capture telemetry
            // Use combination of match hash + current time to ensure unique simulations
            int seed = currentMatch.GetHashCode() ^ (int)(System.DateTime.Now.Ticks & 0xFFFFFFFF);
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Running simulation with seed: {seed}");
            var rng = new DeterministicRandom(seed);
            commentarySink = new CommentarySink(homeId, awayId, rosters, teamNames, rng);
            
            // Create injury manager (required by MatchEngine)
            var injuryManager = new InjuryManager(new MockInjuryRepository());
            
            // Run match simulation on background thread (async)
            MatchResultDTO result = null;
            bool simulationComplete = false;
            
            System.Threading.Tasks.Task.Run(() =>
            {
                result = MatchEngine.PlayMatch(
                    round: 1,
                    homeId: homeId,
                    awayId: awayId,
                    teams: teams,
                    injuryManager: injuryManager,
                    rosters: rosters,
                    quarterSeconds: quarterSeconds,
                    rng: rng,
                    sink: commentarySink
                );
                simulationComplete = true;
            });
            
            // Wait for simulation to complete
            while (!simulationComplete)
            {
                yield return null;
            }
            
            // Convert result to MatchResult format
            currentResult = ConvertToMatchResult(result, homeTeam, awayTeam);
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Simulation complete - Score: {currentResult.HomeScore}-{currentResult.AwayScore}");
            
            // Log telemetry
            UnityEngine.Debug.Log($"[MatchEngine] TELEMETRY:");
            UnityEngine.Debug.Log($"  Total Ticks: {result.TotalTicks}");
            UnityEngine.Debug.Log($"  Inside50 Entries: {result.Inside50Entries}");
            UnityEngine.Debug.Log($"  Shots: {result.Shots}");
            UnityEngine.Debug.Log($"  Goals: {result.Goals}, Behinds: {result.Behinds}");
            if (result.Shots > 0)
                UnityEngine.Debug.Log($"  Conversion Rate: {100.0 * result.Goals / result.Shots:F1}%");
            UnityEngine.Debug.Log($"  Shots per Inside50: {(result.Inside50Entries > 0 ? (double)result.Shots / result.Inside50Entries : 0):F2}");
            
            // Get commentary and snapshots for UI
            var commentary = commentarySink.CommentaryEvents;
            capturedCommentary = commentary; // Store for post-match highlights
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Commentary events captured: {commentary.Count}");
            matchSnapshots = new List<MatchSnapshot>(); // Would need to collect from telemetry hub
            
            // Show simulation progress with telemetry
            if (simulationUI != null)
            {
                if (commentary.Count > 0)
                {
                    // Use real commentary from the simulation (already strings)
                    yield return simulationUI.ShowSimulationProgress(matchSnapshots, commentary);
                }
                else
                {
                    // Fallback if no commentary generated
                    yield return simulationUI.ShowSimulationProgress(currentResult);
                }
            }
        }
        
        private IEnumerator RunSimpleSimulation()
        {
            // Fallback to simple statistical simulation
            var matchId = currentMatch.StableId(GetSeasonSchedule());
            currentResult = AFLManager.Simulation.MatchSimulator.SimulateMatch(
                matchId,
                "R?",
                currentMatch.HomeTeamId,
                currentMatch.AwayTeamId,
                new AFLManager.Simulation.MatchSimulator.DefaultRatingProvider(
                    id => GetTeamAverage(id),
                    id => GetPlayerIds(id)),
                seed: matchId.GetHashCode()
            );
            
            // Show simulation progress
            if (simulationUI != null)
            {
                yield return simulationUI.ShowSimulationProgress(currentResult);
            }
        }
        
        public void ShowPostMatch()
        {
            SetActiveScreen(postMatchScreen);
            
            if (resultsUI != null)
            {
                var homeTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.HomeTeamId);
                var awayTeam = allTeams.FirstOrDefault(t => t.Id == currentMatch.AwayTeamId);
                
                resultsUI.Initialize(currentResult, currentMatch, homeTeam, awayTeam, playerTeamId, capturedCommentary);
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
        
        // Conversion methods between Unity and Core domain models
        
        private List<CorePlayer> ConvertRosterToCoreModels(List<UnityPlayer> roster)
        {
            var coreRoster = new List<CorePlayer>();
            
            if (roster == null) return coreRoster;
            
            var roleCounts = new Dictionary<string, int>();
            
            foreach (var player in roster)
            {
                if (player == null) continue;
                
                var coreRole = ConvertRole(player.Role);
                var corePlayer = new CorePlayer
                {
                    Id = new PlayerId(player.Id.GetHashCode()),
                    Name = player.Name,
                    PrimaryRole = coreRole,
                    Age = player.Age
                };
                
                // Track role distribution
                string roleKey = coreRole.ToString();
                if (!roleCounts.ContainsKey(roleKey))
                    roleCounts[roleKey] = 0;
                roleCounts[roleKey]++;
                
                coreRoster.Add(corePlayer);
            }
            
            // Log role distribution
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Role distribution BEFORE adjustment: {string.Join(", ", roleCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            
            // Apply 6-6-6 AFL structure rebalancing
            RebalanceToAFLStructure(coreRoster);
            
            return coreRoster;
        }
        
        private void RebalanceToAFLStructure(List<CorePlayer> roster)
        {
            // Count players in each line
            int defenders = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.FB || 
                                               p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.HBF);
            int midfielders = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.MID || 
                                                 p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.C ||
                                                 p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.Tagger);
            int forwards = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.FF || 
                                              p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.HFF);
            int rucks = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.RUCK);
            
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Structure: DEF={defenders}, MID={midfielders}, FWD={forwards}, RUCK={rucks}");
            
            // Target: 6 defenders, 6 midfielders, 6 forwards, 4 rucks (22 total)
            var defPlayers = roster.Where(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.FB || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.HBF).ToList();
            var midPlayers = roster.Where(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.MID || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.C || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.Tagger).ToList();
            var fwdPlayers = roster.Where(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.FF || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.HFF).ToList();
            var ruckPlayers = roster.Where(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.RUCK).ToList();
            
            // Rebalance: Convert excess players to fill gaps
            // Priority: Keep 4 rucks, then balance def/mid/fwd to 6 each
            
            // Fix rucks: limit to 4, convert excess to midfielders
            while (ruckPlayers.Count > 4)
            {
                var player = ruckPlayers[ruckPlayers.Count - 1];
                player.PrimaryRole = AFLCoachSim.Core.Domain.ValueObjects.Role.MID;
                ruckPlayers.RemoveAt(ruckPlayers.Count - 1);
                midPlayers.Add(player);
            }
            
            // Ensure at least 6 forwards (take from excess defenders/mids)
            while (fwdPlayers.Count < 6 && (defPlayers.Count > 6 || midPlayers.Count > 6))
            {
                if (midPlayers.Count > 6)
                {
                    var player = midPlayers[midPlayers.Count - 1];
                    player.PrimaryRole = fwdPlayers.Count < 3 ? AFLCoachSim.Core.Domain.ValueObjects.Role.FF : AFLCoachSim.Core.Domain.ValueObjects.Role.HFF;
                    midPlayers.RemoveAt(midPlayers.Count - 1);
                    fwdPlayers.Add(player);
                }
                else if (defPlayers.Count > 6)
                {
                    var player = defPlayers[defPlayers.Count - 1];
                    player.PrimaryRole = fwdPlayers.Count < 3 ? AFLCoachSim.Core.Domain.ValueObjects.Role.FF : AFLCoachSim.Core.Domain.ValueObjects.Role.HFF;
                    defPlayers.RemoveAt(defPlayers.Count - 1);
                    fwdPlayers.Add(player);
                }
            }
            
            // Ensure at least 6 midfielders
            while (midPlayers.Count < 6 && defPlayers.Count > 6)
            {
                var player = defPlayers[defPlayers.Count - 1];
                player.PrimaryRole = AFLCoachSim.Core.Domain.ValueObjects.Role.MID;
                defPlayers.RemoveAt(defPlayers.Count - 1);
                midPlayers.Add(player);
            }
            
            // Ensure at least 6 defenders
            while (defPlayers.Count < 6 && midPlayers.Count > 6)
            {
                var player = midPlayers[midPlayers.Count - 1];
                player.PrimaryRole = defPlayers.Count < 3 ? AFLCoachSim.Core.Domain.ValueObjects.Role.FB : AFLCoachSim.Core.Domain.ValueObjects.Role.HBF;
                midPlayers.RemoveAt(midPlayers.Count - 1);
                defPlayers.Add(player);
            }
            
            // Final count
            int finalDef = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.FB || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.HBF);
            int finalMid = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.MID || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.C || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.Tagger);
            int finalFwd = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.FF || p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.HFF);
            int finalRuck = roster.Count(p => p.PrimaryRole == AFLCoachSim.Core.Domain.ValueObjects.Role.RUCK);
            
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] Rebalanced: DEF={finalDef}, MID={finalMid}, FWD={finalFwd}, RUCK={finalRuck}");
        }
        
        private CoreTeam ConvertTeamToCoreModel(UnityTeam team)
        {
            float attack = CalculateTeamAttack(team);
            float defense = CalculateTeamDefense(team);
            
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] {team.Name} - Attack: {attack:F1}, Defense: {defense:F1}");
            
            return new CoreTeam(
                id: new TeamId(team.Id.GetHashCode()),
                name: team.Name,
                attack: (int)attack,
                defense: (int)defense
            );
        }
        
        private float CalculateTeamAttack(UnityTeam team)
        {
            if (team.Roster == null || team.Roster.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"[AdvancedMatchFlow] {team.Name} has no roster! Using default attack 75");
                return 75f;
            }
            
            float total = 0f;
            int count = 0;
            foreach (var player in team.Roster)
            {
                if (player?.Stats != null)
                {
                    float playerAttack = (player.Stats.Kicking + player.Stats.Playmaking) / 2f;
                    total += playerAttack;
                    count++;
                }
            }
            
            if (count == 0)
            {
                UnityEngine.Debug.LogWarning($"[AdvancedMatchFlow] {team.Name} has {team.Roster.Count} players but none with stats!");
                return 75f;
            }
            
            float avgAttack = total / count;
            UnityEngine.Debug.Log($"[AdvancedMatchFlow] {team.Name} attack: {avgAttack:F1} (from {count} players)");
            return avgAttack;
        }
        
        private float CalculateTeamDefense(UnityTeam team)
        {
            if (team.Roster == null || team.Roster.Count == 0) return 75f;
            
            float total = 0f;
            int count = 0;
            foreach (var player in team.Roster)
            {
                if (player?.Stats != null)
                {
                    total += (player.Stats.Tackling + player.Stats.Stamina) / 2f;
                    count++;
                }
            }
            return count > 0 ? total / count : 75f;
        }
        
        private AFLCoachSim.Core.Domain.ValueObjects.Role ConvertRole(UnityPlayerRole unityRole)
        {
            // Map Unity PlayerRole to Core Role enum
            return unityRole switch
            {
                // Defenders
                UnityPlayerRole.FullBack => AFLCoachSim.Core.Domain.ValueObjects.Role.FB,
                UnityPlayerRole.BackPocket => AFLCoachSim.Core.Domain.ValueObjects.Role.FB,
                UnityPlayerRole.HalfBack => AFLCoachSim.Core.Domain.ValueObjects.Role.HBF,
                UnityPlayerRole.HalfBackFlank => AFLCoachSim.Core.Domain.ValueObjects.Role.HBF,
                UnityPlayerRole.FullBackFlank => AFLCoachSim.Core.Domain.ValueObjects.Role.HBF,
                UnityPlayerRole.CentreHalfBack => AFLCoachSim.Core.Domain.ValueObjects.Role.HBF,
                
                // Midfielders
                UnityPlayerRole.Centre => AFLCoachSim.Core.Domain.ValueObjects.Role.C,
                UnityPlayerRole.Wing => AFLCoachSim.Core.Domain.ValueObjects.Role.MID,
                UnityPlayerRole.Rover => AFLCoachSim.Core.Domain.ValueObjects.Role.MID,
                UnityPlayerRole.RuckRover => AFLCoachSim.Core.Domain.ValueObjects.Role.MID,
                
                // Rucks
                UnityPlayerRole.Ruckman => AFLCoachSim.Core.Domain.ValueObjects.Role.RUCK,
                UnityPlayerRole.Ruck => AFLCoachSim.Core.Domain.ValueObjects.Role.RUCK,
                
                // Forwards
                UnityPlayerRole.FullForward => AFLCoachSim.Core.Domain.ValueObjects.Role.FF,
                UnityPlayerRole.FullForwardFlank => AFLCoachSim.Core.Domain.ValueObjects.Role.FF,
                UnityPlayerRole.HalfForward => AFLCoachSim.Core.Domain.ValueObjects.Role.HFF,
                UnityPlayerRole.HalfForwardFlank => AFLCoachSim.Core.Domain.ValueObjects.Role.HFF,
                UnityPlayerRole.ForwardPocket => AFLCoachSim.Core.Domain.ValueObjects.Role.FF,
                UnityPlayerRole.CentreHalfForward => AFLCoachSim.Core.Domain.ValueObjects.Role.HFF,
                
                // Utility defaults to midfield
                UnityPlayerRole.Utility => AFLCoachSim.Core.Domain.ValueObjects.Role.MID,
                
                _ => AFLCoachSim.Core.Domain.ValueObjects.Role.MID
            };
        }
        
        private MatchResult ConvertToMatchResult(MatchResultDTO dto, UnityTeam homeTeam, UnityTeam awayTeam)
        {
            return new MatchResult
            {
                MatchId = $"{dto.Round}_{dto.Home}_{dto.Away}",
                RoundKey = $"R{dto.Round}",
                HomeTeamId = homeTeam.Id,
                AwayTeamId = awayTeam.Id,
                HomeScore = dto.HomeScore,
                AwayScore = dto.AwayScore,
                HomeGoals = dto.HomeScore / 6,
                HomeBehinds = dto.HomeScore % 6,
                AwayGoals = dto.AwayScore / 6,
                AwayBehinds = dto.AwayScore % 6,
                SimulatedAtUtc = System.DateTime.UtcNow,
                PlayerStats = new Dictionary<string, AFLManager.Models.PlayerStatLine>()
            };
        }
        
        /// <summary>
        /// Mock injury repository for compatibility
        /// </summary>
        private class MockInjuryRepository : AFLCoachSim.Core.Persistence.IInjuryRepository
        {
            public AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory LoadPlayerInjuryHistory(int playerId)
            {
                return new AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory(playerId);
            }
            
            public void SavePlayerInjuryHistory(AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory history)
            {
                // No-op for match simulation
            }
            
            public System.Collections.Generic.IDictionary<int, AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory> LoadAllPlayerInjuryHistories()
            {
                return new System.Collections.Generic.Dictionary<int, AFLCoachSim.Core.Injuries.Domain.PlayerInjuryHistory>();
            }
            
            public void RemovePlayerInjuryHistory(int playerId)
            {
                // No-op for match simulation
            }
            
            public void SaveInjury(AFLCoachSim.Core.Injuries.Domain.Injury injury)
            {
                // No-op for match simulation
            }
            
            public AFLCoachSim.Core.Injuries.Domain.Injury LoadInjury(AFLCoachSim.Core.Injuries.Domain.InjuryId injuryId)
            {
                return null;
            }
            
            public System.Collections.Generic.IEnumerable<AFLCoachSim.Core.Injuries.Domain.Injury> LoadPlayerInjuries(int playerId)
            {
                return new System.Collections.Generic.List<AFLCoachSim.Core.Injuries.Domain.Injury>();
            }
            
            public System.Collections.Generic.IEnumerable<AFLCoachSim.Core.Injuries.Domain.Injury> LoadActiveInjuries()
            {
                return new System.Collections.Generic.List<AFLCoachSim.Core.Injuries.Domain.Injury>();
            }
            
            public void SaveAllInjuryData(AFLCoachSim.Core.DTO.InjuryDataDTO injuryData)
            {
                // No-op for match simulation
            }
            
            public AFLCoachSim.Core.DTO.InjuryDataDTO LoadAllInjuryData()
            {
                return new AFLCoachSim.Core.DTO.InjuryDataDTO();
            }
            
            public void ClearAllInjuryData()
            {
                // No-op for match simulation
            }
            
            public void ClearPlayerInjuryData(int playerId)
            {
                // No-op for match simulation
            }
            
            public bool HasInjuryData()
            {
                return false;
            }
            
            public void BackupInjuryData(string backupSuffix)
            {
                // No-op for match simulation
            }
            
            public bool RestoreInjuryData(string backupSuffix)
            {
                return false;
            }
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
