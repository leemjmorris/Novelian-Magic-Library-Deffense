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

        [Header("Settings")]
        [SerializeField] private float spawnInterval = 2f;

        [Header("Wave Settings")]
        [SerializeField] private int defaultEnemyCount = 10;
        [SerializeField] private int defaultBossCount = 0;

        private bool isPoolReady = false;
        private System.Threading.CancellationTokenSource spawnCts;

        #region WaveData
        private int enemyCount;
        private int initialEnemyCount;
        private int bossCount;

        // JML: 다중 웨이브 지원을 위한 필드
        private List<WaveData> waveDataList = new List<WaveData>();
        private bool useCSVWaveData = false;
        #endregion

        protected override void OnInitialize()
        {
            // Debug.Log("[WaveManager] Initializing pools and warm up");

            // LMJ: Subscribe to EventChannels
            if (monsterEvents != null)
            {
                monsterEvents.AddMonsterDiedListener(HandleMonsterDied);
                monsterEvents.AddBossDiedListener(HandleBossDied);
            }

            // Initialize Monster's static Wall cache (Inspector references preferred)
            InitializeWallCache();

            // LMJ: Initialize pools asynchronously
            InitializePoolsAsync().Forget();
        }

        /// <summary>
        /// Initialize Monster's static Wall cache using Inspector references
        /// Falls back to FindWithTag if Inspector references are not set
        /// </summary>
        private void InitializeWallCache()
        {
            // Use Inspector references if available
            if (wallTarget != null && wallCollider != null && wallComponent != null)
            {
                Monster.InitializeWallCache(wallTarget, wallCollider, wallComponent);
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
                    Monster.InitializeWallCache(wallTarget, wallCollider, wallComponent);
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
                Monster.InitializeWallCache(wallTarget, wallCollider, wallComponent);
                Debug.LogWarning("[WaveManager] Wall references not set in Inspector, using FindWithTag fallback");
            }
            else
            {
                Debug.LogError("[WaveManager] Wall not found! Please assign Wall references in Inspector.");
            }
        }

        /// <summary>
        /// LMJ: Async pool initialization - separated from OnInitialize to avoid blocking
        /// </summary>
        private async UniTaskVoid InitializePoolsAsync()
        {
            // LMJ: Create pools for enemies
            await poolManager.CreatePoolAsync<Monster>(AddressableKey.Monster, defaultCapacity: 20, maxSize: 500);
            // TODO: Re-enable when BossMonster prefab is created and registered in Addressables
            // await poolManager.CreatePoolAsync<BossMonster>(AddressableKey.BossMonster, defaultCapacity: 1, maxSize: 1);

            // LMJ: Warm up pools to avoid runtime spikes
            poolManager.WarmUp<Monster>(50);
            // poolManager.WarmUp<BossMonster>(1);

            isPoolReady = true;
            // Debug.Log("[WaveManager] Pools initialized and ready");
        }

        protected override void OnReset()
        {
            // Debug.Log("[WaveManager] Resetting wave data");
            isPoolReady = false;
            enemyCount = 0;
            bossCount = 0;
            // waveId = 0;
            // rushProgressPoints.Clear();

            // JML: 다중 웨이브 관련 필드 초기화
            waveDataList.Clear();
            useCSVWaveData = false;
        }

        protected override void OnDispose()
        {
            // Debug.Log("[WaveManager] Disposing and unsubscribing events");

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
            Initialize(defaultEnemyCount, defaultBossCount);
        }

        /// <summary>
        /// LMJ: Initialize wave parameters with custom values
        /// </summary>
        public void Initialize(int totalEnemies, int bossCount = 0)
        {
            useCSVWaveData = false;
            waveDataList.Clear();

            enemyCount = totalEnemies;
            initialEnemyCount = totalEnemies;
            this.bossCount = bossCount;

            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(enemyCount);
            }
        }

        /// <summary>
        /// JML: CSV WaveData 리스트 기반 초기화 (다중 웨이브 지원)
        /// </summary>
        public void InitializeWithWaveData(List<WaveData> waves)
        {
            useCSVWaveData = true;
            waveDataList = waves ?? new List<WaveData>();

            // 전체 몬스터 수 계산 (모든 웨이브의 Monster_Count 합산)
            enemyCount = waveDataList.Sum(w => w.Monster_Count);
            initialEnemyCount = enemyCount;
            bossCount = 0; // TODO: CSV에서 보스 데이터 지원 시 수정

            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(enemyCount);
            }

            Debug.Log($"[WaveManager] Initialized with {waveDataList.Count} waves, total monsters: {enemyCount}");
        }

        /// <summary>
        /// LMJ: Start wave loop - waits for pools to be ready
        /// </summary>
        public async UniTaskVoid WaveLoop()
        {
            // LMJ: Wait for pools to be ready before spawning
            await UniTask.WaitUntil(() => isPoolReady);
            // Debug.Log("[WaveManager] Starting wave spawn loop");

            // Create new cancellation token for this wave
            spawnCts?.Cancel();
            spawnCts?.Dispose();
            spawnCts = new System.Threading.CancellationTokenSource();

            // JML: CSV 웨이브 데이터 사용 시 다중 웨이브 스폰
            if (useCSVWaveData && waveDataList.Count > 0)
            {
                SpawnMultipleWaves(spawnCts.Token).Forget();
            }
            else
            {
                SpawnEnemy(spawnCts.Token).Forget();
            }
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
                // Debug.Log("[WaveManager] All monsters defeated!");
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

            // Debug.Log("[WaveManager] Boss defeated!");

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
                    Debug.Log($"[WaveManager] Waiting {remainingWait:F1}s for Wave {waveData.Wave_ID} (Spawn_Time: {waveData.Spawn_Time}s)");
                    await UniTask.Delay((int)(remainingWait * 1000), cancellationToken: cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested || !isPoolReady)
                    break;

                Debug.Log($"[WaveManager] Starting Wave {waveData.Wave_ID}: {waveData.Monster_Count} monsters, interval {waveData.Spawn_Interval}s");

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
        /// </summary>
        private async UniTask SpawnWaveMonsters(WaveData waveData, System.Threading.CancellationToken cancellationToken)
        {
            // MonsterLevelData 가져오기
            MonsterLevelData levelData = CSVLoader.Instance.GetTable<MonsterLevelData>().GetId(waveData.Mon_Level_ID);
            if (levelData == null)
            {
                Debug.LogWarning($"[WaveManager] MonsterLevelData not found for ID: {waveData.Mon_Level_ID}");
            }

            int spawnedCount = 0;
            int targetCount = waveData.Monster_Count;

            while (spawnedCount < targetCount && isPoolReady && !cancellationToken.IsCancellationRequested)
            {
                // Check if spawner still exists (scene not destroyed)
                if (monsterSpawner == null) break;

                Vector3 spawnPos = monsterSpawner.GetRandomSpawnPosition();
                var monster = poolManager.Spawn<Monster>(spawnPos);

                if (monster != null)
                {
                    // JML: CSV 데이터로 몬스터 스탯 초기화
                    if (levelData != null)
                    {
                        monster.Initialize(levelData);
                    }

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

            Debug.Log($"[WaveManager] Wave {waveData.Wave_ID} completed: spawned {spawnedCount}/{targetCount} monsters");
        }

        private async UniTaskVoid SpawnEnemy(System.Threading.CancellationToken cancellationToken)
        {
            // LMJ: Wait for pools to be ready before spawning
            while (!isPoolReady)
            {
                await UniTask.Yield(cancellationToken);
            }

            int totalMonsters = enemyCount;
            int spawnedCount = 0;

            // LCB: Check isPoolReady in loop to stop spawning when reset
            while (spawnedCount < totalMonsters && isPoolReady)
            {
                // Check if spawner still exists (scene not destroyed)
                if (monsterSpawner == null) break;

                Vector3 spawnPos = monsterSpawner.GetRandomSpawnPosition();
                var monster = poolManager.Spawn<Monster>(spawnPos);

                // 목적지 설정 (Wall 위치로)
                if (monster != null && wallTarget != null)
                {
                    monster.SetDestination(wallTarget.position);
                }

                spawnedCount++;
                await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: cancellationToken);
            }

            // LMJ: Spawn boss after all normal enemies
            // LCB: Only spawn boss if pool is still ready
            if (bossCount > 0 && isPoolReady)
            {
                SpawnBoss();
            }
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
            poolManager.ClearAll();
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

        protected override void OnDestroy()
        {
            // Cancel all spawn operations when scene is destroyed
            spawnCts?.Cancel();
            spawnCts?.Dispose();

            base.OnDestroy();
        }
    }

}