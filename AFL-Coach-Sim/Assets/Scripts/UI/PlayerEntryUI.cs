// File: Assets/Scripts/UI/PlayerEntryUI.cs
using UnityEngine;
using TMPro;
using AFLManager.Models;

namespace AFLManager.UI
{
    public class PlayerEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text statsText;

        public void SetData(Player player)
        {
            nameText.text  = player.Name;
            roleText.text  = player.Role.ToString();
            statsText.text = $"K:{player.Stats.Kicking}  S:{player.Stats.Speed}";
        }
    }
}
