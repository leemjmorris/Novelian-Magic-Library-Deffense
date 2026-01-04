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

            // Issue #645: 실패 기록 저장
            SaveAttemptRecord();

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

        #region Issue #645 - Attempt Record

        /// <summary>
        /// 실패 기록 저장 (Firebase)
        /// </summary>
        private void SaveAttemptRecord()
        {
            if (currentDungeonData == null) return;

            if (FirebaseSaveManager.Instance?.CachedData?.progression != null &&
                FirebaseManager.Instance != null)
            {
                var progression = FirebaseSaveManager.Instance.CachedData.progression;

                // bossDungeonAttempted가 null이면 초기화
                if (progression.bossDungeonAttempted == null)
                {
                    progression.bossDungeonAttempted = new System.Collections.Generic.Dictionary<string, bool>();
                }

                string key = currentDungeonData.Floor_Index.ToString();

                // 이미 클리어한 층은 실패 기록 안 함
                if (currentDungeonData.Floor_Index < progression.bossDungeonProgress)
                {
                    Debug.Log($"[BossDungeonFailedPanel] {currentDungeonData.Floor_Index}층은 이미 클리어됨 - 실패 기록 스킵");
                    return;
                }

                // 실패 기록 저장
                progression.bossDungeonAttempted[key] = true;
                FirebaseSaveManager.Instance.SaveProgressionAsync(
                    FirebaseManager.Instance.CurrentUserId,
                    progression
                ).Forget();

                Debug.Log($"[BossDungeonFailedPanel] 실패 기록 저장: {currentDungeonData.Floor_Index}층");
            }
        }

        #endregion

        #region Scene Loading

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            // Issue #605: 로비 전환 전 모든 사운드 정지
            AudioManager.Instance?.StopAllSounds();
            // Issue #645: Pause() 제거 - 씬 전환 중 애니메이션 멈춤 방지
            // LobbyUI에서 ResetTimeScale()을 호출하여 TimeScale이 복구됨
            await FadeController.Instance.LoadSceneWithFade(SceneName.LobbyScene);
        }

        private async UniTaskVoid LoadBossDungeonSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade(SceneName.BossDungeonScene);
        }

        #endregion
    }
}
