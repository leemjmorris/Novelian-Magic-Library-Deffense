using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    // TODO: 몬스터 소환 관리 - WaveManager로 분리 고려
    // TODO: 스테이지 진행 관리
    // TODO: 보스 전투 관리
    // TODO: 스테이지 클리어 조건 관리
    // TODO: 플레이어 레벨업 관리
    // TODO: 아이템 드랍 관리
    // TODO: 캐릭터 배치
    // TODO: BGM 전환 관리
    // TODO: 스테이지별 특수 이벤트 관리

    [SerializeField] TextMeshProUGUI monsterCountText;
    public int MonsterCount { get; private set; } = 50;
    public int CurrentStageId { get; private set; }
    [SerializeField] private float spawnInterval = 2.0f;
    private float timer = 0f;
    private int maxExp = 100;
    private int nextExp = 100;
    private int currentExp = 0;
    private int level = 0;
    private int bossMonsterCount = 1;
    private bool isStageCleared = false;

    private void OnEnable()
    {
        MonsterBase.OnMonsterDied += HandleMonsterDied;
    }

    private void OnDisable()
    {
        MonsterBase.OnMonsterDied -= HandleMonsterDied;
    }
    private void HandleMonsterDied(MonsterBase monster)
    {
        MonsterCount--;
        monsterCountText.text = $"Monster Count: {MonsterCount}";
        Debug.Log($"Current Monster Count: {MonsterCount}");
    }
    private async UniTaskVoid Start()
    {
        await ObjectPoolManager.Instance.CreatePoolAsync<Monster>(AddressableKey.Monster, defaultCapacity: 10, maxSize: 100);
        await ObjectPoolManager.Instance.CreatePoolAsync<BossMonster>(AddressableKey.BossMonster, defaultCapacity: 1, maxSize: 5);
        ObjectPoolManager.Instance.WarmUp<Monster>(50);
        monsterCountText.text = $"Monster Count: {MonsterCount}";
    }

    private void Update()
    {
        if (MonsterCount > 0)
        {
            timer += Time.deltaTime;

            if (timer >= spawnInterval)
            {
                SpawnMonster();
                timer = 0f;
            }
        }
        else if (!isStageCleared)
        {
            Debug.Log("Stage Cleared!");
            monsterCountText.text = "Stage Cleared!";
            ObjectPoolManager.Instance.ClearAll();
            isStageCleared = true;
            Time.timeScale = 0f;
        }
    }

    private void LevelUp()
    {
        while (currentExp > maxExp)
        {
            currentExp -= maxExp;
            maxExp += nextExp;
            level++;
            Debug.Log($"Level Up! New Level: {level}, CurrentExp: {currentExp}, MaxExp: {maxExp}");
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
