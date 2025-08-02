// File: Assets/Scripts/Managers/AIContentGenerator.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Managers
{
    public static class AIContentGenerator
    {
        private static readonly string[] FirstNames = { "Jack", "Liam", "Noah", "Ethan", "Oliver", "Lucas", "Mason", "Logan" };
        private static readonly string[] LastNames = { "Smith", "Jones", "Taylor", "Brown", "Williams", "Wilson", "Johnson", "Lee" };
        private static readonly string[] States = { "Victoria", "New South Wales", "Queensland", "South Australia", "Western Australia", "Tasmania", "Northern Territory", "Australian Capital Territory" };

        private static readonly Array Roles = Enum.GetValues(typeof(PlayerRole));

        /// <summary>
        /// Generate a random player for a given league level.
        /// </summary>
        public static Player GeneratePlayer(LeagueLevel level)
        {
            int age = GenerateAgeForLevel(level);
            var stats = PlayerStatsGenerator.GenerateStats(level, age);
            var player = new Player
            {
                Name = GenerateName(),
                Age = age,
                State = GenerateState(),
                History = GenerateHistory(level, age),
                Role = (PlayerRole)Roles.GetValue(UnityEngine.Random.Range(0, Roles.Length)),
                Stats = stats,
                PotentialCeiling = GeneratePotential(stats),
                Morale = UnityEngine.Random.Range(0.5f, 1f),
                Stamina = 0f,
                Portrait = null,
                Contract = new ContractDetails { Salary = 0f, YearsRemaining = 0 }
            };
            return player;
        }

        private static string GenerateName()
        {
            string first = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)];
            string last = LastNames[UnityEngine.Random.Range(0, LastNames.Length)];
            return $"{first} {last}";
        }

        private static string GenerateState()
        {
            return States[UnityEngine.Random.Range(0, States.Length)];
        }

        private static int GenerateAgeForLevel(LeagueLevel level)
        {
            int min = 14;
            int max = 34;
            switch (level)
            {
                case LeagueLevel.Local:
                case LeagueLevel.Regional:
                    min = 14; max = 35; break;
                case LeagueLevel.State:
                    min = 16; max = 30; break;
                case LeagueLevel.Interstate:
                case LeagueLevel.AFL:
                    min = 18; max = 34; break;
            }
            return UnityEngine.Random.Range(min, max + 1);
        }

        private static string GenerateHistory(LeagueLevel level, int age)
        {
            return level switch
            {
                LeagueLevel.Local => "Local club junior system",
                LeagueLevel.Regional => "Regional competition standout",
                LeagueLevel.State => "State league experience",
                LeagueLevel.Interstate => "Interstate series participant",
                LeagueLevel.AFL => "Professional AFL background",
                _ => "Unknown history"
            };
        }

        private static int GeneratePotential(PlayerStats stats)
        {
            float avg = (stats.Kicking + stats.Handballing + stats.Tackling + stats.Speed + stats.Stamina + stats.Knowledge + stats.Playmaking) / 7f;
            int ceiling = Mathf.Clamp(Mathf.RoundToInt(avg + UnityEngine.Random.Range(5f, 15f)), 0, 99);
            return ceiling;
        }
    }
}
