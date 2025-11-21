using System;
using UnityEngine;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Manages character selection panel at game start
    /// Displays 2 character cards - selecting one instantly places character and closes panel
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panel;

        [Header("Card Container")]
        [SerializeField] private Transform cardContainer;

        [Header("Card Prefab")]
        [SerializeField] private GameObject characterCardPrefab;

        [Header("Character IDs to Display")]
        [SerializeField] private int[] characterIds = { 1, 2 }; // Default: 2 random characters

        // Events
        public event Action<int> OnCharacterSelected; // Fires when card is clicked

        private GameObject[] cardInstances;
        private CharacterPlacementManager placementManager;

        private void Awake()
        {
            // Find CharacterPlacementManager in scene
            placementManager = FindFirstObjectByType<CharacterPlacementManager>();
            if (placementManager == null)
            {
                Debug.LogError("[CharacterSelectPanel] CharacterPlacementManager not found in scene!");
            }

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
                Debug.LogError("[CharacterSelectPanel] Panel GameObject not assigned!");
            }
        }

        /// <summary>
        /// Open character selection panel with 2 character cards
        /// </summary>
        public void Open()
        {
            if (panel == null) return;

            panel.SetActive(true);
            CreateCharacterCards();
        }

        /// <summary>
        /// Open with specific character IDs
        /// </summary>
        public void Open(int[] characterIds)
        {
            if (characterIds == null || characterIds.Length != 2)
            {
                Debug.LogError("[CharacterSelectPanel] Must provide exactly 2 character IDs!");
                return;
            }

            this.characterIds = characterIds;
            Open();
        }

        /// <summary>
        /// Create 2 character cards in the container
        /// </summary>
        private void CreateCharacterCards()
        {
            ClearCards();

            if (cardContainer == null || characterCardPrefab == null)
            {
                Debug.LogError("[CharacterSelectPanel] Card container or prefab not assigned!");
                return;
            }

            cardInstances = new GameObject[2];

            for (int i = 0; i < 2; i++)
            {
                GameObject cardObj = Instantiate(characterCardPrefab, cardContainer);
                cardObj.name = $"CharacterCard_{characterIds[i]}";

                // Setup card button
                UnityEngine.UI.Button cardButton = cardObj.GetComponent<UnityEngine.UI.Button>();
                if (cardButton != null)
                {
                    int characterId = characterIds[i];
                    cardButton.onClick.AddListener(() => OnCardClicked(characterId));
                }
                else
                {
                    Debug.LogWarning($"[CharacterSelectPanel] Card prefab missing Button component!");
                }

                // TODO: Set card visual data (sprite, name, etc.) based on characterId
                // Example: cardObj.GetComponent<CharacterCard>()?.SetData(characterId);

                cardInstances[i] = cardObj;
                Debug.Log($"[CharacterSelectPanel] Created card for character ID {characterIds[i]}");
            }
        }

        /// <summary>
        /// Handle card click - spawn character and close panel
        /// </summary>
        private void OnCardClicked(int characterId)
        {
            Debug.Log($"[CharacterSelectPanel] Card clicked: Character ID {characterId}");

            // Spawn character via CharacterPlacementManager
            if (placementManager != null)
            {
                placementManager.SpawnCharacterById(characterId);
            }
            else
            {
                Debug.LogError("[CharacterSelectPanel] Cannot spawn character - CharacterPlacementManager is null!");
            }

            // Fire event
            OnCharacterSelected?.Invoke(characterId);

            // Close panel
            Close();
        }

        /// <summary>
        /// Close character selection panel
        /// </summary>
        public void Close()
        {
            if (panel == null) return;

            panel.SetActive(false);
            ClearCards();
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
