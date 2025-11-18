using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

    [Header("사용 가능한 캐릭터 ID")]
    [SerializeField] private List<int> availableCharacterIds = new List<int> { 1, 2, 3, 4, 5 };

    // 카드 선택 타임아웃 (20초)
    private const float SELECTION_TIME = 20f;

    // 선택된 캐릭터 ID
    private int selectedCard1Id;
    private int selectedCard2Id;
    private bool isCardSelected = false;

    // 카드 스프라이트 캐시 (Addressable로 로드)
    private Dictionary<int, Sprite> cardSprites = new Dictionary<int, Sprite>();

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

    private async void Start()
    {
        // 카드 스프라이트 사전 로드
        await PreloadCardSprites();
    }

    /// <summary>
    /// 카드 스프라이트 사전 로드 (Addressable)
    /// </summary>
    private async UniTask PreloadCardSprites()
    {
        Debug.Log("[CardSelectionManager] Preloading card sprites...");

        foreach (int characterId in availableCharacterIds)
        {
            try
            {
                string spriteKey = AddressableKey.GetCardSpriteKey(characterId);
                Sprite sprite = await Addressables.LoadAssetAsync<Sprite>(spriteKey).Task;
                cardSprites[characterId] = sprite;
                Debug.Log($"[CardSelectionManager] Loaded card sprite for ID {characterId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardSelectionManager] Failed to load card sprite for ID {characterId}: {e.Message}");
            }
        }

        Debug.Log($"[CardSelectionManager] Preloaded {cardSprites.Count}/{availableCharacterIds.Count} card sprites");
    }

    /// <summary>
    /// 게임 시작 시 카드 선택 (StageManager에서 호출)
    /// </summary>
    public async UniTask ShowStartCards()
    {
        isCardSelected = false;

        // 0. 스프라이트 로드 대기 (Start()에서 로드 중일 수 있음)
        while (cardSprites.Count < availableCharacterIds.Count)
        {
            await UniTask.Yield();
        }

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
        if (availableCharacterIds == null || availableCharacterIds.Count < 2)
        {
            Debug.LogError("[CardSelectionManager] 사용 가능한 캐릭터 ID가 2개 미만입니다!");
            return;
        }

        // 랜덤 선택 (중복 방지)
        int idx1 = UnityEngine.Random.Range(0, availableCharacterIds.Count);
        int idx2 = idx1;
        while (idx2 == idx1 && availableCharacterIds.Count > 1)
        {
            idx2 = UnityEngine.Random.Range(0, availableCharacterIds.Count);
        }

        selectedCard1Id = availableCharacterIds[idx1];
        selectedCard2Id = availableCharacterIds[idx2];

        // Card 1 업데이트
        UpdateCardUI(card1, selectedCard1Id);

        // Card 2 업데이트
        UpdateCardUI(card2, selectedCard2Id);

        Debug.Log($"[CardSelectionManager] 랜덤 캐릭터 선택: ID {selectedCard1Id}, ID {selectedCard2Id}");
    }

    /// <summary>
    /// 카드 UI 업데이트 (CharacterID 기반)
    /// </summary>
    void UpdateCardUI(GameObject cardObj, int characterId)
    {
        if (cardObj == null) return;

        CharacterCard charCard = cardObj.GetComponent<CharacterCard>();
        if (charCard == null) return;

        // 캐릭터 데이터 가져오기 (임시: 하드코딩, 나중에 CSV로 대체)
        string characterName = GetCharacterName(characterId);
        GenreType genreType = GetCharacterGenre(characterId);
        Sprite cardSprite = cardSprites.ContainsKey(characterId) ? cardSprites[characterId] : null;

        if (cardSprite == null)
        {
            Debug.LogWarning($"[CardSelectionManager] 캐릭터 ID {characterId}의 카드 스프라이트가 로드되지 않았습니다!");
        }

        charCard.UpdateCharacter(cardSprite, characterName, genreType);
    }

    /// <summary>
    /// 캐릭터 이름 가져오기 (임시: 하드코딩, 나중에 CSV로 대체)
    /// </summary>
    string GetCharacterName(int characterId)
    {
        // 나중에 CSV 연동: return CSVLoader.Get<CharacterTableData>(characterId).CharacterName;
        switch (characterId)
        {
            case 1: return "Horror Warrior";
            case 2: return "Romance Mage";
            case 3: return "Adventure Ranger";
            case 4: return "Comedy Jester";
            case 5: return "Mystery Detective";
            default: return "Unknown Character";
        }
    }

    /// <summary>
    /// 캐릭터 장르 가져오기 (임시: 하드코딩, 나중에 CSV로 대체)
    /// </summary>
    GenreType GetCharacterGenre(int characterId)
    {
        // 나중에 CSV 연동: return (GenreType)CSVLoader.Get<CharacterTableData>(characterId).GenreType;
        return (GenreType)characterId; // 1=Horror, 2=Romance, 3=Adventure, 4=Comedy, 5=Mystery
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

        SelectCard(selectedCard1Id);
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

        SelectCard(selectedCard2Id);
    }

    /// <summary>
    /// 카드 선택 처리
    /// </summary>
    void SelectCard(int characterId)
    {
        if (characterId <= 0)
        {
            Debug.LogError("[CardSelectionManager] 유효하지 않은 캐릭터 ID입니다!");
            return;
        }

        // CharacterPlacementManager를 사용한 월드 좌표 배치
        if (placementManager != null)
        {
            placementManager.SpawnCharacterById(characterId);
            Debug.Log($"[CardSelectionManager] 월드 좌표에 캐릭터 배치 완료: ID {characterId}");
        }
        else
        {
            // 레거시 UI 기반 슬롯 시스템 (다른 씬 호환)
            PlayerSlot emptySlot = FindNextEmptySlot();
            if (emptySlot != null)
            {
                Sprite cardSprite = cardSprites.ContainsKey(characterId) ? cardSprites[characterId] : null;
                GenreType genreType = GetCharacterGenre(characterId);

                if (cardSprite != null)
                {
                    emptySlot.AssignCharacterSprite(cardSprite, genreType);
                    Debug.Log($"[CardSelectionManager] UI 슬롯에 배치 완료: ID {characterId}");
                }
                else
                {
                    Debug.LogError($"[CardSelectionManager] 캐릭터 ID {characterId}의 스프라이트가 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning("[CardSelectionManager] 빈 슬롯이 없습니다!");
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
    public void AddCharacterToSlot(int characterId)
    {
        if (characterId <= 0)
        {
            Debug.LogError("[CardSelectionManager] 유효하지 않은 캐릭터 ID입니다!");
            return;
        }

        // SelectCard와 동일한 로직 사용
        SelectCard(characterId);
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
