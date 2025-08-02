// File: Assets/Scripts/Demo/TeamDataDemo.cs
using UnityEngine;
using AFLManager.Models;
using AFLManager.Managers;
using AFLManager.Scriptables;

namespace AFLManager.Demo
{
    public class TeamDataDemo : MonoBehaviour
    {
        [Tooltip("Assign the TeamData ScriptableObject here")]
        public TeamData teamData;

        private Team teamModel;

        private void Start()
        {
            if (teamData == null)
            {
                Debug.LogError("TeamData asset not assigned.");
                return;
            }

            teamModel = teamData.ToModel();
            Debug.Log($"Loaded Team: {teamModel.Name}, Level: {teamModel.Level}, Roster Count: {teamModel.Roster.Count}");

            SaveLoadManager.SaveTeam(teamModel);
            Team loaded = SaveLoadManager.LoadTeam(teamModel.Id);
            if (loaded != null)
                Debug.Log($"Reloaded Team: {loaded.Name}, Budget: {loaded.Budget}, Players: {loaded.Roster.Count}");
            else
                Debug.LogError("Failed to reload team from disk.");
        }
    }
}
