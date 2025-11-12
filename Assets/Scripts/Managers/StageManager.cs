using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
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

        public int CurrentStageId { get; private set; }

        #region PlayerStageLevel
        private int maxExp = 100;
        private const int NEXT_EXP = 100;
        private int currentExp = 0;
        private int level = 0;
        #endregion

        #region Timer
        // LMJ: Stage duration can be loaded from CSV in the future
        private float stageDuration = 600; // Default: 10 minutes
        private float RemainingTime { get; set; }
        private bool isStageCleared = false;
        private CancellationTokenSource timerCts;

        public static event Action OnTimeUp;
        #endregion

        /// <summary>
        /// LMJ: Constructor injection for dependencies
        /// </summary>
        public StageManager(WaveManager wave, UIManager ui)
        {
            waveManager = wave;
            uiManager = ui;
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

            // LMJ: Subscribe to monster death event for exp
            Monster.OnMonsterDied += AddExp;

            // LMJ: Initialize wave manager with hardcoded values (can be loaded from CSV later)
            waveManager.Initialize(totalEnemies: 5, bossCount: 0);
            waveManager.WaveLoop().Forget();

            // LMJ: Start stage timer using UniTask (no MonoBehaviour required!)
            timerCts = new CancellationTokenSource();
            StageTimer(timerCts.Token).Forget();

            Debug.Log("[StageManager] Initialized");
        }

        protected override void OnReset()
        {
            Debug.Log("[StageManager] Resetting stage");

            // LMJ: Cancel and dispose timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events
            Monster.OnMonsterDied -= AddExp;

            // LMJ: Reset stage data
            currentExp = 0;
            level = 0;
            maxExp = 100;
            RemainingTime = stageDuration;
            isStageCleared = false;
            CurrentStageId = 0;
        }

        protected override void OnDispose()
        {
            Debug.Log("[StageManager] Disposing stage");

            // LMJ: Cancel timer
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;

            // LMJ: Unsubscribe events
            Monster.OnMonsterDied -= AddExp;
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

                    Debug.Log($"[StageManager] Remaining Time: {RemainingTime} seconds");

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
            OnTimeUp?.Invoke();
        }

        /// <summary>
        /// LMJ: Add experience when monster dies
        /// </summary>
        private void AddExp(Monster monster)
        {
            currentExp += monster.Exp;
            Debug.Log($"[StageManager] Add {monster.Exp} EXP. CurrentExp: {currentExp}/{maxExp}");

            if (currentExp >= maxExp)
            {
                LevelUp().Forget();
            }
        }

        /// <summary>
        /// LMJ: Handle level up with UniTask
        /// </summary>
        private async UniTaskVoid LevelUp()
        {
            while (currentExp >= maxExp)
            {
                currentExp -= maxExp;
                maxExp += NEXT_EXP;
                level++;
                Debug.Log($"[StageManager] Level Up! New Level: {level}, CurrentExp: {currentExp}, MaxExp: {maxExp}");

                // TODO: LMJ: Level Up Rewards (card selection UI)
                Time.timeScale = 0f; // Pause the game for level up

                // TODO: LMJ: Wait for card selection or 5 second timeout
                // ex) await WaitForCardSelectionOrTimeout();
                await UniTask.Delay(5000, ignoreTimeScale: true); // Simulate wait with 5 second delay

                Time.timeScale = 1f; // Resume game
            }
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
    }
}
