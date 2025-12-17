using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;
using Cysharp.Threading.Tasks;
using TMPro;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// JML: 스테이지 실패 시 표시되는 패널
    /// 로비로 돌아가기 / 스테이지 재시작 버튼만 있음
    /// </summary>
    public class StageFailedPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI stageNameText;
        [SerializeField] private TextMeshProUGUI progressTimeText;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button retryButton;

        // 캐시된 결과 데이터
        private float cachedProgressTime;
        private int cachedRemainingMonsters;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            // 버튼 이벤트 연결
            if (lobbyButton != null)
            {
                lobbyButton.onClick.AddListener(OnLobbyButtonClicked);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (lobbyButton != null)
            {
                lobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryButtonClicked);
            }
        }

        /// <summary>
        /// 실패 패널 표시 (데이터 포함)
        /// </summary>
        public void Show(float progressTime, int remainingMonsters)
        {
            cachedProgressTime = progressTime;
            cachedRemainingMonsters = remainingMonsters;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            // 스테이지 정보 표시
            UpdateStageInfo();

            Debug.Log($"[StageFailedPanel] Shown - Time: {progressTime:F1}s, Remaining: {remainingMonsters}");
        }

        /// <summary>
        /// 실패 패널 표시 (데이터 없이 - 하위 호환)
        /// </summary>
        public void Show()
        {
            Show(0f, 0);
        }

        /// <summary>
        /// 스테이지 정보 업데이트
        /// </summary>
        private void UpdateStageInfo()
        {
            // 스테이지 이름
            if (stageNameText != null && SelectedStage.HasSelection)
            {
                stageNameText.text = $"스테이지 {SelectedStage.Data.Chapter_Number}";
            }

            // 랭크 (실패는 항상 F)
            if (rankText != null)
            {
                rankText.text = "F";
            }

            // 남은 몬스터 + 게임 진행 시간
            if (progressTimeText != null)
            {
                int minutes = (int)(cachedProgressTime / 60);
                int seconds = (int)(cachedProgressTime % 60);
                progressTimeText.text = $"남은 몬스터: {cachedRemainingMonsters} 마리\n게임 진행 시간: {minutes:D2}:{seconds:D2}";
            }
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        /// <summary>
        /// 로비로 돌아가기
        /// </summary>
        public void OnLobbyButtonClicked()
        {
            Debug.Log("[StageFailedPanel] Lobby button clicked - Loading LobbyScene");
            Close();
            Time.timeScale = 1f;
            SelectedStage.Clear();
            LoadLobbySceneAsync().Forget();
        }

        /// <summary>
        /// 스테이지 재시작 (SelectedStage.Data 유지)
        /// </summary>
        public void OnRetryButtonClicked()
        {
            Debug.Log("[StageFailedPanel] Retry button clicked - Reloading GameScene");
            Close();
            Time.timeScale = 1f;
            // SelectedStage.Data는 유지하여 같은 스테이지 재시작
            LoadGameSceneAsync().Forget();
        }

        #region Scene Loading

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("LobbyScene");
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene");
        }

        #endregion
    }
}
