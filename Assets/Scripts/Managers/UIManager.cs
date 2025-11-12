using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NovelianMagicLibraryDefense.Core;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LCB/LMJ: Central UI manager that handles all UI elements and button interactions
    /// Refactored to BaseManager pattern for consistency with other managers
    /// Manages monster count, wave timer, barrier HP, and button interactions
    /// </summary>
    [System.Serializable]
    public class UIManager : BaseManager
    {
        // UI References injected from GameManager
        private readonly TextMeshProUGUI monsterCountText;
        private readonly TextMeshProUGUI waveTimerText;
        private readonly Slider barrierHPSlider;
        private readonly TextMeshProUGUI barrierHPText;
        private readonly GameObject cardPanel;

        private readonly Button pauseButton;
        private readonly Button settingsButton;
        private readonly Button skillButton1;
        private readonly Button skillButton2;
        private readonly Button skillButton3;
        private readonly Button skillButton4;

        /// <summary>
        /// LMJ: Constructor injection for UI dependencies
        /// </summary>
        public UIManager(
            TextMeshProUGUI monsterCount,
            TextMeshProUGUI waveTimer,
            Slider barrierSlider,
            TextMeshProUGUI barrierText,
            GameObject cardPanelRef,
            Button pause,
            Button settings,
            Button skill1,
            Button skill2,
            Button skill3,
            Button skill4)
        {
            monsterCountText = monsterCount;
            waveTimerText = waveTimer;
            barrierHPSlider = barrierSlider;
            barrierHPText = barrierText;
            cardPanel = cardPanelRef;
            pauseButton = pause;
            settingsButton = settings;
            skillButton1 = skill1;
            skillButton2 = skill2;
            skillButton3 = skill3;
            skillButton4 = skill4;
        }

        protected override void OnInitialize()
        {
            Debug.Log("[UIManager] Initializing UI");

            // Activate card panel at initialization
            if (cardPanel != null)
            {
                cardPanel.SetActive(true);
                Debug.Log("[UIManager] Card panel activated");
            }

            // Setup button listeners
            SetupButtonListeners();

            // Subscribe to Wall HP changes (event-based, not Update loop)
            Wall.OnHealthChanged += UpdateBarrierHP;

            Debug.Log("[UIManager] Initialized");
        }

        protected override void OnReset()
        {
            Debug.Log("[UIManager] Resetting UI");
            UpdateMonsterCount(0);
            UpdateWaveTimer(0f);
        }

        protected override void OnDispose()
        {
            Debug.Log("[UIManager] Disposing UI");

            // Unsubscribe from events
            Wall.OnHealthChanged -= UpdateBarrierHP;

            // Remove button listeners
            RemoveButtonListeners();
        }

        #region Button Setup

        private void SetupButtonListeners()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);

            if (skillButton1 != null)
                skillButton1.onClick.AddListener(() => OnSkillButtonClicked(1));

            if (skillButton2 != null)
                skillButton2.onClick.AddListener(() => OnSkillButtonClicked(2));

            if (skillButton3 != null)
                skillButton3.onClick.AddListener(() => OnSkillButtonClicked(3));

            if (skillButton4 != null)
                skillButton4.onClick.AddListener(() => OnSkillButtonClicked(4));

            Debug.Log("[UIManager] Button listeners setup complete");
        }

        private void RemoveButtonListeners()
        {
            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(OnPauseButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

            if (skillButton1 != null)
                skillButton1.onClick.RemoveAllListeners();

            if (skillButton2 != null)
                skillButton2.onClick.RemoveAllListeners();

            if (skillButton3 != null)
                skillButton3.onClick.RemoveAllListeners();

            if (skillButton4 != null)
                skillButton4.onClick.RemoveAllListeners();

            Debug.Log("[UIManager] Button listeners removed");
        }

        #endregion

        #region Button Callbacks

        private void OnPauseButtonClicked()
        {
            Debug.Log("[UIManager] Pause button clicked - Logic to be implemented");
            //TODO: Implement pause/resume game logic
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("[UIManager] Settings button clicked - Logic to be implemented");
            //TODO: Implement settings menu logic
        }

        private void OnSkillButtonClicked(int skillIndex)
        {
            Debug.Log($"[UIManager] Skill button {skillIndex} clicked - Logic to be implemented");
            //TODO: Implement skill activation logic
        }

        #endregion

        #region Monster Count Display

        /// <summary>
        /// LMJ: Update monster count display - called by WaveManager
        /// </summary>
        public void UpdateMonsterCount(int count)
        {
            if (monsterCountText != null)
            {
                monsterCountText.text = $"남은 몬스터: {count}";
            }
        }

        /// <summary>
        /// LMJ: Display custom monster text - called by WaveManager for special states
        /// </summary>
        public void SetMonsterText(string text)
        {
            if (monsterCountText != null)
            {
                monsterCountText.text = text;
            }
        }

        #endregion

        #region Wave Timer Display

        /// <summary>
        /// LMJ: Update wave timer display with remaining time in seconds
        /// Called by StageManager
        /// </summary>
        public void UpdateWaveTimer(float timeInSeconds)
        {
            if (waveTimerText != null)
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
                waveTimerText.text = $"웨이브 시간: {minutes:00}:{seconds:00}";
            }
        }

        #endregion

        #region Barrier HP Display

        /// <summary>
        /// LMJ: Update barrier HP slider and text - event-driven from Wall
        /// </summary>
        private void UpdateBarrierHP(float currentHP, float maxHP)
        {
            if (barrierHPSlider != null)
            {
                barrierHPSlider.value = currentHP / maxHP;
            }

            if (barrierHPText != null)
            {
                barrierHPText.text = $"결계 HP: {currentHP:F0}/{maxHP:F0}";
            }
        }

        #endregion
    }
}
