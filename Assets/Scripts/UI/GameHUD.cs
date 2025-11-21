using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Events;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Displays in-game HUD information (monsters, timer, wall health, experience)
    /// Single responsibility: Display game state information
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private WallEvents wallEvents;

        [Header("Monster Display")]
        [SerializeField] private TextMeshProUGUI remainingMonstersText;

        [Header("Wave Timer")]
        [SerializeField] private TextMeshProUGUI waveTimerDisplay;

        [Header("Wall Health")]
        [SerializeField] private Slider wallHealthSlider;
        [SerializeField] private TextMeshProUGUI wallHealthText;

        [Header("Experience")]
        [SerializeField] private Slider experienceSlider;

        private void Awake()
        {
            InitializeUI();
        }

        private void OnEnable()
        {
            // Subscribe to wall health changes
            if (wallEvents != null)
            {
                wallEvents.AddHealthChangedListener(UpdateWallHealth);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from wall health changes
            if (wallEvents != null)
            {
                wallEvents.RemoveHealthChangedListener(UpdateWallHealth);
            }
        }

        /// <summary>
        /// Initialize UI elements to default state
        /// </summary>
        private void InitializeUI()
        {
            if (experienceSlider != null)
                experienceSlider.value = 0f;

            if (wallHealthSlider != null)
                wallHealthSlider.value = 1f;

            UpdateMonsterCount(0);
            UpdateWaveTimer(0f);
        }

        /// <summary>
        /// Update remaining monsters display
        /// </summary>
        public void UpdateMonsterCount(int count)
        {
            if (remainingMonstersText != null)
            {
                remainingMonstersText.text = $"남은 몬스터: {count}";
            }
        }

        /// <summary>
        /// Update wave timer display
        /// </summary>
        public void UpdateWaveTimer(float timeInSeconds)
        {
            if (waveTimerDisplay != null)
            {
                int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
                waveTimerDisplay.text = $"웨이브 시간: {minutes:00}:{seconds:00}";
            }
        }

        /// <summary>
        /// Update wall health display (event-driven)
        /// </summary>
        private void UpdateWallHealth(float currentHP, float maxHP)
        {
            if (wallHealthSlider != null)
            {
                wallHealthSlider.value = currentHP / maxHP;
            }

            if (wallHealthText != null)
            {
                wallHealthText.text = $"결계 HP: {currentHP:F0}/{maxHP:F0}";
            }
        }

        /// <summary>
        /// Update experience display
        /// </summary>
        public void UpdateExperience(float currentExp, float maxExp)
        {
            if (experienceSlider != null)
            {
                experienceSlider.value = currentExp / maxExp;
            }
        }

        /// <summary>
        /// Reset all UI elements to initial state
        /// </summary>
        public void ResetUI()
        {
            UpdateMonsterCount(0);
            UpdateWaveTimer(0f);

            if (experienceSlider != null)
                experienceSlider.value = 0f;

            if (wallHealthSlider != null)
                wallHealthSlider.value = 1f;
        }
    }
}
