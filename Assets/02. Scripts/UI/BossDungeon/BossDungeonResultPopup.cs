using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
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
        [SerializeField] private TextMeshProUGUI rewardText;

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

            // 보상 표시 및 지급
            DisplayRewards();

            // 다음 층 해금 (Firebase 저장)
            UnlockNextFloor();

            // 다음 층 버튼 활성화 (다음 층 데이터가 존재하고 구현된 경우만)
            if (nextFloorButton != null && currentDungeonData != null)
            {
                int nextFloorIndex = currentDungeonData.Floor_Index + 1;
                var nextDungeonData = CSVLoader.Instance?.GetTable<BossDungeonData>()
                    ?.Find(d => d.Floor_Index == nextFloorIndex);
                bool hasNextFloor = nextDungeonData != null && nextDungeonData.IsImplemented;
                nextFloorButton.gameObject.SetActive(hasNextFloor);
            }

            // 애니메이션 재생
            if (animator != null)
            {
                animator.SetTrigger("Clear");
            }

            GameLog.Log($"[BossDungeonResultPopup] 클리어 표시 - 남은 시간: {remainingTime:F1}초");
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

            GameLog.Log($"[BossDungeonResultPopup] 실패 표시 - 사유: {reason}");
        }

        /// <summary>
        /// 보상 표시 및 지급
        /// </summary>
        private void DisplayRewards()
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
                GameLog.Log("[BossDungeonResultPopup] Reward_Group_ID가 0입니다.");
                return;
            }

            var rewardGroupTable = CSVLoader.Instance?.GetTable<RewardGroupData>();
            if (rewardGroupTable == null)
            {
                SetRewardText("보상\n-");
                GameLog.LogWarning("[BossDungeonResultPopup] RewardGroupTable not loaded");
                return;
            }

            RewardGroupData rewardGroup = rewardGroupTable.GetId(rewardGroupId);
            if (rewardGroup == null)
            {
                SetRewardText("보상\n-");
                GameLog.LogWarning($"[BossDungeonResultPopup] RewardGroup not found: {rewardGroupId}");
                return;
            }

            // 보상 텍스트 생성 및 지급
            string rewardTextStr = GetRewardText(rewardGroup);
            SetRewardText(rewardTextStr);
            GiveRewards(rewardGroup);

            GameLog.Log($"[BossDungeonResultPopup] 보상 표시 및 지급 완료 - Reward_Group_ID: {rewardGroupId}");
        }

        /// <summary>
        /// 보상 텍스트 설정
        /// </summary>
        private void SetRewardText(string text)
        {
            if (rewardText != null)
            {
                rewardText.text = text;
            }
        }

        /// <summary>
        /// 보상 텍스트 생성
        /// </summary>
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
            if (rewardTable == null)
            {
                return "보상\n-";
            }

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

            if (rewardStrings.Count == 0)
            {
                return "보상\n-";
            }

            return "보상\n" + string.Join(", ", rewardStrings);
        }

        /// <summary>
        /// 보상 지급
        /// </summary>
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

                // 수량 결정 (Min~Max 범위 내 랜덤)
                int amount = reward.Min_Count == reward.Max_Count
                    ? reward.Min_Count
                    : UnityEngine.Random.Range(reward.Min_Count, reward.Max_Count + 1);

                GiveItem(reward.Item_ID, amount);
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
                    GameLog.Log($"[BossDungeonResultPopup] Currency 지급: ID={itemId}, Amount={amount}");
                }
            }
            else if (itemId >= 10000)
            {
                // Ingredient 지급
                if (IngredientManager.Instance != null)
                {
                    IngredientManager.Instance.AddIngredient(itemId, amount);
                    GameLog.Log($"[BossDungeonResultPopup] Ingredient 지급: ID={itemId}, Amount={amount}");
                }
            }
            else
            {
                GameLog.LogWarning($"[BossDungeonResultPopup] 알 수 없는 Item_ID: {itemId}");
            }
        }

        /// <summary>
        /// Item_ID로 아이템 이름 조회
        /// </summary>
        private string GetItemName(int itemId)
        {
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
        /// 다음 층 해금 (Firebase 저장)
        /// </summary>
        private void UnlockNextFloor()
        {
            if (currentDungeonData == null) return;

            int clearedFloor = currentDungeonData.Floor_Index;

            if (FirebaseSaveManager.Instance?.CachedData?.progression != null &&
                FirebaseManager.Instance != null)
            {
                int currentProgress = FirebaseSaveManager.Instance.CachedData.progression.bossDungeonProgress;

                // 현재 진행도보다 높은 층을 클리어한 경우에만 해금
                if (clearedFloor >= currentProgress)
                {
                    int nextFloor = clearedFloor + 1;
                    FirebaseSaveManager.Instance.CachedData.progression.bossDungeonProgress = nextFloor;
                    FirebaseSaveManager.Instance.SaveProgressionAsync(
                        FirebaseManager.Instance.CurrentUserId,
                        FirebaseSaveManager.Instance.CachedData.progression
                    ).Forget();
                    GameLog.Log($"[BossDungeonResultPopup] 다음 층 해금: {nextFloor}층");
                }
            }
        }

        #region Button Handlers

        /// <summary>
        /// 재도전 버튼
        /// </summary>
        private void OnRetryButton()
        {
            GameLog.Log("[BossDungeonResultPopup] 재도전 버튼 클릭 - 던전 출입증 확인");

            // 1. SelectedBossDungeon 데이터 확인
            if (!SelectedBossDungeon.HasSelection)
            {
                GameLog.LogError("[BossDungeonResultPopup] 선택된 던전이 없습니다");
                return;
            }

            // 2. CurrencyManager 확인
            if (CurrencyManager.Instance == null)
            {
                GameLog.LogError("[BossDungeonResultPopup] CurrencyManager가 초기화되지 않음");
                return;
            }

            const int ENTRY_COST = 1;

            // 3. 던전 출입증 잔량 확인
            if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST))
            {
                int owned = CurrencyManager.Instance.GetCurrency(CurrencyManager.DUNGEON_PASS_ID);
                GameLog.LogWarning($"[BossDungeonResultPopup] 던전 출입증 부족! 필요: {ENTRY_COST}, 보유: {owned}");
                WarningUIManager.Instance?.ShowWarning("던전 출입증이 부족합니다");
                return;
            }

            // 4. 던전 출입증 소모
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST);
            GameLog.Log($"[BossDungeonResultPopup] 던전 출입증 {ENTRY_COST}개 소모. 재도전 진행");

            // 5. 씬 전환
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();

            // 같은 던전 다시 시작
            LoadSceneAsync(SceneName.BossDungeonScene).Forget();
        }

        /// <summary>
        /// 로비 버튼
        /// </summary>
        private void OnLobbyButton()
        {
            GameLog.Log("[BossDungeonResultPopup] 로비 버튼 클릭");

            // Issue #605: 로비 전환 전 게임 일시정지 (사운드 방지)
            TimeManager.Instance?.Pause();
            // Issue #605: 로비 전환 전 모든 사운드 정지
            AudioManager.Instance?.StopAllSounds();
            // FadeController는 Time.unscaledDeltaTime 사용으로 Pause 영향 없음

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

            GameLog.Log("[BossDungeonResultPopup] 다음 층 버튼 클릭 - 던전 출입증 확인");

            // 1. CurrencyManager 확인
            if (CurrencyManager.Instance == null)
            {
                GameLog.LogError("[BossDungeonResultPopup] CurrencyManager가 초기화되지 않음");
                return;
            }

            const int ENTRY_COST = 1;

            // 2. 던전 출입증 잔량 확인
            if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST))
            {
                int owned = CurrencyManager.Instance.GetCurrency(CurrencyManager.DUNGEON_PASS_ID);
                GameLog.LogWarning($"[BossDungeonResultPopup] 던전 출입증 부족! 필요: {ENTRY_COST}, 보유: {owned}");
                WarningUIManager.Instance?.ShowWarning("던전 출입증이 부족합니다");
                return;
            }

            // 3. 다음 층 데이터 설정
            int nextFloorIndex = currentDungeonData.Floor_Index + 1;
            var nextDungeonData = CSVLoader.Instance.GetTable<BossDungeonData>()
                .Find(d => d.Floor_Index == nextFloorIndex);

            if (nextDungeonData == null)
            {
                GameLog.LogError($"[BossDungeonResultPopup] 다음 층({nextFloorIndex}) 데이터를 찾을 수 없습니다!");
                return;
            }

            if (!nextDungeonData.IsImplemented)
            {
                GameLog.LogError($"[BossDungeonResultPopup] 다음 층({nextFloorIndex})은 아직 구현되지 않았습니다!");
                return;
            }

            // 4. 던전 출입증 소모
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST);
            GameLog.Log($"[BossDungeonResultPopup] 던전 출입증 {ENTRY_COST}개 소모. 다음 층 진행");

            // 5. 씬 전환
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();

            SelectedBossDungeon.Data = nextDungeonData;
            LoadSceneAsync(SceneName.BossDungeonScene).Forget();
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
