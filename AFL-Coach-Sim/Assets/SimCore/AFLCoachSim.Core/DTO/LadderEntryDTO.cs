// DTO/LadderEntryDTO.cs
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.DTO
{
        public sealed class LadderEntryDTO
    {
        public TeamId Team { get; set; }
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int PointsFor { get; set; }
        public int PointsAgainst { get; set; }
        public int PremiershipPoints => Wins * 4 + Draws * 2;
        public int Percentage => PointsAgainst == 0 ? 0 : (int)(PointsFor * 10000.0 / PointsAgainst);
    }
}