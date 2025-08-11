// Assets/Scripts/Managers/TeamMainScreenManager.cs
using UnityEngine;
using AFLManager.Managers;  // SaveLoadManager
using AFLManager.Models;
using System.Linq;

public class TeamMainScreenManager : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] UpcomingMatchWidget upcoming;
    [SerializeField] LastResultWidget lastResult;
    [SerializeField] LadderMiniWidget ladderMini;
    [SerializeField] MoraleWidget morale;
    [SerializeField] InjuriesWidget injuries;
    [SerializeField] TrainingWidget training;
    [SerializeField] BudgetWidget budget;
    [SerializeField] ContractsWidget contracts;

    [Header("Nav")]
    [SerializeField] NavigationController nav;

    string playerTeamId;
    SeasonSchedule schedule;

    void Start()
    {
        playerTeamId = PlayerPrefs.GetString("PlayerTeamId", "TEAM_001");
        schedule = SaveLoadManager.LoadSchedule("testSeason");
        if (schedule == null)
        {
            var allTeams = SaveLoadManager.LoadAllTeams();
            schedule = SeasonScheduler.GenerateSeason(allTeams, System.DateTime.Today, 7);
            SaveLoadManager.SaveSchedule("testSeason", schedule);
        }
        RefreshDashboard();
    }

    public void RefreshDashboard()
    {
        var results = SaveLoadManager.LoadAllResults();

        DashboardDataBuilder.BindUpcoming(upcoming, schedule, results, playerTeamId);
        DashboardDataBuilder.BindLastResult(lastResult, results, playerTeamId);
        DashboardDataBuilder.BindMiniLadder(ladderMini, results, SaveLoadManager.LoadAllTeams());
        DashboardDataBuilder.BindMorale(morale, playerTeamId);
        DashboardDataBuilder.BindInjuries(injuries, playerTeamId);
        DashboardDataBuilder.BindTraining(training, playerTeamId);
        DashboardDataBuilder.BindBudget(budget, playerTeamId);
        DashboardDataBuilder.BindContracts(contracts, playerTeamId);
    }

    // Called by UpcomingMatchWidget "Play" button
    public void OnClickPlayNext()
    {
        PreMatchFlow.Begin(this, playerTeamId, schedule);
    }
}
