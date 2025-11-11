using Cysharp.Threading.Tasks;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Reference to the Wave Manager")] private WaveManager waveManager;
    public int CurrentStageId { get; private set; }

    #region PlayerStageLevel
    private int maxExp = 100;
    private const int NEXT_EXP = 100;
    private int currentExp = 0;
    private int level = 0;
    #endregion
    
    #region Timer
    [Header("Stage Timer")]
    [SerializeField] private float stageDuration = 600f; //JML: ex: 10 minutes
    private float RemainingTime { get; set; }
    #endregion
    
    private bool isStageCleared = false;

    private void OnEnable()
    {
        Monster.OnMonsterDied += AddExp;
    }
    private async UniTaskVoid Start()
    {
        await UniTask.DelayFrame(1);

        RemainingTime = stageDuration;

        waveManager.Initialize(totalEnemies: 2000, rushIntervalPercent: 0.05f, bossCount: 1);
        waveManager.WaveLoop().Forget();

        StageTimer().Forget();
    }

    private void Update()
    {

    }

    private async UniTaskVoid StageTimer()
    {
        float startTime = Time.time;
        float endTime = startTime + stageDuration;
        
        while (Time.time < endTime && !isStageCleared)
        {
                RemainingTime = endTime - Time.time;
            
            //JML: Display remaining time in MM:SS format
            //TODO: JML: Connect to UI - Currently logging for testing purposes
            int minutes = Mathf.FloorToInt(RemainingTime / 60f);
            int seconds = Mathf.FloorToInt(RemainingTime % 60f);
            Debug.Log($"TIME {minutes:00}:{seconds:00}");
            
            if (RemainingTime <= 0)
            {
                RemainingTime = 0;
                OnTimeUp();
                break;
            }
            
            await UniTask.Delay(1000); //JML: 1 second delay
        }
    }
    
    private void OnTimeUp()
    {
        Debug.Log("Time's Up! Stage Failed");
        // TODO: JML Game Over Logic
    }

    private void AddExp(Monster monster)
    {
        currentExp += monster.Exp;
        Debug.Log($"Add {monster.Exp} EXP. CurrentExp: {currentExp}/{maxExp}");
        if (currentExp >= maxExp)
        {
            LevelUp().Forget();
        }
    }
    private async UniTaskVoid LevelUp()
    {
        while (currentExp >= maxExp)
        {
            currentExp -= maxExp;
            maxExp += NEXT_EXP;
            level++;
            Debug.Log($"Level Up! New Level: {level}, CurrentExp: {currentExp}, MaxExp: {maxExp}");

            // TODO: JML: Level Up Rewards
            Time.timeScale = 0f; //JML: Pause the game for level up

            // TODO: JML: Wait for card selection or 5 second timeout
            // JML: ex) await WaitForCardSelectionOrTimeout();
            await UniTask.Delay(5000, ignoreTimeScale: true); //JML: Simulate wait with 5 second delay
            Time.timeScale = 1f; //JML: Resume
        }
    }
}
