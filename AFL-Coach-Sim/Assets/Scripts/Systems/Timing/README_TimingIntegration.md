# AFL Coach Sim - Timing Systems Season Integration

## Overview

This document describes the integration of the advanced timing systems (Compressed and Variable Speed Timing) into the AFL Coach Sim season flow. The integration provides intelligent, context-aware timing that adapts to different match types, player preferences, and season phases.

## Key Features

### 🎯 Context-Aware Timing Selection
- **Match Type Awareness**: Different timing systems for regular matches, finals, grand finals
- **Season Phase Adaptation**: Early season vs late season timing preferences
- **Player Team Focus**: Special timing for matches involving the player's team
- **Rivalry Recognition**: Enhanced timing for important rivalry matches
- **Close Match Detection**: Slower pacing for tight, exciting matches

### 🎮 Player Experience Levels
- **Beginner**: Slower, more controlled timing with lower speed limits
- **Intermediate**: Balanced timing with moderate speed ranges
- **Expert**: Full speed control and compressed timing options

### 📊 Season-Level Configuration
- **ScriptableObject Configuration**: Easy-to-configure timing preferences in Unity Editor
- **Adaptive Timing**: Automatic timing system selection based on match context
- **Override Controls**: Player can override timing systems where allowed

## Architecture Overview

```
SeasonTimingConfiguration
├── Match Type Configs (Regular, Finals, Grand Final)
├── Season Phase Configs (Early, Mid, Late, Finals)
├── Context Settings (Close matches, Player team, Rivalries)
└── Player Experience Settings (Beginner, Intermediate, Expert)

TimingIntegratedSeasonBoot
├── Season initialization with timing context
├── Match-by-match timing configuration
├── Real-time and batch simulation modes
└── Telemetry and statistics tracking

TimingIntegratedSeasonScreenManager  
├── Interactive match simulation with timing controls
├── UI integration for speed/pause controls
├── Match status and timing system indicators
└── Event handling for timing changes

TimingIntegratedSeasonScheduler
├── Enhanced fixture generation with timing metadata
├── Special match detection (ANZAC Day, etc.)
├── Rivalry relationship tracking
└── Expected margin and difficulty calculation
```

## Quick Start Guide

### 1. Setup Season Timing Configuration

Create a `SeasonTimingConfiguration` asset in Unity:

```csharp
// In Unity Editor: Create → AFL Manager → Season → Timing Configuration
var config = CreateInstance<SeasonTimingConfiguration>();

// Configure default settings
config.defaultTimingSystem = TimingSystemType.VariableSpeed;
config.allowPlayerTimingOverride = true;
config.enableAdaptiveTiming = true;
```

### 2. Configure Match Types

```csharp
// Example: Grand Final always uses Variable Speed
var grandFinalConfig = new MatchTypeTimingConfig
{
    MatchType = MatchType.GrandFinal,
    PreferredTimingSystem = TimingSystemType.VariableSpeed,
    ForceTimingSystem = true, // Cannot be overridden
    Description = "Grand Final requires full engagement"
};
```

### 3. Set Up Season Phase Timing

```csharp
// Example: Early season non-player matches use compressed timing
var earlySeasonConfig = new SeasonPhaseTimingConfig
{
    SeasonPhase = SeasonPhase.EarlySeason,
    PreferredTimingSystem = TimingSystemType.Compressed,
    OverrideMatchType = false // Don't override special match types
};
```

### 4. Initialize Season Boot

```csharp
public class YourSeasonManager : MonoBehaviour
{
    [SerializeField] private SeasonTimingConfiguration timingConfig;
    [SerializeField] private TimingIntegratedSeasonBoot seasonBoot;
    
    void Start()
    {
        // Configure the season boot
        seasonBoot.seasonTimingConfig = timingConfig;
        seasonBoot.playerExperienceLevel = PlayerExperienceLevel.Intermediate;
        seasonBoot.enableInteractiveMatches = true;
        
        // Subscribe to events
        seasonBoot.OnMatchCompleted += HandleMatchCompleted;
        seasonBoot.OnSeasonTimingUpdate += HandleTimingUpdate;
        
        // Initialize season
        seasonBoot.InitializeSeason();
    }
}
```

## Configuration Examples

### Basic Configuration for All Match Types

```csharp
// Standard league configuration
var config = CreateInstance<SeasonTimingConfiguration>();

// Default to variable speed for most matches
config.defaultTimingSystem = TimingSystemType.VariableSpeed;
config.allowPlayerTimingOverride = true;
config.enableAdaptiveTiming = true;

// Player team matches always use variable speed
config.contextSettings.PlayerTeamPreferences.UseSpecialTiming = true;
config.contextSettings.PlayerTeamPreferences.PreferredSystem = TimingSystemType.VariableSpeed;

// Close matches slow down automatically  
config.contextSettings.SlowDownForCloseMatches = true;

// Expert players can use compressed timing for non-player matches
config.playerSettings.ExpertSettings.UseCustomTiming = true;
config.playerSettings.ExpertSettings.PreferredSystem = TimingSystemType.Compressed;
```

### Finals-Focused Configuration

```csharp
// Finals series configuration
var finalsConfig = new SeasonPhaseTimingConfig
{
    SeasonPhase = SeasonPhase.Finals,
    PreferredTimingSystem = TimingSystemType.VariableSpeed,
    OverrideMatchType = true // Force variable speed for all finals
};

var grandFinalConfig = new MatchTypeTimingConfig  
{
    MatchType = MatchType.GrandFinal,
    PreferredTimingSystem = TimingSystemType.VariableSpeed,
    ForceTimingSystem = true, // Absolutely no overrides for Grand Final
    Description = "Grand Final must be experienced in full detail"
};
```

## Interactive Season Screen Usage

### Setting Up UI Controls

```csharp
public class MySeasonScreen : MonoBehaviour
{
    [SerializeField] private TimingIntegratedSeasonScreenManager seasonScreen;
    [SerializeField] private TimingControlPanel timingControls;
    
    void Start()
    {
        // Configure season screen
        seasonScreen.enableInteractiveMatches = true;
        seasonScreen.showTimingControls = true;
        
        // Subscribe to timing events
        seasonScreen.OnTimingSystemChanged += HandleTimingChanged;
        seasonScreen.OnMatchSpeedChanged += HandleSpeedChanged;
        seasonScreen.OnMatchPausedChanged += HandlePauseChanged;
    }
    
    private void HandleTimingChanged(TimingSystemType newSystem)
    {
        Debug.Log($"Timing system changed to: {newSystem}");
        UpdateUI();
    }
}
```

### Runtime Timing Control

```csharp
// Switch timing system during season (if allowed)
bool success = seasonBoot.SwitchSeasonTimingSystem(TimingSystemType.Compressed);

// Control match speed for variable speed timing
seasonBoot.SetMatchSpeed(2.5f); // 2.5x speed

// Pause/resume current match
seasonBoot.PauseCurrentMatch();
seasonBoot.ResumeCurrentMatch();

// Get current timing status
var status = seasonBoot.GetTimingSystemStatus();
Debug.Log($"Active: {status.ActiveSystem}, Paused: {status.IsMatchPaused}");
```

## Advanced Features

### Custom Match Detection

```csharp
public class CustomSeasonScheduler : TimingIntegratedSeasonScheduler
{
    protected override MatchType DetermineMatchType(int round, int totalRounds, DateTime date)
    {
        // Custom logic for special matches
        if (date.Month == 4 && date.Day == 25)
            return MatchType.Special; // ANZAC Day
            
        if (IsChristmasMatch(date))
            return MatchType.Special;
            
        return base.DetermineMatchType(round, totalRounds, date);
    }
}
```

### Telemetry and Analytics

```csharp
// Access season timing statistics
var stats = seasonBoot.TimingStatistics;
Debug.Log($"Total matches: {stats.TotalMatches}");
Debug.Log($"Compressed timing used: {stats.GetTimingSystemUsagePercentage(TimingSystemType.Compressed)}%");

// Get detailed match telemetry
var telemetry = seasonBoot.GetSeasonTelemetry();
foreach (var snapshot in telemetry.GetFinalSnapshots())
{
    Debug.Log($"Match completed: {snapshot.HomePoints}-{snapshot.AwayPoints}");
}
```

### Dynamic Configuration Updates

```csharp
// Update configuration during season
public void UpdateSeasonTimingPreferences(PlayerExperienceLevel newLevel)
{
    var currentConfig = seasonBoot.seasonTimingConfig;
    
    // Create updated request template
    var baseRequest = new MatchTimingRequest
    {
        PlayerExperienceLevel = newLevel,
        // ... other properties
    };
    
    // Apply to future matches
    foreach (var fixture in GetRemainingFixtures())
    {
        var request = CreateRequestForFixture(fixture, baseRequest);
        var newConfig = currentConfig.GetConfigurationForMatch(request);
        fixture.TimingConfiguration = newConfig;
        fixture.PreferredTimingSystem = currentConfig.GetRecommendedTimingSystem(request);
    }
}
```

## Testing and Validation

### Example Test Script

Use the provided `TimingIntegrationExample` script to validate your setup:

```csharp
// In Unity Editor: attach TimingIntegrationExample to a GameObject
// Configure the timing config reference
// Use context menu: "Run Timing Configuration Test"
// Use context menu: "Test Season Generation"

// Or run the full example
var example = GetComponent<TimingIntegrationExample>();
example.runExampleOnStart = true; // Will run automatically
```

### Manual Testing Checklist

- [ ] **Configuration Loading**: SeasonTimingConfiguration loads properly
- [ ] **Match Type Detection**: Different match types get appropriate timing systems
- [ ] **Player Team Recognition**: Player team matches use preferred timing
- [ ] **Season Phase Progression**: Timing adapts as season progresses
- [ ] **Speed Controls**: Variable speed timing responds to speed changes
- [ ] **Pause Functionality**: Matches can be paused/resumed where supported
- [ ] **Statistics Tracking**: Timing usage statistics are collected correctly
- [ ] **UI Integration**: Timing controls appear and function in season screen

## Performance Considerations

### Optimization Tips

1. **Batch Match Processing**: Use `simulateAllMatches = true` for faster season simulation
2. **Telemetry Control**: Disable `enableSeasonTelemetry` for production if not needed
3. **Configuration Caching**: Cache timing configurations rather than recalculating
4. **UI Updates**: Limit UI refresh frequency during rapid match simulation

### Memory Management

```csharp
// Proper cleanup of timing resources
void OnDestroy()
{
    if (seasonBoot != null)
    {
        seasonBoot.OnMatchCompleted -= HandleMatchCompleted;
        seasonBoot.OnSeasonTimingUpdate -= HandleTimingUpdate;
    }
}
```

## Troubleshooting

### Common Issues

**Q: Timing system not switching during matches**
A: Check that `AllowRuntimeTimingSwitch` is true in your configuration and the match type doesn't force a specific timing system.

**Q: Speed controls not appearing**  
A: Ensure `showTimingControls = true` and the current timing system is `VariableSpeed`.

**Q: Matches simulating too fast/slow**
A: Verify the timing configuration's compression and speed settings match your preferences.

**Q: Player team matches not detected**
A: Set the `playerTeamId` in your `TimingSeasonGenerationOptions` during schedule generation.

### Debug Logging

Enable detailed logging for troubleshooting:

```csharp
// Enable in TimingIntegrationExample
example.enableDetailedLogging = true;

// Check Unity Console for detailed timing system logs
// Look for [TimingIntegratedSeasonBoot] messages
// Look for [TimingSeasonScreen] messages  
// Look for [TimingIntegrationExample] messages
```

## Future Enhancements

### Planned Features
- **Machine Learning Integration**: AI-driven timing recommendations based on player behavior
- **Multiplayer Timing Sync**: Synchronized timing in multiplayer seasons
- **Custom UI Themes**: Different UI styles for different timing systems
- **Advanced Analytics**: Detailed player engagement metrics and timing effectiveness

### Extension Points
- **Custom Timing Systems**: Implement `ITimingSystem` for specialized timing behavior
- **Event-Driven Configuration**: Dynamic timing changes based on season events
- **Platform-Specific Timing**: Different timing defaults for mobile vs desktop
- **Accessibility Features**: Timing accommodations for different accessibility needs

## Conclusion

The timing system integration provides a sophisticated, context-aware foundation for AFL Coach Sim's season simulation. It balances automation with player control, ensuring that each match uses the most appropriate timing system while maintaining the flexibility for players to override when desired.

The system is designed to be extensible and configurable, allowing for future enhancements while maintaining backward compatibility with existing season flows.