using System;
using System.Collections.Generic;
using System.Text; // StringBuilder for optimized string operations
using AFLCoachSim.Core.Engine.Simulation;
using WeatherCondition = AFLCoachSim.Core.Engine.Match.Weather.Weather;

namespace AFLCoachSim.Core.Engine.Match.Commentary
{
    /// <summary>
    /// Generates natural language commentary from match events
    /// PERFORMANCE: Uses StringBuilder pooling to eliminate string allocations
    /// </summary>
    public sealed class CommentaryGenerator
    {
        private readonly DeterministicRandom _rng;

        // Commentary templates for variety
        private readonly Dictionary<MatchEventType, string[]> _templates;

        // Reusable StringBuilder to avoid allocations on each commentary event
        private readonly StringBuilder _stringBuilder;

        // Cached quarter names to avoid ToString() allocations
        private static readonly string[] QuarterNames = { "first", "second", "third", "fourth" };

        public CommentaryGenerator(DeterministicRandom rng = null)
        {
            _rng = rng ?? new DeterministicRandom(12345);
            _templates = InitializeTemplates();
            _stringBuilder = new StringBuilder(128); // Pre-allocate reasonable capacity
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
        
        /// <summary>
        /// OPTIMIZED: Format template using StringBuilder to avoid string allocations
        /// Replaces 5-6 string allocations with reusable StringBuilder
        /// </summary>
        private string FormatTemplate(string template, MatchEvent matchEvent)
        {
            // Clear the pooled StringBuilder for reuse
            _stringBuilder.Clear();

            // Manually replace placeholders by scanning template once
            int lastIndex = 0;
            int templateLength = template.Length;

            for (int i = 0; i < templateLength; i++)
            {
                if (template[i] == '{')
                {
                    // Find matching '}'
                    int endIndex = template.IndexOf('}', i);
                    if (endIndex == -1) break;

                    // Append text before placeholder
                    if (i > lastIndex)
                    {
                        _stringBuilder.Append(template, lastIndex, i - lastIndex);
                    }

                    // Get placeholder name
                    int placeholderStart = i + 1;
                    int placeholderLength = endIndex - placeholderStart;

                    // Replace placeholder based on name
                    if (placeholderLength == 4 && template[placeholderStart] == 't' && template[placeholderStart + 1] == 'i' && template[placeholderStart + 2] == 'm' && template[placeholderStart + 3] == 'e')
                    {
                        // {time}
                        _stringBuilder.Append(matchEvent.TimeDisplay);
                    }
                    else if (placeholderLength == 6 && template[placeholderStart] == 'p' && template[placeholderStart + 1] == 'l' && template[placeholderStart + 2] == 'a' && template[placeholderStart + 3] == 'y' && template[placeholderStart + 4] == 'e' && template[placeholderStart + 5] == 'r')
                    {
                        // {player}
                        _stringBuilder.Append(matchEvent.PrimaryPlayerName ?? "Player");
                    }
                    else if (placeholderLength == 7 && template[placeholderStart] == 'p' && template[placeholderStart + 1] == 'l' && template[placeholderStart + 6] == '2')
                    {
                        // {player2}
                        _stringBuilder.Append(matchEvent.SecondaryPlayerName ?? "teammate");
                    }
                    else if (placeholderLength == 4 && template[placeholderStart] == 'z' && template[placeholderStart + 1] == 'o' && template[placeholderStart + 2] == 'n' && template[placeholderStart + 3] == 'e')
                    {
                        // {zone}
                        if (matchEvent.ZoneDescription != null)
                            _stringBuilder.Append(matchEvent.ZoneDescription);
                    }
                    else if (placeholderLength == 7 && template[placeholderStart] == 'q' && template[placeholderStart + 1] == 'u' && template[placeholderStart + 2] == 'a' && template[placeholderStart + 3] == 'r' && template[placeholderStart + 4] == 't' && template[placeholderStart + 5] == 'e' && template[placeholderStart + 6] == 'r')
                    {
                        // {quarter}
                        _stringBuilder.Append(GetQuarterNameCached(matchEvent.Quarter));
                    }

                    lastIndex = endIndex + 1;
                    i = endIndex;
                }
            }

            // Append remaining text
            if (lastIndex < templateLength)
            {
                _stringBuilder.Append(template, lastIndex, templateLength - lastIndex);
            }

            // Add weather context where relevant (no string concatenation!)
            if (matchEvent.Weather != WeatherCondition.Clear && ShouldMentionWeather(matchEvent.EventType))
            {
                _stringBuilder.Append(GetWeatherSuffix(matchEvent.Weather));
            }

            return _stringBuilder.ToString();
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
        
        private string GetWeatherSuffix(WeatherCondition weather)
        {
            switch (weather)
            {
                case WeatherCondition.Windy:
                    return " despite the swirling wind";
                case WeatherCondition.LightRain:
                    return " in the slippery conditions";
                case WeatherCondition.HeavyRain:
                    return " through the driving rain";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// OPTIMIZED: Get cached quarter name to avoid allocations
        /// </summary>
        private static string GetQuarterNameCached(int quarter)
        {
            // Use cached array for quarters 1-4
            if (quarter >= 1 && quarter <= 4)
                return QuarterNames[quarter - 1];

            // Fallback for invalid quarters (shouldn't happen in normal gameplay)
            return $"quarter {quarter}";
        }

        // Legacy method for backward compatibility
        private string GetQuarterName(int quarter)
        {
            return GetQuarterNameCached(quarter);
        }
    }
}
