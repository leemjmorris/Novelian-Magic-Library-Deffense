using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Manages the stage scroll view and all stage buttons
    /// Handles unlocking stages and scroll position
    /// </summary>
    public class StageScrollManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private List<StageButton> stageButtons = new List<StageButton>();

        [Header("Settings")]
        [SerializeField] private int unlockedStageCount = 1; // How many stages are unlocked

        private void Start()
        {
            InitializeStageButtons();
            ScrollToCurrentStage();
        }

        /// <summary>
        /// Initialize all stage buttons based on unlock status
        /// </summary>
        private void InitializeStageButtons()
        {
            for (int i = 0; i < stageButtons.Count; i++)
            {
                if (stageButtons[i] != null)
                {
                    int stageNumber = i + 1;
                    stageButtons[i].SetStageNumber(stageNumber);

                    // Unlock stages up to unlockedStageCount
                    bool isLocked = stageNumber > unlockedStageCount;
                    stageButtons[i].SetLocked(isLocked);
                }
            }
        }

        /// <summary>
        /// Scroll to show the current (highest unlocked) stage
        /// </summary>
        public void ScrollToCurrentStage()
        {
            if (scrollRect == null || stageButtons.Count == 0)
                return;

            // Calculate normalized position based on unlocked stage
            // Stage 1 at bottom = 0, Stage N at top = 1
            float normalizedPosition = (float)(unlockedStageCount - 1) / (stageButtons.Count - 1);
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            // Set scroll position (1 = top, 0 = bottom)
            scrollRect.verticalNormalizedPosition = 1f - normalizedPosition;
        }

        /// <summary>
        /// Unlock a specific stage
        /// </summary>
        public void UnlockStage(int stageNumber)
        {
            if (stageNumber > unlockedStageCount)
            {
                unlockedStageCount = stageNumber;
                InitializeStageButtons();
            }
        }

        /// <summary>
        /// Set the number of unlocked stages
        /// </summary>
        public void SetUnlockedStageCount(int count)
        {
            unlockedStageCount = Mathf.Max(1, count);
            InitializeStageButtons();
        }

        /// <summary>
        /// Get the current unlocked stage count
        /// </summary>
        public int GetUnlockedStageCount()
        {
            return unlockedStageCount;
        }
    }
}
