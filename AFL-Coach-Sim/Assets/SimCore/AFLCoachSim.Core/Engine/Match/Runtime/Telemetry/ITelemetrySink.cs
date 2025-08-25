namespace AFLCoachSim.Core.Engine.Match.Runtime.Telemetry
{
    public interface ITelemetrySink
    {
        // Called after each tick (or throttled interval)
        void OnTick(MatchSnapshot snapshot);
        // Called once at end of the match
        void OnComplete(MatchSnapshot finalSnapshot);
    }
}