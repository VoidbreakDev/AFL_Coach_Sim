// Assets/Scripts/UI/BudgetWidget.cs
using TMPro;
using UnityEngine;

namespace AFLManager.UI
{
    public class BudgetWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text amount;
        public void Bind(string money) { if (amount) amount.text = money; }
    }
}
