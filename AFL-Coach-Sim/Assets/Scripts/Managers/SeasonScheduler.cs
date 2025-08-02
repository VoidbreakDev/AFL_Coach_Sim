// File: Assets/Scripts/Managers/SeasonScheduler.cs
using System;
using System.Collections.Generic;
using UnityEngine;                  // for Debug.LogWarning
using AFLManager.Models;

namespace AFLManager.Managers
{
    public static class SeasonScheduler
    {
        public static SeasonSchedule GenerateSeason(
            List<Team> teams,
            DateTime startDate,
            int daysBetweenMatches = 7)
        {
            // ← NEW GUARD TO PREVENT OUT-OF-RANGE
            if (teams == null || teams.Count < 2)
            {
                Debug.LogWarning(
                  "SeasonScheduler: Need at least 2 teams (found " 
                  + (teams?.Count ?? 0) + "). Returning empty schedule.");
                return new SeasonSchedule
                {
                    Level    = (teams != null && teams.Count > 0)
                                 ? teams[0].Level
                                 : LeagueLevel.Local,
                    Fixtures = new List<Match>()
                };
            }

            // Make working copy and add a BYE if odd count
            var temp = new List<Team>(teams);
            const string byeName = "BYE";
            if (temp.Count % 2 != 0)
            {
                var bye = new Team();
                bye.Name = byeName;
                temp.Add(bye);
            }

            int n             = temp.Count;
            int rounds        = n - 1;
            int perRound      = n / 2;
            var fixtures      = new List<Match>();

            for (int round = 0; round < rounds; round++)
            {
                DateTime date = startDate.AddDays(round * daysBetweenMatches);
                for (int i = 0; i < perRound; i++)
                {
                    var a = temp[i];
                    var b = temp[n - 1 - i];
                    if (a.Name != byeName && b.Name != byeName)
                    {
                        fixtures.Add(new Match
                        {
                            HomeTeamId  = a.Id,
                            AwayTeamId  = b.Id,
                            FixtureDate = date,
                            Result      = string.Empty
                        });
                    }
                }
                // rotate (fix index 0)
                var last = temp[n - 1];
                temp.RemoveAt(n - 1);
                temp.Insert(1, last);
            }

            return new SeasonSchedule
            {
                Level    = teams[0].Level,
                Fixtures = fixtures
            };
        }
    }
}
