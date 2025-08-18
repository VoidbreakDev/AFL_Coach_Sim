#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UObject = UnityEngine.Object;
using UnityEngine.UI;
using TMPro;

// Your runtime view scripts:
using System; // for String
// If you put LadderTableView/FixtureListView in a namespace, add it here.
// using AFLCoachSim.UI; // example

public static partial class AFL_UIPrefabBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs/UI";

    [MenuItem("AFL Coach Sim/Build UI Prefab Pack")]
    public static void BuildUIPrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabsFolder);

        // Row prefabs
        var ladderRow = CreateLadderRowPrefab($"{PrefabsFolder}/LadderRow.prefab");
        var fixtureRow = CreateFixtureRowPrefab($"{PrefabsFolder}/FixtureRow.prefab");

        // Panels
        var leftPanel = CreateLeftPanelPrefab($"{PrefabsFolder}/LeftPanel_Ladder.prefab", ladderRow);
        var rightPanel = CreateRightPanelPrefab($"{PrefabsFolder}/RightPanel_Fixtures.prefab", fixtureRow);

        // SeasonView (two-panels side-by-side)
        var seasonView = CreateSeasonViewPrefab($"{PrefabsFolder}/SeasonView.prefab", leftPanel, rightPanel);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AFL UIPrefabBuilder] Created prefabs:\n" +
                  $"- {AssetDatabase.GetAssetPath(ladderRow)}\n" +
                  $"- {AssetDatabase.GetAssetPath(fixtureRow)}\n" +
                  $"- {AssetDatabase.GetAssetPath(leftPanel)}\n" +
                  $"- {AssetDatabase.GetAssetPath(rightPanel)}\n" +
                  $"- {AssetDatabase.GetAssetPath(seasonView)}");
    }

    // --- Helpers ---

    private static void EnsureFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            var parent = Path.GetDirectoryName(folder).Replace("\\", "/");
            var name = Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static GameObject SaveNewPrefab(GameObject go, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        GameObject.DestroyImmediate(go);
        return prefab;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetTextDefaults(TextMeshProUGUI tmp, string txt, bool autosize = true, float min = 12, float max = 20, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        tmp.text = txt;
        tmp.enableAutoSizing = autosize;
        tmp.fontSizeMin = min;
        tmp.fontSizeMax = max;
        tmp.alignment = align;
        tmp.overflowMode = TextOverflowModes.Truncate;
        var rt = tmp.rectTransform;
        Stretch(rt);
    }

    private static HorizontalLayoutGroup AddHLayout(GameObject go, float spacing = 8, bool expandWidth = true, bool expandHeight = false)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.childForceExpandWidth = expandWidth;
        h.childForceExpandHeight = expandHeight;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.spacing = spacing;
        return h;
    }

    private static VerticalLayoutGroup AddVLayout(GameObject go, float spacing = 6)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.spacing = spacing;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return v;
    }

    private static GameObject CreateLadderRowPrefab(string path)
    {
        var root = new GameObject("LadderRow", typeof(RectTransform));
        var rt = root.GetComponent<RectTransform>();
        Stretch(rt);

        AddHLayout(root, 8, true, false);
        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 28;

        var nameGO = new GameObject("NameText", typeof(RectTransform));
        nameGO.transform.SetParent(root.transform, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        SetTextDefaults(nameTMP, "1. Team Name", true, 12, 20, TextAlignmentOptions.Left);

        var detailGO = new GameObject("DetailText", typeof(RectTransform));
        detailGO.transform.SetParent(root.transform, false);
        var detailTMP = detailGO.AddComponent<TextMeshProUGUI>();
        SetTextDefaults(detailTMP, "Pts:0  W:0 L:0 D:0  PF:0  PA:0  %:0.00", true, 10, 18, TextAlignmentOptions.Right);

        return SaveNewPrefab(root, path);
    }

    private static GameObject CreateFixtureRowPrefab(string path)
    {
        var root = new GameObject("FixtureRow", typeof(RectTransform));
        var rt = root.GetComponent<RectTransform>();
        Stretch(rt);

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 28;

        var textGO = new GameObject("LineText", typeof(RectTransform));
        textGO.transform.SetParent(root.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        SetTextDefaults(tmp, "R1: Home 80 – 75 Away", true, 12, 20, TextAlignmentOptions.Left);

        return SaveNewPrefab(root, path);
    }

    private static Transform CreateScrollView(Transform parent, string name, out Transform content)
    {
        // ScrollView root
        var svGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        svGO.transform.SetParent(parent, false);
        var svRT = svGO.GetComponent<RectTransform>();
        Stretch(svRT);
        var svImg = svGO.GetComponent<Image>();
        svImg.color = new Color(0, 0, 0, 0.05f);

        // Viewport
        var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGO.transform.SetParent(svGO.transform, false);
        var vpRT = vpGO.GetComponent<RectTransform>();
        Stretch(vpRT);
        var vpImg = vpGO.GetComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.001f); // needed for Mask
        var mask = vpGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        Stretch(contentRT);
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);

        var vlayout = AddVLayout(contentGO, 6);
        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect wiring
        var sr = svGO.GetComponent<ScrollRect>();
        sr.viewport = vpRT;
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        content = contentRT;
        return svRT;
    }

    private static GameObject CreateLeftPanelPrefab(string path, GameObject ladderRowPrefab)
    {
        var root = new GameObject("LeftPanel (Ladder)", typeof(RectTransform), typeof(Image));
        var rt = root.GetComponent<RectTransform>();
        Stretch(rt);
        var img = root.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.17f, 0.35f);

        var le = root.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;

        // Scroll view
        var sv = CreateScrollView(root.transform, "LadderScroll", out var content);

        // Add LadderTableView on the panel (or on scroll root if you prefer)
        var view = root.AddComponent<LadderTableView>();
        view.contentRoot = content;
        view.rowPrefab = ladderRowPrefab;

        return SaveNewPrefab(root, path);
    }

    private static GameObject CreateRightPanelPrefab(string path, GameObject fixtureRowPrefab)
    {
        var root = new GameObject("RightPanel (Fixtures)", typeof(RectTransform), typeof(Image));
        var rt = root.GetComponent<RectTransform>();
        Stretch(rt);
        var img = root.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.17f, 0.35f);

        var le = root.AddComponent<LayoutElement>();
        le.flexibleWidth = 2; // wider

        // Scroll view
        var sv = CreateScrollView(root.transform, "FixtureScroll", out var content);

        // Add FixtureListView
        var view = root.AddComponent<FixtureListView>();
        view.contentRoot = content;
        view.rowPrefab = fixtureRowPrefab;
        view.showCount = 30;

        return SaveNewPrefab(root, path);
    }

    private static GameObject CreateSeasonViewPrefab(string path, GameObject leftPanelPrefab, GameObject rightPanelPrefab)
    {
        var root = new GameObject("SeasonView", typeof(RectTransform));
        Stretch(root.GetComponent<RectTransform>());

        // Horizontal layout to hold both panels
        var h = AddHLayout(root, 16, true, true);

        // Instantiate the panels and then save as a single prefab
        var leftInst = (GameObject)PrefabUtility.InstantiatePrefab(leftPanelPrefab);
        leftInst.transform.SetParent(root.transform, false);

        var rightInst = (GameObject)PrefabUtility.InstantiatePrefab(rightPanelPrefab);
        rightInst.transform.SetParent(root.transform, false);

        return SaveNewPrefab(root, path);
    }
}
public static partial class AFL_UIPrefabBuilder
{
    [MenuItem("AFL Coach Sim/Create Main Canvas Skeleton")]
    public static void CreateMainCanvasSkeleton()
    {
        // 0) Make sure prefab pack exists
        var seasonViewPath = "Assets/Prefabs/UI/SeasonView.prefab";
        var seasonViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(seasonViewPath);
        if (seasonViewPrefab == null)
        {
            if (EditorUtility.DisplayDialog(
                "Prefab Pack Missing",
                "SeasonView.prefab was not found. Build the UI prefab pack now?",
                "Build Now", "Cancel"))
            {
                BuildUIPrefabs();
                seasonViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(seasonViewPath);
                if (seasonViewPrefab == null)
                {
                    Debug.LogError("[AFL] Could not find or build SeasonView.prefab. Aborting.");
                    return;
                }
            }
            else return;
        }

        // 1) Create/ensure EventSystem
        if (UObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2) Create a single main Canvas
        var canvasGO = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 3) Root layout (horizontal splitter)
        var root = new GameObject("RootLayout", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        root.transform.SetParent(canvasGO.transform, false);
        Stretch(root.GetComponent<RectTransform>());
        var hlg = root.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.UpperLeft;

        // 4) Left Rail placeholder
        var leftRail = new GameObject("LeftRail", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        leftRail.transform.SetParent(root.transform, false);
        var lrImg = leftRail.GetComponent<Image>(); lrImg.color = new Color(0.12f, 0.12f, 0.12f, 0.5f);
        var lrVel = leftRail.GetComponent<LayoutElement>(); lrVel.preferredWidth = 280; lrVel.flexibleWidth = 0;
        var lrV = leftRail.GetComponent<VerticalLayoutGroup>();
        lrV.childAlignment = TextAnchor.UpperCenter; lrV.spacing = 8; lrV.childForceExpandWidth = true;

        // 5) Content area (where screens live)
        var contentArea = new GameObject("ContentArea", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        contentArea.transform.SetParent(root.transform, false);
        var caImg = contentArea.GetComponent<Image>(); caImg.color = new Color(0, 0, 0, 0); // transparent
        var caEL = contentArea.GetComponent<LayoutElement>(); caEL.flexibleWidth = 1;

        // 6) DashboardGrid placeholder (you can move your existing dashboard children here)
        var dashboard = new GameObject("DashboardGrid", typeof(RectTransform), typeof(VerticalLayoutGroup));
        dashboard.transform.SetParent(contentArea.transform, false);
        var dV = dashboard.GetComponent<VerticalLayoutGroup>();
        dV.spacing = 10; dV.childControlWidth = true; dV.childControlHeight = true; dV.childForceExpandWidth = true;

        // 7) Instantiate SeasonView (ladder + fixtures) under ContentArea, disabled by default
        var seasonViewInstance = (GameObject)PrefabUtility.InstantiatePrefab(seasonViewPrefab);
        seasonViewInstance.name = "SeasonView";
        seasonViewInstance.transform.SetParent(contentArea.transform, false);
        seasonViewInstance.SetActive(false); // start hidden; use a button to show

        // 8) Add a ViewSwitcher and wire it
        var switcher = contentArea.AddComponent<ViewSwitcher>();
        switcher.dashboardGrid = dashboard;
        switcher.seasonView = seasonViewInstance;

        // 9) Select the canvas and mark scene dirty for save
        Selection.activeObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);

        Debug.Log("[AFL] Main Canvas Skeleton created.\n" +
                  "- LeftRail placeholder (drop your buttons here)\n" +
                  "- ContentArea with DashboardGrid & SeasonView\n" +
                  "- Use ViewSwitcher on ContentArea to toggle views.");
    }
        [MenuItem("AFL Coach Sim/Build Scene Starter Prefab")]
    public static void BuildSceneStarterPrefab()
    {
        // Ensure prefab folders exist
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/UI");

        // Make sure SeasonView exists (build pack if missing)
        var seasonViewPath = "Assets/Prefabs/UI/SeasonView.prefab";
        var seasonViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(seasonViewPath);
        if (seasonViewPrefab == null)
        {
            if (EditorUtility.DisplayDialog(
                "Prefab Pack Missing",
                "SeasonView.prefab was not found. Build the UI prefab pack now?",
                "Build Now", "Cancel"))
            {
                BuildUIPrefabs();
                seasonViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(seasonViewPath);
                if (seasonViewPrefab == null)
                {
                    Debug.LogError("[AFL] Could not find or build SeasonView.prefab. Aborting.");
                    return;
                }
            }
            else return;
        }

        // Root that will be saved as a prefab
        var root = new GameObject("SceneStarterRoot", typeof(RectTransform));

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.transform.SetParent(root.transform, false);
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // MainCanvas
        var canvasGO = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(root.transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // RootLayout
        var rootLayout = new GameObject("RootLayout", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rootLayout.transform.SetParent(canvasGO.transform, false);
        Stretch(rootLayout.GetComponent<RectTransform>());
        var hlg = rootLayout.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.UpperLeft;

        // LeftRail
        var leftRail = new GameObject("LeftRail", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        leftRail.transform.SetParent(rootLayout.transform, false);
        var lrImg = leftRail.GetComponent<Image>(); lrImg.color = new Color(0.12f, 0.12f, 0.12f, 0.5f);
        var lrVel = leftRail.GetComponent<LayoutElement>(); lrVel.preferredWidth = 280; lrVel.flexibleWidth = 0;
        var lrV = leftRail.GetComponent<VerticalLayoutGroup>();
        lrV.childAlignment = TextAnchor.UpperCenter; lrV.spacing = 8; lrV.childForceExpandWidth = true;

        // ContentArea
        var contentArea = new GameObject("ContentArea", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        contentArea.transform.SetParent(rootLayout.transform, false);
        var caImg = contentArea.GetComponent<Image>(); caImg.color = new Color(0, 0, 0, 0); // transparent
        var caLE = contentArea.GetComponent<LayoutElement>(); caLE.flexibleWidth = 1;

        // DashboardGrid placeholder
        var dashboard = new GameObject("DashboardGrid", typeof(RectTransform), typeof(VerticalLayoutGroup));
        dashboard.transform.SetParent(contentArea.transform, false);
        var dV = dashboard.GetComponent<VerticalLayoutGroup>();
        dV.spacing = 10; dV.childControlWidth = true; dV.childControlHeight = true; dV.childForceExpandWidth = true;

        // SeasonView (instantiate prefab and disable by default)
        var seasonViewInstance = (GameObject)PrefabUtility.InstantiatePrefab(seasonViewPrefab);
        seasonViewInstance.name = "SeasonView";
        seasonViewInstance.transform.SetParent(contentArea.transform, false);
        seasonViewInstance.SetActive(false);

        // ViewSwitcher on ContentArea
        var switcher = contentArea.AddComponent<ViewSwitcher>();
        switcher.dashboardGrid = dashboard;
        switcher.seasonView = seasonViewInstance;

        // Save prefab
        var prefabPath = "Assets/Prefabs/UI/SceneStarter.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) AssetDatabase.DeleteAsset(prefabPath);
        var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AFL] Scene Starter prefab created at: {prefabPath}");
    }

}
#endif