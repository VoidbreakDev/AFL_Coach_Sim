# Player Development Panel - Unity UI Hierarchy Blueprint

## 🏗️ Complete UI Structure

This document provides the exact Unity hierarchy structure for implementing the Player Development Panel system. Use this as your blueprint when creating the UI components.

---

## 📋 Main Panel Structure

```
PlayerDevelopmentPanel (GameObject)
├── Component: PlayerDevelopmentPanel
├── Component: Canvas Group (for fade in/out)
└── Background (Image)
    ├── HeaderSection/
    │   ├── PlayerPortrait (Image)
    │   ├── PlayerNameHeader (TextMeshPro - UGUI)
    │   └── CloseButton (Button)
    │       └── Text: "✕" (TextMeshPro - UGUI)
    │
    ├── TabNavigation/
    │   ├── TabBackground (Image)
    │   ├── SpecializationTabButton (Button)
    │   │   └── Text: "Specialization" (TextMeshPro - UGUI)
    │   ├── EventsTabButton (Button)
    │   │   └── Text: "Events" (TextMeshPro - UGUI)
    │   ├── HistoryTabButton (Button)
    │   │   └── Text: "History" (TextMeshPro - UGUI)
    │   └── PlanningTabButton (Button)
    │       └── Text: "Planning" (TextMeshPro - UGUI)
    │
    └── TabContent/
        ├── SpecializationTab/
        ├── EventsTab/
        ├── HistoryTab/
        └── PlanningTab/
```

---

## 🎯 Tab 1: Specialization Tab

```
SpecializationTab (GameObject)
├── Component: Canvas Group
├── CurrentSpecializationInfo/
│   ├── Background (Image)
│   ├── CurrentSpecializationText (TextMeshPro - UGUI)
│   ├── ProgressSection/
│   │   ├── ProgressLabel (TextMeshPro - UGUI) "Mastery Progress:"
│   │   ├── SpecializationProgressSlider (Slider)
│   │   │   ├── Background (Image)
│   │   │   ├── Fill Area/
│   │   │   │   └── Fill (Image) - Color: Cyan
│   │   │   └── Handle Slide Area/
│   │   │       └── Handle (Image)
│   │   └── SpecializationProgressText (TextMeshPro - UGUI)
│   ├── StatsSection/
│   │   ├── CareerExperienceText (TextMeshPro - UGUI)
│   │   └── DevelopmentStageText (TextMeshPro - UGUI)
│   └── Component: Content Size Fitter
│
└── SpecializationTreeSection/
    ├── Component: SpecializationTreeUI
    ├── TreeBackground (Image)
    ├── TierLabels/
    │   ├── Tier1Label (TextMeshPro - UGUI) "Basic"
    │   ├── Tier2Label (TextMeshPro - UGUI) "Advanced"
    │   ├── Tier3Label (TextMeshPro - UGUI) "Elite"
    │   └── Tier4Label (TextMeshPro - UGUI) "Legendary"
    │
    ├── TreeNodes/
    │   ├── Tier1Container (GameObject)
    │   │   └── Component: Horizontal Layout Group
    │   ├── Tier2Container (GameObject)
    │   │   └── Component: Horizontal Layout Group
    │   ├── Tier3Container (GameObject)
    │   │   └── Component: Horizontal Layout Group
    │   └── Tier4Container (GameObject)
    │       └── Component: Horizontal Layout Group
    │
    ├── ConnectionsContainer (GameObject)
    │   └── (Dynamic connection lines created at runtime)
    │
    └── SpecializationInfoPanel/
        ├── Background (Image)
        ├── InfoTitle (TextMeshPro - UGUI)
        ├── InfoDescription (TextMeshPro - UGUI)
        ├── InfoRequirements (TextMeshPro - UGUI)
        ├── AttributeWeightsSection/
        │   ├── WeightsTitle (TextMeshPro - UGUI) "Development Focus:"
        │   └── InfoAttributesContainer (GameObject)
        │       └── Component: Vertical Layout Group
        └── CloseInfoButton (Button)
            └── Text: "Close" (TextMeshPro - UGUI)
```

---

## 🎆 Tab 2: Events Tab

```
EventsTab (GameObject)
├── Component: Canvas Group
├── BreakthroughReadinessSection/
│   ├── Background (Image)
│   ├── ReadinessTitle (TextMeshPro - UGUI) "Breakthrough Readiness"
│   ├── BreakthroughReadinessSlider (Slider)
│   │   ├── Background (Image)
│   │   ├── Fill Area/
│   │   │   └── Fill (Image) - Color: Orange
│   │   └── Handle Slide Area/
│   │       └── Handle (Image)
│   └── BreakthroughReadinessText (TextMeshPro - UGUI)
│
├── ActiveEventSection/
│   ├── Component: BreakthroughEventDisplayUI
│   ├── ActiveEventContainer (GameObject)
│   │   ├── EventBackground (Image)
│   │   ├── EventHeader/
│   │   │   ├── EventIcon (Image)
│   │   │   ├── EventTitleText (TextMeshPro - UGUI)
│   │   │   └── TimeRemainingSection/
│   │   │       ├── TimeRemainingSlider (Slider)
│   │   │       └── TimeRemainingText (TextMeshPro - UGUI)
│   │   ├── EventDescriptionText (TextMeshPro - UGUI)
│   │   └── EffectsSection/
│   │       ├── EffectsTitle (TextMeshPro - UGUI) "Active Effects:"
│   │       └── EffectsContainer (GameObject)
│   │           └── Component: Vertical Layout Group
│   │
│   ├── NoActiveEventMessage (GameObject)
│   │   ├── NoEventIcon (Image)
│   │   └── NoEventText (TextMeshPro - UGUI) "No active breakthrough events"
│   │
│   └── ParticleEffects/
│       ├── PositiveEventParticles (Particle System)
│       └── NegativeEventParticles (Particle System)
│
└── EventHistorySection/
    ├── HistoryTitle (TextMeshPro - UGUI) "Event History"
    ├── HistoryScrollView (Scroll View)
    │   ├── Viewport/
    │   │   └── EventHistoryContainer (GameObject)
    │   │       └── Component: Vertical Layout Group
    │   └── Scrollbar/
    └── Component: Content Size Fitter
```

---

## 📈 Tab 3: History Tab

```
HistoryTab (GameObject)
├── Component: Canvas Group
├── TimelineSection/
│   ├── Component: DevelopmentTimelineUI
│   ├── TimelineTitle (TextMeshPro - UGUI) "Development Timeline"
│   ├── TimelineScrollView (Scroll View)
│   │   ├── Viewport/
│   │   │   └── TimelineContainer (GameObject)
│   │   │       └── Component: Vertical Layout Group
│   │   └── Scrollbar/
│   └── Component: Content Size Fitter
│
├── CareerHighlightsSection/
│   ├── HighlightsTitle (TextMeshPro - UGUI) "Career Highlights"
│   ├── HighlightsGrid (GameObject)
│   │   ├── Component: Grid Layout Group
│   │   └── CareerHighlightsContainer (GameObject)
│   └── Component: Content Size Fitter
│
└── SummarySection/
    ├── SummaryBackground (Image)
    ├── TotalDevelopmentText (TextMeshPro - UGUI)
    └── Component: Content Size Fitter
```

---

## 🎯 Tab 4: Planning Tab

```
PlanningTab (GameObject)
├── Component: Canvas Group
├── DevelopmentPlannerSection/
│   ├── Component: DevelopmentPlannerUI
│   ├── PlannerTitle (TextMeshPro - UGUI) "Development Planning"
│   ├── PlannerContainer (GameObject)
│   └── Component: Content Size Fitter
│
└── RecommendationsSection/
    ├── RecommendationsTitle (TextMeshPro - UGUI) "Training Recommendations"
    ├── RecommendationsScrollView (Scroll View)
    │   ├── Viewport/
    │   │   └── RecommendedFocusContainer (GameObject)
    │   │       └── Component: Vertical Layout Group
    │   └── Scrollbar/
    └── Component: Content Size Fitter
```

---

## 🧩 Prefab Structures

### SpecializationNode Prefab
```
SpecializationNodePrefab (GameObject)
├── Component: SpecializationNodeUI
├── Component: Button
├── NodeBackground (Image)
├── NodeContent/
│   ├── NodeIcon (Image)
│   ├── NodeTitle (TextMeshPro - UGUI)
│   ├── TierText (TextMeshPro - UGUI)
│   └── ProgressContainer/
│       └── ProgressSlider (Slider)
├── LockOverlay (GameObject)
│   ├── LockIcon (Image)
│   └── LockBackground (Image)
└── CompletedCheckmark (Image)
```

### EventHistoryEntry Prefab
```
EventHistoryEntryPrefab (GameObject)
├── Component: EventHistoryEntryUI
├── EntryBackground (Image)
├── EventContent/
│   ├── EventIcon (Image)
│   ├── EventInfo/
│   │   ├── EventNameText (TextMeshPro - UGUI)
│   │   └── EventDescriptionText (TextMeshPro - UGUI)
│   └── DateText (TextMeshPro - UGUI)
└── Component: Layout Element
```

### AttributeWeight Prefab
```
AttributeWeightPrefab (GameObject)
├── Component: AttributeWeightUI
├── WeightBackground (Image)
├── AttributeInfo/
│   ├── AttributeNameText (TextMeshPro - UGUI)
│   ├── WeightSlider (Slider)
│   └── WeightValueText (TextMeshPro - UGUI)
└── Component: Layout Element
```

### BreakthroughEffectEntry Prefab
```
BreakthroughEffectEntryPrefab (GameObject)
├── Component: BreakthroughEffectEntryUI
├── EffectBackground (Image)
├── EffectContent/
│   ├── EffectIcon (Image)
│   ├── EffectArrow (Image)
│   ├── EffectNameText (TextMeshPro - UGUI)
│   └── EffectValueText (TextMeshPro - UGUI)
└── Component: Layout Element
```

### CareerHighlight Prefab
```
CareerHighlightPrefab (GameObject)
├── HighlightBackground (Image)
├── HighlightText (TextMeshPro - UGUI)
└── Component: Layout Element
```

### FocusRecommendation Prefab
```
FocusRecommendationPrefab (GameObject)
├── RecommendationBackground (Image)
├── RecommendationText (TextMeshPro - UGUI)
└── Component: Layout Element
```

### TimelineEntry Prefab
```
TimelineEntryPrefab (GameObject)
├── EntryBackground (Image)
├── TimelineMarker (Image)
├── EntryContent/
│   ├── Title (TextMeshPro - UGUI)
│   └── Description (TextMeshPro - UGUI)
└── Component: Layout Element
```

### ConnectionLine Prefab
```
ConnectionLinePrefab (GameObject)
├── Component: SpecializationConnectionUI
├── LineImage (Image)
└── Component: RectTransform (for positioning)
```

---

## 🎨 Recommended Layout Settings

### Layout Groups
- **Horizontal Layout Groups**: Child Alignment = Middle Center, Spacing = 20
- **Vertical Layout Groups**: Child Alignment = Upper Left, Spacing = 10
- **Grid Layout Groups**: Cell Size = (200, 50), Spacing = (10, 10)

### Content Size Fitters
- Use **Vertical Fit: Preferred Size** for dynamic height containers
- Use **Horizontal Fit: Unconstrained** unless specific width needed

### Scroll Views
- **Elasticity**: 0.1 for smooth scrolling
- **Inertia**: True
- **Deceleration Rate**: 0.135

---

## 🔧 Component Assignment Guide

### PlayerDevelopmentPanel Component Assignments
```csharp
// Header Section
closeButton → HeaderSection/CloseButton
playerNameHeader → HeaderSection/PlayerNameHeader
playerPortrait → HeaderSection/PlayerPortrait

// Tab System
specializationTabButton → TabNavigation/SpecializationTabButton
eventsTabButton → TabNavigation/EventsTabButton
historyTabButton → TabNavigation/HistoryTabButton
planningTabButton → TabNavigation/PlanningTabButton
tabPanels[0] → TabContent/SpecializationTab
tabPanels[1] → TabContent/EventsTab
tabPanels[2] → TabContent/HistoryTab
tabPanels[3] → TabContent/PlanningTab

// Specialization Tab
specializationTree → SpecializationTab/SpecializationTreeSection
currentSpecializationText → SpecializationTab/CurrentSpecializationInfo/CurrentSpecializationText
// ... and so on for each field
```

---

## 💡 Implementation Tips

1. **Start with the main panel structure** and test tab switching
2. **Build one tab at a time** to avoid overwhelming complexity
3. **Create prefabs early** to maintain consistency
4. **Use Layout Groups** for automatic positioning
5. **Test with mock data** using the Context Menu options
6. **Configure Canvas settings** for proper scaling across devices

This hierarchy provides a complete blueprint for implementing the Player Development Panel system in Unity. Each component is designed to work together seamlessly and provide a polished, professional UI experience.