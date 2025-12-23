using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Coffee.UIExtensions;

namespace Tutorial
{
    /// <summary>
    /// 하이라이트 UI (특정 UI 강조 + 텍스트 박스)
    /// UnmaskForUGUI 패키지 사용
    /// </summary>
    public class HighlightView : MonoBehaviour, ITutorialView, IPointerClickHandler
    {
        [Header("Events")]
        [SerializeField] private TutorialEvents tutorialEvents;

        [Header("UI References")]
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private RectTransform textBoxRect;

        [Header("Unmask")]
        [SerializeField] private Unmask unmask;
        [SerializeField] private RectTransform unmaskRect;

        [Header("Indicator")]
        [SerializeField] private GameObject indicatorTop;
        [SerializeField] private GameObject indicatorBottom;

        [Header("Settings")]
        [SerializeField] private Vector2 unmaskPadding = new Vector2(20f, 20f);
        [SerializeField] private float textBoxOffset = 100f;

        private CancellationTokenSource typingCts;
        private bool isTyping = false;
        private string fullText = "";
        private RectTransform currentTarget;

        /// <summary>
        /// 외부에서 TutorialEvents 인스턴스를 주입받는 메서드
        /// </summary>
        public void SetTutorialEvents(TutorialEvents events)
        {
            tutorialEvents = events;
            Debug.Log($"[HighlightView] TutorialEvents injected: {events != null}");
        }

        public void Show(TutorialStep step, string text, float typingSpeed)
        {
            // 먼저 GameObject 활성화
            gameObject.SetActive(true);

            if (viewRoot != null)
                viewRoot.SetActive(true);

            // 텍스트 설정
            fullText = text;

            // 이전 타이핑 취소
            typingCts?.Cancel();
            typingCts?.Dispose();
            typingCts = new CancellationTokenSource();

            TypeTextAsync(text, typingSpeed, typingCts.Token).Forget();
        }

        public void Show(TutorialStep step, string text, RectTransform target, float typingSpeed)
        {
            Show(step, text, typingSpeed);

            currentTarget = target;

            if (target != null)
            {
                SetupUnmask(target);
                PositionTextBox(target);
            }
            else
            {
                // 타겟이 없으면 Unmask 숨기기
                if (unmask != null)
                    unmask.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            typingCts?.Cancel();
            typingCts?.Dispose();
            typingCts = null;

            if (viewRoot != null)
                viewRoot.SetActive(false);

            if (unmask != null)
                unmask.gameObject.SetActive(false);

            HideIndicators();

            gameObject.SetActive(false);
            isTyping = false;
            currentTarget = null;
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

        private void SetupUnmask(RectTransform target)
        {
            if (unmask == null || unmaskRect == null)
                return;

            unmask.gameObject.SetActive(true);

            // Unmask 위치와 크기를 타겟에 맞춤
            Vector3[] targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);

            // 월드 좌표를 로컬 좌표로 변환
            Canvas canvas = unmaskRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            Vector2 minLocal, maxLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetCorners[0]),
                canvas.worldCamera,
                out minLocal
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetCorners[2]),
                canvas.worldCamera,
                out maxLocal
            );

            Vector2 center = (minLocal + maxLocal) / 2f;
            Vector2 size = new Vector2(
                Mathf.Abs(maxLocal.x - minLocal.x) + unmaskPadding.x,
                Mathf.Abs(maxLocal.y - minLocal.y) + unmaskPadding.y
            );

            unmaskRect.anchoredPosition = center;
            unmaskRect.sizeDelta = size;
        }

        private void PositionTextBox(RectTransform target)
        {
            if (textBoxRect == null)
                return;

            // 타겟이 화면 상단에 있으면 텍스트 박스를 하단에, 하단에 있으면 상단에 배치
            Vector3[] targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);

            float screenHeight = Screen.height;
            float targetCenterY = (targetCorners[0].y + targetCorners[2].y) / 2f;

            if (targetCenterY > screenHeight / 2f)
            {
                // 타겟이 상단에 있음 -> 텍스트 박스 하단, 인디케이터 상단
                textBoxRect.anchorMin = new Vector2(0.5f, 0f);
                textBoxRect.anchorMax = new Vector2(0.5f, 0f);
                textBoxRect.pivot = new Vector2(0.5f, 0f);
                textBoxRect.anchoredPosition = new Vector2(0f, textBoxOffset);

                ShowIndicator(true); // 상단 인디케이터
            }
            else
            {
                // 타겟이 하단에 있음 -> 텍스트 박스 상단, 인디케이터 하단
                textBoxRect.anchorMin = new Vector2(0.5f, 1f);
                textBoxRect.anchorMax = new Vector2(0.5f, 1f);
                textBoxRect.pivot = new Vector2(0.5f, 1f);
                textBoxRect.anchoredPosition = new Vector2(0f, -textBoxOffset);

                ShowIndicator(false); // 하단 인디케이터
            }
        }

        private void ShowIndicator(bool top)
        {
            if (indicatorTop != null)
                indicatorTop.SetActive(top);

            if (indicatorBottom != null)
                indicatorBottom.SetActive(!top);
        }

        private void HideIndicators()
        {
            if (indicatorTop != null)
                indicatorTop.SetActive(false);

            if (indicatorBottom != null)
                indicatorBottom.SetActive(false);
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
    }
}
