// Engine/Simulation/MatchSimulator.cs
using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.DTO;

namespace AFLCoachSim.Core.Engine.Simulation
{
    /// <summary>
    /// Lightweight AFL-style scoring model:
    /// - Expected scoring shots from Attack vs Defense with small home boost.
    /// - Convert to goals/behinds with conversion variance.
    /// </summary>
    public sealed class MatchSimulator
    {
        private readonly IReadOnlyDictionary<TeamId, Team> _teams;
        private readonly IRandom _rng;

        public MatchSimulator(IReadOnlyDictionary<TeamId, Team> teams, IRandom rng)
        {
            _teams = teams; _rng = rng;
        }

        public MatchResultDTO Simulate(int round, TeamId home, TeamId away)
        {
            var th = _teams[home]; var ta = _teams[away];

            double homeAdv = 1.05; // small home advantage
            double shotsHome = Math.Max(5, (th.Attack * homeAdv) - ta.Defense * 0.5);
            double shotsAway = Math.Max(5, (ta.Attack) - th.Defense * 0.5);

            int goalsHome = 0, behindsHome = 0;
            int goalsAway = 0, behindsAway = 0;

            // Simple binomial-ish conversion with noise
            for (int i = 0; i < (int)shotsHome; i++)
            {
                bool goal = _rng.NextDouble() < 0.55 + (th.Attack - ta.Defense) * 0.001;
                if (goal) goalsHome++; else behindsHome++;
            }
            for (int i = 0; i < (int)shotsAway; i++)
            {
                bool goal = _rng.NextDouble() < 0.55 + (ta.Attack - th.Defense) * 0.001 - 0.02; // slight away penalty
                if (goal) goalsAway++; else behindsAway++;
            }

            int homeScore = goalsHome * 6 + behindsHome;
            int awayScore = goalsAway * 6 + behindsAway;

            return new MatchResultDTO
            {
                Round = round, Home = home, Away = away,
                HomeScore = homeScore, AwayScore = awayScore
            };
        }
    }
}