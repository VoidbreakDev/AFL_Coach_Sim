// File: Assets/Scripts/Managers/RosterManager.cs
using System.Collections.Generic;
using AFLManager.Models;
using UnityEngine;

namespace AFLManager.Managers
{
    public static class RosterManager
    {
        /// <summary>
        /// Populates the team's roster to a specific size, replacing any existing players.
        /// </summary>
        public static void PopulateRoster(Team team, int targetSize)
        {
            team.Roster = new List<Player>();
            for (int i = 0; i < targetSize; i++)
            {
                var player = AIContentGenerator.GeneratePlayer(team.Level);
                team.Roster.Add(player);
            }
        }

        /// <summary>
        /// Fills the roster up to the target size, preserving existing players.
        /// </summary>
        public static void FillRoster(Team team, int targetSize)
        {
            if (team.Roster == null)
                team.Roster = new List<Player>();

            int toAdd = targetSize - team.Roster.Count;
            for (int i = 0; i < toAdd; i++)
            {
                var player = AIContentGenerator.GeneratePlayer(team.Level);
                team.Roster.Add(player);
            }
        }
    }
}
