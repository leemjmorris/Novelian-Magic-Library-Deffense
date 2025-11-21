using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Stage selection UI controller for StageScene
    /// Handles stage navigation (1-30) with left/right arrows
    /// </summary>
    public class StageSelectUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;

        [Header("Settings")]
        [SerializeField] private int minStage = 1;
        [SerializeField] private int maxStage = 30;

        private int currentStage = 1;

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void Start()
        {
            UpdateUI();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.onClick.AddListener(OnRightArrowClicked);
            }
        }

        private void RemoveButtonListeners()
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.onClick.RemoveListener(OnLeftArrowClicked);
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.onClick.RemoveListener(OnRightArrowClicked);
            }
        }

        private void OnLeftArrowClicked()
        {
            if (currentStage > minStage)
            {
                currentStage--;
                UpdateUI();
                Debug.Log($"[StageSelectUI] Stage changed to {currentStage}");
            }
        }

        private void OnRightArrowClicked()
        {
            if (currentStage < maxStage)
            {
                currentStage++;
                UpdateUI();
                Debug.Log($"[StageSelectUI] Stage changed to {currentStage}");
            }
        }

        private void UpdateUI()
        {
            // Update stage text
            if (stageText != null)
            {
                stageText.text = $"Stage {currentStage}";
            }

            // Update arrow visibility based on current stage
            UpdateArrowVisibility();
        }

        private void UpdateArrowVisibility()
        {
            // Stage 1: Only right arrow visible
            // Stage 2-29: Both arrows visible
            // Stage 30: Only left arrow visible

            if (leftArrowButton != null)
            {
                leftArrowButton.gameObject.SetActive(currentStage > minStage);
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.gameObject.SetActive(currentStage < maxStage);
            }
        }

        /// <summary>
        /// Get the currently selected stage number
        /// </summary>
        public int GetCurrentStage()
        {
            return currentStage;
        }

        /// <summary>
        /// Set the current stage programmatically
        /// </summary>
        public void SetCurrentStage(int stage)
        {
            currentStage = Mathf.Clamp(stage, minStage, maxStage);
            UpdateUI();
        }
    }
}
