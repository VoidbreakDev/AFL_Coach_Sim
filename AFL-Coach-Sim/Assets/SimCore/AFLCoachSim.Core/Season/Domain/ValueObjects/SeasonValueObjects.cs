using System;

namespace AFLCoachSim.Core.Season.Domain.ValueObjects
{
    /// <summary>
    /// Represents the current state of a season
    /// </summary>
    public enum SeasonState
    {
        NotStarted,
        InProgress,
        Finals,
        GrandFinal,
        BrownlowNight,
        TradePeriod,
        OffSeason,
        Complete,
        Cancelled
    }
    
    /// <summary>
    /// Types of rounds in the season
    /// </summary>
    public enum RoundType
    {
        Regular,
        Finals,
        GrandFinal,
        PreSeason
    }
    
    /// <summary>
    /// Status of a scheduled match
    /// </summary>
    public enum MatchStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Postponed,
        Cancelled
    }
    
    /// <summary>
    /// Types of specialty matches
    /// </summary>
    public enum SpecialtyMatchType
    {
        SeasonOpener,     // Carlton vs Richmond - Second Thursday of March
        AnzacDay,         // Collingwood vs Essendon - April 25th
        KingsBirthday,    // Collingwood vs Melbourne - Second Monday of June
        EasterMonday,     // Geelong vs Hawthorn - Easter Monday
        Showdown,         // Adelaide vs Port Adelaide
        Derby,            // West Coast vs Fremantle
        QClash,           // Brisbane vs Gold Coast
        DreamTime,        // Richmond vs Essendon - Sir Doug Nicholls Round
        PrimeTime,        // Thursday/Friday night featured matches
        Rivalry,          // Traditional rivalry matches
        GrandFinal,       // The ultimate match
        Other
    }
    
    /// <summary>
    /// Types of off-season events
    /// </summary>
    public enum OffSeasonEventType
    {
        BrownlowMedal,     // Brownlow Medal Night ceremony
        TradePeriod,       // Player trading window
        Draft,             // AFL National Draft
        RookieDraft,       // Rookie Draft
        PreSeasonDraft,    // Pre-season Draft
        ListLodgement,     // Final team list lodgement
        SeasonLaunch,      // Official season launch
        Other
    }
    
    /// <summary>
    /// Status of a trade period
    /// </summary>
    public enum TradePeriodStatus
    {
        NotStarted,        // Trade period hasn't begun
        Open,             // Trade period is active
        Closed,           // Trade period has ended
        Extended,         // Deadline has been extended
        Suspended         // Temporarily suspended
    }
    
    /// <summary>
    /// Status of an individual trade
    /// </summary>
    public enum TradeStatus
    {
        Proposed,         // Trade has been proposed
        UnderReview,      // Trade is being reviewed by AFL
        Approved,         // Trade has been approved by AFL
        Completed,        // Trade has been executed
        Rejected,         // Trade was rejected
        Withdrawn,        // Trade was withdrawn
        Failed            // Trade failed to complete
    }
    
    /// <summary>
    /// Status of a trade offer
    /// </summary>
    public enum TradeOfferStatus
    {
        Pending,          // Waiting for response
        Accepted,         // Offer has been accepted
        Rejected,         // Offer has been rejected
        Withdrawn,        // Offer has been withdrawn
        Expired,          // Offer has expired
        Countered         // Counter-offer has been made
    }
    
    /// <summary>
    /// Types of trade assets
    /// </summary>
    public enum TradeAssetType
    {
        Player,           // Current AFL player
        DraftPick,        // Current year draft pick
        FuturePick,       // Future year draft pick
        Cash,             // Cash consideration (rarely used in AFL)
        Other             // Other considerations
    }
    
    /// <summary>
    /// Types of trades based on assets involved
    /// </summary>
    public enum TradeType
    {
        PlayerOnly,       // Only players involved
        PickOnly,         // Only draft picks involved
        PlayerAndPick,    // Both players and picks
        FuturePicksOnly,  // Only future picks
        Complex,          // Multiple asset types
        Other             // Other trade types
    }
    
    /// <summary>
    /// Types of AFL drafts
    /// </summary>
    public enum DraftType
    {
        National,         // Main AFL National Draft
        Rookie,           // Rookie Draft
        PreSeason,        // Pre-season Draft
        MidSeason,        // Mid-season Rookie Draft
        Supplemental      // Supplemental Draft
    }
    
    /// <summary>
    /// Status of a draft period
    /// </summary>
    public enum DraftStatus
    {
        NotStarted,       // Draft hasn't begun
        InProgress,       // Draft is currently active
        Paused,           // Draft is temporarily paused
        Completed,        // Draft has finished
        Cancelled,        // Draft was cancelled
        Postponed         // Draft has been postponed
    }
    
    /// <summary>
    /// Status of an individual draft pick
    /// </summary>
    public enum DraftPickStatus
    {
        Available,        // Pick is available to be made
        InProgress,       // Pick is currently being made
        Completed,        // Pick has been made
        Passed,           // Pick was passed/forfeited
        Traded,           // Pick has been traded
        Forfeited,        // Pick was forfeited due to sanctions
        Matched           // Pick was matched (Academy/Father-Son)
    }
    
    /// <summary>
    /// Status of a draft prospect
    /// </summary>
    public enum DraftProspectStatus
    {
        Available,        // Available to be drafted
        Drafted,          // Has been selected
        Withdrawn,        // Withdrawn from draft
        Ineligible,       // Not eligible for this draft
        Reserved,         // Reserved (Academy/Father-Son)
        PassedOver        // Passed over by all teams
    }
    
    /// <summary>
    /// Types of draft bids
    /// </summary>
    public enum DraftBidType
    {
        Academy,                    // Academy player bid
        FatherSon,                  // Father-Son bid
        NextGenerationAcademy,      // Next Generation Academy bid
        International,              // International rookie bid
        Category_B_Rookie          // Category B Rookie bid
    }
    
    /// <summary>
    /// Status of a draft bid
    /// </summary>
    public enum DraftBidStatus
    {
        Pending,          // Bid is pending
        Active,           // Bid is active and can be matched
        Matched,          // Bid has been matched
        Expired,          // Bid has expired
        Withdrawn,        // Bid was withdrawn
        Rejected          // Bid was rejected
    }
    
    /// <summary>
    /// AFL-specific date calculations and utilities
    /// </summary>
    public static class AFLCalendarUtilities
    {
        /// <summary>
        /// Calculate the second Thursday of March for a given year
        /// </summary>
        public static DateTime GetSeasonOpenerDate(int year)
        {
            var firstDayOfMarch = new DateTime(year, 3, 1);
            var daysToThursday = (int)DayOfWeek.Thursday - (int)firstDayOfMarch.DayOfWeek;
            if (daysToThursday < 0) daysToThursday += 7;
            
            var firstThursday = firstDayOfMarch.AddDays(daysToThursday);
            return firstThursday.AddDays(7); // Second Thursday
        }
        
        /// <summary>
        /// Calculate ANZAC Day for a given year (always April 25th)
        /// </summary>
        public static DateTime GetAnzacDay(int year)
        {
            return new DateTime(year, 4, 25);
        }
        
        /// <summary>
        /// Calculate Easter Monday for a given year
        /// </summary>
        public static DateTime GetEasterMonday(int year)
        {
            var easterSunday = GetEasterSunday(year);
            return easterSunday.AddDays(1);
        }
        
        /// <summary>
        /// Calculate King's Birthday (second Monday of June) for a given year
        /// </summary>
        public static DateTime GetKingsBirthday(int year)
        {
            var firstDayOfJune = new DateTime(year, 6, 1);
            var daysToMonday = (int)DayOfWeek.Monday - (int)firstDayOfJune.DayOfWeek;
            if (daysToMonday < 0) daysToMonday += 7;
            
            var firstMonday = firstDayOfJune.AddDays(daysToMonday);
            return firstMonday.AddDays(7); // Second Monday
        }
        
        /// <summary>
        /// Calculate the final weekend of September (Grand Final target)
        /// </summary>
        public static DateTime GetGrandFinalWeekend(int year)
        {
            var lastDayOfSeptember = new DateTime(year, 9, 30);
            var daysToSaturday = (int)DayOfWeek.Saturday - (int)lastDayOfSeptember.DayOfWeek;
            if (daysToSaturday > 0) daysToSaturday -= 7; // Go to previous Saturday
            
            return lastDayOfSeptember.AddDays(daysToSaturday);
        }
        
        /// <summary>
        /// Calculate Brownlow Medal Night (Monday before Grand Final)
        /// </summary>
        public static DateTime GetBrownlowMedalNight(int year)
        {
            var grandFinalDate = GetGrandFinalWeekend(year);
            
            // Find the Monday before Grand Final Saturday
            var daysToMonday = (int)grandFinalDate.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysToMonday < 0) daysToMonday += 7;
            
            return grandFinalDate.AddDays(-daysToMonday);
        }
        
        /// <summary>
        /// Calculate Trade Period start date (Monday after Grand Final)
        /// </summary>
        public static DateTime GetTradePeriodStart(int year)
        {
            var grandFinalDate = GetGrandFinalWeekend(year);
            
            // Find the Monday after Grand Final Saturday
            var daysToMonday = (int)DayOfWeek.Monday - (int)grandFinalDate.DayOfWeek;
            if (daysToMonday <= 0) daysToMonday += 7;
            
            return grandFinalDate.AddDays(daysToMonday);
        }
        
        /// <summary>
        /// Calculate Trade Period end date (10 days after start, excluding weekends)
        /// </summary>
        public static DateTime GetTradePeriodEnd(int year)
        {
            var tradePeriodStart = GetTradePeriodStart(year);
            
            // Add 9 days to get 10 total trade days (including start day)
            return tradePeriodStart.AddDays(9);
        }
        
        /// <summary>
        /// Get AFL Draft date (typically late November)
        /// </summary>
        public static DateTime GetDraftDate(int year)
        {
            // AFL Draft typically occurs in late November
            // Find the last Thursday of November
            var lastDayOfNovember = new DateTime(year, 11, 30);
            var daysToThursday = (int)lastDayOfNovember.DayOfWeek - (int)DayOfWeek.Thursday;
            if (daysToThursday < 0) daysToThursday += 7;
            
            return lastDayOfNovember.AddDays(-daysToThursday);
        }
        
        /// <summary>
        /// Calculate Easter Sunday for a given year (used for Easter Monday calculation)
        /// </summary>
        private static DateTime GetEasterSunday(int year)
        {
            // Anonymous Gregorian algorithm for Easter calculation
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int n = (h + l - 7 * m + 114) / 31;
            int p = (h + l - 7 * m + 114) % 31;
            
            return new DateTime(year, n, p + 1);
        }
        
        /// <summary>
        /// Check if a date falls on a weekend
        /// </summary>
        public static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }
        
        /// <summary>
        /// Get the next weekend from a given date
        /// </summary>
        public static DateTime GetNextWeekend(DateTime date)
        {
            var daysToSaturday = (int)DayOfWeek.Saturday - (int)date.DayOfWeek;
            if (daysToSaturday <= 0) daysToSaturday += 7;
            
            return date.AddDays(daysToSaturday);
        }
        
        /// <summary>
        /// Calculate the number of weeks between two dates
        /// </summary>
        public static int GetWeeksBetween(DateTime start, DateTime end)
        {
            return (int)Math.Ceiling((end - start).TotalDays / 7.0);
        }
        
        /// <summary>
        /// Get typical AFL match time for different days
        /// </summary>
        public static TimeSpan GetTypicalMatchTime(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Thursday => new TimeSpan(19, 25, 0),    // 7:25 PM Thursday night
                DayOfWeek.Friday => new TimeSpan(19, 50, 0),      // 7:50 PM Friday night
                DayOfWeek.Saturday => new TimeSpan(14, 10, 0),    // 2:10 PM Saturday afternoon (early game)
                DayOfWeek.Sunday => new TimeSpan(13, 20, 0),      // 1:20 PM Sunday afternoon
                _ => new TimeSpan(14, 10, 0)                      // Default afternoon time
            };
        }
    }
    
    /// <summary>
    /// Venue information for AFL matches
    /// </summary>
    public class Venue
    {
        public string Name { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Capacity { get; set; }
        public bool IsCovered { get; set; }
        public VenueType Type { get; set; }
        public List<AFLCoachSim.Core.Domain.ValueObjects.TeamId> HomeTeams { get; set; } = new List<AFLCoachSim.Core.Domain.ValueObjects.TeamId>();
        
        /// <summary>
        /// Check if this is a home ground for a specific team
        /// </summary>
        public bool IsHomeGroundFor(AFLCoachSim.Core.Domain.ValueObjects.TeamId teamId)
        {
            return HomeTeams.Contains(teamId);
        }
    }
    
    /// <summary>
    /// Types of AFL venues
    /// </summary>
    public enum VenueType
    {
        Stadium,
        Ground,
        Oval,
        Arena
    }
    
    /// <summary>
    /// Standard AFL venues with their details
    /// </summary>
    public static class AFLVenues
    {
        public static readonly Venue MCG = new Venue
        {
            Name = "Melbourne Cricket Ground",
            City = "Melbourne",
            State = "VIC",
            Capacity = 100024,
            IsCovered = false,
            Type = VenueType.Ground,
            HomeTeams = new List<AFLCoachSim.Core.Domain.ValueObjects.TeamId> 
            { 
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.Melbourne,
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.Collingwood,
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.Richmond
            }
        };
        
        public static readonly Venue MarvelStadium = new Venue
        {
            Name = "Marvel Stadium",
            City = "Melbourne", 
            State = "VIC",
            Capacity = 53359,
            IsCovered = true,
            Type = VenueType.Stadium,
            HomeTeams = new List<AFLCoachSim.Core.Domain.ValueObjects.TeamId>
            {
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.Essendon,
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.Carlton,
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.StKilda,
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.NorthMelbourne,
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.WesternBulldogs
            }
        };
        
        public static readonly Venue AdelaideCrowdsGround = new Venue
        {
            Name = "Adelaide Oval",
            City = "Adelaide",
            State = "SA", 
            Capacity = 53583,
            IsCovered = false,
            Type = VenueType.Oval,
            HomeTeams = new List<AFLCoachSim.Core.Domain.ValueObjects.TeamId>
            {
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.Adelaide,
                AFLCoachSim.Core.Domain.ValueObjects.TeamId.PortAdelaide
            }
        };
        
        // Add other venues as needed...
        
        /// <summary>
        /// Get all standard AFL venues
        /// </summary>
        public static List<Venue> GetAllVenues()
        {
            return new List<Venue>
            {
                MCG,
                MarvelStadium,
                AdelaideCrowdsGround
                // Add more venues as needed
            };
        }
        
        /// <summary>
        /// Get the primary home venue for a team
        /// </summary>
        public static Venue GetHomeVenueForTeam(AFLCoachSim.Core.Domain.ValueObjects.TeamId teamId)
        {
            return GetAllVenues().FirstOrDefault(v => v.IsHomeGroundFor(teamId)) ?? MCG;
        }
    }
}