using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// 텍스트를 계속 페이드인/아웃 반복시키는 컴포넌트
    /// </summary>
    public class TextFadeLoop : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 1f;

        private TextMeshProUGUI text;
        private CancellationTokenSource cts;

        private void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            cts = new CancellationTokenSource();
            FadeLoop(cts.Token).Forget();
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private async UniTaskVoid FadeLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Fade Out (밝음 → 어두움)
                await Fade(maxAlpha, minAlpha, token);

                // Fade In (어두움 → 밝음)
                await Fade(minAlpha, maxAlpha, token);
            }
        }

        private async UniTask Fade(float from, float to, CancellationToken token)
        {
            float elapsed = 0f;
            Color color = text.color;

            while (elapsed < fadeDuration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                color.a = alpha;
                text.color = color;

                await UniTask.Yield(token);
            }

            color.a = to;
            text.color = color;
        }
    }
}
