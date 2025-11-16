using UnityEngine;

/// <summary>
/// Simple script to adjust slot container position based on device aspect ratio
/// Just changes Y offset of PlayerSlots container
/// </summary>
public class DynamicSlotLayout : MonoBehaviour
{
    [Header("Offset Settings")]
    [Tooltip("Y offset for different aspect ratios")]
    public AspectRatioLayoutOffset[] layoutOffsets = new AspectRatioLayoutOffset[]
    {
        new AspectRatioLayoutOffset { aspectRatio = 1.777778f, description = "16:9", yOffset = 0 },
        new AspectRatioLayoutOffset { aspectRatio = 2.0f,       description = "18:9", yOffset = 50 },
        new AspectRatioLayoutOffset { aspectRatio = 2.166667f,  description = "19.5:9", yOffset = 100 },
        new AspectRatioLayoutOffset { aspectRatio = 2.222222f,  description = "20:9", yOffset = 120 },
        new AspectRatioLayoutOffset { aspectRatio = 1.333333f,  description = "4:3 iPad", yOffset = -50 },
        new AspectRatioLayoutOffset { aspectRatio = 0.75f,      description = "3:4 iPad", yOffset = -50 },
        new AspectRatioLayoutOffset { aspectRatio = 0.5625f,    description = "9:16 Galaxy J7", yOffset = 0 },
        new AspectRatioLayoutOffset { aspectRatio = 0.461538f,  description = "9:19.5 iPhone", yOffset = 0 },
    };

    [Header("Default")]
    public float defaultYOffset = 0f;
    public float aspectRatioTolerance = 0.05f;

    [Header("Clamp Settings")]
    [Tooltip("Y 위치를 이 범위 안으로 강제로 제한할지 여부")]
    public bool useClampY = true;
    public float minY = 50f;   
    public float maxY = 200f; 
    private RectTransform rectTransform;


    private float baseY;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseY = rectTransform.anchoredPosition.y;   // ★ 시작 위치 저장
        }
    }

    private void Start()
    {
        ApplyOffset();
    }

    private void ApplyOffset()
    {
        if (rectTransform == null) return;

    float currentAspectRatio = (float)Screen.width / Screen.height;
    float yOffset = GetYOffsetForAspectRatio(currentAspectRatio);

    // 기준 + 오프셋
    float targetY = baseY + yOffset;

    // ★ 여기서 범위 제한 적용
    if (useClampY)
        targetY = Mathf.Clamp(targetY, minY, maxY);

    // 적용
    Vector2 anchoredPos = rectTransform.anchoredPosition;
    anchoredPos.y = targetY;
    rectTransform.anchoredPosition = anchoredPos;

    Debug.Log($"[DynamicSlotLayout] Aspect: {currentAspectRatio:F2}, baseY: {baseY}, " +
              $"Y Offset applied: {yOffset}, finalY: {anchoredPos.y}");
    }

    private float GetYOffsetForAspectRatio(float aspectRatio)
    {
        foreach (var offset in layoutOffsets)
        {
            if (Mathf.Abs(aspectRatio - offset.aspectRatio) <= aspectRatioTolerance)
            {
                Debug.Log($"[DynamicSlotLayout] Matched {offset.description}");
                return offset.yOffset;
            }
        }

        Debug.LogWarning($"[DynamicSlotLayout] No match for {aspectRatio:F2}, using default");
        return defaultYOffset;
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Offset Now")]
    private void ApplyOffsetEditor()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        // 에디터에서 호출할 때도 현재 위치를 기준으로 다시 저장
        baseY = rectTransform.anchoredPosition.y;
        ApplyOffset();
    }
#endif
}

[System.Serializable]
public class AspectRatioLayoutOffset
{
    public float aspectRatio;
    public string description;
    public float yOffset;
}
