using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using System.Linq;

[System.Serializable]
public class MultiSelectDropdown : MonoBehaviour
{
    [System.Serializable]
    public class MultiSelectOption
    {
        public string text;
        public bool isSelected;
        [HideInInspector] public Toggle toggle;
        
        public MultiSelectOption(string optionText)
        {
            text = optionText;
            isSelected = false;
        }
    }
    
    [Header("UI Components")]
    public Button dropdownButton;
    public GameObject dropdownPanel;
    public Transform contentParent;
    public GameObject togglePrefab;
    public Text displayText;
    
    [Header("Settings")]
    public string placeholder = "선택하세요";
    public string allSelectedText = "ALL 선택됨";
    public int maxDisplayItems = 3;
    
    [Header("Data")]
    public List<MultiSelectOption> options = new List<MultiSelectOption>();
    
    // 이벤트
    public System.Action<List<string>> OnSelectionChanged;
    
    private bool isDropdownOpen = false;
    
    void Start()
    {
        if (dropdownButton != null)
        {
            dropdownButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            dropdownButton.onClick.AddListener(ToggleDropdown);
        }
        
        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(false);
        }
        
        UpdateDisplayText();
    }
    
    public void SetOptions(string[] optionTexts, string defaultText = null)
    {
        ClearOptions();
        
        // "ALL" 옵션 추가 (선택사항)
        if (!string.IsNullOrEmpty(defaultText))
        {
            placeholder = defaultText;
            options.Add(new MultiSelectOption("ALL"));
        }
        
        // 일반 옵션들 추가
        foreach (string optionText in optionTexts)
        {
            if (!string.IsNullOrEmpty(optionText))
            {
                options.Add(new MultiSelectOption(optionText));
            }
        }
        
        // 런타임에서는 다음 프레임에 생성
        if (Application.isPlaying)
        {
            StartCoroutine(CreateToggleElementsDelayed());
        }
        else
        {
            CreateToggleElements();
            UpdateDisplayText();
        }
    }
    
    private IEnumerator CreateToggleElementsDelayed()
    {
        yield return null; // 한 프레임 대기
        CreateToggleElements();
        UpdateDisplayText();
        
        // UI 강제 업데이트
        Canvas.ForceUpdateCanvases();
    }
    
    public void ClearOptions()
    {
        // 기존 토글들 제거
        if (contentParent != null)
        {
            // 런타임에서는 Destroy 사용
            if (Application.isPlaying)
            {
                foreach (Transform child in contentParent)
                {
                    Destroy(child.gameObject);
                }
            }
            else
            {
                foreach (Transform child in contentParent)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        options.Clear();
    }
    
    private void CreateToggleElements()
    {
        if (togglePrefab == null || contentParent == null) return;
        
        // 기존 토글들 제거 (중복 방지)
        if (Application.isPlaying)
        {
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            foreach (Transform child in contentParent)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        foreach (var option in options)
        {
            GameObject toggleObj = Instantiate(togglePrefab, contentParent);
            Toggle toggle = toggleObj.GetComponent<Toggle>();
            Text label = toggleObj.GetComponentInChildren<Text>();
            
            if (toggle != null)
            {
                option.toggle = toggle;
                toggle.isOn = option.isSelected;
                toggle.onValueChanged.AddListener((bool value) => OnToggleValueChanged(option, value));
            }
            
            if (label != null)
            {
                label.text = option.text;
            }
        }
    }
    
    private void OnToggleValueChanged(MultiSelectOption option, bool isSelected)
    {
        option.isSelected = isSelected;

        // "ALL" 옵션 처리
        if (options.Count > 0 && option == options[0] && options[0].text.Contains("ALL"))
        {
            // ALL 토글을 클릭하면 나머지 모두 선택/해제
            for (int i = 1; i < options.Count; i++)
            {
                options[i].isSelected = isSelected;
                if (options[i].toggle != null)
                {
                    options[i].toggle.SetIsOnWithoutNotify(isSelected);
                }
            }
        }
        else
        {
            // 개별 토글 조작 시
            if (options.Count > 0 && options[0].text.Contains("ALL"))
            {
                // 나머지 항목이 모두 선택되었으면 "ALL"도 선택
                bool allSelected = true;
                for (int i = 1; i < options.Count; i++)
                {
                    if (!options[i].isSelected)
                    {
                        allSelected = false;
                        break;
                    }
                }
                options[0].isSelected = allSelected;
                if (options[0].toggle != null)
                {
                    options[0].toggle.SetIsOnWithoutNotify(allSelected);
                }
            }
        }

        UpdateDisplayText();
        OnSelectionChanged?.Invoke(GetSelectedValues());
    }
    
    public void SelectAll(bool includeAllOption = true)
    {
        int startIndex = (options.Count > 0 && options[0].text.Contains("ALL") && !includeAllOption) ? 1 : 0;
        
        for (int i = startIndex; i < options.Count; i++)
        {
            options[i].isSelected = true;
            if (options[i].toggle != null)
            {
                options[i].toggle.SetIsOnWithoutNotify(true);
            }
        }
        
        UpdateDisplayText();
        OnSelectionChanged?.Invoke(GetSelectedValues());
    }
    
    public void DeselectAll()
    {
        foreach (var option in options)
        {
            option.isSelected = false;
            if (option.toggle != null)
            {
                option.toggle.SetIsOnWithoutNotify(false);
            }
        }
        
        UpdateDisplayText();
        OnSelectionChanged?.Invoke(GetSelectedValues());
    }
    
    public List<string> GetSelectedValues()
    {
        List<string> selected = new List<string>();
        foreach (var option in options)
        {
            if (option.isSelected && !option.text.Contains("ALL"))
            {
                selected.Add(option.text);
            }
        }
        return selected;
    }
    
    /// <summary>
    /// ALL이 선택되었는지 확인
    /// </summary>
    public bool IsAllSelected()
    {
        if (options.Count > 0 && options[0].text.Contains("ALL"))
        {
            return options[0].isSelected;
        }
        return false;
    }
    
    public void SetSelectedValues(List<string> values)
    {
        foreach (var option in options)
        {
            option.isSelected = values.Contains(option.text);
            if (option.toggle != null)
            {
                option.toggle.SetIsOnWithoutNotify(option.isSelected);
            }
        }
        
        UpdateDisplayText();
    }
    
    private void UpdateDisplayText()
    {
        if (displayText == null) return;
        
        List<string> selected = GetSelectedValues();
        
        if (selected.Count == 0)
        {
            displayText.text = placeholder;
        }
        else if (selected.Count <= maxDisplayItems)
        {
            displayText.text = string.Join(", ", selected);
        }
        else
        {
            displayText.text = $"{selected.Count} Items";
        }
    }
    
    public void ToggleDropdown()
    {
        Debug.Log($"ToggleDropdown called. Current state: {isDropdownOpen}"); // 디버그용
        
        isDropdownOpen = !isDropdownOpen; // 토글 동작
        
        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(isDropdownOpen);
            
            if (isDropdownOpen)
            {
                AdjustDropdownPosition();
                dropdownPanel.transform.SetAsLastSibling();
            }
        }
        
        Debug.Log($"ToggleDropdown completed. New state: {isDropdownOpen}"); // 디버그용
    }
    
    public void CloseDropdown()
    {
        isDropdownOpen = false;
        if (dropdownPanel != null)
        {
            dropdownPanel.SetActive(false);
        }
    }
    
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        // 버튼이나 패널 내부의 UI가 있으면 true
        return results.Count > 0;
    }

    // 외부 클릭 감지로 드롭다운 닫기
    void Update()
    {
        if (isDropdownOpen && Input.GetMouseButtonDown(0))
        {
            // 클릭한 UI 오브젝트가 내 패널/버튼인지 확인
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool clickedInside = false;
            foreach (var result in results)
            {
                // 내 드롭다운 패널 또는 버튼이면
                if (result.gameObject == dropdownPanel || result.gameObject == dropdownButton.gameObject || result.gameObject.transform.IsChildOf(dropdownPanel.transform))
                {
                    clickedInside = true;
                    break;
                }
            }

            if (!clickedInside)
            {
                CloseDropdown();
            }
        }
    }

    private void AdjustDropdownPosition()
    {
        if (dropdownPanel == null || dropdownButton == null) return;

        RectTransform panelRect = dropdownPanel.GetComponent<RectTransform>();

        // 패널을 기본 위치로 초기화 (버튼 아래)
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, 0);

        // 캔버스 가져오기
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        
        // 패널의 월드 좌표를 스크린 좌표로 변환
        Vector3[] panelCorners = new Vector3[4];
        panelRect.GetWorldCorners(panelCorners);
        
        // 패널 하단의 스크린 좌표 계산
        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(cam, panelCorners[0]);
        
        // 화면을 벗어나는지 체크 (스크린 좌표 기준)
        bool isOutOfScreen = bottomLeftScreen.y < 0;
        
        // VerticalLayoutGroup의 reverseArrangement 설정
        VerticalLayoutGroup layoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.reverseArrangement = isOutOfScreen;
        }
        
        // ScrollRect의 스크롤 위치 조정
        ScrollRect scrollRect = dropdownPanel.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            if (isOutOfScreen)
            {
                // 위쪽에 표시될 때는 스크롤을 맨 아래로
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
            else
            {
                // 아래쪽에 표시될 때는 스크롤을 맨 위로
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
        
        panelRect.anchoredPosition = new Vector2(
            panelRect.anchoredPosition.x,
            isOutOfScreen ? 250 : 0
        );
        
        // 디버그 정보 출력 (한 줄로)
        Debug.Log($"[MultiSelectDropdown] WorldBottomLeft:{panelCorners[0]} | ScreenBottomLeft:{bottomLeftScreen} | PanelHeight:{panelRect.rect.height} | ScreenHeight:{Screen.height} | OutOfScreen:{isOutOfScreen} | ReverseOrder:{isOutOfScreen} | FinalPos:{panelRect.anchoredPosition}");
    }
} 