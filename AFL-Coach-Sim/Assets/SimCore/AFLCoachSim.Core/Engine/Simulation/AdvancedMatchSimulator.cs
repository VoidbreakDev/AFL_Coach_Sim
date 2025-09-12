using System.Collections.Generic;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Data;

namespace AFLCoachSim.Core.Engine.Simulation
{
    public sealed class AdvancedMatchSimulator
    {
        private readonly Dictionary<TeamId, Team> _teams;
        private readonly Dictionary<TeamId, System.Collections.Generic.List<Domain.Entities.Player>> _rosters;
        private readonly Dictionary<TeamId, TeamTactics> _tactics;
        private readonly DeterministicRandom _rng;

        public AdvancedMatchSimulator(
            Dictionary<TeamId, Team> teams,
            Dictionary<TeamId, System.Collections.Generic.List<Domain.Entities.Player>> rosters = null,
            Dictionary<TeamId, TeamTactics> tactics = null,
            DeterministicRandom rng = null)
        {
            _teams = teams;
            _rosters = rosters ?? new Dictionary<TeamId, System.Collections.Generic.List<Domain.Entities.Player>>();
            _tactics = tactics ?? new Dictionary<TeamId, TeamTactics>();
            _rng = rng ?? new DeterministicRandom(12345);
        }

        public MatchResultDTO Simulate(int round, TeamId home, TeamId away)
        {
            return Match.MatchEngine.PlayMatch(
                round, home, away, _teams, _rosters, _tactics,
                Match.Weather.Clear, new Match.Ground(), 20 * 60, _rng);
        }
    }
}