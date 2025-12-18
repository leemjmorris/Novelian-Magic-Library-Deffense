using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #476 - 도전던전 실패 패널
    /// StageFailedPanel과 동일한 구조
    /// </summary>
    public class BossDungeonFailedPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI floorText;
        [SerializeField] private TextMeshProUGUI reasonText;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button retryButton;

        [Header("Animation")]
        [SerializeField] private float showDelay = 1f;
        [SerializeField] private Animator animator;

        private BossDungeonData currentDungeonData;

        public bool IsOpen => (panel != null ? panel : gameObject).activeSelf;

        private void Awake()
        {
            // 버튼 이벤트 연결
            if (lobbyButton != null)
                lobbyButton.onClick.AddListener(OnLobbyButtonClicked);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        private void OnDestroy()
        {
            if (lobbyButton != null)
                lobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);

            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryButtonClicked);
        }

        /// <summary>
        /// 실패 패널 표시
        /// </summary>
        public void Show(string reason)
        {
            currentDungeonData = SelectedBossDungeon.Data;

            // Issue #476: 텍스트를 패널 활성화 전에 먼저 세팅 (텍스트 변경이 눈에 보이지 않도록)
            SetupTexts(reason);

            // 패널 활성화
            GameObject targetPanel = panel != null ? panel : gameObject;
            targetPanel.SetActive(true);

            ShowAsync().Forget();
        }

        /// <summary>
        /// 텍스트 미리 세팅 (패널 활성화 전)
        /// </summary>
        private void SetupTexts(string reason)
        {
            if (titleText != null)
                titleText.text = "실패...";

            if (reasonText != null)
                reasonText.text = reason;

            if (floorText != null && currentDungeonData != null)
                floorText.text = $"{currentDungeonData.Floor_Index}층";
        }

        private async UniTaskVoid ShowAsync()
        {
            // 지연 후 게임 일시정지
            if (showDelay > 0)
            {
                await UniTask.Delay((int)(showDelay * 1000));
            }

            // Issue #476: 게임 일시정지
            Time.timeScale = 0f;

            // 애니메이션 재생
            if (animator != null)
            {
                animator.SetTrigger("Fail");
            }

            Debug.Log("[BossDungeonFailedPanel] 실패 표시 완료");
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void Close()
        {
            GameObject targetPanel = panel != null ? panel : gameObject;
            targetPanel.SetActive(false);
        }

        #region Button Handlers

        private void OnLobbyButtonClicked()
        {
            Debug.Log("[BossDungeonFailedPanel] 로비 버튼 클릭");
            Time.timeScale = 1f;
            Close();
            SelectedBossDungeon.Clear();
            LoadSceneAsync(SceneName.LobbyScene).Forget();
        }

        private void OnRetryButtonClicked()
        {
            Debug.Log("[BossDungeonFailedPanel] 재도전 버튼 클릭");
            Time.timeScale = 1f;
            Close();
            LoadSceneAsync(SceneName.BossDungeonScene).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(string sceneName)
        {
            if (FadeController.Instance != null)
            {
                FadeController.Instance.fadePanel.SetActive(true);
                await FadeController.Instance.FadeOut(0.5f);
            }

            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            if (FadeController.Instance != null)
            {
                await FadeController.Instance.FadeIn(0.5f);
                FadeController.Instance.fadePanel.SetActive(false);
            }
        }

        #endregion
    }
}
