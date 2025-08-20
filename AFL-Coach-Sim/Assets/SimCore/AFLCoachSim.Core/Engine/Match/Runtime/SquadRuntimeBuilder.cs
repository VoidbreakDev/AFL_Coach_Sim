using System.Collections.Generic;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Engine.Match.Runtime
{
    public static class SquadRuntimeBuilder
    {
        public static void Build(
            List<Player> onFieldPlayers,
            List<Player> benchPlayers,
            TeamId teamId,
            out List<PlayerRuntime> onField,
            out List<PlayerRuntime> bench)
        {
            onField = new List<PlayerRuntime>(onFieldPlayers.Count);
            bench   = new List<PlayerRuntime>(benchPlayers.Count);

            for (int i = 0; i < onFieldPlayers.Count; i++)
                onField.Add(new PlayerRuntime(onFieldPlayers[i], teamId, true));
            for (int j = 0; j < benchPlayers.Count; j++)
                bench.Add(new PlayerRuntime(benchPlayers[j], teamId, false));
        }
    }
}