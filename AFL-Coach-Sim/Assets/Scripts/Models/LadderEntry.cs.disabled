using System;

namespace AFLManager.Models
{
    [Serializable]
    public class LadderEntry
    {
        public string TeamId;
        public string TeamName;
        public int Games, Wins, Losses, Draws;
        public int Points, PointsFor, PointsAgainst;
        public double Percentage => PointsAgainst == 0 ? 0 : 100.0 * PointsFor / PointsAgainst;
    }
}
