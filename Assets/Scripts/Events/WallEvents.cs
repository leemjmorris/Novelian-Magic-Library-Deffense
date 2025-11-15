using System;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Events
{
    /// <summary>
    /// LMJ: ScriptableObject EventChannel for Wall-related events
    /// Replaces static events to prevent memory leaks and enable Inspector visibility
    /// </summary>
    [CreateAssetMenu(fileName = "WallEvents", menuName = "Events/Wall Events")]
    public class WallEvents : ScriptableObject
    {
        private event Action<float, float> onHealthChanged;
        private event Action onWallDestroyed;

        /// <summary>
        /// Raise when wall health changes
        /// </summary>
        public void RaiseHealthChanged(float currentHP, float maxHP)
        {
            onHealthChanged?.Invoke(currentHP, maxHP);
        }

        /// <summary>
        /// Raise when wall is destroyed
        /// </summary>
        public void RaiseWallDestroyed()
        {
            onWallDestroyed?.Invoke();
        }

        /// <summary>
        /// Subscribe to health changed events
        /// </summary>
        public void AddHealthChangedListener(Action<float, float> listener)
        {
            onHealthChanged += listener;
        }

        /// <summary>
        /// Unsubscribe from health changed events
        /// </summary>
        public void RemoveHealthChangedListener(Action<float, float> listener)
        {
            onHealthChanged -= listener;
        }

        /// <summary>
        /// Subscribe to wall destroyed events
        /// </summary>
        public void AddWallDestroyedListener(Action listener)
        {
            onWallDestroyed += listener;
        }

        /// <summary>
        /// Unsubscribe from wall destroyed events
        /// </summary>
        public void RemoveWallDestroyedListener(Action listener)
        {
            onWallDestroyed -= listener;
        }

        /// <summary>
        /// Clear all listeners when ScriptableObject is disabled
        /// Prevents memory leaks
        /// </summary>
        private void OnDisable()
        {
            onHealthChanged = null;
            onWallDestroyed = null;
        }
    }
}
