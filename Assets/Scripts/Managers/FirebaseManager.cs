using UnityEngine;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using System;

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
