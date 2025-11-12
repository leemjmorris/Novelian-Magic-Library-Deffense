using System;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    public enum StageState
    {
        Playing,    // Stage in progress
        Cleared,    // Stage cleared (all enemies defeated or boss killed)
        Failed      // Stage failed (wall destroyed or time up with enemies remaining)
    }

    [Serializable]
    public class StageStateManager : BaseManager
    {
        private WaveManager waveManager;
        private StageManager stageManager;
        private Wall wall;

        public StageState CurrentState { get; private set; }

        public static event Action<StageState> OnStageStateChanged;

        public StageStateManager(WaveManager wave, StageManager stage, Wall wallRef)
        {
            waveManager = wave;
            stageManager = stage;
            wall = wallRef;
        }

        protected override void OnInitialize()
        {
            Debug.Log("[StageStateManager] Initializing stage state");

            CurrentState = StageState.Playing;

            WaveManager.OnAllMonstersDefeated += HandleAllMonstersDefeated;
            WaveManager.OnBossDefeated += HandleBossDefeated;

            Wall.OnWallDestroyed += HandleWallDestroyed;
            StageManager.OnTimeUp += HandleTimeUp;

            Debug.Log("[StageStateManager] Subscribed to all stage events");
        }

        protected override void OnReset()
        {
            Debug.Log("[StageStateManager] Resetting stage state");
            CurrentState = StageState.Playing;
            Time.timeScale = 1f;
        }

        protected override void OnDispose()
        {
            Debug.Log("[StageStateManager] Disposing and unsubscribing events");

            WaveManager.OnAllMonstersDefeated -= HandleAllMonstersDefeated;
            WaveManager.OnBossDefeated -= HandleBossDefeated;
            Wall.OnWallDestroyed -= HandleWallDestroyed;
            StageManager.OnTimeUp -= HandleTimeUp;
        }

        #region Victory Conditions

        private void HandleAllMonstersDefeated()
        {
            if (CurrentState != StageState.Playing) return;

            Debug.Log("All monsters defeated!");
            CheckVictoryCondition();
        }

        private void HandleBossDefeated()
        {
            if (CurrentState != StageState.Playing) return;

            Debug.Log("Boss defeated!");
            CheckVictoryCondition();
        }
        private void CheckVictoryCondition()
        {
            if (!waveManager.HasRemainingEnemies() && !waveManager.HasBoss())
            {
                Debug.Log("[StageStateManager] All enemies defeated - Stage Cleared!");
                SetStageState(StageState.Cleared);
            }
            else
            {
                Debug.Log($"[StageStateManager] Not yet cleared - Enemies: {waveManager.HasRemainingEnemies()}, Boss: {waveManager.HasBoss()}");
            }
        }
        #endregion

        #region Defeat Conditions

        private void HandleWallDestroyed()
        {
            if (CurrentState != StageState.Playing) return;

            Debug.Log("Wall destroyed!");
            SetStageState(StageState.Failed);
        }

        private void HandleTimeUp()
        {
            if (CurrentState != StageState.Playing) return;

            if (waveManager.HasRemainingEnemies() || waveManager.HasBoss())
            {
                Debug.Log("TimeOut Game Over!");
                SetStageState(StageState.Failed);
            }
            else
            {
                Debug.Log("Stage Cleared!");
                SetStageState(StageState.Cleared);
            }
        }

        #endregion

        private void SetStageState(StageState newState)
        {
            CurrentState = newState;
            Time.timeScale = 0f;

            OnStageStateChanged?.Invoke(newState);

            Debug.Log($"[StageStateManager] Stage State Changed: {newState}");
        }

        public StageState GetCurrentState()
        {
            return CurrentState;
        }
    }
}