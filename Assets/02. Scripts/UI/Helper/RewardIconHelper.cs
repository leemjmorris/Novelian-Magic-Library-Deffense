using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using TMPro;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// 보상 아이콘 표시를 위한 공통 헬퍼 클래스
    /// StagePopUpPanel, BossDungeonPopUpPanel 등에서 재사용
    /// </summary>
    public static class RewardIconHelper
    {
        // 아이콘 캐시 (Addressable 로드 결과)
        private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// RewardGroupID로 보상 아이콘들 생성
        /// </summary>
        /// <param name="rewardGroupId">보상 그룹 ID</param>
        /// <param name="container">아이콘을 생성할 부모 Transform</param>
        /// <param name="prefab">아이콘 프리팹</param>
        /// <param name="spawnedList">생성된 아이콘 오브젝트를 저장할 리스트</param>
        /// <param name="enableTooltip">툴팁 기능 활성화 여부</param>
        public static async UniTask CreateRewardIcons(
            int rewardGroupId,
            Transform container,
            GameObject prefab,
            List<GameObject> spawnedList,
            bool enableTooltip = true)
        {
            if (container == null || prefab == null)
            {
                Debug.LogWarning("[RewardIconHelper] Container or Prefab is null");
                return;
            }

            if (rewardGroupId == 0)
            {
                return;
            }

            var rewardGroupTable = CSVLoader.Instance?.GetTable<RewardGroupData>();
            if (rewardGroupTable == null)
            {
                Debug.LogWarning("[RewardIconHelper] RewardGroupTable not loaded");
                return;
            }

            RewardGroupData rewardGroup = rewardGroupTable.GetId(rewardGroupId);
            if (rewardGroup == null)
            {
                Debug.LogWarning($"[RewardIconHelper] RewardGroup not found: {rewardGroupId}");
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
                Debug.LogWarning("[RewardIconHelper] RewardTable not loaded");
                return;
            }

            // 중복 Item_ID 제거를 위한 HashSet
            HashSet<int> displayedItemIds = new HashSet<int>();

            foreach (int rewardId in rewardIds)
            {
                if (rewardId == 0) continue;

                RewardData reward = rewardTable.GetId(rewardId);
                if (reward == null || reward.Item_ID == 0) continue;

                // 이미 표시된 아이템은 스킵 (중복 방지)
                if (displayedItemIds.Contains(reward.Item_ID)) continue;
                displayedItemIds.Add(reward.Item_ID);

                await CreateSingleRewardIcon(reward, container, prefab, spawnedList, enableTooltip);
            }
        }

        /// <summary>
        /// RewardData 배열로 보상 아이콘들 생성 (직접 RewardData 전달 시 사용)
        /// </summary>
        public static async UniTask CreateRewardIconsFromData(
            RewardData[] rewards,
            Transform container,
            GameObject prefab,
            List<GameObject> spawnedList,
            bool enableTooltip = true)
        {
            if (container == null || prefab == null || rewards == null)
            {
                Debug.LogWarning("[RewardIconHelper] Container, Prefab or Rewards is null");
                return;
            }

            // 중복 Item_ID 제거를 위한 HashSet
            HashSet<int> displayedItemIds = new HashSet<int>();

            foreach (var reward in rewards)
            {
                if (reward == null || reward.Item_ID == 0) continue;

                // 이미 표시된 아이템은 스킵 (중복 방지)
                if (displayedItemIds.Contains(reward.Item_ID)) continue;
                displayedItemIds.Add(reward.Item_ID);

                await CreateSingleRewardIcon(reward, container, prefab, spawnedList, enableTooltip);
            }
        }

        /// <summary>
        /// 단일 보상 아이콘 생성
        /// </summary>
        private static async UniTask CreateSingleRewardIcon(
            RewardData reward,
            Transform container,
            GameObject prefab,
            List<GameObject> spawnedList,
            bool enableTooltip)
        {
            // 아이콘 경로 조회
            string iconKey = GetIconKey(reward.Item_ID);
            if (string.IsNullOrEmpty(iconKey))
            {
                Debug.LogWarning($"[RewardIconHelper] IconKey not found for Item_ID: {reward.Item_ID}");
                return;
            }

            // 아이콘 프리팹 생성
            GameObject iconObj = Object.Instantiate(prefab, container);
            spawnedList.Add(iconObj);

            // 아이콘 로드 및 표시
            Image iconImage = iconObj.GetComponent<Image>();
            if (iconImage == null)
            {
                // 자식에서 Image 찾기
                iconImage = iconObj.GetComponentInChildren<Image>();
            }

            if (iconImage != null)
            {
                await LoadAndSetIcon(iconImage, iconKey);

                // 골드 아이콘(1601), 마석 아이콘(1604, 1605) 크기 조절
                if (reward.Item_ID == 1601 || reward.Item_ID == 1604 || reward.Item_ID == 1605)
                {
                    RectTransform rt = iconObj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = new Vector3(0.5f, 0.5f, 1f);
                    }
                }

                if (enableTooltip)
                {
                    SetupTooltip(iconObj, reward);
                }
            }
            else
            {
                Debug.LogWarning("[RewardIconHelper] Image component not found");
            }
        }

        /// <summary>
        /// 툴팁 설정 (버튼 클릭 시 정보 표시)
        /// </summary>
        private static void SetupTooltip(GameObject iconObj, RewardData reward)
        {
            // 버튼 추가 및 클릭 이벤트 설정
            Button button = iconObj.GetComponent<Button>();
            if (button == null)
            {
                button = iconObj.AddComponent<Button>();
            }

            // 자식에서 툴팁 패널 찾기
            Transform tooltipTransform = iconObj.transform.Find("HideRewardInfo");
            GameObject tooltipPanel = tooltipTransform != null ? tooltipTransform.gameObject : null;
            TextMeshProUGUI tooltipText = tooltipPanel != null ? tooltipPanel.GetComponentInChildren<TextMeshProUGUI>() : null;

            // 골드/마석 아이콘의 툴팁은 스케일 및 위치 보정
            if ((reward.Item_ID == 1601 || reward.Item_ID == 1604 || reward.Item_ID == 1605) && tooltipTransform != null)
            {
                tooltipTransform.localScale = new Vector3(2f, 2f, 1f);
                RectTransform tooltipRect = tooltipTransform.GetComponent<RectTransform>();
                if (tooltipRect != null)
                {
                    tooltipRect.anchoredPosition = new Vector2(tooltipRect.anchoredPosition.x, 237f);
                }
            }

            // 클릭 시 보상 정보 표시
            RewardData capturedReward = reward;
            GameObject capturedTooltipPanel = tooltipPanel;
            TextMeshProUGUI capturedTooltipText = tooltipText;
            button.onClick.AddListener(() =>
            {
                ShowRewardInfo(capturedReward, capturedTooltipPanel, capturedTooltipText);
            });
        }

        /// <summary>
        /// 보상 정보 툴팁 표시
        /// </summary>
        private static void ShowRewardInfo(RewardData reward, GameObject tooltipPanel, TextMeshProUGUI tooltipText)
        {
            if (tooltipPanel == null || tooltipText == null) return;

            // 아이템 이름 조회
            string itemName = GetItemName(reward.Item_ID);

            // 수량 텍스트
            string countText;
            if (reward.Min_Count == reward.Max_Count)
            {
                countText = $"{reward.Min_Count}";
            }
            else
            {
                countText = $"{reward.Min_Count}~{reward.Max_Count}";
            }

            // 확률 텍스트
            string probabilityText = $"{reward.Probability * 100:0.##}%";

            // 툴팁 텍스트 설정
            tooltipText.text = $"{itemName}\n수량: {countText}\n확률: {probabilityText}";

            // 토글 방식: 이미 켜져있으면 끄기
            tooltipPanel.SetActive(!tooltipPanel.activeSelf);
        }

        /// <summary>
        /// 모든 보상 아이콘의 툴팁 숨기기
        /// </summary>
        public static void HideAllTooltips(List<GameObject> spawnedIcons)
        {
            if (spawnedIcons == null) return;

            foreach (var iconObj in spawnedIcons)
            {
                if (iconObj == null) continue;

                Transform tooltipTransform = iconObj.transform.Find("HideRewardInfo");
                if (tooltipTransform != null)
                {
                    tooltipTransform.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 생성된 아이콘들 모두 삭제
        /// </summary>
        public static void ClearIcons(List<GameObject> spawnedIcons)
        {
            if (spawnedIcons == null) return;

            foreach (var iconObj in spawnedIcons)
            {
                if (iconObj != null)
                {
                    Object.Destroy(iconObj);
                }
            }
            spawnedIcons.Clear();
        }

        /// <summary>
        /// Item_ID로 아이콘 키(Addressable Key) 조회
        /// </summary>
        public static string GetIconKey(int itemId)
        {
            int pathId = 0;

            // Item_ID 범위로 타입 구분
            // 1600번대: Currency
            // 10000번대: Ingredient
            if (itemId >= 1600 && itemId < 1700)
            {
                // Currency
                var currencyTable = CSVLoader.Instance?.GetTable<CurrencyData>();
                CurrencyData currency = currencyTable?.GetId(itemId);
                if (currency != null)
                {
                    pathId = currency.Path_ID;
                }
            }
            else if (itemId >= 10000)
            {
                // Ingredient
                var ingredientTable = CSVLoader.Instance?.GetTable<IngredientData>();
                IngredientData ingredient = ingredientTable?.GetId(itemId);
                if (ingredient != null)
                {
                    pathId = ingredient.Path_ID;
                }
            }

            if (pathId == 0) return null;

            // PathTable에서 Addressable_Key 조회
            var pathTable = CSVLoader.Instance?.GetTable<PathData>();
            PathData pathData = pathTable?.GetId(pathId);
            return pathData?.Addressable_Key;
        }

        /// <summary>
        /// Item_ID로 아이템 이름 조회
        /// </summary>
        public static string GetItemName(int itemId)
        {
            int nameId = 0;

            // Item_ID 범위로 타입 구분
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

            if (nameId == 0) return $"아이템 {itemId}";

            // StringTable에서 이름 조회
            var stringTable = CSVLoader.Instance?.GetTable<StringTable>();
            if (stringTable == null) return $"아이템 {itemId}";

            StringTable stringData = stringTable.GetId(nameId);
            return stringData?.Text ?? $"아이템 {itemId}";
        }

        /// <summary>
        /// 아이콘 로드 및 Image에 설정
        /// </summary>
        public static async UniTask LoadAndSetIcon(Image iconImage, string iconKey)
        {
            if (iconImage == null || string.IsNullOrEmpty(iconKey)) return;

            // 캐시 확인
            if (IconCache.TryGetValue(iconKey, out var cachedSprite) && cachedSprite != null)
            {
                iconImage.sprite = cachedSprite;
                return;
            }

            // Addressables로 로드
            try
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(iconKey);
                var locations = await locationsHandle.Task;

                if (locations != null && locations.Count > 0)
                {
                    var handle = Addressables.LoadAssetAsync<Sprite>(iconKey);
                    Sprite icon = await handle.Task;

                    if (icon != null)
                    {
                        IconCache[iconKey] = icon;
                        iconImage.sprite = icon;
                    }
                    else
                    {
                        Debug.LogWarning($"[RewardIconHelper] Icon load failed (null): {iconKey}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[RewardIconHelper] Addressable key not found: {iconKey}");
                }

                Addressables.Release(locationsHandle);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RewardIconHelper] Icon load failed: {iconKey}\n{e.Message}");
            }
        }
    }
}
