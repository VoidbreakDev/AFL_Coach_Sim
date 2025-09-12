//File: Assets/Scripts/Data/LeagueConfigSO.cs
using System.Collections.Generic;
using UnityEngine;
using AFLCoachSim.Core.Data; // SimCore types (LeagueConfig, TeamConfig)

namespace AFLCoachSim.Unity.Data
{
    [CreateAssetMenu(menuName = "AFL Coach Sim/League Config", fileName = "LeagueConfig")]
    public class LeagueConfigSO : ScriptableObject
    {
        [Header("League")]
        public string LeagueName = "AFL";
        public bool DoubleRoundRobin = true;

        [Header("Teams")]
        public List<TeamEntry> Teams = new List<TeamEntry>();

        [System.Serializable]
        public class TeamEntry
        {
            public int Id;
            public string Name = "Team";
            [Range(0, 100)] public int Attack = 50;
            [Range(0, 100)] public int Defense = 50;
        }

        /// <summary>Convert to SimCore LeagueConfig for the engine.</summary>
        public LeagueConfig ToCore()
        {
            var cfg = new LeagueConfig
            {
                LeagueName = string.IsNullOrWhiteSpace(LeagueName) ? "League" : LeagueName,
                DoubleRoundRobin = DoubleRoundRobin,
                Teams = new List<TeamConfig>()
            };

            foreach (var t in Teams)
            {
                cfg.Teams.Add(new TeamConfig
                {
                    Id = t.Id,
                    Name = string.IsNullOrWhiteSpace(t.Name) ? $"Team {t.Id}" : t.Name,
                    Attack = Mathf.Clamp(t.Attack, 0, 100),
                    Defense = Mathf.Clamp(t.Defense, 0, 100)
                });
            }

            return cfg;
        }

        /// <summary>Basic validation for duplicates/ranges.</summary>
        public bool Validate(out string message)
        {
            // Non-empty team set
            if (Teams == null || Teams.Count == 0)
            {
                message = "No teams configured.";
                return false;
            }

            // Unique IDs
            var seen = new HashSet<int>();
            foreach (var t in Teams)
            {
                if (!seen.Add(t.Id))
                {
                    message = $"Duplicate Team Id detected: {t.Id}";
                    return false;
                }
            }

            // Names non-empty
            foreach (var t in Teams)
            {
                if (string.IsNullOrWhiteSpace(t.Name))
                {
                    message = $"Team with Id {t.Id} has an empty Name.";
                    return false;
                }
            }

            // Ratings in range
            foreach (var t in Teams)
            {
                if (t.Attack < 0 || t.Attack > 100 || t.Defense < 0 || t.Defense > 100)
                {
                    message = $"Team {t.Id} \"{t.Name}\" has ratings out of 0..100.";
                    return false;
                }
            }

            message = "OK";
            return true;
        }

        /// <summary>Ensure IDs are unique and compact (1..N), preserving order.</summary>
        public void ReassignSequentialIds()
        {
            for (int i = 0; i < Teams.Count; i++)
                Teams[i].Id = i + 1;
        }
    }
}