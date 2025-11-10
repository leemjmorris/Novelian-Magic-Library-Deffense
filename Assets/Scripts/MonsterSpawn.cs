using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private float spawnInterval = 2.0f;
    [SerializeField] private int maxMonsterCount = 20;
    private float timer = 0f;

    private async void Start()
    {
        await ObjectPoolManager.Instance.CreatePoolAsync<Monster>(AddressableKey.Monster, defaultCapacity: 5, maxSize: 20);
        await ObjectPoolManager.Instance.CreatePoolAsync<BossMonster>(AddressableKey.BossMonster, defaultCapacity: 1, maxSize: 1);
        ObjectPoolManager.Instance.WarmUp<Monster>(20);
        ObjectPoolManager.Instance.WarmUp<BossMonster>(1);
    }
    private void OnEnable()
    {
        Monster.OnMonsterDied += HandleMonsterDied;
        BossMonster.OnMonsterDied += HandleMonsterDied;
    }

    private void OnDisable()
    {
        Monster.OnMonsterDied -= HandleMonsterDied;
        BossMonster.OnMonsterDied -= HandleMonsterDied;
    }

    private void HandleMonsterDied(Monster monster)
    {
        ObjectPoolManager.Instance.Despawn(monster);
    }
    private void HandleMonsterDied(BossMonster bossMonster)
    {
        ObjectPoolManager.Instance.Despawn(bossMonster);
    }


    private void Update()
    {
        if (ObjectPoolManager.Instance.GetActiveCount<Monster>() < maxMonsterCount)
        {
            timer += Time.deltaTime;

            if (timer >= spawnInterval)
            {
                SpawnMonster();
                timer = 0f;
            }
        }
    }

    private void SpawnMonster()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        Vector3 spawnPos = new Vector3(randomX, 2f, -7.5f);
        ObjectPoolManager.Instance.Spawn<Monster>(spawnPos);

    }
    private void SpawnBossMonster()
    {
        Vector3 spawnPos = new Vector3(0, 2f, -7.5f);
        ObjectPoolManager.Instance.Spawn<BossMonster>(spawnPos);
    }
}
