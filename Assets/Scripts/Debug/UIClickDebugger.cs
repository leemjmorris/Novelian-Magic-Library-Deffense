using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;

/// <summary>
/// UI 클릭 문제 디버깅용 스크립트
/// LibraryManagementScene에 배치하여 문제 원인 파악
/// 문제 해결 후 삭제할 것
/// </summary>
public class UIClickDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebug = true;
    [SerializeField] private float checkInterval = 1f;

    private float nextCheckTime;

    private void Start()
    {
        if (!enableDebug) return;

        Debug.Log("========== [UIClickDebugger] 시작 ==========");

        // 즉시 체크
        CheckAllPotentialIssues();
    }

    private void Update()
    {
        if (!enableDebug) return;

        // 주기적 체크
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            CheckAllPotentialIssues();
        }

        // 클릭 시 어떤 UI가 클릭되는지 확인
        if (Input.GetMouseButtonDown(0))
        {
            CheckClickTarget();
        }
    }

    private void CheckAllPotentialIssues()
    {
        Debug.Log("---------- [UIClickDebugger] 상태 체크 ----------");

        // 1. FadeController 상태 체크
        CheckFadeController();

        // 2. EventSystem 체크
        CheckEventSystem();

        // 3. RaycastBlocker 패널들 체크
        CheckRaycastBlockers();

        // 4. CanvasGroup 체크
        CheckCanvasGroups();
    }

    private void CheckFadeController()
    {
        if (FadeController.Instance != null)
        {
            bool panelActive = FadeController.Instance.fadePanel != null &&
                               FadeController.Instance.fadePanel.activeSelf;
            float alpha = FadeController.Instance.fadeImage != null ?
                          FadeController.Instance.fadeImage.color.a : -1;
            bool raycastTarget = FadeController.Instance.fadeImage != null &&
                                 FadeController.Instance.fadeImage.raycastTarget;

            if (panelActive)
            {
                Debug.LogWarning($"[UIClickDebugger] ⚠️ FadeController.fadePanel이 활성화됨! " +
                    $"alpha={alpha:F2}, raycastTarget={raycastTarget}");

                if (raycastTarget && alpha > 0.01f)
                {
                    Debug.LogError("[UIClickDebugger] ❌ FadePanel이 UI 클릭을 차단하고 있습니다!");
                }
            }
            else
            {
                Debug.Log("[UIClickDebugger] ✅ FadeController.fadePanel 비활성화 상태");
            }
        }
        else
        {
            Debug.Log("[UIClickDebugger] FadeController.Instance가 null");
        }
    }

    private void CheckEventSystem()
    {
        var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (eventSystems.Length == 0)
        {
            Debug.LogError("[UIClickDebugger] ❌ EventSystem이 없습니다! UI 입력 불가");
        }
        else if (eventSystems.Length > 1)
        {
            Debug.LogWarning($"[UIClickDebugger] ⚠️ EventSystem이 {eventSystems.Length}개 존재합니다!");
            foreach (var es in eventSystems)
            {
                Debug.LogWarning($"  - {es.gameObject.name} (scene: {es.gameObject.scene.name}, enabled: {es.enabled})");
            }
        }
        else
        {
            var es = eventSystems[0];
            if (!es.enabled)
            {
                Debug.LogError("[UIClickDebugger] ❌ EventSystem이 비활성화되어 있습니다!");
            }
            else
            {
                Debug.Log($"[UIClickDebugger] ✅ EventSystem 정상 (name: {es.gameObject.name})");
            }
        }

        // Current Selected 확인
        if (EventSystem.current != null)
        {
            Debug.Log($"[UIClickDebugger] EventSystem.current: {EventSystem.current.name}");
        }
    }

    private void CheckRaycastBlockers()
    {
        // 이름에 "Raycast" 또는 "Blocker"가 포함된 활성화된 오브젝트 찾기
        var allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);

        foreach (var image in allImages)
        {
            string name = image.gameObject.name.ToLower();
            if ((name.Contains("raycast") || name.Contains("blocker")) &&
                image.gameObject.activeInHierarchy &&
                image.raycastTarget)
            {
                // 투명도 확인
                if (image.color.a < 0.1f || image.sprite == null)
                {
                    Debug.LogWarning($"[UIClickDebugger] ⚠️ 투명 RaycastBlocker 활성화: {image.gameObject.name} " +
                        $"(alpha={image.color.a:F2}, raycastTarget={image.raycastTarget})");
                }
            }
        }

        // raycastPanel 이름으로 찾기
        var raycastPanels = GameObject.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        foreach (var rt in raycastPanels)
        {
            if (rt.gameObject.name.ToLower().Contains("raycastpanel") ||
                rt.gameObject.name.ToLower().Contains("raycast panel") ||
                rt.gameObject.name.ToLower().Contains("raycastblocker"))
            {
                if (rt.gameObject.activeInHierarchy)
                {
                    var img = rt.GetComponent<Image>();
                    if (img != null && img.raycastTarget)
                    {
                        Debug.LogWarning($"[UIClickDebugger] ⚠️ RaycastPanel 활성화: {rt.gameObject.name}");
                    }
                }
            }
        }
    }

    private void CheckCanvasGroups()
    {
        var canvasGroups = FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);

        foreach (var cg in canvasGroups)
        {
            if (cg.gameObject.activeInHierarchy)
            {
                if (!cg.interactable)
                {
                    Debug.LogWarning($"[UIClickDebugger] ⚠️ CanvasGroup interactable=false: {cg.gameObject.name}");
                }
                if (!cg.blocksRaycasts)
                {
                    Debug.Log($"[UIClickDebugger] CanvasGroup blocksRaycasts=false: {cg.gameObject.name}");
                }
            }
        }
    }

    private void CheckClickTarget()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("[UIClickDebugger] ❌ 클릭 시 EventSystem.current가 null!");
            return;
        }

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            Debug.Log("[UIClickDebugger] 클릭 위치에 UI 없음");
        }
        else
        {
            Debug.Log($"[UIClickDebugger] 클릭 위치의 UI ({results.Count}개):");
            for (int i = 0; i < Mathf.Min(results.Count, 5); i++)
            {
                var result = results[i];
                Debug.Log($"  [{i}] {result.gameObject.name} (depth={result.depth}, sortingOrder={result.sortingOrder})");
            }

            // 최상위 요소가 blocker인지 확인
            var topResult = results[0];
            string topName = topResult.gameObject.name.ToLower();
            if (topName.Contains("fade") || topName.Contains("blocker") || topName.Contains("raycast"))
            {
                Debug.LogError($"[UIClickDebugger] ❌ 클릭이 {topResult.gameObject.name}에 의해 차단됨!");
            }
        }
    }
}
