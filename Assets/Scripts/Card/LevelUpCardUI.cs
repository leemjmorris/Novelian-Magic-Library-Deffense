using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
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

    [Header("Card Data (Assign 5 cards for each type)")]
    public List<CharacterData> characterCards; // 5 character cards
    // TODO: Create CardData ScriptableObject and uncomment below
    // public List<CardData> statCards;          // 5 stat buff cards
    // public List<CardData> buffCards;          // 5 buff cards
    // public List<CardData> debuffCards;        // 5 debuff cards
    // public List<CardData> skillCards;         // 5 skill cards

    [Header("Card Selection Manager")]
    public CardSelectionManager cardSelectionManager; // LMJ: Direct reference to CardSelectionManager

    // Card selection timeout (Issue spec: 20 seconds)
    private const float SELECTION_TIME = 20f;

    // Card selection complete flag
    private bool isCardSelected = false;

    // Selected card info (for effect application)
    private CardType selectedCard1Type;
    private CardType selectedCard2Type;
    private int selectedCard1Index;
    private int selectedCard2Index;
    // Cancellation token for selection timer
    private CancellationTokenSource selectionCts = new CancellationTokenSource();  // ← 추가


    void Start()
    {
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
        if (characterCards == null || characterCards.Count < 2)
        {
            // Debug.LogError("[LevelUpCardUI] Character data has less than 2 entries!");
            return;
        }

        // Select random 2 (prevent duplicate)
        int idx1 = UnityEngine.Random.Range(0, characterCards.Count);
        int idx2 = idx1;
        while (idx2 == idx1 && characterCards.Count > 1)
        {
            idx2 = UnityEngine.Random.Range(0, characterCards.Count);
        }

        // Store selection info
        selectedCard1Type = CardType.Character;
        selectedCard2Type = CardType.Character;
        selectedCard1Index = idx1;
        selectedCard2Index = idx2;

        // Update card 1 UI
        UpdateCharacterCardUI(card1, characterCards[idx1]);

        // Update card 2 UI
        UpdateCharacterCardUI(card2, characterCards[idx2]);
    }

    /// <summary>
    /// LCB: Regular level up - 2 random cards (from all types)
    /// TODO: Currently only implements characters, expand to all 5 types later
    /// </summary>
    void LoadTwoRandomCards()
    {
        // TODO: Temporarily loading only character cards (Issue #139 expansion planned)
        // Future: Add CardType.Stat, Buff, Debuff, Skill
        LoadTwoCharacterCards();
    }

    /// <summary>
    /// LCB: Update character card UI
    /// </summary>
    void UpdateCharacterCardUI(GameObject cardObj, CharacterData data)
    {
        if (cardObj == null || data == null) return;

        CharacterCard charCard = cardObj.GetComponent<CharacterCard>();
        if (charCard != null)
        {
            charCard.UpdateCharacter(
                data.characterSprite,
                data.characterName,
                data.genreType
            );
        }
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


        // Debug.Log($"[LevelUpCardUI] Card 1 selected (Type: {selectedCard1Type}, Index: {selectedCard1Index})");

        // Apply card effect
        ApplyCardEffect(selectedCard1Type, selectedCard1Index);
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

        // Debug.Log($"[LevelUpCardUI] Card 2 selected (Type: {selectedCard2Type}, Index: {selectedCard2Index})");

        // Apply card effect
        ApplyCardEffect(selectedCard2Type, selectedCard2Index);
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
    /// LCB: Apply character card effect (Add character to slot via CardSelectionManager)
    /// </summary>
    void ApplyCharacterCard(int index)
    {
        // Debug.Log($"[LevelUpCardUI] ApplyCharacterCard() called with index: {index}");

        if (index < 0 || index >= characterCards.Count)
        {
            Debug.LogError($"[LevelUpCardUI] Invalid character index: {index} (characterCards.Count: {characterCards?.Count ?? 0})");
            return;
        }

        CharacterData selectedChar = characterCards[index];
        // Debug.Log($"[LevelUpCardUI] Character selected: {selectedChar?.characterName ?? "NULL"}");

        // Use direct reference to CardSelectionManager
        if (cardSelectionManager != null)
        {
            // Debug.Log($"[LevelUpCardUI] Calling CardSelectionManager.AddCharacterToSlot()");
            cardSelectionManager.AddCharacterToSlot(selectedChar);
            // Debug.Log($"[LevelUpCardUI] Character added to slot successfully");
        }
        else
        {
            Debug.LogError("[LevelUpCardUI] CardSelectionManager reference is null! Inspector에서 CardManager GameObject를 할당해주세요.");
        }
    }
}
