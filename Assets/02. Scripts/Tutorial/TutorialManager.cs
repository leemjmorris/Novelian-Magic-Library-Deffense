using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 진행을 관리하는 매니저
    /// 어느 씬에서든 Initialize 호출하면 TutorialCanvas를 Addressables로 로드
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("Addressables Keys")]
        [SerializeField] private string tutorialCanvasKey = "TutorialCanvas";
        [SerializeField] private string tutorialEventsKey = "TutorialEvents";

        [Header("Dependencies (Auto-loaded)")]
        [SerializeField] private TutorialEvents tutorialEvents;
        [SerializeField] private TutorialUIController uiController;

        [Header("Current Tutorial")]
        [SerializeField] private TutorialSequence currentSequence;

        // Addressables 핸들
        private AsyncOperationHandle<GameObject> canvasHandle;
        private AsyncOperationHandle<TutorialEvents> eventsHandle;
        private GameObject tutorialCanvasInstance;

        // 초기화 상태
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        // 상태
        private int currentStepIndex = -1;
        private bool isPlaying = false;
        private bool isPaused = false;
        private float savedTimeScale = 1f;
        private CancellationTokenSource cts;

        // 프로퍼티
        public bool IsPlaying => isPlaying;
        public int CurrentStepIndex => currentStepIndex;
        public TutorialStep CurrentStep => GetCurrentStep();
        public TutorialSequence CurrentSequence => currentSequence;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            ReleaseAddressables();
            if (Instance == this)
                Instance = null;
        }

        #region Initialization

        /// <summary>
        /// 튜토리얼 시스템 초기화 (Addressables로 UI 로드)
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (isInitialized)
            {
                Debug.Log("[TutorialManager] Already initialized");
                return;
            }

            Debug.Log("[TutorialManager] Initializing...");

            // TutorialEvents 로드
            if (tutorialEvents == null)
            {
                await LoadTutorialEventsAsync();
            }

            // TutorialCanvas 로드
            if (tutorialCanvasInstance == null)
            {
                await LoadTutorialCanvasAsync();
            }

            isInitialized = true;
            Debug.Log("[TutorialManager] Initialized successfully");
        }

        private async UniTask LoadTutorialEventsAsync()
        {
            try
            {
                eventsHandle = Addressables.LoadAssetAsync<TutorialEvents>(tutorialEventsKey);
                tutorialEvents = await eventsHandle;
                Debug.Log("[TutorialManager] TutorialEvents loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TutorialManager] Failed to load TutorialEvents: {e.Message}");
            }
        }

        private async UniTask LoadTutorialCanvasAsync()
        {
            try
            {
                canvasHandle = Addressables.InstantiateAsync(tutorialCanvasKey);
                tutorialCanvasInstance = await canvasHandle;

                if (tutorialCanvasInstance != null)
                {
                    DontDestroyOnLoad(tutorialCanvasInstance);
                    uiController = tutorialCanvasInstance.GetComponent<TutorialUIController>();

                    if (uiController == null)
                    {
                        uiController = tutorialCanvasInstance.AddComponent<TutorialUIController>();
                    }

                    // UIController 초기화
                    uiController.Initialize(tutorialEvents);

                    Debug.Log("[TutorialManager] TutorialCanvas loaded and initialized");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TutorialManager] Failed to load TutorialCanvas: {e.Message}");
            }
        }

        private void ReleaseAddressables()
        {
            if (canvasHandle.IsValid())
            {
                Addressables.Release(canvasHandle);
            }
            if (eventsHandle.IsValid())
            {
                Addressables.Release(eventsHandle);
            }

            if (tutorialCanvasInstance != null)
            {
                Destroy(tutorialCanvasInstance);
                tutorialCanvasInstance = null;
            }
        }

        #endregion

        private void OnEnable()
        {
            if (tutorialEvents != null)
            {
                tutorialEvents.AddDialogTouchedListener(OnDialogTouched);
                tutorialEvents.AddSkipRequestedListener(OnSkipRequested);
                tutorialEvents.AddStepActionCompletedListener(OnStepActionCompleted);
            }
        }

        private void OnDisable()
        {
            if (tutorialEvents != null)
            {
                tutorialEvents.RemoveDialogTouchedListener(OnDialogTouched);
                tutorialEvents.RemoveSkipRequestedListener(OnSkipRequested);
                tutorialEvents.RemoveStepActionCompletedListener(OnStepActionCompleted);
            }

            CancelCurrentTutorial();
        }

        #region Public Methods

        /// <summary>
        /// 튜토리얼 시작
        /// </summary>
        /// <param name="sequence">실행할 튜토리얼 시퀀스</param>
        /// <param name="forceStart">true면 완료된 튜토리얼도 강제 실행 (디버그/테스트용)</param>
        public void StartTutorial(TutorialSequence sequence, bool forceStart = false)
        {
            if (sequence == null || sequence.Steps.Count == 0)
            {
                Debug.LogWarning("[TutorialManager] Invalid tutorial sequence");
                return;
            }

            // 이미 완료된 튜토리얼이면 실행 안 함 (강제 실행이 아닌 경우)
            if (!forceStart && sequence.IsCompleted())
            {
                Debug.Log($"[TutorialManager] Tutorial '{sequence.TutorialId}' already completed. Skipping.");
                return;
            }

            if (isPlaying)
            {
                Debug.LogWarning("[TutorialManager] Tutorial already playing");
                return;
            }

            currentSequence = sequence;
            currentStepIndex = -1;
            isPlaying = true;

            cts = new CancellationTokenSource();

            tutorialEvents?.RaiseTutorialStarted(sequence.TutorialId);

            PlayTutorialAsync(cts.Token).Forget();
        }

        /// <summary>
        /// 튜토리얼 스킵
        /// </summary>
        public void SkipTutorial()
        {
            if (!isPlaying || currentSequence == null)
                return;

            if (!currentSequence.CanSkip)
            {
                Debug.Log("[TutorialManager] This tutorial cannot be skipped");
                return;
            }

            CancelCurrentTutorial();

            currentSequence.MarkAsCompleted();
            tutorialEvents?.RaiseTutorialSkipped(currentSequence.TutorialId);

            ResumeGame();
            uiController?.HideAll();

            isPlaying = false;
            currentSequence = null;
        }

        /// <summary>
        /// 다음 스텝으로 진행 (외부 호출용)
        /// </summary>
        public void AdvanceToNextStep()
        {
            OnDialogTouched();
        }

        /// <summary>
        /// 특정 튜토리얼 완료 여부 확인
        /// </summary>
        public bool IsTutorialCompleted(string tutorialId)
        {
            return PlayerPrefs.GetInt($"Tutorial_{tutorialId}_Completed", 0) == 1;
        }

        /// <summary>
        /// 모든 튜토리얼 진행 상황 리셋
        /// </summary>
        public void ResetAllProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid PlayTutorialAsync(CancellationToken token)
        {
            try
            {
                while (currentStepIndex < currentSequence.Steps.Count - 1)
                {
                    token.ThrowIfCancellationRequested();

                    currentStepIndex++;
                    var step = currentSequence.Steps[currentStepIndex];

                    tutorialEvents?.RaiseStepChanged(currentStepIndex);

                    await PlayStepAsync(step, token);
                }

                // 튜토리얼 완료
                CompleteTutorial();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[TutorialManager] Tutorial cancelled");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TutorialManager] Error during tutorial: {e}");
            }
        }

        private async UniTask PlayStepAsync(TutorialStep step, CancellationToken token)
        {
            // 시작 딜레이
            if (step.StartDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(step.StartDelay), cancellationToken: token);
            }

            // 게임 일시정지
            if (step.PauseGame)
            {
                PauseGame();
            }

            // 텍스트 로드
            string text = GetTextFromId(step.TextId);

            // UI 표시
            await ShowStepUIAsync(step, text, token);

            // 음성 재생
            if (!string.IsNullOrEmpty(step.VoiceKey))
            {
                PlayVoice(step.VoiceKey);
            }

            // 진행 조건 대기
            await WaitForAdvanceAsync(step, token);

            // UI 숨기기
            uiController?.HideCurrentView();

            // 게임 재개
            if (step.ResumeGameOnComplete)
            {
                ResumeGame();
            }
        }

        private async UniTask ShowStepUIAsync(TutorialStep step, string text, CancellationToken token)
        {
            if (uiController == null)
                return;

            switch (step.StepType)
            {
                case TutorialStepType.FullDialog:
                    uiController.ShowFullDialog(step, text);
                    break;

                case TutorialStepType.CompactDialog:
                    uiController.ShowCompactDialog(step, text);
                    break;

                case TutorialStepType.Highlight:
                    var target = GetHighlightTarget(step);
                    uiController.ShowHighlight(step, text, target);
                    break;
            }

            await UniTask.Yield(token);
        }

        private async UniTask WaitForAdvanceAsync(TutorialStep step, CancellationToken token)
        {
            switch (step.AdvanceType)
            {
                case TutorialAdvanceType.OnTouch:
                    await WaitForTouchAsync(token);
                    break;

                case TutorialAdvanceType.WaitForTargetClick:
                    await WaitForTargetClickAsync(step, token);
                    break;

                case TutorialAdvanceType.WaitForEvent:
                    await WaitForEventAsync(step.CompleteEventKey, token);
                    break;

                case TutorialAdvanceType.Auto:
                    await UniTask.Delay(TimeSpan.FromSeconds(step.AutoAdvanceDelay), cancellationToken: token);
                    break;
            }
        }

        private bool waitingForTouch = false;
        private bool touchReceived = false;

        private async UniTask WaitForTouchAsync(CancellationToken token)
        {
            waitingForTouch = true;
            touchReceived = false;

            while (!touchReceived)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
            }

            waitingForTouch = false;
        }

        private async UniTask WaitForTargetClickAsync(TutorialStep step, CancellationToken token)
        {
            // 타겟 클릭 대기는 이벤트로 처리
            await WaitForEventAsync($"CLICK_{step.HighlightTargetPath}", token);
        }

        private string waitingForEventKey = null;
        private bool eventReceived = false;

        private async UniTask WaitForEventAsync(string eventKey, CancellationToken token)
        {
            waitingForEventKey = eventKey;
            eventReceived = false;

            while (!eventReceived)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
            }

            waitingForEventKey = null;
        }

        private void CompleteTutorial()
        {
            if (currentSequence == null)
                return;

            currentSequence.MarkAsCompleted();
            tutorialEvents?.RaiseTutorialCompleted(currentSequence.TutorialId);

            ResumeGame();
            uiController?.HideAll();

            isPlaying = false;

            // 다음 튜토리얼 체크
            if (!string.IsNullOrEmpty(currentSequence.NextTutorialId))
            {
                // 다음 튜토리얼 로드 및 시작 (구현 필요)
                Debug.Log($"[TutorialManager] Next tutorial: {currentSequence.NextTutorialId}");
            }

            currentSequence = null;
        }

        private void CancelCurrentTutorial()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private TutorialStep GetCurrentStep()
        {
            if (currentSequence == null || currentStepIndex < 0 || currentStepIndex >= currentSequence.Steps.Count)
                return null;

            return currentSequence.Steps[currentStepIndex];
        }

        private string GetTextFromId(int textId)
        {
            // CSVLoader에서 텍스트 로드
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var tutorialData = CSVLoader.Instance.GetData<TutorialData>(textId);
                if (tutorialData != null)
                {
                    return tutorialData.Text;
                }
            }

            return $"[Text ID: {textId}]";
        }

        private RectTransform GetHighlightTarget(TutorialStep step)
        {
            // 직접 참조가 있으면 사용
            if (step.HighlightTarget != null)
                return step.HighlightTarget;

            // 경로로 찾기
            if (!string.IsNullOrEmpty(step.HighlightTargetPath))
            {
                var obj = GameObject.Find(step.HighlightTargetPath);
                if (obj != null)
                {
                    return obj.GetComponent<RectTransform>();
                }
            }

            return null;
        }

        private void PlayVoice(string voiceKey)
        {
            // Addressables로 음성 로드 및 재생
            // TODO: AudioManager 연동
            Debug.Log($"[TutorialManager] Play voice: {voiceKey}");
        }

        private void PauseGame()
        {
            if (!isPaused)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                isPaused = true;
            }
        }

        private void ResumeGame()
        {
            if (isPaused)
            {
                Time.timeScale = savedTimeScale;
                isPaused = false;
            }
        }

        #endregion

        #region Event Handlers

        private void OnDialogTouched()
        {
            if (waitingForTouch)
            {
                touchReceived = true;
            }
        }

        private void OnSkipRequested()
        {
            if (currentSequence != null && currentSequence.CanSkip)
            {
                // 스킵 확인 팝업 표시
                uiController?.ShowSkipConfirmPopup();
            }
        }

        private void OnStepActionCompleted(string actionKey)
        {
            if (waitingForEventKey == actionKey)
            {
                eventReceived = true;
            }
        }

        #endregion
    }
}
