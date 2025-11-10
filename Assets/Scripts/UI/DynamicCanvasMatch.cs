using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class DynamicCanvasMatch : MonoBehaviour
{
    [SerializeField] float pivotAspect = 1080f / 2400f; // Production standard (20:9)
    [SerializeField] float widthBias = 0.0f;            // 0 = pure calculation, + value means weight on Width side
    void OnEnable()
    {
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        float curAspect = (float)Screen.width / Screen.height;

        //If the current ratio is wider than the standard(wide), weight is applied to Height(→ Match 0),
        // If the current ratio is narrower than the standard, weight is applied to Width (→ Match 1).
        float t = Mathf.InverseLerp(pivotAspect * 0.66f, pivotAspect * 1.5f, curAspect);
        float match = Mathf.Clamp01(1f - t + widthBias);
        scaler.matchWidthOrHeight = match;   // 0=Width, 1=Height
        }
    }
