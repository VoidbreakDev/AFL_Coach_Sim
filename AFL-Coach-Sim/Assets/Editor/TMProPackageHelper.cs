#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Helper script to ensure TextMeshPro is properly accessible through UnityEngine.UI in Unity 6
/// </summary>
public static class TMProPackageHelper
{
    [InitializeOnLoadMethod]
    static void CheckTMProPackage()
    {
        try
        {
            // In Unity 6, TextMeshPro is part of UnityEngine.UI
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(TMPro.TextMeshProUGUI));
            if (assembly == null)
            {
                Debug.LogWarning("TextMeshPro not found. Ensure UnityEngine.UI assembly is properly referenced.");
            }
            else
            {
                Debug.Log($"TextMeshPro found in assembly: {assembly.GetName().Name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking TextMeshPro availability: {e.Message}");
        }
    }
}
#endif