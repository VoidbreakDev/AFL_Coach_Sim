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
using AFLCoachSim.Core.Engine.Match.Commentary;

/// <summary>
/// Example showing how to integrate commentary system with existing SeasonBoot
/// This demonstrates the enhanced match simulation with full commentary
/// </summary>
public class SeasonBootWithCommentary : MonoBehaviour
{
    [Header("Config")]
    public LeagueConfigSO league;
    public int seed = 12345;
    
    [Header("Commentary Settings")]
    [SerializeField] private bool enableCommentary = true;
    [SerializeField] private bool showMatchCommentary = true;
    [SerializeField] private bool showHighlightsOnly = false;
    [SerializeField] private int maxCommentaryLines = 20;

    [Header("Views")]
    public LadderTableView ladderView;
    public FixtureListView fixtureView;

    // Data cache - enhanced with commentary
    public List<(int round, TeamId home, TeamId away)> Fixtures { get; private set; }
    public List<MatchResultDTO> Results { get; private set; }
    public List<CommentatedMatchResult> CommentatedResults { get; private set; } = new List<CommentatedMatchResult>();
    public List<LadderEntryDTO> Ladder { get; private set; }
    public TeamDirectory Directory { get; private set; }

    void Start()
    {
        var core = league.ToCore();
        Directory = new TeamDirectory(core.Teams);

        // Build teams for simulation
        var teams = core.Teams
            .Select(t => new Team(new TeamId(t.Id), t.Name, t.Attack, t.Defense))
            .ToDictionary(t => t.Id, t => t);

        // Create simple rosters for commentary (you can enhance this)
        var rosters = CreateSimpleRosters(teams.Keys);

        Fixtures = RoundRobinScheduler.Build(teams.Keys.ToList(), core.DoubleRoundRobin);

        var rng = new DeterministicRandom(seed);
        Results = new List<MatchResultDTO>(Fixtures.Count);

        foreach (var fx in Fixtures)
        {
            if (enableCommentary)
            {
                // Use enhanced commentary system
                var commentatedResult = MatchEngineWithCommentary.PlayMatchWithCommentary(
                    fx.round, fx.home, fx.away, teams, rosters, rng: rng);

                CommentatedResults.Add(commentatedResult);
                Results.Add(commentatedResult.MatchResult);

                // Log commentary for first few matches
                if (showMatchCommentary && CommentatedResults.Count <= 3)
                {
                    LogMatchCommentary(commentatedResult, fx.round);
                }
            }
            else
            {
                // Use standard simulation
                var sim = new MatchSimulator(teams, rng);
                Results.Add(sim.Simulate(fx.round, fx.home, fx.away));
            }
        }

        Ladder = LadderCalculator.BuildLadder(Results);

        // Bind to UI
        if (ladderView) ladderView.Render(Ladder, Directory);
        if (fixtureView) fixtureView.Render(Fixtures, Results, Directory);

        // Log summary
        LogSeasonSummary();
    }

    private void LogMatchCommentary(CommentatedMatchResult result, int round)
    {
        var homeTeam = Directory.NameOf(result.Home);
        var awayTeam = Directory.NameOf(result.Away);

        Debug.Log($"\n🏈 ROUND {round}: {homeTeam} vs {awayTeam}");
        Debug.Log($"Final Score: {homeTeam} {result.HomeScore} - {result.AwayScore} {awayTeam}");

        if (showHighlightsOnly)
        {
            var highlights = MatchEngineWithCommentary.GetMatchHighlights(result);
            Debug.Log($"\n📺 Match Highlights ({highlights.Count} key moments):");
            foreach (var highlight in highlights.Take(maxCommentaryLines))
            {
                Debug.Log($"  {highlight}");
            }
        }
        else
        {
            Debug.Log($"\n📝 Full Commentary ({result.Commentary.Count} events):");
            foreach (var commentary in result.Commentary.Take(maxCommentaryLines))
            {
                Debug.Log($"  {commentary}");
            }
            if (result.Commentary.Count > maxCommentaryLines)
            {
                Debug.Log($"  ... and {result.Commentary.Count - maxCommentaryLines} more events");
            }
        }
    }

    private void LogSeasonSummary()
    {
        if (!enableCommentary || CommentatedResults.Count == 0) return;

        var totalGoals = CommentatedResults.Sum(r => r.Events.Count(e => e.EventType == MatchEventType.Goal));
        var totalSpectacularMarks = CommentatedResults.Sum(r => r.Events.Count(e => e.EventType == MatchEventType.SpectacularMark));
        var totalInjuries = CommentatedResults.Sum(r => r.Events.Count(e => e.EventType == MatchEventType.Injury));

        Debug.Log($"\n📊 SEASON COMMENTARY STATISTICS:");
        Debug.Log($"Matches with Commentary: {CommentatedResults.Count}");
        Debug.Log($"Total Goals: {totalGoals}");
        Debug.Log($"Spectacular Marks: {totalSpectacularMarks}");
        Debug.Log($"Injury Events: {totalInjuries}");
        Debug.Log($"Average Commentary Events per Match: {CommentatedResults.Average(r => r.Commentary.Count):F1}");
    }

    private Dictionary<TeamId, List<AFLCoachSim.Core.Domain.Entities.Player>> CreateSimpleRosters(IEnumerable<TeamId> teamIds)
    {
        var rosters = new Dictionary<TeamId, List<AFLCoachSim.Core.Domain.Entities.Player>>();

        // Sample player names for realistic commentary
        var sampleNames = new[]
        {
            "Jack Miller", "Tom Wilson", "Sam Brown", "Luke Davis", "Ben Jones",
            "Matt Taylor", "Josh Anderson", "Ryan Moore", "Alex Thomas", "Nick Jackson",
            "Jake White", "Dan Martin", "Chris Lee", "Adam Clark", "Luke Roberts",
            "Josh Thompson", "Sam Garcia", "Tom Martinez", "Ben Rodriguez", "Matt Lewis",
            "Alex Walker", "Ryan Hall"
        };

        foreach (var teamId in teamIds)
        {
            var roster = new List<AFLCoachSim.Core.Domain.Entities.Player>();
            
            for (int i = 0; i < 22; i++)
            {
                roster.Add(new AFLCoachSim.Core.Domain.Entities.Player
                {
                    Id = new PlayerId((teamId.Value * 100) + i), // Generate unique int IDs per team
                    Name = sampleNames[i % sampleNames.Length]
                });
            }
            
            rosters[teamId] = roster;
        }

        return rosters;
    }

    /// <summary>
    /// Get commentary for a specific match (useful for match replay UI)
    /// </summary>
    public CommentatedMatchResult GetMatchCommentary(int round, TeamId homeId, TeamId awayId)
    {
        return CommentatedResults.FirstOrDefault(r => 
            r.Round == round && r.Home.Equals(homeId) && r.Away.Equals(awayId));
    }

    /// <summary>
    /// Get all highlights from the season so far
    /// </summary>
    public List<string> GetSeasonHighlights()
    {
        var highlights = new List<string>();
        foreach (var result in CommentatedResults)
        {
            highlights.AddRange(MatchEngineWithCommentary.GetMatchHighlights(result));
        }
        return highlights;
    }
}
