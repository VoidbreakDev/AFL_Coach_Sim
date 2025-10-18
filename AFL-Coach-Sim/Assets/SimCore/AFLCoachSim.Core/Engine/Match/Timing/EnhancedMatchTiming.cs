using System;
using System.Collections.Generic;
using AFLCoachSim.Core.Engine.Match.Runtime.Telemetry;
using AFLCoachSim.Core.Engine.Match.Weather;

namespace AFLCoachSim.Core.Engine.Match.Timing
{
    /// <summary>
    /// Enhanced match timing system with flexible quarter durations, realistic pacing,
    /// time-on periods, and integration with all match engine systems
    /// </summary>
    public class EnhancedMatchTiming
    {
        private readonly MatchTimingConfiguration _config;
        private readonly List<QuarterTimingData> _quarterData;
        private readonly List<TimingEvent> _timingEvents;
        
        // Current state
        private int _currentQuarter;
        private float _timeRemainingInQuarter;
        private float _timeOnClock;
        private bool _clockRunning;
        private float _timeSinceLastTick;
        private Phase _currentPhase;
        
        // Time tracking
        private float _realTimeElapsed;
        private float _gameTimeElapsed;
        private Dictionary<Phase, float> _timeInPhases;
        
        // Advanced timing features
        private float _timeOnPeriod; // Additional time for stoppages
        private bool _inTimeOnPeriod;
        private float _quarterBreakRemaining;
        private bool _inQuarterBreak;
        
        public EnhancedMatchTiming(MatchTimingConfiguration config = null)
        {
            _config = config ?? MatchTimingConfiguration.Default;
            _quarterData = new List<QuarterTimingData>();
            _timingEvents = new List<TimingEvent>();
            _timeInPhases = new Dictionary<Phase, float>();
            
            InitializeTiming();
        }
        
        /// <summary>
        /// Initialize timing for match start
        /// </summary>
        private void InitializeTiming()
        {
            _currentQuarter = 1;
            _timeRemainingInQuarter = _config.QuarterDurationSeconds;
            _timeOnClock = _config.QuarterDurationSeconds;
            _clockRunning = true;
            _timeSinceLastTick = 0f;
            _realTimeElapsed = 0f;
            _gameTimeElapsed = 0f;
            _timeOnPeriod = 0f;
            _inTimeOnPeriod = false;
            _quarterBreakRemaining = 0f;
            _inQuarterBreak = false;
            
            // Initialize phase tracking
            foreach (Phase phase in Enum.GetValues(typeof(Phase)))
            {
                _timeInPhases[phase] = 0f;
            }
            
            // Create initial quarter data
            for (int i = 1; i <= 4; i++)
            {
                _quarterData.Add(new QuarterTimingData
                {
                    Quarter = i,
                    PlannedDuration = _config.QuarterDurationSeconds,
                    ActualDuration = 0f,
                    TimeOnDuration = 0f,
                    PhaseBreakdown = new Dictionary<Phase, float>()
                });
            }
            
            RecordTimingEvent(TimingEventType.MatchStart, "Match commenced");
        }
        
        /// <summary>
        /// Update timing system each simulation tick
        /// </summary>
        public TimingUpdate UpdateTiming(float deltaTime, Phase currentPhase, AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            _currentPhase = currentPhase;
            _realTimeElapsed += deltaTime;
            _timeSinceLastTick += deltaTime;
            
            var update = new TimingUpdate();
            
            // Handle quarter breaks
            if (_inQuarterBreak)
            {
                return HandleQuarterBreak(deltaTime, update);
            }
            
            // Calculate time progression based on phase and match state
            float timeProgression = CalculateTimeProgression(deltaTime, currentPhase, context);
            
            if (_clockRunning)
            {
                _gameTimeElapsed += timeProgression;
                _timeRemainingInQuarter -= timeProgression;
                _timeOnClock -= timeProgression;
                
                // Track time in current phase
                if (_timeInPhases.ContainsKey(currentPhase))
                {
                    _timeInPhases[currentPhase] += timeProgression;
                }
                
                // Update quarter data
                if (_currentQuarter <= _quarterData.Count)
                {
                    var quarterData = _quarterData[_currentQuarter - 1];
                    quarterData.ActualDuration += timeProgression;
                    
                    if (!quarterData.PhaseBreakdown.ContainsKey(currentPhase))
                        quarterData.PhaseBreakdown[currentPhase] = 0f;
                    quarterData.PhaseBreakdown[currentPhase] += timeProgression;
                }
            }
            
            // Check for quarter end or time-on period
            if (_timeRemainingInQuarter <= 0f && !_inTimeOnPeriod)
            {
                return HandleQuarterEnd(update, context);
            }
            
            // Handle time-on periods
            HandleTimeOnPeriods(currentPhase, context, update);
            
            // Populate update information
            update.CurrentQuarter = _currentQuarter;
            update.TimeRemaining = _timeRemainingInQuarter;
            update.TimeOnClock = _timeOnClock;
            update.ClockRunning = _clockRunning;
            update.InTimeOnPeriod = _inTimeOnPeriod;
            update.TimeOnRemaining = _timeOnPeriod;
            update.RealTimeElapsed = _realTimeElapsed;
            update.GameTimeElapsed = _gameTimeElapsed;
            update.CurrentPhase = currentPhase;
            update.PhaseTimeSpent = _timeInPhases.GetValueOrDefault(currentPhase, 0f);
            
            return update;
        }
        
        /// <summary>
        /// Calculate time progression based on current game state
        /// </summary>
        private float CalculateTimeProgression(float deltaTime, Phase currentPhase, AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            float baseProgression = deltaTime;
            
            // Apply phase-specific time scaling
            float phaseModifier = GetPhaseTimeModifier(currentPhase);
            baseProgression *= phaseModifier;
            
            // Apply injury/stoppage modifiers
            if (HasRecentInjuries(context))
            {
                baseProgression *= _config.InjuryTimeModifier;
            }
            
            // Apply weather modifiers
            float weatherModifier = GetWeatherTimeModifier(context.Weather);
            baseProgression *= weatherModifier;
            
            // Apply quarter-specific pacing
            float quarterModifier = GetQuarterPacingModifier(_currentQuarter, _timeRemainingInQuarter);
            baseProgression *= quarterModifier;
            
            return baseProgression;
        }
        
        /// <summary>
        /// Get time modifier based on current phase
        /// </summary>
        private float GetPhaseTimeModifier(Phase phase)
        {
            return phase switch
            {
                Phase.ShotOnGoal => _config.ShotPhaseTimeModifier,
                Phase.Inside50 => _config.Inside50TimeModifier,
                Phase.CenterBounce => _config.CenterBounceTimeModifier,
                Phase.OpenPlay => _config.OpenPlayTimeModifier,
                Phase.Stoppage => _config.StoppageTimeModifier,
                Phase.KickIn => _config.KickInTimeModifier,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Get weather-based time modifier
        /// </summary>
        private float GetWeatherTimeModifier(AFLCoachSim.Core.Engine.Match.Weather.Weather weather)
        {
            return weather switch
            {
                AFLCoachSim.Core.Engine.Match.Weather.Weather.HeavyRain => _config.HeavyRainTimeModifier,
                AFLCoachSim.Core.Engine.Match.Weather.Weather.LightRain => _config.LightRainTimeModifier,
                AFLCoachSim.Core.Engine.Match.Weather.Weather.Windy => _config.WindyTimeModifier,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Get quarter-specific pacing modifier
        /// </summary>
        private float GetQuarterPacingModifier(int quarter, float timeRemaining)
        {
            // Fourth quarter tends to run faster due to urgency
            if (quarter == 4)
            {
                // Last 5 minutes of fourth quarter run faster
                if (timeRemaining <= 300f) // 5 minutes
                {
                    return _config.FourthQuarterFinalMinutesModifier;
                }
                return _config.FourthQuarterModifier;
            }
            
            // First quarter might run slightly slower due to settling in
            if (quarter == 1 && timeRemaining > 900f) // First 15 minutes
            {
                return _config.FirstQuarterSettlingModifier;
            }
            
            return 1.0f;
        }
        
        /// <summary>
        /// Handle quarter end and transition
        /// </summary>
        private TimingUpdate HandleQuarterEnd(TimingUpdate update, AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            // Determine if time-on should be added
            float timeOnToAdd = CalculateTimeOnForQuarter(context);
            
            if (timeOnToAdd > 0f && !_inTimeOnPeriod)
            {
                StartTimeOnPeriod(timeOnToAdd);
                update.TimeOnStarted = true;
                update.TimeOnDuration = timeOnToAdd;
                
                RecordTimingEvent(TimingEventType.TimeOnStarted, 
                    $"Time-on period started: {timeOnToAdd:F1} seconds");
                
                return update;
            }
            
            // Quarter actually ended
            RecordTimingEvent(TimingEventType.QuarterEnd, 
                $"Quarter {_currentQuarter} ended");
            
            // Update quarter data
            if (_currentQuarter <= _quarterData.Count)
            {
                var quarterData = _quarterData[_currentQuarter - 1];
                quarterData.TimeOnDuration = _inTimeOnPeriod ? _timeOnPeriod : 0f;
                quarterData.ActualDuration += quarterData.TimeOnDuration;
            }
            
            update.QuarterEnded = true;
            update.CompletedQuarter = _currentQuarter;
            
            // Check if match is finished
            if (_currentQuarter >= 4)
            {
                update.MatchEnded = true;
                RecordTimingEvent(TimingEventType.MatchEnd, "Match completed");
                return update;
            }
            
            // Start quarter break
            StartQuarterBreak();
            update.QuarterBreakStarted = true;
            
            return update;
        }
        
        /// <summary>
        /// Handle quarter break timing
        /// </summary>
        private TimingUpdate HandleQuarterBreak(float deltaTime, TimingUpdate update)
        {
            _quarterBreakRemaining -= deltaTime;
            
            update.InQuarterBreak = true;
            update.QuarterBreakRemaining = _quarterBreakRemaining;
            update.CurrentQuarter = _currentQuarter; // Still in same quarter during break
            
            if (_quarterBreakRemaining <= 0f)
            {
                EndQuarterBreak();
                update.QuarterBreakEnded = true;
                update.NewQuarterStarted = true;
                update.CurrentQuarter = _currentQuarter;
                
                RecordTimingEvent(TimingEventType.QuarterStart, 
                    $"Quarter {_currentQuarter} started");
            }
            
            return update;
        }
        
        /// <summary>
        /// Start time-on period
        /// </summary>
        private void StartTimeOnPeriod(float duration)
        {
            _inTimeOnPeriod = true;
            _timeOnPeriod = duration;
            _timeRemainingInQuarter = duration;
            _timeOnClock = duration;
        }
        
        /// <summary>
        /// Start quarter break
        /// </summary>
        private void StartQuarterBreak()
        {
            _inQuarterBreak = true;
            _quarterBreakRemaining = GetQuarterBreakDuration(_currentQuarter);
            _clockRunning = false;
        }
        
        /// <summary>
        /// End quarter break and start next quarter
        /// </summary>
        private void EndQuarterBreak()
        {
            _inQuarterBreak = false;
            _quarterBreakRemaining = 0f;
            _currentQuarter++;
            _timeRemainingInQuarter = _config.QuarterDurationSeconds;
            _timeOnClock = _config.QuarterDurationSeconds;
            _clockRunning = true;
            _inTimeOnPeriod = false;
            _timeOnPeriod = 0f;
        }
        
        /// <summary>
        /// Get quarter break duration
        /// </summary>
        private float GetQuarterBreakDuration(int completedQuarter)
        {
            return completedQuarter switch
            {
                1 => _config.QuarterBreakDuration,
                2 => _config.HalfTimeDuration,
                3 => _config.ThreeQuarterBreakDuration,
                _ => 0f
            };
        }
        
        /// <summary>
        /// Calculate time-on duration for current quarter
        /// </summary>
        private float CalculateTimeOnForQuarter(AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            float baseTimeOn = 0f;
            
            // Add time for injuries
            int injuries = GetQuarterInjuries(context);
            baseTimeOn += injuries * _config.TimeOnPerInjury;
            
            // Add time for stoppages
            int majorStoppages = GetQuarterMajorStoppages(context);
            baseTimeOn += majorStoppages * _config.TimeOnPerMajorStoppage;
            
            // Add random variation
            float variation = context.Rng.NextInt((int)-_config.TimeOnRandomVariation, (int)_config.TimeOnRandomVariation);
            baseTimeOn += variation;
            
            // Minimum and maximum bounds
            return Math.Max(0f, Math.Min(baseTimeOn, _config.MaxTimeOnPerQuarter));
        }
        
        /// <summary>
        /// Handle time-on periods during play
        /// </summary>
        private void HandleTimeOnPeriods(Phase currentPhase, AFLCoachSim.Core.Engine.Match.MatchContext context, TimingUpdate update)
        {
            if (_inTimeOnPeriod && _timeOnPeriod <= 0f)
            {
                _inTimeOnPeriod = false;
                update.TimeOnEnded = true;
                
                RecordTimingEvent(TimingEventType.TimeOnEnded, "Time-on period ended");
            }
        }
        
        /// <summary>
        /// Check for recent injuries affecting timing
        /// </summary>
        private bool HasRecentInjuries(AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            // Check if there have been injuries in the last few ticks
            return context.Telemetry.HomeInjuryEvents > 0 || context.Telemetry.AwayInjuryEvents > 0;
        }
        
        /// <summary>
        /// Get injury count for current quarter
        /// </summary>
        private int GetQuarterInjuries(AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            // This would need to track per-quarter injuries
            // For now, return total injuries as approximation
            return context.Telemetry.HomeInjuryEvents + context.Telemetry.AwayInjuryEvents;
        }
        
        /// <summary>
        /// Get major stoppage count for current quarter
        /// </summary>
        private int GetQuarterMajorStoppages(AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            // Estimate based on game flow - this could be enhanced with actual stoppage tracking
            float gameFlow = CalculateGameFlow(context);
            return gameFlow < 0.3f ? 2 : (gameFlow < 0.6f ? 1 : 0);
        }
        
        /// <summary>
        /// Calculate game flow quality (0-1, higher = better flow)
        /// </summary>
        private float CalculateGameFlow(AFLCoachSim.Core.Engine.Match.MatchContext context)
        {
            // Simple calculation based on available data
            float flow = 0.7f; // Base flow
            
            // Weather reduces flow
            if (context.Weather == AFLCoachSim.Core.Engine.Match.Weather.Weather.HeavyRain) flow -= 0.3f;
            else if (context.Weather == AFLCoachSim.Core.Engine.Match.Weather.Weather.LightRain) flow -= 0.15f;
            else if (context.Weather == AFLCoachSim.Core.Engine.Match.Weather.Weather.Windy) flow -= 0.1f;
            
            // Injuries reduce flow
            int totalInjuries = context.Telemetry.HomeInjuryEvents + context.Telemetry.AwayInjuryEvents;
            flow -= totalInjuries * 0.1f;
            
            return Math.Max(0f, Math.Min(1f, flow));
        }
        
        /// <summary>
        /// Record timing event for analysis
        /// </summary>
        private void RecordTimingEvent(TimingEventType eventType, string description)
        {
            _timingEvents.Add(new TimingEvent
            {
                EventType = eventType,
                Timestamp = DateTime.Now,
                GameTime = _gameTimeElapsed,
                RealTime = _realTimeElapsed,
                Quarter = _currentQuarter,
                Description = description
            });
        }
        
        /// <summary>
        /// Get comprehensive timing statistics
        /// </summary>
        public MatchTimingStatistics GetTimingStatistics()
        {
            return new MatchTimingStatistics
            {
                TotalRealTime = _realTimeElapsed,
                TotalGameTime = _gameTimeElapsed,
                CurrentQuarter = _currentQuarter,
                TimeRemaining = _timeRemainingInQuarter,
                InTimeOnPeriod = _inTimeOnPeriod,
                TimeOnRemaining = _timeOnPeriod,
                QuarterData = new List<QuarterTimingData>(_quarterData),
                PhaseBreakdown = new Dictionary<Phase, float>(_timeInPhases),
                TimingEvents = new List<TimingEvent>(_timingEvents),
                AverageTimePerPhase = CalculateAverageTimePerPhase(),
                GamePacing = CalculateGamePacing(),
                MatchCompletionPercentage = CalculateMatchCompletionPercentage()
            };
        }
        
        private Dictionary<Phase, float> CalculateAverageTimePerPhase()
        {
            var averages = new Dictionary<Phase, float>();
            float totalTime = _gameTimeElapsed;
            
            if (totalTime > 0)
            {
                foreach (var phase in _timeInPhases)
                {
                    averages[phase.Key] = phase.Value / totalTime;
                }
            }
            
            return averages;
        }
        
        private float CalculateGamePacing()
        {
            if (_realTimeElapsed == 0) return 1.0f;
            return _gameTimeElapsed / _realTimeElapsed;
        }
        
        private float CalculateMatchCompletionPercentage()
        {
            float totalExpectedTime = _config.QuarterDurationSeconds * 4;
            return Math.Min(1.0f, _gameTimeElapsed / totalExpectedTime);
        }
        
        /// <summary>
        /// Pause/resume match clock
        /// </summary>
        public void PauseClock()
        {
            _clockRunning = false;
            RecordTimingEvent(TimingEventType.ClockPaused, "Match clock paused");
        }
        
        public void ResumeClock()
        {
            _clockRunning = true;
            RecordTimingEvent(TimingEventType.ClockResumed, "Match clock resumed");
        }
        
        /// <summary>
        /// Check if match is complete
        /// </summary>
        public bool IsMatchComplete => _currentQuarter > 4;
        
        /// <summary>
        /// Get current time display string (MM:SS format)
        /// </summary>
        public string GetTimeDisplay()
        {
            int minutes = (int)(_timeOnClock / 60);
            int seconds = (int)(_timeOnClock % 60);
            return $"{minutes:D2}:{seconds:D2}";
        }
        
        /// <summary>
        /// Get detailed time display with time-on indication
        /// </summary>
        public string GetDetailedTimeDisplay()
        {
            string baseTime = GetTimeDisplay();
            if (_inTimeOnPeriod)
            {
                int timeOnMinutes = (int)(_timeOnPeriod / 60);
                int timeOnSeconds = (int)(_timeOnPeriod % 60);
                return $"{baseTime} + {timeOnMinutes:D2}:{timeOnSeconds:D2}";
            }
            return baseTime;
        }
    }
}