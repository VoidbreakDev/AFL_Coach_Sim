# AFL Season Calendar Management System

A comprehensive season/calendar management system for AFL-Coach-Sim that handles fixture generation, specialty matches, bye rounds, and season progression with proper AFL scheduling constraints.

## Overview

The Season Calendar Management System provides:

- **Complete Fixture Generation** - Generates full AFL season calendars with proper team distributions
- **Specialty Match Scheduling** - Handles traditional AFL matches (ANZAC Day, King's Birthday, Season Opener, etc.)
- **Intelligent Bye Round Management** - Distributes bye rounds across mid-season with proper team balancing
- **Season Progression Tracking** - Tracks current round, completed matches, and season state
- **AFL-Accurate Date Calculations** - Proper calculation of key AFL dates and scheduling constraints
- **Comprehensive Analytics** - Detailed reporting on fixture balance, travel, and season statistics

## Key Features

### 🗓️ **AFL Date Calculations**
- **Season Opener**: Second Thursday of March (Carlton vs Richmond)
- **ANZAC Day**: April 25th (Collingwood vs Essendon) 
- **Easter Monday**: Calculated Easter Monday (Geelong vs Hawthorn)
- **King's Birthday**: Second Monday of June (Collingwood vs Melbourne)
- **Grand Final**: Final weekend of September

### 🏆 **Specialty Matches**
- Season Opener (Carlton vs Richmond)
- ANZAC Day Match (Collingwood vs Essendon)
- King's Birthday Match (Collingwood vs Melbourne) 
- Easter Monday Match (Geelong vs Hawthorn)
- State Rivalry Matches (Showdowns, Derbies, Q-Clash)
- Traditional Rivalry Matches

### 📅 **Bye Round Management**
- 4-5 bye rounds distributed across mid-season (typically rounds 12-15)
- Each team gets exactly one bye round
- Balanced distribution (6 teams per bye round)
- Ensures no team has competitive advantage

### 🎯 **Season Progression**
- Track current round and season state
- Complete matches and advance rounds
- Validate season integrity
- Team upcoming match tracking
- Progress statistics and reporting

## Basic Usage

### Generating a Season Calendar

```csharp
// Create fixture generation engine
var fixtureEngine = new FixtureGenerationEngine(seed: 2024);

// Configure generation options
var options = new FixtureGenerationOptions
{
    TotalRounds = 24,
    ByeRoundStart = 12,
    ByeRoundEnd = 15,
    IncludeSeasonOpener = true,
    IncludeAnzacDay = true,
    IncludeKingsBirthday = true,
    IncludeEasterMonday = true,
    IncludeRivalryMatches = true
};

// Generate complete season
var seasonCalendar = fixtureEngine.GenerateSeasonCalendar(2024, options);

// Validate the generated calendar
var validation = seasonCalendar.Validate();
Console.WriteLine($"Season valid: {validation.IsValid}");
```

### Managing Season Progression

```csharp
// Create progression manager
var progressionManager = new SeasonProgressionManager(seasonCalendar);

// Get current round matches
var currentMatches = progressionManager.GetCurrentRoundMatches();

// Complete a match
var result = progressionManager.CompleteMatch(matchId, homeScore: 101, awayScore: 85);

// Advance to next round (when current round complete)
var advanceResult = progressionManager.AdvanceToNextRound();

// Get progress statistics
var stats = progressionManager.GetProgressStats();
Console.WriteLine(stats.GetProgressSummary());
```

### Working with Specialty Matches

```csharp
// Get all specialty matches
var specialtyMatches = seasonCalendar.SpecialtyMatches;

// Find specific specialty match
var anzacDay = specialtyMatches.FirstOrDefault(sm => sm.Type == SpecialtyMatchType.AnzacDay);

// Get specialty matches for a round
var round5SpecialtyMatches = seasonCalendar.GetRoundSpecialtyMatches(5);
```

### Team Schedule Management

```csharp
// Get all matches for a team
var carltonMatches = seasonCalendar.GetTeamMatches(TeamId.Carlton);

// Get team's next match
var nextMatch = seasonCalendar.GetNextMatch(TeamId.Carlton);

// Check if team has bye this round
var hasBye = seasonCalendar.HasBye(TeamId.Carlton, currentRound);

// Get team's upcoming match details
var upcomingMatch = progressionManager.GetTeamUpcomingMatch(TeamId.Carlton);
```

## Domain Model

### Core Entities

#### SeasonCalendar
The main aggregate root containing the complete season structure:

```csharp
public class SeasonCalendar
{
    public int Year { get; set; }
    public int TotalRounds { get; set; } = 24;
    public DateTime SeasonStart { get; set; }
    public DateTime SeasonEnd { get; set; }
    public List<SeasonRound> Rounds { get; set; }
    public List<SpecialtyMatch> SpecialtyMatches { get; set; }
    public ByeRoundConfiguration ByeConfiguration { get; set; }
    public SeasonState CurrentState { get; set; }
    public int CurrentRound { get; set; }
}
```

#### SeasonRound
Represents a single round in the season:

```csharp
public class SeasonRound
{
    public int RoundNumber { get; set; }
    public string RoundName { get; set; }
    public DateTime RoundStartDate { get; set; }
    public DateTime RoundEndDate { get; set; }
    public List<ScheduledMatch> Matches { get; set; }
    public List<TeamId> TeamsOnBye { get; set; }
    public RoundType RoundType { get; set; }
}
```

#### ScheduledMatch
A scheduled match with timing and context:

```csharp
public class ScheduledMatch
{
    public int MatchId { get; set; }
    public int RoundNumber { get; set; }
    public TeamId HomeTeam { get; set; }
    public TeamId AwayTeam { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public string Venue { get; set; }
    public MatchStatus Status { get; set; }
    public Weather Weather { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public List<string> MatchTags { get; set; }
}
```

#### SpecialtyMatch
Special matches with specific requirements:

```csharp
public class SpecialtyMatch
{
    public string Name { get; set; }
    public TeamId HomeTeam { get; set; }
    public TeamId AwayTeam { get; set; }
    public int RoundNumber { get; set; }
    public DateTime TargetDate { get; set; }
    public string Venue { get; set; }
    public SpecialtyMatchType Type { get; set; }
    public int Priority { get; set; }
}
```

### Value Objects

#### Enums
- `SeasonState`: NotStarted, InProgress, Finals, Complete, Cancelled
- `RoundType`: Regular, Finals, GrandFinal, PreSeason
- `MatchStatus`: Scheduled, InProgress, Completed, Postponed, Cancelled
- `SpecialtyMatchType`: SeasonOpener, AnzacDay, KingsBirthday, EasterMonday, etc.

## AFL Calendar Utilities

### Date Calculation Functions

```csharp
// Calculate key AFL dates
var seasonStart = AFLCalendarUtilities.GetSeasonOpenerDate(2024);
var anzacDay = AFLCalendarUtilities.GetAnzacDay(2024);
var easterMonday = AFLCalendarUtilities.GetEasterMonday(2024);
var kingsBirthday = AFLCalendarUtilities.GetKingsBirthday(2024);
var grandFinal = AFLCalendarUtilities.GetGrandFinalWeekend(2024);

// Get typical match times
var fridayNightTime = AFLCalendarUtilities.GetTypicalMatchTime(DayOfWeek.Friday);
var saturdayAfternoonTime = AFLCalendarUtilities.GetTypicalMatchTime(DayOfWeek.Saturday);

// Calculate duration
var seasonWeeks = AFLCalendarUtilities.GetWeeksBetween(seasonStart, grandFinal);
```

## Season Progression

### Progression Manager Features

The `SeasonProgressionManager` provides comprehensive season state management:

```csharp
var manager = new SeasonProgressionManager(seasonCalendar);

// Progress tracking
var stats = manager.GetProgressStats();
var currentRound = manager.GetCurrentRound();
var nextRound = manager.GetNextRound();

// Match management
var currentMatches = manager.GetCurrentRoundMatches();
var result = manager.CompleteMatch(matchId, homeScore, awayScore);

// Date-based queries
var upcomingMatches = manager.GetUpcomingMatches(days: 7);
var weekendMatches = manager.GetMatchesInDateRange(saturday, sunday);

// Team queries  
var teamUpcoming = manager.GetTeamUpcomingMatch(TeamId.Carlton);
var teamsOnBye = manager.GetTeamsOnByeThisRound();

// Validation
var validation = manager.ValidateSeasonIntegrity();
```

## Fixture Generation

### Generation Process

1. **Calculate Key Dates** - Determine all AFL special dates for the year
2. **Create Specialty Matches** - Schedule high-priority matches first
3. **Generate Bye Configuration** - Distribute bye rounds fairly
4. **Create Team Matchups** - Generate all required team pairings
5. **Distribute Across Rounds** - Place matches respecting constraints
6. **Schedule Times & Venues** - Assign specific times and venues
7. **Validate Result** - Ensure calendar integrity

### Generation Options

```csharp
var options = new FixtureGenerationOptions
{
    TotalRounds = 24,              // Season length
    ByeRoundStart = 12,            // First bye round
    ByeRoundEnd = 15,              // Last bye round
    IncludeSeasonOpener = true,    // Carlton vs Richmond opener
    IncludeAnzacDay = true,        // Collingwood vs Essendon
    IncludeKingsBirthday = true,   // Collingwood vs Melbourne  
    IncludeEasterMonday = true,    // Geelong vs Hawthorn
    IncludeRivalryMatches = true   // State rivalries
};
```

### Validation Rules

The system validates:
- Each team plays correct number of matches (23 = 24 rounds - 1 bye)
- All teams have exactly one bye round
- No team plays itself
- No scheduling conflicts (venue/time overlaps)
- Specialty matches are properly scheduled
- Round structure is consistent

## Examples

### Basic Season Generation
```csharp
// See SeasonCalendarExample.RunSeasonCalendarExample()
```

### Calendar Utilities
```csharp
// See SeasonCalendarExample.RunCalendarUtilitiesExample() 
```

### Fixture Balance Analysis
```csharp
// See SeasonCalendarExample.RunFixtureBalanceExample()
```

## Integration

### With Match Engine
The season calendar integrates with the match engine by providing:
- Scheduled matches for simulation
- Match context (round, specialty match tags)
- Team availability (bye rounds)
- Progress tracking

### With Training System
Season progression affects training by:
- Providing match schedules for training planning
- Indicating bye rounds for intensive training
- Tracking season workload

### With Injury System  
Season calendar provides match context for:
- Injury occurrence during specific matches
- Seasonal injury tracking
- Recovery time planning between matches

## Testing

Comprehensive test coverage includes:

### AFL Calendar Tests
- Date calculation accuracy
- Special date handling
- Time zone considerations

### Fixture Generation Tests
- Season structure validation
- Specialty match placement
- Bye round distribution
- Team match balance

### Season Progression Tests
- Match completion handling
- Round advancement logic
- State management
- Progress tracking

### Integration Tests
- Full season generation
- Cross-system compatibility
- Performance validation

Run tests with:
```bash
Unity -runTests -testPlatform editmode -assemblyNames "AFLCoachSim.Core.Tests" -testCategory "Season"
```

## Performance Considerations

- **Fixture Generation**: Optimized algorithms for large team sets
- **Memory Usage**: Efficient data structures for full season storage
- **Query Performance**: Indexed access patterns for common queries
- **Validation**: Fast validation algorithms with early termination

## Future Enhancements

Planned improvements:

1. **Finals Series Integration** - Automatic finals bracket generation
2. **Multi-Season Management** - Handle multiple seasons and transitions
3. **Advanced Analytics** - Travel distance calculations, fixture fairness metrics
4. **Venue Optimization** - Smart venue allocation based on capacity and location
5. **Weather Integration** - Historical weather data for realistic conditions
6. **Broadcasting Schedule** - TV/streaming schedule integration
7. **Dynamic Rescheduling** - Handle match postponements and rescheduling

## API Reference

See the individual class documentation for detailed API reference:

- `FixtureGenerationEngine` - Core fixture generation
- `SeasonProgressionManager` - Season state management  
- `AFLCalendarUtilities` - Date calculations
- `SeasonCalendar` - Main domain aggregate
- `SpecialtyMatch` - Special match handling
- `ByeRoundConfiguration` - Bye round management