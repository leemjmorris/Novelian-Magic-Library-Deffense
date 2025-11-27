using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookmarkEquipSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI optionText;
    [SerializeField] private Image selectionBorder; // LCB: Selection border image (선택 테두리 이미지)
    [SerializeField] private Button slotButton; // LCB: Slot click button (슬롯 클릭 버튼)

    private BookMark bookmark;
    private CharacterInfoPanel characterInfoPanel;
    private BookmarkEquipPanel equipPanel; // LCB: Reference to BookmarkEquipPanel (패널 참조)
    private bool isSelected = false; // LCB: Selection state (선택 상태)

    public int CharacterID { get; private set; }
    public BookMark Bookmark => bookmark; // LCB: Expose bookmark data (책갈피 데이터 노출)
    
    /// <summary>
    /// LCB: Initialize bookmark slot with data and references
    /// 책갈피 슬롯 초기화 (데이터 및 참조 설정)
    /// </summary>
    public void Init(BookMark bookmark, CharacterInfoPanel panel, BookmarkEquipPanel equipPanel)
    {
        this.bookmark = bookmark;
        characterInfoPanel = panel;
        this.equipPanel = equipPanel;
        var bookmarkData = CSVLoader.Instance.GetData<BookmarkData>(bookmark.BookmarkDataID);

        gradeText.text = $"{bookmark.GetGradeName(bookmark.Grade)}";

        // 스킬 책갈피와 스탯 책갈피 구분
        if (bookmark.Type == BookmarkType.Skill)
        {
            // 스킬 책갈피인 경우
            Debug.Log($"[BookmarkEquipSlot] 스킬 책갈피 초기화 - SkillID: {bookmark.SkillID}");
            var skillData = CSVLoader.Instance.GetData<SkillData>(bookmark.SkillID);

            if (skillData == null)
            {
                Debug.LogError($"[BookmarkEquipSlot] SkillData를 찾을 수 없음! SkillID: {bookmark.SkillID}");
                optionText.text = $"스킬 ID: {bookmark.SkillID} (데이터 없음)";
            }
            else
            {
                Debug.Log($"[BookmarkEquipSlot] SkillData 로드 성공 - Skill_Name: {skillData.Skill_Name}");
                optionText.text = $"스킬: {skillData.Skill_Name}";
            }
        }
        else
        {
            // 스탯 책갈피인 경우
            if (bookmarkData.Option_ID > 0)
            {
                var optionData = CSVLoader.Instance.GetData<BookmarkOptionData>(bookmarkData.Option_ID);
                if (optionData != null)
                {
                    optionText.text = $"{CSVLoader.Instance.GetData<StringTable>(optionData.Option_Name_ID)?.Text ?? "Unknown"}\n{bookmark.OptionValue}%";
                }
                else
                {
                    optionText.text = "옵션 데이터 없음";
                }
            }
            else
            {
                optionText.text = "옵션 없음";
            }
        }

        CharacterID = panel.CharacterID;

        // LCB: Setup slot button click listener (슬롯 버튼 클릭 리스너 설정)
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        // LCB: Initialize selection border as deselected (선택 테두리 초기화 - 비선택 상태)
        SetSelected(false);
    }

    /// <summary>
    /// LCB: Called when slot is clicked
    /// 슬롯 클릭 시 호출
    /// </summary>
    private void OnSlotClicked()
    {
        if (equipPanel != null)
        {
            equipPanel.OnSlotClicked(this);
        }
    }

    /// <summary>
    /// LCB: Set selection state and update border visibility
    /// 선택 상태 설정 및 테두리 표시 업데이트
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(selected);
        }
    }

    public void OnClickEquipButton()
    {
        // CharacterInfoPanel에서 선택된 슬롯 인덱스 가져오기
        int slotIndex = characterInfoPanel.GetSelectedSlotIndex();

        // BookMarkManager를 통해 장착
        bool success = BookMarkManager.Instance.EquipBookmarkToCharacter(
            CharacterID,
            bookmark,
            slotIndex
        );

        if (success)
        {
            // 선택된 슬롯에 텍스트 업데이트
            characterInfoPanel.UpdateSlotText(slotIndex, bookmark.Name);
            Debug.Log($"책갈피 '{bookmark.Name}' 슬롯 {slotIndex}에 장착 완료!");
        }
    }
}
