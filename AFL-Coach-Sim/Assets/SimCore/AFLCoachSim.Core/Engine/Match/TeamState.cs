using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Data;

namespace AFLCoachSim.Core.Engine.Match
{
    public sealed class TeamState
    {
        public TeamId TeamId { get; }
        public string TeamName { get; }
        public TeamTactics Tactics { get; }
        public List<Player> OnField { get; } = new(22);
        public List<Player> Bench { get; } = new(4);
        public int InterchangesUsed { get; set; }

        public TeamState(TeamId id, string name, TeamTactics tactics)
        { TeamId = id; TeamName = name; Tactics = tactics; }
    }
}