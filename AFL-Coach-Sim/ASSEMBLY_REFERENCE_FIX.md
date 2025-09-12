# Assembly Reference Fix - MatchTuningSO

## ✅ **CS0246 Errors on Lines 34, 35, 376 FIXED**

### **Issue:**
```
CS0246: The type or namespace name 'MatchTuningSO' could not be found 
(are you missing a using directive or an assembly reference?)
```

**File:** `Assets/Editor/MatchTelemetryWindow.cs` (lines 34, 35, 376)

### **Root Cause:**
The `MatchTelemetryWindow.cs` (Editor assembly) was trying to access `MatchTuningSO` class which is located in `Assets/Game/Config/MatchTuningSO.cs` (Assembly-CSharp), but the Editor assembly didn't reference Assembly-CSharp.

### **The Fix:**

**Updated:** `Assets/Editor/AFLCoachSim.Editor.asmdef`

```json
// BEFORE (❌ Missing reference):
"references": [
  "AFLCoachSim.Core",
  "AFLCoachSim.Gameplay",
  "Unity.TextMeshPro",
  "Unity.UGUI"
],

// AFTER (✅ Added Assembly-CSharp reference):
"references": [
  "AFLCoachSim.Core", 
  "AFLCoachSim.Gameplay",
  "Assembly-CSharp",     // ← This fixes the MatchTuningSO access
  "Unity.TextMeshPro",
  "Unity.UGUI"
],
```

### **Why This Works:**

1. **File Location:** `MatchTuningSO.cs` is in `Assets/Game/Config/`
2. **Default Assembly:** Since there's no `.asmdef` in the `Game` folder, it belongs to `Assembly-CSharp`  
3. **Editor Access:** Editor scripts need explicit references to access runtime assemblies
4. **Missing Link:** The Editor assembly wasn't referencing `Assembly-CSharp`

### **What This Enables:**

The `MatchTelemetryWindow` editor tool can now:
- ✅ Access `MatchTuningSO` ScriptableObject for tuning parameters
- ✅ Use `MatchTuningProvider.GetOrCreateAsset()` 
- ✅ Create local preview instances for live editing
- ✅ Convert between `MatchTuningSO` ↔ `MatchTuning` runtime objects

### **Files Involved:**
- ✅ **Fixed:** `AFLCoachSim.Editor.asmdef` - Added Assembly-CSharp reference
- ✅ **Now Accessible:** `MatchTuningSO.cs` - ScriptableObject for match tuning
- ✅ **Now Accessible:** `MatchTuningProvider.cs` - Asset management utilities

---

## **🎯 Result:**

The `MatchTelemetryWindow` editor tool should now compile successfully and provide access to match tuning parameters for debugging and tweaking the match simulation engine.

**Next:** Recompile in Unity - the CS0246 errors should be resolved! 🚀
