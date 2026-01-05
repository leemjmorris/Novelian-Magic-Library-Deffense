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
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private Slider skillVolumeSlider;

        private bool isOpen = false;
        private bool isInitializingSliders = false;

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
        /// Called when Voice volume slider value changes
        /// Inspector에서 Slider의 OnValueChanged에 할당
        /// </summary>
        public void OnVoiceVolumeChanged(float value)
        {
            Debug.Log($"[SettingsPanel] OnVoiceVolumeChanged called: value={value}, isInitializing={isInitializingSliders}");
            if (isInitializingSliders) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVoiceVolume(value);
            }
        }

        /// <summary>
        /// Called when Skill volume slider value changes
        /// Inspector에서 Slider의 OnValueChanged에 할당
        /// </summary>
        public void OnSkillVolumeChanged(float value)
        {
            Debug.Log($"[SettingsPanel] OnSkillVolumeChanged called: value={value}, isInitializing={isInitializingSliders}");
            if (isInitializingSliders) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSkillVolume(value);
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

            if (voiceVolumeSlider != null)
                voiceVolumeSlider.value = AudioManager.Instance.GetVoiceVolume();

            if (skillVolumeSlider != null)
                skillVolumeSlider.value = AudioManager.Instance.GetSkillVolume();

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
        /// Issue #646: 패널이 비활성화될 때 오디오 설정 저장 보장
        /// </summary>
        private void OnDisable()
        {
            if (isOpen)
            {
                // Issue #646: Close()를 거치지 않고 SetActive(false)로 닫힌 경우에도 설정 저장
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SaveAudioSettings();
                }

                if (pauseOnOpen)
                {
                    TimeManager.Instance?.PopTimeScale();
                }
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

            // Issue #605: 로비 전환 전 게임 일시정지 (사운드 방지)
            TimeManager.Instance?.Pause();
            // Issue #605: 로비 전환 전 모든 사운드 정지
            AudioManager.Instance?.StopAllSounds();
            // FadeController는 Time.unscaledDeltaTime 사용으로 Pause 영향 없음
            LoadSceneWithLoadingUI("LobbyScene").Forget();
        }

        /// <summary>
        /// Load scene with loading UI and fade effect
        /// </summary>
        private async UniTaskVoid LoadSceneWithLoadingUI(string sceneName)
        {
            // Issue #639: 로딩 UI 애니메이션을 위해 TimeScale 복구
            // 환경설정 패널 Open()에서 PushTimeScale(0f)로 일시정지된 상태
            TimeManager.Instance?.ResetTimeScale();

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
            // Issue #605: TimeScale = 0에서도 동작하도록 ignoreTimeScale: true
            await UniTask.Delay(200, ignoreTimeScale: true);

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
