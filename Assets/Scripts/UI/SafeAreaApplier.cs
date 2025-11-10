
using UnityEngine;

[ExecuteAlways]
public class SafeAreaApplier : MonoBehaviour
{
    RectTransform rt;
    void Awake() 
    {
        if (rt == null)
        {
            rt = GetComponent<RectTransform>();
        }
    }
    void OnRectTransformDimensionsChange() { Apply(); }

    void Apply()
    {
        //CBL: rect info
        var sa = Screen.safeArea;
        //CBL: start Point
        Vector2 min = sa.position;
        //CBL: last point
        Vector2 max = sa.position + sa.size;
        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
