# Unity Setup Checklist - Beta 0.1.0.0

Quick reference for setting up all systems in Unity Editor.

---

## ✅ Pre-Setup

- [ ] Open Unity project in Unity 6000.2.7f2
- [ ] Pull latest code from repository
- [ ] Verify all C# scripts compile without errors
- [ ] Backup project before making changes

---

## 📦 Prefabs to Create

### Priority 1: Match Flow Prefabs

**Location:** `Assets/Prefabs/UI/MatchFlow/`

- [ ] **PlayerLineupEntry.prefab**
  - Panel with TextMeshPro for name/rating
  - Min height: 30px
  - Reference in MatchPreviewUI

- [ ] **CommentaryEntry.prefab**
  - Panel with TextMeshPro for commentary text
  - Min height: 40px
  - Reference in MatchSimulationUI

- [ ] **HighlightEntry.prefab**
  - Panel with TextMeshPro for highlight text
  - Min height: 50px
  - Reference in MatchResultsUI

### Priority 2: Team Selection Prefabs

**Location:** `Assets/Prefabs/UI/TeamSelection/`

- [ ] **PlayerSelectionCard.prefab**
  - Background Image with Button
  - Position color bar (5px left edge)
  - Name, Position, Age, Rating texts
  - Selection indicator (checkmark)
  - Min height: 80px

- [ ] **LineupSlot.prefab**
  - Background Image with color tint
  - Position color bar
  - Name and Rating texts
  - Min height: 50px

### Optional: Helper Prefabs

**Location:** `Assets/Prefabs/UI/Helpers/`

- [ ] **ToastNotification.prefab**
  - Panel with TMP for message
  - CanvasGroup for fading
  
- [ ] **ConfirmationDialog.prefab**
  - Modal panel
  - Message text, Confirm/Cancel buttons

---

## 🎬 Scenes to Create/Modify

### 1. MatchFlow.unity (NEW)

**Estimated Time: 3-4 hours**

- [ ] Create new scene: `Assets/Scenes/MatchFlow.unity`
- [ ] Add to Build Settings (File → Build Settings → Add Open Scenes)
- [ ] Create Canvas with CanvasScaler (1920x1080)
- [ ] Create EventSystem

#### MatchFlowManager GameObject
- [ ] Create empty GameObject: "MatchFlowManager"
- [ ] Add MatchFlowManager component
- [ ] Create 3 panel GameObjects: PreMatchScreen, SimulationScreen, PostMatchScreen

#### PreMatchScreen Panel
- [ ] Round/Venue/Date text fields
- [ ] Home/Away team names and ratings
- [ ] Comparison slider
- [ ] Two ScrollViews for lineups (Home/Away)
- [ ] Start Match button
- [ ] Add MatchPreviewUI component
- [ ] **Assign all references in MatchPreviewUI inspector**

#### SimulationScreen Panel
- [ ] Home/Away team names and scores
- [ ] Quarter text (Q1, Q2, etc.)
- [ ] Progress bar (Slider 0-1)
- [ ] Progress text
- [ ] Current commentary text (large)
- [ ] Commentary feed ScrollView
- [ ] Add MatchSimulationUI component
- [ ] **Assign all references in MatchSimulationUI inspector**

#### PostMatchScreen Panel
- [ ] Result header (VICTORY/DEFEAT)
- [ ] Home/Away names and final scores
- [ ] Margin text
- [ ] Quarter scores table (8 text fields)
- [ ] Statistics (6 text fields: disposals, marks, tackles x2)
- [ ] Highlights ScrollView
- [ ] Continue button
- [ ] Add MatchResultsUI component
- [ ] **Assign all references in MatchResultsUI inspector**

#### MatchFlowManager References
- [ ] Assign preMatchScreen → PreMatchScreen panel
- [ ] Assign simulationScreen → SimulationScreen panel
- [ ] Assign postMatchScreen → PostMatchScreen panel
- [ ] Assign preMatchUI → MatchPreviewUI component
- [ ] Assign simulationUI → MatchSimulationUI component
- [ ] Assign resultsUI → MatchResultsUI component

**Test:** Run scene, verify no null references

---

### 2. SeasonScreen.unity (MODIFY)

**Estimated Time: 1-2 hours**

- [ ] Open `Assets/Scenes/SeasonScreen.unity`

#### Add Season Progression UI
- [ ] Create panel: "Season Progression Panel" (top of screen)
- [ ] Add Round Tracker text: "Round X/23"
- [ ] Add Progress bar (Slider 0-1)
- [ ] Add Progress text: "X/23 rounds complete"
- [ ] Add Next Match Info text
- [ ] Add Season Progress text
- [ ] Add "Next Match" button
- [ ] Add "Advance Round" button

#### Add SeasonProgressionController
- [ ] Create empty GameObject: "SeasonProgressionController"
- [ ] Add SeasonProgressionController component
- [ ] **Assign UI references:**
  - nextMatchButton → Next Match button
  - advanceRoundButton → Advance Round button
  - roundTracker → Round tracker text
  - nextMatchInfo → Next match info text
  - seasonProgress → Season progress text
  - progressBar → Progress slider
  - progressText → Progress text

#### Integrate with SeasonScreenManager
- [ ] Find SeasonScreenManager GameObject
- [ ] Add reference to progressionController
- [ ] Wire OnPlayMatch, OnRoundAdvanced, OnSeasonComplete events in code (already done)

**Test:** Load scene, verify progression UI updates

---

### 3. RosterScreen.unity or TeamSelection.unity (NEW/MODIFY)

**Estimated Time: 3-4 hours**

**Option A: Enhance RosterScreen**
- [ ] Open `Assets/Scenes/RosterScreen.unity`
- [ ] Add team selection UI alongside existing roster view

**Option B: Create New Scene**
- [ ] Create new scene: `Assets/Scenes/TeamSelection.unity`
- [ ] Add to Build Settings

#### Create Main Layout
- [ ] Create Canvas with CanvasScaler
- [ ] Add EventSystem

#### Left Panel - Player List
- [ ] Create Panel (400-500px width)
- [ ] Add Position Filter (TMP_Dropdown)
- [ ] Add Search Field (TMP_InputField)
- [ ] Add Show Selected Toggle
- [ ] Create ScrollView for player cards
  - [ ] Add Vertical Layout Group to Content
- [ ] Add "Auto Best 22" button
- [ ] Add "Clear" button

#### Right Panel - Lineup Display
- [ ] Create Panel (flexible width)
- [ ] Add Position Requirements section (Grid 2x2)
  - [ ] Defenders text: "X/6"
  - [ ] Midfielders text: "X/6"
  - [ ] Forwards text: "X/6"
  - [ ] Rucks text: "X/2"
- [ ] Add Selection Info section
  - [ ] Selection count: "X/22"
  - [ ] Validation text
  - [ ] Average rating text
- [ ] Create ScrollView for lineup display
  - [ ] Add Vertical Layout Group to Content
- [ ] Add large "Confirm" button

#### Add TeamSelectionManager
- [ ] Create empty GameObject: "TeamSelectionManager"
- [ ] Add TeamSelectionManager component
- [ ] **Assign ALL references in inspector** (see TEAM_SELECTION_SETUP.md)
  - Player List Container
  - Lineup Display Container
  - Player Card Prefab
  - Lineup Slot Prefab
  - All filter components
  - All info display texts
  - All buttons
  - All position count texts

**Test:** Load scene, click Auto Best 22, verify lineup populates

---

## 🔗 Scene Integration

### Update Build Settings
- [ ] File → Build Settings
- [ ] Verify all scenes are listed in order:
  1. MainMenu
  2. CreateCoach
  3. RosterScreen
  4. SeasonScreen
  5. **MatchFlow** (NEW)
  6. TeamSelection (if created)

### Test Scene Transitions
- [ ] MainMenu → CreateCoach → RosterScreen
- [ ] RosterScreen → SeasonScreen
- [ ] SeasonScreen → MatchFlow → SeasonScreen
- [ ] SeasonScreen → TeamSelection (if applicable)

---

## 🎨 Optional: Helper Systems

**Estimated Time: 1 hour**

### UIFeedbackHelper Setup
- [ ] Create GameObject: "UIFeedbackHelper" in MainMenu scene
- [ ] Add UIFeedbackHelper component
- [ ] Create ToastNotification prefab
- [ ] Create ConfirmationDialog prefab
- [ ] Assign prefab references
- [ ] Add DontDestroyOnLoad in Awake (already in code)

**Test:** Call UIFeedbackHelper.ShowToast("Test") from console

---

## 🧪 Testing Checklist

### Match Flow Tests
- [ ] SeasonScreen "Next Match" button launches MatchFlow
- [ ] Pre-match shows team lineups
- [ ] Start Match button triggers simulation
- [ ] Simulation shows animated scores
- [ ] Post-match shows results
- [ ] Continue button returns to SeasonScreen
- [ ] Match result persists in ladder

### Season Progression Tests
- [ ] Round tracker shows correct round
- [ ] Next Match button appears when match available
- [ ] Advance Round button appears when round complete
- [ ] Progress bar updates correctly
- [ ] Season completion detected

### Team Selection Tests
- [ ] Player cards display correctly
- [ ] Selection/deselection works
- [ ] Position filter works
- [ ] Search filters players
- [ ] Show selected toggle works
- [ ] Auto Best 22 selects 22 players
- [ ] Clear prompts confirmation
- [ ] Validation shows correct status
- [ ] Confirm only enabled when valid
- [ ] Lineup saves and loads

### Integration Tests
- [ ] Complete season playthrough
- [ ] Multiple matches in sequence
- [ ] Save/load between sessions
- [ ] All scene transitions work
- [ ] No null reference exceptions

---

## 🐛 Common Issues & Fixes

### "Null Reference Exception"
- Check all component references are assigned in inspector
- Verify prefabs are assigned correctly
- Check PlayerPrefs keys match between systems

### "Scene Not Found"
- Verify scene name matches exactly (case-sensitive)
- Check scene is added to Build Settings
- Verify SceneManager.LoadScene() uses correct name

### "UI Not Updating"
- Check text fields are assigned
- Verify Container has Layout Group
- Call Refresh methods after data changes

### "Prefabs Not Spawning"
- Check prefab references are assigned
- Verify container Transform is assigned
- Check Instantiate parent parameter is correct

---

## 📋 Final Verification

Before considering beta ready:

- [ ] All scenes load without errors
- [ ] All UI elements display properly
- [ ] All buttons have OnClick events
- [ ] All text fields update correctly
- [ ] Match flow works end-to-end
- [ ] Season progression works
- [ ] Team selection works
- [ ] Data persists correctly
- [ ] No major bugs
- [ ] Game is playable!

---

## 🎉 You're Ready!

Once all checkboxes are complete:
1. Test play full season
2. Fix any bugs found
3. Build executable
4. Share beta with testers!

**Estimated Total Time: 8-11 hours**

**Good luck! 🏈**
