using UnityEngine;
using NovelianMagicLibraryDefense.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor.SearchService;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// StageScene manager for handling scene transitions
    /// Home button -> LobbyScene
    /// Stage button -> GameScene3D
    /// </summary>
    public class StageSceneManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel3;
        [SerializeField] private GameObject deckSetupTextPanel;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private TextMeshProUGUI actionTimeText;
        [SerializeField] private TextMeshProUGUI goldPanelText;
        [SerializeField] private TextMeshProUGUI magicStoneText;
        private int maxAP = 30;

        private void Awake()
        {
            // Panel3 초기 비활성화
            if (panel3 != null)
            {
                panel3.SetActive(false);
            }
        }

        private void OnEnable()
        {
            InitializeCurrency();

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
            }
        }

        private void OnDisable()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
            }
        }

        private void Update()
        {
            UpdateActionTimeText();
        }

        private void UpdateActionTimeText()
        {
            if (actionTimeText == null) return;
            if (CurrencyManager.Instance == null) return;

            int currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
            int max = CurrencyManager.Instance.GetMaxAP();

            // AP가 최대치면 텍스트 비활성화
            if (currentAP >= max)
            {
                actionTimeText.gameObject.SetActive(false);
                return;
            }

            // AP가 최대치 미만이면 남은 시간 표시
            actionTimeText.gameObject.SetActive(true);
            float remainingSeconds = CurrencyManager.Instance.GetAPRecoveryRemainingTime();
            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
            actionTimeText.text = $"{minutes:D2}:{seconds:D2}";
        }

        private void InitializeCurrency()
        {
            // CurrencyTable에서 최대 AP 조회
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var currencyData = CSVLoader.Instance.GetData<CurrencyData>(CurrencyManager.AP_ID);
                if (currencyData != null && currencyData.Currency_Max_Count > 0)
                {
                    maxAP = currencyData.Currency_Max_Count;
                }
            }

            UpdateActionText();
            UpdateGoldText();
            UpdateMagicStoneText();
        }

        private void UpdateActionText()
        {
            if (actionText == null) return;

            int currentAP = 0;
            if (CurrencyManager.Instance != null)
            {
                currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
            }

            actionText.text = $"{currentAP}/{maxAP}";
        }

        private void UpdateGoldText()
        {
            if (goldPanelText == null) return;

            int gold = 0;
            if (CurrencyManager.Instance != null)
            {
                gold = CurrencyManager.Instance.GetCurrency(CurrencyManager.GOLD_ID);
            }

            goldPanelText.text = $"{gold}";
        }

        private void UpdateMagicStoneText()
        {
            if (magicStoneText == null) return;

            int magicStone = 0;
            if (CurrencyManager.Instance != null)
            {
                magicStone = CurrencyManager.Instance.GetCurrency(CurrencyManager.MAGIC_STONE_ID);
            }

            magicStoneText.text = $"{magicStone}";
        }

        private void OnCurrencyChanged(int currencyId, int newAmount)
        {
            if (currencyId == CurrencyManager.AP_ID && actionText != null)
            {
                actionText.text = $"{newAmount}/{maxAP}";
            }
            else if (currencyId == CurrencyManager.GOLD_ID && goldPanelText != null)
            {
                goldPanelText.text = $"{newAmount}";
            }
            else if (currencyId == CurrencyManager.MAGIC_STONE_ID && magicStoneText != null)
            {
                magicStoneText.text = $"{newAmount}";
            }
        }

        public void ShowPanel3()
        {
            if (panel3 != null)
            {
                panel3.SetActive(true);
            }
        }

        public void HidePanel3()
        {
            if (panel3 != null)
            {
                panel3.SetActive(false);
            }
        }

        private void OnHomeButtonClicked()
        {
            Debug.Log("[StageSceneManager] Home button clicked - Loading LobbyScene");
            LoadLobbySceneAsync().Forget();
        }

        private void OnStageStartButtonClicked()
        {
            Debug.Log("[StageSceneManager] Stage button clicked - Loading GameScene3D");
            LoadGameSceneAsync().Forget();
        }

        public void OnLoadLobbyScene()
        {
            LoadLobbySceneAsync().Forget();
        }
        public void OnLoadGameScene()
        {
            // 0. 덱 설정 확인
            if (DeckManager.Instance == null || DeckManager.Instance.IsDeckEmpty())
            {
                Debug.LogWarning("[StageSceneManager] 덱이 설정되지 않음!");
                ShowDeckSetupWarning();
                return;
            }

            // 1. SelectedStage 데이터 확인
            if (!SelectedStage.HasSelection)
            {
                Debug.LogError("[StageSceneManager] 스테이지가 선택되지 않음");
                return;
            }

            var stageData = SelectedStage.Data;
            int apCost = stageData.AP_Cost;

            // 2. AP 잔량 확인
            if (CurrencyManager.Instance == null)
            {
                Debug.LogError("[StageSceneManager] CurrencyManager가 초기화되지 않음");
                return;
            }

            if (!CurrencyManager.Instance.HasEnoughCurrency(CurrencyManager.AP_ID, apCost))
            {
                int currentAP = CurrencyManager.Instance.GetCurrency(CurrencyManager.AP_ID);
                Debug.LogWarning($"[StageSceneManager] AP 부족! 필요: {apCost}, 보유: {currentAP}");
                // TODO JML: AP 부족 팝업 추가하세요
                return;
            }

            // 3. AP 소모
            CurrencyManager.Instance.SpendCurrency(CurrencyManager.AP_ID, apCost);
            Debug.Log($"[StageSceneManager] AP {apCost} 소모. Stage_ID: {stageData.Stage_ID}로 게임 시작");

            // 4. 씬 전환
            LoadGameSceneAsync().Forget();
        }

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade(SceneName.LobbyScene);
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade(SceneName.GameScene);
        }

        private void ShowDeckSetupWarning()
        {
            if (deckSetupTextPanel != null)
            {
                deckSetupTextPanel.SetActive(true);
            }
        }

        public void HideDeckSetupWarning()
        {
            if (deckSetupTextPanel != null)
            {
                deckSetupTextPanel.SetActive(false);
            }
        }
    }
}
