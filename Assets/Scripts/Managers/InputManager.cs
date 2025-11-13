using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NovelianMagicLibraryDefense.Managers
{
    //JML: Input manager using Unity's new Input System for touch and mouse input
    //JML: Inherits from BaseManager to be managed by GameManager
    //JML: Supports both touchscreen (mobile) and mouse (editor) inputs
    [System.Serializable]
    public class InputManager : BaseManager
    {
        //JML: Input Actions asset reference (injected via constructor)
        private TouchControls touchControls;
        
        //JML: Input events - using C# Actions for decoupled event handling
        public event Action<Input.TouchEventData> OnTap;
        public event Action<Input.DragEventData> OnDragStart;
        public event Action<Input.DragEventData> OnDrag;
        public event Action<Input.DragEventData> OnDragEnd;
        public event Action<Input.LongPressEventData> OnLongPress;
        public event Action<Input.SwipeEventData> OnSwipe;
        
        //JML: Configuration settings
        private float longPressDuration = 0.5f;
        private float swipeMinDistance = 50f;
        private float tapMaxDistance = 20f;
        private float tapMaxDuration = 0.3f;
        
        //JML: Touch state tracking
        private bool isTouching;
        private Vector2 touchStartPosition;
        private float touchStartTime;
        private CancellationTokenSource longPressCts;
        
        //JML: Constructor with TouchControls injection
        public InputManager(TouchControls controls)
        {
            touchControls = controls;
        }
        
        protected override void OnInitialize()
        {
            if (touchControls == null)
            {
                Debug.LogError("[InputManager] TouchControls is null! Cannot initialize.");
                return;
            }
            
            //JML: Enable Input Actions
            touchControls.Enable();
            
            //JML: Subscribe to Input System events
            touchControls.Touch.PrimaryTouch.started += OnTouchStarted;
            touchControls.Touch.PrimaryTouch.canceled += OnTouchEnded;
            
            Debug.Log("[InputManager] Initialized successfully");
        }
        
        protected override void OnReset()
        {
            //JML: Cancel any ongoing input detection
            CancelLongPress();
            isTouching = false;
            
            Debug.Log("[InputManager] Reset");
        }
        
        protected override void OnDispose()
        {
            if (touchControls != null)
            {
                //JML: Unsubscribe from Input System events
                touchControls.Touch.PrimaryTouch.started -= OnTouchStarted;
                touchControls.Touch.PrimaryTouch.canceled -= OnTouchEnded;
                
                //JML: Disable Input Actions
                touchControls.Disable();
            }
            
            //JML: Cancel ongoing tasks
            CancelLongPress();
            
            Debug.Log("[InputManager] Disposed");
        }
        
        //JML: Called when touch/click begins
        private void OnTouchStarted(InputAction.CallbackContext context)
        {
            isTouching = true;
            touchStartPosition = touchControls.Touch.TouchPosition.ReadValue<Vector2>();
            touchStartTime = Time.time;
            
            //JML: Invoke drag start event
            OnDragStart?.Invoke(new Input.DragEventData
            {
                startPosition = touchStartPosition,
                currentPosition = touchStartPosition,
                delta = Vector2.zero,
                timestamp = touchStartTime
            });
            
            //JML: Start long press detection using UniTask
            StartLongPressDetection().Forget();
        }
        
        //JML: Called when touch/click ends
        private void OnTouchEnded(InputAction.CallbackContext context)
        {
            if (!isTouching) return;
            
            Vector2 endPosition = touchControls.Touch.TouchPosition.ReadValue<Vector2>();
            float duration = Time.time - touchStartTime;
            Vector2 dragDelta = endPosition - touchStartPosition;
            
            //JML: Cancel long press detection
            CancelLongPress();
            
            //JML: Invoke drag end event
            OnDragEnd?.Invoke(new Input.DragEventData
            {
                startPosition = touchStartPosition,
                currentPosition = endPosition,
                delta = dragDelta,
                timestamp = Time.time
            });
            
            //JML: Detect swipe gesture
            DetectSwipe(touchStartPosition, endPosition, duration);
            
            //JML: Detect tap (short duration and small movement)
            float distance = dragDelta.magnitude;
            if (duration < tapMaxDuration && distance < tapMaxDistance)
            {
                OnTap?.Invoke(new Input.TouchEventData
                {
                    position = endPosition,
                    timestamp = Time.time
                });
            }
            
            isTouching = false;
        }
        
        //JML: Long press detection using UniTask with cancellation support
        private async UniTaskVoid StartLongPressDetection()
        {
            CancelLongPress();
            longPressCts = new CancellationTokenSource();
            
            try
            {
                //JML: Wait for long press duration
                await UniTask.Delay(TimeSpan.FromSeconds(longPressDuration), 
                                   cancellationToken: longPressCts.Token);
                
                //JML: If still touching after delay, trigger long press event
                if (isTouching)
                {
                    OnLongPress?.Invoke(new Input.LongPressEventData
                    {
                        position = touchStartPosition,
                        duration = Time.time - touchStartTime,
                        timestamp = Time.time
                    });
                }
            }
            catch (OperationCanceledException)
            {
                //JML: Long press was cancelled - this is normal behavior
            }
            finally
            {
                longPressCts?.Dispose();
                longPressCts = null;
            }
        }
        
        //JML: Cancel ongoing long press detection
        private void CancelLongPress()
        {
            if (longPressCts != null)
            {
                longPressCts.Cancel();
                longPressCts.Dispose();
                longPressCts = null;
            }
        }
        
        //JML: Detect swipe gesture based on start and end positions
        private void DetectSwipe(Vector2 start, Vector2 end, float duration)
        {
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            
            //JML: Check if movement distance exceeds minimum threshold
            if (distance < swipeMinDistance) return;
            
            //JML: Calculate swipe direction
            Input.SwipeDirection direction = GetSwipeDirection(delta);
            
            if (direction != Input.SwipeDirection.None)
            {
                OnSwipe?.Invoke(new Input.SwipeEventData
                {
                    startPosition = start,
                    endPosition = end,
                    direction = direction,
                    distance = distance,
                    duration = duration,
                    timestamp = Time.time
                });
            }
        }
        
        //JML: Calculate swipe direction from delta vector using angle
        private Input.SwipeDirection GetSwipeDirection(Vector2 delta)
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            
            //JML: Determine direction based on angle ranges
            if (angle >= -45f && angle < 45f)
                return Input.SwipeDirection.Right;
            else if (angle >= 45f && angle < 135f)
                return Input.SwipeDirection.Up;
            else if (angle >= 135f || angle < -135f)
                return Input.SwipeDirection.Left;
            else
                return Input.SwipeDirection.Down;
        }
        
        //JML: Update method to be called every frame for continuous drag events
        //JML: Should be called from MonoBehaviour's Update method
        public void Update()
        {
            if (!isTouching) return;
            
            Vector2 currentPosition = touchControls.Touch.TouchPosition.ReadValue<Vector2>();
            Vector2 delta = touchControls.Touch.TouchDelta.ReadValue<Vector2>();
            
            //JML: Invoke drag event if there's significant movement
            if (delta.magnitude > 0.1f)
            {
                OnDrag?.Invoke(new Input.DragEventData
                {
                    startPosition = touchStartPosition,
                    currentPosition = currentPosition,
                    delta = delta,
                    timestamp = Time.time
                });
            }
        }
        
        //JML: Configuration setters
        public void SetLongPressDuration(float duration) => longPressDuration = duration;
        public void SetSwipeMinDistance(float distance) => swipeMinDistance = distance;
        public void SetTapMaxDistance(float distance) => tapMaxDistance = distance;
        public void SetTapMaxDuration(float duration) => tapMaxDuration = duration;
        
        //JML: State getters
        public bool IsTouching() => isTouching;
        public Vector2 GetTouchStartPosition() => touchStartPosition;
    }
}