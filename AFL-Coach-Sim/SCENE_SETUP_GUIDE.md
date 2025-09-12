# AFL Coach Sim - Main Management UI Scene Setup Guide

This guide walks you through setting up a comprehensive main management interface that incorporates your new roster management UI components along with other game management features.

## Overview

We'll create a **MainManagementScene** that serves as the central hub where players can manage all aspects of their AFL coaching career, including:

- **Roster Management** (using your new SmartTeamBuilderUI, PlayerInspectorUI components)
- Dashboard (overview widgets)
- Budget & Contracts
- Training & Development
- Schedule & Matches
- Trade Market
- Club Management
- Inbox & Communications

## Scene Structure

### 1. Canvas Hierarchy

Create your main management scene with this structure:

```
MainManagementScene
├── Main Canvas (Canvas, CanvasScaler, GraphicRaycaster)
│   ├── Background Image
│   ├── Top Navigation Bar
│   │   ├── Club Logo
│   │   ├── Club Name Text
│   │   ├── Season/Round Info
│   │   ├── Quick Stats (Budget, Morale, etc.)
│   │   └── Settings Button
│   ├── Side Navigation Panel
│   │   ├── Navigation Background
│   │   ├── Dashboard Button
│   │   ├── Roster Button
│   │   ├── Training Button
│   │   ├── Budget Button
│   │   ├── Contracts Button
│   │   ├── Schedule Button
│   │   ├── Trade Market Button
│   │   ├── Club Button
│   │   └── Inbox Button
│   └── Content Area (ViewSwitcher)
│       ├── Dashboard Panel
│       ├── Roster Panel
│       ├── Training Panel
│       ├── Budget Panel
│       ├── Contracts Panel
│       ├── Schedule Panel
│       ├── Trade Market Panel
│       ├── Club Panel
│       └── Inbox Panel
└── Overlay Canvas (for dialogs, popups)
    ├── Player Inspector Dialog
    ├── Confirmation Dialogs
    └── Loading Screen
```

## Step-by-Step Setup

### Step 1: Create the Main Scene

1. Create a new scene: `Assets/Scenes/MainManagementScene.unity`
2. Add a **Canvas** with:
   - **Canvas Scaler**: UI Scale Mode = "Scale With Screen Size", Reference Resolution = 1920x1080
   - **Render Mode**: Screen Space - Overlay

### Step 2: Create the Manager GameObject

Create an empty GameObject called "MainManagementManager" and attach the script:

```csharp
// MainManagementManager.cs - Create this new script
using UnityEngine;
using AFLManager.UI;

namespace AFLManager.Managers
{
    public class MainManagementManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ViewSwitcher contentSwitcher;
        [SerializeField] private SmartTeamBuilderUI teamBuilderUI;
        [SerializeField] private PlayerInspectorUI playerInspectorUI;
        
        [Header("Navigation")]
        [SerializeField] private ViewSwitchButton[] navigationButtons;
        
        [Header("Widgets")]
        [SerializeField] private BudgetWidget budgetWidget;
        [SerializeField] private ContractsWidget contractsWidget;
        // Add other widgets as needed
        
        private string playerTeamId;
        
        void Start()
        {
            playerTeamId = PlayerPrefs.GetString("PlayerTeamId", "TEAM_001");
            InitializeUI();
            ShowDashboard();
        }
        
        private void InitializeUI()
        {
            // Set up navigation button callbacks
            foreach (var button in navigationButtons)
            {
                button.onClick.AddListener(() => contentSwitcher.Show(button.targetKey));
            }
            
            // Initialize roster management
            if (teamBuilderUI != null)
            {
                teamBuilderUI.OnPlayerSelected += ShowPlayerInspector;
            }
        }
        
        public void ShowDashboard()
        {
            contentSwitcher.Show("Dashboard");
            RefreshDashboardData();
        }
        
        public void ShowRosterManagement()
        {
            contentSwitcher.Show("Roster");
            if (teamBuilderUI != null)
            {
                teamBuilderUI.RefreshTeamData();
            }
        }
        
        private void ShowPlayerInspector(PlayerData player)
        {
            if (playerInspectorUI != null)
            {
                playerInspectorUI.ShowPlayer(player);
            }
        }
        
        private void RefreshDashboardData()
        {
            // Refresh all dashboard widgets
            // This would integrate with your existing DashboardDataBuilder
        }
    }
}
```

### Step 3: Set Up the Content Area (ViewSwitcher)

1. Create a **Panel** called "ContentArea" as a child of your main canvas
2. Attach the **ViewSwitcher** component
3. Configure panels for each section:

#### ViewSwitcher Panel Configuration:

```
Panels:
├── Dashboard (key: "Dashboard")
├── Roster (key: "Roster") 
├── Training (key: "Training")
├── Budget (key: "Budget")
├── Contracts (key: "Contracts")
├── Schedule (key: "Schedule")
├── TradeMarket (key: "TradeMarket")
├── Club (key: "Club")
└── Inbox (key: "Inbox")
```

### Step 4: Create the Roster Management Panel

This is where your new UI components will live:

#### Roster Panel Structure:
```
Roster Panel
├── Header
│   ├── Title: "Team Roster"
│   ├── Quick Stats (Total Players, Salary Cap, etc.)
│   └── Action Buttons (Generate New Roster, Import, Export)
├── Team Analysis Section
│   ├── Position Distribution Header
│   └── Position Bars Container (for PositionBarUI components)
│       ├── Defenders Bar (PositionBarUI)
│       ├── Midfielders Bar (PositionBarUI)
│       ├── Forwards Bar (PositionBarUI)
│       ├── Rucks Bar (PositionBarUI)
│       └── Utility Bar (PositionBarUI)
├── Controls Section
│   ├── Filter Dropdown (All, Defenders, Midfielders, etc.)
│   ├── Sort Dropdown (Name, Rating, Age, Position, etc.)
│   ├── Search Field
│   └── View Toggle (List/Grid)
├── Player List Container
│   ├── Scroll View
│   │   └── Content (Grid Layout Group)
│   │       └── Player Cards (PlayerCardUI prefabs spawned here)
│   └── Best 22 Selection Panel
│       ├── Field Formation Visual
│       └── Selected Players Display
└── Smart Team Builder (SmartTeamBuilderUI component)
```

### Step 5: Configure the Smart Team Builder UI

1. Create a **Panel** for the SmartTeamBuilderUI
2. Set up the prefab references:

```
SmartTeamBuilderUI References:
├── positionBarPrefab: Reference to PositionBarUI prefab
├── playerCardPrefab: Reference to PlayerCardUI prefab  
├── positionBarsContainer: Transform for position bars
├── playerListContainer: Transform for player cards
├── filterDropdown: TMP_Dropdown for filtering
├── sortDropdown: TMP_Dropdown for sorting
├── searchField: TMP_InputField for search
└── best22Container: Transform for selected team display
```

### Step 6: Create UI Prefabs

Create these prefabs in `Assets/Prefabs/UI/`:

#### PositionBarUI Prefab:
```
PositionBar Prefab
├── Background Image
├── Position Name (TextMeshPro)
├── Count Text (TextMeshPro)
├── Current Bar (Slider)
├── Ideal Bar (Slider) 
└── Status Icon (Image)
```

#### PlayerCardUI Prefab:
```
PlayerCard Prefab  
├── Card Background (Image + Button)
├── Position Background (Image)
├── Name Text (TextMeshPro)
├── Position Text (TextMeshPro)
├── Age Text (TextMeshPro)
├── Rating Text (TextMeshPro)
├── Status Icons Container
│   ├── Injury Icon (Image)
│   ├── Suspended Icon (Image)
│   └── Captain Icon (Image)
└── Selected Indicator (Image)
```

### Step 7: Set Up Navigation

Create navigation buttons that integrate with the ViewSwitcher:

```csharp
// Attach ViewSwitchButton component to each nav button
// Set targetKey to match ViewSwitcher panel keys:

Dashboard Button: targetKey = "Dashboard"
Roster Button: targetKey = "Roster"  
Training Button: targetKey = "Training"
// etc.
```

### Step 8: Create the Player Inspector Overlay

1. Create a separate **Canvas** with **Sort Order = 100** for overlays
2. Add the Player Inspector UI:

```
Overlay Canvas
└── Player Inspector Dialog
    ├── Background Overlay (semi-transparent)
    ├── Dialog Panel
    │   ├── Header (Player Name, Close Button)
    │   ├── Player Info Section (PlayerInspectorUI)
    │   ├── Position Ratings (PositionRatingUI components)
    │   ├── Stats & Attributes
    │   ├── Contract Info
    │   └── Action Buttons (Edit, Trade, Release)
    └── Confirmation Dialogs
```

### Step 9: Integration with Existing Systems

Connect your new UI to existing data systems:

```csharp
// In MainManagementManager.cs
private void LoadTeamData()
{
    // Use your existing data loading
    string teamId = PlayerPrefs.GetString("PlayerTeamId", "TEAM_001");
    TeamData team = SaveLoadManager.LoadTeam(teamId);
    
    // Convert and pass to UI components
    if (teamBuilderUI != null)
    {
        teamBuilderUI.SetTeamData(team);
    }
}

private void SaveTeamChanges()
{
    // Save any roster changes back to persistence layer
    SaveLoadManager.SaveTeam(playerTeamId, currentTeamData);
}
```

### Step 10: Scene References Setup

In the MainManagementManager inspector, assign:

- **contentSwitcher**: The ViewSwitcher component
- **teamBuilderUI**: SmartTeamBuilderUI component  
- **playerInspectorUI**: PlayerInspectorUI component
- **navigationButtons**: Array of ViewSwitchButton components
- **budgetWidget, contractsWidget**: Existing widget components

## UI Layout Tips

### Responsive Design
- Use **Content Size Fitter** on text elements
- Use **Layout Groups** (Horizontal/Vertical/Grid) for organized layouts
- Set **Layout Element** preferred sizes on key components

### Color Scheme
Consistent with AFL theme:
- **Primary**: Team colors (configurable per club)
- **Secondary**: Dark grays/whites for backgrounds  
- **Accent**: Green for positive stats, red for negative
- **Position Colors**: Blue (Defense), Green (Midfield), Red (Forward), Yellow (Ruck)

### Typography
- **Headers**: Bold, larger sizes (18-24pt)
- **Body Text**: Regular weight (12-16pt)  
- **Captions**: Smaller, lighter weight (10-12pt)

## Testing Checklist

- [ ] Scene loads without errors
- [ ] Navigation buttons switch panels correctly
- [ ] Roster data loads and displays
- [ ] Position bars show correct distribution
- [ ] Player cards display properly with colors
- [ ] Player inspector opens when clicking cards
- [ ] Filtering and sorting work
- [ ] Best 22 selection functions
- [ ] Data persists when switching panels
- [ ] Performance is smooth with full rosters (44+ players)

## Next Steps

1. **Create the scene** following this structure
2. **Test basic navigation** and panel switching
3. **Implement roster data loading** from your existing systems
4. **Add the remaining management panels** (Training, Budget, etc.)
5. **Polish the UI** with animations and feedback
6. **Integrate with match simulation** flow

This setup provides a solid foundation for expanding into a comprehensive management interface while leveraging your new position-aware roster management components.
