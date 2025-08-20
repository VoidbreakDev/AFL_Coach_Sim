using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Domain.Entities;

public static class MatchEngineSmoke
{
    static Dictionary<TeamId, Team> MakeTeams()
    {
        return new Dictionary<TeamId, Team> {
            [new TeamId(1)] = new Team(new TeamId(1), "Home", 60, 60),
            [new TeamId(2)] = new Team(new TeamId(2), "Away", 60, 60)
        };
    }

    static List<Player> MakeUniformRoster(int rating, int count)
    {
        var list = new List<Player>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new Player {
                Name = "P"+i,
                Endurance = rating, Durability = rating, Discipline = 60,
                Attr = new Attributes {
                    WorkRate = rating, DecisionMaking = rating, Kicking = rating, Marking = rating,
                    Clearance = rating, Strength = rating, Positioning = rating, Tackling = rating, Spoiling = rating
                }
            });
        }
        return list;
    }

    [MenuItem("AFL Coach Sim/Sim/Run Quick 1 Match")]
    public static void RunOne()
    {
        var teams   = MakeTeams();
        var rosters = new Dictionary<TeamId, List<Player>> {
            [new TeamId(1)] = MakeUniformRoster(70, 30),
            [new TeamId(2)] = MakeUniformRoster(70, 30),
        };
        var tactics = new Dictionary<TeamId, TeamTactics> {
            [new TeamId(1)] = new TeamTactics { ContestBias=50, KickingRisk=50, TargetInterchangesPerGame=70 },
            [new TeamId(2)] = new TeamTactics { ContestBias=50, KickingRisk=50, TargetInterchangesPerGame=70 },
        };

        var result = AFLCoachSim.Core.Engine.Match.MatchEngine.PlayMatch(
            round: 1,
            homeId: new TeamId(1), awayId: new TeamId(2),
            teams: teams, rosters: rosters, tactics: tactics,
            weather: Weather.Clear, ground: new Ground(),
            quarterSeconds: 20*60,
            rng: new DeterministicRandom(12345)
        );

        Debug.Log($"[M3] Score H/A: {result.HomeScore}/{result.AwayScore}");
        // If you added telemetry to MatchEngine and expose it, log it here
        // Debug.Log($"INT H/A {tel.HomeInterchanges}/{tel.AwayInterchanges} | INJ H/A {tel.HomeInjuryEvents}/{tel.AwayInjuryEvents} | COND H/A {tel.HomeAvgConditionEnd}/{tel.AwayAvgConditionEnd}");
    }

    [MenuItem("AFL Coach Sim/Sim/Sweep Rotations (30 vs 90)")]
    public static void SweepRotations()
    {
        var teams   = MakeTeams();
        var rosters = new Dictionary<TeamId, List<Player>> {
            [new TeamId(1)] = MakeUniformRoster(70, 30),
            [new TeamId(2)] = MakeUniformRoster(70, 30),
        };

        int trials = 40;
        int totLow=0, totHigh=0;

        for (int i=0;i<trials;i++)
        {
            var low = new Dictionary<TeamId, TeamTactics> {
                [new TeamId(1)] = new TeamTactics { TargetInterchangesPerGame=30, ContestBias=50, KickingRisk=50 },
                [new TeamId(2)] = new TeamTactics { TargetInterchangesPerGame=30, ContestBias=50, KickingRisk=50 },
            };
            var high = new Dictionary<TeamId, TeamTactics> {
                [new TeamId(1)] = new TeamTactics { TargetInterchangesPerGame=90, ContestBias=50, KickingRisk=50 },
                [new TeamId(2)] = new TeamTactics { TargetInterchangesPerGame=90, ContestBias=50, KickingRisk=50 },
            };

            var resL = AFLCoachSim.Core.Engine.Match.MatchEngine.PlayMatch(
                1, new TeamId(1), new TeamId(2), teams, rosters, low,
                Weather.Clear, new Ground(), 10*60, new DeterministicRandom(100+i));
            totLow += resL.HomeScore + resL.AwayScore;

            var resH = AFLCoachSim.Core.Engine.Match.MatchEngine.PlayMatch(
                1, new TeamId(1), new TeamId(2), teams, rosters, high,
                Weather.Clear, new Ground(), 10*60, new DeterministicRandom(200+i));
            totHigh += resH.HomeScore + resH.AwayScore;
        }

        Debug.Log($"[M3] Totals over {trials} short sims — LowRot: {totLow}, HighRot: {totHigh} (High should be > Low)");
    }
}