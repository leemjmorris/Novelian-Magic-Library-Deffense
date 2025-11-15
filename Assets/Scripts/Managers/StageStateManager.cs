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

    [Serializable]
    public class StageStateManager : BaseManager
    {
        private WaveManager waveManager;
        private StageManager stageManager;
        private WinLosePanel winLosePanel;
        private Wall wall;
        private StageEvents stageEvents;
        private WallEvents wallEvents;

        public StageState CurrentState { get; private set; }

        public StageStateManager(WaveManager wave, StageManager stage, Wall wallRef, WinLosePanel panel, StageEvents stageEvts, WallEvents wallEvts)
        {
            waveManager = wave;
            stageManager = stage;
            wall = wallRef;
            winLosePanel = panel;
            stageEvents = stageEvts;
            wallEvents = wallEvts;
        }

        protected override void OnInitialize()
        {
            Debug.Log("[StageStateManager] Initializing stage state");

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

        public void SetStageState(StageState newState)
        {
            CurrentState = newState;
            Time.timeScale = 0f;

            // LMJ: Use EventChannel instead of static event
            if (stageEvents != null)
            {
                stageEvents.RaiseStageStateChanged(newState);
            }

            Debug.Log($"[StageStateManager] Stage State Changed: {newState}");

            if (newState == StageState.Cleared)
            {
                //JML: ShowVictoryPanel 
                //TODO JML: Rank calculation logic to be implemented
                winLosePanel.ShowVictoryPanel("S",stageManager.StageName, stageManager.GetProgressTime(), waveManager.GetKillCount(), stageManager.GetReward());
            }
            else if (newState == StageState.Failed)
            {
                //JML: ShowDefeatPanel
                //TODO JML: Rank calculation logic to be implemented
                winLosePanel.ShowDefeatPanel("F", stageManager.StageName, stageManager.GetProgressTime(), waveManager.GetRemainderCount());
            }
        }

        public StageState GetCurrentState()
        {
            return CurrentState;
        }
    }
}