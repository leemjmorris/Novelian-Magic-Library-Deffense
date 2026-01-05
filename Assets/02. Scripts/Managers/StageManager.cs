using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novelian.Combat;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Settings;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manages stage progression, player leveling, and stage timer
    /// MonoBehaviour 기반 Manager
    /// </summary>
    public class StageManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private MonsterEvents monsterEvents;
        [SerializeField] private StageEvents stageEvents;
        [SerializeField] private CharacterPlacementManager characterPlacementManager; // JML: Inspector에서 직접 참조 (Issue #349)
        [SerializeField] private StageStateManager stageStateManager; // JML: 맵 로드 후 Wall 참조 갱신용
        [SerializeField] private CharacterCardGridManager characterCardGridManager; // 스탯 카드 적용 후 UI 갱신용

        [Header("Camera (Required - not in map prefab)")]
        [SerializeField] private Unity.Cinemachine.CinemachineCamera cinemachineCamera;

        [Header("Map References (정적 맵 - Inspector에서 직접 할당)")]
        [SerializeField] private Transform protectionObj;       // Wall 오브젝트
        [SerializeField] private Wall wallComponent;            // Wall 컴포넌트
        [SerializeField] private Transform spawnArea1;          // SpawnArea1 (MonsterSpawner)
        [SerializeField] private Transform spawnArea2;          // SpawnArea2 (BossSpawner, 선택)

        /// <summary>
        /// JML: 맵 로드 완료 여부 (정적 맵 사용으로 항상 true)
        /// </summary>
        public bool IsMapLoaded => true;

        [Header("Settings")]
        [SerializeField] private StageSettings stageSettings;

        public int CurrentStageId { get; private set; }
        public string StageName { get; private set; }

        #region PlayerStageLevel
        private const int MAX_LEVEL = 50;
        private int currentExp = 0;
        private int level = 0;

        // Battle_Exp_UP 버프 배율 (1.0 = 기본값, 1.5 = 50% 증가)
        private float expMultiplier = 1.0f;

        /// <summary>
        /// 다음 레벨업에 필요한 경험치를 CSV에서 가져오기
        /// </summary>
        private int GetRequiredExpForNextLevel()
        {
            int targetLevel = level + 1;

            // 최대 레벨 체크
            if (targetLevel > MAX_LEVEL)
            {
                return int.MaxValue; // 레벨업 불가
            }

            // Level_ID 계산: 0701, 0702, ... 0750
            int levelId = 700 + targetLevel;

            // CSV에서 PlayerLevelData 조회
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var levelData = CSVLoader.Instance.GetData<PlayerLevelData>(levelId);
                if (levelData != null)
                {
                    return (int)levelData.Req_EXP;
                }
            }

            // fallback: stageSettings 사용
            return stageSettings != null ? stageSettings.expPerLevel : 100;
        }

        /// <summary>
        /// 최대 레벨 도달 여부 확인
        /// </summary>
        private bool IsMaxLevel()
        {
            return level >= MAX_LEVEL;
        }

        // LCB: Debug - Press 'L' key to instantly level up (add 100 exp)
        #endregion

        #region Timer
        private float RemainingTime { get; set; }
        private bool isStageCleared = false;
        private CancellationTokenSource timerCts;
        #endregion

        #region Scene Transition Flag (Issue #570)
        /// <summary>
        /// JML: 씬 전환 중 플래그 - 로비 복귀 등 씬 전환 시 true로 설정
        /// 레벨업 등 게임 이벤트를 무시하기 위해 사용
        /// </summary>
        public bool IsExitingStage { get; set; } = false;
        #endregion

        #region GlobalStatBuffs (Issue #349)
        /// <summary>
        /// JML: 전역 스텟 버프 저장소
        /// 스텟 카드 선택 시 누적되며, 필드의 모든 캐릭터에 적용됨
        /// 새로 소환되는 캐릭터에도 자동 적용
        /// </summary>
        private Dictionary<StatType, float> globalStatBuffs = new Dictionary<StatType, float>();
        #endregion

        /// <summary>
        /// LMJ: Set stage duration (useful for CSV data loading in the future)
        /// </summary>
        public void SetStageDuration(float duration)
        {
            if (stageSettings != null)
            {
                stageSettings.stageDuration = duration;
            }
            // Debug.Log($"[StageManager] Stage duration set to {duration} seconds");
        }

        protected override void OnInitialize()
        {
            Debug.Log("[StageManager] OnInitialize called");

            // LMJ: Subscribe to monster death event for exp via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.AddMonsterDiedListener(AddExp);
                Debug.Log($"[StageManager] monsterEvents 구독 완료 (AddExp) - InstanceID: {monsterEvents.GetInstanceID()}");
            }
            else
            {
                Debug.LogError("[StageManager] monsterEvents가 NULL! 인스펙터에서 MonsterEvents 할당 필요!");
            }

            // JML: CSVLoader 초기화 대기 후 스테이지 초기화 (Issue #420)
            // CSVLoader는 Start()에서 로드되므로 Awake에서는 아직 준비 안됨
            WaitForCSVAndInitialize().Forget();
        }

        /// <summary>
        /// JML: CSVLoader 초기화 완료 대기 후 스테이지 초기화 (Issue #420)
        /// </summary>
        private async UniTaskVoid WaitForCSVAndInitialize()
        {
            Debug.Log("[StageManager] Waiting for CSVLoader to initialize...");

            // CSVLoader 초기화 대기 (최대 10초)
            float timeout = 10f;
            float elapsed = 0f;
            while ((CSVLoader.Instance == null || !CSVLoader.Instance.IsInit) && elapsed < timeout)
            {
                await UniTask.Yield();
                elapsed += Time.unscaledDeltaTime;
            }

            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogError("[StageManager] CSVLoader initialization timeout! Please start from BootScene.");
                return;
            }

            Debug.Log($"[StageManager] CSVLoader ready! (waited {elapsed:F2}s)");

            // JML: CSV 데이터 기반 스테이지 초기화
            InitializeFromCSV();

            // LMJ: Start stage timer using UniTask
            timerCts = new CancellationTokenSource();
            StageTimer(timerCts.Token).Forget();
        }

        /// <summary>
        /// JML: CSV 데이터 기반으로 스테이지 초기화
        /// SelectedStage.Data에서 Time_Limit, Wave ID, Barrier_HP 등을 가져옴
        /// 동적 맵 로드 지원: 맵 로드 완료 후 Wall/SpawnArea 참조 및 Wave 초기화
        /// </summary>
        private void InitializeFromCSV()
        {
            Debug.Log("[StageManager] InitializeFromCSV called!");

            // Issue #476: BossDungeon 모드면 StageManager 초기화 스킵
            // BossDungeonManager가 별도로 처리함
            if (SelectedBossDungeon.HasSelection)
            {
                Debug.Log("[StageManager] BossDungeon mode detected - skipping StageManager initialization");
                return;
            }

            // CSVLoader 초기화 확인
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogError("[StageManager] CSVLoader is not initialized! Please start from BootScene.");
                return;
            }

            Debug.Log($"[StageManager] SelectedStage.HasSelection: {SelectedStage.HasSelection}");
            if (!SelectedStage.HasSelection)
            {
                Debug.LogWarning("[StageManager] No stage selected, using default stage (10101)");
                StageName = "Stage 1-1";

                // JML: 기본 스테이지 데이터 로드 (스테이지 선택 없이 GameScene 직접 실행 시)
                StageData defaultStage = CSVLoader.Instance.GetTable<StageData>().GetId(10101);
                if (defaultStage != null)
                {
                    SelectedStage.Data = defaultStage;
                }
                else
                {
                    Debug.LogError("[StageManager] Default stage (010101) not found in CSV!");
                    return;
                }
            }

            StageData stageData = SelectedStage.Data;
            CurrentStageId = stageData.Stage_ID;
            StageName = $"Stage {stageData.Chapter_Number}";

            // 1. Time Limit 설정
            SetStageDuration(stageData.Time_Limit);
            Debug.Log($"[StageManager] Time Limit set to {stageData.Time_Limit} seconds");

            // 2. 정적 맵 초기화 (Wall HP, Wave 등)
            InitializeStaticMap(stageData);
        }

        /// <summary>
        /// 정적 맵 초기화 (동적 맵 로드 제거됨)
        /// Inspector에서 할당된 참조 사용, Wall HP 설정, Wave 초기화
        /// </summary>
        private void InitializeStaticMap(StageData stageData)
        {
            Debug.Log("[StageManager] InitializeStaticMap called!");

            // 1. Wall HP 설정 (StageTable.csv의 Barrier_HP 사용)
            ApplyWallHP(stageData.Barrier_HP);

            // 2. WaveManager에 MonsterSpawner 및 WallTarget 설정
            SetupWaveManager();

            // 3. Monster Wall 캐시 초기화
            InitializeMonsterWallCache();

            // 4. StageStateManager에 Wall 참조 갱신 알림
            if (stageStateManager != null)
            {
                stageStateManager.RefreshWallReferences();
            }

            // 5. Wave 초기화
            InitializeWavesAfterMapLoad(stageData);

            // 6. 카드 선택 표시 (무조건 호출)
            Debug.Log("[StageManager] About to call ShowStartCardSelection...");
            ShowStartCardSelection().Forget();

            Debug.Log("[StageManager] InitializeStaticMap completed!");
        }

        /// <summary>
        /// Wall HP 설정
        /// </summary>
        private void ApplyWallHP(float barrierHP)
        {
            if (wallComponent != null)
            {
                wallComponent.SetMaxHealth(barrierHP);
                Debug.Log($"[StageManager] Wall HP set to {barrierHP}");
            }
            else
            {
                Debug.LogError("[StageManager] wallComponent is null! Inspector에서 할당해주세요.");
            }
        }

        /// <summary>
        /// JML: WaveManager에 MonsterSpawner 및 WallTarget 설정
        /// </summary>
        private void SetupWaveManager()
        {
            if (waveManager == null) return;

            var spawner1 = spawnArea1?.GetComponent<NovelianMagicLibraryDefense.Spawners.MonsterSpawner>();
            var spawner2 = spawnArea2?.GetComponent<NovelianMagicLibraryDefense.Spawners.MonsterSpawner>();

            if (spawner1 != null)
            {
                waveManager.SetMonsterSpawner(spawner1);
                Debug.Log("[StageManager] MonsterSpawner 1 set to WaveManager");
            }
            if (spawner2 != null)
            {
                waveManager.SetBossSpawner(spawner2);
                Debug.Log("[StageManager] MonsterSpawner 2 (Boss) set to WaveManager");
            }

            // WaveManager에 Wall 타겟 설정 (몬스터 목적지 설정에 필요)
            if (protectionObj != null)
            {
                var wallColl = protectionObj.GetComponent<Collider>();
                waveManager.SetWallTarget(protectionObj, wallComponent, wallColl);
                Debug.Log("[StageManager] WallTarget set to WaveManager");
            }
        }

        /// <summary>
        /// JML: 맵 로드 완료 후 Wave 데이터 수집 및 WaveManager 초기화
        /// </summary>
        private void InitializeWavesAfterMapLoad(StageData stageData)
        {
            List<WaveData> waveDataList = CollectWaveData(stageData);
            if (waveDataList.Count > 0)
            {
                waveManager.InitializeWithWaveData(waveDataList);
                waveManager.WaveLoop().Forget();
                Debug.Log($"[StageManager] Initialized with {waveDataList.Count} waves from CSV");
            }
            else
            {
                Debug.LogError("[StageManager] No wave data found! Check Wave IDs in StageTable.csv");
            }
        }

        /// <summary>
        /// Monster의 Wall 캐시 초기화
        /// </summary>
        private void InitializeMonsterWallCache()
        {
            List<Wall> walls = new List<Wall>();
            List<Transform> wallTransforms = new List<Transform>();
            List<Collider> wallColliders = new List<Collider>();

            if (wallComponent != null)
            {
                walls.Add(wallComponent);
                wallTransforms.Add(protectionObj);
                var collider = protectionObj?.GetComponent<Collider>();
                wallColliders.Add(collider);
            }

            Monster.InitializeWallCache(walls, wallTransforms, wallColliders);
            Debug.Log($"[StageManager] Monster Wall cache initialized with {walls.Count} wall(s)");
        }

        /// <summary>
        /// JML: StageData에서 Wave ID들을 수집하여 WaveData 리스트로 반환
        /// Wave_1_ID ~ Wave_4_ID 중 0이 아닌 것만 가져옴
        /// </summary>
        private List<WaveData> CollectWaveData(StageData stageData)
        {
            List<WaveData> waveDataList = new List<WaveData>();
            int[] waveIds = new int[]
            {
                stageData.Wave_1_ID,
                stageData.Wave_2_ID,
                stageData.Wave_3_ID,
                stageData.Wave_4_ID
            };

            foreach (int waveId in waveIds)
            {
                if (waveId == 0) continue;

                WaveData waveData = CSVLoader.Instance.GetTable<WaveData>().GetId(waveId);
                if (waveData != null)
                {
                    waveDataList.Add(waveData);
                    Debug.Log($"[StageManager] Added Wave {waveId}: Monster_ID={waveData.Monster_ID}, " +
                              $"Count={waveData.Monster_Count}, Spawn_Time={waveData.Spawn_Time}s");
                }
                else
                {
                    Debug.LogWarning($"[StageManager] Wave data not found for ID: {waveId}");
                }
            }

            return waveDataList;
        }

        /// <summary>
        /// LMJ: Show start card selection (deck count character cards: 3-4)
        /// Game does NOT pause for start selection
        /// UIManager 준비 대기 후 카드 패널 표시
        /// </summary>
        private async UniTaskVoid ShowStartCardSelection()
        {
            Debug.Log("[StageManager] ShowStartCardSelection called!");

            // UIManager가 준비될 때까지 대기 (최대 5초)
            float timeout = 5f;
            float elapsed = 0f;
            while ((GameManager.Instance == null || GameManager.Instance.UI == null) && elapsed < timeout)
            {
                await UniTask.Yield();
                elapsed += Time.unscaledDeltaTime;
            }

            Debug.Log($"[StageManager] GameManager.Instance: {(GameManager.Instance != null ? "OK" : "NULL")}");
            Debug.Log($"[StageManager] GameManager.Instance.UI: {(GameManager.Instance?.UI != null ? "OK" : "NULL")}");

            var ui = GameManager.Instance?.UI;
            if (ui == null)
            {
                Debug.LogError("[StageManager] UIManager is null after waiting! Cannot show start card selection.");
                return;
            }

            // JML: CardSelectPanel Awake 완료 대기 (재시작 시 타이밍 문제 해결)
            elapsed = 0f;
            while (!ui.IsCardSelectPanelReady() && elapsed < timeout)
            {
                await UniTask.Yield();
                elapsed += Time.unscaledDeltaTime;
            }

            if (!ui.IsCardSelectPanelReady())
            {
                Debug.LogWarning("[StageManager] CardSelectPanel Awake 타임아웃! 강제 진행...");
            }
            else
            {
                Debug.Log("[StageManager] CardSelectPanel Awake 완료 확인!");
            }

            Debug.Log("[StageManager] Calling OpenCardSelectForGameStart...");
            ui.OpenCardSelectForGameStart(); // Opens deck count character cards (3-4)

            // Wait until card panel is closed
            while (ui != null && ui.IsCardSelectOpen())
            {
                await UniTask.Yield();
            }

            Debug.Log("[StageManager] Start card selection completed");

            // JML: 카드 선택 완료 후 파티 시너지 버프 적용
            if (characterPlacementManager != null)
            {
                characterPlacementManager.ApplySynergyToAllCharacters();
                Debug.Log("[StageManager] 파티 시너지 버프 적용 완료");
            }
        }

        protected override void OnReset()
        {
            // Debug.Log("[StageManager] Resetting stage");

            // LMJ: Cancel and dispose timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.RemoveMonsterDiedListener(AddExp);
            }

            // LMJ: Reset stage data
            currentExp = 0;
            level = 0;
            RemainingTime = stageSettings != null ? stageSettings.stageDuration : 600f;
            isStageCleared = false;
            CurrentStageId = 0;
            // Issue #602: TimeManager를 통해 TimeScale 리셋
            TimeManager.Instance?.ResetTimeScale();
            IsExitingStage = false; // Issue #570: 플래그 초기화

            // JML: Reset global stat buffs (Issue #349)
            globalStatBuffs.Clear();

            // JML: Reset global monster debuffs (Issue #424)
            globalMonsterDebuffs.Clear();
        }

        protected override void OnDispose()
        {
            // Debug.Log("[StageManager] Disposing stage");

            // LMJ: Cancel timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.RemoveMonsterDiedListener(AddExp);
            }

            // Issue #602: TimeManager를 통해 TimeScale 리셋
            TimeManager.Instance?.ResetTimeScale();
        }

        /// <summary>
        /// LMJ: Stage timer using UniTask (works without MonoBehaviour!)
        /// Uses CancellationToken for proper cleanup
        /// </summary>
        private async UniTaskVoid StageTimer(CancellationToken ct)
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            RemainingTime = duration;
            float startTime = Time.time;
            float endTime = startTime + duration;

            try
            {
                while (Time.time < endTime && !isStageCleared)
                {
                    RemainingTime = endTime - Time.time;

                    // LMJ: Update UI timer display
                    GameManager.Instance?.UI?.UpdateWaveTimer(RemainingTime);

                    if ((int)RemainingTime <= 0)
                    {
                        RemainingTime = 0;
                        HandleTimeUp();
                        break;
                    }

                    // LMJ: Wait 1 second (1000ms)
                    await UniTask.Delay(1000, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Debug.Log("[StageManager] Timer cancelled");
            }
        }

        public void HandleTimeUp()
        {
            // Debug.Log("[StageManager] Time's Up! Stage Failed");
            waveManager.WaveClear();

            // LMJ: Use EventChannel instead of static event
            if (stageEvents != null)
            {
                stageEvents.RaiseTimeUp();
            }
        }

        /// <summary>
        /// LMJ: Add experience when monster dies
        /// </summary>
        private void AddExp(Monster monster)
        {
            Debug.Log($"[StageManager] AddExp called! Monster: {monster?.name}, Exp: {monster?.Exp}");

            // 최대 레벨이면 경험치 획득 무시
            if (IsMaxLevel())
            {
                Debug.Log("[StageManager] AddExp - 최대 레벨 도달, 경험치 무시");
                return;
            }

            int maxExp = GetRequiredExpForNextLevel();

            // Battle_Exp_UP 버프 배율 적용
            int expToAdd = Mathf.RoundToInt(monster.Exp * expMultiplier);
            currentExp += expToAdd;

            if (expMultiplier > 1.0f)
            {
                Debug.Log($"[StageManager] Exp +{expToAdd} (base {monster.Exp} x{expMultiplier:F2}) -> {currentExp}/{maxExp}");
            }
            else
            {
                Debug.Log($"[StageManager] Exp +{expToAdd} -> {currentExp}/{maxExp}");
            }
            GameManager.Instance?.UI?.UpdateExperience(currentExp, maxExp);
            if (currentExp >= maxExp)
            {
                // Debug.Log($"[StageManager] Level up triggered! currentExp={currentExp}, maxExp={maxExp}"); // LCB: Debug level up trigger
                LevelUp().Forget();
            }
        }

        /// <summary>
        /// LMJ: Handle level up with UniTask
        /// LCB: Integrate level-up card system (Issue 139)
        /// </summary>
        private async UniTaskVoid LevelUp()
        {
            // Issue #570: 씬 전환 중이면 레벨업 무시
            if (IsExitingStage)
            {
                Debug.Log("[StageManager] LevelUp skipped - exiting stage");
                return;
            }

            // Issue #602: TimeManager가 TimeScale을 관리하므로 직접 저장/복원 제거
            int maxExp = GetRequiredExpForNextLevel();
            var ui = GameManager.Instance?.UI;

            while (currentExp >= maxExp && !IsMaxLevel())
            {
                currentExp -= maxExp;
                level++;

                // 레벨업 사운드 재생 (Skill 그룹으로 재생)
                AudioManager.Instance?.PlaySkillSFX("Level_Up");

                // 다음 레벨의 필요 경험치로 갱신
                maxExp = GetRequiredExpForNextLevel();
                ui?.UpdateExperience(currentExp, maxExp);

                // LMJ: Open card selection for level up (Card_Type 1: 4 stat cards, Card_Type 2: deck count character cards)
                Debug.Log($"[StageManager] Level up to {level}! (Next: {maxExp} exp)");

                if (ui != null)
                {
                    ui.OpenCardSelectForLevelUp(); // Card_Type 1: 4 stat cards, Card_Type 2: deck count character cards (3-4)

                    // Wait until card panel is closed
                    while (ui != null && ui.IsCardSelectOpen())
                    {
                        await UniTask.Yield();
                    }
                }
                else
                {
                    Debug.LogError("[StageManager] UIManager is null!");
                }
            }
            // Issue #602: CardSelectPanel이 Close 시 TimeManager.PopTimeScale()을 호출하므로
            // 여기서 별도로 TimeScale을 복원할 필요 없음
        }

        /// <summary>
        /// LMJ: Get current stage progress (useful for UI)
        /// </summary>
        public float GetStageProgress()
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            return 1f - (RemainingTime / duration);
        }

        /// <summary>
        /// LMJ: Get remaining time (useful for UI)
        /// </summary>
        public float GetRemainingTime()
        {
            return RemainingTime;
        }

        /// <summary>
        /// LMJ: Get current level (useful for UI)
        /// </summary>
        public int GetCurrentLevel()
        {
            return level;
        }

        /// <summary>
        /// LMJ: Get current exp progress (useful for UI)
        /// </summary>
        public float GetExpProgress()
        {
            if (IsMaxLevel())
            {
                return 1f; // 최대 레벨이면 100%로 표시
            }
            int maxExp = GetRequiredExpForNextLevel();
            return (float)currentExp / maxExp;
        }

        /// <summary>
        /// Battle_Exp_UP 버프: 경험치 획득 배율 설정
        /// 버프 적용 시 percentage를 더하고, 버프 종료 시 빼면 됨
        /// </summary>
        /// <param name="percentage">추가 배율 (예: 0.5f = 50% 증가)</param>
        public void AddExpMultiplier(float percentage)
        {
            expMultiplier += percentage;
            Debug.Log($"[StageManager] Exp multiplier changed: {expMultiplier:F2} (+{percentage:F2})");
        }

        /// <summary>
        /// Battle_Exp_UP 버프 종료: 경험치 획득 배율 원복
        /// </summary>
        /// <param name="percentage">제거할 배율 (예: 0.5f)</param>
        public void RemoveExpMultiplier(float percentage)
        {
            expMultiplier -= percentage;
            if (expMultiplier < 1.0f) expMultiplier = 1.0f; // 최소값 보장
            Debug.Log($"[StageManager] Exp multiplier changed: {expMultiplier:F2} (-{percentage:F2})");
        }

        /// <summary>
        /// JML: Get remaining time
        /// </summary>
        public float GetRemainderTime()
        {
            return RemainingTime;
        }

        /// <summary>
        /// JML: Get Progress time
        /// </summary>
        public float GetProgressTime()
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            return duration - RemainingTime;
        }

        public int GetReward()
        {
            // JML: Example reward calculation based on level and time
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            int baseReward = 100;
            int timeBonus = (int)(duration - RemainingTime) / 10;
            return baseReward + (level * 50) + timeBonus;
        }

        #region GlobalStatBuff Methods (Issue #349)

        /// <summary>
        /// JML: 전역 몬스터 디버프 저장소 (Issue #424)
        /// 적 공격속도/공격력 감소 카드 선택 시 누적
        /// 새로 스폰되는 몬스터에도 자동 적용
        /// </summary>
        private Dictionary<DeBuffType, float> globalMonsterDebuffs = new Dictionary<DeBuffType, float>();

        /// <summary>
        /// JML: 전역 스텟 버프 적용
        /// 스텟 카드 선택 시 호출됨
        /// 기존 필드 캐릭터 + 새로 소환되는 캐릭터 모두에 적용
        /// </summary>
        /// <param name="statType">스텟 타입 (StatType enum)</param>
        /// <param name="value">증가 값 (% 단위, 예: 0.1 = 10%)</param>
        public void ApplyGlobalStatBuff(StatType statType, float value)
        {
            // HealthRegen은 Wall에 즉시 체력 회복 적용
            if (statType == StatType.HealthRegen)
            {
                if (wallComponent != null)
                {
                    wallComponent.HealByPercent(value);
                    Debug.Log($"[StageManager] Wall 체력 회복: +{value * 100f}%");
                }
                return;
            }

            // 1. 전역 버프 저장소에 누적
            if (globalStatBuffs.ContainsKey(statType))
            {
                globalStatBuffs[statType] += value;
            }
            else
            {
                globalStatBuffs[statType] = value;
            }

            Debug.Log($"[StageManager] Global Stat Buff Applied: {statType} +{value * 100f}% (Total: {globalStatBuffs[statType] * 100f}%)");

            // 2. 현재 필드의 모든 캐릭터에 버프 적용
            ApplyBuffToAllCharacters(statType, value);
        }

        /// <summary>
        /// JML: 특정 스텟의 전역 버프 총합 조회
        /// 새로 소환되는 캐릭터에 적용할 때 사용
        /// </summary>
        public float GetGlobalStatBuff(StatType statType)
        {
            return globalStatBuffs.TryGetValue(statType, out float value) ? value : 0f;
        }

        /// <summary>
        /// JML: 모든 전역 스텟 버프 조회
        /// 새로 소환되는 캐릭터에 모든 버프 적용 시 사용
        /// </summary>
        public Dictionary<StatType, float> GetAllGlobalStatBuffs()
        {
            return new Dictionary<StatType, float>(globalStatBuffs);
        }

        /// <summary>
        /// JML: 현재 필드의 모든 캐릭터에 버프 적용
        /// CharacterPlacementManager의 GetAllCharacters() 사용
        /// Inspector에서 직접 참조 설정 필요 (Tag 기반 lookup 제거)
        /// </summary>
        private void ApplyBuffToAllCharacters(StatType statType, float value)
        {
            // JML: Inspector에서 설정한 CharacterPlacementManager 참조 사용
            if (characterPlacementManager == null)
            {
                Debug.LogWarning("[StageManager] CharacterPlacementManager 참조가 없습니다! Inspector에서 설정해주세요.");
                return;
            }

            var characters = characterPlacementManager.GetAllCharacters();
            if (characters == null || characters.Count == 0)
            {
                Debug.Log("[StageManager] No characters in field to apply buff.");
                return;
            }

            foreach (var character in characters)
            {
                if (character != null)
                {
                    character.ApplyStatBuff(statType, value);
                }
            }

            Debug.Log($"[StageManager] Buff applied to {characters.Count} characters");

            // UI 갱신 - 캐릭터 카드 스탯 정보 업데이트
            if (characterCardGridManager != null)
            {
                characterCardGridManager.RefreshAllStats();
            }
        }

        #endregion

        #region Monster Debuff & Wall Effect Methods (Issue #424)

        /// <summary>
        /// JML: 전역 몬스터 디버프 적용 (적의 공격속도/공격력 감소 카드)
        /// 현재 필드의 모든 몬스터 + 새로 스폰되는 몬스터에 적용
        /// </summary>
        /// <param name="debuffType">디버프 타입 (ATK_Damage_Down, ATK_Speed_Down)</param>
        /// <param name="value">감소 값 (% 단위, 예: 0.1 = 10%)</param>
        public void ApplyGlobalMonsterDebuff(DeBuffType debuffType, float value)
        {
            // 1. 전역 디버프 저장소에 누적
            if (globalMonsterDebuffs.ContainsKey(debuffType))
            {
                globalMonsterDebuffs[debuffType] += value;
            }
            else
            {
                globalMonsterDebuffs[debuffType] = value;
            }

            Debug.Log($"[StageManager] Global Monster Debuff Applied: {debuffType} -{value * 100f}% (Total: {globalMonsterDebuffs[debuffType] * 100f}%)");

            // 2. 현재 필드의 모든 몬스터에 디버프 적용
            ApplyDebuffToAllMonsters(debuffType, value);
        }

        /// <summary>
        /// JML: 현재 필드의 모든 몬스터에 디버프 적용
        /// TargetRegistry를 통해 활성 몬스터 목록 조회
        /// </summary>
        private void ApplyDebuffToAllMonsters(DeBuffType debuffType, float value)
        {
            var allTargets = TargetRegistry.Instance.GetAllTargets();
            int appliedCount = 0;

            foreach (var target in allTargets)
            {
                // Monster로 캐스팅 (ITargetable이 Monster인 경우만)
                if (target is Monster monster)
                {
                    // 영구 디버프로 적용 (duration = float.MaxValue)
                    monster.ApplyDebuff(debuffType, value, float.MaxValue);
                    appliedCount++;
                }
            }

            Debug.Log($"[StageManager] Debuff {debuffType} applied to {appliedCount} monsters");
        }

        /// <summary>
        /// JML: 특정 디버프의 전역 값 조회
        /// 새로 스폰되는 몬스터에 적용할 때 사용
        /// </summary>
        public float GetGlobalMonsterDebuff(DeBuffType debuffType)
        {
            return globalMonsterDebuffs.TryGetValue(debuffType, out float value) ? value : 0f;
        }

        /// <summary>
        /// JML: 모든 전역 몬스터 디버프 조회
        /// 새로 스폰되는 몬스터에 모든 디버프 적용 시 사용
        /// </summary>
        public Dictionary<DeBuffType, float> GetAllGlobalMonsterDebuffs()
        {
            return new Dictionary<DeBuffType, float>(globalMonsterDebuffs);
        }

        /// <summary>
        /// JML: Wall에 Shield 추가 (결계 내구도 증가 카드)
        /// </summary>
        /// <param name="value">추가량 (% 단위, 예: 0.1 = 최대 쉴드의 10%)</param>
        public void ApplyWallShield(float value)
        {
            if (wallComponent != null)
            {
                wallComponent.AddShieldByPercent(value);
                Debug.Log($"[StageManager] Wall Shield 추가: +{value * 100f}%");
            }
            else
            {
                Debug.LogError("[StageManager] wallComponent is null! Cannot apply shield.");
            }
        }

        /// <summary>
        /// JML: Wall 체력 회복 (결계 회복 카드)
        /// </summary>
        /// <param name="value">회복량 (% 단위, 예: 0.1 = 최대 체력의 10%)</param>
        public void ApplyWallHeal(float value)
        {
            if (wallComponent != null)
            {
                wallComponent.HealByPercent(value);
                Debug.Log($"[StageManager] Wall 체력 회복: +{value * 100f}%");
            }
            else
            {
                Debug.LogError("[StageManager] wallComponent is null! Cannot heal.");
            }
        }

        #endregion
    }
}
