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
            // BossDungeonUI.Awake()에서 gameObject가 비활성화되므로 먼저 활성화
            gameObject.SetActive(true);

            // 패배 효과음 재생
            AudioManager.Instance?.PlaySFX("LoseSFX");

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

            GameLog.Log($"[BossDungeonFailedPanel] Shown - Time: {progressTime:F1}s");
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
            GameLog.Log("[BossDungeonFailedPanel] Lobby button clicked - Loading LobbyScene");
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
            GameLog.Log("[BossDungeonFailedPanel] Retry button clicked - Checking dungeon pass and reloading");

            // 1. SelectedBossDungeon 데이터 확인
            if (!SelectedBossDungeon.HasSelection)
            {
                GameLog.LogError("[BossDungeonFailedPanel] 선택된 던전이 없습니다");
                return;
            }

            // 2. CurrencyManager 확인
            if (CurrencyManager.Instance == null)
            {
                GameLog.LogError("[BossDungeonFailedPanel] CurrencyManager가 초기화되지 않음");
                return;
            }

            const int ENTRY_COST = 1;

            // 3. 던전 출입증 잔량 확인
            if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST))
            {
                int owned = CurrencyManager.Instance.GetCurrency(CurrencyManager.DUNGEON_PASS_ID);
                GameLog.LogWarning($"[BossDungeonFailedPanel] 던전 출입증 부족! 필요: {ENTRY_COST}, 보유: {owned}");
                WarningUIManager.Instance?.ShowWarning("던전 출입증이 부족합니다");
                return;
            }

            // 4. 던전 출입증 소모
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST);
            GameLog.Log($"[BossDungeonFailedPanel] 던전 출입증 {ENTRY_COST}개 소모. 재도전 진행");

            // 5. 씬 전환
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
                    GameLog.Log($"[BossDungeonFailedPanel] {currentDungeonData.Floor_Index}층은 이미 클리어됨 - 실패 기록 스킵");
                    return;
                }

                // 실패 기록 저장
                progression.bossDungeonAttempted[key] = true;
                FirebaseSaveManager.Instance.SaveProgressionAsync(
                    FirebaseManager.Instance.CurrentUserId,
                    progression
                ).Forget();

                GameLog.Log($"[BossDungeonFailedPanel] 실패 기록 저장: {currentDungeonData.Floor_Index}층");
            }
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
            await FadeController.Instance.LoadSceneWithFade(SceneName.LobbyScene);
        }

        private async UniTaskVoid LoadBossDungeonSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade(SceneName.BossDungeonScene);
        }

        #endregion
    }
}
