using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Spawners;
using TMPro;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manages enemy wave spawning and spawn logic
    /// MonoBehaviour 기반 Manager
    /// </summary>
    public class WaveManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private ObjectPoolManager poolManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private MonsterEvents monsterEvents;
        [SerializeField] private StageEvents stageEvents;

        [Header("Spawners")]
        [SerializeField] private MonsterSpawner monsterSpawner;
        [SerializeField] private MonsterSpawner bossSpawner;

        [Header("Target")]
        [SerializeField] private Transform wallTarget;
        [SerializeField] private Wall wallComponent;
        [SerializeField] private Collider wallCollider;

        private bool isPoolReady = false;
        private System.Threading.CancellationTokenSource spawnCts;

        #region WaveData
        private int enemyCount;
        private int initialEnemyCount;
        private int bossCount;

        // JML: 다중 웨이브 지원을 위한 필드
        private List<WaveData> waveDataList = new List<WaveData>();

        // JML: 키 기반 몬스터 풀링용 (Monster_ID → Addressable_Key 캐시)
        private Dictionary<int, string> monsterKeyCache = new Dictionary<int, string>();
        #endregion

        protected override void OnInitialize()
        {
            // GameLog.Log("[WaveManager] Initializing pools and warm up");

            // LMJ: Subscribe to EventChannels
            if (monsterEvents != null)
            {
                monsterEvents.AddMonsterDiedListener(HandleMonsterDied);
                monsterEvents.AddBossDiedListener(HandleBossDied);
            }

            // JML: Monster's static Wall cache is now initialized by StageManager.InitializeMonsterWallCache()
            // to support dual defense layout (multiple walls)

            // LMJ: Initialize pools asynchronously
            InitializePoolsAsync().Forget();
        }

        /// <summary>
        /// Initialize Monster's static Wall cache using Inspector references
        /// Falls back to FindWithTag if Inspector references are not set
        /// NOTE: For dual defense layout (multiple walls), use StageManager.InitializeMonsterWallCache() instead
        /// </summary>
        private void InitializeWallCache()
        {
            // Use Inspector references if available
            if (wallTarget != null && wallCollider != null && wallComponent != null)
            {
                // 단일 Wall을 List로 래핑하여 전달
                var walls = new List<Wall> { wallComponent };
                var transforms = new List<Transform> { wallTarget };
                var colliders = new List<Collider> { wallCollider };
                Monster.InitializeWallCache(walls, transforms, colliders);
                return;
            }

            // Fallback: Try to find Wall by tag
            if (wallTarget != null)
            {
                // Get missing components from wallTarget
                if (wallCollider == null)
                    wallCollider = wallTarget.GetComponent<Collider>();
                if (wallComponent == null)
                    wallComponent = wallTarget.GetComponent<Wall>();

                if (wallCollider != null && wallComponent != null)
                {
                    var walls = new List<Wall> { wallComponent };
                    var transforms = new List<Transform> { wallTarget };
                    var colliders = new List<Collider> { wallCollider };
                    Monster.InitializeWallCache(walls, transforms, colliders);
                    return;
                }
            }

            // Last resort: FindWithTag
            GameObject wallObj = GameObject.FindWithTag("Wall");
            if (wallObj != null)
            {
                wallTarget = wallObj.transform;
                wallCollider = wallObj.GetComponent<Collider>();
                wallComponent = wallObj.GetComponent<Wall>();
                var walls = new List<Wall> { wallComponent };
                var transforms = new List<Transform> { wallTarget };
                var colliders = new List<Collider> { wallCollider };
                Monster.InitializeWallCache(walls, transforms, colliders);
                GameLog.LogWarning("[WaveManager] Wall references not set in Inspector, using FindWithTag fallback");
            }
            else
            {
                GameLog.LogError("[WaveManager] Wall not found! Please assign Wall references in Inspector.");
            }
        }

        /// <summary>
        /// JML: 기본 초기화 완료 표시
        /// 실제 몬스터 풀은 InitializeWithWaveDataAsync에서 CSV 기반으로 생성됨
        /// </summary>
        private async UniTaskVoid InitializePoolsAsync()
        {
            // JML: 기본 초기화만 수행 (몬스터 풀은 PreloadMonsterPoolsAsync에서 생성)
            await UniTask.Yield(); // 비동기 메서드 유지

            // TODO: BossMonster 풀 생성 (별도 처리 필요시)
            // await poolManager.CreatePoolByKeyAsync<BossMonster>(bossAddressableKey, defaultCapacity: 1, maxSize: 5);

            // JML: isPoolReady는 PreloadMonsterPoolsAsync에서 설정됨
            // 여기서는 Wall 캐시 초기화 등 기본 설정만 완료된 상태
        }

        protected override void OnReset()
        {
            // GameLog.Log("[WaveManager] Resetting wave data");
            isPoolReady = false;
            enemyCount = 0;
            bossCount = 0;

            // JML: 다중 웨이브 관련 필드 초기화
            waveDataList.Clear();
            monsterKeyCache.Clear();
        }

        protected override void OnDispose()
        {
            // GameLog.Log("[WaveManager] Disposing and unsubscribing events");

            // LMJ: Unsubscribe from EventChannels
            if (monsterEvents != null)
            {
                monsterEvents.RemoveMonsterDiedListener(HandleMonsterDied);
                monsterEvents.RemoveBossDiedListener(HandleBossDied);
            }

            // Clear Monster's static Wall cache when scene unloads
            Monster.ClearWallCache();
        }

        /// <summary>
        /// LMJ: Initialize wave parameters with Inspector default values
        /// </summary>
        public new void Initialize()
        {
            base.Initialize(); // OnInitialize() 호출하여 풀 초기화
        }

        /// <summary>
        /// JML: CSV WaveData 리스트 기반 초기화 (다중 웨이브 지원) - 비동기 버전
        /// 필요한 몬스터 프리팹 풀을 미리 생성
        /// </summary>
        public async UniTask InitializeWithWaveDataAsync(List<WaveData> waves)
        {
            waveDataList = waves ?? new List<WaveData>();
            monsterKeyCache.Clear();

            // 전체 몬스터 수 계산 (모든 웨이브의 Monster_Count 합산)
            enemyCount = waveDataList.Sum(w => w.Monster_Count);
            initialEnemyCount = enemyCount;
            bossCount = 0;

            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(enemyCount);
            }

            // JML: 필요한 몬스터 풀 프리로드
            await PreloadMonsterPoolsAsync(waveDataList);

            GameLog.Log($"[WaveManager] Initialized with {waveDataList.Count} waves, total monsters: {enemyCount}");
        }

        /// <summary>
        /// JML: 동기 버전 (기존 호환용) - 풀 프리로드는 WaveLoop에서 처리
        /// </summary>
        public void InitializeWithWaveData(List<WaveData> waves)
        {
            waveDataList = waves ?? new List<WaveData>();
            monsterKeyCache.Clear();

            enemyCount = waveDataList.Sum(w => w.Monster_Count);
            initialEnemyCount = enemyCount;
            bossCount = 0;

            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(enemyCount);
            }

            GameLog.Log($"[WaveManager] Initialized with {waveDataList.Count} waves, total monsters: {enemyCount}");
        }

        /// <summary>
        /// JML: WaveData에서 필요한 몬스터 종류를 추출하고 키 기반 풀 생성 + 비동기 웜업
        /// </summary>
        private async UniTask PreloadMonsterPoolsAsync(List<WaveData> waves)
        {
            // JML: 먼저 모든 Monster_ID → Addressable_Key 매핑 구축
            var uniqueMonsterIds = waves.Select(w => w.Monster_ID).Distinct().ToList();

            foreach (var monsterId in uniqueMonsterIds)
            {
                string addressableKey = GetMonsterAddressableKey(monsterId);
                if (!string.IsNullOrEmpty(addressableKey))
                {
                    monsterKeyCache[monsterId] = addressableKey;
                }
            }

            // JML: Addressable_Key 기준으로 그룹화하여 웜업 수량 계산
            // 같은 프리팹을 사용하는 모든 Monster_ID의 웨이브 중 최대 수량 사용
            var keyToWarmUpCount = new Dictionary<string, int>();

            foreach (var wave in waves)
            {
                if (monsterKeyCache.TryGetValue(wave.Monster_ID, out string key))
                {
                    if (!keyToWarmUpCount.ContainsKey(key))
                    {
                        keyToWarmUpCount[key] = wave.Monster_Count;
                    }
                    else
                    {
                        // 같은 키의 웨이브 중 최대값 사용
                        keyToWarmUpCount[key] = Mathf.Max(keyToWarmUpCount[key], wave.Monster_Count);
                    }
                }
            }

            GameLog.Log($"[WaveManager] Preloading {keyToWarmUpCount.Count} unique monster prefabs...");

            // JML: 고유 Addressable_Key 기준으로 풀 생성 및 웜업
            foreach (var kvp in keyToWarmUpCount)
            {
                string addressableKey = kvp.Key;
                int warmUpCount = kvp.Value;

                if (!poolManager.HasPoolByKey(addressableKey))
                {
                    bool success = await poolManager.CreatePoolByKeyAsync<Monster>(addressableKey, defaultCapacity: 20, maxSize: 500);
                    if (success)
                    {
                        await poolManager.WarmUpByKeyAsync<Monster>(addressableKey, warmUpCount);
                        GameLog.Log($"[WaveManager] Pool '{addressableKey}' warm-up: {warmUpCount}");
                    }
                    else
                    {
                        GameLog.LogError($"[WaveManager] Failed to create pool for: {addressableKey}");
                    }
                }
            }

            isPoolReady = true;
            GameLog.Log($"[WaveManager] Monster pools preloaded and warmed up: {keyToWarmUpCount.Count} prefabs, {monsterKeyCache.Count} monster types");
        }

        /// <summary>
        /// JML: Monster_ID → MonsterData.Path_ID → PathData.Addressable_Key 조회
        /// </summary>
        private string GetMonsterAddressableKey(int monsterId)
        {
            // 캐시에 있으면 바로 반환
            if (monsterKeyCache.TryGetValue(monsterId, out string cachedKey))
            {
                return cachedKey;
            }

            // MonsterData에서 Path_ID 조회
            MonsterData monsterData = CSVLoader.Instance.GetTable<MonsterData>().GetId(monsterId);
            if (monsterData == null)
            {
                GameLog.LogError($"[WaveManager] MonsterData not found for ID: {monsterId}");
                return null;
            }

            // PathData에서 Addressable_Key 조회
            PathData pathData = CSVLoader.Instance.GetTable<PathData>().GetId(monsterData.Path_ID);
            if (pathData == null)
            {
                GameLog.LogError($"[WaveManager] PathData not found for ID: {monsterData.Path_ID}");
                return null;
            }

            return pathData.Addressable_Key;
        }

        /// <summary>
        /// JML: Start wave loop - CSV 기반 키 풀링만 사용
        /// </summary>
        public async UniTaskVoid WaveLoop()
        {
            // CSV 웨이브 데이터 필수
            if (waveDataList.Count == 0)
            {
                GameLog.LogError("[WaveManager] No wave data! Call InitializeWithWaveDataAsync first.");
                return;
            }

            // JML: 풀이 아직 프리로드 안됐으면 여기서 프리로드
            if (monsterKeyCache.Count == 0)
            {
                await PreloadMonsterPoolsAsync(waveDataList);
            }

            // Wait for pools to be ready before spawning
            await UniTask.WaitUntil(() => isPoolReady);

            // Create new cancellation token for this wave
            spawnCts?.Cancel();
            spawnCts?.Dispose();
            spawnCts = new System.Threading.CancellationTokenSource();

            SpawnMultipleWaves(spawnCts.Token).Forget();
        }

        private void HandleMonsterDied(Monster monster)
        {
            // if (enemyCount <= 0) return;

            enemyCount--;

            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(enemyCount);
            }

            if (enemyCount == 0 && bossCount == 0)
            {
                // GameLog.Log("[WaveManager] All monsters defeated!");
                WaveClear();

                // LMJ: Use EventChannel instead of static event
                if (stageEvents != null)
                {
                    stageEvents.RaiseAllMonstersDefeated();
                }
            }
        }

        private void HandleBossDied(BossMonster boss)
        {
            bossCount--;

            // GameLog.Log("[WaveManager] Boss defeated!");

            // LMJ: Use EventChannel instead of static event
            if (stageEvents != null)
            {
                stageEvents.RaiseBossDefeated();
            }
        }

        /// <summary>
        /// JML: CSV 기반 다중 웨이브 스폰 (Spawn_Time 스케줄링)
        /// 각 웨이브의 Spawn_Time에 따라 해당 시점에 스폰 시작
        /// </summary>
        private async UniTaskVoid SpawnMultipleWaves(System.Threading.CancellationToken cancellationToken)
        {
            // 게임 시작 시간 기록
            float gameStartTime = Time.time;

            // 각 웨이브를 Spawn_Time 순으로 정렬
            var sortedWaves = waveDataList.OrderBy(w => w.Spawn_Time).ToList();

            foreach (var waveData in sortedWaves)
            {
                if (cancellationToken.IsCancellationRequested || !isPoolReady)
                    break;

                // 해당 웨이브의 Spawn_Time까지 대기
                float waitUntil = gameStartTime + waveData.Spawn_Time;
                float remainingWait = waitUntil - Time.time;

                if (remainingWait > 0)
                {
                    GameLog.Log($"[WaveManager] Waiting {remainingWait:F1}s for Wave {waveData.Wave_ID} (Spawn_Time: {waveData.Spawn_Time}s)");
                    await UniTask.Delay((int)(remainingWait * 1000), cancellationToken: cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested || !isPoolReady)
                    break;

                GameLog.Log($"[WaveManager] Starting Wave {waveData.Wave_ID}: {waveData.Monster_Count} monsters, interval {waveData.Spawn_Interval}s");

                // 해당 웨이브의 몬스터들 스폰 (blocking - 웨이브 끝날 때까지 기다림)
                await SpawnWaveMonsters(waveData, cancellationToken);
            }

            // 모든 웨이브 스폰 완료 후 보스 스폰
            if (bossCount > 0 && isPoolReady && !cancellationToken.IsCancellationRequested)
            {
                SpawnBoss();
            }
        }

        /// <summary>
        /// JML: 단일 웨이브의 몬스터들 스폰
        /// MonsterLevelData를 가져와서 Monster.Initialize() 호출
        /// 키 기반 풀링 사용 (SpawnByKey)
        /// 빈 공간 확인 후 스폰, 공간 없으면 대기
        /// </summary>
        private async UniTask SpawnWaveMonsters(WaveData waveData, System.Threading.CancellationToken cancellationToken)
        {
            // MonsterLevelData 가져오기
            MonsterLevelData levelData = CSVLoader.Instance.GetTable<MonsterLevelData>().GetId(waveData.Mon_Level_ID);
            if (levelData == null)
            {
                GameLog.LogWarning($"[WaveManager] MonsterLevelData not found for ID: {waveData.Mon_Level_ID}");
            }

            // JML: Addressable Key 조회 (캐시 또는 CSV 조회)
            string addressableKey = GetMonsterAddressableKey(waveData.Monster_ID);
            if (string.IsNullOrEmpty(addressableKey))
            {
                GameLog.LogError($"[WaveManager] Cannot spawn wave {waveData.Wave_ID}: addressable key not found for Monster_ID {waveData.Monster_ID}");
                return;
            }

            int spawnedCount = 0;
            int targetCount = waveData.Monster_Count;
            bool useSpawner1 = true; // 두 스포너 번갈아 사용
            const int SPAWN_RETRY_DELAY_MS = 100; // 빈 공간 없을 때 대기 시간

            while (spawnedCount < targetCount && isPoolReady && !cancellationToken.IsCancellationRequested)
            {
                // Check if spawner still exists (scene not destroyed)
                if (monsterSpawner == null) break;

                // JML: 빈 공간 찾기 (두 스포너가 있으면 번갈아가며 시도)
                Vector3 spawnPos;
                bool foundEmptySpace = false;

                if (bossSpawner != null)
                {
                    // 두 스포너 번갈아 시도
                    MonsterSpawner currentSpawner = useSpawner1 ? monsterSpawner : bossSpawner;
                    MonsterSpawner otherSpawner = useSpawner1 ? bossSpawner : monsterSpawner;

                    // 현재 스포너에서 빈 공간 찾기
                    if (currentSpawner.TryGetEmptySpawnPosition(out spawnPos))
                    {
                        foundEmptySpace = true;
                    }
                    // 실패하면 다른 스포너에서 시도
                    else if (otherSpawner.TryGetEmptySpawnPosition(out spawnPos))
                    {
                        foundEmptySpace = true;
                    }

                    useSpawner1 = !useSpawner1;
                }
                else
                {
                    // 스포너 하나만 있으면 그것만 사용
                    foundEmptySpace = monsterSpawner.TryGetEmptySpawnPosition(out spawnPos);
                }

                // 빈 공간을 찾지 못했으면 잠시 대기 후 재시도
                if (!foundEmptySpace)
                {
                    await UniTask.Delay(SPAWN_RETRY_DELAY_MS, cancellationToken: cancellationToken);
                    continue; // spawnedCount 증가 없이 다시 시도
                }

                // JML: 키 기반 스폰 사용 (Addressable 프리팹)
                var monster = poolManager.SpawnByKey<Monster>(addressableKey, spawnPos);

                if (monster != null)
                {
                    // JML: Addressable 키 설정 (DespawnByKey에서 사용)
                    monster.SetAddressableKey(addressableKey);

                    // JML: CSV 데이터로 몬스터 스탯 초기화 + MonsterEvents 주입
                    // (Addressables 로드 시 ScriptableObject 참조가 별도 인스턴스로 로드되는 문제 해결)
                    // Monster_ID 전달하여 상성(Genre) 시스템 지원
                    monster.Initialize(levelData, waveData.Monster_ID, monsterEvents);

                    // 목적지 설정 (Wall 위치로)
                    if (wallTarget != null)
                    {
                        monster.SetDestination(wallTarget.position);
                    }
                }

                spawnedCount++;

                // 다음 몬스터 스폰 전 대기 (마지막 몬스터면 대기 안 함)
                if (spawnedCount < targetCount)
                {
                    await UniTask.Delay((int)(waveData.Spawn_Interval * 1000), cancellationToken: cancellationToken);
                }
            }

            GameLog.Log($"[WaveManager] Wave {waveData.Wave_ID} completed: spawned {spawnedCount}/{targetCount} monsters (dual spawner: {bossSpawner != null})");
        }

        private void SpawnBoss()
        {
            Vector3 bossSpawnPos = bossSpawner != null
                ? bossSpawner.GetRandomSpawnPosition()
                : monsterSpawner.GetRandomSpawnPosition();
            var boss = poolManager.Spawn<BossMonster>(bossSpawnPos);

            // 목적지 설정 (Wall 위치로)
            if (boss != null && wallTarget != null)
            {
                boss.SetDestination(wallTarget.position);
            }

        }

        public void WaveClear()
        {
            // JML: 타입 기반 풀 정리 (Projectile, FloatingDamageText 등)
            poolManager.ClearAll();

            // JML: 키 기반 풀 정리 (Addressable 몬스터)
            poolManager.ClearAllKeyBasedPools();
        }
        public bool HasRemainingEnemies()
        {
            return enemyCount > 0;
        }

        public bool HasBoss()
        {
            return bossCount > 0;
        }

        public int GetKillCount()
        {
            return initialEnemyCount - Mathf.Max(0, enemyCount);
        }

        public int GetRemainderCount()
        {
            return enemyCount;
        }

        #region Issue #420 - 레이아웃 프리셋 시스템

        /// <summary>
        /// JML: 몬스터 스포너 동적 설정 (Issue #420)
        /// StageManager에서 레이아웃 변경 시 호출
        /// </summary>
        public void SetMonsterSpawner(MonsterSpawner spawner)
        {
            if (spawner != null)
            {
                monsterSpawner = spawner;
                GameLog.Log($"[WaveManager] MonsterSpawner set to: {spawner.name}");
            }
        }

        /// <summary>
        /// JML: 보스 스포너 동적 설정 (Issue #420)
        /// </summary>
        public void SetBossSpawner(MonsterSpawner spawner)
        {
            if (spawner != null)
            {
                bossSpawner = spawner;
                GameLog.Log($"[WaveManager] BossSpawner set to: {spawner.name}");
            }
        }

        /// <summary>
        /// JML: Wall 타겟 동적 설정 (Issue #420)
        /// StageManager에서 맵 로드 후 Wall 참조 설정 시 호출
        /// </summary>
        public void SetWallTarget(Transform wall, Wall wallComp = null, Collider wallColl = null)
        {
            wallTarget = wall;
            wallComponent = wallComp;
            wallCollider = wallColl;
            GameLog.Log($"[WaveManager] WallTarget set to: {wall?.name ?? "null"}");
        }

        /// <summary>
        /// JML: 다중 스포너 설정 (Issue #420)
        /// spawnArea1 = 몬스터 스포너, spawnArea2 = 보스 스포너 (또는 2번째 몬스터 스포너)
        /// </summary>
        public void SetSpawners(MonsterSpawner spawnArea1, MonsterSpawner spawnArea2 = null)
        {
            SetMonsterSpawner(spawnArea1);
            if (spawnArea2 != null)
            {
                SetBossSpawner(spawnArea2);
            }
        }

        #endregion

        #region Cheat Methods (Issue #435)

        /// <summary>
        /// JML: 치트용 - 스폰 루프 중단 및 남은 몬스터 수 강제 0으로 설정
        /// 아직 스폰되지 않은 예정된 몬스터도 모두 처리된 것으로 간주
        /// </summary>
        public void ForceCompleteAllWaves()
        {
            GameLog.Log($"[WaveManager] ForceCompleteAllWaves - 남은 enemyCount: {enemyCount}, bossCount: {bossCount}");

            // 1. 스폰 루프 중단
            spawnCts?.Cancel();
            spawnCts?.Dispose();
            spawnCts = null;

            // 2. 카운트 강제 0으로 설정
            enemyCount = 0;
            bossCount = 0;

            // 3. UI 업데이트
            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(0);
            }

            // 4. Wave 클리어 처리
            WaveClear();

            // 5. 승리 이벤트 발생
            if (stageEvents != null)
            {
                stageEvents.RaiseAllMonstersDefeated();
            }

            GameLog.Log("[WaveManager] ForceCompleteAllWaves - 모든 웨이브 강제 완료");
        }

        #endregion

        protected override void OnDestroy()
        {
            // Cancel all spawn operations when scene is destroyed
            spawnCts?.Cancel();
            spawnCts?.Dispose();

            base.OnDestroy();
        }
    }

}