using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [SerializeField] private WaveManger waveManager;
    public int CurrentStageId { get; private set; }
    private int maxExp = 100;
    private const int NEXT_EXP = 100;
    private int currentExp = 0;
    private int level = 0;
    private bool isStageCleared = false;

    private void OnEnable()
    {
        Monster.OnMonsterDied += AddExp;
    }
    private async UniTaskVoid Start()
    {
        await UniTask.DelayFrame(1);

        waveManager.Initialize(totalEnemies: 2000, rushIntervalPercent: 0.05f, bossCount: 1);
    
        waveManager.WaveLoop().Forget();
    }

    private void Update()
    {

    }

    private void AddExp(Monster monster)
    {
        currentExp += monster.Exp;
        Debug.Log($"Add {monster.Exp} EXP. CurrentExp: {currentExp}/{maxExp}");
        if (currentExp >= maxExp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        while (currentExp >= maxExp)
        {
            currentExp -= maxExp;
            maxExp += NEXT_EXP;
            level++;
            Debug.Log($"Level Up! New Level: {level}, CurrentExp: {currentExp}, MaxExp: {maxExp}");
            // TODO: JML: Level Up Rewards
        }
    }
}
