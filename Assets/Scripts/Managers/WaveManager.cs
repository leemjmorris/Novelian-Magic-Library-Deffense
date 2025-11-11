using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI monsterCountText;
    #region WaveData
    private int waveId;
    [SerializeField] private int enemyCount = 50;
    [SerializeField] private int bossCount;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float rushSpawnInterval = 0.5f;
    [SerializeField] private float rushDuration = 30f;
    #endregion

    private float rushInterval = 0.25f;
    private List<float> rushProgressPoints = new List<float>();
    private void OnEnable()
    {
        Monster.OnMonsterDied += HandleMonsterDied;
        BossMonster.OnBossDied += HandleBossDied;
    }

    private void OnDisable()
    {
        Monster.OnMonsterDied -= HandleMonsterDied;
        BossMonster.OnBossDied -= HandleBossDied;
    }
    private void HandleMonsterDied(Monster monster)
    {
        if (enemyCount <= 0) return;
        enemyCount--;
        monsterCountText.text = $"Monster Count: {enemyCount}";
    }

    private void HandleBossDied(BossMonster boss)
    {
        bossCount--;
        monsterCountText.text = $"Stage Cleared!";
    }

    private async UniTaskVoid Start()
    {
        await ObjectPoolManager.Instance.CreatePoolAsync<Monster>(AddressableKey.Monster, defaultCapacity: 20, maxSize: 500);
        await ObjectPoolManager.Instance.CreatePoolAsync<BossMonster>(AddressableKey.BossMonster, defaultCapacity: 1, maxSize: 1);
        ObjectPoolManager.Instance.WarmUp<Monster>(50);
        ObjectPoolManager.Instance.WarmUp<BossMonster>(1);
        monsterCountText.text = $"Monster Count: {enemyCount}";
    }

    public void Initialize(int totalEnemies, float rushIntervalPercent, int bossCount = 0)
    {
        enemyCount = totalEnemies;
        rushInterval = rushIntervalPercent;
        this.bossCount = bossCount;

        rushProgressPoints.Clear();
        
        float currentProgress = rushInterval;
        while (currentProgress < 1f)
        {
            rushProgressPoints.Add(currentProgress);
            currentProgress += rushInterval;
        }
    }

    public async UniTaskVoid WaveLoop()
    {
        SpawnEnemy().Forget();
    }

    private async UniTaskVoid SpawnEnemy()
    {
        int totalMonsters = enemyCount;
        int spawnedCount = 0;
        int rushIndex = 0;

        while (enemyCount > 0)
        {
            float progress = (float)spawnedCount / totalMonsters;

            if (rushIndex < rushProgressPoints.Count &&
                progress >= rushProgressPoints[rushIndex])
            {
                await RushSpawn();
                rushIndex++;
                continue;
            }

            float randomX = Random.Range(-0.4f, 0.4f);
            Vector3 spawnPos = new Vector3(randomX, 3f, -7.5f);
            ObjectPoolManager.Instance.Spawn<Monster>(spawnPos);

            spawnedCount++;
            await UniTask.Delay((int)(spawnInterval * 1000));
        }

        if (bossCount > 0)
        {
            SpawnBoss();
        }
        else
        {
            monsterCountText.text = "Stage Cleared!";
        }
    }
    private void SpawnBoss()
    {
        monsterCountText.text = "Boss!!";
        Vector3 bossSpawnPos = new Vector3(0f, 2f, -7.5f);
        ObjectPoolManager.Instance.Spawn<BossMonster>(bossSpawnPos);
    }
    private async UniTask RushSpawn()
    {
        float elapsed = 0f;
        monsterCountText.text = $"Rush Spawn!";
        while (elapsed < rushDuration)
        {
            float randomX = Random.Range(-0.4f, 0.4f);
            Vector3 spawnPos = new Vector3(randomX, 3f, -7.5f);
            ObjectPoolManager.Instance.Spawn<Monster>(spawnPos);
            
            await UniTask.Delay((int)(rushSpawnInterval * 1000));
            elapsed += rushSpawnInterval;
        }
        
        ClearAllMonsters();
    }

    private void ClearAllMonsters()
    {
        ObjectPoolManager.Instance.DespawnAll<Monster>();
    }
}
