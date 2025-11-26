using UnityEngine;
using UnityEngine.SceneManagement;
using NovelianMagicLibraryDefense.Managers;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Manages settings panel (pause menu)
    /// Single responsibility: Control settings panel visibility and pause state
    /// 버튼 OnClick은 Inspector에서 직접 할당
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool pauseOnOpen = true;

        private bool isOpen = false;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            InitializePanel();
        }

        /// <summary>
        /// Called when master volume slider value changes
        /// Inspector에서 Slider의 OnValueChanged에 할당
        /// </summary>
        public void OnMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
        }

        /// <summary>
        /// Called when BGM volume slider value changes
        /// Inspector에서 Slider의 OnValueChanged에 할당
        /// </summary>
        public void OnBGMVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value);
            }
        }

        /// <summary>
        /// Called when SFX volume slider value changes
        /// Inspector에서 Slider의 OnValueChanged에 할당
        /// </summary>
        public void OnSFXVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
        }

        /// <summary>
        /// Initialize panel to closed state
        /// </summary>
        private void InitializePanel()
        {
            gameObject.SetActive(false);
            isOpen = false;
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
            // Save current time scale and pause if needed
            if (pauseOnOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            gameObject.SetActive(true);
            isOpen = true;
        }

        /// <summary>
        /// Close settings panel
        /// </summary>
        public void Close()
        {
            // Save audio settings when closing
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveAudioSettings();
            }

            // Restore previous time scale
            if (pauseOnOpen)
            {
                Time.timeScale = previousTimeScale;
            }

            isOpen = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Go to lobby scene
        /// </summary>
        public void GoToLobby()
        {
            // Restore time scale before scene change
            Time.timeScale = 1f;
            SceneManager.LoadScene("LobbyScene");
        }

        /// <summary>
        /// Check if panel is currently open
        /// </summary>
        public bool IsOpen => isOpen;
    }
}
