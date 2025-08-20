using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match
{
    public sealed class BallState
    {
        public TeamId PossessionTeam { get; private set; }
        public bool InForward50 { get; private set; } // simple flag

        public static BallState FromClearance(TeamId team, bool inF50 = false)
            => new BallState { PossessionTeam = team, InForward50 = inF50 };

        public void TurnoverTo(TeamId opponent) { PossessionTeam = opponent; InForward50 = false; }
        public void EnterF50() { InForward50 = true; }
        public void ExitF50() { InForward50 = false; }
    }
}