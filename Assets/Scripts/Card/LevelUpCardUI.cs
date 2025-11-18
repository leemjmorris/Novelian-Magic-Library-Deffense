using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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


    async void Start()
    {
        // CharacterPlacementManager 찾기
        GameObject managerObj = GameObject.FindGameObjectWithTag("CharacterPlacementManager");
        if (managerObj != null)
        {
            placementManager = managerObj.GetComponent<CharacterPlacementManager>();
        }

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
    /// </summary>
    void LoadTwoCharacterCards()
    {
        if (availableCharacterIds == null || availableCharacterIds.Count < 2)
        {
            Debug.LogError("[LevelUpCardUI] 사용 가능한 캐릭터 ID가 2개 미만입니다!");
            return;
        }

        // Select random 2 (prevent duplicate)
        int idx1 = UnityEngine.Random.Range(0, availableCharacterIds.Count);
        int idx2 = idx1;
        while (idx2 == idx1 && availableCharacterIds.Count > 1)
        {
            idx2 = UnityEngine.Random.Range(0, availableCharacterIds.Count);
        }

        // Store selection info
        selectedCard1Type = CardType.Character;
        selectedCard2Type = CardType.Character;
        selectedCard1Id = availableCharacterIds[idx1];
        selectedCard2Id = availableCharacterIds[idx2];

        // Update card 1 UI
        UpdateCharacterCardUI(card1, selectedCard1Id);

        // Update card 2 UI
        UpdateCharacterCardUI(card2, selectedCard2Id);

        Debug.Log($"[LevelUpCardUI] 랜덤 캐릭터 선택: ID {selectedCard1Id}, ID {selectedCard2Id}");
    }

    /// <summary>
    /// LCB: Regular level up - 2 random cards (from all types)
    /// </summary>
    void LoadTwoRandomCards()
    {
        // 랜덤으로 2개의 카드 타입 선택 (중복 가능)
        CardType[] allTypes = { CardType.Character, CardType.Stat, CardType.Buff, CardType.Debuff, CardType.Skill };

        // Card 1 랜덤 선택
        selectedCard1Type = allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
        if (selectedCard1Type == CardType.Character)
        {
            selectedCard1Id = availableCharacterIds[UnityEngine.Random.Range(0, availableCharacterIds.Count)];
            UpdateCharacterCardUI(card1, selectedCard1Id);
        }

        // Card 2 랜덤 선택
        selectedCard2Type = allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
        if (selectedCard2Type == CardType.Character)
        {
            selectedCard2Id = availableCharacterIds[UnityEngine.Random.Range(0, availableCharacterIds.Count)];
            UpdateCharacterCardUI(card2, selectedCard2Id);
        }

        Debug.Log($"[LevelUpCardUI] 랜덤 카드 선택: Card1={selectedCard1Type}, Card2={selectedCard2Type}");
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
        // Debug.Log($"[LevelUpCardUI] OnCard1Click() called! isCardSelected: {isCardSelected}");

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
        // Debug.Log($"[LevelUpCardUI] OnCard2Click() called! isCardSelected: {isCardSelected}");

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
    /// TODO: Implement effects for each card type
    /// </summary>
    void ApplyCardEffect(CardType type, int index)
    {
        switch (type)
        {
            case CardType.Character:
                ApplyCharacterCard(index);
                break;

            case CardType.Stat:
                // TODO: Stat increase logic
                Debug.Log($"[LevelUpCardUI] Stat card applied (Index: {index})");
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
    /// LCB: Apply character card effect (CharacterID 기반)
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

        // CharacterPlacementManager 직접 사용
        if (placementManager != null)
        {
            placementManager.SpawnCharacterById(characterId);
            Debug.Log($"[LevelUpCardUI] 캐릭터 ID {characterId} 배치 완료");
        }
        // Fallback: CardSelectionManager 사용
        else if (cardSelectionManager != null)
        {
            cardSelectionManager.AddCharacterToSlot(characterId);
            Debug.Log($"[LevelUpCardUI] CardSelectionManager를 통해 캐릭터 ID {characterId} 배치 완료");
        }
        else
        {
            Debug.LogError("[LevelUpCardUI] PlacementManager와 CardSelectionManager 모두 null입니다!");
        }
    }
}
