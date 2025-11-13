using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace NovelianMagicLibraryDefense.Core
{
    /// <summary>
    /// LMJ: Independent fade controller that persists across all scenes
    /// Handles fade in/out effects for scene transitions
    /// This is the ONLY component that uses DontDestroyOnLoad
    /// </summary>
    public class FadeController : MonoBehaviour
    {
        private static FadeController instance;
        public static FadeController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<FadeController>();
                    if (instance == null)
                    {
                        // Create FadeController if it doesn't exist
                        GameObject go = new GameObject("FadeController");
                        instance = go.AddComponent<FadeController>();
                    }
                }
                return instance;
            }
        }

        private Image fadeImage;
        private GameObject fadePanel;
        private Canvas fadeCanvas;

        private const float DEFAULT_FADE_DURATION = 0.5f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            CreateFadePanel();
        }

        /// <summary>
        /// Creates the fade panel UI that persists across scenes
        /// </summary>
        private void CreateFadePanel()
        {
            // Create canvas for fade panel
            GameObject canvasObject = new GameObject("FadeCanvas");
            canvasObject.transform.SetParent(transform);

            fadeCanvas = canvasObject.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // Ensure it's on top of everything

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObject.AddComponent<GraphicRaycaster>();

            // Create fade panel as child of canvas
            fadePanel = new GameObject("FadePanel");
            fadePanel.transform.SetParent(canvasObject.transform, false);

            // Setup RectTransform to cover full screen
            RectTransform rectTransform = fadePanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            // Add Image component for black fade
            fadeImage = fadePanel.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0); // Start transparent
            fadeImage.raycastTarget = true; // Block input during fade

            // Start with panel inactive
            fadePanel.SetActive(false);

            Debug.Log("[FadeController] Fade panel created");
        }

        /// <summary>
        /// Performs full fade out/in cycle with scene load
        /// </summary>
        public async UniTask LoadSceneWithFade(string sceneName)
        {
            Debug.Log($"[FadeController] Loading scene: {sceneName}");

            // Activate fade panel
            fadePanel.SetActive(true);

            // Fade out
            await FadeOut(DEFAULT_FADE_DURATION);

            // Load scene
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            // Fade in
            await FadeIn(DEFAULT_FADE_DURATION);

            // Deactivate fade panel
            fadePanel.SetActive(false);

            Debug.Log($"[FadeController] Scene loaded: {sceneName}");
        }

        /// <summary>
        /// Fades from transparent to opaque (black)
        /// Uses Time.unscaledDeltaTime for frame-rate independence
        /// </summary>
        private async UniTask FadeOut(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                await UniTask.Yield();
            }
            fadeImage.color = new Color(0, 0, 0, 1); // Ensure fully opaque
        }

        /// <summary>
        /// Fades from opaque (black) to transparent
        /// Uses Time.unscaledDeltaTime for frame-rate independence
        /// </summary>
        private async UniTask FadeIn(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                await UniTask.Yield();
            }
            fadeImage.color = new Color(0, 0, 0, 0); // Ensure fully transparent
        }
    }
}
