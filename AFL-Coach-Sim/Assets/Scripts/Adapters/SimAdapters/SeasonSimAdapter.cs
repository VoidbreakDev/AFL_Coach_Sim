// Assets/Scripts/Adapters/SimAdapters/SeasonSimAdapter.cs
using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Ladder;
using AFLCoachSim.Core.Engine.Scheduling;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.DTO;


/// <summary>
/// Simple, Unity-friendly fixture container (avoids named tuples / ValueTuple issues).
/// </summary>
public readonly struct Fixture
{
    public int Round { get; }
    public TeamId Home { get; }
    public TeamId Away { get; }
    public Fixture(int round, TeamId home, TeamId away)
    {
        Round = round; Home = home; Away = away;
    }
}

public static class SeasonSimAdapter
{
    public static (List<Fixture> fixtures,
                   List<MatchResultDTO> results,
                   List<LadderEntryDTO> ladder)
        RunSeason(LeagueConfig config, int seed)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.Teams == null || config.Teams.Count == 0)
            throw new ArgumentException("LeagueConfig.Teams is null or empty", nameof(config));

        // build teams
        var teams = config.Teams
            .Select(t => new Team(new TeamId(t.Id), t.Name, t.Attack, t.Defense))
            .ToDictionary(t => t.Id, t => t);

        var raw = RoundRobinScheduler.Build(teams.Keys.ToList(), config.DoubleRoundRobin);
        var fixtures = new List<Fixture>(raw.Count);
        foreach (var item in raw)
        {
            fixtures.Add(new Fixture(item.round, item.home, item.away));
        }
        var sim = new MatchSimulator(teams, new DeterministicRandom(seed));

        var results = new List<MatchResultDTO>(fixtures.Count);
        foreach (var fx in fixtures)
        {
            results.Add(sim.Simulate(fx.Round, fx.Home, fx.Away));
        }

        var ladder = LadderCalculator.BuildLadder(results);
        return (fixtures, results, ladder);
    }
}