using System.Collections.Generic;
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
            enemyCount = totalEnemies;
            initialEnemyCount = totalEnemies;
            this.bossCount = bossCount;

            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(enemyCount);
            }
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

            SpawnEnemy(spawnCts.Token).Forget();
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