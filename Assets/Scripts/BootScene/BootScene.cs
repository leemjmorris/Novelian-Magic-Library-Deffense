using System;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// JML: Boot scene entry point - initializes all DontDestroyOnLoad managers and systems
/// JML: Loads essential data before transitioning to main game
/// </summary>
public class BootScene : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "LobbyScene";
    [SerializeField] private bool showDebugLogs = true;

    [Header("Manager References")]
    [SerializeField] private CSVLoader csvLoader;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private IngredientManager ingredientManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private BookMarkManager bookMarkManager;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private CharacterEnhancementManager characterEnhancementManager;
    [SerializeField] private StageProgressManager stageProgressManager;
    [SerializeField] private CharacterOwnershipManager characterOwnershipManager;

    [SerializeField] private LoadingUIManager loadingUIManager;

    [Header("Loading Progress")]
    [SerializeField] private float minimumLoadTime = 1.0f; // JML: Minimum time to show loading screen

    private async void Start()
    {
        Log("=== Boot Scene Started ===");

        // LCB: 페이드 패널을 검은 화면으로 미리 설정 (튕김 방지)
        if (FadeController.Instance != null)
        {
            FadeController.Instance.fadePanel.SetActive(true);
            FadeController.Instance.fadeImage.color = new Color(0, 0, 0, 1); // 완전히 검은 화면
        }

        float startTime = Time.time;

        // JML: Initialize all systems in parallel where possible
        await InitializeBootSystems();

        // JML: Wait for minimum load time (for UX purposes)
        float elapsed = Time.time - startTime;
        if (elapsed < minimumLoadTime)
        {
            await UniTask.Delay((int)((minimumLoadTime - elapsed) * 1000));
        }

        // JML: Transition to next scene
        await TransitionToNextScene();
    }

    /// <summary>
    /// JML: Initialize all DontDestroyOnLoad systems
    /// JML: CSVLoader first (dependency), then others in PARALLEL for speed
    /// </summary>
    private async UniTask InitializeBootSystems()
    {
        Log("--- Initializing Boot Systems ---");

        // JML: Step 1: FadeController (quick, no dependencies)
        var fadeTask = InitializeFadeController();

        // JML: Step 2: CSVLoader (MUST complete first - others depend on it)
        await InitializeCSVLoader();

        // JML: Step 3: Initialize all other managers in PARALLEL
        await UniTask.WhenAll(
            fadeTask, // JML: Complete fade if not done
            InitializeCurrencyManager(),
            InitializeIngredientManager(),
            InitializeAudioManager(),
            InitializeBookMarkManager(),
            InitializeLoadingUIManager(), // LCB: 로딩 UI 매니저 초기화 (병렬 처리)
            InitializeDeckManager(),
            InitializeCharacterEnhancementManager(),
            InitializeCharacterOwnershipManager()
        );

        Log("--- All Boot Systems Initialized ---");
    }

    private async UniTask InitializeCharacterOwnershipManager()
    {
        Log("Initializing CharacterOwnershipManager...");

        if (characterOwnershipManager == null)
        {
            Debug.LogError("✗ CharacterOwnershipManager reference is NULL! Assign it in Inspector.");
            return;
        }

        // JML: Wait for Awake to complete
        await UniTask.WaitUntil(() => CharacterOwnershipManager.Instance != null);
        await UniTask.DelayFrame(1); // JML: Wait one more frame for Start()

        if (CharacterOwnershipManager.Instance != null)
        {
            Log("✓ CharacterOwnershipManager ready");
        }
        else
        {
            Debug.LogError("✗ CharacterOwnershipManager failed to initialize!");
        }
    }

    /// <summary>
    /// JML: Ensure FadeController exists and is initialized
    /// </summary>
    private async UniTask InitializeFadeController()
    {
        Log("Initializing FadeController...");

        // JML: Access Instance to trigger creation if needed
        var fade = FadeController.Instance;

        // JML: Wait one frame to ensure Awake completes
        await UniTask.Yield();

        if (fade != null)
        {
            Log("✓ FadeController ready");
        }
        else
        {
            Debug.LogError("✗ FadeController failed to initialize!");
        }
    }

    private async UniTask InitializeStageProgressManager()
    {
        Log("Initializing StageProgressManager...");

        if (stageProgressManager == null)
        {
            Debug.LogError("✗ StageProgressManager reference is NULL! Assign it in Inspector.");
            return;
        }
        // JML: Wait for Awake to complete
        await UniTask.WaitUntil(() => StageProgressManager.Instance != null);
        await UniTask.DelayFrame(1); // JML: Wait one more frame for Start
        if (StageProgressManager.Instance != null)
        {
            Log("✓ StageProgressManager ready");
        }
        else
        {
            Debug.LogError("✗ StageProgressManager failed to initialize!");
        }
    }

    private async UniTask InitializeCharacterEnhancementManager()
    {
        Log("Initializing CharacterEnhancementManager...");

        if (characterEnhancementManager == null)
        {
            Debug.LogError("✗ CharacterEnhancementManager reference is NULL! Assign it in Inspector.");
            return;
        }

        // JML: Wait for Awake to complete
        await UniTask.WaitUntil(() => CharacterEnhancementManager.Instance != null);
        await UniTask.DelayFrame(1); // JML: Wait one more frame for Start()

        if (CharacterEnhancementManager.Instance != null)
        {
            Log("✓ CharacterEnhancementManager ready");
        }
        else
        {
            Debug.LogError("✗ CharacterEnhancementManager failed to initialize!");
        }
    }

    private async UniTask InitializeDeckManager()
    {
        Log("Initializing DeckManager...");

        if (deckManager == null)
        {
            Debug.LogError("✗ DeckManager reference is NULL! Assign it in Inspector.");
            return;
        }

        // JML: Wait for Awake to complete
        await UniTask.WaitUntil(() => DeckManager.Instance != null);
        await UniTask.DelayFrame(1); // JML: Wait one more frame for Start()

        if (DeckManager.Instance != null)
        {
            Log("✓ DeckManager ready");
        }
        else
        {
            Debug.LogError("✗ DeckManager failed to initialize!");
        }
    }

    /// <summary>
    /// JML: Initialize CSVLoader and WAIT for all CSV tables to load
    /// JML: CRITICAL: Must complete before other managers that depend on CSV data
    /// </summary>
    private async UniTask InitializeCSVLoader()
    {
        Log("Initializing CSVLoader...");

        if (csvLoader == null)
        {
            Debug.LogError("✗ CSVLoader reference is NULL! Assign it in Inspector.");
            return;
        }

        // JML: WAIT for CSV loading to complete (CSVLoader.IsInit becomes true)
        int timeoutSeconds = 10;
        float waitTime = 0f;

        while (!csvLoader.IsInit && waitTime < timeoutSeconds)
        {
            await UniTask.Delay(100); // JML: Check every 100ms
            waitTime += 0.1f;
        }

        if (csvLoader.IsInit)
        {
            Log("✓ CSVLoader initialized - All CSV tables loaded");
        }
        else
        {
            Debug.LogError($"✗ CSVLoader timeout after {timeoutSeconds}s! CSV data may not be loaded.");
        }
    }

    /// <summary>
    /// JML: Initialize CurrencyManager (runs in parallel with other managers)
    /// </summary>
    private async UniTask InitializeCurrencyManager()
    {
        Log("Initializing CurrencyManager...");

        if (currencyManager == null)
        {
            Debug.LogError("✗ CurrencyManager reference is NULL! Assign it in Inspector.");
            return;
        }

        // JML: Wait for Awake + Start to complete
        await UniTask.WaitUntil(() => CurrencyManager.Instance != null);
        await UniTask.DelayFrame(1); // JML: Wait one more frame for Start()

        if (CurrencyManager.Instance != null)
        {
            Log($"✓ CurrencyManager ready (Starting Gold: {CurrencyManager.Instance.Gold})");
        }
        else
        {
            Debug.LogError("✗ CurrencyManager failed to initialize!");
        }
    }
    private UniTask InitializeBookMarkManager()
    {
        Log("Initializing BookMarkManager...");

        if (bookMarkManager == null)
        {
            Debug.LogError("✗ BookMarkManager reference is NULL! Assign it in Inspector.");
            return UniTask.CompletedTask;
        }

        // JML: Wait for Awake to complete
        return UniTask.RunOnThreadPool(async () =>
        {
            await UniTask.WaitUntil(() => BookMarkManager.Instance != null);
            await UniTask.DelayFrame(1);

            if (BookMarkManager.Instance != null)
            {
                Log("✓ BookMarkManager ready");
            }
            else
            {
                Debug.LogError("✗ BookMarkManager failed to initialize!");
            }
        });
    }

    /// <summary>
    /// LCB: LoadingUIManager 초기화 (다른 매니저들과 병렬로 실행)
    /// LCB: 싱글톤으로 자동 생성되므로 Instance 접근만으로 초기화 트리거
    /// </summary>
    private async UniTask InitializeLoadingUIManager()
    {
        Log("Initializing LoadingUIManager...");

        // LCB: Instance 접근으로 자동 생성 트리거
        var instance = LoadingUIManager.Instance;

        // LCB: Awake 완료 대기
        await UniTask.DelayFrame(1);

        if (instance != null)
        {
            Log("✓ LoadingUIManager ready");
        }
        else
        {
            Debug.LogError("✗ LoadingUIManager failed to initialize!");
        }
    }

    /// <summary>
    /// JML: Initialize IngredientManager (runs in parallel with other managers)
    /// JML: Depends on CSVLoader being ready
    /// </summary>
    private async UniTask InitializeIngredientManager()
    {
        Log("Initializing IngredientManager...");

        if (ingredientManager == null)
        {
            Debug.LogError("✗ IngredientManager reference is NULL! Assign it in Inspector.");
            return;
        }

        // JML: Wait for Awake to complete
        await UniTask.WaitUntil(() => IngredientManager.Instance != null);
        await UniTask.DelayFrame(1);

        if (IngredientManager.Instance != null)
        {
            Log("✓ IngredientManager ready");
        }
        else
        {
            Debug.LogError("✗ IngredientManager failed to initialize!");
        }
    }

    /// <summary>
    /// JML: Initialize AudioManager (runs in parallel with other managers)
    /// </summary>
    private async UniTask InitializeAudioManager()
    {
        Log("Initializing AudioManager...");

        if (audioManager == null)
        {
            Debug.LogError("✗ AudioManager reference is NULL! Assign it in Inspector.");
            return;
        }

        // JML: Wait for Awake to complete
        await UniTask.WaitUntil(() => AudioManager.Instance != null);
        await UniTask.DelayFrame(1);

        if (AudioManager.Instance != null)
        {
            Log("✓ AudioManager ready");
        }
        else
        {
            Debug.LogError("✗ AudioManager failed to initialize!");
        }
    }

    /// <summary>
    /// JML: Transition to the next scene with fade effect
    /// LCB: 수정 - 페이드인(시작) → 로딩UI → 페이드아웃 → 씬전환 → 페이드인
    /// </summary>
    private async UniTask TransitionToNextScene()
    {
        Log($"=== Transitioning to {nextSceneName} ===");

        if (FadeController.Instance == null || LoadingUIManager.Instance == null)
        {
            Debug.LogWarning("FadeController or LoadingUIManager not available, loading scene directly");
            await SceneManager.LoadSceneAsync(nextSceneName);
            return;
        }

        // Step 1: 페이드 인 (검은 화면에서 밝아짐)
        Log("Step 1: Fade In (Start from black)");
        await FadeController.Instance.FadeIn(0.5f); // 검은색 → 투명 (밝아짐)

        // Step 2: 로딩 UI 표시 및 진행률 애니메이션 (Inspector의 LOADING_DURATION_MS 사용)
        Log("Step 2: Show Loading UI");
        LoadingUIManager.Instance.Show();
        await LoadingUIManager.Instance.FakeLoadAsync();

        // Step 3: 100% 상태 잠깐 보여주기
        await UniTask.Delay(200);

        // Step 4: 페이드 아웃 (화면이 어두워짐) - 전환 시작
        Log("Step 3: Fade Out");
        await FadeController.Instance.FadeOut(0.5f);

        // Step 5: 로딩 UI 숨기기
        await LoadingUIManager.Instance.Hide();

        // Step 6: 씬 로드 (페이드 아웃 상태 유지)
        Log($"Step 4: Loading {nextSceneName}");
        await SceneManager.LoadSceneAsync(nextSceneName);

        // Step 7: 새 씬에서 페이드 인 (밝아짐)
        Log("Step 5: Fade In (New Scene)");
        await FadeController.Instance.FadeIn(0.5f);

        // Step 8: 페이드 패널 비활성화
        FadeController.Instance.fadePanel.SetActive(false);

        Log("=== Scene Transition Complete ===");
    }

    /// <summary>
    /// JML: Conditional debug logging
    /// </summary>
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[BootScene] {message}");
        }
    }
}