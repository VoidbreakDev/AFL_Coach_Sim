// Data/LeagueConfig.cs
using System.Collections.Generic;

namespace AFLCoachSim.Core.Data
{
    public sealed class LeagueConfig
    {
        public string LeagueName { get; set; } = "AFL";
        public List<TeamConfig> Teams { get; set; } = new();
        public bool DoubleRoundRobin { get; set; } = true;
    }
}