using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DeviceSlotPositionConfig))]
public class DeviceSlotConfigEditor : Editor
{
    private Vector2 testOffset = Vector2.zero;
    private float testAspectRatio = 0.46f; // iPhone 12 Portrait default

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DeviceSlotPositionConfig config = (DeviceSlotPositionConfig)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("=== Quick Test ===", EditorStyles.boldLabel);

        // Test Aspect Ratio
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Test Aspect Ratio", GUILayout.Width(120));
        testAspectRatio = EditorGUILayout.FloatField(testAspectRatio);
        EditorGUILayout.EndHorizontal();

        // Quick preset buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("16:9 (1.78)")) testAspectRatio = 1.777778f;
        if (GUILayout.Button("iPhone 12 (0.46)")) testAspectRatio = 0.461538f;
        if (GUILayout.Button("iPad (0.75)")) testAspectRatio = 0.75f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Test button
        if (GUILayout.Button("Get Offset for This Aspect Ratio", GUILayout.Height(30)))
        {
            Vector2 result = config.GetOffsetForAspectRatio(testAspectRatio);
            testOffset = result;
            Debug.Log($"[DeviceSlotConfig] Aspect Ratio {testAspectRatio:F2} → Offset: {result}");
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox($"Result Offset: X={testOffset.x:F2}, Y={testOffset.y:F2}", MessageType.Info);

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("=== Common Aspect Ratios ===", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        ShowAspectRatioInfo("16:9 (Most phones)", 1.777778f, config);
        ShowAspectRatioInfo("18:9 (Galaxy S8, S9)", 2.0f, config);
        ShowAspectRatioInfo("19.5:9 (iPhone X-12 Landscape)", 2.166667f, config);
        ShowAspectRatioInfo("9:16 (Galaxy J7, most portrait)", 0.5625f, config);
        ShowAspectRatioInfo("9:19.5 (iPhone X-12 Portrait)", 0.461538f, config);
        ShowAspectRatioInfo("4:3 (iPad Landscape)", 1.333333f, config);
        ShowAspectRatioInfo("3:4 (iPad Portrait)", 0.75f, config);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "TIP: 에디터에서 값을 변경하면 즉시 저장됩니다.\n" +
            "런타임 테스트는 SlotPositionDebugger를 사용하세요.",
            MessageType.Info);
    }

    private void ShowAspectRatioInfo(string label, float ratio, DeviceSlotPositionConfig config)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(200));

        Vector2 offset = config.GetOffsetForAspectRatio(ratio);
        EditorGUILayout.LabelField($"Ratio: {ratio:F2}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"Offset: ({offset.x:F2}, {offset.y:F2})", GUILayout.Width(120));

        if (GUILayout.Button("Copy", GUILayout.Width(50)))
        {
            testAspectRatio = ratio;
            testOffset = offset;
            EditorGUIUtility.systemCopyBuffer = $"({offset.x:F2}, {offset.y:F2})";
            Debug.Log($"Copied offset for {label}: {offset}");
        }

        EditorGUILayout.EndHorizontal();
    }
}
