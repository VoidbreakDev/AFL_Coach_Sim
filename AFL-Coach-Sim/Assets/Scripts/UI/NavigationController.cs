// Assets/Scripts/UI/NavigationController.cs
using UnityEngine;

public class NavigationController : MonoBehaviour
{
    [SerializeField] GameObject panelRoster;
    [SerializeField] GameObject panelSchedule;
    [SerializeField] GameObject panelTactics;
    // etc...

    public void ShowRoster()   => ShowOnly(panelRoster);
    public void ShowSchedule() => ShowOnly(panelSchedule);
    public void ShowTactics()  => ShowOnly(panelTactics);

    void ShowOnly(GameObject target)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(child.gameObject == target);
    }
}
