using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// LCB/LMJ: Central UI manager that handles all UI elements and button interactions
    /// Converted to MonoBehaviour for Inspector integration and event system compatibility
    /// Manages monster count, wave timer, barrier HP, and button interactions
    /// </summary>
    public class UIManager : BaseManager
    {
        [Header("Event Channels")]
        [SerializeField] private WallEvents wallEvents;

        [Header("UI References - Monster Display")]
        [SerializeField] private TextMeshProUGUI monsterCountText;

        [Header("UI References - Timer Display")]
        [SerializeField] private TextMeshProUGUI waveTimerText;

        [Header("UI References - Barrier HP")]
        [SerializeField] private Slider barrierHPSlider;
        [SerializeField] private TextMeshProUGUI barrierHPText;

        [Header("UI References - Exp Slider")]
        [SerializeField] private Slider expSlider;

        [Header("UI References - Panels")]
        [SerializeField] private GameObject cardPanel;

        [Header("UI References - SpeedButtonText")]
        [SerializeField] private TextMeshProUGUI speedButtonText;

        [Header("UI References - Buttons")]
        [SerializeField] private Button speedButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button skillButton1;
        [SerializeField] private Button skillButton2;
        [SerializeField] private Button skillButton3;
        [SerializeField] private Button skillButton4;

        /// <summary>
        /// LMJ: Initialize UI elements and setup button listeners
        /// </summary>
        protected override void OnInitialize()
        {
            if (expSlider != null)
                expSlider.value = 0f;
            if (barrierHPSlider != null)
            {
                barrierHPSlider.value = 1f;
                Debug.Log($"[UIManager] OnInitialize - barrierHPSlider assigned: {barrierHPSlider != null}");
            }
            else
            {
                Debug.LogError("[UIManager] OnInitialize - barrierHPSlider is null! Inspector 할당 확인 필요!");
            }

            if (speedButton != null)
            {
                Debug.Log($"[UIManager] OnInitialize - speedButton assigned: {speedButton != null}");
            }
            else
            {
                Debug.LogError("[UIManager] OnInitialize - speedButton is null! Inspector 할당 확인 필요!");
            }

            if (speedButtonText != null)
                speedButtonText.text = "X1";

            // Setup button listeners
            SetupButtonListeners();

            // LMJ: Subscribe to Wall HP changes via EventChannel
            if (wallEvents != null)
            {
                wallEvents.AddHealthChangedListener(UpdateBarrierHP);
                Debug.Log("[UIManager] WallEvents listener 등록 완료");
            }
            else
            {
                Debug.LogError("[UIManager] wallEvents is null! Inspector에서 할당해주세요.");
            }
        }

        /// <summary>
        /// LMJ: Reset UI to initial state
        /// </summary>
        protected override void OnReset()
        {
            UpdateMonsterCount(0);
            UpdateWaveTimer(0f);
            if (expSlider != null)
                expSlider.value = 0f;
            if (barrierHPSlider != null)
                barrierHPSlider.value = 1f;
            if (speedButtonText != null)
                speedButtonText.text = "X1";
        }

        protected override void OnDispose()
        {
            // LMJ: Unsubscribe from EventChannel
            if (wallEvents != null)
            {
                wallEvents.RemoveHealthChangedListener(UpdateBarrierHP);
            }

            // Remove button listeners
            RemoveButtonListeners();
        }

        #region Button Setup

        private void SetupButtonListeners()
        {
            if (speedButton != null)
            {
                speedButton.onClick.AddListener(OnSpeedButtonClicked);
                Debug.Log("[UIManager] Speed button listener 등록 완료");
            }
            else
            {
                Debug.LogError("[UIManager] speedButton is null! Inspector에서 할당해주세요.");
            }

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
        }

        private void RemoveButtonListeners()
        {
            if (speedButton != null)
                speedButton.onClick.RemoveListener(OnSpeedButtonClicked);

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
        }

        #endregion

        #region Button Callbacks

        private void OnSpeedButtonClicked()
        {
            Debug.Log($"[UIManager] Speed button clicked! Current Time.timeScale: {Time.timeScale}");

            switch (Time.timeScale)
            {
                case 1f:
                    Time.timeScale = 1.5f;
                    if (speedButtonText != null) speedButtonText.text = "X1.5";
                    Debug.Log("[UIManager] Speed changed to X1.5");
                    break;
                case 1.5f:
                    Time.timeScale = 2f;
                    if (speedButtonText != null) speedButtonText.text = "X2";
                    Debug.Log("[UIManager] Speed changed to X2");
                    break;
                case 2f:
                    Time.timeScale = 1f;
                    if (speedButtonText != null) speedButtonText.text = "X1";
                    Debug.Log("[UIManager] Speed changed to X1");
                    break;
                default:
                    Time.timeScale = 1f;
                    if (speedButtonText != null) speedButtonText.text = "X1";
                    Debug.Log($"[UIManager] Unexpected timeScale {Time.timeScale}, reset to X1");
                    break;
            }
        }

        private void OnSettingsButtonClicked()
        {
            var previousTimeScale = Time.timeScale;
            Time.timeScale = 0f; //JML: Pause the game when settings is opened

            Time.timeScale = previousTimeScale; //JML: Resume the game when settings is closed
        }

        private void OnSkillButtonClicked(int skillIndex)
        {
            // TODO: Implement skill logic
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
            float sliderValue = currentHP / maxHP;
            Debug.Log($"[UIManager] UpdateBarrierHP 호출! currentHP={currentHP}, maxHP={maxHP}, sliderValue={sliderValue}, barrierHPSlider null? {barrierHPSlider == null}");

            if (barrierHPSlider != null)
            {
                barrierHPSlider.value = sliderValue;
                Debug.Log($"[UIManager] Slider value 설정 완료: {barrierHPSlider.value}");
            }
            else
            {
                Debug.LogError("[UIManager] barrierHPSlider is null! Inspector에서 할당해주세요.");
            }

            if (barrierHPText != null)
            {
                barrierHPText.text = $"결계 HP: {currentHP:F0}/{maxHP:F0}";
            }
        }

        #endregion

        #region Experience Display

        public void UpdateExperience(float currentExp, float maxExp)
        {
            if (expSlider != null)
            {
                expSlider.value = currentExp / maxExp;
            }
        }

        #endregion
    }
}
