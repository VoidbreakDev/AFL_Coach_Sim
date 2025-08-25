using UnityEngine;
using UnityEngine.UIElements;

namespace AFLManager.UI
{
    public class UIController : MonoBehaviour
    {
        [Header("UIDocument & Assets")]
        [SerializeField] private UIDocument uiDocument; // Assign in Inspector
        [SerializeField] private VisualTreeAsset mainMenuUxml; // Assign MainMenu.uxml
        [SerializeField] private VisualTreeAsset seasonHubUxml; // Assign SeasonHub.uxml
        [SerializeField] private StyleSheet themeStyle; // Assign theme.uss

        [Header("Panel Settings (shared)")]
        [SerializeField] private PanelSettings panelSettings; // Unity 6.2: share one asset

        // Optional: references to your existing managers
        [Header("Managers (Optional)")]
        [SerializeField] private MonoBehaviour rosterManager; // e.g., RosterManager
        [SerializeField] private MonoBehaviour seasonScreenManager; // e.g., SeasonScreenManager
        [SerializeField] private MonoBehaviour saveLoadManager; // e.g., SaveLoadManager

        private VisualElement root;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (panelSettings != null)
                uiDocument.panelSettings = panelSettings;

            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            root = uiDocument.rootVisualElement;
            root.Clear();
            var tree = mainMenuUxml.Instantiate();
            root.Add(tree);
            if (themeStyle != null && !root.styleSheets.Contains(themeStyle))
                root.styleSheets.Add(themeStyle);

            var binder = GetOrCreate<MainMenuBinder>(root);
            binder.Init(this, rosterManager, seasonScreenManager, saveLoadManager);

            // Default focus for keyboard/gamepad navigation
            root.Q<Button>("SeasonBtn")?.Focus();
        }

        public void ShowSeasonHub()
        {
            root = uiDocument.rootVisualElement;
            root.Clear();
            var tree = seasonHubUxml.Instantiate();
            root.Add(tree);
            if (themeStyle != null && !root.styleSheets.Contains(themeStyle))
                root.styleSheets.Add(themeStyle);

            var binder = GetOrCreate<SeasonHubBinder>(root);
            binder.Init(this, rosterManager, seasonScreenManager, saveLoadManager);

            root.Q<Button>("BackToMenuBtn")?.Focus();
        }

        private T GetOrCreate<T>(VisualElement root) where T : Component
        {
            var existing = GetComponent<T>();
            if (existing != null) return existing;
            return gameObject.AddComponent<T>();
        }
    }
}