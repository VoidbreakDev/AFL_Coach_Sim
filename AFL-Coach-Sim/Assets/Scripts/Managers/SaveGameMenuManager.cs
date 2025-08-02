// File: Assets/Scripts/Managers/SaveGameMenuManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using AFLManager.UI;

namespace AFLManager.Managers
{
    public class SaveGameMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject saveSlotPrefab;  // Assign Prefabs/SaveSlotPanel.prefab
        public Transform slotContainer;    // Assign LoadScrollView/Viewport/Content

        void Start()
        {
            PopulateSaveSlots();
        }

        private void PopulateSaveSlots()
        {
            foreach (Transform t in slotContainer)
                Destroy(t.gameObject);

            string[] files = Directory.GetFiles(Application.persistentDataPath, "team_*.json");
            foreach (var file in files)
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(file);
                string key       = nameNoExt.Substring("team_".Length);
                DateTime dt      = File.GetLastWriteTime(file);

                var go = Instantiate(saveSlotPrefab, slotContainer);
                go.GetComponent<SaveSlotUI>().SetData(key, dt);

                var btn = go.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => LoadGame(key));
            }
        }

        private void LoadGame(string key)
        {
            PlayerPrefs.SetString("CoachName", key);
            SceneManager.LoadScene("RosterScreen");
        }
    }
}
