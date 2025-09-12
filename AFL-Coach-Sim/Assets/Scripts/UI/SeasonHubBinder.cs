using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFLManager.UI
{
    public class SeasonHubBinder : MonoBehaviour
    {
        private UIController controller;
        private MonoBehaviour rosterManager;         // optional
        private MonoBehaviour seasonScreenManager;   // optional
        private MonoBehaviour saveLoadManager;       // optional

        // Example lightweight view model — replace with your real data source
        private readonly List<PlayerVM> _players = new List<PlayerVM>()
        {
            new PlayerVM("J. Smith",  "FWD", 3),
            new PlayerVM("T. Nguyen", "MID", 2),
            new PlayerVM("L. Taylor", "DEF", 4),
        };

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

            // Navigation
            root.Q<Button>("BackToMenuBtn")?.RegisterCallback<ClickEvent>(_ => controller.ShowMainMenu());
            root.Q<Button>("OpenScheduleBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenSchedule"));
            root.Q<Button>("OpenLadderBtn")?.RegisterCallback<ClickEvent>(_ => InvokeIfExists(seasonScreenManager, "OpenLadder"));

            // Labels (pull from Save/Season systems when ready)
            var upcoming = root.Q<Label>("UpcomingFixtureLabel");
            if (upcoming != null) upcoming.text = GetUpcomingFixtureText();

            var ladderMini = root.Q<Label>("LadderMiniLabel");
            if (ladderMini != null) ladderMini.text = "1. Cats   2. Demons   3. Pies";

            // Player ListView sample (bind to your roster data later)
            var list = root.Q<ListView>("PlayerList");
            if (list != null)
            {
                list.itemsSource = _players;
                list.makeItem = () => new Label();
                list.bindItem = (e, i) =>
                {
                    var vm = (PlayerVM)list.itemsSource[i];
                    if (e is Label l) l.text = $"{vm.Name}  ·  {vm.Pos}  ·  Form {vm.Form}";
                };
                list.selectionType = SelectionType.Single;

                list.selectionChanged += selected =>
                {
                    foreach (var item in selected)
                    {
                        if (item is PlayerVM) { InvokeIfExists(rosterManager, "OpenPlayerInspector"); break; }
                    }
                };

                // If you mutate _players at runtime:
                // list.RefreshItems();
            }
        }

        private string GetUpcomingFixtureText()
        {
            // TODO: Replace with SaveLoadManager/Season data
            return "Round 1: Eagles vs Dockers — Optus Stadium";
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
            else Debug.LogWarning($"[{nameof(SeasonHubBinder)}] Method '{method}' not found on {target.GetType().Name}");
#endif
        }

        private class PlayerVM
        {
            public string Name { get; set; }
            public string Pos { get; set; }
            public int Form { get; set; }

            public PlayerVM(string name, string pos, int form)
            {
                Name = name;
                Pos = pos;
                Form = form;
            }
        }
    }
}