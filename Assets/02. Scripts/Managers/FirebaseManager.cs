using UnityEngine;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using System;
using Google;

/// <summary>
/// Firebase 초기화 및 인증 관리
/// TitleScene에서 생성되어 DontDestroyOnLoad로 유지
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    private const string LOG_PREFIX = "<color=#3EB489>[Firebase]</color>";

    // Google Cloud Console에서 생성한 Web Client ID
    private const string WEB_CLIENT_ID = "192924105425-bvrka8999d7b243ue4vd0s5mscenfob2.apps.googleusercontent.com";

    public static FirebaseManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    public bool IsInitialized { get; private set; }
    public bool IsSignedIn => currentUser != null;
    public string CurrentUserId => currentUser?.UserId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Firebase 초기화
    /// </summary>
    public async UniTask<bool> InitializeAsync()
    {
        if (IsInitialized)
        {
            GameLog.Log($"{LOG_PREFIX} 이미 초기화됨");
            return true;
        }

        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                IsInitialized = true;

                // 이미 로그인된 유저가 있는지 확인 (자동 로그인)
                if (auth.CurrentUser != null)
                {
                    try
                    {
                        // 서버에서 유저 정보 유효성 확인
                        await auth.CurrentUser.ReloadAsync();
                        currentUser = auth.CurrentUser;
                        GameLog.Log($"{LOG_PREFIX} 자동 로그인 성공! UserId: {currentUser.UserId}");
                    }
                    catch (Exception)
                    {
                        // 서버에서 유저가 삭제됨 - 로컬 캐시 정리
                        GameLog.LogWarning($"{LOG_PREFIX} 기존 유저가 서버에서 삭제됨. 로컬 캐시 정리.");
                        auth.SignOut();
                        currentUser = null;
                    }
                }
                else
                {
                    GameLog.Log($"{LOG_PREFIX} 초기화 완료. 로그인된 유저 없음.");
                }

                return true;
            }
            else
            {
                GameLog.LogError($"{LOG_PREFIX} Firebase 의존성 해결 실패: {dependencyStatus}");
                return false;
            }
        }
        catch (Exception e)
        {
            GameLog.LogError($"{LOG_PREFIX} 초기화 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 익명 로그인
    /// </summary>
    /// <returns>성공 시 UserId, 실패 시 null</returns>
    public async UniTask<string> SignInAnonymouslyAsync()
    {
        if (!IsInitialized)
        {
            GameLog.LogError($"{LOG_PREFIX} 초기화되지 않음. InitializeAsync를 먼저 호출하세요.");
            return null;
        }

        try
        {
            var authResult = await auth.SignInAnonymouslyAsync();
            currentUser = authResult.User;

            GameLog.Log($"{LOG_PREFIX} 익명 로그인 성공! UserId: {currentUser.UserId}");
            return currentUser.UserId;
        }
        catch (Exception e)
        {
            GameLog.LogError($"{LOG_PREFIX} 익명 로그인 실패: {e.Message}");
            return null;
        }
    }

    #region Google Sign-In

    /// <summary>
    /// 구글 로그인 (Google Sign-In + Firebase)
    /// 계정 선택 UI가 표시됨
    /// </summary>
    /// <returns>성공 시 UserId, 실패 시 null</returns>
    public async UniTask<string> SignInWithGoogleAsync()
    {
        if (!IsInitialized)
        {
            GameLog.LogError($"{LOG_PREFIX} Firebase 초기화 필요. InitializeAsync를 먼저 호출하세요.");
            return null;
        }

#if UNITY_ANDROID || UNITY_IOS
        try
        {
            // 1. Google Sign-In 설정
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = WEB_CLIENT_ID,
                RequestIdToken = true,
                UseGameSignIn = false,
                RequestEmail = true
            };

            // 2. Google Sign-In 실행 (계정 선택 UI 표시)
            var signInTask = GoogleSignIn.DefaultInstance.SignIn();
            var googleUser = await signInTask.AsUniTask();

            if (googleUser == null)
            {
                GameLog.LogError($"{LOG_PREFIX} Google Sign-In 실패: 사용자 정보 없음");
                return null;
            }

            string idToken = googleUser.IdToken;
            if (string.IsNullOrEmpty(idToken))
            {
                GameLog.LogError($"{LOG_PREFIX} Google Sign-In 실패: ID Token 없음");
                return null;
            }

            GameLog.Log($"{LOG_PREFIX} Google Sign-In 성공! Email: {googleUser.Email}");

            // 3. Firebase Credential 생성 및 로그인
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
            currentUser = await auth.SignInWithCredentialAsync(credential);

            GameLog.Log($"{LOG_PREFIX} 구글 로그인 성공! UserId: {currentUser.UserId}");
            return currentUser.UserId;
        }
        catch (GoogleSignIn.SignInException e)
        {
            GameLog.LogError($"{LOG_PREFIX} Google Sign-In 실패: {e.Status} - {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            GameLog.LogError($"{LOG_PREFIX} 구글 로그인 실패: {e.Message}");
            return null;
        }
#else
        GameLog.LogWarning($"{LOG_PREFIX} 구글 로그인은 Android/iOS에서만 지원됩니다.");
        return null;
#endif
    }

    #endregion

    /// <summary>
    /// 로그아웃
    /// </summary>
    public void SignOut()
    {
        if (!IsInitialized || auth == null)
        {
            GameLog.LogWarning($"{LOG_PREFIX} 초기화되지 않음.");
            return;
        }

        string userId = currentUser?.UserId ?? "Unknown";

        // Google Sign-In 로그아웃
#if UNITY_ANDROID || UNITY_IOS
        try
        {
            GoogleSignIn.DefaultInstance.SignOut();
        }
        catch (Exception e)
        {
            GameLog.LogWarning($"{LOG_PREFIX} Google SignOut 실패: {e.Message}");
        }
#endif

        // Firebase 로그아웃
        auth.SignOut();
        currentUser = null;
        GameLog.Log($"{LOG_PREFIX} 로그아웃 완료. UserId: {userId}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
