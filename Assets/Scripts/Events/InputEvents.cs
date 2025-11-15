using System;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Events
{
    /// <summary>
    /// LMJ: ScriptableObject EventChannel for Input-related events
    /// Replaces static events to prevent memory leaks and enable Inspector visibility
    /// Note: CardSlot and CharacterData types are commented out until needed
    /// </summary>
    [CreateAssetMenu(fileName = "InputEvents", menuName = "Events/Input Events")]
    public class InputEvents : ScriptableObject
    {
        // LMJ: Temporarily using Vector2 instead of CardSlot until the type is defined
        private event Action<Vector2> onCardSlotClicked;
        private event Action<ScriptableObject> onCardSelected;

        /// <summary>
        /// Raise when a card slot is clicked
        /// </summary>
        public void RaiseCardSlotClicked(Vector2 position)
        {
            onCardSlotClicked?.Invoke(position);
        }

        /// <summary>
        /// Raise when a card is selected
        /// </summary>
        public void RaiseCardSelected(ScriptableObject cardData)
        {
            onCardSelected?.Invoke(cardData);
        }

        /// <summary>
        /// Subscribe to card slot clicked events
        /// </summary>
        public void AddCardSlotClickedListener(Action<Vector2> listener)
        {
            onCardSlotClicked += listener;
        }

        /// <summary>
        /// Unsubscribe from card slot clicked events
        /// </summary>
        public void RemoveCardSlotClickedListener(Action<Vector2> listener)
        {
            onCardSlotClicked -= listener;
        }

        /// <summary>
        /// Subscribe to card selected events
        /// </summary>
        public void AddCardSelectedListener(Action<ScriptableObject> listener)
        {
            onCardSelected += listener;
        }

        /// <summary>
        /// Unsubscribe from card selected events
        /// </summary>
        public void RemoveCardSelectedListener(Action<ScriptableObject> listener)
        {
            onCardSelected -= listener;
        }

        /// <summary>
        /// Clear all listeners when ScriptableObject is disabled
        /// Prevents memory leaks
        /// </summary>
        private void OnDisable()
        {
            onCardSlotClicked = null;
            onCardSelected = null;
        }
    }
}
