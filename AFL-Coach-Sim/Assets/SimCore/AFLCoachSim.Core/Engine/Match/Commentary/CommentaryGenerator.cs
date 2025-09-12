using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Engine.Simulation;

namespace AFLCoachSim.Core.Engine.Match.Commentary
{
    /// <summary>
    /// Generates natural language commentary from match events
    /// </summary>
    public sealed class CommentaryGenerator
    {
        private readonly DeterministicRandom _rng;
        
        // Commentary templates for variety
        private readonly Dictionary<MatchEventType, string[]> _templates;
        
        public CommentaryGenerator(DeterministicRandom rng = null)
        {
            _rng = rng ?? new DeterministicRandom(12345);
            _templates = InitializeTemplates();
        }
        
        /// <summary>
        /// Generate commentary text for a match event
        /// </summary>
        public string GenerateCommentary(MatchEvent matchEvent)
        {
            if (!_templates.TryGetValue(matchEvent.EventType, out var templates))
                return $"{matchEvent.TimeDisplay} - Match continues...";
            
            var template = templates[_rng.NextInt(0, templates.Length)];
            return FormatTemplate(template, matchEvent);
        }
        
        private string FormatTemplate(string template, MatchEvent matchEvent)
        {
            var result = template
                .Replace("{time}", matchEvent.TimeDisplay)
                .Replace("{player}", matchEvent.PrimaryPlayerName ?? "Player")
                .Replace("{player2}", matchEvent.SecondaryPlayerName ?? "teammate")
                .Replace("{zone}", matchEvent.ZoneDescription ?? "")
                .Replace("{quarter}", GetQuarterName(matchEvent.Quarter));
            
            // Add weather context where relevant
            if (matchEvent.Weather != Weather.Clear && ShouldMentionWeather(matchEvent.EventType))
            {
                result += GetWeatherSuffix(matchEvent.Weather);
            }
            
            return result;
        }
        
        private Dictionary<MatchEventType, string[]> InitializeTemplates()
        {
            return new Dictionary<MatchEventType, string[]>
            {
                [MatchEventType.Goal] = new[]
                {
                    "{time} - {player} slots it through! GOAL!",
                    "{time} - {player} finds the target {zone}. Six points!",
                    "{time} - Beautiful finish by {player}! GOAL!",
                    "{time} - {player} threads the needle! GOAL!",
                    "{time} - {player} makes no mistake {zone}. GOAL!"
                },
                
                [MatchEventType.Behind] = new[]
                {
                    "{time} - {player} just misses {zone}. Behind.",
                    "{time} - {player}'s shot drifts wide. One point.",
                    "{time} - Close, but not close enough for {player}. Behind.",
                    "{time} - {player} hits the post! Just a behind."
                },
                
                [MatchEventType.SpectacularMark] = new[]
                {
                    "{time} - {player} takes a spectacular mark! What a grab!",
                    "{time} - Incredible mark by {player}! The crowd is on its feet!",
                    "{time} - {player} flies high and takes a screamer!",
                    "{time} - Mark of the day candidate from {player}!"
                },
                
                [MatchEventType.Mark] = new[]
                {
                    "{time} - {player} takes a solid mark",
                    "{time} - Good hands from {player}",
                    "{time} - {player} secures the mark",
                    "{time} - Clean take by {player}"
                },
                
                [MatchEventType.Kick] = new[]
                {
                    "{time} - {player} kicks to {player2}",
                    "{time} - {player} finds {player2} with a good kick",
                    "{time} - {player} sends it long to {player2}",
                    "{time} - {player} delivers to {player2}"
                },
                
                [MatchEventType.Handball] = new[]
                {
                    "{time} - {player} handballs to {player2}",
                    "{time} - Quick hands from {player} to {player2}",
                    "{time} - {player} dishes it off to {player2}",
                    "{time} - Slick handball from {player}"
                },
                
                [MatchEventType.Tackle] = new[]
                {
                    "{time} - Great tackle by {player}!",
                    "{time} - {player} brings down his opponent with a strong tackle",
                    "{time} - {player} wraps up the ball carrier",
                    "{time} - Excellent defensive pressure from {player}"
                },
                
                [MatchEventType.Turnover] = new[]
                {
                    "{time} - Turnover! {player} loses possession",
                    "{time} - {player} coughs up the ball",
                    "{time} - Ball spills loose from {player}",
                    "{time} - {player} can't hold onto it"
                },
                
                [MatchEventType.CenterBounceWin] = new[]
                {
                    "{time} - {player} wins the center bounce",
                    "{time} - Clean tap from {player} at the center",
                    "{time} - {player} gets first hands on it from the bounce",
                    "{time} - {player} dominates the ruck contest"
                },
                
                [MatchEventType.Inside50Entry] = new[]
                {
                    "{time} - Ball moves into the forward 50",
                    "{time} - Inside 50 for the attacking team",
                    "{time} - The ball finds its way forward",
                    "{time} - Opportunity building in the forward line"
                },
                
                [MatchEventType.Rebound50] = new[]
                {
                    "{time} - {player} rebounds from defense",
                    "{time} - {player} clears the defensive 50",
                    "{time} - Good defensive work by {player}",
                    "{time} - {player} starts the counter-attack"
                },
                
                [MatchEventType.Injury] = new[]
                {
                    "{time} - {player} is down with an apparent injury",
                    "{time} - Concern for {player} who appears to be hurt",
                    "{time} - {player} requires attention from the trainers",
                    "{time} - Medical staff attend to {player}"
                },
                
                [MatchEventType.Substitution] = new[]
                {
                    "{time} - {player} comes off for {player2}",
                    "{time} - Fresh legs as {player2} replaces {player}",
                    "{time} - Interchange as {player} makes way for {player2}",
                    "{time} - Coaching change: {player2} on for {player}"
                },
                
                [MatchEventType.QuarterStart] = new[]
                {
                    "{time} - The {quarter} quarter gets underway",
                    "{time} - Play resumes for the {quarter} term",
                    "{time} - {quarter} quarter begins"
                },
                
                [MatchEventType.QuarterEnd] = new[]
                {
                    "End of {quarter} quarter",
                    "The siren sounds to end the {quarter} term",
                    "{quarter} quarter concludes"
                }
            };
        }
        
        private bool ShouldMentionWeather(MatchEventType eventType)
        {
            return eventType == MatchEventType.Goal ||
                   eventType == MatchEventType.Behind ||
                   eventType == MatchEventType.Kick ||
                   eventType == MatchEventType.Mark;
        }
        
        private string GetWeatherSuffix(Weather weather)
        {
            switch (weather)
            {
                case Weather.Windy:
                    return " despite the swirling wind";
                case Weather.LightRain:
                    return " in the slippery conditions";
                case Weather.HeavyRain:
                    return " through the driving rain";
                default:
                    return "";
            }
        }
        
        private string GetQuarterName(int quarter)
        {
            switch (quarter)
            {
                case 1:
                    return "first";
                case 2:
                    return "second";
                case 3:
                    return "third";
                case 4:
                    return "fourth";
                default:
                    return $"quarter {quarter}";
            }
        }
    }
}
