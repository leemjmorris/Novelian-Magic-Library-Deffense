using UnityEngine;

//UI를 만들때 SafeArea만큼 리사이즈되는 스크립트다.
public class SafeArea : MonoBehaviour
{
    private RectTransform safeAreaRect;
    private Canvas canvas;
    private Rect lastSafeArea;

    void Start()
    {
        safeAreaRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        OnRectTransformDimensionsChange();
    }

    private void OnRectTransformDimensionsChange() //화면이 변할때 자동으로 호출되는 이벤트 함수다.
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
        var inverseSize = new Vector2(1f, 1f) / canvas.pixelRect.size; //0.0부터 1.0으로 사이즈가 정해진다.
        var newAnchorMin = Vector2.Scale(safeArea.position, inverseSize); //크기를 의미하는게 아니라 각 x y z에 inverseSize를 곱하는 것이다.
        var newAnchorMax = Vector2.Scale(safeArea.position + safeArea.size, inverseSize);

        //앵커 정규화 -> 앵커를 새로 잡는 것이다.
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
