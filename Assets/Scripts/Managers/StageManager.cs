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
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        [Header("Camera (Required - not in map prefab)")]
        [SerializeField] private Unity.Cinemachine.CinemachineCamera cinemachineCamera;

        [Header("Map Prefab Dynamic Loading (Issue #420)")]
        [Tooltip("맵 프리팹에서 동적으로 로드됨 - Inspector 할당 불필요")]
        private Transform protectionObj;       // Tag: Wall
        private Wall wallComponent;            // Tag: Wall에서 GetComponent
        private Transform spawnArea1;          // Tag: SpawnArea1
        private Transform spawnArea2;          // Tag: SpawnArea2
        private Transform protectionObj2;      // Tag: Wall2 (양방향 방어)
        private Wall wallComponent2;           // Tag: Wall2에서 GetComponent
        private GameObject currentMapObject;   // 현재 로드된 맵 오브젝트
        private AsyncOperationHandle<GameObject> loadedMapHandle;  // Addressables 핸들 (메모리 해제용)
        private string currentMapPrefabKey = null;  // 현재 로드된 맵의 Addressable Key (중복 로드 방지용)

        /// <summary>
        /// JML: 맵 로드 완료 여부 (외부에서 확인용)
        /// </summary>
        public bool IsMapLoaded { get; private set; } = false;

        [Header("Settings")]
        [SerializeField] private StageSettings stageSettings;

        public int CurrentStageId { get; private set; }
        public string StageName { get; private set; }

        #region PlayerStageLevel
        private const int MAX_LEVEL = 50;
        private int currentExp = 0;
        private int level = 0;

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
            // CSVLoader 초기화 확인
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogError("[StageManager] CSVLoader is not initialized! Please start from BootScene.");
                return;
            }

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

            // 2. Layout Preset 적용 (맵 로드 → 참조 찾기 → Wall/Wave 초기화)
            // ApplyLayoutPresetAsync 내부에서 Wall HP 설정 및 Wave 초기화 수행
            ApplyLayoutPresetAsync(stageData).Forget();
        }

        /// <summary>
        /// JML: 레이아웃 프리셋 비동기 적용 (Issue #420 - 완전 동적 로드)
        /// 1. 맵 프리팹 로드 → 2. Tag로 Wall/SpawnArea 찾기 → 3. Wall HP 설정 → 4. Wave 초기화
        /// </summary>
        private async UniTaskVoid ApplyLayoutPresetAsync(StageData stageData)
        {
            try
            {
            int layoutType = stageData.Layout_Type;

            if (layoutType <= 0)
            {
                Debug.Log("[StageManager] No layout type specified, using default layout");
                // 기본 레이아웃에서도 Wave 초기화 및 카드 선택 필요
                InitializeWavesAfterMapLoad(stageData);
                ShowStartCardSelection().Forget();
                return;
            }

            // CSV에서 LayoutPresetData 가져오기
            LayoutPresetData layoutData = CSVLoader.Instance.GetData<LayoutPresetData>(layoutType);
            if (layoutData == null)
            {
                Debug.LogWarning($"[StageManager] Layout preset not found for ID: {layoutType}");
                InitializeWavesAfterMapLoad(stageData);
                ShowStartCardSelection().Forget();
                return;
            }

            Debug.Log($"[StageManager] Applying layout preset: {layoutData.Layout_Name} (ID: {layoutType})");

            // 1. Map Prefab 로드 (await로 완료 대기)
            if (!string.IsNullOrEmpty(layoutData.Map_Prefab_Key))
            {
                await LoadAndSwapMapPrefabAsync(layoutData.Map_Prefab_Key);
            }

            // 2. 맵에서 Tag로 참조 찾기
            FindMapReferences(layoutData.Protection_Count);

            // 3. CharacterPlacementManager에 그리드 레이아웃 적용
            if (characterPlacementManager != null)
            {
                characterPlacementManager.ApplyLayout(layoutData);
            }
            else
            {
                Debug.LogWarning("[StageManager] CharacterPlacementManager is null!");
            }

            // 4. Camera 위치/회전 적용
            if (cinemachineCamera != null)
            {
                cinemachineCamera.transform.position = new Vector3(
                    layoutData.Camera_Pos_X,
                    layoutData.Camera_Pos_Y,
                    layoutData.Camera_Pos_Z
                );
                cinemachineCamera.transform.eulerAngles = new Vector3(
                    layoutData.Camera_Rot_X,
                    layoutData.Camera_Rot_Y,
                    layoutData.Camera_Rot_Z
                );
                Debug.Log($"[StageManager] Camera positioned at ({layoutData.Camera_Pos_X}, {layoutData.Camera_Pos_Y}, {layoutData.Camera_Pos_Z}), rotation ({layoutData.Camera_Rot_X}, {layoutData.Camera_Rot_Y}, {layoutData.Camera_Rot_Z})");
            }
            else
            {
                Debug.LogError("[StageManager] cinemachineCamera is null! Inspector에서 할당해주세요.");
            }

            // 5. Wall HP 설정 (맵 로드 후 참조가 있어야 가능)
            if (wallComponent != null)
            {
                wallComponent.SetMaxHealth(stageData.Barrier_HP);
                Debug.Log($"[StageManager] Wall HP set to {stageData.Barrier_HP}");
            }
            else
            {
                Debug.LogError("[StageManager] wallComponent is null after map load! Check 'Wall' tag in map prefab.");
            }

            // 6. Wall 2 HP 설정 (양방향 방어)
            if (layoutData.Protection_Count >= 2 && wallComponent2 != null)
            {
                wallComponent2.SetMaxHealth(stageData.Barrier_HP);
                Debug.Log($"[StageManager] Wall 2 HP set to {stageData.Barrier_HP}");
            }

            // 7. WaveManager에 MonsterSpawner 및 WallTarget 설정
            if (waveManager != null)
            {
                var spawner1 = spawnArea1?.GetComponent<NovelianMagicLibraryDefense.Spawners.MonsterSpawner>();
                var spawner2 = spawnArea2?.GetComponent<NovelianMagicLibraryDefense.Spawners.MonsterSpawner>();

                if (spawner1 != null)
                {
                    waveManager.SetMonsterSpawner(spawner1);
                    Debug.Log("[StageManager] MonsterSpawner 1 set to WaveManager");
                }
                if (spawner2 != null && layoutData.Spawn_Area_Count >= 2)
                {
                    waveManager.SetBossSpawner(spawner2);
                    Debug.Log("[StageManager] MonsterSpawner 2 (Boss) set to WaveManager");
                }

                // JML: WaveManager에 Wall 타겟 설정 (몬스터 목적지 설정에 필요)
                if (protectionObj != null)
                {
                    var wallColl = protectionObj.GetComponent<Collider>();
                    waveManager.SetWallTarget(protectionObj, wallComponent, wallColl);
                    Debug.Log("[StageManager] WallTarget set to WaveManager");
                }
            }

            // 8. Monster Wall 캐시 초기화 (다중 Wall 지원)
            InitializeMonsterWallCache(layoutData.Protection_Count);

            // 9. StageStateManager에 Wall 참조 갱신 알림
            if (stageStateManager != null)
            {
                stageStateManager.RefreshWallReferences();
            }

            // 10. Wave 초기화 (맵 로드 완료 후)
            InitializeWavesAfterMapLoad(stageData);

            // 11. 카드 선택 표시 (맵 로드 및 그리드 생성 완료 후)
            ShowStartCardSelection().Forget();

            Debug.Log($"[StageManager] Layout preset '{layoutData.Layout_Name}' applied successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StageManager] ApplyLayoutPresetAsync EXCEPTION: {e.Message}\n{e.StackTrace}");
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
        /// JML: 맵 프리팹에서 Tag로 Wall, SpawnArea 참조 찾기
        /// Required Tags: Wall, SpawnArea1, SpawnArea2, Wall2 (양방향 방어)
        /// </summary>
        private void FindMapReferences(int protectionCount)
        {
            // Wall 1 찾기 (Tag: Wall)
            GameObject wallObj = GameObject.FindWithTag("Wall");
            if (wallObj != null)
            {
                protectionObj = wallObj.transform;
                wallComponent = wallObj.GetComponent<Wall>();
                Debug.Log($"[StageManager] Found Wall at {wallObj.transform.position}");
            }
            else
            {
                Debug.LogError("[StageManager] Wall not found! Add 'Wall' tag to Protection object in map prefab.");
            }

            // SpawnArea 1 찾기 (Tag: SpawnArea1)
            GameObject spawn1Obj = GameObject.FindWithTag("SpawnArea1");
            if (spawn1Obj != null)
            {
                spawnArea1 = spawn1Obj.transform;
                Debug.Log($"[StageManager] Found SpawnArea1 at {spawn1Obj.transform.position}");
            }
            else
            {
                Debug.LogError("[StageManager] SpawnArea1 not found! Add 'SpawnArea1' tag to spawn area in map prefab.");
            }

            // SpawnArea 2 찾기 (Tag: SpawnArea2)
            GameObject spawn2Obj = GameObject.FindWithTag("SpawnArea2");
            if (spawn2Obj != null)
            {
                spawnArea2 = spawn2Obj.transform;
                Debug.Log($"[StageManager] Found SpawnArea2 at {spawn2Obj.transform.position}");
            }

            // Wall 2 찾기 (양방향 방어, Tag: Wall2)
            if (protectionCount >= 2)
            {
                GameObject wall2Obj = GameObject.FindWithTag("Wall2");
                if (wall2Obj != null)
                {
                    protectionObj2 = wall2Obj.transform;
                    wallComponent2 = wall2Obj.GetComponent<Wall>();
                    Debug.Log($"[StageManager] Found Wall2 at {wall2Obj.transform.position}");
                }
                else
                {
                    Debug.LogError("[StageManager] Wall2 not found for dual defense layout! Add 'Wall2' tag to second Protection.");
                }
            }

            IsMapLoaded = true;
        }

        /// <summary>
        /// JML: Monster의 Wall 캐시 초기화 (양방향 방어 지원)
        /// Protection_Count에 따라 Wall 목록 구성
        /// </summary>
        private void InitializeMonsterWallCache(int protectionCount)
        {
            List<Wall> walls = new List<Wall>();
            List<Transform> wallTransforms = new List<Transform>();
            List<Collider> wallColliders = new List<Collider>();

            // Wall 1 추가
            if (wallComponent != null)
            {
                walls.Add(wallComponent);
                wallTransforms.Add(protectionObj);
                var collider = protectionObj?.GetComponent<Collider>();
                wallColliders.Add(collider);
            }

            // Wall 2 추가 (양방향 방어 시)
            if (protectionCount >= 2 && wallComponent2 != null)
            {
                walls.Add(wallComponent2);
                wallTransforms.Add(protectionObj2);
                var collider = protectionObj2?.GetComponent<Collider>();
                wallColliders.Add(collider);
            }

            // Monster 클래스에 캐시 전달
            Monster.InitializeWallCache(walls, wallTransforms, wallColliders);
            Debug.Log($"[StageManager] Monster Wall cache initialized with {walls.Count} wall(s)");
        }

        /// <summary>
        /// JML: Addressables로 맵 프리팹 로드 후 기존 맵과 교체 (Issue #420)
        /// 같은 키의 맵이 이미 로드되어 있으면 스킵
        /// await 가능한 버전 - 맵 로드 완료까지 대기
        /// </summary>
        private async UniTask LoadAndSwapMapPrefabAsync(string mapPrefabKey)
        {
            // 같은 맵이 이미 로드되어 있으면 스킵
            if (currentMapPrefabKey == mapPrefabKey)
            {
                Debug.Log($"[StageManager] Map '{mapPrefabKey}' is already loaded, skipping swap.");
                return;
            }

            Debug.Log($"[StageManager] Loading map prefab: {mapPrefabKey} (current: {currentMapPrefabKey ?? "none"})");

            try
            {
                // 이전에 로드한 맵이 있으면 해제
                if (loadedMapHandle.IsValid())
                {
                    Addressables.Release(loadedMapHandle);
                }

                // 기존 맵이 있으면 파괴
                if (currentMapObject != null)
                {
                    UnityEngine.Object.Destroy(currentMapObject);
                    currentMapObject = null;
                }

                // 새 맵 프리팹 로드
                loadedMapHandle = Addressables.LoadAssetAsync<GameObject>(mapPrefabKey);
                GameObject mapPrefab = await loadedMapHandle;

                if (mapPrefab == null)
                {
                    Debug.LogError($"[StageManager] Failed to load map prefab: {mapPrefabKey}");
                    return;
                }

                // 새 맵 인스턴스 생성 (0, 0, 20에 생성 - LayoutTestScene 기준 위치)
                GameObject newMap = UnityEngine.Object.Instantiate(mapPrefab, new Vector3(0f, 0f, 20f), Quaternion.identity);
                newMap.name = mapPrefabKey;

                // currentMapObject 참조 업데이트
                currentMapObject = newMap;

                // 현재 맵 키 업데이트
                currentMapPrefabKey = mapPrefabKey;

                // 1프레임 대기 (인스턴스화 완료 보장)
                await UniTask.Yield();

                Debug.Log($"[StageManager] Map prefab '{mapPrefabKey}' loaded and instantiated successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[StageManager] Error loading map prefab '{mapPrefabKey}': {e.Message}");
            }
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
        /// </summary>
        private async UniTaskVoid ShowStartCardSelection()
        {
            Debug.Log("[StageManager] Opening start card selection (deck count character cards)");

            var ui = GameManager.Instance?.UI;
            if (ui != null)
            {
                ui.OpenCardSelectForGameStart(); // Opens deck count character cards (3-4)

                // Wait until card panel is closed
                while (ui != null && ui.IsCardSelectOpen())
                {
                    await UniTask.Yield();
                }

                Debug.Log("[StageManager] Start card selection completed");
            }
            else
            {
                Debug.LogError("[StageManager] UIManager is null! Cannot show start card selection.");
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
            Time.timeScale = 1f;

            // JML: Reset global stat buffs (Issue #349)
            globalStatBuffs.Clear();

            // JML: Reset map key for next stage load (Issue #420)
            currentMapPrefabKey = null;

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

            // JML: Release loaded map prefab (Issue #420)
            if (loadedMapHandle.IsValid())
            {
                Addressables.Release(loadedMapHandle);
            }

            // JML: Ensure time scale reset
            Time.timeScale = 1f;
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
            currentExp += monster.Exp;
            Debug.Log($"[StageManager] Exp +{monster.Exp} -> {currentExp}/{maxExp}");
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
            var previousTimeScale = Time.timeScale;
            // Debug.Log($"타임 스케일 {previousTimeScale}"); // LCB: Debug previous time scale
            int maxExp = GetRequiredExpForNextLevel();
            var ui = GameManager.Instance?.UI;

            while (currentExp >= maxExp && !IsMaxLevel())
            {
                currentExp -= maxExp;
                level++;

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
            Time.timeScale = previousTimeScale; // Resume game
            // Debug.Log($"타임 스케일{Time.timeScale}"); // LCB: Debug resumed time scale
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
            // HealthRegen은 Wall에 즉시 체력 회복 적용 (양방향 방어 시 모든 Wall에 적용)
            if (statType == StatType.HealthRegen)
            {
                if (wallComponent != null)
                {
                    wallComponent.HealByPercent(value);
                    Debug.Log($"[StageManager] Wall 1 체력 회복: +{value * 100f}%");
                }
                if (wallComponent2 != null && wallComponent2.gameObject.activeInHierarchy)
                {
                    wallComponent2.HealByPercent(value);
                    Debug.Log($"[StageManager] Wall 2 체력 회복: +{value * 100f}%");
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
