using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// UI Raycast 디버깅용 스크립트
/// EventSystem 오브젝트에 추가하여 사용
/// 클릭 시 raycast에 감지되는 모든 UI 오브젝트를 콘솔에 출력
/// </summary>
public class RaycastDebugger : MonoBehaviour
{
    void Update()
    {
        // New Input System 사용
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            GameLog.Log($"=== 클릭 위치: {mousePosition} ===");
            GameLog.Log($"Raycast 결과 개수: {results.Count}");

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                string path = GetGameObjectPath(result.gameObject);
                GameLog.Log($"[{i}] {result.gameObject.name} (depth: {result.depth}) - Path: {path}");
            }

            if (results.Count == 0)
            {
                GameLog.LogWarning("Raycast 결과 없음 - UI 요소가 감지되지 않았습니다.");
            }
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
