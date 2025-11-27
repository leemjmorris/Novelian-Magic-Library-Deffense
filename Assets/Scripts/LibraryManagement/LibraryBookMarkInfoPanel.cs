using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LibraryManagementScene에서 책갈피 정보 표시 및 장착/해제 처리
/// </summary>
public class LibraryBookMarkInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image bookmarkIconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;

    [Header("Panel References")]
    [SerializeField] private CharacterInfoPanel characterInfoPanel;
    [SerializeField] private BookmarkEquipPanel bookmarkEquipPanel;

    // 현재 선택된 책갈피
    private BookMark currentBookmark;
    // 현재 클릭한 슬롯 참조
    private CraftSceneBookMarkSlot currentSlot;

    private void Awake()
    {
        // 버튼 리스너 설정
        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
        if (unequipButton != null)
        {
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);
        }
    }

    /// <summary>
    /// 패널 열기 - 책갈피 정보 표시
    /// </summary>
    public void OpenInfoPanel(Sprite icon, string name, string description, BookMark bookmark, CraftSceneBookMarkSlot slot)
    {
        currentBookmark = bookmark;
        currentSlot = slot;

        // UI 업데이트
        if (bookmarkIconImage != null)
            bookmarkIconImage.sprite = icon;
        if (nameText != null)
            nameText.text = name;
        if (descriptionText != null)
            descriptionText.text = description;

        // 버튼 상태 설정 (장착 여부에 따라)
        UpdateButtonStates();

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 버튼 상태 업데이트 - 장착 여부에 따라 버튼 활성화/비활성화
    /// </summary>
    private void UpdateButtonStates()
    {
        if (currentBookmark == null) return;

        bool isEquipped = currentBookmark.IsEquipped;

        // 미장착 책갈피: 장착 버튼만 표시
        // 장착된 책갈피: 해제 버튼만 표시
        if (equipButton != null)
            equipButton.gameObject.SetActive(!isEquipped);
        if (unequipButton != null)
            unequipButton.gameObject.SetActive(isEquipped);
    }

    /// <summary>
    /// 장착 버튼 클릭
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (currentBookmark == null || characterInfoPanel == null)
        {
            Debug.LogWarning("[LibraryBookMarkInfoPanel] 책갈피 또는 캐릭터 정보가 없습니다.");
            return;
        }

        int slotIndex = characterInfoPanel.GetSelectedSlotIndex();
        int characterID = characterInfoPanel.CharacterID;

        // BookMarkManager를 통해 장착
        bool success = BookMarkManager.Instance.EquipBookmarkToCharacter(
            characterID,
            currentBookmark,
            slotIndex
        );

        if (success)
        {
            // UI 업데이트
            characterInfoPanel.UpdateSlotText(slotIndex, currentBookmark.Name);
            characterInfoPanel.RefreshBookmarkUI();

            // 슬롯의 장착 아이콘 활성화
            if (currentSlot != null)
            {
                currentSlot.SetEquipIconActive(true);
            }

            Debug.Log($"[LibraryBookMarkInfoPanel] 책갈피 '{currentBookmark.Name}' 슬롯 {slotIndex}에 장착 완료!");

            // 패널 닫기
            ClosePanel();
        }
    }

    /// <summary>
    /// 해제 버튼 클릭
    /// </summary>
    private void OnUnequipButtonClicked()
    {
        if (currentBookmark == null || characterInfoPanel == null)
        {
            Debug.LogWarning("[LibraryBookMarkInfoPanel] 책갈피 또는 캐릭터 정보가 없습니다.");
            return;
        }

        int characterID = characterInfoPanel.CharacterID;

        // 해당 책갈피가 장착된 슬롯 찾기
        int slotIndex = FindEquippedSlotIndex(characterID, currentBookmark);

        if (slotIndex < 0)
        {
            Debug.LogWarning("[LibraryBookMarkInfoPanel] 장착된 슬롯을 찾을 수 없습니다.");
            return;
        }

        // BookMarkManager를 통해 해제
        bool success = BookMarkManager.Instance.UnequipBookmarkFromCharacter(characterID, slotIndex);

        if (success)
        {
            // UI 업데이트
            characterInfoPanel.UpdateSlotText(slotIndex, $"책갈피 슬롯 {slotIndex + 1}");
            characterInfoPanel.RefreshBookmarkUI();

            // 슬롯의 장착 아이콘 비활성화
            if (currentSlot != null)
            {
                currentSlot.SetEquipIconActive(false);
            }

            Debug.Log($"[LibraryBookMarkInfoPanel] 슬롯 {slotIndex}에서 책갈피 해제 완료!");

            // 패널 닫기
            ClosePanel();
        }
    }

    /// <summary>
    /// 특정 책갈피가 장착된 슬롯 인덱스 찾기
    /// </summary>
    private int FindEquippedSlotIndex(int characterID, BookMark bookmark)
    {
        // 5개 슬롯을 검색
        for (int i = 0; i < 5; i++)
        {
            BookMark equipped = BookMarkManager.Instance.GetCharacterBookmarkAtSlot(characterID, i);
            if (equipped != null && equipped == bookmark)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void ClosePanel()
    {
        currentBookmark = null;
        gameObject.SetActive(false);
    }
}
