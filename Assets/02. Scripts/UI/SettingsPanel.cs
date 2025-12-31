using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Managers;
using NovelianMagicLibraryDefense.Core;
using Cysharp.Threading.Tasks;

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

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        private bool isOpen = false;
        private bool isInitializingSliders = false;
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
            Debug.Log($"[SettingsPanel] OnMasterVolumeChanged called: value={value}, isInitializing={isInitializingSliders}");
            if (isInitializingSliders) return;
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
            Debug.Log($"[SettingsPanel] OnBGMVolumeChanged called: value={value}, isInitializing={isInitializingSliders}");
            if (isInitializingSliders) return;
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
            Debug.Log($"[SettingsPanel] OnSFXVolumeChanged called: value={value}, isInitializing={isInitializingSliders}");
            if (isInitializingSliders) return;
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
            // Issue #602: TimeManager 스택 기반 일시정지
            if (pauseOnOpen)
            {
                TimeManager.Instance?.PushTimeScale(0f);
            }

            gameObject.SetActive(true);
            isOpen = true;

            // 슬라이더 값을 AudioManager의 현재 볼륨 값으로 동기화
            SyncSlidersWithAudioManager();
        }

        /// <summary>
        /// 슬라이더 값을 AudioManager의 현재 볼륨 값으로 동기화
        /// </summary>
        private void SyncSlidersWithAudioManager()
        {
            if (AudioManager.Instance == null) return;

            isInitializingSliders = true;

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();

            if (bgmVolumeSlider != null)
                bgmVolumeSlider.value = AudioManager.Instance.GetBGMVolume();

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();

            isInitializingSliders = false;
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

            // Issue #602: TimeManager 스택에서 복구
            if (pauseOnOpen)
            {
                TimeManager.Instance?.PopTimeScale();
            }

            isOpen = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Issue #602: 패널이 비활성화될 때 TimeScale 복구 보장
        /// </summary>
        private void OnDisable()
        {
            if (isOpen && pauseOnOpen)
            {
                TimeManager.Instance?.PopTimeScale();
                isOpen = false;
            }
        }

        /// <summary>
        /// Go to lobby scene with loading UI
        /// </summary>
        public void GoToLobby()
        {
            // Issue #570: 씬 전환 중 레벨업 방지를 위해 플래그 설정
            if (GameManager.Instance?.Stage != null)
            {
                GameManager.Instance.Stage.IsExitingStage = true;
            }

            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();
            LoadSceneWithLoadingUI("LobbyScene").Forget();
        }

        /// <summary>
        /// Load scene with loading UI and fade effect
        /// </summary>
        private async UniTaskVoid LoadSceneWithLoadingUI(string sceneName)
        {
            // 매니저가 없으면 직접 씬 로드 (fallback)
            if (LoadingUIManager.Instance == null || FadeController.Instance == null)
            {
                Debug.LogWarning("LoadingUIManager or FadeController not available, loading scene directly");
                await SceneManager.LoadSceneAsync(sceneName);
                return;
            }

            // Step 1: 로딩 UI 표시 및 진행률 애니메이션
            LoadingUIManager.Instance.Show();
            await LoadingUIManager.Instance.FakeLoadAsync();

            // Step 2: 100% 상태 잠깐 보여주기
            await UniTask.Delay(200);

            // Step 3: 페이드 아웃 (화면 어두워짐)
            FadeController.Instance.fadePanel.SetActive(true);
            await FadeController.Instance.FadeOut(0.5f);

            // Step 4: 로딩 UI 숨기기
            await LoadingUIManager.Instance.Hide();

            // Step 5: 씬 로드
            await SceneManager.LoadSceneAsync(sceneName);

            // Step 6: 페이드 인 (새 씬 밝아짐)
            await FadeController.Instance.FadeIn(0.5f);

            // Step 7: 페이드 패널 비활성화
            FadeController.Instance.fadePanel.SetActive(false);
        }

        /// <summary>
        /// Check if panel is currently open
        /// </summary>
        public bool IsOpen => isOpen;
    }
}
