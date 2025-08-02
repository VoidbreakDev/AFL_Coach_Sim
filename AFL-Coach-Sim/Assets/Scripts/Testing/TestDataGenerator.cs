// File: Assets/Scripts/Testing/TestDataGenerator.cs
using System.Collections.Generic;
using AFLManager.Models;
using AFLManager.Managers; // for RosterManager, ContractManager

namespace AFLManager.Testing
{
    public static class TestDataGenerator
    {
        /// <summary>
        /// Generates <teamCount> teams all at the same league level,
        /// each with a populated roster of <rosterSize> players.
        /// </summary>
        public static List<Team> GenerateTeams(
            LeagueLevel level,
            int teamCount,
            int rosterSize)
        {
            var teams = new List<Team>();
            for (int i = 1; i <= teamCount; i++)
            {
                var team = new Team
                {
                    Name       = $"{level} Team {i}",
                    Level      = level,
                    Budget     = GetBudgetForLevel(level),
                    SalaryCap  = GetSalaryCapForLevel(level),
                };

                // Fill with players & assign contracts
                RosterManager.PopulateRoster(team, rosterSize);
                ContractManager.AssignContracts(team);

                teams.Add(team);
            }
            return teams;
        }

        private static float GetBudgetForLevel(LeagueLevel level) => level switch
        {
            LeagueLevel.Local      => 10_000f,
            LeagueLevel.Regional   => 20_000f,
            LeagueLevel.State      => 50_000f,
            LeagueLevel.Interstate => 100_000f,
            LeagueLevel.AFL        => 500_000f,
            _                      => 10_000f
        };

        private static float GetSalaryCapForLevel(LeagueLevel level) => level switch
        {
            LeagueLevel.Local      => 15_000f,
            LeagueLevel.Regional   => 30_000f,
            LeagueLevel.State      => 60_000f,
            LeagueLevel.Interstate => 120_000f,
            LeagueLevel.AFL        => 1_000_000f,
            _                      => 15_000f
        };
    }
}
