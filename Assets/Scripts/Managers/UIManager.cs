using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.UI;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LMJ: Lightweight UI coordinator that connects UI components
    /// Delegates to specialized UI components for actual functionality
    /// Single responsibility: Coordinate UI components and provide unified interface
    /// </summary>
    public class UIManager : BaseManager
    {
        [Header("Essential UI - Build Ready")]
        [SerializeField] private GameHUD gameHUD;
        [SerializeField] private CardSelectPanel cardSelectPanel;

        protected override void OnInitialize()
        {
            // UI components initialize themselves
            // UIManager just validates references
            ValidateReferences();
        }

        protected override void OnReset()
        {
            // Reset essential UI components
            if (gameHUD != null)
            {
                gameHUD.ResetUI();
            }

            // Close card panel if open
            if (cardSelectPanel != null && cardSelectPanel.IsOpen)
            {
                cardSelectPanel.Close();
            }
        }

        protected override void OnDispose()
        {
            // UI components clean up themselves
        }

        /// <summary>
        /// Validate that all UI components are assigned
        /// </summary>
        private void ValidateReferences()
        {
            // All UI components are optional - UIManager will handle null references gracefully
            // No errors or warnings logged for missing references
        }

        #region Public API - Essential UI Only

        // ===== GameHUD =====
        public void UpdateMonsterCount(int count)
        {
            if (gameHUD != null)
            {
                gameHUD.UpdateMonsterCount(count);
            }
        }

        public void UpdateWaveTimer(float timeInSeconds)
        {
            if (gameHUD != null)
            {
                gameHUD.UpdateWaveTimer(timeInSeconds);
            }
        }

        public void UpdateExperience(float currentExp, float maxExp)
        {
            if (gameHUD != null)
            {
                gameHUD.UpdateExperience(currentExp, maxExp);
            }
        }

        // ===== CardSelectPanel =====
        /// <summary>
        /// Open card selection for game start (2 character cards only)
        /// </summary>
        public void OpenCardSelectForGameStart()
        {
            if (cardSelectPanel != null)
            {
                cardSelectPanel.OpenForGameStart();
            }
        }

        /// <summary>
        /// Open card selection for level up (2 random cards: character + ability mix)
        /// </summary>
        public void OpenCardSelectForLevelUp()
        {
            if (cardSelectPanel != null)
            {
                cardSelectPanel.OpenForLevelUp();
            }
        }

        /// <summary>
        /// Open card selection with specific cards
        /// </summary>
        public void OpenCardSelectWithCards(CardSelectPanel.CardData[] cards, bool pauseGame = false)
        {
            if (cardSelectPanel != null)
            {
                cardSelectPanel.OpenWithCards(cards, pauseGame);
            }
        }

        public void CloseCardSelect()
        {
            if (cardSelectPanel != null)
            {
                cardSelectPanel.Close();
            }
        }

        public bool IsCardSelectOpen()
        {
            return cardSelectPanel != null && cardSelectPanel.IsOpen;
        }

        #endregion
    }
}
