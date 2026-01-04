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

        [Header("Rank Image")]
        [SerializeField] private Image rankImage;
        [SerializeField] private Sprite[] rankSprites; // 순서: S(0), A(1), B(2), F(3)

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI floorText;
        [SerializeField] private TextMeshProUGUI progressTimeText;
        [SerializeField] private TextMeshProUGUI rewardText;

        [Header("Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button nextFloorButton;

        // 캐시된 결과 데이터
        private float cachedRemainingTime;
        private float cachedTimeLimit;
        private BossDungeonData currentDungeonData;

        // 실제 획득한 보상 캐시 (아이템ID, 수량)
        private System.Collections.Generic.List<(int itemId, int amount)> earnedRewards = new System.Collections.Generic.List<(int, int)>();

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

            if (nextFloorButton != null)
                nextFloorButton.onClick.AddListener(OnNextFloorButtonClicked);
        }

        private void OnDestroy()
        {
            if (lobbyButton != null)
                lobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);

            if (nextFloorButton != null)
                nextFloorButton.onClick.RemoveListener(OnNextFloorButtonClicked);
        }

        /// <summary>
        /// 클리어 패널 표시 (데이터 포함)
        /// </summary>
        public void Show(float remainingTime, float timeLimit)
        {
            cachedRemainingTime = remainingTime;
            cachedTimeLimit = timeLimit;
            currentDungeonData = SelectedBossDungeon.Data;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            // 보상 계산 및 지급 (먼저 실행하여 earnedRewards 채움)
            GiveRewards();

            // 층 정보 표시 (획득한 보상 기반)
            UpdateFloorInfo();

            // 다음 층 버튼 활성화/비활성화 체크
            UpdateNextFloorButton();

            Debug.Log($"[BossDungeonClearPanel] Shown - RemainingTime: {remainingTime:F1}s, TimeLimit: {timeLimit:F1}s");
        }

        /// <summary>
        /// 클리어 패널 표시 (데이터 없이 - 하위 호환)
        /// </summary>
        public void Show()
        {
            Show(0f, 60f);
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

            // 랭크 계산 및 이미지 표시
            if (rankImage != null && rankSprites != null && rankSprites.Length > 0)
            {
                int rankIndex = CalculateRankIndex();
                if (rankIndex >= 0 && rankIndex < rankSprites.Length)
                {
                    rankImage.sprite = rankSprites[rankIndex];
                }
            }

            // 남은 시간 표시
            if (progressTimeText != null)
            {
                int minutes = (int)(cachedRemainingTime / 60);
                int seconds = (int)(cachedRemainingTime % 60);
                progressTimeText.text = $"남은 시간: {minutes:D2}:{seconds:D2}";
            }

            // 보상 표시
            if (rewardText != null)
            {
                rewardText.text = GetRewardText();
            }
        }

        /// <summary>
        /// 랭크 인덱스 계산
        /// 도전던전: 클리어하면 무조건 S랭크 반환
        /// (일반 스테이지와 달리 시간 기반 랭크 미사용)
        /// </summary>
        private int CalculateRankIndex()
        {
            // 도전던전 전용: 클리어 = S랭크 (index 0)
            return 0;
        }

        /// <summary>
        /// 보상 텍스트 생성 (실제 획득한 보상 기반)
        /// </summary>
        private string GetRewardText()
        {
            if (earnedRewards.Count == 0)
            {
                return "보상\n-";
            }

            System.Collections.Generic.List<string> rewardStrings = new System.Collections.Generic.List<string>();

            foreach (var (itemId, amount) in earnedRewards)
            {
                string itemName = GetItemName(itemId);
                if (string.IsNullOrEmpty(itemName))
                {
                    itemName = $"아이템 {itemId}";
                }
                rewardStrings.Add($"{itemName} x{amount}");
            }

            return "보상\n" + string.Join(", ", rewardStrings);
        }

        /// <summary>
        /// 실제 보상 지급 (확률 계산 포함)
        /// </summary>
        private void GiveRewards()
        {
            // 이전 보상 캐시 초기화
            earnedRewards.Clear();

            if (currentDungeonData == null)
            {
                return;
            }

            int rewardGroupId = currentDungeonData.Reward_Group_ID;
            if (rewardGroupId == 0)
            {
                return;
            }

            var rewardGroupTable = CSVLoader.Instance?.GetTable<RewardGroupData>();
            if (rewardGroupTable == null)
            {
                return;
            }

            RewardGroupData rewardGroup = rewardGroupTable.GetId(rewardGroupId);
            if (rewardGroup == null)
            {
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
                return;
            }

            foreach (int rewardId in rewardIds)
            {
                if (rewardId == 0) continue;

                RewardData reward = rewardTable.GetId(rewardId);
                if (reward == null || reward.Item_ID == 0) continue;

                // 확률 체크 (Is_Fixed가 true면 100% 획득, 아니면 Probability로 결정)
                bool shouldGive = reward.Is_Fixed || Random.value <= reward.Probability;

                if (!shouldGive)
                {
                    Debug.Log($"[BossDungeonClearPanel] 확률 실패: Item_ID={reward.Item_ID}, Probability={reward.Probability * 100:F1}%");
                    continue;
                }

                // 수량 결정 (Min~Max 범위 내 랜덤)
                int amount = reward.Min_Count == reward.Max_Count
                    ? reward.Min_Count
                    : Random.Range(reward.Min_Count, reward.Max_Count + 1);

                // 아이템 지급
                GiveItem(reward.Item_ID, amount);

                // 획득 보상 캐시에 추가
                earnedRewards.Add((reward.Item_ID, amount));
            }
        }

        /// <summary>
        /// 아이템 지급 (Currency 또는 Ingredient)
        /// </summary>
        private void GiveItem(int itemId, int amount)
        {
            if (itemId >= 1600 && itemId < 1700)
            {
                // Currency 지급
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.AddCurrency(itemId, amount);
                    Debug.Log($"[BossDungeonClearPanel] Currency 지급: ID={itemId}, Amount={amount}");
                }
            }
            else if (itemId >= 10000)
            {
                // Ingredient 지급
                if (IngredientManager.Instance != null)
                {
                    IngredientManager.Instance.AddIngredient(itemId, amount);
                    Debug.Log($"[BossDungeonClearPanel] Ingredient 지급: ID={itemId}, Amount={amount}");
                }
            }
            else
            {
                Debug.LogWarning($"[BossDungeonClearPanel] 알 수 없는 Item_ID: {itemId}");
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
                var currencyTable = CSVLoader.Instance?.GetTable<CurrencyData>();
                CurrencyData currency = currencyTable?.GetId(itemId);
                if (currency != null)
                {
                    nameId = currency.Currency_Name_ID;
                }
            }
            else if (itemId >= 10000)
            {
                var ingredientTable = CSVLoader.Instance?.GetTable<IngredientData>();
                IngredientData ingredient = ingredientTable?.GetId(itemId);
                if (ingredient != null)
                {
                    nameId = ingredient.Ingredient_Name_ID;
                }
            }

            if (nameId == 0) return null;

            var stringTable = CSVLoader.Instance?.GetTable<StringTable>();
            StringTable stringData = stringTable?.GetId(nameId);
            return stringData?.Text;
        }

        /// <summary>
        /// 다음 층 버튼 상태 업데이트
        /// </summary>
        private void UpdateNextFloorButton()
        {
            if (nextFloorButton == null || currentDungeonData == null) return;

            // 다음 층 데이터 확인
            int nextFloorIndex = currentDungeonData.Floor_Index + 1;
            var nextDungeonData = CSVLoader.Instance?.GetTable<BossDungeonData>()
                ?.Find(d => d.Floor_Index == nextFloorIndex);

            bool hasNextFloor = nextDungeonData != null && nextDungeonData.IsImplemented;
            nextFloorButton.interactable = hasNextFloor;

            if (!hasNextFloor)
            {
                Debug.Log("[BossDungeonClearPanel] 마지막 층 - 다음 층 버튼 비활성화");
            }

            // 다음 층 해금 (Firebase 저장)
            UnlockNextFloor();
        }

        /// <summary>
        /// 다음 층 해금
        /// </summary>
        private void UnlockNextFloor()
        {
            if (currentDungeonData == null) return;

            int clearedFloor = currentDungeonData.Floor_Index;

            if (FirebaseSaveManager.Instance?.CachedData?.progression != null &&
                FirebaseManager.Instance != null)
            {
                var progression = FirebaseSaveManager.Instance.CachedData.progression;
                int currentProgress = progression.bossDungeonProgress;

                if (clearedFloor >= currentProgress)
                {
                    int nextFloor = clearedFloor + 1;
                    progression.bossDungeonProgress = nextFloor;

                    // Issue #645: 클리어한 층의 실패 기록 제거
                    RemoveAttemptRecord(clearedFloor, progression);

                    FirebaseSaveManager.Instance.SaveProgressionAsync(
                        FirebaseManager.Instance.CurrentUserId,
                        progression
                    ).Forget();
                    Debug.Log($"[BossDungeonClearPanel] 다음 층 해금: {nextFloor}층");
                }
            }
        }

        /// <summary>
        /// Issue #645: 클리어한 층의 실패 기록 제거
        /// </summary>
        private void RemoveAttemptRecord(int floorIndex, Firebase.Data.ProgressionData progression)
        {
            if (progression.bossDungeonAttempted == null) return;

            string key = floorIndex.ToString();
            if (progression.bossDungeonAttempted.ContainsKey(key))
            {
                progression.bossDungeonAttempted.Remove(key);
                Debug.Log($"[BossDungeonClearPanel] 실패 기록 제거: {floorIndex}층");
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
            Debug.Log("[BossDungeonClearPanel] Lobby button clicked - Loading LobbyScene");
            Close();
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();
            SelectedBossDungeon.Clear();
            LoadLobbySceneAsync().Forget();
        }

        /// <summary>
        /// 다음 층으로 진행
        /// </summary>
        public void OnNextFloorButtonClicked()
        {
            Debug.Log("[BossDungeonClearPanel] Next Floor button clicked");

            if (currentDungeonData == null)
            {
                Debug.LogWarning("[BossDungeonClearPanel] 현재 던전 정보 없음 - 로비로 이동");
                OnLobbyButtonClicked();
                return;
            }

            int nextFloorIndex = currentDungeonData.Floor_Index + 1;
            var nextDungeonData = CSVLoader.Instance?.GetTable<BossDungeonData>()
                ?.Find(d => d.Floor_Index == nextFloorIndex);

            if (nextDungeonData == null || !nextDungeonData.IsImplemented)
            {
                Debug.LogWarning($"[BossDungeonClearPanel] 다음 층({nextFloorIndex}) 없음 - 로비로 이동");
                OnLobbyButtonClicked();
                return;
            }

            // 다음 층 설정 및 게임 시작
            SelectedBossDungeon.Data = nextDungeonData;
            Debug.Log($"[BossDungeonClearPanel] 다음 층으로 이동: {nextFloorIndex}층");

            Close();
            // Issue #602: 씬 전환 전 TimeScale 스택 리셋
            TimeManager.Instance?.ResetTimeScale();
            LoadBossDungeonSceneAsync().Forget();
        }

        #region Scene Loading

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            // Issue #605: 로비 전환 전 모든 사운드 정지 및 게임 일시정지
            AudioManager.Instance?.StopAllSounds();
            TimeManager.Instance?.Pause();
            await FadeController.Instance.LoadSceneWithFade(SceneName.LobbyScene);
        }

        private async UniTaskVoid LoadBossDungeonSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade(SceneName.BossDungeonScene);
        }

        #endregion
    }
}
