using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Selection;
using AFLCoachSim.Core.Domain.ValueObjects;
using AFLCoachSim.Core.Engine.Simulation;

namespace AFLCoachSim.Core.Engine.Match.Commentary
{
    /// <summary>
    /// Captures match snapshots and generates commentary events
    /// </summary>
    public sealed class CommentarySink : ITelemetrySink
    {
        private readonly CommentaryGenerator _generator;
        private readonly Dictionary<TeamId, List<Domain.Entities.Player>> _rosters;
        private readonly Dictionary<TeamId, string> _teamNames;
        private readonly TeamId _homeTeamId;
        private readonly TeamId _awayTeamId;
        private readonly DeterministicRandom _rng;
        
        // Track previous state to detect changes
        private MatchSnapshot _previousSnapshot;
        
        // Generated commentary events
        public List<string> CommentaryEvents { get; private set; } = new List<string>();
        public List<MatchEvent> MatchEvents { get; private set; } = new List<MatchEvent>();
        
        public CommentarySink(
            TeamId homeTeamId,
            TeamId awayTeamId,
            Dictionary<TeamId, List<Domain.Entities.Player>> rosters,
            Dictionary<TeamId, string> teamNames,
            DeterministicRandom rng = null)
        {
            _homeTeamId = homeTeamId;
            _awayTeamId = awayTeamId;
            _rosters = rosters ?? new Dictionary<TeamId, List<Domain.Entities.Player>>();
            _teamNames = teamNames ?? new Dictionary<TeamId, string>();
            _rng = rng ?? new DeterministicRandom(12345);
            _generator = new CommentaryGenerator(_rng);
        }
        
        public void OnTick(MatchSnapshot snapshot)
        {
            if (_previousSnapshot == null)
            {
                GenerateQuarterStartEvent(snapshot);
            }
            else
            {
                DetectAndGenerateEvents(_previousSnapshot, snapshot);
            }
            
            _previousSnapshot = CloneSnapshot(snapshot);
        }
        
        public void OnComplete(MatchSnapshot finalSnapshot)
        {
            var quarterEndEvent = new MatchEvent
            {
                EventType = MatchEventType.QuarterEnd,
                Quarter = finalSnapshot.Quarter,
                TimeRemaining = 0,
                Phase = finalSnapshot.Phase
            };
            
            AddEvent(quarterEndEvent);
        }
        
        private void DetectAndGenerateEvents(MatchSnapshot previous, MatchSnapshot current)
        {
            // Quarter transition
            if (current.Quarter != previous.Quarter)
            {
                GenerateQuarterStartEvent(current);
            }
            
            // Score changes
            DetectScoringEvents(previous, current);
            
            // Injury and substitution events
            if (current.HomeInjuryEvents > previous.HomeInjuryEvents)
                GenerateInjuryEvent(current, isHome: true);
            if (current.AwayInjuryEvents > previous.AwayInjuryEvents)
                GenerateInjuryEvent(current, isHome: false);
            if (current.HomeInterchanges > previous.HomeInterchanges)
                GenerateSubstitutionEvent(current, isHome: true);
            if (current.AwayInterchanges > previous.AwayInterchanges)
                GenerateSubstitutionEvent(current, isHome: false);
            
            // Phase-based events (with controlled frequency)
            if (_rng.NextFloat() < GetPhaseCommentaryChance(current.Phase))
            {
                GeneratePhaseEvent(current);
            }
        }
        
        private void DetectScoringEvents(MatchSnapshot previous, MatchSnapshot current)
        {
            // Home team scoring
            if (current.HomeGoals > previous.HomeGoals)
                GenerateScoringEvent(current, MatchEventType.Goal, isHome: true);
            if (current.HomeBehinds > previous.HomeBehinds)
                GenerateScoringEvent(current, MatchEventType.Behind, isHome: true);
            
            // Away team scoring
            if (current.AwayGoals > previous.AwayGoals)
                GenerateScoringEvent(current, MatchEventType.Goal, isHome: false);
            if (current.AwayBehinds > previous.AwayBehinds)
                GenerateScoringEvent(current, MatchEventType.Behind, isHome: false);
        }
        
        private void GenerateScoringEvent(MatchSnapshot snapshot, MatchEventType eventType, bool isHome)
        {
            var teamId = isHome ? _homeTeamId : _awayTeamId;
            var player = GetPositionAwarePlayer(teamId, eventType);
            
            var matchEvent = new MatchEvent
            {
                EventType = eventType,
                Quarter = snapshot.Quarter,
                TimeRemaining = snapshot.TimeRemaining,
                Phase = snapshot.Phase,
                PrimaryPlayerName = player?.Name ?? "Unknown Player",
                TeamId = teamId,
                IsHomeTeam = isHome,
                ScoreValue = eventType == MatchEventType.Goal ? 6 : 1,
                ZoneDescription = GetScoringZoneDescription(snapshot.Phase)
            };
            
            AddEvent(matchEvent);
        }
        
        private void GeneratePhaseEvent(MatchSnapshot snapshot)
        {
            MatchEventType eventType;
            switch (snapshot.Phase)
            {
                case Phase.CenterBounce:
                    eventType = MatchEventType.CenterBounceWin;
                    break;
                case Phase.Inside50:
                    eventType = MatchEventType.Inside50Entry;
                    break;
                case Phase.OpenPlay:
                    eventType = GetRandomOpenPlayEvent();
                    break;
                default:
                    eventType = MatchEventType.Mark;
                    break;
            }
            
            // Randomly pick a team for the event
            var isHome = _rng.NextFloat() < 0.5f;
            var teamId = isHome ? _homeTeamId : _awayTeamId;
            var player = GetPositionAwarePlayer(teamId, eventType);
            var secondPlayer = GetPositionAwarePlayer(teamId, eventType);
            
            var matchEvent = new MatchEvent
            {
                EventType = eventType,
                Quarter = snapshot.Quarter,
                TimeRemaining = snapshot.TimeRemaining,
                Phase = snapshot.Phase,
                PrimaryPlayerName = player?.Name ?? "Player",
                SecondaryPlayerName = secondPlayer?.Name,
                TeamId = teamId,
                IsHomeTeam = isHome
            };
            
            AddEvent(matchEvent);
        }
        
        private void GenerateQuarterStartEvent(MatchSnapshot snapshot)
        {
            var matchEvent = new MatchEvent
            {
                EventType = MatchEventType.QuarterStart,
                Quarter = snapshot.Quarter,
                TimeRemaining = snapshot.TimeRemaining,
                Phase = snapshot.Phase
            };
            
            AddEvent(matchEvent);
        }
        
        private void GenerateInjuryEvent(MatchSnapshot snapshot, bool isHome)
        {
            var teamId = isHome ? _homeTeamId : _awayTeamId;
            var player = GetRandomPlayer(teamId); // Injuries can happen to any player
            
            var matchEvent = new MatchEvent
            {
                EventType = MatchEventType.Injury,
                Quarter = snapshot.Quarter,
                TimeRemaining = snapshot.TimeRemaining,
                Phase = snapshot.Phase,
                PrimaryPlayerName = player?.Name ?? "Player",
                TeamId = teamId,
                IsHomeTeam = isHome
            };
            
            AddEvent(matchEvent);
        }
        
        private void GenerateSubstitutionEvent(MatchSnapshot snapshot, bool isHome)
        {
            var teamId = isHome ? _homeTeamId : _awayTeamId;
            var offPlayer = GetRandomPlayer(teamId);
            var onPlayer = GetRandomPlayer(teamId);
            
            var matchEvent = new MatchEvent
            {
                EventType = MatchEventType.Substitution,
                Quarter = snapshot.Quarter,
                TimeRemaining = snapshot.TimeRemaining,
                Phase = snapshot.Phase,
                PrimaryPlayerName = offPlayer?.Name ?? "Player",
                SecondaryPlayerName = onPlayer?.Name ?? "Player",
                TeamId = teamId,
                IsHomeTeam = isHome
            };
            
            AddEvent(matchEvent);
        }
        
        private void AddEvent(MatchEvent matchEvent)
        {
            MatchEvents.Add(matchEvent);
            var commentary = _generator.GenerateCommentary(matchEvent);
            CommentaryEvents.Add(commentary);
        }
        
        // Helper methods
        private Domain.Entities.Player GetRandomPlayer(TeamId teamId)
        {
            if (!_rosters.TryGetValue(teamId, out var roster) || roster.Count == 0)
                return null;
                
            return roster[_rng.NextInt(0, roster.Count)];
        }
        
        /// <summary>
        /// Gets a player appropriate for the specific event type based on position
        /// </summary>
        private Domain.Entities.Player GetPositionAwarePlayer(TeamId teamId, MatchEventType eventType)
        {
            if (!_rosters.TryGetValue(teamId, out var roster) || roster.Count == 0)
                return null;

            // Use PositionalSelector to get appropriate player
            var selectedPlayer = PositionalSelector.GetPrimaryEventPlayer(roster, eventType, _rng);
            return selectedPlayer ?? roster[_rng.NextInt(0, roster.Count)];
        }
        
        private MatchEventType GetRandomOpenPlayEvent()
        {
            var events = new[] { MatchEventType.Mark, MatchEventType.Handball, MatchEventType.Kick, MatchEventType.Tackle };
            return events[_rng.NextInt(0, events.Length)];
        }
        
        private float GetPhaseCommentaryChance(Phase phase)
        {
            switch (phase)
            {
                case Phase.ShotOnGoal:
                    return 0.8f;  // High chance for shots
                case Phase.Inside50:
                    return 0.3f;    // Medium chance  
                case Phase.CenterBounce:
                    return 0.5f; // Medium chance
                case Phase.OpenPlay:
                    return 0.1f;     // Low chance
                default:
                    return 0.05f;                  // Very low chance
            }
        }
        
        private string GetScoringZoneDescription(Phase phase)
        {
            switch (phase)
            {
                case Phase.ShotOnGoal:
                    return _rng.NextFloat() < 0.5f ? "from close range" : "from the set shot";
                case Phase.Inside50:
                    return "from inside 50";
                default:
                    return "";
            }
        }
        
        private MatchSnapshot CloneSnapshot(MatchSnapshot original)
        {
            return new MatchSnapshot
            {
                Quarter = original.Quarter,
                TimeRemaining = original.TimeRemaining,
                Phase = original.Phase,
                HomeGoals = original.HomeGoals,
                HomeBehinds = original.HomeBehinds,
                HomePoints = original.HomePoints,
                AwayGoals = original.AwayGoals,
                AwayBehinds = original.AwayBehinds,
                AwayPoints = original.AwayPoints,
                HomeInterchanges = original.HomeInterchanges,
                AwayInterchanges = original.AwayInterchanges,
                HomeInjuryEvents = original.HomeInjuryEvents,
                AwayInjuryEvents = original.AwayInjuryEvents,
                HomeAvgConditionEnd = original.HomeAvgConditionEnd,
                AwayAvgConditionEnd = original.AwayAvgConditionEnd
            };
        }
    }
}
