using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Controls stage navigation UI
    /// Manages navigation between stages (1-1, 1-2, etc.) and button visibility
    /// </summary>
    public class StageNavigationController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private TextMeshProUGUI stageNumberText;

        [Header("Chapter Selection")]
        [SerializeField] private TMP_Dropdown chapterDropdown;
        [SerializeField] private TMP_Dropdown viewportChapterDropdown;

        [Header("Viewport Stage Slots")]
        [SerializeField] private Toggle[] viewportStageToggles;

        [Header("Stage Settings")]
        [SerializeField] private int currentChapter = 1;
        [SerializeField] private int currentStage = 1;
        [SerializeField] private int maxStagesPerChapter = 10;

        private void Awake()
        {
            if (leftButton != null)
            {
                leftButton.onClick.AddListener(OnLeftButtonClicked);
            }

            if (rightButton != null)
            {
                rightButton.onClick.AddListener(OnRightButtonClicked);
            }

            SetupChapterDropdown();
            SetupViewportChapterDropdown();
            SetupViewportStageToggles();

            UpdateUI();
        }

        private void SetupChapterDropdown()
        {
            if (chapterDropdown == null) return;

            chapterDropdown.ClearOptions();

            List<string> options = new List<string>();
            for (int i = 1; i <= 3; i++)
            {
                options.Add($"챕터 {i}");
            }

            chapterDropdown.AddOptions(options);
            chapterDropdown.value = currentChapter - 1;
            chapterDropdown.RefreshShownValue();
            chapterDropdown.onValueChanged.AddListener(OnChapterDropdownChanged);
        }

        private void SetupViewportChapterDropdown()
        {
            if (viewportChapterDropdown == null) return;

            viewportChapterDropdown.ClearOptions();

            List<string> options = new List<string>();
            for (int i = 1; i <= 3; i++)
            {
                options.Add($"챕터 {i}");
            }

            viewportChapterDropdown.AddOptions(options);
            viewportChapterDropdown.value = currentChapter - 1;
            viewportChapterDropdown.RefreshShownValue();
            viewportChapterDropdown.onValueChanged.AddListener(OnViewportChapterDropdownChanged);
        }

        private void SetupViewportStageToggles()
        {
            if (viewportStageToggles == null || viewportStageToggles.Length == 0) return;

            for (int i = 0; i < viewportStageToggles.Length; i++)
            {
                if (viewportStageToggles[i] != null)
                {
                    int stageIndex = i + 1;
                    viewportStageToggles[i].onValueChanged.AddListener((isOn) =>
                    {
                        if (isOn)
                        {
                            OnViewportStageToggleSelected(stageIndex);
                        }
                    });
                }
            }
        }

        private void OnDestroy()
        {
            if (leftButton != null)
            {
                leftButton.onClick.RemoveListener(OnLeftButtonClicked);
            }

            if (rightButton != null)
            {
                rightButton.onClick.RemoveListener(OnRightButtonClicked);
            }

            if (chapterDropdown != null)
            {
                chapterDropdown.onValueChanged.RemoveListener(OnChapterDropdownChanged);
            }

            if (viewportChapterDropdown != null)
            {
                viewportChapterDropdown.onValueChanged.RemoveListener(OnViewportChapterDropdownChanged);
            }

            if (viewportStageToggles != null)
            {
                for (int i = 0; i < viewportStageToggles.Length; i++)
                {
                    if (viewportStageToggles[i] != null)
                    {
                        viewportStageToggles[i].onValueChanged.RemoveAllListeners();
                    }
                }
            }
        }

        private void OnChapterDropdownChanged(int dropdownIndex)
        {
            currentChapter = dropdownIndex + 1;
            currentStage = 1;
            UpdateUI();
        }

        private void OnViewportChapterDropdownChanged(int dropdownIndex)
        {
            currentChapter = dropdownIndex + 1;
            currentStage = 1;
            UpdateUI();
        }

        private void OnViewportStageToggleSelected(int stage)
        {
            currentStage = stage;
            UpdateUI();
        }

        private void OnLeftButtonClicked()
        {
            if (currentStage > 1)
            {
                currentStage--;
                UpdateUI();
            }
        }

        private void OnRightButtonClicked()
        {
            if (currentStage < maxStagesPerChapter)
            {
                currentStage++;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            // Sync both dropdowns
            if (chapterDropdown != null)
            {
                chapterDropdown.SetValueWithoutNotify(currentChapter - 1);
            }

            if (viewportChapterDropdown != null)
            {
                viewportChapterDropdown.SetValueWithoutNotify(currentChapter - 1);
            }

            // Update text displays
            if (stageText != null)
            {
                stageText.text = $"챕터 {currentChapter}";
            }

            if (stageNumberText != null)
            {
                stageNumberText.text = $"{currentChapter}-{currentStage}";
            }

            // Update viewport toggles
            UpdateViewportToggles();

            // Update arrow button visibility
            UpdateButtonVisibility();
        }

        private void UpdateViewportToggles()
        {
            if (viewportStageToggles == null) return;

            for (int i = 0; i < viewportStageToggles.Length; i++)
            {
                if (viewportStageToggles[i] != null)
                {
                    int stageNumber = i + 1;

                    if (stageNumber > maxStagesPerChapter)
                    {
                        viewportStageToggles[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        viewportStageToggles[i].gameObject.SetActive(true);
                        viewportStageToggles[i].SetIsOnWithoutNotify(stageNumber == currentStage);
                    }
                }
            }
        }

        private void UpdateButtonVisibility()
        {
            if (leftButton != null)
            {
                leftButton.gameObject.SetActive(currentStage > 1);
            }

            if (rightButton != null)
            {
                rightButton.gameObject.SetActive(currentStage < maxStagesPerChapter);
            }
        }

        public void SetStage(int chapter, int stage)
        {
            currentChapter = chapter;
            currentStage = Mathf.Clamp(stage, 1, maxStagesPerChapter);
            UpdateUI();
        }

        public (int chapter, int stage) GetCurrentStage()
        {
            return (currentChapter, currentStage);
        }

        public void SetMaxStagesPerChapter(int maxStages)
        {
            maxStagesPerChapter = maxStages;
            currentStage = Mathf.Clamp(currentStage, 1, maxStagesPerChapter);
            UpdateUI();
        }
    }
}
