using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// Input System 기반 터치/마우스 입력 처리 매니저
    /// Android: 터치 입력 (싱글 터치만)
    /// Unity Editor: 마우스 입력 (#if UNITY_EDITOR)
    /// </summary>
    [System.Serializable]
    public class InputManager : BaseManager
    {
        // Input Actions
        private InputActions inputActions;

        // 입력 상태 관리
        private bool isInputActive = false;
        private bool isLongPressCompleted = false;
        private Vector2 pressStartPosition;
        private CancellationTokenSource longPressCts;

        // 드래그 감지 설정
        private const float LONG_PRESS_DURATION = 2f; // 2초
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
            Debug.Log("[InputManager] Initializing Input System");

            // Input Actions 생성
            inputActions = new InputActions();

#if UNITY_EDITOR
            // Unity Editor: 마우스 입력 설정
            SetupMouseInput();
            inputActions.Mouse.Enable();
            Debug.Log("[InputManager] Mouse input enabled (Unity Editor)");
#else
            // Android 빌드: 터치 입력 설정
            SetupTouchInput();
            inputActions.Touch.Enable();
            Debug.Log("[InputManager] Touch input enabled (Android)");
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor 전용: 마우스 입력 설정
        /// </summary>
        private void SetupMouseInput()
        {
            inputActions.Mouse.Click.started += OnPointerDown;
            inputActions.Mouse.Click.canceled += OnPointerUp;
        }
#else
        /// <summary>
        /// Android 빌드: 터치 입력 설정
        /// </summary>
        private void SetupTouchInput()
        {
            inputActions.Touch.TouchPress.started += OnPointerDown;
            inputActions.Touch.TouchPress.canceled += OnPointerUp;
        }
#endif

        /// <summary>
        /// 입력 시작 (터치 다운 / 마우스 클릭 다운)
        /// </summary>
        private void OnPointerDown(InputAction.CallbackContext context)
        {
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
            return inputActions.Mouse.MousePosition.ReadValue<Vector2>();
#else
            return inputActions.Touch.TouchPosition.ReadValue<Vector2>();
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

            // 타이머 정리
            longPressCts?.Cancel();
            longPressCts?.Dispose();
            longPressCts = null;

#if UNITY_EDITOR
            // 마우스 입력 해제
            if (inputActions != null)
            {
                inputActions.Mouse.Click.started -= OnPointerDown;
                inputActions.Mouse.Click.canceled -= OnPointerUp;
                inputActions.Mouse.Disable();
            }
#else
            // 터치 입력 해제
            if (inputActions != null)
            {
                inputActions.Touch.TouchPress.started -= OnPointerDown;
                inputActions.Touch.TouchPress.canceled -= OnPointerUp;
                inputActions.Touch.Disable();
            }
#endif

            // Input Actions 정리
            inputActions?.Dispose();
            inputActions = null;

            Debug.Log("[InputManager] Disposed successfully");
        }
    }
}
