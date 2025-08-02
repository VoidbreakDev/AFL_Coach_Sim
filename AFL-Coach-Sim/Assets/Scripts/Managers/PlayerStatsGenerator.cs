// File: Assets/Scripts/Managers/PlayerStatsGenerator.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using AFLManager.Models;

namespace AFLManager.Managers
{
    public static class PlayerStatsGenerator
    {
        private const int MaxStat = 99;
        private const int PeakLow = 23;
        private const int PeakHigh = 26;

        private class StatRange
        {
            public int Min;
            public int Max;
            public StatRange(int min, int max) { Min = min; Max = max; }
        }

        private static readonly Dictionary<LeagueLevel, StatRange> BaseRanges = new Dictionary<LeagueLevel, StatRange>
        {
            { LeagueLevel.Local, new StatRange(1, 20) },
            { LeagueLevel.Regional, new StatRange(15, 35) },
            { LeagueLevel.State, new StatRange(30, 55) },
            { LeagueLevel.Interstate, new StatRange(50, 75) },
            { LeagueLevel.AFL, new StatRange(70, 99) }
        };

        public static PlayerStats GenerateStats(LeagueLevel level, int age)
        {
            if (!IsEligibleForLeague(level, age))
                throw new ArgumentException($"Age {age} not eligible for league {level}");

            var range = BaseRanges[level];
            float factor = GetAgeFactor(age);
            var stats = new PlayerStats
            {
                Kicking = GenerateStat(range, factor),
                Handballing = GenerateStat(range, factor),
                Tackling = GenerateStat(range, factor),
                Speed = GenerateStat(range, factor),
                Stamina = GenerateStat(range, factor),
                Knowledge = GenerateStat(range, factor),
                Playmaking = GenerateStat(range, factor)
            };
            return stats;
        }

        public static bool IsEligibleForLeague(LeagueLevel level, int age)
        {
            switch (level)
            {
                case LeagueLevel.Local:
                case LeagueLevel.Regional:
                    return age >= 14;
                case LeagueLevel.State:
                    return age >= 16;
                case LeagueLevel.Interstate:
                case LeagueLevel.AFL:
                    return age >= 18;
                default:
                    return false;
            }
        }

        private static int GenerateStat(StatRange range, float factor)
        {
            int baseValue = UnityEngine.Random.Range(range.Min, Mathf.Min(range.Max, MaxStat) + 1);
            int boost = Mathf.RoundToInt((range.Max - range.Min) * factor * 0.2f);
            return Mathf.Clamp(baseValue + boost, range.Min, range.Max);
        }

        private static float GetAgeFactor(int age)
        {
            if (age < 14) return 0f;
            if (age < PeakLow)
            {
                // Linear from 0.2 at age 14 to 1.0 at PeakLow
                return Mathf.Lerp(0.2f, 1f, (age - 14f) / (PeakLow - 14f));
            }
            if (age <= PeakHigh) return 1f;
            if (age <= 30)
            {
                // Decline from 1.0 at PeakHigh to 0.6 at age 30
                return Mathf.Lerp(1f, 0.6f, (age - PeakHigh) / (30f - PeakHigh));
            }
            if (age <= 34)
            {
                // Decline from 0.6 at 30 to 0.3 at 34
                return Mathf.Lerp(0.6f, 0.3f, (age - 30f) / 4f);
            }
            // After 34
            return 0.2f;
        }     
    }
}
