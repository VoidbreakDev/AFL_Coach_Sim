// DTO/MatchResultDTO.cs
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.DTO
{
    public sealed class MatchResultDTO
{
    public int Round { get; set; }
    public TeamId Home { get; set; }
    public TeamId Away { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public TeamId Winner => HomeScore > AwayScore ? Home : (AwayScore > HomeScore ? Away : new TeamId(0));
    public bool IsDraw => HomeScore == AwayScore;
}
}