using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dynamically calculates and manages slot positions for all devices
/// Uses raycast to virtual plane for accurate world position calculation
/// </summary>
public class DynamicSlotPositionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas slotCanvas;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private DeviceSlotPositionConfig deviceConfig;

    [Header("Virtual Plane Settings")]
    [SerializeField] private bool showDebugPlane = false;
    [SerializeField] private Material debugPlaneMaterial;

    [Header("Runtime Info")]
    [SerializeField] private float currentAspectRatio;
    [SerializeField] private Vector2 currentOffset;
    [SerializeField] private float currentScreenScale;

    private GameObject virtualPlane;
    private Plane calculationPlane;
    private Dictionary<int, Vector3> slotWorldPositions = new Dictionary<int, Vector3>();

    private void Awake()
    {
        InitializeCameras();
        InitializeVirtualPlane();
        CalculateDeviceParameters();

        // Auto-add debugger if not present
        if (FindFirstObjectByType<SlotPositionDebugger>() == null)
        {
            gameObject.AddComponent<SlotPositionDebugger>();
            Debug.Log("[DynamicSlotPositionManager] Auto-added SlotPositionDebugger component");
        }
    }

    private void InitializeCameras()
    {
        // Auto-assign cameras if not set
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (slotCanvas == null)
        {
            slotCanvas = GetComponentInParent<Canvas>();
        }

        if (uiCamera == null && slotCanvas != null)
        {
            uiCamera = slotCanvas.worldCamera;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[DynamicSlotPositionManager] Main Camera not found!");
        }

        if (uiCamera == null)
        {
            Debug.LogError("[DynamicSlotPositionManager] UI Camera not found!");
        }
    }

    private void InitializeVirtualPlane()
    {
        if (deviceConfig == null)
        {
            Debug.LogError("[DynamicSlotPositionManager] DeviceSlotPositionConfig is not assigned!");
            return;
        }

        // Create virtual plane at target Z position
        Vector3 planePosition = new Vector3(0, 0, deviceConfig.baseZPosition);
        Vector3 planeNormal = Vector3.back; // Plane faces the camera

        calculationPlane = new Plane(planeNormal, planePosition);

        // Create visual debug plane (optional)
        if (showDebugPlane)
        {
            CreateDebugPlane(planePosition, planeNormal);
        }

        Debug.Log($"[DynamicSlotPositionManager] Virtual plane created at Z={deviceConfig.baseZPosition}");
    }

    private void CreateDebugPlane(Vector3 position, Vector3 normal)
    {
        virtualPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        virtualPlane.name = "VirtualPlane_Debug";
        virtualPlane.transform.position = position;
        virtualPlane.transform.rotation = Quaternion.LookRotation(normal);
        virtualPlane.transform.localScale = new Vector3(5, 1, 5);

        // Make it semi-transparent blue
        if (debugPlaneMaterial != null)
        {
            virtualPlane.GetComponent<Renderer>().material = debugPlaneMaterial;
        }
        else
        {
            Material mat = virtualPlane.GetComponent<Renderer>().material;
            mat.color = new Color(0, 0.5f, 1f, 0.3f);
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        // Remove collider (we don't need physics)
        Destroy(virtualPlane.GetComponent<Collider>());
    }

    private void CalculateDeviceParameters()
    {
        if (deviceConfig == null) return;

        currentAspectRatio = (float)Screen.width / Screen.height;
        currentOffset = deviceConfig.GetOffsetForCurrentDevice();
        currentScreenScale = deviceConfig.GetScreenSizeScale();

        Debug.Log($"[DynamicSlotPositionManager] Device Info - " +
                  $"Resolution: {Screen.width}x{Screen.height}, " +
                  $"Aspect Ratio: {currentAspectRatio:F2}, " +
                  $"Offset: {currentOffset}, " +
                  $"Scale: {currentScreenScale:F2}");
    }

    /// <summary>
    /// Update offset in runtime (for debugging and testing)
    /// </summary>
    public void UpdateOffset(Vector2 newOffset)
    {
        currentOffset = newOffset;
        slotWorldPositions.Clear(); // Clear cache to recalculate
        Debug.Log($"[DynamicSlotPositionManager] Offset updated to: {currentOffset}");
    }

    /// <summary>
    /// Recalculate device parameters (call when screen changes)
    /// </summary>
    public void RefreshDeviceParameters()
    {
        CalculateDeviceParameters();
        slotWorldPositions.Clear();
    }

    /// <summary>
    /// Calculate world position for a UI slot using raycast to virtual plane
    /// </summary>
    public Vector3 CalculateWorldPositionForSlot(RectTransform slotRectTransform)
    {
        if (slotRectTransform == null || uiCamera == null || mainCamera == null)
        {
            Debug.LogWarning("[DynamicSlotPositionManager] Missing required components!");
            return Vector3.zero;
        }

        // Step 1: Get screen position of UI element
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, slotRectTransform.position);

        // Step 2: Create ray from main camera through screen position
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // Step 3: Find intersection with virtual plane
        float enter;
        Vector3 worldPosition = Vector3.zero;

        if (calculationPlane.Raycast(ray, out enter))
        {
            // Get the intersection point
            worldPosition = ray.GetPoint(enter);

            // Step 4: Apply device-specific offset
            worldPosition += new Vector3(currentOffset.x, currentOffset.y, 0);

            Debug.Log($"[DynamicSlotPositionManager] Raycast Hit - " +
                      $"Screen: {screenPos}, " +
                      $"Ray Origin: {ray.origin}, " +
                      $"Ray Direction: {ray.direction}, " +
                      $"Hit Distance: {enter:F2}, " +
                      $"World Position: {worldPosition}");
        }
        else
        {
            Debug.LogWarning("[DynamicSlotPositionManager] Raycast failed to hit virtual plane!");

            // Fallback: Use simple screen to world conversion
            worldPosition = mainCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y,
                Mathf.Abs(mainCamera.transform.position.z - deviceConfig.baseZPosition)));
        }

        return worldPosition;
    }

    /// <summary>
    /// Calculate world position with direct screen coordinates (for testing)
    /// </summary>
    public Vector3 CalculateWorldPositionFromScreen(Vector2 screenPosition)
    {
        if (mainCamera == null) return Vector3.zero;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        float enter;

        if (calculationPlane.Raycast(ray, out enter))
        {
            Vector3 worldPosition = ray.GetPoint(enter);
            worldPosition += new Vector3(currentOffset.x, currentOffset.y, 0);
            return worldPosition;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Get cached world position for a slot index
    /// </summary>
    public Vector3 GetCachedPosition(int slotIndex)
    {
        if (slotWorldPositions.ContainsKey(slotIndex))
        {
            return slotWorldPositions[slotIndex];
        }

        Debug.LogWarning($"[DynamicSlotPositionManager] No cached position for slot {slotIndex}");
        return Vector3.zero;
    }

    /// <summary>
    /// Cache position for a slot
    /// </summary>
    public void CacheSlotPosition(int slotIndex, Vector3 worldPosition)
    {
        slotWorldPositions[slotIndex] = worldPosition;
    }

    /// <summary>
    /// Recalculate all slot positions (call when screen orientation changes)
    /// </summary>
    public void RecalculateAllPositions(PlayerSlot[] slots)
    {
        CalculateDeviceParameters();
        slotWorldPositions.Clear();

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            RectTransform rectTransform = slot.transform as RectTransform;
            if (rectTransform != null)
            {
                Vector3 worldPos = CalculateWorldPositionForSlot(rectTransform);
                CacheSlotPosition(slot.slotIndex, worldPos);
            }
        }

        Debug.Log($"[DynamicSlotPositionManager] Recalculated {slotWorldPositions.Count} slot positions");
    }

    /// <summary>
    /// Get current device offset (for debugging)
    /// </summary>
    public Vector2 GetCurrentOffset() => currentOffset;

    /// <summary>
    /// Get current screen scale (for debugging)
    /// </summary>
    public float GetCurrentScale() => currentScreenScale;

    private void OnDestroy()
    {
        if (virtualPlane != null)
        {
            Destroy(virtualPlane);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || deviceConfig == null) return;

        // Draw virtual plane bounds
        Gizmos.color = new Color(0, 0.5f, 1f, 0.5f);
        Vector3 planeCenter = new Vector3(0, 0, deviceConfig.baseZPosition);
        Gizmos.DrawWireCube(planeCenter, new Vector3(20, 15, 0.1f));

        // Draw cached slot positions
        Gizmos.color = Color.green;
        foreach (var kvp in slotWorldPositions)
        {
            Gizmos.DrawWireSphere(kvp.Value, 0.2f);
        }
    }
#endif
}
