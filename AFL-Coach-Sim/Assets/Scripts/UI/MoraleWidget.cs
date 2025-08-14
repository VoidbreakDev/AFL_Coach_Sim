// Assets/Scripts/UI/MoraleWidget.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AFLManager.UI
{
    public class MoraleWidget : MonoBehaviour
    {
        [SerializeField] Image barFill;
        [SerializeField] TMP_Text label;

        public void Bind(float pct, string word)
        {
            if (barFill) barFill.fillAmount = Mathf.Clamp01(pct);
            if (label) label.text = word;
        }
    }
}
