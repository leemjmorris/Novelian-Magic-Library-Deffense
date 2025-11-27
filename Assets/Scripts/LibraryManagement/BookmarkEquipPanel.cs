using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookmarkEquipPanel : MonoBehaviour
{
    [Header("Slot Settings")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotContainer;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button equipButton; // LCB: Equip button (장착 버튼)
    [SerializeField] private Button unequipButton; // LCB: Unequip button (해제 버튼)

    [Header("Panel References")]
    [SerializeField] private GameObject InfoPanel;
    private CharacterInfoPanel characterInfoPanel;

    [SerializeField] BookMarkManager bmManager;

    // LCB: Track currently selected slot (현재 선택된 슬롯 추적)
    private BookmarkEquipSlot currentSelectedSlot;


    private void Awake()
    {
        characterInfoPanel = InfoPanel.GetComponent<CharacterInfoPanel>();

        // LCB: Setup button listeners (버튼 리스너 설정)
        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
        if (unequipButton != null)
        {
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);
        }

        // LCB: Initialize buttons as disabled (버튼 초기 비활성화)
        UpdateButtonStates();
    }

    private void OnEnable()
    {
        // LCB: Clear previous selection (이전 선택 초기화)
        currentSelectedSlot = null;
        UpdateButtonStates();

        List<BookMark> unequippedBookmarks = BookMarkManager.Instance.GetAllBookmarks();
        for (int i = 0; i < unequippedBookmarks.Count; i++)
        {
            if (!unequippedBookmarks[i].IsEquipped)
            {
                GameObject slot = Instantiate(slotPrefab, slotContainer);
                BookmarkEquipSlot slotComponent = slot.GetComponent<BookmarkEquipSlot>();
                // LCB: Pass this panel reference to slot (슬롯에 패널 참조 전달)
                slotComponent.Init(unequippedBookmarks[i], characterInfoPanel, this);
            }
        }
    }

    private void OnDisable()
    {
        // 패널이 비활성화될 때 슬롯 정리 (재활성화 시 다시 생성됨)
        if (slotContainer != null)
        {
            foreach (Transform child in slotContainer)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// LCB: Called when a bookmark slot is clicked
    /// 책갈피 슬롯 클릭 시 호출
    /// </summary>
    public void OnSlotClicked(BookmarkEquipSlot clickedSlot)
    {
        // LCB: Deselect previous slot (이전 슬롯 선택 해제)
        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(false);
        }

        // LCB: Select new slot (새 슬롯 선택)
        currentSelectedSlot = clickedSlot;
        currentSelectedSlot.SetSelected(true);

        // LCB: Update button states (버튼 상태 업데이트)
        UpdateButtonStates();

        Debug.Log($"[BookmarkEquipPanel] 책갈피 선택: {clickedSlot.Bookmark.Name}");
    }

    /// <summary>
    /// LCB: Update equip/unequip button states based on selection
    /// 선택 상태에 따라 장착/해제 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        bool hasSelection = currentSelectedSlot != null;

        if (equipButton != null)
        {
            equipButton.interactable = hasSelection;
        }

        // LCB: Unequip button only enabled when selected character slot has bookmark
        // 해제 버튼은 선택된 캐릭터 슬롯에 책갈피가 있을 때만 활성화
        if (unequipButton != null)
        {
            int selectedSlotIndex = characterInfoPanel.GetSelectedSlotIndex();
            BookMark equippedBookmark = BookMarkManager.Instance.GetCharacterBookmarkAtSlot(
                characterInfoPanel.CharacterID,
                selectedSlotIndex
            );
            unequipButton.interactable = equippedBookmark != null;
        }
    }

    /// <summary>
    /// LCB: Equip selected bookmark to character slot
    /// 선택된 책갈피를 캐릭터 슬롯에 장착
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (currentSelectedSlot == null)
        {
            Debug.LogWarning("[BookmarkEquipPanel] 선택된 책갈피가 없습니다.");
            return;
        }

        BookMark selectedBookmark = currentSelectedSlot.Bookmark;
        int slotIndex = characterInfoPanel.GetSelectedSlotIndex();
        int characterID = characterInfoPanel.CharacterID;

        // LCB: Equip bookmark through manager (매니저를 통해 책갈피 장착)
        bool success = BookMarkManager.Instance.EquipBookmarkToCharacter(
            characterID,
            selectedBookmark,
            slotIndex
        );

        if (success)
        {
            // LCB: Update character info panel UI (캐릭터 정보 패널 UI 업데이트)
            characterInfoPanel.UpdateSlotText(slotIndex, selectedBookmark.Name);
            characterInfoPanel.RefreshBookmarkUI();

            Debug.Log($"[BookmarkEquipPanel] 책갈피 '{selectedBookmark.Name}' 슬롯 {slotIndex}에 장착 완료!");

            // LCB: Close panel and refresh (패널 닫고 새로고침)
            ClosePanel();
        }
    }

    /// <summary>
    /// LCB: Unequip bookmark from character slot
    /// 캐릭터 슬롯에서 책갈피 해제
    /// </summary>
    private void OnUnequipButtonClicked()
    {
        int slotIndex = characterInfoPanel.GetSelectedSlotIndex();
        int characterID = characterInfoPanel.CharacterID;

        // LCB: Unequip bookmark through manager (매니저를 통해 책갈피 해제)
        bool success = BookMarkManager.Instance.UnequipBookmarkFromCharacter(characterID, slotIndex);

        if (success)
        {
            // LCB: Update character info panel UI (캐릭터 정보 패널 UI 업데이트)
            characterInfoPanel.UpdateSlotText(slotIndex, $"책갈피 슬롯 {slotIndex + 1}");
            characterInfoPanel.RefreshBookmarkUI();

            Debug.Log($"[BookmarkEquipPanel] 슬롯 {slotIndex}에서 책갈피 해제 완료!");

            // LCB: Update button states (버튼 상태 업데이트)
            UpdateButtonStates();
        }
    }

    public void ClosePanel()
    {
        this.gameObject.SetActive(false);
        InfoPanel.SetActive(true);
    }
}
