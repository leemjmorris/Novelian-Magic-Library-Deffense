using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 선택 시스템 - 1개 카드에 랜덤 장르 로드
/// </summary>
public class CardSelectionManager : MonoBehaviour
{
    [Header("카드")]
    public GameObject cardPanel;
    public CharacterCard characterCard;

    [Header("슬롯")]
    public List<PlayerSlot> playerSlots;

    [Header("캐릭터 데이터")]
    public List<CharacterData> allCharacterData; // 5개 장르

    private CharacterData selectedData;

    void Start()
    {
        LoadRandomCharacter();
    }

    /// <summary>
    /// 5개 장르 중 랜덤 선택해서 카드에 로드
    /// </summary>
    void LoadRandomCharacter()
    {
        if (allCharacterData == null || allCharacterData.Count == 0)
        {
            Debug.LogError("CharacterData가 없습니다!");
            return;
        }

        // 랜덤 선택
        int randomIndex = Random.Range(0, allCharacterData.Count);
        selectedData = allCharacterData[randomIndex];

        // 카드 업데이트 (이미지 + 텍스트)
        if (characterCard != null && selectedData.characterSprite != null)
        {
            characterCard.UpdateCharacter(
                selectedData.characterSprite,
                selectedData.characterName,
                selectedData.genreType
            );
        }

        Debug.Log($"랜덤 캐릭터 로드: {selectedData.characterName} ({selectedData.genreType})");
    }

    /// <summary>
    /// 카드 클릭 시 호출
    /// </summary>
    public void OnCardSelected()
    {
        if (selectedData == null)
        {
            Debug.LogError("선택된 데이터가 없습니다!");
            return;
        }

        // 카드에 현재 표시된 스프라이트 가져오기
        Sprite cardSprite = characterCard.characterImage.sprite;

        if (cardSprite == null)
        {
            Debug.LogError("카드 이미지가 null입니다!");
            return;
        }

        // 빈 슬롯 찾아서 배치
        PlayerSlot emptySlot = FindNextEmptySlot();
        if (emptySlot != null)
        {
            // 카드에 표시된 스프라이트를 슬롯에 전달
            emptySlot.AssignCharacterSprite(cardSprite, selectedData.genreType);

            Debug.Log($"슬롯에 배치: {selectedData.characterName}, 스프라이트: {cardSprite.name}");

            // 카드 패널 비활성화
            if (cardPanel != null)
            {
                cardPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("빈 슬롯이 없습니다!");
        }
    }

    PlayerSlot FindNextEmptySlot()
    {
        foreach (PlayerSlot slot in playerSlots)
        {
            if (slot.IsEmpty()) return slot;
        }
        return null;
    }

    public void ShowCardPanel()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
            LoadRandomCharacter();
        }
    }

    public void ClearAllSlots()
    {
        foreach (PlayerSlot slot in playerSlots)
        {
            slot.ClearSlot();
        }
    }
}
