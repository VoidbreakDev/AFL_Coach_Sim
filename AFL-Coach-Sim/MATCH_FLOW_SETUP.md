# Match Flow Scene Setup Guide

This guide explains how to set up the **MatchFlow** scene in Unity for the 0.1.0.0 beta release.

## Overview

The Match Flow system provides a complete match experience with three screens:
1. **Pre-Match** - Team lineups and comparison
2. **Simulation** - Live match progress with commentary
3. **Post-Match** - Results, stats, and highlights

## Scene Setup

### 1. Create the Scene

1. Create a new scene: `Assets/Scenes/MatchFlow.unity`
2. Add it to Build Settings (File → Build Settings → Add Open Scenes)

### 2. Scene Structure

```
MatchFlow Scene
├── MatchFlowManager (GameObject with MatchFlowManager component)
├── Main Canvas
│   ├── PreMatchScreen (Panel)
│   │   ├── MatchPreviewUI component
│   │   ├── Match Info Section
│   │   ├── Team Comparison Section
│   │   ├── Lineups Section (2 columns)
│   │   └── Start Match Button
│   ├── SimulationScreen (Panel - initially inactive)
│   │   ├── MatchSimulationUI component
│   │   ├── Score Display
│   │   ├── Progress Bar
│   │   └── Commentary Feed
│   └── PostMatchScreen (Panel - initially inactive)
│       ├── MatchResultsUI component
│       ├── Final Score Display
│       ├── Quarter-by-Quarter Scores
│       ├── Statistics
│       ├── Highlights
│       └── Continue Button
└── EventSystem
```

### 3. MatchFlowManager Setup

Create an empty GameObject called "MatchFlowManager" with the `MatchFlowManager` component:

**Inspector Settings:**
- **UI Screens:**
  - Pre Match Screen: Assign PreMatchScreen panel
  - Simulation Screen: Assign SimulationScreen panel
  - Post Match Screen: Assign PostMatchScreen panel
- **UI Components:**
  - Pre Match UI: Assign MatchPreviewUI component
  - Simulation UI: Assign MatchSimulationUI component
  - Results UI: Assign MatchResultsUI component

### 4. Pre-Match Screen Layout

#### Match Info Header
```
- Round Text (TMP)
- Venue Text (TMP)
- Date Text (TMP)
```

#### Team Comparison
```
Left Column:               Center:                Right Column:
- Home Team Name (TMP)     - VS Text             - Away Team Name (TMP)
- Home Rating (TMP)        - Comparison Slider   - Away Rating (TMP)
- Home Form (TMP)                                - Away Form (TMP)
```

#### Lineups
```
Two ScrollView panels side-by-side:
- Home Lineup Container (Vertical Layout Group)
- Away Lineup Container (Vertical Layout Group)
```

#### Controls
```
- Start Match Button (Button)
- Back Button (Button)
```

### 5. Simulation Screen Layout

#### Score Display
```
Top Section:
- Home Team Name (TMP)  |  Score (TMP)  vs  Score (TMP)  |  Away Team Name (TMP)
```

#### Progress
```
- Quarter Text (TMP) - "Q1", "Q2", etc.
- Progress Bar (Slider) - 0 to 1
- Progress Text (TMP) - Time remaining
```

#### Commentary Feed
```
- Current Commentary (TMP) - Large text at top
- Commentary Feed Container (ScrollView)
  - Vertical Layout Group
  - Spawns commentary entries dynamically
```

### 6. Post-Match Screen Layout

#### Result Header
```
- Result Header Text (TMP) - "VICTORY!" or "DEFEAT"
```

#### Final Scores
```
Home Team Name | Home Score  vs  Away Score | Away Team Name
Margin Text - "Team X by Y points"
```

#### Quarter Scores
```
Table format:
       Q1  Q2  Q3  Q4  Final
Home:  XX  XX  XX  XX   XXX
Away:  XX  XX  XX  XX   XXX
```

#### Statistics
```
Disposals:  XXX  |  XXX
Marks:      XXX  |  XXX
Tackles:    XXX  |  XXX
```

#### Highlights
```
ScrollView with commentary highlights:
- "Q1 - Strong start..."
- "Q2 - Momentum swings..."
- etc.
```

#### Controls
```
- Continue Button (Button) - Returns to season
- View Stats Button (Button) - Optional future feature
```

### 7. Prefabs Needed

Create these prefabs in `Assets/Prefabs/UI/`:

#### PlayerLineupEntry.prefab
```
- Panel
  - Text (TMP) - Player name and rating
  - Layout Element (min height: 30)
```

#### CommentaryEntry.prefab
```
- Panel
  - Text (TMP) - Commentary text
  - Layout Element (min height: 40)
```

#### HighlightEntry.prefab
```
- Panel
  - Text (TMP) - Highlight text
  - Layout Element (min height: 50)
```

### 8. Component References

#### MatchPreviewUI Component
Assign in inspector:
- **Match Info:** roundText, venueText, dateText
- **Teams:** homeTeamName, awayTeamName
- **Comparison:** homeRating, awayRating, comparisonSlider, homeForm, awayForm
- **Lineups:** homeLineupContainer, awayLineupContainer, playerLineupEntryPrefab
- **Controls:** startMatchButton, backButton

#### MatchSimulationUI Component
Assign in inspector:
- **Team Display:** homeTeamName, awayTeamName, homeScore, awayScore
- **Progress:** quarterText, progressBar, progressText
- **Commentary:** commentaryText, commentaryFeedContainer, commentaryEntryPrefab
- **Animation:** simulationSpeed (default: 2)

#### MatchResultsUI Component
Assign in inspector:
- **Result:** resultHeaderText, homeTeamName, awayTeamName, homeScore, awayScore, marginText
- **Quarter Scores:** homeQ1-4, awayQ1-4
- **Statistics:** homeDisposals, awayDisposals, homeMarks, awayMarks, homeTackles, awayTackles
- **Highlights:** highlightsContainer, highlightEntryPrefab
- **Controls:** continueButton, viewStatsButton

## Integration

### How Other Scenes Launch Match Flow

From any scene, use this pattern:

```csharp
private void LaunchMatchFlow(Match match)
{
    // Store match data
    PlayerPrefs.SetString("CurrentMatchData", JsonUtility.ToJson(match));
    PlayerPrefs.SetString("CurrentMatchPlayerTeam", playerTeamId);
    PlayerPrefs.SetString("MatchFlowReturnScene", SceneManager.GetActiveScene().name);
    PlayerPrefs.Save();

    // Load scene
    SceneManager.LoadScene("MatchFlow");
}
```

### Data Flow

1. **Season/Dashboard** stores match data in PlayerPrefs
2. **MatchFlow** loads match data on Start()
3. **Pre-Match** displays lineups and comparison
4. **Simulation** runs match with animated progress
5. **Post-Match** shows results
6. **Continue** returns to origin scene

## Styling Recommendations

### Colors
- **Background:** Dark gray (#2C2C2C)
- **Panels:** Medium gray (#3C3C3C)
- **Text:** White (#FFFFFF)
- **Accent (Victory):** Green (#4CAF50)
- **Accent (Defeat):** Red (#F44336)
- **Progress Bar:** Team colors

### Fonts
- **Headers:** Bold, 24-32pt
- **Body Text:** Regular, 14-18pt
- **Scores:** Bold, 36-48pt

### Layout
- Use **Content Size Fitter** on text elements
- Use **Layout Groups** for organized sections
- **Screen Resolution:** Design for 1920x1080, scale with Canvas Scaler

## Testing

### Test Checklist
- [ ] Scene loads without errors
- [ ] Pre-match screen displays team info
- [ ] Lineups populate correctly
- [ ] Start button triggers simulation
- [ ] Simulation shows animated progress
- [ ] Commentary updates during simulation
- [ ] Post-match screen shows correct result
- [ ] Continue button returns to origin scene
- [ ] Data persists correctly

### Test Scenarios
1. **Player team wins** - Should show "VICTORY!"
2. **Player team loses** - Should show "DEFEAT"
3. **Close match** - Scores should progress realistically
4. **Blowout** - Should still be engaging

## Next Steps

After setting up the MatchFlow scene:
1. Test with SeasonScreen integration
2. Test with TeamMainScreen integration
3. Add sound effects (crowd noise, commentary audio)
4. Polish animations and transitions
5. Optimize for mobile if needed

## Troubleshooting

**Scene won't load:**
- Check scene is in Build Settings
- Verify scene name is exactly "MatchFlow"

**Data not loading:**
- Verify PlayerPrefs are being set before scene load
- Check JSON serialization of Match object

**UI not displaying:**
- Check all component references are assigned
- Verify panels are children of Canvas
- Check active states of panels

**Match won't simulate:**
- Verify MatchSimulator is working
- Check team data is loading correctly
- Review console for errors
