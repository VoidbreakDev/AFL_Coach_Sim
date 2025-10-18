#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SimpleTMPCheck
{
    static SimpleTMPCheck()
    {
        EditorApplication.delayCall += CheckTMPAvailability;
    }
    
    static void CheckTMPAvailability()
    {
        EditorApplication.delayCall -= CheckTMPAvailability;
        
        Debug.Log("=== TextMeshPro Availability Check ===");
        
        try
        {
            // Check if we can access TMPro namespace
            var tmproAssembly = System.Reflection.Assembly.GetAssembly(typeof(TMPro.TextMeshProUGUI));
            if (tmproAssembly != null)
            {
                Debug.Log($"✅ TextMeshPro found in assembly: {tmproAssembly.FullName}");
            }
            else
            {
                Debug.LogError("❌ TextMeshPro assembly not found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error accessing TextMeshPro: {e.Message}");
            Debug.LogError($"   This indicates TMPro types are not available to the current assembly");
            Debug.LogError($"   Check that UnityEngine.UI is properly referenced in AFLCoachSim.Gameplay.asmdef");
        }
        
        // Check assembly references
        var gameplayAsmdef = AssetDatabase.LoadAssetAtPath<UnityEditor.Compilation.AssemblyDefinitionAsset>("Assets/Scripts/AFLCoachSim.Gameplay.asmdef");
        if (gameplayAsmdef != null)
        {
            Debug.Log("✅ Found AFLCoachSim.Gameplay.asmdef");
            var json = gameplayAsmdef.text;
            if (json.Contains("UnityEngine.UI"))
            {
                Debug.Log("✅ UnityEngine.UI reference found in assembly definition");
            }
            else
            {
                Debug.LogError("❌ UnityEngine.UI reference missing from assembly definition");
            }
        }
        else
        {
            Debug.LogError("❌ Could not load AFLCoachSim.Gameplay.asmdef");
        }
        
        Debug.Log("=== End Check ===");
    }
}
#endif