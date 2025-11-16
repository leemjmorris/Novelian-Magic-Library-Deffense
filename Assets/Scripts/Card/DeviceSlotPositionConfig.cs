using UnityEngine;
using System;

/// <summary>
/// Device-specific slot position configuration
/// Stores offset values for different screen aspect ratios
/// </summary>
[CreateAssetMenu(fileName = "DeviceSlotConfig", menuName = "Game/Device Slot Position Config")]
public class DeviceSlotPositionConfig : ScriptableObject
{
    [Header("Default Settings")]
    [Tooltip("Base Z position for character placement plane")]
    public float baseZPosition = -7.5f;

    [Tooltip("Default offset for unknown aspect ratios")]
    public Vector2 defaultOffset = Vector2.zero;

    [Header("Aspect Ratio Specific Offsets")]
    public AspectRatioOffset[] aspectRatioOffsets = new AspectRatioOffset[]
    {
        new AspectRatioOffset { aspectRatio = 1.777778f, description = "16:9 (Most common)", offset = new Vector2(0, 0) },
        new AspectRatioOffset { aspectRatio = 2.0f, description = "18:9 (Galaxy S8, S9)", offset = new Vector2(0, 0.2f) },
        new AspectRatioOffset { aspectRatio = 2.166667f, description = "19.5:9 (iPhone X, 11, 12 Landscape)", offset = new Vector2(0, 0.3f) },
        new AspectRatioOffset { aspectRatio = 2.222222f, description = "20:9 (Galaxy S20, S21)", offset = new Vector2(0, 0.35f) },
        new AspectRatioOffset { aspectRatio = 1.333333f, description = "4:3 (iPad Landscape)", offset = new Vector2(0, -0.5f) },
        new AspectRatioOffset { aspectRatio = 0.75f, description = "3:4 (iPad Portrait)", offset = new Vector2(0, -0.5f) },
        new AspectRatioOffset { aspectRatio = 0.5625f, description = "9:16 (Galaxy J7, most portrait)", offset = new Vector2(0, 0) },
        new AspectRatioOffset { aspectRatio = 0.461538f, description = "9:19.5 (iPhone X, 11, 12 Portrait)", offset = new Vector2(0, 0) },
    };

    [Header("Advanced Settings")]
    [Tooltip("Tolerance for aspect ratio comparison")]
    public float aspectRatioTolerance = 0.05f;

    [Tooltip("Additional scale multiplier for different screen sizes")]
    public bool useScreenSizeScaling = true;

    [Tooltip("Reference screen height for scaling (1920x1080 = 1080)")]
    public float referenceScreenHeight = 1080f;

    /// <summary>
    /// Get offset for current device
    /// </summary>
    public Vector2 GetOffsetForCurrentDevice()
    {
        float currentAspectRatio = (float)Screen.width / Screen.height;
        return GetOffsetForAspectRatio(currentAspectRatio);
    }

    /// <summary>
    /// Get offset for specific aspect ratio
    /// </summary>
    public Vector2 GetOffsetForAspectRatio(float aspectRatio)
    {
        foreach (var config in aspectRatioOffsets)
        {
            if (Mathf.Abs(aspectRatio - config.aspectRatio) <= aspectRatioTolerance)
            {
                Vector2 offset = config.offset;

                // Apply screen size scaling if enabled
                if (useScreenSizeScaling)
                {
                    float scaleFactor = Screen.height / referenceScreenHeight;
                    offset *= scaleFactor;
                }

                return offset;
            }
        }

        Debug.LogWarning($"No matching aspect ratio found for {aspectRatio:F2}. Using default offset.");
        return defaultOffset;
    }

    /// <summary>
    /// Get scale multiplier for current screen size
    /// </summary>
    public float GetScreenSizeScale()
    {
        if (!useScreenSizeScaling) return 1f;
        return Screen.height / referenceScreenHeight;
    }
}

[System.Serializable]
public class AspectRatioOffset
{
    public float aspectRatio;
    public string description;
    public Vector2 offset;
}
