using UnityEngine;
using UnityEngine.UI;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using Cysharp.Threading.Tasks;
using TMPro;

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

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI stageNameText;
        [SerializeField] private TextMeshProUGUI progressTimeText;
        [SerializeField] private TextMeshProUGUI rewardText;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button nextStageButton;

        // 캐시된 결과 데이터
        private float cachedProgressTime;
        private int cachedKillCount;
        private float cachedWallHpRatio; // Wall HP 비율 (0.0 ~ 1.0)

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
        /// 클리어 패널 표시 (데이터 포함)
        /// </summary>
        public void Show(float progressTime, int killCount, float wallHpRatio)
        {
            cachedProgressTime = progressTime;
            cachedKillCount = killCount;
            cachedWallHpRatio = wallHpRatio;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            // 스테이지 정보 표시
            UpdateStageInfo();

            // 다음 스테이지 버튼 활성화/비활성화 체크
            UpdateNextStageButton();

            // 보상 지급
            GiveRewards();

            Debug.Log($"[StageClearPanel] Shown - Time: {progressTime:F1}s, Kills: {killCount}, WallHP: {wallHpRatio:P0}");
        }

        /// <summary>
        /// 클리어 패널 표시 (데이터 없이 - 하위 호환)
        /// </summary>
        public void Show()
        {
            Show(0f, 0, 1f);
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

            // 랭크 계산 및 표시
            if (rankText != null)
            {
                rankText.text = CalculateRank();
            }

            // 진행시간 + 처치 몬스터
            if (progressTimeText != null)
            {
                int minutes = (int)(cachedProgressTime / 60);
                int seconds = (int)(cachedProgressTime % 60);
                progressTimeText.text = $"진행시간: {minutes:D2}:{seconds:D2}\n처치 몬스터: {cachedKillCount} 마리";
            }

            // 보상 표시
            if (rewardText != null)
            {
                rewardText.text = GetRewardText();
            }
        }

        /// <summary>
        /// 랭크 계산 (Wall HP 비율 기반)
        /// S: 91~100%, A: 71~90%, B: 51~70%, C: 0~50%
        /// </summary>
        private string CalculateRank()
        {
            // Wall HP 비율에 따른 랭크 (HP가 많이 남을수록 높은 랭크)
            if (cachedWallHpRatio >= 0.91f) return "S";
            if (cachedWallHpRatio >= 0.71f) return "A";
            if (cachedWallHpRatio >= 0.51f) return "B";
            return "C";
        }

        /// <summary>
        /// 보상 텍스트 생성 (CSV 연동)
        /// </summary>
        private string GetRewardText()
        {
            const string FALLBACK_REWARD = "보상\n1500G";

            if (!SelectedStage.HasSelection)
            {
                return "보상\n-";
            }

            // RewardGroupTable에서 Reward_Group_ID로 조회
            int rewardGroupId = SelectedStage.Data.Reward_Group_ID;
            if (rewardGroupId == 0)
            {
                return FALLBACK_REWARD;
            }

            var rewardGroupTable = CSVLoader.Instance?.GetTable<RewardGroupData>();
            if (rewardGroupTable == null)
            {
                Debug.LogWarning("[StageClearPanel] RewardGroupTable not loaded");
                return FALLBACK_REWARD;
            }

            RewardGroupData rewardGroup = rewardGroupTable.GetId(rewardGroupId);
            if (rewardGroup == null)
            {
                Debug.LogWarning($"[StageClearPanel] RewardGroup not found: {rewardGroupId}");
                return FALLBACK_REWARD;
            }

            // Reward_X_ID들 수집 (0이 아닌 것만)
            int[] rewardIds = new int[]
            {
                rewardGroup.Reward_1_ID,
                rewardGroup.Reward_2_ID,
                rewardGroup.Reward_3_ID,
                rewardGroup.Reward_4_ID,
                rewardGroup.Reward_5_ID
            };

            var rewardTable = CSVLoader.Instance?.GetTable<RewardData>();
            if (rewardTable == null)
            {
                Debug.LogWarning("[StageClearPanel] RewardTable not loaded");
                return FALLBACK_REWARD;
            }

            System.Collections.Generic.List<string> rewardStrings = new System.Collections.Generic.List<string>();

            foreach (int rewardId in rewardIds)
            {
                if (rewardId == 0) continue;

                RewardData reward = rewardTable.GetId(rewardId);
                if (reward == null || reward.Item_ID == 0) continue;

                string itemName = GetItemName(reward.Item_ID);
                if (string.IsNullOrEmpty(itemName)) continue;

                // 수량 표시 (Min_Count == Max_Count면 단일 수량, 아니면 범위)
                string countStr = reward.Min_Count == reward.Max_Count
                    ? $"{reward.Min_Count}"
                    : $"{reward.Min_Count}~{reward.Max_Count}";

                rewardStrings.Add($"{itemName} x{countStr}");
            }

            // 유효한 보상이 없으면 fallback
            if (rewardStrings.Count == 0)
            {
                return FALLBACK_REWARD;
            }

            return "보상\n" + string.Join(", ", rewardStrings);
        }

        /// <summary>
        /// 실제 보상 지급 (CSV 데이터 없으면 1500G fallback)
        /// </summary>
        private void GiveRewards()
        {
            const int FALLBACK_GOLD = 1500;
            const int GOLD_CURRENCY_ID = 1601; // 골드 Currency ID

            // CSV 데이터가 없으면 1500G fallback
            if (!SelectedStage.HasSelection)
            {
                GiveFallbackReward(GOLD_CURRENCY_ID, FALLBACK_GOLD);
                return;
            }

            int rewardGroupId = SelectedStage.Data.Reward_Group_ID;
            if (rewardGroupId == 0)
            {
                GiveFallbackReward(GOLD_CURRENCY_ID, FALLBACK_GOLD);
                return;
            }

            var rewardGroupTable = CSVLoader.Instance?.GetTable<RewardGroupData>();
            if (rewardGroupTable == null)
            {
                GiveFallbackReward(GOLD_CURRENCY_ID, FALLBACK_GOLD);
                return;
            }

            RewardGroupData rewardGroup = rewardGroupTable.GetId(rewardGroupId);
            if (rewardGroup == null)
            {
                GiveFallbackReward(GOLD_CURRENCY_ID, FALLBACK_GOLD);
                return;
            }

            // Reward_X_ID들 수집 (0이 아닌 것만)
            int[] rewardIds = new int[]
            {
                rewardGroup.Reward_1_ID,
                rewardGroup.Reward_2_ID,
                rewardGroup.Reward_3_ID,
                rewardGroup.Reward_4_ID,
                rewardGroup.Reward_5_ID
            };

            var rewardTable = CSVLoader.Instance?.GetTable<RewardData>();
            if (rewardTable == null)
            {
                GiveFallbackReward(GOLD_CURRENCY_ID, FALLBACK_GOLD);
                return;
            }

            bool anyRewardGiven = false;

            foreach (int rewardId in rewardIds)
            {
                if (rewardId == 0) continue;

                RewardData reward = rewardTable.GetId(rewardId);
                if (reward == null || reward.Item_ID == 0) continue;

                // 수량 결정 (Min~Max 범위 내 랜덤)
                int amount = reward.Min_Count == reward.Max_Count
                    ? reward.Min_Count
                    : Random.Range(reward.Min_Count, reward.Max_Count + 1);

                // 아이템 지급
                GiveItem(reward.Item_ID, amount);
                anyRewardGiven = true;
            }

            // 유효한 보상이 없으면 fallback
            if (!anyRewardGiven)
            {
                GiveFallbackReward(GOLD_CURRENCY_ID, FALLBACK_GOLD);
            }
        }

        /// <summary>
        /// Fallback 보상 지급 (1500G)
        /// </summary>
        private void GiveFallbackReward(int currencyId, int amount)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCurrency(currencyId, amount);
                Debug.Log($"[StageClearPanel] Fallback 보상 지급: {amount}G");
            }
        }

        /// <summary>
        /// 아이템 지급 (Currency 또는 Ingredient)
        /// </summary>
        private void GiveItem(int itemId, int amount)
        {
            // Item_ID 범위로 타입 구분
            // 1600번대: Currency
            // 10000번대: Ingredient

            if (itemId >= 1600 && itemId < 1700)
            {
                // Currency 지급
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.AddCurrency(itemId, amount);
                    Debug.Log($"[StageClearPanel] Currency 지급: ID={itemId}, Amount={amount}");
                }
            }
            else if (itemId >= 10000)
            {
                // Ingredient 지급
                if (IngredientManager.Instance != null)
                {
                    IngredientManager.Instance.AddIngredient(itemId, amount);
                    Debug.Log($"[StageClearPanel] Ingredient 지급: ID={itemId}, Amount={amount}");
                }
            }
            else
            {
                Debug.LogWarning($"[StageClearPanel] 알 수 없는 Item_ID: {itemId}");
            }
        }

        /// <summary>
        /// Item_ID로 아이템 이름 조회 (Currency 또는 Ingredient)
        /// </summary>
        private string GetItemName(int itemId)
        {
            // Item_ID 범위로 타입 구분
            // 1600번대: Currency
            // 10000번대: Ingredient

            int nameId = 0;

            if (itemId >= 1600 && itemId < 1700)
            {
                // Currency
                var currencyTable = CSVLoader.Instance?.GetTable<CurrencyData>();
                CurrencyData currency = currencyTable?.GetId(itemId);
                if (currency != null)
                {
                    nameId = currency.Currency_Name_ID;
                }
            }
            else if (itemId >= 10000)
            {
                // Ingredient
                var ingredientTable = CSVLoader.Instance?.GetTable<IngredientData>();
                IngredientData ingredient = ingredientTable?.GetId(itemId);
                if (ingredient != null)
                {
                    nameId = ingredient.Ingredient_Name_ID;
                }
            }

            if (nameId == 0) return null;

            // StringTable에서 이름 조회
            var stringTable = CSVLoader.Instance?.GetTable<StringTable>();
            StringTable stringData = stringTable?.GetId(nameId);
            return stringData?.Text;
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
