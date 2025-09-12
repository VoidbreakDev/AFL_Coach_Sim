using UnityEngine;
using UnityEngine.UIElements;

namespace AFLManager.UI
{
    public class MainMenuBinder : MonoBehaviour
    {
        private UIController controller;
        private MonoBehaviour rosterManager;         // optional
        private MonoBehaviour seasonScreenManager;   // optional
        private MonoBehaviour saveLoadManager;       // optional

        public void Init(
            UIController controller,
            MonoBehaviour rosterManager,
            MonoBehaviour seasonScreenManager,
            MonoBehaviour saveLoadManager)
        {
            this.controller = controller;
            this.rosterManager = rosterManager;
            this.seasonScreenManager = seasonScreenManager;
            this.saveLoadManager = saveLoadManager;

            var root = controller.GetComponent<UIDocument>().rootVisualElement;

            // Navigate to Season Hub
            root.Q<Button>("OpenSeasonBtn")?.RegisterCallback<ClickEvent>(_ => controller.ShowSeasonHub());
            root.Q<Button>("SeasonBtn")?.RegisterCallback<ClickEvent>(_ => controller.ShowSeasonHub());

            // Quick nav (wire to your existing managers when ready)
            root.Q<Button>("RosterBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(rosterManager, "Open"));
            root.Q<Button>("ScheduleBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenSchedule"));
            root.Q<Button>("ContractsBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenContracts"));
            root.Q<Button>("BudgetBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenBudget"));
            root.Q<Button>("TrainingBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenTraining"));
            root.Q<Button>("ClubBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenClub"));
            root.Q<Button>("SettingsBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenSettings"));

            // Primary actions
            root.Q<Button>("ContinueBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(saveLoadManager, "LoadLastSave"));
            root.Q<Button>("NewCareerBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "StartNewCareer"));
            root.Q<Button>("OpenSettingsBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenSettings"));
        }

        private void InvokeIfExists(MonoBehaviour target, string method)
        {
            if (target == null || string.IsNullOrEmpty(method)) return;
            var m = target.GetType().GetMethod(
                method,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (m != null) m.Invoke(target, null);
#if UNITY_EDITOR
            else Debug.LogWarning($"[{nameof(MainMenuBinder)}] Method '{method}' not found on {target.GetType().Name}");
#endif
        }
    }
}