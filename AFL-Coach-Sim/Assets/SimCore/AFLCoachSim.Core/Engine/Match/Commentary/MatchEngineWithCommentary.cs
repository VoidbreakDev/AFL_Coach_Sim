using System.Collections.Generic;
using AFLCoachSim.Core.DTO;
using AFLCoachSim.Core.Domain.Aggregates;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Simulation;
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Engine.Match.Tuning;

namespace AFLCoachSim.Core.Engine.Match.Commentary
{
    /// <summary>
    /// Enhanced match result that includes commentary
    /// </summary>
    public sealed class CommentatedMatchResult
    {
        public MatchResultDTO MatchResult { get; set; }
        public List<string> Commentary { get; set; } = new List<string>();
        public List<MatchEvent> Events { get; set; } = new List<MatchEvent>();
        
        // Convenience properties for common match result access
        public int Round => MatchResult.Round;
        public TeamId Home => MatchResult.Home;
        public TeamId Away => MatchResult.Away;
        public int HomeScore => MatchResult.HomeScore;
        public int AwayScore => MatchResult.AwayScore;
    }
    
    /// <summary>
    /// Helper class to easily integrate commentary with existing MatchEngine
    /// </summary>
    public static class MatchEngineWithCommentary
    {
        /// <summary>
        /// Play a match with full commentary generation
        /// </summary>
        public static CommentatedMatchResult PlayMatchWithCommentary(
            int round,
            TeamId homeId,
            TeamId awayId,
            Dictionary<TeamId, Team> teams,
            Dictionary<TeamId, List<Domain.Entities.Player>> rosters = null,
            Dictionary<TeamId, TeamTactics> tactics = null,
            Weather weather = Weather.Clear,
            Ground ground = null,
            int quarterSeconds = 20 * 60,
            DeterministicRandom rng = null,
            MatchTuning tuning = null)
        {
            if (rng == null)
                rng = new DeterministicRandom(12345);
            
            // Build team names dictionary
            var teamNames = new Dictionary<TeamId, string>
            {
                { homeId, teams[homeId].Name },
                { awayId, teams[awayId].Name }
            };
            
            // Create commentary sink
            var commentarySink = new CommentarySink(homeId, awayId, rosters, teamNames, rng);
            
            // Run the match with commentary
            var result = MatchEngine.PlayMatch(
                round, homeId, awayId, teams, rosters, tactics,
                weather, ground, quarterSeconds, rng, tuning, commentarySink);
            
            // Return enhanced result with commentary
            return new CommentatedMatchResult
            {
                MatchResult = result,
                Commentary = commentarySink.CommentaryEvents,
                Events = commentarySink.MatchEvents
            };
        }
        
        /// <summary>
        /// Get just the highlights (key events) from a match
        /// </summary>
        public static List<string> GetMatchHighlights(CommentatedMatchResult result)
        {
            var highlights = new List<string>();
            
            foreach (var matchEvent in result.Events)
            {
                // Include only significant events as highlights
                if (IsHighlightEvent(matchEvent.EventType))
                {
                    var commentary = result.Commentary[result.Events.IndexOf(matchEvent)];
                    highlights.Add(commentary);
                }
            }
            
            return highlights;
        }
        
        /// <summary>
        /// Get quarter-by-quarter summary
        /// </summary>
        public static Dictionary<int, List<string>> GetQuarterSummaries(CommentatedMatchResult result)
        {
            var summaries = new Dictionary<int, List<string>>
            {
                { 1, new List<string>() }, { 2, new List<string>() }, { 3, new List<string>() }, { 4, new List<string>() }
            };
            
            for (int i = 0; i < result.Events.Count; i++)
            {
                var matchEvent = result.Events[i];
                if (summaries.ContainsKey(matchEvent.Quarter))
                {
                    summaries[matchEvent.Quarter].Add(result.Commentary[i]);
                }
            }
            
            return summaries;
        }
        
        private static bool IsHighlightEvent(MatchEventType eventType)
        {
            switch (eventType)
            {
                case MatchEventType.Goal:
                    return true;
                case MatchEventType.Behind:
                    return false; // Usually not highlights
                case MatchEventType.SpectacularMark:
                    return true;
                case MatchEventType.Injury:
                    return true;
                case MatchEventType.Substitution:
                    return false; // Usually not highlights
                case MatchEventType.QuarterStart:
                    return true;
                case MatchEventType.QuarterEnd:
                    return true;
                default:
                    return false;
            }
        }
    }
}
