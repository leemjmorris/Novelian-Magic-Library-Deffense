using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Manages stage progression, player leveling, and stage timer
    /// Refactored from MonoBehaviour to BaseManager
    /// Timer can be configured via CSV data in the future
    /// </summary>
    [System.Serializable]  // LMJ: Prevents Unity from treating this as a Component
    public class StageManager : BaseManager
    {

        private WaveManager waveManager;
        private UIManager uiManager;
        private MonsterEvents monsterEvents;
        private StageEvents stageEvents;
        private LevelUpCardUI levelUpCardUI; // LCB: Cache LevelUpCardUI reference to avoid FindWithTag on inactive objects
        private CardSelectionManager cardSelectionManager; // LMJ: Direct reference to CardSelectionManager

        public int CurrentStageId { get; private set; }
        public string StageName { get; private set; }

        #region PlayerStageLevel
        private int maxExp = 100;
        private const int NEXT_EXP = 100;
        private int currentExp = 0;
        private int level = 0;

        // LCB: Debug - Press 'L' key to instantly level up (add 100 exp)
        #endregion

        #region Timer
        // LMJ: Stage duration can be loaded from CSV in the future
        private float stageDuration = 600; // Default: 10 minutes
        private float RemainingTime { get; set; }
        private bool isStageCleared = false;
        private CancellationTokenSource timerCts;
        #endregion

        /// <summary>
        /// LMJ: Constructor injection for dependencies
        /// </summary>
        public StageManager(WaveManager wave, UIManager ui, MonsterEvents monsterEvts, StageEvents stageEvts, CardSelectionManager cardSelection = null)
        {
            waveManager = wave;
            cardSelectionManager = cardSelection;
            uiManager = ui;
            monsterEvents = monsterEvts;
            stageEvents = stageEvts;
        }

        /// <summary>
        /// LMJ: Set stage duration (useful for CSV data loading in the future)
        /// </summary>
        public void SetStageDuration(float duration)
        {
            stageDuration = duration;
            Debug.Log($"[StageManager] Stage duration set to {duration} seconds");
        }

        protected override void OnInitialize()
        {
            Debug.Log("[StageManager] Initializing stage");

            StageName = $"Stage 1-1"; //TODO JML: CSV Loaded

            // LMJ: Subscribe to monster death event for exp via EventChannel
            if (monsterEvents != null)
            {
                monsterEvents.AddMonsterDiedListener(AddExp);
            }

            // LCB: Find and cache LevelUpCardUI using FindWithTag (faster than FindFirstObjectByType)
            // LCB: Now works because LevelUpPanel stays active with CanvasGroup controlling visibility
            GameObject cardUIObj = GameObject.FindWithTag("CardUI");
            levelUpCardUI = cardUIObj != null ? cardUIObj.GetComponent<LevelUpCardUI>() : null;
            if (levelUpCardUI == null)
            {
                Debug.LogError("[StageManager] LevelUpCardUI not found! Make sure 'CardUI' tag is assigned.");
            }
            else
            {
                Debug.Log("[StageManager] LevelUpCardUI found and cached via FindWithTag");
            }

            // LMJ: Initialize wave manager with hardcoded values (can be loaded from CSV later)
            waveManager.Initialize(totalEnemies: 20, bossCount: 0);
            waveManager.WaveLoop().Forget();

            // LMJ: Start stage timer using UniTask (no MonoBehaviour required!)
            timerCts = new CancellationTokenSource();
            StageTimer(timerCts.Token).Forget();

            // LCB: Show start card selection before starting the game
            ShowStartCardSelection().Forget();

            Debug.Log("[StageManager] Initialized");
        }

        /// <summary>
        /// LCB: Show start card selection and pause the game
        /// </summary>
        private async UniTaskVoid ShowStartCardSelection()
        {
            // 1. Pause the game
            Time.timeScale = 0f;//LCB: pause
            Debug.Log("[StageManager] Game paused for start card selection");

            // 2. Show start card selection UI using direct reference
            if (cardSelectionManager != null)
            {
                await cardSelectionManager.ShowStartCards();
                Debug.Log("[StageManager] Start card selection completed");
            }
            else
            {
                Debug.LogWarning("[StageManager] CardSelectionManager not found! Skipping start card selection.");
            }

            // 3. Resume the game
            Time.timeScale = 1f;
            Debug.Log("[StageManager] Game resumed after start card selection");
        }

        protected override void OnReset()
        {
            Debug.Log("[StageManager] Resetting stage");

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
            maxExp = 100;
            RemainingTime = stageDuration;
            isStageCleared = false;
            CurrentStageId = 0;
            Time.timeScale = 1f;
        }

        protected override void OnDispose()
        {
            Debug.Log("[StageManager] Disposing stage");

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
            RemainingTime = stageDuration;
            float startTime = Time.time;
            float endTime = startTime + stageDuration;

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
                Debug.Log("[StageManager] Timer cancelled");
            }
        }

        public void HandleTimeUp()
        {
            Debug.Log("[StageManager] Time's Up! Stage Failed");
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
            currentExp += monster.Exp;
            Debug.Log($"[StageManager] Exp +{monster.Exp} -> {currentExp}/{maxExp}"); // LCB: Debug exp gain
            uiManager.UpdateExperience(currentExp, maxExp);
            if (currentExp >= maxExp)
            {
                Debug.Log($"[StageManager] Level up triggered! currentExp={currentExp}, maxExp={maxExp}"); // LCB: Debug level up trigger
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
            Debug.Log($"타임 스케일 {previousTimeScale}"); // LCB: Debug previous time scale
            while (currentExp >= maxExp)
            {
                currentExp -= maxExp;
                maxExp += NEXT_EXP;
                level++;
                uiManager.UpdateExperience(currentExp, maxExp);

                // LCB: Call level-up card system

                Time.timeScale = 0f; // Pause the game for level up

                // LCB: Display card selection UI using cached reference (works even if object is inactive)
                Debug.Log($"[StageManager] Level up to {level}, using cached LevelUpCardUI...");
                if (levelUpCardUI != null)
                {
                    Debug.Log($"[StageManager] Calling ShowCards(level={level})");
                    await levelUpCardUI.ShowCards(level); // level 1 means first level-up
                }
                else
                {
                    Debug.LogError("[StageManager] LevelUpCardUI is null! Was it destroyed or not found on Initialize?");
                }
            }
            Time.timeScale = previousTimeScale; // Resume game
            Debug.Log($"타임 스케일{Time.timeScale}"); // LCB: Debug resumed time scale
        }

        /// <summary>
        /// LMJ: Get current stage progress (useful for UI)
        /// </summary>
        public float GetStageProgress()
        {
            return 1f - (RemainingTime / stageDuration);
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
            return stageDuration - RemainingTime;
        }

        public int GetReward()
        {
            // JML: Example reward calculation based on level and time
            int baseReward = 100;
            int timeBonus = (int)(stageDuration - RemainingTime) / 10;
            return baseReward + (level * 50) + timeBonus;
        }
    }
}
