// File: Assets/Scripts/Managers/CreateCoachManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;                     // ← add this

public class CreateCoachManager : MonoBehaviour
{
    [SerializeField]            // or public
    private TMP_InputField coachNameInput;  // ← change type here

    public void OnCreate()
    {
        string name = coachNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Coach name cannot be empty.");
            return;
        }

        PlayerPrefs.SetString("CoachName", name);
        SceneManager.LoadScene("RosterScreen");
    }

    public void OnBack()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
