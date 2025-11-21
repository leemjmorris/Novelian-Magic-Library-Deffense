using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// LMJ: Handles victory and defeat screen display
    /// Single responsibility: Manage game result UI (win/lose panels)
    /// </summary>
    public class GameResultPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [Header("Victory UI Elements")]
        [SerializeField] private TextMeshProUGUI victoryRankText;
        [SerializeField] private TextMeshProUGUI victoryStageName;
        [SerializeField] private TextMeshProUGUI victoryClearTime;
        [SerializeField] private TextMeshProUGUI victoryKillCount;
        [SerializeField] private TextMeshProUGUI victoryReward;

        [Header("Defeat UI Elements")]
        [SerializeField] private TextMeshProUGUI defeatRankText;
        [SerializeField] private TextMeshProUGUI defeatStageName;
        [SerializeField] private TextMeshProUGUI defeatSurvivalTime;
        [SerializeField] private TextMeshProUGUI defeatRemainingMonsters;

        [Header("Buttons")]
        [SerializeField] private Button victoryMainMenuButton;
        [SerializeField] private Button victoryNextStageButton;
        [SerializeField] private Button defeatMainMenuButton;
        [SerializeField] private Button defeatRetryButton;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            // Initially hide both panels
            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show victory screen with game results
        /// </summary>
        public void ShowVictoryPanel(string rank, string stageName, float clearTime, int killCount, int reward)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(false);
            }

            // Update victory UI texts
            if (victoryRankText != null)
            {
                victoryRankText.text = rank;
            }

            if (victoryStageName != null)
            {
                victoryStageName.text = stageName;
            }

            if (victoryClearTime != null)
            {
                victoryClearTime.text = FormatTime(clearTime);
            }

            if (victoryKillCount != null)
            {
                victoryKillCount.text = $"Kills: {killCount}";
            }

            if (victoryReward != null)
            {
                victoryReward.text = $"Reward: {reward}";
            }

            Debug.Log($"[GameResultPanel] Victory screen shown: {stageName} - Rank {rank}");
        }

        /// <summary>
        /// Show defeat screen with survival stats
        /// </summary>
        public void ShowDefeatPanel(string rank, string stageName, float survivalTime, int remainingMonsters)
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }

            // Update defeat UI texts
            if (defeatRankText != null)
            {
                defeatRankText.text = rank;
            }

            if (defeatStageName != null)
            {
                defeatStageName.text = stageName;
            }

            if (defeatSurvivalTime != null)
            {
                defeatSurvivalTime.text = FormatTime(survivalTime);
            }

            if (defeatRemainingMonsters != null)
            {
                defeatRemainingMonsters.text = $"Remaining: {remainingMonsters}";
            }

            Debug.Log($"[GameResultPanel] Defeat screen shown: {stageName} - Survived {FormatTime(survivalTime)}");
        }

        /// <summary>
        /// Close the result panel
        /// </summary>
        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(false);
            }

            Debug.Log("[GameResultPanel] Result panel closed");
        }

        /// <summary>
        /// Format time in seconds to MM:SS format
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        #region Button Handlers (to be connected in Inspector)

        public void OnMainMenuButtonClicked()
        {
            Debug.Log("[GameResultPanel] Main Menu button clicked");
            // TODO: Implement scene transition to main menu
            Close();
        }

        public void OnNextStageButtonClicked()
        {
            Debug.Log("[GameResultPanel] Next Stage button clicked");
            // TODO: Implement next stage transition
            Close();
        }

        public void OnRetryButtonClicked()
        {
            Debug.Log("[GameResultPanel] Retry button clicked");
            // TODO: Implement stage retry
            Close();
        }

        #endregion
    }
}
