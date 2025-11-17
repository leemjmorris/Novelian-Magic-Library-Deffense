using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// Input System 기반 터치/마우스 입력 처리 매니저
    /// Android: 터치 입력 (싱글 터치만)
    /// Unity Editor: 마우스 입력 (기본) 또는 터치 시뮬레이션
    /// MonoBehaviour 기반 Manager (VContainer 지원)
    /// Singleton 패턴으로 어디서든 접근 가능
    /// </summary>
    public class InputManager : BaseManager
    {
        // Singleton Instance
        [Header("Singleton Settings")]
        [Tooltip("인스펙터에서 직접 할당 가능 (옵션). 비어있으면 자동으로 찾습니다.")]
        [SerializeField] private InputManager manualInstance;

        private static InputManager instance;
        public static InputManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // 1순위: 인스펙터에서 직접 할당된 인스턴스
                    InputManager[] allInstances = FindObjectsByType<InputManager>(FindObjectsSortMode.None);
                    foreach (var mgr in allInstances)
                    {
                        if (mgr.manualInstance != null)
                        {
                            instance = mgr.manualInstance;
                            break;
                        }
                    }

                    // 2순위: Tag로 찾기
                    if (instance == null)
                    {
                        GameObject managerObj = GameObject.FindGameObjectWithTag("Manager");
                        if (managerObj != null)
                        {
                            instance = managerObj.GetComponent<InputManager>();
                        }
                    }

                    // 3순위: Tag 없으면 이름으로 찾기
                    if (instance == null)
                    {
                        GameObject managerObj = GameObject.Find("InputManger"); // 씬에 있는 오브젝트 이름
                        if (managerObj != null)
                        {
                            instance = managerObj.GetComponent<InputManager>();
                        }
                    }

                    // 4순위: FindFirstObjectByType으로 찾기
                    if (instance == null)
                    {
                        instance = FindFirstObjectByType<InputManager>();
                    }

                    if (instance == null)
                    {
                        Debug.LogError("[InputManager] Instance not found in scene! Make sure InputManager GameObject has 'Manager' tag or assign it manually in Inspector.");
                    }
                }
                return instance;
            }
        }

#if UNITY_EDITOR
        [Header("Editor Test Settings")]
        [Tooltip("체크하면 에디터에서 터치 입력 시뮬레이션 (Android 테스트용)")]
        [SerializeField] private bool simulateTouchInEditor = false;
#endif

        // Input Actions
        private InputActions inputActions;

        // 입력 상태 관리
        private bool isInputActive = false;
        private bool isLongPressCompleted = false;
        private Vector2 pressStartPosition;
        private CancellationTokenSource longPressCts;

        // 드래그 감지 설정
        private const float LONG_PRESS_DURATION = 1f; // 1초
        private const float DRAG_THRESHOLD = 10f; // 드래그 감지 최소 이동 거리 (픽셀)

        #region Events
        /// <summary>
        /// 짧은 터치/클릭 이벤트 (2초 미만)
        /// </summary>
        public static event Action<Vector2> OnShortPress;

        /// <summary>
        /// 롱터치/롱클릭 시작 이벤트 (2초 완료)
        /// </summary>
        public static event Action<Vector2> OnLongPressStart;

        /// <summary>
        /// 드래그 중 위치 업데이트 이벤트
        /// </summary>
        public static event Action<Vector2> OnDragUpdate;

        /// <summary>
        /// 드롭 완료 이벤트
        /// </summary>
        public static event Action<Vector2> OnDrop;
        #endregion

        protected override void OnInitialize()
        {
            // Singleton 설정
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[InputManager] Duplicate instance detected! Destroying this instance.");
                Destroy(gameObject);
                return;
            }
            instance = this;

            Debug.Log("[InputManager] Initializing Input System");

            // Input Actions 생성
            inputActions = new InputActions();

#if UNITY_EDITOR
            // Unity Editor: 테스트 모드에 따라 입력 전환
            if (simulateTouchInEditor)
            {
                // Enable EnhancedTouch for proper touch simulation in Editor
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
                SetupTouchInput();
                inputActions.Touch.Enable();
                Debug.Log("[InputManager] Touch input enabled (Unity Editor - Simulation Mode with EnhancedTouch)");
            }
            else
            {
                SetupMouseInput();
                inputActions.Mouse.Enable();
                Debug.Log("[InputManager] Mouse input enabled (Unity Editor)");
            }
#else
            // Android 빌드: 터치 입력 설정
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            SetupTouchInput();
            inputActions.Touch.Enable();
            Debug.Log("[InputManager] Touch input enabled (Android)");
#endif
        }

        /// <summary>
        /// 마우스 입력 설정
        /// </summary>
        private void SetupMouseInput()
        {
            Debug.Log("[InputManager] SetupMouseInput() - 마우스 이벤트 등록 중...");
            inputActions.Mouse.Click.started += OnPointerDown;
            inputActions.Mouse.Click.canceled += OnPointerUp;
            Debug.Log("[InputManager] SetupMouseInput() - 마우스 이벤트 등록 완료!");
        }

        /// <summary>
        /// 터치 입력 설정
        /// </summary>
        private void SetupTouchInput()
        {
            Debug.Log("[InputManager] SetupTouchInput() - 터치 이벤트 등록 중...");
            inputActions.Touch.TouchPress.started += OnPointerDown;
            inputActions.Touch.TouchPress.canceled += OnPointerUp;
            Debug.Log("[InputManager] SetupTouchInput() - 터치 이벤트 등록 완료!");
        }

        /// <summary>
        /// 입력 시작 (터치 다운 / 마우스 클릭 다운)
        /// </summary>
        private void OnPointerDown(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputManager] 🔵 OnPointerDown 호출됨! context.phase={context.phase}");

            // UI 클릭 감지: EventSystem이 UI 위에서 클릭했는지 확인
            bool isOverUI = IsPointerOverUI();
            Debug.Log($"[InputManager] UI 위에 있는가? {isOverUI}");

            if (isOverUI)
            {
                Debug.Log("[InputManager] Click on UI detected, ignoring input");
                return;
            }

            // 멀티터치 차단: 이미 입력이 활성화되어 있으면 무시
            if (isInputActive)
            {
                Debug.Log("[InputManager] Multi-touch blocked - input already active");
                return;
            }

            isInputActive = true;
            isLongPressCompleted = false;
            pressStartPosition = GetCurrentPosition();

            // 2초 롱프레스 타이머 시작
            longPressCts?.Cancel();
            longPressCts?.Dispose();
            longPressCts = new CancellationTokenSource();

            StartLongPressTimer(longPressCts.Token).Forget();

            Debug.Log($"[InputManager] Pointer down at {pressStartPosition}");
        }

        /// <summary>
        /// 입력 종료 (터치 업 / 마우스 클릭 업)
        /// </summary>
        private void OnPointerUp(InputAction.CallbackContext context)
        {
            if (!isInputActive)
            {
                return;
            }

            // 타이머 취소
            longPressCts?.Cancel();

            Vector2 currentPosition = GetCurrentPosition();

            // 드래그 중이었으면 드롭 이벤트 발생
            if (isLongPressCompleted)
            {
                Debug.Log($"[InputManager] Drop at {currentPosition}");
                OnDrop?.Invoke(currentPosition);
            }
            else
            {
                // 짧은 터치/클릭 이벤트 발생
                Debug.Log($"[InputManager] Short press at {currentPosition}");
                OnShortPress?.Invoke(currentPosition);
            }

            // 상태 초기화
            isInputActive = false;
            isLongPressCompleted = false;
        }

        /// <summary>
        /// 2초 롱프레스 타이머
        /// </summary>
        private async UniTaskVoid StartLongPressTimer(CancellationToken ct)
        {
            try
            {
                // 2초 대기
                await UniTask.Delay(TimeSpan.FromSeconds(LONG_PRESS_DURATION), cancellationToken: ct);

                // 2초 완료: 롱프레스 상태로 전환
                isLongPressCompleted = true;
                Debug.Log($"[InputManager] Long press completed at {pressStartPosition}");
                Debug.Log($"[InputManager] OnLongPressStart 구독자 수: {OnLongPressStart?.GetInvocationList().Length ?? 0}");
                OnLongPressStart?.Invoke(pressStartPosition);

                // 드래그 감지 시작
                StartDragDetection(ct).Forget();
            }
            catch (OperationCanceledException)
            {
                // 타이머 취소됨 (정상 동작)
            }
        }

        /// <summary>
        /// 드래그 감지 (롱프레스 완료 후)
        /// </summary>
        private async UniTaskVoid StartDragDetection(CancellationToken ct)
        {
            Vector2 lastPosition = pressStartPosition;

            try
            {
                while (!ct.IsCancellationRequested && isInputActive)
                {
                    Vector2 currentPosition = GetCurrentPosition();
                    float distance = Vector2.Distance(lastPosition, currentPosition);

                    // 이동 거리가 임계값을 넘으면 드래그 이벤트 발생
                    if (distance > DRAG_THRESHOLD)
                    {
                        OnDragUpdate?.Invoke(currentPosition);
                        lastPosition = currentPosition;
                    }

                    // 프레임마다 체크
                    await UniTask.Yield(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // 드래그 취소됨 (정상 동작)
            }
        }

        /// <summary>
        /// 현재 포인터 위치 가져오기 (플랫폼별)
        /// </summary>
        private Vector2 GetCurrentPosition()
        {
#if UNITY_EDITOR
            if (simulateTouchInEditor)
            {
                return inputActions.Touch.TouchPosition.ReadValue<Vector2>();
            }
            else
            {
                return inputActions.Mouse.MousePosition.ReadValue<Vector2>();
            }
#else
            return inputActions.Touch.TouchPosition.ReadValue<Vector2>();
#endif
        }

        /// <summary>
        /// 포인터가 UI 위에 있는지 확인 (EventSystem 사용)
        /// </summary>
        private bool IsPointerOverUI()
        {
            // EventSystem이 없으면 UI 체크 불가능
            if (EventSystem.current == null)
            {
                return false;
            }

#if UNITY_EDITOR
            if (simulateTouchInEditor)
            {
                // 터치 시뮬레이션 모드: 터치 입력으로 UI 체크
                if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
                {
                    int touchId = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].finger.index;
                    return EventSystem.current.IsPointerOverGameObject(touchId);
                }
                return false;
            }
            else
            {
                // 마우스 모드: 마우스 위치로 UI 체크
                return EventSystem.current.IsPointerOverGameObject();
            }
#else
            // Android: 터치 입력으로 UI 체크
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                int touchId = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].finger.index;
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }
            return false;
#endif
        }

        protected override void OnReset()
        {
            Debug.Log("[InputManager] Resetting input state");

            // 타이머 취소
            longPressCts?.Cancel();

            // 상태 초기화
            isInputActive = false;
            isLongPressCompleted = false;
        }

        protected override void OnDispose()
        {
            Debug.Log("[InputManager] Disposing Input System");

            // Singleton 정리
            if (instance == this)
            {
                instance = null;
            }

            // 타이머 정리
            longPressCts?.Cancel();
            longPressCts?.Dispose();
            longPressCts = null;

            // 입력 이벤트 해제 및 비활성화
            if (inputActions != null)
            {
#if UNITY_EDITOR
                if (simulateTouchInEditor)
                {
                    // 터치 입력 해제
                    inputActions.Touch.TouchPress.started -= OnPointerDown;
                    inputActions.Touch.TouchPress.canceled -= OnPointerUp;
                    inputActions.Touch.Disable();
                    // Disable EnhancedTouch
                    UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
                }
                else
                {
                    // 마우스 입력 해제
                    inputActions.Mouse.Click.started -= OnPointerDown;
                    inputActions.Mouse.Click.canceled -= OnPointerUp;
                    inputActions.Mouse.Disable();
                }
#else
                // 터치 입력 해제
                inputActions.Touch.TouchPress.started -= OnPointerDown;
                inputActions.Touch.TouchPress.canceled -= OnPointerUp;
                inputActions.Touch.Disable();
                // Disable EnhancedTouch
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
#endif
            }

            // Input Actions 정리
            inputActions?.Dispose();
            inputActions = null;

            Debug.Log("[InputManager] Disposed successfully");
        }
    }
}
