// Domain/Aggregates/Team.cs
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Domain.Aggregates
{
    public sealed class Team
    {
        public TeamId Id { get; }
        public string Name { get; }
        public int Attack { get; }   // 0..100 simple starter ratings
        public int Defense { get; }

        public Team(TeamId id, string name, int attack, int defense)
        {
            Id = id; Name = name;
            Attack = attack; Defense = defense;
        }
    }
}