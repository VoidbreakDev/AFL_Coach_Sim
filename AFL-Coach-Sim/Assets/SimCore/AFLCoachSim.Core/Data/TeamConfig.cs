// Data/TeamConfig.cs
namespace AFLCoachSim.Core.Data
{
    public sealed class TeamConfig
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Attack { get; set; } = 50;
        public int Defense { get; set; } = 50;
    }
}