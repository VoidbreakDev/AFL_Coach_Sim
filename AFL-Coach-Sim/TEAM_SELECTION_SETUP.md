# Team Selection UI Setup Guide

Complete guide for setting up the team selection interface in Unity for roster management.

## Overview

The Team Selection system allows players to:
- Select their starting 22 players
- Filter by position (Defenders, Midfielders, Forwards, Rucks)
- Search players by name
- Auto-select best 22
- Validate lineup meets requirements (6 def, 6 mid, 6 fwd, 2 ruck)
- Save and load lineups

## Components Created

### Scripts
1. `TeamSelectionManager.cs` - Main controller
2. `PlayerSelectionCard.cs` - Individual player card
3. `LineupSlot.cs` - Selected player display slot
4. `TeamDataHelper.cs` - Utility methods (already created)

---

## Scene Setup Options

### Option 1: Add to RosterScreen (Recommended)

Enhance the existing `RosterScreen.unity` scene with team selection.

### Option 2: Create New TeamSelectionScene

Create a dedicated scene for more detailed lineup management.

---

## UI Structure

```
TeamSelectionScene/RosterScreen
├── Canvas
│   ├── Header
│   │   ├── Title: "Team Selection"
│   │   └── Team Info
│   ├── Main Content (Horizontal Layout)
│   │   ├── Left Panel - Player List
│   │   │   ├── Filters Section
│   │   │   │   ├── Position Dropdown
│   │   │   │   ├── Search Field
│   │   │   │   └── Show Selected Toggle
│   │   │   ├── Player Cards Container (ScrollView)
│   │   │   │   └── [PlayerSelectionCard prefabs spawned here]
│   │   │   └── Actions
│   │   │       ├── Auto Best 22 Button
│   │   │       └── Clear Button
│   │   └── Right Panel - Lineup Display
│   │       ├── Position Requirements
│   │       │   ├── Defenders: X/6
│   │       │   ├── Midfielders: X/6
│   │       │   ├── Forwards: X/6
│   │       │   └── Rucks: X/2
│   │       ├── Selection Info
│   │       │   ├── Count: X/22
│   │       │   ├── Validation Status
│   │       │   └── Average Rating
│   │       ├── Lineup Display Container (ScrollView)
│   │       │   └── [LineupSlot prefabs spawned here]
│   │       └── Confirm Button
│   └── TeamSelectionManager (GameObject with component)
└── EventSystem
```

---

## Step-by-Step Setup

### 1. Create Prefabs

#### PlayerSelectionCard.prefab

```
PlayerSelectionCard
├── Image (Background)
│   ├── Button component
│   └── Layout Element (min height: 80)
├── Position Color Bar (Image - left edge, 5px wide)
├── Content (Horizontal Layout Group)
│   ├── Player Info (Vertical Layout Group)
│   │   ├── Name (TextMeshPro - bold, 16pt)
│   │   ├── Position (TextMeshPro - 12pt)
│   │   └── Age (TextMeshPro - 10pt, gray)
│   └── Rating (TextMeshPro - 24pt, right-aligned)
└── Selection Indicator (Image - checkmark icon, top-right corner)
```

**Component References (PlayerSelectionCard.cs):**
- playerNameText → Name TMP
- positionText → Position TMP
- ratingText → Rating TMP
- ageText → Age TMP
- positionColorBar → Position Color Bar Image
- selectionIndicator → Selection Indicator Image
- selectButton → Button component on root

---

#### LineupSlot.prefab

```
LineupSlot
├── Image (Background with position color tint)
│   └── Layout Element (min height: 50)
├── Position Color Bar (Image - left edge)
├── Content (Horizontal Layout Group)
│   ├── Name (TextMeshPro - 14pt)
│   └── Rating (TextMeshPro - 16pt, bold)
```

**Component References (LineupSlot.cs):**
- playerNameText → Name TMP
- ratingText → Rating TMP
- positionColorBar → Position Color Bar Image
- backgroundImage → Background Image

---

### 2. Create Main UI Layout

#### Header Section
```
- Title (TMP): "Select Your Starting 22"
- Subtitle (TMP): "Choose the best lineup for your next match"
```

#### Left Panel - Player List
```
Panel (400-500px width)
├── Filters (Vertical Layout Group, padding: 10)
│   ├── Position Filter (TMP_Dropdown)
│   ├── Search Field (TMP_InputField with placeholder "Search players...")
│   └── Show Selected Toggle (Toggle with label)
├── Player List (ScrollView)
│   ├── Viewport
│   │   └── Content (Vertical Layout Group, spacing: 5)
│   │       └── [Cards spawn here]
└── Actions (Horizontal Layout Group)
    ├── Auto Best 22 Button
    └── Clear Button
```

#### Right Panel - Lineup Display
```
Panel (flexible width, min 300px)
├── Position Requirements (Grid Layout 2x2)
│   ├── Defenders Counter (TMP)
│   ├── Midfielders Counter (TMP)
│   ├── Forwards Counter (TMP)
│   └── Rucks Counter (TMP)
├── Selection Info (Vertical Layout Group)
│   ├── Selection Count (TMP) - "X/22 selected"
│   ├── Validation Status (TMP) - "✓ Valid" or errors
│   └── Average Rating (TMP) - "Avg Rating: XX.X"
├── Lineup Display (ScrollView)
│   ├── Viewport
│   │   └── Content (Vertical Layout Group, spacing: 5)
│   │       └── [Lineup slots spawn here grouped by position]
└── Confirm Button (Large, green when valid)
```

---

### 3. TeamSelectionManager Setup

Create empty GameObject: "TeamSelectionManager"

**Inspector References:**
```
Team Data:
- (teamId loaded from PlayerPrefs automatically)

UI References:
- Player List Container: ScrollView → Viewport → Content (left panel)
- Lineup Display Container: ScrollView → Viewport → Content (right panel)
- Player Card Prefab: PlayerSelectionCard prefab
- Lineup Slot Prefab: LineupSlot prefab

Filters:
- Position Filter: TMP_Dropdown for position filtering
- Search Field: TMP_InputField for search
- Show Selected Only: Toggle component

Info Display:
- Selection Count Text: "X/22 selected" TMP
- Validation Text: Status message TMP
- Lineup Rating Text: Average rating TMP
- Confirm Button: Confirm button
- Auto Best 22 Button: Auto-select button
- Clear Button: Clear selection button

Position Requirements Display:
- Defenders Count Text: "X/6" TMP
- Midfielders Count Text: "X/6" TMP
- Forwards Count Text: "X/6" TMP
- Rucks Count Text: "X/2" TMP
```

---

## Visual Design

### Color Scheme

**Position Colors** (matches TeamDataHelper):
- Defenders: Blue (51, 102, 204)
- Midfielders: Green (51, 179, 77)
- Forwards: Red (230, 77, 51)
- Rucks: Yellow (230, 204, 51)

**UI Colors**:
- Background: Dark gray (#2C2C2C)
- Panels: Medium gray (#3C3C3C)
- Selected: Green tint (#4CAF50)
- Unselected: Dark gray (#323232)
- Valid: Green (#4CAF50)
- Invalid: Red (#F44336)

### Typography
- **Headers:** Bold, 20-24pt
- **Player Names:** Bold, 14-16pt
- **Stats/Numbers:** Bold, 16-24pt
- **Labels:** Regular, 12-14pt

---

## Position Requirements

The system validates:
- **6 Defenders** minimum
- **6 Midfielders** minimum
- **6 Forwards** minimum
- **2 Rucks** minimum
- **22 Total** players

Remaining 2 slots can be any position (bench).

---

## Features

### Auto Best 22
Automatically selects top 22 players by rating.

### Position Filter
- All Positions
- Defenders only
- Midfielders only
- Forwards only
- Rucks only

### Search
Type-ahead filtering by player name.

### Show Selected
Toggle to view only selected players.

### Validation
Real-time validation with colored indicators:
- ✓ Green = Valid
- ✗ Red = Invalid (shows what's missing)

### Persistence
Lineup saves automatically on confirmation and loads on return.

---

## Testing Checklist

- [ ] Scene loads team data correctly
- [ ] Player cards display properly
- [ ] Selection/deselection works
- [ ] Position filter functions
- [ ] Search filters players
- [ ] Show selected toggle works
- [ ] Position counts update
- [ ] Validation shows correct status
- [ ] Auto Best 22 button works
- [ ] Clear button prompts confirmation
- [ ] Confirm button only enabled when valid
- [ ] Lineup persists after save
- [ ] Lineup loads on return

---

## Integration with Other Systems

### From RosterScreen
```csharp
// Add button to open team selection
public void OnSelectTeamButton()
{
    SceneManager.LoadScene("TeamSelection");
}
```

### To Match Flow
```csharp
// TeamSelectionManager can connect to match flow
void Start()
{
    var manager = GetComponent<TeamSelectionManager>();
    manager.OnLineupConfirmed = (lineup) =>
    {
        // Store lineup for match
        // Return to previous scene or proceed to match
    };
}
```

### Pre-Match Integration
The MatchPreviewUI can pull from saved lineup:
```csharp
string savedLineup = PlayerPrefs.GetString($"Lineup_{teamId}", "");
// Load and display the 22 selected players
```

---

## Advanced Features (Future)

### Captain Selection
Add captain designation to lineup.

### Formation Display
Visual football field layout showing player positions.

### Player Stats on Hover
Detailed stats popup when hovering cards.

### Drag & Drop
Drag players between lists instead of clicking.

### Tactics Integration
Link with TeamTactics for formation selection.

---

## Troubleshooting

**Cards not spawning:**
- Check prefab references assigned
- Verify container has Content Size Fitter or Layout Group
- Check team data is loading

**Validation always failing:**
- Verify position categories in TeamDataHelper match your PlayerRole enum
- Check minimum requirements (6, 6, 6, 2)

**Selection not persisting:**
- Ensure PlayerPrefs.Save() is called
- Check teamId is consistent
- Verify LoadSavedLineup is being called

**UI not updating:**
- Call RefreshPlayerList() after changes
- Call UpdateLineupDisplay() after selection changes
- Check text component references are assigned

---

## Next Steps

1. Create prefabs following the structure above
2. Build the UI layout in scene
3. Create TeamSelectionManager GameObject
4. Assign all references in inspector
5. Test with actual team data
6. Integrate with RosterScreen or create new scene
7. Connect to Match Flow system

The system is fully functional once UI is set up!
