using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Data
{
    public sealed class TeamRosterConfig
    {
        public int TeamId;
        public List<PlayerConfig> Players = new List<PlayerConfig>();

        public (TeamId, List<Player>) ToDomain()
        {
            var tid = new TeamId(TeamId);
            var roster = new List<Player>(Players.Count);
            foreach (var p in Players) roster.Add(p.ToDomain());
            return (tid, roster);
        }
    }
}