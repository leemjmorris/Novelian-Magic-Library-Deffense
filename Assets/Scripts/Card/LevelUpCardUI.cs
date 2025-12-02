using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LCB: Level-up card selection UI controller
/// Issue #139 - Level-up card selection UI system
///
/// Features:
/// - First level up: Display only 2 character cards
/// - Regular level up: Random selection from all card types
/// - 20 second timer & 50% auto-select on timeout
/// - Reuses existing cardPanel
/// </summary>
public class LevelUpCardUI : MonoBehaviour
{
    [Header("Existing Card Panel (Assign in Hierarchy)")]
    public GameObject cardPanel; // cardPanel referenced by UIManager
    public CanvasGroup canvasGroup; // LCB: For show/hide without SetActive (keeps FindWithTag working)

    [Header("2 Cards (Assign in Hierarchy)")]
    public GameObject card1; // First card object
    public GameObject card2; // Second card object

    [Header("Timer Text (Optional)")]
    public TextMeshProUGUI timerText; // 20 second timer display (optional)

    [Header("사용 가능한 캐릭터 ID")]
    [SerializeField] private List<int> availableCharacterIds = new List<int> { 1, 2, 3, 4, 5 };
    // TODO: Create CardData ScriptableObject and uncomment below
    // public List<CardData> statCards;          // 5 stat buff cards
    // public List<CardData> buffCards;          // 5 buff cards
    // public List<CardData> debuffCards;        // 5 debuff cards
    // public List<CardData> skillCards;         // 5 skill cards

    [Header("Card Selection Manager")]
    public CardSelectionManager cardSelectionManager; // LMJ: Direct reference to CardSelectionManager

    [Header("CharacterPlacementManager")]
    private CharacterPlacementManager placementManager;

    // Card selection timeout (Issue spec: 20 seconds)
    private const float SELECTION_TIME = 20f;

    // Card selection complete flag
    private bool isCardSelected = false;

    // Selected card info (CharacterID 기반)
    private CardType selectedCard1Type;
    private CardType selectedCard2Type;
    private int selectedCard1Id;
    private int selectedCard2Id;

    // Cancellation token for selection timer
    private CancellationTokenSource selectionCts = new CancellationTokenSource();

    // 카드 스프라이트 캐시
    private Dictionary<int, Sprite> cardSprites = new Dictionary<int, Sprite>();

    #region Issue #349 - 스텟 카드 시스템

    /// <summary>
    /// JML: 스텟 카드 ID 범위 정의 (CardLevelTable.csv 기준)
    /// StatType enum과 매핑
    /// </summary>
    private static readonly Dictionary<StatType, int[]> statCardIdRanges = new Dictionary<StatType, int[]>
    {
        { StatType.Damage, new int[] { 25001, 25002, 25003 } },         // 공격력 증가
        { StatType.CritMultiplier, new int[] { 25004, 25005, 25006 } }, // 치명타 배율
        { StatType.AttackSpeed, new int[] { 25007, 25008, 25009 } },    // 공격속도
        { StatType.CritChance, new int[] { 25010, 25011, 25012 } },     // 치명타 확률
        { StatType.ProjectileSpeed, new int[] { 25013, 25014, 25015 } },// 투사체 속도
        { StatType.TotalDamage, new int[] { 25016, 25017, 25018 } },    // 총 공격력
        { StatType.BonusDamage, new int[] { 25019, 25020, 25021 } },    // 추가 데미지
        { StatType.HealthRegen, new int[] { 25022, 25023, 25024 } },    // 체력 회복
        { StatType.Range, new int[] { 25085, 25086, 25087 } }           // 사거리 증가
    };

    /// <summary>
    /// JML: 현재 활성화된 스텟 타입 (프리팹이 준비된 것만)
    /// </summary>
    private static readonly StatType[] availableStatTypes = new StatType[]
    {
        StatType.Damage,           // StatCard-Damage.prefab
        StatType.AttackSpeed,      // StatCard-AttackSpeed.prefab
        StatType.ProjectileSpeed,  // StatCard-ProjectileSpeed.prefab
        StatType.Range             // StatCard-Range.prefab
    };

    // 스텟 카드 현재 티어 추적 (StatType별로 현재 티어 저장)
    private Dictionary<StatType, int> statCardCurrentTiers = new Dictionary<StatType, int>();

    // 선택된 스텟 카드 정보
    private StatType selectedStatCard1Type;
    private StatType selectedStatCard2Type;
    private int selectedStatCard1Tier;
    private int selectedStatCard2Tier;

    #endregion

    async void Start()
    {
        // CharacterPlacementManager 찾기 (Tag.CharacterInfoPanel 사용)
        GameObject managerObj = GameObject.FindGameObjectWithTag(Tag.CharacterInfoPanel);
        if (managerObj != null)
        {
            placementManager = managerObj.GetComponent<CharacterPlacementManager>();
        }

        // JML: 카드 버튼 onClick 이벤트 자동 연결 (Issue #349)
        SetupCardButtons();

        // 카드 스프라이트 사전 로드
        await PreloadCardSprites();

        // LCB: Initially hide panel using CanvasGroup (keeps GameObject active for FindWithTag)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (cardPanel != null)
        {
            // LCB: Fallback to SetActive if CanvasGroup not assigned
            cardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// JML: 카드 버튼 onClick 이벤트 자동 연결 (Issue #349)
    /// Inspector에서 수동 연결 없이도 동작하도록 함
    /// </summary>
    private void SetupCardButtons()
    {
        // Card1 버튼 연결
        if (card1 != null)
        {
            Button btn1 = card1.GetComponent<Button>();
            if (btn1 == null)
            {
                btn1 = card1.GetComponentInChildren<Button>();
            }

            if (btn1 != null)
            {
                btn1.onClick.RemoveAllListeners();
                btn1.onClick.AddListener(OnCard1Click);
                Debug.Log("[LevelUpCardUI] Card1 버튼 onClick 이벤트 연결 완료");
            }
            else
            {
                Debug.LogWarning("[LevelUpCardUI] Card1에 Button 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[LevelUpCardUI] card1이 null입니다! Inspector에서 할당해주세요.");
        }

        // Card2 버튼 연결
        if (card2 != null)
        {
            Button btn2 = card2.GetComponent<Button>();
            if (btn2 == null)
            {
                btn2 = card2.GetComponentInChildren<Button>();
            }

            if (btn2 != null)
            {
                btn2.onClick.RemoveAllListeners();
                btn2.onClick.AddListener(OnCard2Click);
                Debug.Log("[LevelUpCardUI] Card2 버튼 onClick 이벤트 연결 완료");
            }
            else
            {
                Debug.LogWarning("[LevelUpCardUI] Card2에 Button 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[LevelUpCardUI] card2가 null입니다! Inspector에서 할당해주세요.");
        }
    }

    private async UniTask PreloadCardSprites()
    {
        Debug.Log("[LevelUpCardUI] Preloading card sprites...");

        foreach (int characterId in availableCharacterIds)
        {
            try
            {
                string spriteKey = AddressableKey.GetCardSpriteKey(characterId);
                Sprite sprite = await Addressables.LoadAssetAsync<Sprite>(spriteKey).Task;
                cardSprites[characterId] = sprite;
                Debug.Log($"[LevelUpCardUI] Loaded card sprite for ID {characterId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelUpCardUI] Failed to load card sprite for ID {characterId}: {e.Message}");
            }
        }

        Debug.Log($"[LevelUpCardUI] Preloaded {cardSprites.Count}/{availableCharacterIds.Count} card sprites");
    }

    /// <summary>
    /// LCB: Display 2 cards on level up (Called from StageManager.LevelUp())
    /// </summary>
    /// <param name="currentLevel">Current level (1 means first level up)</param>
    public async UniTask ShowCards(int currentLevel)
    {
        isCardSelected = false;

        // LCB: Show panel using CanvasGroup (preferred) or SetActive (fallback)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else if (cardPanel != null)
        {
            cardPanel.SetActive(true);
        }
        else
        {
            // Debug.LogError("[LevelUpCardUI] cardPanel and canvasGroup are both null!");
        }

        // 2. Load 2 cards
        bool isFirstLevelUp = (currentLevel == 0);
        if (isFirstLevelUp)
        {
            LoadTwoCharacterCards();
        }
        else
        {
            LoadTwoRandomCards();
        }

        // 3. Start 20 second timer & wait for selection
        await WaitForSelection();

        // LCB: Hide panel using CanvasGroup (preferred) or SetActive (fallback)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// LCB: First level up - Display only 2 character cards
    /// JML: Issue #349 - 필드 캐릭터도 카드 풀에 포함
    /// </summary>
    void LoadTwoCharacterCards()
    {
        // JML: 필드 캐릭터 포함 카드 풀 구성
        List<int> cardPool = BuildCharacterCardPool();

        if (cardPool == null || cardPool.Count < 2)
        {
            Debug.LogError("[LevelUpCardUI] 사용 가능한 캐릭터 ID가 2개 미만입니다!");
            return;
        }

        // Select random 2 (prevent duplicate)
        int idx1 = UnityEngine.Random.Range(0, cardPool.Count);
        int idx2 = idx1;
        while (idx2 == idx1 && cardPool.Count > 1)
        {
            idx2 = UnityEngine.Random.Range(0, cardPool.Count);
        }

        // Store selection info
        selectedCard1Type = CardType.Character;
        selectedCard2Type = CardType.Character;
        selectedCard1Id = cardPool[idx1];
        selectedCard2Id = cardPool[idx2];

        // Update card 1 UI
        UpdateCharacterCardUI(card1, selectedCard1Id);

        // Update card 2 UI
        UpdateCharacterCardUI(card2, selectedCard2Id);

        Debug.Log($"[LevelUpCardUI] 랜덤 캐릭터 선택: ID {selectedCard1Id}, ID {selectedCard2Id}");
    }

    /// <summary>
    /// JML: 캐릭터 카드 풀 구성 (Issue #349)
    /// 기본 캐릭터 목록 + 필드 캐릭터 (중복 허용)
    /// 최종 레벨 캐릭터는 제외
    /// </summary>
    List<int> BuildCharacterCardPool()
    {
        List<int> cardPool = new List<int>();

        // 1. 기본 캐릭터 목록 추가
        cardPool.AddRange(availableCharacterIds);

        // 2. 필드 캐릭터 추가 (성급 업그레이드 가능하도록)
        if (placementManager != null)
        {
            List<int> fieldCharacterIds = placementManager.GetAllCharacterIds();
            foreach (int charId in fieldCharacterIds)
            {
                // 최종 레벨이 아닌 캐릭터만 추가 (중복 추가)
                if (!IsCharacterFinalLevel(charId))
                {
                    cardPool.Add(charId);
                }
            }
            Debug.Log($"[LevelUpCardUI] Card pool: {availableCharacterIds.Count} base + {fieldCharacterIds.Count} field = {cardPool.Count} total");
        }

        return cardPool;
    }

    /// <summary>
    /// JML: 캐릭터가 최종 레벨인지 확인 (Issue #349)
    /// TODO: 캐릭터 레벨/티어 시스템 구현 시 확장
    /// </summary>
    bool IsCharacterFinalLevel(int characterId)
    {
        // 현재는 캐릭터 티어 시스템이 없으므로 항상 false 반환
        // 추후 캐릭터 성급 시스템 구현 시 여기서 체크
        return false;
    }

    /// <summary>
    /// LCB: Regular level up - 2 random cards (from all types)
    /// JML: Issue #349 - 스텟 카드 지원 추가
    /// </summary>
    void LoadTwoRandomCards()
    {
        // JML: 현재는 캐릭터 카드와 스텟 카드만 지원
        // 추후 Buff, Debuff, Skill 카드 추가 시 확장
        CardType[] availableTypes = { CardType.Character, CardType.Stat };

        // Card 1 랜덤 선택
        selectedCard1Type = availableTypes[UnityEngine.Random.Range(0, availableTypes.Length)];
        SetupCard(card1, 0, selectedCard1Type);

        // Card 2 랜덤 선택
        selectedCard2Type = availableTypes[UnityEngine.Random.Range(0, availableTypes.Length)];
        SetupCard(card2, 1, selectedCard2Type);

        Debug.Log($"[LevelUpCardUI] 랜덤 카드 선택: Card1={selectedCard1Type}, Card2={selectedCard2Type}");
    }

    /// <summary>
    /// JML: 카드 타입에 따라 UI 설정
    /// </summary>
    void SetupCard(GameObject cardObj, int cardIndex, CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Character:
                int charId = availableCharacterIds[UnityEngine.Random.Range(0, availableCharacterIds.Count)];
                if (cardIndex == 0)
                    selectedCard1Id = charId;
                else
                    selectedCard2Id = charId;
                UpdateCharacterCardUI(cardObj, charId);
                break;

            case CardType.Stat:
                SetupStatCard(cardObj, cardIndex);
                break;

            default:
                Debug.LogWarning($"[LevelUpCardUI] Unsupported card type: {cardType}");
                break;
        }
    }

    /// <summary>
    /// JML: 스텟 카드 UI 설정 (Issue #349)
    /// </summary>
    void SetupStatCard(GameObject cardObj, int cardIndex)
    {
        // 사용 가능한 스텟 타입 중에서 최종 레벨이 아닌 것만 필터링
        List<StatType> validStatTypes = new List<StatType>();
        foreach (var statType in availableStatTypes)
        {
            if (!IsStatCardFinalLevel(statType))
            {
                validStatTypes.Add(statType);
            }
        }

        // 모든 스텟 카드가 최종 레벨이면 랜덤으로 하나 선택 (효과 없음)
        if (validStatTypes.Count == 0)
        {
            validStatTypes.AddRange(availableStatTypes);
            Debug.Log("[LevelUpCardUI] All stat cards at final level, showing anyway");
        }

        // 랜덤 스텟 타입 선택
        StatType selectedType = validStatTypes[UnityEngine.Random.Range(0, validStatTypes.Count)];
        int currentTier = GetStatCardCurrentTier(selectedType);

        // 선택 정보 저장
        if (cardIndex == 0)
        {
            selectedStatCard1Type = selectedType;
            selectedStatCard1Tier = currentTier;
        }
        else
        {
            selectedStatCard2Type = selectedType;
            selectedStatCard2Tier = currentTier;
        }

        // UI 업데이트 (CharacterCard 컴포넌트 재사용)
        UpdateStatCardUI(cardObj, selectedType, currentTier);
    }

    /// <summary>
    /// JML: 스텟 카드 UI 업데이트
    /// </summary>
    void UpdateStatCardUI(GameObject cardObj, StatType statType, int tier)
    {
        if (cardObj == null) return;

        CharacterCard charCard = cardObj.GetComponent<CharacterCard>();
        if (charCard == null) return;

        // 스텟 타입별 이름
        string statName = GetStatTypeName(statType);
        string displayName = $"{statName} Tier {tier}";

        // TODO: 스텟 카드 전용 스프라이트 로드 (현재는 null)
        Sprite cardSprite = null;

        // GenreType은 스텟 카드에서는 의미 없음 (0 사용)
        charCard.UpdateCharacter(cardSprite, displayName, 0);

        Debug.Log($"[LevelUpCardUI] Stat card UI updated: {statType} Tier {tier}");
    }

    /// <summary>
    /// JML: StatType 한글 이름 반환
    /// </summary>
    string GetStatTypeName(StatType statType)
    {
        switch (statType)
        {
            case StatType.Damage: return "공격력 증가";
            case StatType.CritMultiplier: return "치명타 배율";
            case StatType.AttackSpeed: return "공격속도 증가";
            case StatType.CritChance: return "치명타 확률";
            case StatType.ProjectileSpeed: return "투사체 속도";
            case StatType.TotalDamage: return "총 공격력";
            case StatType.BonusDamage: return "추가 데미지";
            case StatType.HealthRegen: return "체력 회복";
            case StatType.Range: return "사거리 증가";
            default: return "알 수 없음";
        }
    }

    /// <summary>
    /// LCB: Update character card UI (CharacterID 기반)
    /// </summary>
    void UpdateCharacterCardUI(GameObject cardObj, int characterId)
    {
        if (cardObj == null) return;

        CharacterCard charCard = cardObj.GetComponent<CharacterCard>();
        if (charCard == null) return;

        // 캐릭터 데이터 가져오기 (CardSelectionManager와 동일)
        string characterName = GetCharacterName(characterId);
        GenreType genreType = GetCharacterGenre(characterId);
        Sprite cardSprite = cardSprites.ContainsKey(characterId) ? cardSprites[characterId] : null;

        if (cardSprite == null)
        {
            Debug.LogWarning($"[LevelUpCardUI] 캐릭터 ID {characterId}의 카드 스프라이트가 로드되지 않았습니다!");
        }

        charCard.UpdateCharacter(cardSprite, characterName, genreType);
    }

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

    GenreType GetCharacterGenre(int characterId)
    {
        // 나중에 CSV 연동: return (GenreType)CSVLoader.Get<CharacterTableData>(characterId).GenreType;
        return (GenreType)characterId;
    }

    /// <summary>
    /// LCB: 20 second timer & wait for card selection
    /// ignoreTimeScale=true allows operation even when Time.timeScale=0
    /// </summary>
    async UniTask WaitForSelection()
    {   
        selectionCts?.Dispose(); // Dispose previous if exists
        selectionCts = new CancellationTokenSource();
        float remainingTime = SELECTION_TIME;

        try
        {
            while (remainingTime > 0 && !isCardSelected)
            {
                // Update timer text (if exists)
                if (timerText != null)
                {
                    timerText.text = $"{(int)remainingTime}s";
                }

                // Wait 1 second (ignoreTimeScale ignores pause, cancellationToken for immediate cancel)
                await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: selectionCts.Token);
                remainingTime -= 1f;
            }

            // 50% auto-select on timeout
            if (!isCardSelected)
            {
                // Debug.Log("[LevelUpCardUI] 20 second timeout!");
                AutoSelectCard();
            }
        }
        catch (OperationCanceledException)
        {
            // Debug.Log("[LevelUpCardUI] Selection timer cancelled");
        }

        finally
        {
            selectionCts.Dispose();
            selectionCts = null;
        }
    
    }

    /// <summary>
    /// LCB: Auto-select with 50% probability on timeout (Issue spec)
    /// </summary>
    void AutoSelectCard()
    {
        float random = UnityEngine.Random.Range(0f, 1f);

        if (random < 0.5f)
        {
            // Debug.Log("[LevelUpCardUI] Auto-select: Card 1");
            OnCard1Click();
        }
        else
        {
            // Debug.Log("[LevelUpCardUI] Auto-select: Card 2");
            OnCard2Click();
        }
    }

    /// <summary>
    /// LCB: Card 1 click handler (Connect to Button.onClick)
    /// </summary>
    public void OnCard1Click()
    {
        Debug.Log($"[LevelUpCardUI] OnCard1Click() called! isCardSelected: {isCardSelected}, Card1Type: {selectedCard1Type}");

        if (isCardSelected)
        {
            // Debug.LogWarning("[LevelUpCardUI] Card already selected, ignoring click");
            return; // Prevent duplicate clicks
        }

        isCardSelected = true;

        // Cancel timer immediately and reset timer text to 0
        if (isCardSelected)
        {
            selectionCts?.Cancel();
            if (timerText != null)
            {
                timerText.text = "0s";
            }
        }


        // Debug.Log($"[LevelUpCardUI] Card 1 selected (Type: {selectedCard1Type}, ID: {selectedCard1Id})");

        // Apply card effect (cardIndex: 0 = card1)
        ApplyCardEffect(selectedCard1Type, 0);
    }

    /// <summary>
    /// LCB: Card 2 click handler (Connect to Button.onClick)
    /// </summary>
    public void OnCard2Click()
    {
        Debug.Log($"[LevelUpCardUI] OnCard2Click() called! isCardSelected: {isCardSelected}, Card2Type: {selectedCard2Type}");

        if (isCardSelected)
        {
            // Debug.LogWarning("[LevelUpCardUI] Card already selected, ignoring click");
            return; // Prevent duplicate clicks
        }

        isCardSelected = true;

        // Cancel timer immediately and reset timer text to 0
        selectionCts?.Cancel();
        if (timerText != null)
        {
            timerText.text = "0s";
        }

        // Debug.Log($"[LevelUpCardUI] Card 2 selected (Type: {selectedCard2Type}, ID: {selectedCard2Id})");

        // Apply card effect (cardIndex: 1 = card2)
        ApplyCardEffect(selectedCard2Type, 1);
    }

    /// <summary>
    /// LCB: Apply card effect
    /// JML: Issue #349 - 스텟 카드 전역 적용 로직 구현
    /// </summary>
    void ApplyCardEffect(CardType type, int index)
    {
        switch (type)
        {
            case CardType.Character:
                ApplyCharacterCard(index);
                break;

            case CardType.Stat:
                ApplyStatCard(index);
                break;

            case CardType.Buff:
                // TODO: Buff application logic
                Debug.Log($"[LevelUpCardUI] Buff card applied (Index: {index})");
                break;

            case CardType.Debuff:
                // TODO: Debuff application logic
                Debug.Log($"[LevelUpCardUI] Debuff card applied (Index: {index})");
                break;

            case CardType.Skill:
                // TODO: Skill addition logic
                Debug.Log($"[LevelUpCardUI] Skill card applied (Index: {index})");
                break;
        }
    }

    /// <summary>
    /// JML: 스텟 카드 효과 적용 (Issue #349)
    /// StageManager를 통해 전역 버프로 적용
    /// </summary>
    void ApplyStatCard(int cardIndex)
    {
        Debug.Log($"[LevelUpCardUI] ApplyStatCard() called with cardIndex: {cardIndex}");

        // cardIndex: 0 = card1, 1 = card2
        StatType statType = (cardIndex == 0) ? selectedStatCard1Type : selectedStatCard2Type;
        int tier = (cardIndex == 0) ? selectedStatCard1Tier : selectedStatCard2Tier;

        Debug.Log($"[LevelUpCardUI] StatType: {statType}, Tier: {tier}");

        // CardLevelTable에서 데이터 조회
        int cardLevelId = GetStatCardIdByTier(statType, tier);
        Debug.Log($"[LevelUpCardUI] CardLevelId: {cardLevelId}");

        // CSVLoader 상태 확인
        if (CSVLoader.Instance == null)
        {
            Debug.LogError("[LevelUpCardUI] CSVLoader.Instance is null!");
            return;
        }
        if (!CSVLoader.Instance.IsInit)
        {
            Debug.LogError("[LevelUpCardUI] CSVLoader is not initialized!");
            return;
        }

        CardLevelData cardData = CSVLoader.Instance.GetData<CardLevelData>(cardLevelId);

        if (cardData == null)
        {
            Debug.LogError($"[LevelUpCardUI] CardLevelData not found for ID: {cardLevelId}. Check if CardLevelTable.csv is loaded correctly.");
            return;
        }

        Debug.Log($"[LevelUpCardUI] CardData found: Tier={cardData.Tier}, value_change={cardData.value_change}, Is_Final_Level={cardData.Is_Final_Level}");

        // StageManager를 통해 전역 버프 적용
        var stageManager = GameManager.Instance?.Stage;
        if (stageManager != null)
        {
            stageManager.ApplyGlobalStatBuff(statType, cardData.value_change);
            Debug.Log($"[LevelUpCardUI] Stat card applied: {statType} Tier {tier} (+{cardData.value_change * 100f}%)");

            // 티어 업그레이드 (최대 3티어까지)
            if (tier < 3 && cardData.Is_Final_Level == 0)
            {
                statCardCurrentTiers[statType] = tier + 1;
                Debug.Log($"[LevelUpCardUI] {statType} upgraded to Tier {tier + 1}");
            }
        }
        else
        {
            Debug.LogError("[LevelUpCardUI] StageManager not found!");
        }
    }

    /// <summary>
    /// JML: StatType과 Tier로 CardLevelID 조회
    /// </summary>
    int GetStatCardIdByTier(StatType statType, int tier)
    {
        if (!statCardIdRanges.ContainsKey(statType))
        {
            Debug.LogError($"[LevelUpCardUI] Unknown StatType: {statType}");
            return 0;
        }

        int[] ids = statCardIdRanges[statType];
        int tierIndex = Mathf.Clamp(tier - 1, 0, ids.Length - 1);
        return ids[tierIndex];
    }

    /// <summary>
    /// JML: 스텟 카드의 현재 티어 조회 (없으면 1 반환)
    /// </summary>
    int GetStatCardCurrentTier(StatType statType)
    {
        return statCardCurrentTiers.TryGetValue(statType, out int tier) ? tier : 1;
    }

    /// <summary>
    /// JML: 스텟 카드가 최종 레벨인지 확인
    /// </summary>
    bool IsStatCardFinalLevel(StatType statType)
    {
        int tier = GetStatCardCurrentTier(statType);
        int cardLevelId = GetStatCardIdByTier(statType, tier);
        CardLevelData cardData = CSVLoader.Instance?.GetData<CardLevelData>(cardLevelId);
        return cardData?.Is_Final_Level == 1;
    }

    /// <summary>
    /// LCB: Apply character card effect (CharacterID 기반)
    /// JML: Issue #349 - 필드에 이미 있는 캐릭터면 성급 업그레이드
    /// </summary>
    void ApplyCharacterCard(int cardIndex)
    {
        // cardIndex: 0 = card1, 1 = card2
        int characterId = (cardIndex == 0) ? selectedCard1Id : selectedCard2Id;

        Debug.Log($"[LevelUpCardUI] ApplyCharacterCard() called with cardIndex: {cardIndex}, CharacterID: {characterId}");

        if (characterId <= 0)
        {
            Debug.LogError($"[LevelUpCardUI] 유효하지 않은 캐릭터 ID: {characterId}");
            return;
        }

        if (placementManager == null)
        {
            Debug.LogError("[LevelUpCardUI] CharacterPlacementManager를 찾을 수 없습니다!");
            return;
        }

        // JML: 필드에 이미 있는 캐릭터인지 확인
        var existingCharacter = placementManager.GetCharacterById(characterId);
        if (existingCharacter != null)
        {
            // 이미 필드에 있으면 성급 업그레이드
            UpgradeCharacterTier(existingCharacter, characterId);
        }
        else
        {
            // 새로운 캐릭터면 소환
            placementManager.SpawnCharacterById(characterId);
            Debug.Log($"[LevelUpCardUI] 캐릭터 ID {characterId} 배치 완료");
        }
    }

    /// <summary>
    /// JML: 캐릭터 성급 업그레이드 (Issue #349)
    /// 중복 캐릭터 선택 시 스탯 증가
    /// </summary>
    void UpgradeCharacterTier(Novelian.Combat.Character character, int characterId)
    {
        if (character == null) return;

        // 성급 업그레이드 버프 적용
        // 기본적으로 모든 스탯 10% 증가
        float upgradeBonus = 0.1f;

        character.ApplyStatBuff(StatType.Damage, upgradeBonus);
        character.ApplyStatBuff(StatType.AttackSpeed, upgradeBonus);

        Debug.Log($"[LevelUpCardUI] 캐릭터 ID {characterId} 성급 업그레이드! (데미지 +10%, 공격속도 +10%)");

        // TODO: 캐릭터별 성급 티어 추적 및 최대 성급 제한 구현
        // TODO: 성급에 따른 외형 변화 구현
    }
}
