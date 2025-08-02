// File: Assets/Scripts/Managers/ContractManager.cs
using System.Linq;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Managers
{
    public static class ContractManager
    {
        /// <summary>
        /// Assigns contract salaries to each player based on their average stat and the team's salary cap.
        /// Also assigns a random contract length.
        /// </summary>
        public static void AssignContracts(Team team)
        {
            if (team.Roster == null || team.Roster.Count == 0)
            {
                Debug.LogWarning("No players in roster to assign contracts.");
                return;
            }

            float totalCap = team.SalaryCap;
            // Compute total weight from each player's average stat
            float totalWeight = team.Roster.Sum(p => p.Stats.GetAverage());
            if (totalWeight <= 0f)
            {
                Debug.LogWarning("Total player stat average is zero; cannot assign salaries.");
                return;
            }

            foreach (var player in team.Roster)
            {
                float weight = player.Stats.GetAverage();
                // Proportional salary
                float salary = Mathf.Round((weight / totalWeight) * totalCap);
                player.Contract = new ContractDetails
                {
                    Salary = salary,
                    YearsRemaining = Random.Range(1, 6) // 1 to 5 years
                };
            }
        }

        /// <summary>
        /// Checks if the team's total salary is within the salary cap.
        /// </summary>
        public static bool IsUnderSalaryCap(Team team)
        {
            float totalSalaries = team.Roster.Sum(p => p.Contract?.Salary ?? 0f);
            return totalSalaries <= team.SalaryCap;
        }

        /// <summary>
        /// Returns the total salaries committed by the team.
        /// </summary>
        public static float GetTotalSalaryCommitment(Team team)
        {
            return team.Roster.Sum(p => p.Contract?.Salary ?? 0f);
        }
    }
}
