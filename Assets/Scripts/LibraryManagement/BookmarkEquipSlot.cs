using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookmarkEquipSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI optionText;
    private BookMark bookmark;
    private CharacterInfoPanel characterInfoPanel;
    public int CharacterID { get; private set; }
    
    public void Init(BookMark bookmark, CharacterInfoPanel panel)
    {
        this.bookmark = bookmark;
        characterInfoPanel = panel;
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
