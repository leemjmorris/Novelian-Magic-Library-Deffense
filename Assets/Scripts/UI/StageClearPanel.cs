using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using Cysharp.Threading.Tasks;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// JML: 스테이지 클리어 시 표시되는 패널
    /// 로비로 돌아가기 / 다음 스테이지 진행 버튼
    /// </summary>
    public class StageClearPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button nextStageButton;

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

            if (nextStageButton != null)
            {
                nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (lobbyButton != null)
            {
                lobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);
            }

            if (nextStageButton != null)
            {
                nextStageButton.onClick.RemoveListener(OnNextStageButtonClicked);
            }
        }

        /// <summary>
        /// 클리어 패널 표시
        /// </summary>
        public void Show()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            // 다음 스테이지 버튼 활성화/비활성화 체크
            UpdateNextStageButton();

            Debug.Log("[StageClearPanel] Shown");
        }

        /// <summary>
        /// 다음 스테이지 버튼 상태 업데이트
        /// </summary>
        private void UpdateNextStageButton()
        {
            if (nextStageButton == null) return;

            // 다음 스테이지가 있는지 확인
            StageData nextStage = GetNextStageData();
            bool hasNextStage = nextStage != null;

            nextStageButton.interactable = hasNextStage;

            if (!hasNextStage)
            {
                Debug.Log("[StageClearPanel] 마지막 스테이지 - 다음 스테이지 버튼 비활성화");
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
            Debug.Log("[StageClearPanel] Lobby button clicked - Loading LobbyScene");
            Close();
            Time.timeScale = 1f;
            SelectedStage.Clear();
            LoadLobbySceneAsync().Forget();
        }

        /// <summary>
        /// 다음 스테이지로 진행
        /// </summary>
        public void OnNextStageButtonClicked()
        {
            Debug.Log("[StageClearPanel] Next Stage button clicked");

            StageData nextStage = GetNextStageData();
            if (nextStage == null)
            {
                Debug.LogWarning("[StageClearPanel] 다음 스테이지가 없음 - 로비로 이동");
                OnLobbyButtonClicked();
                return;
            }

            // 다음 스테이지가 해금되었는지 확인
            bool isUnlocked = StageProgressManager.Instance?.IsStageUnlocked(nextStage.Chapter_Number) ?? false;
            if (!isUnlocked)
            {
                Debug.LogWarning($"[StageClearPanel] 스테이지 {nextStage.Chapter_Number}이 해금되지 않음");
                OnLobbyButtonClicked();
                return;
            }

            // 다음 스테이지 설정 및 게임 시작
            SelectedStage.Data = nextStage;
            Debug.Log($"[StageClearPanel] 다음 스테이지로 이동: Chapter {nextStage.Chapter_Number}, Stage_ID={nextStage.Stage_ID}");

            Close();
            Time.timeScale = 1f;
            LoadGameSceneAsync().Forget();
        }

        /// <summary>
        /// 다음 스테이지 데이터 가져오기
        /// </summary>
        private StageData GetNextStageData()
        {
            if (!SelectedStage.HasSelection)
            {
                Debug.LogWarning("[StageClearPanel] 현재 스테이지 정보 없음");
                return null;
            }

            int currentChapter = SelectedStage.Data.Chapter_Number;
            int nextChapter = currentChapter + 1;

            // CSV에서 다음 Chapter_Number 스테이지 찾기
            if (CSVLoader.Instance == null)
            {
                Debug.LogError("[StageClearPanel] CSVLoader가 초기화되지 않음");
                return null;
            }

            var table = CSVLoader.Instance.GetTable<StageData>();
            if (table == null)
            {
                Debug.LogError("[StageClearPanel] StageTable이 로드되지 않음");
                return null;
            }

            StageData nextStage = table.Find(s => s.Chapter_Number == nextChapter);
            return nextStage;
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
