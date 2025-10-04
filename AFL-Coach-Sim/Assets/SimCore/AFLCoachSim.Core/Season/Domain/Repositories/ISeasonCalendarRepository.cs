using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Domain.Repositories
{
    /// <summary>
    /// Repository interface for season calendar persistence
    /// </summary>
    public interface ISeasonCalendarRepository
    {
        /// <summary>
        /// Gets a season calendar by year
        /// </summary>
        Task<SeasonCalendar?> GetByYearAsync(int year);

        /// <summary>
        /// Gets a season calendar by ID
        /// </summary>
        Task<SeasonCalendar?> GetByIdAsync(int id);

        /// <summary>
        /// Gets all season calendars
        /// </summary>
        Task<IEnumerable<SeasonCalendar>> GetAllAsync();

        /// <summary>
        /// Gets season calendars within a year range
        /// </summary>
        Task<IEnumerable<SeasonCalendar>> GetByYearRangeAsync(int startYear, int endYear);

        /// <summary>
        /// Saves a season calendar
        /// </summary>
        Task<SeasonCalendar> SaveAsync(SeasonCalendar calendar);

        /// <summary>
        /// Updates a season calendar
        /// </summary>
        Task<SeasonCalendar> UpdateAsync(SeasonCalendar calendar);

        /// <summary>
        /// Deletes a season calendar
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Checks if a season calendar exists for a given year
        /// </summary>
        Task<bool> ExistsAsync(int year);

        /// <summary>
        /// Gets the current active season
        /// </summary>
        Task<SeasonCalendar?> GetCurrentSeasonAsync();

        /// <summary>
        /// Gets seasons by state
        /// </summary>
        Task<IEnumerable<SeasonCalendar>> GetByStateAsync(SeasonState state);

        /// <summary>
        /// Updates match result
        /// </summary>
        Task<bool> UpdateMatchResultAsync(int calendarId, int roundNumber, int matchId, int homeScore, int awayScore);

        /// <summary>
        /// Updates season state
        /// </summary>
        Task<bool> UpdateSeasonStateAsync(int calendarId, SeasonState newState);

        /// <summary>
        /// Updates off-season event status
        /// </summary>
        Task<bool> UpdateOffSeasonEventAsync(int calendarId, int eventId, bool isActive, bool isCompleted, DateTime? completedDate = null);

        /// <summary>
        /// Gets upcoming off-season events
        /// </summary>
        Task<IEnumerable<OffSeasonEvent>> GetUpcomingOffSeasonEventsAsync(DateTime currentDate, int daysAhead = 30);

        /// <summary>
        /// Gets active off-season events
        /// </summary>
        Task<IEnumerable<OffSeasonEvent>> GetActiveOffSeasonEventsAsync(DateTime currentDate);
    }
}