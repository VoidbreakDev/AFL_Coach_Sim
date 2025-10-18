using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Infrastructure.Logging;

namespace AFLCoachSim.Core.Season.Services
{
    /// <summary>
    /// Core engine for generating AFL season fixtures with proper scheduling constraints
    /// </summary>
    public class FixtureGenerationEngine
    {
        private readonly Random _random;
        private readonly List<TeamId> _allTeams;
        private const int STANDARD_ROUND_COUNT = 24;
        
        public FixtureGenerationEngine(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _allTeams = Enum.GetValues(typeof(TeamId)).Cast<TeamId>().Where(t => t != TeamId.None).ToList();
        }
        
        /// <summary>
        /// Generate a complete AFL season calendar
        /// </summary>
        public SeasonCalendar GenerateSeasonCalendar(int year, FixtureGenerationOptions options = null)
        {
            options ??= FixtureGenerationOptions.Default;
            
            CoreLogger.Log($"[FixtureGenerator] Generating {year} AFL season calendar...");
            
            var calendar = new SeasonCalendar
            {
                Year = year,
                TotalRounds = options.TotalRounds,
                SeasonStart = AFLCalendarUtilities.GetSeasonOpenerDate(year),
                SeasonEnd = AFLCalendarUtilities.GetGrandFinalWeekend(year),
                CurrentState = SeasonState.NotStarted,
                CurrentRound = 1
            };
            
            // Step 1: Generate specialty matches with their target dates
            var specialtyMatches = GenerateSpecialtyMatches(year, options);
            calendar.SpecialtyMatches = specialtyMatches;
            
            // Step 2: Create bye round configuration
            var byeConfig = GenerateByeRoundConfiguration(options);
            calendar.ByeConfiguration = byeConfig;
            
            // Step 3: Generate base fixture (all team pairings)
            var allMatchups = GenerateAllMatchups(options);
            
            // Step 4: Distribute matches across rounds with constraints
            var rounds = DistributeMatchesAcrossRounds(allMatchups, specialtyMatches, byeConfig, calendar.SeasonStart, options);
            calendar.Rounds = rounds;
            
            // Step 5: Schedule specific match times and venues
            ScheduleMatchTimesAndVenues(calendar, options);
            
            // Step 6: Validate the generated calendar
            var validation = calendar.Validate();
            if (!validation.IsValid)
            {
                CoreLogger.LogWarning($"[FixtureGenerator] Generated calendar has issues: {string.Join(", ", validation.Errors)}");
            }
            
            CoreLogger.Log($"[FixtureGenerator] Season calendar generated successfully with {calendar.Rounds.Count} rounds and {calendar.Rounds.Sum(r => r.Matches.Count)} matches");
            
            return calendar;
        }
        
        /// <summary>
        /// Generate all specialty matches for the year
        /// </summary>
        private List<SpecialtyMatch> GenerateSpecialtyMatches(int year, FixtureGenerationOptions options)
        {
            var specialtyMatches = new List<SpecialtyMatch>();
            
            // Season Opener - Carlton vs Richmond (Second Thursday of March)
            if (options.IncludeSeasonOpener)
            {
                specialtyMatches.Add(new SpecialtyMatch
                {
                    Name = "Season Opener",
                    Description = "Traditional season opening match",
                    HomeTeam = TeamId.Carlton,
                    AwayTeam = TeamId.Richmond,
                    RoundNumber = 1,
                    TargetDate = AFLCalendarUtilities.GetSeasonOpenerDate(year),
                    Venue = AFLVenues.MCG.Name,
                    Type = SpecialtyMatchType.SeasonOpener,
                    Priority = 10
                });
            }
            
            // ANZAC Day - Collingwood vs Essendon (April 25th)
            if (options.IncludeAnzacDay)
            {
                var anzacDate = AFLCalendarUtilities.GetAnzacDay(year);
                var roundNumber = CalculateRoundForDate(anzacDate, AFLCalendarUtilities.GetSeasonOpenerDate(year));
                
                specialtyMatches.Add(new SpecialtyMatch
                {
                    Name = "ANZAC Day Match",
                    Description = "Traditional ANZAC Day clash",
                    HomeTeam = TeamId.Collingwood,
                    AwayTeam = TeamId.Essendon,
                    RoundNumber = roundNumber,
                    TargetDate = anzacDate,
                    Venue = AFLVenues.MCG.Name,
                    Type = SpecialtyMatchType.AnzacDay,
                    Priority = 9
                });
            }
            
            // King's Birthday - Collingwood vs Melbourne (Second Monday of June)
            if (options.IncludeKingsBirthday)
            {
                var kingsBirthday = AFLCalendarUtilities.GetKingsBirthday(year);
                var roundNumber = CalculateRoundForDate(kingsBirthday, AFLCalendarUtilities.GetSeasonOpenerDate(year));
                
                specialtyMatches.Add(new SpecialtyMatch
                {
                    Name = "King's Birthday Match",
                    Description = "Traditional King's Birthday match",
                    HomeTeam = TeamId.Collingwood,
                    AwayTeam = TeamId.Melbourne,
                    RoundNumber = roundNumber,
                    TargetDate = kingsBirthday,
                    Venue = AFLVenues.MCG.Name,
                    Type = SpecialtyMatchType.KingsBirthday,
                    Priority = 8
                });
            }
            
            // Easter Monday - Geelong vs Hawthorn
            if (options.IncludeEasterMonday)
            {
                var easterMonday = AFLCalendarUtilities.GetEasterMonday(year);
                var roundNumber = CalculateRoundForDate(easterMonday, AFLCalendarUtilities.GetSeasonOpenerDate(year));
                
                specialtyMatches.Add(new SpecialtyMatch
                {
                    Name = "Easter Monday Match",
                    Description = "Traditional Easter Monday clash",
                    HomeTeam = TeamId.Geelong,
                    AwayTeam = TeamId.Hawthorn,
                    RoundNumber = roundNumber,
                    TargetDate = easterMonday,
                    Venue = AFLVenues.MCG.Name,
                    Type = SpecialtyMatchType.EasterMonday,
                    Priority = 7
                });
            }
            
            // Add rivalry matches if requested
            if (options.IncludeRivalryMatches)
            {
                specialtyMatches.AddRange(GenerateRivalryMatches(year, options));
            }
            
            return specialtyMatches.OrderByDescending(sm => sm.Priority).ToList();
        }
        
        /// <summary>
        /// Generate rivalry and derby matches
        /// </summary>
        private List<SpecialtyMatch> GenerateRivalryMatches(int year, FixtureGenerationOptions options)
        {
            var rivalryMatches = new List<SpecialtyMatch>();
            var seasonStart = AFLCalendarUtilities.GetSeasonOpenerDate(year);
            
            // Adelaide vs Port Adelaide - Showdown (twice per year)
            for (int i = 0; i < 2; i++)
            {
                var homeTeam = i == 0 ? TeamId.Adelaide : TeamId.PortAdelaide;
                var awayTeam = i == 0 ? TeamId.PortAdelaide : TeamId.Adelaide;
                var roundNumber = i == 0 ? 8 : 16; // Spread throughout season
                
                rivalryMatches.Add(new SpecialtyMatch
                {
                    Name = "Showdown",
                    Description = "Adelaide rivalry match",
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    RoundNumber = roundNumber,
                    TargetDate = seasonStart.AddDays((roundNumber - 1) * 7),
                    Venue = AFLVenues.AdelaideCrowdsGround.Name,
                    Type = SpecialtyMatchType.Showdown,
                    IsFlexibleDate = true,
                    Priority = 5
                });
            }
            
            // Add other rivalry matches as needed...
            
            return rivalryMatches;
        }
        
        /// <summary>
        /// Generate bye round configuration
        /// </summary>
        private ByeRoundConfiguration GenerateByeRoundConfiguration(FixtureGenerationOptions options)
        {
            var config = new ByeRoundConfiguration
            {
                StartRound = options.ByeRoundStart,
                EndRound = options.ByeRoundEnd,
                TeamsPerByeRound = 6 // 18 teams / 3 bye rounds = 6 teams per bye round
            };
            
            // Distribute teams across bye rounds
            var shuffledTeams = _allTeams.OrderBy(x => _random.Next()).ToList();
            var teamIndex = 0;
            
            for (int round = config.StartRound; round <= config.EndRound; round++)
            {
                var byeTeams = shuffledTeams.Skip(teamIndex).Take(config.TeamsPerByeRound).ToList();
                config.ByeRoundAssignments[round] = byeTeams;
                teamIndex += config.TeamsPerByeRound;
            }
            
            return config;
        }
        
        /// <summary>
        /// Generate all required team matchups for the season
        /// </summary>
        private List<TeamMatchup> GenerateAllMatchups(FixtureGenerationOptions options)
        {
            var matchups = new List<TeamMatchup>();
            
            // Each team plays every other team at least once
            for (int i = 0; i < _allTeams.Count; i++)
            {
                for (int j = i + 1; j < _allTeams.Count; j++)
                {
                    var homeTeam = _allTeams[i];
                    var awayTeam = _allTeams[j];
                    
                    // Add the matchup
                    matchups.Add(new TeamMatchup(homeTeam, awayTeam));
                    
                    // Some teams play twice - add reverse matchup for balance
                    if (ShouldPlayTwice(homeTeam, awayTeam, options))
                    {
                        matchups.Add(new TeamMatchup(awayTeam, homeTeam));
                    }
                }
            }
            
            // Add additional matchups if needed to reach target matches per team
            var targetMatchesPerTeam = options.TotalRounds - 1; // Account for bye round
            while (!ValidateMatchupsCount(matchups, targetMatchesPerTeam))
            {
                AddAdditionalMatchups(matchups, targetMatchesPerTeam);
            }
            
            return matchups;
        }
        
        /// <summary>
        /// Distribute matchups across rounds with constraints
        /// </summary>
        private List<SeasonRound> DistributeMatchesAcrossRounds(List<TeamMatchup> allMatchups, 
                                                               List<SpecialtyMatch> specialtyMatches,
                                                               ByeRoundConfiguration byeConfig,
                                                               DateTime seasonStart,
                                                               FixtureGenerationOptions options)
        {
            var rounds = new List<SeasonRound>();
            var availableMatchups = new List<TeamMatchup>(allMatchups);
            var matchId = 1;
            
            for (int roundNum = 1; roundNum <= options.TotalRounds; roundNum++)
            {
                var round = new SeasonRound
                {
                    RoundNumber = roundNum,
                    RoundName = $"Round {roundNum}",
                    RoundStartDate = seasonStart.AddDays((roundNum - 1) * 7),
                    RoundEndDate = seasonStart.AddDays(roundNum * 7 - 1),
                    RoundType = RoundType.Regular
                };
                
                // Handle bye rounds
                if (byeConfig.ByeRoundAssignments.ContainsKey(roundNum))
                {
                    round.TeamsOnBye = byeConfig.ByeRoundAssignments[roundNum];
                }
                
                // Add specialty matches first
                var roundSpecialtyMatches = specialtyMatches.Where(sm => sm.RoundNumber == roundNum).ToList();
                foreach (var specialtyMatch in roundSpecialtyMatches)
                {
                    var scheduledMatch = specialtyMatch.ToScheduledMatch(matchId++, round.RoundStartDate);
                    round.Matches.Add(scheduledMatch);
                    
                    // Remove corresponding matchup from available list
                    availableMatchups.RemoveAll(m => 
                        (m.HomeTeam == specialtyMatch.HomeTeam && m.AwayTeam == specialtyMatch.AwayTeam) ||
                        (m.HomeTeam == specialtyMatch.AwayTeam && m.AwayTeam == specialtyMatch.HomeTeam));
                }
                
                // Fill remaining matches for the round
                var teamsAlreadyPlaying = round.Matches.SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
                                                     .Concat(round.TeamsOnBye)
                                                     .ToHashSet();
                
                var matchesNeeded = (18 - round.TeamsOnBye.Count) / 2 - round.Matches.Count;
                
                for (int i = 0; i < matchesNeeded && availableMatchups.Any(); i++)
                {
                    var matchup = FindBestMatchupForRound(availableMatchups, teamsAlreadyPlaying);
                    if (matchup != null)
                    {
                        var scheduledMatch = new ScheduledMatch
                        {
                            MatchId = matchId++,
                            RoundNumber = roundNum,
                            HomeTeam = matchup.HomeTeam,
                            AwayTeam = matchup.AwayTeam,
                            ScheduledDateTime = round.RoundStartDate,
                            Venue = AFLVenues.GetHomeVenueForTeam(matchup.HomeTeam).Name
                        };
                        
                        round.Matches.Add(scheduledMatch);
                        availableMatchups.Remove(matchup);
                        teamsAlreadyPlaying.Add(matchup.HomeTeam);
                        teamsAlreadyPlaying.Add(matchup.AwayTeam);
                    }
                }
                
                rounds.Add(round);
            }
            
            return rounds;
        }
        
        /// <summary>
        /// Schedule specific match times and venues
        /// </summary>
        private void ScheduleMatchTimesAndVenues(SeasonCalendar calendar, FixtureGenerationOptions options)
        {
            foreach (var round in calendar.Rounds)
            {
                var matchesPerTimeSlot = DistributeMatchesAcrossWeekend(round.Matches.Count);
                var matchIndex = 0;
                
                foreach (var match in round.Matches)
                {
                    // Determine day and time
                    var dayOffset = GetDayOffsetForMatch(matchIndex, matchesPerTimeSlot);
                    var matchTime = GetMatchTimeForDay(round.RoundStartDate.AddDays(dayOffset).DayOfWeek);
                    
                    match.ScheduledDateTime = round.RoundStartDate.AddDays(dayOffset).Add(matchTime);
                    
                    // Set venue if not already set by specialty match
                    if (string.IsNullOrEmpty(match.Venue))
                    {
                        match.Venue = AFLVenues.GetHomeVenueForTeam(match.HomeTeam).Name;
                    }
                    
                    matchIndex++;
                }
            }
        }
        
        #region Helper Methods
        
        private int CalculateRoundForDate(DateTime targetDate, DateTime seasonStart)
        {
            var weeksDifference = (int)Math.Ceiling((targetDate - seasonStart).TotalDays / 7.0);
            return Math.Max(1, Math.Min(weeksDifference + 1, STANDARD_ROUND_COUNT));
        }
        
        private bool ShouldPlayTwice(TeamId team1, TeamId team2, FixtureGenerationOptions options)
        {
            // Traditional rivalries typically play twice
            if (IsRivalry(team1, team2)) return true;
            
            // State-based teams often play interstate teams twice
            if (AreFromDifferentStates(team1, team2)) return _random.NextDouble() < 0.3;
            
            return _random.NextDouble() < 0.1; // 10% chance for other matchups
        }
        
        private bool IsRivalry(TeamId team1, TeamId team2)
        {
            var rivalries = new[]
            {
                (TeamId.Adelaide, TeamId.PortAdelaide),
                (TeamId.Collingwood, TeamId.Carlton),
                (TeamId.Essendon, TeamId.Carlton),
                (TeamId.Geelong, TeamId.Hawthorn),
                (TeamId.WestCoast, TeamId.Fremantle),
                (TeamId.Brisbane, TeamId.GoldCoast)
            };
            
            return rivalries.Any(r => 
                (r.Item1 == team1 && r.Item2 == team2) || 
                (r.Item1 == team2 && r.Item2 == team1));
        }
        
        private bool AreFromDifferentStates(TeamId team1, TeamId team2)
        {
            // Simplified state grouping - in reality you'd have a more detailed mapping
            return GetTeamState(team1) != GetTeamState(team2);
        }
        
        private string GetTeamState(TeamId teamId)
        {
            if (teamId == TeamId.Adelaide || teamId == TeamId.PortAdelaide)
                return "SA";
            if (teamId == TeamId.Brisbane || teamId == TeamId.GoldCoast)
                return "QLD";
            if (teamId == TeamId.WestCoast || teamId == TeamId.Fremantle)
                return "WA";
            return "VIC";
        }
        
        private bool ValidateMatchupsCount(List<TeamMatchup> matchups, int targetPerTeam)
        {
            var teamCounts = new Dictionary<TeamId, int>();
            foreach (var matchup in matchups)
            {
                teamCounts[matchup.HomeTeam] = teamCounts.GetValueOrDefault(matchup.HomeTeam) + 1;
                teamCounts[matchup.AwayTeam] = teamCounts.GetValueOrDefault(matchup.AwayTeam) + 1;
            }
            
            return _allTeams.All(team => teamCounts.GetValueOrDefault(team) == targetPerTeam);
        }
        
        private void AddAdditionalMatchups(List<TeamMatchup> matchups, int targetPerTeam)
        {
            // Find teams that need more matches and pair them up
            var teamCounts = new Dictionary<TeamId, int>();
            foreach (var matchup in matchups)
            {
                teamCounts[matchup.HomeTeam] = teamCounts.GetValueOrDefault(matchup.HomeTeam) + 1;
                teamCounts[matchup.AwayTeam] = teamCounts.GetValueOrDefault(matchup.AwayTeam) + 1;
            }
            
            var teamsNeedingMatches = _allTeams.Where(t => teamCounts.GetValueOrDefault(t) < targetPerTeam).ToList();
            
            while (teamsNeedingMatches.Count >= 2)
            {
                var team1 = teamsNeedingMatches[0];
                var team2 = teamsNeedingMatches[1];
                
                matchups.Add(new TeamMatchup(team1, team2));
                
                teamCounts[team1] = teamCounts.GetValueOrDefault(team1) + 1;
                teamCounts[team2] = teamCounts.GetValueOrDefault(team2) + 1;
                
                if (teamCounts[team1] >= targetPerTeam) teamsNeedingMatches.Remove(team1);
                if (teamCounts[team2] >= targetPerTeam) teamsNeedingMatches.Remove(team2);
            }
        }
        
        private TeamMatchup FindBestMatchupForRound(List<TeamMatchup> availableMatchups, HashSet<TeamId> teamsAlreadyPlaying)
        {
            return availableMatchups.FirstOrDefault(m => 
                !teamsAlreadyPlaying.Contains(m.HomeTeam) && 
                !teamsAlreadyPlaying.Contains(m.AwayTeam));
        }
        
        private Dictionary<DayOfWeek, int> DistributeMatchesAcrossWeekend(int totalMatches)
        {
            // Typical AFL weekend distribution
            return new Dictionary<DayOfWeek, int>
            {
                [DayOfWeek.Friday] = Math.Min(1, totalMatches),
                [DayOfWeek.Saturday] = Math.Min(4, Math.Max(0, totalMatches - 1)),
                [DayOfWeek.Sunday] = Math.Max(0, totalMatches - 5)
            };
        }
        
        private int GetDayOffsetForMatch(int matchIndex, Dictionary<DayOfWeek, int> distribution)
        {
            int currentIndex = 0;
            
            if (matchIndex < distribution[DayOfWeek.Friday])
                return 1; // Friday is 1 day after Thursday round start
                
            currentIndex += distribution[DayOfWeek.Friday];
            if (matchIndex < currentIndex + distribution[DayOfWeek.Saturday])
                return 2; // Saturday is 2 days after Thursday
                
            return 3; // Sunday is 3 days after Thursday
        }
        
        private TimeSpan GetMatchTimeForDay(DayOfWeek dayOfWeek)
        {
            return AFLCalendarUtilities.GetTypicalMatchTime(dayOfWeek);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a team matchup pairing
    /// </summary>
    public class TeamMatchup
    {
        public TeamId HomeTeam { get; set; }
        public TeamId AwayTeam { get; set; }
        
        public TeamMatchup(TeamId homeTeam, TeamId awayTeam)
        {
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
        }
    }
    
    /// <summary>
    /// Options for fixture generation
    /// </summary>
    public class FixtureGenerationOptions
    {
        public int TotalRounds { get; set; } = 24;
        public int ByeRoundStart { get; set; } = 12;
        public int ByeRoundEnd { get; set; } = 15;
        
        public bool IncludeSeasonOpener { get; set; } = true;
        public bool IncludeAnzacDay { get; set; } = true;
        public bool IncludeKingsBirthday { get; set; } = true;
        public bool IncludeEasterMonday { get; set; } = true;
        public bool IncludeRivalryMatches { get; set; } = true;
        
        public static FixtureGenerationOptions Default => new FixtureGenerationOptions();
    }
}