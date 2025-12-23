using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Coffee.UIExtensions;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 UI 전체를 제어하는 컨트롤러
    /// </summary>
    public class TutorialUIController : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField] private TutorialEvents tutorialEvents;

        [Header("Views")]
        [SerializeField] private FullDialogView fullDialogView;
        [SerializeField] private CompactDialogView compactDialogView;
        [SerializeField] private HighlightView highlightView;
        [SerializeField] private SkipConfirmPopup skipConfirmPopup;

        [Header("Common")]
        [SerializeField] private CanvasGroup dimBackground;
        [SerializeField] private Button skipButton;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.03f;

        private ITutorialView currentView;
        private Coroutine typingCoroutine;
        private bool isInitialized = false;

        private void Awake()
        {
            // 자동 초기화 (SerializedField가 설정되지 않은 경우)
            AutoFindViews();

            // 초기 상태: 모두 숨김
            HideAll();

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipButtonClicked);
            }
        }

        /// <summary>
        /// TutorialManager에서 호출하는 초기화
        /// </summary>
        public void Initialize(TutorialEvents events)
        {
            if (isInitialized) return;

            tutorialEvents = events;
            AutoFindViews();

            // 각 View에 TutorialEvents 주입 (동일한 인스턴스 공유)
            InjectEventsToViews(events);

            HideAll();

            isInitialized = true;
            Debug.Log("[TutorialUIController] Initialized with TutorialEvents injection");
        }

        /// <summary>
        /// 모든 View에 TutorialEvents 인스턴스를 주입
        /// </summary>
        private void InjectEventsToViews(TutorialEvents events)
        {
            if (fullDialogView != null)
                fullDialogView.SetTutorialEvents(events);

            if (compactDialogView != null)
                compactDialogView.SetTutorialEvents(events);

            if (highlightView != null)
                highlightView.SetTutorialEvents(events);

            Debug.Log("[TutorialUIController] TutorialEvents injected to all views");
        }

        private void AutoFindViews()
        {
            if (fullDialogView == null)
                fullDialogView = GetComponentInChildren<FullDialogView>(true);

            if (compactDialogView == null)
                compactDialogView = GetComponentInChildren<CompactDialogView>(true);

            if (highlightView == null)
                highlightView = GetComponentInChildren<HighlightView>(true);

            if (skipConfirmPopup == null)
                skipConfirmPopup = GetComponentInChildren<SkipConfirmPopup>(true);

            if (dimBackground == null)
            {
                var dim = transform.Find("DimBackground");
                if (dim != null)
                    dimBackground = dim.GetComponent<CanvasGroup>();
            }
        }

        #region Public Methods

        public void ShowFullDialog(TutorialStep step, string text)
        {
            HideCurrentView();

            if (fullDialogView != null)
            {
                currentView = fullDialogView;
                fullDialogView.Show(step, text, typingSpeed);

                if (step.DimBackground)
                    ShowDimBackground();

                ShowSkipButton();
            }
        }

        public void ShowCompactDialog(TutorialStep step, string text)
        {
            HideCurrentView();

            if (compactDialogView != null)
            {
                currentView = compactDialogView;
                compactDialogView.Show(step, text, typingSpeed);

                if (step.DimBackground)
                    ShowDimBackground();

                ShowSkipButton();
            }
        }

        public void ShowHighlight(TutorialStep step, string text, RectTransform target)
        {
            HideCurrentView();

            if (highlightView != null)
            {
                currentView = highlightView;
                highlightView.Show(step, text, target, typingSpeed);

                // 하이라이트는 항상 딤 배경 사용
                ShowDimBackground();
                ShowSkipButton();
            }
        }

        public void ShowSkipConfirmPopup()
        {
            if (skipConfirmPopup != null)
            {
                skipConfirmPopup.Show(
                    onConfirm: () => TutorialManager.Instance?.SkipTutorial(),
                    onCancel: () => skipConfirmPopup.Hide()
                );
            }
        }

        public void HideCurrentView()
        {
            currentView?.Hide();
            currentView = null;
        }

        public void HideAll()
        {
            fullDialogView?.Hide();
            compactDialogView?.Hide();
            highlightView?.Hide();
            skipConfirmPopup?.Hide();

            HideDimBackground();
            HideSkipButton();

            currentView = null;
        }

        #endregion

        #region Private Methods

        private void ShowDimBackground()
        {
            if (dimBackground != null)
            {
                dimBackground.gameObject.SetActive(true);
                dimBackground.alpha = 1f;
            }
        }

        private void HideDimBackground()
        {
            if (dimBackground != null)
            {
                dimBackground.alpha = 0f;
                dimBackground.gameObject.SetActive(false);
            }
        }

        private void ShowSkipButton()
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
            }
        }

        private void HideSkipButton()
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false);
            }
        }

        private void OnSkipButtonClicked()
        {
            tutorialEvents?.RaiseSkipRequested();
        }

        #endregion
    }

    /// <summary>
    /// 튜토리얼 뷰 인터페이스
    /// </summary>
    public interface ITutorialView
    {
        void Show(TutorialStep step, string text, float typingSpeed);
        void Hide();
    }
}
