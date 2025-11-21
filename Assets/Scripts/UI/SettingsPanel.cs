using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Manages settings panel (pause menu)
    /// Single responsibility: Control settings panel visibility and pause state
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panel;

        [Header("Button References")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [SerializeField] private bool pauseOnOpen = true;

        private bool isOpen = false;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            SetupButtons();
            InitializePanel();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        /// <summary>
        /// Setup button listeners
        /// </summary>
        private void SetupButtons()
        {
            if (openButton != null)
            {
                openButton.onClick.AddListener(Toggle);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        /// <summary>
        /// Remove button listeners
        /// </summary>
        private void RemoveButtonListeners()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Toggle);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        /// <summary>
        /// Initialize panel to closed state
        /// </summary>
        private void InitializePanel()
        {
            if (panel != null)
            {
                panel.SetActive(false);
                isOpen = false;
            }
            else
            {
                Debug.LogError("[SettingsPanel] Panel GameObject not assigned!");
            }
        }

        /// <summary>
        /// Toggle panel open/closed
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// Open settings panel
        /// </summary>
        public void Open()
        {
            if (panel == null) return;

            // Save current time scale and pause if needed
            if (pauseOnOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            panel.SetActive(true);
            isOpen = true;
        }

        /// <summary>
        /// Close settings panel
        /// </summary>
        public void Close()
        {
            if (panel == null) return;

            panel.SetActive(false);
            isOpen = false;

            // Restore previous time scale
            if (pauseOnOpen)
            {
                Time.timeScale = previousTimeScale;
            }
        }

        /// <summary>
        /// Check if panel is currently open
        /// </summary>
        public bool IsOpen => isOpen;
    }
}
