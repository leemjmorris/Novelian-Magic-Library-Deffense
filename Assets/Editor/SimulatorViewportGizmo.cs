using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class SimulatorViewportGizmo
{
    private static Vector2 currentSimulatorResolution = Vector2.zero;
    private static bool isEnabled = true;
    
    static SimulatorViewportGizmo()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    [MenuItem("Tools/Simulator Viewport Gizmo/Toggle")]
    private static void ToggleGizmo()
    {
        isEnabled = !isEnabled;
        SceneView.RepaintAll();
        Debug.Log($"Simulator Viewport Gizmo: {(isEnabled ? "Enabled" : "Disabled")}");
    }
    
    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!isEnabled) return;
        
        // Game View의 현재 해상도 가져오기
        Vector2 gameViewSize = GetGameViewSize();
        
        if (gameViewSize != currentSimulatorResolution)
        {
            currentSimulatorResolution = gameViewSize;
        }
        
        if (currentSimulatorResolution.x > 0 && currentSimulatorResolution.y > 0)
        {
            DrawViewportGizmo(sceneView);
        }
    }
    
    private static Vector2 GetGameViewSize()
    {
        // Unity의 Game View 해상도를 가져옴
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
        return (Vector2)Res;
    }
    
    private static void DrawViewportGizmo(SceneView sceneView)
    {
        // 월드 좌표 (0,0,0) 기준으로 고정된 뷰포트 그리기
        float aspectRatio = currentSimulatorResolution.x / currentSimulatorResolution.y;

        // 기본 높이 10 유닛 기준으로 계산 (필요시 조정 가능)
        float height = 10f;
        float width = height * aspectRatio;

        // 월드 좌표 (0,0,0) 중심으로 사각형 계산
        Vector3 center = Vector3.zero;

        // 뷰포트 사각형의 네 모서리 계산 (XY 평면)
        Vector3 topLeft = center + new Vector3(-width * 0.5f, height * 0.5f, 0);
        Vector3 topRight = center + new Vector3(width * 0.5f, height * 0.5f, 0);
        Vector3 bottomRight = center + new Vector3(width * 0.5f, -height * 0.5f, 0);
        Vector3 bottomLeft = center + new Vector3(-width * 0.5f, -height * 0.5f, 0);

        // Gizmo 그리기
        Color gizmoColor = new Color(0f, 1f, 1f, 0.8f); // 시안색
        Handles.color = gizmoColor;

        // 테두리 그리기
        Handles.DrawLine(topLeft, topRight, 3f);
        Handles.DrawLine(topRight, bottomRight, 3f);
        Handles.DrawLine(bottomRight, bottomLeft, 3f);
        Handles.DrawLine(bottomLeft, topLeft, 3f);

        // 반투명 면 그리기
        Color fillColor = new Color(0f, 1f, 1f, 0.1f);
        Handles.DrawSolidRectangleWithOutline(
            new Vector3[] { topLeft, topRight, bottomRight, bottomLeft },
            fillColor,
            gizmoColor
        );

        // 해상도 텍스트 표시
        Handles.BeginGUI();
        Vector2 labelPos = HandleUtility.WorldToGUIPoint(topLeft);
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.cyan;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;

        string resolutionText = $"{(int)currentSimulatorResolution.x} x {(int)currentSimulatorResolution.y}";
        string aspectText = $"({aspectRatio:F2}:1)";
        Vector2 textSize = style.CalcSize(new GUIContent(resolutionText));

        GUI.Label(new Rect(labelPos.x, labelPos.y - 40, textSize.x + 100, 20), resolutionText, style);
        style.fontSize = 11;
        GUI.Label(new Rect(labelPos.x, labelPos.y - 22, textSize.x + 100, 20), aspectText, style);

        Handles.EndGUI();

        // Scene View 지속적으로 다시 그리기
        sceneView.Repaint();
    }
}