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
            // BGM 일시적으로 낮추고 패배 효과음 재생
            DuckBGMForResultSFX("LoseSFX").Forget();

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

            GameLog.Log($"[StageFailedPanel] Shown - Time: {progressTime:F1}s, Remaining: {remainingMonsters}, Kills: {killCount}");
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
            GameLog.Log("[StageFailedPanel] Lobby button clicked - Loading LobbyScene");

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
            GameLog.Log("[StageFailedPanel] Retry button clicked - Checking AP and reloading GameScene");

            // 1. SelectedStage 데이터 확인
            if (!SelectedStage.HasSelection)
            {
                GameLog.LogError("[StageFailedPanel] 스테이지가 선택되지 않음");
                return;
            }

            var stageData = SelectedStage.Data;
            int apCost = stageData.AP_Cost;

            // 2. CurrencyManager 확인
            if (CurrencyManager.Instance == null)
            {
                GameLog.LogError("[StageFailedPanel] CurrencyManager가 초기화되지 않음");
                return;
            }

            // 3. AP 잔량 확인
            if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.AP_ID, apCost))
            {
                int currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
                GameLog.LogWarning($"[StageFailedPanel] AP 부족! 필요: {apCost}, 보유: {currentAP}");
                WarningUIManager.Instance?.ShowWarning("AP가 부족합니다");
                return;
            }

            // 4. AP 소모
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.AP_ID, apCost);
            GameLog.Log($"[StageFailedPanel] AP {apCost} 소모. 재시작 진행");

            // 5. 씬 전환
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
                GameLog.LogWarning("[StageFailedPanel] Firebase 캐시 없음 - 킬 카운트 저장 스킵");
                return;
            }

            var progression = FirebaseSaveManager.Instance.CachedData.progression;
            if (progression == null)
            {
                GameLog.LogWarning("[StageFailedPanel] Progression 데이터 없음 - 킬 카운트 저장 스킵");
                return;
            }

            // 킬 카운트 누적
            progression.totalKilledMonsters += killCount;
            GameLog.Log($"[StageFailedPanel] 처치 몬스터 누적 (실패): +{killCount}, 총합: {progression.totalKilledMonsters}");

            string oderId = FirebaseManager.Instance?.CurrentUserId;
            if (string.IsNullOrEmpty(oderId))
            {
                GameLog.LogWarning("[StageFailedPanel] 유저 ID 없음 - Firebase 저장 스킵");
                return;
            }

            // Firebase에 진행도 저장
            await FirebaseSaveManager.Instance.SaveProgressionAsync(oderId, progression);

            // 리더보드 업데이트
            await FirebaseSaveManager.Instance.UpdateLeaderboardAsync(oderId, progression.totalKilledMonsters);
        }

        #endregion

        #region Audio Ducking

        /// <summary>
        /// 결과 효과음 재생 시 BGM 볼륨 일시 감소
        /// </summary>
        private async UniTaskVoid DuckBGMForResultSFX(string sfxName)
        {
            var audioManager = AudioManager.Instance;
            if (audioManager == null) return;

            // 원래 볼륨 저장
            float originalVolume = audioManager.GetBGMVolume();

            // 볼륨 낮추기 (0.2 = 20%)
            audioManager.SetBGMVolume(0.2f);

            // 효과음 재생
            audioManager.PlaySFX(sfxName);

            // 2초 대기 (효과음 길이 고려)
            await UniTask.Delay(2000, ignoreTimeScale: true);

            // 원래 볼륨으로 복구
            audioManager.SetBGMVolume(originalVolume);
        }

        #endregion

        #region Scene Loading

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            // Issue #605: 로비 전환 전 게임 일시정지 (사운드 방지)
            TimeManager.Instance?.Pause();
            // Issue #605: 로비 전환 전 모든 사운드 정지
            AudioManager.Instance?.StopAllSounds();
            // FadeController는 Time.unscaledDeltaTime 사용으로 Pause 영향 없음
            await FadeController.Instance.LoadSceneWithFade("LobbyScene");
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene");
        }

        #endregion
    }
}
