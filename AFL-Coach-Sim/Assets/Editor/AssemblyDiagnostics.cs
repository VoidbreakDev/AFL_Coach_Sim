#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Diagnostic script to check Unity assembly references and TextMeshPro availability
/// </summary>
public static class AssemblyDiagnostics
{
    [MenuItem("AFL Coach Sim/Check Assembly Status")]
    public static void CheckAssemblyStatus()
    {
        Debug.Log("=== Assembly Diagnostics ===");
        
        // Check all loaded assemblies
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        Debug.Log($"Total loaded assemblies: {assemblies.Length}");
        
        // Look for UI-related assemblies
        var uiAssemblies = assemblies.Where(a => a.GetName().Name.ToLower().Contains("ui") || 
                                               a.GetName().Name.ToLower().Contains("textmesh")).ToArray();
        
        Debug.Log("UI-related assemblies:");
        foreach (var assembly in uiAssemblies)
        {
            Debug.Log($"  - {assembly.GetName().Name} (Version: {assembly.GetName().Version})");
        }
        
        // Try to find TMPro types
        try
        {
            var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI");
            if (tmpType != null)
            {
                Debug.Log($"✓ TMPro.TextMeshProUGUI found in assembly: {tmpType.Assembly.GetName().Name}");
            }
            else
            {
                Debug.LogWarning("✗ TMPro.TextMeshProUGUI not found");
            }
            
            var tmpTextType = System.Type.GetType("TMPro.TMP_Text");
            if (tmpTextType != null)
            {
                Debug.Log($"✓ TMPro.TMP_Text found in assembly: {tmpTextType.Assembly.GetName().Name}");
            }
            else
            {
                Debug.LogWarning("✗ TMPro.TMP_Text not found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking TMPro types: {e.Message}");
        }
        
        Debug.Log("=== End Diagnostics ===");
    }
}
#endif