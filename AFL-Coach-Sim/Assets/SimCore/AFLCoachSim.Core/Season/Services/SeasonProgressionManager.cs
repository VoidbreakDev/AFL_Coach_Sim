using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Season.Domain.Entities;
using AFLCoachSim.Core.Season.Domain.ValueObjects;
using AFLCoachSim.Core.Infrastructure.Logging;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Engine.Match.Weather;

namespace AFLCoachSim.Core.Season.Services
{
    /// <summary>
    /// Manages season progression, tracking current round, completed matches, and season state
    /// </summary>
    public class SeasonProgressionManager
    {
        private SeasonCalendar _season;
        
        public SeasonProgressionManager(SeasonCalendar season)
        {
            _season = season ?? throw new ArgumentNullException(nameof(season));
        }
        
        /// <summary>
        /// Advance to the next round in the season
        /// </summary>
        public SeasonProgressionResult AdvanceToNextRound()
        {
            var result = new SeasonProgressionResult();
            
            if (_season.CurrentState == SeasonState.Complete || _season.CurrentState == SeasonState.Cancelled)
            {
                result.Success = false;
                result.Message = $"Season is already {_season.CurrentState.ToString().ToLower()}";
                return result;
            }
            
            var currentRound = GetCurrentRound();
            if (currentRound == null)
            {
                result.Success = false;
                result.Message = "Current round not found";
                return result;
            }
            
            // Check if current round is complete
            if (!currentRound.IsComplete())
            {
                result.Success = false;
                result.Message = $"Round {_season.CurrentRound} is not complete. {currentRound.Matches.Count(m => m.Status != MatchStatus.Completed)} matches remaining.";
                return result;
            }
            
            // Advance to next round
            _season.CurrentRound++;
            
            // Check if season is complete
            if (_season.CurrentRound > _season.TotalRounds)
            {
                _season.CurrentState = SeasonState.Complete;
                result.Message = "Season completed!";
            }
            else
            {
                _season.CurrentState = SeasonState.InProgress;
                result.Message = $"Advanced to Round {_season.CurrentRound}";
            }
            
            result.Success = true;
            result.NewCurrentRound = _season.CurrentRound;
            result.NewSeasonState = _season.CurrentState;
            
            CoreLogger.Log($"[SeasonProgression] {result.Message}");
            return result;
        }
        
        /// <summary>
        /// Complete a specific match and update its status
        /// </summary>
        public MatchCompletionResult CompleteMatch(int matchId, int homeScore, int awayScore, Weather actualWeather = Weather.Clear)
        {
            var match = FindMatch(matchId);
            if (match == null)
            {
                return new MatchCompletionResult
                {
                    Success = false,
                    Message = $"Match {matchId} not found"
                };
            }
            
            if (match.Status == MatchStatus.Completed)
            {
                return new MatchCompletionResult
                {
                    Success = false,
                    Message = $"Match {matchId} is already completed"
                };
            }
            
            // Update match details
            match.Status = MatchStatus.Completed;
            match.HomeScore = homeScore;
            match.AwayScore = awayScore;
            match.Weather = actualWeather;
            
            var result = new MatchCompletionResult
            {
                Success = true,
                Message = $"Match completed: {match.HomeTeam} {homeScore} - {awayScore} {match.AwayTeam}",
                Match = match,
                Result = match.GetResult()
            };
            
            // Check if this completes the current round
            var currentRound = GetCurrentRound();
            if (currentRound != null && currentRound.IsComplete())
            {
                result.RoundCompleted = true;
                result.Message += $". Round {currentRound.RoundNumber} is now complete.";
            }
            
            CoreLogger.Log($"[SeasonProgression] {result.Message}");
            return result;
        }
        
        /// <summary>
        /// Get the current round
        /// </summary>
        public SeasonRound GetCurrentRound()
        {
            return _season.Rounds.FirstOrDefault(r => r.RoundNumber == _season.CurrentRound);
        }
        
        /// <summary>
        /// Get the next round
        /// </summary>
        public SeasonRound GetNextRound()
        {
            return _season.Rounds.FirstOrDefault(r => r.RoundNumber == _season.CurrentRound + 1);
        }
        
        /// <summary>
        /// Get all pending matches for the current round
        /// </summary>
        public List<ScheduledMatch> GetCurrentRoundMatches()
        {
            var currentRound = GetCurrentRound();
            return currentRound?.Matches.Where(m => m.Status == MatchStatus.Scheduled).ToList() ?? new List<ScheduledMatch>();
        }
        
        /// <summary>
        /// Get next match for a specific team
        /// </summary>
        public ScheduledMatch GetNextMatchForTeam(TeamId teamId)
        {
            return _season.GetNextMatch(teamId);
        }
        
        /// <summary>
        /// Get season progress statistics
        /// </summary>
        public SeasonProgressStats GetProgressStats()
        {
            var totalMatches = _season.Rounds.Sum(r => r.Matches.Count);
            var completedMatches = _season.Rounds.Sum(r => r.Matches.Count(m => m.Status == MatchStatus.Completed));
            var currentRound = GetCurrentRound();
            
            return new SeasonProgressStats
            {
                CurrentRound = _season.CurrentRound,
                TotalRounds = _season.TotalRounds,
                TotalMatches = totalMatches,
                CompletedMatches = completedMatches,
                RemainingMatches = totalMatches - completedMatches,
                SeasonState = _season.CurrentState,
                CurrentRoundComplete = currentRound?.IsComplete() ?? false,
                ProgressPercentage = totalMatches > 0 ? (float)completedMatches / totalMatches : 0f
            };
        }
        
        /// <summary>
        /// Get matches scheduled for a specific date range
        /// </summary>
        public List<ScheduledMatch> GetMatchesInDateRange(DateTime startDate, DateTime endDate)
        {
            return _season.Rounds
                         .SelectMany(r => r.Matches)
                         .Where(m => m.ScheduledDateTime.Date >= startDate.Date && 
                                   m.ScheduledDateTime.Date <= endDate.Date)
                         .OrderBy(m => m.ScheduledDateTime)
                         .ToList();
        }
        
        /// <summary>
        /// Get upcoming matches for the next N days
        /// </summary>
        public List<ScheduledMatch> GetUpcomingMatches(int days = 7)
        {
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(days);
            
            return GetMatchesInDateRange(startDate, endDate)
                   .Where(m => m.Status == MatchStatus.Scheduled)
                   .ToList();
        }
        
        /// <summary>
        /// Check if a team has a bye in the current round
        /// </summary>
        public bool IsTeamOnByeThisRound(TeamId teamId)
        {
            return _season.HasBye(teamId, _season.CurrentRound);
        }
        
        /// <summary>
        /// Get all teams on bye in the current round
        /// </summary>
        public List<TeamId> GetTeamsOnByeThisRound()
        {
            var currentRound = GetCurrentRound();
            return currentRound?.TeamsOnBye ?? new List<TeamId>();
        }
        
        /// <summary>
        /// Get team's next scheduled match details
        /// </summary>
        public TeamUpcomingMatch GetTeamUpcomingMatch(TeamId teamId)
        {
            var nextMatch = GetNextMatchForTeam(teamId);
            if (nextMatch == null)
            {
                return new TeamUpcomingMatch
                {
                    TeamId = teamId,
                    HasUpcomingMatch = false,
                    Message = "No upcoming matches scheduled"
                };
            }
            
            var isHome = nextMatch.HomeTeam == teamId;
            var opponent = isHome ? nextMatch.AwayTeam : nextMatch.HomeTeam;
            var daysUntil = (int)(nextMatch.ScheduledDateTime.Date - DateTime.Today).TotalDays;
            
            return new TeamUpcomingMatch
            {
                TeamId = teamId,
                HasUpcomingMatch = true,
                Match = nextMatch,
                Opponent = opponent,
                IsHomeGame = isHome,
                DaysUntilMatch = daysUntil,
                Message = $"{(isHome ? "vs" : "@")} {opponent} in {daysUntil} days"
            };
        }
        
        /// <summary>
        /// Validate season calendar integrity
        /// </summary>
        public SeasonValidationResult ValidateSeasonIntegrity()
        {
            var result = new SeasonValidationResult();
            var calendarValidation = _season.Validate();
            
            result.IsValid = calendarValidation.IsValid;
            result.Errors.AddRange(calendarValidation.Errors);
            
            // Additional progression-specific validations
            if (_season.CurrentRound < 1 || _season.CurrentRound > _season.TotalRounds + 1)
            {
                result.Errors.Add($"Invalid current round: {_season.CurrentRound}");
                result.IsValid = false;
            }
            
            // Check for scheduling conflicts
            var conflicts = FindSchedulingConflicts();
            if (conflicts.Any())
            {
                result.Errors.AddRange(conflicts);
                result.IsValid = false;
            }
            
            return result;
        }
        
        #region Private Methods
        
        private ScheduledMatch FindMatch(int matchId)
        {
            return _season.Rounds
                         .SelectMany(r => r.Matches)
                         .FirstOrDefault(m => m.MatchId == matchId);
        }
        
        private List<string> FindSchedulingConflicts()
        {
            var conflicts = new List<string>();
            
            // Check for teams playing multiple matches in the same round
            foreach (var round in _season.Rounds)
            {
                var teamsInRound = round.Matches.SelectMany(m => new[] { m.HomeTeam, m.AwayTeam }).ToList();
                var duplicateTeams = teamsInRound.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key);
                
                foreach (var team in duplicateTeams)
                {
                    conflicts.Add($"Team {team} has multiple matches in Round {round.RoundNumber}");
                }
            }
            
            // Check for overlapping match times at the same venue
            var venueTimeSlots = _season.Rounds
                .SelectMany(r => r.Matches)
                .GroupBy(m => m.Venue)
                .Where(g => g.Count() > 1);
                
            foreach (var venueGroup in venueTimeSlots)
            {
                var matches = venueGroup.OrderBy(m => m.ScheduledDateTime).ToList();
                for (int i = 0; i < matches.Count - 1; i++)
                {
                    var match1 = matches[i];
                    var match2 = matches[i + 1];
                    
                    // Assume matches take 3 hours
                    if (match2.ScheduledDateTime < match1.ScheduledDateTime.AddHours(3))
                    {
                        conflicts.Add($"Venue conflict at {match1.Venue}: Match {match1.MatchId} and {match2.MatchId} overlap");
                    }
                }
            }
            
            return conflicts;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Result of season progression operation
    /// </summary>
    public class SeasonProgressionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int NewCurrentRound { get; set; }
        public SeasonState NewSeasonState { get; set; }
    }
    
    /// <summary>
    /// Result of match completion operation
    /// </summary>
    public class MatchCompletionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ScheduledMatch Match { get; set; }
        public MatchResult Result { get; set; }
        public bool RoundCompleted { get; set; }
    }
    
    /// <summary>
    /// Season progress statistics
    /// </summary>
    public class SeasonProgressStats
    {
        public int CurrentRound { get; set; }
        public int TotalRounds { get; set; }
        public int TotalMatches { get; set; }
        public int CompletedMatches { get; set; }
        public int RemainingMatches { get; set; }
        public SeasonState SeasonState { get; set; }
        public bool CurrentRoundComplete { get; set; }
        public float ProgressPercentage { get; set; }
        
        public string GetProgressSummary()
        {
            return $"Round {CurrentRound}/{TotalRounds} - {CompletedMatches}/{TotalMatches} matches complete ({ProgressPercentage:P0})";
        }
    }
    
    /// <summary>
    /// Team's upcoming match information
    /// </summary>
    public class TeamUpcomingMatch
    {
        public TeamId TeamId { get; set; }
        public bool HasUpcomingMatch { get; set; }
        public ScheduledMatch Match { get; set; }
        public TeamId Opponent { get; set; }
        public bool IsHomeGame { get; set; }
        public int DaysUntilMatch { get; set; }
        public string Message { get; set; }
    }
    
    /// <summary>
    /// Season validation result
    /// </summary>
    public class SeasonValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        
        public string GetErrorSummary()
        {
            return IsValid ? "Season is valid" : $"Season has {Errors.Count} errors: {string.Join(", ", Errors)}";
        }
    }
}