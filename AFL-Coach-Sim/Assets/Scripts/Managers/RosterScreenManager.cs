// File: Assets/Scripts/Managers/RosterScreenManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using AFLManager.Scriptables;
using AFLManager.Models;
using AFLManager.Managers;
using AFLManager.UI;

namespace AFLManager.Managers
{
    public class RosterScreenManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private TeamData defaultTeamData;  // assign your starter TeamData here
        [SerializeField] private int rosterSize = 22;

        [Header("UI References")]
        public GameObject playerEntryPrefab;
        public Transform rosterContainer;

        private Team teamModel;

        void Start()
        {
            LoadOrCreateTeam();
            RenderRoster();
        }

        private void LoadOrCreateTeam()
        {
            string coachKey = PlayerPrefs.GetString("CoachName", "DefaultCoach");
            teamModel = SaveLoadManager.LoadTeam(coachKey);

            if (teamModel == null)
            {
                if (defaultTeamData == null)
                {
                    Debug.LogError("DefaultTeamData not assigned on RosterScreenManager!");
                    return;
                }

                teamModel = defaultTeamData.ToModel();
                RosterManager.PopulateRoster(teamModel, rosterSize);
                ContractManager.AssignContracts(teamModel);
                SaveLoadManager.SaveTeam(coachKey, teamModel);
            }
        }

        private void RenderRoster()
        {
            foreach (Transform child in rosterContainer)
                Destroy(child.gameObject);

            foreach (var player in teamModel.Roster)
                Instantiate(playerEntryPrefab, rosterContainer)
                    .GetComponent<PlayerEntryUI>()
                    .SetData(player);
        }

        public void OnSeasonButton()
        {
            SceneManager.LoadScene("SeasonScreen");
        }
    }
}
