// Core/Engine/Match/Runtime/MatchTelemetry.cs
namespace AFLCoachSim.Core.Engine.Match.Runtime
{
    public sealed class MatchTelemetry
    {
        public int HomeInterchanges;
        public int AwayInterchanges;
        public int HomeInjuryEvents;
        public int AwayInjuryEvents;
        public int HomeAvgConditionEnd;
        public int AwayAvgConditionEnd;
        
        // Additional telemetry for compatibility
        public System.Collections.Generic.List<string> Events { get; set; } = new System.Collections.Generic.List<string>();
    }
}