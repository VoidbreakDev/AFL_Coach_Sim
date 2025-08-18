using UnityEngine;

public class ViewSwitcher : MonoBehaviour
{
    public GameObject dashboardGrid;
    public GameObject seasonView;

    public void ShowDashboard()
    {
        if (dashboardGrid) dashboardGrid.SetActive(true);
        if (seasonView) seasonView.SetActive(false);
    }

    public void ShowSeason()
    {
        if (dashboardGrid) dashboardGrid.SetActive(false);
        if (seasonView) seasonView.SetActive(true);
    }
}