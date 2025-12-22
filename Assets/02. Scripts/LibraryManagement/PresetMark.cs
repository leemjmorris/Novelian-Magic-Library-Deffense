using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 덱 프리셋 버튼 컴포넌트
/// 선택된 프리셋은 노란색, 비선택은 기본색으로 표시
/// </summary>
public class PresetMark : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image markImage;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.016f, 1f); // 노란색
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);    // 회색

    private int presetIndex = -1;
    private TeamSetupPanel panel;
    private bool isSelected = false;

    public int PresetIndex => presetIndex;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (markImage == null)
            markImage = GetComponent<Image>();
    }

    /// <summary>
    /// 초기화 (TeamSetupPanel에서 호출)
    /// </summary>
    public void Initialize(int index, TeamSetupPanel parentPanel)
    {
        presetIndex = index;
        panel = parentPanel;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        // 초기 색상 설정
        UpdateVisual();
    }

    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }

    /// <summary>
    /// 비주얼 업데이트 (색상 변경)
    /// </summary>
    private void UpdateVisual()
    {
        if (markImage != null)
        {
            markImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    /// <summary>
    /// 버튼 클릭 시 호출
    /// </summary>
    private void OnButtonClicked()
    {
        if (panel != null)
        {
            panel.OnPresetMarkClicked(presetIndex);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
