using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Simulation; // DeterministicRandom
using System.Collections.Generic;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Rotations;
using AFLCoachSim.Core.Engine.Match.Injury;
using AFLCoachSim.Core.Engine.Match.Tuning;

namespace AFLCoachSim.Core.Engine.Match
{
    public sealed class MatchContext
    {
        public MatchTuning Tuning;
        public Phase Phase = Phase.CenterBounce;
        public Score Score = new Score();
        public int Quarter = 1;
        public int TimeRemaining; // seconds in quarter
        public Weather Weather = Weather.Clear;
        public Ground Ground = new Ground();
        public TeamState Home, Away;
        public BallState Ball;
        public DeterministicRandom Rng;
        public TeamId LeadingTeamId => Score.HomePoints >= Score.AwayPoints ? Home.TeamId : Away.TeamId;

        // Runtime squads
        public List<PlayerRuntime> HomeOnField, HomeBench;
        public List<PlayerRuntime> AwayOnField, AwayBench;

        // Models
        public FatigueModel FatigueModel;
        public RotationManager RotationManager;
        public InjuryModel InjuryModel; // Modern injury model with unified injury system
        public Injury.MatchInjuryContextProvider InjuryContextProvider; // Provides match context to injury manager
        public MatchTelemetry Telemetry = new MatchTelemetry();
    }
}
