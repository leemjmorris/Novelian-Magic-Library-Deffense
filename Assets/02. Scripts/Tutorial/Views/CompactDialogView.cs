using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using TMPro;

namespace Tutorial
{
    /// <summary>
    /// 변형 구조 대화 UI (중앙/상단, 썸네일 + 대사, 이름 없음)
    /// </summary>
    public class CompactDialogView : MonoBehaviour, ITutorialView, IPointerClickHandler
    {
        [Header("Events")]
        [SerializeField] private TutorialEvents tutorialEvents;

        [Header("UI References")]
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private TextMeshProUGUI dialogText;

        private CancellationTokenSource typingCts;
        private bool isTyping = false;
        private string fullText = "";

        /// <summary>
        /// 외부에서 TutorialEvents 인스턴스를 주입받는 메서드
        /// </summary>
        public void SetTutorialEvents(TutorialEvents events)
        {
            tutorialEvents = events;
            Debug.Log($"[CompactDialogView] TutorialEvents injected: {events != null}");
        }

        public void Show(TutorialStep step, string text, float typingSpeed)
        {
            // 먼저 GameObject 활성화
            gameObject.SetActive(true);

            if (viewRoot != null)
                viewRoot.SetActive(true);

            // 썸네일 설정 (첫 번째 캐릭터의 썸네일 사용)
            if (step.Characters.Count > 0 && thumbnailImage != null)
            {
                thumbnailImage.gameObject.SetActive(true);
                // Addressables로 썸네일 로드
                var illustKey = step.Characters[0].IllustrationKey;
                if (!string.IsNullOrEmpty(illustKey))
                {
                    LoadThumbnailAsync(thumbnailImage, illustKey).Forget();
                }
            }
            else if (thumbnailImage != null)
            {
                thumbnailImage.gameObject.SetActive(false);
            }

            // 텍스트 설정
            fullText = text;

            // 이전 타이핑 취소
            typingCts?.Cancel();
            typingCts?.Dispose();
            typingCts = new CancellationTokenSource();

            TypeTextAsync(text, typingSpeed, typingCts.Token).Forget();
        }

        public void Hide()
        {
            typingCts?.Cancel();
            typingCts?.Dispose();
            typingCts = null;

            if (viewRoot != null)
                viewRoot.SetActive(false);

            gameObject.SetActive(false);
            isTyping = false;
        }

        private void OnDestroy()
        {
            typingCts?.Cancel();
            typingCts?.Dispose();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isTyping)
            {
                CompleteTyping();
            }
            else
            {
                tutorialEvents?.RaiseDialogTouched();
            }
        }

        private async UniTaskVoid TypeTextAsync(string text, float speed, CancellationToken token)
        {
            isTyping = true;
            dialogText.text = "";

            try
            {
                foreach (char c in text)
                {
                    token.ThrowIfCancellationRequested();
                    dialogText.text += c;
                    await UniTask.Delay(TimeSpan.FromSeconds(speed), ignoreTimeScale: true, cancellationToken: token);
                }

                isTyping = false;
            }
            catch (OperationCanceledException)
            {
                // 취소됨 - 정상 동작
            }
        }

        private void CompleteTyping()
        {
            typingCts?.Cancel();

            dialogText.text = fullText;
            isTyping = false;
        }

        private async UniTaskVoid LoadThumbnailAsync(Image targetImage, string pathIdString)
        {
            try
            {
                // IllustrationKey는 실제로 Path_ID이므로 PathData를 조회하여 실제 Addressable_Key를 얻어야 함
                if (int.TryParse(pathIdString, out int pathId))
                {
                    var pathData = CSVLoader.Instance.GetData<PathData>(pathId);
                    if (pathData != null && !string.IsNullOrEmpty(pathData.Addressable_Key))
                    {
                        var sprite = await Addressables.LoadAssetAsync<Sprite>(pathData.Addressable_Key).ToUniTask();
                        if (sprite != null && targetImage != null)
                        {
                            targetImage.sprite = sprite;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CompactDialogView] PathData not found for Path_ID: {pathId}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CompactDialogView] Invalid Path_ID format: {pathIdString}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CompactDialogView] Failed to load thumbnail: {pathIdString}, Error: {e.Message}");
            }
        }
    }
}
