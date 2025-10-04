using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Utilities;

namespace AFLCoachSim.Core.Season.Domain.Services
{
    /// <summary>
    /// Factory for creating standard AFL off-season events
    /// </summary>
    public static class OffSeasonEventFactory
    {
        /// <summary>
        /// Creates all standard off-season events for a given season
        /// </summary>
        public static List<OffSeasonEvent> CreateStandardOffSeasonEvents(int seasonId, int year)
        {
            var events = new List<OffSeasonEvent>();

            // Calculate key off-season dates
            var grandFinalDate = AFLCalendarUtilities.CalculateGrandFinalWeekend(year);
            var brownlowDate = AFLCalendarUtilities.CalculateBrownlowMedalNight(year);
            var tradePeriodStart = AFLCalendarUtilities.CalculateTradePeriodStart(year);
            var tradePeriodEnd = AFLCalendarUtilities.CalculateTradePeriodEnd(year);
            var draftDate = AFLCalendarUtilities.CalculateAFLDraftDate(year);

            // Brownlow Medal Night
            events.Add(new OffSeasonEvent(
                seasonId,
                OffSeasonEventType.BrownlowMedal,
                "Brownlow Medal Night",
                "Annual presentation of the Brownlow Medal to the fairest and best player",
                brownlowDate,
                properties: new Dictionary<string, object>
                {
                    ["Venue"] = "Crown Palladium, Melbourne",
                    ["StartTime"] = "19:30",
                    ["IsGalaEvent"] = true,
                    ["VotesRevealed"] = false
                }
            ));

            // Trade Period
            events.Add(new OffSeasonEvent(
                seasonId,
                OffSeasonEventType.TradePeriod,
                "AFL Trade Period",
                "Official player trading window between clubs",
                tradePeriodStart,
                tradePeriodEnd,
                properties: new Dictionary<string, object>
                {
                    ["TotalTrades"] = 0,
                    ["ActiveTrades"] = new List<string>(),
                    ["DeadlineDay"] = tradePeriodEnd,
                    ["IsActive"] = false
                }
            ));

            // AFL National Draft
            events.Add(new OffSeasonEvent(
                seasonId,
                OffSeasonEventType.Draft,
                "AFL National Draft",
                "Annual selection of young talent by AFL clubs",
                draftDate,
                properties: new Dictionary<string, object>
                {
                    ["Venue"] = "Marvel Stadium, Melbourne",
                    ["TotalPicks"] = 0,
                    ["CompletedRounds"] = 0,
                    ["LiveBidding"] = true,
                    ["AcademyBids"] = new List<string>()
                }
            ));

            // Rookie Draft (usually 1-2 days after National Draft)
            var rookieDraftDate = draftDate.AddDays(1);
            if (rookieDraftDate.DayOfWeek == DayOfWeek.Sunday)
                rookieDraftDate = rookieDraftDate.AddDays(1); // Move to Monday if on Sunday

            events.Add(new OffSeasonEvent(
                seasonId,
                OffSeasonEventType.RookieDraft,
                "AFL Rookie Draft",
                "Draft for rookie-listed players and mature age recruits",
                rookieDraftDate,
                properties: new Dictionary<string, object>
                {
                    ["Venue"] = "AFL House, Melbourne",
                    ["TotalPicks"] = 0,
                    ["MaxListSize"] = 6, // Up to 6 rookie picks per club
                    ["MinimumAge"] = 18
                }
            ));

            // Pre-Season Draft (usually in December)
            var preSeasonDraftDate = new DateTime(year, 12, 15);
            // Move to weekday if weekend
            while (preSeasonDraftDate.DayOfWeek == DayOfWeek.Saturday || preSeasonDraftDate.DayOfWeek == DayOfWeek.Sunday)
            {
                preSeasonDraftDate = preSeasonDraftDate.AddDays(1);
            }

            events.Add(new OffSeasonEvent(
                seasonId,
                OffSeasonEventType.PreSeasonDraft,
                "AFL Pre-Season Draft",
                "Draft for delisted players and supplementary selections",
                preSeasonDraftDate,
                properties: new Dictionary<string, object>
                {
                    ["Venue"] = "AFL House, Melbourne",
                    ["TotalPicks"] = 0,
                    ["EligiblePlayers"] = new List<string>(),
                    ["IsSupplementary"] = true
                }
            ));

            // List Lodgement (typically late January/early February of following year)
            var listLodgementDate = new DateTime(year + 1, 1, 31);
            // Move to weekday if weekend
            while (listLodgementDate.DayOfWeek == DayOfWeek.Saturday || listLodgementDate.DayOfWeek == DayOfWeek.Sunday)
            {
                listLodgementDate = listLodgementDate.AddDays(1);
            }

            events.Add(new OffSeasonEvent(
                seasonId,
                OffSeasonEventType.ListLodgement,
                "Final Team List Lodgement",
                "Deadline for clubs to lodge their final playing lists",
                listLodgementDate,
                properties: new Dictionary<string, object>
                {
                    ["PrimaryListSize"] = 38, // Standard AFL list size
                    ["RookieListSize"] = 6,   // Maximum rookie list size
                    ["DeadlineTime"] = "17:00",
                    ["PenaltyForLate"] = "Draft pick penalties"
                }
            ));

            // Season Launch (typically early March of following year)
            var seasonLaunchDate = AFLCalendarUtilities.CalculateSeasonStart(year + 1).AddDays(-10);
            // Move to Tuesday or Wednesday for media coverage
            while (seasonLaunchDate.DayOfWeek != DayOfWeek.Tuesday && seasonLaunchDate.DayOfWeek != DayOfWeek.Wednesday)
            {
                seasonLaunchDate = seasonLaunchDate.AddDays(1);
            }

            events.Add(new OffSeasonEvent(
                seasonId,
                OffSeasonEventType.SeasonLaunch,
                $"{year + 1} AFL Season Launch",
                $"Official launch of the {year + 1} AFL season",
                seasonLaunchDate,
                properties: new Dictionary<string, object>
                {
                    ["Venue"] = "TBD",
                    ["MediaEvents"] = new List<string>(),
                    ["PlayerRepresentatives"] = new List<string>(),
                    ["SeasonTheme"] = $"{year + 1} Season"
                }
            ));

            return events;
        }

        /// <summary>
        /// Creates a custom off-season event
        /// </summary>
        public static OffSeasonEvent CreateCustomEvent(
            int seasonId,
            OffSeasonEventType eventType,
            string name,
            string description,
            DateTime date,
            DateTime? endDate = null,
            Dictionary<string, object>? properties = null)
        {
            return new OffSeasonEvent(seasonId, eventType, name, description, date, endDate, properties);
        }

        /// <summary>
        /// Updates off-season event properties based on event type
        /// </summary>
        public static void UpdateEventProperties(OffSeasonEvent offSeasonEvent, Dictionary<string, object> updates)
        {
            switch (offSeasonEvent.EventType)
            {
                case OffSeasonEventType.TradePeriod:
                    UpdateTradePeriodProperties(offSeasonEvent, updates);
                    break;
                case OffSeasonEventType.Draft:
                    UpdateDraftProperties(offSeasonEvent, updates);
                    break;
                case OffSeasonEventType.BrownlowMedal:
                    UpdateBrownlowProperties(offSeasonEvent, updates);
                    break;
                default:
                    offSeasonEvent.UpdateProperties(updates);
                    break;
            }
        }

        private static void UpdateTradePeriodProperties(OffSeasonEvent tradePeriod, Dictionary<string, object> updates)
        {
            var validKeys = new HashSet<string>
            {
                "TotalTrades", "ActiveTrades", "DeadlineDay", "IsActive", "MajorTrades", "DeadlineDeals"
            };

            var filteredUpdates = new Dictionary<string, object>();
            foreach (var kvp in updates)
            {
                if (validKeys.Contains(kvp.Key))
                {
                    filteredUpdates[kvp.Key] = kvp.Value;
                }
            }

            tradePeriod.UpdateProperties(filteredUpdates);
        }

        private static void UpdateDraftProperties(OffSeasonEvent draft, Dictionary<string, object> updates)
        {
            var validKeys = new HashSet<string>
            {
                "TotalPicks", "CompletedRounds", "LiveBidding", "AcademyBids", "FatherSonBids", "CurrentPick"
            };

            var filteredUpdates = new Dictionary<string, object>();
            foreach (var kvp in updates)
            {
                if (validKeys.Contains(kvp.Key))
                {
                    filteredUpdates[kvp.Key] = kvp.Value;
                }
            }

            draft.UpdateProperties(filteredUpdates);
        }

        private static void UpdateBrownlowProperties(OffSeasonEvent brownlow, Dictionary<string, object> updates)
        {
            var validKeys = new HashSet<string>
            {
                "VotesRevealed", "Winner", "RunnerUp", "TotalVotes", "Venue", "StartTime"
            };

            var filteredUpdates = new Dictionary<string, object>();
            foreach (var kvp in updates)
            {
                if (validKeys.Contains(kvp.Key))
                {
                    filteredUpdates[kvp.Key] = kvp.Value;
                }
            }

            brownlow.UpdateProperties(filteredUpdates);
        }
    }
}