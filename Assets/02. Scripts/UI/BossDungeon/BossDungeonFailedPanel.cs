using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
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

        [Header("Rank Image")]
        [SerializeField] private Image rankImage;
        [SerializeField] private Sprite rankFSprite; // F 랭크 이미지

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI floorText;
        [SerializeField] private TextMeshProUGUI progressTimeText;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button retryButton;

        // 캐시된 결과 데이터
        private float cachedProgressTime;
        private BossDungeonData currentDungeonData;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

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
        /// 실패 패널 표시 (데이터 포함)
        /// </summary>
        public void Show(float progressTime)
        {
            cachedProgressTime = progressTime;
            currentDungeonData = SelectedBossDungeon.Data;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            // 던전 정보 표시
            UpdateFloorInfo();

            Debug.Log($"[BossDungeonFailedPanel] Shown - Time: {progressTime:F1}s");
        }

        /// <summary>
        /// 실패 패널 표시 (데이터 없이 - 하위 호환)
        /// </summary>
        public void Show()
        {
            Show(0f);
        }

        /// <summary>
        /// 층 정보 업데이트
        /// </summary>
        private void UpdateFloorInfo()
        {
            // 층 이름
            if (floorText != null && currentDungeonData != null)
            {
                floorText.text = $"{currentDungeonData.Floor_Index}층";
            }

            // 랭크 이미지 (실패는 항상 F)
            if (rankImage != null && rankFSprite != null)
            {
                rankImage.sprite = rankFSprite;
            }

            // 게임 진행 시간
            if (progressTimeText != null)
            {
                int minutes = (int)(cachedProgressTime / 60);
                int seconds = (int)(cachedProgressTime % 60);
                progressTimeText.text = $"게임 진행 시간: {minutes:D2}:{seconds:D2}";
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
            Debug.Log("[BossDungeonFailedPanel] Lobby button clicked - Loading LobbyScene");
            Close();
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();
            SelectedBossDungeon.Clear();
            LoadLobbySceneAsync().Forget();
        }

        /// <summary>
        /// 재도전 (SelectedBossDungeon.Data 유지)
        /// </summary>
        public void OnRetryButtonClicked()
        {
            Debug.Log("[BossDungeonFailedPanel] Retry button clicked - Reloading BossDungeonScene");
            Close();
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();
            // SelectedBossDungeon.Data는 유지하여 같은 층 재시작
            LoadBossDungeonSceneAsync().Forget();
        }

        #region Scene Loading

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade(SceneName.LobbyScene);
        }

        private async UniTaskVoid LoadBossDungeonSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade(SceneName.BossDungeonScene);
        }

        #endregion
    }
}
