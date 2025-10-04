using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Infrastructure.Data.DTOs
{
    /// <summary>
    /// Data transfer object for SeasonCalendar persistence
    /// </summary>
    public class SeasonCalendarDto
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int TotalRounds { get; set; }
        public DateTime SeasonStart { get; set; }
        public DateTime SeasonEnd { get; set; }
        public SeasonState CurrentState { get; set; }
        public int CurrentRound { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public List<SeasonRoundDto> Rounds { get; set; } = new List<SeasonRoundDto>();
        public List<SpecialtyMatchDto> SpecialtyMatches { get; set; } = new List<SpecialtyMatchDto>();
        public List<OffSeasonEventDto> OffSeasonEvents { get; set; } = new List<OffSeasonEventDto>();
        public ByeRoundConfigurationDto? ByeConfiguration { get; set; }
    }

    /// <summary>
    /// Data transfer object for SeasonRound persistence
    /// </summary>
    public class SeasonRoundDto
    {
        public int Id { get; set; }
        public int SeasonCalendarId { get; set; }
        public int RoundNumber { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public DateTime RoundStartDate { get; set; }
        public DateTime RoundEndDate { get; set; }
        public RoundType RoundType { get; set; }
        public string TeamsOnByeJson { get; set; } = "[]"; // JSON serialized TeamId array
        
        // Navigation properties
        public List<ScheduledMatchDto> Matches { get; set; } = new List<ScheduledMatchDto>();
    }

    /// <summary>
    /// Data transfer object for ScheduledMatch persistence
    /// </summary>
    public class ScheduledMatchDto
    {
        public int Id { get; set; }
        public int SeasonRoundId { get; set; }
        public int MatchId { get; set; }
        public int RoundNumber { get; set; }
        public int HomeTeamId { get; set; } // Store as int for TeamId enum
        public int AwayTeamId { get; set; } // Store as int for TeamId enum
        public DateTime ScheduledDateTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public MatchStatus Status { get; set; }
        public int Weather { get; set; } // Store as int for Weather enum
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string MatchTagsJson { get; set; } = "[]"; // JSON serialized string array
    }

    /// <summary>
    /// Data transfer object for SpecialtyMatch persistence
    /// </summary>
    public class SpecialtyMatchDto
    {
        public int Id { get; set; }
        public int SeasonCalendarId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int HomeTeamId { get; set; } // Store as int for TeamId enum
        public int AwayTeamId { get; set; } // Store as int for TeamId enum
        public int RoundNumber { get; set; }
        public DateTime TargetDate { get; set; }
        public string Venue { get; set; } = string.Empty;
        public SpecialtyMatchType Type { get; set; }
        public bool IsFlexibleDate { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// Data transfer object for OffSeasonEvent persistence
    /// </summary>
    public class OffSeasonEventDto
    {
        public int Id { get; set; }
        public int SeasonCalendarId { get; set; }
        public OffSeasonEventType EventType { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string PropertiesJson { get; set; } = "{}"; // JSON serialized properties
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Data transfer object for ByeRoundConfiguration persistence
    /// </summary>
    public class ByeRoundConfigurationDto
    {
        public int Id { get; set; }
        public int SeasonCalendarId { get; set; }
        public int StartRound { get; set; }
        public int EndRound { get; set; }
        public int TeamsPerByeRound { get; set; }
        public string ByeRoundAssignmentsJson { get; set; } = "{}"; // JSON serialized dictionary
    }
}