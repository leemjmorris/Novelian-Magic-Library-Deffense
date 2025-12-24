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
    [SerializeField] private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 비활성화 (어두운 회색 + 반투명)

    private int presetIndex = -1;
    private bool isDisabled = false;
    private TeamSetupPanel panel;
    private System.Action<int> onClickCallback;
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
        onClickCallback = null;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        // 초기 색상 설정
        UpdateVisual();
    }

    /// <summary>
    /// 초기화 (콜백 방식 - 파견씬 등에서 사용)
    /// </summary>
    public void Initialize(int index, System.Action<int> clickCallback)
    {
        presetIndex = index;
        panel = null;
        onClickCallback = clickCallback;

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
    /// 버튼 비활성화 설정 (다른 파견에서 사용 중일 때)
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        isDisabled = disabled;
        if (button != null)
        {
            button.interactable = !disabled;
        }
        UpdateVisual();
    }

    /// <summary>
    /// 비활성화 상태 반환
    /// </summary>
    public bool IsDisabled => isDisabled;

    /// <summary>
    /// 비주얼 업데이트 (색상 변경)
    /// </summary>
    private void UpdateVisual()
    {
        if (markImage != null)
        {
            if (isDisabled)
            {
                markImage.color = disabledColor;
            }
            else
            {
                markImage.color = isSelected ? selectedColor : normalColor;
            }
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
        else if (onClickCallback != null)
        {
            onClickCallback.Invoke(presetIndex);
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
