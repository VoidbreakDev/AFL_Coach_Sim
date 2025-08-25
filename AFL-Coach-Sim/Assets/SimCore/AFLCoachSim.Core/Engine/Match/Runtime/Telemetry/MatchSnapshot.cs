namespace AFLCoachSim.Core.Engine.Match.Runtime.Telemetry
{
    public sealed class MatchSnapshot
    {
        public int Quarter;
        public int TimeRemaining;  // seconds in current quarter
        public Phase Phase;

        public int HomeGoals, HomeBehinds, HomePoints;
        public int AwayGoals, AwayBehinds, AwayPoints;

        public int HomeInterchanges;
        public int AwayInterchanges;
        public int HomeInjuryEvents;
        public int AwayInjuryEvents;
        public int HomeAvgConditionEnd; // running estimate (or final on end)
        public int AwayAvgConditionEnd;
    }
}