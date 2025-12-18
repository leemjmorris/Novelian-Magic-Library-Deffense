using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #476 - 도전던전 결과 팝업
    /// 클리어/실패 결과 표시 및 보상 처리
    /// </summary>
    public class BossDungeonResultPopup : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private GameObject failPanel;

        [Header("Clear UI")]
        [SerializeField] private TextMeshProUGUI clearTitleText;
        [SerializeField] private TextMeshProUGUI clearTimeText;
        [SerializeField] private TextMeshProUGUI clearFloorText;
        [SerializeField] private Transform rewardContainer;
        [SerializeField] private GameObject rewardItemPrefab;

        [Header("Fail UI")]
        [SerializeField] private TextMeshProUGUI failTitleText;
        [SerializeField] private TextMeshProUGUI failReasonText;
        [SerializeField] private TextMeshProUGUI failFloorText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button nextFloorButton;

        [Header("Animation")]
        [SerializeField] private float showDelay = 1f;
        [SerializeField] private Animator animator;

        private BossDungeonData currentDungeonData;

        private void Awake()
        {
            // 초기 상태 숨김
            if (clearPanel != null)
                clearPanel.SetActive(false);

            if (failPanel != null)
                failPanel.SetActive(false);

            // 버튼 이벤트 연결
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryButton);

            if (lobbyButton != null)
                lobbyButton.onClick.AddListener(OnLobbyButton);

            if (nextFloorButton != null)
                nextFloorButton.onClick.AddListener(OnNextFloorButton);
        }

        /// <summary>
        /// 클리어 결과 표시
        /// </summary>
        public void ShowClear(float remainingTime)
        {
            currentDungeonData = SelectedBossDungeon.Data;

            ShowClearAsync(remainingTime).Forget();
        }

        private async UniTaskVoid ShowClearAsync(float remainingTime)
        {
            // 지연 후 표시 (연출용)
            await UniTask.Delay((int)(showDelay * 1000));

            if (clearPanel != null)
            {
                clearPanel.SetActive(true);
            }

            // 클리어 정보 표시
            if (clearTitleText != null)
            {
                clearTitleText.text = "클리어!";
            }

            if (clearTimeText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                clearTimeText.text = $"남은 시간: {minutes:00}:{seconds:00}";
            }

            if (clearFloorText != null && currentDungeonData != null)
            {
                clearFloorText.text = $"{currentDungeonData.Floor_Index}층 클리어";
            }

            // 보상 표시
            DisplayRewards();

            // 다음 층 버튼 활성화 (마지막 층이 아닐 경우)
            if (nextFloorButton != null && currentDungeonData != null)
            {
                // TODO: 최대 층 체크 (현재는 100층까지)
                bool hasNextFloor = currentDungeonData.Floor_Index < 100;
                nextFloorButton.gameObject.SetActive(hasNextFloor);
            }

            // 애니메이션 재생
            if (animator != null)
            {
                animator.SetTrigger("Clear");
            }

            Debug.Log($"[BossDungeonResultPopup] 클리어 표시 - 남은 시간: {remainingTime:F1}초");
        }

        /// <summary>
        /// 실패 결과 표시
        /// </summary>
        public void ShowFail(string reason)
        {
            currentDungeonData = SelectedBossDungeon.Data;

            ShowFailAsync(reason).Forget();
        }

        private async UniTaskVoid ShowFailAsync(string reason)
        {
            // 지연 후 표시 (연출용)
            await UniTask.Delay((int)(showDelay * 1000));

            if (failPanel != null)
            {
                failPanel.SetActive(true);
            }

            // 실패 정보 표시
            if (failTitleText != null)
            {
                failTitleText.text = "실패...";
            }

            if (failReasonText != null)
            {
                failReasonText.text = reason;
            }

            if (failFloorText != null && currentDungeonData != null)
            {
                failFloorText.text = $"{currentDungeonData.Floor_Index}층";
            }

            // 다음 층 버튼 숨김
            if (nextFloorButton != null)
            {
                nextFloorButton.gameObject.SetActive(false);
            }

            // 애니메이션 재생
            if (animator != null)
            {
                animator.SetTrigger("Fail");
            }

            Debug.Log($"[BossDungeonResultPopup] 실패 표시 - 사유: {reason}");
        }

        /// <summary>
        /// 보상 표시
        /// </summary>
        private void DisplayRewards()
        {
            if (rewardContainer == null || rewardItemPrefab == null || currentDungeonData == null)
                return;

            // 기존 보상 아이템 제거
            foreach (Transform child in rewardContainer)
            {
                Destroy(child.gameObject);
            }

            // TODO: Reward_Group_ID로 보상 데이터 조회 및 표시
            // var rewardGroup = CSVLoader.Instance.GetData<RewardGroupData>(currentDungeonData.Reward_Group_ID);
            // foreach (var reward in rewardGroup.Rewards)
            // {
            //     var rewardItem = Instantiate(rewardItemPrefab, rewardContainer);
            //     rewardItem.GetComponent<RewardItemUI>().SetReward(reward);
            // }

            Debug.Log($"[BossDungeonResultPopup] 보상 표시 - Reward_Group_ID: {currentDungeonData.Reward_Group_ID}");
        }

        #region Button Handlers

        /// <summary>
        /// 재도전 버튼
        /// </summary>
        private void OnRetryButton()
        {
            Debug.Log("[BossDungeonResultPopup] 재도전 버튼 클릭");

            // 같은 던전 다시 시작
            LoadSceneAsync(SceneName.BossDungeonScene).Forget();
        }

        /// <summary>
        /// 로비 버튼
        /// </summary>
        private void OnLobbyButton()
        {
            Debug.Log("[BossDungeonResultPopup] 로비 버튼 클릭");

            // 선택 데이터 초기화
            SelectedBossDungeon.Clear();

            LoadSceneAsync(SceneName.LobbyScene).Forget();
        }

        /// <summary>
        /// 다음 층 버튼
        /// </summary>
        private void OnNextFloorButton()
        {
            if (currentDungeonData == null) return;

            Debug.Log("[BossDungeonResultPopup] 다음 층 버튼 클릭");

            // 다음 층 데이터 설정
            int nextFloorIndex = currentDungeonData.Floor_Index + 1;
            var nextDungeonData = CSVLoader.Instance.GetTable<BossDungeonData>()
                .Find(d => d.Floor_Index == nextFloorIndex);

            if (nextDungeonData != null)
            {
                SelectedBossDungeon.Data = nextDungeonData;
                LoadSceneAsync(SceneName.BossDungeonScene).Forget();
            }
            else
            {
                Debug.LogError($"[BossDungeonResultPopup] 다음 층({nextFloorIndex}) 데이터를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 씬 로드 (페이드 효과 포함)
        /// </summary>
        private async UniTaskVoid LoadSceneAsync(string sceneName)
        {
            // 페이드 아웃
            if (FadeController.Instance != null)
            {
                FadeController.Instance.fadePanel.SetActive(true);
                await FadeController.Instance.FadeOut(0.5f);
            }

            // 씬 로드
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            // 페이드 인
            if (FadeController.Instance != null)
            {
                await FadeController.Instance.FadeIn(0.5f);
                FadeController.Instance.fadePanel.SetActive(false);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryButton);

            if (lobbyButton != null)
                lobbyButton.onClick.RemoveListener(OnLobbyButton);

            if (nextFloorButton != null)
                nextFloorButton.onClick.RemoveListener(OnNextFloorButton);
        }
    }
}
