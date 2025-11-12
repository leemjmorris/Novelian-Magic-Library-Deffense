using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using TMPro;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manages enemy wave spawning and spawn logic
    /// Refactored from MonoBehaviour to BaseManager
    /// </summary>
    [System.Serializable]  // LMJ: Prevents Unity from treating this as a Component
    public class WaveManager : BaseManager
    {
        private ObjectPoolManager poolManager;
        private TextMeshProUGUI monsterCountText;
        private bool isPoolReady = false;

        public static event System.Action OnAllMonstersDefeated;
        public static event System.Action OnBossDefeated;

        #region WaveData
        // private int waveId;  // LMJ: Reserved for future use
        private int enemyCount = 50;
        private int bossCount;
        private float spawnInterval = 1f;

        // LMJ: RushSpawn feature disabled
        // private float rushSpawnInterval = 0.5f;
        // private float rushDuration = 30f;
        // private float rushInterval = 0.25f;
        // private List<float> rushProgressPoints = new List<float>();
        #endregion

        /// <summary>
        /// LMJ: Constructor injection for dependencies
        /// </summary>
        public WaveManager(ObjectPoolManager pool, TextMeshProUGUI ui)
        {
            poolManager = pool;
            monsterCountText = ui;
        }

        protected override void OnInitialize()
        {
            Debug.Log("[WaveManager] Initializing pools and warm up");

            // LMJ: Subscribe to events first
            Monster.OnMonsterDied += HandleMonsterDied;
            BossMonster.OnBossDied += HandleBossDied;

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
            await poolManager.CreatePoolAsync<BossMonster>(AddressableKey.BossMonster, defaultCapacity: 1, maxSize: 1);

            // LMJ: Warm up pools to avoid runtime spikes
            poolManager.WarmUp<Monster>(50);
            poolManager.WarmUp<BossMonster>(1);

            isPoolReady = true;
            Debug.Log("[WaveManager] Pools initialized and ready");
        }

        protected override void OnReset()
        {
            Debug.Log("[WaveManager] Resetting wave data");
            isPoolReady = false;
            enemyCount = 0;
            bossCount = 0;
            // waveId = 0;
            // rushProgressPoints.Clear();
        }

        protected override void OnDispose()
        {
            Debug.Log("[WaveManager] Disposing and unsubscribing events");

            // LMJ: Unsubscribe from events
            Monster.OnMonsterDied -= HandleMonsterDied;
            BossMonster.OnBossDied -= HandleBossDied;
        }

        /// <summary>
        /// LMJ: Initialize wave parameters
        /// </summary>
        public void Initialize(int totalEnemies, int bossCount = 0)
        {
            enemyCount = totalEnemies;
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

            if (monsterCountText != null)
            {
                monsterCountText.text = $"Monster Count: {enemyCount}";
            }

            Debug.Log($"[WaveManager] Wave initialized - Enemies: {totalEnemies}, Bosses: {bossCount}");
        }

        /// <summary>
        /// LMJ: Start wave loop - waits for pools to be ready
        /// </summary>
        public async UniTaskVoid WaveLoop()
        {
            // LMJ: Wait for pools to be ready before spawning
            await UniTask.WaitUntil(() => isPoolReady);
            Debug.Log("[WaveManager] Starting wave spawn loop");
            SpawnEnemy().Forget();
        }

        private void HandleMonsterDied(Monster monster)
        {
            // if (enemyCount <= 0) return;

            enemyCount--;

            if (monsterCountText != null)
            {
                monsterCountText.text = $"Monster Count: {enemyCount}";
            }

            if (enemyCount == 0 && bossCount == 0)
            {
                Debug.Log("[WaveManager] All monsters defeated!");
                OnAllMonstersDefeated?.Invoke();
            }
        }

        private void HandleBossDied(BossMonster boss)
        {
            bossCount--;

            if (monsterCountText != null)
            {
                monsterCountText.text = $"Stage Cleared!";
            }

            Debug.Log("[WaveManager] Boss defeated!");
            OnBossDefeated?.Invoke();
        }

        private async UniTaskVoid SpawnEnemy()
        {
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
                float randomX = Random.Range(-0.4f, 0.4f);
                Vector3 spawnPos = new Vector3(randomX, 3f, -7.5f);
                poolManager.Spawn<Monster>(spawnPos);

                spawnedCount++;
                Debug.Log($"[WaveManager] Spawned monster {spawnedCount}/{totalMonsters}");
                await UniTask.Delay((int)(spawnInterval * 1000));
            }

            // LMJ: Spawn boss after all normal enemies
            if (bossCount > 0)
            {
                SpawnBoss();
            }
        }

        private void SpawnBoss()
        {
            if (monsterCountText != null)
            {
                monsterCountText.text = "Boss!!";
            }

            Vector3 bossSpawnPos = new Vector3(0f, 2f, -7.5f);
            poolManager.Spawn<BossMonster>(bossSpawnPos);

            Debug.Log("[WaveManager] Boss spawned");
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

        public bool HasRemainingEnemies()
        {
            return enemyCount > 0;
        }

        public bool HasBoss()
        {
            return bossCount > 0;
        }
    }

}