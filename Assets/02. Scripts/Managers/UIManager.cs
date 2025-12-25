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
        /// Open card selection for game start (deck count character cards: 3-4)
        /// </summary>
        public void OpenCardSelectForGameStart()
        {
            Debug.Log($"[UIManager] OpenCardSelectForGameStart called. cardSelectPanel: {(cardSelectPanel != null ? "OK" : "NULL")}");
            if (cardSelectPanel != null)
            {
                cardSelectPanel.OpenForGameStart();
            }
            else
            {
                Debug.LogError("[UIManager] cardSelectPanel is NULL! Inspector에서 CardSelectPanel 할당 필요!");
            }
        }

        /// <summary>
        /// Open card selection for level up
        /// - Card_Type 1: 4 stat cards (fixed)
        /// - Card_Type 2: deck count character cards (3-4)
        /// </summary>
        public void OpenCardSelectForLevelUp()
        {
            if (cardSelectPanel != null)
            {
                cardSelectPanel.OpenForLevelUp();
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

        /// <summary>
        /// JML: CardSelectPanel Awake 완료 여부 확인 (재시작 시 타이밍 문제 해결용)
        /// </summary>
        public bool IsCardSelectPanelReady()
        {
            return cardSelectPanel != null && cardSelectPanel.IsAwakeComplete;
        }

        #endregion
    }
}
