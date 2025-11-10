using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private float spawnInterval = 2.0f;
    [SerializeField] private int maxMonsterCount = 20;

    private float timer = 0f;

    // JML: Testing max one monster at a time
    private int monsterCount = 0;

    private async void Start()
    {
        await ObjectPoolManager.Instance.CreatePoolAsync<Monster>("Monster", defaultCapacity: 5, maxSize: 20);
        ObjectPoolManager.Instance.WarmUp<Monster>(20);
    }
    private void OnEnable()
    {
        Monster.OnMonsterDied += HandleMonsterDied;
    }

    private void OnDisable()
    {
        Monster.OnMonsterDied -= HandleMonsterDied;
    }

    private void HandleMonsterDied(Monster monster)
    {
        monsterCount--;
        ObjectPoolManager.Instance.Despawn(monster);
    }
    

    private void Update()
    {
        
        //CBL: every 1.5 seconds -> JML: changed to spawnInterval variable
        // if (monsterCount < maxMonsterCount)
        // {
        //     timer += Time.deltaTime;
        //     if (timer >= spawnInterval)
        //     {
        //         SpawnMonster();
        //         timer = 0f;
        //     }
        // }

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
    void SpawnMonster()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        Vector3 spawnPos = new Vector3(randomX, 2f, -7.5f);
        ObjectPoolManager.Instance.Spawn<Monster>(spawnPos);
        
    }
}
