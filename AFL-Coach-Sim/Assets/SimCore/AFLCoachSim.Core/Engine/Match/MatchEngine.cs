using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Domain.Aggregates; // Team
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Selection;
using AFLCoachSim.Core.Engine.Simulation; // DeterministicRandom
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Engine.Match.Runtime;
using AFLCoachSim.Core.Engine.Match.Fatigue;
using AFLCoachSim.Core.Engine.Match.Rotations;
using AFLCoachSim.Core.Engine.Match.Injury;
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
            DeterministicRandom rng = null)
        {
            rng ??= new DeterministicRandom(12345);
            ground ??= new Ground();

            var homeTeam = teams[homeId];
            var awayTeam = teams[awayId];

            var homeTactics = tactics != null && tactics.TryGetValue(homeId, out var ht) ? ht : new TeamTactics();
            var awayTactics = tactics != null && tactics.TryGetValue(awayId, out var at) ? at : new TeamTactics();

            var ctx = new MatchContext
            {
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

            var homeRoster = rosters != null && rosters.TryGetValue(homeId, out var hr) ? hr : new List<Domain.Entities.Player>();
            var awayRoster = rosters != null && rosters.TryGetValue(awayId, out var ar) ? ar : new List<Domain.Entities.Player>();
            AutoSelector.Select22(homeRoster, homeId, ctx.Home.OnField, ctx.Home.Bench);
            AutoSelector.Select22(awayRoster, awayId, ctx.Away.OnField, ctx.Away.Bench);

            // Build runtime squads
            SquadRuntimeBuilder.Build(ctx.Home.OnField, ctx.Home.Bench, ctx.Home.TeamId, out ctx.HomeOnField, out ctx.HomeBench);
            SquadRuntimeBuilder.Build(ctx.Away.OnField, ctx.Away.Bench, ctx.Away.TeamId, out ctx.AwayOnField, out ctx.AwayBench);

            // Models
            ctx.FatigueModel   = new FatigueModel();
            ctx.RotationManager = new RotationManager();
            ctx.InjuryModel     = new InjuryModel();

            for (int q = 1; q <= 4; q++)
            {
                ctx.Quarter = q;
                ctx.TimeRemaining = quarterSeconds;
                ctx.Phase = Phase.CenterBounce;

                while (ctx.TimeRemaining > 0)
                {
                    SimTick(ctx, 5);
                    ctx.TimeRemaining -= 5;
                }
            }
            // Finalize match telemetry
            ctx.Telemetry.HomeAvgConditionEnd = AverageCondition(ctx.HomeOnField, ctx.HomeBench);
            ctx.Telemetry.AwayAvgConditionEnd = AverageCondition(ctx.AwayOnField, ctx.AwayBench);

            return new MatchResultDTO
            {
                Round = round,
                Home = homeId,
                Away = awayId,
                HomeScore = ctx.Score.HomePoints,
                AwayScore = ctx.Score.AwayPoints
            };
        }

            private static void SimTick(MatchContext ctx, int dt)
            {
                // M3 models
                ctx.FatigueModel.ApplyFatigue(ctx.HomeOnField, ctx.Phase, dt);
                ctx.FatigueModel.ApplyFatigue(ctx.AwayOnField, ctx.Phase, dt);

                bool swapped;
                if (ctx.RotationManager.MaybeRotate(ctx.HomeOnField, ctx.HomeBench, ctx.Home.Tactics, dt, out swapped) && swapped)
                    ctx.Telemetry.HomeInterchanges++;
                if (ctx.RotationManager.MaybeRotate(ctx.AwayOnField, ctx.AwayBench, ctx.Away.Tactics, dt, out swapped) && swapped)
                    ctx.Telemetry.AwayInterchanges++;

                int hinj = ctx.InjuryModel.Step(ctx.HomeOnField, ctx.HomeBench, ctx.Phase, dt, ctx.Rng);
                int ainj = ctx.InjuryModel.Step(ctx.AwayOnField, ctx.AwayBench, ctx.Phase, dt, ctx.Rng);
                ctx.Telemetry.HomeInjuryEvents += hinj;
                ctx.Telemetry.AwayInjuryEvents += ainj;

                // Phase logic (existing)
                switch (ctx.Phase)
                {
                    case Phase.CenterBounce: ResolveCenterBounce(ctx); break;
                    case Phase.Stoppage:     ResolveStoppage(ctx);     break;
                    case Phase.OpenPlay:     ResolveOpenPlay(ctx);     break;
                    case Phase.Inside50:     ResolveInside50(ctx);     break;
                    case Phase.ShotOnGoal:   ResolveShot(ctx);         break;
                    case Phase.KickIn:       ResolveKickIn(ctx);       break;
                }
            }

        private static void ResolveCenterBounce(MatchContext ctx)
        {
            float homeMid = Rating.MidfieldUnit(ctx.Home.OnField);
            float awayMid = Rating.MidfieldUnit(ctx.Away.OnField);
            float h = homeMid * (0.9f + 0.2f * (ctx.Home.Tactics.ContestBias / 100f));
            float a = awayMid * (0.9f + 0.2f * (ctx.Away.Tactics.ContestBias / 100f));
            h *= 1.03f; // slight HGA at bounce

            bool homeWins = ctx.Rng.NextFloat() < Rating.Softmax(h, a);
            ctx.Ball = BallState.FromClearance(homeWins ? ctx.Home.TeamId : ctx.Away.TeamId);
            ctx.Phase = Phase.OpenPlay;
        }

        private static void ResolveStoppage(MatchContext ctx)
        {
            float homeMid = Rating.MidfieldUnit(ctx.Home.OnField);
            float awayMid = Rating.MidfieldUnit(ctx.Away.OnField);
            float h = homeMid * (0.9f + 0.2f * (ctx.Home.Tactics.ContestBias / 100f));
            float a = awayMid * (0.9f + 0.2f * (ctx.Away.Tactics.ContestBias / 100f));

            bool homeWins = ctx.Rng.NextFloat() < Rating.Softmax(h, a);
            ctx.Ball = BallState.FromClearance(homeWins ? ctx.Home.TeamId : ctx.Away.TeamId);
            ctx.Phase = Phase.OpenPlay;
        }

        private static void ResolveOpenPlay(MatchContext ctx)
        {
            bool home = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId);
            var att = home ? ctx.Home : ctx.Away;
            var def = home ? ctx.Away : ctx.Home;

            float attackQuality = Rating.Inside50Quality(att.OnField);
            float defensePressure = Rating.DefensePressure(def.OnField);

            float weatherPenalty = ctx.Weather switch
            {
                Weather.Clear => 0f,
                Weather.Windy => 5f,
                Weather.LightRain => 7.5f,
                Weather.HeavyRain => 12f,
                _ => 0f
            };

            float baseProgress = attackQuality - 0.6f * defensePressure - weatherPenalty;
            float pProgress = Clamp01(0.5f + (baseProgress / 200f));

            var r = ctx.Rng.NextFloat();
            if (r < pProgress * 0.7f)
            {
                ctx.Ball.EnterF50();
                ctx.Phase = Phase.Inside50;
            }
            else if (r < pProgress * 0.7f + 0.15f)
            {
                ctx.Phase = Phase.Stoppage;
            }
            else
            {
                ctx.Ball.TurnoverTo(def.TeamId);
                ctx.Phase = Phase.OpenPlay;
            }
        }

        private static void ResolveInside50(MatchContext ctx)
        {
            bool home = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId);
            var att = home ? ctx.Home : ctx.Away;
            var def = home ? ctx.Away : ctx.Home;

            float markTaker = Rating.Inside50Quality(att.OnField);
            float defense = Rating.DefensePressure(def.OnField);
            float entryBias = 0.5f + 0.5f * (att.Tactics.KickingRisk / 100f);
            float xShot = Clamp01(0.25f + (markTaker - 0.5f * defense) / 150f) * entryBias;

            var r = ctx.Rng.NextFloat();
            if (r < xShot) ctx.Phase = Phase.ShotOnGoal;
            else if (r < xShot + 0.15f) ctx.Phase = Phase.Stoppage;
            else { ctx.Ball.TurnoverTo(def.TeamId); ctx.Phase = Phase.OpenPlay; }
        }

        private static void ResolveShot(MatchContext ctx)
        {
            bool home = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId);
            var att = home ? ctx.Home : ctx.Away;

            float baseAcc = Rating.Inside50Quality(att.OnField) / 100f;
            float weatherPenalty = ctx.Weather == Weather.Windy ? 0.08f
                               : ctx.Weather == Weather.LightRain ? 0.05f
                               : ctx.Weather == Weather.HeavyRain ? 0.11f : 0f;

            float pGoal = Clamp01(0.35f + 0.35f * baseAcc - weatherPenalty);

            var u = ctx.Rng.NextFloat();
            if (u < pGoal) ctx.Score.AddGoal(home);
            else if (u < pGoal + 0.35f) ctx.Score.AddBehind(home);

            if (u < pGoal) ctx.Phase = Phase.CenterBounce;
            else
            {
                var opp = home ? ctx.Away.TeamId : ctx.Home.TeamId;
                ctx.Ball = BallState.FromClearance(opp, false);
                ctx.Phase = Phase.KickIn;
            }
        }

        private static void ResolveKickIn(MatchContext ctx)
        {
            var r = ctx.Rng.NextFloat();
            if (r < 0.55f) ctx.Phase = Phase.OpenPlay;
            else
            {
                var opp = ctx.Ball.PossessionTeam.Equals(ctx.Home.TeamId) ? ctx.Away.TeamId : ctx.Home.TeamId;
                ctx.Ball.TurnoverTo(opp);
                ctx.Phase = Phase.OpenPlay;
            }
        }
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

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}