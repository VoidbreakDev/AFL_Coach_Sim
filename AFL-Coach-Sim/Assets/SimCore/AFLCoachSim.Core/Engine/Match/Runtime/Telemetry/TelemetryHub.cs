using System;

namespace AFLCoachSim.Core.Engine.Match.Runtime.Telemetry
{
    public static class TelemetryHub
    {
        // Editor subscribes to this; game code just publishes.
        public static event Action<MatchSnapshot> OnSnapshot;
        public static event Action<MatchSnapshot> OnComplete;

        // Fast no-op if nobody is listening
        public static void Publish(MatchSnapshot snap) => OnSnapshot?.Invoke(snap);
        public static void PublishComplete(MatchSnapshot snap) => OnComplete?.Invoke(snap);
    }
}