using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Infrastructure.Data.DTOs;

namespace AFLCoachSim.Core.Season.Infrastructure.Data.Mappers
{
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
                TeamsOnByeJson = JsonSerializer.Serialize(round.TeamsOnBye.Cast<int>().ToArray()),
                Matches = round.Matches.Select(ToDto).ToList()
            };
        }

        public static SeasonRound ToDomain(this SeasonRoundDto dto)
        {
            var teamsOnBye = new List<TeamId>();
            try
            {
                var teamIds = JsonSerializer.Deserialize<int[]>(dto.TeamsOnByeJson ?? "[]");
                teamsOnBye = teamIds?.Cast<TeamId>().ToList() ?? new List<TeamId>();
            }
            catch (JsonException)
            {
                // If JSON deserialization fails, default to empty list
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
                MatchTagsJson = JsonSerializer.Serialize(match.MatchTags.ToArray())
            };
        }

        public static ScheduledMatch ToDomain(this ScheduledMatchDto dto)
        {
            var matchTags = new List<string>();
            try
            {
                matchTags = JsonSerializer.Deserialize<string[]>(dto.MatchTagsJson ?? "[]")?.ToList() ?? new List<string>();
            }
            catch (JsonException)
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

        // OffSeasonEvent Mappings
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
                PropertiesJson = JsonSerializer.Serialize(offSeasonEvent.Properties),
                CreatedAt = offSeasonEvent.CreatedAt,
                UpdatedAt = offSeasonEvent.UpdatedAt
            };
        }

        public static OffSeasonEvent ToDomain(this OffSeasonEventDto dto)
        {
            var properties = new Dictionary<string, object>();
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(dto.PropertiesJson ?? "{}");
                properties = JsonElementToDictionary(jsonElement);
            }
            catch (JsonException)
            {
                properties = new Dictionary<string, object>();
            }

            // Create the event using reflection to set private properties
            var offSeasonEvent = new OffSeasonEvent(
                dto.SeasonCalendarId,
                dto.EventType,
                dto.Name,
                dto.Description,
                dto.Date,
                dto.EndDate,
                properties
            );

            // Set state properties using reflection if needed
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

        // ByeRoundConfiguration Mappings
        public static ByeRoundConfigurationDto ToDto(this ByeRoundConfiguration config)
        {
            var assignments = config.ByeRoundAssignments
                .ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value.Cast<int>().ToArray()
                );

            return new ByeRoundConfigurationDto
            {
                StartRound = config.StartRound,
                EndRound = config.EndRound,
                TeamsPerByeRound = config.TeamsPerByeRound,
                ByeRoundAssignmentsJson = JsonSerializer.Serialize(assignments)
            };
        }

        public static ByeRoundConfiguration ToDomain(this ByeRoundConfigurationDto dto)
        {
            var assignments = new Dictionary<int, List<TeamId>>();
            try
            {
                var jsonAssignments = JsonSerializer.Deserialize<Dictionary<string, int[]>>(dto.ByeRoundAssignmentsJson ?? "{}");
                if (jsonAssignments != null)
                {
                    assignments = jsonAssignments.ToDictionary(
                        kvp => int.Parse(kvp.Key),
                        kvp => kvp.Value.Cast<TeamId>().ToList()
                    );
                }
            }
            catch (JsonException)
            {
                assignments = new Dictionary<int, List<TeamId>>();
            }

            return new ByeRoundConfiguration
            {
                StartRound = dto.StartRound,
                EndRound = dto.EndRound,
                TeamsPerByeRound = dto.TeamsPerByeRound,
                ByeRoundAssignments = assignments
            };
        }

        // Helper method to convert JsonElement to Dictionary<string, object>
        private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var property in element.EnumerateObject())
            {
                dictionary[property.Name] = JsonElementToObject(property.Value);
            }

            return dictionary;
        }

        private static object JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Object => JsonElementToDictionary(element),
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
                _ => element.ToString()
            };
        }
    }
}