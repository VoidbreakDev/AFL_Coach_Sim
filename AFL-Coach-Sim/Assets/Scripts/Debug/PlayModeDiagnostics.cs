using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayModeDiagnostics : MonoBehaviour
{
    void Start()
    {
        var cams = Camera.allCameras;
        Debug.Log($"[Diag] Cameras: {cams.Length}");
        foreach (var c in cams)
            Debug.Log($"[Diag] Camera '{c.name}' enabled={c.enabled} cullingMask={c.cullingMask} targetDisplay={c.targetDisplay} clearFlags={c.clearFlags}");

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"[Diag] Canvases: {canvases.Length}");
        foreach (var cv in canvases)
            Debug.Log($"[Diag] Canvas '{cv.name}' enabled={cv.enabled} mode={cv.renderMode} sortOrder={cv.sortingOrder}");

        Debug.Log("[Diag] Scene roots:");
        foreach (var go in gameObject.scene.GetRootGameObjects())
            Debug.Log($"  - {go.name}");

        // Optional: on-screen “hello” proves UI path is alive
        var hud = new GameObject("TempHUD");
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hud.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        hud.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("HUDText");
        textGO.transform.SetParent(hud.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "UI Pipeline OK";
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }
}