// Assets/Scripts/UI/NavigationController.cs
using UnityEngine;

namespace AFLManager.UI
{
    public class NavigationController : MonoBehaviour
    {
        [SerializeField] GameObject panelRoster;
        [SerializeField] GameObject panelSchedule;
        [SerializeField] GameObject panelTactics;

        public void ShowRoster()   => ShowOnly(panelRoster);
        public void ShowSchedule() => ShowOnly(panelSchedule);
        public void ShowTactics()  => ShowOnly(panelTactics);

        void ShowOnly(GameObject target)
        {
            foreach (Transform c in transform)
                c.gameObject.SetActive(c.gameObject == target);
        }
    }
}
