// Assets/Scripts/UI/TrainingWidget.cs
using TMPro;
using UnityEngine;

namespace AFLManager.UI
{
    public class TrainingWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text nextDate;
        public void Bind(string dateStr) { if (nextDate) nextDate.text = dateStr; }
    }
}