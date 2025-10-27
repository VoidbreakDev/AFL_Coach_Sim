// Assets/Scripts/Utilities/SceneTransitionHelper.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AFLManager.Models;

namespace AFLManager.Utilities
{
    /// <summary>
    /// Helper for smooth scene transitions with data passing and optional loading screens
    /// </summary>
    public static class SceneTransitionHelper
    {
        private static GameObject loadingScreenInstance;
        
        /// <summary>
        /// Load scene with match data
        /// </summary>
        public static void LoadMatchFlow(Match match, string playerTeamId, string returnScene)
        {
            // Store match data
            PlayerPrefs.SetString("CurrentMatchData", JsonUtility.ToJson(match));
            PlayerPrefs.SetString("CurrentMatchPlayerTeam", playerTeamId);
            PlayerPrefs.SetString("MatchFlowReturnScene", returnScene);
            PlayerPrefs.Save();
            
            // Load scene
            SceneManager.LoadScene("MatchFlow");
        }
        
        /// <summary>
        /// Return from match flow to origin scene
        /// </summary>
        public static void ReturnFromMatchFlow()
        {
            string returnScene = PlayerPrefs.GetString("MatchFlowReturnScene", "SeasonScreen");
            SceneManager.LoadScene(returnScene);
        }
        
        /// <summary>
        /// Load any scene with optional fade
        /// </summary>
        public static void LoadScene(string sceneName, bool showLoading = false)
        {
            if (showLoading)
            {
                // Could implement loading screen here
                Debug.Log($"Loading scene: {sceneName}");
            }
            
            SceneManager.LoadScene(sceneName);
        }
        
        /// <summary>
        /// Load scene asynchronously
        /// </summary>
        public static void LoadSceneAsync(MonoBehaviour context, string sceneName, System.Action onComplete = null)
        {
            context.StartCoroutine(LoadSceneAsyncCoroutine(sceneName, onComplete));
        }
        
        private static IEnumerator LoadSceneAsyncCoroutine(string sceneName, System.Action onComplete)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            while (!asyncLoad.isDone)
            {
                // Could update loading progress here
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                yield return null;
            }
            
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Get current scene name
        /// </summary>
        public static string GetCurrentScene()
        {
            return SceneManager.GetActiveScene().name;
        }
        
        /// <summary>
        /// Check if scene exists in build
        /// </summary>
        public static bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (name == sceneName)
                    return true;
            }
            return false;
        }
    }
}
