using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Manages level-up card selection panel
    /// Displays 3 upgrade cards - selecting one applies upgrade and closes panel
    /// Reuses card UI system like CharacterSelectPanel
    /// </summary>
    public class LevelUpCardPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panel;

        [Header("Card Container")]
        [SerializeField] private Transform cardContainer;

        [Header("Card Prefab")]
        [SerializeField] private GameObject cardPrefab; // Reuse same card prefab as CharacterSelectPanel

        [Header("Card IDs to Display")]
        [SerializeField] private int[] cardIds = { 1, 2, 3 }; // Default: 3 random cards

        [Header("Settings")]
        [SerializeField] private bool pauseOnOpen = true;

        // Events
        public event Action<int> OnCardSelected; // Fires when card is clicked and upgrade is applied

        private GameObject[] cardInstances;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            InitializePanel();
        }

        /// <summary>
        /// Initialize panel to closed state
        /// </summary>
        private void InitializePanel()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
            else
            {
                Debug.LogError("[LevelUpCardPanel] Panel GameObject not assigned!");
            }
        }

        /// <summary>
        /// Open level-up card panel with default cards
        /// </summary>
        public void Open()
        {
            if (panel == null) return;

            // Pause game if needed
            if (pauseOnOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            panel.SetActive(true);
            CreateCards();
        }

        /// <summary>
        /// Open with specific card IDs (3 cards)
        /// </summary>
        public void Open(int[] cardIds)
        {
            if (cardIds == null || cardIds.Length != 3)
            {
                Debug.LogError("[LevelUpCardPanel] Must provide exactly 3 card IDs!");
                return;
            }

            this.cardIds = cardIds;
            Open();
        }

        /// <summary>
        /// Close level-up card panel
        /// </summary>
        public void Close()
        {
            if (panel == null) return;

            panel.SetActive(false);

            // Resume game
            if (pauseOnOpen)
            {
                Time.timeScale = previousTimeScale;
            }

            // Cleanup card instances
            ClearCards();
        }

        /// <summary>
        /// Create 3 level-up cards in the container
        /// </summary>
        private void CreateCards()
        {
            ClearCards();

            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogError("[LevelUpCardPanel] Card container or prefab not assigned!");
                return;
            }

            cardInstances = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, cardContainer);
                cardObj.name = $"LevelUpCard_{cardIds[i]}";

                // Setup card button
                UnityEngine.UI.Button cardButton = cardObj.GetComponent<UnityEngine.UI.Button>();
                if (cardButton != null)
                {
                    int cardId = cardIds[i];
                    cardButton.onClick.AddListener(() => OnCardClicked(cardId));
                }
                else
                {
                    Debug.LogWarning($"[LevelUpCardPanel] Card prefab missing Button component!");
                }

                // TODO: Set card visual data (sprite, description, etc.) based on cardId
                // Example: cardObj.GetComponent<LevelUpCard>()?.SetData(cardId);

                cardInstances[i] = cardObj;
                Debug.Log($"[LevelUpCardPanel] Created card for upgrade ID {cardIds[i]}");
            }
        }

        /// <summary>
        /// Handle card click - apply upgrade and close panel
        /// </summary>
        private void OnCardClicked(int cardId)
        {
            Debug.Log($"[LevelUpCardPanel] Card clicked: Upgrade ID {cardId}");

            // TODO: Apply upgrade logic here
            // Example: UpgradeManager.ApplyUpgrade(cardId);

            // Fire event
            OnCardSelected?.Invoke(cardId);

            // Close panel
            Close();
        }

        /// <summary>
        /// Clear all spawned cards
        /// </summary>
        private void ClearCards()
        {
            if (cardContainer == null) return;

            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            cardInstances = null;
        }

        /// <summary>
        /// Check if panel is currently open
        /// </summary>
        public bool IsOpen => panel != null && panel.activeSelf;
    }
}
