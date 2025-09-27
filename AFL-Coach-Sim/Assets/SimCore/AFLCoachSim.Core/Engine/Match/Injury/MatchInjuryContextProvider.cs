using System;
using AFLCoachSim.Core.Engine.Match;
using AFLCoachSim.Core.Injuries;
using AFLCoachSim.Core.Injuries.Domain;

namespace AFLCoachSim.Core.Engine.Match.Injury
{
    /// <summary>
    /// Provides contextual information about match events to the injury manager
    /// </summary>
    public class MatchInjuryContextProvider : IInjuryContextProvider
    {
        private MatchContext _currentMatchContext;
        
        public void SetMatchContext(MatchContext matchContext)
        {
            _currentMatchContext = matchContext;
        }
        
        public InjuryContext GetInjuryContext(int playerId, InjuryType injuryType, InjurySeverity severity)
        {
            if (_currentMatchContext == null)
            {
                return CreateDefaultContext(playerId, injuryType, severity);
            }
            
            var context = new InjuryContext
            {
                PlayerId = playerId,
                OccurredAt = DateTime.UtcNow,
                Location = "Match",
                Activity = GetActivityDescription(),
                EnvironmentalFactors = GetEnvironmentalFactors(),
                IntensityLevel = GetMatchIntensityLevel(),
                InjuryDescription = GenerateMatchSpecificDescription(injuryType, severity),
                AdditionalNotes = GetAdditionalMatchNotes()
            };
            
            return context;
        }
        
        private InjuryContext CreateDefaultContext(int playerId, InjuryType injuryType, InjurySeverity severity)
        {
            return new InjuryContext
            {
                PlayerId = playerId,
                OccurredAt = DateTime.UtcNow,
                Location = "Match",
                Activity = "Match Play",
                EnvironmentalFactors = "Standard AFL conditions",
                IntensityLevel = IntensityLevel.High,
                InjuryDescription = $"Sustained {severity.ToString().ToLower()} {injuryType.ToString().ToLower()} injury during match",
                AdditionalNotes = "Context not available"
            };
        }
        
        private string GetActivityDescription()
        {
            if (_currentMatchContext?.CurrentPhase == null)
                return "Match Play";
                
            return _currentMatchContext.CurrentPhase switch
            {
                Phase.CenterBounce => "Center bounce contest",
                Phase.OpenPlay => "Open field play",
                Phase.Inside50 => "Forward 50 contest",
                _ => "Match Play"
            };
        }
        
        private string GetEnvironmentalFactors()
        {
            var factors = "AFL match conditions";
            
            if (_currentMatchContext?.Weather != null)
            {
                var weather = _currentMatchContext.Weather;
                factors = $"Weather: {weather.Condition}, Temperature: {weather.Temperature}°C";
                
                if (weather.IsWet)
                {
                    factors += ", Wet conditions";
                }
                
                if (weather.IsWindy)
                {
                    factors += $", Wind: {weather.WindSpeed}km/h";
                }
            }
            
            if (_currentMatchContext?.Venue != null)
            {
                factors += $", Venue: {_currentMatchContext.Venue}";
            }
            
            return factors;
        }
        
        private IntensityLevel GetMatchIntensityLevel()
        {
            if (_currentMatchContext?.CurrentPhase == null)
                return IntensityLevel.High;
                
            return _currentMatchContext.CurrentPhase switch
            {
                Phase.CenterBounce => IntensityLevel.VeryHigh,
                Phase.Inside50 => IntensityLevel.VeryHigh,
                Phase.OpenPlay => IntensityLevel.High,
                _ => IntensityLevel.High
            };
        }
        
        private string GenerateMatchSpecificDescription(InjuryType injuryType, InjurySeverity severity)
        {
            var phaseContext = GetPhaseSpecificContext();
            var injuryTypeDescription = GetInjuryTypeDescription(injuryType);
            var severityDescription = GetSeverityDescription(severity);
            
            return $"Player sustained {severityDescription} {injuryTypeDescription} {phaseContext}";
        }
        
        private string GetPhaseSpecificContext()
        {
            if (_currentMatchContext?.CurrentPhase == null)
                return "during match play";
                
            return _currentMatchContext.CurrentPhase switch
            {
                Phase.CenterBounce => "during center bounce contest with heavy body contact",
                Phase.Inside50 => "during forward 50 aerial contest or ground ball contest",
                Phase.OpenPlay => "during running play in open field",
                _ => "during match play"
            };
        }
        
        private string GetInjuryTypeDescription(InjuryType injuryType)
        {
            return injuryType switch
            {
                InjuryType.Muscle => "muscle strain",
                InjuryType.Joint => "joint injury",
                InjuryType.Bone => "bone injury",
                InjuryType.Ligament => "ligament damage",
                InjuryType.Concussion => "head knock with concussion symptoms",
                InjuryType.Skin => "laceration or abrasion",
                InjuryType.Other => "soft tissue injury",
                _ => "injury"
            };
        }
        
        private string GetSeverityDescription(InjurySeverity severity)
        {
            return severity switch
            {
                InjurySeverity.Niggle => "a minor",
                InjurySeverity.Minor => "a minor",
                InjurySeverity.Moderate => "a moderate",
                InjurySeverity.Major => "a significant",
                InjurySeverity.Severe => "a severe",
                _ => "an"
            };
        }
        
        private string GetAdditionalMatchNotes()
        {
            var notes = "";
            
            if (_currentMatchContext != null)
            {
                notes = $"Quarter: {_currentMatchContext.CurrentQuarter}, ";
                notes += $"Time: {_currentMatchContext.TimeInQuarter:mm\\:ss}";
                
                if (_currentMatchContext.ScoreDifferential.HasValue)
                {
                    var margin = Math.Abs(_currentMatchContext.ScoreDifferential.Value);
                    var leader = _currentMatchContext.ScoreDifferential.Value > 0 ? "Home" : "Away";
                    notes += $", Score: {leader} leading by {margin}";
                }
                
                if (_currentMatchContext.RecentEvents?.Count > 0)
                {
                    notes += $", Recent events: {string.Join(", ", _currentMatchContext.RecentEvents)}";
                }
            }
            
            return notes;
        }
    }
    
    /// <summary>
    /// Contains contextual information about the current match state
    /// </summary>
    public class MatchContext
    {
        public Phase? CurrentPhase { get; set; }
        public int CurrentQuarter { get; set; }
        public TimeSpan TimeInQuarter { get; set; }
        public string Venue { get; set; }
        public MatchWeather Weather { get; set; }
        public int? ScoreDifferential { get; set; } // Positive = home team leading
        public System.Collections.Generic.List<string> RecentEvents { get; set; } = new System.Collections.Generic.List<string>();
        
        public void AddEvent(string eventDescription)
        {
            RecentEvents.Add(eventDescription);
            // Keep only recent events (last 5)
            if (RecentEvents.Count > 5)
            {
                RecentEvents.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// Weather conditions during the match
    /// </summary>
    public class MatchWeather
    {
        public string Condition { get; set; } = "Clear";
        public int Temperature { get; set; } = 20;
        public int WindSpeed { get; set; } = 0;
        public bool IsWet => Condition.Contains("Rain") || Condition.Contains("Drizzle");
        public bool IsWindy => WindSpeed > 20;
        public bool IsHot => Temperature > 30;
        public bool IsCold => Temperature < 10;
        
        /// <summary>
        /// Get injury risk modifier based on weather conditions
        /// </summary>
        public float GetInjuryRiskModifier()
        {
            float modifier = 1.0f;
            
            if (IsWet)
            {
                modifier += 0.15f; // 15% increased risk in wet conditions
            }
            
            if (IsWindy)
            {
                modifier += 0.05f; // 5% increased risk in very windy conditions
            }
            
            if (IsHot)
            {
                modifier += 0.10f; // 10% increased risk in hot conditions (fatigue/dehydration)
            }
            
            if (IsCold)
            {
                modifier += 0.08f; // 8% increased risk in cold conditions (muscle stiffness)
            }
            
            return modifier;
        }
    }
}