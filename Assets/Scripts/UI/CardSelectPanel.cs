using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Universal card selection panel for both character and ability cards
    /// - Game Start: 2 character cards only
    /// - Level Up: 2 random cards (mix of character + ability cards)
    /// Selecting a card instantly processes it and closes panel
    /// </summary>
    public class CardSelectPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panel;

        [Header("Card Container")]
        [SerializeField] private Transform cardContainer;

        [Header("Card Prefab")]
        [SerializeField] private GameObject cardPrefab;

        [Header("Settings")]
        [SerializeField] private bool pauseOnGameStart = true; // Pause for game start
        [SerializeField] private bool pauseOnLevelUp = true; // Pause for level-up

        // Events
        public event Action<CardData> OnCardSelected; // Fires when card is clicked

        private GameObject[] cardInstances;
        private CharacterPlacementManager placementManager;
        private float previousTimeScale = 1f;
        private bool isPaused = false;

        public enum CardType
        {
            Character,
            Ability
        }

        public struct CardData
        {
            public CardType Type;
            public int Id;

            public CardData(CardType type, int id)
            {
                Type = type;
                Id = id;
            }
        }

        private async void Awake()
        {
            // Find CharacterPlacementManager in scene
            placementManager = FindFirstObjectByType<CharacterPlacementManager>();
            if (placementManager == null)
            {
                Debug.LogWarning("[CardSelectPanel] CharacterPlacementManager not found in scene!");
            }
            else
            {
                // Wait for CharacterPlacementManager to finish preloading characters
                Debug.Log("[CardSelectPanel] Waiting for CharacterPlacementManager to preload characters...");
                int maxWaitFrames = 300; // 5 seconds at 60fps
                int frameCount = 0;

                while (!placementManager.IsPreloadComplete() && frameCount < maxWaitFrames)
                {
                    await Cysharp.Threading.Tasks.UniTask.Yield();
                    frameCount++;
                }

                if (placementManager.IsPreloadComplete())
                {
                    Debug.Log("[CardSelectPanel] CharacterPlacementManager preload complete!");
                }
                else
                {
                    Debug.LogWarning("[CardSelectPanel] CharacterPlacementManager preload timeout after 5 seconds!");
                }
            }

            // Don't initialize panel here - let it stay in its current state
            // Panel will be opened/closed by other scripts calling OpenForGameStart() or OpenForLevelUp()
            Debug.Log("[CardSelectPanel] Awake completed, ready to use");
        }

        /// <summary>
        /// Initialize panel to closed state
        /// </summary>
        private void InitializePanel()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
            else
            {
                Debug.LogError("[CardSelectPanel] Panel GameObject not assigned!");
            }
        }

        /// <summary>
        /// Open panel for game start - 2 random character cards only
        /// </summary>
        public void OpenForGameStart()
        {
            if (panel == null) return;

            // Pause game for game start
            if (pauseOnGameStart)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                isPaused = true;
            }

            // Get 2 random character IDs
            int[] characterIds = GetRandomCharacterIds(2);
            CardData[] cards = new CardData[2];
            cards[0] = new CardData(CardType.Character, characterIds[0]);
            cards[1] = new CardData(CardType.Character, characterIds[1]);

            panel.SetActive(true);
            CreateCards(cards);

            Debug.Log("[CardSelectPanel] Opened for game start - 2 character cards (paused)");
        }

        /// <summary>
        /// Open panel for level up - 2 random cards (character or ability)
        /// If all slots are full, show only ability cards
        /// </summary>
        public void OpenForLevelUp()
        {
            if (panel == null) return;

            // Pause game if needed
            if (pauseOnLevelUp)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                isPaused = true;
            }

            // Check if there are empty slots
            bool hasEmptySlot = placementManager != null && placementManager.HasEmptySlot();

            CardData[] cards;
            if (hasEmptySlot)
            {
                // Get 2 random cards (mix of character and ability)
                cards = GetRandomMixedCards(2);
                Debug.Log("[CardSelectPanel] Opened for level up - 2 random cards (slots available)");
            }
            else
            {
                // All slots full: show only ability cards
                cards = GetRandomAbilityCards(2);
                Debug.Log("[CardSelectPanel] Opened for level up - 2 ability cards (all slots full)");
            }

            panel.SetActive(true);
            CreateCards(cards);
        }

        /// <summary>
        /// Open with specific cards (for testing or custom scenarios)
        /// </summary>
        public void OpenWithCards(CardData[] cards, bool pauseGame = false)
        {
            if (panel == null) return;
            if (cards == null || cards.Length != 2)
            {
                Debug.LogError("[CardSelectPanel] Must provide exactly 2 cards!");
                return;
            }

            // Pause game if requested
            if (pauseGame)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                isPaused = true;
            }

            panel.SetActive(true);
            CreateCards(cards);
        }

        /// <summary>
        /// Close card selection panel
        /// </summary>
        public void Close()
        {
            if (panel == null) return;

            panel.SetActive(false);

            // Resume game if it was paused
            if (isPaused)
            {
                Time.timeScale = previousTimeScale;
                isPaused = false;
            }

            ClearCards();
        }

        /// <summary>
        /// Create 2 cards in the container
        /// </summary>
        private void CreateCards(CardData[] cards)
        {
            ClearCards();

            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogError("[CardSelectPanel] Card container or prefab not assigned!");
                return;
            }

            cardInstances = new GameObject[2];

            for (int i = 0; i < 2; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, cardContainer);
                cardObj.name = $"{cards[i].Type}Card_{cards[i].Id}";

                // Setup card button
                UnityEngine.UI.Button cardButton = cardObj.GetComponent<UnityEngine.UI.Button>();
                if (cardButton != null)
                {
                    CardData cardData = cards[i];
                    cardButton.onClick.AddListener(() => OnCardClicked(cardData));
                }
                else
                {
                    Debug.LogWarning($"[CardSelectPanel] Card prefab missing Button component!");
                }

                // TODO: Set card visual data (sprite, name, etc.) based on card type and ID
                // Example: cardObj.GetComponent<Card>()?.SetData(cards[i]);

                cardInstances[i] = cardObj;
                Debug.Log($"[CardSelectPanel] Created {cards[i].Type} card (ID: {cards[i].Id})");
            }
        }

        /// <summary>
        /// Handle card click - process card and close panel
        /// </summary>
        private void OnCardClicked(CardData cardData)
        {
            // JML: 캐릭터 타입일 때만 CSV에서 이름 가져오기 (Issue #320)
            string cardName = cardData.Type == CardType.Character
                ? GetCharacterNameFromCSV(cardData.Id)
                : $"Ability_{cardData.Id}";
            Debug.Log($"[CardSelectPanel] 카드 선택: {cardName} (Type: {cardData.Type}, ID: {cardData.Id})");

            // Process card based on type
            if (cardData.Type == CardType.Character)
            {
                // Spawn character via CharacterPlacementManager
                if (placementManager != null)
                {
                    placementManager.SpawnCharacterById(cardData.Id);
                    Debug.Log($"[CardSelectPanel] 캐릭터 배치 완료: {cardName} (ID: {cardData.Id})");
                }
                else
                {
                    Debug.LogError("[CardSelectPanel] Cannot spawn character - CharacterPlacementManager is null!");
                }
            }
            else if (cardData.Type == CardType.Ability)
            {
                // TODO: Apply ability upgrade
                Debug.Log($"[CardSelectPanel] TODO: Apply ability upgrade ID {cardData.Id}");
                // Example: AbilityManager.ApplyAbility(cardData.Id);
            }

            // Fire event
            OnCardSelected?.Invoke(cardData);

            // Close panel
            Close();
        }

        /// <summary>
        /// JML: CSV에서 캐릭터 이름 가져오기 (Issue #320)
        /// </summary>
        private string GetCharacterNameFromCSV(int characterId)
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                return $"Character_{characterId}";
            }

            var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
            if (characterData == null)
            {
                return $"Character_{characterId}";
            }

            var stringData = CSVLoader.Instance.GetData<StringTable>(characterData.Character_Name_ID);
            return stringData?.Text ?? $"Character_{characterId}";
        }

        /// <summary>
        /// Clear all spawned cards
        /// </summary>
        private void ClearCards()
        {
            if (cardContainer == null) return;

            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            cardInstances = null;
        }

        /// <summary>
        /// JML: DeckManager에서 캐릭터 ID 가져오기 (Issue #320)
        /// </summary>
        private int[] GetRandomCharacterIds(int count)
        {
            // DeckManager에서 덱 가져오기
            var deck = DeckManager.Instance?.GetValidCharacters();

            if (deck == null || deck.Count == 0)
            {
                Debug.LogWarning("[CardSelectPanel] DeckManager가 없거나 덱이 비어있습니다!");
                return new int[0];
            }

            // 덱에서 랜덤하게 count개 선택
            int[] selected = new int[Math.Min(count, deck.Count)];
            var shuffled = new System.Collections.Generic.List<int>(deck);

            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            for (int i = 0; i < selected.Length; i++)
            {
                selected[i] = shuffled[i];
            }

            Debug.Log($"[CardSelectPanel] DeckManager에서 캐릭터 {selected.Length}개 선택: [{string.Join(", ", selected)}]");
            return selected;
        }

        /// <summary>
        /// JML: DeckManager 연동된 혼합 카드 (Issue #320)
        /// </summary>
        private CardData[] GetRandomMixedCards(int count)
        {
            // DeckManager에서 캐릭터 풀 가져오기
            var characterPool = DeckManager.Instance?.GetValidCharacters();
            int[] abilityPool = { 1, 2, 3, 4, 5 };  // TODO: AbilityTable 연동

            CardData[] cards = new CardData[count];

            for (int i = 0; i < count; i++)
            {
                // 50% 확률로 캐릭터 또는 어빌리티
                bool isCharacter = UnityEngine.Random.value > 0.5f;

                if (isCharacter && characterPool != null && characterPool.Count > 0)
                {
                    int randomId = characterPool[UnityEngine.Random.Range(0, characterPool.Count)];
                    cards[i] = new CardData(CardType.Character, randomId);
                }
                else
                {
                    int randomId = abilityPool[UnityEngine.Random.Range(0, abilityPool.Length)];
                    cards[i] = new CardData(CardType.Ability, randomId);
                }
            }

            return cards;
        }

        /// <summary>
        /// Get N random ability cards only (when all character slots are full)
        /// </summary>
        private CardData[] GetRandomAbilityCards(int count)
        {
            // TODO: Get from CardTableData or config
            // For now, hardcoded pool
            int[] abilityPool = { 1, 2, 3, 4, 5 };

            CardData[] cards = new CardData[count];
            int[] shuffled = (int[])abilityPool.Clone();

            // Fisher-Yates shuffle
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            for (int i = 0; i < count; i++)
            {
                cards[i] = new CardData(CardType.Ability, shuffled[i % shuffled.Length]);
            }

            return cards;
        }

        /// <summary>
        /// Check if panel is currently open
        /// </summary>
        public bool IsOpen => panel != null && panel.activeSelf;
    }
}
