using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.UI
{
public class TabButton : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button characterTabButton;
    [SerializeField] private Button partyTabButton;
    [SerializeField] private Button teamSetupTabButton;

    [Header("Panels")]
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject teamSetupPanel;

    [Header("Debug Settings")]
    [SerializeField] private bool enableUIDebug = true;

    // CBL: 파견 빈 슬롯에서 덱 설정으로 바로 이동 시 사용
    private const string PREF_KEY_OPEN_TEAM_SETUP = "ShouldOpenTeamSetup";
    private bool shouldOpenTeamSetupOnStart = false; // Start()에서 사용할 플래그
    private CancellationTokenSource cts; // 비동기 작업 취소용

    public static bool ShouldOpenTeamSetup
    {
        get => PlayerPrefs.GetInt(PREF_KEY_OPEN_TEAM_SETUP, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(PREF_KEY_OPEN_TEAM_SETUP, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[TabButton] ShouldOpenTeamSetup 설정됨: {value}");
        }
    }

    private void Awake()
    {
        // CBL: 파견에서 바로 덱 설정 탭으로 이동하는 경우
        // Awake에서 플래그 확인 후 인스턴스 변수에 저장 (Start에서 사용)
        shouldOpenTeamSetupOnStart = ShouldOpenTeamSetup;
        Debug.Log($"[TabButton] Awake - ShouldOpenTeamSetup 플래그 확인: {shouldOpenTeamSetupOnStart}, InstanceID: {GetInstanceID()}");

        if (shouldOpenTeamSetupOnStart)
        {
            // 플래그를 여기서 초기화하지 않음 - ForceOpenTeamSetupDelayed 완료 후 초기화
            // 씬이 여러 번 로드되는 경우를 대비

            // CBL: Awake에서 즉시 패널 상태 설정 (가장 빠른 시점)
            if (characterPanel != null) characterPanel.SetActive(false);
            if (partyPanel != null) partyPanel.SetActive(false);
            if (teamSetupPanel != null) teamSetupPanel.SetActive(true);
            Debug.Log("[TabButton] Awake - 덱 설정 패널 즉시 활성화");
        }
    }

    private void Start()
    {
        // CancellationToken 초기화
        cts = new CancellationTokenSource();

        // CharacterManagement BGM 재생 (크로스페이드)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.CrossfadeBGM("BGM_CharacterManagement", 1f);
        }

        characterTabButton.onClick.AddListener(OnCharacterTabClicked);
        partyTabButton.onClick.AddListener(OnPartyTabClicked);
        teamSetupTabButton.onClick.AddListener(OnTeamSetupTabClicked);

        // CBL: 파견에서 덱 설정으로 바로 이동하는 경우
        // 다음 프레임에서 강제로 덱 설정 탭 전환 (씬 로드 완료 후 실행)
        if (shouldOpenTeamSetupOnStart)
        {
            ForceOpenTeamSetupDelayed(cts.Token).Forget();
        }

        // UI 클릭 문제 디버깅
        if (enableUIDebug)
        {
            CheckUIClickIssuesAsync(cts.Token).Forget();
        }
    }

    /// <summary>
    /// CBL: 파견에서 이동 시 덱 설정 탭으로 강제 전환 (페이드 완료 후 + 여러 프레임 유지)
    /// </summary>
    private async UniTaskVoid ForceOpenTeamSetupDelayed(CancellationToken token)
    {
        Debug.Log("[TabButton] ForceOpenTeamSetupDelayed - 시작");

        try
        {
            // FadeController의 페이드인이 완료될 때까지 대기 (약 0.5초)
            if (FadeController.Instance != null && FadeController.Instance.fadePanel != null)
            {
                // 페이드 패널이 비활성화될 때까지 대기
                await UniTask.WaitUntil(() =>
                    !FadeController.Instance.fadePanel.activeSelf ||
                    (FadeController.Instance.fadeImage != null && FadeController.Instance.fadeImage.color.a < 0.1f),
                    cancellationToken: token);
                Debug.Log("[TabButton] ForceOpenTeamSetupDelayed - 페이드 완료 감지");
            }

            // 페이드 완료 후 추가 대기
            await UniTask.Delay(100, cancellationToken: token);

            // 10프레임 동안 매 프레임마다 덱 설정 탭 강제 유지
            for (int i = 0; i < 10; i++)
            {
                token.ThrowIfCancellationRequested();

                // 매 프레임마다 패널 상태 강제 설정
                if (characterPanel != null) characterPanel.SetActive(false);
                if (partyPanel != null) partyPanel.SetActive(false);
                if (teamSetupPanel != null) teamSetupPanel.SetActive(true);

                if (characterTabButton != null) characterTabButton.interactable = true;
                if (partyTabButton != null) partyTabButton.interactable = true;
                if (teamSetupTabButton != null) teamSetupTabButton.interactable = false;

                if (i == 0 || i == 9)
                {
                    Debug.Log($"[TabButton] ForceOpenTeamSetupDelayed - 프레임 {i + 1}/10 패널 강제 설정");
                }

                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            }

            shouldOpenTeamSetupOnStart = false;
            // 플래그 초기화는 ForceOpenTeamSetupDelayed가 성공적으로 완료된 후에만 수행
            ShouldOpenTeamSetup = false;
            Debug.Log("[TabButton] ForceOpenTeamSetupDelayed - 완료, 플래그 초기화됨");
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[TabButton] ForceOpenTeamSetupDelayed - 취소됨 (플래그 유지)");
        }
    }

    /// <summary>
    /// UI 클릭 문제 원인 체크 (비동기로 1초 후 실행)
    /// </summary>
    private async UniTaskVoid CheckUIClickIssuesAsync(CancellationToken token)
    {
        try
        {
            // 씬 로드 후 안정화 대기
            await UniTask.Delay(1000, cancellationToken: token);

            Debug.Log("========== [UIClickDebug] LibraryManagementScene 상태 체크 ==========");

            // 1. FadeController 체크
            if (FadeController.Instance != null && FadeController.Instance.fadePanel != null)
            {
                bool panelActive = FadeController.Instance.fadePanel.activeSelf;
                float alpha = FadeController.Instance.fadeImage != null ? FadeController.Instance.fadeImage.color.a : -1;
                bool raycastTarget = FadeController.Instance.fadeImage != null && FadeController.Instance.fadeImage.raycastTarget;

                if (panelActive && raycastTarget)
                {
                    Debug.LogError($"[UIClickDebug] ❌ FadePanel이 UI 클릭을 차단 중! panelActive={panelActive}, alpha={alpha:F2}, raycastTarget={raycastTarget}");
                }
                else
                {
                    Debug.Log($"[UIClickDebug] ✅ FadePanel 정상 (active={panelActive}, alpha={alpha:F2})");
                }
            }

            // 2. EventSystem 체크
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (eventSystems.Length == 0)
            {
                Debug.LogError("[UIClickDebug] ❌ EventSystem이 없습니다!");
            }
            else if (eventSystems.Length > 1)
            {
                Debug.LogWarning($"[UIClickDebug] ⚠️ EventSystem이 {eventSystems.Length}개 존재! (중복 문제 가능)");
                foreach (var es in eventSystems)
                {
                    Debug.LogWarning($"  - {es.gameObject.name} (scene: {es.gameObject.scene.name})");
                }
            }
            else
            {
                Debug.Log($"[UIClickDebug] ✅ EventSystem 정상 ({eventSystems[0].gameObject.name})");
            }

            // 3. RaycastBlocker 체크
            var allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (var img in allImages)
            {
                string name = img.gameObject.name.ToLower();
                if ((name.Contains("raycast") || name.Contains("blocker")) &&
                    img.gameObject.activeInHierarchy && img.raycastTarget)
                {
                    Debug.LogWarning($"[UIClickDebug] ⚠️ RaycastBlocker 활성화: {img.gameObject.name}");
                }
            }

            // 4. CanvasGroup 체크
            var canvasGroups = FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
            foreach (var cg in canvasGroups)
            {
                if (cg.gameObject.activeInHierarchy && !cg.interactable)
                {
                    Debug.LogWarning($"[UIClickDebug] ⚠️ CanvasGroup interactable=false: {cg.gameObject.name}");
                }
            }

            Debug.Log("========== [UIClickDebug] 체크 완료 ==========");
        }
        catch (System.OperationCanceledException)
        {
            // 취소됨 - 무시
        }
    }
    
    private void OnDestroy()
    {
        // 비동기 작업 취소
        cts?.Cancel();
        cts?.Dispose();

        if (characterTabButton != null)
            characterTabButton.onClick.RemoveListener(OnCharacterTabClicked);
        if (partyTabButton != null)
            partyTabButton.onClick.RemoveListener(OnPartyTabClicked);
        if (teamSetupTabButton != null)
            teamSetupTabButton.onClick.RemoveListener(OnTeamSetupTabClicked);
    }

    private void OnCharacterTabClicked()
    {
        Debug.Log("Character Tab Clicked");
        
        characterTabButton.interactable = false;
        partyTabButton.interactable = true;
        teamSetupTabButton.interactable = true;

        characterPanel.SetActive(true);
        partyPanel.SetActive(false);
        teamSetupPanel.SetActive(false);
        
    }
    
    private void OnPartyTabClicked()
    {
        Debug.Log("Party Tab Clicked");

        characterTabButton.interactable = true;
        partyTabButton.interactable = false;
        teamSetupTabButton.interactable = true;

        characterPanel.SetActive(false);
        partyPanel.SetActive(true);
        teamSetupPanel.SetActive(false);
    }
    
    private void OnTeamSetupTabClicked()
    {
        Debug.Log("Team Setup Tab Clicked");

        characterTabButton.interactable = true;
        partyTabButton.interactable = true;
        teamSetupTabButton.interactable = false;

        characterPanel.SetActive(false);
        partyPanel.SetActive(false);
        teamSetupPanel.SetActive(true);
        
    }

    public void LobbyButtonClicked()
    {
        LoadLobbyScene().Forget();
    }

    private async UniTaskVoid LoadLobbyScene()
    {
        await FadeController.Instance.LoadSceneWithFade("LobbyScene");
    }
}
}