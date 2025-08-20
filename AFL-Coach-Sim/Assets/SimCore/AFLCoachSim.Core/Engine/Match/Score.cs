namespace AFLCoachSim.Core.Engine.Match
{
    public sealed class Score
    {
        public int HomeGoals, HomeBehinds, AwayGoals, AwayBehinds;
        public int HomePoints => HomeGoals * 6 + HomeBehinds;
        public int AwayPoints => AwayGoals * 6 + AwayBehinds;
        public void AddGoal(bool home) { if (home) HomeGoals++; else AwayGoals++; }
        public void AddBehind(bool home) { if (home) HomeBehinds++; else AwayBehinds++; }
    }
}