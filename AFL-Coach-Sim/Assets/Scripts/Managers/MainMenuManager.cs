// File: Assets/Scripts/Managers/MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AFLManager.Managers
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;  // assign your MainMenuPanel here
        [SerializeField] private GameObject loadGamePanel;  // already assigned

        public void OnNewGame()
        {
            SceneManager.LoadScene("CreateCoach");
        }

        public void OnLoadGame()
        {
            mainMenuPanel.SetActive(false);    // hide the main buttons
            loadGamePanel.SetActive(true);     // show the load screen
        }

        public void OnCancelLoad()
        {   
            loadGamePanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }


        // … your other methods …
    }
}
