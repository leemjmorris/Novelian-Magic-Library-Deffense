using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// 클릭하면 확장/축소되는 메뉴 컨트롤러
    /// 작은 패널 상태에서 클릭하면 길게 확장됨
    /// </summary>
    public class ExpandableMenuController : MonoBehaviour
    {
        [Header("메뉴 설정")]
        [SerializeField] private RectTransform menuPanel;
        [SerializeField] private Button toggleButton;

        [Header("크기 설정")]
        [SerializeField] private float collapsedHeight = 160f;
        [SerializeField] private float expandedHeight = 1100f;

        [Header("애니메이션 설정")]
        [SerializeField] private float animationDuration = 0.3f;

        [Header("콘텐츠 (확장 시 보이는 요소들)")]
        [SerializeField] private GameObject[] expandedContent;

        [Header("축소 상태 아이콘 (선택)")]
        [SerializeField] private GameObject collapsedIcon;
        [SerializeField] private GameObject expandedIcon;

        private bool isExpanded = false;
        private CancellationTokenSource cts;

        private void Awake()
        {
            if (menuPanel == null)
                menuPanel = GetComponent<RectTransform>();

            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleMenu);
        }

        private void OnEnable()
        {
            // 로비 진입 시 항상 활성화 + 축소 상태로 시작
            SetCollapsedState();
        }

        private void OnDestroy()
        {
            CancelAnimation();
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(ToggleMenu);
        }

        public void ToggleMenu()
        {
            if (isExpanded)
                Collapse();
            else
                Expand();
        }

        public void Expand()
        {
            if (isExpanded) return;

            isExpanded = true;
            CancelAnimation();

            SetContentActive(true);
            UpdateIcons();

            cts = new CancellationTokenSource();
            AnimateHeightAsync(expandedHeight, null, cts.Token).Forget();
        }

        public void Collapse()
        {
            if (!isExpanded) return;

            isExpanded = false;
            CancelAnimation();

            UpdateIcons();

            cts = new CancellationTokenSource();
            AnimateHeightAsync(collapsedHeight, () => SetContentActive(false), cts.Token).Forget();
        }

        public void SetCollapsedState()
        {
            isExpanded = false;
            CancelAnimation();
            menuPanel.sizeDelta = new Vector2(menuPanel.sizeDelta.x, collapsedHeight);
            SetContentActive(false);
            UpdateIcons();
        }

        public void SetExpandedState()
        {
            isExpanded = true;
            CancelAnimation();
            menuPanel.sizeDelta = new Vector2(menuPanel.sizeDelta.x, expandedHeight);
            SetContentActive(true);
            UpdateIcons();
        }

        private async UniTaskVoid AnimateHeightAsync(float targetHeight, System.Action onComplete, CancellationToken token)
        {
            float startHeight = menuPanel.sizeDelta.y;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                t = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic

                float newHeight = Mathf.Lerp(startHeight, targetHeight, t);
                menuPanel.sizeDelta = new Vector2(menuPanel.sizeDelta.x, newHeight);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (!token.IsCancellationRequested)
            {
                menuPanel.sizeDelta = new Vector2(menuPanel.sizeDelta.x, targetHeight);
                onComplete?.Invoke();
            }
        }

        private void CancelAnimation()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private void SetContentActive(bool active)
        {
            if (expandedContent == null) return;

            foreach (var content in expandedContent)
            {
                if (content != null)
                    content.SetActive(active);
            }
        }

        private void UpdateIcons()
        {
            if (collapsedIcon != null)
                collapsedIcon.SetActive(!isExpanded);
            if (expandedIcon != null)
                expandedIcon.SetActive(isExpanded);
        }

        public bool IsExpanded => isExpanded;
    }
}
