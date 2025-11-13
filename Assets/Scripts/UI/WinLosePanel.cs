using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LCB: Manages victory and defeat condition panels in combat
    /// Handles showing appropriate UI based on battle outcome
    /// </summary>
    public class WinLosePanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [Header("Victory UI Elements")]
        [SerializeField] private TextMeshProUGUI victoryStageText;
        [SerializeField] private TextMeshProUGUI victoryRankText;
        [SerializeField] private TextMeshProUGUI victoryTimeText;
        [SerializeField] private TextMeshProUGUI victoryRewardText;
        [SerializeField] private Button victoryLobbyButton;
        [SerializeField] private Button stageSelectButton;

        [Header("Defeat UI Elements")]
        [SerializeField] private TextMeshProUGUI defeatStageText;
        [SerializeField] private TextMeshProUGUI defeatTimeText;
        [SerializeField] private TextMeshProUGUI defeatRankText;
        [SerializeField] private TextMeshProUGUI remainderText;
        [SerializeField] private Button defeatRetryButton;
        [SerializeField] private Button defeatLobbyButton;

        private void Awake()
        {
            //LCB: Setup button listeners on awake
            SetupButtonListeners();

            //LCB: Hide both panels initially
            HideAllPanels();
        }

        private void OnDestroy()
        {
            //LCB: Clean up button listeners
            RemoveButtonListeners();
        }

        #region Button Setup

        //LCB: Setup all button click listeners for both panels
        private void SetupButtonListeners()
        {
            if (victoryLobbyButton != null)
                victoryLobbyButton.onClick.AddListener(OnLobbyButtonClicked);

            if (stageSelectButton != null)
                stageSelectButton.onClick.AddListener(OnStageSelectButtonClicked);

            if (defeatRetryButton != null)
                defeatRetryButton.onClick.AddListener(OnDefeatRetryButtonClicked);

            if (defeatLobbyButton != null)
                defeatLobbyButton.onClick.AddListener(OnLobbyButtonClicked);
        }

        //LCB: Remove all button click listeners
        private void RemoveButtonListeners()
        {
            if (victoryLobbyButton != null)
                victoryLobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);

            if (stageSelectButton != null)
                stageSelectButton.onClick.RemoveListener(OnStageSelectButtonClicked);

            if (defeatRetryButton != null)
                defeatRetryButton.onClick.RemoveListener(OnDefeatRetryButtonClicked);

            if (defeatLobbyButton != null)
                defeatLobbyButton.onClick.RemoveListener(OnLobbyButtonClicked);
        }

        #endregion

        #region Victory Panel Methods

        //LCB: Show victory panel with stage completion data
        public void ShowVictoryPanel(string Rnak, string stageName, float clearTime, int killCount, int reward)
        {
            gameObject.SetActive(true);

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(false);
            }

            if (victoryPanel != null)
            {
                UpdateVictoryInfo(Rnak, stageName, clearTime, killCount, reward);
                
                victoryPanel.SetActive(true);

                Debug.Log("[WinLosePanel] Victory panel displayed");
            }
            //TODO: Implement victory logic (animations, sounds, etc.)
        }

        //LCB: Update victory panel information
        private void UpdateVictoryInfo(string Rnak, string stageName, float clearTime, int killCount, int reward)
        {
            if (victoryRankText != null)
            {
                victoryRankText.text = Rnak;
            }
            if (victoryStageText != null)
            {
                victoryStageText.text = stageName;
            }

            if (victoryTimeText != null)
            {
                int minutes = Mathf.FloorToInt(clearTime / 60f);
                int seconds = Mathf.FloorToInt(clearTime % 60f);
                victoryTimeText.text = $"진행시간 : {minutes:00}:{seconds:00}\n처치 몬스터 : {killCount}마리";
            }

            if (victoryRewardText != null)
            {
                victoryRewardText.text = $"보상: {reward}G";
            }
        }

        //LCB: Handle victory next stage button click
        private void OnLobbyButtonClicked()
        {
            Debug.Log("[WinLosePanel] Lobby button clicked - Logic to be implemented");
            //TODO: Implement Lobby scene loaded logic
        }

        //LCB: Handle victory stage select button click
        private void OnStageSelectButtonClicked()
        {
            Debug.Log("[WinLosePanel] Victory Stage Select button clicked - Logic to be implemented");
            //TODO: Implement stage select logic
        }

        #endregion

        #region Defeat Panel Methods

        //LCB: Show defeat panel with stage failure data
        public void ShowDefeatPanel(string Rnak, string stageName, float survivalTime, int RemainderCount)
        {
            gameObject.SetActive(true);

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (defeatPanel != null)
            {
                
                defeatPanel.SetActive(true);
                UpdateDefeatInfo(Rnak, stageName, survivalTime, RemainderCount);
                Debug.Log("[WinLosePanel] Defeat panel displayed");
            }

            

            //TODO: Implement defeat logic (animations, sounds, etc.)
        }

        //LCB: Update defeat panel information
        private void UpdateDefeatInfo(string Rnak, string stageName, float survivalTime, int RemainderCount)
        {
            if (defeatRankText != null)
            {
                defeatRankText.text = Rnak;
            }
            if (defeatStageText != null)
            {
                defeatStageText.text = stageName;
            }

            if (defeatTimeText != null)
            {
                int minutes = Mathf.FloorToInt(survivalTime / 60f);
                int seconds = Mathf.FloorToInt(survivalTime % 60f);
                defeatTimeText.text = $"전투시간 : {minutes:00}:{seconds:00}";
            }

            if (defeatRankText != null)
            {
                remainderText.text = $"남은 몬스터 : {RemainderCount}마리";
            }
        }

        //LCB: Handle defeat retry button click
        private void OnDefeatRetryButtonClicked()
        {
            Debug.Log("[WinLosePanel] Defeat Retry button clicked - Logic to be implemented");
            //TODO: Implement retry stage logic
        }

        #endregion

        #region Panel Control

        //LCB: Hide all panels
        public void HideAllPanels()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(false);
            }
            gameObject.SetActive(false);

            Debug.Log("[WinLosePanel] All panels hidden");
        }

        //LCB: Check if any panel is currently visible
        public bool IsAnyPanelVisible()
        {
            bool victoryVisible = victoryPanel != null && victoryPanel.activeSelf;
            bool defeatVisible = defeatPanel != null && defeatPanel.activeSelf;
            return victoryVisible || defeatVisible;
        }

        #endregion
    }
}
