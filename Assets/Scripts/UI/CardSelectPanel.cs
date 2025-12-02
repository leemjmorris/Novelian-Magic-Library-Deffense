using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using NovelianMagicLibraryDefense.Managers;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// JML: CSV 기반 카드 선택 패널 (Issue #349)
    /// - Game Start: 캐릭터 카드 2장
    /// - Level Up: PlayerLevelTable에 따라 캐릭터/스탯 카드 표시
    /// - 중복 캐릭터 선택 시 성급 업그레이드
    /// - 모든 캐릭터 최종 성급이면 스탯 카드로 대체
    /// </summary>
    public class CardSelectPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panel;

        [Header("Card Container")]
        [SerializeField] private Transform cardContainer;

        [Header("Card Prefabs")]
        [SerializeField] private GameObject characterCardPrefab;
        [SerializeField] private GameObject statCard_AttackSpeed;
        [SerializeField] private GameObject statCard_Damage;
        [SerializeField] private GameObject statCard_ProjectileSpeed;
        [SerializeField] private GameObject statCard_Range;

        [Header("Settings")]
        [SerializeField] private bool pauseOnGameStart = true;
        [SerializeField] private bool pauseOnLevelUp = true;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        private const float SELECTION_TIME = 20f;
        private CancellationTokenSource selectionCts;
        private bool isCardSelected = false;

        // Events
        public event Action<CardData> OnCardSelected;

        private GameObject[] cardInstances;
        private CharacterPlacementManager placementManager;
        private float previousTimeScale = 1f;
        private bool isPaused = false;

        // JML: 현재 플레이어 레벨 (StageManager에서 가져옴)
        private int currentPlayerLevel = 1;

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
            placementManager = FindFirstObjectByType<CharacterPlacementManager>();
            if (placementManager == null)
            {
                Debug.LogWarning("[CardSelectPanel] CharacterPlacementManager not found in scene!");
            }
            else
            {
                Debug.Log("[CardSelectPanel] Waiting for CharacterPlacementManager to preload characters...");
                int maxWaitFrames = 300;
                int frameCount = 0;

                while (!placementManager.IsPreloadComplete() && frameCount < maxWaitFrames)
                {
                    await UniTask.Yield();
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

            Debug.Log("[CardSelectPanel] Awake completed, ready to use");
        }

        /// <summary>
        /// Open panel for game start - 2 random character cards only
        /// </summary>
        public void OpenForGameStart()
        {
            if (panel == null) return;

            if (pauseOnGameStart)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                isPaused = true;
            }

            // 게임 시작: 캐릭터 카드 2장
            CardData[] cards = GetCharacterCards(2);

            panel.SetActive(true);
            CreateCards(cards);
            StartSelectionTimer().Forget();

            Debug.Log("[CardSelectPanel] Opened for game start - 2 character cards");
        }

        /// <summary>
        /// JML: CSV 기반 레벨업 카드 표시 (Issue #349)
        /// PlayerLevelTable에서 Character_Card_Appear 값에 따라 카드 타입 결정
        /// </summary>
        public void OpenForLevelUp()
        {
            if (panel == null) return;

            if (pauseOnLevelUp)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                isPaused = true;
            }

            // 현재 레벨 가져오기
            currentPlayerLevel = GameManager.Instance?.Stage?.GetCurrentLevel() ?? 1;
            int levelId = 700 + currentPlayerLevel; // 0701, 0702, ...

            // PlayerLevelTable에서 데이터 조회
            var levelData = CSVLoader.Instance?.GetData<PlayerLevelData>(levelId);

            CardData[] cards;

            if (levelData != null && levelData.Character_Card_Appear == 1)
            {
                // 캐릭터 카드 레벨 (3, 6, 9, 12...)
                cards = GetCharacterCardsForLevelUp();
                Debug.Log($"[CardSelectPanel] Level {currentPlayerLevel}: Character_Card_Appear=1 → 캐릭터 카드");
            }
            else if (levelData != null && levelData.Card_List_ID > 0)
            {
                // 스탯 카드 레벨 - CardListTable에서 지정된 카드 2장
                cards = GetStatCardsFromCardList(levelData.Card_List_ID);
                Debug.Log($"[CardSelectPanel] Level {currentPlayerLevel}: Card_List_ID={levelData.Card_List_ID} → 스탯 카드");
            }
            else
            {
                // fallback: 랜덤 스탯 카드
                cards = GetRandomStatCards(2);
                Debug.LogWarning($"[CardSelectPanel] Level {currentPlayerLevel}: No valid data, using random stat cards");
            }

            panel.SetActive(true);
            CreateCards(cards);
            StartSelectionTimer().Forget();
        }

        /// <summary>
        /// JML: 캐릭터 카드 2장 생성 (캐릭터 레벨용)
        /// 유효한 캐릭터 풀에서 선택 (최종 성급 제외)
        /// 부족하면 스탯 카드로 대체
        /// </summary>
        private CardData[] GetCharacterCardsForLevelUp()
        {
            List<int> validCharacterPool = BuildValidCharacterPool();
            CardData[] cards = new CardData[2];

            if (validCharacterPool.Count >= 2)
            {
                // 2개 이상 유효한 캐릭터가 있으면 랜덤 선택
                ShuffleList(validCharacterPool);
                cards[0] = new CardData(CardType.Character, validCharacterPool[0]);
                cards[1] = new CardData(CardType.Character, validCharacterPool[1]);
            }
            else if (validCharacterPool.Count == 1)
            {
                // 1개만 유효: 캐릭터 1장 + 스탯 1장
                cards[0] = new CardData(CardType.Character, validCharacterPool[0]);
                cards[1] = GetRandomStatCard();
                Debug.Log("[CardSelectPanel] 유효한 캐릭터 1개 → 캐릭터 1장 + 스탯 1장");
            }
            else
            {
                // 0개 유효 (모두 최종 성급): 스탯 카드 2장
                var statCards = GetRandomStatCards(2);
                cards[0] = statCards[0];
                cards[1] = statCards[1];
                Debug.Log("[CardSelectPanel] 모든 캐릭터 최종 성급 → 스탯 카드 2장");
            }

            return cards;
        }

        /// <summary>
        /// JML: 유효한 캐릭터 풀 구성
        /// 1. 아직 소환 안된 캐릭터 (덱에 있고 필드에 없음)
        /// 2. 필드에 있지만 최종 성급 아닌 캐릭터
        /// </summary>
        private List<int> BuildValidCharacterPool()
        {
            List<int> validPool = new List<int>();

            // 덱에서 캐릭터 목록 가져오기
            var deck = DeckManager.Instance?.GetValidCharacters();
            if (deck == null || deck.Count == 0)
            {
                Debug.LogWarning("[CardSelectPanel] DeckManager가 없거나 덱이 비어있습니다!");
                return validPool;
            }

            // 필드 캐릭터 정보 수집
            var fieldCharacters = placementManager?.GetAllCharacters() ?? new List<Novelian.Combat.Character>();
            var fieldCharacterIds = new HashSet<int>();
            var finalStarCharacterIds = new HashSet<int>();

            foreach (var character in fieldCharacters)
            {
                int charId = character.GetCharacterId();
                fieldCharacterIds.Add(charId);

                if (character.IsFinalStarTier())
                {
                    finalStarCharacterIds.Add(charId);
                }
            }

            // 유효한 캐릭터 필터링
            foreach (int charId in deck)
            {
                // 필드에 없으면 소환 가능 (빈 슬롯이 있을 때)
                if (!fieldCharacterIds.Contains(charId))
                {
                    if (placementManager != null && placementManager.HasEmptySlot())
                    {
                        validPool.Add(charId);
                    }
                }
                // 필드에 있지만 최종 성급 아니면 업그레이드 가능
                else if (!finalStarCharacterIds.Contains(charId))
                {
                    validPool.Add(charId);
                }
            }

            Debug.Log($"[CardSelectPanel] 유효한 캐릭터 풀: {validPool.Count}개 [{string.Join(", ", validPool)}]");
            return validPool;
        }

        /// <summary>
        /// JML: CardListTable에서 지정된 스탯 카드 2장 가져오기
        /// </summary>
        private CardData[] GetStatCardsFromCardList(int cardListId)
        {
            var cardListData = CSVLoader.Instance?.GetData<CardListData>(cardListId);

            if (cardListData == null)
            {
                Debug.LogWarning($"[CardSelectPanel] CardListData not found for ID: {cardListId}");
                return GetRandomStatCards(2);
            }

            CardData[] cards = new CardData[2];

            // Card_1_ID, Card_2_ID를 Ability ID로 매핑
            cards[0] = ConvertCardIdToCardData(cardListData.Card_1_ID);
            cards[1] = ConvertCardIdToCardData(cardListData.Card_2_ID);

            Debug.Log($"[CardSelectPanel] CardList {cardListId}: Card1={cardListData.Card_1_ID}, Card2={cardListData.Card_2_ID}");
            return cards;
        }

        /// <summary>
        /// JML: CardTable의 Card_ID를 CardData로 변환
        /// Card_Type: 1-3=스탯카드, 4=캐릭터카드
        /// </summary>
        private CardData ConvertCardIdToCardData(int cardId)
        {
            var cardData = CSVLoader.Instance?.GetData<global::CardData>(cardId);

            if (cardData == null)
            {
                Debug.LogWarning($"[CardSelectPanel] CardData not found for ID: {cardId}");
                return GetRandomStatCard();
            }

            // Card_Type 4 = 캐릭터 카드
            if (cardData.Card_Type == 4)
            {
                // 캐릭터 카드인 경우, 덱에서 랜덤 선택
                return GetCharacterCards(1)[0];
            }

            // Card_Type 1-3 = 스탯 카드, CardId를 Ability ID로 매핑
            int abilityId = MapCardIdToAbilityId(cardId);
            return new CardData(CardType.Ability, abilityId);
        }

        /// <summary>
        /// JML: CardTable의 Card_ID를 내부 Ability ID(1~4)로 매핑
        /// 081001=공격력(2), 081002=치명타배율, 081003=공격속도(1), 081004=치명타확률
        /// 082005=투사체속도(3), 082006=총공격력, 083007=추가데미지, 083008=체력회복
        /// </summary>
        private int MapCardIdToAbilityId(int cardId)
        {
            return cardId switch
            {
                081001 => 2, // 공격력 증가
                081002 => 5, // 치명타 배율 (현재 미지원, fallback)
                081003 => 1, // 공격속도 증가
                081004 => 5, // 치명타 확률 (현재 미지원, fallback)
                082005 => 3, // 투사체 속도 증가
                082006 => 2, // 총 공격력 → 공격력으로 대체
                083007 => 2, // 추가 데미지 → 공격력으로 대체
                083008 => 5, // 체력 회복 (현재 미지원, fallback)
                _ => 2 // fallback: 공격력
            };
        }

        /// <summary>
        /// JML: 게임 시작용 캐릭터 카드 (빈 슬롯에 소환)
        /// </summary>
        private CardData[] GetCharacterCards(int count)
        {
            var deck = DeckManager.Instance?.GetValidCharacters();

            if (deck == null || deck.Count == 0)
            {
                Debug.LogWarning("[CardSelectPanel] DeckManager가 없거나 덱이 비어있습니다!");
                return new CardData[0];
            }

            var shuffled = new List<int>(deck);
            ShuffleList(shuffled);

            CardData[] cards = new CardData[Math.Min(count, shuffled.Count)];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = new CardData(CardType.Character, shuffled[i]);
            }

            return cards;
        }

        /// <summary>
        /// JML: 랜덤 스탯 카드 N장
        /// </summary>
        private CardData[] GetRandomStatCards(int count)
        {
            int[] abilityPool = { 1, 2, 3, 4 }; // 공격속도, 공격력, 투사체속도, 사거리
            CardData[] cards = new CardData[count];

            for (int i = 0; i < count; i++)
            {
                int randomId = abilityPool[UnityEngine.Random.Range(0, abilityPool.Length)];
                cards[i] = new CardData(CardType.Ability, randomId);
            }

            return cards;
        }

        /// <summary>
        /// JML: 랜덤 스탯 카드 1장
        /// </summary>
        private CardData GetRandomStatCard()
        {
            int[] abilityPool = { 1, 2, 3, 4 };
            int randomId = abilityPool[UnityEngine.Random.Range(0, abilityPool.Length)];
            return new CardData(CardType.Ability, randomId);
        }

        /// <summary>
        /// Fisher-Yates shuffle
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Create 2 cards in the container
        /// </summary>
        private void CreateCards(CardData[] cards)
        {
            ClearCards();

            if (cardContainer == null)
            {
                Debug.LogError("[CardSelectPanel] Card container not assigned!");
                return;
            }

            cardInstances = new GameObject[cards.Length];

            for (int i = 0; i < cards.Length; i++)
            {
                GameObject prefab = GetCardPrefab(cards[i]);
                if (prefab == null)
                {
                    Debug.LogError($"[CardSelectPanel] Prefab not found for {cards[i].Type} (ID: {cards[i].Id})");
                    continue;
                }

                GameObject cardObj = Instantiate(prefab, cardContainer);
                cardObj.name = $"{cards[i].Type}Card_{cards[i].Id}";

                var cardText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
                if (cardText != null)
                {
                    cardText.text = GetCardName(cards[i]);
                }

                var cardButton = cardObj.GetComponent<UnityEngine.UI.Button>();
                if (cardButton != null)
                {
                    CardData cardData = cards[i];
                    cardButton.onClick.AddListener(() => OnCardClicked(cardData));
                }

                cardInstances[i] = cardObj;
                Debug.Log($"[CardSelectPanel] Created {cards[i].Type} card: {GetCardName(cards[i])} (ID: {cards[i].Id})");
            }
        }

        private GameObject GetCardPrefab(CardData cardData)
        {
            if (cardData.Type == CardType.Character)
            {
                return characterCardPrefab;
            }

            return cardData.Id switch
            {
                1 => statCard_AttackSpeed,
                2 => statCard_Damage,
                3 => statCard_ProjectileSpeed,
                4 => statCard_Range,
                _ => statCard_Damage
            };
        }

        private string GetCardName(CardData cardData)
        {
            if (cardData.Type == CardType.Character)
            {
                return GetCharacterNameFromCSV(cardData.Id);
            }

            return cardData.Id switch
            {
                1 => "공격 속도",
                2 => "공격력",
                3 => "투사체 속도",
                4 => "사거리",
                _ => $"Stat_{cardData.Id}"
            };
        }

        /// <summary>
        /// JML: 카드 클릭 처리 (Issue #349)
        /// 캐릭터 카드: 필드에 없으면 소환, 있으면 성급 업그레이드
        /// </summary>
        private void OnCardClicked(CardData cardData)
        {
            if (isCardSelected) return;
            StopTimer();

            string cardName = cardData.Type == CardType.Character
                ? GetCharacterNameFromCSV(cardData.Id)
                : $"Ability_{cardData.Id}";
            Debug.Log($"[CardSelectPanel] 카드 선택: {cardName} (Type: {cardData.Type}, ID: {cardData.Id})");

            if (cardData.Type == CardType.Character)
            {
                ProcessCharacterCard(cardData.Id);
            }
            else if (cardData.Type == CardType.Ability)
            {
                ApplyStatCardEffect(cardData.Id);
            }

            OnCardSelected?.Invoke(cardData);
            Close();
        }

        /// <summary>
        /// JML: 캐릭터 카드 처리 (Issue #349)
        /// 필드에 없으면 소환, 있으면 성급 업그레이드
        /// </summary>
        private void ProcessCharacterCard(int characterId)
        {
            if (placementManager == null)
            {
                Debug.LogError("[CardSelectPanel] CharacterPlacementManager is null!");
                return;
            }

            // 필드에 해당 캐릭터가 있는지 확인
            var existingCharacter = placementManager.GetCharacterById(characterId);

            if (existingCharacter != null)
            {
                // 이미 필드에 있으면 성급 업그레이드
                bool upgraded = existingCharacter.UpgradeStarTier();
                if (upgraded)
                {
                    Debug.Log($"[CardSelectPanel] 캐릭터 ID {characterId} 성급 업그레이드 완료!");
                }
            }
            else
            {
                // 필드에 없으면 새로 소환
                placementManager.SpawnCharacterById(characterId);
                Debug.Log($"[CardSelectPanel] 캐릭터 ID {characterId} 소환 완료!");
            }
        }

        /// <summary>
        /// JML: 스탯 카드 효과 적용 (Issue #349)
        /// CardLevelTable의 value_change 값 사용
        /// </summary>
        private void ApplyStatCardEffect(int abilityId)
        {
            StatType statType = abilityId switch
            {
                1 => StatType.AttackSpeed,
                2 => StatType.Damage,
                3 => StatType.ProjectileSpeed,
                4 => StatType.Range,
                _ => StatType.Damage
            };

            // CSV에서 value_change 조회 (CardLevelTable)
            // abilityId를 CardLevelTable ID로 매핑
            int cardLevelId = MapAbilityIdToCardLevelId(abilityId);
            float buffValue = GetValueChangeFromCSV(cardLevelId);

            var stageManager = GameManager.Instance?.Stage;
            if (stageManager != null)
            {
                stageManager.ApplyGlobalStatBuff(statType, buffValue);
                Debug.Log($"[CardSelectPanel] 스탯 버프 적용: {statType} +{buffValue * 100f}% (CardLevelID: {cardLevelId})");
            }
            else
            {
                Debug.LogError("[CardSelectPanel] StageManager를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// JML: Ability ID를 CardLevelTable ID로 매핑
        /// 현재 티어 1 기준 (추후 티어 시스템 구현 시 확장)
        /// </summary>
        private int MapAbilityIdToCardLevelId(int abilityId)
        {
            // Tier 1 기준 매핑
            return abilityId switch
            {
                1 => 25007, // 공격속도 증가 Tier1
                2 => 25001, // 공격력 증가 Tier1
                3 => 25013, // 투사체 속도 증가 Tier1
                4 => 25085, // 사거리 증가 Tier1
                _ => 25001  // fallback: 공격력 증가
            };
        }

        /// <summary>
        /// JML: CardLevelTable에서 value_change 조회
        /// </summary>
        private float GetValueChangeFromCSV(int cardLevelId)
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[CardSelectPanel] CSVLoader not ready, using default 0.1");
                return 0.1f;
            }

            var cardLevelData = CSVLoader.Instance.GetData<CardLevelData>(cardLevelId);
            if (cardLevelData == null)
            {
                Debug.LogWarning($"[CardSelectPanel] CardLevelData not found for ID: {cardLevelId}, using default 0.1");
                return 0.1f;
            }

            Debug.Log($"[CardSelectPanel] CSV value_change: {cardLevelData.value_change} (ID: {cardLevelId}, Tier: {cardLevelData.Tier})");
            return cardLevelData.value_change;
        }

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

        public void Close()
        {
            if (panel == null) return;

            StopTimer();
            panel.SetActive(false);

            if (isPaused)
            {
                Time.timeScale = previousTimeScale;
                isPaused = false;
            }

            ClearCards();
            isCardSelected = false;
        }

        private void ClearCards()
        {
            if (cardContainer == null) return;

            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            cardInstances = null;
        }

        #region Timer Methods

        private async UniTask StartSelectionTimer()
        {
            isCardSelected = false;
            selectionCts?.Dispose();
            selectionCts = new CancellationTokenSource();
            float remainingTime = SELECTION_TIME;

            UpdateTimerText(remainingTime);

            try
            {
                while (remainingTime > 0 && !isCardSelected)
                {
                    await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: selectionCts.Token);
                    remainingTime -= 1f;
                    UpdateTimerText(remainingTime);
                }

                if (!isCardSelected)
                {
                    Debug.Log("[CardSelectPanel] 20초 타임아웃! 랜덤 카드 선택");
                    AutoSelectCard();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[CardSelectPanel] 타이머 취소됨 (카드 선택됨)");
            }
            finally
            {
                selectionCts?.Dispose();
                selectionCts = null;
            }
        }

        private void UpdateTimerText(float remainingTime)
        {
            if (timerText != null)
            {
                timerText.text = $"{Mathf.CeilToInt(remainingTime)}s";
            }
        }

        private void StopTimer()
        {
            isCardSelected = true;
            selectionCts?.Cancel();
            UpdateTimerText(0);
        }

        private void AutoSelectCard()
        {
            if (cardInstances == null || cardInstances.Length == 0)
            {
                Debug.LogWarning("[CardSelectPanel] 선택할 카드가 없습니다!");
                Close();
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, cardInstances.Length);
            var cardButton = cardInstances[randomIndex]?.GetComponent<UnityEngine.UI.Button>();
            if (cardButton != null)
            {
                Debug.Log($"[CardSelectPanel] 랜덤 선택: 카드 {randomIndex + 1}");
                cardButton.onClick.Invoke();
            }
            else
            {
                Debug.LogWarning("[CardSelectPanel] 랜덤 선택된 카드에 버튼이 없습니다!");
                Close();
            }
        }

        #endregion

        public bool IsOpen => panel != null && panel.activeSelf;
    }
}
