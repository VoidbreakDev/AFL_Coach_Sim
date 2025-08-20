using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Runtime
{
    public sealed class PlayerRuntime
    {
        public Player Player { get; private set; }
        public TeamId TeamId { get; private set; }
        public bool OnField { get; set; }

        // Running tallies (match)
        public int SecondsPlayed;
        public int SecondsSinceRotation;
        public bool InjuredOut;          // cannot return
        public int ReturnInSeconds;      // if >0, injury temporary; counts down on bench

        // Cached effective multipliers (updated by fatigue/injury)
        public float FatigueMult = 1f;   // 0.6..1.0 typical
        public float InjuryMult = 1f;    // 0.5..1.0

        public PlayerRuntime(Player p, TeamId teamId, bool onField)
        {
            Player = p;
            TeamId = teamId;
            OnField = onField;
        }
    }
}