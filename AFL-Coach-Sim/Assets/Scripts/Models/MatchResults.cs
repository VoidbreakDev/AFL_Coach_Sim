using System;
using System.Collections.Generic;

namespace AFLManager.Models
{
    [Serializable]
    public class PlayerStatLine
    {
        public string PlayerId;
        public int Disposals;
        public int Goals;
        public int Tackles;
    }

    [Serializable]
    public class MatchResult
    {
        public string MatchId;        // Use whatever your Match model uses (e.g., GUID/string/int → ToString())
        public string RoundKey;       // e.g., "R1", "Finals_W1", etc.
        public string HomeTeamId;
        public string AwayTeamId;

        public int HomeScore;         // Total points
        public int AwayScore;

        public int HomeGoals;         // Optional, if you later want goals/behinds
        public int HomeBehinds;
        public int AwayGoals;
        public int AwayBehinds;

        public string BestOnGroundPlayerId; // Optional (can be null)
        public Dictionary<string, PlayerStatLine> PlayerStats = new Dictionary<string, PlayerStatLine>();
        public DateTime FixtureDate;  // When the match was played
        public DateTime SimulatedAtUtc;
        public bool IsFinal => HomeScore != AwayScore; // draw allowed, but still “finalised”
    }
}
