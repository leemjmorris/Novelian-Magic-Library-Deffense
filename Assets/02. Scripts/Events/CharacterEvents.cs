using System;
using UnityEngine;
using Novelian.Combat;

namespace NovelianMagicLibraryDefense.Events
{
    /// <summary>
    /// LMJ: ScriptableObject EventChannel for Character-related events
    /// Replaces static events to prevent memory leaks and enable Inspector visibility
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterEvents", menuName = "Events/Character Events")]
    public class CharacterEvents : ScriptableObject
    {
        private event Action<Character> onCharacterDied;
        private event Action<Character, int> onCharacterLevelUp;

        /// <summary>
        /// Raise when a character dies
        /// </summary>
        public void RaiseCharacterDied(Character character)
        {
            onCharacterDied?.Invoke(character);
        }

        /// <summary>
        /// Raise when a character levels up
        /// </summary>
        public void RaiseCharacterLevelUp(Character character, int newLevel)
        {
            onCharacterLevelUp?.Invoke(character, newLevel);
        }

        /// <summary>
        /// Subscribe to character death events
        /// </summary>
        public void AddCharacterDiedListener(Action<Character> listener)
        {
            onCharacterDied += listener;
        }

        /// <summary>
        /// Unsubscribe from character death events
        /// </summary>
        public void RemoveCharacterDiedListener(Action<Character> listener)
        {
            onCharacterDied -= listener;
        }

        /// <summary>
        /// Subscribe to character level up events
        /// </summary>
        public void AddCharacterLevelUpListener(Action<Character, int> listener)
        {
            onCharacterLevelUp += listener;
        }

        /// <summary>
        /// Unsubscribe from character level up events
        /// </summary>
        public void RemoveCharacterLevelUpListener(Action<Character, int> listener)
        {
            onCharacterLevelUp -= listener;
        }

        /// <summary>
        /// Clear all listeners when ScriptableObject is disabled
        /// Prevents memory leaks
        /// </summary>
        private void OnDisable()
        {
            onCharacterDied = null;
            onCharacterLevelUp = null;
        }
    }
}
