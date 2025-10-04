using System;

namespace AFLCoachSim.Core.Season.Utilities
{
    /// <summary>
    /// Utility class for calculating key dates in the AFL calendar
    /// </summary>
    public static class AFLCalendarUtilities
    {
        /// <summary>
        /// Calculate the Grand Final weekend date (typically last Saturday in September)
        /// </summary>
        public static DateTime CalculateGrandFinalWeekend(int year)
        {
            var lastDayOfSeptember = new DateTime(year, 9, 30);
            
            // Find the last Saturday in September
            while (lastDayOfSeptember.DayOfWeek != DayOfWeek.Saturday)
            {
                lastDayOfSeptember = lastDayOfSeptember.AddDays(-1);
            }
            
            return lastDayOfSeptember;
        }

        /// <summary>
        /// Calculate the Brownlow Medal night (typically Monday before Grand Final)
        /// </summary>
        public static DateTime CalculateBrownlowMedalNight(int year)
        {
            var grandFinalWeekend = CalculateGrandFinalWeekend(year);
            return grandFinalWeekend.AddDays(-5); // Monday before Grand Final Saturday
        }

        /// <summary>
        /// Calculate trade period start date (typically first weekday after Grand Final)
        /// </summary>
        public static DateTime CalculateTradePeriodStart(int year)
        {
            var grandFinalWeekend = CalculateGrandFinalWeekend(year);
            var tradePeriodStart = grandFinalWeekend.AddDays(2); // Monday after Grand Final Saturday
            
            // Ensure it's a weekday
            while (tradePeriodStart.DayOfWeek == DayOfWeek.Saturday || tradePeriodStart.DayOfWeek == DayOfWeek.Sunday)
            {
                tradePeriodStart = tradePeriodStart.AddDays(1);
            }
            
            return tradePeriodStart;
        }

        /// <summary>
        /// Calculate trade period end date (typically 2 weeks after start)
        /// </summary>
        public static DateTime CalculateTradePeriodEnd(int year)
        {
            var tradePeriodStart = CalculateTradePeriodStart(year);
            var tradePeriodEnd = tradePeriodStart.AddDays(14); // 2 weeks
            
            // Typically ends on a Wednesday or Thursday
            while (tradePeriodEnd.DayOfWeek != DayOfWeek.Wednesday && tradePeriodEnd.DayOfWeek != DayOfWeek.Thursday)
            {
                tradePeriodEnd = tradePeriodEnd.AddDays(1);
            }
            
            return tradePeriodEnd;
        }

        /// <summary>
        /// Calculate AFL National Draft date (typically last Thursday in November)
        /// </summary>
        public static DateTime CalculateAFLDraftDate(int year)
        {
            var lastDayOfNovember = new DateTime(year, 11, 30);
            
            // Find the last Thursday in November
            while (lastDayOfNovember.DayOfWeek != DayOfWeek.Thursday)
            {
                lastDayOfNovember = lastDayOfNovember.AddDays(-1);
            }
            
            return lastDayOfNovember;
        }

        /// <summary>
        /// Calculate season start date (typically second Thursday in March of following year)
        /// </summary>
        public static DateTime CalculateSeasonStart(int year)
        {
            var firstOfMarch = new DateTime(year, 3, 1);
            
            // Find the first Thursday in March
            while (firstOfMarch.DayOfWeek != DayOfWeek.Thursday)
            {
                firstOfMarch = firstOfMarch.AddDays(1);
            }
            
            // Season typically starts on the second Thursday
            return firstOfMarch.AddDays(7);
        }

        /// <summary>
        /// Calculate season end date (Grand Final weekend)
        /// </summary>
        public static DateTime CalculateSeasonEnd(int year)
        {
            return CalculateGrandFinalWeekend(year);
        }

        /// <summary>
        /// Calculate finals series start date (typically first weekend in September)
        /// </summary>
        public static DateTime CalculateFinalsStart(int year)
        {
            var firstOfSeptember = new DateTime(year, 9, 1);
            
            // Find the first Saturday in September
            while (firstOfSeptember.DayOfWeek != DayOfWeek.Saturday)
            {
                firstOfSeptember = firstOfSeptember.AddDays(1);
            }
            
            return firstOfSeptember;
        }

        /// <summary>
        /// Calculate bye round period (typically mid-season around rounds 12-15)
        /// </summary>
        public static (DateTime start, DateTime end) CalculateByePeriod(int year)
        {
            var seasonStart = CalculateSeasonStart(year);
            
            // Bye period typically starts around round 12 (11 weeks after season start)
            var byeStart = seasonStart.AddDays(11 * 7);
            var byeEnd = byeStart.AddDays(4 * 7); // 4 weeks of bye rounds
            
            return (byeStart, byeEnd);
        }

        /// <summary>
        /// Check if a given date falls within the AFL season
        /// </summary>
        public static bool IsWithinSeason(DateTime date, int year)
        {
            var seasonStart = CalculateSeasonStart(year);
            var seasonEnd = CalculateSeasonEnd(year);
            
            return date >= seasonStart && date <= seasonEnd;
        }

        /// <summary>
        /// Get the AFL season year for a given date
        /// </summary>
        public static int GetSeasonYear(DateTime date)
        {
            // If the date is in January-February, it belongs to the previous year's season
            // (which runs into the new year for finals/off-season)
            if (date.Month <= 2)
            {
                return date.Year - 1;
            }
            
            return date.Year;
        }
    }
}