namespace AFLCoachSim.Core.Data
{
    public sealed class TeamTacticsConfig
    {
        public int TeamId;
        // 0..100 sliders
        public int Tempo = 50;
        public int CorridorUsage = 50;
        public int PressAggression = 50;
        public int ContestBias = 50;
        public int KickingRisk = 50;
        public int TargetInterchangesPerGame = 65;

        public TeamTactics ToDomain() => new TeamTactics
        {
            Tempo = Tempo, CorridorUsage = CorridorUsage, PressAggression = PressAggression,
            ContestBias = ContestBias, KickingRisk = KickingRisk, TargetInterchangesPerGame = TargetInterchangesPerGame
        };
    }

    public sealed class TeamTactics
    {
        public int Tempo, CorridorUsage, PressAggression, ContestBias, KickingRisk;
        public int TargetInterchangesPerGame;
    }
}