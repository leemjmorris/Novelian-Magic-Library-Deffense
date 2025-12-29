using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Core;
using NovelianMagicLibraryDefense.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #511 - 도전던전 확인 패널
    /// 시작하기 버튼 + 던전 출입증 소모 로직
    /// 보상 아이콘 표시 (RewardIconHelper 사용)
    /// </summary>
    public class BossDungeonPopUpPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI floorText;        // "3층"
        [SerializeField] private TextMeshProUGUI costText;         // "보유수량/필요수량"
        [SerializeField] private Button startButton;
        [SerializeField] private Button closeButton;

        [Header("Reward Icons")]
        [SerializeField] private Transform rewardIconContainer;    // 보상 아이콘 컨테이너
        [SerializeField] private GameObject rewardIconPrefab;      // 보상 아이콘 프리팹
        [SerializeField] private GridLayoutGroup gridLayoutGroup;  // Grid Layout Group

        [Header("Warning Panel")]
        [SerializeField] private GameObject deckSetupWarningPanel;

        // 동적 생성된 아이콘 오브젝트 리스트
        private List<GameObject> spawnedIcons = new List<GameObject>();

        private const int ENTRY_COST = 1;
        private readonly Color normalColor = Color.white;
        private readonly Color insufficientColor = Color.red;
        private bool isLoading = false;  // 중복 클릭 방지

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartButtonClicked);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        /// <summary>
        /// 패널 표시 및 UI 업데이트
        /// </summary>
        public void Show()
        {
            if (!SelectedBossDungeon.HasSelection)
            {
                Debug.LogError("[BossDungeonPopUpPanel] 선택된 던전이 없습니다");
                return;
            }

            var dungeonData = SelectedBossDungeon.Data;

            // UI 업데이트
            if (floorText != null)
                floorText.text = $"{dungeonData.Floor_Index}층";

            UpdateCostText();

            // 보상 아이콘 업데이트
            UpdateRewardIcons().Forget();

            // 패널 활성화
            if (panel != null)
                panel.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        /// <summary>
        /// 보상 아이콘 업데이트 (RewardIconHelper 사용)
        /// </summary>
        private async UniTaskVoid UpdateRewardIcons()
        {
            // 기존 아이콘 삭제
            RewardIconHelper.ClearIcons(spawnedIcons);

            if (!SelectedBossDungeon.HasSelection)
            {
                return;
            }

            if (rewardIconContainer == null || rewardIconPrefab == null)
            {
                // Inspector에서 설정 안 된 경우 무시 (정상 동작)
                return;
            }

            // Grid Layout을 1줄로 고정
            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                gridLayoutGroup.constraintCount = 1;
            }

            int rewardGroupId = SelectedBossDungeon.Data.Reward_Group_ID;
            await RewardIconHelper.CreateRewardIcons(
                rewardGroupId,
                rewardIconContainer,
                rewardIconPrefab,
                spawnedIcons,
                enableTooltip: true
            );
        }

        /// <summary>
        /// 비용 텍스트 업데이트 (보유수량/필요수량, 부족시 빨간색)
        /// </summary>
        private void UpdateCostText()
        {
            if (costText == null) return;

            int owned = 0;
            if (CurrencyManager.Instance != null)
            {
                owned = CurrencyManager.Instance.GetCurrency(CurrencyManager.DUNGEON_PASS_ID);
            }

            costText.text = $"{owned}/{ENTRY_COST}";
            costText.color = owned >= ENTRY_COST ? normalColor : insufficientColor;
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void Close()
        {
            // 보상 아이콘 정리
            RewardIconHelper.ClearIcons(spawnedIcons);

            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        /// <summary>
        /// 시작하기 버튼 클릭
        /// StageSceneManager.OnLoadGameScene() 패턴 참고
        /// </summary>
        public void OnStartButtonClicked()
        {
            // 중복 클릭 방지
            if (isLoading)
            {
                Debug.LogWarning("[BossDungeonPopUpPanel] 이미 로딩 중입니다");
                return;
            }

            // 1. 선택된 던전 확인
            if (!SelectedBossDungeon.HasSelection)
            {
                Debug.LogError("[BossDungeonPopUpPanel] 선택된 던전이 없습니다");
                return;
            }

            // 2. 덱 설정 확인
            if (DeckManager.Instance == null || DeckManager.Instance.IsDeckEmpty())
            {
                Debug.LogWarning("[BossDungeonPopUpPanel] 덱이 설정되지 않음!");
                ShowDeckSetupWarning();
                return;
            }

            // 3. 던전 출입증 확인
            if (CurrencyManager.Instance == null)
            {
                Debug.LogError("[BossDungeonPopUpPanel] CurrencyManager가 없습니다");
                return;
            }

            if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST))
            {
                int owned = CurrencyManager.Instance.GetCurrency(CurrencyManager.DUNGEON_PASS_ID);
                Debug.LogWarning($"[BossDungeonPopUpPanel] 던전 출입증 부족! 필요: {ENTRY_COST}, 보유: {owned}");
                WarningUIManager.Instance?.ShowWarning("던전 출입증이 부족합니다");
                return;
            }

            // 4. 던전 출입증 소모
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.DUNGEON_PASS_ID, ENTRY_COST);
            Debug.Log($"[BossDungeonPopUpPanel] 던전 출입증 {ENTRY_COST}개 소모");

            // 5. 로딩 시작 표시
            isLoading = true;

            // 6. 패널 닫기
            Close();

            // 7. 씬 전환
            LoadBossDungeonSceneAsync().Forget();
        }

        private void ShowDeckSetupWarning()
        {
            if (deckSetupWarningPanel != null)
            {
                deckSetupWarningPanel.SetActive(true);
            }
            else
            {
                WarningUIManager.Instance?.ShowWarning("덱을 먼저 설정해주세요");
            }
        }

        public void HideDeckSetupWarning()
        {
            if (deckSetupWarningPanel != null)
            {
                deckSetupWarningPanel.SetActive(false);
            }
        }

        private async UniTaskVoid LoadBossDungeonSceneAsync()
        {
            if (FadeController.Instance != null)
            {
                await FadeController.Instance.LoadSceneWithFade(SceneName.BossDungeonScene);
            }
            else
            {
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneName.BossDungeonScene);
            }
        }
    }
}
