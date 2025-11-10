
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SafeAreaCreator
{
    [MenuItem("GameObject/UI/Safe Area Panel", false, 0)]
    public static void CreateSafeAreaPanel()
    {
        // CBL: Find or create a canvas
        Canvas canvas = null;
        if (Selection.activeTransform != null)
            canvas = Selection.activeTransform.GetComponentInParent<Canvas>();
        if (!canvas)
            canvas = Object.FindFirstObjectByType<Canvas>();

        if (!canvas)
        {
            // New Canvas + Scaler recommended settings
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400); // Vertical standard (20:9)
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem <-if not, create
            if (!Object.FindFirstObjectByType<EventSystem>())
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }
        }

        // 2) SafeArea panel duplication check
        var existing = canvas.transform.Find("SafeArea");
        if (existing != null)
        {
            Selection.activeTransform = existing;
            EditorUtility.DisplayDialog("Safe Area", "이미 SafeArea 패널이 있습니다.", "OK");
            return;
        }

        // 3) SafeArea panel Create
        var go = new GameObject("SafeArea", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create SafeArea Panel");
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // 4) Paste runtime script 

        var safeAreaType = typeof(global::SafeArea);
        go.AddComponent(safeAreaType);

        // 5) 선택 포커스
        Selection.activeGameObject = go;

        // 6) 안내
        EditorUtility.DisplayDialog(
            "Safe Area",
            "SafeArea 패널을 만들었어요!\n" +
            "\n이제 모든 UI를 SafeArea 자식으로 배치하면 노치/제스처 영역을 자동으로 회피합니다.",
            "OK"
        );
    }
}
#endif
