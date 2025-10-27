// Assets/Scripts/Managers/TeamMainScreenManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AFLManager.Models;
using AFLManager.UI;
using AFLManager.Managers;    // SaveLoadManager, SeasonScheduler

namespace AFLManager.Managers
{
    public class TeamMainScreenManager : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] UpcomingMatchWidget upcoming;
        [SerializeField] LastResultWidget lastResult;
        [SerializeField] LadderMiniWidget ladderMini;
        [SerializeField] MoraleWidget morale;
        [SerializeField] InjuriesWidget injuries;
        [SerializeField] TrainingWidget training;
        [SerializeField] BudgetWidget budget;
        [SerializeField] ContractsWidget contracts;

        [Header("Nav (optional)")]
        [SerializeField] NavigationController nav;

        private string playerTeamId;
        private SeasonSchedule schedule;
        private List<Team> cachedTeams;

        void Start()
        {
            playerTeamId = PlayerPrefs.GetString("PlayerTeamId", "TEAM_001");
            cachedTeams = LoadAllTeams();

            schedule = SaveLoadManager.LoadSchedule("testSeason");
            if (schedule == null)
            {
                schedule = SeasonScheduler.GenerateSeason(cachedTeams, DateTime.Today, 7);
                SaveLoadManager.SaveSchedule("testSeason", schedule);
            }

            if (upcoming) upcoming.OnPlay = OnClickPlayNext;

            RefreshDashboard();
        }

        public void RefreshDashboard()
        {
            var results = SaveLoadManager.LoadAllResults();

            DashboardDataBuilder.BindUpcoming(upcoming, schedule, results, playerTeamId, cachedTeams.Count);
            DashboardDataBuilder.BindLastResult(lastResult, results, playerTeamId);
            DashboardDataBuilder.BindMiniLadder(ladderMini, results, cachedTeams);
            DashboardDataBuilder.BindMorale(morale, playerTeamId);
            DashboardDataBuilder.BindInjuries(injuries, playerTeamId);
            DashboardDataBuilder.BindTraining(training, playerTeamId);
            DashboardDataBuilder.BindBudget(budget, playerTeamId);
            DashboardDataBuilder.BindContracts(contracts, playerTeamId);
        }

        public void OnClickPlayNext()
        {
            var results = SaveLoadManager.LoadAllResults();
            var nextMatch = schedule.Fixtures
                .Where(m => m.InvolvesTeam(playerTeamId))
                .FirstOrDefault(m => !results.Any(r => r.MatchId == m.StableId(schedule)));

            if (nextMatch != null)
            {
                LaunchMatchFlow(nextMatch);
            }
            else
            {
                Debug.Log("[TeamMainScreen] No upcoming matches");
            }
        }

        private void LaunchMatchFlow(Match match)
        {
            // Store match data for MatchFlow scene
            PlayerPrefs.SetString("CurrentMatchData", JsonUtility.ToJson(match));
            PlayerPrefs.SetString("CurrentMatchPlayerTeam", playerTeamId);
            PlayerPrefs.SetString("MatchFlowReturnScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();

            // Load match flow scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("MatchFlow");
        }

        private List<Team> LoadAllTeams()
        {
            var teams = new List<Team>();
            var dir = Application.persistentDataPath;
            foreach (var file in Directory.GetFiles(dir, "team_*.json"))
            {
                var key = Path.GetFileNameWithoutExtension(file).Replace("team_", "");
                var t = SaveLoadManager.LoadTeam(key);
                if (t != null)
                {
                    t.Roster ??= new List<Player>();
                    teams.Add(t);
                }
            }
            return teams;
        }
    }
}
