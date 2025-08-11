using System.Collections.Generic;
using AFLManager.Models;
using AFLManager.Simulation;
using UnityEngine;

public class SimSmokeTest : MonoBehaviour
{
    void Start()
    {
        var teams = new List<string> { "TEAM_A", "TEAM_B" };
        var names = new Dictionary<string, string> { { "TEAM_A", "Adelaide" }, { "TEAM_B", "Brisbane" } };

        var result = MatchSimulator.SimulateMatch(
            "match-1", "R1", "TEAM_A", "TEAM_B",
            new MatchSimulator.DefaultRatingProvider(
                id => id == "TEAM_A" ? 72 : 68,
                id => new[] { $"{id}_P1", $"{id}_P2", $"{id}_P3", $"{id}_P4", $"{id}_P5", $"{id}_P6" }
            ),
            seed: 12345
        );

        var ladder = LadderCalculator.BuildLadder(teams, names, new[] { result });
        Debug.Log($"Result: {names[result.HomeTeamId]} {result.HomeScore} - {result.AwayScore} {names[result.AwayTeamId]}");
        Debug.Log($"Ladder Top: {ladder[0].TeamName} {ladder[0].Points} pts, % {ladder[0].Percentage:F1}");
    }
}
