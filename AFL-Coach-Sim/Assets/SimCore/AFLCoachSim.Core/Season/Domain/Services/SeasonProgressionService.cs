using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;

namespace AFLCoachSim.Core.Season.Domain.Services
{
    /// <summary>
    /// Service for managing season progression including off-season events
    /// </summary>
    public class SeasonProgressionService
    {
        private readonly SeasonCalendar _seasonCalendar;

        public SeasonProgressionService(SeasonCalendar seasonCalendar)
        {
            _seasonCalendar = seasonCalendar ?? throw new ArgumentNullException(nameof(seasonCalendar));
        }

        /// <summary>
        /// Updates the season state based on current date and progress
        /// </summary>
        public SeasonState UpdateSeasonState(DateTime currentDate)
        {
            var currentState = _seasonCalendar.CurrentState;

            // Check if season has started
            if (currentDate < _seasonCalendar.SeasonStart)
            {
                return SeasonState.NotStarted;
            }

            // Check if in regular season
            if (currentDate >= _seasonCalendar.SeasonStart && currentDate <= _seasonCalendar.SeasonEnd)
            {
                var finalsRounds = _seasonCalendar.Rounds.Where(r => r.RoundType == RoundType.Finals).ToList();
                
                if (finalsRounds.Any())
                {
                    var firstFinalsRound = finalsRounds.OrderBy(r => r.RoundNumber).First();
                    var grandFinalRound = finalsRounds.Where(r => r.RoundName.Contains("Grand Final")).FirstOrDefault();
                    
                    if (grandFinalRound != null && currentDate >= grandFinalRound.RoundStartDate)
                    {
                        // Check if Grand Final is complete
                        if (grandFinalRound.IsComplete())
                        {
                            return SeasonState.GrandFinal; // Completed Grand Final, moving to off-season
                        }
                        return SeasonState.GrandFinal;
                    }
                    
                    if (currentDate >= firstFinalsRound.RoundStartDate)
                    {
                        return SeasonState.Finals;
                    }
                }

                return SeasonState.InProgress;
            }

            // Check off-season events
            var activeOffSeasonEvents = _seasonCalendar.GetActiveOffSeasonEvents(currentDate);
            if (activeOffSeasonEvents.Any())
            {
                var brownlowEvent = activeOffSeasonEvents.FirstOrDefault(e => e.EventType == OffSeasonEventType.BrownlowMedal);
                if (brownlowEvent != null)
                {
                    return SeasonState.BrownlowNight;
                }

                var tradePeriodEvent = activeOffSeasonEvents.FirstOrDefault(e => e.EventType == OffSeasonEventType.TradePeriod);
                if (tradePeriodEvent != null)
                {
                    return SeasonState.TradePeriod;
                }

                return SeasonState.OffSeason;
            }

            // Check if there are upcoming events this calendar year or if season is truly complete
            var upcomingEvents = _seasonCalendar.OffSeasonEvents
                .Where(e => !e.IsCompleted && e.Date >= currentDate && e.Date.Year <= currentDate.Year + 1)
                .ToList();

            if (upcomingEvents.Any())
            {
                return SeasonState.OffSeason;
            }

            return SeasonState.Complete;
        }

        /// <summary>
        /// Progresses to the next round
        /// </summary>
        public bool ProgressToNextRound()
        {
            var currentRound = _seasonCalendar.Rounds.FirstOrDefault(r => r.RoundNumber == _seasonCalendar.CurrentRound);
            if (currentRound == null || !currentRound.IsComplete())
            {
                return false; // Cannot progress if current round is incomplete
            }

            if (_seasonCalendar.CurrentRound >= _seasonCalendar.TotalRounds)
            {
                return false; // Season is complete
            }

            _seasonCalendar.CurrentRound++;
            return true;
        }

        /// <summary>
        /// Starts an off-season event
        /// </summary>
        public bool StartOffSeasonEvent(OffSeasonEventType eventType, DateTime currentDate)
        {
            var offSeasonEvent = _seasonCalendar.OffSeasonEvents
                .FirstOrDefault(e => e.EventType == eventType && !e.IsCompleted && !e.IsActive);

            if (offSeasonEvent == null || currentDate < offSeasonEvent.Date.Date)
            {
                return false;
            }

            try
            {
                offSeasonEvent.Start();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Completes an off-season event
        /// </summary>
        public bool CompleteOffSeasonEvent(OffSeasonEventType eventType)
        {
            var offSeasonEvent = _seasonCalendar.OffSeasonEvents
                .FirstOrDefault(e => e.EventType == eventType && e.IsActive);

            if (offSeasonEvent == null)
            {
                return false;
            }

            try
            {
                offSeasonEvent.Complete();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the current phase description
        /// </summary>
        public string GetCurrentPhaseDescription(DateTime currentDate)
        {
            var state = UpdateSeasonState(currentDate);
            
            return state switch
            {
                SeasonState.NotStarted => $"Season starts on {_seasonCalendar.SeasonStart:MMMM d, yyyy}",
                SeasonState.InProgress => $"Round {_seasonCalendar.CurrentRound} of {_seasonCalendar.TotalRounds}",
                SeasonState.Finals => GetFinalsDescription(),
                SeasonState.GrandFinal => "Grand Final Weekend",
                SeasonState.BrownlowNight => "Brownlow Medal Night",
                SeasonState.TradePeriod => GetTradePeriodDescription(currentDate),
                SeasonState.OffSeason => GetOffSeasonDescription(currentDate),
                SeasonState.Complete => $"{_seasonCalendar.Year} Season Complete",
                SeasonState.Cancelled => $"{_seasonCalendar.Year} Season Cancelled",
                _ => "Unknown Season State"
            };
        }

        /// <summary>
        /// Gets upcoming events for the next specified number of days
        /// </summary>
        public List<OffSeasonEvent> GetUpcomingEvents(DateTime currentDate, int daysAhead = 30)
        {
            var endDate = currentDate.AddDays(daysAhead);
            
            return _seasonCalendar.OffSeasonEvents
                .Where(e => !e.IsCompleted && e.Date >= currentDate.Date && e.Date <= endDate)
                .OrderBy(e => e.Date)
                .ToList();
        }

        /// <summary>
        /// Checks if any important off-season events are due soon
        /// </summary>
        public List<OffSeasonEvent> GetImminentEvents(DateTime currentDate, int daysWarning = 7)
        {
            var warningDate = currentDate.AddDays(daysWarning);
            
            var importantEventTypes = new HashSet<OffSeasonEventType>
            {
                OffSeasonEventType.BrownlowMedal,
                OffSeasonEventType.TradePeriod,
                OffSeasonEventType.Draft,
                OffSeasonEventType.ListLodgement
            };
            
            return _seasonCalendar.OffSeasonEvents
                .Where(e => !e.IsCompleted && 
                           !e.IsActive &&
                           importantEventTypes.Contains(e.EventType) &&
                           e.Date >= currentDate.Date && 
                           e.Date <= warningDate)
                .OrderBy(e => e.Date)
                .ToList();
        }

        private string GetFinalsDescription()
        {
            var finalsRounds = _seasonCalendar.Rounds
                .Where(r => r.RoundType == RoundType.Finals)
                .OrderBy(r => r.RoundNumber)
                .ToList();

            var currentFinalsRound = finalsRounds.FirstOrDefault(r => r.RoundNumber == _seasonCalendar.CurrentRound);
            
            return currentFinalsRound?.RoundName ?? "Finals Series";
        }

        private string GetTradePeriodDescription(DateTime currentDate)
        {
            var tradePeriod = _seasonCalendar.OffSeasonEvents
                .FirstOrDefault(e => e.EventType == OffSeasonEventType.TradePeriod && e.IsOngoing(currentDate));

            if (tradePeriod?.EndDate != null)
            {
                var daysRemaining = (tradePeriod.EndDate.Value.Date - currentDate.Date).Days;
                return $"Trade Period - {daysRemaining} days remaining";
            }

            return "Trade Period";
        }

        private string GetOffSeasonDescription(DateTime currentDate)
        {
            var nextEvent = _seasonCalendar.GetNextOffSeasonEvent(currentDate);
            
            if (nextEvent != null)
            {
                var daysUntilNext = (nextEvent.Date.Date - currentDate.Date).Days;
                if (daysUntilNext <= 7)
                {
                    return $"Off Season - {nextEvent.Name} in {daysUntilNext} day{(daysUntilNext == 1 ? "" : "s")}";
                }
                return $"Off Season - Next: {nextEvent.Name} ({nextEvent.Date:MMM d})";
            }

            return "Off Season";
        }

        /// <summary>
        /// Updates match results and checks for round completion
        /// </summary>
        public bool UpdateMatchResult(int roundNumber, int matchId, int homeScore, int awayScore)
        {
            var round = _seasonCalendar.Rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
            if (round == null) return false;

            var match = round.Matches.FirstOrDefault(m => m.MatchId == matchId);
            if (match == null) return false;

            match.HomeScore = homeScore;
            match.AwayScore = awayScore;
            match.Status = MatchStatus.Completed;

            // Check if round is complete and update season state
            if (round.IsComplete())
            {
                // Auto-progress if this was the current round
                if (roundNumber == _seasonCalendar.CurrentRound)
                {
                    ProgressToNextRound();
                }
            }

            return true;
        }
    }
}