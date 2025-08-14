// Assets/Scripts/UI/ContractsWidget.cs
using TMPro;
using UnityEngine;

namespace AFLManager.UI
{
    public class ContractsWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text summary;
        public void Bind(string s) { if (summary) summary.text = s; }
    }
}