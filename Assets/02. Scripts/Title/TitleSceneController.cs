using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NovelianMagicLibraryDefense.Managers;

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
    [SerializeField] private RectTransform titleText;

    [Header("Animation Settings")]
    [SerializeField] private float titleFloatDistance = 20f;
    [SerializeField] private float titleFloatDuration = 2f;
    [SerializeField] private float titleFadeInDuration = 1f;
    [SerializeField] private float pressTextFadeDuration = 1f;
    [SerializeField] private float pressTextPulseScale = 1.1f;
    [SerializeField] private float pressTextPulseDuration = 0.8f;

    [Header("Glow Settings")]
    [SerializeField] private float glowMinIntensity = 0.5f;
    [SerializeField] private float glowMaxIntensity = 1.5f;
    [SerializeField] private float glowDuration = 1.5f;

    private bool isProcessing;
    private bool isLoginButtonsShown;
    private Image pressTextImage;
    private Image titleTextImage;
    private Vector2 titleOriginalPosition;
    private Sequence pressTextSequence;
    private Material titleMaterial;
    private Tweener titleFloatTween;
    private Tweener titleFadeTween;
    private Tweener glowTween;
    private Tweener pressTextFadeTween;
    private Tweener pressTextScaleTween;
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");

    private async void Start()
    {
        // BGM 재생 (씬 시작과 동시에)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGMWithFade("BGM_Title", 1f);
        }

        InitializeAnimations();
        PlayTitleAnimations();
        await InitializeFirebaseAsync();
    }

    private void OnDestroy()
    {
        // 모든 Tween 정리
        titleFloatTween?.Kill();
        titleFadeTween?.Kill();
        glowTween?.Kill();
        pressTextSequence?.Kill();
        pressTextFadeTween?.Kill();
        pressTextScaleTween?.Kill();

        // Material 인스턴스 정리
        if (titleMaterial != null)
        {
            Destroy(titleMaterial);
        }
    }

    /// <summary>
    /// 애니메이션 초기화
    /// </summary>
    private void InitializeAnimations()
    {
        if (titleText != null)
        {
            titleTextImage = titleText.GetComponent<Image>();
            titleOriginalPosition = titleText.anchoredPosition;

            if (titleTextImage != null)
            {
                var color = titleTextImage.color;
                color.a = 0f;
                titleTextImage.color = color;
            }
        }

        if (pressText != null)
        {
            pressTextImage = pressText.GetComponent<Image>();

            if (pressTextImage != null)
            {
                var color = pressTextImage.color;
                color.a = 0f;
                pressTextImage.color = color;
            }
        }
    }

    /// <summary>
    /// 타이틀 애니메이션 재생
    /// </summary>
    private void PlayTitleAnimations()
    {
        // TitleText 페이드인 + 플로팅 애니메이션
        if (titleTextImage != null && titleText != null)
        {
            // 페이드인
            titleFadeTween = titleTextImage.DOFade(1f, titleFadeInDuration).SetEase(Ease.OutQuad);

            // 플로팅 (위아래로 떠다니는 효과) - Yoyo 루프로 자연스럽게
            titleFloatTween = titleText.DOAnchorPosY(titleOriginalPosition.y + titleFloatDistance, titleFloatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // Glow 애니메이션 (Material이 UIGlow Shader를 사용하는 경우)
            if (titleTextImage.material != null && titleTextImage.material.HasProperty(GlowIntensityID))
            {
                // Material 인스턴스 생성 (원본 변경 방지)
                titleMaterial = Instantiate(titleTextImage.material);
                titleTextImage.material = titleMaterial;

                // Glow Intensity 애니메이션
                titleMaterial.SetFloat(GlowIntensityID, glowMinIntensity);
                glowTween = DOTween.To(
                    () => titleMaterial.GetFloat(GlowIntensityID),
                    x => titleMaterial.SetFloat(GlowIntensityID, x),
                    glowMaxIntensity,
                    glowDuration
                ).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
        }

        // PressText 페이드인 후 깜빡임 + 펄스 애니메이션
        if (pressTextImage != null && pressText != null)
        {
            pressTextSequence = DOTween.Sequence();

            // 1초 후 페이드인 시작
            pressTextSequence
                .AppendInterval(titleFadeInDuration)
                .Append(pressTextImage.DOFade(1f, pressTextFadeDuration).SetEase(Ease.OutQuad))
                .AppendCallback(() => StartPressTextLoopAnimation());
        }
    }

    /// <summary>
    /// PressText 루프 애니메이션 (깜빡임 + 펄스)
    /// </summary>
    private void StartPressTextLoopAnimation()
    {
        if (pressTextImage == null || pressText == null) return;

        var rectTransform = pressText.GetComponent<RectTransform>();

        // 깜빡임 (페이드 인/아웃 반복)
        pressTextFadeTween = pressTextImage.DOFade(0.3f, pressTextFadeDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 펄스 (스케일 반복)
        if (rectTransform != null)
        {
            pressTextScaleTween = rectTransform.DOScale(pressTextPulseScale, pressTextPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
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
    /// 게스트 로그인 Button OnClick 이벤트에서 호출
    /// </summary>
    public void OnStartButtonClicked()
    {
        if (isProcessing) return;

        ProcessLoginAsync().Forget();
    }

    /// <summary>
    /// 구글 로그인 Button OnClick 이벤트에서 호출
    /// </summary>
    public void OnGoogleLoginButtonClicked()
    {
        if (isProcessing) return;

        ProcessGoogleLoginAsync().Forget();
    }

    /// <summary>
    /// 구글 로그인 처리
    /// </summary>
    private async UniTaskVoid ProcessGoogleLoginAsync()
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

            // 구글 로그인
            Debug.Log($"{LOG_PREFIX} 구글 로그인 시도 중...");
            string userId = await FirebaseManager.Instance.SignInWithGoogleAsync();

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"{LOG_PREFIX} 구글 로그인 실패!");
                isProcessing = false;
                return;
            }

            Debug.Log($"{LOG_PREFIX} 구글 로그인 성공! UserId: {userId}");
            Debug.Log($"{LOG_PREFIX} BootScene으로 이동합니다...");
            LoadBootScene();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 구글 로그인 에러: {e.Message}");
            isProcessing = false;
        }
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
