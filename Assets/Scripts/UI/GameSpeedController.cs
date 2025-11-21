using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Controls game speed (time scale)
    /// Single responsibility: Manage game speed settings
    /// </summary>
    public class GameSpeedController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button speedButton;
        [SerializeField] private TextMeshProUGUI speedText;

        [Header("Speed Settings")]
        [SerializeField] private float[] speedOptions = { 1f, 1.5f, 2f };

        private int currentSpeedIndex = 0;

        private void Awake()
        {
            SetupButton();
            UpdateSpeedDisplay();
        }

        private void OnDestroy()
        {
            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(CycleSpeed);
            }
        }

        /// <summary>
        /// Setup button listener
        /// </summary>
        private void SetupButton()
        {
            if (speedButton != null)
            {
                speedButton.onClick.AddListener(CycleSpeed);
            }
            else
            {
                Debug.LogError("[GameSpeedController] Speed button not assigned!");
            }
        }

        /// <summary>
        /// Cycle through available speed options
        /// </summary>
        public void CycleSpeed()
        {
            currentSpeedIndex = (currentSpeedIndex + 1) % speedOptions.Length;
            SetSpeed(speedOptions[currentSpeedIndex]);
        }

        /// <summary>
        /// Set game speed to specific value
        /// </summary>
        public void SetSpeed(float speed)
        {
            Time.timeScale = speed;
            UpdateSpeedDisplay();
        }

        /// <summary>
        /// Update speed display text
        /// </summary>
        private void UpdateSpeedDisplay()
        {
            if (speedText != null)
            {
                float currentSpeed = speedOptions[currentSpeedIndex];
                speedText.text = $"X{currentSpeed:F1}";
            }
        }

        /// <summary>
        /// Reset speed to default (1x)
        /// </summary>
        public void ResetSpeed()
        {
            currentSpeedIndex = 0;
            SetSpeed(speedOptions[0]);
        }

        /// <summary>
        /// Get current speed multiplier
        /// </summary>
        public float CurrentSpeed => speedOptions[currentSpeedIndex];
    }
}
