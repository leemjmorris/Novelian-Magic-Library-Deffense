using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;
using NovelianMagicLibraryDefense.Core;
using Firebase.Auth;
using Google;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Account UI 패널 관리
    /// 볼륨 설정, 로그인/로그아웃, 계정 정보 표시 담당
    /// </summary>
    public class AccountUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject accountUIPanel; // 본인 패널 참조

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;  // MainSlider
        [SerializeField] private Slider bgmVolumeSlider;     // BGMSlider
        [SerializeField] private Slider sfxVolumeSlider;     // SoundEffectSlider - 일반 효과음
        [SerializeField] private Slider voiceVolumeSlider;   // SoundEffectSlider - 캐릭터 대사
        [SerializeField] private Slider skillVolumeSlider;   // SoundEffectSlider - 스킬 효과음

        [Header("Account Info")]
        [SerializeField] private TextMeshProUGUI accountText; // 로그인 상태 표시
        [SerializeField] private TextMeshProUGUI uidText;     // 유저 ID 표시

        private bool isInitializingSliders = false;

        private void OnEnable()
        {
            // 슬라이더 값을 AudioManager의 현재 볼륨 값으로 동기화
            SyncSlidersWithAudioManager();

            // 계정 정보 표시
            RefreshAccountInfo();
        }

        #region Slider Sync

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

        #endregion

        #region Volume Slider Callbacks

        /// <summary>
        /// 마스터 볼륨 슬라이더 값 변경 시 호출
        /// Inspector에서 MainSlider의 OnValueChanged에 연결
        /// </summary>
        public void OnMasterVolumeChanged(float value)
        {
            if (isInitializingSliders) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
        }

        /// <summary>
        /// BGM 볼륨 슬라이더 값 변경 시 호출
        /// Inspector에서 BGMSlider의 OnValueChanged에 연결
        /// </summary>
        public void OnBGMVolumeChanged(float value)
        {
            if (isInitializingSliders) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value);
            }
        }

        /// <summary>
        /// SFX 볼륨 슬라이더 값 변경 시 호출 (일반 효과음)
        /// Inspector에서 SFX Slider의 OnValueChanged에 연결
        /// </summary>
        public void OnSFXVolumeChanged(float value)
        {
            if (isInitializingSliders) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
        }

        /// <summary>
        /// Voice 볼륨 슬라이더 값 변경 시 호출 (캐릭터 대사)
        /// Inspector에서 Voice Slider의 OnValueChanged에 연결
        /// </summary>
        public void OnVoiceVolumeChanged(float value)
        {
            if (isInitializingSliders) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetVoiceVolume(value);
            }
        }

        /// <summary>
        /// Skill 볼륨 슬라이더 값 변경 시 호출 (스킬 효과음)
        /// Inspector에서 Skill Slider의 OnValueChanged에 연결
        /// </summary>
        public void OnSkillVolumeChanged(float value)
        {
            if (isInitializingSliders) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSkillVolume(value);
            }
        }

        #endregion

        #region Account Info

        /// <summary>
        /// 계정 정보 갱신
        /// </summary>
        private void RefreshAccountInfo()
        {
            var currentUser = FirebaseAuth.DefaultInstance?.CurrentUser;

            if (currentUser == null)
            {
                if (accountText != null)
                    accountText.text = "Not Logged In";
                if (uidText != null)
                    uidText.text = "UID: -";
                return;
            }

            // UID 표시
            if (uidText != null)
            {
                uidText.text = $"UID: {currentUser.UserId}";
            }

            // 계정 상태 표시 (로그인 방식)
            if (accountText != null)
            {
                if (currentUser.IsAnonymous)
                {
                    accountText.text = "익명 로그인";
                }
                else
                {
                    // Google 로그인 - 이메일 표시
                    string email = currentUser.Email;
                    accountText.text = string.IsNullOrEmpty(email) ? "Google 로그인" : email;
                }
            }
        }

        #endregion

        #region Button Callbacks

        /// <summary>
        /// 닫기 버튼 클릭 시 패널 비활성화
        /// Inspector에서 CloseButton의 OnClick에 연결
        /// </summary>
        public void OnCloseButton()
        {
            // 볼륨 설정 저장
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveAudioSettings();
            }

            if (accountUIPanel != null)
            {
                accountUIPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 로그인 버튼 클릭 시 호출
        /// 익명 → 구글 계정 연결, 이미 구글 → 경고 표시
        /// Inspector에서 LoginButton의 OnClick에 연결
        /// </summary>
        public void OnLoginButton()
        {
            OnLoginButtonAsync().Forget();
        }

        private async UniTaskVoid OnLoginButtonAsync()
        {
            var currentUser = FirebaseAuth.DefaultInstance?.CurrentUser;

            if (currentUser == null)
            {
                WarningUIManager.Instance?.ShowWarning("로그인 정보가 없습니다.");
                return;
            }

            if (!currentUser.IsAnonymous)
            {
                // 이미 구글 로그인 상태
                WarningUIManager.Instance?.ShowWarning("이미 Google 계정으로 로그인되어 있습니다.");
                return;
            }

            // 익명 → 구글 계정 연결
            await LinkWithGoogleAsync();
        }

        /// <summary>
        /// 익명 계정을 구글 계정에 연결
        /// </summary>
        private async UniTask LinkWithGoogleAsync()
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                // Google Sign-In 설정
                GoogleSignIn.Configuration = new GoogleSignInConfiguration
                {
                    WebClientId = "192924105425-bvrka8999d7b243ue4vd0s5mscenfob2.apps.googleusercontent.com",
                    RequestIdToken = true,
                    UseGameSignIn = false,
                    RequestEmail = true
                };

                // Google Sign-In 실행
                var signInTask = GoogleSignIn.DefaultInstance.SignIn();
                var googleUser = await signInTask.AsUniTask();

                if (googleUser == null || string.IsNullOrEmpty(googleUser.IdToken))
                {
                    WarningUIManager.Instance?.ShowWarning("Google 로그인에 실패했습니다.");
                    return;
                }

                // Firebase Credential 생성
                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

                // 현재 익명 계정에 구글 계정 연결
                var currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
                await currentUser.LinkWithCredentialAsync(credential);

                GameLog.Log($"[AccountUI] 계정 연결 성공! Email: {googleUser.Email}");
                WarningUIManager.Instance?.ShowWarning("Google 계정 연결 완료!");

                // UI 갱신
                RefreshAccountInfo();
            }
            catch (System.AggregateException ae)
            {
                // 이미 다른 계정에 연결된 경우 등
                GameLog.LogError($"[AccountUI] 계정 연결 실패: {ae.Flatten().InnerException?.Message ?? ae.Message}");
                WarningUIManager.Instance?.ShowWarning("계정 연결에 실패했습니다.");
            }
            catch (System.Exception e)
            {
                GameLog.LogError($"[AccountUI] 계정 연결 실패: {e.Message}");
                WarningUIManager.Instance?.ShowWarning("계정 연결에 실패했습니다.");
            }
#else
            WarningUIManager.Instance?.ShowWarning("모바일에서만 지원됩니다.");
#endif
        }

        /// <summary>
        /// 게임 종료 버튼 클릭 시 호출
        /// Inspector에서 Exit Game Button의 OnClick에 연결
        /// </summary>
        public void OnExitGameButton()
        {
            // 볼륨 설정 저장
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveAudioSettings();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 로그아웃 버튼 클릭 시 호출
        /// Inspector에서 Logout Button의 OnClick에 연결
        /// </summary>
        public void OnLogoutButton()
        {
            OnLogoutButtonAsync().Forget();
        }

        private async UniTaskVoid OnLogoutButtonAsync()
        {
            // 볼륨 설정 저장
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SaveAudioSettings();
            }

            // Firebase 로그아웃
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.SignOut();
            }

            GameLog.Log("[AccountUI] 로그아웃 완료, 타이틀 씬으로 이동");

            // 타이틀 씬으로 이동
            await LoadSceneWithFade(SceneName.TitleScene);
        }

        /// <summary>
        /// 계정 삭제 버튼 클릭 시 호출
        /// Inspector에서 Delete Account Button의 OnClick에 연결
        /// </summary>
        public void OnDeleteAccountButton()
        {
            OnDeleteAccountButtonAsync().Forget();
        }

        private async UniTaskVoid OnDeleteAccountButtonAsync()
        {
            var currentUser = FirebaseAuth.DefaultInstance?.CurrentUser;

            if (currentUser == null)
            {
                WarningUIManager.Instance?.ShowWarning("삭제할 계정이 없습니다.");
                return;
            }

            // 확인 팝업 표시
            if (WarningUIManager.Instance == null)
            {
                GameLog.LogWarning("[AccountUI] WarningUIManager not found");
                return;
            }

            bool confirmed = await WarningUIManager.Instance.ShowConfirmAsync("정말 계정을 삭제하시겠습니까?\n삭제된 계정은 복구할 수 없습니다.");

            if (!confirmed)
            {
                GameLog.Log("[AccountUI] 계정 삭제 취소됨");
                return;
            }

            try
            {
                // 볼륨 설정 저장
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SaveAudioSettings();
                }

                // Firebase 유저 삭제
                await currentUser.DeleteAsync();

                GameLog.Log("[AccountUI] 계정 삭제 완료, 타이틀 씬으로 이동");
                WarningUIManager.Instance?.ShowWarning("계정이 삭제되었습니다.");

                // 타이틀 씬으로 이동
                await LoadSceneWithFade(SceneName.TitleScene);
            }
            catch (System.Exception e)
            {
                GameLog.LogError($"[AccountUI] 계정 삭제 실패: {e.Message}");
                WarningUIManager.Instance?.ShowWarning("계정 삭제에 실패했습니다.");
            }
        }

        #endregion

        #region Scene Transition

        /// <summary>
        /// 페이드 효과와 함께 씬 전환
        /// </summary>
        private async UniTask LoadSceneWithFade(string sceneName)
        {
            if (FadeController.Instance == null)
            {
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                return;
            }

            // 페이드 아웃
            FadeController.Instance.fadePanel.SetActive(true);
            await FadeController.Instance.FadeOut(0.5f);

            // 씬 로드
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            // 페이드 인
            await FadeController.Instance.FadeIn(0.5f);
            FadeController.Instance.fadePanel.SetActive(false);
        }

        #endregion
    }
}
