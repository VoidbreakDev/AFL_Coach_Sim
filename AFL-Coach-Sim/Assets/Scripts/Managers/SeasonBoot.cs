using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using AFLCoachSim.Unity.Data;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Engine.Scheduling;
using AFLCoachSim.Core.Engine.Ladder;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.DTO;

public class SeasonBoot : MonoBehaviour
{
    [Header("Config")]
    public LeagueConfigSO league;
    public int seed = 12345;

    [Header("Views")]
    public LadderTableView ladderView;
    public FixtureListView fixtureView;

    // Data cache
    public List<(int round, TeamId home, TeamId away)> Fixtures { get; private set; }
    public List<MatchResultDTO> Results { get; private set; }
    public List<LadderEntryDTO> Ladder { get; private set; }
    public TeamDirectory Directory { get; private set; }

    void Start()
    {
        var core = league.ToCore();
        Directory = new TeamDirectory(core.Teams);

        // Build teams for sim
        var teams = core.Teams
            .Select(t => new Team(new TeamId(t.Id), t.Name, t.Attack, t.Defense))
            .ToDictionary(t => t.Id, t => t);

        Fixtures = RoundRobinScheduler.Build(teams.Keys.ToList(), core.DoubleRoundRobin);

        var sim = new MatchSimulator(teams, new DeterministicRandom(seed));
        Results = new List<MatchResultDTO>(Fixtures.Count);
        foreach (var fx in Fixtures)
            Results.Add(sim.Simulate(fx.round, fx.home, fx.away));

        Ladder = LadderCalculator.BuildLadder(Results);

        // Bind to UI
        if (ladderView) ladderView.Render(Ladder, Directory);
        if (fixtureView) fixtureView.Render(Fixtures, Results, Directory);
    }
}