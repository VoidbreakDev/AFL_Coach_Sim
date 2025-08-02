// File: Assets/Scripts/Models/Match.cs
using System;                          // ← for DateTime

namespace AFLManager.Models
{
    public class Match
    {
        public string HomeTeamId;
        public string AwayTeamId;
        public DateTime FixtureDate;     // now recognized
        public string Result;            // e.g. "Home 75–68 Away"
    }
}
