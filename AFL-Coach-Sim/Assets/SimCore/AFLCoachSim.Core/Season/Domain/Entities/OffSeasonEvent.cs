using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Domain.Entities
{
    /// <summary>
    /// Represents an off-season event (Brownlow, Trade Period, Draft, etc.)
    /// </summary>
    public class OffSeasonEvent
    {
        public int Id { get; private set; }
        public int SeasonId { get; private set; }
        public OffSeasonEventType EventType { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DateTime Date { get; private set; }
        public DateTime? EndDate { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }
        public DateTime? CompletedDate { get; private set; }
        public Dictionary<string, object> Properties { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // Default constructor for EF Core
        protected OffSeasonEvent() 
        {
            Properties = new Dictionary<string, object>();
        }

        public OffSeasonEvent(
            int seasonId,
            OffSeasonEventType eventType,
            string name,
            string description,
            DateTime date,
            DateTime? endDate = null,
            Dictionary<string, object>? properties = null)
        {
            SeasonId = seasonId;
            EventType = eventType;
            Name = name;
            Description = description;
            Date = date;
            EndDate = endDate;
            IsActive = false;
            IsCompleted = false;
            Properties = properties ?? new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the off-season event
        /// </summary>
        public void Start()
        {
            if (IsActive)
                throw new InvalidOperationException($"Off-season event {Name} is already active");

            if (IsCompleted)
                throw new InvalidOperationException($"Off-season event {Name} has already been completed");

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Completes the off-season event
        /// </summary>
        public void Complete()
        {
            if (!IsActive)
                throw new InvalidOperationException($"Off-season event {Name} is not active");

            if (IsCompleted)
                throw new InvalidOperationException($"Off-season event {Name} has already been completed");

            IsActive = false;
            IsCompleted = true;
            CompletedDate = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancels the off-season event
        /// </summary>
        public void Cancel()
        {
            if (IsCompleted)
                throw new InvalidOperationException($"Cannot cancel completed off-season event {Name}");

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates event properties
        /// </summary>
        public void UpdateProperties(Dictionary<string, object> newProperties)
        {
            if (IsCompleted)
                throw new InvalidOperationException($"Cannot update properties of completed off-season event {Name}");

            foreach (var kvp in newProperties)
            {
                Properties[kvp.Key] = kvp.Value;
            }

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a property value
        /// </summary>
        public T? GetProperty<T>(string key)
        {
            if (Properties.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return default(T);
        }

        /// <summary>
        /// Sets a property value
        /// </summary>
        public void SetProperty(string key, object value)
        {
            if (IsCompleted)
                throw new InvalidOperationException($"Cannot set property on completed off-season event {Name}");

            Properties[key] = value;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the event is a multi-day event
        /// </summary>
        public bool IsMultiDayEvent => EndDate.HasValue && EndDate.Value.Date != Date.Date;

        /// <summary>
        /// Gets the duration of the event in days
        /// </summary>
        public int DurationInDays
        {
            get
            {
                if (!EndDate.HasValue) return 1;
                return (EndDate.Value.Date - Date.Date).Days + 1;
            }
        }

        /// <summary>
        /// Checks if the event is currently ongoing
        /// </summary>
        public bool IsOngoing(DateTime currentDate)
        {
            if (!IsActive) return false;

            var current = currentDate.Date;
            var start = Date.Date;
            var end = EndDate?.Date ?? start;

            return current >= start && current <= end;
        }

        /// <summary>
        /// Checks if the event is scheduled for a specific date
        /// </summary>
        public bool IsScheduledFor(DateTime date)
        {
            var targetDate = date.Date;
            var start = Date.Date;
            var end = EndDate?.Date ?? start;

            return targetDate >= start && targetDate <= end;
        }
    }
}