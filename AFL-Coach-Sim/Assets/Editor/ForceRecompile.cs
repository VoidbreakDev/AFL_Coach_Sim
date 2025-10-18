#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Forces Unity to refresh and recompile all scripts
/// </summary>
public static class ForceRecompile
{
    [MenuItem("AFL Coach Sim/Force Recompile All")]
    public static void ForceRecompileAll()
    {
        Debug.Log("Forcing Unity to refresh and recompile all scripts...");
        
        // Refresh the Asset Database
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        
        // Request script compilation
        EditorUtility.RequestScriptReload();
        
        Debug.Log("Recompile requested. Unity should refresh all assembly references.");
    }
    
    [MenuItem("AFL Coach Sim/Reimport All Assets")]
    public static void ReimportAllAssets()
    {
        Debug.Log("Reimporting all assets...");
        AssetDatabase.ImportAsset("Assets", ImportAssetOptions.ImportRecursive);
        Debug.Log("Asset reimport complete.");
    }
}
#endif