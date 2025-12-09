using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

/// <summary>
/// TitleScene UI 컨트롤러
/// 게임 시작 시 Firebase 자동 초기화 + 자동 로그인 체크
/// 버튼 클릭 시 BootScene으로 이동
/// </summary>
public class TitleSceneController : MonoBehaviour
{
    private const string LOG_PREFIX = "<color=#3EB489>[Firebase]</color>";
    private const string BOOT_SCENE_NAME = "BootScene";

    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject pressText;

    private bool isProcessing;
    private bool isLoginButtonsShown;

    private async void Start()
    {
        await InitializeFirebaseAsync();
    }

    /// <summary>
    /// 게임 시작 시 Firebase 초기화 + 자동 로그인 체크
    /// </summary>
    private async UniTask InitializeFirebaseAsync()
    {
        // 1. FirebaseManager가 없으면 생성
        if (FirebaseManager.Instance == null)
        {
            var go = new GameObject("FirebaseManager");
            go.AddComponent<FirebaseManager>();
        }

        // 2. Firebase 초기화
        Debug.Log($"{LOG_PREFIX} Firebase 초기화 중...");
        bool initialized = await FirebaseManager.Instance.InitializeAsync();

        if (!initialized)
        {
            Debug.LogError($"{LOG_PREFIX} Firebase 초기화 실패!");
            return;
        }

        // 3. 자동 로그인 여부 확인
        if (FirebaseManager.Instance.IsSignedIn)
        {
            Debug.Log($"{LOG_PREFIX} 자동 로그인 완료! UserId: {FirebaseManager.Instance.CurrentUserId}");
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} 로그인 필요. 버튼을 눌러주세요.");
        }
    }

    /// <summary>
    /// 로그인 Button OnClick 이벤트에서 호출
    /// </summary>
    public void OnStartButtonClicked()
    {
        if (isProcessing) return;

        ProcessLoginAsync().Forget();
    }

    /// <summary>
    /// 로그아웃 Button OnClick 이벤트에서 호출
    /// </summary>
    public void OnLogoutButtonClicked()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsSignedIn)
        {
            Debug.LogWarning($"{LOG_PREFIX} 로그인 되지 않음.");
            return;
        }

        FirebaseManager.Instance.SignOut();
        isProcessing = false;
    }

    private async UniTaskVoid ProcessLoginAsync()
    {
        isProcessing = true;

        try
        {
            // 이미 로그인되어 있는지 확인
            if (FirebaseManager.Instance.IsSignedIn)
            {
                Debug.Log($"{LOG_PREFIX} 이미 로그인됨! UserId: {FirebaseManager.Instance.CurrentUserId}");
                Debug.Log($"{LOG_PREFIX} BootScene으로 이동합니다...");
                LoadBootScene();
                return;
            }

            // 익명 로그인
            Debug.Log($"{LOG_PREFIX} 익명 로그인 시도 중...");
            string userId = await FirebaseManager.Instance.SignInAnonymouslyAsync();

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"{LOG_PREFIX} 익명 로그인 실패!");
                isProcessing = false;
                return;
            }

            Debug.Log($"{LOG_PREFIX} 로그인 성공! UserId: {userId}");
            Debug.Log($"{LOG_PREFIX} BootScene으로 이동합니다...");
            LoadBootScene();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 에러: {e.Message}");
            isProcessing = false;
        }
    }

    /// <summary>
    /// BootScene으로 씬 전환
    /// </summary>
    private void LoadBootScene()
    {
        SceneManager.LoadScene(BOOT_SCENE_NAME);
    }

    /// <summary>
    /// Background 터치 시 호출 - 로그인 상태 확인 후 분기
    /// </summary>
    public void OnBackgroundClicked()
    {
        if (isLoginButtonsShown || isProcessing) return;

        // 이미 로그인된 상태면 바로 BootScene으로
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsSignedIn)
        {
            Debug.Log($"{LOG_PREFIX} 이미 로그인됨! BootScene으로 이동합니다...");
            LoadBootScene();
            return;
        }

        // 로그인 안 된 상태면 LoginPanel 표시
        isLoginButtonsShown = true;

        if (loginPanel != null)
            loginPanel.SetActive(true);

        if (pressText != null)
            pressText.SetActive(false);

        Debug.Log($"{LOG_PREFIX} 로그인 필요 - 로그인 패널 표시");
    }
}
