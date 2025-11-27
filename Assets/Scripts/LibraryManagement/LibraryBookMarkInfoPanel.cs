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
    [SerializeField] private Button equipButton;      // 장착 버튼
    [SerializeField] private Button changeButton;     // 변경 버튼 (슬롯에 다른 책갈피가 있을 때)
    [SerializeField] private Button unequipButton;    // 해제 버튼

    [Header("Panel References")]
    [SerializeField] private CharacterInfoPanel characterInfoPanel;
    [SerializeField] private BookmarkEquipPanel bookmarkEquipPanel;

    // 현재 선택된 책갈피
    private BookMark currentBookmark;
    // 현재 클릭한 슬롯 참조
    private CraftSceneBookMarkSlot currentSlot;
    // 변경 시 기존에 장착되어 있던 책갈피
    private BookMark previousBookmark;

    private void Awake()
    {
        // 버튼 리스너 설정
        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
        if (changeButton != null)
        {
            changeButton.onClick.AddListener(OnChangeButtonClicked);
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
    /// 버튼 상태 업데이트 - 장착 여부와 슬롯 상태에 따라 버튼 활성화/비활성화
    /// </summary>
    private void UpdateButtonStates()
    {
        if (currentBookmark == null || characterInfoPanel == null) return;

        int slotIndex = characterInfoPanel.GetSelectedSlotIndex();
        int characterID = characterInfoPanel.CharacterID;

        // 현재 슬롯에 장착된 책갈피 확인
        previousBookmark = BookMarkManager.Instance.GetCharacterBookmarkAtSlot(characterID, slotIndex);
        bool slotHasOtherBookmark = previousBookmark != null && previousBookmark != currentBookmark;

        // 현재 책갈피가 "이 슬롯"에 장착되어 있는지 확인
        bool isEquippedInThisSlot = currentBookmark.IsEquipped
            && currentBookmark.EquippedLibrarianID == characterID
            && currentBookmark.EquipSlotIndex == slotIndex;

        // 현재 책갈피가 다른 슬롯에 장착되어 있는지 확인
        bool isEquippedInOtherSlot = currentBookmark.IsEquipped && !isEquippedInThisSlot;

        // 버튼 상태 결정:
        // 1. 이 슬롯에 장착된 책갈피 → 해제 버튼만 표시
        // 2. 다른 슬롯에 장착된 책갈피 or 슬롯에 다른 책갈피 있음 → 변경 버튼만 표시
        // 3. 미장착 + 슬롯 비어있음 → 장착 버튼만 표시
        bool showEquip = !currentBookmark.IsEquipped && !slotHasOtherBookmark;
        bool showChange = isEquippedInOtherSlot || slotHasOtherBookmark;
        bool showUnequip = isEquippedInThisSlot;

        if (equipButton != null)
            equipButton.gameObject.SetActive(showEquip);
        if (changeButton != null)
            changeButton.gameObject.SetActive(showChange);
        if (unequipButton != null)
            unequipButton.gameObject.SetActive(showUnequip);
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
    /// 해제 버튼 클릭 - 현재 선택된 슬롯에 장착된 책갈피 해제
    /// </summary>
    private void OnUnequipButtonClicked()
    {
        if (characterInfoPanel == null)
        {
            Debug.LogWarning("[LibraryBookMarkInfoPanel] 캐릭터 정보가 없습니다.");
            return;
        }

        int characterID = characterInfoPanel.CharacterID;
        int slotIndex = characterInfoPanel.GetSelectedSlotIndex();

        // 해당 슬롯에 장착된 책갈피 가져오기
        BookMark bookmarkToUnequip = BookMarkManager.Instance.GetCharacterBookmarkAtSlot(characterID, slotIndex);

        if (bookmarkToUnequip == null)
        {
            Debug.LogWarning("[LibraryBookMarkInfoPanel] 해당 슬롯에 장착된 책갈피가 없습니다.");
            return;
        }

        // 해제할 책갈피의 UI 슬롯 찾기
        CraftSceneBookMarkSlot slotToUpdate = bookmarkEquipPanel != null
            ? bookmarkEquipPanel.FindSlotByBookmark(bookmarkToUnequip)
            : null;

        // BookMarkManager를 통해 해제
        bool success = BookMarkManager.Instance.UnequipBookmarkFromCharacter(characterID, slotIndex);

        if (success)
        {
            // UI 업데이트
            characterInfoPanel.UpdateSlotText(slotIndex, $"책갈피 슬롯 {slotIndex + 1}");
            characterInfoPanel.RefreshBookmarkUI();

            // 슬롯의 장착 아이콘 비활성화
            if (slotToUpdate != null)
            {
                slotToUpdate.SetEquipIconActive(false);
            }

            Debug.Log($"[LibraryBookMarkInfoPanel] 슬롯 {slotIndex}에서 책갈피 '{bookmarkToUnequip.Name}' 해제 완료!");

            // 패널 닫기
            ClosePanel();
        }
    }

    /// <summary>
    /// 변경 버튼 클릭 - 기존 책갈피 해제 후 새 책갈피 장착
    /// </summary>
    private void OnChangeButtonClicked()
    {
        if (currentBookmark == null || characterInfoPanel == null)
        {
            Debug.LogWarning("[LibraryBookMarkInfoPanel] 책갈피 또는 캐릭터 정보가 없습니다.");
            return;
        }

        int targetSlotIndex = characterInfoPanel.GetSelectedSlotIndex();
        int characterID = characterInfoPanel.CharacterID;

        // Case 1: 현재 책갈피가 다른 슬롯에 장착되어 있는 경우 → 기존 슬롯에서 해제
        if (currentBookmark.IsEquipped)
        {
            int oldSlotIndex = currentBookmark.EquipSlotIndex;

            // 기존 슬롯에서 해제
            BookMarkManager.Instance.UnequipBookmarkFromCharacter(characterID, oldSlotIndex);

            // 기존 슬롯 UI 업데이트
            characterInfoPanel.UpdateSlotText(oldSlotIndex, $"책갈피 슬롯 {oldSlotIndex + 1}");
        }

        // Case 2: 타겟 슬롯에 다른 책갈피가 있는 경우 → 해제
        if (previousBookmark != null)
        {
            // 기존 책갈피의 UI 슬롯 찾기 (equipIcon 비활성화용)
            CraftSceneBookMarkSlot previousSlot = bookmarkEquipPanel != null
                ? bookmarkEquipPanel.FindSlotByBookmark(previousBookmark)
                : null;

            // 타겟 슬롯의 기존 책갈피 해제
            BookMarkManager.Instance.UnequipBookmarkFromCharacter(characterID, targetSlotIndex);

            // 기존 책갈피 슬롯의 equipIcon 비활성화
            if (previousSlot != null)
            {
                previousSlot.SetEquipIconActive(false);
            }
        }

        // 새 책갈피 장착
        bool equipSuccess = BookMarkManager.Instance.EquipBookmarkToCharacter(
            characterID,
            currentBookmark,
            targetSlotIndex
        );

        if (equipSuccess)
        {
            // UI 업데이트
            characterInfoPanel.UpdateSlotText(targetSlotIndex, currentBookmark.Name);
            characterInfoPanel.RefreshBookmarkUI();

            // 새 책갈피 슬롯의 장착 아이콘 활성화
            if (currentSlot != null)
            {
                currentSlot.SetEquipIconActive(true);
            }

            Debug.Log($"[LibraryBookMarkInfoPanel] 책갈피 '{currentBookmark.Name}' 슬롯 {targetSlotIndex}으로 변경 완료!");

            // 패널 닫기
            ClosePanel();
        }
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
