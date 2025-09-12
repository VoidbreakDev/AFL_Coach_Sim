# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview
AFL-Coach-Sim is a Unity-based Australian Football League coaching simulation game built with Unity 6000.2.1f1. The project uses a clean architecture with separate core simulation logic and Unity presentation layers.

## Development Commands

### Build & Test
```bash
# Run Unity EditMode tests via CLI
Unity -runTests -testPlatform editmode -testResults test-results.xml

# Run static analysis scan
python Tools/ci/static_scan.py --repo-root . --out static_scan_report.csv

# Build project (requires Unity Hub and Unity 6000.2.1f1)
Unity -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -buildPath build/
```

### Testing
```bash
# Run specific test assemblies
Unity -runTests -testPlatform editmode -assemblyNames "AFLCoachSim.Core.Tests" -testResults core-tests.xml

# Run Unity tests in specific category
Unity -runTests -testPlatform editmode -testCategory "Match" -testResults match-tests.xml
```

### Development Setup
```bash
# Install required dependencies (Python for CI tools)
python -m pip install pandas

# Restore packages (Unity will do this automatically when opening)
# Check Unity Package Manager for dependencies
```

## Architecture Overview

### Core Structure
The project follows a **Domain-Driven Design** approach with clear separation between:

1. **Core Simulation Layer** (`Assets/SimCore/AFLCoachSim.Core/`)
   - Domain entities and aggregates (Team, Player, Match)
   - Match simulation engine with advanced modeling
   - Pure C# with no Unity dependencies

2. **Unity Presentation Layer** (`Assets/Scripts/`)
   - MonoBehaviour managers and UI controllers
   - Unity-specific adapters and ScriptableObjects
   - Scene management and user interface

3. **Data Layer** 
   - SQLite persistence (`Assets/Plugins/SQLite/`)
   - ScriptableObject configurations (`Assets/Scripts/Data/`)
   - JSON serialization utilities

### Key Architectural Patterns

**Match Simulation Engine** (`AFLCoachSim.Core.Engine.Match.MatchEngine`):
- Phase-based simulation (CenterBounce → OpenPlay → Inside50 → ShotOnGoal)
- Advanced modeling including fatigue, rotations, injuries, and weather effects
- Deterministic random number generation for reproducible results
- Real-time telemetry and analytics support

**Domain Aggregates**:
- `Team`: Core team entity with attack/defense ratings
- `Player`: Individual player with attributes, roles, and runtime state
- `Match`: Complete match context with score, teams, and game state

**Simulation Models**:
- **M1**: Player role fitting and selection algorithms
- **M2**: Center bounce distribution mechanics  
- **M3**: Fatigue, rotation, and injury risk systems

### Assembly Structure
- `AFLCoachSim.Core`: Core simulation logic (Unity-independent)
- `AFLCoachSim.Editor`: Unity Editor tools and inspectors
- `AFLCoachSim.Gameplay`: Game-specific Unity components
- `Assembly-CSharp`: Main Unity scripts and managers

### Testing Strategy
- **Core Tests**: Pure C# unit tests in `AFLCoachSim.Core.Tests`
- **Unity Tests**: Integration tests in `Assets/Tests` and `Assets/Scripts/Tests`
- **Static Analysis**: Custom Python scanner for Unity-specific anti-patterns
- **CI Pipeline**: GitHub Actions with Unity EditMode test runner

### Key Manager Classes
- `SeasonBoot`: Main game initialization and simulation orchestration
- `MatchEngine`: Core match simulation with phase-based gameplay
- `LadderCalculator`: League standings and statistical calculations
- `AdvancedMatchSimulator`: High-level match simulation interface

### Data Flow
1. **Initialization**: `SeasonBoot` loads league config and initializes teams
2. **Simulation**: `AdvancedMatchSimulator` runs matches using `MatchEngine`
3. **Results**: `LadderCalculator` processes results and updates standings
4. **Persistence**: Results saved via repository pattern with SQLite backend

## Development Guidelines

### Code Organization
- Place Unity-independent logic in `AFLCoachSim.Core` namespace
- Use dependency injection for cross-cutting concerns (telemetry, persistence)
- Follow domain-driven design principles with clear aggregate boundaries

### Performance Considerations
- Avoid expensive Unity API calls in Update methods (flagged by static scanner)
- Use object pooling for frequently instantiated simulation objects
- Batch UI updates rather than updating individual elements per frame

### Testing Approach
- Write unit tests for core simulation logic in `AFLCoachSim.Core.Tests`
- Use Unity Test Framework for MonoBehaviour and integration tests
- Include deterministic random seeds for reproducible test scenarios

### Static Analysis Rules
The project includes a custom static scanner that flags:
- Expensive Unity API calls in Update methods
- LINQ usage in performance-critical loops
- Empty catch blocks and async void methods
- Missing event unsubscription patterns
- Editor-only code in runtime assemblies

## CI/CD Configuration
- **Unity Version**: 6000.2.1f1 (specified in CI workflows)
- **Test Authentication**: Login-based Unity licensing (UNITY_EMAIL/UNITY_PASSWORD)
- **Coverage**: Automatic code coverage reporting with HTML reports
- **Static Analysis**: Python-based heuristic scanning for Unity anti-patterns

## Package Dependencies
- **Unity Input System**: Modern input handling
- **Universal Render Pipeline (URP)**: Graphics rendering
- **Unity Barracuda**: ML inference (from GitHub)
- **Unity Test Framework**: Unit and integration testing
- **TextMesh Pro**: Advanced text rendering
- **Unity AI Navigation**: Pathfinding capabilities

## Development Notes
- The project uses .editorconfig for consistent code style
- Git LFS is configured for binary assets
- VSCode settings optimize file explorer for Unity projects
- Solution file groups related C# projects for better organization
