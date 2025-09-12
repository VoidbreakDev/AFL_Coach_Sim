using System.Collections.Generic;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Domain.Aggregates; // Team
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Selection;
using AFLCoachSim.Core.Engine.Simulation; // DeterministicRandom
using AFLCoachSim.Core.Data; // TeamTactics
using AFLCoachSim.Core.Engine.Match.Runtime; // PlayerRuntime
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Rotations;
using AFLCoachSim.Core.Engine.Match.Injury;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Tuning;

namespace AFLCoachSim.Core.Engine.Match
{
    public static class MatchEngine
    {
        public static MatchResultDTO PlayMatch(
            int round,
            TeamId homeId,
            TeamId awayId,
            Dictionary<TeamId, Team> teams,
            Dictionary<TeamId, List<Domain.Entities.Player>> rosters = null,
            Dictionary<TeamId, TeamTactics> tactics = null,
            Weather weather = Weather.Clear,
            Ground ground = null,
            int quarterSeconds = 20 * 60,
            DeterministicRandom rng = null,
            MatchTuning tuning = null,
            ITelemetrySink sink = null)
        {
            if (rng == null) rng = new DeterministicRandom(12345);
            if (ground == null) ground = new Ground();

            var homeTeam = teams[homeId];
            var awayTeam = teams[awayId];

            var homeTactics = tactics != null && tactics.TryGetValue(homeId, out var ht) ? ht : new TeamTactics();
            var awayTactics = tactics != null && tactics.TryGetValue(awayId, out var at) ? at : new TeamTactics();

            var ctx = new MatchContext
            {
                Tuning = tuning ?? MatchTuning.Default, // Core stays Unity-free; provider lives on the Unity side
                Phase = Phase.CenterBounce,
                Quarter = 1,
                TimeRemaining = quarterSeconds,
                Weather = weather,
                Ground = ground,
                Home = new TeamState(homeId, homeTeam.Name, homeTactics),
                Away = new TeamState(awayId, awayTeam.Name, awayTactics),
                Ball = BallState.FromClearance(homeId),
                Rng = rng
            };

            // Select squads (22) from provided rosters
            var homeRoster = rosters != null && rosters.TryGetValue(homeId, out var hr) ? hr : new List<Domain.Entities.Player>();
            var awayRoster = rosters != null && rosters.TryGetValue(awayId, out var ar) ? ar : new List<Domain.Entities.Player>();
            AutoSelector.Select22(homeRoster, homeId, ctx.Home.OnField, ctx.Home.Bench);
            AutoSelector.Select22(awayRoster, awayId, ctx.Away.OnField, ctx.Away.Bench);

            // Build runtime squads
            SquadRuntimeBuilder.Build(ctx.Home.OnField, ctx.Home.Bench, ctx.Home.TeamId, out ctx.HomeOnField, out ctx.HomeBench);
            SquadRuntimeBuilder.Build(ctx.Away.OnField, ctx.Away.Bench, ctx.Away.TeamId, out ctx.AwayOnField, out ctx.AwayBench);

            // Models
            ctx.FatigueModel = new FatigueModel();
            ctx.RotationManager = new RotationManager();
            ctx.InjuryModel = new InjuryModel();

            // Four quarters
            for (int q = 1; q <= 4; q++)
            {
                ctx.Quarter = q;
                ctx.TimeRemaining = quarterSeconds;
                ctx.Phase = Phase.CenterBounce;

                while (ctx.TimeRemaining > 0)
                {
                    SimTick(ctx, 5, sink);
                    ctx.TimeRemaining -= 5;
                }
            }

            // Finalize match telemetry
            ctx.Telemetry.HomeAvgConditionEnd = AverageCondition(ctx.HomeOnField, ctx.HomeBench);
            ctx.Telemetry.AwayAvgConditionEnd = AverageCondition(ctx.AwayOnField, ctx.AwayBench);

            // Publish a final snapshot
            var final = MakeSnapshot(ctx);
            sink?.OnComplete(final);
            TelemetryHub.PublishComplete(final);

            return new MatchResultDTO
            {
                Round = round,
                Home = homeId,
                Away = awayId,
                HomeScore = ctx.Score.HomePoints,
                AwayScore = ctx.Score.AwayPoints
            };
        }

        private static void SimTick(MatchContext ctx, int dt, ITelemetrySink sink)
        {
            // M3 models
            ctx.FatigueModel.ApplyFatigue(ctx.HomeOnField, ctx.Phase, dt);
            ctx.FatigueModel.ApplyFatigue(ctx.AwayOnField, ctx.Phase, dt);

            bool swapped;
            if (ctx.RotationManager.MaybeRotate(ctx.HomeOnField, ctx.HomeBench, ctx.Home.Tactics, dt, out swapped) && swapped)
                ctx.Telemetry.HomeInterchanges++;
            if (ctx.RotationManager.MaybeRotate(ctx.AwayOnField, ctx.AwayBench, ctx.Away.Tactics, dt, out swapped) && swapped)
                ctx.Telemetry.AwayInterchanges++;

            int hinj = ctx.InjuryModel.Step(ctx.HomeOnField, ctx.HomeBench, ctx.Phase, dt, ctx.Rng,
                                ctx.Telemetry.HomeInjuryEvents, ctx.Tuning.InjuryMaxPerTeam, ctx.Tuning);
            int ainj = ctx.InjuryModel.Step(ctx.AwayOnField, ctx.AwayBench, ctx.Phase, dt, ctx.Rng,
                                ctx.Telemetry.AwayInjuryEvents, ctx.Tuning.InjuryMaxPerTeam, ctx.Tuning);
            ctx.Telemetry.HomeInjuryEvents += hinj;
            ctx.Telemetry.AwayInjuryEvents += ainj;

            // Phase logic
            switch (ctx.Phase)
            {
                case Phase.CenterBounce: ResolveCenterBounce(ctx); break;
                case Phase.Stoppage:     ResolveStoppage(ctx);     break;
                case Phase.OpenPlay:     ResolveOpenPlay(ctx);     break;
                case Phase.Inside50:     ResolveInside50(ctx);     break;
                case Phase.ShotOnGoal:   ResolveShot(ctx);         break;
                case Phase.KickIn:       ResolveKickIn(ctx);       break;
            }

            var snap = MakeSnapshot(ctx);
            sink?.OnTick(snap);
            TelemetryHub.Publish(snap);
        }

        // ---------- Phases ----------
        private static void ResolveCenterBounce(MatchContext ctx)
        {
            // Use runtime-aware midfield evaluation (fatigue/injury aware)
            float homeMid = Rating.MidfieldUnit(ctx.HomeOnField);
            float awayMid = Rating.MidfieldUnit(ctx.AwayOnField);
            float h = homeMid * (0.9f + 0.2f * (ctx.Home.Tactics.ContestBias / 100f));
            float a = awayMid * (0.9f + 0.2f * (ctx.Away.Tactics.ContestBias / 100f));
            h *= 1.03f; // slight HGA at bounce

            bool homeWins = ctx.Rng.NextFloat() < Rating.Softmax(h, a);
            ctx.Ball = BallState.FromClearance(homeWins ? ctx.Home.TeamId : ctx.Away.TeamId);
            ctx.Phase = Phase.OpenPlay;
        }

        private static void ResolveStoppage(MatchContext ctx)
        {
            float homeMid = Rating.MidfieldUnit(ctx.HomeOnField);
            float awayMid = Rating.MidfieldUnit(ctx.AwayOnField);
            float h = homeMid * (0.9f + 0.2f * (ctx.Home.Tactics.ContestBias / 100f));
            float a = awayMid * (0.9f + 0.2f * (ctx.Away.Tactics.ContestBias / 100f));

            bool homeWins = ctx.Rng.NextFloat() < Rating.Softmax(h, a);
            ctx.Ball = BallState.FromClearance(homeWins ? ctx.Home.TeamId : ctx.Away.TeamId);
            ctx.Phase = Phase.OpenPlay;
        }

        private static void ResolveOpenPlay(MatchContext ctx)
        {
            // Attacking/defending on-field groups (runtime)
            var atkOn = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.HomeOnField : ctx.AwayOnField;
            var defOn = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.AwayOnField : ctx.HomeOnField;

            // Quality vs pressure
            float attackQuality   = Rating.Inside50Quality(atkOn);
            float defensePressure = Rating.DefensePressure(defOn);

            // Weather penalty for progression
            float weatherPenalty = ctx.Weather == Weather.Windy     ? ctx.Tuning.WeatherProgressPenalty_Windy
                                 : ctx.Weather == Weather.LightRain ? ctx.Tuning.WeatherProgressPenalty_LightRain
                                 : ctx.Weather == Weather.HeavyRain ? ctx.Tuning.WeatherProgressPenalty_HeavyRain : 0f;

            float baseProgress = attackQuality - 0.6f * defensePressure - weatherPenalty;
            float pProgress    = Clamp01(ctx.Tuning.ProgressBase + baseProgress * ctx.Tuning.ProgressScale);

            float u = ctx.Rng.NextFloat();
            if (u < pProgress)
            {
                ctx.Ball.EnterF50();
                ctx.Phase = Phase.Inside50;
            }
            else
            {
                // Missed progression: mostly stoppage, sometimes turnover
                if (ctx.Rng.NextFloat() < 0.25f)
                {
                    var opp = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.Away.TeamId : ctx.Home.TeamId;
                    ctx.Ball.TurnoverTo(opp);
                    ctx.Phase = Phase.OpenPlay;
                }
                else
                {
                    ctx.Phase = Phase.Stoppage;
                }
            }
        }

        private static void ResolveInside50(MatchContext ctx)
        {
            // Attacking/defending on-field groups (runtime)
            var atkOn = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.HomeOnField : ctx.AwayOnField;
            var defOn = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.AwayOnField : ctx.HomeOnField;
            var attTactics = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.Home.Tactics : ctx.Away.Tactics;

            float attackQuality   = Rating.Inside50Quality(atkOn);
            float defensePressure = Rating.DefensePressure(defOn);

            // Chance to manufacture a shot from the entry
            float entryBias = 0.5f + 0.5f * (attTactics.KickingRisk / 100f);
            float chanceCreateShot = Clamp01(0.30f + 0.55f * (attackQuality - 0.5f) - 0.40f * (defensePressure - 0.5f));
            chanceCreateShot *= entryBias;

            float r = ctx.Rng.NextFloat();
            if (r < chanceCreateShot)
            {
                ctx.Phase = Phase.ShotOnGoal;
            }
            else if (r < chanceCreateShot + 0.15f)
            {
                ctx.Phase = Phase.Stoppage;
            }
            else
            {
                var opp = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.Away.TeamId : ctx.Home.TeamId;
                ctx.Ball.TurnoverTo(opp);
                ctx.Phase = Phase.OpenPlay;
            }
        }

        private static void ResolveShot(MatchContext ctx)
        {
            // Attacking/defending on-field groups (runtime)
            bool homePoss = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId);
            var atkOn = homePoss ? ctx.HomeOnField : ctx.AwayOnField;
            var defOn = homePoss ? ctx.AwayOnField : ctx.HomeOnField;

            // Offensive shot quality vs defensive pressure feeding accuracy
            float attackQuality   = Rating.Inside50Quality(atkOn);
            float defensePressure = Rating.DefensePressure(defOn);
            float baseAcc = attackQuality - 0.5f * defensePressure; // normalized later via tuning

            float weatherAccPenalty = ctx.Weather == Weather.Windy     ? ctx.Tuning.WeatherAccuracyPenalty_Windy
                                     : ctx.Weather == Weather.LightRain ? ctx.Tuning.WeatherAccuracyPenalty_LightRain
                                     : ctx.Weather == Weather.HeavyRain ? ctx.Tuning.WeatherAccuracyPenalty_HeavyRain : 0f;

            float pGoal = Clamp01(ctx.Tuning.ShotBaseGoal + ctx.Tuning.ShotScaleWithQual * baseAcc - weatherAccPenalty);

            var u = ctx.Rng.NextFloat();
            if (u < pGoal)
            {
                ctx.Score.AddGoal(homePoss);
                ctx.Phase = Phase.CenterBounce;
            }
            else if (u < pGoal + 0.35f)
            {
                ctx.Score.AddBehind(homePoss);
                // Opp kick-in
                var opp = homePoss ? ctx.Away.TeamId : ctx.Home.TeamId;
                ctx.Ball = BallState.FromClearance(opp, false);
                ctx.Phase = Phase.KickIn;
            }
            else
            {
                // Miss entirely: opp kick-in
                var opp = homePoss ? ctx.Away.TeamId : ctx.Home.TeamId;
                ctx.Ball = BallState.FromClearance(opp, false);
                ctx.Phase = Phase.KickIn;
            }
        }

        private static void ResolveKickIn(MatchContext ctx)
        {
            // Simple kick-in resolution: often resumes open play for the kicking team, sometimes immediate turnover
            var r = ctx.Rng.NextFloat();
            if (r < 0.60f)
            {
                ctx.Phase = Phase.OpenPlay;
            }
            else
            {
                var opp = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.Away.TeamId : ctx.Home.TeamId;
                ctx.Ball.TurnoverTo(opp);
                ctx.Phase = Phase.OpenPlay;
            }
        }

        // ---------- Helpers ----------
        private static int AverageCondition(
            System.Collections.Generic.IList<Runtime.PlayerRuntime> onField,
            System.Collections.Generic.IList<Runtime.PlayerRuntime> bench)
        {
            int sum = 0, cnt = 0;
            if (onField != null)
                for (int i = 0; i < onField.Count; i++) { sum += onField[i].Player.Condition; cnt++; }
            if (bench != null)
                for (int j = 0; j < bench.Count; j++) { sum += bench[j].Player.Condition; cnt++; }
            return cnt == 0 ? 0 : (sum / cnt);
        }

        private static MatchSnapshot MakeSnapshot(MatchContext ctx)
        {
            return new MatchSnapshot
            {
                Quarter = ctx.Quarter,
                TimeRemaining = ctx.TimeRemaining,
                Phase = ctx.Phase,
                HomeGoals = ctx.Score.HomeGoals,
                HomeBehinds = ctx.Score.HomeBehinds,
                HomePoints = ctx.Score.HomePoints,
                AwayGoals = ctx.Score.AwayGoals,
                AwayBehinds = ctx.Score.AwayBehinds,
                AwayPoints = ctx.Score.AwayPoints,
                HomeInterchanges = ctx.Telemetry.HomeInterchanges,
                AwayInterchanges = ctx.Telemetry.AwayInterchanges,
                HomeInjuryEvents = ctx.Telemetry.HomeInjuryEvents,
                AwayInjuryEvents = ctx.Telemetry.AwayInjuryEvents,
                HomeAvgConditionEnd = ctx.Telemetry.HomeAvgConditionEnd,
                AwayAvgConditionEnd = ctx.Telemetry.AwayAvgConditionEnd
            };
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}