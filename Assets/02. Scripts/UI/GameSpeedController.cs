using TMPro;
using UnityEngine;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Controls game speed (time scale)
    /// Single responsibility: Manage game speed settings
    /// OnClick은 Inspector에서 직접 할당
    /// </summary>
    public class GameSpeedController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI speedText;

        [Header("Speed Settings")]
        [SerializeField] private float[] speedOptions = { 1f, 1.5f, 2f };

        private int currentSpeedIndex = 0;

        private void Awake()
        {
            AutoFindReferences();
            UpdateSpeedDisplay();
        }

        /// <summary>
        /// Auto-find references if not assigned in Inspector
        /// </summary>
        private void AutoFindReferences()
        {
            if (speedText == null)
            {
                speedText = GetComponentInChildren<TextMeshProUGUI>();
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
