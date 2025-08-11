using System;
using System.Collections.Generic;
using AFLManager.Models;

namespace AFLManager.Simulation
{
    /// <summary>
    /// v0 simulation:
    /// - Team rating = avg player rating (or injected) + small home advantage
    /// - Convert ratings → expected scoring rate, sample goals via Poisson-ish approach
    /// - Produces total points only (goals/behinds split is cosmetic for now)
    /// Deterministic if you pass a seed (recommend: seed = matchId.GetHashCode()).
    /// </summary>
    public static class MatchSimulator
    {
        public interface ITeamRatingProvider
        {
            // Return a scalar 0–100-ish rating for a team (use your PlayerTeamModel roster).
            double GetTeamRating(string teamId);
            // Optional: per-player list if you want to attach stat lines later.
            IEnumerable<string> GetStartingPlayerIds(string teamId);
        }

        public sealed class DefaultRatingProvider : ITeamRatingProvider
        {
            private readonly Func<string, double> _ratingFunc;
            private readonly Func<string, IEnumerable<string>> _playersFunc;
            public DefaultRatingProvider(Func<string, double> ratingFunc,
                                         Func<string, IEnumerable<string>> playersFunc = null)
            {
                _ratingFunc = ratingFunc;
                _playersFunc = playersFunc ?? (_ => Array.Empty<string>());
            }
            public double GetTeamRating(string teamId) => _ratingFunc(teamId);
            public IEnumerable<string> GetStartingPlayerIds(string teamId) => _playersFunc(teamId);
        }

        public static MatchResult SimulateMatch(
            string matchId,
            string roundKey,
            string homeTeamId,
            string awayTeamId,
            ITeamRatingProvider ratingProvider,
            int? seed = null)
        {
            // Seeded RNG for replayable results
            var rng = seed.HasValue ? new Random(seed.Value) : new Random();

            // 1) Ratings + small home advantage
            var homeRating = ratingProvider.GetTeamRating(homeTeamId);
            var awayRating = ratingProvider.GetTeamRating(awayTeamId);
            const double homeAdv = 2.5; // tweak later
            homeRating += homeAdv;

            // 2) Transform ratings → expected points.
            // Keep it simple: base ~70–90 points per side; rating delta nudges expectation.
            // Expected points are bounded to reasonable AFL scores.
            double ratingDelta = homeRating - awayRating; // positive favors home
            double basePts = 78.0;                        // league mean
            double sens    = 0.55;                        // sensitivity (bigger = more swing)
            double homeExp = Clamp(basePts + sens * ratingDelta, 45, 130);
            double awayExp = Clamp(basePts - sens * ratingDelta, 45, 130);

            // 3) Sample actual points using mildly noisy normals (faster than full Poisson here)
            int homePoints = SampleMatchPoints(rng, homeExp, 14); // stdev ~14 points
            int awayPoints = SampleMatchPoints(rng, awayExp, 14);

            // 4) Cosmetic goals/behinds split (optional)
            (int gH, int bH) = SplitGoalsBehinds(rng, homePoints);
            (int gA, int bA) = SplitGoalsBehinds(rng, awayPoints);

            var result = new MatchResult
            {
                MatchId = matchId,
                RoundKey = roundKey,
                HomeTeamId = homeTeamId,
                AwayTeamId = awayTeamId,
                HomeScore = homePoints,
                AwayScore = awayPoints,
                HomeGoals = gH,
                HomeBehinds = bH,
                AwayGoals = gA,
                AwayBehinds = bA,
                SimulatedAtUtc = DateTime.UtcNow
            };

            // 5) Lightweight per‑player stat sprinkle (totally optional for v0)
            // You can plug in real positions later and bias goals toward FWDs, disposals toward MIDs, etc.
            DistributeBasicPlayerStats(rng, ratingProvider.GetStartingPlayerIds(homeTeamId), ratingProvider.GetStartingPlayerIds(awayTeamId), result);

            // 6) Best on ground: pick heaviest stat line for now
            result.BestOnGroundPlayerId = PickBog(result);

            return result;
        }

        private static int SampleMatchPoints(Random rng, double mean, double stdev)
        {
            // Box–Muller normal sample, clamp to reasonable AFL range, and round to 6*(goals)+behinds grid
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double val = mean + stdev * randStdNormal;
            int pts = (int)Math.Round(Clamp(val, 30, 160));

            // Optionally nudge to realistic score shapes (multiples of 6 + behinds 0–5)
            return pts;
        }

        private static (int goals, int behinds) SplitGoalsBehinds(Random rng, int points)
        {
            // Aim for ~55–65% of scoring shots being goals: g*6 + b = points
            // Find a plausible pair
            int bestG = points / 6;
            int bestB = points - bestG * 6;
            // Add tiny randomness to not always be the same
            int tweak = rng.Next(-2, 3);
            bestG = Math.Max(0, bestG + tweak);
            bestB = points - bestG * 6;
            if (bestB < 0) { bestB = rng.Next(0, 6); bestG = Math.Max(0, (points - bestB) / 6); }
            return (bestG, bestB);
        }

        private static void DistributeBasicPlayerStats(
            Random rng,
            IEnumerable<string> homePlayers,
            IEnumerable<string> awayPlayers,
            MatchResult result)
        {
            void Bump(string pid, int disp, int gls, int tck)
            {
                if (!result.PlayerStats.TryGetValue(pid, out var line))
                {
                    line = new PlayerStatLine { PlayerId = pid };
                    result.PlayerStats[pid] = line;
                }
                line.Disposals += disp;
                line.Goals += gls;
                line.Tackles += tck;
            }

            void Sprinkle(IEnumerable<string> players, int teamPoints)
            {
                var p = new List<string>(players);
                if (p.Count == 0) return;

                int shots = Math.Max(8, teamPoints / 5); // very rough = number of possession clusters
                for (int i = 0; i < shots; i++)
                {
                    string pid = p[rng.Next(p.Count)];
                    int disp = rng.Next(2, 8);
                    int tck  = rng.Next(0, 3);
                    int gls  = rng.NextDouble() < 0.15 ? 1 : 0;
                    Bump(pid, disp, gls, tck);
                }
            }

            Sprinkle(homePlayers, result.HomeScore);
            Sprinkle(awayPlayers, result.AwayScore);
        }

        private static string PickBog(MatchResult r)
        {
            string bestId = null;
            int bestScore = int.MinValue;
            foreach (var kv in r.PlayerStats)
            {
                // Heuristic weight; tweak later
                int s = kv.Value.Disposals + 3 * kv.Value.Goals + 2 * kv.Value.Tackles;
                if (s > bestScore) { bestScore = s; bestId = kv.Key; }
            }
            return bestId;
        }

        private static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));
    }
}
