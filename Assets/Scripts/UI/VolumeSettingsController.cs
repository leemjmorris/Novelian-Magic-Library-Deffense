using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NovelianMagicLibraryDefense.Managers;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// VolumeSettingsController: Manages preferences panel and volume settings
    /// Controls panel open/close, volume sliders, and game pause state
    /// Logs volume values from 0-100 when sliders are dragged
    /// </summary>
    public class VolumeSettingsController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button stageButton;

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        private void Start()
        {
            SetupButtonListeners();
            InitializeSliders();
            SetupVolumeListeners();
            LoadCurrentVolumes();
        }

        /// <summary>
        /// Setup button event listeners
        /// </summary>
        private void SetupButtonListeners()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
                Debug.Log("[VolumeSettingsController] Close button listener 등록 완료");
            }

            if (stageButton != null)
            {
                stageButton.onClick.AddListener(OnStageButtonClicked);
                Debug.Log("[VolumeSettingsController] Stage button listener 등록 완료");
            }
        }

        /// <summary>
        /// Called when close button is clicked - closes panel and resumes game
        /// </summary>
        private void OnCloseButtonClicked()
        {
            UIManager uiManager = GetComponentInParent<Canvas>()?.GetComponentInChildren<UIManager>();
            if (uiManager != null)
            {
                SaveSettings();
                uiManager.ClosePreferencesPanel();
            }
            else
            {
                Debug.LogError("[VolumeSettingsController] UIManager를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// LCB: Called when stage button is clicked - loads StageScene
        /// </summary>
        private void OnStageButtonClicked()
        {
            SaveSettings();
            Time.timeScale = 1f; // LCB: Reset time scale before loading scene

            // LCB: Stop WaveManager spawning before scene change
            if (GameManager.Instance != null && GameManager.Instance.Wave != null)
            {
                GameManager.Instance.Wave.Reset();
            }

            Debug.Log("[VolumeSettingsController] Loading StageScene...");
            SceneManager.LoadScene("lobbyScene");
        }

        /// <summary>
        /// Initialize slider settings (0-100 range, whole numbers)
        /// </summary>
        private void InitializeSliders()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0;
                masterVolumeSlider.maxValue = 100;
                masterVolumeSlider.wholeNumbers = true;
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.minValue = 0;
                bgmVolumeSlider.maxValue = 100;
                bgmVolumeSlider.wholeNumbers = true;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0;
                sfxVolumeSlider.maxValue = 100;
                sfxVolumeSlider.wholeNumbers = true;
            }

            Debug.Log("[VolumeSettingsController] Sliders initialized (0-100 range)");
        }

        /// <summary>
        /// Setup event listeners for slider value changes
        /// </summary>
        private void SetupVolumeListeners()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            Debug.Log("[VolumeSettingsController] Volume listeners set up");
        }

        /// <summary>
        /// Load current volumes from AudioManager and set slider values
        /// </summary>
        private void LoadCurrentVolumes()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("[VolumeSettingsController] AudioManager instance not found, using default values (100)");

                // Set default values to 100 if AudioManager is not available
                if (masterVolumeSlider != null)
                {
                    masterVolumeSlider.value = 100f;
                }

                if (bgmVolumeSlider != null)
                {
                    bgmVolumeSlider.value = 100f;
                }

                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.value = 100f;
                }
                return;
            }

            // Convert 0-1 range to 0-100 range
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume() * 100f;
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = AudioManager.Instance.GetBGMVolume() * 100f;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume() * 100f;
            }

            Debug.Log("[VolumeSettingsController] Loaded current volumes from AudioManager");
        }

        /// <summary>
        /// Called when master volume slider value changes
        /// </summary>
        private void OnMasterVolumeChanged(float value)
        {
            Debug.Log($"[메인 볼륨] {value}");

            // Apply to AudioManager (convert 0-100 to 0-1)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value / 100f);
            }
        }

        /// <summary>
        /// Called when BGM volume slider value changes
        /// </summary>
        private void OnBGMVolumeChanged(float value)
        {
            Debug.Log($"[BGM 볼륨] {value}");

            // Apply to AudioManager (convert 0-100 to 0-1)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value / 100f);
            }
        }

        /// <summary>
        /// Called when SFX volume slider value changes
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            Debug.Log($"[효과음 볼륨] {value}");

            // Apply to AudioManager (convert 0-100 to 0-1)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value / 100f);
            }
        }

        /// <summary>
        /// Save current volume settings
        /// </summary>
        private void SaveSettings()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveAudioSettings();
                Debug.Log("[VolumeSettingsController] Volume settings saved");
            }
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (stageButton != null)
            {
                stageButton.onClick.RemoveListener(OnStageButtonClicked);
            }

            // Clean up volume listeners
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            }
        }
    }
}
