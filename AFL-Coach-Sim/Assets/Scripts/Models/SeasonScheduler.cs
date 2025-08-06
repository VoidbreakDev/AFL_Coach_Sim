// File: Assets/Scripts/Models/SeasonSchedule.cs
using System;                          
using System.Collections.Generic;      // for List<T>

namespace AFLManager.Models
{
    [Serializable]
    public class SeasonSchedule
    {
        public LeagueLevel Level;
        public List<Match> Fixtures;
        
        // Constructor to ensure Fixtures is never null
        public SeasonSchedule()
        {
            Fixtures = new List<Match>();
        }
    }
}
