using UnityEngine;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

/// <summary>
/// Firebase 초기화 및 인증 관리
/// TitleScene에서 생성되어 DontDestroyOnLoad로 유지
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    private const string LOG_PREFIX = "<color=#3EB489>[Firebase]</color>";

    public static FirebaseManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    public bool IsInitialized { get; private set; }
    public bool IsSignedIn => currentUser != null;
    public string CurrentUserId => currentUser?.UserId;

    // GPGS 관련
    private bool isGPGSInitialized = false;
    public bool IsGPGSInitialized => isGPGSInitialized;

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
            Debug.Log($"{LOG_PREFIX} 이미 초기화됨");
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
                        Debug.Log($"{LOG_PREFIX} 자동 로그인 성공! UserId: {currentUser.UserId}");
                    }
                    catch (Exception)
                    {
                        // 서버에서 유저가 삭제됨 - 로컬 캐시 정리
                        Debug.LogWarning($"{LOG_PREFIX} 기존 유저가 서버에서 삭제됨. 로컬 캐시 정리.");
                        auth.SignOut();
                        currentUser = null;
                    }
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX} 초기화 완료. 로그인된 유저 없음.");
                }

                return true;
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} Firebase 의존성 해결 실패: {dependencyStatus}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화 실패: {e.Message}");
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
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않음. InitializeAsync를 먼저 호출하세요.");
            return null;
        }

        try
        {
            var authResult = await auth.SignInAnonymouslyAsync();
            currentUser = authResult.User;

            Debug.Log($"{LOG_PREFIX} 익명 로그인 성공! UserId: {currentUser.UserId}");
            return currentUser.UserId;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 익명 로그인 실패: {e.Message}");
            return null;
        }
    }

    #region Google Play Games Services

    /// <summary>
    /// Google Play Games Services 초기화
    /// </summary>
    public void InitializeGPGS()
    {
        if (isGPGSInitialized)
        {
            Debug.Log($"{LOG_PREFIX} GPGS 이미 초기화됨");
            return;
        }

#if UNITY_ANDROID
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        isGPGSInitialized = true;
        Debug.Log($"{LOG_PREFIX} GPGS 초기화 완료");
#else
        Debug.LogWarning($"{LOG_PREFIX} GPGS는 Android에서만 지원됩니다.");
#endif
    }

    /// <summary>
    /// 구글 로그인 (GPGS + Firebase)
    /// </summary>
    /// <returns>성공 시 UserId, 실패 시 null</returns>
    public async UniTask<string> SignInWithGoogleAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} Firebase 초기화 필요. InitializeAsync를 먼저 호출하세요.");
            return null;
        }

#if UNITY_ANDROID
        // 1. GPGS 초기화
        InitializeGPGS();

        try
        {
            // 2. GPGS 로그인 + Server Auth Code 요청
            string serverAuthCode = await AuthenticateWithGPGSAsync();

            if (string.IsNullOrEmpty(serverAuthCode))
            {
                Debug.LogError($"{LOG_PREFIX} GPGS Server Auth Code 획득 실패");
                return null;
            }

            Debug.Log($"{LOG_PREFIX} GPGS 로그인 성공! Server Auth Code 획득됨");

            // 3. Firebase Credential 생성 및 로그인
            Credential credential = PlayGamesAuthProvider.GetCredential(serverAuthCode);
            currentUser = await auth.SignInWithCredentialAsync(credential);

            Debug.Log($"{LOG_PREFIX} 구글 로그인 성공! UserId: {currentUser.UserId}");
            return currentUser.UserId;
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 구글 로그인 실패: {e.Message}");
            return null;
        }
#else
        Debug.LogWarning($"{LOG_PREFIX} 구글 로그인은 Android에서만 지원됩니다.");
        return null;
#endif
    }

#if UNITY_ANDROID
    /// <summary>
    /// GPGS 인증 및 Server Auth Code 획득
    /// 항상 계정 선택 UI를 표시하기 위해 ManuallyAuthenticate 사용
    /// </summary>
    private UniTask<string> AuthenticateWithGPGSAsync()
    {
        var tcs = new UniTaskCompletionSource<string>();
        ShowToast("구글 계정 선택 UI 표시 중...");

        // 항상 수동 로그인 (계정 선택 UI 표시)
        PlayGamesPlatform.Instance.ManuallyAuthenticate((status) =>
        {
            Debug.Log($"{LOG_PREFIX} GPGS ManuallyAuthenticate 결과: {status}");
            ShowToast($"ManuallyAuth: {status}");

            if (status == SignInStatus.Success)
            {
                RequestServerAuthCode(tcs);
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX} GPGS 인증 실패: {status}");
                ShowToast($"GPGS 실패: {status}");
                tcs.TrySetResult(null);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    /// Server Auth Code 요청
    /// </summary>
    private void RequestServerAuthCode(UniTaskCompletionSource<string> tcs)
    {
        Debug.Log($"{LOG_PREFIX} GPGS 인증 성공! Server Auth Code 요청 중...");
        ShowToast("Server Auth Code 요청 중...");

        PlayGamesPlatform.Instance.RequestServerSideAccess(
            forceRefreshToken: true,
            (authCode) =>
            {
                if (!string.IsNullOrEmpty(authCode))
                {
                    Debug.Log($"{LOG_PREFIX} Server Auth Code 획득 성공");
                    ShowToast("Server Auth Code 획득 성공!");
                    tcs.TrySetResult(authCode);
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Server Auth Code가 비어있음");
                    ShowToast("Server Auth Code 실패 - 비어있음");
                    tcs.TrySetResult(null);
                }
            });
    }

    /// <summary>
    /// Android 토스트 메시지 표시 (디버그용)
    /// </summary>
    private void ShowToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
            {
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using (var toast = toastClass.CallStatic<AndroidJavaObject>("makeText", activity, message, 0))
                    {
                        toast.Call("show");
                    }
                }));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} Toast 실패: {e.Message}");
        }
#else
        Debug.Log($"{LOG_PREFIX} [Toast] {message}");
#endif
    }
#endif

    #endregion

    /// <summary>
    /// 로그아웃
    /// </summary>
    public void SignOut()
    {
        if (!IsInitialized || auth == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 초기화되지 않음.");
            return;
        }

        string userId = currentUser?.UserId ?? "Unknown";
        auth.SignOut();
        currentUser = null;
        Debug.Log($"{LOG_PREFIX} 로그아웃 완료. UserId: {userId}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
