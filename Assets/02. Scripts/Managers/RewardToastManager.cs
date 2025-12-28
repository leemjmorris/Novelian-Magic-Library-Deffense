using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// 아이템 획득 시 토스트 UI를 표시하는 매니저
    /// BootScene에서 초기화되어 DontDestroyOnLoad로 유지됨
    /// </summary>
    public class RewardToastManager : MonoBehaviour
    {
        private static RewardToastManager instance;
        public static RewardToastManager Instance => instance;

        [Header("Toast Panel (Assign in Inspector)")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemAmountText;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float delayBetweenToasts = 0.2f;

        private CanvasGroup toastCanvasGroup;
        private CancellationTokenSource toastCts;
        private Queue<(int itemId, int amount)> rewardQueue = new Queue<(int, int)>();
        private bool isShowingToast = false;
        private Dictionary<int, Sprite> cachedIcons = new Dictionary<int, Sprite>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // DontDestroyOnLoad는 루트 GameObject에서만 작동하므로 부모에서 분리
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            if (toastPanel != null)
            {
                toastCanvasGroup = toastPanel.GetComponent<CanvasGroup>();
                if (toastCanvasGroup == null)
                {
                    toastCanvasGroup = toastPanel.AddComponent<CanvasGroup>();
                }
                toastPanel.SetActive(false);
            }

            Debug.Log("[RewardToastManager] Initialized");
        }

        /// <summary>
        /// 단일 아이템 보상 토스트 표시
        /// </summary>
        public void ShowReward(int itemId, int amount)
        {
            rewardQueue.Enqueue((itemId, amount));

            if (!isShowingToast)
            {
                ProcessQueueAsync().Forget();
            }
        }

        /// <summary>
        /// 다중 아이템 보상 토스트 표시
        /// </summary>
        public void ShowRewards(List<(int itemId, int amount)> rewards)
        {
            foreach (var reward in rewards)
            {
                rewardQueue.Enqueue(reward);
            }

            if (!isShowingToast)
            {
                ProcessQueueAsync().Forget();
            }
        }

        /// <summary>
        /// 큐에 있는 보상들을 순차적으로 표시
        /// </summary>
        private async UniTaskVoid ProcessQueueAsync()
        {
            isShowingToast = true;

            while (rewardQueue.Count > 0)
            {
                var (itemId, amount) = rewardQueue.Dequeue();
                await ShowSingleToastAsync(itemId, amount);

                if (rewardQueue.Count > 0)
                {
                    await UniTask.Delay((int)(delayBetweenToasts * 1000));
                }
            }

            isShowingToast = false;
        }

        /// <summary>
        /// 단일 토스트 표시 (페이드 인/아웃)
        /// </summary>
        private async UniTask ShowSingleToastAsync(int itemId, int amount)
        {
            if (toastPanel == null || toastCanvasGroup == null)
            {
                Debug.LogWarning("[RewardToastManager] Toast panel references not assigned!");
                return;
            }

            toastCts?.Cancel();
            toastCts?.Dispose();
            toastCts = new CancellationTokenSource();
            var token = toastCts.Token;

            try
            {
                // 아이템 정보 설정
                string itemName = GetItemName(itemId);
                if (itemNameText != null) itemNameText.text = itemName;
                if (itemAmountText != null) itemAmountText.text = $"+{amount}";

                // 아이콘 로드
                await LoadItemIconAsync(itemId, token);

                // 패널 활성화 및 페이드 인
                toastCanvasGroup.alpha = 0f;
                toastPanel.SetActive(true);

                await FadeCanvasGroupAsync(0f, 1f, fadeDuration, token);
                await UniTask.Delay((int)(displayDuration * 1000), cancellationToken: token);
                await FadeCanvasGroupAsync(1f, 0f, fadeDuration, token);

                toastPanel.SetActive(false);
            }
            catch (System.OperationCanceledException)
            {
                // 취소됨
            }
        }

        /// <summary>
        /// 아이템 아이콘 비동기 로드 (PathTable 경유)
        /// Item_ID → IngredientData.Path_ID → PathData.Addressable_Key
        /// </summary>
        private async UniTask LoadItemIconAsync(int itemId, CancellationToken token)
        {
            if (itemIconImage == null) return;

            // 캐시 확인
            if (cachedIcons.TryGetValue(itemId, out Sprite cachedIcon))
            {
                itemIconImage.sprite = cachedIcon;
                return;
            }

            string iconKey;

            // 화폐 아이템 처리 (골드: 1601, 다이아: 1602)
            if (itemId == 1601)
            {
                iconKey = "ItemIcon_Coin_Gold";
            }
            else if (itemId == 1602)
            {
                iconKey = "ItemIcon_Gem_Diamond_Blue";
            }
            else
            {
                // 일반 재료 아이템: IngredientData에서 Path_ID 조회
                var ingredientData = CSVLoader.Instance.GetData<IngredientData>(itemId);
                if (ingredientData == null || ingredientData.Path_ID <= 0)
                {
                    Debug.LogWarning($"[RewardToastManager] IngredientData 없음 또는 Path_ID 없음: {itemId}");
                    return;
                }

                // PathTable에서 Addressable_Key 조회
                var pathData = CSVLoader.Instance.GetData<PathData>(ingredientData.Path_ID);
                if (pathData == null || string.IsNullOrEmpty(pathData.Addressable_Key) || pathData.Addressable_Key == "0")
                {
                    Debug.LogWarning($"[RewardToastManager] PathData 없음 또는 키 없음: Path_ID={ingredientData.Path_ID}");
                    return;
                }

                iconKey = pathData.Addressable_Key;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(iconKey);
                var icon = await handle.ToUniTask(cancellationToken: token);

                if (icon != null)
                {
                    cachedIcons[itemId] = icon;
                    itemIconImage.sprite = icon;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RewardToastManager] Failed to load icon for item {itemId} (key: {iconKey}): {e.Message}");
            }
        }

        /// <summary>
        /// 아이템 ID로 아이템 이름 가져오기
        /// </summary>
        private string GetItemName(int itemId)
        {
            return itemId switch
            {
                10101 => "희미 종이",
                10102 => "응축 종이",
                10103 => "비범 종이",
                10104 => "신성 종이",
                10105 => "고대 종이",
                10106 => "잉크",
                10207 => "로맨스페이지",
                10208 => "코미디페이지",
                10209 => "모험페이지",
                10210 => "공포페이지",
                10211 => "추리페이지",
                10313 => "클립",
                10114 => "룬석",
                1601 => "골드",
                1602 => "다이아",
                _ => $"아이템 #{itemId}"
            };
        }

        /// <summary>
        /// CanvasGroup 알파값 페이드
        /// </summary>
        private async UniTask FadeCanvasGroupAsync(float from, float to, float duration, CancellationToken token)
        {
            float elapsed = 0f;
            toastCanvasGroup.alpha = from;

            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                toastCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                await UniTask.Yield(token);
            }

            toastCanvasGroup.alpha = to;
        }

        private void OnDestroy()
        {
            toastCts?.Cancel();
            toastCts?.Dispose();
        }
    }
}
