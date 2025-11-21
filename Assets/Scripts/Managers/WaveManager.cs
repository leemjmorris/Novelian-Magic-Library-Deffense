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
        [SerializeField] private DestinationArea destinationArea;

        [Header("Settings")]
        [SerializeField] private float spawnInterval = 2f;

        private bool isPoolReady = false;

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

            // LMJ: Initialize pools asynchronously
            InitializePoolsAsync().Forget();
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
        }

        /// <summary>
        /// LMJ: Initialize wave parameters
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
            SpawnEnemy().Forget();
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

        private async UniTaskVoid SpawnEnemy()
        {
            // LMJ: Wait for pools to be ready before spawning
            while (!isPoolReady)
            {
                await UniTask.Yield();
            }

            int totalMonsters = enemyCount;
            int spawnedCount = 0;

            // LCB: Check isPoolReady in loop to stop spawning when reset
            while (spawnedCount < totalMonsters && isPoolReady)
            {
                Vector3 spawnPos = monsterSpawner.GetRandomSpawnPosition();
                var monster = poolManager.Spawn<Monster>(spawnPos);

                // 목적지 설정
                if (monster != null && destinationArea != null)
                {
                    monster.SetDestination(destinationArea.GetRandomDestinationPosition());
                }

                spawnedCount++;
                await UniTask.Delay((int)(spawnInterval * 1000));
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

            // 목적지 설정
            if (boss != null && destinationArea != null)
            {
                boss.SetDestination(destinationArea.GetRandomDestinationPosition());
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
    }

}