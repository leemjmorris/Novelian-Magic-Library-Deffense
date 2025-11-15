using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Settings;
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

        [Header("Settings")]
        [SerializeField] private WaveSettings waveSettings;

        private bool isPoolReady = false;

        #region WaveData
        // private int waveId;  // LMJ: Reserved for future use
        private int enemyCount;
        private int initialEnemyCount;
        private int bossCount;

        // LMJ: RushSpawn feature disabled
        // private float rushSpawnInterval = 0.5f;
        // private float rushDuration = 30f;
        // private float rushInterval = 0.25f;
        // private List<float> rushProgressPoints = new List<float>();
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
            // rushInterval = rushIntervalPercent;  // LMJ: RushSpawn disabled
            this.bossCount = bossCount;

            // LMJ: RushSpawn feature disabled
            /*
            rushProgressPoints.Clear();

            float currentProgress = rushInterval;
            while (currentProgress < 1f)
            {
                rushProgressPoints.Add(currentProgress);
                currentProgress += rushInterval;
            }
            */

            if (uiManager != null)
            {
                uiManager.UpdateMonsterCount(enemyCount);
            }

            // Debug.Log($"[WaveManager] Wave initialized - Enemies: {totalEnemies}, Bosses: {bossCount}");
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

            while (spawnedCount < totalMonsters)
            {
                // LMJ: RushSpawn feature disabled
                /*
                float progress = (float)spawnedCount / totalMonsters;

                if (rushIndex < rushProgressPoints.Count &&
                    progress >= rushProgressPoints[rushIndex])
                {
                    await RushSpawn();
                    rushIndex++;
                    continue;
                }
                */

                // LMJ: Normal spawn
                Vector3 spawnPos = waveSettings.GetRandomSpawnPosition();
                poolManager.Spawn<Monster>(spawnPos);

                spawnedCount++;
                await UniTask.Delay((int)(waveSettings.spawnInterval * 1000));
            }

            // LMJ: Spawn boss after all normal enemies
            if (bossCount > 0)
            {
                SpawnBoss();
            }
        }

        private void SpawnBoss()
        {
            Vector3 bossSpawnPos = waveSettings.GetBossSpawnPosition();
            poolManager.Spawn<BossMonster>(bossSpawnPos);

            // Debug.Log("[WaveManager] Boss spawned");
        }

        // LMJ: RushSpawn feature disabled - kept as reference
        /*
        private async UniTask RushSpawn()
        {
            float elapsed = 0f;

            if (monsterCountText != null)
            {
                monsterCountText.text = $"Rush Spawn!";
            }

            while (elapsed < rushDuration)
            {
                float randomX = Random.Range(-0.4f, 0.4f);
                Vector3 spawnPos = new Vector3(randomX, 3f, -7.5f);
                poolManager.Spawn<Monster>(spawnPos);

                await UniTask.Delay((int)(rushSpawnInterval * 1000));
                elapsed += rushSpawnInterval;
            }

            ClearAllMonsters();
        }

        private void ClearAllMonsters()
        {
            poolManager.DespawnAll<Monster>();
        }
        */
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