using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
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

        [Header("Rank Image")]
        [SerializeField] private Image rankImage;
        [SerializeField] private Sprite rankFSprite; // F 랭크 이미지

        [Header("Text Fields")]
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

            // 처치 몬스터 수 누적 저장 (실패해도 카운트)
            int killCount = GameManager.Instance?.Wave?.GetKillCount() ?? 0;
            if (killCount > 0)
            {
                SaveKillCountAsync(killCount).Forget();
            }

            Debug.Log($"[StageFailedPanel] Shown - Time: {progressTime:F1}s, Remaining: {remainingMonsters}, Kills: {killCount}");
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

            // 랭크 이미지 (실패는 항상 F)
            if (rankImage != null && rankFSprite != null)
            {
                rankImage.sprite = rankFSprite;
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

            // Issue #570: 씬 전환 중 레벨업 방지를 위해 플래그 설정
            if (GameManager.Instance?.Stage != null)
            {
                GameManager.Instance.Stage.IsExitingStage = true;
            }

            Close();
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();
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
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();
            // SelectedStage.Data는 유지하여 같은 스테이지 재시작
            LoadGameSceneAsync().Forget();
        }

        #region Kill Count Tracking

        /// <summary>
        /// 처치 몬스터 수 누적 저장 (실패해도 카운트)
        /// </summary>
        private async UniTaskVoid SaveKillCountAsync(int killCount)
        {
            if (killCount <= 0) return;

            if (FirebaseSaveManager.Instance == null || FirebaseSaveManager.Instance.CachedData == null)
            {
                Debug.LogWarning("[StageFailedPanel] Firebase 캐시 없음 - 킬 카운트 저장 스킵");
                return;
            }

            var progression = FirebaseSaveManager.Instance.CachedData.progression;
            if (progression == null)
            {
                Debug.LogWarning("[StageFailedPanel] Progression 데이터 없음 - 킬 카운트 저장 스킵");
                return;
            }

            // 킬 카운트 누적
            progression.totalKilledMonsters += killCount;
            Debug.Log($"[StageFailedPanel] 처치 몬스터 누적 (실패): +{killCount}, 총합: {progression.totalKilledMonsters}");

            string oderId = FirebaseManager.Instance?.CurrentUserId;
            if (string.IsNullOrEmpty(oderId))
            {
                Debug.LogWarning("[StageFailedPanel] 유저 ID 없음 - Firebase 저장 스킵");
                return;
            }

            // Firebase에 진행도 저장
            await FirebaseSaveManager.Instance.SaveProgressionAsync(oderId, progression);

            // 리더보드 업데이트
            await FirebaseSaveManager.Instance.UpdateLeaderboardAsync(oderId, progression.totalKilledMonsters);
        }

        #endregion

        #region Scene Loading

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            // Issue #605: 로비 전환 전 모든 사운드 정지
            AudioManager.Instance?.StopAllSounds();
            // Issue #645: Pause() 제거 - 씬 전환 중 애니메이션 멈춤 방지
            // LobbyUI에서 ResetTimeScale()을 호출하여 TimeScale이 복구됨
            await FadeController.Instance.LoadSceneWithFade("LobbyScene");
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene");
        }

        #endregion
    }
}
