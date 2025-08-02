// File: Assets/Scripts/Scriptables/TeamData.cs
using System.Collections.Generic;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Scriptables

{
    [CreateAssetMenu(
        fileName = "NewTeamData",
        menuName = "AFL Manager/Team Data",
        order = 1)]
    public class TeamData : ScriptableObject
    {
        public string Id;
        public string Name;
        public LeagueLevel Level;
        public float Budget;
        public float SalaryCap;
        public List<PlayerData> RosterData;

        public Team ToModel()
        {
            var team = new Team
            {
                Name = Name,
                Level = Level,
                Budget = Budget,
                SalaryCap = SalaryCap,
                // You may need to add these properties to TeamData if they exist:
                // CoachModifiers = CoachModifiers,
                // Record = Record,
                // Premierships = Premierships
            };
            team.Roster = new List<Player>();
            if (RosterData != null)
            {
                foreach (var pd in RosterData)
                    team.Roster.Add(pd.ToModel());
            }
            return team;
        }
    }
}
