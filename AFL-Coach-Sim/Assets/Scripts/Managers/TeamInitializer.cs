// File: Assets/Scripts/Managers/TeamInitializer.cs
using UnityEngine;
using AFLManager.Scriptables;
using AFLManager.Models;
using AFLManager.Managers;    // SaveLoadManager, RosterManager, ContractManager
using AFLManager.UI;          // ← add this

namespace AFLManager.Managers
{
    public class TeamInitializer : MonoBehaviour
    {
        public TeamData teamData;
        public int rosterSize = 22;
        public Transform rosterContainer;
        public GameObject playerEntryPrefab;

        private Team teamModel;

        void Start()
        {
            string coachKey = PlayerPrefs.GetString("CoachName", "DefaultCoach");
            teamModel = SaveLoadManager.LoadTeam(coachKey);

            if (teamModel == null)
            {
                teamModel = teamData.ToModel();
                RosterManager.PopulateRoster(teamModel, rosterSize);
                ContractManager.AssignContracts(teamModel);
                SaveLoadManager.SaveTeam(coachKey, teamModel);
            }

            RenderRoster();
        }

        void RenderRoster()
        {
            foreach (Transform child in rosterContainer)
                Destroy(child.gameObject);

            foreach (var player in teamModel.Roster)
            {
                var go = Instantiate(playerEntryPrefab, rosterContainer);
                go.GetComponent<PlayerEntryUI>().SetData(player);
            }
        }
    }
}
