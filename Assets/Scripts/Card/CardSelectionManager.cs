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

    // CharacterPlacementManager 캐싱
    private CharacterPlacementManager placementManager;

    private void Awake()
    {
        // CharacterPlacementManager를 태그로 찾아서 캐싱
        GameObject managerObj = GameObject.FindGameObjectWithTag("CharacterPlacementManager");
        if (managerObj != null)
        {
            placementManager = managerObj.GetComponent<CharacterPlacementManager>();
        }
    }

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
        else
        {
            Debug.LogError("[CardSelectionManager] cardPanel이 null입니다! Inspector에서 할당해주세요.");
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
                AutoSelectCard();
            }
        }
        catch (OperationCanceledException)
        {
            // Timer cancelled (normal when card selected)
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

        // Debug.Log($"랜덤 캐릭터 로드: {selectedCard1Data.characterName}, {selectedCard2Data.characterName}");
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
            OnCard1Selected();
        }
        else
        {
            OnCard2Selected();
        }
    }

    /// <summary>
    /// Card 1 클릭 시 호출 (Button.onClick에 연결)
    /// </summary>
    public void OnCard1Selected()
    {
        if (isCardSelected) return;

        isCardSelected = true;

        // 타이머 즉시 취소 및 0초 표시
        selectionCts?.Cancel();
        if (timerText != null)
        {
            timerText.text = "0s";
        }

        SelectCard(selectedCard1Data, card1);
    }

    /// <summary>
    /// Card 2 클릭 시 호출 (Button.onClick에 연결)
    /// </summary>
    public void OnCard2Selected()
    {
        if (isCardSelected) return;

        isCardSelected = true;

        // 타이머 즉시 취소 및 0초 표시
        selectionCts?.Cancel();
        if (timerText != null)
        {
            timerText.text = "0s";
        }

        SelectCard(selectedCard2Data, card2);
    }

    /// <summary>
    /// 카드 선택 처리
    /// </summary>
    void SelectCard(CharacterData data, GameObject cardObj)
    {
        if (data == null)
        {
            Debug.LogError("[CardSelectionManager] 선택된 데이터가 없습니다!");
            return;
        }

        CharacterCard charCard = cardObj?.GetComponent<CharacterCard>();
        if (charCard == null || charCard.characterImage == null)
        {
            Debug.LogError($"[CardSelectionManager] 카드 컴포넌트 또는 이미지가 null입니다!");
            return;
        }

        Sprite cardSprite = charCard.characterImage.sprite;
        if (cardSprite == null)
        {
            Debug.LogError("[CardSelectionManager] 카드 이미지가 null입니다!");
            return;
        }

        // CharacterPlacementManager를 사용한 월드 좌표 배치 (TestScene 전용)
        if (placementManager != null)
        {
            placementManager.SpawnCharacterAtRandomSlot(cardSprite);
            Debug.Log($"[CardSelectionManager] 월드 좌표에 캐릭터 배치 완료: {data.characterName}");
        }
        else
        {
            // 기존 UI 기반 슬롯 시스템 (다른 씬 호환)
            PlayerSlot emptySlot = FindNextEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.AssignCharacterSprite(cardSprite, data.genreType);
                // Debug.Log($"[CardSelectionManager] 슬롯에 배치 완료: {data.characterName}");
            }
            else
            {
                // Debug.LogWarning("[CardSelectionManager] 빈 슬롯이 없습니다!");
            }
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
    public void AddCharacterToSlot(CharacterData data)
    {
        if (data == null)
        {
            Debug.LogError("[CardSelectionManager] CharacterData is null!");
            return;
        }

        // CharacterPlacementManager를 사용한 월드 좌표 배치 (TestScene 전용)
        if (placementManager != null)
        {
            placementManager.SpawnCharacterAtRandomSlot(data.characterSprite);
            Debug.Log($"[CardSelectionManager] 월드 좌표에 캐릭터 추가 완료: {data.characterName}");
        }
        else
        {
            // 기존 UI 기반 슬롯 시스템 (다른 씬 호환)
            PlayerSlot emptySlot = FindNextEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.AssignCharacterSprite(data.characterSprite, data.genreType);
                // Debug.Log($"[CardSelectionManager] Character added to slot: {data.characterName}");
            }
            else
            {
                // Debug.LogWarning("[CardSelectionManager] No empty slot available!");
            }
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
