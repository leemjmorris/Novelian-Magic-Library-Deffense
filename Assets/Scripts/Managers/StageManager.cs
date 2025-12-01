using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Settings;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manages stage progression, player leveling, and stage timer
    /// MonoBehaviour 기반 Manager
    /// </summary>
    public class StageManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private MonsterEvents monsterEvents;
        [SerializeField] private StageEvents stageEvents;
        [SerializeField] private Wall wallComponent; // JML: Wall 참조 (Barrier HP 설정용)

        [Header("Settings")]
        [SerializeField] private StageSettings stageSettings;

        public int CurrentStageId { get; private set; }
        public string StageName { get; private set; }

        #region PlayerStageLevel
        private int currentExp = 0;
        private int level = 0;

        // LCB: Debug - Press 'L' key to instantly level up (add 100 exp)
        #endregion

        #region Timer
        private float RemainingTime { get; set; }
        private bool isStageCleared = false;
        private CancellationTokenSource timerCts;
        #endregion

        /// <summary>
        /// LMJ: Set stage duration (useful for CSV data loading in the future)
        /// </summary>
        public void SetStageDuration(float duration)
        {
            if (stageSettings != null)
            {
                stageSettings.stageDuration = duration;
            }
            // Debug.Log($"[StageManager] Stage duration set to {duration} seconds");
        }

        protected override void OnInitialize()
        {
            // Debug.Log("[StageManager] Initializing stage");

            // JML: CSV 데이터 기반 스테이지 초기화
            InitializeFromCSV();

            // LMJ: Subscribe to monster death event for exp via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.AddMonsterDiedListener(AddExp);
            }

            // LMJ: Start stage timer using UniTask (no MonoBehaviour required!)
            timerCts = new CancellationTokenSource();
            StageTimer(timerCts.Token).Forget();

            // LCB: Show start card selection before starting the game
            ShowStartCardSelection().Forget();

            // Debug.Log("[StageManager] Initialized");
        }

        /// <summary>
        /// JML: CSV 데이터 기반으로 스테이지 초기화
        /// SelectedStage.Data에서 Time_Limit, Wave ID, Barrier_HP 등을 가져옴
        /// </summary>
        private void InitializeFromCSV()
        {
            if (!SelectedStage.HasSelection)
            {
                Debug.LogWarning("[StageManager] No stage selected, using default stage (010101)");
                StageName = "Stage 1-1";

                // JML: 기본 스테이지 데이터 로드 (스테이지 선택 없이 GameScene 직접 실행 시)
                StageData defaultStage = CSVLoader.Instance.GetTable<StageData>().GetId(010101);
                if (defaultStage != null)
                {
                    SelectedStage.Data = defaultStage;
                }
                else
                {
                    Debug.LogError("[StageManager] Default stage (010101) not found in CSV!");
                    return;
                }
            }

            StageData stageData = SelectedStage.Data;
            CurrentStageId = stageData.Stage_ID;
            StageName = $"Stage {stageData.Chapter_Number}";

            // 1. Time Limit 설정
            SetStageDuration(stageData.Time_Limit);
            Debug.Log($"[StageManager] Time Limit set to {stageData.Time_Limit} seconds");

            // 2. Wall(Barrier) HP 설정
            if (wallComponent != null)
            {
                wallComponent.SetMaxHealth(stageData.Barrier_HP);
                Debug.Log($"[StageManager] Wall HP set to {stageData.Barrier_HP}");
            }
            else
            {
                Debug.LogError("[StageManager] wallComponent is null! Cannot set Barrier HP.");
            }

            // 3. Wave 데이터 수집 및 WaveManager 초기화
            List<WaveData> waveDataList = CollectWaveData(stageData);
            if (waveDataList.Count > 0)
            {
                waveManager.InitializeWithWaveData(waveDataList);
                waveManager.WaveLoop().Forget();
                Debug.Log($"[StageManager] Initialized with {waveDataList.Count} waves from CSV");
            }
            else
            {
                Debug.LogError("[StageManager] No wave data found! Check Wave IDs in StageTable.csv");
            }
        }

        /// <summary>
        /// JML: StageData에서 Wave ID들을 수집하여 WaveData 리스트로 반환
        /// Wave_1_ID ~ Wave_4_ID 중 0이 아닌 것만 가져옴
        /// </summary>
        private List<WaveData> CollectWaveData(StageData stageData)
        {
            List<WaveData> waveDataList = new List<WaveData>();
            int[] waveIds = new int[]
            {
                stageData.Wave_1_ID,
                stageData.Wave_2_ID,
                stageData.Wave_3_ID,
                stageData.Wave_4_ID
            };

            foreach (int waveId in waveIds)
            {
                if (waveId == 0) continue;

                WaveData waveData = CSVLoader.Instance.GetTable<WaveData>().GetId(waveId);
                if (waveData != null)
                {
                    waveDataList.Add(waveData);
                    Debug.Log($"[StageManager] Added Wave {waveId}: Monster_ID={waveData.Monster_ID}, " +
                              $"Count={waveData.Monster_Count}, Spawn_Time={waveData.Spawn_Time}s");
                }
                else
                {
                    Debug.LogWarning($"[StageManager] Wave data not found for ID: {waveId}");
                }
            }

            return waveDataList;
        }

        /// <summary>
        /// LMJ: Show start card selection (2 character cards only)
        /// Game does NOT pause for start selection
        /// </summary>
        private async UniTaskVoid ShowStartCardSelection()
        {
            Debug.Log("[StageManager] Opening start card selection (2 character cards)");

            if (uiManager != null)
            {
                uiManager.OpenCardSelectForGameStart(); // Opens 2 character cards, no pause

                // Wait until card panel is closed
                while (uiManager != null && uiManager.IsCardSelectOpen())
                {
                    await UniTask.Yield();
                }

                Debug.Log("[StageManager] Start card selection completed");
            }
            else
            {
                Debug.LogError("[StageManager] UIManager is null! Cannot show start card selection.");
            }
        }

        protected override void OnReset()
        {
            // Debug.Log("[StageManager] Resetting stage");

            // LMJ: Cancel and dispose timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.RemoveMonsterDiedListener(AddExp);
            }

            // LMJ: Reset stage data
            currentExp = 0;
            level = 0;
            RemainingTime = stageSettings != null ? stageSettings.stageDuration : 600f;
            isStageCleared = false;
            CurrentStageId = 0;
            Time.timeScale = 1f;
        }

        protected override void OnDispose()
        {
            // Debug.Log("[StageManager] Disposing stage");

            // LMJ: Cancel timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.RemoveMonsterDiedListener(AddExp);
            }

            // JML: Ensure time scale reset
            Time.timeScale = 1f;
        }

        /// <summary>
        /// LMJ: Stage timer using UniTask (works without MonoBehaviour!)
        /// Uses CancellationToken for proper cleanup
        /// </summary>
        private async UniTaskVoid StageTimer(CancellationToken ct)
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            RemainingTime = duration;
            float startTime = Time.time;
            float endTime = startTime + duration;

            try
            {
                while (Time.time < endTime && !isStageCleared)
                {
                    RemainingTime = endTime - Time.time;

                    // LMJ: Update UI timer display
                    if (uiManager != null)
                    {
                        uiManager.UpdateWaveTimer(RemainingTime);
                    }

                    if ((int)RemainingTime <= 0)
                    {
                        RemainingTime = 0;
                        HandleTimeUp();
                        break;
                    }

                    // LMJ: Wait 1 second (1000ms)
                    await UniTask.Delay(1000, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Debug.Log("[StageManager] Timer cancelled");
            }
        }

        public void HandleTimeUp()
        {
            // Debug.Log("[StageManager] Time's Up! Stage Failed");
            waveManager.WaveClear();

            // LMJ: Use EventChannel instead of static event
            if (stageEvents != null)
            {
                stageEvents.RaiseTimeUp();
            }
        }

        /// <summary>
        /// LMJ: Add experience when monster dies
        /// </summary>
        private void AddExp(Monster monster)
        {
            int maxExp = stageSettings != null ? stageSettings.expPerLevel : 100;
            currentExp += monster.Exp;
            // Debug.Log($"[StageManager] Exp +{monster.Exp} -> {currentExp}/{maxExp}"); // LCB: Debug exp gain
            uiManager.UpdateExperience(currentExp, maxExp);
            if (currentExp >= maxExp)
            {
                // Debug.Log($"[StageManager] Level up triggered! currentExp={currentExp}, maxExp={maxExp}"); // LCB: Debug level up trigger
                LevelUp().Forget();
            }
        }

        /// <summary>
        /// LMJ: Handle level up with UniTask
        /// LCB: Integrate level-up card system (Issue 139)
        /// </summary>
        private async UniTaskVoid LevelUp()
        {
            var previousTimeScale = Time.timeScale;
            // Debug.Log($"타임 스케일 {previousTimeScale}"); // LCB: Debug previous time scale
            int maxExp = stageSettings != null ? stageSettings.expPerLevel : 100;
            while (currentExp >= maxExp)
            {
                currentExp -= maxExp;
                level++;
                uiManager.UpdateExperience(currentExp, maxExp);

                // LMJ: Open card selection for level up
                Debug.Log($"[StageManager] Level up to {level}!");

                if (uiManager != null)
                {
                    uiManager.OpenCardSelectForLevelUp(); // Opens with 2 random cards (character + ability mix)

                    // Wait until card panel is closed
                    while (uiManager != null && GameManager.Instance?.UI?.IsCardSelectOpen() == true)
                    {
                        await UniTask.Yield();
                    }
                }
                else
                {
                    Debug.LogError("[StageManager] UIManager is null!");
                }
            }
            Time.timeScale = previousTimeScale; // Resume game
            // Debug.Log($"타임 스케일{Time.timeScale}"); // LCB: Debug resumed time scale
        }

        /// <summary>
        /// LMJ: Get current stage progress (useful for UI)
        /// </summary>
        public float GetStageProgress()
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            return 1f - (RemainingTime / duration);
        }

        /// <summary>
        /// LMJ: Get remaining time (useful for UI)
        /// </summary>
        public float GetRemainingTime()
        {
            return RemainingTime;
        }

        /// <summary>
        /// LMJ: Get current level (useful for UI)
        /// </summary>
        public int GetCurrentLevel()
        {
            return level;
        }

        /// <summary>
        /// LMJ: Get current exp progress (useful for UI)
        /// </summary>
        public float GetExpProgress()
        {
            int maxExp = stageSettings != null ? stageSettings.expPerLevel : 100;
            return (float)currentExp / maxExp;
        }

        /// <summary>
        /// JML: Get remaining time
        /// </summary>
        public float GetRemainderTime()
        {
            return RemainingTime;
        }

        /// <summary>
        /// JML: Get Progress time
        /// </summary>
        public float GetProgressTime()
        {
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            return duration - RemainingTime;
        }

        public int GetReward()
        {
            // JML: Example reward calculation based on level and time
            float duration = stageSettings != null ? stageSettings.stageDuration : 600f;
            int baseReward = 100;
            int timeBonus = (int)(duration - RemainingTime) / 10;
            return baseReward + (level * 50) + timeBonus;
        }
    }
}
