using UnityEngine;
using UnityEngine.Events;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private float spawnInterval = 2.0f;

    
    private float timer = 0f;

    // JML: Testing max one monster at a time
    private int monsterCount = 0;

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
    }
    private int maxMonsterCount = 1;

    private void Update()
    {
        
        //CBL: every 1.5 seconds -> JML: changed to spawnInterval variable
        if (monsterCount < maxMonsterCount)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                spawnMonster();
                timer = 0f;
            }
        }
    }
    void spawnMonster()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        Vector2 spawnPos = new Vector2(randomX, 2f);
        Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        monsterCount++;
    }
}
