// File: Assets/Scripts/Dev/LadderMiniRowDebug.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AFLManager.Dev
{
    [ExecuteAlways]
    public class LadderMiniRowDebug : MonoBehaviour
    {
        void OnEnable()
        {
            // Ensure the row has a visible background strip
            var img = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            if (img) { var c = new Color(0,0,0,0.15f); img.color = c; img.raycastTarget = false; } // subtle dark

            // Ensure texts are dark enough to see
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            {
                var col = t.color; col.a = 1f; t.color = new Color(0.15f,0.15f,0.15f,1f);
                t.enableAutoSizing = false;
                t.fontSize = Mathf.Max(t.fontSize, 16);
                t.gameObject.SetActive(true);
            }

            // Reset any weird scaling
            var rt = transform as RectTransform;
            if (rt) { rt.localScale = Vector3.one; rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 0.5f); }
        }
    }
}
