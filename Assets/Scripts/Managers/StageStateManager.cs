using System;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using NovelianMagicLibraryDefense.Managers;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    public enum StageState
    {
        Playing,    // Stage in progress
        Cleared,    // Stage cleared (all enemies defeated or boss killed)
        Failed      // Stage failed (wall destroyed or time up with enemies remaining)
    }

    /// <summary>
    /// MonoBehaviour 기반 Manager (VContainer 지원)
    /// </summary>
    public class StageStateManager : BaseManager
    {
        [Header("Dependencies")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private StageManager stageManager;
        [SerializeField] private GameResultPanel gameResultPanel;
        [SerializeField] private Wall wall;
        [SerializeField] private StageEvents stageEvents;
        [SerializeField] private WallEvents wallEvents;

        public StageState CurrentState { get; private set; }

        protected override void OnInitialize()
        {
            CurrentState = StageState.Playing;

            // LMJ: Subscribe to EventChannels instead of static events
            if (stageEvents != null)
            {
                stageEvents.AddAllMonstersDefeatedListener(HandleAllMonstersDefeated);
                stageEvents.AddBossDefeatedListener(HandleBossDefeated);
                stageEvents.AddTimeUpListener(HandleTimeUp);
            }

            if (wallEvents != null)
            {
                wallEvents.AddWallDestroyedListener(HandleWallDestroyed);
            }
        }

        protected override void OnReset()
        {
            CurrentState = StageState.Playing;
            Time.timeScale = 1f;
        }

        protected override void OnDispose()
        {
            // LMJ: Unsubscribe from EventChannels
            if (stageEvents != null)
            {
                stageEvents.RemoveAllMonstersDefeatedListener(HandleAllMonstersDefeated);
                stageEvents.RemoveBossDefeatedListener(HandleBossDefeated);
                stageEvents.RemoveTimeUpListener(HandleTimeUp);
            }

            if (wallEvents != null)
            {
                wallEvents.RemoveWallDestroyedListener(HandleWallDestroyed);
            }
        }

        #region Victory Conditions

        private void HandleAllMonstersDefeated()
        {
            if (CurrentState != StageState.Playing) return;
            CheckVictoryCondition();
        }

        private void HandleBossDefeated()
        {
            if (CurrentState != StageState.Playing) return;
            CheckVictoryCondition();
        }
        private void CheckVictoryCondition()
        {
            if (!waveManager.HasRemainingEnemies() && !waveManager.HasBoss())
            {
                SetStageState(StageState.Cleared);
            }
        }
        #endregion

        #region Defeat Conditions

        private void HandleWallDestroyed()
        {
            if (CurrentState != StageState.Playing) return;
            SetStageState(StageState.Failed);
        }

        private void HandleTimeUp()
        {
            if (CurrentState != StageState.Playing) return;

            if (waveManager.HasRemainingEnemies() || waveManager.HasBoss())
            {
                SetStageState(StageState.Failed);
            }
            else
            {
                SetStageState(StageState.Cleared);
            }
        }

        #endregion

        public void SetStageState(StageState newState)
        {
            CurrentState = newState;
            Time.timeScale = 0f;

            // LMJ: Use EventChannel instead of static event
            if (stageEvents != null)
            {
                stageEvents.RaiseStageStateChanged(newState);
            }

            if (newState == StageState.Cleared)
            {
                Debug.Log("[StageStateManager] Stage Cleared!");
                if (gameResultPanel != null)
                {
                    gameResultPanel.ShowVictoryPanel("S", stageManager.StageName, stageManager.GetProgressTime(), waveManager.GetKillCount(), stageManager.GetReward());
                }
                else
                {
                    Debug.LogError("[StageStateManager] GameResultPanel is null! Inspector에서 할당해주세요.");
                }
            }
            else if (newState == StageState.Failed)
            {
                Debug.Log("[StageStateManager] Stage Failed!");
                if (gameResultPanel != null)
                {
                    gameResultPanel.ShowDefeatPanel("F", stageManager.StageName, stageManager.GetProgressTime(), waveManager.GetRemainderCount());
                }
                else
                {
                    Debug.LogError("[StageStateManager] GameResultPanel is null! Inspector에서 할당해주세요.");
                }
            }
        }

        public StageState GetCurrentState()
        {
            return CurrentState;
        }
    }
}