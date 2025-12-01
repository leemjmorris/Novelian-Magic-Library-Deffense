using UnityEngine;
using NovelianMagicLibraryDefense.Core;
using Cysharp.Threading.Tasks;

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

        private void Awake()
        {
            // Panel3 초기 비활성화
            if (panel3 != null)
            {
                panel3.SetActive(false);
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
            await FadeController.Instance.LoadSceneWithFade("LobbyScene");
        }

        private async UniTaskVoid LoadGameSceneAsync()
        {
            await FadeController.Instance.LoadSceneWithFade("GameScene");
        }
    }
}
