using System;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Events
{
    /// <summary>
    /// LMJ: ScriptableObject EventChannel for Monster-related events
    /// Replaces static events to prevent memory leaks and enable Inspector visibility
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterEvents", menuName = "Events/Monster Events")]
    public class MonsterEvents : ScriptableObject
    {
        private event Action<Monster> onMonsterDied;
        private event Action<BossMonster> onBossDied;

        /// <summary>
        /// Raise when a regular monster dies
        /// </summary>
        public void RaiseMonsterDied(Monster monster)
        {
            onMonsterDied?.Invoke(monster);
        }

        /// <summary>
        /// Raise when a boss monster dies
        /// </summary>
        public void RaiseBossDied(BossMonster boss)
        {
            onBossDied?.Invoke(boss);
        }

        /// <summary>
        /// Subscribe to monster death events
        /// </summary>
        public void AddMonsterDiedListener(Action<Monster> listener)
        {
            onMonsterDied += listener;
        }

        /// <summary>
        /// Unsubscribe from monster death events
        /// </summary>
        public void RemoveMonsterDiedListener(Action<Monster> listener)
        {
            onMonsterDied -= listener;
        }

        /// <summary>
        /// Subscribe to boss death events
        /// </summary>
        public void AddBossDiedListener(Action<BossMonster> listener)
        {
            onBossDied += listener;
        }

        /// <summary>
        /// Unsubscribe from boss death events
        /// </summary>
        public void RemoveBossDiedListener(Action<BossMonster> listener)
        {
            onBossDied -= listener;
        }

        /// <summary>
        /// Clear all listeners when ScriptableObject is disabled
        /// Prevents memory leaks
        /// </summary>
        private void OnDisable()
        {
            onMonsterDied = null;
            onBossDied = null;
        }
    }
}
