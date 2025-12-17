using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{

    private VisualElement _bottomContainer;
    private Button _closeButton;
    private Button _openButton;

    private VisualElement _buttonSheet;
    private VisualElement _scrim;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _bottomContainer = root.Q<VisualElement>("Container_Button");
        _closeButton = root.Q<Button>("Button_Close");
        _openButton = root.Q<Button>("Button_Open");
          _buttonSheet = root.Q<VisualElement>("ButtonSheet");
        _scrim = root.Q<VisualElement>("Scrim");

        //시작할때 바텀 시트 그룹을 숨긴다.
        _bottomContainer.style.display = DisplayStyle.None;

        _openButton.RegisterCallback<ClickEvent>(OnOpenButtonClicked);
        _closeButton.RegisterCallback<ClickEvent>(OnCloseButtonClicked);

    }
      

    private void OnOpenButtonClicked(ClickEvent evt)
    {
        //바텀 시트 그룹을 보이게 한다.
        _bottomContainer.style.display = DisplayStyle.Flex;
        //바텀 시트와 페이드인 애니메이션
        _buttonSheet.AddToClassList("bottomsheet--up");
        _scrim.AddToClassList("scrim--fadein");
    }

    private void OnCloseButtonClicked(ClickEvent evt)
    {
        _bottomContainer.style.display = DisplayStyle.None;
        _buttonSheet.RemoveFromClassList("bottomsheet--up");
        _scrim.RemoveFromClassList("scrim--fadein");
    }

}
