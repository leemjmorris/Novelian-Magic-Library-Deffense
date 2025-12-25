using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using NovelianMagicLibraryDefense.Managers;
using Novelian.Combat;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// JML: CSV 기반 카드 선택 패널 (Issue #349, #424)
    /// - Game Start: 덱 캐릭터 수만큼 캐릭터 카드 표시 (3~4장)
    /// - Level Up (Card_Type=1): 스탯/스킬 카드 4장 고정
    /// - Level Up (Card_Type=2): 덱 캐릭터 수만큼 캐릭터 카드 표시 (3~4장)
    /// - 중복 캐릭터 선택 시 성급 업그레이드
    /// - 모든 캐릭터 최종 성급이면 스탯 카드로 대체
    /// </summary>
    public class CardSelectPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject cardGrid;  // Issue #476: CardGrid 활성화용

        [Header("Card Slots (CardGrid의 Card1~4)")]
        [Tooltip("CardGrid 오브젝트의 Card1~4 슬롯을 연결 (StageCard 컴포넌트 필요)")]
        [SerializeField] private StageCard[] cardSlots = new StageCard[4];

        [Header("Legacy - Card Container (Deprecated)")]
        [SerializeField] private Transform cardContainer;

        [Header("Legacy - Card Prefabs (Deprecated)")]
        [SerializeField] private GameObject characterCardPrefab;
        [SerializeField] private GameObject statCard_AttackSpeed;
        [SerializeField] private GameObject statCard_Damage;
        [SerializeField] private GameObject statCard_ProjectileSpeed;
        [SerializeField] private GameObject statCard_Range;
        [SerializeField] private GameObject statCard_CritMultiplier;
        [SerializeField] private GameObject statCard_CritChance;
        [SerializeField] private GameObject statCard_TotalDamage;
        [SerializeField] private GameObject statCard_BonusDamage;
        [SerializeField] private GameObject statCard_HealthRegen;

        [Header("Settings")]
        [SerializeField] private bool pauseOnGameStart = true;
        [SerializeField] private bool pauseOnLevelUp = true;

        [Header("Dependencies")]
        [SerializeField] private CharacterPlacementManager placementManager;

        [Header("CharacterCardGrid (Issue #424)")]
        [SerializeField] private CharacterCardGridManager characterCardGridManager;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        private const float SELECTION_TIME = 20f;
        private CancellationTokenSource selectionCts;
        private bool isCardSelected = false;

        // Events
        public event Action<CardData> OnCardSelected;

        private GameObject[] cardInstances;
        private float previousTimeScale = 1f;
        private bool isPaused = false;

        // JML: 카드 슬롯 시스템용 활성 카드 데이터 추적
        private CardData[] activeCardData;
        private bool useCardSlotSystem = false;

        // JML: 현재 플레이어 레벨 (StageManager에서 가져옴)
        private int currentPlayerLevel = 1;

        // JML: 서포트 스킬 카드 선택 상태 (Issue #437)
        private int selectedSupportCardId = -1;
        private int selectedSlotIndex = -1;
        private const float SELECTED_CARD_SCALE = 1.15f;
        private const float NORMAL_CARD_SCALE = 1.0f;

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
            // JML: 카드 슬롯 초기 비활성화 (씬 진입 시 카드 초기화가 보이는 문제 방지)
            HideCardSlotsOnAwake();

            // JML: Inspector 참조가 없으면 Tag로 fallback 시도
            if (placementManager == null)
            {
                GameObject cpmObj = GameObject.FindWithTag("CharacterPlacementManager");
                if (cpmObj != null)
                {
                    placementManager = cpmObj.GetComponent<CharacterPlacementManager>();
                }
            }

            if (placementManager == null)
            {
                Debug.LogWarning("[CardSelectPanel] CharacterPlacementManager not found! Inspector에서 할당하거나 SetPlacementManager()로 설정해주세요.");
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
        /// Issue #476: CharacterPlacementManager 참조 설정 (BossDungeonManager에서 호출)
        /// </summary>
        public void SetPlacementManager(CharacterPlacementManager manager)
        {
            if (manager != null)
            {
                placementManager = manager;
                Debug.Log("[CardSelectPanel] PlacementManager가 외부에서 설정됨");
            }
        }

        /// <summary>
        /// Issue #476: CharacterCardGridManager 참조 설정 (BossDungeonManager에서 호출)
        /// </summary>
        public void SetCharacterCardGridManager(CharacterCardGridManager manager)
        {
            if (manager != null)
            {
                characterCardGridManager = manager;
                Debug.Log($"[CardSelectPanel] CharacterCardGridManager가 외부에서 설정됨: instanceId={manager.GetInstanceID()}");
            }
        }

        /// <summary>
        /// JML: Awake에서 카드 슬롯 즉시 비활성화 (씬 진입 시 초기화 과정이 보이는 문제 방지)
        /// </summary>
        private void HideCardSlotsOnAwake()
        {
            if (cardSlots == null) return;

            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null)
                {
                    cardSlots[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Open panel for game start - character cards based on deck count (3-4)
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

            panel.SetActive(true);

            // 데이터 로딩 대기 후 카드 표시
            WaitForDataAndShowCards(isGameStart: true).Forget();
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

            panel.SetActive(true);

            // 데이터 로딩 대기 후 카드 표시
            WaitForDataAndShowCards(isGameStart: false).Forget();
        }

        /// <summary>
        /// Issue #476: 도전던전 전용 카드 선택
        /// 캐릭터 카드 3장 + 스탯 카드 1장 (캐릭터가 3성이면 캐릭터 2장 + 스탯 2장)
        /// </summary>
        public void OpenForBossDungeon()
        {
            Debug.Log("[CardSelectPanel] OpenForBossDungeon 호출됨");

            if (panel == null)
            {
                Debug.LogError("[CardSelectPanel] panel이 null입니다! Inspector에서 Panel을 연결해주세요.");
                return;
            }

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPaused = true;

            panel.SetActive(true);

            // Issue #476: CardGrid도 활성화 (카드 슬롯 표시용)
            if (cardGrid != null)
            {
                cardGrid.SetActive(true);
                Debug.Log("[CardSelectPanel] CardGrid 활성화됨");
            }
            else
            {
                // cardGrid 참조가 없으면 cardSlots[0]의 부모를 활성화 시도
                if (cardSlots != null && cardSlots.Length > 0 && cardSlots[0] != null)
                {
                    var parent = cardSlots[0].transform.parent;
                    if (parent != null)
                    {
                        parent.gameObject.SetActive(true);
                        Debug.Log($"[CardSelectPanel] CardGrid(부모) 활성화됨: {parent.name}");
                    }
                }
            }

            Debug.Log("[CardSelectPanel] 패널 활성화됨, 카드 로딩 시작...");

            // 데이터 로딩 대기 후 카드 표시
            WaitForDataAndShowBossDungeonCards().Forget();
        }

        /// <summary>
        /// Issue #476: 도전던전 카드 표시 (캐릭터 3장 + 스탯 1장)
        /// </summary>
        private async UniTaskVoid WaitForDataAndShowBossDungeonCards()
        {
            SetCardSlotsInteractable(false);

            // CSV 로딩 대기
            if (CSVLoader.Instance != null && !CSVLoader.Instance.IsInit)
            {
                int maxWaitFrames = 600;
                int frameCount = 0;
                while (!CSVLoader.Instance.IsInit && frameCount < maxWaitFrames)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    frameCount++;
                }
            }

            // 캐릭터 프리팹 프리로드 대기
            if (placementManager != null && !placementManager.IsPreloadComplete())
            {
                int maxWaitFrames = 300;
                int frameCount = 0;
                while (!placementManager.IsPreloadComplete() && frameCount < maxWaitFrames)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    frameCount++;
                }
            }

            // 도전던전 카드 생성: 캐릭터 3장 + 스탯 1장 (또는 캐릭터 2장 + 스탯 2장)
            CardData[] cards = GetBossDungeonCards();
            CreateCards(cards);

            SetCardSlotsInteractable(true);
            StartSelectionTimer().Forget();
        }

        /// <summary>
        /// Issue #476: 도전던전용 카드 구성
        /// 기본: 캐릭터 3장 + 스탯 1장
        /// 캐릭터가 3성이면 스탯으로 대체
        /// </summary>
        private CardData[] GetBossDungeonCards()
        {
            List<int> validCharacterPool = BuildValidCharacterPoolForUpgrade();
            CardData[] cards = new CardData[4];

            // 캐릭터 카드 수 결정 (기본 3장, 업그레이드 가능한 캐릭터 수에 따라 조정)
            int charCardCount = Math.Min(3, validCharacterPool.Count);
            int statCardCount = 4 - charCardCount;

            // 캐릭터 카드 채우기 (업그레이드용)
            ShuffleList(validCharacterPool);
            for (int i = 0; i < charCardCount; i++)
            {
                cards[i] = new CardData(CardType.Character, validCharacterPool[i]);
            }

            // 스탯 카드 채우기
            var statCards = GetRandomStatCards(statCardCount);
            for (int i = 0; i < statCardCount; i++)
            {
                cards[charCardCount + i] = statCards[i];
            }

            Debug.Log($"[CardSelectPanel] 도전던전 카드: 캐릭터 {charCardCount}장 + 스탯 {statCardCount}장");
            return cards;
        }

        /// <summary>
        /// Issue #476: 업그레이드 가능한 캐릭터 풀 (필드에 있고 최종 성급 아닌 캐릭터만)
        /// </summary>
        private List<int> BuildValidCharacterPoolForUpgrade()
        {
            List<int> validPool = new List<int>();

            if (placementManager == null)
            {
                Debug.LogWarning("[CardSelectPanel] placementManager가 null입니다! Inspector에서 할당하거나 SetPlacementManager()로 설정해주세요.");
                return validPool;
            }

            var fieldCharacters = placementManager.GetAllCharacters() ?? new List<Novelian.Combat.Character>();
            Debug.Log($"[CardSelectPanel] 필드 캐릭터 수: {fieldCharacters.Count}개");

            foreach (var character in fieldCharacters)
            {
                // 최종 성급이 아니면 업그레이드 가능
                if (!character.IsFinalStarTier())
                {
                    validPool.Add(character.GetCharacterId());
                    Debug.Log($"[CardSelectPanel] 업그레이드 가능: {character.GetCharacterId()}");
                }
            }

            Debug.Log($"[CardSelectPanel] 업그레이드 가능 캐릭터: {validPool.Count}개");
            return validPool;
        }

        /// <summary>
        /// JML: 데이터 로딩 대기 후 카드 표시 (Issue #484)
        /// CSV 로딩 + 캐릭터 프리팹 프리로드 완료까지 로딩 인디케이터 표시
        /// </summary>
        private async UniTaskVoid WaitForDataAndShowCards(bool isGameStart)
        {
            // 카드 슬롯 버튼 비활성화
            SetCardSlotsInteractable(false);

            // 1. CSV 로딩 대기
            if (CSVLoader.Instance != null && !CSVLoader.Instance.IsInit)
            {
                int maxWaitFrames = 600; // 최대 10초 (60fps 기준)
                int frameCount = 0;

                while (!CSVLoader.Instance.IsInit && frameCount < maxWaitFrames)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    frameCount++;
                }

                if (!CSVLoader.Instance.IsInit)
                {
                    Debug.LogWarning("[CardSelectPanel] CSV 로딩 타임아웃!");
                }
            }

            // 2. 캐릭터 프리팹 프리로드 대기
            if (placementManager != null && !placementManager.IsPreloadComplete())
            {
                int maxWaitFrames = 300; // 최대 5초
                int frameCount = 0;

                while (!placementManager.IsPreloadComplete() && frameCount < maxWaitFrames)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    frameCount++;
                }

                if (!placementManager.IsPreloadComplete())
                {
                    Debug.LogWarning("[CardSelectPanel] 캐릭터 프리로드 타임아웃!");
                }
            }

            // 카드 생성
            CardData[] cards;
            if (isGameStart)
            {
                int deckCount = DeckManager.Instance?.GetDeckCount() ?? 4;
                cards = GetCharacterCards(deckCount);
                Debug.Log($"[CardSelectPanel] Opened for game start - {deckCount} character cards");
            }
            else
            {
                // 레벨업 카드 로직
                currentPlayerLevel = GameManager.Instance?.Stage?.GetCurrentLevel() ?? 1;
                int levelId = 700 + currentPlayerLevel;
                var levelData = CSVLoader.Instance?.GetData<PlayerLevelData>(levelId);

                if (levelData != null && levelData.Card_Type == 2)
                {
                    cards = GetCharacterCardsForLevelUp();
                    Debug.Log($"[CardSelectPanel] Level {currentPlayerLevel}: Card_Type=2 → 캐릭터 카드 {cards.Length}장");
                }
                else
                {
                    cards = GetRandomStatCards(4);
                    Debug.Log($"[CardSelectPanel] Level {currentPlayerLevel}: Card_Type=1 → 스킬/스탯 카드 4장");
                }
            }

            CreateCards(cards);

            // 카드 슬롯 버튼 활성화
            SetCardSlotsInteractable(true);

            // 타이머 시작
            StartSelectionTimer().Forget();
        }

        /// <summary>
        /// JML: 카드 슬롯 버튼 활성화/비활성화 (Issue #484)
        /// </summary>
        private void SetCardSlotsInteractable(bool interactable)
        {
            if (cardSlots == null) return;

            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] == null) continue;

                var button = cardSlots[i].GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }

        /// <summary>
        /// JML: 캐릭터 카드 생성 (캐릭터 레벨용) - 덱 수에 따라 3~4장
        /// 유효한 캐릭터 풀에서 선택 (최종 성급 제외)
        /// 부족하면 스탯 카드로 대체
        /// </summary>
        private CardData[] GetCharacterCardsForLevelUp()
        {
            // 덱 캐릭터 수 기반 카드 장수 결정 (3~4장)
            int deckCount = DeckManager.Instance?.GetDeckCount() ?? 4;
            List<int> validCharacterPool = BuildValidCharacterPool();
            CardData[] cards = new CardData[deckCount];

            if (validCharacterPool.Count >= deckCount)
            {
                // 유효 캐릭터가 충분하면 모두 캐릭터 카드로 채움
                ShuffleList(validCharacterPool);
                for (int i = 0; i < deckCount; i++)
                {
                    cards[i] = new CardData(CardType.Character, validCharacterPool[i]);
                }
                Debug.Log($"[CardSelectPanel] 유효 캐릭터 충분 → 캐릭터 카드 {deckCount}장");
            }
            else if (validCharacterPool.Count > 0)
            {
                // 유효 캐릭터가 부족하면: 캐릭터 카드 + 스탯 카드로 채움
                ShuffleList(validCharacterPool);
                int charCount = validCharacterPool.Count;
                int statCount = deckCount - charCount;

                // 캐릭터 카드 먼저 채움
                for (int i = 0; i < charCount; i++)
                {
                    cards[i] = new CardData(CardType.Character, validCharacterPool[i]);
                }

                // 나머지는 스탯 카드로 채움
                var statCards = GetRandomStatCards(statCount);
                for (int i = 0; i < statCount; i++)
                {
                    cards[charCount + i] = statCards[i];
                }

                Debug.Log($"[CardSelectPanel] 유효 캐릭터 {charCount}개 → 캐릭터 {charCount}장 + 스탯 {statCount}장");
            }
            else
            {
                // 0개 유효 (모두 최종 성급): 스탯 카드로만 채움
                var statCards = GetRandomStatCards(deckCount);
                for (int i = 0; i < deckCount; i++)
                {
                    cards[i] = statCards[i];
                }
                Debug.Log($"[CardSelectPanel] 모든 캐릭터 최종 성급 → 스탯 카드 {deckCount}장");
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
        /// JML: CardTable의 Card_ID를 StatType 기반 Ability ID로 매핑
        /// StatType enum 값과 일치시킴 (Defines.cs 참조)
        /// </summary>
        private int MapCardIdToAbilityId(int cardId)
        {
            return cardId switch
            {
                081001 => (int)StatType.Damage,           // 공격력 증가
                081002 => (int)StatType.CritMultiplier,   // 치명타 배율 증가
                081003 => (int)StatType.AttackSpeed,      // 공격속도 증가
                081004 => (int)StatType.CritChance,       // 치명타 확률 증가
                082005 => (int)StatType.ProjectileSpeed,  // 투사체 속도 증가
                082006 => (int)StatType.TotalDamage,      // 총 공격력 증가
                083007 => (int)StatType.BonusDamage,      // 추가 데미지
                083008 => (int)StatType.HealthRegen,      // 체력 회복
                _ => (int)StatType.Damage // fallback: 공격력
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
        /// JML: 랜덤 스탯 카드 N장 (CardTable 기반, 확률 적용, 중복 방지)
        /// </summary>
        private CardData[] GetRandomStatCards(int count)
        {
            // CardTable에서 Card_Type=1 (스탯/스킬 카드) 가져오기
            var cardTable = CSVLoader.Instance?.GetTable<global::CardData>()?.GetAll();
            if (cardTable == null || cardTable.Count == 0)
            {
                Debug.LogWarning("[CardSelectPanel] CardTable이 비어있습니다. 기본 스탯 카드 사용");
                return GetFallbackStatCards(count);
            }

            // Card_Type=1인 카드만 필터링하여 풀 생성
            // 서포트 스킬 카드(081029~081051)는 호환 캐릭터가 있는 경우에만 추가 (Issue #437)
            var statCardPool = new List<global::CardData>();
            foreach (var card in cardTable)
            {
                if (card.Card_Type == 1)
                {
                    // 서포트 스킬 카드 호환성 필터링
                    if (!IsSupportSkillCardCompatible(card.Card_ID))
                    {
                        continue;  // 호환 캐릭터 없으면 풀에서 제외
                    }
                    statCardPool.Add(card);
                }
            }

            if (statCardPool.Count == 0)
            {
                Debug.LogWarning("[CardSelectPanel] Card_Type=1 카드가 없습니다. 기본 스탯 카드 사용");
                return GetFallbackStatCards(count);
            }

            // 중복 방지를 위한 선택된 카드 ID 추적
            var selectedCardIds = new HashSet<int>();
            var cards = new List<CardData>();

            for (int i = 0; i < count && statCardPool.Count > 0; i++)
            {
                // 확률 기반 가중치 랜덤 선택
                global::CardData selectedCard = SelectCardByProbability(statCardPool);

                if (selectedCard != null)
                {
                    // CardTable ID를 그대로 사용 (변환하지 않음)
                    cards.Add(new CardData(CardType.Ability, selectedCard.Card_ID));

                    // 중복 방지: 선택된 카드를 풀에서 제거
                    statCardPool.Remove(selectedCard);
                    selectedCardIds.Add(selectedCard.Card_ID);

                    Debug.Log($"[CardSelectPanel] 카드 선택: {selectedCard.Card_ID} (확률: {selectedCard.Probability})");
                }
            }

            return cards.ToArray();
        }

        /// <summary>
        /// JML: 확률(Probability) 기반 가중치 랜덤 선택
        /// </summary>
        private global::CardData SelectCardByProbability(List<global::CardData> cardPool)
        {
            if (cardPool == null || cardPool.Count == 0) return null;

            // 총 확률 계산
            float totalProbability = 0f;
            foreach (var card in cardPool)
            {
                totalProbability += card.Probability;
            }

            // 랜덤 값 생성
            float randomValue = UnityEngine.Random.Range(0f, totalProbability);

            // 가중치 기반 선택
            float cumulative = 0f;
            foreach (var card in cardPool)
            {
                cumulative += card.Probability;
                if (randomValue <= cumulative)
                {
                    return card;
                }
            }

            // fallback: 마지막 카드 반환
            return cardPool[cardPool.Count - 1];
        }

        /// <summary>
        /// JML: CardTable ID를 AbilityId(StatType)로 매핑
        /// </summary>
        private int MapCardTableIdToAbilityId(int cardTableId)
        {
            return cardTableId switch
            {
                081001 => (int)StatType.Damage,           // 공격력 증가
                081002 => (int)StatType.CritMultiplier,   // 치명타 피해 증가
                081003 => (int)StatType.AttackSpeed,      // 공격 속도 증가
                081004 => (int)StatType.CritChance,       // 치명타 확률 증가
                // 081005~081008: 특수 효과 카드 (적 디버프, 결계)
                081005 => 5,  // 적의 공격속도 감소
                081006 => 6,  // 적의 공격력 감소
                081007 => 7,  // 결계 내구도 증가
                081008 => 8,  // 결계 회복
                // 081029~081051: 서포트 스킬 카드 → CardTable ID 그대로 사용
                _ => cardTableId
            };
        }

        /// <summary>
        /// JML: CSV 로딩 실패 시 기본 스탯 카드 (중복 방지 적용)
        /// </summary>
        private CardData[] GetFallbackStatCards(int count)
        {
            int[] abilityPool = {
                (int)StatType.Damage,
                (int)StatType.CritMultiplier,
                (int)StatType.AttackSpeed,
                (int)StatType.CritChance,
                (int)StatType.ProjectileSpeed,
                (int)StatType.TotalDamage,
                (int)StatType.BonusDamage,
                (int)StatType.HealthRegen,
                (int)StatType.Range
            };

            // 중복 방지: 셔플 후 앞에서 count개 선택
            var shuffled = new List<int>(abilityPool);
            ShuffleList(shuffled);

            CardData[] cards = new CardData[Math.Min(count, shuffled.Count)];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = new CardData(CardType.Ability, shuffled[i]);
            }

            return cards;
        }

        /// <summary>
        /// JML: 랜덤 스탯 카드 1장
        /// </summary>
        private CardData GetRandomStatCard()
        {
            int[] abilityPool = {
                (int)StatType.Damage,
                (int)StatType.CritMultiplier,
                (int)StatType.AttackSpeed,
                (int)StatType.CritChance,
                (int)StatType.ProjectileSpeed,
                (int)StatType.TotalDamage,
                (int)StatType.BonusDamage,
                (int)StatType.HealthRegen,
                (int)StatType.Range
            };
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
        /// JML: 카드 생성/설정 (Issue #424)
        /// cardSlots이 연결되어 있으면 새 시스템 사용, 아니면 기존 시스템 사용
        /// </summary>
        private void CreateCards(CardData[] cards)
        {
            // cardSlots가 유효한지 확인 (하나라도 할당되어 있으면 새 시스템 사용)
            useCardSlotSystem = cardSlots != null && cardSlots.Length > 0 && cardSlots[0] != null;

            if (useCardSlotSystem)
            {
                SetupCardSlots(cards).Forget();
            }
            else
            {
                CreateCardsLegacy(cards);
            }
        }

        /// <summary>
        /// JML: 새 카드 슬롯 시스템 - CardGrid의 Card1~4 사용 (Issue #424)
        /// StageCard.Initialize로 CardTable 기반 카드 설정
        /// </summary>
        private async UniTask SetupCardSlots(CardData[] cards)
        {
            activeCardData = cards;

            // 모든 슬롯 비활성화
            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null)
                {
                    cardSlots[i].gameObject.SetActive(false);
                }
            }

            // 모든 카드 초기화 작업을 동시에 실행
            var initTasks = new List<UniTask>();
            var cardTableIds = new int[cards.Length];

            for (int i = 0; i < cards.Length && i < cardSlots.Length; i++)
            {
                if (cardSlots[i] == null) continue;

                // CardTable ID로 변환
                cardTableIds[i] = ConvertToCardTableId(cards[i]);

                if (cardTableIds[i] > 0)
                {
                    // 모든 Initialize를 동시에 실행
                    initTasks.Add(cardSlots[i].Initialize(cardTableIds[i]));
                }
                else
                {
                    // CardTable에 없는 카드 (fallback: 직접 텍스트 설정)
                    SetupCardSlotManually(cardSlots[i], cards[i]);
                }
            }

            // 모든 초기화가 완료될 때까지 대기
            if (initTasks.Count > 0)
            {
                await UniTask.WhenAll(initTasks);
            }

            // 모든 초기화 완료 후 한번에 카드 표시
            for (int i = 0; i < cards.Length && i < cardSlots.Length; i++)
            {
                if (cardSlots[i] == null) continue;

                // 버튼 이벤트 설정
                var button = cardSlots[i].GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    int slotIndex = i; // 클로저용 복사본
                    button.onClick.AddListener(() => OnCardSlotClicked(slotIndex));
                }

                cardSlots[i].gameObject.SetActive(true);
                Debug.Log($"[CardSelectPanel] CardSlot[{i}] 설정: CardTableID={cardTableIds[i]}, Type={cards[i].Type}, ID={cards[i].Id}");
            }
        }

        /// <summary>
        /// JML: 카드 슬롯 클릭 처리 (새 시스템용)
        /// 서포트 스킬 카드: 선택 상태만 (스케일 업 + 호환 캐릭터 표시)
        /// 스탯/캐릭터 카드: 기존대로 바로 적용
        /// </summary>
        private void OnCardSlotClicked(int slotIndex)
        {
            if (isCardSelected || activeCardData == null || slotIndex >= activeCardData.Length) return;

            CardData cardData = activeCardData[slotIndex];

            // 서포트 스킬 카드인지 확인 (081029~081051)
            if (cardData.Type == CardType.Ability && IsSupportSkillCard(cardData.Id))
            {
                // 서포트 스킬: 선택 상태만 (장착은 캐릭터 클릭 시)
                SelectSupportSkillCard(slotIndex, cardData.Id);
            }
            else
            {
                // 스탯/캐릭터 카드: 기존대로 바로 적용
                OnCardClicked(cardData);
            }
        }

        /// <summary>
        /// JML: 서포트 스킬 카드인지 확인 (081029~081051 범위)
        /// </summary>
        private bool IsSupportSkillCard(int cardId)
        {
            return cardId >= 81029 && cardId <= 81051;
        }

        /// <summary>
        /// JML: 서포트 스킬 카드 선택 (Issue #437)
        /// 스케일 업 연출 + 호환 캐릭터 표시
        /// </summary>
        private void SelectSupportSkillCard(int slotIndex, int cardId)
        {
            // 이전 선택 해제 (다른 스킬 클릭 시)
            if (selectedSlotIndex >= 0 && selectedSlotIndex < cardSlots.Length && cardSlots[selectedSlotIndex] != null)
            {
                cardSlots[selectedSlotIndex].transform.localScale = Vector3.one * NORMAL_CARD_SCALE;
            }

            // 새 선택
            selectedSlotIndex = slotIndex;
            selectedSupportCardId = cardId;

            // 스케일 업 연출
            if (cardSlots[slotIndex] != null)
            {
                cardSlots[slotIndex].transform.localScale = Vector3.one * SELECTED_CARD_SCALE;
            }

            // CardTable ID → Support_ID 변환 (081029 → 40001)
            int supportId = (cardId - 81029) + 40001;

            // 호환 캐릭터 표시 (CharacterCardGrid 애니메이션)
            if (characterCardGridManager != null)
            {
                Debug.Log($"[CardSelectPanel] ShowCompatibleCharacters 호출 전: characterCardGridManager.instanceId={characterCardGridManager.GetInstanceID()}");
                characterCardGridManager.ShowCompatibleCharacters(supportId, this);
            }

            Debug.Log($"[CardSelectPanel] 서포트 스킬 선택: CardID={cardId}, SupportID={supportId}");
        }

        /// <summary>
        /// JML: 서포트 스킬 선택 해제
        /// </summary>
        private void DeselectSupportSkillCard()
        {
            if (selectedSlotIndex >= 0 && selectedSlotIndex < cardSlots.Length && cardSlots[selectedSlotIndex] != null)
            {
                cardSlots[selectedSlotIndex].transform.localScale = Vector3.one * NORMAL_CARD_SCALE;
            }

            selectedSlotIndex = -1;
            selectedSupportCardId = -1;

            // 캐릭터 애니메이션 초기화
            if (characterCardGridManager != null)
            {
                characterCardGridManager.ResetCharacterAnimations();
            }
        }

        /// <summary>
        /// JML: 캐릭터 카드 클릭 시 선택된 스킬 장착 (CharacterCardGridManager에서 호출)
        /// </summary>
        public void EquipSelectedSkillToCharacter(int characterId)
        {
            if (selectedSupportCardId < 0)
            {
                Debug.LogWarning("[CardSelectPanel] 선택된 서포트 스킬이 없습니다.");
                return;
            }

            // 서포트 스킬 장착
            int supportId = (selectedSupportCardId - 81029) + 40001;
            var targetCharacter = placementManager?.GetCharacterById(characterId);

            if (targetCharacter == null)
            {
                Debug.LogError($"[CardSelectPanel] 캐릭터 {characterId}를 찾을 수 없습니다.");
                return;
            }

            bool success = targetCharacter.EquipSupportSkill(supportId);
            if (success)
            {
                var supportData = CSVLoader.Instance?.GetData<SupportSkillData>(supportId);
                string skillName = supportData?.support_name ?? $"Support_{supportId}";
                Debug.Log($"[CardSelectPanel] 서포트 스킬 '{skillName}' → 캐릭터 {characterId}에 장착 완료!");

                // 선택 초기화
                StopTimer();
                isCardSelected = true;

                // UI 갱신
                if (characterCardGridManager != null)
                {
                    characterCardGridManager.RefreshAllStats();
                }

                // 패널 닫기
                Close();
            }
            else
            {
                Debug.LogWarning($"[CardSelectPanel] 서포트 스킬 장착 실패! (SupportID: {supportId}, CharacterID: {characterId})");
            }
        }

        /// <summary>
        /// JML: 현재 서포트 스킬이 선택되어 있는지 확인
        /// </summary>
        public bool HasSelectedSupportSkill()
        {
            return selectedSupportCardId >= 0;
        }

        /// <summary>
        /// JML: 선택된 서포트 스킬 ID 반환
        /// </summary>
        public int GetSelectedSupportId()
        {
            if (selectedSupportCardId < 0) return -1;
            return (selectedSupportCardId - 81029) + 40001;
        }

        /// <summary>
        /// JML: CardData를 CardTable ID로 변환
        /// - Character: CharacterID → CardTable의 Card_Type 2 (082XXX)
        /// - Ability: StatType → CardTable의 Card_Type 1 (081XXX)
        /// </summary>
        private int ConvertToCardTableId(CardData cardData)
        {
            if (cardData.Type == CardType.Character)
            {
                // CharacterID (021001~025020) → CardTable ID (082009~082028)
                // CharacterTable의 Character_ID를 CardTable의 캐릭터 카드 ID로 매핑
                return MapCharacterIdToCardTableId(cardData.Id);
            }
            else
            {
                // 이미 CardTable ID (081xxx)이면 그대로 반환
                if (cardData.Id >= 81000)
                {
                    return cardData.Id;
                }
                // StatType enum → CardTable ID (081001~081008)
                return MapStatTypeToCardTableId(cardData.Id);
            }
        }

        /// <summary>
        /// JML: CharacterID를 CardTable의 캐릭터 카드 ID로 매핑
        /// CharacterTable의 Character_ID → CardTable의 082XXX
        /// </summary>
        private int MapCharacterIdToCardTableId(int characterId)
        {
            // CharacterTable ID 패턴: 021001~025020 (Genre 1~5, 각 4캐릭터)
            // CardTable의 캐릭터 카드: 082009~082028 (20개 캐릭터)
            // 매핑: characterId의 하위 3자리로 순번 추출

            // 캐릭터 ID 패턴 분석: 02XYYY (X=장르, YYY=순번)
            // 예: 021001=그믐, 021002=테네브라, ... 025020=베리타

            // CardTable 매핑: 082009=그믐, 082010=테네브라, ...
            // CardTable의 Card_Name_ID가 CharacterTable의 Character_Name_ID와 일치

            // CSV 기반 매핑 시도
            var cardTable = CSVLoader.Instance?.GetTable<global::CardData>()?.GetAll();
            if (cardTable != null)
            {
                var charData = CSVLoader.Instance?.GetData<CharacterData>(characterId);
                if (charData != null)
                {
                    // Character_Name_ID로 CardTable에서 찾기
                    foreach (var card in cardTable)
                    {
                        if (card.Card_Type == 2 && card.Card_Name_ID == charData.Character_Name_ID)
                        {
                            return card.Card_ID;
                        }
                    }
                }
            }

            // fallback: ID 패턴 기반 매핑
            // 021001→082009, 021002→082010, ...
            int genre = (characterId / 1000) % 10;
            int sequence = characterId % 1000;
            int cardIndex = (genre - 1) * 4 + sequence;
            return 082008 + cardIndex;
        }

        /// <summary>
        /// JML: StatType enum을 CardTable ID로 매핑
        /// </summary>
        private int MapStatTypeToCardTableId(int statTypeValue)
        {
            return statTypeValue switch
            {
                (int)StatType.Damage => 081001,           // 공격력 증가
                (int)StatType.CritMultiplier => 081002,   // 치명타 피해 증가
                (int)StatType.AttackSpeed => 081003,      // 공격 속도 증가
                (int)StatType.CritChance => 081004,       // 치명타 확률 증가
                // StatType.ProjectileSpeed, TotalDamage 등은 CardTable에 없음 → fallback
                _ => 081001 // fallback: 공격력 증가
            };
        }

        /// <summary>
        /// JML: CardTable에 없는 카드 직접 설정 (fallback)
        /// </summary>
        private void SetupCardSlotManually(StageCard slot, CardData cardData)
        {
            // StageCard의 텍스트 필드에 직접 접근하여 설정
            var nameText = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = GetCardName(cardData);
            }
        }

        /// <summary>
        /// Legacy: 기존 카드 생성 방식 (cardContainer + Instantiate)
        /// </summary>
        private void CreateCardsLegacy(CardData[] cards)
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

            // StatType enum 값에 맞춰 프리팹 반환 (없으면 statCard_Damage로 fallback)
            return cardData.Id switch
            {
                (int)StatType.Damage => statCard_Damage,
                (int)StatType.CritMultiplier => statCard_CritMultiplier ?? statCard_Damage,
                (int)StatType.AttackSpeed => statCard_AttackSpeed,
                (int)StatType.CritChance => statCard_CritChance ?? statCard_Damage,
                (int)StatType.ProjectileSpeed => statCard_ProjectileSpeed,
                (int)StatType.TotalDamage => statCard_TotalDamage ?? statCard_Damage,
                (int)StatType.BonusDamage => statCard_BonusDamage ?? statCard_Damage,
                (int)StatType.HealthRegen => statCard_HealthRegen ?? statCard_Damage,
                (int)StatType.Range => statCard_Range,
                _ => statCard_Damage
            };
        }

        private string GetCardName(CardData cardData)
        {
            if (cardData.Type == CardType.Character)
            {
                return GetCharacterNameFromCSV(cardData.Id);
            }

            // StatType enum 값에 맞춰 이름 반환
            return cardData.Id switch
            {
                (int)StatType.Damage => "공격력",
                (int)StatType.CritMultiplier => "치명타 배율",
                (int)StatType.AttackSpeed => "공격 속도",
                (int)StatType.CritChance => "치명타 확률",
                (int)StatType.ProjectileSpeed => "투사체 속도",
                (int)StatType.TotalDamage => "총 공격력",
                (int)StatType.BonusDamage => "추가 데미지",
                (int)StatType.HealthRegen => "체력 회복",
                (int)StatType.Range => "사거리",
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

                // 스탯 카드 적용 후 CharacterCardGrid UI 갱신
                if (characterCardGridManager != null)
                {
                    characterCardGridManager.RefreshAllStats();
                }
            }

            OnCardSelected?.Invoke(cardData);
            Close();
        }

        /// <summary>
        /// JML: 캐릭터 카드 처리 (Issue #349, #424)
        /// 필드에 없으면 소환, 있으면 성급 업그레이드
        /// CharacterCardGridManager에 UI 업데이트 알림
        /// </summary>
        private void ProcessCharacterCard(int characterId)
        {
            if (placementManager == null)
            {
                Debug.LogError("[CardSelectPanel] CharacterPlacementManager is null! Inspector에서 할당하거나 SetPlacementManager()로 설정해주세요.");
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

                    // CharacterCardGridManager에 업그레이드 알림
                    if (characterCardGridManager != null)
                    {
                        characterCardGridManager.OnCharacterUpgraded(characterId, existingCharacter.GetStarTier());
                    }
                }
            }
            else
            {
                // 필드에 없으면 새로 소환
                bool spawned = placementManager.SpawnCharacterById(characterId);

                if (spawned)
                {
                    Debug.Log($"[CardSelectPanel] 캐릭터 ID {characterId} 소환 완료!");

                    // CharacterCardGridManager에 소환 알림 (Issue #424)
                    // UI 슬롯은 왼쪽→오른쪽 순차 배치 (CharacterCardGridManager 기준)
                    if (characterCardGridManager != null)
                    {
                        int uiSlotIndex = characterCardGridManager.GetFirstEmptySlotIndex();
                        if (uiSlotIndex >= 0)
                        {
                            var spawnedCharacter = placementManager.GetCharacterById(characterId);
                            int starTier = spawnedCharacter?.GetStarTier() ?? 1;
                            characterCardGridManager.OnCharacterSpawned(uiSlotIndex, characterId, starTier, spawnedCharacter).Forget();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// JML: 스탯 카드 효과 적용 (Issue #424 - CardTable 기반)
        /// CardTable의 Card_ID에 따라 대상별 효과 분기:
        /// - 081001-081004: Character stat buffs
        /// - 081005-081006: Monster debuffs
        /// - 081007: Wall shield
        /// - 081008: Wall heal
        /// - 081029-081051: Support skill cards
        /// </summary>
        private void ApplyStatCardEffect(int abilityId)
        {
            var stageManager = GameManager.Instance?.Stage;
            if (stageManager == null)
            {
                Debug.LogError("[CardSelectPanel] StageManager를 찾을 수 없습니다!");
                return;
            }

            // CardTable ID인지 확인 (081xxx 범위)
            if (abilityId >= 81000)
            {
                ApplyCardEffectByCardId(abilityId);
                return;
            }

            // 특수 효과 카드 (5~8: 적 디버프, 결계)
            if (abilityId >= 5 && abilityId <= 8)
            {
                ApplySpecialCardEffect(abilityId, stageManager);
                return;
            }

            // abilityId가 StatType enum 값이므로 직접 캐스팅
            StatType statType = (StatType)abilityId;

            // CSV에서 value_change 조회 (CardLevelTable)
            int cardLevelId = MapAbilityIdToCardLevelId(abilityId);
            float buffValue = GetValueChangeFromCSV(cardLevelId);

            stageManager.ApplyGlobalStatBuff(statType, buffValue);
            Debug.Log($"[CardSelectPanel] 스탯 버프 적용: {statType} +{buffValue * 100f}% (CardLevelID: {cardLevelId})");
        }

        /// <summary>
        /// JML: 특수 효과 카드 적용 (적 디버프, 결계)
        /// </summary>
        private void ApplySpecialCardEffect(int effectId, StageManager stageManager)
        {
            // CardTable ID로 변환하여 value_change 조회
            int cardTableId = effectId switch
            {
                5 => 081005,  // 적의 공격속도 감소
                6 => 081006,  // 적의 공격력 감소
                7 => 081007,  // 결계 내구도 증가
                8 => 081008,  // 결계 회복
                _ => 081001
            };

            var cardData = CSVLoader.Instance?.GetData<global::CardData>(cardTableId);
            float buffValue = cardData != null ? GetValueChangeFromCSV(cardData.Card_Level1_ID) : 0.1f;

            switch (effectId)
            {
                case 5: // 적의 공격속도 감소
                    stageManager.ApplyGlobalMonsterDebuff(DeBuffType.ATK_Speed_Down, buffValue);
                    Debug.Log($"[CardSelectPanel] 적 공격속도 감소: -{buffValue * 100f}%");
                    break;
                case 6: // 적의 공격력 감소
                    stageManager.ApplyGlobalMonsterDebuff(DeBuffType.ATK_Damage_Down, buffValue);
                    Debug.Log($"[CardSelectPanel] 적 공격력 감소: -{buffValue * 100f}%");
                    break;
                case 7: // 결계 내구도 증가
                    stageManager.ApplyWallShield(buffValue);
                    Debug.Log($"[CardSelectPanel] 결계 Shield 추가: +{buffValue * 100f}%");
                    break;
                case 8: // 결계 회복
                    stageManager.ApplyWallHeal(buffValue);
                    Debug.Log($"[CardSelectPanel] 결계 회복: +{buffValue * 100f}%");
                    break;
            }
        }

        /// <summary>
        /// JML: CardTable ID 기반 카드 효과 적용 (Issue #424)
        /// </summary>
        private void ApplyCardEffectByCardId(int cardId)
        {
            var stageManager = GameManager.Instance?.Stage;
            if (stageManager == null)
            {
                Debug.LogError("[CardSelectPanel] StageManager를 찾을 수 없습니다!");
                return;
            }

            // CardTable에서 데이터 조회
            var cardData = CSVLoader.Instance?.GetData<global::CardData>(cardId);
            if (cardData == null)
            {
                Debug.LogWarning($"[CardSelectPanel] CardData not found for ID: {cardId}");
                return;
            }

            // 서포트 스킬 카드 (081029~081051): CardLevelTable 조회 없이 바로 처리
            if (cardId >= 081029 && cardId <= 081051)
            {
                ApplySupportSkillCard(cardId, cardData.Card_Level1_ID);
                return;
            }

            // 스탯/디버프/결계 카드: Card_Level1_ID로 value_change 조회 (Tier 1 기준)
            float buffValue = GetValueChangeFromCSV(cardData.Card_Level1_ID);

            // Card_ID에 따른 효과 분기
            switch (cardId)
            {
                // Character Stat Buffs (081001-081004)
                case 081001: // 공격력 증가
                    stageManager.ApplyGlobalStatBuff(StatType.Damage, buffValue);
                    Debug.Log($"[CardSelectPanel] 공격력 증가: +{buffValue * 100f}%");
                    break;

                case 081002: // 치명타 피해 증가
                    stageManager.ApplyGlobalStatBuff(StatType.CritMultiplier, buffValue);
                    Debug.Log($"[CardSelectPanel] 치명타 피해 증가: +{buffValue * 100f}%");
                    break;

                case 081003: // 공격 속도 증가
                    stageManager.ApplyGlobalStatBuff(StatType.AttackSpeed, buffValue);
                    Debug.Log($"[CardSelectPanel] 공격 속도 증가: +{buffValue * 100f}%");
                    break;

                case 081004: // 치명타 확률 증가
                    stageManager.ApplyGlobalStatBuff(StatType.CritChance, buffValue);
                    Debug.Log($"[CardSelectPanel] 치명타 확률 증가: +{buffValue * 100f}%");
                    break;

                // Monster Debuffs (081005-081006)
                case 081005: // 적의 공격속도 감소
                    stageManager.ApplyGlobalMonsterDebuff(DeBuffType.ATK_Speed_Down, buffValue);
                    Debug.Log($"[CardSelectPanel] 적 공격속도 감소: -{buffValue * 100f}%");
                    break;

                case 081006: // 적의 공격력 감소
                    stageManager.ApplyGlobalMonsterDebuff(DeBuffType.ATK_Damage_Down, buffValue);
                    Debug.Log($"[CardSelectPanel] 적 공격력 감소: -{buffValue * 100f}%");
                    break;

                // Wall Effects (081007-081008)
                case 081007: // 결계 내구도 증가 (Shield)
                    stageManager.ApplyWallShield(buffValue);
                    Debug.Log($"[CardSelectPanel] 결계 Shield 추가: +{buffValue * 100f}%");
                    break;

                case 081008: // 결계 회복 (Heal)
                    stageManager.ApplyWallHeal(buffValue);
                    Debug.Log($"[CardSelectPanel] 결계 회복: +{buffValue * 100f}%");
                    break;

                default:
                    Debug.LogWarning($"[CardSelectPanel] Unknown card ID: {cardId}");
                    break;
            }
        }

        /// <summary>
        /// JML: 서포트 스킬 카드 적용 (Issue #424)
        /// CardTable ID → SupportSkillTable ID 매핑
        /// 호환성 필터 적용 후 적합한 캐릭터에 자동 장착
        /// </summary>
        private void ApplySupportSkillCard(int cardId, int skillCardLevelId)
        {
            // CardTable ID → SupportSkillTable ID 변환
            // 081029 → 40001, 081030 → 40002, ...
            int supportId = (cardId - 081029) + 40001;

            Debug.Log($"[CardSelectPanel] 서포트 스킬 카드: CardID={cardId} → SupportID={supportId}");

            // 서포트 스킬 데이터 조회
            var supportData = CSVLoader.Instance?.GetData<SupportSkillData>(supportId);
            if (supportData == null)
            {
                Debug.LogWarning($"[CardSelectPanel] SupportSkillData not found for ID: {supportId}");
                return;
            }

            // 필드 캐릭터 중 호환 가능한 캐릭터 찾기
            var fieldCharacters = placementManager?.GetAllCharacters() ?? new List<Novelian.Combat.Character>();
            Novelian.Combat.Character targetCharacter = null;

            foreach (var character in fieldCharacters)
            {
                // 이미 서포트 스킬이 있는 캐릭터는 스킵
                if (character.HasSupportSkill()) continue;

                // 메인 스킬 타입 조회
                int mainSkillId = character.GetBasicAttackSkillId();
                var mainSkillData = CSVLoader.Instance?.GetData<MainSkillData>(mainSkillId);
                if (mainSkillData == null) continue;

                // 호환성 검증 (SkillExecutor 사용)
                if (SkillExecutor.Instance != null && SkillExecutor.Instance.IsValidCombination(mainSkillData, supportData))
                {
                    targetCharacter = character;
                    Debug.Log($"[CardSelectPanel] 호환 캐릭터 발견: {character.GetCharacterId()}, 스킬타입: {mainSkillData.GetSkillType()}");
                    break;
                }
            }

            if (targetCharacter == null)
            {
                // 호환 캐릭터 없음 → 카드 버리고 게임 진행
                Debug.Log("[CardSelectPanel] 호환되는 캐릭터 없음. 서포트 스킬 카드 버림.");
                return;
            }

            // 서포트 스킬 장착
            bool success = targetCharacter.EquipSupportSkill(supportId);
            if (success)
            {
                string skillName = supportData?.support_name ?? $"Support_{supportId}";
                Debug.Log($"[CardSelectPanel] 서포트 스킬 '{skillName}' 장착 완료! (캐릭터 ID: {targetCharacter.GetCharacterId()})");
            }
            else
            {
                Debug.LogWarning($"[CardSelectPanel] 서포트 스킬 장착 실패! (SupportID: {supportId})");
            }
        }

        /// <summary>
        /// JML: 특정 서포트 스킬과 호환되는 캐릭터 목록 반환 (Issue #424)
        /// </summary>
        private List<Novelian.Combat.Character> GetCompatibleCharacters(int supportId)
        {
            var compatibleCharacters = new List<Novelian.Combat.Character>();

            var supportData = CSVLoader.Instance?.GetData<SupportSkillData>(supportId);
            if (supportData == null) return compatibleCharacters;

            var fieldCharacters = placementManager?.GetAllCharacters() ?? new List<Novelian.Combat.Character>();

            foreach (var character in fieldCharacters)
            {
                int mainSkillId = character.GetBasicAttackSkillId();
                var mainSkillData = CSVLoader.Instance?.GetData<MainSkillData>(mainSkillId);
                if (mainSkillData == null) continue;

                if (SkillExecutor.Instance != null && SkillExecutor.Instance.IsValidCombination(mainSkillData, supportData))
                {
                    compatibleCharacters.Add(character);
                }
            }

            return compatibleCharacters;
        }

        /// <summary>
        /// JML: 서포트 스킬 카드가 현재 필드 캐릭터와 호환되는지 확인 (Issue #437)
        /// 카드 표시 전에 필터링하여 호환 불가 카드를 제외
        /// </summary>
        /// <param name="cardId">CardTable ID (081029~081037)</param>
        /// <returns>호환 가능한 캐릭터가 있으면 true</returns>
        private bool IsSupportSkillCardCompatible(int cardId)
        {
            // 서포트 스킬 카드 범위 확인 (081029~081037)
            if (cardId < 81029 || cardId > 81037) return true;  // 일반 카드는 통과

            // CardTable ID → Support_ID 변환 (081029 → 40001, 081030 → 40002, ...)
            int supportId = (cardId - 81029) + 40001;

            // 서포트 스킬 데이터 조회
            var supportData = CSVLoader.Instance?.GetData<SupportSkillData>(supportId);
            if (supportData == null)
            {
                Debug.LogWarning($"[CardSelectPanel] SupportSkillData not found for Support_ID: {supportId}");
                return true;  // 데이터 없으면 통과 (fallback)
            }

            // 필드 캐릭터 중 호환 가능한 캐릭터 검색
            var fieldCharacters = placementManager?.GetAllCharacters();
            if (fieldCharacters == null || fieldCharacters.Count == 0)
            {
                Debug.Log($"[CardSelectPanel] 필드에 캐릭터 없음 → 서포트 카드 {cardId} 필터링");
                return false;  // 필드에 캐릭터 없으면 서포트 카드 표시 안 함
            }

            foreach (var character in fieldCharacters)
            {
                // 이미 서포트 스킬이 있는 캐릭터는 스킵
                if (character.HasSupportSkill()) continue;

                // 메인 스킬 타입 조회
                int mainSkillId = character.GetBasicAttackSkillId();
                var mainSkillData = CSVLoader.Instance?.GetData<MainSkillData>(mainSkillId);
                if (mainSkillData == null) continue;

                // 호환성 검증 (SkillExecutor 사용)
                if (SkillExecutor.Instance != null && SkillExecutor.Instance.IsValidCombination(mainSkillData, supportData))
                {
                    return true;  // 호환 가능한 캐릭터 존재
                }
            }

            Debug.Log($"[CardSelectPanel] 서포트 카드 {cardId} (Support_ID: {supportId}) 필터링됨 (호환 캐릭터 없음)");
            return false;  // 호환 가능한 캐릭터 없음
        }

        /// <summary>
        /// JML: Ability ID(StatType)를 CardLevelTable ID로 매핑
        /// 현재 티어 1 기준 (추후 티어 시스템 구현 시 확장)
        /// </summary>
        private int MapAbilityIdToCardLevelId(int abilityId)
        {
            // Tier 1 기준 매핑 (StatType enum 값 사용)
            return abilityId switch
            {
                (int)StatType.Damage => 25001,           // 공격력 증가 Tier1
                (int)StatType.CritMultiplier => 25004,   // 치명타 배율 증가 Tier1
                (int)StatType.AttackSpeed => 25007,      // 공격속도 증가 Tier1
                (int)StatType.CritChance => 25010,       // 치명타 확률 증가 Tier1
                (int)StatType.ProjectileSpeed => 25013,  // 투사체 속도 증가 Tier1
                (int)StatType.TotalDamage => 25016,      // 총 공격력 증가 Tier1
                (int)StatType.BonusDamage => 25019,      // 추가 데미지 Tier1
                (int)StatType.HealthRegen => 25022,      // 체력 회복 Tier1
                (int)StatType.Range => 25085,            // 사거리 증가 Tier1
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

            // 서포트 스킬 선택 상태 초기화 (Issue #437)
            ResetSupportSkillSelection();

            ClearCards();
            isCardSelected = false;
        }

        /// <summary>
        /// JML: 서포트 스킬 선택 상태 완전 초기화 (Issue #437)
        /// </summary>
        private void ResetSupportSkillSelection()
        {
            // 스케일 복원
            if (selectedSlotIndex >= 0 && selectedSlotIndex < cardSlots.Length && cardSlots[selectedSlotIndex] != null)
            {
                cardSlots[selectedSlotIndex].transform.localScale = Vector3.one * NORMAL_CARD_SCALE;
            }

            selectedSlotIndex = -1;
            selectedSupportCardId = -1;

            // 캐릭터 애니메이션 초기화
            if (characterCardGridManager != null)
            {
                characterCardGridManager.ResetCharacterAnimations();
            }
        }

        private void ClearCards()
        {
            // 새 시스템: 카드 슬롯 비활성화
            if (useCardSlotSystem && cardSlots != null)
            {
                for (int i = 0; i < cardSlots.Length; i++)
                {
                    if (cardSlots[i] != null)
                    {
                        var button = cardSlots[i].GetComponent<UnityEngine.UI.Button>();
                        if (button != null)
                        {
                            button.onClick.RemoveAllListeners();
                        }
                        cardSlots[i].gameObject.SetActive(false);
                    }
                }
                activeCardData = null;
                return;
            }

            // Legacy: 동적 생성된 카드 삭제
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

            // Issue #476: 기존 타이머 취소 후 새로 생성
            selectionCts?.Cancel();
            selectionCts?.Dispose();
            selectionCts = new CancellationTokenSource();

            // 로컬 변수로 캡처 (동시 실행 시 안전성 확보)
            var cts = selectionCts;
            float remainingTime = SELECTION_TIME;

            UpdateTimerText(remainingTime);

            try
            {
                while (remainingTime > 0 && !isCardSelected && cts != null && !cts.IsCancellationRequested)
                {
                    await UniTask.Delay(1000, ignoreTimeScale: true, cancellationToken: cts.Token);
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
                // 현재 CTS와 같은 경우에만 정리 (다른 타이머가 이미 시작되었을 수 있음)
                if (selectionCts == cts)
                {
                    selectionCts?.Dispose();
                    selectionCts = null;
                }
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
            // 새 시스템: cardSlots 사용
            if (useCardSlotSystem && activeCardData != null && activeCardData.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, activeCardData.Length);
                Debug.Log($"[CardSelectPanel] 랜덤 선택: 카드 슬롯 {randomIndex + 1}");
                OnCardSlotClicked(randomIndex);
                return;
            }

            // Legacy: cardInstances 사용
            if (cardInstances == null || cardInstances.Length == 0)
            {
                Debug.LogWarning("[CardSelectPanel] 선택할 카드가 없습니다!");
                Close();
                return;
            }

            int legacyRandomIndex = UnityEngine.Random.Range(0, cardInstances.Length);
            var cardButton = cardInstances[legacyRandomIndex]?.GetComponent<UnityEngine.UI.Button>();
            if (cardButton != null)
            {
                Debug.Log($"[CardSelectPanel] 랜덤 선택: 카드 {legacyRandomIndex + 1}");
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
