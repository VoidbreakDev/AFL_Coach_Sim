using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[DisallowMultipleComponent]
public class ViewSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class PanelEntry
    {
        public string key;              // e.g., "Dashboard", "Season", "Training"
        public GameObject panel;        // root of the panel content
    }

    [Header("Panels (keys must be unique)")]
    public List<PanelEntry> panels = new List<PanelEntry>();

    [Header("Defaults & Behavior")]
    public string defaultKey = "Dashboard";
    public bool deactivateInactive = true;      // disable GameObjects for inactive views
    public bool useFade = false;                 // optional fade
    [Range(0f, 1f)] public float fadeDuration = 0.15f;

    [Header("Events")]
    public UnityEvent<string> onBeforeSwitch;    // passes nextKey
    public UnityEvent<string> onAfterSwitch;     // passes currentKey

    // runtime
    private Dictionary<string, GameObject> _map;
    private string _currentKey;

    void Awake()
    {
        BuildMap();
        // If current key is valid, keep it; else fall back to defaultKey or first
        var startKey = string.IsNullOrEmpty(_currentKey) ? ResolveDefaultKey() : _currentKey;
        InstantShow(startKey);
    }

    void OnValidate()
    {
        // Keep keys trimmed and unique-ish during editing
        if (panels != null)
        {
            foreach (var p in panels)
                if (p != null && p.key != null) p.key = p.key.Trim();
        }
    }

    public void Show(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (_map == null || _map.Count == 0) BuildMap();
        if (!_map.ContainsKey(key))
        {
            Debug.LogWarning($"[ViewSwitcher] Unknown key: {key}");
            return;
        }

        if (_currentKey == key) return;

        onBeforeSwitch?.Invoke(key);
        SetActiveState(key);
        _currentKey = key;
        onAfterSwitch?.Invoke(_currentKey);
    }

    public void ShowIndex(int index)
    {
        if (panels == null || panels.Count == 0) return;
        index = Mathf.Clamp(index, 0, panels.Count - 1);
        Show(panels[index].key);
    }

    public void Next()
    {
        if (panels == null || panels.Count == 0) return;
        int idx = IndexOf(_currentKey);
        idx = (idx + 1) % panels.Count;
        Show(panels[idx].key);
    }

    public void Prev()
    {
        if (panels == null || panels.Count == 0) return;
        int idx = IndexOf(_currentKey);
        idx = (idx - 1 + panels.Count) % panels.Count;
        Show(panels[idx].key);
    }

    public string CurrentKey() => _currentKey;

    // — Internals —

    private void BuildMap()
    {
        _map = new Dictionary<string, GameObject>();
        if (panels == null) return;
        foreach (var p in panels)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.key) || p.panel == null) continue;
            var k = p.key.Trim();
            if (_map.ContainsKey(k))
                Debug.LogWarning($"[ViewSwitcher] Duplicate key '{k}' — ignoring later entries.");
            else
                _map.Add(k, p.panel);
        }
    }

    private string ResolveDefaultKey()
    {
        if (!string.IsNullOrWhiteSpace(defaultKey) && _map.ContainsKey(defaultKey))
            return defaultKey;

        // fallback to first valid entry
        var first = panels?.FirstOrDefault(pe => pe != null && pe.panel != null && !string.IsNullOrWhiteSpace(pe.key));
        return first != null ? first.key.Trim() : string.Empty;
    }

    private int IndexOf(string key)
    {
        if (panels == null || panels.Count == 0) return 0;
        for (int i = 0; i < panels.Count; i++)
            if (panels[i] != null && panels[i].key == key) return i;
        return 0;
    }

    private void InstantShow(string key)
    {
        if (string.IsNullOrEmpty(key) || _map == null) return;

        foreach (var kv in _map)
        {
            bool active = kv.Key == key;
            if (useFade)
            {
                // Set visibility via CanvasGroup (no animation on first frame)
                EnsureCanvasGroup(kv.Value, out var cg);
                cg.alpha = active ? 1f : 0f;
                cg.blocksRaycasts = active;
                cg.interactable = active;
            }
            if (deactivateInactive) kv.Value.SetActive(active);
            else if (!useFade) kv.Value.SetActive(true); // keep all active if not fading
        }

        _currentKey = key;
    }

    private void SetActiveState(string key)
    {
        if (useFade)
        {
            // Keep all panels active, animate CanvasGroups
            foreach (var kv in _map)
            {
                EnsureCanvasGroup(kv.Value, out var cg);
                bool makeActive = kv.Key == key;

                // ensure object active if fading
                if (!kv.Value.activeSelf) kv.Value.SetActive(true);

                StopAllCoroutines();
                StartCoroutine(FadeCanvasGroup(cg, makeActive ? 1f : 0f, fadeDuration));

                cg.blocksRaycasts = makeActive;
                cg.interactable = makeActive;
            }
        }
        else
        {
            // Hard toggle
            foreach (var kv in _map)
                kv.Value.SetActive(kv.Key == key || !deactivateInactive);
        }
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float to, float duration)
    {
        float from = cg.alpha;
        if (Mathf.Approximately(from, to)) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ignore timescale
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private static void EnsureCanvasGroup(GameObject go, out CanvasGroup cg)
    {
        cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
    }
}