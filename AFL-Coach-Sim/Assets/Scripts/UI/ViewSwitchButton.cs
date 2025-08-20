using UnityEngine;

public class ViewSwitchButton : MonoBehaviour
{
    public ViewSwitcher switcher;
    public string targetKey;

    // Hook this to your Button.onClick
    public void Activate()
    {
        if (switcher && !string.IsNullOrWhiteSpace(targetKey))
            switcher.Show(targetKey.Trim());
    }
}