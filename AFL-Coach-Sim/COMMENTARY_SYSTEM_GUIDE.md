# AFL Coach Sim - Commentary System Guide

## 🎯 What's Been Added

The commentary system transforms your match simulation from simple score results into engaging, narrative-driven match experiences. Players now see realistic AFL commentary like:

```
Q1, 20:00 - The first quarter gets underway
Q1, 18:45 - Max Gawn wins the center bounce
Q1, 17:22 - Nick Daicos kicks to Bobby Hill  
Q1, 16:33 - Bobby Hill takes a spectacular mark! What a grab!
Q1, 16:15 - Bobby Hill slots it through! GOAL!
```

## 🛠 How It Works

### Core Components

1. **MatchEvent & MatchEventType** - Rich event structure with timing, players, context
2. **CommentaryGenerator** - Natural language generation with multiple templates  
3. **CommentarySink** - Hooks into your existing telemetry to detect events
4. **MatchEngineWithCommentary** - Easy integration wrapper

### Integration Points

- **Builds on your existing MatchEngine** - No breaking changes
- **Uses your telemetry system** - Leverages existing MatchSnapshot infrastructure  
- **Works with your team/player data** - Pulls real names from rosters
- **Deterministic** - Uses your existing random number generation

## 🚀 How to Use

### Simple Usage (Recommended)

Replace your current match simulation:

```csharp
// OLD: Standard simulation
var sim = new MatchSimulator(teams, rng);
var result = sim.Simulate(round, homeId, awayId);

// NEW: With commentary
var result = MatchEngineWithCommentary.PlayMatchWithCommentary(
    round, homeId, awayId, teams, rosters, rng: rng);

// Access commentary
foreach(var commentary in result.Commentary) 
{
    Debug.Log(commentary);
}

// Get just the highlights
var highlights = MatchEngineWithCommentary.GetMatchHighlights(result);
```

### Advanced Usage

```csharp
// Get quarter-by-quarter breakdown
var summaries = MatchEngineWithCommentary.GetQuarterSummaries(result);

// Access individual events with metadata
foreach(var matchEvent in result.Events)
{
    if (matchEvent.EventType == MatchEventType.Goal)
    {
        Debug.Log($"Goal by {matchEvent.PrimaryPlayerName} in Q{matchEvent.Quarter}!");
    }
}

// Access original match result
var standardResult = result.MatchResult; // This is your normal MatchResultDTO
```

## 📁 File Structure

```
Assets/SimCore/AFLCoachSim.Core/Engine/Match/Commentary/
├── MatchEvent.cs              # Event structure & types
├── CommentaryGenerator.cs     # Natural language generation  
├── CommentarySink.cs         # Telemetry integration
└── MatchEngineWithCommentary.cs # Easy integration wrapper

Assets/Scripts/
├── Demo/CommentaryDemo.cs                    # Standalone demo
└── Examples/SeasonBootWithCommentary.cs     # Integration example
```

## 🎮 Demo & Examples

### Standalone Demo
Add `CommentaryDemo.cs` to a GameObject to see the system in action with real AFL player names.

### Season Integration  
`SeasonBootWithCommentary.cs` shows how to integrate with your existing SeasonBoot system.

## ⚙️ Configuration

The commentary system is highly configurable:

```csharp
// Control event frequency
private float GetPhaseCommentaryChance(Phase phase)
{
    return phase switch
    {
        Phase.ShotOnGoal => 0.8f,  // High chance for shots
        Phase.Inside50 => 0.3f,    // Medium chance  
        Phase.OpenPlay => 0.1f,    // Low chance (prevents spam)
        _ => 0.05f
    };
}

// Weather integration
if (matchEvent.Weather != Weather.Clear && ShouldMentionWeather(matchEvent.EventType))
{
    result += GetWeatherSuffix(matchEvent.Weather);
}
```

## 🔧 Troubleshooting

### Compilation Error Fixed
The original inheritance issue with sealed `MatchResultDTO` has been resolved using composition pattern.

### Performance 
- Commentary events are generated selectively to avoid spam
- Uses existing deterministic RNG for consistency
- Minimal overhead on match simulation

### Customization
- Easy to add new event types in `MatchEventType` enum
- Commentary templates are fully customizable in `CommentaryGenerator`
- Event detection logic can be tuned in `CommentarySink`

## 🎯 Beta Impact

This enhancement transforms match simulation from:
- **Before**: "Home 85 - 67 Away" 
- **After**: Full narrative with 20+ commentary events per match

**Perfect for beta testing** - gives players compelling content to engage with while you build out other systems.

## 📈 Next Steps

With commentary system complete, the next Phase 1 features are:
1. **Basic Player Management** - Expand stats, injuries, morale
2. **Simple League System** - Season progression with enhanced matches  
3. **Roster Management** - Squad selection with commentary integration

The commentary system provides the engagement foundation that makes all other features more compelling!
