using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Match.Weather;
using AFLCoachSim.Core.Engine.Match;

namespace AFLCoachSim.Core.Season.Domain.Entities
{
    /// <summary>
    /// Represents a complete AFL season calendar with all fixtures and scheduling
    /// </summary>
    public class SeasonCalendar
    {
        public int Year { get; set; }
        public int TotalRounds { get; set; } = 24; // Standard AFL season length
        public DateTime SeasonStart { get; set; } // Second Thursday of March
        public DateTime SeasonEnd { get; set; }   // Final weekend of September (or early October)
        public List<SeasonRound> Rounds { get; set; } = new List<SeasonRound>();
        public List<SpecialtyMatch> SpecialtyMatches { get; set; } = new List<SpecialtyMatch>();
        public List<OffSeasonEvent> OffSeasonEvents { get; set; } = new List<OffSeasonEvent>();
        public ByeRoundConfiguration ByeConfiguration { get; set; }
        public SeasonState CurrentState { get; set; } = SeasonState.NotStarted;
        public int CurrentRound { get; set; } = 1;
        
        /// <summary>
        /// Get all matches for a specific round
        /// </summary>
        public IEnumerable<ScheduledMatch> GetRoundMatches(int roundNumber)
        {
            var round = Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
            return round?.Matches ?? Enumerable.Empty<ScheduledMatch>();
        }
        
        /// <summary>
        /// Get all matches for a specific team
        /// </summary>
        public IEnumerable<ScheduledMatch> GetTeamMatches(TeamId teamId)
        {
            return Rounds.SelectMany(r => r.Matches)
                        .Where(m => m.HomeTeam == teamId || m.AwayTeam == teamId);
        }
        
        /// <summary>
        /// Get the team's next match
        /// </summary>
        public ScheduledMatch GetNextMatch(TeamId teamId)
        {
            return GetTeamMatches(teamId)
                   .Where(m => m.Status == MatchStatus.Scheduled)
                   .OrderBy(m => m.ScheduledDateTime)
                   .FirstOrDefault();
        }
        
        /// <summary>
        /// Check if a team has a bye in a specific round
        /// </summary>
        public bool HasBye(TeamId teamId, int roundNumber)
        {
            var round = Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
            return round != null && round.IsTeamOnBye(teamId);
        }
        
        /// <summary>
        /// Get specialty matches for a specific round
        /// </summary>
        public IEnumerable<SpecialtyMatch> GetRoundSpecialtyMatches(int roundNumber)
        {
            return SpecialtyMatches.Where(sm => sm.RoundNumber == roundNumber);
        }
        
        /// <summary>
        /// Get off-season events by type
        /// </summary>
        public IEnumerable<OffSeasonEvent> GetOffSeasonEventsByType(OffSeasonEventType eventType)
        {
            return OffSeasonEvents.Where(e => e.EventType == eventType);
        }
        
        /// <summary>
        /// Get the next off-season event
        /// </summary>
        public OffSeasonEvent GetNextOffSeasonEvent(DateTime currentDate)
        {
            return OffSeasonEvents
                   .Where(e => !e.IsCompleted && e.Date >= currentDate.Date)
                   .OrderBy(e => e.Date)
                   .FirstOrDefault();
        }
        
        /// <summary>
        /// Get active off-season events
        /// </summary>
        public IEnumerable<OffSeasonEvent> GetActiveOffSeasonEvents(DateTime currentDate)
        {
            return OffSeasonEvents.Where(e => e.IsOngoing(currentDate));
        }
        
        /// <summary>
        /// Validate the season calendar for consistency
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Check round count
            if (Rounds.Count != TotalRounds)
            {
                result.AddError($"Expected {TotalRounds} rounds, found {Rounds.Count}");
            }
            
            // Check each team plays correct number of matches
            var teamCounts = new Dictionary<TeamId, int>();
            foreach (var match in Rounds.SelectMany(r => r.Matches))
            {
                teamCounts[match.HomeTeam] = teamCounts.GetValueOrDefault(match.HomeTeam) + 1;
                teamCounts[match.AwayTeam] = teamCounts.GetValueOrDefault(match.AwayTeam) + 1;
            }
            
            foreach (var team in TeamId.GetAllTeams())
            {
                var expectedMatches = TotalRounds - 1; // Account for bye rounds
                if (teamCounts.GetValueOrDefault(team) != expectedMatches)
                {
                    result.AddError($"Team {team} has {teamCounts.GetValueOrDefault(team)} matches, expected {expectedMatches}");
                }
            }
            
            // Check bye rounds
            if (ByeConfiguration != null)
            {
                var byeValidation = ByeConfiguration.Validate();
                result.AddErrors(byeValidation.Errors);
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Represents a single round in the AFL season
    /// </summary>
    public class SeasonRound
    {
        public int RoundNumber { get; set; }
        public string RoundName { get; set; } // e.g., "Round 1", "Finals Week 1", "Grand Final"
        public DateTime RoundStartDate { get; set; }
        public DateTime RoundEndDate { get; set; }
        public List<ScheduledMatch> Matches { get; set; } = new List<ScheduledMatch>();
        public List<TeamId> TeamsOnBye { get; set; } = new List<TeamId>();
        public RoundType RoundType { get; set; } = RoundType.Regular;
        
        /// <summary>
        /// Check if a team is on bye this round
        /// </summary>
        public bool IsTeamOnBye(TeamId teamId)
        {
            return TeamsOnBye.Contains(teamId);
        }
        
        /// <summary>
        /// Get all teams playing in this round
        /// </summary>
        public IEnumerable<TeamId> GetPlayingTeams()
        {
            return Matches.SelectMany(m => new[] { m.HomeTeam, m.AwayTeam });
        }
        
        /// <summary>
        /// Check if the round is complete (all matches finished)
        /// </summary>
        public bool IsComplete()
        {
            return Matches.All(m => m.Status == MatchStatus.Completed);
        }
    }
    
    /// <summary>
    /// Represents a scheduled match with timing and context
    /// </summary>
    public class ScheduledMatch
    {
        public int MatchId { get; set; }
        public int RoundNumber { get; set; }
        public TeamId HomeTeam { get; set; }
        public TeamId AwayTeam { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public string Venue { get; set; }
        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
        public Weather Weather { get; set; } = Weather.Clear;
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public List<string> MatchTags { get; set; } = new List<string>(); // e.g., "Prime Time", "Rivalry", "Derby"
        
        /// <summary>
        /// Check if this is a specialty match
        /// </summary>
        public bool IsSpecialtyMatch => MatchTags.Any();
        
        /// <summary>
        /// Get the result if match is completed
        /// </summary>
        public MatchResult? GetResult()
        {
            if (Status != MatchStatus.Completed || !HomeScore.HasValue || !AwayScore.HasValue)
                return null;
                
            return new MatchResult
            {
                HomeTeam = HomeTeam,
                AwayTeam = AwayTeam,
                HomeScore = HomeScore.Value,
                AwayScore = AwayScore.Value,
                Winner = HomeScore > AwayScore ? HomeTeam : (AwayScore > HomeScore ? AwayTeam : TeamId.None),
                Margin = Math.Abs(HomeScore.Value - AwayScore.Value)
            };
        }
    }
    
    /// <summary>
    /// Represents specialty matches with specific requirements
    /// </summary>
    public class SpecialtyMatch
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TeamId HomeTeam { get; set; }
        public TeamId AwayTeam { get; set; }
        public int RoundNumber { get; set; }
        public DateTime TargetDate { get; set; } // Specific date requirement
        public string Venue { get; set; }
        public SpecialtyMatchType Type { get; set; }
        public bool IsFlexibleDate { get; set; } = false; // Can move if date conflicts
        public int Priority { get; set; } = 1; // Higher number = higher priority
        
        /// <summary>
        /// Convert to scheduled match
        /// </summary>
        public ScheduledMatch ToScheduledMatch(int matchId, DateTime actualDateTime)
        {
            return new ScheduledMatch
            {
                MatchId = matchId,
                RoundNumber = RoundNumber,
                HomeTeam = HomeTeam,
                AwayTeam = AwayTeam,
                ScheduledDateTime = actualDateTime,
                Venue = Venue,
                MatchTags = new List<string> { Type.ToString(), Name }
            };
        }
    }
    
    /// <summary>
    /// Configuration for bye rounds
    /// </summary>
    public class ByeRoundConfiguration
    {
        public int StartRound { get; set; } = 12; // Usually around mid-season
        public int EndRound { get; set; } = 15;   // Span 4 rounds typically
        public int TeamsPerByeRound { get; set; } = 6; // 6 teams on bye each round
        public Dictionary<int, List<TeamId>> ByeRoundAssignments { get; set; } = new Dictionary<int, List<TeamId>>();
        
        /// <summary>
        /// Validate bye configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            var totalTeams = TeamId.GetAllTeams().Length;
            
            // Check all teams have exactly one bye
            var allByeTeams = ByeRoundAssignments.Values.SelectMany(teams => teams).ToList();
            var teamByeCounts = allByeTeams.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
            
            foreach (var team in TeamId.GetAllTeams())
            {
                var byeCount = teamByeCounts.GetValueOrDefault(team, 0);
                if (byeCount != 1)
                {
                    result.AddError($"Team {team} has {byeCount} byes, expected exactly 1");
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Match result data
    /// </summary>
    public class MatchResult
    {
        public TeamId HomeTeam { get; set; }
        public TeamId AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public TeamId Winner { get; set; }
        public int Margin { get; set; }
        public bool IsDraw => Winner == TeamId.None;
    }
    
    /// <summary>
    /// Validation result helper
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsValid => !Errors.Any();
        
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        
        public void AddErrors(IEnumerable<string> errors)
        {
            Errors.AddRange(errors);
        }
    }
}