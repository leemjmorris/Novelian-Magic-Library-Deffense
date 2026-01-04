using UnityEngine;
using UnityEngine.InputSystem;
using NovelianMagicLibraryDefense.Managers;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// 화면 터치 시 사운드 재생 (New Input System)
    /// UI, 게임 영역 구분 없이 모든 터치에 반응
    /// DontDestroyOnLoad로 모든 씬에서 작동
    /// </summary>
    public class GameTouchSoundManager : MonoBehaviour
    {
        private static GameTouchSoundManager instance;
        private InputAction touchAction;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // New Input System: 마우스 좌클릭 + 터치스크린 터치
            touchAction = new InputAction();
            touchAction.AddBinding("<Mouse>/leftButton");
            touchAction.AddBinding("<Touchscreen>/touch*/press");
            touchAction.performed += OnTouch;
        }

        private void OnEnable()
        {
            touchAction?.Enable();
        }

        private void OnDisable()
        {
            touchAction?.Disable();
        }

        private void OnDestroy()
        {
            touchAction?.Dispose();
        }

        private void OnTouch(InputAction.CallbackContext context)
        {
            AudioManager.Instance?.PlaySFX("UI_click");
        }
    }
}
