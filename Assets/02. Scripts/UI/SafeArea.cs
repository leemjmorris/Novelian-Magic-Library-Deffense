using UnityEngine;

/// <summary>
/// UI를 디바이스 SafeArea만큼 조정하는 스크립트
/// 방향별로 선택적으로 SafeArea를 적용할 수 있음
/// </summary>
public class SafeArea : MonoBehaviour
{
    [Header("SafeArea 적용 방향 선택")]
    [SerializeField] private bool applyTop = true;
    [SerializeField] private bool applyBottom = true;
    [SerializeField] private bool applyLeft = true;
    [SerializeField] private bool applyRight = true;

    private RectTransform safeAreaRect;
    private Canvas canvas;
    private Rect lastSafeArea;

    void Start()
    {
        safeAreaRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        OnRectTransformDimensionsChange();
    }

    private void OnRectTransformDimensionsChange() // 화면이 바뀔때 자동으로 호출되는 이벤트 함수
    {
        if (GetSafeArea() != lastSafeArea && canvas != null)
        {
            lastSafeArea = GetSafeArea();
            UpdateSizeToSafeArea();
        }
    }

    private void UpdateSizeToSafeArea()
    {
        var safeArea = GetSafeArea();
        var canvasRect = canvas.pixelRect;
        var inverseSize = new Vector2(1f, 1f) / canvasRect.size; // 0.0에서 1.0까지 정규화된 값으로 변환

        // SafeArea 좌표를 정규화
        var safeAreaMin = Vector2.Scale(safeArea.position, inverseSize);
        var safeAreaMax = Vector2.Scale(safeArea.position + safeArea.size, inverseSize);

        // 현재 앵커 값 가져오기 (기본값은 전체 화면)
        Vector2 newAnchorMin = new Vector2(0f, 0f);
        Vector2 newAnchorMax = new Vector2(1f, 1f);

        // 선택적으로 SafeArea 적용
        if (applyLeft)
        {
            newAnchorMin.x = safeAreaMin.x;
        }
        if (applyRight)
        {
            newAnchorMax.x = safeAreaMax.x;
        }
        if (applyBottom)
        {
            newAnchorMin.y = safeAreaMin.y;
        }
        if (applyTop)
        {
            newAnchorMax.y = safeAreaMax.y;
        }

        // 앵커 정규화 -> 앵커는 부모 기준 비율
        safeAreaRect.anchorMin = newAnchorMin;
        safeAreaRect.anchorMax = newAnchorMax;

        safeAreaRect.offsetMin = Vector2.zero;
        safeAreaRect.offsetMax = Vector2.zero;
    }

    private Rect GetSafeArea()
    {
        return Screen.safeArea;
    }
}
