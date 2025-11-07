using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    public float spawnInterval = 0.7f;

    private float timer = 0f;

    private void Update()
    {
        timer += Time.deltaTime;
        //CBL: every 1.5 seconds
        if (timer>=spawnInterval)
        {
            timer = 0f;
            spawnMoster();
            
        }
    }
    void spawnMoster()
    {
        float randomX = Random.Range(-0.4f, 0.4f);
        Vector2 spawnPos = new Vector2(randomX, 2f);
        Instantiate(monsterPrefab, spawnPos, Quaternion.identity);

    }
}
