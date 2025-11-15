using System;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Events
{
    /// <summary>
    /// LMJ: ScriptableObject EventChannel for Stage and Wave-related events
    /// Replaces static events to prevent memory leaks and enable Inspector visibility
    /// </summary>
    [CreateAssetMenu(fileName = "StageEvents", menuName = "Events/Stage Events")]
    public class StageEvents : ScriptableObject
    {
        private event Action onTimeUp;
        private event Action onAllMonstersDefeated;
        private event Action onBossDefeated;
        private event Action<StageState> onStageStateChanged;

        /// <summary>
        /// Raise when stage time is up
        /// </summary>
        public void RaiseTimeUp()
        {
            onTimeUp?.Invoke();
        }

        /// <summary>
        /// Raise when all monsters in wave are defeated
        /// </summary>
        public void RaiseAllMonstersDefeated()
        {
            onAllMonstersDefeated?.Invoke();
        }

        /// <summary>
        /// Raise when boss is defeated
        /// </summary>
        public void RaiseBossDefeated()
        {
            onBossDefeated?.Invoke();
        }

        /// <summary>
        /// Raise when stage state changes
        /// </summary>
        public void RaiseStageStateChanged(StageState newState)
        {
            onStageStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Subscribe to time up events
        /// </summary>
        public void AddTimeUpListener(Action listener)
        {
            onTimeUp += listener;
        }

        /// <summary>
        /// Unsubscribe from time up events
        /// </summary>
        public void RemoveTimeUpListener(Action listener)
        {
            onTimeUp -= listener;
        }

        /// <summary>
        /// Subscribe to all monsters defeated events
        /// </summary>
        public void AddAllMonstersDefeatedListener(Action listener)
        {
            onAllMonstersDefeated += listener;
        }

        /// <summary>
        /// Unsubscribe from all monsters defeated events
        /// </summary>
        public void RemoveAllMonstersDefeatedListener(Action listener)
        {
            onAllMonstersDefeated -= listener;
        }

        /// <summary>
        /// Subscribe to boss defeated events
        /// </summary>
        public void AddBossDefeatedListener(Action listener)
        {
            onBossDefeated += listener;
        }

        /// <summary>
        /// Unsubscribe from boss defeated events
        /// </summary>
        public void RemoveBossDefeatedListener(Action listener)
        {
            onBossDefeated -= listener;
        }

        /// <summary>
        /// Subscribe to stage state changed events
        /// </summary>
        public void AddStageStateChangedListener(Action<StageState> listener)
        {
            onStageStateChanged += listener;
        }

        /// <summary>
        /// Unsubscribe from stage state changed events
        /// </summary>
        public void RemoveStageStateChangedListener(Action<StageState> listener)
        {
            onStageStateChanged -= listener;
        }

        /// <summary>
        /// Clear all listeners when ScriptableObject is disabled
        /// Prevents memory leaks
        /// </summary>
        private void OnDisable()
        {
            onTimeUp = null;
            onAllMonstersDefeated = null;
            onBossDefeated = null;
            onStageStateChanged = null;
        }
    }
}
