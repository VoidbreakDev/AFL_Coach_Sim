// File: Assets/Scripts/UI/SaveSlotUI.cs
using UnityEngine;
using TMPro;
using System;

namespace AFLManager.UI
{
    public class SaveSlotUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text coachNameText;
        [SerializeField] private TMP_Text dateText;

        public void SetData(string coachKey, DateTime saveDate)
        {
            coachNameText.text = coachKey;
            dateText.text      = saveDate.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
