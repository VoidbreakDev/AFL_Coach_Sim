# Unity Compilation Error Fixes

## How to Share Errors With Me

1. Open Unity
2. Open Console window (Window → General → Console)
3. Click on an error
4. Right-click → Copy
5. Paste ALL errors here

## Common Issues & Fixes

### Issue 1: "PositionCategory" not found

If you see errors about `PositionCategory` not being found, it's because it's defined in `TeamDataHelper` in the `AFLManager.Utilities` namespace.

**Fix:** Add this using statement to any file that uses `PositionCategory`:
```csharp
using AFLManager.Utilities;
```

Files that need this:
- `TeamSelectionManager.cs` (already has it)
- `PlayerSelectionCard.cs` (already has it)
- `LineupSlot.cs` (already has it)

### Issue 2: "MatchResult" vs "MatchResultDTO"

The codebase uses `MatchResult` (from AFLManager.Models) in some places.

**Check:** Does `MatchResult` exist or is it `MatchResultDTO`?

### Issue 3: UI scripts in wrong namespace

`MatchPreviewUI`, `MatchSimulationUI`, and `MatchResultsUI` are in `AFLManager.Managers` namespace but in the UI folder.

**Fix option 1:** Keep as-is (works fine)
**Fix option 2:** Change to `AFLManager.UI` namespace

### Issue 4: Assembly references

If getting "type not found" errors across assemblies:

1. Select `Assets/Scripts/AFLCoachSim.Gameplay.asmdef`
2. Check References includes:
   - AFLCoachSim.Core
   - UnityEngine.UI  
   - Unity.TextMeshPro

---

## Please Share Specific Errors

Copy errors from Unity Console that look like:

```
Assets/Scripts/...cs(123,45): error CS0246: The type or namespace name 'X' could not be found
```

Then I can provide exact fixes!
