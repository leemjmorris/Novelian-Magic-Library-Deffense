using UnityEngine;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine.InputSystem;

public class TestObj : MonoBehaviour
{
    public GameManager gameManager;

    // Input Actions (동일한 InputActions 사용)
    private InputActions inputActions;

#if UNITY_EDITOR
    private void OnEnable()
    {
        // Input Actions 생성 및 활성화
        inputActions = new InputActions();
        inputActions.Enable();

        // InputManager 이벤트 구독 (테스트용)
        InputManager.OnShortPress += OnShortPressTest;
        InputManager.OnLongPressStart += OnLongPressTest;
        InputManager.OnDragUpdate += OnDragUpdateTest;
        InputManager.OnDrop += OnDropTest;

        Debug.Log("[TestObj] InputManager 이벤트 구독 완료 - 마우스 클릭 및 드래그 테스트 준비됨");
    }

    private void OnDisable()
    {
        // InputManager 이벤트 구독 해제
        InputManager.OnShortPress -= OnShortPressTest;
        InputManager.OnLongPressStart -= OnLongPressTest;
        InputManager.OnDragUpdate -= OnDragUpdateTest;
        InputManager.OnDrop -= OnDropTest;

        // Input Actions 비활성화 및 정리
        inputActions?.Disable();
        inputActions?.Dispose();
    }

    // 테스트용 이벤트 핸들러
    private void OnShortPressTest(Vector2 position)
    {
        Debug.Log($"<color=green>[TEST] Short Press 감지! 위치: {position}</color>");
    }

    private void OnLongPressTest(Vector2 position)
    {
        Debug.Log($"<color=yellow>[TEST] Long Press 시작! 위치: {position} (2초 유지 완료)</color>");
    }

    private void OnDragUpdateTest(Vector2 position)
    {
        Debug.Log($"<color=cyan>[TEST] Drag 중... 위치: {position}</color>");
    }

    private void OnDropTest(Vector2 position)
    {
        Debug.Log($"<color=magenta>[TEST] Drop 완료! 위치: {position}</color>");
    }

    private void Update()
    {
        // Keyboard API 대신 Input Actions 사용
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                gameManager.StageState.SetStageState(StageState.Cleared);
            }
            else if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                gameManager.StageState.SetStageState(StageState.Failed);
            }
        }
    }
#endif
}