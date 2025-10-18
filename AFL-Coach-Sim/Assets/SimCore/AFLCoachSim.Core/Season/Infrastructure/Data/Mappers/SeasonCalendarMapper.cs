using System;
using System.Collections.Generic;
using System.Linq;
// Removed UnityEngine dependency - using simple string concatenation for arrays
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Infrastructure.Data.DTOs;
using AFLCoachSim.Core.Engine.Match.Weather;

namespace AFLCoachSim.Core.Season.Infrastructure.Data.Mappers
{
    // Simple string-based serialization (no Unity dependencies)

    /// <summary>
    /// Mapper for converting between domain entities and DTOs
    /// </summary>
    public static class SeasonCalendarMapper
    {
        // SeasonCalendar Mappings
        public static SeasonCalendarDto ToDto(this SeasonCalendar calendar)
        {
            return new SeasonCalendarDto
            {
                Year = calendar.Year,
                TotalRounds = calendar.TotalRounds,
                SeasonStart = calendar.SeasonStart,
                SeasonEnd = calendar.SeasonEnd,
                CurrentState = calendar.CurrentState,
                CurrentRound = calendar.CurrentRound,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Rounds = calendar.Rounds.Select(ToDto).ToList(),
                SpecialtyMatches = calendar.SpecialtyMatches.Select(ToDto).ToList(),
                OffSeasonEvents = calendar.OffSeasonEvents.Select(ToDto).ToList(),
                ByeConfiguration = calendar.ByeConfiguration?.ToDto()
            };
        }

        public static SeasonCalendar ToDomain(this SeasonCalendarDto dto)
        {
            var calendar = new SeasonCalendar
            {
                Year = dto.Year,
                TotalRounds = dto.TotalRounds,
                SeasonStart = dto.SeasonStart,
                SeasonEnd = dto.SeasonEnd,
                CurrentState = dto.CurrentState,
                CurrentRound = dto.CurrentRound,
                Rounds = dto.Rounds.Select(ToDomain).ToList(),
                SpecialtyMatches = dto.SpecialtyMatches.Select(ToDomain).ToList(),
                OffSeasonEvents = dto.OffSeasonEvents.Select(ToDomain).ToList(),
                ByeConfiguration = dto.ByeConfiguration?.ToDomain()
            };

            return calendar;
        }

        // SeasonRound Mappings
        public static SeasonRoundDto ToDto(this SeasonRound round)
        {
            return new SeasonRoundDto
            {
                RoundNumber = round.RoundNumber,
                RoundName = round.RoundName,
                RoundStartDate = round.RoundStartDate,
                RoundEndDate = round.RoundEndDate,
                RoundType = round.RoundType,
                TeamsOnByeJson = string.Join(",", round.TeamsOnBye.Cast<int>()),
                Matches = round.Matches.Select(ToDto).ToList()
            };
        }

        public static SeasonRound ToDomain(this SeasonRoundDto dto)
        {
            var teamsOnBye = new List<TeamId>();
            try
            {
                if (!string.IsNullOrEmpty(dto.TeamsOnByeJson))
                {
                    var teamIds = dto.TeamsOnByeJson.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse);
                    teamsOnBye = teamIds.Cast<TeamId>().ToList();
                }
            }
            catch (System.Exception)
            {
                // If parsing fails, default to empty list
                teamsOnBye = new List<TeamId>();
            }

            return new SeasonRound
            {
                RoundNumber = dto.RoundNumber,
                RoundName = dto.RoundName,
                RoundStartDate = dto.RoundStartDate,
                RoundEndDate = dto.RoundEndDate,
                RoundType = dto.RoundType,
                TeamsOnBye = teamsOnBye,
                Matches = dto.Matches.Select(ToDomain).ToList()
            };
        }

        // ScheduledMatch Mappings
        public static ScheduledMatchDto ToDto(this ScheduledMatch match)
        {
            return new ScheduledMatchDto
            {
                MatchId = match.MatchId,
                RoundNumber = match.RoundNumber,
                HomeTeamId = (int)match.HomeTeam,
                AwayTeamId = (int)match.AwayTeam,
                ScheduledDateTime = match.ScheduledDateTime,
                Venue = match.Venue,
                Status = match.Status,
                Weather = (int)match.Weather,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                MatchTagsJson = string.Join("|", match.MatchTags)
            };
        }

        public static ScheduledMatch ToDomain(this ScheduledMatchDto dto)
        {
            var matchTags = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(dto.MatchTagsJson))
                {
                    matchTags = dto.MatchTagsJson.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }
            }
            catch (System.Exception)
            {
                matchTags = new List<string>();
            }

            return new ScheduledMatch
            {
                MatchId = dto.MatchId,
                RoundNumber = dto.RoundNumber,
                HomeTeam = (TeamId)dto.HomeTeamId,
                AwayTeam = (TeamId)dto.AwayTeamId,
                ScheduledDateTime = dto.ScheduledDateTime,
                Venue = dto.Venue,
                Status = dto.Status,
                Weather = (Weather)dto.Weather,
                HomeScore = dto.HomeScore,
                AwayScore = dto.AwayScore,
                MatchTags = matchTags
            };
        }

        // SpecialtyMatch Mappings
        public static SpecialtyMatchDto ToDto(this SpecialtyMatch match)
        {
            return new SpecialtyMatchDto
            {
                Name = match.Name,
                Description = match.Description,
                HomeTeamId = (int)match.HomeTeam,
                AwayTeamId = (int)match.AwayTeam,
                RoundNumber = match.RoundNumber,
                TargetDate = match.TargetDate,
                Venue = match.Venue,
                Type = match.Type,
                IsFlexibleDate = match.IsFlexibleDate,
                Priority = match.Priority
            };
        }

        public static SpecialtyMatch ToDomain(this SpecialtyMatchDto dto)
        {
            return new SpecialtyMatch
            {
                Name = dto.Name,
                Description = dto.Description,
                HomeTeam = (TeamId)dto.HomeTeamId,
                AwayTeam = (TeamId)dto.AwayTeamId,
                RoundNumber = dto.RoundNumber,
                TargetDate = dto.TargetDate,
                Venue = dto.Venue,
                Type = dto.Type,
                IsFlexibleDate = dto.IsFlexibleDate,
                Priority = dto.Priority
            };
        }

        // OffSeasonEvent Mappings (simplified to avoid complex Dictionary serialization)
        public static OffSeasonEventDto ToDto(this OffSeasonEvent offSeasonEvent)
        {
            return new OffSeasonEventDto
            {
                EventType = offSeasonEvent.EventType,
                Name = offSeasonEvent.Name,
                Description = offSeasonEvent.Description,
                Date = offSeasonEvent.Date,
                EndDate = offSeasonEvent.EndDate,
                IsActive = offSeasonEvent.IsActive,
                IsCompleted = offSeasonEvent.IsCompleted,
                CompletedDate = offSeasonEvent.CompletedDate,
                PropertiesJson = "{}", // Simplified - empty properties for Unity compatibility
                CreatedAt = offSeasonEvent.CreatedAt,
                UpdatedAt = offSeasonEvent.UpdatedAt
            };
        }

        public static OffSeasonEvent ToDomain(this OffSeasonEventDto dto)
        {
            var properties = new Dictionary<string, object>(); // Empty properties for simplicity

            // Create the event using simplified constructor
            var offSeasonEvent = new OffSeasonEvent(
                dto.SeasonCalendarId,
                dto.EventType,
                dto.Name,
                dto.Description,
                dto.Date,
                dto.EndDate,
                properties
            );

            // Set state properties using methods if available
            if (dto.IsActive)
            {
                try { offSeasonEvent.Start(); } catch { /* Ignore if already active */ }
            }
            
            if (dto.IsCompleted)
            {
                try 
                { 
                    if (!offSeasonEvent.IsActive) offSeasonEvent.Start();
                    offSeasonEvent.Complete(); 
                } 
                catch { /* Ignore state transition errors */ }
            }

            return offSeasonEvent;
        }

        // ByeRoundConfiguration Mappings (simplified)
        public static ByeRoundConfigurationDto ToDto(this ByeRoundConfiguration config)
        {
            return new ByeRoundConfigurationDto
            {
                StartRound = config.StartRound,
                EndRound = config.EndRound,
                TeamsPerByeRound = config.TeamsPerByeRound,
                ByeRoundAssignmentsJson = "{}" // Simplified for Unity compatibility
            };
        }

        public static ByeRoundConfiguration ToDomain(this ByeRoundConfigurationDto dto)
        {
            var assignments = new Dictionary<int, List<TeamId>>(); // Empty assignments for simplicity

            return new ByeRoundConfiguration
            {
                StartRound = dto.StartRound,
                EndRound = dto.EndRound,
                TeamsPerByeRound = dto.TeamsPerByeRound,
                ByeRoundAssignments = assignments
            };
        }
    }
}