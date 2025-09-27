# Match Injury System

The AFL-Coach-Sim match injury system provides realistic injury simulation during match play with detailed tracking, contextual descriptions, and comprehensive analytics.

## Overview

The modern injury system integrates seamlessly with match simulation to provide:

- **Realistic AFL-style injuries** - Phase-based injury types (muscle strains in open play, joint injuries in contests, etc.)
- **Detailed injury tracking** - Specific body parts, injury descriptions, and recovery profiles
- **Pre-existing injury management** - Players carry injuries into matches with appropriate performance impacts
- **Contextual injury reporting** - Match phase, weather, and game situation context for injuries
- **Comprehensive analytics** - Detailed reports on injury types, severities, and impact

## Basic Usage

```csharp
// Create injury manager with your chosen persistence layer
var repository = new YourInjuryRepository();
var service = new InjuryService(repository);
var injuryManager = new InjuryManager(service);

// Run match simulation with injury system
var result = MatchEngine.PlayMatch(
    round: 1,
    homeId: TeamId.Adelaide,
    awayId: TeamId.Brisbane,
    teams: teams,
    injuryManager: injuryManager, // Required parameter
    rosters: rosters,
    tactics: tactics,
    weather: Weather.Clear,
    quarterSeconds: 20 * 60,
    rng: rng,
    tuning: tuning
);
```

## Key Components

### InjuryModel
The core injury simulation model that:
- Integrates with the unified injury management system
- Processes phase-based injury risks during match ticks
- Applies realistic injury effects to player performance
- Tracks injury states throughout the match

### MatchInjuryContextProvider
Provides rich contextual information for injury descriptions:
- Current match phase and quarter
- Weather conditions and venue
- Match score and recent events
- Player-specific context

### MatchTuning Integration
Enhanced tuning parameters for detailed injury configuration:
- Phase-specific injury type probabilities
- Age-based risk modifiers
- Performance impact scaling by severity
- Recovery time modifiers

## Injury Types and Effects

The system models realistic AFL injury types:

### Muscle Injuries
- **Common in**: Open play (running), contests (contact)
- **Body parts**: Hamstring, calf, quad, groin
- **Effects**: Reduced movement and agility

### Joint Injuries
- **Common in**: All phases, especially contests
- **Body parts**: Knee, ankle, shoulder
- **Effects**: Overall mobility reduction

### Ligament Injuries
- **Common in**: High-impact contests
- **Body parts**: Knee (ACL/MCL), ankle
- **Effects**: Significant performance impact, often season-ending

### Concussion
- **Common in**: Aerial contests, ground ball contests
- **Effects**: Immediate removal from match regardless of severity
- **Special handling**: Concussion protocol compliance

### Bone and Skin Injuries
- Various fractures, cuts, and abrasions
- Context-appropriate body part selection

## Performance Impact

Injuries affect player performance in realistic ways:
- **Niggle**: 5% performance reduction
- **Minor**: 15% performance reduction
- **Moderate**: 30% performance reduction  
- **Major**: 50% performance reduction
- **Severe**: 70% performance reduction

## Match Return Times

Injuries have appropriate return-to-field times:
- **Niggle**: Brief assessment (30 seconds)
- **Minor**: Medical attention (3 minutes)
- **Moderate/Major/Severe**: Out for match
- **Concussion**: Immediate and permanent removal regardless of severity

## Analytics and Reporting

The system provides detailed match injury analytics:

```csharp
// Get analytics after match
var analytics = injuryModel.GetMatchAnalytics();

Console.WriteLine($"Total new injuries: {analytics.TotalNewInjuries}");
Console.WriteLine($"Injury rate: {analytics.InjuryRate:P1}");

// Breakdown by type and severity
foreach (var type in analytics.NewInjuriesByType.GroupBy(t => t))
{
    Console.WriteLine($"{type.Key}: {type.Count()}");
}
```

## Weather and Environmental Effects

Weather conditions affect injury risk:
- **Wet conditions**: +15% injury risk (slips, falls)
- **Windy conditions**: +5% injury risk (balance issues)
- **Hot conditions**: +10% injury risk (fatigue, dehydration)
- **Cold conditions**: +8% injury risk (muscle stiffness)

## Integration with Training System

The match injury system seamlessly integrates with the training system:
- Pre-existing injuries from training affect match performance
- Match injuries are tracked through recovery
- Injury history influences future risk calculations

## Example Usage

See `MatchInjuryExample.cs` for a complete working example that demonstrates:
- Setting up the injury management system
- Creating teams and players with pre-existing injuries
- Running matches with injury simulation
- Generating detailed injury reports

## Migration from Legacy System

The legacy injury system has been completely replaced. The new system:
- ✅ **Requires** an `InjuryManager` parameter in `MatchEngine.PlayMatch()`
- ✅ **Provides** much more detailed and realistic injury simulation
- ✅ **Integrates** with the unified injury management system
- ✅ **Maintains** similar performance characteristics
- ✅ **Offers** comprehensive reporting and analytics

## Configuration

Injury behavior can be tuned through `MatchTuning` parameters:
- Base injury risk rates
- Phase-specific multipliers
- Age-based risk adjustments
- Performance impact scaling
- Recovery modifiers

See `MatchTuning.cs` for all available parameters.