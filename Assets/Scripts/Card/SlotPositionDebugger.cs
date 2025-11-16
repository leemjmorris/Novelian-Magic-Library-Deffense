using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime debugger for adjusting slot positions
/// Shows UI controls to adjust offset in real-time
/// </summary>
public class SlotPositionDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DynamicSlotPositionManager positionManager;
    [SerializeField] private PlayerSlot[] testSlots;

    [Header("Debug Controls")]
    [SerializeField] private bool showDebugUI = true;

    [Header("Offset Adjustment")]
    [SerializeField] private Vector2 testOffset = Vector2.zero;
    [SerializeField] private float offsetStep = 0.1f;
    [SerializeField] private bool applyOffsetNow = false;
    [SerializeField] private bool resetOffsetNow = false;

    private bool debugUIVisible = false;
    private Rect windowRect = new Rect(20, 100, 450, 600);
    private GUIStyle largeButtonStyle;
    private GUIStyle largeLabelStyle;

    private void Start()
    {
        if (positionManager == null)
        {
            positionManager = FindFirstObjectByType<DynamicSlotPositionManager>();
        }

        if (testSlots == null || testSlots.Length == 0)
        {
            testSlots = FindObjectsOfType<PlayerSlot>();
        }

        debugUIVisible = showDebugUI;
    }

    private void Update()
    {
        // Inspector controls
        if (applyOffsetNow)
        {
            applyOffsetNow = false;
            ApplyOffset();
        }

        if (resetOffsetNow)
        {
            resetOffsetNow = false;
            testOffset = Vector2.zero;
            ApplyOffset();
        }
    }

    private void OnGUI()
    {
        if (!debugUIVisible) return;

        // Initialize styles
        if (largeButtonStyle == null)
        {
            largeButtonStyle = new GUIStyle(GUI.skin.button);
            largeButtonStyle.fontSize = 18;
            largeButtonStyle.padding = new RectOffset(10, 10, 10, 10);
        }

        if (largeLabelStyle == null)
        {
            largeLabelStyle = new GUIStyle(GUI.skin.label);
            largeLabelStyle.fontSize = 16;
        }

        // Make sure GUI is rendered on top
        GUI.depth = -1000;
        windowRect = GUI.Window(0, windowRect, DrawDebugWindow, "Slot Position Debugger", new GUIStyle(GUI.skin.window) { fontSize = 20 });
    }

    private void DrawDebugWindow(int windowID)
    {
        GUILayout.BeginVertical();

        // Device Info
        GUILayout.Label("=== Device Info ===", new GUIStyle(GUI.skin.box) { fontSize = 16 });
        if (positionManager != null)
        {
            GUILayout.Label($"Resolution: {Screen.width}x{Screen.height}", largeLabelStyle);
            GUILayout.Label($"Aspect: {((float)Screen.width / Screen.height):F2}", largeLabelStyle);
            GUILayout.Label($"Offset: {positionManager.GetCurrentOffset()}", largeLabelStyle);
        }

        GUILayout.Space(15);

        // Offset Controls
        GUILayout.Label("=== Offset Adjustment ===", new GUIStyle(GUI.skin.box) { fontSize = 16 });

        GUILayout.BeginHorizontal();
        GUILayout.Label($"X: {testOffset.x:F2}", largeLabelStyle, GUILayout.Width(100));
        if (GUILayout.Button("-", largeButtonStyle, GUILayout.Width(60), GUILayout.Height(40)))
        {
            testOffset.x -= offsetStep;
            ApplyOffset();
        }
        if (GUILayout.Button("+", largeButtonStyle, GUILayout.Width(60), GUILayout.Height(40)))
        {
            testOffset.x += offsetStep;
            ApplyOffset();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Y: {testOffset.y:F2}", largeLabelStyle, GUILayout.Width(100));
        if (GUILayout.Button("-", largeButtonStyle, GUILayout.Width(60), GUILayout.Height(40)))
        {
            testOffset.y -= offsetStep;
            ApplyOffset();
        }
        if (GUILayout.Button("+", largeButtonStyle, GUILayout.Width(60), GUILayout.Height(40)))
        {
            testOffset.y += offsetStep;
            ApplyOffset();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Step size
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Step: {offsetStep:F2}", largeLabelStyle);
        if (GUILayout.Button("0.01", largeButtonStyle, GUILayout.Width(70))) offsetStep = 0.01f;
        if (GUILayout.Button("0.1", largeButtonStyle, GUILayout.Width(70))) offsetStep = 0.1f;
        if (GUILayout.Button("0.5", largeButtonStyle, GUILayout.Width(70))) offsetStep = 0.5f;
        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        // Action Buttons
        if (GUILayout.Button("Reset Offset", largeButtonStyle, GUILayout.Height(45)))
        {
            testOffset = Vector2.zero;
            ApplyOffset();
        }

        if (GUILayout.Button("Apply Offset", largeButtonStyle, GUILayout.Height(45)))
        {
            ApplyOffset();
        }

        if (GUILayout.Button("Recalculate Positions", largeButtonStyle, GUILayout.Height(45)))
        {
            RecalculateAllSlots();
        }

        GUILayout.Space(15);

        // Copy to Clipboard
        if (GUILayout.Button("Copy to Clipboard", largeButtonStyle, GUILayout.Height(45)))
        {
            string offsetText = $"X={testOffset.x:F2}, Y={testOffset.y:F2}";
            GUIUtility.systemCopyBuffer = offsetText;
            Debug.Log($"Copied: {offsetText}");
        }

        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    private void ApplyOffset()
    {
        if (positionManager != null)
        {
            positionManager.UpdateOffset(testOffset);
            Debug.Log($"[SlotPositionDebugger] Applied offset: {testOffset}");
        }
    }

    private void RecalculateAllSlots()
    {
        if (positionManager == null || testSlots == null) return;

        positionManager.RecalculateAllPositions(testSlots);
        Debug.Log($"[SlotPositionDebugger] Recalculated {testSlots.Length} slot positions");
    }
}
