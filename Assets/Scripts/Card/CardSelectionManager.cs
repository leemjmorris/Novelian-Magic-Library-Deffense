using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

/// <summary>
/// 시작 카드 선택 시스템 - 2개 캐릭터 카드에서 선택
/// - 20초 타이머 (Time.timeScale 무시)
/// - 타임아웃 시 50% 확률 자동 선택
/// </summary>
public class CardSelectionManager : MonoBehaviour
{
    [Header("카드 패널")]
    public GameObject cardPanel;

    [Header("2개 카드")]
    public GameObject card1;
    public GameObject card2;

    [Header("타이머 텍스트 (선택사항)")]
    public TextMeshProUGUI timerText;

    [Header("슬롯")]
    public List<PlayerSlot> playerSlots;

    [Header("캐릭터 데이터")]
    public List<CharacterData> allCharacterData; // 5개 장르

    // 카드 선택 타임아웃 (20초)
    private const float SELECTION_TIME = 20f;

    // 선택된 카드 데이터
    private CharacterData selectedCard1Data;
    private CharacterData selectedCard2Data;
    private bool isCardSelected = false;

    // 타이머 취소 토큰
    private CancellationTokenSource selectionCts;


    /// <summary>
    /// 게임 시작 시 카드 선택 (StageManager에서 호출)
    /// </summary>
    public async UniTask ShowStartCards()
    {
        isCardSelected = false;

        // 1. 카드 패널 활성화
        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
        }

        // 2. 2개 랜덤 캐릭터 로드
        LoadTwoRandomCharacters();

        // 3. 20초 타이머 시작 & 선택 대기
        await WaitForSelection();

        // 4. 카드 패널 비활성화
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 카드 선택 대기 (20초 타이머)
    /// </summary>
    async UniTask WaitForSelection()
    {
        selectionCts?.Dispose();
        selectionCts = new CancellationTokenSource();
        float remainingTime = SELECTION_TIME;

        try
        {
            while (remainingTime > 0 && !isCardSelected)
            {
                // 타이머 텍스트 업데이트
                if (timerText != null)
                {
                    timerText.text = $"{(int)remainingTime}s";
                }

                // 1초 대기 (ignoreTimeScale=true로 Time.timeScale 무시)
                await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: selectionCts.Token);
                remainingTime -= 1f;
            }

            // 타임아웃 시 50% 확률 자동 선택
            if (!isCardSelected)
            {
                Debug.Log("[StartSelectionManager] 20초 타임아웃! 자동 선택");
                AutoSelectCard();
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[StartSelectionManager] 타이머 취소됨");
        }
        finally
        {
            selectionCts?.Dispose();
            selectionCts = null;
        }
    }

    /// <summary>
    /// 2개의 랜덤 캐릭터 로드 (중복 방지)
    /// </summary>
    void LoadTwoRandomCharacters()
    {
        if (allCharacterData == null || allCharacterData.Count < 2)
        {
            Debug.LogError("CharacterData가 2개 미만입니다!");
            return;
        }

        // 랜덤 선택 (중복 방지)
        int idx1 = UnityEngine.Random.Range(0, allCharacterData.Count);
        int idx2 = idx1;
        while (idx2 == idx1 && allCharacterData.Count > 1)
        {
            idx2 = UnityEngine.Random.Range(0, allCharacterData.Count);
        }

        selectedCard1Data = allCharacterData[idx1];
        selectedCard2Data = allCharacterData[idx2];

        // Card 1 업데이트
        UpdateCardUI(card1, selectedCard1Data);

        // Card 2 업데이트
        UpdateCardUI(card2, selectedCard2Data);

        Debug.Log($"랜덤 캐릭터 로드: {selectedCard1Data.characterName}, {selectedCard2Data.characterName}");
    }

    /// <summary>
    /// 카드 UI 업데이트
    /// </summary>
    void UpdateCardUI(GameObject cardObj, CharacterData data)
    {
        if (cardObj == null || data == null) return;

        CharacterCard charCard = cardObj.GetComponent<CharacterCard>();
        if (charCard != null && data.characterSprite != null)
        {
            charCard.UpdateCharacter(
                data.characterSprite,
                data.characterName,
                data.genreType
            );
        }
    }


    /// <summary>
    /// 50% 확률로 자동 선택
    /// </summary>
    void AutoSelectCard()
    {
        float random = UnityEngine.Random.Range(0f, 1f);

        if (random < 0.5f)
        {
            Debug.Log("[StartSelectionManager] 자동 선택: Card 1");
            OnCard1Selected();
        }
        else
        {
            Debug.Log("[StartSelectionManager] 자동 선택: Card 2");
            OnCard2Selected();
        }
    }

    /// <summary>
    /// Card 1 클릭 시 호출 (Button.onClick에 연결)
    /// </summary>
    public void OnCard1Selected()
    {
        Debug.Log($"[CardSelectionManager] OnCard1Selected 호출! isCardSelected={isCardSelected}");

        if (isCardSelected)
        {
            Debug.LogWarning("[CardSelectionManager] 이미 카드가 선택되었습니다.");
            return;
        }

        isCardSelected = true;

        // 타이머 즉시 취소 및 0초 표시
        selectionCts?.Cancel();
        if (timerText != null)
        {
            timerText.text = "0s";
        }

        Debug.Log($"[CardSelectionManager] Card 1 선택 처리 시작 - Data: {selectedCard1Data?.characterName}");
        SelectCard(selectedCard1Data);
    }

    /// <summary>
    /// Card 2 클릭 시 호출 (Button.onClick에 연결)
    /// </summary>
    public void OnCard2Selected()
    {
        Debug.Log($"[CardSelectionManager] OnCard2Selected 호출! isCardSelected={isCardSelected}");

        if (isCardSelected)
        {
            Debug.LogWarning("[CardSelectionManager] 이미 카드가 선택되었습니다.");
            return;
        }

        isCardSelected = true;

        // 타이머 즉시 취소 및 0초 표시
        selectionCts?.Cancel();
        if (timerText != null)
        {
            timerText.text = "0s";
        }

        Debug.Log($"[CardSelectionManager] Card 2 선택 처리 시작 - Data: {selectedCard2Data?.characterName}");
        SelectCard(selectedCard2Data);
    }

    /// <summary>
    /// 카드 선택 처리 - 물리 오브젝트 기반
    /// </summary>
    void SelectCard(CharacterData data)
    {
        Debug.Log($"[카드선택] SelectCard 시작 - data null? {data == null}");

        if (data == null)
        {
            Debug.LogError("[카드선택] 선택된 데이터가 없습니다!");
            return;
        }

        
        bool added = AddCharacterToSlot(data);

        if (!added)
        {
            Debug.LogWarning("[카드선택] 시작 카드 선택에서 사용할 빈 슬롯이 없습니다!");
        }
        else
        {
            Debug.Log($"[카드선택] 시작 카드 선택으로 캐릭터 배치 완료: {data.characterName}");
        }
    }

    /// <summary>
    /// 레거시: 1장 카드 시스템 호환용 (LevelUpCardUI에서 호출)
    /// </summary>
    public void OnCardSelected()
    {
        // 기본적으로 Card 1 선택
        OnCard1Selected();
    }

    /// <summary>
    /// 특정 캐릭터를 슬롯에 추가 (LevelUpCardUI에서 호출)
    /// </summary>
    public bool AddCharacterToSlot(CharacterData data)
    {
        int slotIndex = FindNextEmptySlotIndex();
        if (slotIndex < 0)
        {
            Debug.LogWarning("[카드선택] 레벨업 시 사용할 빈 슬롯이 없습니다! → 캐릭터 추가 안함");
            return false;
        }

        var slot = playerSlots[slotIndex];

        Debug.Log($"[카드선택] 레벨업용 빈 슬롯 발견! 슬롯 인덱스: {slotIndex}");
        slot.AssignCharacterData(data);

        Debug.Log($"[카드선택] 레벨업으로 캐릭터 생성 완료: {data.name}, slot={slotIndex}");
        return true;
    }
    private int FindNextEmptySlotIndex()
    {
        // 슬롯 0부터 순서대로 빈 슬롯 찾기
        for (int i = 0; i < playerSlots.Count; i++)
        {
            if (playerSlots[i].IsEmpty())
                return i;
        }

        // 없으면 -1
        Debug.Log("[카드선택] FindNextEmptySlotIndex → 사용 가능한 빈 슬롯 없음");
        return -1;
    }
    PlayerSlot FindNextEmptySlot()
    {
        List<PlayerSlot> emptySlots = new List<PlayerSlot>();

        foreach (PlayerSlot slot in playerSlots)
        {
            if (slot == null)
                continue;

            // LCB: Use slot's state flag
            if (slot.IsEmpty())
            {
                emptySlots.Add(slot);
            }
            else
            {
                Debug.Log($"[카드선택] 슬롯 {slot.slotIndex} 은(는) 이미 사용중");
            }
        }

        if (emptySlots.Count == 0)
        {
            Debug.Log("[카드선택] FindNextEmptySlot → 사용 가능한 빈 슬롯 없음");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, emptySlots.Count);
        PlayerSlot selectedSlot = emptySlots[randomIndex];

        Debug.Log($"[카드선택] 빈 슬롯 개수: {emptySlots.Count}, 선택 인덱스: {randomIndex}, 최종 슬롯 번호: {selectedSlot.slotIndex}");

        return selectedSlot;
    }

    public void ShowCardPanel()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
            LoadTwoRandomCharacters();
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
