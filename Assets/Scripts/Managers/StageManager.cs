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

    [SerializeField] private WaveManger waveManager;
    public int CurrentStageId { get; private set; }
    private int maxExp = 100;
    private int nextExp = 100;
    private int currentExp = 0;
    private int level = 0;
    private bool isStageCleared = false;

    private async UniTaskVoid Start()
    {
        await UniTask.DelayFrame(1);

        waveManager.Initialize(totalEnemies: 2000, rushIntervalPercent: 0.05f, bossCount: 1);
    
        waveManager.WaveLoop().Forget();
    }

    private void Update()
    {

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
}
