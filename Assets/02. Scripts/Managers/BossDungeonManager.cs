using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Spawners;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// Issue #476 - 도전던전 전용 매니저
    /// 보스 1마리 스폰, 제한시간 관리, 스턴 게이지 시스템
    /// </summary>
    public class BossDungeonManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private ObjectPoolManager poolManager;
        [SerializeField] private MonsterEvents monsterEvents;
        [SerializeField] private StageEvents stageEvents;

        [Header("Spawner")]
        [SerializeField] private MonsterSpawner bossSpawner;

        [Header("Target")]
        [SerializeField] private Transform wallTarget;
        [SerializeField] private Wall wallComponent;
        [SerializeField] private Collider wallCollider;

        [Header("UI References")]
        [SerializeField] private BossDungeonUI dungeonUI;
        [SerializeField] private CardSelectPanel cardSelectPanel;
        [SerializeField] private CharacterPlacementManager characterPlacementManager;
        [SerializeField] private CharacterCardGridManager characterCardGridManager;

        // Issue #476: 카드 선택 관련
        private int cardSelectionCount = 0;
        private const int MAX_CARD_SELECTIONS = 4;

        // 던전 데이터 (SelectedBossDungeon에서 가져옴)
        private BossDungeonData dungeonData;

        // 보스 관련
        private BossMonster currentBoss;
        private string bossAddressableKey;
        private bool isBossSpawned = false;

        // 타이머 관련
        private float remainingTime;
        private bool isTimerRunning = false;
        private System.Threading.CancellationTokenSource timerCts;

        // Issue #476: 결계 스턴 게이지 관련 (보스가 결계 공격 → 게이지 증가 → 캐릭터 스턴)
        private float currentStunGauge;  // 0에서 시작, maxStunGauge까지 증가
        private float maxStunGauge;
        private bool areCharactersStunned = false;  // 캐릭터들 스턴 상태
        private const float STUN_GAUGE_RECHARGE_DELAY = 2f;  // 스턴 후 게이지 재충전까지 대기 시간
        private const float STUN_GAUGE_DRAIN_DURATION = 0.4f;  // 게이지 감소 애니메이션 시간
        private System.Threading.CancellationTokenSource stunGaugeCts;

        // 결과
        public bool IsCleared { get; private set; } = false;
        public bool IsFailed { get; private set; } = false;

        /// <summary>
        /// Issue #476: 씬 로드 시 자동 초기화
        /// </summary>
        private void Awake()
        {
            Debug.Log("[BossDungeonManager] Awake 호출됨");

            // BossDungeon BGM 재생 (크로스페이드)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.CrossfadeBGM("BGM_BossDungeon", 1f);
            }

            Initialize();  // BaseManager.Initialize() → OnInitialize() 호출
        }

        protected override void OnInitialize()
        {
            Debug.Log("[BossDungeonManager] OnInitialize 시작");

            // 이벤트 구독
            if (monsterEvents != null)
            {
                monsterEvents.AddBossDiedListener(HandleBossDied);
                Debug.Log("[BossDungeonManager] MonsterEvents 구독 완료");
            }
            else
            {
                Debug.LogWarning("[BossDungeonManager] MonsterEvents가 null입니다!");
            }

            // Issue #476: Inspector에서 연결 필수 (Find 메서드 사용 금지)
            if (characterPlacementManager == null)
                Debug.LogError("[BossDungeonManager] CharacterPlacementManager가 Inspector에서 연결되지 않았습니다!");
            if (characterCardGridManager == null)
                Debug.LogError("[BossDungeonManager] CharacterCardGridManager가 Inspector에서 연결되지 않았습니다!");
            if (cardSelectPanel == null)
                Debug.LogError("[BossDungeonManager] CardSelectPanel이 Inspector에서 연결되지 않았습니다!");

            // SelectedBossDungeon에서 던전 데이터 가져오기
            Debug.Log($"[BossDungeonManager] SelectedBossDungeon.HasSelection = {SelectedBossDungeon.HasSelection}");

            if (SelectedBossDungeon.HasSelection)
            {
                dungeonData = SelectedBossDungeon.Data;
                Debug.Log($"[BossDungeonManager] 던전 데이터 로드됨: Floor={dungeonData?.Floor_Index}, Dungeon_ID={dungeonData?.Dungeon_ID}");
                InitializeDungeonData();

                // Issue #476: UI 초기화
                if (dungeonUI != null)
                {
                    dungeonUI.Initialize(dungeonData);
                    Debug.Log("[BossDungeonManager] dungeonUI 초기화 완료");
                }

                // Issue #476: 카드 선택 시퀀스 시작
                Debug.Log("[BossDungeonManager] StartCardSelectionSequence 호출...");
                StartCardSelectionSequence().Forget();
            }
            else
            {
                Debug.LogError("[BossDungeonManager] 선택된 던전 데이터가 없습니다! " +
                               "LobbyScene에서 BossDungeonButton 클릭 시 SelectedBossDungeon.Data가 설정되어야 합니다.");
            }
        }

        /// <summary>
        /// 던전 데이터로 초기화
        /// </summary>
        private void InitializeDungeonData()
        {
            if (dungeonData == null) return;

            // 타이머 초기화
            remainingTime = dungeonData.Time_Limit;

            // 스턴 게이지 초기화
            maxStunGauge = dungeonData.Stun_Gauge;
            currentStunGauge = 0f;

            Debug.Log($"[BossDungeonManager] 던전 초기화: Floor={dungeonData.Floor_Index}, " +
                      $"Boss_ID={dungeonData.Boss_ID}, Time_Limit={dungeonData.Time_Limit}초, " +
                      $"Stun_Gauge={dungeonData.Stun_Gauge}, Stun_Damage={dungeonData.Stun_Damage}");
        }

        protected override void OnReset()
        {
            StopTimer();
            isBossSpawned = false;
            IsCleared = false;
            IsFailed = false;
            currentStunGauge = 0f;
            areCharactersStunned = false;
            stunGaugeCts?.Cancel();
            stunGaugeCts?.Dispose();
            stunGaugeCts = null;
        }

        protected override void OnDispose()
        {
            if (monsterEvents != null)
            {
                monsterEvents.RemoveBossDiedListener(HandleBossDied);
            }

            StopTimer();
            stunGaugeCts?.Cancel();
            stunGaugeCts?.Dispose();
            stunGaugeCts = null;
            // Issue #476: SelectedBossDungeon.Clear() 제거
            // 다음 층/리트라이 시 데이터가 유지되어야 함
            // 로비 이동 시에만 BossDungeonClearPanel/FailedPanel.OnLobbyButtonClicked()에서 Clear() 호출
        }

        #region Card Selection (Issue #476)

        /// <summary>
        /// Issue #476: 카드 선택 시퀀스
        /// 1. 덱의 모든 캐릭터 자동 소환
        /// 2. 카드 선택 4회 (캐릭터 3장 + 스탯 1장)
        /// </summary>
        private async UniTask StartCardSelectionSequence()
        {
            Debug.Log("[BossDungeonManager] ===== 도전던전 초기화 시작 =====");

            // 1. 덱의 모든 캐릭터 자동 소환
            Debug.Log("[BossDungeonManager] STEP 1: 덱 캐릭터 소환 시작...");
            await SpawnAllDeckCharacters();
            Debug.Log("[BossDungeonManager] STEP 1 완료: 덱 캐릭터 소환 끝");

            // 2. 카드 선택 시퀀스 (4번)
            cardSelectionCount = 0;
            Debug.Log("[BossDungeonManager] STEP 2: 카드 선택 시퀀스 시작 (4회)");
            Debug.Log($"[BossDungeonManager] cardSelectPanel: {(cardSelectPanel != null ? "있음" : "NULL!")}");

            if (cardSelectPanel == null)
            {
                Debug.LogError("[BossDungeonManager] cardSelectPanel이 null입니다! " +
                               "Inspector에서 CardSelectPanel을 연결해주세요.");
                // 카드 선택 없이 바로 던전 시작
                Debug.Log("[BossDungeonManager] 카드 선택 없이 던전 시작...");
                await StartDungeonAsync();
                return;
            }

            // Issue #476: CardSelectPanel에 참조 전달 (FindFirstObjectByType 사용 금지)
            if (characterPlacementManager != null)
            {
                cardSelectPanel.SetPlacementManager(characterPlacementManager);
            }
            if (characterCardGridManager != null)
            {
                cardSelectPanel.SetCharacterCardGridManager(characterCardGridManager);
            }

            while (cardSelectionCount < MAX_CARD_SELECTIONS)
            {
                Debug.Log($"[BossDungeonManager] 카드 선택 {cardSelectionCount + 1}/{MAX_CARD_SELECTIONS} 시작...");

                // 도전던전 전용 카드 선택 패널 (캐릭터 3장 + 스탯 1장)
                cardSelectPanel.OpenForBossDungeon();
                Debug.Log("[BossDungeonManager] OpenForBossDungeon 호출됨, 패널 닫힘 대기...");

                // 카드 선택 완료 대기
                await UniTask.WaitUntil(() => !cardSelectPanel.IsOpen);

                cardSelectionCount++;
                Debug.Log($"[BossDungeonManager] 카드 선택 {cardSelectionCount}/{MAX_CARD_SELECTIONS} 완료!");
            }

            Debug.Log("[BossDungeonManager] STEP 2 완료: 카드 선택 시퀀스 끝!");
            Debug.Log("[BossDungeonManager] STEP 3: 던전 시작...");

            // 던전 시작
            await StartDungeonAsync();
        }

        /// <summary>
        /// Issue #476: 덱의 모든 캐릭터 자동 소환
        /// </summary>
        private async UniTask SpawnAllDeckCharacters()
        {
            Debug.Log("[BossDungeonManager] SpawnAllDeckCharacters 시작");

            // Issue #476: CSV 로딩 대기 (GameScene의 CardSelectPanel과 동일하게)
            // 두 번째 진입 시 CSVLoader가 아직 초기화 안됐으면 ChaCard.Initialize()가 실패함
            if (CSVLoader.Instance != null && !CSVLoader.Instance.IsInit)
            {
                Debug.Log("[BossDungeonManager] CSV 로딩 대기 중...");
                int maxWaitFrames = 600;
                int frameCount = 0;
                while (!CSVLoader.Instance.IsInit && frameCount < maxWaitFrames)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    frameCount++;
                }
                Debug.Log($"[BossDungeonManager] CSV 로딩 완료 (IsInit: {CSVLoader.Instance.IsInit})");
            }

            // CharacterPlacementManager 확인 (Inspector에서 할당 필수)
            if (characterPlacementManager == null)
            {
                Debug.LogError("[BossDungeonManager] CharacterPlacementManager가 null입니다! " +
                               "Inspector에서 CharacterPlacementManager를 할당해주세요.");
                return;
            }
            Debug.Log("[BossDungeonManager] CharacterPlacementManager 확인됨!");

            // Issue #476: CharacterPlacementManager 초기화 완료 대기 (프리로드 + 그리드 생성)
            Debug.Log("[BossDungeonManager] CharacterPlacementManager 초기화 대기 중...");
            await characterPlacementManager.WaitForInitializationAsync();
            Debug.Log($"[BossDungeonManager] CharacterPlacementManager 초기화 완료 (IsFullyInitialized: {characterPlacementManager.IsFullyInitialized})");

            // 덱에서 캐릭터 목록 가져오기
            Debug.Log($"[BossDungeonManager] DeckManager.Instance: {(DeckManager.Instance != null ? "있음" : "NULL!")}");
            var deckCharacters = DeckManager.Instance?.GetValidCharacters();
            if (deckCharacters == null || deckCharacters.Count == 0)
            {
                Debug.LogError("[BossDungeonManager] DeckManager가 없거나 덱이 비어있습니다! " +
                               "LobbyScene에서 덱 설정이 되어 있는지 확인하세요.");
                return;
            }

            Debug.Log($"[BossDungeonManager] 덱 캐릭터 {deckCharacters.Count}명 소환 시작: [{string.Join(", ", deckCharacters)}]");

            // Issue #476: 카드 초기화 작업 수집 (병렬 실행 후 모두 완료 대기)
            var cardInitTasks = new System.Collections.Generic.List<UniTask>();
            int uiSlotIndex = 0;

            foreach (int characterId in deckCharacters)
            {
                Debug.Log($"[BossDungeonManager] 캐릭터 {characterId} 소환 시도...");
                bool spawned = characterPlacementManager.SpawnCharacterById(characterId);

                if (spawned)
                {
                    Debug.Log($"[BossDungeonManager] 캐릭터 {characterId} 소환 완료!");

                    // CharacterCardGrid UI 업데이트
                    if (characterCardGridManager != null)
                    {
                        Debug.Log($"[BossDungeonManager] characterCardGridManager.instanceId={characterCardGridManager.GetInstanceID()}");
                        var spawnedCharacter = characterPlacementManager.GetCharacterById(characterId);
                        int starTier = spawnedCharacter?.GetStarTier() ?? 1;
                        // Issue #476: .Forget() 대신 Task 수집 → 나중에 일괄 대기
                        cardInitTasks.Add(characterCardGridManager.OnCharacterSpawned(uiSlotIndex, characterId, starTier, spawnedCharacter));
                        uiSlotIndex++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[BossDungeonManager] 캐릭터 {characterId} 소환 실패!");
                }
            }

            // Issue #476: 모든 카드 초기화 완료 대기 (레이스 컨디션 방지)
            if (cardInitTasks.Count > 0)
            {
                await UniTask.WhenAll(cardInitTasks);
                Debug.Log($"[BossDungeonManager] 모든 카드 UI 초기화 완료");
            }

            Debug.Log($"[BossDungeonManager] 덱 캐릭터 소환 완료: {uiSlotIndex}명");
        }

        #endregion

        /// <summary>
        /// 던전 시작 (카드 선택 완료 후 호출)
        /// </summary>
        public async UniTask StartDungeonAsync()
        {
            if (dungeonData == null)
            {
                Debug.LogError("[BossDungeonManager] 던전 데이터가 없어서 시작할 수 없습니다!");
                return;
            }

            // 보스 스폰
            await SpawnBossAsync();

            // 타이머 시작
            StartTimer();

            Debug.Log("[BossDungeonManager] 도전던전 시작!");
        }

        /// <summary>
        /// 보스 스폰
        /// Issue #476: Monster 프리팹을 그대로 사용하고, BossMonster 컴포넌트를 런타임에 추가
        /// 이 방식으로 기존 스테이지 시스템의 Monster.cs는 전혀 수정하지 않음
        /// </summary>
        private async UniTask SpawnBossAsync()
        {
            if (isBossSpawned) return;

            // Boss_ID로 MonsterData 조회 → Addressable Key 가져오기
            bossAddressableKey = AddressableKey.GetMonsterAddressableKey(dungeonData.Boss_ID);
            if (string.IsNullOrEmpty(bossAddressableKey))
            {
                Debug.LogError($"[BossDungeonManager] Boss_ID {dungeonData.Boss_ID}의 Addressable Key를 찾을 수 없습니다!");
                return;
            }

            // 풀 생성 (Monster 타입으로 - 모든 몬스터 프리팹에 있음)
            if (!poolManager.HasPoolByKey(bossAddressableKey))
            {
                bool success = await poolManager.CreatePoolByKeyAsync<Monster>(bossAddressableKey, defaultCapacity: 1, maxSize: 3);
                if (!success)
                {
                    Debug.LogError($"[BossDungeonManager] 보스 풀 생성 실패: {bossAddressableKey}");
                    return;
                }
            }

            // 스폰 위치 (Y값은 지면 높이로 고정)
            Vector3 spawnPos = bossSpawner != null
                ? bossSpawner.GetRandomSpawnPosition()
                : Vector3.zero;
            spawnPos.y = 1f;

            // Monster로 스폰 (모든 프리팹에 Monster 컴포넌트가 있음)
            Monster monsterComponent = poolManager.SpawnByKey<Monster>(bossAddressableKey, spawnPos);

            if (monsterComponent != null)
            {
                GameObject bossObject = monsterComponent.gameObject;

                // Monster 컴포넌트 즉시 제거 (Despawn 충돌 방지 + OnEnable/OnDisable 꼬임 방지)
                // Object.Destroy()는 프레임 끝에 실행되어 SetActive 시 Monster.OnEnable()이 호출될 수 있음
                DestroyImmediate(monsterComponent);
                Debug.Log($"[BossDungeonManager] Monster 컴포넌트 즉시 제거됨");

                // 태그 변경: Monster → BossMonster (Projectile 충돌 판정용)
                bossObject.tag = Tag.BossMonster;
                Debug.Log($"[BossDungeonManager] 태그 변경: {Tag.BossMonster}");

                // BossMonster 컴포넌트 가져오기 또는 추가
                currentBoss = bossObject.GetComponent<BossMonster>();
                if (currentBoss == null)
                {
                    // GameObject 비활성화 → AddComponent 시 OnEnable 호출 방지
                    bossObject.SetActive(false);

                    currentBoss = bossObject.AddComponent<BossMonster>();
                    currentBoss.InitializeReferences(); // 참조 먼저 설정

                    // 다시 활성화 → OnEnable 시 참조가 이미 있음
                    bossObject.SetActive(true);
                    Debug.Log($"[BossDungeonManager] BossMonster 컴포넌트 런타임 추가됨");
                }

                // BossMonster 활성화
                currentBoss.enabled = true;

                // MonsterLevelData 가져와서 초기화
                MonsterLevelData levelData = CSVLoader.Instance.GetData<MonsterLevelData>(dungeonData.Boss_Level_ID);
                currentBoss.Initialize(levelData, dungeonData.Boss_ID, monsterEvents);

                // OnSpawn 호출 먼저 (상태 초기화 + TargetRegistry 등록)
                currentBoss.OnSpawn();

                // 목적지 설정
                if (wallTarget != null)
                {
                    currentBoss.SetDestination(wallTarget.position);
                }

                // 도전던전 전용 보스 설정 (OnSpawn 이후에 호출해야 wall 참조가 유지됨)
                SetupBossForDungeon(currentBoss);

                // Monster.OnDisable()에서 꺼진 콜라이더 다시 활성화
                Collider bossCollider = bossObject.GetComponent<Collider>();
                if (bossCollider != null)
                {
                    bossCollider.enabled = true;
                    Debug.Log($"[BossDungeonManager] 보스 콜라이더 재활성화");
                }

                isBossSpawned = true;
                Debug.Log($"[BossDungeonManager] 보스 스폰 완료: {bossAddressableKey}");
            }
        }

        /// <summary>
        /// 도전던전 전용 보스 설정 (스턴 게이지, 공격 패턴 등)
        /// </summary>
        private void SetupBossForDungeon(BossMonster boss)
        {
            if (boss == null || dungeonData == null) return;

            // Issue #476: BossDungeonManager 참조 주입 (스턴 게이지용)
            boss.SetBossDungeonManager(this);

            // Issue #476: Wall 참조 설정 (거리 기반 공격 범위 체크용 - Monster.cs와 동일 방식)
            if (wallComponent != null && wallCollider != null)
            {
                boss.SetWallTarget(wallComponent, wallCollider);
            }
            else
            {
                Debug.LogWarning("[BossDungeonManager] Wall 참조가 설정되지 않았습니다! " +
                                 "Inspector에서 wallComponent와 wallCollider를 연결해주세요.");
            }

            // 공격 주기 설정
            boss.SetAttackInterval(dungeonData.Attack_Period);

            Debug.Log($"[BossDungeonManager] 보스 설정 완료: Attack_Period={dungeonData.Attack_Period}초, " +
                      $"Wall={wallComponent != null}, WallCollider={wallCollider != null}");
        }

        #region Timer

        /// <summary>
        /// 타이머 시작
        /// </summary>
        private void StartTimer()
        {
            StopTimer();

            timerCts = new System.Threading.CancellationTokenSource();
            isTimerRunning = true;
            TimerLoopAsync(timerCts.Token).Forget();
        }

        /// <summary>
        /// 타이머 중지
        /// </summary>
        private void StopTimer()
        {
            isTimerRunning = false;
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;
        }

        /// <summary>
        /// 타이머 루프
        /// </summary>
        private async UniTaskVoid TimerLoopAsync(System.Threading.CancellationToken token)
        {
            while (isTimerRunning && remainingTime > 0 && !token.IsCancellationRequested)
            {
                await UniTask.Yield(token);

                remainingTime -= Time.deltaTime;

                // UI 업데이트
                if (dungeonUI != null)
                {
                    dungeonUI.UpdateTimer(remainingTime);
                }

                // 시간 경고 (30초, 10초)
                CheckTimeWarning();
            }

            // 시간 초과 → 실패
            if (remainingTime <= 0 && !IsCleared && !IsFailed)
            {
                OnDungeonFailed("시간 초과!");
            }
        }

        /// <summary>
        /// 시간 경고 체크
        /// </summary>
        private void CheckTimeWarning()
        {
            if (dungeonUI == null) return;

            if (remainingTime <= 10f && remainingTime > 9.9f)
            {
                dungeonUI.ShowTimeWarning(true); // 긴급 경고
            }
            else if (remainingTime <= 30f && remainingTime > 29.9f)
            {
                dungeonUI.ShowTimeWarning(false); // 일반 경고
            }
        }

        #endregion

        #region Stun Gauge (Issue #476: 결계 스턴 게이지 시스템)

        /// <summary>
        /// 보스가 결계를 공격할 때 호출 (BossMonster에서 호출)
        /// 스턴 게이지 증가 → 100 도달 시 캐릭터들 스턴
        /// </summary>
        public void OnBossAttackWall()
        {
            if (dungeonData == null || areCharactersStunned || IsCleared || IsFailed) return;

            // 스턴 게이지 증가 (0 → maxStunGauge)
            currentStunGauge += dungeonData.Stun_Damage;
            currentStunGauge = Mathf.Min(currentStunGauge, maxStunGauge);

            Debug.Log($"[BossDungeonManager] 결계 스턴 게이지: {currentStunGauge}/{maxStunGauge}");

            // UI 업데이트
            if (dungeonUI != null)
            {
                dungeonUI.UpdateStunGauge(currentStunGauge, maxStunGauge);
            }

            // 스턴 게이지 꽉 참 → 캐릭터들 스턴
            if (currentStunGauge >= maxStunGauge)
            {
                ApplyCharacterStun();
            }
        }

        /// <summary>
        /// 캐릭터들에게 스턴 적용 (결계 스턴 게이지 100 도달 시)
        /// </summary>
        private void ApplyCharacterStun()
        {
            if (areCharactersStunned) return;

            areCharactersStunned = true;
            float stunDuration = dungeonData.Stun_Duration;

            // 모든 캐릭터에게 스턴 적용
            if (characterPlacementManager != null)
            {
                var allCharacters = characterPlacementManager.GetAllCharacters();
                foreach (var character in allCharacters)
                {
                    if (character != null && character.IsAlive())
                    {
                        character.ApplyStunFromBossDungeon(stunDuration);
                    }
                }
            }

            // UI 업데이트 (캐릭터 스턴 효과 표시)
            if (dungeonUI != null)
            {
                dungeonUI.ShowCharacterStunEffect(stunDuration);
            }

            Debug.Log($"[BossDungeonManager] 캐릭터들 스턴! 지속시간: {stunDuration}초");

            // 스턴 해제 및 게이지 리셋 예약
            ReleaseCharacterStunAsync(stunDuration).Forget();
        }

        /// <summary>
        /// 캐릭터 스턴 해제 및 게이지 리셋
        /// </summary>
        private async UniTaskVoid ReleaseCharacterStunAsync(float stunDuration)
        {
            // 기존 CTS 취소
            stunGaugeCts?.Cancel();
            stunGaugeCts?.Dispose();
            stunGaugeCts = new System.Threading.CancellationTokenSource();
            var token = stunGaugeCts.Token;

            try
            {
                // 스턴 지속시간 대기
                await UniTask.Delay((int)(stunDuration * 1000), cancellationToken: token);

                // 캐릭터들 스턴 해제
                areCharactersStunned = false;
                if (characterPlacementManager != null)
                {
                    var allCharacters = characterPlacementManager.GetAllCharacters();
                    foreach (var character in allCharacters)
                    {
                        if (character != null)
                        {
                            character.ReleaseStunFromBossDungeon();
                        }
                    }
                }

                Debug.Log("[BossDungeonManager] 캐릭터들 스턴 해제");

                // 2초 대기 후 게이지 감소 시작
                await UniTask.Delay((int)(STUN_GAUGE_RECHARGE_DELAY * 1000), cancellationToken: token);

                // 게이지 빠르게 감소 (0.4초 동안)
                await DrainStunGaugeAsync(token);

                Debug.Log("[BossDungeonManager] 스턴 게이지 리셋 완료");
            }
            catch (System.OperationCanceledException)
            {
                // 취소됨 (던전 종료 등)
            }
        }

        /// <summary>
        /// 스턴 게이지 빠르게 감소 애니메이션
        /// </summary>
        private async UniTask DrainStunGaugeAsync(System.Threading.CancellationToken token)
        {
            float startGauge = currentStunGauge;
            float elapsed = 0f;

            while (elapsed < STUN_GAUGE_DRAIN_DURATION && !token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / STUN_GAUGE_DRAIN_DURATION;
                currentStunGauge = Mathf.Lerp(startGauge, 0f, t);

                // UI 업데이트
                if (dungeonUI != null)
                {
                    dungeonUI.UpdateStunGauge(currentStunGauge, maxStunGauge);
                }

                await UniTask.Yield(token);
            }

            currentStunGauge = 0f;
            if (dungeonUI != null)
            {
                dungeonUI.UpdateStunGauge(0f, maxStunGauge);
            }
        }

        /// <summary>
        /// 공격 카운트다운 업데이트 (BossMonster에서 호출)
        /// </summary>
        public void UpdateAttackCountdown(float remainingSeconds)
        {
            if (dungeonUI != null)
            {
                dungeonUI.UpdateAttackCountdown(remainingSeconds);
            }
        }

        #endregion

        #region Result

        /// <summary>
        /// 보스 처치 시 호출
        /// </summary>
        private void HandleBossDied(BossMonster boss)
        {
            Debug.Log($"[BossDungeonManager] HandleBossDied 호출됨! boss={boss}, currentBoss={currentBoss}, 일치={boss == currentBoss}");

            if (boss != currentBoss)
            {
                Debug.LogWarning($"[BossDungeonManager] boss != currentBoss 불일치로 무시됨");
                return;
            }

            OnDungeonCleared();
        }

        /// <summary>
        /// 던전 클리어
        /// </summary>
        private void OnDungeonCleared()
        {
            Debug.Log($"[BossDungeonManager] OnDungeonCleared 호출됨! IsCleared={IsCleared}, IsFailed={IsFailed}");

            if (IsCleared || IsFailed)
            {
                Debug.LogWarning("[BossDungeonManager] 이미 클리어/실패 상태라 무시됨");
                return;
            }

            IsCleared = true;
            StopTimer();

            Debug.Log($"[BossDungeonManager] 던전 클리어! 남은 시간: {remainingTime:F1}초");

            // UI에 클리어 결과 표시
            Debug.Log($"[BossDungeonManager] dungeonUI: {(dungeonUI != null ? "있음" : "NULL!")}");
            if (dungeonUI != null)
            {
                Debug.Log("[BossDungeonManager] ShowClearResult 호출 중...");
                dungeonUI.ShowClearResult(remainingTime);
            }
            else
            {
                Debug.LogError("[BossDungeonManager] dungeonUI가 null이라 클리어 패널 표시 불가!");
            }

            // Note: BossDungeon은 자체 결과 처리(dungeonUI)를 사용하므로
            // stageEvents.RaiseBossDefeated()는 호출하지 않음 (StageStateManager 없음)
        }

        /// <summary>
        /// 던전 실패
        /// </summary>
        public void OnDungeonFailed(string reason)
        {
            Debug.Log($"[BossDungeonManager] OnDungeonFailed 호출됨! reason={reason}, IsCleared={IsCleared}, IsFailed={IsFailed}");

            if (IsCleared || IsFailed)
            {
                Debug.LogWarning("[BossDungeonManager] 이미 클리어/실패 상태라 무시됨");
                return;
            }

            IsFailed = true;
            StopTimer();

            Debug.Log($"[BossDungeonManager] 던전 실패: {reason}");

            // UI에 실패 결과 표시
            Debug.Log($"[BossDungeonManager] dungeonUI: {(dungeonUI != null ? "있음" : "NULL!")}");
            if (dungeonUI != null)
            {
                Debug.Log("[BossDungeonManager] ShowFailResult 호출 중...");
                dungeonUI.ShowFailResult(reason);
            }
            else
            {
                Debug.LogError("[BossDungeonManager] dungeonUI가 null이라 실패 패널 표시 불가!");
            }
        }

        #endregion

        #region Public Getters

        public float GetRemainingTime() => remainingTime;
        public float GetStunGaugePercent() => maxStunGauge > 0 ? currentStunGauge / maxStunGauge : 0f;
        public bool AreCharactersStunned() => areCharactersStunned;
        public BossDungeonData GetDungeonData() => dungeonData;
        public float GetAttackInterval() => dungeonData?.Attack_Period ?? 10f;

        #endregion

        protected override void OnDestroy()
        {
            StopTimer();
            base.OnDestroy();
        }
    }
}
