// Assets/Scripts/UI/InjuriesWidget.cs
using TMPro;
using UnityEngine;

namespace AFLManager.UI
{
    public class InjuriesWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text summary;
        public void BindNone() { if (summary) summary.text = "No injuries"; }
    }
}