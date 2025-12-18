using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #476 - 도전던전 클리어 패널
    /// StageClearPanel과 동일한 구조
    /// </summary>
    public class BossDungeonClearPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI floorText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI rewardText;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button nextFloorButton;

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

            if (nextFloorButton != null)
                nextFloorButton.onClick.AddListener(OnNextFloorButtonClicked);
        }

        private void OnDestroy()
        {
            if (lobbyButton != null)
                lobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);

            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryButtonClicked);

            if (nextFloorButton != null)
                nextFloorButton.onClick.RemoveListener(OnNextFloorButtonClicked);
        }

        /// <summary>
        /// 클리어 패널 표시
        /// </summary>
        public void Show(float remainingTime)
        {
            currentDungeonData = SelectedBossDungeon.Data;

            // Issue #476: 텍스트를 패널 활성화 전에 먼저 세팅 (텍스트 변경이 눈에 보이지 않도록)
            SetupTexts(remainingTime);

            // 패널 활성화
            GameObject targetPanel = panel != null ? panel : gameObject;
            targetPanel.SetActive(true);

            ShowAsync().Forget();
        }

        /// <summary>
        /// 텍스트 미리 세팅 (패널 활성화 전)
        /// </summary>
        private void SetupTexts(float remainingTime)
        {
            if (titleText != null)
                titleText.text = "클리어!";

            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timeText.text = $"남은 시간: {minutes:00}:{seconds:00}";
            }

            if (floorText != null && currentDungeonData != null)
                floorText.text = $"{currentDungeonData.Floor_Index}층 클리어";

            // 보상 텍스트 세팅
            SetupRewardText();

            // 다음 층 버튼 활성화 (마지막 층이 아닐 경우)
            if (nextFloorButton != null && currentDungeonData != null)
            {
                bool hasNextFloor = currentDungeonData.Floor_Index < 100;
                nextFloorButton.gameObject.SetActive(hasNextFloor);
            }
        }

        private async UniTaskVoid ShowAsync()
        {
            // 지연 후 게임 일시정지 및 보상 지급
            if (showDelay > 0)
            {
                await UniTask.Delay((int)(showDelay * 1000));
            }

            // Issue #476: 게임 일시정지
            Time.timeScale = 0f;

            // 보상 지급 (텍스트는 이미 세팅됨)
            GiveRewardsToPlayer();

            // 다음 층 해금 (Firebase 저장)
            UnlockNextFloor();

            // 애니메이션 재생
            if (animator != null)
            {
                animator.SetTrigger("Clear");
            }

            Debug.Log("[BossDungeonClearPanel] 클리어 표시 완료");
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void Close()
        {
            GameObject targetPanel = panel != null ? panel : gameObject;
            targetPanel.SetActive(false);
        }

        #region Rewards

        /// <summary>
        /// 보상 텍스트만 세팅 (패널 활성화 전에 호출)
        /// </summary>
        private void SetupRewardText()
        {
            if (currentDungeonData == null)
            {
                SetRewardText("보상\n-");
                return;
            }

            int rewardGroupId = currentDungeonData.Reward_Group_ID;
            if (rewardGroupId == 0)
            {
                SetRewardText("보상\n-");
                return;
            }

            var rewardGroupTable = CSVLoader.Instance?.GetTable<RewardGroupData>();
            if (rewardGroupTable == null)
            {
                SetRewardText("보상\n-");
                return;
            }

            RewardGroupData rewardGroup = rewardGroupTable.GetId(rewardGroupId);
            if (rewardGroup == null)
            {
                SetRewardText("보상\n-");
                return;
            }

            string rewardTextStr = GetRewardText(rewardGroup);
            SetRewardText(rewardTextStr);
        }

        /// <summary>
        /// 보상 지급 (딜레이 후 호출)
        /// </summary>
        private void GiveRewardsToPlayer()
        {
            if (currentDungeonData == null) return;

            int rewardGroupId = currentDungeonData.Reward_Group_ID;
            if (rewardGroupId == 0) return;

            var rewardGroupTable = CSVLoader.Instance?.GetTable<RewardGroupData>();
            if (rewardGroupTable == null) return;

            RewardGroupData rewardGroup = rewardGroupTable.GetId(rewardGroupId);
            if (rewardGroup == null) return;

            GiveRewards(rewardGroup);
        }

        private void SetRewardText(string text)
        {
            if (rewardText != null)
            {
                rewardText.text = text;
            }
        }

        private string GetRewardText(RewardGroupData rewardGroup)
        {
            int[] rewardIds = new int[]
            {
                rewardGroup.Reward_1_ID,
                rewardGroup.Reward_2_ID,
                rewardGroup.Reward_3_ID,
                rewardGroup.Reward_4_ID,
                rewardGroup.Reward_5_ID
            };

            var rewardTable = CSVLoader.Instance?.GetTable<RewardData>();
            if (rewardTable == null) return "보상\n-";

            System.Collections.Generic.List<string> rewardStrings = new System.Collections.Generic.List<string>();

            foreach (int rewardId in rewardIds)
            {
                if (rewardId == 0) continue;

                RewardData reward = rewardTable.GetId(rewardId);
                if (reward == null || reward.Item_ID == 0) continue;

                string itemName = GetItemName(reward.Item_ID);
                if (string.IsNullOrEmpty(itemName)) continue;

                string countStr = reward.Min_Count == reward.Max_Count
                    ? $"{reward.Min_Count}"
                    : $"{reward.Min_Count}~{reward.Max_Count}";

                rewardStrings.Add($"{itemName} x{countStr}");
            }

            if (rewardStrings.Count == 0) return "보상\n-";

            return "보상\n" + string.Join(", ", rewardStrings);
        }

        private void GiveRewards(RewardGroupData rewardGroup)
        {
            int[] rewardIds = new int[]
            {
                rewardGroup.Reward_1_ID,
                rewardGroup.Reward_2_ID,
                rewardGroup.Reward_3_ID,
                rewardGroup.Reward_4_ID,
                rewardGroup.Reward_5_ID
            };

            var rewardTable = CSVLoader.Instance?.GetTable<RewardData>();
            if (rewardTable == null) return;

            foreach (int rewardId in rewardIds)
            {
                if (rewardId == 0) continue;

                RewardData reward = rewardTable.GetId(rewardId);
                if (reward == null || reward.Item_ID == 0) continue;

                int amount = reward.Min_Count == reward.Max_Count
                    ? reward.Min_Count
                    : Random.Range(reward.Min_Count, reward.Max_Count + 1);

                GiveItem(reward.Item_ID, amount);
            }
        }

        private void GiveItem(int itemId, int amount)
        {
            if (itemId >= 1600 && itemId < 1700)
            {
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.AddCurrency(itemId, amount);
                    Debug.Log($"[BossDungeonClearPanel] Currency 지급: ID={itemId}, Amount={amount}");
                }
            }
            else if (itemId >= 10000)
            {
                if (IngredientManager.Instance != null)
                {
                    IngredientManager.Instance.AddIngredient(itemId, amount);
                    Debug.Log($"[BossDungeonClearPanel] Ingredient 지급: ID={itemId}, Amount={amount}");
                }
            }
        }

        private string GetItemName(int itemId)
        {
            int nameId = 0;

            if (itemId >= 1600 && itemId < 1700)
            {
                var currencyTable = CSVLoader.Instance?.GetTable<CurrencyData>();
                CurrencyData currency = currencyTable?.GetId(itemId);
                if (currency != null) nameId = currency.Currency_Name_ID;
            }
            else if (itemId >= 10000)
            {
                var ingredientTable = CSVLoader.Instance?.GetTable<IngredientData>();
                IngredientData ingredient = ingredientTable?.GetId(itemId);
                if (ingredient != null) nameId = ingredient.Ingredient_Name_ID;
            }

            if (nameId == 0) return null;

            var stringTable = CSVLoader.Instance?.GetTable<StringTable>();
            StringTable stringData = stringTable?.GetId(nameId);
            return stringData?.Text;
        }

        #endregion

        #region Floor Unlock

        private void UnlockNextFloor()
        {
            if (currentDungeonData == null) return;

            int clearedFloor = currentDungeonData.Floor_Index;

            if (FirebaseSaveManager.Instance?.CachedData?.progression != null &&
                FirebaseManager.Instance != null)
            {
                int currentProgress = FirebaseSaveManager.Instance.CachedData.progression.bossDungeonProgress;

                if (clearedFloor >= currentProgress)
                {
                    int nextFloor = clearedFloor + 1;
                    FirebaseSaveManager.Instance.CachedData.progression.bossDungeonProgress = nextFloor;
                    FirebaseSaveManager.Instance.SaveProgressionAsync(
                        FirebaseManager.Instance.CurrentUserId,
                        FirebaseSaveManager.Instance.CachedData.progression
                    ).Forget();
                    Debug.Log($"[BossDungeonClearPanel] 다음 층 해금: {nextFloor}층");
                }
            }
        }

        #endregion

        #region Button Handlers

        private void OnLobbyButtonClicked()
        {
            Debug.Log("[BossDungeonClearPanel] 로비 버튼 클릭");
            Time.timeScale = 1f;
            Close();
            SelectedBossDungeon.Clear();
            LoadSceneAsync(SceneName.LobbyScene).Forget();
        }

        private void OnRetryButtonClicked()
        {
            Debug.Log("[BossDungeonClearPanel] 재도전 버튼 클릭");
            Time.timeScale = 1f;
            Close();
            LoadSceneAsync(SceneName.BossDungeonScene).Forget();
        }

        private void OnNextFloorButtonClicked()
        {
            if (currentDungeonData == null) return;

            Time.timeScale = 1f;

            int nextFloorIndex = currentDungeonData.Floor_Index + 1;
            var nextDungeonData = CSVLoader.Instance?.GetTable<BossDungeonData>()
                ?.Find(d => d.Floor_Index == nextFloorIndex);

            if (nextDungeonData != null)
            {
                SelectedBossDungeon.Data = nextDungeonData;
                Close();
                LoadSceneAsync(SceneName.BossDungeonScene).Forget();
            }
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
