// Domain/Aggregates/Match.cs
using AFLCoachSim.Core.Domain.ValueObjects;
using System;

namespace AFLCoachSim.Core.Domain.Aggregates
{
    public sealed class Match
    {
        public Guid MatchId { get; } = Guid.NewGuid();
        public TeamId Home { get; }
        public TeamId Away { get; }
        public int Round { get; }

        public Match(TeamId home, TeamId away, int round)
        {
            Home = home; Away = away; Round = round;
        }
    }
}